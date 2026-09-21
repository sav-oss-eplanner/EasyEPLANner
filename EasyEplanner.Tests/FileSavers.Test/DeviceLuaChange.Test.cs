using EplanDevice;
using Moq;
using NUnit.Framework;
using StaticHelper;
using System.Collections.Generic;
using System.Linq;

namespace EasyEplannerTests
{
    public class DeviceLuaChangeTest
    {
        [Test]
        public void Constructor_NullArguments_BecomeEmptyStrings()
        {
            var change = new DeviceLuaChange(null, DeviceLuaChangeKind.Parameter,
                null, null, null);

            Assert.AreEqual(string.Empty, change.DeviceName);
            Assert.AreEqual(string.Empty, change.FieldName);
            Assert.AreEqual(string.Empty, change.OldValue);
            Assert.AreEqual(string.Empty, change.NewValue);
        }

        [TestCase(DeviceLuaChangeKind.Description, "Описание")]
        [TestCase(DeviceLuaChangeKind.SubType, "Подтип")]
        public void Caption_KnownKindsWithoutField_ReturnsCaption(
            DeviceLuaChangeKind kind, string expected)
        {
            var change = new DeviceLuaChange("TANK1V1", kind, "", "a", "b");

            Assert.AreEqual(expected, change.Caption);
        }

        [Test]
        public void Caption_ParameterAndPropertyAndRuntime_ContainsFieldName()
        {
            var parameter = new DeviceLuaChange("TANK1V1",
                DeviceLuaChangeKind.Parameter, "P_MIN_V", "1", "2");
            var property = new DeviceLuaChange("TANK1V1",
                DeviceLuaChangeKind.Property, "RSERIAL", "1", "2");
            var runtime = new DeviceLuaChange("TANK1V1",
                DeviceLuaChangeKind.RuntimeParameter, "P_ON_TIME", "1", "2");

            Assert.AreEqual("Параметр P_MIN_V", parameter.Caption);
            Assert.AreEqual("Свойство RSERIAL", property.Caption);
            Assert.AreEqual("Рабочий параметр P_ON_TIME", runtime.Caption);
        }

        [Test]
        public void Caption_UnknownKind_ReturnsFieldName()
        {
            var change = new DeviceLuaChange("TANK1V1",
                (DeviceLuaChangeKind)999, "FIELD", "1", "2");

            Assert.AreEqual("FIELD", change.Caption);
        }

        [Test]
        public void ToString_ReturnsDeviceCaptionAndValues()
        {
            var change = new DeviceLuaChange("TANK1V1",
                DeviceLuaChangeKind.Description, "", "old", "new");

            Assert.AreEqual("TANK1V1: Описание: \"old\" → \"new\"",
                change.ToString());
        }
    }

    public class DeviceLuaChangeApplierBranchTest
    {
        [TearDown]
        public void TearDown()
        {
            DeviceManager.GetInstance().Clear();
        }

        [Test]
        public void Apply_NullManagerOrChanges_ReturnsFalse()
        {
            var changes = new List<DeviceLuaChange>
            {
                new DeviceLuaChange("TANK1V1", DeviceLuaChangeKind.Description,
                    "", "a", "b"),
            };

            Assert.IsFalse(DeviceLuaChangeApplier.Apply(null, changes));
            Assert.IsFalse(DeviceLuaChangeApplier.Apply(
                DeviceManager.GetInstance(), null));
        }

        [Test]
        public void Apply_SkipsNullChangesAndEmptyDeviceNames()
        {
            var device = CreateAi("TANK2AI1", "old");
            AddDevice(device);

            var changes = new List<DeviceLuaChange>
            {
                null,
                new DeviceLuaChange("", DeviceLuaChangeKind.Description,
                    "", "a", "b"),
                new DeviceLuaChange("TANK2AI1",
                    DeviceLuaChangeKind.Description, "", "old", "new"),
            };

            bool subtypeChanged = DeviceLuaChangeApplier.Apply(
                DeviceManager.GetInstance(), changes);

            Assert.IsFalse(subtypeChanged);
            Assert.AreEqual("new", device.Description);
        }

