using System;
using System.IO;
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
                MessageBox.Show(e.Exception.ToString(), "EasyEPlanner");
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                MessageBox.Show(e.ExceptionObject.ToString(), "EasyEPlanner");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private readonly MenuStrip menuStrip;
        private readonly DockPanel dockPanel;
        private readonly StatusStrip statusStrip;
        private readonly ToolStripStatusLabel statusLabel;

        private AppDockContent techEditorDoc;
        private AppDockContent devicesDoc;
        private AppDockContent plcDoc;
        private AppDockContent bindingDoc;

        public MainForm()
        {
            Text = "EasyEPlanner";
            Width = 1200;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;

            menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("Файл");
            fileMenu.DropDownItems.Add("Открыть папку проекта...", null,
                (_, __) => OpenProjectFolder());
            fileMenu.DropDownItems.Add("Сохранить", null,
                (_, __) => SaveProject());
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
        }

        private void BootstrapManagers()
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
                    "EasyEPlanner",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

        private void OpenFolder(string folder)
        {
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
                ProjectContextHolder.Current = context;

                DeviceManager.GetInstance().Clear();
                IOManager.GetInstance().Clear();

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

                ProjectManager.GetInstance()
                    .LoadTechObjectsFromContext(out string errors);

                IOViewControl.RefreshFromIOManager();
                DevicesViewControl.Instance?.RebuildTree();
                BindingViewControl.Instance?.RebuildTree();

                ShowTechEditor();
                RefreshOpenSidePanels();

                statusLabel.Text = string.IsNullOrEmpty(errors)
                    ? $"Проект: {context.ProjectName}"
                    : $"Проект: {context.ProjectName}. {errors}";
            }
            catch (Exception ex)
            {
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

        private void SaveProject()
        {
            if (ProjectContextHolder.Current == null)
            {
                MessageBox.Show(this, "Сначала откройте папку проекта.",
                    "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                ProjectManager.GetInstance().SaveTechObjectsFromContext(false);
                statusLabel.Text =
                    $"Сохранено: {ProjectContextHolder.Current.ProjectName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка сохранения",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowTechEditor()
        {
            if (!EnsureProjectLoaded())
                return;

            techEditorDoc = EnsureDockDoc(ref techEditorDoc,
                "Редактор технологических объектов");
            ProjectManager.GetInstance()
                .StartEditInHost(techEditorDoc.HostPanel);
            techEditorDoc.ShowOrActivate(dockPanel);
        }

        private void ShowDevices()
        {
            if (!EnsureProjectLoaded())
                return;

            devicesDoc = EnsureDockDoc(ref devicesDoc, "Устройства");
            DevicesViewControl.StartInHost(devicesDoc.HostPanel);
            devicesDoc.ShowOrActivate(dockPanel);
        }

        private void ShowPlc()
        {
            if (!EnsureProjectLoaded())
                return;

            plcDoc = EnsureDockDoc(ref plcDoc, "Структура ПЛК");
            IOViewControl.StartInHost(plcDoc.HostPanel);
            plcDoc.ShowOrActivate(dockPanel);
        }

        private void ShowBinding()
        {
            if (!EnsureProjectLoaded())
                return;

            bindingDoc = EnsureDockDoc(ref bindingDoc, "Привязка");
            BindingViewControl.StartInHost(bindingDoc.HostPanel);
            bindingDoc.ShowOrActivate(dockPanel);
        }

        private AppDockContent EnsureDockDoc(ref AppDockContent doc,
            string title)
        {
            if (doc == null || doc.IsDisposed)
                doc = new AppDockContent(title);
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
                "EasyEPlanner", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
    }
}
