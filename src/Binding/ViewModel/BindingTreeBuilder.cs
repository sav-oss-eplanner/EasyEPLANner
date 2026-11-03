using EasyEPlanner.Devices.ViewModel;
using Editor;
using EplanDevice;
using IO.ViewModel;
using StaticHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using TechObject;

namespace EasyEPlanner.Binding.ViewModel
{
    public static class BindingTreeBuilder
    {
        private static readonly string[] VirtSubTypes =
        [
            "AI_VIRT",
            "AO_VIRT",
            "DI_VIRT",
            "DO_VIRT",
        ];

        public static BindingRoot BuildEmpty(IBindingViewModel context, string name)
        {
            return new BindingRoot(context, name);
        }

        public static BindingRoot BuildSignalTree(IBindingViewModel context)
        {
            var root = new BindingRoot(context, "Устройства проекта");
            var devices = (context.DeviceManager?.Devices ?? [])
                .Where(HasBindableSignals);
            if (context.GroupingMode is DevicesGroupingMode.ObjectThenType)
                BuildObjectThenType(root, context, devices, includeChannels: true);
            else
                BuildTypeThenObject(root, context, devices, includeChannels: true);
            return root;
        }

        public static BindingRoot BuildDevicesTree(
            IBindingViewModel context,
            IEnumerable<IODevice> devices,
            DeviceType[] types,
            DeviceSubType[] subTypes,
            string preferredNameEplan,
            int preferredTechNumber,
            IReadOnlyCollection<string> boundDeviceNames)
        {
            var root = new BindingRoot(context, "Устройства проекта");
            var filtered = devices
                .Where(dev => MatchesTypeFilter(dev, types, subTypes))
                .ToList();
            BuildObjectThenType(root, context, filtered, includeChannels: false);
            SortObjectGroups(root, preferredNameEplan, preferredTechNumber,
                boundDeviceNames);
            return root;
        }

        public static BindingRoot BuildParametersTree(
            IBindingViewModel context,
            Params parameters)
        {
            var root = new BindingRoot(context, "Параметры объекта");
            if (parameters?.Items is null)
            {
                root.SetName("Параметры объекта (0)");
                return root;
            }

            var luaNames = parameters.Items.OfType<Param>()
                .Select(p => p.GetNameLua())
                .Distinct()
                .ToList();

            foreach (var name in luaNames)
            {
                var param = parameters.GetParam(name);
                if (param is null)
                    continue;
                root.AddChild(new BindingParameterNode(context, root, param));
            }

            root.SetName($"Параметры объекта ({luaNames.Count})");
            return root;
        }

        public static BindingRoot BuildOperationsTree(
            IBindingViewModel context,
            TechObjectManager techManager,
            TechObject.TechObject mainTechObject,
            Restriction restriction,
            bool showOneNode)
        {
            var root = new BindingRoot(context,
                techManager?.DisplayText?[0] ?? "Операции проекта");
            if (techManager?.Items is null)
                return root;

            FillTreeObjects(techManager.Items, root, new FillTreeArgs
            {
                Context = context,
                MainTechObject = mainTechObject,
                Restriction = restriction,
                ShowOneNode = showOneNode,
                IncludeModes = true,
            });
            return root;
        }

        public static BindingRoot BuildAttachedObjectsTree(
            IBindingViewModel context,
            TechObjectManager techManager,
            TechObject.TechObject mainTechObject,
            bool showOneNode)
        {
            var root = new BindingRoot(context,
                techManager?.DisplayText?[0] ?? "Объекты проекта");
            if (techManager?.Items is null)
                return root;

            FillTreeObjects(techManager.Items, root, new FillTreeArgs
            {
                Context = context,
                MainTechObject = mainTechObject,
                ShowOneNode = showOneNode,
                IncludeModes = false,
            });
            return root;
        }

        public static bool MatchesTypeFilter(
            IODevice dev,
            DeviceType[] types,
            DeviceSubType[] subTypes)
        {
            if (types is { Length: > 0 } && !types.Contains(dev.DeviceType))
                return false;

            if (subTypes is { Length: > 0 } &&
                !subTypes.Contains(dev.DeviceSubType))
                return false;

            return true;
        }

        public static bool HasBindableSignals(IODevice dev) =>
            dev?.Channels is { Count: > 0 };

        public static int GetObjectGroupPriority(
            BindingObjectGroupNode group,
            string preferredNameEplan,
            int preferredTechNumber,
            IReadOnlyCollection<string> boundDeviceNames)
        {
            if (!string.IsNullOrEmpty(preferredNameEplan) &&
                string.Equals(group.ObjectName, preferredNameEplan,
                    StringComparison.OrdinalIgnoreCase))
            {
                if (preferredTechNumber > 0 &&
                    group.ObjectNumber == preferredTechNumber)
                    return 0;
                return 1;
            }

            if (boundDeviceNames is { Count: > 0 } &&
                BindingCheckHelper.Enumerate(group)
                    .OfType<BindingDeviceNode>()
                    .Any(d => boundDeviceNames.Contains(d.Device.Name)))
            {
                return 2;
            }

            return 3;
        }

