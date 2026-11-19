using EasyEPlanner;
using EasyEPlanner.FileSavers.XML;
using IO;
using StaticHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;

namespace EplanDevice
{
    /// <summary>
    /// Технологическое устройство, подключенное к модулям ввода\вывод IO.
    /// </summary>
    public partial class IODevice : Device, IIODevice
    {
        /// <summary>
        /// Лимит названия устройства в символах
        /// </summary>
        public const int DeviceNameLimit = 30;

        /// <summary>
        /// Получение строкового представления подтипа устройства.
        /// </summary>
        public virtual string GetDeviceSubTypeStr(DeviceType dt,
            DeviceSubType dst)
        {
            switch(dt)
            {
                case DeviceType.NONE:
                    return dt.ToString();
            }
            return "";
        }

        /// <summary>
        /// Получение свойств устройства.
        /// String - название тэга
        /// Int - количество повторений (размерность). Дефолт - 1.
        /// </summary>
        /// <param name="dst">Подтип устройства</param>
        /// <param name="dt">Тип устройства</param>
        public virtual Dictionary<ITag, int> GetDeviceProperties(
            DeviceType dt, DeviceSubType dst)
        {
            switch (dt)
            {
                case DeviceType.NONE:
                    return null; 
            }
            return null;
        }

        public virtual string GetRange(Parameter p_min, Parameter p_max)
        {
            if (parameters.TryGetValue(p_min, out var min_value) &&
                parameters.TryGetValue(p_max, out var max_value) &&
                min_value is not null &&
                max_value is not null)
            {
                return $"_{min_value}..{max_value}";

            }

            return string.Empty;
        }

        /// <summary>
        /// Генерация тэгов устройства.
        /// </summary>
        /// <param name="rootNode">Корневой узел</param>
        public virtual void GenerateDeviceTags(IDriver root)
        {
            if (GetDeviceProperties(DeviceType, DeviceSubType) is not { } propertiesList)
                return;

            foreach (var tagPair in propertiesList)
            {
                string propName = tagPair.Key.Name;
                string propDescription = tagPair.Key.Description;
                int theSameTagsCount = tagPair.Value;

                root.AddChannel($"{DeviceType}_{propName}",
                    $"{Name}.{propName}", $"{propDescription}", theSameTagsCount);
            }
        }

        /// <summary>
        /// Конструктор на основе имени.
        /// </summary>
        /// <param name="name">Имя устройства (формат - А1V12).</param>
        /// <param name="eplanName">Имя устройства в Eplan (+A1-V12)</param>
        /// <param name="description">Описание устройства.</param>
        /// <param name="deviceType">Тип устройства (V для А1V12).</param>
        /// <param name="deviceNumber">Номер устройства (12 для А1V12).</param>
        /// <param name="objectName">Объект устройства (А для А1V12).</param>
        /// <param name="objectNumber">Номер объекта устройства (1 для А1V12).
        /// </param>
        protected internal IODevice(string name, string eplanName,
            string description, string deviceType, int deviceNumber,
            string objectName, int objectNumber) : this(name, eplanName,
                description, deviceNumber, objectName, objectNumber)
        {
            try
            {
                dType = (DeviceType)Enum.Parse(typeof(DeviceType), deviceType, true);
            }
            catch (Exception)
            {
                dType = DeviceType.NONE;
            }
        }

        /// <summary>
        /// Конструктор на основе имени.
        /// </summary>
        /// <param name="name">Имя устройства (формат - А1V12).</param>
        /// <param name="eplanName">Имя устройства в Eplan (+A1-V12).</param>
        /// <param name="description">Описание устройства.</param>
        /// <param name="deviceNumber">Номер устройства (12 для А1V12).</param>
        /// <param name="objectName">Объект устройства (А для А1V12).</param>
        /// <param name="objectNumber">Номер объекта устройства (1 для А1V12).
        /// </param>
        protected internal IODevice(string name, string eplanName,
            string description, int deviceNumber, string objectName,
            int objectNumber) : base(name, eplanName, description,
                deviceNumber, objectName, objectNumber)
        {
            DO = new List<IOChannel>();
            DI = new List<IOChannel>();
            AO = new List<IOChannel>();
            AI = new List<IOChannel>();

            parameters = new Dictionary<Parameter, object>();
            rtParameters = new Dictionary<string, object>();
            properties = new Dictionary<string, object>();
            iolConfProperties = new Dictionary<string, double>();

            iolinkProperties = new IOLinkSize();
        }

