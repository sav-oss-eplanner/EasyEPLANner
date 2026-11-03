using EasyEPlanner;
using EplanDevice;
using IO;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace EasyEplannerTests
{
    public class LuaMainIoLoaderTest
    {
        [SetUp]
        public void SetUp()
        {
            string assemblyDir = Path.GetDirectoryName(
                typeof(ProjectManager).Assembly.Location);
            string systemLua = Path.Combine(assemblyDir, "Lua");
            ProjectContextHolder.Current = new FileProjectContext(
                Path.GetTempPath(), systemLua, assemblyDir);
        }

        [TearDown]
        public void TearDown()
        {
            ProjectContextHolder.Current = null;
            DeviceManager.GetInstance().Clear();
            IOManager.GetInstance().Clear();
        }

        [Test]
        public void LoadFromLua_LoadsNodesAndDevices()
        {
            const string lua = @"
PAC_name = 'Test'
nodes =
{
    {
        name = 'A100',
        ntype = 100,
        n = 1,
        IP = '192.168.1.10',
        modules =
        {
            { 530 },
        },
    },
}
devices =
{
    {
        name = 'TANK1V1',
        descr = 'Valve',
        dtype = 1,
        subtype = 1,
        article = '',
    },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            Assert.AreEqual(1, IOManager.GetInstance().IONodes.Count);
            Assert.AreEqual("A100", IOManager.GetInstance().IONodes[0].Name);
            Assert.AreEqual(1, IOManager.GetInstance().IONodes[0].IOModules.Count);
            Assert.AreNotEqual(IOModuleInfo.Stub.Name,
                IOManager.GetInstance().IONodes[0].IOModules[0].Info.Name);
            Assert.AreEqual("TANK1V1",
                DeviceManager.GetInstance().GetDevice("TANK1V1").Name);
        }

        [Test]
        public void LoadFromLua_LoadsModulesAndEmptySlots()
        {
            const string lua = @"
nodes =
{
    {
        name = 'A1',
        ntype = 203,
        n = 1,
        IP = '10.0.0.1',
        modules =
        {
            { 530 },
            {},
            { 657 },
        },
    },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            var node = IOManager.GetInstance().IONodes[0];
            Assert.AreEqual(3, node.IOModules.Count);
            Assert.AreEqual("750-530", node.IOModules[0].Info.Name);
            Assert.AreEqual(IOModuleInfo.Stub.Name, node.IOModules[1].Info.Name);
            Assert.AreEqual("750-657", node.IOModules[2].Info.Name);
            Assert.AreEqual("A2", node.IOModules[0].Name);
            Assert.AreEqual("A4", node.IOModules[2].Name);
        }

        [Test]
        public void LoadFromLua_BindsDeviceChannelsToModuleClamps()
        {
            const string lua = @"
nodes =
{
    {
        name = 'A1',
        ntype = 203,
        n = 1,
        IP = '10.0.0.1',
        modules =
        {
            { 530 },
        },
    },
}
devices =
{
    {
        name = 'TANK1V1',
        descr = 'Valve',
        dtype = 0,
        subtype = 1,
        article = '',
        DO =
        {
            {
                node = 0,
                offset = 0,
                physical_port = 1,
                logical_port = 1,
                module_offset = 0,
            },
        },
    },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            var module = IOManager.GetInstance().IONodes[0].IOModules[0];
            var binding = module.GetClampBinding(1)?.ToList();

            Assert.NotNull(binding);
            Assert.AreEqual(1, binding.Count);
            Assert.AreEqual("TANK1V1", binding[0].Item1.Name);
        }

        [Test]
        public void LoadFromLua_LoadsParametersPropertiesAndChannelBinding()
        {
            // V_DO1_DI1_FB_OFF (subtype 3): параметры P_ON_TIME, P_FB
            // V_DO2 (subtype 2): два DO — привязка по порядку каналов
            // после SortChannels ("Открыть", затем "Закрыть")
            // M_ATV (subtype 9): свойство IP
            const string lua = @"
nodes =
{
    {
        name = 'A1',
        ntype = 203,
        n = 1,
        IP = '10.0.0.1',
        modules =
        {
            { 530 },
        },
    },
}
devices =
{
    {
        name = 'TANK1V3',
        descr = 'Valve FB',
        dtype = 0,
        subtype = 3,
        article = '',
        par = { 5, 1 },
    },
    {
        name = 'TANK1V2',
        descr = 'Valve DO2',
        dtype = 0,
        subtype = 2,
        article = '',
        DO =
        {
            {
                node = 0,
                offset = 0,
                physical_port = 1,
                logical_port = 1,
                module_offset = 0,
            },
            {
                node = 0,
                offset = 1,
                physical_port = 2,
                logical_port = 2,
                module_offset = 0,
            },
        },
    },
    {
        name = 'LINE1M1',
        descr = 'Motor',
        dtype = 2,
        subtype = 9,
        article = '',
        prop =
        {
            IP = '10.1.2.3',
        },
    },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            var valveFb = DeviceManager.GetInstance().GetDevice("TANK1V3");
            Assert.AreNotEqual(StaticHelper.CommonConst.Cap, valveFb.Description);
            Assert.AreEqual(DeviceSubType.V_DO1_DI1_FB_OFF, valveFb.DeviceSubType);
            Assert.AreEqual(5.0, valveFb.Parameters[IODevice.Parameter.P_ON_TIME]);
            Assert.AreEqual(1.0, valveFb.Parameters[IODevice.Parameter.P_FB]);

            var valveDo2 = DeviceManager.GetInstance().GetDevice("TANK1V2");
            // Каналы сопоставляются по позиции: после SortChannels() DO
            // упорядочены как ["Открыть", "Закрыть"], поэтому первая запись
            // DO в файле соответствует "Открыть", вторая — "Закрыть".
            var openCh = valveDo2.Channels.First(c => c.Comment == "Открыть");
            var closeCh = valveDo2.Channels.First(c => c.Comment == "Закрыть");
            Assert.IsFalse(openCh.IsEmpty());
            Assert.IsFalse(closeCh.IsEmpty());
            Assert.AreEqual(1, openCh.PhysicalClamp);
            Assert.AreEqual(2, closeCh.PhysicalClamp);
            Assert.AreEqual(2, openCh.FullModule);

            var motor = DeviceManager.GetInstance().GetDevice("LINE1M1");
            Assert.AreEqual(DeviceSubType.M_ATV, motor.DeviceSubType);
            Assert.AreEqual("10.1.2.3",
                motor.Properties[IODevice.Property.IP]?.ToString().Trim('\''));
        }

        [Test]
        public void ParseDeviceSnapshots_DoesNotChangeManagers()
        {
            const string lua = @"
devices =
{
    {
        name = 'TANK1V1',
        descr = 'Valve from lua',
        dtype = 1,
        subtype = 1,
        par = { 150 --[[P_ON_TIME]], },
    },
}
";
            DeviceManager.GetInstance().Clear();
            var existing = new DO("KEEP1", "+KEEP1", "keep", 1, "KEEP", 1);
            existing.SetSubType("DO");
            DeviceManager.GetInstance().Devices.Add(existing);

            var snapshots = LuaMainIoLoader.ParseDeviceSnapshots(lua);

            Assert.AreEqual(1, DeviceManager.GetInstance().Devices.Count);
            Assert.AreEqual("KEEP1", DeviceManager.GetInstance().Devices[0].Name);
            Assert.AreEqual(1, snapshots.Count);
            Assert.AreEqual("TANK1V1", snapshots[0].Name);
            Assert.AreEqual("Valve from lua", snapshots[0].Description);
            Assert.AreEqual("V_DO1", snapshots[0].SubType);
            Assert.AreEqual("150", snapshots[0].OrderedParameters[0]);
        }

        [Test]
        public void ParseDeviceSnapshots_ReadsNamedParametersAndProperties()
        {
            const string lua = @"
devices =
{
    {
        name = 'TANK1C1',
        descr = 'PID',
        dtype = 12,
        subtype = 1,
        par = { P_k = 1.5, P_Ti = 2 },
        prop = { IN_VALUE = 'TANK1AI1' },
        rt_par = { 3 },
    },
}
";
            var snapshots = LuaMainIoLoader.ParseDeviceSnapshots(lua);

            Assert.AreEqual(1, snapshots.Count);
            Assert.AreEqual("1.5", snapshots[0].NamedParameters["P_k"]);
            Assert.AreEqual("2", snapshots[0].NamedParameters["P_Ti"]);
            Assert.AreEqual("TANK1AI1", snapshots[0].Properties["IN_VALUE"]);
            Assert.AreEqual("3", snapshots[0].OrderedRuntimeParameters[0]);
        }

        [Test]
        public void FileProjectContext_UsesFolderNameAsProjectName()
        {
            var context = new FileProjectContext(
                @"C:\projects\MyPlant", @"C:\app\Lua", @"C:\app");

            Assert.AreEqual("MyPlant", context.ProjectName);
            StringAssert.EndsWith("MyPlant", context.ProjectFolderPath);
        }
    }
}
