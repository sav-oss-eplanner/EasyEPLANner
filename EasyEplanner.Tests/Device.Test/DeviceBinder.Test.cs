using EasyEPlanner;
using EplanDevice;
using NUnit.Framework;
using StaticHelper;
using System.Linq;
using System.Reflection;

namespace EasyEPlanner.Devices.Tests
{
    public class DeviceBinderTest
    {
        [TestCase("text\r\nline", "text\nline")]
        [TestCase("  spaced  ", "spaced")]
        [TestCase(null, "")]
        public void NormalizeFunctionalText_TrimsCarriageReturns(string input,
            string expected)
        {
            var result = InvokeStatic<string>("NormalizeFunctionalText", input);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void FunctionalTextContainsDevice_FindsDeviceInFunctionalText()
        {
            const string functionalText = "+TANK2-DO1\r\nОписание";
            var binder = CreateBinder();

            var contains = Invoke<bool>(binder, "FunctionalTextContainsDevice",
                functionalText, "+TANK2-DO1");

            Assert.IsTrue(contains);
        }

        [Test]
        public void FunctionalTextContainsDevice_ReturnsFalseForMissingDevice()
        {
            const string functionalText = "OTHER\r\nОписание";
            var binder = CreateBinder();

            var contains = Invoke<bool>(binder, "FunctionalTextContainsDevice",
                functionalText, "+TANK2-DO1");

            Assert.IsFalse(contains);
        }

        [Test]
        public void FunctionalTextContainsDevice_FindsDeviceAmongMultipleBindings()
        {
            const string functionalText =
                "+DEV1-DO1\r\nAction\r\n+TANK2-DO1\r\nOpen";
            var binder = CreateBinder();

            Assert.IsTrue(Invoke<bool>(binder, "FunctionalTextContainsDevice",
                functionalText, "+TANK2-DO1"));
            Assert.IsFalse(Invoke<bool>(binder, "FunctionalTextContainsDevice",
                functionalText, "+MISSING-DO9"));
        }

        [Test]
        public void PrepareBindToEmptyClamp_SetsSetDevicesChannel()
        {
            var binder = CreateBinder();
            var device = CreateBoundDoDevice();
            SetProperty(binder, "SelectedDevice", device);
            SetProperty(binder, "NewFunctionalText", "+TANK2-DO1\r\n");

            InvokeVoid(binder, "PrepareBindToEmptyClamp");

            Assert.AreEqual("+TANK2-DO1\r\n",
                GetProperty<string>(binder, "SetDevicesChannel"));
        }

        [Test]
        public void PrepareReplaceBinding_SetsResetAndSetChannels()
        {
            var binder = CreateBinder();
            const string newText = "+TANK2-DO1\r\nOpen\r\n";
            SetProperty(binder, "NewFunctionalText", newText);

            InvokeVoid(binder, "PrepareReplaceBinding", "+OLD-DO1\r\n");

            Assert.AreEqual("+OLD-DO1\r\n",
                GetProperty<string>(binder, "ResetDevicesChannel"));
            Assert.AreEqual(newText,
                GetProperty<string>(binder, "SetDevicesChannel"));
        }

        [Test]
        public void PrepareMultiBindWithCtrl_AppendsDeviceWhenCommentMatches()
        {
            var binder = CreateBinder();
            var device = CreateBoundDoDevice();
            var channel = device.Channels.Single();
            SetChannelComment(channel, "Open");
            SetProperty(binder, "SelectedDevice", device);
            SetProperty(binder, "SelectedChannel", channel);
            SetProperty(binder, "NewFunctionalText",
                "+TANK2-DO1\r\ndevice desc\r\nOpen\r\n");

            const string oldFunctionalText = "+OTHER-DO1\r\nOpen\r\n";
            InvokeVoid(binder, "PrepareMultiBindWithCtrl", oldFunctionalText);

            var newText = GetProperty<string>(binder, "NewFunctionalText");
            StringAssert.Contains("+OTHER-DO1", newText);
            StringAssert.Contains("+TANK2-DO1", newText);
            Assert.AreEqual("+TANK2-DO1\r\ndevice desc\r\nOpen\r\n",
                GetProperty<string>(binder, "SetDevicesChannel"));
        }

        [Test]
        public void PrepareMultiBindWithCtrl_ReplacesDeviceWhenAlreadyBound()
        {
            var binder = CreateBinder();
            var device = CreateBoundDoDevice();
            var channel = device.Channels.Single();
            SetChannelComment(channel, "Open");
            SetProperty(binder, "SelectedDevice", device);
            SetProperty(binder, "SelectedChannel", channel);
            SetProperty(binder, "NewFunctionalText",
                "+TANK2-DO1\r\ndevice desc\r\nOpen\r\n");

            const string oldFunctionalText = "+TANK2-DO1\r\nOpen\r\n+OTHER-DO2\r\n";
            InvokeVoid(binder, "PrepareMultiBindWithCtrl", oldFunctionalText);

            Assert.AreEqual("+TANK2-DO1\r\ndevice desc\r\nOpen\r\n",
                GetProperty<string>(binder, "ResetDevicesChannel"));
            Assert.AreEqual(CommonConst.Reserve,
                GetProperty<string>(binder, "NewFunctionalText"));
        }

        [Test]
        public void Bind_DeviceAndChannel_DoesNotThrowWhenBindingPreconditionsMissing()
        {
            var binder = CreateBinder();
            var device = CreateBoundDoDevice();

            Assert.DoesNotThrow(() =>
                binder.Bind(device, device.Channels.Single()));
        }

        private static DO CreateBoundDoDevice()
        {
            var device = new DO("TANK2DO1", "+TANK2-DO1", "device desc", 1, "TANK", 2);
            device.SetSubType("DO");
            return device;
        }

        private static void SetChannelComment(IODevice.IOChannel channel, string comment)
        {
            typeof(IODevice.IOChannel).GetField("comment",
                BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(channel, comment);
        }

        private static DeviceBinder CreateBinder()
        {
            // Helpers зависят от EPLAN DLL; для unit-тестов логики текста
            // привязки достаточно null (эти методы helper не используют).
            return new DeviceBinder(null, null);
        }

        private static T InvokeStatic<T>(string methodName, params object[] args)
        {
            var method = typeof(DeviceBinder).GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Static);
            return (T)method.Invoke(null, args);
        }

        private static T Invoke<T>(object instance, string methodName,
            params object[] args)
        {
            var method = instance.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)method.Invoke(instance, args);
        }

        private static void InvokeVoid(object instance, string methodName,
            params object[] args)
        {
            var method = instance.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(instance, args);
        }

        private static void SetProperty(object target, string name, object value)
        {
            var field = target.GetType().GetField($"<{name}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                target.GetType().GetProperty(name,
                    BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(target, value);
                return;
            }

            field.SetValue(target, value);
        }

        private static T GetProperty<T>(object target, string name)
        {
            var field = target.GetType().GetField($"<{name}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                return (T)field.GetValue(target);

            return (T)target.GetType().GetProperty(name,
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }
    }
}
