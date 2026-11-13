using EasyEPlanner;
using EplanDevice;
using IO;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using TechObject;

namespace EasyEplannerTests
{
    public class ProjectDescriptionSaverCoverageTest
    {
        private string temporaryRoot;
        private Encoding originalEncoding;

        [SetUp]
        public void SetUp()
        {
            originalEncoding = EncodingDetector.MainFilesEncoding;
            EncodingDetector.MainFilesEncoding = Encoding.UTF8;
            temporaryRoot = Path.Combine(Path.GetTempPath(),
                "EasyEPlannerSaveCoverage_" + Guid.NewGuid().ToString("N"));
            DeviceManager.GetInstance().Clear();
            IOManager.GetInstance().Clear();
            TechObjectManager.GetInstance().TechObjects.Clear();
            TechObjectManager.GetInstance().GenericTechObjects.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            EncodingDetector.MainFilesEncoding = originalEncoding;
            DeviceManager.GetInstance().Clear();
            IOManager.GetInstance().Clear();
            TechObjectManager.GetInstance().TechObjects.Clear();
            TechObjectManager.GetInstance().GenericTechObjects.Clear();
            if (Directory.Exists(temporaryRoot))
                Directory.Delete(temporaryRoot, true);
        }

        [Test]
        public void SaveTechObjectsAndRestrictions_CreatesProjectFilesAndFolder()
        {
            string projectFolder = Path.Combine(temporaryRoot, "project");

            ProjectDescriptionSaver.SaveTechObjectsAndRestrictions(
                "TestProject", projectFolder);

            string objectsPath = Path.Combine(projectFolder,
                ProjectDescriptionSaver.MainTechObjectsFileName);
            string restrictionsPath = Path.Combine(projectFolder,
                ProjectDescriptionSaver.MainRestrictionsFileName);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(Directory.Exists(projectFolder));
                Assert.IsTrue(File.Exists(objectsPath));
                Assert.IsTrue(File.Exists(restrictionsPath));
                StringAssert.Contains("PAC_name = 'TestProject'",
                    File.ReadAllText(objectsPath));
                StringAssert.Contains("Файл ограничений проекта",
                    File.ReadAllText(restrictionsPath));
            });
        }

        [Test]
        public void SaveMainIoFile_WhenProjectHasNoIoAndFileIsMissing_SkipsCreation()
        {
            Directory.CreateDirectory(temporaryRoot);

            ProjectDescriptionSaver.SaveMainIoFile("EmptyProject",
                temporaryRoot);

            Assert.IsFalse(File.Exists(Path.Combine(temporaryRoot,
                "main.io.lua")));
        }

        [Test]
        public void SaveMainIoFile_WhenFileAlreadyExists_RewritesHeader()
        {
            Directory.CreateDirectory(temporaryRoot);
            string path = Path.Combine(temporaryRoot, "main.io.lua");
            File.WriteAllText(path, "old content");

            ProjectDescriptionSaver.SaveMainIoFile("ExistingProject",
                temporaryRoot);

            string saved = File.ReadAllText(path);
            StringAssert.Contains("PAC_name = 'ExistingProject'", saved);
            StringAssert.DoesNotContain("old content", saved);
        }
    }
}
