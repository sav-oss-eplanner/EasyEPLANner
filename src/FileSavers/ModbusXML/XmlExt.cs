using System.Collections.Generic;
using System.Xml;

namespace EasyEPlanner.FileSavers.ModbusXML
{
    /// <summary>
    /// Расширение функций для XML
    /// </summary>
    public static class XmlExt
    {
        /// <summary>
        /// Базовое пространство имен для XML-элементов
        /// </summary>
        private static readonly string baseNS = "http://brestmilk.by/";

        /// <summary>
        /// Добавление XML-элемента
        /// </summary>
        /// <param name="element">XML-элемент для вставки</param>
        /// <param name="prefix">Префикс тега</param>
        /// <param name="name">Название тега</param>
        /// <param name="ns">Пространство имен</param>
        /// <param name="innerText">Внутренний текст</param>
        /// <returns>Вставленный XML-элемент</returns>
        public static XmlElement Insert(this XmlElement element, string prefix, string name, string ns = "", object innerText = null)
        {
            var innnerElement = element.OwnerDocument.CreateElement(prefix, name, $"{baseNS}{prefix}/");

            if (innerText != null)
            {
                innnerElement.InnerText = innerText.ToString();
            }

            if (ns != string.Empty)
            {
                innnerElement.SetAttribute($"xmlns:{ns}", $"{baseNS}{ns}/");
            }

            element.AppendChild(innnerElement);
            return innnerElement;
        }

        /// <summary>
        /// Добавление XML-элементов
        /// </summary>
        /// <param name="element">XML-элемент для вставки</param>
        /// <param name="prefix">Префикс тегов</param>
        /// <param name="tags"></param>
        public static void Insert(this XmlElement element, string prefix, Dictionary<string, object> tags)
        {
            foreach (var tag in tags)
            {
                var innnerElement = element.OwnerDocument.CreateElement(prefix, tag.Key, $"{baseNS}{prefix}/");

                if (tag.Value != null)
                {
                    innnerElement.InnerText = tag.Value.ToString();
                }

                element.AppendChild(innnerElement);
            }
        }
    }
}


