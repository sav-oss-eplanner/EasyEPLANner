using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyEPlanner.FileSavers.ModbusXML
{
    /// <summary>
    /// Данные драйвера базы каналов
    /// </summary>
    public class Driver
    {
        /// <summary>
        /// Название
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Идентификатор
        /// </summary>
        public int ID { get; set; } = 0;

        /// <summary>
        /// Описание
        /// </summary>
        public string Description { get; set; } = "Универсальный драйвер для протоколов Modbus и SNMP";
    }


    /// <summary>
    /// Данные подтипа базы каналов
    /// </summary>
    public class Subtype
    {
        /// <summary>
        /// Название
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Идентификатор
        /// </summary>
        public int ID { get; set; } = 0;

        /// <summary>
        /// Описание
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Параметр: IP-адрес
        /// </summary>
        public string IP { get; set; } = "";

        /// <summary>
        /// Параметр: порт
        /// </summary>
        public int Port { get; set; } = 506;

        /// <summary>
        /// Параметр: proto
        /// </summary>
        public string Proto { get; set; } = "Modbus";

        /// <summary>
        /// Параметр: timeout
        /// </summary>
        public int TimeOut { get; set; } = 500;
    }


    /// <summary>
    /// Данные для экспортирования CSV в базу каналов
    /// </summary>
    public interface IModbusChbaseViewModel
    {
        /// <summary>
        /// Путь к файлу CSV с описанием проекта
        /// </summary>
        string CsvFile { get; set; }

        /// <summary>
        /// Данные драйвера базы каналов
        /// </summary>
        Driver Driver { get; set; }

        /// <summary>
        /// Данные подтипа в базе каналов
        /// </summary>
        Subtype Subtype {  get; set; }
    }


    public class ModbusChbaseModelView : IModbusChbaseViewModel
    {
        public string CsvFile { get; set; } = "";

        public Driver Driver { get; set; } = new Driver();
        
        public Subtype Subtype { get; set; } = new Subtype();
    }
}
