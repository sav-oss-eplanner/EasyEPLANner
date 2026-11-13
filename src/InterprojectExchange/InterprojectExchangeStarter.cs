using LuaInterface;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EasyEPlanner;
using System.Linq;
using System.Windows.Forms;
using System;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace InterprojectExchange
{
    /// <summary>
    /// Межконтроллерный обмен сигналами. Стартовый класс
    /// </summary>
    public class InterprojectExchangeStarter
    {
        public InterprojectExchangeStarter()
        {
            interprojectExchange = InterprojectExchange.GetInstance();
            interprojectExchange.Clear();
        }

        /// <summary>
        /// Инициализация экземпляра Lua
        /// </summary>
        private void InitLuaInstance()
        {
            lua = new Lua();
            lua.RegisterFunction("CreateModel", this,
                GetType().GetMethod("CreateModel"));
            lua.RegisterFunction("CreateMainModel", this,
                GetType().GetMethod("CreateMainModel"));
            lua.RegisterFunction("GetModel", this,
                GetType().GetMethod("GetModel"));
            lua.RegisterFunction("GetMainProjectName", this,
                GetType().GetMethod("GetMainProjectName"));
            lua.RegisterFunction("GetSelectedModel", this,
                GetType().GetMethod("GetSelectedModel"));
            lua.RegisterFunction("WarningProjectNameInIOFile", this,
                GetType().GetMethod("WarningProjectNameInIOFile"));
        }

        /// <summary>
        /// Начало настройки межконтроллерного обмена.
        /// </summary>
        public void Start()
        {
            interprojectExchange.Owner = this;
            form = new InterprojectExchangeForm();
            bool isReadSignals = UpdateDevices();
            bool isLoadData = LoadCurrentInterprojectExchange(isReadSignals);
            ShowForm(isLoadData);
        }

        /// <summary>
        /// Обновление текущих устройств.
        /// </summary>
        private bool UpdateDevices()
        {
            if (!ProjectManager.GetInstance().IsStandalone)
            {
                EProjectManager.GetInstance().SyncAndSave();
            }

            return true;
        }

        /// <summary>
        /// Загрузить текущие данные по межпроектному обмену сигналами.
        /// </summary>
        /// <param name="isReadDevices">Прочитаны ли устройства текущие</param>
        private bool LoadCurrentInterprojectExchange(bool isReadDevices)
        {
            if (isReadDevices == false)
            {
                return false;
            }

            _projectsNotOpenedOnLoad.Clear();
            string projName = interprojectExchange.MainProjectName;
            if (!TryResolveProjectFolder(projName, out string mainProjectFolder))
            {
                _projectsNotOpenedOnLoad.Add(
                    $"{projName}: не найден каталог проекта");
                return false;
            }

            LoadProjectsData(mainProjectFolder, projName);
            return true;
        }

        /// <summary>
        /// Создать модель, вызывается из Lua
        /// </summary>
        /// <returns></returns>
        public IProjectModel CreateModel()
        {
            IProjectModel model = new AdvancedProjectModel();
            interprojectExchange.AddModel(model);
            return model;
        }

        /// <summary>
        /// Создать главную модель, вызывается из Lua
        /// </summary>
        /// <returns></returns>
        public IProjectModel CreateMainModel()
        {
            IProjectModel model = new CurrentProjectModel();
            interprojectExchange.AddModel(model);
            return model;
        }

        /// <summary>
        /// Получить модель, вызывается из Lua
        /// </summary>
        /// <param name="projectName">Имя проекта</param>
        /// <returns></returns>
        public IProjectModel GetModel(string projectName)
        {
            var model = interprojectExchange.GetModel(projectName);
            return model;
        }

        /// <summary>
        /// Получить выбранную модель
        /// </summary>
        /// <returns></returns>
        public IProjectModel GetSelectedModel()
        {
            var model = interprojectExchange.SelectedModel;
            return model;
        }

        /// <summary>
        ///  Получить имя главного (открытого в Eplan) проекта, вызывается
        ///  из LUA
        /// </summary>
        /// <returns></returns>
        public string GetMainProjectName()
        {
            return interprojectExchange.MainProjectName;
        }

        /// <summary>
        /// Загрузить данные проекта при загрузке системы
        /// </summary>
        /// <param name="pathToProjectDir">Путь к папке с проектом</param>
        /// <param name="projName">Имя проекта</param>
        /// <param name="errors">Ошибки во время загрузки</param>
        /// <returns>Загружены данные или нет</returns>
        public bool LoadProjectData(string pathToProjectFolder,
            string projName, out string errors)
        {
            InitLuaInstance();
            LoadScripts();
            LoadMainIOData(pathToProjectFolder, projName);
            LoadAdvancedProjectSharedLuaData(pathToProjectFolder, projName);
            errors = SetIPFromMainModel(projName);
            if (string.IsNullOrEmpty(errors))
            {
                var model = interprojectExchange.GetModel(projName);
                model.PathToProject = pathToProjectFolder;
                model.Loaded = true;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Загрузить данные проекта
        /// </summary>
        /// <param name="pathToMainProject">Путь к папке с главным проектом
        /// </param>
        /// <param name="projName">Имя проекта</param>
        /// <returns></returns>
        public void LoadProjectsData(string pathToMainProjectFolder,
            string projName = "")
        {
            InitLuaInstance();
            LoadScripts();
            if (!LoadMainIOData(pathToMainProjectFolder, projName))
            {
                AddProjectNotOpened(projName, "не удалось загрузить main.io.lua");
            }

            GenerateSharedDevices(projName);
            LoadCurrentProjectSharedLuaData(pathToMainProjectFolder, projName);
            var mainModel = interprojectExchange.MainModel;
            if (mainModel != null)
            {
                mainModel.PathToProject = pathToMainProjectFolder;
            }

            foreach (var model in interprojectExchange.Models)
            {
                if (model == interprojectExchange.MainModel)
                {
                    continue;
                }

                string alternativeProject = model.ProjectName;
                if (string.IsNullOrEmpty(alternativeProject) ||
                    alternativeProject == projName)
                {
                    continue;
                }

                if (!TryResolveProjectFolder(alternativeProject,
                    out string projectFolder))
                {
                    AddProjectNotOpened(alternativeProject,
                        "не найден каталог проекта");
                    continue;
                }

                InitLuaInstance();
                LoadScripts();
                model.Selected = true;
                bool loaded = LoadMainIOData(projectFolder, alternativeProject);
                model.Loaded = loaded;
                if (!loaded)
                {
                    AddProjectNotOpened(alternativeProject,
                        "не удалось загрузить main.io.lua");
                }

                GenerateSharedDevices(alternativeProject);
                LoadAdvancedProjectSharedLuaData(projectFolder, alternativeProject);
                model.Selected = false;
                SetIPFromMainModel(alternativeProject);
                model.PathToProject = projectFolder;
            }
        }

        private void AddProjectNotOpened(string projectName, string reason)
        {
            _projectsNotOpenedOnLoad.Add($"{projectName}: {reason}");
        }

        private void ShowProjectsNotOpenedSummary()
        {
            string summary = BuildProjectsNotOpenedSummaryText();
            if (summary == null || form == null)
            {
                return;
            }

            form.ShowWarningMessage(summary, MessageBoxButtons.OK);
        }

        private string BuildProjectsNotOpenedSummaryText()
        {
            if (_projectsNotOpenedOnLoad.Count == 0)
            {
                return null;
            }

            var summary = new StringBuilder();
            summary.AppendLine("Не удалось открыть следующие проекты:");
            foreach (string line in _projectsNotOpenedOnLoad)
            {
                summary.AppendLine($"• {line}");
            }

            return summary.ToString().TrimEnd();
        }

        private static bool TryResolveProjectFolder(string projectName,
            out string projectFolder)
        {
            // Текущий открытый проект уже знает свой путь через контекст
            // (в standalone-приложении folder_path в configuration.ini
            // не задан и незачем показывать ошибку конфигурации).
            var currentContext = ProjectContextHolder.Current;
            if (currentContext != null &&
                string.Equals(currentContext.ProjectName, projectName,
                    System.StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(currentContext.ProjectFolderPath) &&
                File.Exists(Path.Combine(currentContext.ProjectFolderPath,
                    devicesAndPLCFile)))
            {
                projectFolder = currentContext.ProjectFolderPath;
                InterprojectProjectCatalog.Register(projectFolder, projectName);
                return true;
            }

            if (InterprojectProjectCatalog.TryGetProjectFolder(projectName,
                out projectFolder))
            {
                return true;
            }

            string projectsBasePath = ProjectManager.GetInstance()
                .GetPtusaProjectsPath(projectName);
            string legacyFolder = Path.Combine(projectsBasePath, projectName);
            if (File.Exists(Path.Combine(legacyFolder, devicesAndPLCFile)))
            {
                projectFolder = legacyFolder;
                InterprojectProjectCatalog.Register(legacyFolder, projectName);
                return true;
            }

            projectFolder = null;
            return false;
        }

        /// <summary>
        /// Загрузить основные скрипты, нужные для чтения LUA-файлов.
        /// </summary>
        private void LoadScripts()
        {
            string systemFilesPath = ProjectManager.GetInstance()
                .SystemFilesPath;
            // Загрузка скрипта для чтения shared.lua
            string pathToSharedLua = Path.Combine(systemFilesPath, 
                sharedLuaIntializerFile);
            LoadScript(pathToSharedLua);

            // Загрузка скрипта для чтения main.io.lua
            string pathToInterprojectIOLua = Path.Combine(systemFilesPath, 
                devicesAndPLCInitializer);
            LoadScript(pathToInterprojectIOLua);
        }

        /// <summary>
        /// Загрузить LUA-скрипт из файла
        /// </summary>
        /// <param name="path">Путь к файлу скрипта</param>
        private void LoadScript(string path)
        {
            var reader = new StreamReader(path,
                EncodingDetector.DetectFileEncoding(path));
            string script = reader.ReadToEnd();
            reader.Close();
            lua.DoString(script);
        }

        /// <summary>
        /// Чтение информации о ПЛК из main.io.lua
        /// </summary>
        /// <param name="pathToProjectFolder">Каталог с main.io.lua</param>
        /// <param name="projName">Имя проекта (PAC_name)</param>
        private bool LoadMainIOData(string pathToProjectFolder,
            string projName)
        {
            string pathToIOFile = Path.Combine(pathToProjectFolder,
                devicesAndPLCFile);
            if (File.Exists(pathToIOFile))
            {
                var reader = new StreamReader(pathToIOFile,
                    EncodingDetector.DetectFileEncoding(pathToIOFile));
                string ioInfo = reader.ReadToEnd();
                reader.Close();
                lua.DoString(ioInfo);
                string folderName = Path.GetFileName(
                    pathToProjectFolder.TrimEnd('\\', '/'))
                    .Replace("'", "\\'");
                lua.DoString($"init_io_file('{folderName}')");
                return true;
            }
            else
            {
                form?.ShowErrorMessage($"Не найден файл main.io.lua проекта" +
                    $" \"{projName}\"");
                return false;
            }
        }

        public void WarningProjectNameInIOFile(string folderName)
        {
            form.ShowWarningMessage(
                $"PAC_name в main.io.lua не совпадает с именем каталога \"{folderName}\". " +
                "Используется PAC_name из файла.",
                MessageBoxButtons.OK);
        }

        /// <summary>
        /// Инициализирует переменные с названиями стройств,
        /// использующиеся в shared.lua 
        /// </summary>
        /// <param name="projectName">Имя проекта</param>
        private void GenerateSharedDevices(string projectName)
        {
            var devices = interprojectExchange.
                GetModel(projectName).Devices;
            if (devices.Count == 0)
            {
                return;
            }

            var devicesLua = new StringBuilder();
            foreach (var device in devices)
            {
                string name = ToLuaStringLiteral(device.Name);
                devicesLua.AppendLine($"_G[\"{name}\"] = \"{name}\"");
                devicesLua.AppendLine($"_G[\"__{name}\"] = \"{name}\"");
            }

            lua.DoString(devicesLua.ToString());
        }

        private static string ToLuaStringLiteral(string value) =>
            value.Replace("\\", "\\\\").Replace("\"", "\\\"");

        /// <summary>
        /// Чтение Shared файла текущего проекта
        /// </summary>
        /// <param name="pathToProjectFolder">Каталог с файлами проекта</param>
        /// <param name="projName">Имя проекта</param>
        private void LoadCurrentProjectSharedLuaData(string pathToProjectFolder,
            string projName)
        {
            string pathToSharedFile = Path.Combine(pathToProjectFolder,
                signalsFile);
            if (File.Exists(pathToSharedFile))
            {
                var reader = new StreamReader(pathToSharedFile,
                    EncodingDetector.DetectFileEncoding(pathToSharedFile));
                string sharedInfo = reader.ReadToEnd();
                reader.Close();
                lua.DoString(sharedInfo);
                // Функция из Lua
                lua.DoString("init_current_project_shared_lua()");
                ReadModelSharedFileToList(projName, pathToSharedFile);
            }
        }

        /// <summary>
        /// Чтение Shared файла альтернативного проекта
        /// </summary>
        /// <param name="pathToProjectFolder">Каталог с файлами проекта</param>
        /// <param name="projName">Имя проекта</param>
        public void LoadAdvancedProjectSharedLuaData(string pathToProjectFolder,
            string projName)
        {
            string pathToSharedFile = Path.Combine(pathToProjectFolder,
                signalsFile);
            if (File.Exists(pathToSharedFile))
            {
                var reader = new StreamReader(pathToSharedFile,
                    EncodingDetector.DetectFileEncoding(pathToSharedFile));
                string sharedInfo = reader.ReadToEnd();
                reader.Close();
                lua.DoString(sharedInfo);
                // Функция из Lua
                lua.DoString("init_advanced_project_shared_lua()");
                ReadModelSharedFileToList(projName, pathToSharedFile);
            }
        }

        /// <summary>
        /// Прочитать shared.lua в список для манипуляций
        /// </summary>
        /// <param name="projName">Имя проекта для поиска модели</param>
        /// <param name="pathToSharedFile">Путь к файлу shared.lua</param>
        private void ReadModelSharedFileToList(string projName, 
            string pathToSharedFile)
        {
            IProjectModel model = interprojectExchange.Models
                .Where(x => x.ProjectName == projName)
                .FirstOrDefault();
            if (model != null)
            {
                model.SharedFileAsStringList = File
                    .ReadAllLines(pathToSharedFile,
                    EncodingDetector.DetectFileEncoding(pathToSharedFile))
                    .ToList();
            }
        }

        /// <summary>
        /// Установка IP-адреса для альтернативных моделей из главной модели
        /// </summary>
        /// <returns>Ошибки</returns>
        private string SetIPFromMainModel(string projName)
        {
            var mainModel = interprojectExchange.MainModel;
            string alreadySelectedProject = mainModel.SelectedAdvancedProject;
            mainModel.SelectedAdvancedProject = interprojectExchange
                .MainProjectName;
            IProjectModel model = interprojectExchange.GetModel(projName);
            string errors = string.Empty;
            bool modelNotFound = model == null;
            if (modelNotFound)
            {
                errors += "Модель загружаемого проекта не была создана или" +
                    " создана неправильно. Проверьте файлы проекта. Откройте " +
                    "и закройте связываемый проект.\n";
            }
            else
            {
                model.PacInfo.IP = mainModel.PacInfo.IP;
                mainModel.SelectedAdvancedProject = alreadySelectedProject;
            }

            return errors;
        }

        /// <summary>
        /// Проверка корректности пути к проекту
        /// </summary>
        /// <param name="pathToDir">Путь к папке с файлами проекта</param>
        /// <returns></returns>
        public bool CheckProjectData(string pathToDir)
        {
            bool res = false;
            string fileWithDevicesPath = Path.Combine(pathToDir,
                devicesAndPLCFile);
            if (File.Exists(fileWithDevicesPath))
            {
                res = true;
            }
            return res;
        }

        /// <summary>
        /// Показать форму для работы с межпроектным обменом.
        /// </summary>
        [ExcludeFromCodeCoverage]
        private void ShowForm(bool isLoaded)
        {
            if (form == null || form.IsDisposed)
            {
                form = new InterprojectExchangeForm();
            }

            if (isLoaded == false)
            {
                ShowProjectsNotOpenedSummary();
                return;
            }

            ShowProjectsNotOpenedSummary();
            form.ShowDialog();
        }

        /// <summary>
        /// Сохранение межконтроллерного обмена
        /// </summary>
        public void Save()
        {
            interprojectExchange.Models
                .RemoveAll(model => string.IsNullOrEmpty(model.ProjectName));
            if (interprojectExchange.Models.Count < 1)
            {
                throw new ApplicationException();
            }
            interprojectExchangeSaver = new InterprojectExchangeSaver(
                interprojectExchange, signalsFile);
            _ = interprojectExchangeSaver.SaveAsync();
        }

        const string devicesAndPLCFile = "main.io.lua";
        const string signalsFile = "shared.lua";
        const string sharedLuaIntializerFile = "sys_shared_initializer.lua";
        const string devicesAndPLCInitializer = "sys_interproject_io.lua";

        private InterprojectExchangeForm form;
        private readonly IInterprojectExchange interprojectExchange;
        private InterprojectExchangeSaver interprojectExchangeSaver;
        private readonly List<string> _projectsNotOpenedOnLoad = new List<string>();

        Lua lua;
    }
}