        /// <summary>
        /// Установить размеры IO-Link областей устройства.
        /// </summary>
        /// <param name="articleName">Изделие устройства</param>
        public void SetIOLinkSizes(string articleName)
        {
            var articles = DeviceManager.GetInstance().IOLinkSizes;
            if (articles.ContainsKey(articleName) == false)
            {
                Logs.AddMessage($"Неизвестный артикул '{articleName}' IO-Link устройства {Name};\n");
                return;
            }

            var sizes = articles[articleName];
            iolinkProperties.SizeIn = sizes.SizeIn;
            iolinkProperties.SizeOut = sizes.SizeOut;
            iolinkProperties.SizeInFromFile = sizes.SizeInFromFile;
            iolinkProperties.SizeOutFromFile = sizes.SizeOutFromFile;
        }

        /// <summary>
        /// Установка номера узла, в котором подключается устройство.
        /// </summary>
        public void SetLocation(int devLocation)
        {
            dLocation = devLocation;
        }

        /// <summary>
        /// Установка параметра.
        /// </summary>
        public string SetParameter(string name, double value)
        {
            string res = "";

            object val = null;
            if (parameters.TryGetValue(name, out val))
            {
                parameters[name] = value;
            }
            else
            {
                res = string.Format("\"{0}\" - параметр не найден\n", name);
            }

            return res;
        }

        /// <summary>
        /// Установка параметров для устройства на схеме
        /// </summary>
        /// <returns></returns>
        public void UpdateParameters()
        {
            if (Function is null)
                return;

            var parametersList = new List<string>();
            foreach (var parameter in parameters)
            {
                if (parameter.Value != null)
                    parametersList.Add(
                        $"{parameter.Key.Name}={FormatSchemeValue(parameter.Value)}");
            }

            Function.Parameters = string.Join(", ", parametersList);
        }

        /// <summary>
        /// Установка свойств для устройства на схеме
        /// </summary>
        /// <returns></returns>
        public void UpdateProperties()
        {
            if (Function is null)
                return;

            var propertiesList = new List<string>();
            foreach (var property in properties)
            {
                propertiesList.Add($"{property.Key}='{property.Value}'");
            }

            Function.Properties = string.Join(",", propertiesList);
        }

        /// <summary>
        /// Установка параметров времени выполнения на ФСА
        /// </summary>
        public void UpdateRuntimeParameters()
        {
            if (Function is null)
                return;

            var runtimeParametersList = new List<string>();
            foreach (var rtPar in rtParameters)
            {
                runtimeParametersList.Add(
                    $"{rtPar.Key}={FormatSchemeValue(rtPar.Value)}");
            }

            Function.RuntimeParameters = string.Join(",", runtimeParametersList);
        }

