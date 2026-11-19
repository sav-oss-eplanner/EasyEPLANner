using EplanDevice;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Forms;

namespace EasyEPlanner
{
    /// <summary>
    /// Сверка main.io.lua с устройствами ФСА при открытии проекта в EPLAN.
    /// </summary>
    public static class DeviceLuaFsaSynchronizer
    {
        /// <summary>
        /// Подтверждение применения изменений. В тестах подменяется.
        /// </summary>
        public static Func<IReadOnlyList<DeviceLuaChange>, bool> ConfirmChanges
        { get; set; } = AskUser;

        public static void TryApplyFromProjectFolder(
            IDeviceManager deviceManager,
            string projectFolder,
            Action resynchronizeDevices)
        {
            if (deviceManager is null || string.IsNullOrEmpty(projectFolder))
                return;

            string path = Path.Combine(projectFolder,
                LuaMainIoLoader.MainIoFileName);
            if (!File.Exists(path))
                return;

            try
            {
                var snapshots = LuaMainIoLoader.ParseDeviceSnapshotsFromFile(path);
                var changes = DeviceLuaChangeComparer.Compare(
                    deviceManager.Devices, snapshots);
                if (changes.Count == 0)
                    return;

                var confirm = ConfirmChanges ?? AskUser;
                if (!confirm(changes))
                    return;

                bool subtypeChanged = DeviceLuaChangeApplier.Apply(
                    deviceManager, changes);
                if (subtypeChanged)
                    resynchronizeDevices?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось сверить устройства с main.io.lua:\n" + ex.Message,
                    "EasyEPlanner",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        [ExcludeFromCodeCoverage]
        private static bool AskUser(IReadOnlyList<DeviceLuaChange> changes)
        {
            using (var dialog = new DeviceLuaChangesDialog(changes))
            {
                return dialog.ShowDialog() == DialogResult.OK;
            }
        }
    }
}
