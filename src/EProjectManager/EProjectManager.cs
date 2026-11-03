using System;
using Eplan.EplApi.DataModel;
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.HEServices;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using IO.View;

namespace EasyEPlanner
{
    /// <summary>
    /// Менеджер проекта в CAD Eplan.
    /// </summary>
    class EProjectManager : IEProjectManager
    {
        public static EProjectManager GetInstance()
        {
            if (instance == null)
            {
                instance = new EProjectManager();
            }
            return instance;
        }

        private EProjectManager()
        {
            eplanEventListener = new EplanEventListener();
            actMnr = new ActionManager();
        }

        [ExcludeFromCodeCoverage]
        public void StartEditModesWithDelay(int ms = 1)
        {
            System.Threading.Thread.Sleep(ms);
            StartEditModes();
        }

        [ExcludeFromCodeCoverage]
        public void StartEditModes()
        {
            if (selectInteractionWhileEditModes?.IsFinish is false)
                return;

            string strAction = "XGedStartInteractionAction";
            if (startInteractionAction == null)
            {
                startInteractionAction = actMnr.FindAction(strAction);
            }

            if (startInteractionAction != null)
            {
                var oContext = new ActionCallingContext();
                oContext.AddParameter("Name",
                    "SelectInteractionWhileEditModes");
                startInteractionAction.Execute(oContext);
            }

            EnabledEditMode = true;
        }

        [ExcludeFromCodeCoverage]
        public bool IsEditBindingActive()
        {
            if (EnabledEditMode)
                return true;

            return Editor.Editor.GetInstance()?.Editable ?? false;
        }

        [ExcludeFromCodeCoverage]
        public void RestartEditModes()
        {
            if (!IsEditBindingActive())
                return;

            EnabledEditMode = true;
            isRestartingEditModes = true;
            try
            {
                if (selectInteractionWhileEditModes != null)
                {
                    selectInteractionWhileEditModes.OnStop();
                    selectInteractionWhileEditModes = null;
                }

                StartEditModes();
            }
            finally
            {
                isRestartingEditModes = false;
            }
        }

        [ExcludeFromCodeCoverage]
        public bool ShouldAutorestartInteraction =>
            IsEditBindingActive() && !isRestartingEditModes;

        [ExcludeFromCodeCoverage]
        public void StopEditModes()
        {
            EnabledEditMode = false;

            if (selectInteractionWhileEditModes != null)
            {
                selectInteractionWhileEditModes.OnStop();
                selectInteractionWhileEditModes = null;
            }
        }

        public void SetEditInteraction(SelectInteractionWhileEditModes inter)
        {
            selectInteractionWhileEditModes = inter;
        }

        public SelectInteractionWhileEditModes GetEditInteraction()
        {
            return selectInteractionWhileEditModes;
        }

        /// <summary>
        /// Проверка имени проекта
        /// </summary>
        /// <param name="name">Имя проекта</param>
        public void CheckProjectName(ref string name)
        {
            const int MaxProjectNameLength = 70;
            if (name.Length > MaxProjectNameLength)
            {
                string errorMessage = $"Некорректно задано имя проекта - " +
                    $"длина не должна превышать {MaxProjectNameLength}" +
                    $"символов. ";
                Logs.AddMessage(errorMessage);
                int startSubstrIndex = 0;
                name = name.Substring(startSubstrIndex, MaxProjectNameLength);
            }

            const string RegexPattern =
                "(?<plant>[A-Z]+[1-9]{1})\\-(?<project>[а-яА-Я0-9\\-]+$)";
            Match match = Regex.Match(name, RegexPattern);
            if (!match.Success)
            {
                string errorMessage = "Некорректно задано имя проекта - " +
                    "рекомендуется для имени площадки использовать " +
                    "английский, а для проекта - русский, вместо пробелов - " +
                    "знак минус (прим., LOC1-Название-проекта). ";
                Logs.AddMessage(errorMessage);
                name = GetModifyingCurrentProjectName();
            }
        }

        public string GetCurrentProjectName()
        {
            string name = currentProject.ProjectName;
            return name;
        }

        public string GetModifyingCurrentProjectName()
        {
            string name = currentProject.ProjectName.Replace(" ", "-");
            return name;
        }

