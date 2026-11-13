using EasyEPlanner.Devices.ViewModel;
using EasyEPlanner.Devices.ViewModel.ViewInterface;
using EplanDevice;
using IO.ViewModel;
using Moq;
using NUnit.Framework;
using StaticHelper;
using System.Linq;
using System.Reflection;

namespace EasyEPlanner.Devices.Tests
{
    public class DevicesTreeItemsTest
    {
        [TearDown]
        public void TearDown()
        {
            ResetDeviceManager();
        }

        [Test]
        public void TypeGroupNode_UpdateHeader_IncludesDeviceCount()
        {
            var context = new DevicesViewModel(null);
            var typeNode = new DevicesTypeGroupNode(context,
                (FilterableViewItemBase)context.Root, "DO", DeviceType.DO);

            typeNode.IncrementCount();
            typeNode.IncrementCount();
            typeNode.UpdateHeader();

            Assert.AreEqual("DO (2)", typeNode.Name);
            Assert.AreEqual(DevicesIcon.Type, typeNode.Icon);
        }

        [Test]
        public void ObjectGroupNode_UpdateHeader_AndSearchableText()
        {
            var context = new DevicesViewModel(null);
            var objectNode = new DevicesObjectGroupNode(context,
                (FilterableViewItemBase)context.Root, "TANK2", "TANK2");

            objectNode.IncrementCount();
            objectNode.UpdateHeader();

            Assert.AreEqual("TANK2 (1)", objectNode.Name);
            StringAssert.Contains("TANK2", objectNode.GetSearchableText());
            Assert.AreEqual(DevicesIcon.Object, objectNode.Icon);
        }

        [Test]
        public void DeviceNode_BuildGroups_ChildrenHaveGroupParent()
        {
            var device = CreateTankDoDevice();
            var context = new DevicesViewModel(null);
            var deviceNode = new DevicesDeviceNode(context,
                (FilterableViewItemBase)context.Root, device, device.Name);

            var groupNames = deviceNode.Items.OfType<DevicesGroupNode>()
                .Select(g => g.Name).ToArray();
            CollectionAssert.AreEquivalent(
                new[] { "Данные", "Каналы" }, groupNames);

            var dataGroup = deviceNode.Items.OfType<DevicesGroupNode>()
                .Single(g => g.Name == "Данные");
            var subtypeItem = dataGroup.Items.OfType<DevicesSubtypeItem>().Single();
            Assert.AreSame(dataGroup, subtypeItem.ParentItem);

            var channelsGroup = deviceNode.Items.OfType<DevicesGroupNode>()
                .Single(g => g.Name == "Каналы");
            var channelItem = channelsGroup.Items.OfType<DevicesChannelItem>().Single();
            Assert.AreSame(channelsGroup, channelItem.ParentItem);
            Assert.AreSame(device, channelItem.Device);
        }

        [Test]
        public void TreeBuilder_TypeThenObject_GroupsByTypeAndObject()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);

            var root = (DevicesRoot)context.Root;
            Assert.AreEqual("Устройства проекта (1)", root.Name);

            var typeNode = root.Items.OfType<DevicesTypeGroupNode>().Single();
            Assert.AreEqual("DO (1)", typeNode.Name);

            var objectNode = typeNode.Items.OfType<DevicesObjectGroupNode>().Single();
            Assert.AreEqual("TANK2 (1)", objectNode.Name);

