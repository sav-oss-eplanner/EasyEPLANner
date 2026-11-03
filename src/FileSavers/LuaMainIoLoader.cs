using EplanDevice;
using IO;
using LuaInterface;
using StaticHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EasyEPlanner
{
    /// <summary>
    /// Загрузка узлов и устройств из main.io.lua в IOManager и DeviceManager
    /// (для standalone App, без EPLAN).
    /// </summary>
    public static class LuaMainIoLoader
    {
        public const string MainIoFileName = "main.io.lua";

        private static readonly Regex ExtensionNodeNameRegex = new Regex(
            @"^A(?<parent>\d+)\.(?<ext>\d+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            RegexDefaults.Timeout);

        /// <summary>
        /// Загрузить main.io.lua в менеджеры.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public static void LoadFromFile(string pathToMainIo)
        {
            if (string.IsNullOrEmpty(pathToMainIo) || !File.Exists(pathToMainIo))
                return;

            string luaText;
            using (var reader = new StreamReader(pathToMainIo,
                EncodingDetector.DetectFileEncoding(pathToMainIo), true))
            {
                luaText = reader.ReadToEnd();
            }

            LoadFromLua(luaText);
        }

        /// <summary>
        /// Прочитать описания устройств из main.io.lua без изменения менеджеров.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public static IReadOnlyList<DeviceLuaSnapshot> ParseDeviceSnapshotsFromFile(
            string pathToMainIo)
        {
            if (string.IsNullOrEmpty(pathToMainIo) || !File.Exists(pathToMainIo))
                return Array.Empty<DeviceLuaSnapshot>();

            string luaText;
            using (var reader = new StreamReader(pathToMainIo,
                EncodingDetector.DetectFileEncoding(pathToMainIo), true))
            {
                luaText = reader.ReadToEnd();
            }

            return ParseDeviceSnapshots(luaText);
        }

        /// <summary>
        /// Прочитать описания устройств из текста main.io.lua без изменения менеджеров.
        /// </summary>
        public static IReadOnlyList<DeviceLuaSnapshot> ParseDeviceSnapshots(
            string luaText)
        {
            if (string.IsNullOrWhiteSpace(luaText))
                return Array.Empty<DeviceLuaSnapshot>();

            var lua = new Lua();
            lua.DoString(luaText);

            var devices = lua["devices"] as LuaTable;
            if (devices == null)
                return Array.Empty<DeviceLuaSnapshot>();

            var result = new List<DeviceLuaSnapshot>();
            foreach (var key in EnumerateLuaKeys(devices))
            {
                if (devices[key] is not LuaTable deviceTable)
                    continue;

                string name = Convert.ToString(deviceTable["name"] ?? "");
                if (string.IsNullOrEmpty(name))
                    continue;

                var snapshot = new DeviceLuaSnapshot
                {
                    Name = name,
                    Description = Convert.ToString(deviceTable["descr"] ?? ""),
                    SubType = ResolveSubtypeName(name, deviceTable["subtype"]),
                };

                FillSnapshotParameters(snapshot, deviceTable["par"] as LuaTable);
                FillSnapshotRuntimeParameters(snapshot,
                    deviceTable["rt_par"] as LuaTable);
                FillSnapshotProperties(snapshot, deviceTable["prop"] as LuaTable);

                result.Add(snapshot);
            }

            return result;
        }

        /// <summary>
        /// Загрузить содержимое main.io.lua.
        /// </summary>
        public static void LoadFromLua(string luaText)
        {
            if (string.IsNullOrWhiteSpace(luaText))
                return;

            IOManager.GetInstance().Clear();
            DeviceManager.GetInstance().Clear();

            var lua = new Lua();
            lua.DoString(luaText);

            LoadNodes(lua);
            LoadDevices(lua);
        }

        private static void LoadNodes(Lua lua)
        {
            var nodes = lua["nodes"] as LuaTable;
            if (nodes == null)
                return;

            var nodeTables = EnumerateLuaValues(nodes)
                .OfType<LuaTable>()
                .ToList();

            foreach (var nodeTable in nodeTables)
            {
                string name = Convert.ToString(nodeTable["name"] ?? string.Empty);
                if (IsExtensionNodeName(name))
                    continue;

                LoadMainNode(nodeTable, name);
            }

            foreach (var nodeTable in nodeTables)
            {
                string name = Convert.ToString(nodeTable["name"] ?? string.Empty);
                if (!IsExtensionNodeName(name))
                    continue;

                LoadExtensionNode(nodeTable, name);
            }
        }

        private static void LoadMainNode(LuaTable nodeTable, string name)
        {
            int ntype = ToInt(nodeTable["ntype"], -1);
            int n = ToInt(nodeTable["n"], 1);
            string ip = Convert.ToString(nodeTable["IP"] ?? string.Empty);
            string location = Convert.ToString(
                nodeTable["location"] ?? string.Empty);
            string locationDescription = Convert.ToString(
                nodeTable["location_description"] ?? string.Empty);
            string typeName = IONodeInfo.GetNameByType(
                (IONode.TYPES)ntype) ?? "750-xxx";
            int nodeNumber = ParseNodeNumber(name, n);

            var node = IOManager.GetInstance().AddNode(n, nodeNumber,
                typeName, ip, name, location, locationDescription);

            if (ntype == (int)IONode.TYPES.T_EMPTY)
            {
                IOManager.GetInstance().StoreNtypeEnabled(name, false);
                node.NtypeEnabled = false;
            }

            LoadModules(node, nodeTable["modules"] as LuaTable);
        }

        private static void LoadExtensionNode(LuaTable nodeTable, string name)
        {
            var match = ExtensionNodeNameRegex.Match(name);
            if (!match.Success)
                return;

            int parentNumber = int.Parse(match.Groups["parent"].Value);
            int extensionNumber = int.Parse(match.Groups["ext"].Value);
            int parentIdx = FindNodeIndexByNumber(parentNumber);
            if (parentIdx < 0)
                return;

            int ntype = ToInt(nodeTable["ntype"],
                (int)IONode.TYPES.T_PXC_EXTENSION);
            string ip = Convert.ToString(nodeTable["IP"] ?? string.Empty);
            string location = Convert.ToString(
                nodeTable["location"] ?? string.Empty);
            string locationDescription = Convert.ToString(
                nodeTable["location_description"] ?? string.Empty);
            string typeName = IONodeInfo.GetNameByType(
                (IONode.TYPES)ntype) ?? "750-xxx";
            int nodeNumber = parentNumber * 100 + extensionNumber;

            var extensionNode = IOManager.GetInstance().AddExtensionNode(
                parentIdx,
                new IOManager.ExtensionNodeInfo
                {
                    ExtensionNumber = extensionNumber,
                    NodeNumber = nodeNumber,
                    Type = typeName,
                    IP = ip,
                    Name = name,
                    Location = location,
                    LocationDescription = locationDescription,
                });

            if (extensionNode == null)
                return;

            if (ntype == (int)IONode.TYPES.T_EMPTY)
            {
                IOManager.GetInstance().StoreNtypeEnabled(name, false);
                extensionNode.NtypeEnabled = false;
            }

            LoadModules(extensionNode, nodeTable["modules"] as LuaTable);
        }

        private static int FindNodeIndexByNumber(int nodeNumber)
        {
            var nodes = IOManager.GetInstance().IONodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i]?.NodeNumber == nodeNumber ||
                    nodes[i]?.N == nodeNumber)
                {
                    return i;
                }
            }

            return nodeNumber == 1 ? 0 : -1;
        }

        private static bool IsExtensionNodeName(string name) =>
            !string.IsNullOrEmpty(name) &&
            ExtensionNodeNameRegex.IsMatch(name);

        private static void LoadModules(IIONode node, LuaTable modulesTable)
        {
            if (node == null || modulesTable == null)
                return;

            int position = 1;
            foreach (var key in OrderLuaTableKeys(modulesTable))
            {
                TryAddModuleAtPosition(node, modulesTable[key], position);
                position++;
            }
        }

        /// <summary>
        /// LuaInterface.LuaTable.Keys — ICollection, не IEnumerable&lt;object&gt;.
        /// </summary>
        private static IEnumerable<object> OrderLuaTableKeys(LuaTable table)
        {
            if (table?.Keys is not IEnumerable keys)
                return Array.Empty<object>();

            return keys.Cast<object>()
                .OrderBy(k =>
                {
                    try { return Convert.ToDouble(k); }
                    catch { return double.MaxValue; }
                });
        }

        private static IEnumerable<object> EnumerateLuaValues(LuaTable table)
        {
            if (table?.Values is not IEnumerable values)
                return Array.Empty<object>();

            return values.Cast<object>();
        }

        private static void TryAddModuleAtPosition(IIONode node,
            object moduleEntry, int position)
        {
            int physicalNumber = ResolvePhysicalNumber(node, position);

            if (!TryParseModuleNumber(moduleEntry, out int moduleNumber))
            {
                TrySetModule(node, CreateStubModule(physicalNumber), position);
                return;
            }

            var info = IOModuleInfo.GetModuleInfoByNumber(moduleNumber);
            GetInAndOutOffset(node, info, out int inOffset, out int outOffset);

            var module = new IOModule(inOffset, outOffset, info, physicalNumber,
                info.Name, (IOModule.EplanData)null);

            node.DI_count += info.DICount;
            node.DO_count += info.DOCount;
            node.AI_count += info.AICount;
            node.AO_count += info.AOCount;

            TrySetModule(node, module, position);
        }

        private static int ResolvePhysicalNumber(IIONode node, int position)
        {
            // A1 → A2,A3…; A100 → A101,A102…
            int baseNumber = node.NodeNumber;
            if (baseNumber <= 0)
                baseNumber = node.N;
            return baseNumber + position;
        }

        private static void TrySetModule(IIONode node, IIOModule module,
            int position)
        {
            try
            {
                node.SetModule(module, position);
            }
            catch
            {
                try
                {
                    node.SetModule(
                        CreateStubModule(ResolvePhysicalNumber(node, position)),
                        position);
                }
                catch
                {
                    // ignore
                }
            }
        }

        private static IIOModule CreateStubModule(int physicalNumber) =>
            new IOModule(0, 0, IOModuleInfo.Stub, physicalNumber,
                IOModuleInfo.Stub.Name, (IOModule.EplanData)null);

        private static bool TryParseModuleNumber(object moduleEntry,
            out int moduleNumber)
        {
            moduleNumber = -1;
            if (moduleEntry == null)
                return false;

            if (moduleEntry is double or float or int or long)
            {
                moduleNumber = ToInt(moduleEntry, -1);
                return moduleNumber > 0;
            }

            if (moduleEntry is not LuaTable moduleTable)
                return false;

            if (moduleTable.Keys.Count == 0)
                return false;

            foreach (var moduleKey in EnumerateLuaKeys(moduleTable))
            {
                moduleNumber = ToInt(moduleTable[moduleKey], -1);
                if (moduleNumber > 0)
                    return true;
            }

            return false;
        }

        private static IEnumerable<object> EnumerateLuaKeys(LuaTable table)
        {
            if (table?.Keys is not IEnumerable keys)
                return Array.Empty<object>();

            return keys.Cast<object>();
        }

        private static void GetInAndOutOffset(IIONode node,
            IOModuleInfo moduleInfo, out int inOffset, out int outOffset)
        {
            inOffset = 0;
            outOffset = 0;

            switch (moduleInfo.AddressSpaceType)
            {
                case IOModuleInfo.ADDRESS_SPACE_TYPE.DI:
                    inOffset = node.DI_count;
                    break;
                case IOModuleInfo.ADDRESS_SPACE_TYPE.DO:
                    outOffset = node.DO_count;
                    break;
                case IOModuleInfo.ADDRESS_SPACE_TYPE.AI:
                    inOffset = node.AI_count;
                    break;
                case IOModuleInfo.ADDRESS_SPACE_TYPE.AO:
                    outOffset = node.AO_count;
                    break;
                case IOModuleInfo.ADDRESS_SPACE_TYPE.AOAI:
                case IOModuleInfo.ADDRESS_SPACE_TYPE.AOAIDODI:
                    inOffset = node.AI_count;
                    outOffset = node.AO_count;
                    break;
                case IOModuleInfo.ADDRESS_SPACE_TYPE.DODI:
                    inOffset = node.DI_count;
                    outOffset = node.DO_count;
                    break;
            }
        }

        private static void LoadDevices(Lua lua)
        {
            var devices = lua["devices"] as LuaTable;
            if (devices == null)
                return;

            foreach (var key in EnumerateLuaKeys(devices))
            {
                if (devices[key] is not LuaTable deviceTable)
                    continue;

                string name = Convert.ToString(deviceTable["name"] ?? "");
                string descr = Convert.ToString(deviceTable["descr"] ?? "");
                string article = Convert.ToString(
                    deviceTable["article"] ?? "");
                string subtype = ResolveSubtypeName(name,
                    deviceTable["subtype"]);

                string props = BuildPropertiesString(
                    deviceTable["prop"] as LuaTable);

                var added = DeviceManager.GetInstance().AddDeviceFromLua(
                    name, descr, subtype, string.Empty, string.Empty, props,
                    1, out _, article, string.Empty);

                if (added is not IODevice ioDevice)
                    continue;

                LoadDeviceParameters(ioDevice, deviceTable["par"] as LuaTable);
                LoadDeviceRuntimeParameters(ioDevice,
                    deviceTable["rt_par"] as LuaTable);

                // Как при сохранении main.io.lua — иначе порядковые
                // привязки DO/DI клапанов попадают не на те каналы.
                ioDevice.SortChannels();

                LoadDeviceChannels(ioDevice, deviceTable, "DO",
                    IOModuleInfo.ADDRESS_SPACE_TYPE.DO);
                LoadDeviceChannels(ioDevice, deviceTable, "DI",
                    IOModuleInfo.ADDRESS_SPACE_TYPE.DI);
                LoadDeviceChannels(ioDevice, deviceTable, "AO",
                    IOModuleInfo.ADDRESS_SPACE_TYPE.AO);
                LoadDeviceChannels(ioDevice, deviceTable, "AI",
                    IOModuleInfo.ADDRESS_SPACE_TYPE.AI);
            }

            // GetDevice использует BinarySearch — список должен быть отсортирован.
            DeviceManager.GetInstance().Sort();
        }

        /// <summary>
        /// par в main.io.lua: именованная таблица (ПИД) или массив значений
        /// в порядке Parameters (имена только в комментариях --[[P_…]]).
        /// </summary>
        private static void FillSnapshotParameters(DeviceLuaSnapshot snapshot,
            LuaTable parTable)
        {
            if (snapshot == null || parTable == null)
                return;

            if (TryLoadNamedNumericTable(parTable, out var namedValues))
            {
                foreach (var pair in namedValues)
                    snapshot.NamedParameters[pair.Key] = FormatLuaNumber(pair.Value);
                return;
            }

            foreach (var value in EnumerateOrderedNumericValues(parTable))
                snapshot.OrderedParameters.Add(FormatLuaNumber(value));
        }

        private static void FillSnapshotRuntimeParameters(
            DeviceLuaSnapshot snapshot, LuaTable rtParTable)
        {
            if (snapshot == null || rtParTable == null)
                return;

            if (TryLoadNamedNumericTable(rtParTable, out var namedValues))
            {
                foreach (var pair in namedValues)
                    snapshot.NamedRuntimeParameters[pair.Key] =
                        FormatLuaNumber(pair.Value);
                return;
            }

            foreach (var value in EnumerateOrderedNumericValues(rtParTable))
                snapshot.OrderedRuntimeParameters.Add(FormatLuaNumber(value));
        }

        private static void FillSnapshotProperties(DeviceLuaSnapshot snapshot,
            LuaTable propTable)
        {
            if (snapshot == null || propTable == null)
                return;

            foreach (var key in EnumerateLuaKeys(propTable))
            {
                string propName = Convert.ToString(key);
                if (string.IsNullOrEmpty(propName))
                    continue;

                object value = propTable[key];
                string propValue;
                if (value is LuaTable valueTable)
                {
                    propValue = string.Join(",",
                        EnumerateLuaValues(valueTable)
                            .Select(v => Convert.ToString(v).Trim('\'')));
                }
                else
                {
                    propValue = Convert.ToString(value ?? "").Trim('\'');
                }

                snapshot.Properties[propName] = propValue;
            }
        }

        private static string FormatLuaNumber(double value) =>
            value.ToString("G", System.Globalization.CultureInfo.InvariantCulture);

        private static void LoadDeviceParameters(IODevice device,
            LuaTable parTable)
        {
            if (device == null || parTable == null ||
                device.Parameters.Count == 0)
                return;

            if (TryLoadNamedNumericTable(parTable,
                out var namedValues))
            {
                foreach (var pair in namedValues)
                    device.SetParameter(pair.Key, pair.Value);
                return;
            }

            var values = EnumerateOrderedNumericValues(parTable);
            if (values.Count == 0)
                return;

            var allKeys = device.Parameters.Keys.ToList();
            var nonNullKeys = device.Parameters
                .Where(p => p.Value != null)
                .Select(p => p.Key)
                .ToList();

            // При сохранении пишутся только непустые; если пользователь
            // задал бывшие null — число значений = числу всех ключей.
            IList<IODevice.Parameter> targetKeys =
                values.Count == allKeys.Count ? allKeys :
                values.Count == nonNullKeys.Count ? nonNullKeys :
                allKeys;

            int count = Math.Min(values.Count, targetKeys.Count);
            for (int i = 0; i < count; i++)
                device.SetParameter(targetKeys[i].Name, values[i]);
        }

        private static void LoadDeviceRuntimeParameters(IODevice device,
            LuaTable rtParTable)
        {
            if (device == null || rtParTable == null ||
                device.RuntimeParameters.Count == 0)
                return;

            if (TryLoadNamedNumericTable(rtParTable,
                out var namedValues))
            {
                foreach (var pair in namedValues)
                    device.SetRuntimeParameter(pair.Key, pair.Value);
                return;
            }

            var values = EnumerateOrderedNumericValues(rtParTable);
            if (values.Count == 0)
                return;

            var allKeys = device.RuntimeParameters.Keys.ToList();
            var nonNullKeys = device.RuntimeParameters
                .Where(p => p.Value != null)
                .Select(p => p.Key)
                .ToList();

            IList<string> targetKeys =
                values.Count == allKeys.Count ? allKeys :
                values.Count == nonNullKeys.Count ? nonNullKeys :
                allKeys;

            int count = Math.Min(values.Count, targetKeys.Count);
            for (int i = 0; i < count; i++)
                device.SetRuntimeParameter(targetKeys[i], values[i]);
        }

        private static bool TryLoadNamedNumericTable(LuaTable table,
            out Dictionary<string, double> namedValues)
        {
            namedValues = new Dictionary<string, double>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var key in EnumerateLuaKeys(table))
            {
                if (key is double or float or int or long)
                    continue;

                string name = Convert.ToString(key);
                if (string.IsNullOrEmpty(name) || int.TryParse(name, out _))
                    continue;

                if (TryToDouble(table[key], out double number))
                    namedValues[name] = number;
            }

            return namedValues.Count > 0;
        }

        private static List<double> EnumerateOrderedNumericValues(
            LuaTable table)
        {
            var result = new List<double>();
            foreach (var key in OrderLuaTableKeys(table))
            {
                if (TryToDouble(table[key], out double number))
                    result.Add(number);
            }

            return result;
        }

        private static bool TryToDouble(object value, out double number)
        {
            number = 0;
            if (value == null || value is LuaTable)
                return false;
            try
            {
                number = Convert.ToDouble(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void LoadDeviceChannels(IODevice device,
            LuaTable deviceTable, string channelGroupName,
            IOModuleInfo.ADDRESS_SPACE_TYPE addressSpace)
        {
            if (deviceTable[channelGroupName] is not LuaTable channels)
                return;

            var deviceChannels = device.Channels
                .Where(c => c.Name == channelGroupName)
                .ToList();
            int channelOrdinal = 0;

            foreach (var key in OrderLuaTableKeys(channels))
            {
                if (channels[key] is not LuaTable ch)
                    continue;

                // Каналы сохраняются строго в порядке их описания у
                // устройства (включая непривязанные — "заглушки"), поэтому
                // сопоставление идёт по позиции, без комментария.
                if (channelOrdinal >= deviceChannels.Count)
                    break;
                IODevice.IOChannel targetChannel = deviceChannels[channelOrdinal];
                channelOrdinal++;

                int node = ToInt(ch["node"], -1);
                int physicalPort = ToInt(ch["physical_port"], -1);
                int logicalPort = ToInt(ch["logical_port"], -1);
                int moduleOffset = ToInt(ch["module_offset"], 0);
                if (node < 0 || physicalPort < 0)
                    continue;

                var nodes = IOManager.GetInstance().IONodes;
                if (node >= nodes.Count || nodes[node] == null)
                    continue;

                // В main.io.lua номер модуля не сохраняется — только
                // module_offset; logical_port — это клемма, не слот модуля.
                int moduleSlot = FindModuleSlot(nodes[node], moduleOffset,
                    addressSpace);
                if (moduleSlot < 1)
                    continue;

                var ioModule = nodes[node].IOModules[moduleSlot - 1];
                int fullModule = ioModule?.PhysicalNumber
                    ?? (nodes[node].NodeNumber + moduleSlot);

                DeviceManager.GetInstance().AddDeviceChannel(device,
                    addressSpace, node, moduleSlot, physicalPort,
                    targetChannel.Comment ?? string.Empty,
                    out _, fullModule, logicalPort, moduleOffset,
                    channelGroupName);
            }
        }

        /// <summary>
        /// Найти слот модуля (1-based) по сохранённому module_offset.
        /// </summary>
        private static int FindModuleSlot(IIONode node, int moduleOffset,
            IOModuleInfo.ADDRESS_SPACE_TYPE addressSpace)
        {
            if (node?.IOModules == null)
                return -1;

            for (int i = 0; i < node.IOModules.Count; i++)
            {
                var module = node.IOModules[i];
                if (module?.Info == null ||
                    module.Info.Name == IOModuleInfo.Stub.Name)
                {
                    continue;
                }

                bool match = addressSpace switch
                {
                    IOModuleInfo.ADDRESS_SPACE_TYPE.DI or
                    IOModuleInfo.ADDRESS_SPACE_TYPE.AI =>
                        module.InOffset == moduleOffset,
                    IOModuleInfo.ADDRESS_SPACE_TYPE.DO or
                    IOModuleInfo.ADDRESS_SPACE_TYPE.AO =>
                        module.OutOffset == moduleOffset,
                    _ => module.InOffset == moduleOffset ||
                        module.OutOffset == moduleOffset,
                };

                if (match)
                    return i + 1;
            }

            // Fallback: единственный непустой модуль на узле.
            var realModules = node.IOModules
                .Select((m, idx) => (m, idx))
                .Where(x => x.m?.Info != null &&
                    x.m.Info.Name != IOModuleInfo.Stub.Name)
                .ToList();
            return realModules.Count == 1 ? realModules[0].idx + 1 : -1;
        }

        private static string BuildPropertiesString(LuaTable propTable)
        {
            if (propTable == null)
                return string.Empty;

            // Формат как у EPLAN / ProcessProperties: Name='value',
            var builder = new StringBuilder();
            foreach (var key in EnumerateLuaKeys(propTable))
            {
                string propName = Convert.ToString(key);
                object value = propTable[key];
                string propValue;
                if (value is LuaTable valueTable)
                {
                    propValue = string.Join(",",
                        EnumerateLuaValues(valueTable)
                            .Select(v => Convert.ToString(v).Trim('\'')));
                }
                else
                {
                    propValue = Convert.ToString(value ?? "").Trim('\'');
                }

                builder.Append($"{propName}='{propValue}',");
            }

            return builder.ToString();
        }

        /// <summary>
        /// В main.io.lua subtype — числовой индекс; имя (V_DO1 и т.п.)
        /// только в комментарии Lua и после DoString недоступно.
        /// Восстанавливаем имя по типу из имени устройства и индексу.
        /// </summary>
        private static string ResolveSubtypeName(string deviceName,
            object subtypeValue)
        {
            string subtype = Convert.ToString(subtypeValue ?? "");
            if (string.IsNullOrEmpty(subtype))
                return string.Empty;

            if (!int.TryParse(subtype, out int subtypeIndex))
                return subtype;

            DeviceManager.GetInstance().CheckDeviceName(deviceName,
                out _, out _, out _, out _, out string typeStr, out _);
            if (!Enum.TryParse(typeStr, out DeviceType deviceType) ||
                deviceType == DeviceType.NONE)
                return string.Empty;

            foreach (var st in deviceType.SubTypes())
            {
                if (st.GetIndex() == subtypeIndex)
                    return st.ToString();
            }

            return string.Empty;
        }

        private static int ParseNodeNumber(string name, int fallback)
        {
            if (string.IsNullOrEmpty(name))
                return fallback * 100;

            if (ExtensionNodeNameRegex.IsMatch(name))
            {
                var match = ExtensionNodeNameRegex.Match(name);
                int parent = int.Parse(match.Groups["parent"].Value);
                int ext = int.Parse(match.Groups["ext"].Value);
                return parent * 100 + ext;
            }

            string digits = new string(name.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out int number)
                ? number
                : fallback * 100;
        }

        private static int ToInt(object value, int defaultValue)
        {
            if (value == null)
                return defaultValue;
            try
            {
                return Convert.ToInt32(Convert.ToDouble(value));
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
