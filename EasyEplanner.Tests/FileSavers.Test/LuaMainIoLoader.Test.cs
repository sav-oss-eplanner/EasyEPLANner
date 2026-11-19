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
            IOManager.GetInstance().ResetStoredNtypes();
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
        public void LoadFromLua_EmptyTextKeepsManagersUnchanged()
        {
            const string lua = @"
nodes =
{
    {
        name = 'A100',
        ntype = 100,
        n = 1,
        IP = '10.0.0.1',
    },
}
";
            LuaMainIoLoader.LoadFromLua(lua);

            // Пустой текст не должен очищать уже загруженные узлы.
            LuaMainIoLoader.LoadFromLua(" \t\r\n");

            Assert.AreEqual(1, IOManager.GetInstance().IONodes.Count);
            Assert.AreEqual("A100", IOManager.GetInstance().IONodes[0].Name);
        }

        [Test]
        public void LoadFromLua_WithoutNodesAndDevicesTablesKeepsManagersEmpty()
        {
            // Ни таблицы nodes, ни devices — загрузка не должна падать.
            LuaMainIoLoader.LoadFromLua("PAC_name = 'Test'\n");

            Assert.AreEqual(0, IOManager.GetInstance().IONodes.Count);
            Assert.AreEqual(0, DeviceManager.GetInstance().Devices.Count);
        }

        [Test]
        public void LoadFromLua_SkipsNonTableNodeAndDeviceEntries()
        {
            const string lua = @"
nodes =
{
    'A1',
}
devices =
{
    'TANK1V1',
    { name = 'X1', subtype = 1 },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            // Строковые записи пропускаются; 'X1' — неизвестный тип устройства.
            Assert.AreEqual(0, IOManager.GetInstance().IONodes.Count);
            Assert.AreEqual(0, DeviceManager.GetInstance().Devices.Count);
        }

        [Test]
        public void LoadFromLua_TreatsNodeWithoutNtypeAsDisabledAndIgnoresItsModules()
        {
            const string luaWithoutNtype = @"
nodes =
{
    {
        name = 'A100',
        n = 1,
        IP = '10.0.0.1',
        modules = { { 530 } },
    },
}
";
            const string luaWithNtype = @"
nodes =
{
    {
        name = 'A100',
        ntype = 100,
        n = 1,
        IP = '10.0.0.1',
    },
}
";

            // Узел без ntype — T_EMPTY; у такого узла нет адресного
            // пространства, поэтому модули заменяются заглушками и
            // отбрасываются, а выключение ntype запоминается по имени.
            LuaMainIoLoader.LoadFromLua(luaWithoutNtype);
            var disabledNode = IOManager.GetInstance().IONodes[0];
            Assert.AreEqual(IONode.TYPES.T_EMPTY, disabledNode.Type);
            Assert.AreEqual(0, disabledNode.IOModules.Count);

            LuaMainIoLoader.LoadFromLua(luaWithNtype);
            var reloadedNode = IOManager.GetInstance().IONodes[0];
            Assert.AreEqual(IONode.TYPES.T_ETHERNET, reloadedNode.Type);
            Assert.IsFalse(reloadedNode.NtypeEnabled);
        }

        [Test]
        public void LoadFromLua_LoadsExtensionNode()
        {
            const string lua = @"
nodes =
{
    {
        name = 'A100',
        ntype = 100,
        n = 1,
        IP = '192.168.1.10',
    },
    {
        name = 'A100.1',
        IP = '192.168.1.11',
        location = '+MCC1',
        location_description = 'Cab 1',
    },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            Assert.AreEqual(1, IOManager.GetInstance().IONodes.Count);
            var parent = IOManager.GetInstance().IONodes[0];
            Assert.AreEqual(1, parent.ExtensionModules.Count);

            var extension = parent.ExtensionModules[0];
            Assert.AreEqual("A100.1", extension.Name);
            Assert.AreEqual(100 * 100 + 1, extension.NodeNumber);
            Assert.AreEqual(1, extension.N);
            Assert.AreEqual(IONode.TYPES.T_PXC_EXTENSION, extension.Type);
            Assert.AreEqual("AXC F XT ETH 1TX", extension.TypeStr);
            Assert.AreEqual("192.168.1.11", extension.IP);
            Assert.AreEqual("+MCC1", extension.Location);
            Assert.AreEqual("Cab 1", extension.LocationDescription);
            Assert.IsTrue(extension.NtypeEnabled);
        }

        [Test]
        public void LoadFromLua_SkipsExtensionNodeWithUnknownParent()
        {
            const string lua = @"
nodes =
{
    {
        name = 'A100',
        ntype = 100,
        n = 1,
        IP = '10.0.0.1',
    },
    {
        name = 'A500.1',
        IP = '10.0.0.2',
    },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            Assert.AreEqual(1, IOManager.GetInstance().IONodes.Count);
            Assert.AreEqual(0,
                IOManager.GetInstance().IONodes[0].ExtensionModules.Count);
        }

        [Test]
        public void LoadFromLua_RestoresDisabledNtypeForExtensionNodeAfterReload()
        {
            const string luaWithEmptyExtension = @"
nodes =
{
    { name = 'A100', ntype = 100, n = 1, IP = '192.168.1.10' },
    { name = 'A100.1', ntype = -1, IP = '192.168.1.11' },
}
";
            const string luaWithEnabledExtension = @"
nodes =
{
    { name = 'A100', ntype = 100, n = 1, IP = '192.168.1.10' },
    { name = 'A100.1', IP = '192.168.1.11' },
}
";

            LuaMainIoLoader.LoadFromLua(luaWithEmptyExtension);
            Assert.AreEqual(IONode.TYPES.T_EMPTY,
                IOManager.GetInstance().IONodes[0].ExtensionModules[0].Type);

            LuaMainIoLoader.LoadFromLua(luaWithEnabledExtension);
            var extension = IOManager.GetInstance().IONodes[0].ExtensionModules[0];
            Assert.AreEqual(IONode.TYPES.T_PXC_EXTENSION, extension.Type);
            Assert.IsFalse(extension.NtypeEnabled);
        }

        [Test]
        public void LoadFromLua_OrdersModulesByNumericKey()
        {
            const string lua = @"
nodes =
{
    {
        name = 'A1',
        ntype = 100,
        n = 1,
        IP = '10.0.0.1',
        modules =
        {
            [3] = { 657 },
            [1] = { 530 },
            extra = {},
        },
    },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            var modules = IOManager.GetInstance().IONodes[0].IOModules;
            Assert.AreEqual(3, modules.Count);
            Assert.AreEqual("750-530", modules[0].Info.Name);
            Assert.AreEqual("750-657", modules[1].Info.Name);
            Assert.AreEqual(IOModuleInfo.Stub.Name, modules[2].Info.Name);
            Assert.AreEqual("A2", modules[0].Name);
            Assert.AreEqual("A3", modules[1].Name);
            Assert.AreEqual("A4", modules[2].Name);
        }

        [Test]
        public void LoadFromLua_CreatesStubForInvalidModuleEntries()
        {
            const string lua = @"
nodes =
{
    {
        name = 'A1',
        ntype = 100,
        n = 1,
        IP = '10.0.0.1',
        modules = { { 0 }, { -5 }, '750-530' },
    },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            var modules = IOManager.GetInstance().IONodes[0].IOModules;
            Assert.AreEqual(3, modules.Count);
            foreach (var module in modules)
            {
                Assert.AreEqual(IOModuleInfo.Stub.Name, module.Info.Name);
            }
            Assert.AreEqual("A2", modules[0].Name);
            Assert.AreEqual("A3", modules[1].Name);
            Assert.AreEqual("A4", modules[2].Name);
        }

        [Test]
        public void LoadFromLua_ReplacesModuleExceedingNodeAddressAreaWithStub()
        {
            // Модуль Phoenix Contact (AXL F DI8/1 DO8/1 1H, DODI) занимает
            // адресное пространство, поэтому на узле WAGO (лимит 0)
            // SetModule завершается ошибкой и модуль заменяется заглушкой.
            const string lua = @"
nodes =
{
    {
        name = 'A1',
        ntype = 100,
        n = 1,
        IP = '10.0.0.1',
        modules = { { 2701916 } },
    },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            var module = IOManager.GetInstance().IONodes[0].IOModules[0];
            Assert.AreEqual(IOModuleInfo.Stub.Name, module.Info.Name);
            Assert.AreEqual(2, module.PhysicalNumber);
        }

        [Test]
        public void LoadFromLua_BindsChannelsByAccumulatedAddressSpaceOffsets()
        {
            // DI-модули занимают InOffset по счётчику DI_count, DO/AO —
            // OutOffset по DO_count/AO_count; каналы привязываются к узлу
            // и модулю по сохранённому module_offset.
            const string lua = @"
nodes =
{
    {
        name = 'A1', ntype = 100, n = 1, IP = '10.0.0.1',
        modules = { { 402 }, { 402 } },
    },
    {
        name = 'A2', ntype = 100, n = 2, IP = '10.0.0.2',
        modules = { { 530 }, { 530 } },
    },
    {
        name = 'A3', ntype = 100, n = 3, IP = '10.0.0.3',
        modules = { { 555 }, { 555 }, { 450 }, { 450 } },
    },
}
devices =
{
    {
        name = 'TANK1DI1', descr = 'DI 1', subtype = 13001, article = '',
        DI = { { node = 0, offset = 0, physical_port = 1,
                 logical_port = 1, module_offset = 0 } },
    },
    {
        name = 'TANK1DI2', descr = 'DI 2', subtype = 13001, article = '',
        DI = { { node = 0, offset = 4, physical_port = 2,
                 logical_port = 2, module_offset = 4 } },
    },
    {
        name = 'TANK1DO1', descr = 'DO 1', article = '',
        DO = { { node = 1, offset = 0, physical_port = 1,
                 logical_port = 1, module_offset = 0 } },
    },
    {
        name = 'TANK1DO2', descr = 'DO 2', article = '',
        DO = { { node = 1, offset = 8, physical_port = 2,
                 logical_port = 2, module_offset = 8 } },
    },
    {
        name = 'TANK1AO1', descr = 'AO 1', article = '',
        AO = { { node = 2, offset = 0, physical_port = 1,
                 logical_port = 1, module_offset = 0 } },
    },
    {
        name = 'TANK1AO2', descr = 'AO 2', article = '',
        AO = { { node = 2, offset = 4, physical_port = 2,
                 logical_port = 2, module_offset = 4 } },
    },
    {
        name = 'TANK1AI1', descr = 'AI 1', subtype = 15001, article = '',
        AI = { { node = 2, offset = 4, physical_port = 1,
                 logical_port = 1, module_offset = 4 } },
    },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            Assert.AreEqual(3, IOManager.GetInstance().IONodes.Count);
            CheckChannel("TANK1DI1", "DI", 1, 2);
            CheckChannel("TANK1DI2", "DI", 2, 3);
            CheckChannel("TANK1DO1", "DO", 1, 3);
            CheckChannel("TANK1DO2", "DO", 2, 4);
            CheckChannel("TANK1AO1", "AO", 1, 4);
            CheckChannel("TANK1AO2", "AO", 2, 5);
            CheckChannel("TANK1AI1", "AI", 1, 7);
        }

        [Test]
        public void LoadFromLua_FallsBackToSingleRealModuleWhenOffsetMismatched()
        {
            const string lua = @"
nodes =
{
    {
        name = 'A1',
        ntype = 100,
        n = 1,
        IP = '10.0.0.1',
        modules = { {}, { 530 } },
    },
}
devices =
{
    {
        name = 'TANK1V1',
        descr = 'Valve',
        subtype = 1,
        article = '',
        DO =
        {
            { node = 0, offset = 0, physical_port = 1,
              logical_port = 1, module_offset = 7 },
        },
    },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            // Заглушки пропускаются; если смещение не совпало, а реальный
            // модуль на узле один, канал привязывается к нему.
            var channel = DeviceManager.GetInstance().GetDevice("TANK1V1")
                .Channels.First(c => c.Name == "DO");
            Assert.IsFalse(channel.IsEmpty());
            Assert.AreEqual(1, channel.PhysicalClamp);
            Assert.AreEqual(3, channel.FullModule);
        }

        [Test]
        public void LoadFromLua_SkipsMalformedChannelEntries()
        {
            const string lua = @"
nodes =
{
    {
        name = 'A1',
        ntype = 100,
        n = 1,
        IP = '10.0.0.1',
        modules = { { 530 }, { 657 } },
    },
}
devices =
{
    {
        name = 'TANK1HLA1',
        descr = 'HLA',
        subtype = 21001,
        article = '',
        DO =
        {
            'BAD',
            { offset = 0, physical_port = 1, logical_port = 1,
              module_offset = 0 },
            { node = 0, offset = 0, logical_port = 1, module_offset = 0 },
            { node = 7, offset = 0, physical_port = 1, logical_port = 1,
              module_offset = 0 },
            { node = 0, offset = 0, physical_port = 1, logical_port = 1,
              module_offset = 99 },
        },
    },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            // Некорректные записи (нет узла, нет клеммы, узел вне диапазона,
            // модуль не найден по смещению) не привязывают каналы.
            var hla = DeviceManager.GetInstance().GetDevice("TANK1HLA1");
            Assert.AreEqual(4, hla.Channels.Count);
            Assert.IsTrue(hla.Channels.All(c => c.IsEmpty()));
        }

        [Test]
        public void LoadFromLua_IgnoresChannelEntriesBeyondDeviceChannels()
        {
            const string lua = @"
nodes =
{
    {
        name = 'A1',
        ntype = 100,
        n = 1,
        IP = '10.0.0.1',
        modules = { { 530 } },
    },
}
devices =
{
    {
        name = 'TANK1V2',
        descr = 'Valve DO2',
        subtype = 2,
        article = '',
        DO =
        {
            { node = 0, offset = 0, physical_port = 1, logical_port = 1,
              module_offset = 0 },
            { node = 0, offset = 1, physical_port = 2, logical_port = 2,
              module_offset = 0 },
            { node = 0, offset = 2, physical_port = 3, logical_port = 3,
              module_offset = 0 },
        },
    },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            // Записей больше, чем каналов устройства, — лишние отбрасываются.
            var valve = DeviceManager.GetInstance().GetDevice("TANK1V2");
            var openCh = valve.Channels.First(c => c.Comment == "Открыть");
            var closeCh = valve.Channels.First(c => c.Comment == "Закрыть");
            Assert.AreEqual(1, openCh.PhysicalClamp);
            Assert.AreEqual(2, closeCh.PhysicalClamp);
        }

        [Test]
        public void LoadFromLua_LoadsNamedDeviceParameters()
        {
            const string lua = @"
devices =
{
    {
        name = 'TANK1C1',
        descr = 'PID',
        subtype = 20001,
        article = '',
        par = { P_k = 1.5, P_Ti = 2 },
    },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            var pid = DeviceManager.GetInstance().GetDevice("TANK1C1");
            Assert.AreEqual(1.5,
                pid.Parameters[IODevice.Parameter.P_k]);
            Assert.AreEqual(2.0,
                pid.Parameters[IODevice.Parameter.P_Ti]);
        }

        [Test]
        public void LoadFromLua_MapsOrderedParametersByKeysCount()
        {
            const string lua = @"
devices =
{
    {
        name = 'TANK1V3',
        descr = 'Valve FB',
        subtype = 3,
        article = '',
        par = { 7 },
    },
    {
        name = 'TANK1C2',
        descr = 'PID',
        subtype = 20001,
        article = '',
        par = { 2, 3 },
    },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            // Число значений равно числу непустых параметров (P_FB) —
            // значения не должны «съезжать» на P_ON_TIME.
            var valve = DeviceManager.GetInstance().GetDevice("TANK1V3");
            Assert.AreEqual(7.0,
                valve.Parameters[IODevice.Parameter.P_FB]);
            Assert.IsNull(valve.Parameters[IODevice.Parameter.P_ON_TIME]);

            // Число значений меньше числа параметров — заполняются первые
            // параметры по порядку, остальные остаются по умолчанию.
            var pid = DeviceManager.GetInstance().GetDevice("TANK1C2");
            Assert.AreEqual(2.0, pid.Parameters[IODevice.Parameter.P_k]);
            Assert.AreEqual(3.0, pid.Parameters[IODevice.Parameter.P_Ti]);
            Assert.AreEqual(0.01, pid.Parameters[IODevice.Parameter.P_Td]);
        }

        [Test]
        public void LoadFromLua_LoadsNamedAndOrderedRuntimeParameters()
        {
            const string lua = @"
devices =
{
    {
        name = 'TANK1VC1',
        descr = 'Valve EY named',
        subtype = 4,
        article = '',
        rt_par = { R_EY_NUMBER = 3 },
    },
    {
        name = 'TANK1VC2',
        descr = 'Valve EY ordered',
        subtype = 4,
        article = '',
        rt_par = { 5 },
    },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            Assert.AreEqual(3.0, DeviceManager.GetInstance()
                .GetDevice("TANK1VC1").RuntimeParameters["R_EY_NUMBER"]);
            Assert.AreEqual(5.0, DeviceManager.GetInstance()
                .GetDevice("TANK1VC2").RuntimeParameters["R_EY_NUMBER"]);
        }

        [Test]
        public void LoadFromLua_IgnoresParametersWhenDeviceHasNone()
        {
            const string lua = @"
devices =
{
    {
        name = 'TANK1DO1',
        descr = 'DO',
        article = '',
        par = { 5 },
    },
    {
        name = 'TANK1V3',
        descr = 'Valve FB',
        subtype = 3,
        article = '',
        par = {},
    },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            // У DO нет параметров; у V_DO1_DI1_FB_OFF пустой par —
            // значения не должны измениться.
            var doDevice = DeviceManager.GetInstance().GetDevice("TANK1DO1");
            Assert.AreEqual(0, doDevice.Parameters.Count);

            var valve = DeviceManager.GetInstance().GetDevice("TANK1V3");
            Assert.IsNull(valve.Parameters[IODevice.Parameter.P_ON_TIME]);
            Assert.AreEqual(1, valve.Parameters[IODevice.Parameter.P_FB]);
        }

        [Test]
        public void LoadFromLua_JoinsTablePropertyValues()
        {
            const string lua = @"
devices =
{
    {
        name = 'TANK1HLA1',
        descr = 'HLA VIRT',
        subtype = 2,
        article = '',
        prop = { SIGNALS_SEQUENCE = { 'G', 'R' } },
    },
}
";

            LuaMainIoLoader.LoadFromLua(lua);

            var hla = DeviceManager.GetInstance().GetDevice("TANK1HLA1");
            Assert.AreEqual("G,R",
                hla.Properties["SIGNALS_SEQUENCE"].ToString());
        }

        [Test]
        public void ParseDeviceSnapshots_ReturnsEmptyForNoDevices()
        {
            Assert.AreEqual(0, LuaMainIoLoader.ParseDeviceSnapshots(null).Count);
            Assert.AreEqual(0, LuaMainIoLoader.ParseDeviceSnapshots("   ").Count);
            Assert.AreEqual(0, LuaMainIoLoader
                .ParseDeviceSnapshots("PAC_name = 'Test'").Count);
            Assert.AreEqual(0, LuaMainIoLoader
                .ParseDeviceSnapshots("devices = 'BAD'").Count);
        }

        [Test]
        public void ParseDeviceSnapshots_SkipsNonTableAndNamelessEntries()
        {
            const string lua = @"
devices =
{
    'TANK1V1',
    { name = '' },
    { name = 'TANK1V2', descr = 'Valve', subtype = 1 },
}
";

            var snapshots = LuaMainIoLoader.ParseDeviceSnapshots(lua);

            Assert.AreEqual(1, snapshots.Count);
            Assert.AreEqual("TANK1V2", snapshots[0].Name);
            Assert.AreEqual("Valve", snapshots[0].Description);
            Assert.AreEqual("V_DO1", snapshots[0].SubType);
        }

        [Test]
        public void ParseDeviceSnapshots_ResolvesSubTypeVariants()
        {
            const string lua = @"
devices =
{
    { name = 'TANK1V1', subtype = 'V_DO1' },
    { name = 'TANK1V2', subtype = 999 },
    { name = 'X1', subtype = 1 },
}
";

            var snapshots = LuaMainIoLoader.ParseDeviceSnapshots(lua);

            // Строковый подтип переносится как есть; неизвестный индекс и
            // неверное имя устройства дают пустой подтип.
            Assert.AreEqual("V_DO1", snapshots[0].SubType);
            Assert.AreEqual(string.Empty, snapshots[1].SubType);
            Assert.AreEqual(string.Empty, snapshots[2].SubType);
        }

        [Test]
        public void ParseDeviceSnapshots_JoinsTablePropertyValues()
        {
            const string lua = @"
devices =
{
    {
        name = 'TANK1HLA1',
        descr = 'HLA VIRT',
        subtype = 2,
        prop = { SIGNALS_SEQUENCE = { 'G', 'R' } },
    },
}
";

            var snapshots = LuaMainIoLoader.ParseDeviceSnapshots(lua);

            Assert.AreEqual("G,R",
                snapshots[0].Properties["SIGNALS_SEQUENCE"]);
        }

        [Test]
        public void ParseDeviceSnapshots_PrefersNamedParametersInMixedTable()
        {
            const string lua = @"
devices =
{
    {
        name = 'TANK1C1',
        descr = 'PID',
        subtype = 1,
        par = { 1, 2, P_k = 3, ['4'] = 6, BAD = 'x' },
    },
}
";

            var snapshots = LuaMainIoLoader.ParseDeviceSnapshots(lua);

            // Числовые ключи и нечисловые значения пропускаются; при наличии
            // именованных параметров упорядоченные не заполняются.
            Assert.AreEqual(1, snapshots.Count);
            Assert.AreEqual("C_PID", snapshots[0].SubType);
            Assert.AreEqual(1, snapshots[0].NamedParameters.Count);
            Assert.AreEqual("3", snapshots[0].NamedParameters["P_k"]);
            Assert.AreEqual(0, snapshots[0].OrderedParameters.Count);
        }

        private static void CheckChannel(string deviceName, string channelName,
            int expectedClamp, int expectedFullModule)
        {
            var channel = DeviceManager.GetInstance().GetDevice(deviceName)
                .Channels.First(c => c.Name == channelName);

            Assert.IsFalse(channel.IsEmpty(), deviceName);
            Assert.AreEqual(expectedClamp, channel.PhysicalClamp, deviceName);
            Assert.AreEqual(expectedFullModule, channel.FullModule, deviceName);
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
