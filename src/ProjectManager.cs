using EplanDevice;
using IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using TechObject;
using System.Threading;
using System.Threading.Tasks;
using EasyEPlanner.PxcIolinkConfiguration;
using System;
using System.Text;
using EasyEPlanner.PxcIolinkConfiguration.Interfaces;
using StaticHelper;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using PInvoke;
using Editor;

namespace EasyEPlanner
{
    /// <summary>
    /// Менеджер проекта.
    /// </summary>
    public class ProjectManager
    {
        /// <summary>
        /// Вычисление CRC16 кода строки.
        /// </summary>
        public static ushort CRC16(string str)
        {
            byte[] pcBlock = System.Text.Encoding.Default.GetBytes(str);
            int len = pcBlock.GetLength(0);

            ushort crc = 0xFFFF;

            ushort idx = 0;
            while (len-- > 0)
            {
                crc ^= (ushort)(pcBlock[idx++] << 8);

                for (int i = 0; i < 8; i++)
                {
                    crc = (ushort)((crc & 0x8000) > 0 ? 
                        ((crc << 1) ^ 0x1021) : (crc << 1));
                }
            }

            return crc;
        }

        /// <summary>
        /// Сохранение описания в виде скрипта Lua.
        /// </summary>
        public void SaveAsLua(string PAC_Name, string path, bool silentMode)
        {
            var param = new ProjectDescriptionSaver.ParametersForSave(PAC_Name, 
                path, silentMode);

            if (silentMode)
            {
                ProjectDescriptionSaver.Save(param);
            }
            else
            {
                var t = new Thread(new ParameterizedThreadStart(
                    ProjectDescriptionSaver.Save));
                t.CurrentCulture = Thread.CurrentThread.CurrentCulture;
                t.Start(param);
            }
        }

        #region Загрузка описания проекта
        /// <summary>
        /// Считывание описания.
        /// </summary>
        public int LoadDescription(out string errStr,
            string projectName, bool loadFromLua)
        {
            Logs.Clear();
            eProjectManager.ProjectDataIsLoaded = false;

            var oProgress = new Eplan.EplApi.Base.Progress("EnhancedProgress");
            oProgress.SetAllowCancel(false);
            oProgress.SetTitle("Считывание данных проекта");

            int res = 0;
            errStr = string.Empty;
            string thrownExceptions = string.Empty;

            try
            {
                oProgress.BeginPart(15, "Считывание IO");
                if (loadFromLua)
                    IOManager.GetInstance().ResetStoredNtypes();
                projectConfiguration.ReadIO();
                if (loadFromLua)
                    ProjectConfiguration.LoadNtypesFromLua();
                oProgress.EndPart();

                oProgress.BeginPart(15, "Считывание устройств");
                if (projectConfiguration.DevicesIsRead == true)
                {
                    projectConfiguration.SynchronizeDevices();
                }
                else
                {
                    projectConfiguration.ReadDevices();
                }
                oProgress.EndPart();

                if (loadFromLua)
                {
                    oProgress.BeginPart(5, "Сверка устройств с main.io.lua");
                    DeviceLuaFsaSynchronizer.TryApplyFromProjectFolder(
                        deviceManager,
                        ProjectContextHolder.Current?.ProjectFolderPath,
                        () => projectConfiguration.SynchronizeDevices());
                    oProgress.EndPart();
                }

                oProgress.BeginPart(25, "Считывание привязки устройств");
                projectConfiguration.ReadBinding();
                oProgress.EndPart();

                if (loadFromLua)
                {
                    EncodingDetector.MainFilesEncoding = null;

                    var luaStr = string.Empty;
                    oProgress.BeginPart(15, "Считывание технологических " +
                        "объектов");
                    res += LoadDescriptionFromFile(out luaStr,
                        out string mainObjectsErrors, projectName, 
                        $"\\{ProjectDescriptionSaver.MainTechObjectsFileName}");
                    thrownExceptions += mainObjectsErrors;
                    techObjectManager.LoadDescription(luaStr, projectName);
                    techObjectManager.GenericTechObjects.ForEach(obj => obj.Update());

                    luaStr = string.Empty;
                    res += LoadDescriptionFromFile(out luaStr,
                        out string restrictionsErrors, projectName,
                        $"\\{ProjectDescriptionSaver.MainRestrictionsFileName}");
                    thrownExceptions += restrictionsErrors;
                    techObjectManager.LoadRestrictions(luaStr);
                    oProgress.EndPart();
                }
                else
                {
                    oProgress.BeginPart(15, "Проверка данных и тестирование");
                    projectConfiguration.Check();
                    oProgress.EndPart();
                }

                oProgress.BeginPart(15, "Расчет IO-Link");
                ioManager.CalculateIOLinkAdresses();
                oProgress.EndPart(true);
                eProjectManager.ProjectDataIsLoaded = true;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
                oProgress.EndPart(true);
                eProjectManager.ProjectDataIsLoaded = false;
            }

            errStr = thrownExceptions;
            return res;
        }

