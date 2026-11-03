using EplanDevice;
using IO;
using Moq;
using NUnit.Framework;
using StaticHelper;
using System.Collections.Generic;
using System.Reflection;
using static EplanDevice.IODevice;

namespace EasyEplannerTests.StaticHelperTest
{
    [NonParallelizable]
    public class IOHelperTest
    {
        const string ValveClamp = "+KOAG4-Y1";
        const string ValveClampWithPort = "+KOAG4-Y1:3";
        const int BoundModuleNumber = 202;

        [SetUp]
        public void SetUp()
        {
            ResetSingleton(typeof(DeviceManager));
            ResetSingleton(typeof(IOManager));
            IOManager.GetInstance().Clear();
        }

        [Test]
        public void TryGetValveTerminalIOModuleFromModel_UnknownDevice_ReturnsNull()
        {
            var result = InvokeTryGetFromModel("+UNKNOWN-Y9");

            Assert.IsNull(result);
        }

        [Test]
        public void TryGetValveTerminalIOModuleFromModel_NonValveTerminal_ReturnsNull()
        {
            AddDevice(new DO("TANK2DO1", "+TANK2-DO1", "desc", 1, "TANK", 2));

            var result = InvokeTryGetFromModel("+TANK2-DO1");

            Assert.IsNull(result);
        }

        [Test]
        public void TryGetValveTerminalIOModuleFromModel_EmptyChannel_ReturnsNull()
        {
            AddDevice(CreateValveTerminal(ValveClamp));

            var result = InvokeTryGetFromModel(ValveClamp);

            Assert.IsNull(result);
        }

        [Test]
        public void TryGetValveTerminalIOModuleFromModel_ModuleNotFound_ReturnsNull()
        {
            var valve = CreateValveTerminal(ValveClamp);
            BindValveTerminal(valve, BoundModuleNumber);
            AddDevice(valve);

            var result = InvokeTryGetFromModel(ValveClamp);

            Assert.IsNull(result);
        }

        [Test]
        public void TryGetValveTerminalIOModuleFromModel_WithColon_UsesDeviceNameBeforeColon()
        {
            var valve = CreateValveTerminal(ValveClamp);
            BindValveTerminal(valve, BoundModuleNumber);
            AddDevice(valve);
            var moduleFunction = Mock.Of<IEplanFunction>();
            SetupIOModule(BoundModuleNumber, moduleFunction);

            var result = InvokeTryGetFromModel(ValveClampWithPort);

            Assert.AreSame(moduleFunction, result);
        }

        [Test]
        public void TryGetValveTerminalIOModuleFromModel_DevVtugType_ReturnsModuleFunction()
        {
            const string devVtugClamp = "+KOAG4-DEV_VTUG1";
            var valve = new DEV_VTUG("KOAG4DEV_VTUG1", devVtugClamp, "vtug", 1,
                "KOAG", 4, "");
            BindValveTerminal(valve, BoundModuleNumber);
            AddDevice(valve);
            var moduleFunction = Mock.Of<IEplanFunction>();
            SetupIOModule(BoundModuleNumber, moduleFunction);

            var result = InvokeTryGetFromModel(devVtugClamp);

            Assert.AreSame(moduleFunction, result);
        }

        [Test]
        public void TryGetValveTerminalIOModuleFromModel_BoundY_ReturnsModuleFunction()
        {
            var valve = CreateValveTerminal(ValveClamp);
            BindValveTerminal(valve, BoundModuleNumber);
            AddDevice(valve);
            var moduleFunction = Mock.Of<IEplanFunction>();
            SetupIOModule(BoundModuleNumber, moduleFunction);

            var result = InvokeTryGetFromModel(ValveClamp);

            Assert.AreSame(moduleFunction, result);
        }

        private static IEplanFunction InvokeTryGetFromModel(string clampName)
        {
            var method = typeof(IOHelper).GetMethod(
                "TryGetValveTerminalIOModuleFromModel",
                BindingFlags.NonPublic | BindingFlags.Static);

            return (IEplanFunction)method.Invoke(null, new object[] { clampName });
        }

        private static Y CreateValveTerminal(string eplanName)
        {
            return new Y("KOAG4Y1", eplanName, "valve terminal", 1, "KOAG", 4, "");
        }

        private static void BindValveTerminal(IODevice valve, int fullModule)
        {
            valve.Channels[0].SetChannel(0, 1, 0, fullModule, 0, 0);
        }

        private static void AddDevice(IODevice device)
        {
            var manager = DeviceManager.GetInstance();
            manager.Devices.Add(device);
            manager.Devices.Sort();
        }

        private static void SetupIOModule(int physicalNumber,
            IEplanFunction moduleFunction)
        {
            var node = new IONode("", 1, 100, "", "A100", "", "");
            node.IOModules.Add(new IOModule(0, 0, null, physicalNumber,
                string.Empty, moduleFunction));

            typeof(IOManager).GetField("iONodes",
                BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(IOManager.GetInstance(), new List<IIONode> { node });
        }

        private static void ResetSingleton(System.Type type)
        {
            type.GetField("instance",
                BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, null);
        }
    }
}
