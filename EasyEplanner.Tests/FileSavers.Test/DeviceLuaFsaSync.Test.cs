using EasyEPlanner;
using EplanDevice;
using Moq;
using NUnit.Framework;
using StaticHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasyEplannerTests
{
    public class DeviceLuaChangeComparerTest
    {
        [Test]
        public void Compare_NoDifferences_ReturnsEmpty()
        {
            var device = CreateAi("TANK2AI1", "level", 0);
            var lua = new DeviceLuaSnapshot
            {
                Name = "TANK2AI1",
                Description = "level",
                SubType = "AI",
            };
            lua.NamedParameters["P_MIN_V"] = "0";

            var changes = DeviceLuaChangeComparer.Compare(
                new IODevice[] { device }, new DeviceLuaSnapshot[] { lua });

            Assert.IsEmpty(changes);
        }

        [Test]
        public void Compare_FindsDescriptionSubtypeAndParameterChanges()
        {
            var device = CreateAi("TANK2AI1", "old", 0);
            var lua = new DeviceLuaSnapshot
            {
                Name = "TANK2AI1",
                Description = "new",
                SubType = "AI_VIRT",
            };
            lua.NamedParameters["P_MIN_V"] = "1.5";

            var changes = DeviceLuaChangeComparer.Compare(
                new IODevice[] { device }, new DeviceLuaSnapshot[] { lua });

            Assert.AreEqual(3, changes.Count);
            Assert.IsTrue(changes.Any(c =>
                c.Kind == DeviceLuaChangeKind.Description &&
                c.NewValue == "new"));
            Assert.IsTrue(changes.Any(c =>
                c.Kind == DeviceLuaChangeKind.SubType &&
                c.NewValue == "AI_VIRT"));
            Assert.IsTrue(changes.Any(c =>
                c.Kind == DeviceLuaChangeKind.Parameter &&
                c.FieldName == "P_MIN_V" &&
                c.NewValue == "1.5"));
        }

        [Test]
        public void Compare_IgnoresDevicesMissingOnFsa()
        {
            var lua = new DeviceLuaSnapshot
            {
                Name = "UNKNOWN1",
                Description = "x",
            };

            var changes = DeviceLuaChangeComparer.Compare(
                new IODevice[] { CreateAi("TANK2AI1", "level", 0) },
                new DeviceLuaSnapshot[] { lua });

            Assert.IsEmpty(changes);
        }

        [Test]
        public void Compare_DescriptionWithLineBreaks_MatchesLuaDotSpaceFormat()
        {
            var device = CreateAi("TANK2AI1", "first\nsecond", 0);
            device.Function = Mock.Of<IEplanFunction>(f =>
                f.Description == "first\u00B6second");
            var lua = new DeviceLuaSnapshot
            {
                Name = "TANK2AI1",
                Description = "first. second",
                SubType = "AI",
            };
            lua.NamedParameters["P_MIN_V"] = "0";

            var changes = DeviceLuaChangeComparer.Compare(
                new IODevice[] { device }, new DeviceLuaSnapshot[] { lua });

            Assert.IsEmpty(changes);
        }

        [Test]
        public void Compare_IgnoresRuntimeParametersUntilProjectUpdate()
        {
            var device = new V("TANK1V1", "+TANK1-V1", "valve", 1, "TANK", 1, "");
            device.SetSubType("V_IOLINK_VTUG_DO1");
            var lua = new DeviceLuaSnapshot
            {
                Name = "TANK1V1",
                Description = "valve",
                SubType = "V_IOLINK_VTUG_DO1",
            };
            lua.OrderedRuntimeParameters.Add("5");
            lua.OrderedRuntimeParameters.Add("2");

            var changes = DeviceLuaChangeComparer.Compare(
                new IODevice[] { device }, new DeviceLuaSnapshot[] { lua });

            Assert.IsFalse(changes.Any(c =>
                c.Kind == DeviceLuaChangeKind.RuntimeParameter));
        }

        [Test]
        public void ValuesEqual_TreatsEquivalentNumbersAsEqual()
        {
            Assert.IsTrue(DeviceLuaChangeComparer.ValuesEqual("1", "1.0"));
            Assert.IsTrue(DeviceLuaChangeComparer.ValuesEqual("  a  ", "a"));
            Assert.IsFalse(DeviceLuaChangeComparer.ValuesEqual("a", "b"));
        }

        private static AI CreateAi(string name, string description, double min)
        {
            var device = new AI(name, "+" + name, description, 1, "TANK", 2);
            device.SetSubType("AI");
            device.SetParameter("P_MIN_V", min);
            return device;
        }
    }

    public class DeviceLuaChangeApplierTest
    {
        [TearDown]
        public void TearDown()
        {
            DeviceManager.GetInstance().Clear();
        }

        [Test]
        public void Apply_UpdatesDescriptionParametersAndFunction()
        {
            var device = new AI("TANK2AI1", "+TANK2-AI1", "old", 1, "TANK", 2);
            device.SetSubType("AI");
            var function = new Mock<IEplanFunction>();
            device.Function = function.Object;

            DeviceManager.GetInstance().Devices.Add(device);
            DeviceManager.GetInstance().Sort();

            var changes = new List<DeviceLuaChange>
            {
                new DeviceLuaChange("TANK2AI1", DeviceLuaChangeKind.Description,
                    "", "old", "new"),
                new DeviceLuaChange("TANK2AI1", DeviceLuaChangeKind.Parameter,
                    "P_MIN_V", "0", "2.5"),
            };

            bool subtypeChanged = DeviceLuaChangeApplier.Apply(
                DeviceManager.GetInstance(), changes);

            Assert.IsFalse(subtypeChanged);
            Assert.AreEqual("new", device.Description);
            Assert.AreEqual(2.5, device.Parameters[IODevice.Parameter.P_MIN_V]);
            function.VerifySet(f => f.Description = "new");
            function.VerifySet(f => f.Parameters = It.Is<string>(s =>
                s.Contains("P_MIN_V=2.5")));
        }

        [Test]
        public void Apply_SubtypeWithFunction_WritesToFunction()
        {
            var device = new DO("TANK2DO1", "+TANK2-DO1", "desc", 1, "TANK", 2);
            device.SetSubType("DO");
            var function = new Mock<IEplanFunction>();
            device.Function = function.Object;

            DeviceManager.GetInstance().Devices.Add(device);
            DeviceManager.GetInstance().Sort();

            var changes = new DeviceLuaChange[]
            {
                new DeviceLuaChange("TANK2DO1", DeviceLuaChangeKind.SubType,
                    "", "DO", "DO_VIRT"),
            };

            bool subtypeChanged = DeviceLuaChangeApplier.Apply(
                DeviceManager.GetInstance(), changes);

            Assert.IsTrue(subtypeChanged);
            function.VerifySet(f => f.SubType = "DO_VIRT");
            Assert.AreEqual(DeviceSubType.DO, device.DeviceSubType);
        }
    }

    public class DeviceLuaFsaSynchronizerTest
    {
        [TearDown]
        public void TearDown()
        {
            DeviceLuaFsaSynchronizer.ConfirmChanges = null;
            DeviceManager.GetInstance().Clear();
        }

        [Test]
        public void TryApplyFromProjectFolder_WhenUserAccepts_AppliesChanges()
        {
            var device = new AI("TANK2AI1", "+TANK2-AI1", "old", 1, "TANK", 2);
            device.SetSubType("AI");
            DeviceManager.GetInstance().Devices.Add(device);
            DeviceManager.GetInstance().Sort();

            string folder = Path.Combine(Path.GetTempPath(),
                "EasyEPlannerLuaSyncTest");
            Directory.CreateDirectory(folder);
            string luaPath = Path.Combine(folder, "main.io.lua");
            File.WriteAllText(luaPath, @"
devices =
{
    {
        name = 'TANK2AI1',
        descr = 'from lua',
        dtype = 10,
        subtype = 1,
        par = { P_MIN_V = 4 },
    },
}
");

            DeviceLuaFsaSynchronizer.ConfirmChanges = changes => true;
            bool resynced = false;

            DeviceLuaFsaSynchronizer.TryApplyFromProjectFolder(
                DeviceManager.GetInstance(), folder,
                () => { resynced = true; });

            Assert.AreEqual("from lua", device.Description);
            Assert.AreEqual(4, device.Parameters[IODevice.Parameter.P_MIN_V]);
            Assert.IsFalse(resynced);
        }

        [Test]
        public void TryApplyFromProjectFolder_WhenUserSkips_DoesNotApply()
        {
            var device = new AI("TANK2AI1", "+TANK2-AI1", "old", 1, "TANK", 2);
            device.SetSubType("AI");
            DeviceManager.GetInstance().Devices.Add(device);
            DeviceManager.GetInstance().Sort();

            string folder = Path.Combine(Path.GetTempPath(),
                "EasyEPlannerLuaSyncTestSkip");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "main.io.lua"), @"
devices =
{
    {
        name = 'TANK2AI1',
        descr = 'from lua',
        dtype = 10,
        subtype = 1,
    },
}
");

            DeviceLuaFsaSynchronizer.ConfirmChanges = changes => false;

            DeviceLuaFsaSynchronizer.TryApplyFromProjectFolder(
                DeviceManager.GetInstance(), folder, () => { });

            Assert.AreEqual("old", device.Description);
        }
    }
}
