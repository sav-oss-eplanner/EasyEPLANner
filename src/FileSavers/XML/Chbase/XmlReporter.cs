using EasyEPlanner.FileSavers.XML;
using EasyEPlanner.PxcIolinkConfiguration.Models;
using EplanDevice;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using TechObject;

namespace EasyEPlanner
{
    public sealed class ChannelBaseSubtypeStatistics
    {
        public ChannelBaseSubtypeStatistics(string baseName, List<ISubtype> subtypes)
        {
            BaseName = baseName;
            SubtypeCount = subtypes.Count;
            TotalChannelCount = subtypes.Sum(subtype => subtype.Channels.Count);
            var largestSubtype = subtypes
                .OrderByDescending(subtype => subtype.Channels.Count)
                .FirstOrDefault();
            MaxChannelsPerSubtype = largestSubtype?.Channels.Count ?? 0;
            MaxChannelsSubtypeName = largestSubtype?.Description ?? string.Empty;
        }

        public string BaseName { get; }
        public int SubtypeCount { get; }
        public int TotalChannelCount { get; }
        public int MaxChannelsPerSubtype { get; }
        public string MaxChannelsSubtypeName { get; }
    }

    public sealed class ChannelBaseExportAnalysis
    {
        public ChannelBaseExportAnalysis(List<ChannelBaseSubtypeStatistics> bases)
        {
            Bases = bases;
            ExceedsSubtypeLimit = bases.Any(item => item.SubtypeCount > XmlReporter.MaximumSubtypeCount);
            ExceedsChannelLimit = bases.Any(item => item.MaxChannelsPerSubtype > XmlReporter.MaximumChannelCountPerSubtype);
        }

        public IReadOnlyList<ChannelBaseSubtypeStatistics> Bases { get; }
        public bool ExceedsSubtypeLimit { get; }
        public bool ExceedsChannelLimit { get; }
        public bool IsWithinLimits => !ExceedsSubtypeLimit && !ExceedsChannelLimit;
    }

