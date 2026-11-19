namespace EplanDevice
{
    public interface IDevice
    {
        /// <summary>
        /// Имя устройства (например - А1V12).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Имя устройства (например "+А1-V12").
        /// </summary>
        string EplanName { get; }

        /// <summary>
        /// Обозначение устройства без объекта. Пример: V1 <br/>
        /// Используется если обозначение устройства не соответствует типу: "FC1" (подтип "C") 
        /// </summary>
        string DeviceDesignation { get; }

        /// <summary>
        /// Описание устройства.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Установить описание устройства в памяти.
        /// На ФСА значение записывается через Function.Description.
        /// </summary>
        void SetDescription(string description);

        /// <summary>
        /// Сменить подтип в памяти без пересоздания каналов.
        /// На ФСА значение записывается через Function.SubType.
        /// </summary>
        void ApplySubTypeName(string subType);

        /// <summary>
        /// Номер объекта устройства.
        /// </summary>
        int ObjectNumber { get; }

        /// <summary>
        /// Объект устройства.
        /// </summary>
        string ObjectName { get; }

        /// <summary>
        /// Номер устройства.
        /// </summary>
        long DeviceNumber { get; }

        /// <summary>
        /// Тип устройства.
        /// </summary>
        DeviceType DeviceType { get; }

        /// <summary>
        /// Подтип устройства.
        /// </summary>
        DeviceSubType DeviceSubType { get; }

        /// <summary>
        /// Получение типа подключения для устройства
        /// </summary>
        string GetConnectionType();

        /// <summary>
        /// Получение диапазона настройки
        /// </summary>
        string GetRange();
    }
}
