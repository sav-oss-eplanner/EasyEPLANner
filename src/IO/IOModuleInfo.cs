using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IO
{
    /// <summary>
    /// Описание модуля ввода-вывода IO.
    /// </summary>
    public interface IIOModuleInfo
    {
        /// <summary>
        /// Тип адресного пространства модуля ввода-вывода IO.
        /// </summary>
        IOModuleInfo.ADDRESS_SPACE_TYPE AddressSpaceType { get; set; }

        /// <summary>
        /// Количество аналоговых входов. 
        /// </summary>
        int AICount { get; set; }

        /// <summary>
        /// задает клеммы, к которым не возможна одноверменная привязка
        /// </summary>
        List<List<int>> AlternateChannelsClamps { get; set; }

        /// <summary>
        /// Количество аналоговых выходов. 
        /// </summary>
        int AOCount { get; set; }

        /// <summary>
        /// Адреса каналов ввода.
        /// </summary>
        int[] ChannelAddressesIn { get; set; }

        /// <summary>
        /// Адреса каналов вывода.
        /// </summary>
        int[] ChannelAddressesOut { get; set; }

        /// <summary>
        /// Клеммы каналов ввода-вывода.
        /// </summary>
        int[] ChannelClamps { get; set; }

        /// <summary>
        /// Описание модуля ввода-вывода IO.
        /// </summary> 
        string Description { get; set; }

        /// <summary>
        /// Количество дискретных входов. 
        /// </summary>
        int DICount { get; set; }

        /// <summary>
        /// Количество дискретных выходов. 
        /// </summary>
        int DOCount { get; set; }

        /// <summary>
        /// Имя серии модуля ввода-вывода IO (например 750-800).
        /// </summary>    
        string GroupName { get; set; }

        /// <summary>
        /// Адресное пространство, занимаемое модулем.
        /// </summary>
        int LocalbusData { get; set; }

        /// <summary>
        /// Физический цвет модуля
        /// </summary>
        Color ModuleColor { get; set; }

        /// <summary>
        /// Имя модуля ввода-вывода IO (серия-номер, например: 750-860).
        /// </summary>  
        string Name { get; set; }

        /// <summary>
        /// Номер модуля ввода-вывода IO (например: 860).
        /// </summary>  
        int Number { get; set; }

        /// <summary>
        /// Имя типа (дискретный выход, аналоговый выход, ...).
        /// </summary>
        string TypeName { get; set; }

        /// <summary>
        /// Вспомогательный модуль 
        /// (не имеет адресного пространства)
        /// </summary>
        bool IsSupportive { get; }

        /// <summary>
        /// Копия
        /// </summary>
        object Clone(); 
    }


    /// <summary>
    /// Описание модуля ввода-вывода IO.
    /// </summary>
    public class IOModuleInfo : ICloneable, IIOModuleInfo
    {
        /// <summary>
        /// Добавить информацию о модуле ввода вывода
        /// </summary>
        /// <param name="number">Номер модуля ввода-вывода IO </param>
        /// <param name="name">Имя модуля ввода-вывода IO</param>
        /// <param name="description">Описание модуля ввода-вывода IO</param>
        /// <param name="addressSpaceTypeNum">Тип адресного пространства</param>
        /// <param name="typeName">Имя типа (дискретный выход и др.)</param>
        /// <param name="groupName">Имя серии (прим., 750-800)</param>
        /// <param name="channelClamps">Клеммы каналов ввода-вывода</param>
        /// <param name="channelAddressesIn">Адреса каналов ввода</param>
        /// <param name="channelAddressesOut">Адреса каналов вывода</param>
        /// <param name="DOCount">Количество дискретных выходов</param>
        /// <param name="DICount">Количество дискретных входов</param>
        /// <param name="AOCount">Количество аналоговых выходов</param>
        /// <param name="AICount">Количество аналоговых входов</param>
        /// <param name="colorAsString">Физический цвет модуля</param>
        public static void AddModuleInfo(int number, string name,
            string description, int addressSpaceTypeNum, string typeName,
            string groupName, int[] channelClamps, List<List<int>> alternateChannelsClamps, int[] channelAddressesIn,
            int[] channelAddressesOut, int DOCount, int DICount,
            int AOCount, int AICount, int localbusData, string colorAsString)
        {
            var addressSpaceType = (ADDRESS_SPACE_TYPE)addressSpaceTypeNum;
            Color color = Color.FromName(colorAsString);

            var moduleInfo = new IOModuleInfo(number, name, description,
                 addressSpaceType, typeName, groupName, channelClamps,
                 channelAddressesIn, channelAddressesOut, DOCount, DICount,
                 AOCount, AICount, localbusData, color)
            {
                AlternateChannelsClamps = alternateChannelsClamps,
            };

            if (modules.Where(x => x.Name == moduleInfo.Name).Count() == 0)
            {
                modules.Add(moduleInfo);
            }
        }

        /// <summary>
        /// Получение описания модуля ввода-вывода IO на основе его имени.
        /// </summary>
        /// <param name="name">Имя модуля (750-860).</param>
        /// <param name="isStub">Признак не идентифицированного модуля.</param>
        public static IOModuleInfo GetModuleInfo(string name, out bool isStub)
        {
            isStub = false;

            IOModuleInfo res = modules.Find(x => x.Name == name);
            if (res != null)
            {
                return res.Clone() as IOModuleInfo;
            }

            isStub = true;
            return Stub;
        }

        /// <summary>
        /// Описание модуля по номеру (как в main.io.lua).
        /// </summary>
        public static IOModuleInfo GetModuleInfoByNumber(int number)
        {
            IOModuleInfo res = modules.Find(x => x.Number == number);
            if (res != null)
            {
                return res.Clone() as IOModuleInfo;
            }

            return Stub;
        }

        /// <summary>
        /// Закрытый конструктор. Используется для создания списка применяемых
        /// модулей.
        /// </summary>
        /// <param name="n">Номер модуля ввода-вывода IO </param>
        /// <param name="name">Имя модуля ввода-вывода IO</param>
        /// <param name="description">Описание модуля ввода-вывода IO</param>
        /// <param name="addressSpaceType">Тип адресного пространства</param>
        /// <param name="typeName">Имя типа (дискретный выход и др.)</param>
        /// <param name="groupName">Имя серии (прим., 750-800)</param>
        /// <param name="channelClamps">Клеммы каналов ввода-вывода</param>
        /// <param name="channelAddressesIn">Адреса каналов ввода</param>
        /// <param name="channelAddressesOut">Адреса каналов вывода</param>
        /// <param name="DOCount">Количество дискретных выходов</param>
        /// <param name="DICount">Количество дискретных входов</param>
        /// <param name="AOCount">Количество аналоговых выходов</param>
        /// <param name="AICount">Количество аналоговых входов</param>
        /// <param name="color">Физический цвет модуля</param>
        private IOModuleInfo(int n, string name, string description,
            ADDRESS_SPACE_TYPE addressSpaceType, string typeName,
            string groupName, int[] channelClamps, int[] channelAddressesIn,
            int[] channelAddressesOut, int DOCount, int DICount, int AOCount,
            int AICount, int localbusData, Color color)
        {
            Number = n;
            Name = name;
            Description = description;

            AddressSpaceType = addressSpaceType;
            TypeName = typeName;
            GroupName = groupName;

            ChannelClamps = channelClamps;
            ChannelAddressesIn = channelAddressesIn;
            ChannelAddressesOut = channelAddressesOut;

            this.DOCount = DOCount;
            this.DICount = DICount;
            this.AOCount = AOCount;
            this.AICount = AICount;

            LocalbusData = localbusData;

            if (LocalbusData == 0)
                LocalbusData = CalculateLocalbusData();

            ModuleColor = color;
        }

        private int CalculateLocalbusData()
        {
            if (Name?.StartsWith("AXL") ?? false) // Phoenix Contact
                return AXLLocalbusOffset + Math.Max(
                    Math.Max(DOCount / AXLDIDOCountDivider, DICount / AXLDIDOCountDivider),
                    Math.Max(AOCount * AXLAIAOCountCoefficient, AICount * AXLAIAOCountCoefficient));

            return 0;
        }

        public object Clone()
        {
            var channelClamps = ChannelClamps.Clone() as int[];
            var channelAddressesIn = ChannelAddressesIn.Clone() as int[];
            var channelAddressesOut = ChannelAddressesOut.Clone() as int[];
            var alternateChannelClamps = new List<List<int>>(AlternateChannelsClamps);

            return new IOModuleInfo(Number, Name, Description,
                AddressSpaceType, TypeName, GroupName, channelClamps,
                channelAddressesIn, channelAddressesOut, DOCount, DICount,
                AOCount, AICount, LocalbusData, ModuleColor)
            {
                AlternateChannelsClamps = alternateChannelClamps,
            };
        }
      
        public string Name { get; set; }

        public int Number { get; set; }
 
        public string Description { get; set; }

        public ADDRESS_SPACE_TYPE AddressSpaceType { get; set; }

        public int[] ChannelClamps { get; set; }

        public List<List<int>> AlternateChannelsClamps { get; set; }

        public int[] ChannelAddressesIn { get; set; }

        public int[] ChannelAddressesOut { get; set; }

        public int DOCount { get; set; }

        public int DICount { get; set; }

        public int AOCount { get; set; }

        public int AICount { get; set; }

        public int LocalbusData { get; set; }

        public string TypeName { get; set; }

        public Color ModuleColor { get; set; }
    
        public string GroupName { get; set; }

        /// <summary>
        /// Количество модулей ввода-вывода.
        /// </summary>
        public static int Count => modules.Count;

        /// <summary>
        /// Модули ввода-вывода.
        /// </summary>
        public static List<IOModuleInfo> Modules => modules;

        public bool IsSupportive => 
            AOCount == 0 && AICount == 0 &&
            DOCount == 0 && DICount == 0;

        /// <summary>
        /// Тип адресного пространства модуля
        /// </summary>
        public enum ADDRESS_SPACE_TYPE
        {
            NONE,
            DO,
            DI,
            AO,
            AI,
            AOAI,
            DODI,
            AOAIDODI,
        };

        /// <summary>
        /// Список модулей ввода-вывода.
        /// </summary>
        private static List<IOModuleInfo> modules = new List<IOModuleInfo>();

        /// <summary>
        /// Заглушка, для возврата в случае поиска неописанных модулей. 
        /// </summary>
        public static IOModuleInfo Stub = new(
            0, "не определен", "Модуль не определен", ADDRESS_SPACE_TYPE.NONE,
            "", "", [], [], [], 0, 0, 0, 0, 0, Color.Empty);

        /// <summary>
        /// Изначальное адрессное пространство, занимаемое любым модулем AXL
        /// </summary>
        private static readonly int AXLLocalbusOffset = 2;

        /// <summary>
        /// Делитель для DO/DI_Count при расчете адресного пространства 
        /// PhoenixContact
        /// </summary>
        private static readonly int AXLDIDOCountDivider = 8;

        /// <summary>
        /// Множитель для AI/AO_Count при расчете адресного пространства
        /// Phoenix Contact
        /// </summary>
        private static readonly int AXLAIAOCountCoefficient = 2;
    }
}