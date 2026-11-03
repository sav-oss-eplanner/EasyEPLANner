using EplanDevice;
using StaticHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EasyEPlanner
{
    /// <summary>
    /// Сравнение устройств ФСА со слепком main.io.lua.
    /// </summary>
    public static class DeviceLuaChangeComparer
    {
        public static IReadOnlyList<DeviceLuaChange> Compare(
            IEnumerable<IODevice> devices,
            IEnumerable<DeviceLuaSnapshot> snapshots)
        {
            var changes = new List<DeviceLuaChange>();
            if (devices is null || snapshots is null)
                return changes;

            var luaByName = snapshots
                .Where(s => !string.IsNullOrEmpty(s?.Name))
                .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var device in devices)
            {
                if (device is null ||
                    !luaByName.TryGetValue(device.Name, out var lua))
                {
                    continue;
                }

                AddIfChanged(changes, device.Name, DeviceLuaChangeKind.Description,
                    string.Empty,
                    device.Function?.Description ?? device.Description,
                    lua.Description);

                string fsaSubType = GetSubTypeName(device);
                if (!string.IsNullOrEmpty(lua.SubType))
                {
                    AddIfChanged(changes, device.Name, DeviceLuaChangeKind.SubType,
                        string.Empty, fsaSubType, lua.SubType);
                }

                CompareMap(changes, device.Name, DeviceLuaChangeKind.Parameter,
                    ResolveParameters(lua, device), ToStringMap(device.Parameters));
                CompareMap(changes, device.Name, DeviceLuaChangeKind.Property,
                    lua.Properties, ToStringMap(device.Properties));
                // Рабочие параметры заполняются при обновлении проекта
                // (привязка, Check) и в момент сверки ещё не соответствуют ФСА.
            }

            return changes;
        }

        private static void CompareMap(
            List<DeviceLuaChange> changes,
            string deviceName,
            DeviceLuaChangeKind kind,
            IDictionary<string, string> luaValues,
            IDictionary<string, string> fsaValues)
        {
            if (luaValues is null || luaValues.Count == 0)
                return;

            foreach (var pair in luaValues)
            {
                fsaValues.TryGetValue(pair.Key, out string oldValue);
                AddIfChanged(changes, deviceName, kind, pair.Key,
                    oldValue, pair.Value);
            }
        }

        private static void AddIfChanged(
            List<DeviceLuaChange> changes,
            string deviceName,
            DeviceLuaChangeKind kind,
            string fieldName,
            string oldValue,
            string newValue)
        {
            if (kind == DeviceLuaChangeKind.Description)
            {
                if (EplanMultilineText.IsSameDescription(oldValue, newValue))
                    return;

                changes.Add(new DeviceLuaChange(deviceName, kind, fieldName,
                    EplanMultilineText.NormalizeDescription(oldValue),
                    EplanMultilineText.NormalizeDescription(newValue)));
                return;
            }

            if (ValuesEqual(oldValue, newValue))
            {
                return;
            }

            changes.Add(new DeviceLuaChange(deviceName, kind, fieldName,
                FormatDisplay(oldValue), FormatDisplay(newValue)));
        }

        public static bool ValuesEqual(string left, string right)
        {
            left = Normalize(left);
            right = Normalize(right);
            if (left == right)
                return true;

            return TryParseNumber(left, out double leftNumber) &&
                TryParseNumber(right, out double rightNumber) &&
                Math.Abs(leftNumber - rightNumber) < 1e-9;
        }

        private static bool TryParseNumber(string value, out double number)
        {
            return double.TryParse(value, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out number) ||
                double.TryParse(value, NumberStyles.Any,
                    CultureInfo.CurrentCulture, out number);
        }

        private static string Normalize(string value) =>
            (value ?? string.Empty).Replace("\r\n", "\n").Trim();

        private static string FormatDisplay(string value) =>
            Normalize(value);

        private static string GetSubTypeName(IODevice device)
        {
            string name = device.GetDeviceSubTypeStr(
                device.DeviceType, device.DeviceSubType);
            if (!string.IsNullOrEmpty(name))
                return name;

            return device.DeviceSubType == DeviceSubType.NONE
                ? string.Empty
                : device.DeviceSubType.ToString();
        }

        private static Dictionary<string, string> ResolveParameters(
            DeviceLuaSnapshot lua, IODevice device)
        {
            if (lua.NamedParameters.Count > 0)
                return new Dictionary<string, string>(lua.NamedParameters,
                    StringComparer.OrdinalIgnoreCase);

            return MapOrdered(lua.OrderedParameters,
                device.Parameters.Select(p => (p.Key.Name, p.Value)));
        }

        private static Dictionary<string, string> MapOrdered(
            IReadOnlyList<string> ordered,
            IEnumerable<(string Name, object Value)> deviceValues)
        {
            var result = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
            if (ordered is null || ordered.Count == 0)
                return result;

            var all = deviceValues.ToList();
            var nonNull = all.Where(p => p.Value != null).ToList();
            var target = ordered.Count == all.Count ? all :
                ordered.Count == nonNull.Count ? nonNull : all;

            int count = Math.Min(ordered.Count, target.Count);
            for (int i = 0; i < count; i++)
                result[target[i].Name] = ordered[i];

            return result;
        }

        private static Dictionary<string, string> ToStringMap<TKey>(
            IDictionary<TKey, object> source)
        {
            var result = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
            if (source is null)
                return result;

            foreach (var pair in source)
            {
                string key = pair.Key?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(key))
                    continue;

                result[key] = pair.Value?.ToString() ?? string.Empty;
            }

            return result;
        }
    }
}
