using Aga.Controls.Tree;
using EplanDevice;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace EasyEPlanner
{
    /// <summary>
    /// Узел для свойств устройства с несколькими колонками для <see cref="DFrm"/>
    /// </summary>
    /// <param name="name">Название - первая колонка</param>
    /// <param name="value">Значение - вторая колонка</param>
    public abstract class ColumnNode(string name, string value) : Node(name)
    {
        /// <summary>
        /// Значение
        /// </summary>
        public virtual string Value { get; set; } = value;
        
        /// <summary>
        /// Название
        /// </summary>
        public string Name => Text;

        /// <summary>
        /// Обновить свойство устройства
        /// </summary>
        /// <param name="device">Устройство</param>
        /// <param name="newValue">Новое значение</param>
        /// <param name="oldValue">Старое значение</param>
        public abstract void Update(IIODevice device, string newValue, string oldValue);
    }

    /// <summary>
    /// Узел подтипа устройства
    /// </summary>
    public class DeviceSubTypeNode(string value) : ColumnNode("Подтип", value)
    {
        public override void Update(IIODevice device, string newValue, string oldValue)
        {
            if (newValue == oldValue)
                return;

            if (device.DeviceType.SubTypeNames().Contains(newValue))
            {
                if (device.Function != null)
                {
                    device.Function.SubType = newValue;
                    Value = "Обновите проект";
                }
                else
                {
                    device.ApplySubTypeName(newValue);
                    Value = newValue;
                }
            }
            else Value = oldValue;
        }
    }

    /// <summary>
    /// Узел с описанием устройства
    /// </summary>
    public class DeviceDescriptionNode(string value) : ColumnNode("Описание", value)
    {
        public override void Update(IIODevice device, string newValue, string oldValue)
        {
            if (newValue == oldValue)
                return;

            Value = newValue;
            device.SetDescription(newValue);
            if (device.Function != null)
                device.Function.Description = newValue;
        }
    }

    /// <summary>
    /// Узел с изделием устройства
    /// </summary>
    public class DeviceArticleNode(string value) : ColumnNode("Изделие", value)
    {
        public override void Update(IIODevice device, string newValue, string oldValue)
        {
            /// Не редактируется в <see cref="DFrm"/>
            Value = oldValue;
        }
    }

    /// <summary>
    /// Узел с параметром устройства
    /// </summary>
    public class ParameterNode(string name, string value) : ColumnNode(name, value)
    {
        public override void Update(IIODevice device, string newValue, string oldValue)
        {
            if (oldValue == newValue)
                return;

            if (double.TryParse(newValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double parValue))
            {
                Value = newValue;
                var parameter = (IODevice.Parameter)Tag;
                device.SetParameter(parameter.Name, parValue);
                device.UpdateParameters();
            }
            else Value = oldValue;
        }
    }

    /// <summary>
    /// Устройство со свойством проекта
    /// </summary>
    public class PropertyNode(string name, string value) : ColumnNode(name, value)
    {
        public override void Update(IIODevice device, string newValue, string oldValue)
        {
            if (oldValue == newValue)
                return;

            /// К свойству нельзя привязать несколько значений и в значении есть запятая
            if (device.MultipleProperties.Contains(Name) ||
                !newValue.Contains(","))
            {
                Value = newValue;
                device.SetProperty(Name, newValue);
                device.UpdateProperties();
            }
            else Value = oldValue;
        }
    }

    /// <summary>
    /// Устройство с рабочим параметром устройства
    /// </summary>
    public class RuntimeParameterNode(string name, string value) : ColumnNode(name, value)
    {
        public override void Update(IIODevice device, string newValue, string oldValue)
        {
            if (oldValue == newValue)
                return;

            if (int.TryParse(newValue, out int parValue))
            {
                Value = newValue;
                device.SetRuntimeParameter(Name, parValue);
                device.UpdateRuntimeParameters();
            }
            else Value = oldValue;
        }
    }
}
