namespace EasyEPlanner
{
    public enum DeviceLuaChangeKind
    {
        Description,
        SubType,
        Parameter,
        Property,
        RuntimeParameter,
    }

    /// <summary>
    /// Отличие поля устройства между main.io.lua и ФСА.
    /// </summary>
    public sealed class DeviceLuaChange
    {
        public DeviceLuaChange(
            string deviceName,
            DeviceLuaChangeKind kind,
            string fieldName,
            string oldValue,
            string newValue)
        {
            DeviceName = deviceName ?? string.Empty;
            Kind = kind;
            FieldName = fieldName ?? string.Empty;
            OldValue = oldValue ?? string.Empty;
            NewValue = newValue ?? string.Empty;
        }

        public string DeviceName { get; }

        public DeviceLuaChangeKind Kind { get; }

        /// <summary>
        /// Имя поля: пусто для описания/подтипа, имя параметра/свойства иначе.
        /// </summary>
        public string FieldName { get; }

        public string OldValue { get; }

        public string NewValue { get; }

        public string Caption => Kind switch
        {
            DeviceLuaChangeKind.Description => "Описание",
            DeviceLuaChangeKind.SubType => "Подтип",
            DeviceLuaChangeKind.Parameter => $"Параметр {FieldName}",
            DeviceLuaChangeKind.Property => $"Свойство {FieldName}",
            DeviceLuaChangeKind.RuntimeParameter => $"Рабочий параметр {FieldName}",
            _ => FieldName,
        };

        public override string ToString() =>
            $"{DeviceName}: {Caption}: \"{OldValue}\" → \"{NewValue}\"";
    }
}
