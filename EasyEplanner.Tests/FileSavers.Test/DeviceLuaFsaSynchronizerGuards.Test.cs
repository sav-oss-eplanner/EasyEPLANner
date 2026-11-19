using EasyEPlanner;
using EplanDevice;
using NUnit.Framework;
using System;
using System.IO;

namespace EasyEplannerTests
{
    public class DeviceLuaFsaSynchronizerGuardsTest
    {
        [TearDown]
        public void TearDown()
        {
            DeviceLuaFsaSynchronizer.ConfirmChanges = null;
            DeviceManager.GetInstance().Clear();
        }

        [Test]
        public void TryApplyFromProjectFolder_WhenManagerIsNull_ReturnsWithoutPrompt()
        {
            DeviceLuaFsaSynchronizer.ConfirmChanges = _ =>
            {
                Assert.Fail("There are no changes to confirm.");
                return false;
            };

            Assert.DoesNotThrow(() =>
                DeviceLuaFsaSynchronizer.TryApplyFromProjectFolder(
                    null, "some-folder", null));
        }

        [Test]
        public void TryApplyFromProjectFolder_WhenFolderIsEmpty_ReturnsWithoutPrompt()
        {
            DeviceLuaFsaSynchronizer.ConfirmChanges = _ =>
            {
                Assert.Fail("There are no changes to confirm.");
                return false;
            };

            Assert.DoesNotThrow(() =>
                DeviceLuaFsaSynchronizer.TryApplyFromProjectFolder(
                    DeviceManager.GetInstance(), string.Empty, null));
        }

        [Test]
        public void TryApplyFromProjectFolder_WhenMainIoIsMissing_ReturnsWithoutPrompt()
        {
            string folder = CreateTempDirectory();
            DeviceLuaFsaSynchronizer.ConfirmChanges = _ =>
            {
                Assert.Fail("There are no changes to confirm.");
                return false;
            };
            try
            {
                Assert.DoesNotThrow(() =>
                    DeviceLuaFsaSynchronizer.TryApplyFromProjectFolder(
                        DeviceManager.GetInstance(), folder, null));
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void TryApplyFromProjectFolder_WhenNoChanges_DoesNotAskForConfirmation()
        {
            string folder = CreateTempDirectory();
            File.WriteAllText(Path.Combine(folder, "main.io.lua"),
                "devices = {}\n");
            DeviceLuaFsaSynchronizer.ConfirmChanges = _ =>
            {
                Assert.Fail("There are no changes to confirm.");
                return false;
            };
            try
            {
                Assert.DoesNotThrow(() =>
                    DeviceLuaFsaSynchronizer.TryApplyFromProjectFolder(
                        DeviceManager.GetInstance(), folder, null));
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        private static string CreateTempDirectory()
        {
            string folder = Path.Combine(Path.GetTempPath(),
                "EasyEPlannerLuaSync_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            return folder;
        }
    }
}
