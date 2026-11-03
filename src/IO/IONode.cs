using StaticHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;

namespace IO
{
    /// <summary>
    /// Узел модулей ввода-вывода IO.
    /// </summary>
    public class IONode : IIONode
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="typeStr">Тип.</param>
        /// <param name="n">Номер (также используется как адрес для COM-порта).
        /// </param>
        /// <param name="ip">IP-адрес.</param>
        /// <param name="name">Имя на схеме (А100 и др.).</param>
        public IONode(string typeStr, int n, int nodeNumber , string ip,
            string name, string location, string locationDescription)
        {
            this.typeStr = typeStr;
            var nodeinfo = IONodeInfo.GetNodeInfo(typeStr, out _);
            IsCoupler = nodeinfo.IsCoupler;
            type = nodeinfo.Type;
            
            AddressArea = IOAddressArea.GetModulesPerNodeAmount(type);

            this.ip = ip;
            this.n = n;
            this.nodeNumber = nodeNumber;
            this.name = name;
            this.location = location;
            LocationDescription = locationDescription;

            iOModules = new List<IIOModule>();
            extensionModules = new List<IIONode>();

            DI_count = 0;
            DO_count = 0;
            AI_count = 0;
            AO_count = 0;
        }

        /// <summary>
        /// Исключение нулевого значения AddressArea
        /// </summary>
        public class AddressAreaNullReferenceException : Exception
        {
            public AddressAreaNullReferenceException(string message) : base(message) { }
        }

        /// <summary>
        /// Исключение переполнения количества модулей в узле
        /// </summary>
        public class ModulesPerNodeOutOfRageException : Exception
        {
            public ModulesPerNodeOutOfRageException(string message) : base(message) { }
        }

        /// <summary>
        /// Исключение переполнения адресного пространства в узле
        /// </summary>
        public class AddressAreaOutOfRangeException : Exception
        {
            public AddressAreaOutOfRangeException(string message) : base(message) { }
        }

        /// <summary>
        /// Исключение переполнения адресного пространства в узле
        /// </summary>
        public class IndefiniteModulesException : Exception
        {
            public IndefiniteModulesException(string message) : base(message) { }
        }

        public void SetModule(IIOModule iOModule, int position)
        {
            if (AddressArea is null)
            {
                throw new AddressAreaNullReferenceException($"Ошибка доступа к диапазону адресного пространства узла:" +
                    $" {nameof(AddressArea)} is null;\n");
            }

            if (position > AddressArea.ModulesPerNodeMax + IOModules.Count(m => m.Info.IsSupportive))
            {
                throw new ModulesPerNodeOutOfRageException($"Модуль \"{iOModule.Name}\" " +
                    $"выходит за диапазон максимального количества модулей для узла \"{name}\". ");
            }

            if (currentAddressArea + iOModule.AddressArea > AddressArea.AddressAreaMax)
            {
                throw new AddressAreaOutOfRangeException($"Модуль \"{iOModule.Name}\" {iOModule.ArticleName} ({iOModule.AddressArea} byte) " +
                    $"выходит за диапазон адресного пространства узла \"{name}\" [{currentAddressArea}/{AddressArea.AddressAreaMax}]. ");
            }

            var initialCountModules = iOModules.Count;
            for (int i = initialCountModules; i < position; i++)
            {
                iOModules.Add(StubIOModule);
            }

            currentAddressArea += iOModule.AddressArea;

            iOModules[position - 1] = iOModule;

            if (position - initialCountModules is { } indefiniteCount and > 1)
            {
                throw new AddressAreaOutOfRangeException(
                    $"До модуля \"{iOModule.Name}\" {iOModule.ArticleName} не определено {indefiniteCount} модулей;\n");
            }
        }

        public void AddExtensionModule(IIONode extensionModule)
        {
            if (extensionModules.Any(module => module.Name == extensionModule.Name))
            {
                throw new InvalidOperationException($"Модуль расширения \"{extensionModule.Name}\" " +
                    $"для узла \"{name}\" определяется дважды.");
            }

            extensionModules.Add(extensionModule);
        }