        [Test]
        public void Apply_UnknownDevice_SkipsChange()
        {
            var device = CreateAi("TANK2AI1", "old");
            AddDevice(device);

            var changes = new List<DeviceLuaChange>
            {
                new DeviceLuaChange("UNKNOWN1",
                    DeviceLuaChangeKind.Description, "", "a", "b"),
            };

            bool subtypeChanged = DeviceLuaChangeApplier.Apply(
                DeviceManager.GetInstance(), changes);

            Assert.IsFalse(subtypeChanged);
            Assert.AreEqual("old", device.Description);
        }

        [Test]
        public void Apply_CapDevice_SkipsChange()
        {
            var device = CreateAi("TANK2AI1", StaticHelper.CommonConst.Cap);
            AddDevice(device);

            var changes = new List<DeviceLuaChange>
            {
                new DeviceLuaChange("TANK2AI1",
                    DeviceLuaChangeKind.Description, "", "cap", "new"),
            };

            bool subtypeChanged = DeviceLuaChangeApplier.Apply(
                DeviceManager.GetInstance(), changes);

            Assert.IsFalse(subtypeChanged);
            Assert.AreEqual(StaticHelper.CommonConst.Cap, device.Description);
        }

        [Test]
        public void Apply_SubtypeWithoutFunction_UpdatesDeviceSubType()
        {
            var device = new DO("TANK2DO1", "+TANK2-DO1", "desc", 1, "TANK", 2);
            device.SetSubType("DO");
            AddDevice(device);

            var changes = new List<DeviceLuaChange>
            {
                new DeviceLuaChange("TANK2DO1", DeviceLuaChangeKind.SubType,
                    "", "DO", "DO_VIRT"),
            };

            bool subtypeChanged = DeviceLuaChangeApplier.Apply(
                DeviceManager.GetInstance(), changes);

            Assert.IsTrue(subtypeChanged);
            Assert.AreEqual(DeviceSubType.DO_VIRT, device.DeviceSubType);
        }

        [Test]
        public void Apply_ParameterWithInvalidNumber_SkipsChange()
        {
            var device = CreateAi("TANK2AI1", "desc");
            device.SetParameter("P_MIN_V", 1.0);
            AddDevice(device);

            var changes = new List<DeviceLuaChange>
            {
                new DeviceLuaChange("TANK2AI1",
                    DeviceLuaChangeKind.Parameter, "P_MIN_V", "1", "abc"),
            };

            bool subtypeChanged = DeviceLuaChangeApplier.Apply(
                DeviceManager.GetInstance(), changes);

            Assert.IsFalse(subtypeChanged);
            Assert.AreEqual(1.0, device.Parameters[IODevice.Parameter.P_MIN_V]);
        }

        [Test]
        public void Apply_PropertyNotFound_WritesToPropertiesDictionary()
        {
            var device = CreateAi("TANK2AI1", "desc");
            AddDevice(device);

            var changes = new List<DeviceLuaChange>
            {
                new DeviceLuaChange("TANK2AI1",
                    DeviceLuaChangeKind.Property, "UNKNOWN_PROPERTY",
                    "", "value"),
            };

            bool subtypeChanged = DeviceLuaChangeApplier.Apply(
                DeviceManager.GetInstance(), changes);

            Assert.IsFalse(subtypeChanged);
            Assert.AreEqual("value", device.Properties["UNKNOWN_PROPERTY"]);
        }

        [Test]
        public void Apply_RuntimeParameter_ValidAndInvalidNumbers()
        {
            var device = CreateAi("TANK2AI1", "desc");
            AddDevice(device);

            var changes = new List<DeviceLuaChange>
            {
                new DeviceLuaChange("TANK2AI1",
                    DeviceLuaChangeKind.RuntimeParameter, "P_RT", "1", "abc"),
                new DeviceLuaChange("TANK2AI1",
                    DeviceLuaChangeKind.RuntimeParameter, "P_RT", "1", "3.5"),
            };

            bool subtypeChanged = DeviceLuaChangeApplier.Apply(
                DeviceManager.GetInstance(), changes);

            Assert.IsFalse(subtypeChanged);
        }

        private static AI CreateAi(string name, string description)
        {
            var device = new AI(name, "+" + name, description, 1, "TANK", 2);
            device.SetSubType("AI");
            return device;
        }

        private static void AddDevice(IODevice device)
        {
            var manager = DeviceManager.GetInstance();
            manager.Devices.Add(device);
            manager.Sort();
        }
    }

