using System;
using System.IO;
using EplanDevice;
using IO;
using System.Windows.Forms;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using Microsoft.SqlServer.Server;
using System.Diagnostics.CodeAnalysis;

namespace EasyEPlanner
{
    public static class ProjectDescriptionSaver
    {
        static ProjectDescriptionSaver()
        {
            LoadFilePatterns();
        }

        /// <summary>
        /// Загрузка шаблонов для сохраняемых файлов
        /// </summary>
        private static void LoadFilePatterns()
        {
            const string mainProgramPatternFileName = "mainPattern.plua";
            string pathToSystemFiles = ProjectManager.GetInstance()
                .SystemFilesPath;
            string pathToMainPluaFile = Path
                .Combine(pathToSystemFiles, mainProgramPatternFileName);

            try
            {
                var reader = new StreamReader(pathToMainPluaFile,
                    EncodingDetector.DetectFileEncoding(pathToMainPluaFile));
                mainProgramFilePattern = reader.ReadToEnd();
                reader.Close();
                mainProgramFilePatternIsLoaded = true;
            }
            catch
            {
                MessageBox.Show($"Не найден шаблон {mainProgramFileName}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        /// <summary>
        /// Сохранить описание проекта
        /// </summary>
        /// <param name="param">Параметры</param>
        public static void Save(object param)
        {
            var par = param as ParametersForSave;
            if (!par.silentMode)
            {
                Logs.Show();
                Logs.DisableButtons();
                Logs.SetProgress(0);
            }

            try
            {
                try
                {
                    if (!Directory.Exists(par.path))
                    {
                        Directory.CreateDirectory(par.path);
                    }
                }
                catch (DriveNotFoundException)
                {
                    if (!par.silentMode)
                    {
                        Logs.AddMessage("Ошибка подключения к диску с " +
                            "проектами. Подключите диск!");
                        Logs.SetProgress(100);
                    }
                    return;
                }

                SaveIOFile(par);

                if (par.silentMode == false)
                {
                    Logs.SetProgress(50);
                }

                SaveTechObjectsFile(par);
                SaveRestrictionsFile(par);
                SaveMainFile(par);
                SaveModbusFile(par);
                SaveProfibusFile(par);
                SavePrgFile(par);
                SaveSharedFile(par);

                if (par.silentMode == false)
                {
                    if (!Logs.IsEmpty())
                    {
                        Logs.AddMessage("Done.");
                        Logs.ShowLastLine();
                    }
                    else
                    {
                        Logs.Hide();
                    }
                }

                DeleteMainDevices(par);
            }
            catch (Exception ex)
            {
                if (par.silentMode == false)
                {
                    Logs.AddMessage("Exception - " + ex);
                    Logs.AddMessage("");
                    Logs.AddMessage("");
                    Logs.ShowLastLine();
                }
            }
            finally
            {
                if (!par.silentMode && Logs.IsNull() == false)
                {
                    Logs.EnableButtons();
                    Logs.SetProgress(100);
                }
            }
        }

        /// <summary>
        /// Сохранить описание контроллера, шины в main.io.lua
        /// </summary>
        /// <param name="par">Параметры</param>
        private static void SaveIOFile(ParametersForSave par)
        {
            string pathToFile = par.path + @"\" + mainIOFileName;

            string versionForPlc = string
                .Format(VersionPattern, mainIOFileVersion);
            string pacName = string.Format("PAC_name = \'{0}\'", par.PAC_Name);
            ushort crc = ProjectManager.CRC16(par.PAC_Name);
            string pacId = string.Format("PAC_id = \'{0}\'", crc);
            string ioDescription = IOManager.SaveAsLuaTable("");
            string devicesForIo = deviceManager.SaveAsLuaTableForMainIO("");

            var fileData = new StringBuilder();
            fileData.AppendLine(versionForPlc);
            fileData.AppendLine(AssemblyVersion.GetVersionAsLuaComment());

            fileData.Append(AddDashes());
            fileData.AppendLine(pacName);
            fileData.AppendLine(pacId);
            fileData.Append(AddDashes());

            fileData.Append(ioDescription);
            fileData.Append(devicesForIo);

            SaveData(pathToFile, fileData);
        }

        /// <summary>
        /// Сохранить только объекты и ограничения (без main.io.lua).
        /// </summary>
        public static void SaveTechObjectsAndRestrictions(string pacName,
            string path, bool silentMode = true)
        {
            var par = new ParametersForSave(pacName, path, silentMode);
            if (!Directory.Exists(par.path))
            {
                Directory.CreateDirectory(par.path);
            }

            SaveTechObjectsFile(par);
            SaveRestrictionsFile(par);
        }

        /// <summary>
        /// Сохранить только main.io.lua (устройства и узлы).
        /// </summary>
        public static void SaveMainIoFile(string pacName, string path,
            bool silentMode = true)
        {
            var par = new ParametersForSave(pacName, path, silentMode);
            if (!Directory.Exists(par.path))
            {
                Directory.CreateDirectory(par.path);
            }

            string pathToFile = par.path + @"\" + mainIOFileName;
            if (!File.Exists(pathToFile) &&
                deviceManager.Devices.Count == 0 &&
                IOManager.IONodes.Count == 0)
            {
                return;
            }

            SaveIOFile(par);
        }

        /// <summary>
        /// Сохранить объекты проекта в main.objects.lua.
        /// </summary>
        /// <param name="par">Параметры</param>
        private static void SaveTechObjectsFile(ParametersForSave par)
        {
            string pathToFile = par.path + @"\" + MainTechObjectsFileName;

            string versionForPlc = string.Format(VersionPattern,
                    mainTechObjectsFileVersion);
            string pacName = string
                .Format(PacNamePattern, par.PAC_Name);
            string description = techObjectManager.SaveAsLuaTable("");

            var fileData = new StringBuilder();
            fileData.AppendLine(versionForPlc);
            fileData.AppendLine(AssemblyVersion.GetVersionAsLuaComment());
            fileData.AppendLine(pacName);
            fileData.Append(AddDashes());
            fileData.Append(AddDashes());
            fileData.Append(description);

            SaveData(pathToFile, fileData);
        }

        /// <summary>
        /// Сохранить ограничения проекта в main.restrictions.lua
        /// </summary>
        /// <param name="par">Параметры</param>
        private static void SaveRestrictionsFile(ParametersForSave par)
        {
            string pathToFile = par.path + @"\" + MainRestrictionsFileName;

            string versionForPlc = string.Format(VersionPattern,
                    mainRestrictionsFileVersion);
            string fileHeader = "--Файл ограничений проекта";
            string resctrictions = techObjectManager
                .SaveRestrictionAsLua("");

            var fileData = new StringBuilder();
            fileData.AppendLine(versionForPlc);
            fileData.AppendLine(AssemblyVersion.GetVersionAsLuaComment());
            fileData.AppendLine(fileHeader);
            fileData.Append(AddDashes());
            fileData.Append(AddDashes());
            fileData.Append(resctrictions);

            SaveData(pathToFile, fileData);
        }

        /// <summary>
        /// Сохранить файл main.plua (базовая инициализация).
        /// </summary>
        /// <param name="par">Параметры</param>
        private static void SaveMainFile(ParametersForSave par)
        {
            string fileName = par.path + @"\" + mainProgramFileName;
            if (!File.Exists(fileName) && mainProgramFilePatternIsLoaded)
            {
                //Создаем пустое описание управляющей программы.
                var fileWriter = new StreamWriter(fileName,
                    false, EncodingDetector.MainFilesEncoding);
                string mainPluaFilePattern = mainProgramFilePattern;
                mainPluaFilePattern = mainPluaFilePattern
                    .Replace("ProjectName", par.PAC_Name);

                StringBuilder packagesLine = new StringBuilder();
                if (packages != null && packages.Count > 0)
                {
                    packagesLine.Append("package.path = package.path");
                    foreach (var package in packages)
                    {
                        packagesLine.Append($" .. '{package}'");
                    }  
                }
                mainPluaFilePattern = string.Format(mainPluaFilePattern, packagesLine);

                fileWriter.WriteLine(mainPluaFilePattern);

                fileWriter.Flush();
                fileWriter.Close();
            }
        }

        public static void AddPackage(string package)
        {
            if (package.Trim() == string.Empty)
                return;

            if (packages == null)
                packages = new List<string>();
            packages.Add(package.Trim());
        }

        /// <summary>
        /// Сохранить файл для modbus в main.modbus.lua
        /// </summary>
        /// <param name="par">Параметры</param>
        private static void SaveModbusFile(ParametersForSave par)
        {
            string fileName = par.path + @"\" + mainModbusSRVFileName;
            if (!File.Exists(fileName))
            {
                //Создаем базовое описание сервера MODBUS.
                string filePattern = Properties.Resources.ResourceManager
                    .GetString("modbusSRVFilePattern");
                string modBusContent = string.Format(filePattern,
                    mainModbusSrvFileVersion);
                File.WriteAllText(fileName, modBusContent,
                    EncodingDetector.MainFilesEncoding);
            }
        }

        /// <summary>
        /// Сохранить файл для profibus в main.profibus.lua
        /// </summary>
        /// <param name="par">Параметры</param>
        private static void SaveProfibusFile(ParametersForSave par)
        {
            string fileName = par.path + @"\" + mainProfibusFileName;
            if (!File.Exists(fileName))
            {
                //Создаем пустое описание конфигурации PROFIBUS.
                string content = "--version  = 1\n";
                content += AssemblyVersion.GetVersionAsLuaComment() + "\n";
                content += "--Описание конфигурации PROFIBUS\n";
                content += AddDashes();
                content += "system = system or { }\n";
                content += "system.init_profibus = function()\n";
                content += "end\n";

                File.WriteAllText(fileName, content,
                    EncodingDetector.MainFilesEncoding);
            }
        }

        /// <summary>
        /// Сохранить программное описание проекта в prg.lua
        /// </summary>
        /// <param name="par">Параметры</param>
        private static void SavePrgFile(ParametersForSave par)
        {
            string pathToFile = par.path + @"\" + mainPRGFileName;

            string versionForPlc = string
                .Format(VersionPattern, mainPRGFileVersion);
            string pacName = string
                .Format(PacNamePattern, par.PAC_Name);
            string prgFileData = PrgLuaSaver.Save("\t");

            var fileData = new StringBuilder();
            fileData.AppendLine(versionForPlc);
            fileData.AppendLine(AssemblyVersion.GetVersionAsLuaComment());
            fileData.AppendLine(pacName);
            fileData.Append(AddDashes());
            fileData.Append(AddDashes());
            fileData.AppendLine(prgFileData);

            SaveData(pathToFile, fileData);
        }

        [ExcludeFromCodeCoverage]
        private static void SaveSharedFile(ParametersForSave par)
        {
            string fileName = par.path + @"\" + sharedFileName;
            if (!File.Exists(fileName))
            {
                string sharedContent = Properties.Resources.ResourceManager
                    .GetString("sharedFilePattern");
                sharedContent = string.Concat(
                    $"--{AssemblyVersion.GetStringForFileWithVersion()}",
                    "\n", sharedContent);
                File.WriteAllText(fileName, sharedContent,
                    EncodingDetector.MainFilesEncoding);
            }
            else
            {
                // Проверка ip-адресов связанных проектов
                foreach(Match advPrj in 
                    Regex.Matches(File.ReadAllText(fileName),
                    @"(\['(?<name>[\w-]+)'\])\W+(?:ip\s+=\s'(?<ip>[\w\d.]*)',)",
                    RegexOptions.None, TimeSpan.FromMilliseconds(100)))
                {
                    var pathToIO = Path.Combine(
                        ProjectManager.GetInstance().GetPtusaProjectsPath(advPrj.Groups["name"].Value),
                        advPrj.Groups["name"].Value,
                        "main.io.lua");
                    if (File.Exists(pathToIO))
                    {
                        var advPrjIP = Regex.Match(File.ReadAllText(pathToIO),
                            @"IP\s+=\s'(?<ip>[\w.]+)'",
                            RegexOptions.None, TimeSpan.FromMilliseconds(100));

                        if (advPrjIP.Success && advPrjIP.Groups["ip"].Value != advPrj.Groups["ip"].Value)
                        {
                            Logs.AddMessage($"В связанном проекте ['{advPrj.Groups["name"]}']" +
                                $" ip контроллера ({advPrj.Groups["ip"]}) не совпадает с указанным в" +
                                $" файле shared.lua ({advPrjIP.Groups["ip"]});\n");
                        }
                    }
                    else
                    {
                        Logs.AddMessage($"Файл main.io.lua связанного проекта ['{advPrj.Groups["name"]}'] не найден;\n");
                    }
                }
            }
        }

        private static string AddDashes()
        {
            return new string('-', numberOfDashes) + "\n";
        }

        private static void SaveData(string pathToFile, StringBuilder fileData)
        {
            bool shouldSave = ShouldSaveFile(pathToFile, fileData);
            if (shouldSave)
            {
                var fileWriter = new StreamWriter(pathToFile,
                    false, EncodingDetector.MainFilesEncoding);

                fileWriter.Write(fileData);

                fileWriter.Flush();
                fileWriter.Close();
                fileWriter.Dispose();
            }
        }

        private static bool ShouldSaveFile(string pathToFile,
            StringBuilder currentFileData)
        {
            DriveInfo drive = new DriveInfo(new FileInfo(pathToFile).Directory.Root.FullName);
            long freeSpace = drive.AvailableFreeSpace;

            const string splitPattern = "\r\n|\n\r|\r|\n";
            const int eplannerVersionId = 1;
            if (!File.Exists(pathToFile)) return true;

            string previousfileData = File.ReadAllText(pathToFile);
            string[] previousVersion = Regex
                .Split(previousfileData.ToString(), splitPattern);

            bool cantCheckVersion = previousVersion.Length <= eplannerVersionId;
            if (cantCheckVersion) return true;
            previousVersion[eplannerVersionId] = string.Empty;

            string[] currentVersion = Regex
                .Split(currentFileData.ToString(), splitPattern);
            currentVersion[eplannerVersionId] = string.Empty;

            bool save = !currentVersion.SequenceEqual(previousVersion);

            long fileSpace = currentVersion.Length - previousVersion.Length;
            if (save && fileSpace > freeSpace * freeSpaceMultiplier)
            {
                Logs.AddMessage($"Недостаточно места на диске. Для файла \"{pathToFile.Split('\\').Last()}\" необходимо " +
                    $"{((fileSpace > byteConversionFactor) ? (fileSpace / byteConversionFactor).ToString() + " B" : fileSpace.ToString() + " kB")}" +
                    $" свободного места. Освободите место на диске и повторите попытку.");
                return false;
            }

            return save;
        }

        /// <summary>
        /// Проверка наличия файла main.devices.lua и его удаление,
        /// так как не используется в новой функциональности.
        /// </summary>
        /// <param name="par">Параметры</param>
        private static void DeleteMainDevices(ParametersForSave par)
        {
            var mainDevicesPath = par.path + @"\" + mainDevicesLua;
            File.Delete(mainDevicesPath);
        }

        private const string VersionPattern = "--version  = {0}";
        private const string PacNamePattern = "--PAC_name = \'{0}\'";

        private const int mainIOFileVersion = 1;
        private const int mainTechObjectsFileVersion = 1;
        private const int mainRestrictionsFileVersion = 1;
        private const int mainPRGFileVersion = 1;
        private const int mainModbusSrvFileVersion = 1;
        private const int freeSpaceMultiplier = 3;
        private const int byteConversionFactor = 1024;

        private const string mainIOFileName = "main.io.lua";
        public const string MainTechObjectsFileName = "main.objects.lua";
        public const string MainRestrictionsFileName = "main.restrictions.lua";
        private const string mainProgramFileName = "main.plua";
        private static string mainProgramFilePattern = "";
        private static bool mainProgramFilePatternIsLoaded = false;
        private const string mainModbusSRVFileName = "main.modbus_srv.lua";
        private const string mainProfibusFileName = "main.profibus.lua";
        private const string mainPRGFileName = "prg.lua";
        private const string sharedFileName = "shared.lua";
        private const string mainDevicesLua = "main.devices.lua";

        private const int numberOfDashes = 78;

        private static TechObject.ITechObjectManager techObjectManager =
              TechObject.TechObjectManager.GetInstance();
        private static DeviceManager deviceManager = DeviceManager
            .GetInstance();
        private static IOManager IOManager = IOManager.GetInstance();

        public static List<string> Packages
        {
            get
            {
                return packages;
            }
        }

        private static List<string> packages;
        
        public static string MainProgramFileName
        {
            get
            {
                return mainProgramFileName;
            }
        }

        public class ParametersForSave
        {
            public string PAC_Name;
            public string path;
            public bool silentMode;

            public ParametersForSave(string PAC_Name, string path,
                bool silentMode)
            {
                this.PAC_Name = PAC_Name;
                this.path = path;
                this.silentMode = silentMode;
            }
        }
    }
}