        [ExcludeFromCodeCoverage]
        public virtual IIOModule StubIOModule => new IOModule(0, 0, IOModuleInfo.Stub);

        public IIOModule this[int idx]
        {
            get
            {
                if (idx >= iOModules.Count || idx < 0)
                {
                    return null;
                }
                else
                {
                    return iOModules[idx];
                }
            }
        }

        public string SaveAsLuaTable(string prefix)
        {
            bool saveDisabled = CanDisableNtype && !ntypeEnabled;
            int savedNtype = saveDisabled ? (int)TYPES.T_EMPTY : (int)type;
            string ntypeComment = saveDisabled ? "выключен" : typeStr;

            string str = prefix + "{\n";
            str += prefix + "name    = \'" + name + "\',\n";
            str += prefix + "ntype   = " + savedNtype + ", " + "--" + ntypeComment + "\n";
            str += prefix + "n       = " + n + ",\n";
            str += prefix + "IP      = \'" + ip + "\',\n";
            if (!string.IsNullOrEmpty(location))
            {
                str += prefix + "location = \'" +
                    location.Replace("'", "\\'") + "\',\n";
            }

            if (!string.IsNullOrEmpty(LocationDescription))
            {
                str += prefix + "location_description = \'" +
                    LocationDescription.Replace("'", "\\'") + "\',\n";
            }

            str += prefix + "modules =\n";
            str += prefix + "\t{\n";

            foreach (var module in iOModules)
            {
                if (module != null)
                {
                    str += module.SaveAsLuaTable(prefix + "\t") + ",\n";
                }
                else
                {
                    str += prefix + "\t" + "{}" + ",\n";
                }
            }

            str += prefix + "\t}\n";
            str += prefix + "}";

            return str;
        }

        public void SaveAsConnectionArray(ref object[,] res, ref int idx, 
            Dictionary<string, int> modulesCount,
            Dictionary<string, Color> modulesColor, int nodeIdx, 
            Dictionary<string, object[,]> asInterfaceConnection)
        {
            for (int i = 0; i < iOModules.Count; i++)
            {
                iOModules[i].SaveAsConnectionArray(ref res, ref idx, i + 1, modulesCount, modulesColor);
                if (iOModules[i].Info.AddressSpaceType == IOModuleInfo.ADDRESS_SPACE_TYPE.AOAI ||
                    iOModules[i].Info.AddressSpaceType == IOModuleInfo.ADDRESS_SPACE_TYPE.AOAIDODI)
                {
                    iOModules[i].SaveASInterfaceConnection(nodeIdx, i + 1, asInterfaceConnection);
                }
            }

        }

        /// <summary>
        /// Типы модулей.
        /// </summary>
        public enum TYPES
        {
            T_EMPTY = -1, /// Не определен.
            T_INTERNAL_750_86x, /// Модули в контроллере 750-863.
            T_INTERNAL_750_820x = 2, /// Модули в контроллере PFC200.
            T_ETHERNET = 100, /// Удаленный Ethernet узел.             
            T_PHOENIX_CONTACT = 200, /// Модули в контроллере Phoenix Contact.
            T_PHOENIX_CONTACT_1152 = 201, /// Контроллер Phoenix Contact AXC F 1152
            T_PHOENIX_CONTACT_2152 = 202, /// Контроллер Phoenix Contact AXC F 2152
            T_PHOENIX_CONTACT_3152 = 203, /// Контроллер Phoenix Contact AXC F 3152
            T_PXC_EXTENSION = 300, /// Модуль расширения
        };

        /// <summary>
        /// Максимальное количество доступных для
        /// подключения к узлу модулей
        /// </summary>
        public class IOAddressArea
        {
            /// <summary> Узлы Phoenix Contact </summary>
            public static readonly IOAddressArea PHOENIX_CONTACT = new IOAddressArea(63, 1482);
            /// <summary> Узлы Wago </summary>
            public static readonly IOAddressArea WAGO = new IOAddressArea(64, 0);

