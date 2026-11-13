using EplanDevice;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace EasyEPlanner
{
    /// <summary>
    /// Запись принятых изменений из main.io.lua на ФСА и в память.
    /// </summary>
    public static class DeviceLuaChangeApplier
    {
        public static bool Apply(
            IDeviceManager deviceManager,
            IEnumerable<DeviceLuaChange> changes)
        {
            if (deviceManager is null || changes is null)
                return false;

            bool subtypeChanged = false;
            foreach (var change in changes)
            {
                if (change is null || string.IsNullOrEmpty(change.DeviceName))
                    continue;

                var device = deviceManager.GetDevice(change.DeviceName);
                if (device is null || device.Description == StaticHelper.CommonConst.Cap)
                    continue;

                ApplyChange(device, change);
                if (change.Kind == DeviceLuaChangeKind.SubType)
                    subtypeChanged = true;
            }

            return subtypeChanged;
        }

        private static void ApplyChange(IODevice device, DeviceLuaChange change)
        {
            switch (change.Kind)
            {
                case DeviceLuaChangeKind.Description:
                    device.SetDescription(change.NewValue);
                    if (device.Function != null)
                        device.Function.Description = change.NewValue;
                    break;

                case DeviceLuaChangeKind.SubType:
                    if (device.Function != null)
                        device.Function.SubType = change.NewValue;
                    else
                        device.ApplySubTypeName(change.NewValue);
                    break;

                case DeviceLuaChangeKind.Parameter:
                    if (TryParseDouble(change.NewValue, out double parValue))
                    {
                        device.SetParameter(change.FieldName, parValue);
                        device.UpdateParameters();
                    }
                    break;

                case DeviceLuaChangeKind.Property:
                    ApplyProperty(device, change.FieldName, change.NewValue);
                    device.UpdateProperties();
                    break;

                case DeviceLuaChangeKind.RuntimeParameter:
                    if (TryParseDouble(change.NewValue, out double rtValue))
                    {
                        device.SetRuntimeParameter(change.FieldName, rtValue);
                        device.UpdateRuntimeParameters();
                    }
                    break;
            }
        }

        private static void ApplyProperty(IODevice device, string name,
            string value)
        {
            string error = device.SetProperty(name, value);
            if (string.IsNullOrEmpty(error))
                return;

            device.Properties[name] = value ?? string.Empty;
        }

        private static bool TryParseDouble(string value, out double number) =>
            double.TryParse(value, NumberStyles.Any,
                CultureInfo.InvariantCulture, out number);
    }
}
