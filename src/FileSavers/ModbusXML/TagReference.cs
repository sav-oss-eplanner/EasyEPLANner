using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyEPlanner.FileSavers.ModbusXML
{
    /// <summary>
    /// Ссылка на переменную для тега в WinForms
    /// </summary>
    /// <typeparam name="T"></typeparam>
    sealed class TagReference<T>
    {
        private readonly Func<T> getter;
        private readonly Action<T> setter;

        public TagReference(Func<T> getter, Action<T> setter)
        {
            this.getter = getter;
            this.setter = setter;
        }

        /// <summary>
        /// Установить/получить значение переменной
        /// </summary>
        public T Value
        {
            get { return getter(); }
            set { setter(value); }
        }
    }
}