            protected IOAddressArea(int modulesPerNodeMax, int addressAreaMax)
            {
                ModulesPerNodeMax = modulesPerNodeMax;
                AddressAreaMax = addressAreaMax;
            }

            public static IOAddressArea GetModulesPerNodeAmount(TYPES type)
            {
                switch (type)
                {
                    case TYPES.T_INTERNAL_750_86x:
                    case TYPES.T_INTERNAL_750_820x:
                    case TYPES.T_ETHERNET:
                        return WAGO;
                    case TYPES.T_PHOENIX_CONTACT:
                    case TYPES.T_PHOENIX_CONTACT_1152:
                    case TYPES.T_PHOENIX_CONTACT_2152:
                    case TYPES.T_PHOENIX_CONTACT_3152:
                        return PHOENIX_CONTACT;
                    default: return null;
                }
            }

            public int ModulesPerNodeMax { get; }
            public int AddressAreaMax { get; }
        }

        public int DI_count { get; set; }

        public int DO_count { get; set; }

        public int AI_count { get; set; }

        public int AO_count { get; set; }

        public List<IIOModule> IOModules
        {
            get
            {
                return iOModules;
            }
        }

        public List<IIONode> ExtensionModules
        {
            get
            {
                return extensionModules;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public string IP
        {
            get
            {
                return ip;
            }
        }

        public TYPES Type
        {
            get
            {
                return type;
            }
        }

        /// <summary>
        /// Объем адрессного пространства
        /// </summary>
        public IOAddressArea AddressArea { get; private set; }

        public string TypeStr
        {
            get
            {
                return typeStr;
            }
        }

        public int N
        {
            get
            {
                return n;
            }
        }

        public int NodeNumber
        {
            get
            {
                return nodeNumber;
            }
        }

        public string Location
        {
            get
            {
                return location;
            }
        }
        
        public string LocationDescription { get; private set; }

        public bool IsCoupler { get; private set; } = false;

        public bool CanDisableNtype =>
            type != TYPES.T_EMPTY &&
            (IsCoupler || type == TYPES.T_PXC_EXTENSION);

        public bool NtypeEnabled
        {
            get => ntypeEnabled;
            set
            {
                if (!CanDisableNtype)
                    return;

                ntypeEnabled = value;
                IOManager.GetInstance().StoreNtypeEnabled(name, value);
            }
        }

        /// <summary>
        /// Установить функцию для узла
        /// </summary>
        /// <param name="function"></param>
        public void SetEplanFunction(IEplanFunction functin)
        {
            Function = functin;
        }

        public IEplanFunction Function { get; private set; }

        #region Закрытые поля.
        /// <summary>
        /// Модули узла.
        /// </summary>
        private List<IIOModule> iOModules;

        /// <summary>
        /// Модули расширения узла.
        /// </summary>
        private readonly List<IIONode> extensionModules;

        /// <summary>
        /// Тип узла (строка).
        /// </summary>
        private string typeStr;

        /// <summary>
        /// Тип узла.
        /// </summary>
        private TYPES type;

        /// <summary>
        /// IP-адрес.
        /// </summary>
        private string ip;

        /// <summary>
        /// Номер.
        /// </summary>
        private int n;

        /// <summary>
        /// Полный номер узла.
        /// </summary>
        private int nodeNumber;

        /// <summary>
        /// Имя узла (прим., А100).
        /// </summary>
        private string name;

        /// <summary>
        /// Местоположение узла (прим., +MCC1)
        /// </summary>
        private string location;

        /// <summary>
        /// Текущее занимаемое модулями адресное пространство
        /// </summary>
        private int currentAddressArea = 0;

        /// <summary>
        /// Сохранять настоящий ntype в main.io.lua.
        /// </summary>
        private bool ntypeEnabled = true;
        #endregion
    }
}