        internal static void SortObjectGroups(
            BindingRoot root,
            string preferredNameEplan,
            int preferredTechNumber,
            IReadOnlyCollection<string> boundDeviceNames)
        {
            var groups = root.Items.OfType<BindingObjectGroupNode>().ToList();
            if (groups.Count <= 1)
                return;

            var ordered = groups
                .OrderBy(g => GetObjectGroupPriority(g, preferredNameEplan,
                    preferredTechNumber, boundDeviceNames))
                .ThenBy(g => g.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Cast<IViewItem>()
                .ToList();

            root.SetChildren(ordered);
        }

        private static void BuildTypeThenObject(
            BindingRoot root,
            IBindingViewModel context,
            IEnumerable<IODevice> devices,
            bool includeChannels)
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
                if (parentForDevice is BindingObjectGroupNode objectNode)
                    objectNode.IncrementCount();

                parentForDevice.AddChild(
                    CreateDeviceNode(context, parentForDevice, dev, includeChannels));
            }

            int total = 0;
            foreach (var typeNode in typeNodes)
            {
                foreach (var objectNode in typeNode.Items.OfType<BindingObjectGroupNode>())
                    objectNode.UpdateHeader();
                typeNode.UpdateHeader();

                if (counts[typeNode.TypeKey] > 0)
                {
                    root.AddChild(typeNode);
                    total += counts[typeNode.TypeKey];
                }
            }

            root.SetName($"Устройства проекта ({total})");
        }

        private static void BuildObjectThenType(
            BindingRoot root,
            IBindingViewModel context,
            IEnumerable<IODevice> devices,
            bool includeChannels)
        {
            var objectNodes = new Dictionary<string, BindingObjectGroupNode>();
            var typeNodesByObject =
                new Dictionary<string, Dictionary<string, BindingTypeGroupNode>>();
            int total = 0;

            foreach (var dev in devices)
            {
                total++;
                var objectKey = GetObjectKey(dev);
                if (!objectNodes.TryGetValue(objectKey, out var objectNode))
                {
                    objectNode = new BindingObjectGroupNode(
                        context, root, objectKey, dev.ObjectName ?? string.Empty,
                        dev.ObjectNumber, GetObjectDisplay(dev));
                    objectNodes[objectKey] = objectNode;
                    typeNodesByObject[objectKey] = [];
                    root.AddChild(objectNode);
                }

                objectNode.IncrementCount();

                var typeKey = GetTypeKey(dev);
                if (!typeNodesByObject[objectKey].TryGetValue(typeKey, out var typeNode))
                {
                    typeNode = new BindingTypeGroupNode(
                        context, objectNode, typeKey, GetTypeTag(dev));
                    typeNodesByObject[objectKey][typeKey] = typeNode;
                    objectNode.AddChild(typeNode);
                }

                typeNode.IncrementCount();
                typeNode.AddChild(CreateDeviceNode(context, typeNode, dev, includeChannels));
            }

            foreach (var objectNode in objectNodes.Values)
                objectNode.UpdateHeader();

            foreach (var types in typeNodesByObject.Values)
            {
                foreach (var typeNode in types.Values)
                    typeNode.UpdateHeader();
            }

            root.SetName($"Устройства проекта ({total})");
        }

        private sealed class FillTreeArgs
        {
            public IBindingViewModel Context { get; set; }

            public TechObject.TechObject MainTechObject { get; set; }

            public Restriction Restriction { get; set; }

            public bool ShowOneNode { get; set; }

            public bool IncludeModes { get; set; }
        }

        private static void FillTreeObjects(
            ITreeViewItem[] treeItems,
            BindingFilterableViewItemBase parent,
            FillTreeArgs args)
        {
            if (treeItems is null)
                return;

            var manager = TechObjectManager.GetInstance();
            foreach (var treeItem in treeItems)
            {
                if (treeItem is TechObject.TechObject techObject)
                {
                    AddTechObjectNode(parent, techObject, manager, args);
                    continue;
                }

                var folder = new BindingFolderNode(args.Context, parent,
                    treeItem.DisplayText[0]);
                parent.AddChild(folder);
                FillTreeObjects(treeItem.Items ?? [], folder, args);
            }

            RemoveEmptyFolders(parent);
        }

        private static void AddTechObjectNode(
            BindingFilterableViewItemBase parent,
            TechObject.TechObject techObject,
            TechObjectManager manager,
            FillTreeArgs args)
        {
            bool hide = args.ShowOneNode
                ? techObject != args.MainTechObject
                : techObject == args.MainTechObject;
            if (hide)
                return;

            int objectNumber = manager.GetTechObjectN(techObject);
            var node = new BindingTechObjectNode(args.Context, parent,
                techObject, objectNumber, FormatTechObjectName(techObject));
            parent.AddChild(node);

            if (!args.IncludeModes)
                return;

            foreach (var mode in techObject.ModesManager.Modes)
            {
                if (args.Restriction is not null &&
                    IsSameRestrictionOwner(args.Restriction, techObject, mode))
                    continue;

                node.AddChild(new BindingModeNode(args.Context, node, mode,
                    objectNumber, mode.GetModeNumber()));
            }
        }