    public class DeviceLuaChangeComparerBranchTest
    {
        [Test]
        public void Compare_OrderedParameters_MapToNonNullParameters()
        {
            // У устройства 3 параметра, задан только один (nonNull = 1),
            // в lua одна позиция → сопоставление с непустым параметром.
            var device = new AI("TANK2AI1", "+TANK2-AI1", "desc", 1, "TANK", 2);
            device.SetSubType("AI");
            device.SetParameter("P_MIN_V", 1.0);

            var lua = new DeviceLuaSnapshot
            {
                Name = "TANK2AI1",
                Description = "desc",
                SubType = "AI",
            };
            lua.OrderedParameters.Add("0.5");

            var changes = DeviceLuaChangeComparer.Compare(
                new IODevice[] { device },
                new DeviceLuaSnapshot[] { lua });

            var parameterChange = changes.SingleOrDefault(c =>
                c.Kind == DeviceLuaChangeKind.Parameter);
            Assert.NotNull(parameterChange);
            Assert.AreEqual("P_MIN_V", parameterChange.FieldName);
            Assert.AreEqual("0.5", parameterChange.NewValue);
        }

        [Test]
        public void Compare_OrderedParametersWithAllSet_MapToAllParameters()
        {
            // Все параметры заданы (nonNull = all = 3), в lua одна позиция
            // → резервное сопоставление со всеми параметрами по порядку.
            var device = new AI("TANK2AI1", "+TANK2-AI1", "desc", 1, "TANK", 2);
            device.SetSubType("AI");
            device.SetParameter("P_C0", 2.0);
            device.SetParameter("P_MIN_V", 1.0);

            var lua = new DeviceLuaSnapshot
            {
                Name = "TANK2AI1",
                Description = "desc",
                SubType = "AI",
            };
            lua.OrderedParameters.Add("9");

            var changes = DeviceLuaChangeComparer.Compare(
                new IODevice[] { device },
                new DeviceLuaSnapshot[] { lua });

            var parameterChange = changes.SingleOrDefault(c =>
                c.Kind == DeviceLuaChangeKind.Parameter);
            Assert.NotNull(parameterChange);
            Assert.AreEqual("P_C0", parameterChange.FieldName);
        }

        [Test]
        public void Compare_OrderedParametersEqualAll_MapsByPosition()
        {
            var device = new AI("TANK2AI1", "+TANK2-AI1", "desc", 1, "TANK", 2);
            device.SetSubType("AI");
            device.SetParameter("P_C0", 1.0);
            device.SetParameter("P_MIN_V", 2.0);
            device.SetParameter("P_MAX_V", 3.0);

            var lua = new DeviceLuaSnapshot
            {
                Name = "TANK2AI1",
                Description = "desc",
                SubType = "AI",
            };
            lua.OrderedParameters.Add("1");
            lua.OrderedParameters.Add("7");
            lua.OrderedParameters.Add("3");

            var changes = DeviceLuaChangeComparer.Compare(
                new IODevice[] { device },
                new DeviceLuaSnapshot[] { lua });

            var parameterChange = changes.SingleOrDefault(c =>
                c.Kind == DeviceLuaChangeKind.Parameter);
            Assert.NotNull(parameterChange);
            Assert.AreEqual("P_MIN_V", parameterChange.FieldName);
            Assert.AreEqual("7", parameterChange.NewValue);
        }

        [Test]
        public void Compare_DeviceWithoutLuaParameters_NoParameterChanges()
        {
            var device = CreateAi("TANK2AI1", "desc");

            var lua = new DeviceLuaSnapshot
            {
                Name = "TANK2AI1",
                Description = "desc",
                SubType = "AI",
            };

            var changes = DeviceLuaChangeComparer.Compare(
                new IODevice[] { device },
                new DeviceLuaSnapshot[] { lua });

            Assert.IsEmpty(changes);
        }

        [Test]
        public void Snapshot_RuntimeParameterCollections_InitializedEmpty()
        {
            var lua = new DeviceLuaSnapshot();

            Assert.IsEmpty(lua.NamedRuntimeParameters);
            Assert.IsEmpty(lua.OrderedRuntimeParameters);
        }

        private static AI CreateAi(string name, string description)
        {
            var device = new AI(name, "+" + name, description, 1, "TANK", 2);
            device.SetSubType("AI");
            return device;
        }
    }
}