    public static class ChannelBaseAnalysisRunner
    {
        public static T Run<T>(Func<T> analysis)
        {
            if (analysis == null)
            {
                throw new ArgumentNullException(nameof(analysis));
            }

            var previousCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture =
                    StaticHelper.CommonConst.CultureWithDotInsteadComma;
                return analysis();
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previousCulture;
            }
        }
    }

    public static class ChannelBaseIdGenerator
    {
        public static long Generate(int driverId, int subtypeId, int channelId)
        {
            if (driverId < 0 || driverId > byte.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(driverId));
            }

            if (subtypeId < 0 || subtypeId > byte.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(subtypeId));
            }

            if (channelId < 0 || channelId > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(channelId));
            }

            string hexId = $"{driverId:X2}{subtypeId:X2}{channelId:X4}";
            return long.Parse(hexId, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }
    }

    [ExcludeFromCodeCoverage]
    public class XmlReporter
    {
        public const int MaximumSubtypeCount = 256;
        public const int MaximumChannelCountPerSubtype = 65536;
        const string xmlns = "xmlns";
        const string prefixDriver = "driver";
        const string prefixParameters = "parameters";
        const string prefixSubtypes = "subtypes";
        const string prefixChannels = "channels";

        const string rootNS = "https://www.savushkin.com/";
        readonly string nsDriver = Path.Combine(rootNS, "driver/");
        readonly string nsSubtypes = Path.Combine(rootNS, "subtypes/");
        readonly string nsParameters = Path.Combine(rootNS, "parameters/");
        readonly string nsChannels = Path.Combine(rootNS, "channels/");
        readonly string nsCommunication = Path.Combine(rootNS, "communication/");


        /// <summary>
        /// Экспорт из проекта базы каналов.
        /// </summary>
        public void SaveAsCDBX(string projectName, bool combineTag = false,
            bool useNewNames = false, bool rewrite = false, bool splitDatabases = false)
        {
            Thread t = new Thread(
                new ParameterizedThreadStart(SaveAsXMLThread));

            var dataForSave = new DataForSaveAsXml(projectName, rewrite,
                combineTag, useNewNames, splitDatabases);
            t.CurrentCulture = StaticHelper.CommonConst
                .CultureWithDotInsteadComma;
            t.Start(dataForSave);
        }
        
        public void AwaitSaveAsCDBX(string projectName, bool combineTag = false, bool useNewNames = false, bool rewrite = false, bool splitDatabases = false)
        {
            SaveAsXML(projectName, rewrite, combineTag, useNewNames, splitDatabases);
        }


        /// <summary>
        /// Экспорт базы каналов, поток.
        /// </summary>
        /// <param name="param">Параметр потока</param>
        private void SaveAsXMLThread(object param)
        {
            var data = param as DataForSaveAsXml;

            Logs.Show();
            Logs.DisableButtons();
            Logs.Clear();
            Logs.SetProgress(0);

            try
            {
                Logs.SetProgress(1);
                SaveAsXML(data.pathToFile, data.rewrite, data.cdbxTagView,
                    data.cdbxNewNames, data.splitDatabases);
                Logs.SetProgress(50);
                Logs.AddMessage("Done.");
            }
            catch (System.Exception ex)
            {
                Logs.AddMessage("Exception - " + ex);
            }
            finally
            {
                if (!Logs.IsNull())
                {
                    Logs.EnableButtons();
                    Logs.SetProgress(100);
                }
            }
        }

        /// <summary>
        /// Класс для передачи данных при сохранении XML базы каналов
        /// </summary>
        private sealed class DataForSaveAsXml
        {
            public DataForSaveAsXml(string pathToFile, bool rewrite,
                bool cdbxTagView, bool cdbxNewNames, bool splitDatabases)
            {
                this.pathToFile = pathToFile;
                this.rewrite = rewrite;
                this.cdbxNewNames = cdbxNewNames;
                this.cdbxTagView = cdbxTagView;
                this.splitDatabases = splitDatabases;
            }

            public string pathToFile;
            public bool rewrite;
            public bool cdbxTagView;
            public bool cdbxNewNames;
            public bool splitDatabases;
        }

        /// <summary>
        /// Экспорт проекта в базу каналов.
        /// </summary>
        /// <param name="path">Путь к файлу</param>
        /// <param name="rewrite">Перезаписывать или нет</param>
        /// <param name="cdbxNewNames">Использовать имена объектов вместо OBJECT
        /// </param>
        /// <param name="cdbxTagView">Сгруппировать тэги в один подтип</param>
        private void SaveAsXML(string path, bool rewrite = false,
            bool cdbxTagView = false, bool cdbxNewNames = false,
            bool splitDatabases = false)
        {
            projectConfig.SynchronizeDevices();

            if (splitDatabases)
            {
                var techObjectsRoot = new Driver(deviceManager);
                techObjectManager.GetObjectForXML(techObjectsRoot, cdbxTagView, cdbxNewNames);

                var devicesRoot = new Driver(deviceManager);
                deviceManager.GetObjectForXML(devicesRoot);

                string directory = Path.GetDirectoryName(path);
                string fileName = Path.GetFileNameWithoutExtension(path);
                string extension = Path.GetExtension(path);
                SaveAsXMLCore(Path.Combine(directory, $"{fileName}_TechObjectsAndSystem{extension}"), true, techObjectsRoot, 1);
                SaveAsXMLCore(Path.Combine(directory, $"{fileName}_Devices{extension}"), true, devicesRoot, 2);
                return;
            }

            var root = CreateCombinedRoot(cdbxTagView, cdbxNewNames);
            SaveAsXMLCore(path, rewrite, root);
        }

        private IDriver CreateCombinedRoot(bool combineTags, bool useNewNames)
        {
            var root = new Driver(deviceManager);
            techObjectManager.GetObjectForXML(root, combineTags, useNewNames);
            deviceManager.GetObjectForXML(root);
            return root;
        }

        private void SaveAsXMLCore(string path, bool rewrite, IDriver root,
            int driverId = 1)
        {
            var xmlDoc = new XmlDocument();
            if (!File.Exists(path) || rewrite)
            { // Create new chbase
                if (rewrite && File.Exists(path))
                    File.Delete(path);

                CreateNewChannelBase(path, xmlDoc, root, driverId);
            }
            else
            {
                xmlDoc.Load(path);

                var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace(prefixDriver, nsDriver);
                XmlElement xmlroot = xmlDoc.DocumentElement;
                var elm = xmlroot.SelectSingleNode("//driver:id", nsmgr) as 
                    XmlElement;
                string baseId = elm.InnerText;
                               
                var subTypesId = GenerateSubTypesIdsList();

                elm = xmlroot.SelectSingleNode("//driver:subtypes", nsmgr) as 
                    XmlElement;
                nsmgr.AddNamespace(prefixSubtypes, nsSubtypes);
                // Настройка элементов базы каналов
                foreach (XmlElement item in elm.ChildNodes)
                {
                    SetUpExistingChannelBase(subTypesId, item, root);                   
                }

                // Вычисление свободных идентификаторов и их присвоение
                foreach(ISubtype subtype in root.Subtypes)
                {
                    CalculateIdentificatorsForChannelBase(subtype, elm, nsmgr,
                        baseId, subTypesId, xmlDoc);
                }
            }

            xmlDoc.Save(path);
        }

        /// <summary>
        /// Сгенерировать новую базу каналов
        /// </summary>
        /// <param name="path">Путь к месту хранения</param>
        /// <param name="xmlDoc">XML-документ, куда писать</param>
        /// <param name="rootNode">Узел с данными</param>
        private void CreateNewChannelBase(string path, XmlDocument xmlDoc,
            IDriver root, int driverId)
        {
            var textWritter = new XmlTextWriter(path,
                EncodingDetector.UTF8Bom);
            textWritter.WriteStartDocument();
            textWritter.WriteStartElement(prefixDriver);
            textWritter.WriteAttributeString(xmlns, prefixDriver, null, nsDriver);
            textWritter.WriteEndElement();
            textWritter.Close();
            xmlDoc.Load(path);

            XmlElement subtypesNode = WriteCommonXMLPart(xmlDoc, driverId);
            CreateNewChannels(xmlDoc, subtypesNode, root.Subtypes, driverId);
        }

        /// <summary>
        /// Формирование общей структуры базы каналов
        /// </summary>
        private XmlElement WriteCommonXMLPart(XmlDocument xmlDoc, int driverId)
        {
            xmlDoc.DocumentElement.AddElement(prefixDriver, "inf", nsDriver, "BASE F11F3DCC-09F8-4D04-BCB7-81D5D7C48C78");
            xmlDoc.DocumentElement.AddElement(prefixDriver, "dbbuild", nsDriver, "4");
            var driverXml = xmlDoc.DocumentElement.AddElement(prefixDriver, prefixDriver, nsDriver);

            driverXml.AddElement(prefixDriver, "id", nsDriver, driverId.ToString());
            driverXml.AddElement(prefixDriver, "tid", nsDriver, "0");
            driverXml.AddElement(prefixDriver, "dllname", nsDriver, "PAC_easy_drv_LZ.dll");
            driverXml.AddElement(prefixDriver, "access", nsDriver, "2");
            driverXml.AddElement(prefixDriver, "maxsubtypescount", nsDriver, "10");
            driverXml.AddElement(prefixDriver, "enabled", nsDriver, "-1");
            driverXml.AddElement(prefixDriver, "descr", nsDriver, "Система PLC-X1");
            driverXml.AddElement(prefixDriver, "drvname", nsDriver, Path.GetFileNameWithoutExtension(xmlDoc.BaseURI));
            driverXml.AddElement(prefixDriver, "defname", nsDriver, "Opc Driver");
            driverXml.AddElement(prefixDriver, "defdescr", nsDriver, "Универсальный драйвер для протоколов Modbus и SNMP");

            var communication = driverXml.AddElement(prefixDriver, "communication", nsDriver);
            communication.SetAttribute($"{xmlns}:{prefixParameters}", nsCommunication);
            
            var parameters = communication.AddElement("communication", prefixParameters, nsCommunication);
            parameters.SetAttribute($"{xmlns}:{prefixParameters}", nsParameters);
            AddParameter(parameters, "TYPE", "COM");
            AddParameter(parameters, "PORTNAME", "COM4");
            AddParameter(parameters, "SPEED", "12");
            AddParameter(parameters, "PARITY", "0");
            AddParameter(parameters, "DATABITS", "4");
            AddParameter(parameters, "STOPBITS", "0");

            var init_parameters = driverXml.AddElement(prefixDriver, "init_parameters", nsDriver);
            init_parameters.SetAttribute($"{xmlns}:{prefixParameters}", nsParameters);
            AddParameter(init_parameters, "IP", "IP127.0.0.1");
            string plcName;
            if (string.IsNullOrEmpty(EProjectManager.GetInstance().GetCurrentProjectName()))
            {
                Logs.AddMessage("Не задано PLC_NAME.");
                plcName = "PLC_NAME";
            }
            else
            {
                string projectName = EProjectManager.GetInstance().GetCurrentProjectName();
                EProjectManager.GetInstance().CheckProjectName(ref projectName);
                plcName = projectName;
            }
            AddParameter(init_parameters, "PLC_NAME", plcName);
            AddParameter(init_parameters, "PORT", "10000");
            AddParameter(init_parameters, "Kontroller", "LINUX");

            driverXml.AddElement(prefixDriver, "common_parameters", nsDriver)
                .SetAttribute($"{xmlns}:{prefixParameters}", nsParameters);

            driverXml.AddElement(prefixDriver, "final_parameters", nsDriver)
                .SetAttribute($"{xmlns}:{prefixParameters}", nsParameters);

            var subtypes = driverXml.AddElement(prefixDriver, prefixSubtypes, nsDriver);
            subtypes.SetAttribute($"{xmlns}:{prefixSubtypes}", nsSubtypes);

            return subtypes;
        }


        /// <summary>
        /// Добавить параметр в элемент
        /// </summary>
        /// <param name="parent">Элемент для вставки параметра</param>
        /// <param name="name">Название параметра</param>
        /// <param name="value">Значение параметра</param>
        private void AddParameter(XmlElement parent, string name, string value)
        {
            var parameter = parent.AddElement(prefixParameters, "parameter", nsParameters);
            parameter.AddElement(prefixParameters, "name", nsParameters, name);
            parameter.AddElement(prefixParameters, "value", nsParameters, value);
        }

        /// <summary>
        /// Создание узлов и каналов в новой пустой базе каналов
        /// </summary>
        private void CreateNewChannels(XmlDocument xmlDoc, XmlElement subtypesNode,
            List<ISubtype> subtypes, int driverId)
        {
            foreach (var i in Enumerable.Range(0, subtypes.Count))
            {
                XmlElement subtypeElm = AddSubType(subtypesNode, subtypes[i], i);

                foreach (var j in Enumerable.Range(0, subtypes[i].Channels.Count))
                {
                    long channelId = ChannelBaseIdGenerator.Generate(driverId, i, j);
                    AddChannel(xmlDoc, subtypes[i].Channels[j], subtypeElm, channelId);
                }
            }
        }

        /// <summary>
        /// Генерация заполненного списка с номерами узлов
        /// </summary>
        /// <returns></returns>
        private static List<string> GenerateSubTypesIdsList()
        {
            const int maxNodesCount = MaximumSubtypeCount;
            var list = new List<string>();
            for (int i = 0; i < maxNodesCount; i++)
            {
                list.Add(i.ToString());
            }
            return list;
        }

        /// <summary>
        /// Настройка существующей базы каналов
        /// </summary>
        private static void SetUpExistingChannelBase(List<string> subtupesId,
            XmlElement item, IDriver root)
        {
            const int channelLocationId = 9;
            XmlNodeList subTypeChannels = item.ChildNodes[channelLocationId]
                .ChildNodes;
            if (subtupesId.Contains(item.ChildNodes[0].InnerText))
            {
                subtupesId.Remove(item.ChildNodes[0].InnerText);
            }

            string objectName = item.ChildNodes[6].InnerText;
            var subtypes = root.Subtypes.Where(s => s.Description == objectName).ToList();
            SetUpChannelBaseObject(subtypes, item, subTypeChannels);
        }

        /// <summary>
        /// Настройка узла базы каналов
        /// </summary>
        /// <param name="subtypes">Редактируемые узлы базы каналов</param>
        /// <param name="item">Узел в XML</param>
        /// <param name="subTypeChannels">Тэги узла в XML</param>
        private static void SetUpChannelBaseObject(List<ISubtype> subtypes, 
            XmlElement item, XmlNodeList subTypeChannels)
        {
            const int channelDescrNum = 4;

            ReplaceObjectNumber(subTypeChannels, subtypes, channelDescrNum);
            ReplaceStepsToRunSteps(subTypeChannels, channelDescrNum);
            OnOffNodesDependOnTheirState(subtypes, item, subTypeChannels,
                channelDescrNum);
        }

        /// <summary>
        /// Перезаписать номера каналов подтипа (в OBJECT).
        /// </summary>
        /// <param name="subTypeChannels">Список каналов</param>
        /// <param name="subtypes">Редактируемые узлы</param>
        /// <param name="channelDescrNum">Номер ячейки описания канала</param>
        private static void ReplaceObjectNumber(XmlNodeList subTypeChannels,
            List<ISubtype> subtypes, int channelDescrNum)
        {
            const string searchPattern = @"(?<name>OBJECT)(?<n>[0-9]+)+";

            XmlNode firstChannel = subTypeChannels[0];
            XmlNode firstChannelDescr =
                firstChannel.ChildNodes[channelDescrNum];
            if (firstChannelDescr != null &&
                firstChannelDescr.InnerText.Contains(DefaultNodeName) &&
                subtypes.SelectMany(st => st.Channels).Any())
            {
                string newNodeName = subtypes[0].Channels[0].Description;
                string newNum = Regex.Match(newNodeName, searchPattern, RegexOptions.None, TimeSpan.FromMilliseconds(100))
                    .Groups["n"].Value;

                string oldNodeName = firstChannelDescr.InnerText;
                string oldNum = Regex.Match(oldNodeName, searchPattern, RegexOptions.None, TimeSpan.FromMilliseconds(100))
                    .Groups["n"].Value;

                if (newNum != oldNum)
                {
                    foreach (XmlElement channel in subTypeChannels)
                    {
                        channel.ChildNodes[channelDescrNum].InnerText =
                            Regex.Replace(
                                channel.ChildNodes[channelDescrNum].InnerText,
                                searchPattern, $"{DefaultNodeName}{newNum}",
                                RegexOptions.None, TimeSpan.FromMilliseconds(100));
                    }
                }
            }
        }

        /// <summary>
        /// Замена устаревшего описания .STEPS на .RUN_STEPS
        /// </summary>
        /// <param name="channels">Список каналов </param>
        /// <param name="channelDescrNum">Номер ячейки описания канала</param>
        private static void ReplaceStepsToRunSteps(XmlNodeList channels,
            int channelDescrNum)
        {
            const string searchOldStepsPattern = "\\.STEPS[1-9]\\[";

            foreach (XmlElement channel in channels)
            {
                var channelDescription = channel.ChildNodes[channelDescrNum]
                    .InnerText;
                Match oldStepsRegex = Regex
                    .Match(channelDescription, searchOldStepsPattern,
                    RegexOptions.None, TimeSpan.FromMilliseconds(100));
                if (oldStepsRegex.Success)
                {
                    channel.ChildNodes[channelDescrNum].InnerText =
                        channelDescription.Replace(".STEPS", ".RUN_STEPS");
                }
            }
        }

        ///<summary>
        /// Отключение и включение узлов и их тэгов в зависимости от их
        /// нового состояния (если удалены -> отключаются).
        ///</summary>
        /// <param name="subtypes">Узел базы каналов</param>
        /// <param name="item">Узел в XML</param>
        /// <param name="subTypeChannels">Тэги узла в XML</param>
        /// <param name="channelDescrNum">Номер ячейки описания канала</param>
        private static void OnOffNodesDependOnTheirState(List<ISubtype> subtypes,
            XmlElement item, XmlNodeList subTypeChannels, int channelDescrNum)
        {
            const int channelEnabledId = 3;
            const string disable = "0";
            const string enable = "-1";

            if (subtypes.Count == 0)
            { 
                // Удалить узел
                item.ChildNodes[channelEnabledId].InnerText = disable; // Комментирование удаленных узлов.
            }
            else
            {
                item.ChildNodes[channelEnabledId].InnerText = enable;

                foreach (XmlElement chan in subTypeChannels)
                {
                    foreach (var subtype in subtypes)
                    {
                        var cahnnels = subtype.Channels.Where(ch => ch.Description == chan.ChildNodes[channelDescrNum].InnerText.Trim());
                        if (cahnnels.Any())
                        {
                            chan.ChildNodes[channelEnabledId].InnerText = enable;
                        }
                        else
                        {
                            chan.ChildNodes[channelEnabledId].InnerText = disable;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Расчет идентификаторов для базы каналов
        /// </summary>
        private void CalculateIdentificatorsForChannelBase(ISubtype subtype,
            XmlElement elm, XmlNamespaceManager nsmgr, string baseId,
            List<string> subTypesId, XmlDocument xmlDoc)
        {
            string xpath = "//subtypes:subtype[subtypes:sdrvname='" +
                subtype.Description + "']";
            var subElm = elm.SelectSingleNode(xpath, nsmgr) as XmlElement;
            if (subElm is null)
            {
                if (subTypesId.Count <= 0)
                {
                    Logs.AddMessage("Превышено количество подтипов " +
                        "в базе каналов.");
                    return;
                }
               
               
                long subtypeId = long.Parse(subTypesId[0]);
                subTypesId.RemoveAt(0);
                XmlElement newSubtype = AddSubType(elm, subtype, subtypeId);
                string hex = long.Parse(baseId).ToString("X2") +
                    subtypeId.ToString("X2");
                for (int i = 0; i < subtype.Channels.Count; i++)
                {
                    long channelId = long.Parse((hex + i.ToString("X4")),
                        System.Globalization.NumberStyles.HexNumber);
                    AddChannel(xmlDoc, subtype.Channels[i], newSubtype,
                        channelId);
                }
                return;
            }
            
            nsmgr.AddNamespace("channels", nsChannels);
            CalculateIdentificatorsForSubtype(subtype, subElm, baseId, xmlDoc);
        }

        /// <summary>
        /// Расчет идентификаторов для подтипа
        /// </summary>
        private void CalculateIdentificatorsForSubtype(ISubtype subtype, XmlElement subElm, string baseId, XmlDocument xmlDoc)
        {
            var channelsElm = subElm.ChildNodes[9] as XmlElement;
            var channelsId = new List<long>();
            
            foreach (IChannel channel in subtype.Channels)
            {
                XmlNode tagNode = null;
                foreach (XmlNode node in channelsElm.ChildNodes)
                {
                    if (node.InnerText.Contains(channel.Name))
                    {
                        tagNode = node;
                    }
                }

                if (tagNode is not null)
                    continue;

                // Нахождение адреса канала среди свободных
                if (channelsId.Count == 0)
                {
                    long beginId = long.Parse(
                        long.Parse(baseId).ToString("X2") +
                        long.Parse(subElm.ChildNodes[0].InnerText).ToString("X2") +
                        "0000",
                        System.Globalization.NumberStyles.HexNumber);
                    channelsId = FindFreeChannel(channelsElm, beginId);
                }

                long channelId = channelsId[0];
                channelsId.RemoveAt(0);
                AddChannel(xmlDoc, channel, channelsElm, channelId);
            }
        }

        /// <summary>
        /// Нахождение адреса канала среди свободных
        /// </summary>
        /// <param name="channelsElm">Список каналов</param>
        /// <param name="beginId">Начальный индекс</param>
        private static List<long> FindFreeChannel(XmlElement channelsElm, long beginId)
        {
            const int maxTagsCount = MaximumChannelCountPerSubtype;
            var res = new List<long>();

            for (int i = 0; i < maxTagsCount; i++)
            {
                res.Add(beginId + i);
            }

            foreach (XmlElement channId in channelsElm.ChildNodes)
            {
                long id = long.Parse(channId.FirstChild.InnerText);
                if (res.Contains(id))
                {
                    res.Remove(id);
                }
            }

            return res;
        }

        /// <summary>
        /// Добавление узла в базу каналов
        /// </summary>
        private XmlElement AddSubType(XmlElement subtypesNode, ISubtype subtype, long subTypeId)
        {
            var xmlSubType = subtypesNode.AddElement(prefixSubtypes, "subtype", nsSubtypes);

            xmlSubType.AddElement(prefixSubtypes, "sid", nsSubtypes, subTypeId.ToString());
            xmlSubType.AddElement(prefixSubtypes, "stid", nsSubtypes, "0");
            xmlSubType.AddElement(prefixSubtypes, "maxchannels", nsSubtypes, "0");
            xmlSubType.AddElement(prefixSubtypes, "enabled", nsSubtypes, "-1");
            xmlSubType.AddElement(prefixSubtypes, "descr", nsSubtypes, "Описание");
            xmlSubType.AddElement(prefixSubtypes, "defdescr", nsSubtypes, "Описание");
            xmlSubType.AddElement(prefixSubtypes, "sdrvname", nsSubtypes, subtype.Description);
            xmlSubType.AddElement(prefixSubtypes, "sdrvdefname", nsSubtypes, "Узел");
            xmlSubType.AddElement(prefixSubtypes, "common_parameters", nsSubtypes)
                .SetAttribute($"{xmlns}:{prefixParameters}", nsParameters);
            var channels = xmlSubType.AddElement(prefixSubtypes, "channels", nsSubtypes);
            channels.SetAttribute($"{xmlns}:{prefixChannels}", nsChannels);

            return channels;
        }

        /// <summary>
        /// Добавление канала с указанным адресом
        /// </summary>
        private void AddChannel(XmlDocument xmlDoc, IChannel channel,
            XmlElement subtypeElm, long channelId)
        {
            var xmlChannel = subtypeElm.AddElement(prefixChannels, "channel", nsChannels);
            xmlChannel.AddElement(prefixChannels, "id", nsChannels, channelId.ToString());
            xmlChannel.AddElement(prefixChannels, "requesttype", nsChannels, channel.IsRequestByTime ? "0" : "1");
            xmlChannel.AddElement(prefixChannels, "requestperiod", nsChannels, channel.RequestPeriod.ToString());
            xmlChannel.AddElement(prefixChannels, "enabled", nsChannels, "-1");
            xmlChannel.AddElement(prefixChannels, "descr", nsChannels, channel.Description);
            xmlChannel.AddElement(prefixChannels, "delta", nsChannels, channel.Delta.ToString());
            xmlChannel.AddElement(prefixChannels, "apptime", nsChannels, "0");
            xmlChannel.AddElement(prefixChannels, "protocol", nsChannels, channel.IsLogged ? "-1" : "0");

            xmlChannel.AddElement(prefixChannels, "transexprin", nsChannels)
                .AppendChild(xmlDoc.CreateCDataSection(""));

            xmlChannel.AddElement(prefixChannels, "transexprout", nsChannels)
                .AppendChild(xmlDoc.CreateCDataSection(""));

            var channelElm = xmlChannel.AddElement(prefixChannels, "channel_parameters", nsChannels);
            channelElm.SetAttribute($"{xmlns}:{prefixParameters}", nsParameters);

            foreach (var parameter in channel.Parameters)
            {
                AddChannelAtribute(channelElm, parameter.Key, parameter.Value);
            }
        }

        /// <summary>
        /// Добавление атрибута канала
        /// </summary>
        private void AddChannelAtribute(XmlElement elm, string atribute, string value)
        {
            var par = elm.AddElement(prefixParameters, "channel", nsParameters);
            par.AddElement(prefixParameters, "name", nsParameters, atribute);
            par.AddElement(prefixParameters, "value", nsParameters, value);
        }

        private const string DefaultNodeName = "OBJECT";

        /// <summary>
        /// Получить предварительную статистику экспорта для выбранных настроек.
        /// </summary>
        public ChannelBaseExportAnalysis AnalyzeExport(bool combineTags, bool splitDatabases)
        {
            projectConfig.SynchronizeDevices();
            var statistics = new List<ChannelBaseSubtypeStatistics>();

            if (splitDatabases)
            {
                var techObjectsRoot = new Driver(deviceManager);
                techObjectManager.GetObjectForXML(techObjectsRoot, combineTags, false);
                statistics.Add(new ChannelBaseSubtypeStatistics(
                    "Технологические объекты", techObjectsRoot.Subtypes));

                var devicesRoot = new Driver(deviceManager);
                deviceManager.GetObjectForXML(devicesRoot);
                statistics.Add(new ChannelBaseSubtypeStatistics("Устройства", devicesRoot.Subtypes));
            }
            else
            {
                var root = new Driver(deviceManager);
                techObjectManager.GetObjectForXML(root, combineTags, false);
                deviceManager.GetObjectForXML(root);
                statistics.Add(new ChannelBaseSubtypeStatistics("Общая база", root.Subtypes));
            }

            return new ChannelBaseExportAnalysis(statistics);
        }

        /// <summary>
        /// Получить количество тегов проекта.
        /// </summary>
        /// <returns></returns>
        public static int GetTagsCount()
        {
            var root = new Driver(deviceManager);
            bool useNewNames = false;
            bool combineTags = false;
            techObjectManager.GetObjectForXML(root, combineTags, useNewNames);
            deviceManager.GetObjectForXML(root);

            int tagsCount = 0;
            foreach (var subtype in root.Subtypes)
            {
                tagsCount += subtype.Channels.Count;
            }

            return tagsCount;
        }

        public void AutomaticExportNewChannelBaseCombineTags(
            string projectDirPath, string projectName)
        {
            string path = Path.Combine(projectDirPath, "chbase");
            Directory.CreateDirectory(path);
            path = Path.Combine(path, $"{projectName}.cdbx");
            SaveAsXML(path, true, true);
        }

        static ProjectConfiguration projectConfig = ProjectConfiguration
            .GetInstance();
        static ITechObjectManager techObjectManager = TechObjectManager
            .GetInstance();
        static IDeviceManager deviceManager = DeviceManager.GetInstance();
    }
}
