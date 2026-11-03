using EasyEPlanner.Binding.ViewModel;
using EasyEPlanner.Devices.ViewModel;
using EasyEPlanner.Devices.ViewModel.ViewInterface;
using Editor;
using EplanDevice;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using TechObject;
using TechObject.AttachedObjectStrategy;

namespace EasyEPlanner.Binding.Tests
{
    public class BindingViewModelTest
    {
        [TearDown]
        public void TearDown()
        {
            ResetDeviceManager();
            ResetTechObjectManager();
        }

        [Test]
        public void ShowSignalBinding_DefaultGroupingIsTypeThenObject()
        {
            var context = new BindingViewModel(null);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(BindingMode.SignalBinding, context.Mode);
                Assert.AreEqual(DevicesGroupingMode.TypeThenObject, context.GroupingMode);
                Assert.IsTrue(context.GroupingToggleVisible);
                Assert.IsTrue(context.HideBoundChannelsVisible);
                Assert.IsTrue(context.HideBoundChannels);
                Assert.IsFalse(context.CheckBoxesEnabled);
                Assert.AreEqual("Устройства проекта (0)", context.Root.Name);
            });
        }

        [Test]
        public void SignalTree_TypeThenObject_GroupsByTypeAndObject()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);

            var root = (BindingRoot)context.Root;
            var typeNode = root.Items.OfType<BindingTypeGroupNode>().Single();
            Assert.AreEqual("DO (1)", typeNode.Name);

            var objectNode = typeNode.Items.OfType<BindingObjectGroupNode>().Single();
            Assert.AreEqual("TANK2 (1)", objectNode.Name);

            var deviceNode = objectNode.Items.OfType<BindingDeviceNode>().Single();
            Assert.AreSame(device, deviceNode.Device);
            Assert.IsNotEmpty(deviceNode.Items.OfType<BindingChannelItem>());
        }

        [Test]
        public void SignalTree_ObjectThenType_GroupsByObjectAndType()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            context.GroupingMode = DevicesGroupingMode.ObjectThenType;
            context.RebuildTree();

            var root = (BindingRoot)context.Root;
            var objectNode = root.Items.OfType<BindingObjectGroupNode>().Single();
            var typeNode = objectNode.Items.OfType<BindingTypeGroupNode>().Single();
            var deviceNode = typeNode.Items.OfType<BindingDeviceNode>().Single();

            Assert.AreEqual("TANK2 (1)", objectNode.Name);
            Assert.AreEqual("DO (1)", typeNode.Name);
            Assert.AreSame(device, deviceNode.Device);
        }

        [Test]
        public void SignalTree_SkipsDevicesWithoutChannels()
        {
            var withSignals = CreateTankDoDevice();
            var withoutSignals = new DO("TANK2DO2", "+TANK2-DO2", "virt", 2, "TANK", 2);
            withoutSignals.SetSubType("DO_VIRT");
            var context = CreateContextWithDevices(withSignals, withoutSignals);

            var devices = BindingCheckHelper
                .Enumerate((BindingFilterableViewItemBase)context.Root)
                .OfType<BindingDeviceNode>()
                .Select(d => d.Device)
                .ToArray();

            CollectionAssert.AreEqual(new[] { withSignals }, devices);
            Assert.IsFalse(devices.Any(d => d.DeviceSubType == DeviceSubType.DO_VIRT));
        }

        [Test]
        public void EditorDevicesTree_AlwaysObjectThenType_WithoutChannels()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            var item = MockDevListItem(new[] { DeviceType.DO }, displayParameters: false,
                checkedValue: string.Empty);

            context.ShowEditorBinding(item, rebuildTree: true);

            Assert.AreEqual(BindingMode.ObjectBinding, context.Mode);
            Assert.AreEqual(BindingContentKind.Devices, context.ContentKind);
            Assert.IsTrue(context.CheckBoxesEnabled);
            Assert.IsFalse(context.GroupingToggleVisible);

            var root = (BindingRoot)context.Root;
            var objectNode = root.Items.OfType<BindingObjectGroupNode>().Single();
            var typeNode = objectNode.Items.OfType<BindingTypeGroupNode>().Single();
            var deviceNode = typeNode.Items.OfType<BindingDeviceNode>().Single();

            Assert.AreSame(device, deviceNode.Device);
            Assert.IsEmpty(deviceNode.Items.OfType<BindingChannelItem>());
        }

        [Test]
        public void EditorDevicesTree_FiltersByDeviceType()
        {
            var doDevice = CreateTankDoDevice();
            var aiDevice = CreateTankAiDevice();
            var context = CreateContextWithDevices(doDevice, aiDevice);
            var item = MockDevListItem(new[] { DeviceType.AI }, displayParameters: false,
                checkedValue: string.Empty);

            context.ShowEditorBinding(item, rebuildTree: true);

            var devices = BindingCheckHelper
                .Enumerate((BindingFilterableViewItemBase)context.Root)
                .OfType<BindingDeviceNode>()
                .Select(d => d.Device)
                .ToArray();

            CollectionAssert.AreEqual(new[] { aiDevice }, devices);
        }

        [Test]
        public void ObjectGroupPriority_PreferredEplanNameFirstThenBoundDevices()
        {
            var preferred = new BindingObjectGroupNode(new BindingViewModel(null),
                null, "TANK1", "TANK", 1, "TANK1");
            var bound = new BindingObjectGroupNode(new BindingViewModel(null),
                null, "OTHER2", "OTHER", 2, "OTHER2");
            bound.AddChild(new BindingDeviceNode(bound.Context, bound,
                CreateOtherDoDevice(), "DO1"));
            var rest = new BindingObjectGroupNode(new BindingViewModel(null),
                null, "ZZZ1", "ZZZ", 1, "ZZZ1");

            Assert.AreEqual(0, BindingTreeBuilder.GetObjectGroupPriority(
                preferred, "TANK", 1, new HashSet<string> { "OTHER2DO1" }));
            Assert.AreEqual(2, BindingTreeBuilder.GetObjectGroupPriority(
                bound, "TANK", 1, new HashSet<string> { "OTHER2DO1" }));
            Assert.AreEqual(3, BindingTreeBuilder.GetObjectGroupPriority(
                rest, "TANK", 1, new HashSet<string> { "OTHER2DO1" }));
        }

        [Test]
        public void Checkboxes_CascadeToChildrenAndIndeterminateParents()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            var item = MockDevListItem(new[] { DeviceType.DO }, displayParameters: false,
                checkedValue: string.Empty);
            context.ShowEditorBinding(item, rebuildTree: true);

            var root = (BindingRoot)context.Root;
            var objectNode = root.Items.OfType<BindingObjectGroupNode>().Single();
            context.SetItemCheckState(objectNode, CheckState.Checked);

            var deviceNode = BindingCheckHelper.Enumerate(objectNode)
                .OfType<BindingDeviceNode>().Single();
            Assert.AreEqual(CheckState.Checked, deviceNode.CheckState);
            Assert.AreEqual(CheckState.Checked, objectNode.CheckState);

            context.SetItemCheckState(deviceNode, CheckState.Unchecked);
            Assert.AreEqual(CheckState.Unchecked, deviceNode.CheckState);
            Assert.AreEqual(CheckState.Unchecked, objectNode.CheckState);
        }

        [Test]
        public void Checkboxes_CanUncheckPreviouslyCheckedItem()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            var item = MockDevListItem(new[] { DeviceType.DO }, displayParameters: false,
                checkedValue: string.Empty);
            context.ShowEditorBinding(item, rebuildTree: true);

            var deviceNode = BindingCheckHelper
                .Enumerate((BindingFilterableViewItemBase)context.Root)
                .OfType<BindingDeviceNode>()
                .Single();

            context.SetItemCheckState(deviceNode, CheckState.Checked);
            Assert.AreEqual(CheckState.Checked, deviceNode.CheckState);

            context.SetItemCheckState(deviceNode, CheckState.Indeterminate);
            Assert.AreEqual(CheckState.Unchecked, deviceNode.CheckState);
        }

        [Test]
        public void Checkboxes_TwoDevicesInSameGroup_BothStayChecked()
        {
            var first = new DO("TANK2DO1", "+TANK2-DO1", "desc", 1, "TANK", 2);
            first.SetSubType("DO");
            var second = new DO("TANK2DO2", "+TANK2-DO2", "desc", 2, "TANK", 2);
            second.SetSubType("DO");
            var context = CreateContextWithDevices(first, second);
            var item = MockDevListItem(new[] { DeviceType.DO }, displayParameters: false,
                checkedValue: string.Empty);
            context.ShowEditorBinding(item, rebuildTree: true);

            var devices = BindingCheckHelper
                .Enumerate((BindingFilterableViewItemBase)context.Root)
                .OfType<BindingDeviceNode>()
                .ToArray();

            context.SetItemCheckState(devices[0], CheckState.Checked);
            context.SetItemCheckState(devices[1], CheckState.Checked);

            Assert.AreEqual(CheckState.Checked, devices[0].CheckState);
            Assert.AreEqual(CheckState.Checked, devices[1].CheckState);
        }

        [Test]
        public void AttachedAggregatesToUnit_AllowsOnlyOneObjectPerGroup()
        {
            var context = new BindingViewModel(null);
            context.ShowEmpty();
            SetPrivate(context, "ContentKind", BindingContentKind.AttachedObjects);
            SetPrivate(context, "AttachedEditType",
                BindingAttachedEditType.AttachedAgregatesToUnit);

            var root = (BindingRoot)context.Root;
            var mixers = new BindingFolderNode(context, root, "Узел перемешивания");
            var heaters = new BindingFolderNode(context, root, "Узел нагревания");
            var mixer1 = new BindingTechObjectNode(context, mixers, null, 1, "Mixer 1");
            var mixer2 = new BindingTechObjectNode(context, mixers, null, 2, "Mixer 2");
            var heater1 = new BindingTechObjectNode(context, heaters, null, 3, "Heater 1");
            root.AddChild(mixers);
            root.AddChild(heaters);
            mixers.AddChild(mixer1);
            mixers.AddChild(mixer2);
            heaters.AddChild(heater1);

            context.SetItemCheckState(mixer1, CheckState.Checked);
            context.SetItemCheckState(heater1, CheckState.Checked);
            context.SetItemCheckState(mixer2, CheckState.Checked);

            Assert.AreEqual(CheckState.Unchecked, mixer1.CheckState);
            Assert.AreEqual(CheckState.Checked, mixer2.CheckState);
            Assert.AreEqual(CheckState.Checked, heater1.CheckState);
        }

        [Test]
        public void AttachedAggregatesToUnit_DoesNotCheckWholeGroup()
        {
            var context = new BindingViewModel(null);
            context.ShowEmpty();
            SetPrivate(context, "ContentKind", BindingContentKind.AttachedObjects);
            SetPrivate(context, "AttachedEditType",
                BindingAttachedEditType.AttachedAgregatesToUnit);

            var root = (BindingRoot)context.Root;
            var mixers = new BindingFolderNode(context, root, "Узел перемешивания");
            var mixer1 = new BindingTechObjectNode(context, mixers, null, 1, "Mixer 1");
            var mixer2 = new BindingTechObjectNode(context, mixers, null, 2, "Mixer 2");
            root.AddChild(mixers);
            mixers.AddChild(mixer1);
            mixers.AddChild(mixer2);

            context.SetItemCheckState(mixers, CheckState.Checked);

            Assert.AreEqual(CheckState.Unchecked, mixer1.CheckState);
            Assert.AreEqual(CheckState.Unchecked, mixer2.CheckState);
            Assert.AreEqual(CheckState.Unchecked, mixers.CheckState);
        }

        [Test]
        public void Checkboxes_IndeterminateOnUncheckedParent_DoesNotCheckAllChildren()
        {
            var first = new DO("TANK2DO1", "+TANK2-DO1", "desc", 1, "TANK", 2);
            first.SetSubType("DO");
            var second = new DO("TANK2DO2", "+TANK2-DO2", "desc", 2, "TANK", 2);
            second.SetSubType("DO");
            var context = CreateContextWithDevices(first, second);
            var item = MockDevListItem(new[] { DeviceType.DO }, displayParameters: false,
                checkedValue: string.Empty);
            context.ShowEditorBinding(item, rebuildTree: true);

            var devices = BindingCheckHelper
                .Enumerate((BindingFilterableViewItemBase)context.Root)
                .OfType<BindingDeviceNode>()
                .ToArray();
            var typeNode = devices[0].ParentItem;

            Assert.AreEqual(CheckState.Unchecked, typeNode.CheckState);
            context.SetItemCheckState(typeNode, CheckState.Indeterminate);

            Assert.AreEqual(CheckState.Indeterminate, typeNode.CheckState);
            Assert.AreEqual(CheckState.Unchecked, devices[0].CheckState);
            Assert.AreEqual(CheckState.Unchecked, devices[1].CheckState);
        }

        [Test]
        public void ShowEmpty_DoesNotRebuildWhenAlreadyEmpty()
        {
            var context = new BindingViewModel(null);
            context.ShowEmpty();
            var firstRoot = context.Root;

            context.ShowEmpty();

            Assert.AreSame(firstRoot, context.Root);
            Assert.IsTrue(context.IsShowingEmptyEditorTree);
        }

        [Test]
        public void ApplyCheckedValues_MarksBoundDevice()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            var item = MockDevListItem(new[] { DeviceType.DO }, displayParameters: false,
                checkedValue: device.Name);

            context.ShowEditorBinding(item, rebuildTree: true);

            var deviceNode = BindingCheckHelper
                .Enumerate((BindingFilterableViewItemBase)context.Root)
                .OfType<BindingDeviceNode>()
                .Single();
            Assert.AreEqual(CheckState.Checked, deviceNode.CheckState);
            Assert.AreEqual(CheckState.Checked, deviceNode.ParentItem.CheckState);
        }

        [Test]
        public void CollectDevicesAndParameters_ReturnsCheckedDeviceNames()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            var item = MockDevListItem(new[] { DeviceType.DO }, displayParameters: false,
                checkedValue: string.Empty);
            context.ShowEditorBinding(item, rebuildTree: true);

            var deviceNode = BindingCheckHelper
                .Enumerate((BindingFilterableViewItemBase)context.Root)
                .OfType<BindingDeviceNode>()
                .Single();
            context.SetItemCheckState(deviceNode, CheckState.Checked);

            Assert.AreEqual(device.Name,
                BindingSelectionCollector.CollectDevicesAndParameters(context.Roots));
        }

        [Test]
        public void CollectRestrictions_BuildsObjectToModesDictionary()
        {
            var context = new BindingViewModel(null);
            var root = BindingTreeBuilder.BuildEmpty(context, "root");
            var tech = new BindingTechObjectNode(context, root, null, 3, "TANK1");
            var mode = new BindingModeNode(context, tech, new Mode("Операция 1", getN => 1, null), 3, 2);
            root.AddChild(tech);
            tech.AddChild(mode);
            mode.SetCheckStateInternal(CheckState.Checked);

            var dict = BindingSelectionCollector.CollectRestrictions(new IBindingRoot[] { root });
            CollectionAssert.AreEqual(new[] { 2 }, dict[3]);
        }

        [Test]
        public void CollectAttachedObjects_UsesTechObjectNumbers()
        {
            var context = new BindingViewModel(null);
            var root = BindingTreeBuilder.BuildEmpty(context, "root");
            var tech = new BindingTechObjectNode(context, root, null, 7, "TANK1");
            root.AddChild(tech);
            tech.SetCheckStateInternal(CheckState.Checked);

            var dict = BindingSelectionCollector.CollectAttachedObjects(new IBindingRoot[] { root });
            CollectionAssert.AreEqual(new[] { 7 }, dict.Keys.ToArray());
        }

        [Test]
        public void Filter_FindsDeviceByEplanName()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            var root = (BindingRoot)context.Root;
            root.ResetFilter();

            Assert.IsTrue(root.Filter("+TANK2-DO1", hideEmptyItems: false));
            Assert.IsTrue(context.SearchContext.FoundItems
                .OfType<BindingDeviceNode>().Any());
        }

        [Test]
        public void HideBoundChannels_HidesBoundChannelAndParentDevice()
        {
            var device = CreateTankDoDevice();
            BindFirstChannel(device);
            var context = CreateContextWithDevices(device);
            context.HideBoundChannels = true;

            var root = (BindingRoot)context.Root;
            root.ResetFilter();
            Assert.IsTrue(root.Filter(string.Empty, hideEmptyItems: false));

            var channelNode = BindingCheckHelper.Enumerate(root)
                .OfType<BindingChannelItem>().Single();
            var deviceNode = BindingCheckHelper.Enumerate(root)
                .OfType<BindingDeviceNode>().Single();

            Assert.IsFalse(channelNode.Filtered.Value);
            Assert.IsFalse(deviceNode.Filtered.Value);
        }

        [Test]
        public void HideBoundChannels_KeepsUnboundChannel()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            context.HideBoundChannels = true;

            var root = (BindingRoot)context.Root;
            root.ResetFilter();
            Assert.IsTrue(root.Filter(string.Empty, hideEmptyItems: false));

            var channelNode = BindingCheckHelper.Enumerate(root)
                .OfType<BindingChannelItem>().Single();
            Assert.IsTrue(channelNode.Filtered.Value);
        }

        [Test]
        public void HideBoundChannels_AfterLiveBind_HidesChannelAndShowsClamp()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            context.HideBoundChannels = true;

            var root = (BindingRoot)context.Root;
            root.ResetFilter();
            Assert.IsTrue(root.Filter(string.Empty, hideEmptyItems: false));

            var channelNode = BindingCheckHelper.Enumerate(root)
                .OfType<BindingChannelItem>().Single();
            Assert.IsTrue(channelNode.Filtered.Value);
            Assert.AreEqual(string.Empty, channelNode.Description);

            BindFirstChannel(device);
            root.ResetFilter();
            root.Filter(string.Empty, hideEmptyItems: false);

            Assert.IsFalse(channelNode.Filtered.Value);
            Assert.AreEqual("A101:5", channelNode.Description);
        }

        [Test]
        public void HideBoundChannels_AfterUnbind_ShowsChannelWithoutClamp()
        {
            var device = CreateTankDoDevice();
            BindFirstChannel(device);
            var context = CreateContextWithDevices(device);
            context.HideBoundChannels = true;

            var root = (BindingRoot)context.Root;
            root.ResetFilter();
            root.Filter(string.Empty, hideEmptyItems: false);

            var channelNode = BindingCheckHelper.Enumerate(root)
                .OfType<BindingChannelItem>().Single();
            Assert.IsFalse(channelNode.Filtered.Value);

            device.Channels[0].Clear();
            root.ResetFilter();
            root.Filter(string.Empty, hideEmptyItems: false);

            Assert.IsTrue(channelNode.Filtered.Value);
            Assert.AreEqual(string.Empty, channelNode.Description);
        }

        [Test]
        public void HideBoundChannels_WhenDisabled_ShowsBoundChannel()
        {
            var device = CreateTankDoDevice();
            BindFirstChannel(device);
            var context = CreateContextWithDevices(device);
            context.HideBoundChannels = false;

            var root = (BindingRoot)context.Root;
            root.ResetFilter();
            var channelNode = BindingCheckHelper.Enumerate(root)
                .OfType<BindingChannelItem>().Single();

            Assert.IsTrue(channelNode.Filter(string.Empty, hideEmptyItems: false));
        }

        [Test]
        public void HideBoundChannels_VisibleOnlyInSignalMode()
        {
            var context = new BindingViewModel(null);
            Assert.IsTrue(context.HideBoundChannelsVisible);

            context.ShowEmpty();
            Assert.IsFalse(context.HideBoundChannelsVisible);
        }

        [Test]
        public void ResolveContentKind_MapsEditorItems()
        {
            Assert.AreEqual(BindingContentKind.None,
                BindingViewModel.ResolveContentKind(null));

            var devices = MockDevListItem(new[] { DeviceType.V }, displayParameters: false,
                checkedValue: string.Empty);
            Assert.AreEqual(BindingContentKind.Devices,
                BindingViewModel.ResolveContentKind(devices));

            var parameters = MockDevListItem(new DeviceType[0], displayParameters: true,
                checkedValue: string.Empty);
            Assert.AreEqual(BindingContentKind.Parameters,
                BindingViewModel.ResolveContentKind(parameters));

            var both = MockDevListItem(new[] { DeviceType.V }, displayParameters: true,
                checkedValue: string.Empty);
            Assert.AreEqual(BindingContentKind.DevicesAndParameters,
                BindingViewModel.ResolveContentKind(both));
        }

        [Test]
        public void ParseBoundNames_SplitsBySpace()
        {
            CollectionAssert.AreEquivalent(
                new[] { "V1", "V2" },
                BindingViewModel.ParseBoundNames(" V1  V2 "));
        }

        [Test]
        public void MatchesTypeFilter_NullTypesMeansAll()
        {
            var device = CreateTankDoDevice();
            Assert.IsTrue(BindingTreeBuilder.MatchesTypeFilter(device, null, null));
            Assert.IsFalse(BindingTreeBuilder.MatchesTypeFilter(device,
                new[] { DeviceType.AI }, null));
            Assert.IsFalse(BindingTreeBuilder.MatchesTypeFilter(device, null,
                new[] { DeviceSubType.AI }));
            Assert.IsTrue(BindingTreeBuilder.MatchesTypeFilter(device, null,
                new[] { DeviceSubType.DO }));
        }

        [Test]
        public void HasBindableSignals_NullDevice_ReturnsFalse()
        {
            Assert.IsFalse(BindingTreeBuilder.HasBindableSignals(null));
        }

        [Test]
        public void ParseBoundNames_Empty_ReturnsEmptySet()
        {
            Assert.IsEmpty(BindingViewModel.ParseBoundNames(null));
            Assert.IsEmpty(BindingViewModel.ParseBoundNames("   "));
        }

        [Test]
        public void ResolveContentKind_RestrictionAndAttachedObjects()
        {
            Assert.AreEqual(BindingContentKind.Operations,
                BindingViewModel.ResolveContentKind(new Restriction(
                    "r", "", "lua", new SortedDictionary<int, List<int>>())));

            var attached = new AttachedObjects("", null,
                new AttachedWithoutInitStrategy("Привязанные", "attached",
                    new List<BaseTechObjectManager.ObjectType>()));
            Assert.AreEqual(BindingContentKind.AttachedObjects,
                BindingViewModel.ResolveContentKind(attached));

            var unused = new Mock<ITreeViewItem>();
            unused.Setup(m => m.IsUseDevList).Returns(false);
            Assert.AreEqual(BindingContentKind.None,
                BindingViewModel.ResolveContentKind(unused.Object));
        }

        [Test]
        public void ResolveContentKind_NullTypesWithParameters_IsDevicesAndParameters()
        {
            var item = MockDevListItem(null, displayParameters: true,
                checkedValue: string.Empty);
            Assert.AreEqual(BindingContentKind.DevicesAndParameters,
                BindingViewModel.ResolveContentKind(item));
        }

        [Test]
        public void WalkToTechObject_FindsParentOrReturnsNull()
        {
            Assert.IsNull(WalkToTechObject(null));

            var tech = CreateTechObject("Танк", 1);
            Assert.AreSame(tech, WalkToTechObject(tech));

            var child = new Mock<ITreeViewItem>();
            child.Setup(m => m.Parent).Returns(tech);
            Assert.AreSame(tech, WalkToTechObject(child.Object));
        }

        [Test]
        public void ShowEditorBinding_DoesNotRebuildWhenTypesUnchanged()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            var item = MockDevListItem(new[] { DeviceType.DO }, false, string.Empty);

            context.ShowEditorBinding(item, rebuildTree: true);
            var firstRoot = context.Root;

            context.ShowEditorBinding(item, rebuildTree: false);
            Assert.AreSame(firstRoot, context.Root);
        }

        [Test]
        public void ShowEditorBinding_NoneKind_BuildsEmptyTree()
        {
            var context = new BindingViewModel(null);
            var unused = new Mock<ITreeViewItem>();
            unused.Setup(m => m.IsUseDevList).Returns(false);

            context.ShowEditorBinding(unused.Object, rebuildTree: true);

            Assert.AreEqual(BindingContentKind.None, context.ContentKind);
            Assert.AreEqual("Привязка", context.Root.Name);
        }

        [Test]
        public void ShowEditorBinding_ActionParameter_EnablesSingleSelect()
        {
            var context = new BindingViewModel(null);
            context.ShowEditorBinding(new ActionParameter("p", "Параметр"), true);
            Assert.IsTrue(context.SingleSelect);
        }

        [Test]
        public void ShowEditorBinding_AttachedEditTypes()
        {
            var unitBase = new BaseTechObject
            {
                EplanName = "TANK",
                S88Level = (int)BaseTechObjectManager.ObjectType.Unit,
            };
            var unit = CreateTechObject("Аппарат", 1, unitBase);
            var context = new BindingViewModel(null);

            context.ShowEditorBinding(unit.AttachedObjects, true);
            Assert.AreEqual(BindingAttachedEditType.AttachedAgregatesToUnit,
                context.AttachedEditType);

            var aggregateBase = new BaseTechObject
            {
                EplanName = "MIX",
                S88Level = (int)BaseTechObjectManager.ObjectType.Aggregate,
            };
            var aggregate = CreateTechObject("Агрегат", 2, aggregateBase);
            context.ShowEditorBinding(aggregate.AttachedObjects, true);
            Assert.AreEqual(BindingAttachedEditType.AttachedAggregatesToAggregates,
                context.AttachedEditType);

            var withoutOwner = new AttachedObjects("", null,
                new AttachedWithoutInitStrategy("Привязанные", "attached",
                    new List<BaseTechObjectManager.ObjectType>()));
            context.ShowEditorBinding(withoutOwner, true);
            Assert.AreEqual(BindingAttachedEditType.AttachedObjectToStep,
                context.AttachedEditType);
            Assert.IsTrue(context.SingleSelect);

            var withOwner = new AttachedObjects("", unit,
                new AttachedWithoutInitStrategy("Объекты", "objects",
                    new List<BaseTechObjectManager.ObjectType>()));
            context.ShowEditorBinding(withOwner, true);
            Assert.AreEqual(BindingAttachedEditType.AttachedUnitsToObjectGroup,
                context.AttachedEditType);
        }

        [Test]
        public void ShowEditorBinding_Restriction_ChecksMatchingModes()
        {
            var tech = CreateTechObject("Танк 1", 1);
            tech.ModesManager.AddMode("Мойка", "");
            TechObjectManager.GetInstance().ImportObject(tech);

            int objectNumber = TechObjectManager.GetInstance().GetTechObjectN(tech);
            int modeNumber = tech.ModesManager.Modes[0].GetModeNumber();
            var dict = new SortedDictionary<int, List<int>>();
            dict[objectNumber] = new List<int> { modeNumber };
            var restriction = new Restriction("r", "", "lua", dict);

            var context = new BindingViewModel(null);
            context.ShowEditorBinding(restriction, true);

            Assert.AreEqual(BindingContentKind.Operations, context.ContentKind);
            Assert.AreEqual(BindingAttachedEditType.Restriction,
                context.AttachedEditType);

            var modeNode = BindingCheckHelper
                .Enumerate((BindingFilterableViewItemBase)context.Root)
                .OfType<BindingModeNode>()
                .Single();
            Assert.AreEqual(CheckState.Checked, modeNode.CheckState);
        }

        [Test]
        public void ApplyCheckedValues_MarksBoundParameterByLuaNameAndNumber()
        {
            var context = new BindingViewModel(null);
            context.ShowEmpty();
            SetPrivate(context, "ContentKind", BindingContentKind.Parameters);

            var parameters = new Params("Параметры", "par", false, "P");
            var param = parameters.AddParam(new Param(parameters.GetIdx,
                "Температура", false, 0, "шт", "T"));
            var root = (BindingRoot)context.Root;
            var node = new BindingParameterNode(context, root, param);
            root.AddChild(node);

            SetPrivate(context, "SelectedItem",
                MockDevListItem(new DeviceType[0], true, "T"));
            context.ApplyCheckedValues();
            Assert.AreEqual(CheckState.Checked, node.CheckState);

            node.SetCheckStateInternal(CheckState.Unchecked);
            SetPrivate(context, "SelectedItem",
                MockDevListItem(new DeviceType[0], true,
                    param.GetParameterNumber.ToString()));
            context.ApplyCheckedValues();
            Assert.AreEqual(CheckState.Checked, node.CheckState);
        }

        [Test]
        public void ApplyAttachedObjectChecks_MarksTechObjectByNumber()
        {
            var first = CreateTechObject("Танк 1", 1);
            var second = CreateTechObject("Танк 2", 2);
            TechObjectManager.GetInstance().ImportObject(first);
            TechObjectManager.GetInstance().ImportObject(second);
            int objectNumber = TechObjectManager.GetInstance()
                .GetTechObjectN(second);
            var attached = new AttachedObjects(objectNumber.ToString(), first,
                new AttachedWithoutInitStrategy("Объекты", "objects",
                    new List<BaseTechObjectManager.ObjectType>()));

            var context = new BindingViewModel(null);
            context.ShowEditorBinding(attached, true);

            var node = BindingCheckHelper
                .Enumerate((BindingFilterableViewItemBase)context.Root)
                .OfType<BindingTechObjectNode>()
                .Single(n => n.CheckState == CheckState.Checked);
            Assert.AreEqual(objectNumber, node.ObjectNumber);
        }

        [Test]
        public void RebuildTree_ObjectBindingAndUnknownMode()
        {
            var context = new BindingViewModel(null);
            context.ShowEmpty();
            context.RebuildTree();
            Assert.AreEqual("Привязка", context.Root.Name);
            Assert.AreEqual(BindingContentKind.None, context.ContentKind);

            SetPrivate(context, "Mode", (BindingMode)99);
            context.RebuildTree();
            Assert.AreEqual("Привязка", context.Root.Name);
        }

        [Test]
        public void ApplyCheckedValues_WithoutSelectedItem_UnchecksAll()
        {
            var context = new BindingViewModel(null);
            context.ShowEmpty();
            ((BindingFilterableViewItemBase)context.Root)
                .SetCheckStateInternal(CheckState.Checked);

            context.ApplyCheckedValues();
            Assert.AreEqual(CheckState.Unchecked,
                ((BindingFilterableViewItemBase)context.Root).CheckState);
        }

        [Test]
        public void SetItemCheckState_IgnoresWhenDisabledOrNull()
        {
            var context = new BindingViewModel(null);
            var root = (BindingRoot)context.Root;
            root.SetCheckStateInternal(CheckState.Unchecked);

            context.SetItemCheckState(null, CheckState.Checked);
            context.SetItemCheckState(root, CheckState.Checked);

            Assert.AreEqual(CheckState.Unchecked, root.CheckState);
        }

        [Test]
        public void SetItemCheckState_SingleSelect_UnchecksPrevious()
        {
            var first = new DO("TANK2DO1", "+TANK2-DO1", "desc", 1, "TANK", 2);
            first.SetSubType("DO");
            var second = new DO("TANK2DO2", "+TANK2-DO2", "desc", 2, "TANK", 2);
            second.SetSubType("DO");
            var context = CreateContextWithDevices(first, second);
            var item = MockDevListItem(new[] { DeviceType.DO }, false, string.Empty);
            context.ShowEditorBinding(item, true);
            SetPrivate(context, "SingleSelect", true);

            var devices = BindingCheckHelper
                .Enumerate((BindingFilterableViewItemBase)context.Root)
                .OfType<BindingDeviceNode>()
                .ToArray();

            context.SetItemCheckState(devices[0], CheckState.Checked);
            context.SetItemCheckState(devices[1], CheckState.Checked);

            Assert.AreEqual(CheckState.Unchecked, devices[0].CheckState);
            Assert.AreEqual(CheckState.Checked, devices[1].CheckState);
        }

        [Test]
        public void NotifyEditor_InvokesStringAndDictCallbacks()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            var item = MockDevListItem(new[] { DeviceType.DO }, false, string.Empty);
            context.ShowEditorBinding(item, true);

            string captured = null;
            context.OnSetStringValue = value => captured = value;
            var deviceNode = BindingCheckHelper
                .Enumerate((BindingFilterableViewItemBase)context.Root)
                .OfType<BindingDeviceNode>()
                .Single();
            context.SetItemCheckState(deviceNode, CheckState.Checked);
            Assert.AreEqual(device.Name, captured);

            context.ShowEmpty();
            SetPrivate(context, "ContentKind", BindingContentKind.Operations);
            IDictionary<int, List<int>> dict = null;
            context.OnSetDictValue = value => dict = value;
            var mode = new BindingModeNode(context, (BindingRoot)context.Root,
                new Mode("Операция 1", getN => 1, null), 3, 2);
            ((BindingRoot)context.Root).AddChild(mode);
            context.SetItemCheckState(mode, CheckState.Checked);
            CollectionAssert.AreEqual(new[] { 2 }, dict[3]);

            SetPrivate(context, "ContentKind", BindingContentKind.AttachedObjects);
            var tech = new BindingTechObjectNode(context,
                (BindingRoot)context.Root, null, 8, "TANK");
            ((BindingRoot)context.Root).AddChild(tech);
            context.SetItemCheckState(tech, CheckState.Checked);
            CollectionAssert.AreEqual(new[] { 8 }, dict.Keys.ToArray());
        }

        [Test]
        public void CollectRestrictions_MergesSortsAndSkipsDuplicates()
        {
            var context = new BindingViewModel(null);
            var root = BindingTreeBuilder.BuildEmpty(context, "root");
            var tech = new BindingTechObjectNode(context, root, null, 3, "TANK1");
            var modeA = new BindingModeNode(context, tech,
                new Mode("A", getN => 1, null), 3, 2);
            var modeB = new BindingModeNode(context, tech,
                new Mode("B", getN => 2, null), 3, 1);
            var modeDuplicate = new BindingModeNode(context, tech,
                new Mode("A2", getN => 3, null), 3, 2);
            root.AddChild(tech);
            tech.AddChild(modeA);
            tech.AddChild(modeB);
            tech.AddChild(modeDuplicate);
            modeA.SetCheckStateInternal(CheckState.Checked);
            modeB.SetCheckStateInternal(CheckState.Checked);
            modeDuplicate.SetCheckStateInternal(CheckState.Checked);

            var dict = BindingSelectionCollector.CollectRestrictions(
                new IBindingRoot[] { root });
            CollectionAssert.AreEqual(new[] { 1, 2 }, dict[3]);
        }

        [Test]
        public void CollectDevicesAndParameters_IncludesCheckedParameterLuaName()
        {
            var context = new BindingViewModel(null);
            var root = BindingTreeBuilder.BuildEmpty(context, "root");
            var parameters = new Params("Параметры", "par", false, "P");
            var param = parameters.AddParam(new Param(parameters.GetIdx,
                "Температура", false, 0, "шт", "T"));
            var node = new BindingParameterNode(context, root, param);
            root.AddChild(node);
            node.SetCheckStateInternal(CheckState.Checked);

            Assert.AreEqual("T",
                BindingSelectionCollector.CollectDevicesAndParameters(
                    new IBindingRoot[] { root }));
        }

        [Test]
        public void UncheckSiblings_NullParent_DoesNothing()
        {
            var context = new BindingViewModel(null);
            Assert.DoesNotThrow(() => BindingCheckHelper.UncheckSiblings(
                (BindingFilterableViewItemBase)context.Root));
        }

        [Test]
        public void Filter_UsesCachedValueAndHidesEmptyGroupsOnSearch()
        {
            var device = CreateTankDoDevice();
            BindFirstChannel(device);
            var context = CreateContextWithDevices(device);
            context.HideBoundChannels = true;
            var root = (BindingRoot)context.Root;

            root.ResetFilter();
            bool first = root.Filter("NOMATCH", false);
            bool second = root.Filter("OTHER", false);
            Assert.AreEqual(first, second);
            Assert.IsFalse(first);

            root.ResetFilter();
            Assert.IsFalse(root.Filter("NOMATCH", false));
        }

        [Test]
        public void SignalTree_DeviceWithoutObject_GoesToNoObjectGroup()
        {
            var device = new DO("DO1", "+KO-DO1", "desc", 1, "", 0);
            device.SetSubType("DO");
            var context = CreateContextWithDevices(device);
            context.GroupingMode = DevicesGroupingMode.ObjectThenType;
            context.RebuildTree();

            var objectNode = ((BindingRoot)context.Root).Items
                .OfType<BindingObjectGroupNode>().Single();
            Assert.AreEqual("__no_object__", objectNode.ObjectKey);
            Assert.AreEqual("Без объекта", objectNode.DisplayName);
        }

        [Test]
        public void EditorDevicesTree_SortsPreferredThenBoundObjectGroups()
        {
            var preferred = new DO("TANK1DO1", "+TANK1-DO1", "desc", 1, "TANK", 1);
            preferred.SetSubType("DO");
            var bound = CreateOtherDoDevice();
            var rest = new DO("ZZZ1DO1", "+ZZZ1-DO1", "desc", 1, "ZZZ", 1);
            rest.SetSubType("DO");
            var context = CreateContextWithDevices(rest, bound, preferred);
            var item = MockDevListItem(new[] { DeviceType.DO }, false, bound.Name);

            context.ShowEditorBinding(item, true);
            var groups = ((BindingRoot)context.Root).Items
                .OfType<BindingObjectGroupNode>()
                .Select(g => g.ObjectKey)
                .ToArray();
            CollectionAssert.AreEqual(new[] { "OTHER2", "TANK1", "ZZZ1" }, groups);
        }

        [Test]
        public void ObjectGroupPriority_SameEplanNameWrongNumber_IsSecond()
        {
            var group = new BindingObjectGroupNode(new BindingViewModel(null),
                null, "TANK2", "TANK", 2, "TANK2");
            Assert.AreEqual(1, BindingTreeBuilder.GetObjectGroupPriority(
                group, "TANK", 1, new HashSet<string>()));
        }

        [Test]
        public void BuildParametersTree_NullAndFilled()
        {
            var context = new BindingViewModel(null);
            Assert.AreEqual("Параметры объекта (0)",
                BindingTreeBuilder.BuildParametersTree(context, null).Name);

            var parameters = new Params("Параметры", "par", false, "P");
            parameters.AddParam(new Param(parameters.GetIdx, "Температура",
                false, 0, "шт", "T"));
            var root = BindingTreeBuilder.BuildParametersTree(context, parameters);
            Assert.AreEqual("Параметры объекта (1)", root.Name);
            Assert.AreEqual("T",
                root.Items.OfType<BindingParameterNode>().Single().LuaName);
        }

        [Test]
        public void BuildOperationsTree_HidesCurrentObjectAndEmptyModes()
        {
            var first = CreateTechObject("Танк 1", 1);
            first.ModesManager.AddMode("Мойка", "");
            var empty = CreateTechObject("Пустой", 2);
            TechObjectManager.GetInstance().ImportObject(first);
            TechObjectManager.GetInstance().ImportObject(empty);

            var context = new BindingViewModel(null);
            SetPrivate(context, "ContentKind", BindingContentKind.Operations);

            var root = BindingTreeBuilder.BuildOperationsTree(
                context, TechObjectManager.GetInstance(), first, null, false);

            var objects = BindingCheckHelper.Enumerate(root)
                .OfType<BindingTechObjectNode>()
                .ToArray();
            Assert.IsEmpty(objects);

            root = BindingTreeBuilder.BuildOperationsTree(
                context, TechObjectManager.GetInstance(), first, null, true);
            objects = BindingCheckHelper.Enumerate(root)
                .OfType<BindingTechObjectNode>()
                .ToArray();
            Assert.AreEqual(1, objects.Length);
            Assert.AreSame(first, objects[0].TechObject);
            Assert.IsTrue(objects[0].Name.Contains("{2}"));
        }

        [Test]
        public void BuildOperationsTree_SkipsRestrictionOwnerMode()
        {
            var tech = CreateTechObject("Танк 1", 1);
            tech.ModesManager.AddMode("Мойка", "");
            tech.ModesManager.AddMode("Наполнение", "");
            var mode = tech.ModesManager.Modes[0];
            mode.AddParent(tech.ModesManager);
            tech.ModesManager.AddParent(tech);
            var restriction = new Restriction("r", "", "lua",
                new SortedDictionary<int, List<int>>());
            restriction.AddParent(mode.GetRestrictionManager());
            mode.GetRestrictionManager().AddParent(mode);

            TechObjectManager.GetInstance().ImportObject(tech);
            var context = new BindingViewModel(null);
            SetPrivate(context, "ContentKind", BindingContentKind.Operations);

            var root = BindingTreeBuilder.BuildOperationsTree(
                context, TechObjectManager.GetInstance(), null, restriction,
                false);
            var skippedName = mode.Name;
            var remaining = BindingCheckHelper.Enumerate(root)
                .OfType<BindingModeNode>()
                .Select(m => m.Mode.Name)
                .ToArray();
            CollectionAssert.DoesNotContain(remaining, skippedName);
            Assert.IsTrue(remaining.Any());
        }

        [Test]
        public void BuildAttachedObjectsTree_KeepsObjectsWithoutModes()
        {
            var tech = CreateTechObject("Танк 1", 1);
            TechObjectManager.GetInstance().ImportObject(tech);
            var context = new BindingViewModel(null);
            SetPrivate(context, "ContentKind", BindingContentKind.AttachedObjects);

            var root = BindingTreeBuilder.BuildAttachedObjectsTree(
                context, null, null, false);
            Assert.AreEqual("Объекты проекта", root.Name);

            root = BindingTreeBuilder.BuildAttachedObjectsTree(
                context, TechObjectManager.GetInstance(), tech, false);
            Assert.IsEmpty(BindingCheckHelper.Enumerate(root)
                .OfType<BindingTechObjectNode>());

            root = BindingTreeBuilder.BuildAttachedObjectsTree(
                context, TechObjectManager.GetInstance(), null, false);
            Assert.AreEqual(1, BindingCheckHelper.Enumerate(root)
                .OfType<BindingTechObjectNode>().Count());
        }

        [Test]
        public void BuildOperationsTree_NullManager_UsesDefaultTitle()
        {
            var context = new BindingViewModel(null);
            var root = BindingTreeBuilder.BuildOperationsTree(
                context, null, null, null, false);
            Assert.AreEqual("Операции проекта", root.Name);
        }

        [Test]
        public void TreeItemIconsAndSearchableText()
        {
            var context = new BindingViewModel(null);
            var root = (BindingRoot)context.Root;
            var device = CreateTankDoDevice();
            var typeGroup = new BindingTypeGroupNode(context, root, "DO", DeviceType.DO);
            var objectGroup = new BindingObjectGroupNode(context, typeGroup,
                "TANK2", "TANK", 2, "TANK2");
            var deviceNode = new BindingDeviceNode(context, objectGroup, device,
                "DO1");
            var folder = new BindingFolderNode(context, deviceNode, "folder");
            var channel = new BindingChannelItem(context, folder, device.Channels[0]);
            var techObject = CreateTechObject("Танк", 1);
            var techNode = new BindingTechObjectNode(context, root, techObject,
                1, "Танк 1");
            var parameters = new Params("Параметры", "par", false, "P");
            var param = parameters.AddParam(new Param(parameters.GetIdx,
                "Температура", false, 0, "шт", "T"));
            var paramNode = new BindingParameterNode(context, root, param);
            var modeNode = new BindingModeNode(context, techNode,
                new Mode("Операция 1", getN => 1, null), 1, 1);

            Assert.AreEqual(DevicesIcon.Root, root.Icon);
            Assert.AreEqual(DevicesIcon.Type, typeGroup.Icon);
            Assert.AreEqual(DevicesIcon.Object, objectGroup.Icon);
            Assert.AreEqual(DevicesIcon.Device, deviceNode.Icon);
            Assert.AreEqual(DevicesIcon.Channel, channel.Icon);
            Assert.AreEqual(DevicesIcon.Parameters, paramNode.Icon);
            Assert.AreEqual(DevicesIcon.Object, techNode.Icon);
            Assert.AreEqual(DevicesIcon.Data, modeNode.Icon);
            Assert.AreEqual(DevicesIcon.Type, folder.Icon);
            Assert.AreSame(device, channel.Device);
            Assert.IsFalse(channel.CanCheck);
            Assert.AreEqual(DevicesIcon.None, channel.DescriptionIcon);
            Assert.IsTrue(objectGroup.GetSearchableText().Contains("TANK2"));
            Assert.IsTrue(deviceNode.GetSearchableText().Contains(device.EplanName));
            Assert.IsTrue(paramNode.GetSearchableText().Contains("T"));
            Assert.IsTrue(techNode.GetSearchableText().Contains("TANK"));

            BindFirstChannel(device);
            Assert.AreEqual(DevicesIcon.Clamp, channel.DescriptionIcon);
        }

        private static ITreeViewItem MockDevListItem(
            DeviceType[] types,
            bool displayParameters,
            string checkedValue)
        {
            var mock = new Mock<ITreeViewItem>();
            mock.Setup(m => m.IsUseDevList).Returns(true);
            mock.Setup(m => m.EditText).Returns(new[] { "", checkedValue });
            DeviceType[] typesOut = types;
            DeviceSubType[] subOut = null;
            bool displayOut = displayParameters;
            mock.Setup(m => m.GetDisplayObjects(out typesOut, out subOut, out displayOut));
            return mock.Object;
        }

        private static DO CreateTankDoDevice()
        {
            var device = new DO("TANK2DO1", "+TANK2-DO1", "desc", 1, "TANK", 2);
            device.SetSubType("DO");
            return device;
        }

        private static void BindFirstChannel(IODevice device)
        {
            device.Channels[0].SetChannel(0, 1, 5, 101, 0, 0);
        }

        private static DO CreateOtherDoDevice()
        {
            var device = new DO("OTHER2DO1", "+OTHER2-DO1", "desc", 1, "OTHER", 2);
            device.SetSubType("DO");
            return device;
        }

        private static AI CreateTankAiDevice()
        {
            var device = new AI("TANK2AI1", "+TANK2-AI1", "desc", 1, "TANK", 2);
            device.SetSubType("AI");
            return device;
        }

        private static TechObject.TechObject WalkToTechObject(ITreeViewItem item)
        {
            return (TechObject.TechObject)typeof(BindingViewModel)
                .GetMethod("WalkToTechObject",
                    BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { item });
        }

        private static TechObject.TechObject CreateTechObject(string name, int n,
            BaseTechObject baseTechObject = null)
        {
            return new TechObject.TechObject(name, getN => n, n, 2, "TANK", -1,
                name, "", baseTechObject ?? new BaseTechObject { EplanName = "TANK" });
        }

        private static void ResetTechObjectManager()
        {
            var instance = typeof(TechObjectManager).GetField("instance",
                BindingFlags.NonPublic | BindingFlags.Static);
            instance.SetValue(null, null);
        }

        private static BindingViewModel CreateContextWithDevices(params IODevice[] devices)
        {
            var manager = DeviceManager.GetInstance();
            manager.Devices.Clear();
            foreach (var device in devices)
                manager.Devices.Add(device);

            return new BindingViewModel(manager);
        }

        private static void ResetDeviceManager()
        {
            var instance = typeof(DeviceManager).GetField("instance",
                BindingFlags.NonPublic | BindingFlags.Static);
            instance.SetValue(null, null);
        }

        private static void SetPrivate(object obj, string propertyName, object value)
        {
            obj.GetType().GetProperty(propertyName)
                .GetSetMethod(true)
                .Invoke(obj, new[] { value });
        }
    }
}
