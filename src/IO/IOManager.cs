using EasyEPlanner;
using LuaInterface;
using StaticHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace IO
{
    public class IOManager : IIOManager
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        private IOManager()
        {
            iONodes = new List<IIONode>();
            deletedModules = new List<IIOModule>();
            disabledNtypeNodeNames = new HashSet<string>();
            InitIoModulesInfo();
            InitIoNodesInfo();
        }

        /// <summary>
        /// Получение экземпляра класса.
        /// </summary>
        /// <returns>Единственный экземпляр класса.</returns>
        public static IOManager GetInstance()
        {
            if (null == instance)
            {
                instance = new IOManager();
            }

            return instance;
        }

        /// <summary>
        /// Получение модуля по номеру узла и смещение (поддержка считывания
        /// информации об устройствах из старого описания *.ds4). 
        /// </summary>        
        /// <param name="n">Номер (c единицы).</param>
        /// <param name="offset">Смещение.</param>
        /// <param name="addressSpaceType">Тип адресного пространства.</param>
        public IIOModule GetModuleByOffset(int n, int offset,
            IOModuleInfo.ADDRESS_SPACE_TYPE addressSpaceType)
        {
            IIOModule res = null;

            if (iONodes.Count >= n && n > 0)
            {
                int idx = 0;

                foreach (var module in IONodes[n - 1].IOModules)
                {
                    if (module.Info.AddressSpaceType == addressSpaceType)
                    {
                        int moduleOffset = 0;
                        switch (addressSpaceType)
                        {
                            case IOModuleInfo.ADDRESS_SPACE_TYPE.DI:
                            case IOModuleInfo.ADDRESS_SPACE_TYPE.AI:
                                moduleOffset = module.InOffset;
                                break;

                            case IOModuleInfo.ADDRESS_SPACE_TYPE.DO:
                            case IOModuleInfo.ADDRESS_SPACE_TYPE.AO:
                                moduleOffset = module.OutOffset;
                                break;
                        }

                        if (moduleOffset > offset)
                        {
                            break;
                        }
                        else
                        {
                            res = module;
                        }
                    }

                    idx++;
                }
            }

            return res;
        }

        /// <summary>
        /// Получить модуль ввода-вывода по его физическому номеру (прим., 202)
        /// </summary>
        /// <param name="number">Физический номер</param>
        /// <returns>Модуль ввода-вывода</returns>
        public IIOModule GetModuleByPhysicalNumber(int number)
        {
            foreach (var node in iONodes)
            {
                foreach (var module in node.IOModules)
                {
                    if (module.PhysicalNumber == number)
                    {
                        return module;
                    }
                }
            }

           const string Message = "Модуль не найден";
           throw new Exception(Message);
        }

        /// <summary>
        /// Добавление модуля в узел в заданную позицию.
        /// </summary>        
        /// <param name="n">Номер (c единицы).</param>
        /// <param name="type">Тип (например 750-352).</param>
        /// <param name="IP">IP-адрес.</param>
        [ExcludeFromCodeCoverage]
        public IONode AddNode(int n, int nodeNumber, string type, string IP,
            string name, string location, string locationDescription)
        {
            int voidNodeNumber = nodeNumber - 100;
            if (iONodes.Count < n)
            {
                for (int i = iONodes.Count; i < n; i++)
                {
                    if (voidNodeNumber == 0)
                        voidNodeNumber = 1;
                    iONodes.Add(new IONode("750-xxx", i + 1, voidNodeNumber, "", "", "", ""));
                    voidNodeNumber -= 100;
                }
            }

            CheckNodeIP(name, IP);

            var node = new IONode(type, n, nodeNumber, IP, name, location, locationDescription);
            ApplyStoredNtype(node);
            iONodes[n - 1] = node;

            return node;
        }

         /// <summary>
        /// Добавление модуля расширения в узел.
        /// </summary>
        /// <param name="nodeIdx">Индекс родительского узла.</param>
        /// <param name="extensionNodeInfo">Информация о модуле расширения.</param>
        [ExcludeFromCodeCoverage]
        public IONode AddExtensionNode(int nodeIdx,
            ExtensionNodeInfo extensionNodeInfo)
        {
            var parentNode = this[nodeIdx];
            if (parentNode is null)
            {
                return null;
            }

            CheckNodeIP(extensionNodeInfo.Name, extensionNodeInfo.IP);

            var extensionNode = new IONode(extensionNodeInfo.Type,
                extensionNodeInfo.ExtensionNumber,
                extensionNodeInfo.NodeNumber,
                extensionNodeInfo.IP,
                extensionNodeInfo.Name,
                extensionNodeInfo.Location,
                extensionNodeInfo.LocationDescription);
            ApplyStoredNtype(extensionNode);
            parentNode.AddExtensionModule(extensionNode);

            return extensionNode;
        }

        [ExcludeFromCodeCoverage]
        public class ExtensionNodeInfo
        {
            public int ExtensionNumber { get; set; }

            public int NodeNumber { get; set; }

            public string Type { get; set; }

            public string IP { get; set; }

            public string Name { get; set; }

            public string Location { get; set; }

            public string LocationDescription { get; set; }
        }

        [ExcludeFromCodeCoverage]
        private void CheckNodeIP(string name, string IP)
        {
            bool skipRangeCheck =
                ProjectContextHolder.Current is FileProjectContext;
            if (!skipRangeCheck &&
                ProjectConfiguration.GetInstance()?.
                BelongToRangesIP(IPConverter.ConvertIPStrToLong(IP)) == false)
            {
                Logs.AddMessage($"IP-адрес узла '{name}' ({IP}) выходит за диапазон ip-адресов проекта;");
            }

            if (IP != "" && GetNodesWithExtensions()
                .FirstOrDefault(node => node.IP == IP) is IONode nodeWithSameIp)
            {
                Logs.AddMessage($"IP-адрес узла '{name}' совпадает с адресом узла '{nodeWithSameIp.Name}': ({IP});");
            }
        }

        /// <summary>
        /// Получение узла.
        /// </summary>
        /// <param name="iONode">Индекс узла.</param>
        /// <returns>Узел с заданным индексом.</returns>
        public IIONode this[int idx]
        {
            get
            {
                if (idx >= iONodes.Count || idx < 0)
                {
                    return null;
                }
                else
                {
                    return iONodes[idx];
                }
            }
        }

        /// <summary>
        /// Сброс информации о модулях IO.
        /// </summary>
        public void Clear()
        {
            iONodes.Clear();
            deletedModules.Clear();
        }

        public void AddDeletedModule(IIOModule module)
        {
            deletedModules.Add(module);
        }

        /// <summary>
        /// Сохранение в виде таблицы Lua.
        /// </summary>
        /// <param name="prefix">Префикс (для выравнивания).</param>
        public string SaveAsLuaTable(string prefix)
        {
            string str = "--Узлы IO\n" +
                "nodes =\n" + "\t{\n";
            foreach (var node in iONodes)
            {
                if (node == null)
                {
                    continue;
                }

                str += node.SaveAsLuaTable("\t\t") + ",\n";
            }
            str += "\t}\n";

            str = str.Replace("\t", "    ");
            return str;
        }

        /// <summary>
        /// Применить ntype из main.io.lua: -1 выключает сохранение типа узла.
        /// </summary>
        public void ApplyNtypesFromLua(string lua)
        {
            if (string.IsNullOrEmpty(lua))
                return;

            var ntypesByName = new Dictionary<string, int>();
            foreach (Match match in NtypeFromLuaRegex.Matches(lua))
            {
                ntypesByName[match.Groups[1].Value] =
                    int.Parse(match.Groups[2].Value);
            }

            foreach (var node in GetNodesWithExtensions())
            {
                if (!node.CanDisableNtype)
                    continue;

                if (!ntypesByName.TryGetValue(node.Name, out int ntype))
                    continue;

                bool enabled = ntype != (int)IONode.TYPES.T_EMPTY;
                StoreNtypeEnabled(node.Name, enabled);
                node.NtypeEnabled = enabled;
            }
        }

        /// <summary>
        /// Запомнить, выключен ли ntype узла. Значение сохраняется при
        /// перечитывании IO из EPLAN.
        /// </summary>
        public void StoreNtypeEnabled(string name, bool enabled)
        {
            if (string.IsNullOrEmpty(name))
                return;

            if (enabled)
                disabledNtypeNodeNames.Remove(name);
            else
                disabledNtypeNodeNames.Add(name);
        }

        /// <summary>
        /// Сбросить запомненные выключения ntype (полная загрузка проекта).
        /// </summary>
        public void ResetStoredNtypes()
        {
            disabledNtypeNodeNames.Clear();
        }

        /// <summary>
        /// Восстановить выключение ntype на текущих узлах после ReadIO.
        /// </summary>
        public void ApplyStoredNtypes()
        {
            foreach (var node in GetNodesWithExtensions())
                ApplyStoredNtype(node);
        }

        private void ApplyStoredNtype(IIONode node)
        {
            if (node is null || !node.CanDisableNtype)
                return;

            node.NtypeEnabled = !disabledNtypeNodeNames.Contains(node.Name);
        }

        /// <summary>
        /// Проверка корректного заполнения узлами.
        /// </summary>
        public string Check()
        {
            var str = "";

            foreach (var node in iONodes)
            {
                if (node != null && node.Type == IONode.TYPES.T_EMPTY)
                {
                    str += "Отсутствует узел \"A" + node.NodeNumber + "\".\n";

                    str += CheckNodeIPEquality(node);

                    for (int i = 0; i < node.IOModules.Count; i++)
                    {
                        str += node.IOModules[i].Check(i, node.Name);
                    }
                }
            }

            str += CheckDisabledNtypes();

            long startingIP = ProjectConfiguration
                .GetInstance().StartingIPInterval;
            long endingIP = ProjectConfiguration.GetInstance()
                .EndingIPInterval;
            if (startingIP != 0 && endingIP != 0)
            {
                str += CheckIONodesIPRange(startingIP, endingIP);
            }

            return str;
        }

        /// <summary>
        /// Сообщения об узлах с выключенным ntype.
        /// </summary>
        private string CheckDisabledNtypes()
        {
            var messages = new StringBuilder();
            foreach (var node in GetNodesWithExtensions())
            {
                if (node.CanDisableNtype && !node.NtypeEnabled)
                    messages.Append($"Узел \"{node.Name}\" отключен;\n");
            }

            return messages.ToString();
        }

        /// <summary>
        /// Проверка совпадения IP-адресов в узлах ввода-вывода.
        /// </summary>
        /// <param name="node">Узел ввода вывода</param>
        /// <returns></returns>
        private string CheckNodeIPEquality(IIONode node)
        {
            string str = string.Empty;
            foreach (var node2 in GetNodesWithExtensions())
            {
                if (node == node2) continue;

                if (node.IP == node2.IP && node.IP != "")
                {
                    str += $"\"{node.Name}\": IP адрес совпадает с " +
                        $"\"{node2.Name}\" - {node.IP}.\n";
                }
            }

            return str;
        }

        /// <summary>
        /// Проверка попадания в диапазон IP-адресов узлов ввода-вывода
        /// </summary>
        /// <param name="endingIP">Конец интервала адресов</param>
        /// <param name="startingIP">Начало интервала адресов</param>
        /// <returns>Ошибки</returns>
        private string CheckIONodesIPRange(long startingIP, long endingIP)
        {
            string errors = "";
            var plcWithIP = GetNodesWithExtensions();
            foreach (var node in plcWithIP)
            {
                string IPstr = node.IP;
                if (IPstr == "")
                {
                    continue;
                }

                long nodeIP = StaticHelper.IPConverter
                    .ConvertIPStrToLong(IPstr);
                if (nodeIP - startingIP < 0 || endingIP - nodeIP < 0)
                {
                    errors += $"IP-адрес узла A{node.NodeNumber} " +
                        $"вышел за диапазон.\n";
                }
            }

            return errors;
        }

        private IEnumerable<IIONode> GetNodesWithExtensions()
        {
            foreach (var node in iONodes)
            {
                if (node is null)
                {
                    continue;
                }

                yield return node;

                foreach (var extensionModule in node.ExtensionModules)
                {
                    yield return extensionModule;
                }
            }
        }

        public void CalculateIOLinkAdresses()
        {
            foreach (var node in GetInstance().IONodes)
            {
                foreach (var module in node.IOModules)
                {
                    if (module.Info == null)
                    {
                        continue;
                    }

                    module.CalculateIOLinkAdresses();
                }
            }
        }

        /// <summary>
        /// Инициализировать модули информацию о модулях ввода-вывода.
        /// </summary>
        private void InitIoModulesInfo()
        {
            var lua = new LuaInterface.Lua();
            const string fileName = "sys_io_modules.lua";
            const string templateName = "sysIoModulesLuaFilePattern";
            string pathToFile = Path.Combine(
                ProjectManager.GetInstance().SystemFilesPath, fileName);

            if (!File.Exists(pathToFile))
                NoFileErrorShow(templateName, pathToFile);

            object[] result = lua.DoString(
                new StreamReader(pathToFile, EncodingDetector.DetectFileEncoding(pathToFile), true)
                    .ReadToEnd());

            if (result == null)
                return;

            var dataTables = result[0] as LuaInterface.LuaTable;
            foreach (var table in dataTables.Values)
            {
                var tableData = table as LuaInterface.LuaTable;

                int number = Convert.ToInt32((double)tableData["n"]);
                string name = (string)tableData["name"];
                string description = (string)tableData["description"];
                int addressSpaceTypeNumber = Convert.ToInt32(
                    (double)tableData["addressSpaceType"]);
                string typeName = (string)tableData["typeName"];
                string groupName = (string)tableData["groupName"];

                var channelClamps = new List<int>();
                var alternateChannalClamps = new List<List<int>>();
                var channelAddressesIn = new List<int>();
                var channelAddrOut = new List<int>();

                var channelClampsTable = tableData[
                    "channelClamps"] as LuaInterface.LuaTable;
                var channelAddressesInTable = tableData[
                    "channelAddressesIn"] as LuaInterface.LuaTable;
                var channelAddressesOutTable = tableData[
                    "channelAddressesOut"] as LuaInterface.LuaTable;
                foreach (object item in channelClampsTable.Values)
                {
                    var nums = item as LuaTable;
                    if (nums is null)
                    {
                        channelClamps.Add(Convert.ToInt32((double)item));
                        alternateChannalClamps.Add(new List<int>() { Convert.ToInt32((double)item) });
                        continue;
                    }
                    var list = new List<int>();
                    foreach (var num in nums.Values)
                    {
                        channelClamps.Add(Convert.ToInt32((double)num));
                        list.Add(Convert.ToInt32((double)num));
                    }
                    alternateChannalClamps.Add(list);
                }
                foreach (var num in channelAddressesInTable.Values)
                {
                    channelAddressesIn.Add(Convert.ToInt32((double)num));
                }
                foreach (var num in channelAddressesOutTable.Values)
                {
                    channelAddrOut.Add(Convert.ToInt32((double)num));
                }

                int DOcnt = Convert.ToInt32((double)tableData["DO_count"]);
                int DIcnt = Convert.ToInt32((double)tableData["DI_count"]);
                int AOcnt = Convert.ToInt32((double)tableData["AO_count"]);
                int AIcnt = Convert.ToInt32((double)tableData["AI_count"]);
                string color = (string)tableData["Color"];

                int LocalbusData = Convert.ToInt32((tableData["LocalbusData"] ?? 0));

                IOModuleInfo.AddModuleInfo(number, name, description,
                    addressSpaceTypeNumber, typeName, groupName,
                    channelClamps.ToArray(), alternateChannalClamps, channelAddressesIn.ToArray(),
                    channelAddrOut.ToArray(), DOcnt, DIcnt, AOcnt, AIcnt,
                    LocalbusData, color);
            }

        }

        [ExcludeFromCodeCoverage]
        private void NoFileErrorShow(string templateName, string pathToFile)
        {
            string template = EasyEPlanner.Properties.Resources
                   .ResourceManager.GetString(templateName);
            File.WriteAllText(pathToFile, template);
            MessageBox.Show("Файл с описанием модулей ввода-вывода" +
                " не найден. Будет создан пустой файл (без описания).",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void InitIoNodesInfo()
        {
            var lua = new LuaInterface.Lua();
            const string fileName = "sys_io_nodes.lua";
            const string templateName = "sysIoNodesLuaFilePattern";
            string pathToFile = Path.Combine(
                ProjectManager.GetInstance().SystemFilesPath, fileName);

            if (File.Exists(pathToFile))
            {
                object[] result = lua.DoFile(pathToFile);
                if (result == null)
                {
                    return;
                }

                var dataTables = result[0] as LuaInterface.LuaTable;
                foreach (var table in dataTables.Values)
                {
                    var tableData = table as LuaInterface.LuaTable;

                    string name = (string)tableData["name"];
                    int enumNumber = Convert.ToInt32((double)tableData["type"]);
                    IONode.TYPES type = (IONode.TYPES)enumNumber;
                    bool isCoupler = (bool)tableData["isCoupler"];

                    IONodeInfo.AddNodeInfo(name, type, isCoupler);
                }
            }
            else
            {
                string template = EasyEPlanner.Properties.Resources
                    .ResourceManager.GetString(templateName);
                File.WriteAllText(pathToFile, template);
                MessageBox.Show("Файл с описанием узлов ввода-вывода" +
                    " не найден. Будет создан пустой файл (без описания).",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public List<IIONode> IONodes
        {
            get
            {
                return iONodes;
            }
        }

        public List<IIOModule> DeletedModules
        {
            get
            {
                return deletedModules;
            }
        }

        /// <summary>
        /// Номера IO-Link модулей, которые используются
        /// </summary>
        public enum IOLinkModules
        {
            Wago = 657,
            PhoenixContactSmart = 1088132,
            PhoenixContactStandard = 1027843,
        }

        /// <summary>
        /// Шаблон для разбора имени узла, модуля ввода-вывода (прим., А100).
        /// </summary>
        public const string IONamePattern = @"=*-A(?<n>\d+)(\.(?<ext>\d+))?$";

        private static readonly Regex NtypeFromLuaRegex = new Regex(
            @"name\s*=\s*'([^']+)'\s*,\s*ntype\s*=\s*(-?\d+)",
            RegexOptions.None, RegexDefaults.Timeout);

        #region Закрытые поля.
        private readonly List<IIONode> iONodes;     ///Узлы проекта.
        private readonly List<IIOModule> deletedModules; ///Исключенные модули проекта.
        private readonly HashSet<string> disabledNtypeNodeNames;
        private static IOManager instance;  ///Экземпляр класса.
        #endregion
    }
}
