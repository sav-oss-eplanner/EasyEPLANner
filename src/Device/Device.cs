using System;
using System.Linq;

namespace EplanDevice
{
    /// <summary>
    /// Устройство - клапан, насос и т.д.
    /// </summary>
    public class Device : IComparable, IEquatable<Device>, IDevice
    {

        public static int Compare(Device dx, Device dy)
        {
            if (dx == null && dy == null)
                return 0;

            if (dx == null)
                return -1;

            if (dy == null)
                return 1;


            if (dx.dType == dy.dType)
            {
                if (dx.objectName == dy.objectName)
                {
                    if (dx.ObjectNumber == dy.ObjectNumber)
                    {
                        if (dx.DeviceNumber == dy.DeviceNumber)
                        {
                            return 0;
                        }
                        else
                        {
                            return dx.DeviceNumber.CompareTo(dy.DeviceNumber);
                        }
                    }
                    else
                    {
                        return dx.ObjectNumber.CompareTo(dy.ObjectNumber);
                    }
                }
                else
                {
                    return dx.objectName.CompareTo(dy.objectName);
                }
            }
            else
            {
                return dx.dType.CompareTo(dy.dType);
            }
        }

        /// <summary>
        /// Конструктор на основе имени.
        /// </summary>
        /// <param name="name">Имя устройства (А1V12).</param>
        /// <param name="eplanName">Имя устройства в Eplan (+A1-V12)</param>
        /// <param name="deviceNumber">Номер устройства</param>
        /// <param name="objectName">Имя объекта (A)</param>
        /// <param name="objectNumber">Номер объекта (1)</param>
        /// <param name="description">Описание устройства.</param>
        protected Device(string name, string eplanName, string description,
            int deviceNumber, string objectName, int objectNumber)
        {
            this.name = name;
            this.eplanName = eplanName;
            this.description = description;
            this.deviceNumber = deviceNumber;
            this.objectName = objectName;
            this.objectNumber = objectNumber;
        }

        /// <summary>
        /// Установка подтипа устройства.
        /// </summary>
        public virtual string SetSubType(string subType)
        {
            ApplySubTypeName(subType);
            return string.Empty;
        }

        /// <summary>
        /// Сменить подтип в памяти без пересоздания каналов.
        /// </summary>
        public void ApplySubTypeName(string subType)
        {
            try
            {
                dSubType = (DeviceSubType)Enum.Parse(typeof(DeviceSubType),
                    subType.ToUpper());
            }
            catch
            {
                dSubType = DeviceSubType.NONE;
            }
        }

        /// <summary>
        /// Установить описание устройства в памяти.
        /// </summary>
        public void SetDescription(string value)
        {
            description = value ?? string.Empty;
        }

        public virtual string GetConnectionType()
        {
            return string.Empty;
        }

        public virtual string GetRange()
        {
            return string.Empty;
        }

        public Device Clone()
        {
            return (Device)MemberwiseClone();
        }

        #region Реализация интерфейсов IComparable, IEquatable<Device>.
        /// <summary>
        /// Метод проверки равенства объектов (для поиска).
        /// </summary>
        public bool Equals(Device otherDevice)
        {
            if (otherDevice == null)
            {
                return false;
            }

            if (name.Equals(otherDevice.name))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Метод проверки равенства объектов (для поиска).
        /// </summary>
        public override bool Equals(Object otherDevice)
        {
            if (otherDevice == null)
                return ReferenceEquals(this, otherDevice);

            return Equals(otherDevice as Device);
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        /// <summary>
        /// Метод сравнения объектов (для сортировки).
        /// </summary>
        public int CompareTo(object otherDevice)
        {
            if (otherDevice == null)
                return 1;

            Device obj = otherDevice as Device;

            if (dType == obj.dType)
            {
                return string.CompareOrdinal(name, obj.name);
            }
            else
            {
                return dType.CompareTo(obj.dType);
            }
        }
        #endregion

        public string Name
        {
            get
            {
                return name;
            }
        }

        public string EplanName
        {
            get
            {
                return eplanName;
            }
        }

        public string DeviceDesignation => EplanName.Split('-').LastOrDefault() ?? string.Empty;

        public long DeviceNumber
        {
            get
            {
                return deviceNumber;
            }
        }

        public DeviceType DeviceType
        {
            get
            {
                return dType;
            }
        }

        public DeviceSubType DeviceSubType
        {
            get
            {
                return dSubType;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        public string ObjectName
        {
            get
            {
                return objectName;
            }
        }

        public int ObjectNumber
        {
            get
            {
                return objectNumber;
            }
        }

        /// <summary>
        /// Отладочное строковое представление устройства.
        /// </summary>
        public virtual string DPrint()
        {
            return String.Format("{0,8} {1} ", name, description);
        }

        #region Защищенные поля.
        protected readonly string name; /// Имя устройства (А1V12).
        protected string eplanName; /// Имя из Eplan (+A1-V12)
        protected int deviceNumber;     /// Номер устройства (12 в R1V12).
        protected DeviceType dType = DeviceType.NONE;
        protected DeviceSubType dSubType = DeviceSubType.NONE;
        protected int dLocation; /// Номер узла, в котором должно располагаться устройство.
        protected string objectName;    /// Объект устройства (R в R1V12).
        protected int objectNumber;     /// Номер объекта (1 в R1V12)
        protected string description;  /// Описание устройства (пример - "Отсечной клапан линии").   
        #endregion
    }
}