        /// <summary>
        /// Считывание описания.
        /// </summary>
        private int LoadDescriptionFromFile(out string LuaStr,
            out string errStr, string projectName, string fileName)
        {
            LuaStr = string.Empty;
            errStr = string.Empty;
            int res = 0;

            StreamReader sr = null;
            string path;
            if (ProjectContextHolder.Current != null &&
                !string.IsNullOrEmpty(
                    ProjectContextHolder.Current.ProjectFolderPath) &&
                string.Equals(ProjectContextHolder.Current.ProjectName,
                    projectName, System.StringComparison.OrdinalIgnoreCase))
            {
                path = ProjectContextHolder.Current.ProjectFolderPath +
                    fileName;
            }
            else
            {
                path = GetPtusaProjectsPath(projectName) + projectName +
                    fileName;
            }

            try
            {
                if (!File.Exists(path))
                {
                    errStr += "Файл описания проекта \"" + path +
                        "\" отсутствует! Создано пустое описание.\n";
                    res = 1;
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, string.Empty);
                }
            }
            catch (DriveNotFoundException)
            {
                errStr += "Укажите правильные настройки каталога!\n";
                res = 1;
                return res;
            }

            bool needEncoding = EncodingDetector.MainFilesEncoding == null &&
                fileName.Contains(ProjectDescriptionSaver
                .MainTechObjectsFileName);
            if (needEncoding)
            {
                EncodingDetector.MainFilesEncoding = EncodingDetector
                    .DetectFileEncoding(path);
            }

            sr = new StreamReader(path, EncodingDetector.MainFilesEncoding);
            LuaStr = sr.ReadToEnd();
            sr.Close();

            return res;
        }
        #endregion

        /// <summary>
        /// Путь к файлам .lua (к проекту)
        /// </summary>
        /// <returns></returns>
        /// <summary>
        /// Первый сконфигурированный каталог с проектами (folder_path)
        /// без поиска конкретного проекта и без вывода MessageBox.
        /// Используется, например, как стартовая папка диалога выбора
        /// проекта в standalone-приложении.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public string GetDefaultProjectsRootPath()
        {
            try
            {
                string path = Path.Combine(OriginalAssemblyPath,
                    StaticHelper.CommonConst.ConfigFileName);
                if (!File.Exists(path))
                    return "";

                PInvoke.IniFile iniFile = new PInvoke.IniFile(path);
                string firstPath = iniFile
                    .ReadString("path", "folder_path", "")
                    .Split(';')
                    .FirstOrDefault(p => !string.IsNullOrEmpty(p) &&
                        Directory.Exists(p));

                return firstPath ?? "";
            }
            catch
            {
                return "";
            }
        }

