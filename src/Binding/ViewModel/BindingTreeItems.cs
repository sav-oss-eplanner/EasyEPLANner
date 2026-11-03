using EasyEPlanner.Devices.ViewModel.ViewInterface;
using EplanDevice;
using IO;
using StaticHelper;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TechObject;

namespace EasyEPlanner.Binding.ViewModel
{
    public sealed class BindingRoot : BindingFilterableViewItemBase, IBindingRoot, IBoldName
    {
        public BindingRoot(IBindingViewModel context, string name)
            : base(context, null)
        {
            Name = name;
        }

        public override string Name { get; protected set; }

        public void SetName(string name) => Name = name;

        public override DevicesIcon Icon => DevicesIcon.Root;

        protected override bool StayVisibleWhenNoMatchingChildren => true;
    }

    public sealed class BindingTypeGroupNode : BindingFilterableViewItemBase, IBoldName
    {
        private int deviceCount;

        public BindingTypeGroupNode(
            IBindingViewModel context,
            BindingFilterableViewItemBase parent,
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

    public sealed class BindingObjectGroupNode : BindingFilterableViewItemBase, IBoldName
    {
        private int deviceCount;

        public BindingObjectGroupNode(
            IBindingViewModel context,
            BindingFilterableViewItemBase parent,
            string objectKey,
            string objectName,
            int objectNumber,
            string displayName)
            : base(context, parent)
        {
            ObjectKey = objectKey;
            ObjectName = objectName;
            ObjectNumber = objectNumber;
            DisplayName = displayName;
            Name = displayName;
        }

        public string ObjectKey { get; }

        public string ObjectName { get; }

        public int ObjectNumber { get; }

        public string DisplayName { get; }

        public override string Name { get; protected set; }

        public override DevicesIcon Icon => DevicesIcon.Object;

        public void IncrementCount() => deviceCount++;

        public void UpdateHeader() => Name = $"{DisplayName} ({deviceCount})";

        public override string GetSearchableText() =>
            $"{DisplayName} {ObjectKey} {Name} {Description}".Trim();
    }

    public sealed class BindingDeviceNode : BindingFilterableViewItemBase, IBoldName, IGoToFas
    {
        public BindingDeviceNode(
            IBindingViewModel context,
            BindingFilterableViewItemBase parent,
            IODevice device,
            string displayName)
            : base(context, parent)
        {
            Device = device;
            Name = displayName;
        }

        public IODevice Device { get; }

        public override string Name { get; protected set; }

        public override DevicesIcon Icon => DevicesIcon.Device;

        [ExcludeFromCodeCoverage]
        public IEplanFunction EplanFunction => Device.Function;

        public override string GetSearchableText()
        {
            var parts = new[]
            {
                Name,
                Description,
                Device.Name,
                Device.EplanName,
                Device.ObjectName + Device.ObjectNumber,
            };
            return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }
    }

    public sealed class BindingChannelItem : BindingFilterableViewItemBase, IO.ViewModel.IToolTip,
        IHasDevicesDescriptionIcon, IGoToFas
    {
        public BindingChannelItem(
            IBindingViewModel context,
            BindingFilterableViewItemBase parent,
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
                    if (node is BindingDeviceNode deviceNode)
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

        string IO.ViewModel.IToolTip.Name => Name;

        public override DevicesIcon Icon => DevicesIcon.Channel;

        public DevicesIcon DescriptionIcon =>
            Channel.IsEmpty() ? DevicesIcon.None : DevicesIcon.Clamp;

        public override bool CanCheck => false;

        protected override bool IsHiddenByBoundFilter() =>
            Context?.HideBoundChannels == true &&
            Context.Mode is BindingMode.SignalBinding &&
            !Channel.IsEmpty();

        [ExcludeFromCodeCoverage]
        private IEplanFunction ResolveClampEplanFunction()
        {
            if (Channel.IsEmpty())
                return null;

            try
            {
                var module = IOManager.GetInstance()
                    .GetModuleByPhysicalNumber(Channel.FullModule);
                return module.ClampFunctions.TryGetValue(Channel.PhysicalClamp,
                    out var clampFunction)
                    ? clampFunction
                    : null;
            }
            catch
            {
                return null;
            }
        }
    }

    public sealed class BindingParameterNode : BindingFilterableViewItemBase
    {
        public BindingParameterNode(
            IBindingViewModel context,
            BindingFilterableViewItemBase parent,
            Param param)
            : base(context, parent)
        {
            Param = param;
            LuaName = param.GetNameLua();
            Name = $"{param.GetParameterNumber}. {LuaName}";
        }

        public Param Param { get; }

        public string LuaName { get; }

        public override string Name { get; protected set; }

        public override DevicesIcon Icon => DevicesIcon.Parameters;

        public override string GetSearchableText() =>
            $"{Name} {LuaName} {Param.GetName()}".Trim();
    }

    public sealed class BindingTechObjectNode : BindingFilterableViewItemBase, IBoldName
    {
        public BindingTechObjectNode(
            IBindingViewModel context,
            BindingFilterableViewItemBase parent,
            TechObject.TechObject techObject,
            int objectNumber,
            string displayName)
            : base(context, parent)
        {
            TechObject = techObject;
            ObjectNumber = objectNumber;
            Name = displayName;
        }

        public TechObject.TechObject TechObject { get; }

        public int ObjectNumber { get; }

        public override string Name { get; protected set; }

        public override DevicesIcon Icon => DevicesIcon.Object;

        public override string GetSearchableText() =>
            $"{Name} {TechObject?.NameEplan}".Trim();
    }

    public sealed class BindingModeNode : BindingFilterableViewItemBase
    {
        public BindingModeNode(
            IBindingViewModel context,
            BindingFilterableViewItemBase parent,
            Mode mode,
            int objectNumber,
            int modeNumber)
            : base(context, parent)
        {
            Mode = mode;
            ObjectNumber = objectNumber;
            ModeNumber = modeNumber;
            Name = mode.DisplayText[0];
        }

        public Mode Mode { get; }

        public int ObjectNumber { get; }

        public int ModeNumber { get; }

        public override string Name { get; protected set; }

        public override DevicesIcon Icon => DevicesIcon.Data;
    }

    public sealed class BindingFolderNode : BindingFilterableViewItemBase, IBoldName
    {
        public BindingFolderNode(
            IBindingViewModel context,
            BindingFilterableViewItemBase parent,
            string name)
            : base(context, parent)
        {
            Name = name;
        }

        public override string Name { get; protected set; }

        public override DevicesIcon Icon => DevicesIcon.Type;
    }
}
