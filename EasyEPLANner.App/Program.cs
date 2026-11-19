using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EasyEPlanner;
using EasyEPlanner.Binding.View;
using EasyEPlanner.Devices.View;
using EasyEPlanner.ModbusExchange.View;
using EplanDevice;
using InterprojectExchange;
using IO;
using IO.View;
using WeifenLuo.WinFormsUI.Docking;

namespace EasyEPlanner.App
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.SetUnhandledExceptionMode(
                UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
                MessageBox.Show(e.Exception.ToString(), "EasyEPLANner");
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                MessageBox.Show(e.ExceptionObject.ToString(), "EasyEPLANner");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    [ExcludeFromCodeCoverage]
    public class MainForm : Form
    {
        private const string TechEditorPersistString = "EasyEPlanner.TechEditor";
        private const string DevicesPersistString = "EasyEPlanner.Devices";
        private const string PlcPersistString = "EasyEPlanner.Plc";
        private const string BindingPersistString = "EasyEPlanner.Binding";

        private readonly DockPanel dockPanel;
        private readonly StatusStrip statusStrip;
        private readonly ToolStripStatusLabel statusLabel;

        private AppDockContent techEditorDoc;
        private AppDockContent devicesDoc;
        private AppDockContent plcDoc;
        private AppDockContent bindingDoc;

        public MainForm()
        {
            Text = "EasyEPLANner";
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                Application.ExecutablePath) ?? SystemIcons.Application;
            Width = 1200;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;
            RestoreMainWindowBounds();

            var menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("Файл");
            fileMenu.DropDownItems.Add("Открыть папку проекта...", null,
                (_, __) => OpenProjectFolder());
            fileMenu.DropDownItems.Add("Сохранить", null,
                (_, __) => SaveProject());
            fileMenu.DropDownItems.Add("Закрыть проект", null,
                (_, __) => CloseProject());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Выход", null, (_, __) => Close());

            var viewMenu = new ToolStripMenuItem("Вид");
            viewMenu.DropDownItems.Add("Редактор технологических объектов",
                null, (_, __) => ShowTechEditor());
            viewMenu.DropDownItems.Add("Устройства", null,
                (_, __) => ShowDevices());
            viewMenu.DropDownItems.Add("Структура ПЛК", null,
                (_, __) => ShowPlc());
            viewMenu.DropDownItems.Add("Привязка", null,
                (_, __) => ShowBinding());

            var toolsMenu = new ToolStripMenuItem("Сервис");
            toolsMenu.DropDownItems.Add("Modbus-обмен", null,
                (_, __) => ShowModbus());
            toolsMenu.DropDownItems.Add("Обмен сигналами между проектами", null,
                (_, __) => ShowInterprojectExchange());

            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(viewMenu);
            menuStrip.Items.Add(toolsMenu);

            dockPanel = new DockPanel
            {
                Dock = DockStyle.Fill,
                DocumentStyle = DocumentStyle.DockingWindow,
                Theme = new VS2015LightTheme(),
            };

            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Откройте папку проекта");
            statusStrip.Items.Add(statusLabel);

            Controls.Add(dockPanel);
            Controls.Add(statusStrip);
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;

            BootstrapManagers();
            Shown += (_, __) => RestoreLastSession();
            FormClosing += (_, __) => SaveWindowState();
        }

        private static void BootstrapManagers()
        {
            try
            {
                string assemblyDir = Path.GetDirectoryName(
                    typeof(ProjectManager).Assembly.Location);
                string systemLua = Path.Combine(assemblyDir, "Lua");
                var bootstrapContext = new FileProjectContext(
                    Path.GetTempPath(), systemLua, assemblyDir);
                ProjectManager.GetInstance().InitStandalone(bootstrapContext);
                ProjectContextHolder.Current = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка инициализации:\n" + ex,
                    "EasyEPLANner",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void RestoreMainWindowBounds()
        {
            try
            {
                if (!StandaloneAppState.TryLoadWindowBounds(out Rectangle bounds,
                    out FormWindowState windowState) ||
                    !Screen.AllScreens.Any(screen =>
                        screen.WorkingArea.IntersectsWith(bounds)))
                {
                    return;
                }

                StartPosition = FormStartPosition.Manual;
                Bounds = bounds;
                WindowState = windowState;
            }
            catch
            {
                // Bad or unavailable per-user settings should not stop startup.
            }
        }

        private void RestoreLastSession()
        {
            bool hasSavedLayout = LoadDockLayout();
            string lastProject;
            try
            {
                lastProject = StandaloneAppState.LoadLastProject();
            }
            catch
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(lastProject))
                return;

            if (!FileProjectContext.IsValidProjectFolder(lastProject))
            {
                try
                {
                    StandaloneAppState.ClearLastProject();
                }
                catch
                {
                    // Ignore stale settings that cannot be removed.
                }

                HideProjectDocuments();
                statusLabel.Text = "Последний проект не найден. Откройте папку проекта.";
                return;
            }

            OpenFolder(lastProject, !hasSavedLayout);
        }

        private bool LoadDockLayout()
        {
            if (!File.Exists(StandaloneAppState.DockLayoutPath))
                return false;

            try
            {
                dockPanel.LoadFromXml(StandaloneAppState.DockLayoutPath,
                    GetContentFromPersistString);
                return dockPanel.Contents.Count > 0;
            }
            catch
            {
                foreach (IDockContent content in dockPanel.Contents.ToArray())
                    (content as DockContent)?.Hide();
                return false;
            }
        }

        private IDockContent GetContentFromPersistString(string persistString)
        {
            switch (persistString)
            {
                case TechEditorPersistString:
                    return EnsureDockDoc(ref techEditorDoc,
                        "Редактор технологических объектов",
                        TechEditorPersistString);
                case DevicesPersistString:
                    return EnsureDockDoc(ref devicesDoc, "Устройства",
                        DevicesPersistString);
                case PlcPersistString:
                    return EnsureDockDoc(ref plcDoc, "Структура ПЛК",
                        PlcPersistString);
                case BindingPersistString:
                    return EnsureDockDoc(ref bindingDoc, "Привязка",
                        BindingPersistString);
                default:
                    return null;
            }
        }

        private void SaveWindowState()
        {
            try
            {
                Rectangle bounds = WindowState == FormWindowState.Normal
                    ? Bounds
                    : RestoreBounds;
                StandaloneAppState.SaveWindowBounds(bounds, WindowState);
            }
            catch
            {
                // Persistence failure must not prevent application shutdown.
            }

            try
            {
                StandaloneAppState.SaveLastProject(
                    ProjectContextHolder.Current?.ProjectFolderPath);
            }
            catch
            {
                // Persistence failure must not prevent application shutdown.
            }

            try
            {
                StandaloneAppState.EnsureDockLayoutDirectory();
                dockPanel.SaveAsXml(StandaloneAppState.DockLayoutPath,
                    Encoding.UTF8);
            }
            catch
            {
                // The application can still close if the layout cannot be saved.
            }
        }

        private void OpenProjectFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Выберите папку проекта с main.objects.lua";

                string defaultRoot = ProjectManager.GetInstance()
                    .GetDefaultProjectsRootPath();
                if (!string.IsNullOrEmpty(defaultRoot))
                    dialog.SelectedPath = defaultRoot;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                OpenFolder(dialog.SelectedPath);
            }
        }

        private void OpenFolder(string folder, bool activateTechEditor = true)
        {
            if (!FileProjectContext.TryValidateProjectFolder(folder,
                out string projectFolder, out string errorReason))
            {
                string displayPath = string.IsNullOrWhiteSpace(projectFolder)
                    ? "(путь не указан)"
                    : projectFolder;
                string message = $"Открываемая папка \"{displayPath}\" " +
                    $"не распознана как проект: {errorReason}.";
                statusLabel.Text = message;
                MessageBox.Show(this, message, "Папка не распознана",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            folder = projectFolder;
            Cursor = Cursors.WaitCursor;
            statusLabel.Text = "Загрузка проекта...";
            statusStrip.Refresh();
            Application.DoEvents();

            try
            {
                string assemblyDir = Path.GetDirectoryName(
                    typeof(ProjectManager).Assembly.Location);
                string systemLua = Path.Combine(assemblyDir, "Lua");
                var context = new FileProjectContext(folder, systemLua,
                    assemblyDir);
                folder = context.ProjectFolderPath;
                ProjectContextHolder.Current = context;

                ClearProjectData();

                // Сначала устройства из main.io.lua — иначе при загрузке
                // объектов Step.AddDev не находит индексы устройств.
                string mainIoPath = Path.Combine(folder, "main.io.lua");
                if (File.Exists(mainIoPath))
                {
                    statusLabel.Text = "Загрузка main.io.lua...";
                    statusStrip.Refresh();
                    Application.DoEvents();

                    LuaMainIoLoader.LoadFromFile(mainIoPath);
                    IOManager.GetInstance().CalculateIOLinkAdresses();
                }

                statusLabel.Text = "Загрузка технологических объектов...";
                statusStrip.Refresh();
                Application.DoEvents();

                var projectManager = ProjectManager.GetInstance();
                projectManager.LoadTechObjectsFromContext(out string errors);

                IOViewControl.RefreshFromIOManager();
                DevicesViewControl.Instance?.RebuildTree();
                BindingViewControl.Instance?.RebuildTree();

                if (activateTechEditor)
                    ShowTechEditor();
                else if (techEditorDoc != null && !techEditorDoc.IsDisposed &&
                    techEditorDoc.Visible)
                    projectManager.StartEditInHost(techEditorDoc.HostPanel);

                projectManager.RefreshEditor();
                RefreshOpenSidePanels();
                StandaloneAppState.SaveLastProject(context.ProjectFolderPath);

                statusLabel.Text = string.IsNullOrEmpty(errors)
                    ? $"Проект: {context.ProjectName}"
                    : $"Проект: {context.ProjectName}. {errors}";
            }
            catch (Exception ex)
            {
                try
                {
                    ClearProjectData();
                    ProjectContextHolder.Current = null;
                }
                catch
                {
                    ProjectContextHolder.Current = null;
                }

                HideProjectDocuments();
                try
                {
                    StandaloneAppState.ClearLastProject();
                }
                catch
                {
                    // Keep the loading error as the primary failure.
                }

                statusLabel.Text = "Ошибка загрузки проекта";
                MessageBox.Show(this, ex.Message, "Ошибка открытия проекта",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void RefreshOpenSidePanels()
        {
            if (devicesDoc != null && !devicesDoc.IsDisposed &&
                devicesDoc.Visible)
            {
                DevicesViewControl.StartInHost(devicesDoc.HostPanel);
            }

            if (plcDoc != null && !plcDoc.IsDisposed && plcDoc.Visible)
            {
                IOViewControl.StartInHost(plcDoc.HostPanel);
            }

            if (bindingDoc != null && !bindingDoc.IsDisposed &&
                bindingDoc.Visible)
            {
                BindingViewControl.StartInHost(bindingDoc.HostPanel);
            }
            else if (BindingViewControl.Instance != null &&
                !BindingViewControl.Instance.IsDisposed)
            {
                BindingViewControl.Instance.RebuildTree();
            }
        }

        private void CloseProject()
        {
            if (ProjectContextHolder.Current == null)
                return;

            DialogResult saveChoice = MessageBox.Show(this,
                "Сохранить проект перед закрытием?", "Закрытие проекта",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (saveChoice == DialogResult.Cancel ||
                (saveChoice == DialogResult.Yes && !SaveProject()))
            {
                return;
            }

            try
            {
                ClearProjectData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка закрытия проекта",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ProjectContextHolder.Current = null;
                HideProjectDocuments();
                try
                {
                    StandaloneAppState.ClearLastProject();
                }
                catch
                {
                    // A settings write failure must not leave the project loaded.
                }

                statusLabel.Text = "Проект закрыт. Откройте папку проекта.";
            }
        }

        private void ClearProjectData()
        {
            DeviceManager.GetInstance().Clear();
            IOManager.GetInstance().Clear();
            IOManager.GetInstance().ResetStoredNtypes();
            TechObject.TechObjectManager.GetInstance().ClearProject();
            ProjectManager.GetInstance().RefreshEditor();

            IOViewControl.RefreshFromIOManager();
            DevicesViewControl.Instance?.RebuildTree();
            BindingViewControl.Instance?.RebuildTree();
        }

        private void HideProjectDocuments()
        {
            techEditorDoc?.Hide();
            devicesDoc?.Hide();
            plcDoc?.Hide();
            bindingDoc?.Hide();
        }

        private bool SaveProject()
        {
            if (ProjectContextHolder.Current == null)
            {
                MessageBox.Show(this, "Сначала откройте папку проекта.",
                    "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            try
            {
                if (!ProjectManager.TrySaveTechObjectsFromContext(false))
                    return false;

                statusLabel.Text =
                    $"Сохранено: {ProjectContextHolder.Current.ProjectName}";
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка сохранения",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void ShowTechEditor()
        {
            if (!EnsureProjectLoaded())
                return;

            techEditorDoc = EnsureDockDoc(ref techEditorDoc,
                "Редактор технологических объектов", TechEditorPersistString);
            ProjectManager.GetInstance()
                .StartEditInHost(techEditorDoc.HostPanel);
            techEditorDoc.ShowOrActivate(dockPanel);
        }

        private void ShowDevices()
        {
            if (!EnsureProjectLoaded())
                return;

            devicesDoc = EnsureDockDoc(ref devicesDoc, "Устройства",
                DevicesPersistString);
            DevicesViewControl.StartInHost(devicesDoc.HostPanel);
            devicesDoc.ShowOrActivate(dockPanel);
        }

        private void ShowPlc()
        {
            if (!EnsureProjectLoaded())
                return;

            plcDoc = EnsureDockDoc(ref plcDoc, "Структура ПЛК",
                PlcPersistString);
            IOViewControl.StartInHost(plcDoc.HostPanel);
            plcDoc.ShowOrActivate(dockPanel);
        }

        private void ShowBinding()
        {
            if (!EnsureProjectLoaded())
                return;

            bindingDoc = EnsureDockDoc(ref bindingDoc, "Привязка",
                BindingPersistString);
            BindingViewControl.StartInHost(bindingDoc.HostPanel);
            bindingDoc.ShowOrActivate(dockPanel);
        }

        private static AppDockContent EnsureDockDoc(ref AppDockContent doc,
            string title, string persistString)
        {
            if (doc == null || doc.IsDisposed)
                doc = new AppDockContent(title, persistString);
            return doc;
        }

        private void ShowModbus()
        {
            if (!EnsureProjectLoaded())
                return;

            using (var view = new ModbusExchangeView())
            {
                view.ShowDialog(this);
            }
        }

        private void ShowInterprojectExchange()
        {
            if (!EnsureProjectLoaded())
                return;

            try
            {
                new InterprojectExchangeStarter().Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message,
                    "Обмен сигналами между проектами",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool EnsureProjectLoaded()
        {
            if (ProjectContextHolder.Current != null)
                return true;

            MessageBox.Show(this, "Сначала откройте папку проекта.",
                "EasyEPLANner", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
    }
}
