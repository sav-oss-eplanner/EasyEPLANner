using IO;

namespace EplanDevice
{
    public partial class IODevice
    {
        /// <summary>
        /// Канал ввода-вывода.
        /// </summary>
        public class IOChannel : IIOChannel
        {
            public static int Compare(IOChannel wx, IOChannel wy)
            {
                if (wx == null && wy == null)
                    return 0;

                if (wx == null)
                    return -1;

                if (wy == null)
                    return 1;

                return wx.ToInt().CompareTo(wy.ToInt());
            }

            /// <param name="node">Номер узла.</param>
            /// <param name="module">Номер модуля.</param>
            /// <param name="physicalClamp">Физический номер клеммы.</param>
            /// <param name="fullModule">Полный номер модуля (101).</param>
            /// <param name="logicalClamp">Порядковый логический номер клеммы.</param>
            /// <param name="moduleOffset">Сдвиг модуля к которому привязан канал.</param>
            public void SetChannel(int node, int module, int physicalClamp, int fullModule,
                int logicalClamp, int moduleOffset)
            {
                this.node = node;
                this.module = module;
                this.physicalClamp = physicalClamp;

                this.fullModule = fullModule;
                this.logicalClamp = logicalClamp;
                this.moduleOffset = moduleOffset;
            }

            /// <summary>
            /// Сброс привязки канала ввода-вывода.
            /// </summary>
            public void Clear()
            {
                node = -1;
                module = -1;
                physicalClamp = -1;
                fullModule = -1;
                logicalClamp = -1;
                moduleOffset = -1;
            }

            /// <param name="name">Имя канала (DO, DI, AO, AI).</param>
            /// <param name="node">Номер узла.</param>
            /// <param name="module">Номер модуля.</param>
            /// <param name="clamp">Номер клеммы.</param>
            /// <param name="comment">Комментарий к каналу.</param>
            public IOChannel(string name, int node, int module, int clamp, string comment)
            {
                this.name = name;

                this.node = node;
                this.module = module;
                this.physicalClamp = clamp;
                this.comment = comment;
            }

            private int ToInt()
            {
                switch (name)
                {
                    case DO:
                        return 0;

                    case DI:
                        return 1;

                    case AI:
                        return 2;

                    case AO:
                        return 3;

                    case AIAO:
                        return 4;

                    case DODI:
                        return 5;

                    default:
                        return 6;
                }
            }

            /// <summary>
            /// Сохранение в виде таблицы Lua.
            /// Строка сохраняется всегда, даже если канал не привязан
            /// (пустой, "заглушка") — это сохраняет позиционный порядок
            /// каналов устройства, по которому они сопоставляются при
            /// последующей загрузке (см. LuaMainIoLoader.LoadDeviceChannels).
            /// </summary>
            /// <param name="prefix">Префикс (для выравнивания).</param>
            public string SaveAsLuaTable(string prefix)
            {
                bool isBound = physicalClamp >= 0 &&
                    IOManager.GetInstance()[node] != null &&
                    IOManager.GetInstance()[node][module - 1] != null;

                int offset = -1;
                int outNode = -1;
                int outPhysicalClamp = -1;
                int outLogicalClamp = -1;
                int outModuleOffset = -1;

                if (isBound)
                {
                    outNode = node;
                    outPhysicalClamp = physicalClamp;
                    outLogicalClamp = logicalClamp;
                    outModuleOffset = moduleOffset;

                    switch (name)
                    {
                        case DO:
                            offset = CalculateDO();
                            break;

                        case AO:
                            offset = CalculateAO();
                            break;

                        case DI:
                            offset = CalculateDI();
                            break;

                        case AI:
                            offset = CalculateAI();
                            break;

                        default:
                            offset = -1;
                            break;
                    }
                }

                string res = prefix + "{\n";

                // Комментарий сохраняется только как читаемый Lua-комментарий
                // (поле comment не пишется — сопоставление каналов при
                // загрузке идёт по позиции, а не по комментарию).
                if (comment != string.Empty)
                {
                    res += prefix + "-- " + comment + "\n";
                }

                res += prefix + $"node          = {outNode},\n";
                res += prefix + $"offset        = {offset},\n";
                res += prefix + $"physical_port = {outPhysicalClamp},\n";
                res += prefix + $"logical_port  = {outLogicalClamp},\n";
                res += prefix + $"module_offset = {outModuleOffset}\n";
                res += prefix + "},\n";

                return res;
            }

            /// <summary>
            /// Расчет AI адреса для сохранения в файл
            /// </summary>
            /// <returns>Адрес</returns>
            private int CalculateAI()
            {
                int offset = -1;

                if (physicalClamp <= IOManager.GetInstance()[node][module - 1]
                    .Info.ChannelAddressesIn.Length)
                {
                    IIOModule md = IOManager.GetInstance()[node][module - 1];
                    offset = md.InOffset;
                    offset += md.Info.ChannelAddressesIn[physicalClamp];

                    return offset;
                }

                return offset;
            }

