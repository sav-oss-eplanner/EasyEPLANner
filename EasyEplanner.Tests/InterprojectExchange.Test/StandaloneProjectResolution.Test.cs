using EasyEPlanner;
using InterprojectExchange;
using NUnit.Framework;
using System;
using System.IO;

namespace Tests.InterprojectExchangeTest
{
    public class StandaloneProjectResolutionTest
    {
        [Test]
        public void MainProjectName_ReadsPacNameFromCurrentFolder()
        {
            string folder = CreateTempDirectory();
            IProjectContext previous = ProjectContextHolder.Current;
            try
            {
                File.WriteAllText(Path.Combine(folder, "main.io.lua"),
                    "PAC_name = 'PacFromFile'\n");
                ProjectContextHolder.Current = new FileProjectContext(folder,
                    folder, folder);

                Assert.AreEqual("PacFromFile", new TestExchange().MainProjectName);
            }
            finally
            {
                ProjectContextHolder.Current = previous;
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void MainProjectName_FallsBackToContextNameWhenMainIoMissing()
        {
            string folder = CreateTempDirectory();
            IProjectContext previous = ProjectContextHolder.Current;
            try
            {
                ProjectContextHolder.Current = new FileProjectContext(folder,
                    folder, folder);

                Assert.AreEqual(Path.GetFileName(folder),
                    new TestExchange().MainProjectName);
            }
            finally
            {
                ProjectContextHolder.Current = previous;
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void MainProjectName_ReturnsEmptyWhenContextIsMissing()
        {
            IProjectContext previous = ProjectContextHolder.Current;
            try
            {
                ProjectContextHolder.Current = null;
                Assert.AreEqual(string.Empty,
                    new TestExchange().MainProjectName);
            }
            finally
            {
                ProjectContextHolder.Current = previous;
            }
        }

        [Test]
        public void TryResolveProjectFolder_UsesMatchingCurrentContext()
        {
            string folder = CreateTempDirectory();
            IProjectContext previous = ProjectContextHolder.Current;
            try
            {
                File.WriteAllText(Path.Combine(folder, "main.io.lua"),
                    "PAC_name = 'CurrentProject'\n");
                ProjectContextHolder.Current = new FileProjectContext(folder,
                    folder, folder);

                bool resolved = InterprojectExchangeStarterTestHelper
                    .InvokeTryResolveProjectFolder(
                        ProjectContextHolder.Current.ProjectName.ToLowerInvariant(),
                        out string resolvedFolder);

                Assert.IsTrue(resolved);
                Assert.AreEqual(Path.GetFullPath(folder), resolvedFolder);
            }
            finally
            {
                ProjectContextHolder.Current = previous;
                InterprojectProjectCatalog.Invalidate();
                Directory.Delete(folder, true);
            }
        }

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(),
                "EasyEPlannerStandalone_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private sealed class TestExchange : global::InterprojectExchange.InterprojectExchange
        {
        }
    }
}
