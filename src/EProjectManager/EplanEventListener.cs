using Eplan.EplApi.ApplicationFramework;
using System;
using System.Collections.Generic;
using IdleTimeModule;
using IdleTimeModule.EplanAPIHelper;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using IO.View;
using System.Diagnostics.CodeAnalysis;
using EasyEPlanner.Main;
using PInvoke;

namespace EasyEPlanner
{
    [ExcludeFromCodeCoverage]
    class EplanEventListener
    {
        Eplan.EplApi.ApplicationFramework.EventHandler onUserPreCloseProject =
            new Eplan.EplApi.ApplicationFramework.EventHandler();
        Eplan.EplApi.ApplicationFramework.EventHandler onPostOpenProject =
            new Eplan.EplApi.ApplicationFramework.EventHandler();

        Eplan.EplApi.ApplicationFramework.EventHandler onMainEnd =
            new Eplan.EplApi.ApplicationFramework.EventHandler();
        Eplan.EplApi.ApplicationFramework.EventHandler onMainStart =
            new Eplan.EplApi.ApplicationFramework.EventHandler();

        Eplan.EplApi.ApplicationFramework.EventHandler onNotifyPageChanged =
            new Eplan.EplApi.ApplicationFramework.EventHandler();

        private const string OnChangeActiveTabMessageName =
            "AFX_WM_ONCHANGE_ACTIVE_TAB";

        private readonly List<ActiveTabHook> activeTabHooks =
            new List<ActiveTabHook>();
        private static int restartEditModesGeneration;

        public EplanEventListener()
        {
            onUserPreCloseProject
                .SetEvent("Eplan.EplApi.OnUserPreCloseProject");
            onUserPreCloseProject.EplanEvent +=
                new EventHandlerFunction(OnUserPreCloseProject);

            onPostOpenProject.SetEvent("Eplan.EplApi.OnPostOpenProject");
            onPostOpenProject.EplanEvent +=
                new EventHandlerFunction(OnPostOpenProject);

            onMainEnd.SetEvent("Eplan.EplApi.OnMainEnd");
            onMainEnd.EplanEvent +=
                new EventHandlerFunction(OnMainEnd);

            onMainStart.SetEvent("Eplan.EplApi.OnMainStart");
            onMainStart.EplanEvent +=
                new EventHandlerFunction(OnMainStart);

            onNotifyPageChanged.SetEvent("NotifyPageOpened");
            onNotifyPageChanged.EplanEvent +=
                new EventHandlerFunction(OnNotifyPageChanged);

            IEplanHelper eplanHelper = new EplanHelper();
            IModuleConfiguration moduleConfiguration =
                new ModuleConfiguration();
            IRunningProcess runningProcess =
                new RunningProcess(Process.GetCurrentProcess());
            idleTimeModule = new IdleTimeModule.IdleTimeModule(
                eplanHelper, moduleConfiguration, runningProcess);
        }

        private void OnNotifyPageChanged(IEventParameter eventParameter)
        {
            ScheduleRestartEditModesAfterGedChange();
        }

        /// <summary>
        /// Перезапуск interaction привязки на UI-потоке EPLAN
        /// (после смены вкладки или страницы GED).
        /// </summary>
        private static void ScheduleRestartEditModesAfterGedChange(
            int delayMs = 100)
        {
            if (!EProjectManager.GetInstance().IsEditBindingActive())
                return;

            int generation = Interlocked.Increment(ref restartEditModesGeneration);
            Thread.Sleep(delayMs);
            if (generation != restartEditModesGeneration)
                return;
            if (!EProjectManager.GetInstance().IsEditBindingActive())
                return;

            EProjectManager.GetInstance().RestartEditModes();
        }

        private void InstallOnChangeActiveTabHooks()
        {
            UninstallOnChangeActiveTabHooks();

            uint messageId = PI.RegisterWindowMessage(
                OnChangeActiveTabMessageName);
            if (messageId == 0)
                return;

            var hookedThreads = new HashSet<uint>();
            foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
            {
                try
                {
                    uint threadId = (uint)thread.Id;
                    if (!hookedThreads.Add(threadId))
                        continue;

                    var hook = new ActiveTabHook(messageId,
                        () => ScheduleRestartEditModesAfterGedChange());
                    if (hook.Install(threadId))
                        activeTabHooks.Add(hook);
                }
                catch (InvalidOperationException)
                {
                    // do nothing
                }
            }
        }

        private void UninstallOnChangeActiveTabHooks()
        {
            foreach (ActiveTabHook hook in activeTabHooks)
                hook.Uninstall();

            activeTabHooks.Clear();
        }

        private sealed class ActiveTabHook
        {
            private readonly uint messageId;
            private readonly System.Action scheduleRestart;
            private readonly PI.HookProc hookProc;
            private IntPtr hookHandle = IntPtr.Zero;

            public ActiveTabHook(uint messageId, System.Action scheduleRestart)
            {
                this.messageId = messageId;
                this.scheduleRestart = scheduleRestart;
                hookProc = HookCallback;
            }

            public bool Install(uint threadId)
            {
                hookHandle = PI.SetWindowsHookEx(
                    PI.HookType.WH_CALLWNDPROC,
                    hookProc,
                    IntPtr.Zero,
                    threadId);
                return hookHandle != IntPtr.Zero;
            }

