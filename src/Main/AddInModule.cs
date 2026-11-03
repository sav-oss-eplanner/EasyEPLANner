using EasyEPlanner.Main;
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Starter;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows.Forms;
[assembly: EplanSignedAssemblyAttribute(true)]

namespace EasyEPlanner
{
    /// <summary>
    /// Точка входа в приложение, стартовый класс.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AddInModule : IEplAddIn, IEplAddInShadowCopy
    {
        /// <summary>
        /// This function is called by the framework of EPLAN, when the 
        /// framework already has initialized its graphical user interface 
        /// (GUI) and the add-in can start to modify the GUI.
        /// The function only is called, if the add-in is loaded on 
        /// system-startup.
        /// </summary>
        /// <returns>true, if function succeeds</returns>
        public bool OnInitGui()
        {
            Eplan.EplApi.Gui.Menu oMenu = new Eplan.EplApi.Gui.Menu();

            uint menuID = oMenu.AddMainMenu(
                "EPlaner", Eplan.EplApi.Gui.Menu.MainMenuName.eMainMenuHelp,
                "Экспорт XML для EasyServer", "SaveAsXMLAction",
                "Экспорт XML для EasyServer", 0);

            menuID = oMenu.AddMenuItem(
                 "Экспорт компонентов *.mpkx",
                 nameof(SaveAsMpkx),
                 "Экспорт компонентов *.mpkx", menuID, 1,
                 false, false);

            menuID = oMenu.AddMenuItem(
                "Экспорт технологических устройств в Excel",
                "ExportTechDevsToExcel",
                "Экспорт технологических устройств в Excel", menuID, 1, 
                false, true);

            menuID = oMenu.AddMenuItem("Редактировать технологические объекты",
                "ShowTechObjectsAction",
                "Редактирование технологических объектов", menuID, 1, 
                false, false);

            menuID = oMenu.AddMenuItem("Структура ПЛК",
               nameof(ShowPlcAction), "Структура ПЛК", menuID,
               1, false, false);

            menuID = oMenu.AddMenuItem("Устройства",
                nameof(ShowDevicesNewAction), "Устройства", menuID,
                int.MaxValue, false, false);

            menuID = oMenu.AddMenuItem("Привязка",
                nameof(ShowBindingAction), "Привязка", menuID,
                1, false, true);

            menuID = oMenu.AddMenuItem("Обмен сигналами между проектами",
                "InterprojectExchangeAction",
                "Настройка межпроектного обмена сигналами", menuID, 1, false, 
                false);

            menuID = oMenu.AddMenuItem("Обмен сигналами Modbus",
                nameof(ModbusExchangeAction),
                "Обмен сигналами Modbus", menuID, 1, false,
                false);

            menuID = oMenu.AddMenuItem(
                "Синхронизация названий устройств и модулей", 
                "BindingSynchronization",
                "Синхронизация названий устройств и модулей", menuID, 1, 
                false, false);

            menuID = oMenu.AddMenuItem(
                "Генерация описания IOL-Conf",
                "PxcIolModulesConfiguration",
                "Генерация описания IOL-Conf", menuID, 1,
                false, false);

            menuID = oMenu.AddMenuItem(
                "Импорт ICP CON проекта",
                nameof(ImportIcpWagoProject),
                "Импорт ICP CON проекта", menuID, 1,
                true, false);

            menuID = oMenu.AddMenuItem(
                "Создать/изменить файл переименования устройств",
                nameof(ImportIcpCreateRenamingMap),
                "Создать/изменить файл переименования устройств", menuID, 1,
                false, false);

            menuID = oMenu.AddMenuItem(
                "Документ соответствия названий устройств CSV",
                nameof(ImportIcpCreateDeviceNameMathchingFile),
                "Документ соответствия названий устройств CSV", menuID, 1,
                false, false);

            menuID = oMenu.AddMenuItem(
                "Обновления", nameof(OpenUpdater),
                "", menuID, 1, true, false);

            menuID = oMenu.AddMenuItem("О дополнении", "AboutProgramm", "", 
                menuID, 1, false, false);

            ProjectManager.GetInstance().Init();

            return true;
        }

        public bool OnRegister(ref bool bLoadOnStart)
        {
            bLoadOnStart = true;
            return true;
        }

        public bool OnUnregister()
        {
            return true;
        }

        public bool OnInit()
        {
            var commandLines = Environment.GetCommandLineArgs();
            if (commandLines.Contains(RunEplanWithoutUpdaterArg) is false)
            {
                var path = new FileInfo(AddInModule.OriginalAssemblyPath).Directory.FullName;
                if (File.Exists(path + LauncherPath))
                {
                    var proc = Process.Start(path + LauncherPath, $"{RunUpdaterAtInit} {Process.GetCurrentProcess().Id}");
                    proc.WaitForExit();
                }
            }

            return true;
        }

        public bool OnExit()
        {
            return true;
        }

        public void OnBeforeInit(string strOriginalAssemblyPath)
        {
            OriginalAssemblyPath = strOriginalAssemblyPath;
        }

        /// <summary>
        /// Путь к дополнению (откуда подключена).
        /// </summary>
        public static string OriginalAssemblyPath;

        /// <summary>
        /// Путь к исполняемому файлу средства обновления
        /// </summary>
        public static readonly string LauncherPath = @"\Updater\EasyEPLANnerUpdater.exe";

        /// <summary>
        /// Аргумент для запуска EPLAN из средства обновления
        /// </summary>
        public static readonly string RunEplanWithoutUpdaterArg = "/WithoutUpdater";

        /// <summary>
        /// Аргумент запуска средства обновления при запуске EPLAN
        /// </summary>
        public static readonly string RunUpdaterAtInit = "es";

        /// <summary>
        /// Аргумент запуска средства обновления из меню Eplanner
        /// </summary>
        public static readonly string RunUpdaterFromMenu = "em";
    }

    /// <summary>    
    /// Вспомогательные функции.
    /// </summary>
    public class WindowWrapper : IWin32Window
    {
        public WindowWrapper(IntPtr handle)
        {
            _hwnd = handle;
        }

        public IntPtr Handle
        {
            get
            {
                return _hwnd;
            }
        }

        private IntPtr _hwnd;
    }
}
