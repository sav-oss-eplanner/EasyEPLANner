using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace EasyEPlanner.FileSavers.ModbusXML
{
    public class ModbusXmlSaver
    {
        private readonly XmlDocument xmlDoc;

        public ModbusXmlSaver()
        {
            xmlDoc = new XmlDocument();
        }

        /// <summary>
        /// Сохранить файл базы каналов
        /// </summary>
        /// <param name="path">Путь для сохранения файла</param>
        /// <param name="context">Данные для сохранения базы каналов</param>
        public void Save(string path, IModbusChbaseViewModel context)
        {
            var writer = new XmlTextWriter(path, EncodingDetector.UTF8Bom);

            writer.WriteStartDocument();
            writer.WriteStartElement("driver");
            writer.WriteAttributeString("xmlns", "driver", null, "http://brestmilk.by/driver/");
            writer.WriteEndElement();
            writer.Close();
            xmlDoc.Load(path);

            xmlDoc.DocumentElement.Insert("driver", "inf", "", "BASE F11F3DCC-09F8-4D04-BCB7-81D5D7C48C78");
            xmlDoc.DocumentElement.Insert("driver", "dbbuild", "", "4");
           
            var driver = SetupDriver(context);
            var subtype = SetupSubtype(driver, context);

            var channels = subtype.Insert("subtypes", "channels", "channels");

            string csvData = "";
            using (StreamReader reader = new StreamReader(context.CsvFile))
            {
                csvData = reader.ReadToEnd();
            }
            
            SetupTags(channels, ParseCSV(csvData));

            xmlDoc.Save(path);
        }


        /// <summary>
        /// Сохранение драйвера в базе каналов
        /// </summary>
        /// <param name="context">Данные драйвера</param>
        /// <returns>XML-узел драйвера</returns>
        private XmlElement SetupDriver(IModbusChbaseViewModel context)
        {
            var driver = xmlDoc.DocumentElement.Insert("driver", "driver");

            driver.Insert("driver", new Dictionary<string, object>()
            {
                ["id"] = context.Driver.ID,
                ["tid"] = 0,
                ["dllname"] = $"{context.Driver.Name}.dll",
                ["access"] = 1,
                ["maxsubtypescount"] = 10,
                ["enabled"] = -1,
                ["descr"] = context.Driver.Description,
                ["drvname"] = context.Driver.Name,
                ["defname"] = "Opc Driver",
                ["defdescr"] = context.Driver.Description,

            });

            var communication = driver.Insert("driver", "communication", "communication");
            communication.Insert("communication", "parameters", "parameters");

            driver.Insert("driver", "init_parameters", "parameters");
            driver.Insert("driver", "common_parameters", "parameters");
            driver.Insert("driver", "final_parameters", "parameters");

            return driver;
        }

        /// <summary>
        /// Сохранение подтипа в базе каналов
        /// </summary>
        /// <param name="driver">XML-узле драйвера</param>
        /// <param name="context">Данные подтипа/param>
        /// <returns>XML-узел подтипа</returns>
        private XmlElement SetupSubtype(XmlElement driver, IModbusChbaseViewModel context)
        {
            var subtypes = driver.Insert("driver", "subtypes", "subtypes");
            var subtype = subtypes.Insert("subtypes", "subtype");

            subtype.Insert("subtypes", new Dictionary<string, object>()
            {
                ["sid"] = context.Subtype.ID,
                ["stid"] = 0,
                ["maxchannels"] = 0,
                ["enabled"] = -1,
                ["descr"] = context.Subtype.Description,
                ["defdescr"] = context.Subtype.Description,
                ["sdrvname"] = context.Subtype.Name,
                ["sdrvdefname"] = context.Subtype.Name,

            });

            var common_parameters = subtype.Insert("subtypes", "common_parameters", "parameters");

            AddParameter(common_parameters, "IP", context.Subtype.IP);
            AddParameter(common_parameters, "Proto", context.Subtype.Proto);
            AddParameter(common_parameters, "Port", context.Subtype.Port);
            AddParameter(common_parameters, "Timeout", context.Subtype.TimeOut);

            return subtype;
        }

        /// <summary>
        /// Добавление параметра в узел
        /// </summary>
        /// <param name="element">XML-элемент для добавления параметра</param>
        /// <param name="name">Название параметра</param>
        /// <param name="value">Значение параметра</param>
        private static void AddParameter(XmlElement element, string name, object value)
        {
            element
                .Insert("parameters", "parameter")
                .Insert("parameters", new Dictionary<string, object>()
                {
                    ["name"] = name,
                    ["value"] = value.ToString()
                });
        }

        /// <summary>
        /// Разбор CSV файла с описанием проекта
        /// </summary>
        /// <param name="csvData">Данные CSV-файла</param>
        /// <returns>Разобранные данные проекта для базы каналов</returns>
        private static IEnumerable<Channel> ParseCSV(string csvData)
        {
            var tags = csvData.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l =>
                    {
                        var e = l.Split(';');
                        return  new Channel(e[0], e[1],e[2]);
                    })
                    .Where(l => l.Type != "" && !l.Type.Contains("Array"));

            var groups = tags.GroupBy(l => $"{l.Type}_{l.Address}");

            tags = groups.Select(g => g.Key.Contains("Bool") ? new Channel(g.Key, g.First().Type, g.First().Address) : g.First()).ToList();


            foreach (var tag in tags.SkipWhile(t => t.Tag != "INCOMMING").Skip(1).TakeWhile(t => t.Tag != "OUTGOING"))
            {
                tag.IsIncomming = true;
            }

            return tags.Where(t => t.Tag != "INCOMMING" && t.Tag != "OUTGOING" && t.Tag != "END");
        }

        /// <summary>
        /// Сохранение тегов базы каналов
        /// </summary>
        /// <param name="channels">Родительский XML-элемент</param>
        /// <param name="tags">Теги для сохранения</param>
        private void SetupTags(XmlElement channels, IEnumerable<Channel> tags)
        {
            var channel_id = 0;
            foreach (var tag in tags)
            {
                var channel = channels.Insert("channels", "channel", "channels");
                channel.Insert("channels", new Dictionary<string, object>()
                {
                    ["id"] = 0x50050000 + channel_id++,
                    ["requesttype"] = 1,
                    ["requestperiod"] = 1,
                    ["enabled"] = -1,
                    ["descr"] = tag.Description,
                    ["delta"] = 0,
                    ["apptime"] = 0,
                    ["protocol"] = 0,
                });
                var transexprin = channel.Insert("channels", "transexprin");
                var transexprout = channel.Insert("channels", "transexprout");

                transexprin.AppendChild(xmlDoc.CreateCDataSection(""));
                transexprout.AppendChild(xmlDoc.CreateCDataSection(""));

                var parameters = channel.Insert("channels", "channels_parameters", "parameters");

                if (tag.Type == "Real")
                    AddParameter(parameters, "ByteOrder", "4321");
            }
        }
    }

    /// <summary>
    /// Данные тега из CSV для базы каналов
    /// </summary>
    public class Channel
    {
        public Channel(string tag, string type, string address)
        {
            Tag = tag;
            Type = type;

            if (int.TryParse(address.Split('.')[0], out var addres_n))
            {
                Address = addres_n / 2;
            }
        }

        public Channel(string tag, string type, int address)
        {
            Tag = tag;
            Type = type;
            Address = address;
        }

        /// <summary>
        /// Входные/выходные каналы
        /// </summary>
        public bool IsIncomming { get; set; } = false; 

        /// <summary>
        /// Название канала
        /// </summary>
        public string Tag { get; }

        /// <summary>
        /// Тип данных канала
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Адрес канала
        /// </summary>
        public int Address { get; } = 0;

        /// <summary>
        /// Описание тега для базы каналов
        /// </summary>
        public string Description
        {
            get
            {
                switch (Type)
                {
                    case "Bool": return $"4X: {Address}: {Tag}";
                    case "Word": return $"4X: {Address}: {Tag} {(IsIncomming ? "отправляем" : "получаем")}";
                    case "Real" when IsIncomming: return $"4XR: {Address}: задание {Tag}";
                    case "Real": case "Int": return $"4XR: {Address}: {Tag}";

                    default: return "";
                }
            }
        }
    }
}