        private static void RemoveEmptyFolders(BindingFilterableViewItemBase parent)
        {
            var kept = parent.Items
                .OfType<BindingFilterableViewItemBase>()
                .Where(child =>
                {
                    RemoveEmptyFolders(child);
                    if (child is BindingFolderNode && !child.Items.Any())
                        return false;
                    if (child is BindingTechObjectNode && !child.Items.Any() &&
                        child.Context.ContentKind is BindingContentKind.Operations)
                        return false;
                    return true;
                })
                .Cast<IViewItem>()
                .ToList();
            parent.SetChildren(kept);
        }

        private static bool IsSameRestrictionOwner(
            Restriction restriction,
            TechObject.TechObject techObject,
            Mode mode)
        {
            var selectedMode = restriction.Parent?.Parent as Mode;
            var selectedTechObject = selectedMode?.Parent?.Parent as TechObject.TechObject;
            if (selectedMode is null || selectedTechObject is null)
                return false;

            return techObject.DisplayText[0] == selectedTechObject.DisplayText[0]
                && mode.Name == selectedMode.Name;
        }

        private static string FormatTechObjectName(TechObject.TechObject techObject)
        {
            string name = techObject.DisplayText[0];
            if (techObject.TechType != -1)
                name += $" {{{techObject.TechType}}}";
            return name;
        }

        private static BindingDeviceNode CreateDeviceNode(
            IBindingViewModel context,
            BindingFilterableViewItemBase parent,
            IODevice dev,
            bool includeChannels)
        {
            var description = GenerateDeviceDescription(dev);
            string displayName;
            if (!string.IsNullOrEmpty(dev.ObjectName))
            {
                var eplanNameParts = dev.EplanName.Split('-');
                displayName = $"{eplanNameParts[eplanNameParts.Length - 1]}\t {description}";
            }
            else
            {
                displayName = $"{dev.Name}\t  {description}";
            }

            var deviceNode = new BindingDeviceNode(context, parent, dev, displayName.Trim());
            if (includeChannels)
            {
                foreach (var channel in dev.Channels ?? [])
                    deviceNode.AddChild(new BindingChannelItem(context, deviceNode, channel));
            }

            return deviceNode;
        }

        private static string GenerateDeviceDescription(IODevice dev) =>
            EplanMultilineText.FormatForCell(GetEplanDescription(dev));

        private static string GetEplanDescription(IODevice device)
        {
            if (device?.Function != null)
                return device.Function.Description ?? string.Empty;

            return device?.Description ?? string.Empty;
        }

        private static List<BindingTypeGroupNode> CreateTypeNodeCatalog(
            BindingRoot root,
            IBindingViewModel context)
        {
            var nodes = new List<BindingTypeGroupNode>();
            foreach (DeviceType devType in Enum.GetValues(typeof(DeviceType)))
                nodes.Add(new BindingTypeGroupNode(context, root, devType.ToString(), devType));

            foreach (var virt in VirtSubTypes)
            {
                var tag = (DeviceSubType)Enum.Parse(typeof(DeviceSubType), virt);
                nodes.Add(new BindingTypeGroupNode(context, root, virt, tag));
            }

            return nodes;
        }

        private static BindingTypeGroupNode ResolveTypeNode(
            List<BindingTypeGroupNode> typeNodes,
            IODevice dev)
        {
            var subTypeStr = dev.GetDeviceSubTypeStr(dev.DeviceType, dev.DeviceSubType);
            if (VirtSubTypes.Contains(subTypeStr))
                return typeNodes.FirstOrDefault(n => n.TypeKey == subTypeStr);

            return typeNodes.FirstOrDefault(n =>
                n.Tag is DeviceType dt && dt == dev.DeviceType);
        }

        private static BindingFilterableViewItemBase ResolveObjectParent(
            BindingTypeGroupNode typeNode,
            IODevice dev)
        {
            if (string.IsNullOrEmpty(dev.ObjectName))
                return typeNode;

            var objectKey = GetObjectKey(dev);
            var existing = typeNode.Items
                .OfType<BindingObjectGroupNode>()
                .FirstOrDefault(n => n.ObjectKey == objectKey);
            if (existing is not null)
                return existing;

            var objectNode = new BindingObjectGroupNode(
                typeNode.Context, typeNode, objectKey, dev.ObjectName,
                dev.ObjectNumber, GetObjectDisplay(dev));
            typeNode.AddChild(objectNode);
            return objectNode;
        }

        internal static string GetObjectKey(IODevice dev) =>
            string.IsNullOrEmpty(dev.ObjectName)
                ? "__no_object__"
                : dev.ObjectName + dev.ObjectNumber;

        internal static string GetObjectDisplay(IODevice dev) =>
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
