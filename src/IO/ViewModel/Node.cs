using BrightIdeasSoftware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.ViewModel
{
    public class Node : INode, IExpandable, IHasIcon, IHasDescriptionIcon
    {
        private readonly List<IModule> modules = [];

        private readonly List<IViewItem> items = [];

        public Node(IIONode node, ILocation owner)
        {
            IONode = node;

            if (node.Type is IO.IONode.TYPES.T_EMPTY)
                return;

            if (node.CanDisableNtype)
            {
                items.Add(new ComboBoxProperty("Состояние",
                    () => GetEnabledText(IONode.NtypeEnabled),
                    value => IONode.NtypeEnabled = value == EnabledNtypeText,
                    EnabledValues));
            }

            var ip = new Property("IP-адрес",
                () => IONode.Function?.IP ?? IONode.IP ?? string.Empty,
                ipValue =>
                {
                    if (IONode.Function != null)
                        IONode.Function.IP = ipValue;
                });

            var SubnetMask = new Property("Маска подсети",
                () => IONode.Function?.SubnetMask ?? string.Empty,
                mask =>
                {
                    if (IONode.Function != null)
                        IONode.Function.SubnetMask = mask;
                });

            var Gateway = new Property("Сетевой шлюз",
                () => IONode.Function?.Gateway ?? string.Empty,
                gw =>
                {
                    if (IONode.Function != null)
                        IONode.Function.Gateway = gw;
                });

            modules.AddRange(node.IOModules.Select(m => new Module(m, this)));
            
            items.Add(ip);
            items.Add(SubnetMask);
            items.Add(Gateway);
            items.AddRange(node.ExtensionModules.Select(m => new Node(m, owner)));
            items.AddRange(modules);
        }

        public IIONode IONode { get; private set; }

        public IEnumerable<IViewItem> Items => items;

        public string Name
        {
            get
            {
                if (IONode.Type is IO.IONode.TYPES.T_EMPTY)
                {
                    return $"{IONode.N}. Заглушка";
                }

                if (IONode.Name?.Contains('.') ?? false)
                {
                    return IONode.Name;
                }

                return $"{IONode.N}. {IONode.Name}";
            }
        }

        public string Description => IONode.TypeStr;

        public bool Expanded 
        {
            get => IONode.Function?.Expanded ?? false;
            set => IONode.Function?.Expanded = value;
        }

        Icon IHasIcon.Icon => Icon.Node;

        public bool HasBindingError =>
            modules.Any(m => m.HasBindingError) ||
            items.OfType<Node>().Any(n => n.HasBindingError);

        Icon IHasDescriptionIcon.Icon =>
            HasBindingError ? Icon.Error : Icon.None;

        /// <summary>
        /// Узел выключен в main.io.lua (ntype = -1).
        /// </summary>
        public bool IsNtypeDisabled =>
            IONode.CanDisableNtype && !IONode.NtypeEnabled;

        private static string GetEnabledText(bool enabled) =>
            enabled ? EnabledNtypeText : DisabledNtypeText;

        private const string EnabledNtypeText = "Включен";

        private const string DisabledNtypeText = "Выключен";

        private static readonly string[] EnabledValues =
        {
            EnabledNtypeText,
            DisabledNtypeText
        };
    }
}
