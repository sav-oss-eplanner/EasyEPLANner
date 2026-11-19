using EasyEPlanner;
using NUnit.Framework;
using System;
using System.IO;

namespace EasyEplannerTests.ProjectContext
{
    public class FileProjectContextTest
    {
        [TestCase(null)]
        [TestCase("")]
        public void IsValidProjectFolder_ReturnsFalse_WhenPathIsEmpty(
            string path)
        {
            Assert.IsFalse(FileProjectContext.IsValidProjectFolder(path));
        }

        [Test]
        public void TryValidateProjectFolder_ReturnsReason_WhenPathIsEmpty()
        {
            bool isValid = FileProjectContext.TryValidateProjectFolder(null,
                out string fullPath, out string reason);

            Assert.IsFalse(isValid);
            Assert.IsNull(fullPath);
            Assert.AreEqual("путь к папке не указан", reason);
        }

        [Test]
        public void TryValidateProjectFolder_ReturnsReason_WhenDirectoryIsMissing()
        {
            string missingFolder = Path.Combine(Path.GetTempPath(),
                "EasyEPlannerProjectTest_" + Guid.NewGuid().ToString("N"));

            bool isValid = FileProjectContext.TryValidateProjectFolder(
                missingFolder, out string fullPath, out string reason);

            Assert.IsFalse(isValid);
            Assert.AreEqual(Path.GetFullPath(missingFolder), fullPath);
            Assert.AreEqual("папка не существует или недоступна", reason);
        }

        [Test]
        public void TryValidateProjectFolder_ReturnsReason_WhenRequiredFileIsMissing()
        {
            string folder = CreateTemporaryFolder();
            try
            {
                bool isValid = FileProjectContext.TryValidateProjectFolder(
                    folder, out string fullPath, out string reason);

                Assert.IsFalse(isValid);
                Assert.AreEqual(Path.GetFullPath(folder), fullPath);
                Assert.AreEqual("отсутствует обязательный файл main.objects.lua",
                    reason);
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void TryValidateProjectFolder_ReturnsFullPath_WhenProjectIsValid()
        {
            string folder = CreateTemporaryFolder();
            try
            {
                File.WriteAllText(Path.Combine(folder, "main.objects.lua"),
                    string.Empty);

                bool isValid = FileProjectContext.TryValidateProjectFolder(
                    folder, out string fullPath, out string reason);

                Assert.IsTrue(isValid);
                Assert.AreEqual(Path.GetFullPath(folder), fullPath);
                Assert.IsNull(reason);
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void IsValidProjectFolder_ReturnsFalse_WhenRequiredFileIsMissing()
        {
            string folder = CreateTemporaryFolder();
            try
            {
                Assert.IsFalse(FileProjectContext.IsValidProjectFolder(folder));
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void IsValidProjectFolder_ReturnsTrue_WhenMainObjectsExists()
        {
            string folder = CreateTemporaryFolder();
            try
            {
                File.WriteAllText(Path.Combine(folder, "main.objects.lua"),
                    string.Empty);

                Assert.IsTrue(FileProjectContext.IsValidProjectFolder(folder));
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        private static string CreateTemporaryFolder()
        {
            string folder = Path.Combine(Path.GetTempPath(),
                "EasyEPlannerProjectTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            return folder;
        }
    }
}