        public Project GetCurrentPrj()
        {
            return currentProject;
        }

        public void ResetCurrentPrj()
        {
            currentProject = null;
        }

        public void SetCurrentPrj(Project project)
        {
            currentProject = project;
        }

        [ExcludeFromCodeCoverage]
        public void SyncAndSave(bool saveDescrSilentMode = true)
        {
            if (currentProject == null || !ProjectDataIsLoaded)
                return;

            if (!IsManagedProjectActive())
            {
                if (!saveDescrSilentMode)
                {
                    MessageBox.Show(
                        "Синхронизация и сохранение доступны только для " +
                        $"проекта «{GetCurrentProjectName()}», с которым " +
                        "работает дополнение. Переключитесь на этот проект " +
                        "в EPLAN или закройте остальные открытые проекты.",
                        "EPlaner",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                return;
            }

            String strAction = "LoadDescriptionAction";
            ActionManager oAMnr = new ActionManager();
            Eplan.EplApi.ApplicationFramework.Action oAction =
                oAMnr.FindAction(strAction);
            ActionCallingContext ctx = new ActionCallingContext();

            if (oAction != null)
            {
                ctx.AddParameter("loadFromLua", "no");
                oAction.Execute(ctx);
            }

            strAction = "SaveDescriptionAction";
            oAction = oAMnr.FindAction(strAction);
            if (oAction != null)
            {
                ctx.AddParameter("silentMode",
                    saveDescrSilentMode ? "yes" : "no");
                oAction.Execute(ctx);
            }
        }

        /// <summary>
        /// Активный в EPLAN проект совпадает с проектом дополнения.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public bool IsManagedProjectActive()
        {
            if (currentProject == null)
                return false;

            var selectionSet = new SelectionSet();
            selectionSet.LockSelectionByDefault = false;
            selectionSet.LockProjectByDefault = false;
            Project activeProject = selectionSet.GetCurrentProject(false);

            return activeProject != null &&
                activeProject.ProjectName == currentProject.ProjectName;
        }

        /// <summary>
        /// Сохранить данные и закончить работу с дополнением (при закрытии
        /// проекта или Eplan).
        /// </summary>
        public void SaveAndClose()
        {
            SyncAndSave();
            StopEditModes();

            string projectName = currentProject.ProjectName;
            string projectDirPath = currentProject.ProjectDirectoryPath;
            ExcelRepoter.AutomaticExportExcelForSCADA(projectDirPath,
                projectName);
            var xmlReporter = new XmlReporter();
            xmlReporter.AutomaticExportNewChannelBaseCombineTags(
                projectDirPath, projectName);

            // Проверка и сохранение состояний окон.
            ModeFrm.CheckShown();
            ModeFrm.SaveCfg(ModeFrm.modeIsShown);
            DFrm.CheckShown();
            DFrm.SaveCfg(DFrm.deviceIsShown);
            Editor.NewEditorControl.CheckShown();
            Editor.NewEditorControl.SaveCfg();
            IOViewControl.SaveCfg();
            EasyEPlanner.Devices.View.DevicesViewControl.SaveCfg();
            EasyEPlanner.Binding.View.BindingViewControl.SaveCfg();

            if (Editor.Editor.GetInstance().IsShown())
            {
                Editor.Editor.GetInstance().CloseEditor();
            }

            ModeFrm.GetInstance().CloseEditor();
            DFrm.GetInstance().CloseEditor();
            EasyEPlanner.Binding.View.BindingViewControl.Instance?.CloseEditor();
        }

        /// <summary>
        /// Включен ли режим редактирования объектов
        /// </summary>
        public bool EnabledEditMode { get; set; }

        /// <summary>
        /// Загружены или нет данные проекта.
        /// </summary>
        public bool ProjectDataIsLoaded { get; set; } = false;

        private Project currentProject = null;
        private SelectInteractionWhileEditModes
            selectInteractionWhileEditModes = null;
        private bool isRestartingEditModes;
        private EplanEventListener eplanEventListener;
        private ActionManager actMnr;
        private Eplan.EplApi.ApplicationFramework.Action startInteractionAction;

        private static EProjectManager instance;
        public static bool isPreCloseProjectComplete = false;
    }
}