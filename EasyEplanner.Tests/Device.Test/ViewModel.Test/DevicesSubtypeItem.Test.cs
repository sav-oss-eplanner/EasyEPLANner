using EasyEPlanner.Devices.ViewModel;
using EplanDevice;
using Moq;
using NUnit.Framework;
using StaticHelper;

namespace EasyEPlanner.Devices.Tests
{
    public class DevicesSubtypeItemTest
    {
        [Test]
        public void SetValue_SameSubtype_ReturnsFalse()
        {
            var subtypeItem = CreateSubtypeItem(out _);

            Assert.IsFalse(subtypeItem.SetValue("DO"));
        }

        [Test]
        public void SetValue_ValidSubtype_UpdatesFunctionAndShowsRefreshHint()
        {
            var subtypeItem = CreateSubtypeItem(out var function);

            Assert.IsTrue(subtypeItem.SetValue("DO_VIRT"));
            Assert.AreEqual("Обновите проект", subtypeItem.Description);
            Mock.Get(function).VerifySet(f => f.SubType = "DO_VIRT");
        }

        [Test]
        public void SetValue_ValidSubtypeWithoutFunction_UpdatesInMemory()
        {
            var device = new DO("TANK2DO1", "+TANK2-DO1", "desc", 1, "TANK", 2);
            device.SetSubType("DO");

            var context = new DevicesViewModel(null);
            var deviceNode = new DevicesDeviceNode(context,
                (FilterableViewItemBase)context.Root, device, device.Name);
            var subtypeItem = new DevicesSubtypeItem(context, deviceNode, device);

            Assert.IsTrue(subtypeItem.SetValue("DO_VIRT"));
            Assert.AreEqual(DeviceSubType.DO_VIRT, device.DeviceSubType);
            Assert.AreEqual("DO_VIRT", subtypeItem.Description);
        }

        [Test]
        public void SetValue_InvalidSubtype_ReturnsFalse()
        {
            var subtypeItem = CreateSubtypeItem(out var function);

            Assert.IsFalse(subtypeItem.SetValue("INVALID"));
            Mock.Get(function).VerifySet(f => f.SubType = It.IsAny<string>(), Times.Never);
        }

        private static DevicesSubtypeItem CreateSubtypeItem(out IEplanFunction function)
        {
            var device = new DO("TANK2DO1", "+TANK2-DO1", "desc", 1, "TANK", 2);
            device.SetSubType("DO");

            function = Mock.Of<IEplanFunction>();
            device.Function = function;

            var context = new DevicesViewModel(null);
            var deviceNode = new DevicesDeviceNode(context,
                (FilterableViewItemBase)context.Root, device, device.Name);
            return new DevicesSubtypeItem(context, deviceNode, device);
        }
    }
}
