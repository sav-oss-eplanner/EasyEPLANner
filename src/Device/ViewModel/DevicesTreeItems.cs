using EasyEPlanner.Devices.ViewModel.ViewInterface;
using EplanDevice;
using IO;
using IO.ViewModel;
using StaticHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace EasyEPlanner.Devices.ViewModel
{
    public abstract class FilterableViewItemBase : IFilterableViewItem, IExpandable, IViewItem,
        IHasDevicesIcon
    {
        private readonly List<IViewItem> items = [];

        protected FilterableViewItemBase(IDevicesViewModel context, FilterableViewItemBase parent)
        {
            Context = context;
            Parent = parent;
        }

        public IDevicesViewModel Context { get; }

        protected FilterableViewItemBase Parent { get; }

        public FilterableViewItemBase ParentItem => Parent;

        public IEnumerable<IViewItem> Items => items;

        public bool Expanded { get; set; }

        public abstract string Name { get; protected set; }

        public virtual string Description => string.Empty;

        public virtual DevicesIcon Icon => DevicesIcon.None;

        public bool? Filtered { get; private set; }

        protected bool ThisOrParentsContains { get; set; }

        public void AddChild(IViewItem child)
        {
            items.Add(child);
        }

        public void AddChildren(IEnumerable<IViewItem> children)
        {
            items.AddRange(children);
        }

        public bool Filter(string searchString, bool hideEmptyItems)
        {
            if (Filtered.HasValue)
                return Filtered.Value;

            if (string.IsNullOrEmpty(searchString))
            {
                Filtered = true;
                return true;
            }

            if (Contains(searchString))
            {
                if (!Context.SearchContext.FoundItems.Contains(this))
                    Context.SearchContext.FoundItems.Add(this);
                ThisOrParentsContains = true;
                Filtered = true;
            }

            ThisOrParentsContains |= Parent?.ThisOrParentsContains ?? false;

            var childsPassedFilter = false;
            foreach (var item in items.OfType<IFilterableViewItem>())
            {
                childsPassedFilter |= item.Filter(searchString, hideEmptyItems);
            }

            Filtered = childsPassedFilter || ThisOrParentsContains;
            return Filtered.Value;
        }

        public void ResetFilter()
        {
            Filtered = null;
            ThisOrParentsContains = false;
            foreach (var item in items.OfType<IFilterableViewItem>())
                item.ResetFilter();
        }

        public virtual bool Contains(string value) =>
            DevicesSearch.Contains(GetSearchableText(), value);

        /// <summary>
        /// Текст для поиска и подсветки (может шире, чем отображаемое имя в ячейке).
        /// </summary>
        public virtual string GetSearchableText() =>
            $"{Name} {Description}".Trim();
    }

    public sealed class DevicesTypeGroupNode : FilterableViewItemBase, IBoldName
    {
        private int deviceCount;

        public DevicesTypeGroupNode(
            IDevicesViewModel context,
            FilterableViewItemBase parent,
            string typeKey,
            object tag)
            : base(context, parent)
        {
            TypeKey = typeKey;
            Tag = tag;
            Name = typeKey;
        }

        public string TypeKey { get; }

        public object Tag { get; }

        public override string Name { get; protected set; }

        public override DevicesIcon Icon => DevicesIcon.Type;

        public void IncrementCount() => deviceCount++;

        public void UpdateHeader() => Name = $"{TypeKey} ({deviceCount})";
    }

    public sealed class DevicesObjectGroupNode : FilterableViewItemBase, IBoldName
    {
        private int deviceCount;

        public DevicesObjectGroupNode(
            IDevicesViewModel context,
            FilterableViewItemBase parent,
            string objectKey,
            string displayName)
            : base(context, parent)
        {
            ObjectKey = objectKey;
            DisplayName = displayName;
            Name = displayName;
        }

        public string ObjectKey { get; }

        public string DisplayName { get; }

        public override string Name { get; protected set; }

        public override DevicesIcon Icon => DevicesIcon.Object;

        public void IncrementCount() => deviceCount++;

        public void UpdateHeader() => Name = $"{DisplayName} ({deviceCount})";

        public override string GetSearchableText() =>
            $"{DisplayName} {ObjectKey} {Name} {Description}".Trim();
    }

    public sealed class DevicesDeviceNode : FilterableViewItemBase, IBoldName, IGoToFas
    {
        public DevicesDeviceNode(
            IDevicesViewModel context,
            FilterableViewItemBase parent,
            IODevice device,
            string displayName)
            : base(context, parent)
        {
            Device = device;
            Name = displayName;
            AddChildren(DevicesDeviceContentBuilder.BuildGroups(this, device));
        }

        public IODevice Device { get; }

        public override string Name { get; protected set; }

        public override DevicesIcon Icon => DevicesIcon.Device;

        public IEplanFunction EplanFunction => Device.Function;

        public override string GetSearchableText()
        {
            var parts = new List<string> { Name, Description, Device.Name, Device.EplanName };
            if (!string.IsNullOrEmpty(Device.ObjectName))
            {
                var objectPrefix = Device.ObjectName + Device.ObjectNumber;
                parts.Add(objectPrefix);
                parts.Add(objectPrefix + Device.DeviceDesignation);
            }

            return string.Join(" ",
                parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        }
    }

    public sealed class DevicesGroupNode : FilterableViewItemBase
    {
        public DevicesGroupNode(
            IDevicesViewModel context,
            FilterableViewItemBase parent,
            string groupName,
            DevicesIcon icon,
            IEnumerable<IViewItem> children)
            : base(context, parent)
        {
            Name = groupName;
            groupIcon = icon;
            AddChildren(children);
        }

        private readonly DevicesIcon groupIcon;

        public override string Name { get; protected set; }

        public override DevicesIcon Icon => groupIcon;
    }

    public sealed class DevicesSubtypeItem : FilterableViewItemBase, IComboBoxEditable, IToolTip
    {
        private string valueOverride;

        public DevicesSubtypeItem(
            IDevicesViewModel context,
            FilterableViewItemBase parent,
            IODevice device)
            : base(context, parent)
        {
            Device = device;
            Name = "Подтип";
        }

        public IODevice Device { get; }

        public override string Name { get; protected set; }

        public override string Description =>
            valueOverride ?? Device.DeviceSubType.ToString();

        string IToolTip.Name => Name;

        public string Value => Description;

        public IEnumerable<string> ComboBoxItems =>
            Device.DeviceType.SubTypeNames();

        public bool SetValue(string value)
        {
            if (value == Description)
                return false;

            if (!Device.DeviceType.SubTypeNames().Contains(value))
                return false;

            if (Device.Function != null)
            {
                Device.Function.SubType = value;
                valueOverride = "Обновите проект";
            }
            else
            {
                Device.ApplySubTypeName(value);
                valueOverride = null;
            }

            return true;
        }
    }

    public sealed class DevicesDescriptionItem : FilterableViewItemBase, IEditable, IToolTip
    {
        public DevicesDescriptionItem(
            IDevicesViewModel context,
            FilterableViewItemBase parent,
            IODevice device)
            : base(context, parent)
        {
            Device = device;
            Name = "Описание";
        }

        public IODevice Device { get; }

        public override string Name { get; protected set; }

        private string EplanDescription =>
            DevicesTreeBuilder.GetEplanDescription(Device);

        public override string Description =>
            EplanMultilineText.FormatForCell(EplanDescription);

        string IToolTip.Name => Name;

        string IToolTip.Description =>
            EplanMultilineText.FormatForTooltip(EplanDescription);

        public string Value => EplanDescription;

        public bool SetValue(string value)
        {
            if (value == Value)
                return false;

            Device.SetDescription(value);
            if (Device.Function != null)
                Device.Function.Description = value;

            return true;
        }
    }

    public sealed class DevicesArticleItem : FilterableViewItemBase, IToolTip
    {
        public DevicesArticleItem(
            IDevicesViewModel context,
            FilterableViewItemBase parent,
            IODevice device)
            : base(context, parent)
        {
            Device = device;
            Name = "Изделие";
        }

        public IODevice Device { get; }

        public override string Name { get; protected set; }

        public override string Description => Device.ArticleName ?? string.Empty;

        string IToolTip.Name => Name;
    }

    public sealed class DevicesParameterItem : FilterableViewItemBase, IEditable, IToolTip
    {
        public DevicesParameterItem(
            IDevicesViewModel context,
            FilterableViewItemBase parent,
            IODevice device,
            IODevice.Parameter parameter,
            object value)
            : base(context, parent)
        {
            Device = device;
            Parameter = parameter;
            Name = parameter.Name;
            description = value?.ToString() ?? string.Empty;
            tooltipDescription = parameter.Description ?? string.Empty;
        }

        private string description;
        private readonly string tooltipDescription;

        public IODevice Device { get; }

        public IODevice.Parameter Parameter { get; }

        public override string Name { get; protected set; }

        public override string Description => description;

        string IToolTip.Name => tooltipDescription;

        string IToolTip.Description => string.Empty;

        public string Value => description;

        public bool SetValue(string value)
        {
            if (value == description)
                return false;

            if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture,
                    out double parValue))
            {
                return false;
            }

            description = value;
            Device.SetParameter(Parameter.Name, parValue);
            Device.UpdateParameters();
            return true;
        }
    }

    public sealed class DevicesPropertyItem : FilterableViewItemBase, IEditable, IToolTip
    {
        public DevicesPropertyItem(
            IDevicesViewModel context,
            FilterableViewItemBase parent,
            IODevice device,
            string propertyName,
            string value)
            : base(context, parent)
        {
            Device = device;
            PropertyName = propertyName;
            Name = propertyName;
            description = value ?? string.Empty;
        }

        private string description;

        public IODevice Device { get; }

        public string PropertyName { get; }

        public override string Name { get; protected set; }

        public override string Description => description;

        string IToolTip.Name => PropertyName;

        string IToolTip.Description => string.Empty;

        public string Value => description;

        public bool SetValue(string value)
        {
            if (value == description)
                return false;

            if (Device.MultipleProperties.Contains(PropertyName) || !value.Contains(","))
            {
                description = value;
                Device.SetProperty(PropertyName, value);
                Device.UpdateProperties();
                return true;
            }

            return false;
        }
    }

    public sealed class DevicesRuntimeParameterItem : FilterableViewItemBase, IEditable, IToolTip
    {
        public DevicesRuntimeParameterItem(
            IDevicesViewModel context,
            FilterableViewItemBase parent,
            IODevice device,
            string parameterName,
            string value)
            : base(context, parent)
        {
            Device = device;
            ParameterName = parameterName;
            Name = parameterName;
            description = value ?? string.Empty;
        }

        private string description;

        public IODevice Device { get; }

        public string ParameterName { get; }

        public override string Name { get; protected set; }

        public override string Description => description;

        string IToolTip.Name => Name;

        public string Value => description;

        public bool SetValue(string value)
        {
            if (value == description)
                return false;

            if (!int.TryParse(value, out int parValue))
                return false;

            description = value;
            Device.SetRuntimeParameter(ParameterName, parValue);
            Device.UpdateRuntimeParameters();
            return true;
        }
    }

    public sealed class DevicesChannelItem : FilterableViewItemBase, IToolTip, IHasDevicesDescriptionIcon,
        IGoToFas
    {
        public DevicesChannelItem(
            IDevicesViewModel context,
            FilterableViewItemBase parent,
            IODevice.IOChannel channel)
            : base(context, parent)
        {
            Channel = channel;
            Name = channel.Name + " " + channel.Comment;
        }

        public IODevice.IOChannel Channel { get; }

        public IODevice Device
        {
            get
            {
                var node = ParentItem;
                while (node is not null)
                {
                    if (node is DevicesDeviceNode deviceNode)
                        return deviceNode.Device;

                    node = node.ParentItem;
                }

                return null;
            }
        }

        [ExcludeFromCodeCoverage]
        public IEplanFunction EplanFunction => ResolveClampEplanFunction();

        public override string Name { get; protected set; }

        public override string Description =>
            Channel.IsEmpty()
                ? string.Empty
                : $"A{Channel.FullModule}:{Channel.PhysicalClamp}";

        string IToolTip.Name => Name;

        public override DevicesIcon Icon => DevicesIcon.Channel;

        public DevicesIcon DescriptionIcon =>
            Channel.IsEmpty() ? DevicesIcon.None : DevicesIcon.Clamp;

        [ExcludeFromCodeCoverage]
        private IEplanFunction ResolveClampEplanFunction()
        {
            if (Channel.IsEmpty())
                return null;

            try
            {
                var module = IOManager.GetInstance()
                    .GetModuleByPhysicalNumber(Channel.FullModule);
                return module.ClampFunctions.TryGetValue(Channel.PhysicalClamp, out var clampFunction)
                    ? clampFunction
                    : null;
            }
            catch
            {
                return null;
            }
        }
    }

    internal static class DevicesDeviceContentBuilder
    {
        public static IEnumerable<IViewItem> BuildGroups(
            FilterableViewItemBase parent,
            IODevice device)
        {
            var context = parent.Context;
            var groups = new List<IViewItem>();

            groups.Add(CreateGroup(context, parent, "Данные", DevicesIcon.Data, group =>
                new List<IViewItem>
                {
                    new DevicesSubtypeItem(context, group, device),
                    new DevicesDescriptionItem(context, group, device),
                    new DevicesArticleItem(context, group, device),
                }));

            if (device.Parameters?.Any() == true)
            {
                groups.Add(CreateGroup(context, parent, "Параметры", DevicesIcon.Parameters, group =>
                    device.Parameters.Select(p => new DevicesParameterItem(
                        context, group, device, p.Key, p.Value)).Cast<IViewItem>().ToList()));
            }

            if (device.RuntimeParameters?.Any() == true)
            {
                groups.Add(CreateGroup(context, parent, "Рабочие параметры",
                    DevicesIcon.RuntimeParameters, group =>
                    device.RuntimeParameters.Select(p => new DevicesRuntimeParameterItem(
                        context, group, device, p.Key, p.Value?.ToString() ?? ""))
                        .Cast<IViewItem>().ToList()));
            }

            if (device.Properties?.Any() == true)
            {
                groups.Add(CreateGroup(context, parent, "Свойства", DevicesIcon.Properties, group =>
                    device.Properties.Select(p => new DevicesPropertyItem(
                        context, group, device, p.Key, p.Value?.ToString() ?? ""))
                        .Cast<IViewItem>().ToList()));
            }

            if (device.Channels?.Any() == true)
            {
                groups.Add(CreateGroup(context, parent, "Каналы", DevicesIcon.Channels, group =>
                    device.Channels.Select(ch => new DevicesChannelItem(context, group, ch))
                        .Cast<IViewItem>().ToList()));
            }

            return groups;
        }

        private static DevicesGroupNode CreateGroup(
            IDevicesViewModel context,
            FilterableViewItemBase parent,
            string name,
            DevicesIcon icon,
            Func<FilterableViewItemBase, IList<IViewItem>> childrenFactory)
        {
            var group = new DevicesGroupNode(context, parent, name, icon,
                Array.Empty<IViewItem>());
            group.AddChildren(childrenFactory(group));
            return group;
        }
    }

    internal static class DevicesTreeBuilder
    {
        private static readonly string[] VirtSubTypes =
        [
            "AI_VIRT",
            "AO_VIRT",
            "DI_VIRT",
            "DO_VIRT",
        ];

        public static void Build(DevicesRoot root, IDevicesViewModel context)
        {
            var devices = context.DeviceManager?.Devices ?? [];
            if (context.GroupingMode is DevicesGroupingMode.TypeThenObject)
                BuildTypeThenObject(root, context, devices);
            else
                BuildObjectThenType(root, context, devices);
        }

        private static void BuildTypeThenObject(
            DevicesRoot root,
            IDevicesViewModel context,
            IEnumerable<IODevice> devices)
        {
            var typeNodes = CreateTypeNodeCatalog(root, context);
            var counts = typeNodes.ToDictionary(n => n.TypeKey, _ => 0);

            foreach (var dev in devices)
            {
                var typeNode = ResolveTypeNode(typeNodes, dev);
                if (typeNode is null)
                    continue;

                counts[typeNode.TypeKey]++;
                typeNode.IncrementCount();

                var parentForDevice = ResolveObjectParent(typeNode, dev);
                if (parentForDevice is DevicesObjectGroupNode objectNode)
                    objectNode.IncrementCount();

                var deviceNode = CreateDeviceNode(context, parentForDevice, dev);
                parentForDevice.AddChild(deviceNode);
            }

            foreach (var typeNode in typeNodes)
            {
                foreach (var objectNode in typeNode.Items.OfType<DevicesObjectGroupNode>())
                    objectNode.UpdateHeader();
                typeNode.UpdateHeader();

                if (counts[typeNode.TypeKey] > 0)
                    root.AddChild(typeNode);
            }

            root.SetDeviceCount(counts.Values.Sum());
        }

        private static void BuildObjectThenType(
            DevicesRoot root,
            IDevicesViewModel context,
            IEnumerable<IODevice> devices)
        {
            var objectNodes = new Dictionary<string, DevicesObjectGroupNode>();
            var typeNodesByObject = new Dictionary<string, Dictionary<string, DevicesTypeGroupNode>>();

            foreach (var dev in devices)
            {
                var objectKey = GetObjectKey(dev);
                var objectDisplay = GetObjectDisplay(dev);
                if (!objectNodes.TryGetValue(objectKey, out var objectNode))
                {
                    objectNode = new DevicesObjectGroupNode(context, root, objectKey, objectDisplay);
                    objectNodes[objectKey] = objectNode;
                    typeNodesByObject[objectKey] = new Dictionary<string, DevicesTypeGroupNode>();
                    root.AddChild(objectNode);
                }

                objectNode.IncrementCount();

                var typeKey = GetTypeKey(dev);
                if (!typeNodesByObject[objectKey].TryGetValue(typeKey, out var typeNode))
                {
                    typeNode = new DevicesTypeGroupNode(context, objectNode, typeKey, GetTypeTag(dev));
                    typeNodesByObject[objectKey][typeKey] = typeNode;
                    objectNode.AddChild(typeNode);
                }

                typeNode.IncrementCount();
                var deviceNode = CreateDeviceNode(context, typeNode, dev);
                typeNode.AddChild(deviceNode);
            }

            foreach (var objectNode in objectNodes.Values)
                objectNode.UpdateHeader();

            foreach (var types in typeNodesByObject.Values)
            {
                foreach (var typeNode in types.Values)
                    typeNode.UpdateHeader();
            }

            root.SetDeviceCount(devices.Count());
        }

        private static List<DevicesTypeGroupNode> CreateTypeNodeCatalog(
            DevicesRoot root,
            IDevicesViewModel context)
        {
            var nodes = new List<DevicesTypeGroupNode>();
            foreach (DeviceType devType in Enum.GetValues(typeof(DeviceType)))
            {
                nodes.Add(new DevicesTypeGroupNode(context, root, devType.ToString(), devType));
            }

            foreach (var virt in VirtSubTypes)
            {
                var tag = (DeviceSubType)Enum.Parse(typeof(DeviceSubType), virt);
                nodes.Add(new DevicesTypeGroupNode(context, root, virt, tag));
            }

            return nodes;
        }

        private static DevicesTypeGroupNode ResolveTypeNode(
            List<DevicesTypeGroupNode> typeNodes,
            IODevice dev)
        {
            var subTypeStr = dev.GetDeviceSubTypeStr(dev.DeviceType, dev.DeviceSubType);
            if (VirtSubTypes.Contains(subTypeStr))
            {
                return typeNodes.FirstOrDefault(n => n.TypeKey == subTypeStr);
            }

            return typeNodes.FirstOrDefault(n =>
                n.Tag is DeviceType dt && dt == dev.DeviceType);
        }

        private static FilterableViewItemBase ResolveObjectParent(
            DevicesTypeGroupNode typeNode,
            IODevice dev)
        {
            if (string.IsNullOrEmpty(dev.ObjectName))
                return typeNode;

            var objectKey = GetObjectKey(dev);
            var existing = typeNode.Items
                .OfType<DevicesObjectGroupNode>()
                .FirstOrDefault(n => n.ObjectKey == objectKey);

            if (existing is not null)
                return existing;

            var objectNode = new DevicesObjectGroupNode(
                typeNode.Context, typeNode, objectKey, GetObjectDisplay(dev));
            typeNode.AddChild(objectNode);
            return objectNode;
        }

        private static DevicesDeviceNode CreateDeviceNode(
            IDevicesViewModel context,
            FilterableViewItemBase parent,
            IODevice dev)
        {
            var description = GenerateDeviceDescription(dev);
            string displayName;
            if (!string.IsNullOrEmpty(dev.ObjectName))
            {
                var eplanNameParts = dev.EplanName.Split('-');
                displayName = $"{eplanNameParts[eplanNameParts.Length - 1]}\t {description}";
            }
            else
                displayName = $"{dev.Name}\t  {description}";

            return new DevicesDeviceNode(context, parent, dev, displayName.Trim());
        }

        private static string GenerateDeviceDescription(IODevice dev) =>
            EplanMultilineText.FormatForCell(GetEplanDescription(dev));

        internal static string GetEplanDescription(IODevice device)
        {
            if (device?.Function != null)
                return device.Function.Description ?? string.Empty;

            return device?.Description ?? string.Empty;
        }

        private static string GetObjectKey(IODevice dev) =>
            string.IsNullOrEmpty(dev.ObjectName)
                ? "__no_object__"
                : dev.ObjectName + dev.ObjectNumber;

        private static string GetObjectDisplay(IODevice dev) =>
            string.IsNullOrEmpty(dev.ObjectName)
                ? "Без объекта"
                : dev.ObjectName + dev.ObjectNumber;

        private static string GetTypeKey(IODevice dev)
        {
            var subTypeStr = dev.GetDeviceSubTypeStr(dev.DeviceType, dev.DeviceSubType);
            return VirtSubTypes.Contains(subTypeStr) ? subTypeStr : dev.DeviceType.ToString();
        }

        private static object GetTypeTag(IODevice dev)
        {
            var subTypeStr = dev.GetDeviceSubTypeStr(dev.DeviceType, dev.DeviceSubType);
            return VirtSubTypes.Contains(subTypeStr)
                ? (DeviceSubType)Enum.Parse(typeof(DeviceSubType), subTypeStr)
                : dev.DeviceType;
        }
    }
}