            /// <summary>
            /// Расчет AO адреса для сохранения в файл
            /// </summary>
            /// <returns>Адрес</returns>
            private int CalculateAO()
            {
                int offset = -1;

                if (physicalClamp <= IOManager.GetInstance()[node][module - 1]
                    .Info.ChannelAddressesOut.Length)
                {
                    IIOModule md = IOManager.GetInstance()[node][module - 1];
                    offset = md.OutOffset;
                    offset += md.Info.ChannelAddressesOut[physicalClamp];

                    return offset;
                }

                return offset;
            }

            /// <summary>
            /// Расчет DI адреса для сохранения в файл
            /// </summary>
            /// <returns>Адрес</returns>
            private int CalculateDI()
            {
                int offset = -1;

                if (physicalClamp <= IOManager.GetInstance()[node][module - 1]
                    .Info.ChannelAddressesIn.Length)
                {
                    IIOModule md = IOManager.GetInstance()[node][module - 1];
                    if (md.IsIOLink() == true)
                    {
                        offset = 0;
                    }
                    else
                    {
                        offset = md.InOffset;
                    }
                    offset += md.Info.ChannelAddressesIn[physicalClamp];

                    return offset;
                }

                return offset;
            }

            /// <summary>
            /// Расчет DO адреса для сохранения в файл
            /// </summary>
            /// <returns>Адрес</returns>
            private int CalculateDO()
            {
                int offset = -1;

                if (physicalClamp <= IOManager.GetInstance()[node][module - 1]
                    .Info.ChannelAddressesOut.Length)
                {
                    IIOModule md = IOManager.GetInstance()[node][module - 1];
                    if (md.IsIOLink() == true)
                    {
                        offset = 0;
                    }
                    else
                    {
                        offset = md.OutOffset;
                    }
                    offset += md.Info.ChannelAddressesOut[physicalClamp];

                    return offset;
                }

                return offset;
            }

            public bool IsEmpty()
            {
                return node == -1;
            }

            /// <summary>
            /// Номер узла.
            /// </summary>
            public int Node
            {
                get
                {
                    return node;
                }
            }

            /// <summary>
            /// Номер модуля.
            /// </summary>
            public int Module
            {
                get
                {
                    return module;
                }
            }

            /// <summary>
            /// Физический номер клеммы.
            /// </summary>
            public int PhysicalClamp
            {
                get
                {
                    return physicalClamp;
                }
            }

            public int FullModule
            {
                get
                {
                    return fullModule;
                }
            }

            public string Comment
            {
                get
                {
                    return comment;
                }
            }

            public string Name
            {
                get
                {
                    return name;
                }
            }

            public int LogicalClamp
            {
                get
                {
                    return logicalClamp;
                }
            }

            
            public int ModuleOffset
            {
                get
                {
                    return moduleOffset;
                }
            }

            /// <summary>
            /// Шаблон для разбора комментария к устройству.
            /// </summary>
            public const string ChannelCommentPattern =
                @"(Открыть мини(?n:\s+|$))|" +
                @"(Открыть НС(?n:\s+|$))|" +
                @"(Открыть ВС(?n:\s+|$))|" +
                @"(Открыть(?n:\s+|$))|" +
                @"(Закрыть(?n:\s+|$))|" +
                @"(Открыт(?n:\s+|$))|" +
                @"(Закрыт(?n:\s+|$))|" +
                @"(Объем(?n:\s+|$))|" +
                @"(Поток(?n:\s+|$))|" +
                @"(Пуск(?n:\s+|$))|" +
                @"(Реверс(?n:\s+|$))|" +
                @"(Обратная связь(?n:\s+|$))|" +
                @"(Частота вращения(?n:\s+|$))|" +
                @"(Авария(?n:\s+|$))|" +
                @"(Напряжение моста\(\+Ud\)(?n:\s+|$))|" +
                @"(Референсное напряжение\(\+Uref\)(?n:\s+|$))|" +
                @"(Красный цвет(?n:\s+|$))|" +
                @"(Желтый цвет(?n:\s+|$))|" +
                @"(Зеленый цвет(?n:\s+|$))|" +
                @"(Звуковая сигнализация(?n:\s+|$))|" +
                @"(Готовность(?n:\s+|$))|" +
                @"(Сигнал активации(?n:\s+|$))|" +
                @"(Результат обработки(?n:\s+\d*|$))";

            public const string AI = "AI";
            public const string AO = "AO";
            public const string DI = "DI";
            public const string DO = "DO";
            const string AIAO = "AIAO";
            const string DODI = "DODI";

            #region Закрытые поля
            private int node;            ///Номер узла.
            private int module;          ///Номер модуля.
            private int fullModule;      ///Полный номер модуля.
            private int physicalClamp;   ///Физический номер клеммы.
            private string comment;      ///Комментарий.
            private string name;         ///Имя канала (DO, DI, AO ,AI).
            private int logicalClamp;    ///Логический номер клеммы.
            private int moduleOffset;    ///Сдвиг начала модуля.
            #endregion
        }
    }
}
