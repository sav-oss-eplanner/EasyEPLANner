using System;
using System.Collections.Generic;
using System.Linq;

namespace IO
{
    /// <summary>
    /// Описание узла ввода-вывода IO.
    /// </summary>
    public class IONodeInfo : ICloneable
    {
        /// <summary>
        /// Добавить информацию об узле ввода вывода
        /// </summary>
        /// <param name="isCoupler">Является ли узел каплером</param>
        /// <param name="name">Имя модуля ввода-вывода IO</param>
        /// <param name="type">Тип узла</param>
        public static void AddNodeInfo(string name, IONode.TYPES type,
            bool isCoupler)
        {
            var nodeInfo = new IONodeInfo(name, type, isCoupler);

            bool nodeNotExists = nodes
                .Where(x => x.Name == nodeInfo.Name).Count() == 0;
            if (nodeNotExists)
            {
                nodes.Add(nodeInfo);
            }
        }

        /// <summary>
        /// Получение описания узла ввода-вывода IO на основе его имени.
        /// </summary>
        /// <param name="name">Имя узла (750-860).</param>
        /// <param name="isStub">Признак не идентифицированного модуля.</param>
        public static IONodeInfo GetNodeInfo(string name, out bool isStub)
        {
            isStub = false;

            IONodeInfo res = nodes.Find(x => x.Name == name);
            if (res != null)
            {
                return res.Clone() as IONodeInfo;
            }

            isStub = true;
            return Stub;
        }

        /// <summary>
        /// Имя узла по типу (первый подходящий из sys_io_nodes.lua).
        /// </summary>
        public static string GetNameByType(IONode.TYPES type)
        {
            IONodeInfo res = nodes.Find(x => x.Type == type);
            return res?.Name;
        }

        /// <summary>
        /// Закрытый конструктор. Используется для создания списка применяемых
        /// узлов.
        /// </summary>
        /// <param name="isCoupler">Является ли узел каплером</param>
        /// <param name="name">Имя узла</param>
        /// <param name="type">Тип узла</param>
        private IONodeInfo(string name, IONode.TYPES type, bool isCoupler)
        {
            IsCoupler = isCoupler;
            Name = name;
            Type = type;
        }

        public object Clone()
        {
            return new IONodeInfo(Name, Type, IsCoupler);
        }

        /// <summary>
        /// Имя узла ввода-вывода IO (серия-номер, например: 750-341).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Тип узла ввода-вывода.
        /// </summary>  
        public IONode.TYPES Type { get; set; }

        /// <summary>
        /// Является каплером.
        /// </summary>  
        public bool IsCoupler { get; set; }

        /// <summary>
        /// Количество узлов ввода-вывода.
        /// </summary>
        public static int Count => nodes.Count;

        /// <summary>
        /// Узлы ввода-вывода.
        /// </summary>
        public static List<IONodeInfo> Nodes => nodes;

        /// <summary>
        /// Список узлов ввода-вывода.
        /// </summary>
        private static List<IONodeInfo> nodes = new List<IONodeInfo>();

        /// <summary>
        /// Заглушка, для возврата в случае поиска неописанных узлов. 
        /// </summary>
        public static IONodeInfo Stub = new IONodeInfo("",
            IONode.TYPES.T_EMPTY, false);
    }
}