        private static string FormatSchemeValue(object value)
        {
            if (value is IFormattable formattable)
                return formattable.ToString(null,
                    System.Globalization.CultureInfo.InvariantCulture);

            return value?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Установка рабочего параметра.
        /// </summary>
        public string SetRuntimeParameter(string name, double value)
        {
            string res = "";

            object val = null;
            if (rtParameters.TryGetValue(name, out val))
            {
                rtParameters[name] = value;
            }
            else
            {
                res = string.Format("\"{0}\" - рабочий параметр не найден\n", name);
            }

            return res;
        }

        /// <summary>
        /// Получение рабочего параметра.
        /// </summary>
        /// <param name="name">Имя устройства</param>
        /// <returns></returns>
        public string GetRuntimeParameter(string name)
        {
            object value = default;

            if (rtParameters.TryGetValue(name, out value))
            {
                return value.ToString();
            }
            else
            {
                return "";
            }
        }


        public bool ClearChannel(
            IOModuleInfo.ADDRESS_SPACE_TYPE addressSpace,
            string comment, string channelName)
        {
            List<IOChannel> findedChannels = GetChannels(addressSpace,
                channelName, comment);

            if (findedChannels.Count > 0)
            {
                foreach (IOChannel channel in findedChannels)
                {
                    channel.Clear();
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Установка канала ввода\вывода.
        /// </summary>
        /// <param name="addressSpace">Тип адресного пространства канала.
        /// </param>
        /// <param name="node">Номер узла.</param>
        /// <param name="module">Номер модуля.</param>
        /// <param name="physicalKlemme">Номер клеммы.</param>
        /// <param name="comment">Комментарий к каналу.</param>
        /// <param name="error">Строка с описанием ошибки при возникновении 
        /// таковой.</param>
        public bool SetChannel(IOModuleInfo.ADDRESS_SPACE_TYPE addressSpace,
            int node, int module, int physicalKlemme, string comment,
            out string error, int fullModule, int logicalPort,
            int moduleOffset, string channelName)
        {
            error = "";

            List<IOChannel> findedChannels = GetChannels(addressSpace,
                channelName, comment);

            if (findedChannels.Count > 0)
            {
                foreach (IOChannel channel in findedChannels)
                {
                    if (!channel.IsEmpty())
                    {
                        error = string.Format(
                            "\"{0}\" : канал {1}.\"{2}\" уже привязан " +
                            "к A{3}.{4} \"{5}\".",
                            name, addressSpace, comment,
                            100 * (channel.Node + 1) + channel.Module,
                            channel.PhysicalClamp, channel.Comment);
                        return false;
                    }

                    channel.SetChannel(node, module, physicalKlemme,
                        fullModule, logicalPort, moduleOffset);

                    List<IIONode> nodes = IOManager.GetInstance().IONodes;
                    if (nodes.Count > node &&
                        nodes[node].IOModules.Count > module - 1)
                    {
                        nodes[node].IOModules[module - 1]
                            .AssignChannelToDevice(
                            physicalKlemme, this, channel);
                    }
                }
                return true;
            }
            else
            {
                error = string.Format(
                    "\"{0}\" : нет такого канала {1}:\"{2}\".",
                    name, addressSpace, comment);
                return false;
            }
        }

        /// <summary>
        /// Получить каналы устройства, которые привязывались или будут
        /// привязываться к модулю ввода-вывода
        /// </summary>
        /// <param name="addressSpace">Тип адресного пространства
        /// модуля ввода-вывода</param>
        /// <param name="channelName">Имя канала для IO-Link</param>
        /// <param name="comment">Комментарий канала</param>
        /// <returns></returns>
        public List<IOChannel> GetChannels(
            IOModuleInfo.ADDRESS_SPACE_TYPE addressSpace, string channelName,
            string comment)
        {
            var IO = new List<IOChannel>();

            switch (addressSpace)
            {
                case IOModuleInfo.ADDRESS_SPACE_TYPE.DO:
                    IO.AddRange(DO);
                    break;

                case IOModuleInfo.ADDRESS_SPACE_TYPE.DI:
                    IO.AddRange(DI);
                    break;

                case IOModuleInfo.ADDRESS_SPACE_TYPE.AO:
                    IO.AddRange(AO);
                    break;

                case IOModuleInfo.ADDRESS_SPACE_TYPE.AI:
                    IO.AddRange(AI);
                    break;

                case IOModuleInfo.ADDRESS_SPACE_TYPE.AOAI:
                    IO.AddRange(AO);
                    IO.AddRange(AI);
                    break;

                case IOModuleInfo.ADDRESS_SPACE_TYPE.DODI:
                    IO.AddRange(DO);
                    IO.AddRange(DI);
                    break;

                case IOModuleInfo.ADDRESS_SPACE_TYPE.AOAIDODI:
                    switch (channelName)
                    {
                        case IOChannel.DO:
                            IO.AddRange(DO);
                            break;

                        case IOChannel.DI:
                            IO.AddRange(DI);
                            break;

                        default:
                            IO.AddRange(AO);
                            IO.AddRange(AI);
                            break;
                    }
                    break;
            }

            bool haveNewLineSymbol = comment
                .Contains(CommonConst.NewLineWithCarriageReturn) ||
                comment.Contains(CommonConst.NewLine);
            if (haveNewLineSymbol)
            {
                comment = comment
                    .Replace(CommonConst.NewLineWithCarriageReturn, "");
                comment = comment
                    .Replace(CommonConst.NewLine, "");
            }

            List<IOChannel> findedChannels = IO.FindAll(delegate (IOChannel channel)
            {
                return channel.Comment == comment;
            });

            return findedChannels;
        }

        /// <summary>
        /// Установка свойства. У устройства могут быть дополнительные свойства,
        /// которые задаются отдельно (доп. поле №4).
        /// </summary>
        virtual public string SetProperty(string name, object value)
        {
            string res = string.Empty;
            if (properties.TryGetValue(name, out _))
            {
                string valueAsStr = value as string;
                valueAsStr = valueAsStr.Replace("\'", string.Empty);
                properties[name] = valueAsStr.Trim();
                OnPropertyChanged?.Invoke();
            }
            else
            {
                res = $"\"{name}\" - свойство не найдено." +
                    $"{CommonConst.NewLine}";
            }

            return res;
        }

        /// <summary>
        /// Проверка устройства на корректную инициализацию.
        /// </summary>
        /// <returns>Строка с описанием ошибки.</returns>
        public virtual string Check()
        {
            var err = new List<string>();
            if (Name.Length > DeviceNameLimit)
                err.Add($"{Name} : превышена длина названия устройства ({DeviceNameLimit} символов).\n");

            // Каналы ввода-вывода
            err.AddRange(Channels.Where(c => c.IsEmpty())
                .Select(c => $"{name} : не привязанный канал {c.Name} \"{c.Comment}\".\n"));

            // Параметры
            err.AddRange(Parameters.Where(par => par.Value is null)
                .Select(par => $"{name} : не задан параметр (доп. поле 3) \"{par.Key.Name}\".\n"));

            // Свойства
            err.AddRange(Properties.Where(prop => prop.Value is null)
                .Select(prop => $"{name} : не задано свойство (доп. поле 4) \"{prop.Key}\".\n"));

            // Рабочие параметры
            err.AddRange(RuntimeParameters.Where(par => par.Value is null && !((RuntimeParameter)par.Key).AutoGenerated)
                .Select(par => $"{name} : не задан рабочий параметр (доп. поле 5) \"{par.Key}\".\n"));

            return string.Join("", err);
        }

        public IEplanFunction Function { get; set; }

        /// <summary>
        /// Сохранение в виде массива данных (для экспорта в таблицу).
        /// </summary>
        /// <returns>Количество записанных строк.</returns>
        public int SaveAsArray(ref object[,] arr, int row, int maxColumn)
        {
            int column = 0;

            arr[row, column++] = name;
            arr[row, column++] = description;
            arr[row, column++] = dType.ToString();
            if (dType != DeviceType.NONE && dSubType != DeviceSubType.NONE)
            {
                arr[row, column++] = GetDeviceSubTypeStr(dType, dSubType);
            }
            else
            {
                arr[row, column++] = "";
            }

            //Параметры.
            foreach (var par in parameters)
            {
                arr[row, column++] = par.Key;
                arr[row, column++] = par.Value;
                if (column >= maxColumn) break;
            }

            return 1;
        }

        #region сохранение в Lua
        /// <summary>
        /// Сохранение в виде таблицы Lua.
        /// </summary>
        /// <param name="prefix">Префикс (для выравнивания).</param>
        virtual public string SaveAsLuaTable(string prefix)
        {
            string res = prefix + "{\n";

            res += SaveBasicData(prefix);
            res += SaveProperties(prefix);
            res += SaveBindedSignals(DO, prefix);
            res += SaveBindedSignals(DI, prefix);
            res += SaveBindedSignals(AO, prefix);
            res += SaveBindedSignals(AI, prefix);
            res += SaveRuntimeParameters(prefix);
            res += SaveParameters(prefix);

            res += prefix + "}";
            return res;
        }

        /// <summary>
        /// Сохранить базовую информацию об устройстве
        /// </summary>
        /// <param name="prefix">Префикс (для выравнивания).</param>
        /// <returns></returns>
        private string SaveBasicData(string prefix)
        {
            string res = string.Empty;

            res += prefix + "name    = \'" + Name + "\',\n";
            res += prefix + "descr   = \'" +
                EplanMultilineText.NormalizeDescription(Description) +
                "\',\n";
            res += prefix + "dtype   = " + (int)dType + ",\n";
            res += prefix + "subtype = " + dSubType.GetIndex() + ", -- " +
                GetDeviceSubTypeStr(dType, dSubType) + "\n";
            res += prefix + $"article = \'{ArticleName}\',\n";

            return res;
        }

        /// <summary>
        /// Сохранить свойства
        /// </summary>
        /// <param name="prefix">Префикс (для выравнивания).</param>
        /// <returns></returns>
        private string SaveProperties(string prefix)
        {
            string res = string.Empty;

            if (properties.Count > 0)
            {
                var validProperties = properties
                    .Where(x => x.Value != null &&
                    (x.Value.ToString() != "\'\'" &&
                    x.Value.ToString() != string.Empty));
                if (validProperties.Count() > 0)
                {
                    res += prefix + "prop = --Дополнительные свойства\n";
                    res += prefix + "\t{\n";

                    foreach (var prop in validProperties)
                    {
                        var values = prop.Value.ToString().Split(',');
                        res += prefix + $"\t{prop.Key} = ";


                        if (values.Count() > 1)
                        {
                            res += "{ ";
                            foreach (var value in values)
                            {
                                res += $"\'{value}\', ";
                            }
                            res += "},\n";
                        }
                        else
                        {
                            res += $"\'{prop.Value}\',\n";
                        }
                    }
                    res += prefix + "\t},\n";
                }
            }

            return res;
        }

        /// <summary>
        /// Сохранить привязанные сигналы
        /// </summary>
        /// <param name="channels">Список каналов для сохранения
        /// (AO, AI, DO, DI)</param>
        /// <param name="prefix">Префикс (для выравнивания).</param>
        /// <returns></returns>
        private string SaveBindedSignals(List<IOChannel> channels,
            string prefix)
        {
            string res = string.Empty;

            int bindedChannels = CountOfBindedChannels(channels);
            if (channels.Count > 0 && bindedChannels > 0)
            {
                string typeName = channels.First().Name;
                res += $"{prefix}{typeName} =\n";
                res += $"{prefix}\t{{\n";
                foreach (IOChannel ch in channels)
                {
                    res += ch.SaveAsLuaTable($"{prefix}\t\t");
                }
                res += $"{prefix}\t}},\n";
            }

            return res;
        }

        /// <summary>
        /// Сохранить рабочие параметры
        /// </summary>
        /// <param name="prefix">Префикс (для выравнивания).</param>
        /// <returns></returns>
        private string SaveRuntimeParameters(string prefix)
        {
            string res = string.Empty;

            if (rtParameters.Count > 0)
            {
                string tmp = string.Empty;

                foreach (var par in rtParameters)
                {
                    if (par.Value != null)
                    {
                        string tmpForSpacebars = $"\t\t\t\t{par.Value},";
                        int tmpForSpacebarsLength = tmpForSpacebars.Length % 4;
                        var spacebars =
                            new string(' ', 4 + (4 - tmpForSpacebarsLength));

                        tmp += $"\t\t\t\t{par.Value},{spacebars}--{par.Key}\n";
                    }
                }

                if (tmp != string.Empty)
                {
                    res += prefix + "rt_par = \n\t\t\t\t{\n";
                    res += tmp;
                    res += prefix + "\t\t},\n";
                }
            }

            return res;
        }

        /// <summary>
        /// Сохранить параметры
        /// </summary>
        /// <param name="prefix">Префикс (для выравнивания).</param>
        /// <returns></returns>
        protected virtual string SaveParameters(string prefix)
        {
            string res = string.Empty;

            if (parameters.Count > 0)
            {
                string tmp = string.Empty;
                foreach (var par in parameters)
                {
                    if (par.Value != null)
                    {
                        tmp += par.Value + " --[[" + par.Key + "]], ";
                    }
                }

                if (tmp != string.Empty)
                {
                    res += prefix + "par = {";
                    res += tmp.Remove(tmp.Length - 1 - 1);
                    res += " }\n";
                }
            }

            return res;
        }
        #endregion

        /// <summary>
        /// Сортировка каналов устройства для соответствия.
        /// </summary>
        public void SortChannels()
        {
            if (dType == DeviceType.V)
            {
                if (DI.Count > 1)
                {
                    List<IOChannel> tmp = new List<IOChannel>();

                    foreach (string descr in new string[] { "Открыт", "Закрыт" })
                    {
                        IOChannel resCh = DI.Find(delegate (IOChannel ch)
                        {
                            return ch.Comment == descr;
                        });

                        if (resCh != null)
                        {
                            tmp.Add(resCh);
                        }

                    }

                    DI = tmp;
                }

                if (DO.Count > 1)
                {
                    List<IOChannel> tmp2 = new List<IOChannel>();

                    foreach (string descr in new string[] { "Открыть", "Открыть мини", "Открыть ВС",
                                    "Открыть НС", "Закрыть" })
                    {
                        IOChannel resCh = DO.Find(delegate (IOChannel ch)
                        {
                            return ch.Comment == descr;
                        });

                        if (resCh != null)
                        {
                            tmp2.Add(resCh);
                        }

                    }

                    DO = tmp2;
                }
            }
        }

        /// <summary>
        /// Получение списка каналов устройства.
        /// </summary>
        public List<IOChannel> Channels
        {
            get
            {
                List<IOChannel> res = new List<IOChannel>();
                res.AddRange(DO);
                res.AddRange(DI);
                res.AddRange(AO);
                res.AddRange(AI);

                return res;
            }
        }


        /// <summary>
        /// Очистка привязки каналов устройства.
        /// </summary>
        public void ClearChannels()
        {
            foreach (IOChannel ch in Channels)
            {
                ch.Clear();
            }
        }

        /// <summary>
        /// Возвращает количество каналов, которые привязаны к модулю
        /// ввода-вывода
        /// </summary>
        /// <param name="channels">Список каналов устройства 
        /// (AO,AI,DO,DI)</param>
        /// <returns></returns>
        private int CountOfBindedChannels(List<IOChannel> channels)
        {
            var count = 0;

            foreach (var channel in channels)
            {
                if (channel.IsEmpty() == false)
                {
                    count++;
                }
            }

            return count;
        }

        public void SetIolConfProperty(string propertyName, double value)
        {
            if (iolConfProperties.ContainsKey(propertyName))
            {
                iolConfProperties[propertyName] = value;
            }
            else
            {
                iolConfProperties.Add(propertyName, value);
            }
        }

        public virtual List<string> MultipleProperties => [];

        public bool AllowedType(params DeviceType[] allowed)
            => allowed.Contains(DeviceType);

        public bool AllowedSubtype(params DeviceSubType[] allowed) 
            => allowed.Contains(DeviceSubType);

        /// <summary>
        /// Получить свойства устройства.
        /// </summary>
        public Dictionary<string, object> Properties
        {
            get
            {
                return properties;
            }
        }

        /// <summary>
        /// Получить параметры устройства.
        /// </summary>
        public Dictionary<Parameter, object> Parameters
        {
            get
            {
                return parameters;
            }
        }

        /// <summary>
        /// Формат единиц измерения для входных величин ПИД
        /// </summary>
        public virtual string PIDUnitFormat { get => UnitFormat.Empty; }

        /// <summary>
        /// Рабочие параметры устройства.
        /// </summary>
        public Dictionary<string, object> RuntimeParameters
        {
            get
            {
                return rtParameters;
            }
        }

        public Dictionary<string, double> IolConfProperties
        {
            get
            {
                return iolConfProperties;
            }
        }

        public string ArticleName { get; set; } = "";

        public IOLinkSize IOLinkProperties
        {
            get
            {
                return iolinkProperties;
            }
        }

        #region Закрытые поля.
        protected List<IOChannel> DO; ///Каналы дискретных выходов.
        protected List<IOChannel> DI; ///Каналы дискретных входов.
        protected List<IOChannel> AO; ///Каналы аналоговых выходов.
        protected List<IOChannel> AI; ///Каналы аналоговых входов.

        protected Dictionary<Parameter, object> parameters;   ///Параметры.
        protected Dictionary<string, object> rtParameters; ///Рабочие параметры.
        protected Dictionary<string, object> properties; ///Свойства.
        protected Dictionary<string, double> iolConfProperties; ///Свойства IOL-Conf.

        protected IOLinkSize iolinkProperties; ///IO-Link свойства устройства.

        protected delegate void PropertyChanged();
        protected event PropertyChanged OnPropertyChanged;
        #endregion
    }
}