            public void Uninstall()
            {
                if (hookHandle == IntPtr.Zero)
                    return;

                PI.UnhookWindowsHookEx(hookHandle);
                hookHandle = IntPtr.Zero;
            }

            private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
            {
                if (code >= 0 && messageId != 0)
                {
                    var msg = (PI.CWPSTRUCT)Marshal.PtrToStructure(
                        lParam, typeof(PI.CWPSTRUCT));
                    if (msg.message == messageId)
                        scheduleRestart();
                }

                return PI.CallNextHookEx(hookHandle, code, wParam, lParam);
            }
        }

        private void OnUserPreCloseProject(IEventParameter iEventParameter)
        {
            string projectName =
                (new EventParameterString(iEventParameter)).String;
            int dot_pos = projectName.LastIndexOf('\\');
            if (dot_pos > 0)
            {
                projectName = projectName.Substring(dot_pos + 1);
            }

            projectName = projectName.Remove(projectName.IndexOf('.'));

            string currentProjectName = EProjectManager.GetInstance()
                .GetCurrentProjectName();
            if (projectName == currentProjectName)
            {
                EProjectManager.GetInstance().SaveAndClose();

                DFrm.GetInstance().ShowNoDevices();
                IOViewControl.Instance?.Clear();
                EasyEPlanner.Devices.View.DevicesViewControl.Instance?.Clear();
                EasyEPlanner.Binding.View.BindingViewControl.Instance?.Clear();

                EProjectManager.GetInstance().ResetCurrentPrj();
                EProjectManager.isPreCloseProjectComplete = true;
            }
        }

        private void OnPostOpenProject(IEventParameter iEventParameter)
        {
            // Если нет активного проекта, считываем описание и работаем с ним.
            if (EProjectManager.GetInstance().GetCurrentPrj() == null)
            {
                // Получение текущего проекта.
                var selectionSet = new Eplan.EplApi.HEServices.SelectionSet();
                selectionSet.LockSelectionByDefault = false;
                selectionSet.LockProjectByDefault = false;

                EProjectManager.GetInstance()
                    .SetCurrentPrj(selectionSet.GetCurrentProject(true));

                string strAction = "LoadDescriptionAction";
                var oAMnr = new ActionManager();
                Eplan.EplApi.ApplicationFramework.Action oAction =
                    oAMnr.FindAction(strAction);
                var ctx = new ActionCallingContext();

                oAction?.Execute(ctx);

                AttemptRestoreWindow(oAMnr.FindAction(nameof(ShowTechObjectsAction)),
                    ctx, "show_obj_window");

                AttemptRestoreWindow(oAMnr.FindAction(nameof(ShowDevicesNewAction)),
                    ctx, EasyEPlanner.Devices.View.DevicesViewControl.CfgShowWindowKey);

                AttemptRestoreWindow(oAMnr.FindAction(nameof(ShowBindingAction)),
                    ctx, EasyEPlanner.Binding.View.BindingViewControl.CfgShowWindowKey);

                AttemptRestoreWindow(oAMnr.FindAction(nameof(ShowPlcAction)),
                    ctx, IOViewControl.CfgShowWindowKey);

                // Проект открыт, ставим флаг в изначальное состояние.
                EProjectManager.isPreCloseProjectComplete = false;
            }

            InstallOnChangeActiveTabHooks();
        }

        /// <summary>
        /// Восстановить окно при необходимости
        /// </summary>
        /// <param name="action">Действие по открытию окна</param>
        /// <param name="ctx">Контекст запуска действия</param>
        /// <param name="cfgWindowKey">Ключ в конфигурации</param>
        private static void AttemptRestoreWindow(
            Eplan.EplApi.ApplicationFramework.Action action,
            ActionCallingContext ctx,
            string cfgWindowKey)
        {
            var iniFile = new PInvoke.IniFile(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                @"\Eplan\eplan.cfg");

            string res = iniFile.ReadString("main", cfgWindowKey, "false")
                .Trim().ToLower();

            if (res is "true")
            {
                action.Execute(ctx);
            }
        }

        private void OnMainEnd(IEventParameter iEventParameter)
        {
            UninstallOnChangeActiveTabHooks();
            Interlocked.Increment(ref restartEditModesGeneration);

            if (EProjectManager.GetInstance().GetCurrentPrj() != null)
            {
                EProjectManager.GetInstance().SaveAndClose();

                // Проверяю, закрыт ли проект(выполнено ли PreCloseProject)
                // или же он закрыт через закрытие окна Eplan
                if (EProjectManager.isPreCloseProjectComplete == false)
                {
                    // Если проект закрыт через крестик
                    // при новом открытии Eplan не открывать окна
                    DFrm.SaveCfg(false);
                    ModeFrm.SaveCfg(false);
                    Editor.NewEditorControl.SaveCfg(false);
                    IOViewControl.SaveCfg(false);
                    EasyEPlanner.Devices.View.DevicesViewControl.SaveCfg(false);
                }
            }

            idleTimeModule.Stop();
        }

        private void OnMainStart(IEventParameter iEventParameter)
        {
            idleTimeModule.BeforeClosingProject +=
                EProjectManager.GetInstance().SyncAndSave;
            idleTimeModule.Start(AddInModule.OriginalAssemblyPath);
            InstallOnChangeActiveTabHooks();
        }

        private IIdleTimeModule idleTimeModule;
    }
}
