using EasyEPlanner;
using EplanDevice;
using IO;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using TechObject;

namespace EasyEplannerTests.ProjectContext
{
    /// <summary>
    /// Тесты standalone-поведения ProjectManager (автономное приложение
    /// без EPLAN): инициализация контекста, загрузка и сохранение
    /// описания проекта.
    /// </summary>
    public class ProjectManagerStandaloneTest
    {
        private const string TestBaseObjectEplanName =
            "TEST_STANDALONE_OBJECT";

        private static readonly string TestBaseObjectsDescription =
            "base_tech_objects = function()\n" +
            "    return\n" +
            "    {\n" +
            "        " + TestBaseObjectEplanName + " =\n" +
            "        {\n" +
            "            name = 'Тестовый объект',\n" +
            "            defaultNameEplan = 'TEST',\n" +
            "            s88Level = 2,\n" +
            "            basicName = 'test_standalone_object',\n" +
            "        },\n" +
            "    }\n" +
            "end\n";

        private string assemblyDir;
        private string systemLua;
        private string tempFolder;
        private object savedLog;
        private IProjectContext savedContext;

        [SetUp]
        public void SetUp()
        {
            assemblyDir = Path.GetDirectoryName(
                typeof(ProjectManager).Assembly.Location);
            systemLua = Path.Combine(assemblyDir, "Lua");
            tempFolder = CreateTemporaryFolder();

            savedLog = GetLogsLogField();
            savedContext = ProjectContextHolder.Current;
            ProjectContextHolder.Current = null;

            DeviceManager.GetInstance().Clear();
            IOManager.GetInstance().Clear();
            typeof(ProjectManager).GetField("techObjectManager",
                BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(ProjectManager.GetInstance(),
                    TechObjectManager.GetInstance());
            EncodingDetector.MainFilesEncoding =
                System.Text.Encoding.UTF8;
        }

        [TearDown]
        public void TearDown()
        {
            ProjectContextHolder.Current = savedContext;
            SetLogsLogField(savedLog);

            var techObjectManager = TechObjectManager.GetInstance();
            techObjectManager.TechObjects.Clear();
            techObjectManager.GenericTechObjects.Clear();
            BaseTechObjectManager.GetInstance().Objects.RemoveAll(
                o => o.EplanName == TestBaseObjectEplanName);

            DeviceManager.GetInstance().Clear();
            IOManager.GetInstance().Clear();
            EncodingDetector.MainFilesEncoding = null;

            typeof(ProjectManager).GetField("instance",
                BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, null);

            try
            {
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
            }
            catch (Exception ex) when (ex is IOException ||
                ex is UnauthorizedAccessException)
            {
                // Не мешаем другим тестам, если каталог занят.
            }
        }

        [Test]
        public void LoadTechObjectsFromContext_ReturnsError_WhenContextIsNotSet()
        {
            ProjectContextHolder.Current = null;

            int res = ProjectManager.GetInstance()
                .LoadTechObjectsFromContext(out string errStr);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, res);
                Assert.AreEqual("Контекст проекта не задан.", errStr);
            });
        }

        [Test]
        public void LoadTechObjectsFromContext_ReturnsError_WhenProjectFolderPathIsEmpty()
        {
            var context = new Mock<IProjectContext>();
            context.SetupGet(c => c.ProjectFolderPath).Returns(string.Empty);
            ProjectContextHolder.Current = context.Object;

            int res = ProjectManager.GetInstance()
                .LoadTechObjectsFromContext(out string errStr);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, res);
                Assert.AreEqual("Контекст проекта не задан.", errStr);
            });
        }

        [Test]
        public void LoadTechObjectsFromContext_CreatesEmptyFiles_AndReportsError_WhenProjectFilesAreMissing()
        {
            var context = new FileProjectContext(tempFolder, systemLua,
                assemblyDir);
            ProjectContextHolder.Current = context;

            int res = ProjectManager.GetInstance()
                .LoadTechObjectsFromContext(out string errStr);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(2, res);
                StringAssert.Contains("отсутствует", errStr);
                StringAssert.Contains(
                    ProjectDescriptionSaver.MainTechObjectsFileName, errStr);
                StringAssert.Contains(
                    ProjectDescriptionSaver.MainRestrictionsFileName, errStr);
                Assert.IsTrue(File.Exists(Path.Combine(tempFolder,
                    ProjectDescriptionSaver.MainTechObjectsFileName)));
                Assert.IsTrue(File.Exists(Path.Combine(tempFolder,
                    ProjectDescriptionSaver.MainRestrictionsFileName)));
            });
        }

        [Test]
        public void LoadTechObjectsFromContext_LoadsEmptyProjectFiles_WithoutErrors()
        {
            File.WriteAllText(Path.Combine(tempFolder,
                ProjectDescriptionSaver.MainTechObjectsFileName),
                string.Empty);
            File.WriteAllText(Path.Combine(tempFolder,
                ProjectDescriptionSaver.MainRestrictionsFileName),
                string.Empty);
            var context = new FileProjectContext(tempFolder, systemLua,
                assemblyDir);
            ProjectContextHolder.Current = context;

            int res = ProjectManager.GetInstance()
                .LoadTechObjectsFromContext(out string errStr);

            var techObjectManager = TechObjectManager.GetInstance();
            Assert.Multiple(() =>
            {
                Assert.AreEqual(0, res);
                Assert.IsEmpty(errStr);
                Assert.AreEqual(0, techObjectManager.TechObjects.Count);
                Assert.AreEqual(0,
                    techObjectManager.GenericTechObjects.Count);
            });
        }

        [Test]
        public void TrySaveTechObjectsFromContext_ReturnsFalse_WhenContextIsNotSet()
        {
            ProjectContextHolder.Current = null;

            Assert.IsFalse(ProjectManager.TrySaveTechObjectsFromContext());
        }

        [Test]
        public void TrySaveTechObjectsFromContext_ReturnsFalse_WhenProjectFolderPathIsEmpty()
        {
            var context = new Mock<IProjectContext>();
            context.SetupGet(c => c.ProjectFolderPath).Returns(string.Empty);
            ProjectContextHolder.Current = context.Object;

            Assert.IsFalse(ProjectManager.TrySaveTechObjectsFromContext());
        }

        [Test]
        public void TrySaveTechObjectsFromContext_SavesObjectsAndRestrictions_FromContext()
        {
            var context = new FileProjectContext(tempFolder, systemLua,
                assemblyDir);
            ProjectContextHolder.Current = context;

            bool saved = ProjectManager.TrySaveTechObjectsFromContext();

            Assert.Multiple(() =>
            {
                Assert.IsTrue(saved);
                Assert.AreEqual(2, Directory.GetFiles(tempFolder).Length);
                StringAssert.Contains("init_tech_objects_modes",
                    File.ReadAllText(Path.Combine(tempFolder,
                        ProjectDescriptionSaver.MainTechObjectsFileName)));
                StringAssert.Contains("restrictions =",
                    File.ReadAllText(Path.Combine(tempFolder,
                        ProjectDescriptionSaver.MainRestrictionsFileName)));
            });
        }

        [Test]
        public void TrySaveTechObjectsFromContext_ReportsErrorToLog_WhenSaveFails()
        {
            // silentMode: false и заглушка ILog вместо формы: ошибки
            // сохранения уходят в лог, MessageBox не выводится.
            var messages = new List<string>();
            var log = new Mock<ILog>();
            log.Setup(l => l.AddMessage(It.IsAny<string>()))
                .Callback<string>(messages.Add);
            Logs.Init(log.Object);

            string filePath = Path.Combine(tempFolder,
                "project-path-is-a-file.txt");
            File.WriteAllText(filePath, string.Empty);
            var context = new FileProjectContext(filePath, systemLua,
                assemblyDir);
            ProjectContextHolder.Current = context;

            bool saved = ProjectManager.TrySaveTechObjectsFromContext(
                silentMode: false);

            Assert.Multiple(() =>
            {
                Assert.IsFalse(saved);
                Assert.IsTrue(messages.Any(
                    m => m.Contains("Ошибка сохранения")),
                    "Лог должен содержать сообщение об ошибке сохранения");
            });
        }

        [Test]
        public void TrySaveTechObjectsFromContext_ReportsSuccessToLog_WhenNotSilent()
        {
            var messages = new List<string>();
            var log = new Mock<ILog>();
            log.Setup(l => l.AddMessage(It.IsAny<string>()))
                .Callback<string>(messages.Add);
            Logs.Init(log.Object);

            var context = new FileProjectContext(tempFolder, systemLua,
                assemblyDir);
            ProjectContextHolder.Current = context;

            bool saved = ProjectManager.TrySaveTechObjectsFromContext(
                silentMode: false);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(saved);
                Assert.IsTrue(messages.Any(m => m.Contains("сохранен.")),
                    "Лог должен содержать сообщение об успешном сохранении");
            });
        }

        [Test]
        public void RefreshEditor_DoesNotThrow_WhenEditorIsNotCreated()
        {
            // В standalone редактор создаётся лениво в StartEditInHost.
            Assert.DoesNotThrow(() =>
                ProjectManager.GetInstance().RefreshEditor());
        }

        [Test]
        public void OriginalAssemblyPath_UsesContextPath_WhenContextIsSet()
        {
            var context = new FileProjectContext(tempFolder, systemLua,
                assemblyDir);
            ProjectContextHolder.Current = context;

            Assert.AreEqual(assemblyDir,
                ProjectManager.GetInstance().OriginalAssemblyPath);
        }

        [Test]
        public void SystemFilesPath_UsesContextPath_WhenContextIsSet()
        {
            var context = new FileProjectContext(tempFolder, systemLua,
                assemblyDir);
            ProjectContextHolder.Current = context;

            Assert.AreEqual(systemLua,
                ProjectManager.GetInstance().SystemFilesPath);
        }

        [Test]
        public void InitStandalone_InitializesContextStandaloneModeAndBaseObjects()
        {
            // Загрузка базовых объектов требует sys_base_objects_initializer.lua
            // и файлов описания. Без них загрузчик выводит MessageBox,
            // поэтому при отсутствии каталога тест пропускается.
            string originalLua = FindOriginalLuaFolder();
            if (originalLua == null)
            {
                Assert.Ignore("Не найден каталог Lua с описанием базовых " +
                    "объектов (src\\bin\\<configuration>\\Lua).");
            }

            string tempSysLua = CopySystemLuaWithTestBaseObject(originalLua);
            var context = new FileProjectContext(tempFolder, tempSysLua,
                assemblyDir);

            ProjectManager.GetInstance().InitStandalone(context);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(ProjectManager.GetInstance().IsStandalone);
                Assert.AreSame(context, ProjectContextHolder.Current);
                Assert.AreEqual(assemblyDir,
                    ProjectManager.GetInstance().OriginalAssemblyPath);
                Assert.AreEqual(tempSysLua,
                    ProjectManager.GetInstance().SystemFilesPath);
                Assert.AreEqual(tempSysLua,
                    ProjectManager.GetInstance().OriginalSystemFilesPath);
            });
            Assert.IsTrue(BaseTechObjectManager.GetInstance().Objects
                .Exists(o => o.EplanName == TestBaseObjectEplanName));

            // Без контекста путь сборки берётся из InitStandalone, а
            // путь к Lua - рядом с этой сборкой.
            ProjectContextHolder.Current = null;
            Assert.Multiple(() =>
            {
                Assert.AreEqual(assemblyDir,
                    ProjectManager.GetInstance().OriginalAssemblyPath);
                Assert.AreEqual(Path.Combine(assemblyDir, "Lua"),
                    ProjectManager.GetInstance().OriginalSystemFilesPath);
            });
        }

        /// <summary>
        /// Копирует необходимые системные скрипты во временный каталог и
        /// добавляет минимальное описание одного базового объекта.
        /// </summary>
        private string CopySystemLuaWithTestBaseObject(string originalLua)
        {
            string tempSysLua = Path.Combine(tempFolder, "Lua");
            Directory.CreateDirectory(tempSysLua);
            foreach (string fileName in new[]
                { "sys.lua", "sys_restriction.lua",
                    "sys_base_objects_initializer.lua" })
            {
                File.Copy(Path.Combine(originalLua, fileName),
                    Path.Combine(tempSysLua, fileName), true);
            }

            string baseObjectsDir = Path.Combine(tempSysLua,
                "BaseObjectsDescriptionFiles");
            Directory.CreateDirectory(baseObjectsDir);
            File.WriteAllText(Path.Combine(baseObjectsDir,
                "test_base_objects.lua"), TestBaseObjectsDescription);

            return tempSysLua;
        }

        /// <summary>
        /// Ищет каталог Lua рядом со сборкой надстройки
        /// (src\bin\configuration\Lua) с описанием базовых объектов.
        /// </summary>
        private static string FindOriginalLuaFolder()
        {
            string repoRoot = FindRepositoryRoot();
            string binRoot = repoRoot == null ? null :
                Path.Combine(repoRoot, "src", "bin");
            if (binRoot == null || !Directory.Exists(binRoot))
            {
                return null;
            }

            foreach (string configurationDir in
                Directory.GetDirectories(binRoot))
            {
                string luaDir = Path.Combine(configurationDir, "Lua");
                if (IsBaseObjectsLuaFolder(luaDir))
                {
                    return luaDir;
                }
            }

            return null;
        }

        private static bool IsBaseObjectsLuaFolder(string luaDir)
        {
            string baseObjectsDir = Path.Combine(luaDir,
                "BaseObjectsDescriptionFiles");
            if (!File.Exists(Path.Combine(luaDir,
                "sys_base_objects_initializer.lua")) ||
                !Directory.Exists(baseObjectsDir))
            {
                return false;
            }

            return Directory.GetFiles(baseObjectsDir).Any(file =>
                !string.Equals(Path.GetFileName(file),
                    "DescriptionTemplate.lua",
                    StringComparison.OrdinalIgnoreCase));
        }

        private static string FindRepositoryRoot()
        {
            string dir = Path.GetDirectoryName(
                typeof(ProjectManagerStandaloneTest).Assembly.Location);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, "src", "EasyEPlanner.sln")))
                {
                    return dir;
                }

                dir = Path.GetDirectoryName(dir);
            }

            return null;
        }

        private static string CreateTemporaryFolder()
        {
            string folder = Path.Combine(Path.GetTempPath(),
                "EasyEPlannerStandaloneTest_" +
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            return folder;
        }

        private static object GetLogsLogField()
        {
            return typeof(Logs).GetField("log",
                BindingFlags.NonPublic | BindingFlags.Static)
                ?.GetValue(null);
        }

        private static void SetLogsLogField(object value)
        {
            typeof(Logs).GetField("log",
                BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, value);
        }
    }

    /// <summary>
    /// Дополнительные ветки FileProjectContext, не покрытые тестами
    /// в FileProjectContext.Test.cs.
    /// </summary>
    public class FileProjectContextPathsTest
    {
        [TestCase("a<b")]
        [TestCase("a|b")]
        [TestCase("c:\\fo:o\\bar")]
        public void TryValidateProjectFolder_ReturnsReason_WhenPathIsInvalid(
            string path)
        {
            bool isValid = FileProjectContext.TryValidateProjectFolder(path,
                out string fullPath, out string reason);

            Assert.Multiple(() =>
            {
                Assert.IsFalse(isValid);
                Assert.AreEqual("указан некорректный путь к папке", reason);
                Assert.AreEqual(path, fullPath);
            });
        }

        [Test]
        public void TryValidateProjectFolder_ReturnsReason_WhenPathIsWhitespace()
        {
            bool isValid = FileProjectContext.TryValidateProjectFolder(
                "   ", out string fullPath, out string reason);

            Assert.Multiple(() =>
            {
                Assert.IsFalse(isValid);
                Assert.AreEqual("путь к папке не указан", reason);
                Assert.AreEqual("   ", fullPath);
                Assert.IsFalse(FileProjectContext.IsValidProjectFolder(
                    "   "));
            });
        }

        [Test]
        public void Constructor_TrimsTrailingDirectorySeparators()
        {
            var withBackslash = new FileProjectContext(
                @"C:\projects\MyPlant\", @"C:\app\Lua", @"C:\app");
            var withSlash = new FileProjectContext(
                @"C:\projects\MyPlant/", @"C:\app\Lua", @"C:\app");

            Assert.Multiple(() =>
            {
                Assert.AreEqual(@"C:\projects\MyPlant",
                    withBackslash.ProjectFolderPath);
                Assert.AreEqual("MyPlant", withBackslash.ProjectName);
                Assert.AreEqual(@"C:\projects\MyPlant",
                    withSlash.ProjectFolderPath);
                Assert.AreEqual("MyPlant", withSlash.ProjectName);
                Assert.AreEqual(@"C:\app\Lua", withBackslash.SystemFilesPath);
                Assert.AreEqual(@"C:\app",
                    withBackslash.OriginalAssemblyPath);
            });
        }
    }

    /// <summary>
    /// Тесты StandaloneAppState (сохранение последнего проекта и
    /// состояния окна в standalone-приложении).
    /// Класс internal в EasyEPLANner.exe, поэтому тестируется через
    /// отражение; состояние каталога настроек сохраняется и
    /// восстанавливается в TearDown.
    /// </summary>
    public class StandaloneAppStateTest
    {
        private const string AppAssemblyFileName = "EasyEPLANner.exe";
        private const string AppStateTypeName =
            "EasyEPlanner.App.StandaloneAppState";
        private const string LastProjectFileName = "last-project.txt";
        private const string WindowBoundsFileName = "window-bounds.txt";

        private Type appStateType;
        private Type rectangleType;
        private string settingsDir;
        private string lastProjectPath;
        private string windowBoundsPath;
        private bool settingsDirExisted;
        private bool lastProjectExisted;
        private byte[] lastProjectBackup;
        private bool windowBoundsExisted;
        private byte[] windowBoundsBackup;

        [SetUp]
        public void SetUp()
        {
            Assembly appAssembly = LoadAppAssembly();
            if (appAssembly == null)
            {
                Assert.Ignore("Сборка автономного приложения " +
                    "EasyEPLANner.exe не найдена в src\\bin.");
            }

            appStateType = appAssembly.GetType(AppStateTypeName, true);

            settingsDir = Path.GetDirectoryName(GetDockLayoutPath());
            lastProjectPath = Path.Combine(settingsDir, LastProjectFileName);
            windowBoundsPath = Path.Combine(settingsDir,
                WindowBoundsFileName);

            settingsDirExisted = Directory.Exists(settingsDir);
            lastProjectExisted = File.Exists(lastProjectPath);
            if (lastProjectExisted)
            {
                lastProjectBackup = File.ReadAllBytes(lastProjectPath);
                File.Delete(lastProjectPath);
            }

            windowBoundsExisted = File.Exists(windowBoundsPath);
            if (windowBoundsExisted)
            {
                windowBoundsBackup = File.ReadAllBytes(windowBoundsPath);
                File.Delete(windowBoundsPath);
            }
        }

        [TearDown]
        public void TearDown()
        {
            RestoreFile(lastProjectPath, lastProjectExisted,
                lastProjectBackup);
            RestoreFile(windowBoundsPath, windowBoundsExisted,
                windowBoundsBackup);
            if (!settingsDirExisted && Directory.Exists(settingsDir))
            {
                Directory.Delete(settingsDir, true);
            }
        }

        [Test]
        public void LoadLastProject_ReturnsEmpty_WhenFileDoesNotExist()
        {
            Assert.IsEmpty(LoadLastProject());
        }

        [Test]
        public void SaveLastProject_WritesFileAndLoadReturnsSamePath()
        {
            string projectPath = @"C:\projects\TestProject";

            SaveLastProject(projectPath);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(File.Exists(lastProjectPath));
                Assert.AreEqual(projectPath, LoadLastProject());
            });
        }

        [Test]
        public void SaveLastProject_AcceptsNullAndLoadsEmptyString()
        {
            SaveLastProject(null);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(File.Exists(lastProjectPath));
                Assert.IsEmpty(LoadLastProject());
            });
        }

        [Test]
        public void LoadLastProject_TrimsWhitespace()
        {
            File.WriteAllText(lastProjectPath,
                "  C:\\projects\\TestProject \r\n");

            Assert.AreEqual(@"C:\projects\TestProject", LoadLastProject());
        }

        [Test]
        public void ClearLastProject_DeletesFile()
        {
            SaveLastProject(@"C:\projects\TestProject");

            ClearLastProject();

            Assert.Multiple(() =>
            {
                Assert.IsFalse(File.Exists(lastProjectPath));
                Assert.IsEmpty(LoadLastProject());
            });
        }

        [Test]
        public void EnsureDockLayoutDirectory_CreatesSettingsDirectory()
        {
            EnsureDockLayoutDirectory();

            Assert.Multiple(() =>
            {
                Assert.IsTrue(Directory.Exists(settingsDir));
                StringAssert.EndsWith("dock-layout.xml",
                    GetDockLayoutPath());
            });
        }

        [Test]
        public void SaveAndTryLoadWindowBounds_RoundTrip()
        {
            object bounds = CreateRectangle(10, -20, 800, 600);

            SaveWindowBounds(bounds, FormWindowState.Maximized);

            Assert.AreEqual(string.Format("10|-20|800|600|{0}",
                (int)FormWindowState.Maximized),
                File.ReadAllText(windowBoundsPath));

            bool loaded = TryLoadWindowBounds(out object loadedBounds,
                out object loadedState);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(loaded);
                AssertRectangle(loadedBounds, 10, -20, 800, 600);
                Assert.AreEqual(FormWindowState.Maximized, loadedState);
            });
        }

        [Test]
        public void TryLoadWindowBounds_RestoresNormalState_ForMinimizedWindow()
        {
            SaveWindowBounds(CreateRectangle(0, 0, 800, 600),
                FormWindowState.Minimized);

            bool loaded = TryLoadWindowBounds(out object loadedBounds,
                out object loadedState);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(loaded);
                Assert.AreEqual(FormWindowState.Normal, loadedState);
                AssertRectangle(loadedBounds, 0, 0, 800, 600);
            });
        }

        [Test]
        public void TryLoadWindowBounds_ReturnsFalse_WhenFileDoesNotExist()
        {
            bool loaded = TryLoadWindowBounds(out object loadedBounds,
                out object loadedState);

            Assert.Multiple(() =>
            {
                Assert.IsFalse(loaded);
                AssertRectangle(loadedBounds, 0, 0, 0, 0);
                Assert.AreEqual(FormWindowState.Normal, loadedState);
            });
        }

        [TestCase("1|2|3")]
        [TestCase("a|b|c|d|e")]
        [TestCase("0|0|0|0|1")]
        [TestCase("-10|20|-800|600|1")]
        [TestCase("10|20|800|600|99")]
        public void TryLoadWindowBounds_ReturnsFalse_WhenFileIsMalformed(
            string content)
        {
            File.WriteAllText(windowBoundsPath, content);

            bool loaded = TryLoadWindowBounds(out object _,
                out object _);

            Assert.IsFalse(loaded);
        }

        private string LoadLastProject()
        {
            return (string)InvokeAppState("LoadLastProject");
        }

        private void SaveLastProject(string projectPath)
        {
            InvokeAppState("SaveLastProject", projectPath);
        }

        private void ClearLastProject()
        {
            InvokeAppState("ClearLastProject");
        }

        private void EnsureDockLayoutDirectory()
        {
            InvokeAppState("EnsureDockLayoutDirectory");
        }

        private string GetDockLayoutPath()
        {
            return (string)appStateType.GetProperty("DockLayoutPath",
                BindingFlags.Public | BindingFlags.Static).GetValue(null);
        }

        private void SaveWindowBounds(object bounds,
            FormWindowState windowState)
        {
            InvokeAppState("SaveWindowBounds", bounds, windowState);
        }

        private bool TryLoadWindowBounds(out object bounds,
            out object windowState)
        {
            var args = new object[] { null, null };
            bool result = (bool)appStateType
                .GetMethod("TryLoadWindowBounds",
                    BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, args);

            bounds = args[0];
            windowState = args[1];
            return result;
        }

        private object InvokeAppState(string methodName,
            params object[] args)
        {
            return appStateType.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, args);
        }

        private object CreateRectangle(int x, int y, int width, int height)
        {
            rectangleType = rectangleType ?? GetRectangleParameterType();
            return Activator.CreateInstance(rectangleType,
                x, y, width, height);
        }

        private Type GetRectangleParameterType()
        {
            return appStateType
                .GetMethod("TryLoadWindowBounds",
                    BindingFlags.Public | BindingFlags.Static)
                .GetParameters()[0].ParameterType.GetElementType();
        }

        private void AssertRectangle(object rectangle, int x, int y,
            int width, int height)
        {
            Assert.Multiple(() =>
            {
                Assert.AreEqual(x, GetRectangleValue(rectangle, "X"));
                Assert.AreEqual(y, GetRectangleValue(rectangle, "Y"));
                Assert.AreEqual(width,
                    GetRectangleValue(rectangle, "Width"));
                Assert.AreEqual(height,
                    GetRectangleValue(rectangle, "Height"));
            });
        }

        private int GetRectangleValue(object rectangle, string property)
        {
            return (int)rectangle.GetType()
                .GetProperty(property).GetValue(rectangle);
        }

        private Assembly LoadAppAssembly()
        {
            string repoRoot = FindRepositoryRoot();
            string binRoot = repoRoot == null ? null :
                Path.Combine(repoRoot, "src", "bin");
            if (binRoot == null || !Directory.Exists(binRoot))
            {
                return null;
            }

            foreach (string configurationDir in
                Directory.GetDirectories(binRoot))
            {
                string candidate = Path.Combine(configurationDir,
                    AppAssemblyFileName);
                if (File.Exists(candidate))
                {
                    return Assembly.LoadFrom(candidate);
                }
            }

            return null;
        }

        private static string FindRepositoryRoot()
        {
            string dir = Path.GetDirectoryName(
                typeof(StandaloneAppStateTest).Assembly.Location);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, "src", "EasyEPlanner.sln")))
                {
                    return dir;
                }

                dir = Path.GetDirectoryName(dir);
            }

            return null;
        }

        private static void RestoreFile(string path, bool existed,
            byte[] backup)
        {
            if (existed)
            {
                File.WriteAllBytes(path, backup);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