        [ExcludeFromCodeCoverage]
        public string GetPtusaProjectsPath(string projectName)
        {
            try
            {
                string path = Path.Combine(OriginalAssemblyPath, StaticHelper.CommonConst.ConfigFileName);

                PInvoke.IniFile iniFile = new PInvoke.IniFile(path);
                // Поиск файла .ini
                if (!File.Exists(path))
                {
                    // Если не нашли - создаем новый, 
                    // записываем дефолтные данные
                    StreamWriter sr = new StreamWriter(path, true);
                    sr.WriteLine("[path]\nfolder_path=");
                    sr.Close();
                    sr.Flush();
                }

                // Считывание и возврат пути каталога проектов
                string[] projectsFolderArray = iniFile
                    .ReadString("path", "folder_path", "")
                    .Split(';');

                foreach (string pathFromArray in projectsFolderArray)
                {
                    if (string.IsNullOrEmpty(pathFromArray))
                        continue;

                    if (Directory.Exists(Path.Combine(pathFromArray, projectName)))
                    {
                        return pathFromArray + '\\';
                    }
                }

                if (!string.IsNullOrEmpty(projectsFolderArray.FirstOrDefault()))
                    return projectsFolderArray.FirstOrDefault() + '\\';

                if (!isStandalone)
                {
                    MessageBox.Show("Путь к каталогу с проектами не найден.\n" +
                        "Пожалуйста, проверьте конфигурацию!", "Внимание",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch
            {
                if (!isStandalone)
                {
                    MessageBox.Show("Файл конфигурации не найден - будет " +
                        "создан новый со стандартным описанием. Пожалуйста, " +
                        "измените путь к каталогу с проектами, где хранятся " +
                        "Lua файлы!", "Внимание", MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            return "";
        }

        /// <summary>
        /// Путь к Eplan-макросам модулей ввода-вывода
        /// </summary>
        [ExcludeFromCodeCoverage]
        public string GetWagoMacrosPath(string key)
        {
            try
            {
                string path = Path.Combine(OriginalAssemblyPath, StaticHelper.CommonConst.ConfigFileName);

                PInvoke.IniFile iniFile = new PInvoke.IniFile(path);
                if (File.Exists(path))
                {
                    var cyrillic = Encoding.GetEncoding("Windows-1251");

                    return cyrillic.GetString(
                        Encoding.Convert(
                            Encoding.GetEncoding("UTF-8"),
                            cyrillic,
                            cyrillic.GetBytes(iniFile.ReadString("path", key, ""))));
                }
            }
            catch
            {
                MessageBox.Show("Файл конфигурации не найден", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return "";
        }

        /// <summary>
        /// Инициализация.
        /// </summary>
        public void Init()
        {
            CheckLibsAndFiles();

            editor = Editor.Editor.GetInstance();
            techObjectManager = TechObjectManager.GetInstance();
            Logs.Init(new LogFrm());
            ioManager = IO.IOManager.GetInstance();
            deviceManager = DeviceManager.GetInstance();
            projectConfiguration = ProjectConfiguration.GetInstance();
            eProjectManager = EProjectManager.GetInstance();
            ProjectContextHolder.Current = new EplanProjectContext();
            LoadBaseTechObjectsFromFiles();
        }

        /// <summary>
        /// Инициализация для standalone-приложения (без EPLAN).
        /// </summary>
        public void InitStandalone(IProjectContext context)
        {
            ProjectContextHolder.Current = context;
            standaloneOriginalAssemblyPath = context?.OriginalAssemblyPath
                ?? AssemblyPath;
            isStandalone = true;

            // Editor создаётся лениво в StartEditInHost — его конструктор
            // тяжёлый и не нужен до открытия проекта.
            techObjectManager = TechObjectManager.GetInstance();
            Logs.Init(new LogFrm());
            ioManager = IO.IOManager.GetInstance();
            deviceManager = DeviceManager.GetInstance();
            // ProjectConfiguration / EProjectManager не инициализируем —
            // они требуют рантайм EPLAN.
            LoadBaseTechObjectsFromFiles();
        }

        /// <summary>
        /// Режим standalone App (без рантайма EPLAN).
        /// </summary>
        public bool IsStandalone => isStandalone;

        /// <summary>
        /// Открыть редактор в указанном хосте (standalone Form).
        /// </summary>
        public void StartEditInHost(System.Windows.Forms.Control host)
        {
            editor = Editor.Editor.GetInstance();
            editor.OpenEditor(techObjectManager as Editor.ITreeViewItem, host);
        }

        /// <summary>
        /// Загрузить технологические объекты и ограничения из папки проекта.
        /// </summary>
        public int LoadTechObjectsFromContext(out string errStr)
        {
            errStr = string.Empty;
            var context = ProjectContextHolder.Current;
            if (context == null ||
                string.IsNullOrEmpty(context.ProjectFolderPath))
            {
                errStr = "Контекст проекта не задан.";
                return 1;
            }

            EncodingDetector.MainFilesEncoding = null;
            string projectName = context.ProjectName;
            int res = 0;
            string thrown = string.Empty;

            res += LoadDescriptionFromFile(out string objectsLua,
                out string objectsErrors, projectName,
                $"\\{ProjectDescriptionSaver.MainTechObjectsFileName}");
            thrown += objectsErrors;
            techObjectManager.LoadDescription(objectsLua, projectName);
            techObjectManager.GenericTechObjects.ForEach(obj => obj.Update());

            res += LoadDescriptionFromFile(out string restrictionsLua,
                out string restrictionsErrors, projectName,
                $"\\{ProjectDescriptionSaver.MainRestrictionsFileName}");
            thrown += restrictionsErrors;
            techObjectManager.LoadRestrictions(restrictionsLua);

            errStr = thrown;
            return res;
        }

        /// <summary>
        /// Сохранить объекты, ограничения и main.io.lua текущего контекста.
        /// </summary>
        public void SaveTechObjectsFromContext(bool silentMode = true)
        {
            var context = ProjectContextHolder.Current;
            if (context == null ||
                string.IsNullOrEmpty(context.ProjectFolderPath))
            {
                return;
            }

            bool showLog = !silentMode && !Logs.IsNull();
            if (showLog)
            {
                Logs.Show();
                Logs.DisableButtons();
                Logs.SetProgress(0);
            }

            bool hasError = false;
            try
            {
                ProjectDescriptionSaver.SaveTechObjectsAndRestrictions(
                    context.ProjectName, context.ProjectFolderPath, silentMode);
                ProjectDescriptionSaver.SaveMainIoFile(
                    context.ProjectName, context.ProjectFolderPath, silentMode);
            }
            catch (Exception ex)
            {
                hasError = true;
                if (showLog)
                {
                    Logs.AddMessage("Ошибка сохранения - " + ex);
                    Logs.ShowLastLine();
                }
                else
                {
                    MessageBox.Show("Ошибка сохранения:\n" + ex.Message,
                        "Сохранение", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (showLog)
                {
                    Logs.EnableButtons();
                    Logs.SetProgress(100);
                }
            }

            // Сообщение об успехе — только если в текущем сохранении
            // не было ошибок (иначе лог вводил бы в заблуждение).
            if (showLog && !hasError)
            {
                Logs.AddMessage($"Проект \"{context.ProjectName}\" " +
                    "сохранен.");
                Logs.ShowLastLine();
            }
        }

        /// <summary>
        /// Проверка доступности библиотек и файлов для надстройки.
        /// </summary>
        private void CheckLibsAndFiles()
        {
            Task.Run(() =>
            {
                MarkForDeleteGitAndSvnDirectoriesInShadowAssembly();
            });

            CheckExcelLibs();
            CopySystemFiles();
        }

        private void MarkForDeleteGitAndSvnDirectoriesInShadowAssembly()
        {
            string shadowAssemblyPath = GetShadowAssemblyPath();

            var pathsToControlVersionDirs = new List<string>();
            var checkingDirectories = new string[] { ".svn", ".git" };
            foreach(var dir in checkingDirectories)
            {
                pathsToControlVersionDirs.AddRange(Directory.GetDirectories(
                    shadowAssemblyPath, dir, SearchOption.AllDirectories));
            }

            foreach (var pathToCVDir in pathsToControlVersionDirs)
            {
                var directoryInfo = new DirectoryInfo(pathToCVDir);
                FileInfo[] files = directoryInfo
                    .GetFiles("*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    file.IsReadOnly = false;
                }
            }
        }

        private string GetShadowAssemblyPath()
        {
            int pathOffset = 3;
            List<string> pathParts = AssemblyPath
                .Split('\\')
                .ToList();
            pathParts.RemoveRange(pathParts.Count - pathOffset, pathOffset);
            var pathToShadowAssembly = string.Join("\\", pathParts);

            return pathToShadowAssembly;
        }

        /// <summary>
        /// Проверить Excel библиотеки надстройки.
        /// </summary>
        private void CheckExcelLibs()
        {
            const string spireLicense = "Spire.License.dll";
            const string spireXLS = "Spire.XLS.dll";
            const string spirePDF = "Spire.Pdf.dll";

            string SpireLicensePath = Path.Combine(AssemblyPath, spireLicense);
            string SpireXLSPath = Path.Combine(AssemblyPath, spireXLS);
            string SpirePDFPath = Path.Combine(AssemblyPath, spirePDF);

            if (File.Exists(SpireLicensePath) == false ||
                File.Exists(SpireXLSPath) == false ||
                File.Exists(SpirePDFPath) == false)
            {
                var files = new string[] { spireLicense, spireXLS, spirePDF };
                CopySpireXLSFiles(AssemblyPath, files, OriginalAssemblyPath);
            }
        }

        /// <summary>
        /// Копировать файлы библиотек Spire XLS
        /// </summary>
        /// <param name="shadowAssemblySpireFilesDir">Путь к библиотекам
        /// в теневом хранилище Eplan</param>
        /// <param name="files">Имена файлов для копирования</param>
        /// <param name="originalPath">Путь к надстройке из каталога
        /// подключения надстройки</param>
        private void CopySpireXLSFiles(string shadowAssemblySpireFilesDir,
            string[] files, string originalPath)
        {
            var libsDir = new DirectoryInfo(originalPath);
            foreach (FileInfo file in libsDir.GetFiles())
            {
                if (files.Contains(file.Name))
                {
                    string path = Path.Combine(shadowAssemblySpireFilesDir,
                        file.Name);
                    file.CopyTo(path, true);
                }
            }
        }

        /// <summary>
        /// Копирует системные .lua файлы если они не загрузились
        /// в теневое хранилище (Win 7 fix).
        /// <param name="systemFilesPath">Путь к Lua файлам
        /// в теневом хранилище Eplan</param>
        /// <param name="originalSystemFilesPath">Путь к файлам Lua в месте 
        /// подключения надстройки к программе</param>
        /// </summary>
        private void CopySystemFiles()
        {
            Directory.CreateDirectory(SystemFilesPath);

            var systemFilesDir = new DirectoryInfo(OriginalSystemFilesPath);
            FileInfo[] systemFiles = systemFilesDir.GetFiles();
            foreach (FileInfo systemFile in systemFiles)
            {
                string pathToFile = Path.Combine(SystemFilesPath,
                    systemFile.Name);
                systemFile.CopyTo(pathToFile, true);
            }
        }

        /// <summary>
        /// Загрузка базовых объектов в редактор из файлов описания
        /// </summary>
        private void LoadBaseTechObjectsFromFiles()
        {
            IBaseTechObjectManager baseTechObjectManager =
                BaseTechObjectManager.GetInstance();
            IBaseTechObjectsLoader baseTechObjectsLoader = 
                new BaseTechObjectLoader();

            baseTechObjectsLoader.LoadTo(baseTechObjectManager);
        }

        #region Генерация Excel
        /// <summary>
        /// Сохранение описания в виде таблицы Excel.
        /// </summary>
        public void SaveAsExcelDescription(string path)
        {
            var t = new System.Threading.Thread(
                new System.Threading.ParameterizedThreadStart(
                    ExportToExcel));

            t.Start(path);
        }

        /// <summary>
        /// Экспорт технологических объектов в Excel.
        /// </summary>
        /// <param name="path">Путь к директории сохранения</param>
        /// <param name="projectName">Имя проекта</param>
        private void ExportToExcel(object param)
        {
            var par = param as string;
            Logs.Show();

            Logs.DisableButtons();
            Logs.Clear();
            Logs.SetProgress(0);

            try
            {
                Logs.SetProgress(1);
                ExcelRepoter.ExportTechDevs(par);
                Logs.AddMessage("Done.");
            }

            catch (System.Exception ex)
            {
                Logs.AddMessage("Exception - " + ex);
            }
            finally
            {
                if (Logs.IsNull() == false)
                {
                    Logs.EnableButtons();
                    Logs.SetProgress(100);
                }
            }
        }
        #endregion

        /// <summary>
        /// Обновление подписей к клеммам модулей IO
        /// в соответствии с актуальным названием устройств.
        /// </summary>
        public void UpdateModulesBinding()
        {
            var errors = "";
            try
            {
                Logs.Clear();
                Logs.Show();
                Logs.AddMessage("Выполняется синхронизация..");
                errors = ModulesBindingUpdater.GetInstance().Execute();
                Logs.Clear();
            }
            catch (System.Exception ex)
            {
                Logs.AddMessage("Exception - " + ex);
            }
            finally
            {
                if (errors != "")
                {
                    Logs.AddMessage(errors);
                }

                if (Logs.IsNull() == false)
                {
                    Logs.AddMessage("Синхронизация завершена. ");
                    Logs.SetProgress(100);
                    Logs.EnableButtons();
                }
            }
        }

        /// <summary>
        /// Получение экземпляра класса.
        /// </summary>
        /// <returns>Единственный экземпляр класса.</returns>
        public static ProjectManager GetInstance()
        {
            if (null == instance)
            {
                instance = new ProjectManager();
            }

            return instance;
        }

        /// <summary>
        /// Редактирование технологических объектов. Новое дерево.
        /// </summary>
        /// <returns>Результат редактирования</returns>
        public void StartEdit()
        {
            editor.OpenEditor(techObjectManager as Editor.ITreeViewItem);
        }

        #region Подсветка объектов на схеме
        /// <summary>
        /// Участвующие в операции устройства, подсвеченные на карте Eplan.
        /// </summary>
        List<object> highlightedObjects = new List<object>();

        /// <summary>
        /// Отключить подсветку устройств
        /// </summary>
        /// <param name="isClosingProject">Флаг закрытия проекта</param>
        [ExcludeFromCodeCoverage]
        public void RemoveHighLighting(bool isClosingProject = false)
        {
            if (isStandalone)
            {
                highlightedObjects.Clear();
                return;
            }

            RemoveHighLightingEplan(isClosingProject);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RemoveHighLightingEplan(bool isClosingProject)
        {
            foreach (var drawedObject in highlightedObjects
                .OfType<Eplan.EplApi.DataModel.Graphics.GraphicalPlacement>())
            {
                if (isClosingProject)
                {
                    drawedObject.SmartLock();
                }
                drawedObject.Remove();
            }

            highlightedObjects.Clear();
        }


        public enum Color : short
        {
            RED = 1,
            GREEN = 3,
            GRAY = 252,
        }

        /// <summary>
        /// Подсветка из нового редактора
        /// </summary>
        /// <param name="objectsToDraw"></param>
        [ExcludeFromCodeCoverage]
        public void SetHighlighting(List<Editor.DrawInfo> objectsToDraw)
        {
            if (isStandalone || objectsToDraw == null)
            {
                return;
            }

            SetHighlightingEplan(objectsToDraw);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SetHighlightingEplan(List<Editor.DrawInfo> objectsToDraw)
        {
            foreach (Editor.DrawInfo drawObj in objectsToDraw.Where(o => o.DrawingStyle != Editor.DrawInfo.Style.NO_DRAW ))
            {
                Eplan.EplApi.DataModel.Function objectFunction =
                    ((drawObj.DrawingDevice as IODevice).Function as EplanFunction).Function;

                if (objectFunction == null)
                {
                    continue;
                }

                Eplan.EplApi.Base.PointD[] points = objectFunction
                    .GetBoundingBox();

                if (drawObj.DrawingDevice.DeviceType is DeviceType.DO or DeviceType.DI &&
                    objectFunction.Connections.Length > 0)
                {
                    points[1].X = objectFunction.Connections[0].StartPin
                        .ParentFunction.GetBoundingBox()[1].X;
                }

                switch (drawObj.DrawingStyle)
                {
                    case DrawInfo.Style.RED_BOX:
                        AddBoxForHighlighting(Color.RED, objectFunction, points);
                        break;

                    case DrawInfo.Style.GREEN_BOX:
                        AddBoxForHighlighting(Color.GREEN, objectFunction, points);
                        break;

                    case DrawInfo.Style.GRAY_BOX:
                        AddBoxForHighlighting(Color.GRAY, objectFunction, points);
                        break;

                    case DrawInfo.Style.GREEN_UPPER_BOX:
                        AddUpperBoxHighlighting(Color.GREEN, objectFunction, points);
                        break;

                    case DrawInfo.Style.GREEN_LOWER_BOX:
                        AddLowerBoxHighlighting(Color.GREEN, objectFunction, points);
                        break;

                    case DrawInfo.Style.GREEN_GRAY_BOX:
                        AddUpperBoxHighlighting(Color.GREEN, objectFunction, points);
                        AddLowerBoxHighlighting(Color.GRAY, objectFunction, points);
                        break;
                }

                switch (drawObj.Action)
                {
                    case DrawInfo.ActionType.DELAYED_ON_DEVICE:
                        AddPolygonForeground(Color.GRAY, objectFunction, points);
                        break;

                    case DrawInfo.ActionType.DELAYED_OFF_DEVICE:
                        AddPolygonForeground(Color.GREEN, objectFunction, points);
                        break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void AddUpperBoxHighlighting(Color colour,
            Eplan.EplApi.DataModel.Function objectFunction,
            Eplan.EplApi.Base.PointD[] points)
        {

            var copy = points.ToArray();
            copy[0].Y += (points[1].Y - points[0].Y) / 2;
            AddBoxForHighlighting(colour, objectFunction, copy);
        }

        /// <summary>
        /// 
        /// </summary>
        public void AddLowerBoxHighlighting(Color colour,
            Eplan.EplApi.DataModel.Function objectFunction,
            Eplan.EplApi.Base.PointD[] points)
        {
            var copy = points.ToArray();
            copy[1].Y -= (points[1].Y - points[0].Y) / 2;
            AddBoxForHighlighting(colour, objectFunction, copy);
        }

        /// <summary>
        /// Добавить прямоугольник в подсвечиваемые элементы
        /// </summary>
        /// <param name="colour">Цвет</param>
        /// <param name="objectFunction">Функция объекта</param>
        /// <param name="points">Точки</param>
        private void AddBoxForHighlighting(Color colour,
            Eplan.EplApi.DataModel.Function objectFunction,
            Eplan.EplApi.Base.PointD[] points)
        {
            var rc = new Eplan.EplApi.DataModel.Graphics.Rectangle();
            rc.Create(objectFunction.Page);
            rc.IsSurfaceFilled = true;
            rc.DrawingOrder = -1;
            rc.SetArea(points[0], points[1]);
            rc.Pen = new Eplan.EplApi.DataModel.Graphics.Pen((short)colour, -16002,
                -16002, -16002, 0);
            rc.Properties.set_PROPUSER_TEST(1, objectFunction
                .ToStringIdentifier());
            highlightedObjects.Add(rc);
        }

        /// <summary>
        /// Добавить треугольник по диагонали для отображения переходных переключений (переключение с задержкой)
        /// </summary>
        [ExcludeFromCodeCoverage]
        private void AddPolygonForeground(Color colour,
            Eplan.EplApi.DataModel.Function objectFunction,
            Eplan.EplApi.Base.PointD[] points)
        {
            var trngl = new Eplan.EplApi.DataModel.Graphics.PolyLine();
            trngl.Create(objectFunction.Page);
            trngl.DrawingOrder = 1;
            var p = new Eplan.EplApi.Base.PointD(points[0].X, points[1].Y);
            trngl.SetPointAt(0, ref points[0]);
            trngl.SetPointAt(1, ref points[1]);
            trngl.SetPointAt(2, ref p);
            trngl.SetPointAt(3, ref points[0]);

            trngl.Pen = new Eplan.EplApi.DataModel.Graphics.Pen((short)colour, -16002, -16002, -16002, 0);
            trngl.FillPattern = Eplan.EplApi.DataModel.Graphics.GraphicalPlacement.FillPatternType.Solid;

            highlightedObjects.Add(trngl);
        }

        #endregion

        #region OSTIS
        /// <summary>
        /// Получить ссылку на систему помощи Ostis
        /// </summary>
        /// <returns></returns>
        public string GetOstisHelpSystemLink()
        {
            var configFile = new PInvoke.IniFile(Path.Combine(
                OriginalAssemblyPath, 
                StaticHelper.CommonConst.ConfigFileName));
            string link = configFile.ReadString("helpSystem", "address", null);
            if (string.IsNullOrEmpty(link))
            {
                configFile.WriteString("helpSystem", "address", "");
            }
            return link;
        }

        /// <summary>
        /// Получить ссылку на основную страницы системы помощи Ostis
        /// </summary>
        /// <returns></returns>
        public string GetOstisHelpSystemMainPageLink()
        {
            var configFile = new PInvoke.IniFile(Path.Combine(
                OriginalAssemblyPath,
                StaticHelper.CommonConst.ConfigFileName));
            string link = configFile.ReadString("helpSystem", "mainAddress ", 
                null);
            if (string.IsNullOrEmpty(link))
            {
                configFile.WriteString("helpSystem", "mainAddress", "");
            }
            return link;
        }
        #endregion

        #region Генерация описания IOL-Conf
        public void GenerateIolConf()
        {
            var oProgress = new Eplan.EplApi.Base.Progress("EnhancedProgress");
            oProgress.SetAllowCancel(false);
            oProgress.SetTitle("Генерация описания IOL-Conf");

            oProgress.BeginPart(20, "Крутим планету и синхронизируем описание проекта");
            eProjectManager.SyncAndSave();
            oProgress.EndPart();

            oProgress.BeginPart(30, "Готовим окружение");
            IXmlSensorSerializer sensorSerializer = new XmlSensorSerializer();
            IXmlTemplateReader templateReader = new XmlTemplateReader(sensorSerializer);
            IModuleDescriptionBuilder moduleDescriptionBuilder = new ModuleDescriptionBuilder();
            IDevicesDescriptionBuilder devicesDescriptionBuilder = new DevicesDescriptionBuilder();
            ISensorDescriptionBuilder sensorDescriptionBuilder = new SensorDescriptionBuilder(
                moduleDescriptionBuilder, devicesDescriptionBuilder);
            IPxcIolinkConfiguration pxcConfiguration = new PxcIolinkModulesConfiguration(
                sensorSerializer, templateReader, sensorDescriptionBuilder);

            bool exceptionRaised = false;
            Logs.Clear();
            oProgress.EndPart();

            try
            {
                oProgress.BeginPart(40, "Запускаем фиксиков и проверяем каталоги");
                string projectName = eProjectManager.GetCurrentProjectName();
                string projectsFolderPath = GetPtusaProjectsPath(projectName);
                string projectFilesPath = Path.Combine(projectsFolderPath, projectName);
                bool foldersCreated = pxcConfiguration.CreateFolders(OriginalAssemblyPath, projectFilesPath);
                oProgress.EndPart();

                oProgress.BeginPart(50, "Прибиваем обои и читаем шаблоны");
                bool templatesLoaded = pxcConfiguration.ReadTemplates(foldersCreated);
                oProgress.EndPart();

                oProgress.BeginPart(70, "Красим потолок и генерируем описание модулей");
                pxcConfiguration.CreateModulesDescription(templatesLoaded, ioManager);
                oProgress.EndPart();
            }
            catch(AggregateException ae)
            {
                foreach(var e in ae.InnerExceptions)
                {
                    Logs.AddMessage(SeekExceptionEnd(e));
                }
                exceptionRaised = true;
            }
            catch (Exception e)
            {
                Logs.AddMessage(SeekExceptionEnd(e));
                exceptionRaised = true;
            }

            if (exceptionRaised)
            {
                Logs.EnableButtons();
                Logs.Show();
            }

            oProgress.EndPart(true);
        }

        private string SeekExceptionEnd(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine(ex.Message);
            if (ex.InnerException != null)
            {
                sb.AppendLine(SeekExceptionEnd(ex.InnerException));
            }

            return sb.ToString();
        }
        #endregion

        /// <summary>
        /// Путь к надстройке, к месту, из которого она подключалась к программе
        /// инженером (или к сборке standalone App).
        /// </summary>
        /// <remarks>
        /// AddInModule нельзя читать в этом же методе: JIT подтянет
        /// Eplan.EplApi.AFu даже если ветка не выполняется (standalone).
        /// </remarks>
        public string OriginalAssemblyPath
        {
            get
            {
                string fromContext = ProjectContextHolder.Current
                    ?.OriginalAssemblyPath;
                if (!string.IsNullOrEmpty(fromContext))
                {
                    return fromContext;
                }

                if (!string.IsNullOrEmpty(standaloneOriginalAssemblyPath))
                {
                    return standaloneOriginalAssemblyPath;
                }

                return GetEplanOriginalAssemblyPath();
            }
        }

        /// <summary>
        /// Путь из EPLAN AddInModule (только внутри EPLAN).
        /// NoInlining — чтобы тип AddInModule не грузился при JIT
        /// геттера OriginalAssemblyPath в standalone.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetEplanOriginalAssemblyPath()
        {
            return Path.GetDirectoryName(AddInModule.OriginalAssemblyPath);
        }

        /// <summary>
        /// Название папки с системными скриптами
        /// </summary>
        private const string luaFolder = "Lua";

        /// <summary>
        /// Папка с скриптами командой строки
        /// </summary>
        private const string cmdScriptsFolder = "CMD";

        /// <summary>
        /// Путь к надстройке в теневом хранилище Eplan
        /// </summary>
        public string AssemblyPath 
        {
            get
            {
                return Path.GetDirectoryName(Assembly
                    .GetExecutingAssembly().Location);
            }
        }

        /// <summary>
        /// Путь к системным файлам Lua в теневом хранилище Eplan
        /// (или из контекста standalone App).
        /// </summary>
        public string SystemFilesPath 
        {
            get
            {
                if (!string.IsNullOrEmpty(
                    ProjectContextHolder.Current?.SystemFilesPath))
                {
                    return ProjectContextHolder.Current.SystemFilesPath;
                }

                return Path.Combine(AssemblyPath, luaFolder);
            }
        }

        /// <summary>
        /// Путь к системным файлам Lua по месту подключения надстройки
        /// </summary>
        public string OriginalSystemFilesPath 
        {
            get
            {
                // Standalone: один каталог Lua из FileProjectContext.
                // В EPLAN Current.SystemFilesPath указывает на ShadowCopy —
                // его нельзя использовать как «исходный» путь (описания
                // базовых объектов лежат только рядом с надстройкой).
                if (isStandalone && !string.IsNullOrEmpty(
                    ProjectContextHolder.Current?.SystemFilesPath))
                {
                    return ProjectContextHolder.Current.SystemFilesPath;
                }

                if (!string.IsNullOrEmpty(standaloneOriginalAssemblyPath))
                {
                    return Path.Combine(standaloneOriginalAssemblyPath,
                        luaFolder);
                }

                return Path.Combine(OriginalAssemblyPath, luaFolder);
            }
        }

        /// <summary>
        /// Путь к файлам с скриптами командной строки для проверки проекта по
        /// месту подключения надстройки
        /// </summary>
        public string OriginalCMDFilesPath
        {
            get
            {
                return Path.Combine(OriginalAssemblyPath, cmdScriptsFolder);
            }
        }

        /// <summary>
        /// Путь сборки, заданный InitStandalone (без обращения к AddInModule).
        /// </summary>
        private string standaloneOriginalAssemblyPath;

        private bool isStandalone;

        private ProjectManager() { }

        /// <summary>
        /// Редактор технологических объектов.
        /// </summary>
        private Editor.IEditor editor;

        /// <summary>
        /// Менеджер технологических объектов.
        /// </summary>
        private ITechObjectManager techObjectManager;

        /// <summary>
        /// Менеджер модулей ввода/вывода.
        /// </summary>
        private IIOManager ioManager;

        /// <summary>
        /// Конфигурация проекта.
        /// </summary>
        private ProjectConfiguration projectConfiguration;

        /// <summary>
        /// Экземпляр класса ProjectManager
        /// </summary>
        private static ProjectManager instance;

        /// <summary>
        /// Менеджер проектов в Eplan (встроенный).
        /// </summary>
        private EProjectManager eProjectManager;

        /// <summary>
        /// Менеджер устройств.
        /// </summary>
        private IDeviceManager deviceManager;
    }
}