            var deviceNode = objectNode.Items.OfType<DevicesDeviceNode>().Single();
            Assert.AreSame(device, deviceNode.Device);
        }

        [Test]
        public void TreeBuilder_TypeThenObject_PlacesDeviceWithoutObjectUnderType()
        {
            var device = new DO("STANDALONE1", "+STANDALONE1", "desc", 1, "", 0);
            device.SetSubType("DO");
            var context = CreateContextWithDevices(device);

            var root = (DevicesRoot)context.Root;
            var typeNode = root.Items.OfType<DevicesTypeGroupNode>().Single();
            var deviceNode = typeNode.Items.OfType<DevicesDeviceNode>().Single();

            Assert.IsEmpty(typeNode.Items.OfType<DevicesObjectGroupNode>());
            Assert.AreSame(device, deviceNode.Device);
        }

        [Test]
        public void TreeBuilder_TypeThenObject_OmitsEmptyTypeGroups()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);

            var root = (DevicesRoot)context.Root;
            var typeKeys = root.Items.OfType<DevicesTypeGroupNode>()
                .Select(n => n.TypeKey).ToArray();

            CollectionAssert.AreEqual(new[] { "DO" }, typeKeys);
        }

        [Test]
        public void TreeBuilder_ObjectThenType_GroupsByObjectAndType()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            context.GroupingMode = DevicesGroupingMode.ObjectThenType;
            context.RebuildTree();

            var root = (DevicesRoot)context.Root;
            var objectNode = root.Items.OfType<DevicesObjectGroupNode>().Single();
            Assert.AreEqual("TANK2 (1)", objectNode.Name);

            var typeNode = objectNode.Items.OfType<DevicesTypeGroupNode>().Single();
            Assert.AreEqual("DO (1)", typeNode.Name);

            var deviceNode = typeNode.Items.OfType<DevicesDeviceNode>().Single();
            Assert.AreSame(device, deviceNode.Device);
        }

        [Test]
        public void TreeBuilder_ResolvesVirtSubtypeUnderDedicatedTypeNode()
        {
            var device = new DO("VIRT1", "+VIRT1", "desc", 1, "TANK", 1);
            device.SetSubType("DO_VIRT");
            var context = CreateContextWithDevices(device);

            var root = (DevicesRoot)context.Root;
            var typeNode = root.Items.OfType<DevicesTypeGroupNode>().Single();
            Assert.AreEqual("DO_VIRT (1)", typeNode.Name);
        }

        [Test]
        public void FilterableViewItemBase_Filter_FindsDeviceByEplanName()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);
            var root = (DevicesRoot)context.Root;
            root.ResetFilter();

            Assert.IsTrue(root.Filter("+TANK2-DO1", hideEmptyItems: false));
            Assert.IsTrue(context.SearchContext.FoundItems
                .OfType<DevicesDeviceNode>().Any());
        }

        [Test]
        public void FilterableViewItemBase_ResetFilter_AllowsNewSearch()
        {
            var device = CreateTankDoDevice();
            var context = CreateContextWithDevices(device);

            var root = (DevicesRoot)context.Root;
            Assert.IsTrue(root.Filter("+TANK2-DO1", hideEmptyItems: false));
            root.ResetFilter();

            Assert.IsFalse(root.Filter("ZZZNOMATCH", hideEmptyItems: false));
        }

        [Test]
        public void DevicesDescriptionItem_UsesFunctionDescriptionWhenPresent()
        {
            var device = CreateTankDoDevice();
            device.Function = Mock.Of<IEplanFunction>(f =>
                f.Description == "from eplan");

            var context = new DevicesViewModel(null);
            var deviceNode = new DevicesDeviceNode(context,
                (FilterableViewItemBase)context.Root, device, device.Name);
            var item = new DevicesDescriptionItem(context, deviceNode, device);

            Assert.AreEqual("from eplan", item.Value);
            Assert.AreEqual("from eplan", item.Description);
        }

        [Test]
        public void DevicesDescriptionItem_FallsBackToDeviceDescriptionWithoutFunction()
        {
            var device = CreateTankDoDevice();
            var context = new DevicesViewModel(null);
            var deviceNode = new DevicesDeviceNode(context,
                (FilterableViewItemBase)context.Root, device, device.Name);
            var item = new DevicesDescriptionItem(context, deviceNode, device);

            Assert.AreEqual("desc", item.Value);
        }

        [Test]
        public void DevicesDescriptionItem_FormatsMultilineDescriptionForCell()
        {
            var device = CreateTankDoDevice();
            device.Function = Mock.Of<IEplanFunction>(f =>
                f.Description == "first\u00B6second");

            var context = new DevicesViewModel(null);
            var deviceNode = new DevicesDeviceNode(context,
                (FilterableViewItemBase)context.Root, device, device.Name);
            var item = new DevicesDescriptionItem(context, deviceNode, device);

            Assert.AreEqual("first · second", item.Description);
        }

        [Test]
        public void DevicesDescriptionItem_SetValue_WithoutFunction_UpdatesDevice()
        {
            var device = CreateTankDoDevice();
            var context = new DevicesViewModel(null);
            var deviceNode = new DevicesDeviceNode(context,
                (FilterableViewItemBase)context.Root, device, device.Name);
            var item = new DevicesDescriptionItem(context, deviceNode, device);

            Assert.IsTrue(item.SetValue("new desc"));
            Assert.AreEqual("new desc", device.Description);
            Assert.AreEqual("new desc", item.Value);
        }

        [Test]
        public void DevicesParameterItem_SetValue_WithoutFunction_DoesNotThrow()
        {
            var device = CreateTankAiDevice();
            var parameter = IODevice.Parameter.P_MIN_V;
            var item = CreateParameterItem(device, parameter, "0");

            Assert.IsTrue(item.SetValue("1.5"));
            Assert.AreEqual(1.5, device.Parameters[parameter]);
        }

        [Test]
        public void DevicesPropertyItem_SetValue_UpdatesDescriptionAndEplanFunction()
        {
            var device = CreateTankDoDevice();
            var function = new Mock<IEplanFunction>();
            device.Function = function.Object;
            device.Properties["TestProp"] = "old";

            var context = new DevicesViewModel(null);
            var deviceNode = new DevicesDeviceNode(context,
                (FilterableViewItemBase)context.Root, device, device.Name);
            var item = new DevicesPropertyItem(context, deviceNode, device,
                "TestProp", "old");

            Assert.IsTrue(item.SetValue("new"));
            Assert.AreEqual("new", item.Description);
            Assert.AreEqual("new", device.Properties["TestProp"]);
            function.VerifySet(f => f.Properties = It.IsAny<string>());
        }

        [Test]
        public void DevicesRuntimeParameterItem_SetValue_ParsesInteger()
        {
            var device = CreateTankDoDevice();
            device.Function = Mock.Of<IEplanFunction>();
            device.SetRuntimeParameter("RParam", 5);

            var context = new DevicesViewModel(null);
            var deviceNode = new DevicesDeviceNode(context,
                (FilterableViewItemBase)context.Root, device, device.Name);
            var item = new DevicesRuntimeParameterItem(context, deviceNode, device,
                "RParam", "5");

            Assert.IsFalse(item.SetValue("not-a-number"));
            Assert.IsTrue(item.SetValue("10"));
            Assert.AreEqual("10", item.Description);
        }

        [Test]
        public void DevicesParameterItem_Constructor_ExposesParameterMetadata()
        {
            var device = CreateTankAiDevice();
            var parameter = IODevice.Parameter.P_MIN_V;
            var item = CreateParameterItem(device, parameter, "2.5");

            Assert.AreEqual(parameter.Name, item.Name);
            Assert.AreEqual("2.5", item.Description);
            Assert.AreEqual("2.5", item.Value);
            Assert.AreSame(device, item.Device);
            Assert.AreSame(parameter, item.Parameter);
            Assert.AreEqual(parameter.Description, (item as IToolTip).Name);
            Assert.AreEqual(string.Empty, (item as IToolTip).Description);
        }

        [Test]
        public void DevicesParameterItem_SetValue_ParsesDoubleAndUpdatesDevice()
        {
            var device = CreateTankAiDevice();
            var function = new Mock<IEplanFunction>();
            device.Function = function.Object;
            var parameter = IODevice.Parameter.P_MIN_V;
            var item = CreateParameterItem(device, parameter, "0");

            Assert.IsFalse(item.SetValue("0"));
            Assert.IsFalse(item.SetValue("not-a-number"));
            Assert.IsTrue(item.SetValue("1.5"));
            Assert.AreEqual("1.5", item.Description);
            Assert.AreEqual(1.5, device.Parameters[parameter]);
            function.VerifySet(f => f.Parameters = It.Is<string>(s =>
                s.Contains("P_MIN_V=")));
        }

        [Test]
        public void DeviceNode_BuildGroups_IncludesParametersForAiDevice()
        {
            var device = CreateTankAiDevice();
            var context = new DevicesViewModel(null);
            var deviceNode = new DevicesDeviceNode(context,
                (FilterableViewItemBase)context.Root, device, device.Name);

            var parametersGroup = deviceNode.Items.OfType<DevicesGroupNode>()
                .Single(g => g.Name == "Параметры");
            var parameterItems = parametersGroup.Items
                .OfType<DevicesParameterItem>().ToArray();

            Assert.IsTrue(parameterItems.Length >= 3);
            Assert.IsTrue(parameterItems.Any(i =>
                i.Parameter == IODevice.Parameter.P_MIN_V));
            Assert.IsTrue(parameterItems.All(i => i.ParentItem == parametersGroup));
        }

        private static DevicesParameterItem CreateParameterItem(
            IODevice device, IODevice.Parameter parameter, string value)
        {
            var context = new DevicesViewModel(null);
            var deviceNode = new DevicesDeviceNode(context,
                (FilterableViewItemBase)context.Root, device, device.Name);
            return new DevicesParameterItem(context, deviceNode, device,
                parameter, value);
        }

        private static DO CreateTankDoDevice()
        {
            var device = new DO("TANK2DO1", "+TANK2-DO1", "desc", 1, "TANK", 2);
            device.SetSubType("DO");
            return device;
        }

        private static AI CreateTankAiDevice()
        {
            var device = new AI("TANK2AI1", "+TANK2-AI1", "desc", 1, "TANK", 2);
            device.SetSubType("AI");
            return device;
        }

        private static DevicesViewModel CreateContextWithDevices(params IODevice[] devices)
        {
            var manager = DeviceManager.GetInstance();
            manager.Devices.Clear();
            foreach (var device in devices)
                manager.Devices.Add(device);

            return new DevicesViewModel(manager);
        }

        private static void ResetDeviceManager()
        {
            var instance = typeof(DeviceManager).GetField("instance",
                BindingFlags.NonPublic | BindingFlags.Static);
            instance.SetValue(null, null);
        }
    }
}
