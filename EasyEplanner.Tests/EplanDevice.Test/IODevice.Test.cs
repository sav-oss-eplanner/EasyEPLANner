using NUnit.Framework;
using System.Linq;
using System.Windows.Forms;
using EplanDevice;
using System.Collections.Generic;
using System.Management.Instrumentation;
using System;
using Moq;
using StaticHelper;
using EasyEPlanner.FileSavers.XML;
using IO.ViewModel;

namespace Tests.EplanDevices
{
    public class IODeviceTest
    {
        class IODeviceFake : IODevice
        {
            public IODeviceFake(string name, string eplanName,
            string description, string deviceType, int deviceNumber,
            string objectName, int objectNumber) : base(name, eplanName,
                description, deviceType, deviceNumber, objectName, objectNumber)
            {
                OnPropertyChanged += () => InvokedOnPropertyChanged = true;
            }

            /// <summary>
            /// Property for checking invoking of OnPropertyChanged event
            /// </summary>
            public bool InvokedOnPropertyChanged { get; set; } = false;
        }

        [Test]
        public void Constructor_SetCorrectDevType_ReturnsDevType()
        {
            string settingDevType = "V"; //Valve
            DeviceType expectedDevType = DeviceType.V;
            
            var device = new IODeviceFake(string.Empty, string.Empty,
                string.Empty, settingDevType, 0, string.Empty, 0);

            Assert.AreEqual(expectedDevType, device.DeviceType);
        }

        [Test]
        public void Constructor_SetWrongDevType_ReturnsNoneDevType()
        {
            string settingDevType = "SuperDuperDev"; //Not exists
            DeviceType expectedDevType = DeviceType.NONE;

            var device = new IODeviceFake(string.Empty, string.Empty,
                string.Empty, settingDevType, 0, string.Empty, 0);

            Assert.AreEqual(expectedDevType, device.DeviceType);
        }

        [Test]
        public void SetProperty_SetCorrectValue_InvokeEventAndSetValue()
        {
            var device = new IODeviceFake(string.Empty, string.Empty,
                string.Empty, string.Empty, 0, string.Empty, 0);
            string propertyName = "TEST_PROPERTY";
            device.Properties.Add(propertyName, null);
            var propertyValue = "value";

            string errors = device.SetProperty(propertyName, propertyValue);

            Assert.Multiple(() =>
            {
                Assert.IsEmpty(errors);
                Assert.AreEqual(propertyValue, device.Properties[propertyName]);
                Assert.IsTrue(device.InvokedOnPropertyChanged);
            });
        }

        [Test]
        public void SetProperty_SetIncorrectValue_NotSetAndReturnErrorString()
        {
            var device = new IODeviceFake(string.Empty, string.Empty,
                string.Empty, string.Empty, 0, string.Empty, 0);
            string wrongPropertyName = "BlahBlahBlah";
            string anyValue = "JustValue";
            // Property is not exists, we will have error

            string errors = device.SetProperty(wrongPropertyName, anyValue);

            Assert.Multiple(() =>
            {
                Assert.IsNotEmpty(errors);
                Assert.IsFalse(device.InvokedOnPropertyChanged);
            });
        }


        [TestCaseSource(nameof(TestSortingChannelsForVDeviceData))]
        public void SortChannels_NewValveDevices_ReturnsSortedArrayOfChannels(
            IODevice dev, string subType, string[] expected)
        {
            dev.SetSubType(subType);
            dev.SortChannels();
            string[] actual = dev.Channels
                .Where(x => x.Comment != string.Empty)
                .Select(x => x.Comment ).ToArray();
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// 1 - Устройство V (клапан) для тестирования
        /// 2 - Задаваемый подтип
        /// 3 - Ожидаемое значение
        /// </summary>
        /// <returns></returns>
        public static object[] TestSortingChannelsForVDeviceData()
        {
            return new object[] 
            {
                new object[] 
                {
                    VTest.GetRandomVDevice(), 
                    "V_DO2", 
                    new string[] {"Открыть", "Закрыть"} 
                },
                new object[] 
                {
                    VTest.GetRandomVDevice(),
                    "V_DO1_DI2",
                    new string[] {"Открыт", "Закрыт"} 
                },
                new object[] 
                {
                    VTest.GetRandomVDevice(), 
                    "V_DO2_DI2",
                    new string[] {"Открыть", "Закрыть", "Открыт", "Закрыт"} },
                new object[] 
                {
                    VTest.GetRandomVDevice(),
                    "V_MIXPROOF",
                    new string[] {"Открыть", "Открыть ВС", "Открыть НС", 
                        "Открыт", "Закрыт"} 
                },
                new object[] 
                {
                    VTest.GetRandomVDevice(),
                    "V_BOTTOM_MIXPROOF",
                    new string[] {"Открыть", "Открыть мини", "Открыть НС", 
                        "Открыт", "Закрыт"} 
                },
            };
        }

        [TestCaseSource(nameof(GenerateDeviceTagsCaseSource))]
        public void GenerateDeviceTags_DeviceAODefault_ReturnsTree(
            IODevice dev, IDriver expectedRoot)
        {
            var root = new Driver(Mock.Of<IDeviceManager>(dm => dm.Devices == new List<IODevice>()));
            
            dev.GenerateDeviceTags(root);

            Assert.IsTrue(root.Subtypes.Count > 0);
            for (int i = 0; i < root.Subtypes.Count; i++)
            {
                var actualSubType = root.Subtypes[i];
                var expectedSubType = expectedRoot.Subtypes[i];
                Assert.AreEqual(expectedSubType.Description, actualSubType.Description);
                Assert.IsTrue(actualSubType.Channels.Count > 0);
                for (int j = 0; j < actualSubType.Channels.Count; j++)
                {
                    Assert.AreEqual(expectedSubType.Channels[j].Description, actualSubType.Channels[j].Description);
                }
            }
        }

        public static object[] GenerateDeviceTagsCaseSource()
        {
            string devName = "TANK1AO1";
            string eplanName = "+TANK1-AO1";
            string descr = "Сигнал AO";
            string objName = "TANK";
            int objNum = 1;
            int devNum = 1;

            var expectedRoot = new Driver(Mock.Of<IDeviceManager>());

            expectedRoot.AddChannel("AO_M", new Channel($"{devName}.M") { Comment = IODevice.Tag.M.Description});
            expectedRoot.AddChannel("AO_V", new Channel($"{devName}.V") { Comment = IODevice.Tag.V.Description });
            expectedRoot.AddChannel("AO_P_MIN_V", new Channel($"{devName}.P_MIN_V") { Comment = IODevice.Parameter.P_MIN_V.Description });
            expectedRoot.AddChannel("AO_P_MAX_V", new Channel($"{devName}.P_MAX_V") { Comment = IODevice.Parameter.P_MAX_V.Description });

            var dev = new AO(devName, eplanName, descr, devNum, objName, objNum);
            dev.SetSubType("AO");
            var defaultAODev = new object[] { dev, expectedRoot };

            return new object[] { defaultAODev };
        }

        [Test]
        public void SetIolConfProperty_NewDevice_AddNewProperties()
        {
            //Arrange
            var dev = new IODeviceFake(string.Empty, string.Empty,
                string.Empty, "V", 1, string.Empty, 1);
            int expectedInitialValueOfProps = 0;
            int actualInitialValueOfProps = dev.IolConfProperties.Count;
            var propsToSet = new Dictionary<string, double>
            {
                { "t1", 0 },
                { "t2", 1 },
                { "t3", 2 },
            };

            //Act
            foreach(var prop in propsToSet)
            {
                dev.SetIolConfProperty(prop.Key, prop.Value);
            }

            //Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(expectedInitialValueOfProps, actualInitialValueOfProps);
                Assert.AreEqual(propsToSet.Count, dev.IolConfProperties.Count);
                foreach(var prop in propsToSet)
                {
                    Assert.AreEqual(prop.Value, dev.IolConfProperties[prop.Key]);
                    Assert.IsTrue(dev.IolConfProperties.ContainsKey(prop.Key));
                }
            });
        }

        /// <summary>
        /// Проверка установки паракметров
        /// Вызывает исключение, так как связано с API
        /// </summary>
        [Test]
        public void UpdateParameters_ThrowsException()
        {
            IODevice dev = new AO("KOAG4AO1", "+KOAG4-AO1", "Test device", 1, "KOAG", 4);
            dev.SetSubType("AO");

            dev.SetParameter("P_MIN_V", 1);

            var functionMock = new Mock<IEplanFunction>();

            dev.Function = functionMock.Object;

            dev.UpdateParameters();

            functionMock.VerifySet(s => s.Parameters = "P_MIN_V=1");
            Assert.Pass();
        }

        [Test]
        public void UpdateParameters_WithoutFunction_DoesNotThrow()
        {
            IODevice dev = new AO("KOAG4AO1", "+KOAG4-AO1", "Test device", 1, "KOAG", 4);
            dev.SetSubType("AO");
            dev.SetParameter("P_MIN_V", 1);

            Assert.DoesNotThrow(() => dev.UpdateParameters());
            Assert.DoesNotThrow(() => dev.UpdateProperties());
            Assert.DoesNotThrow(() => dev.UpdateRuntimeParameters());
        }

        [Test]
        public void Check_CheckEmpty()
        {
            IODevice dev = new AO("KOAG4AO1", "+KOAG4-AO1", "Test device", 1, "KOAG", 4);
            dev.SetSubType("AO");

            string expectedResult = "KOAG4AO1 : не привязанный канал AO \"\".\nKOAG4AO1 : не задан параметр (доп. поле 3) \"P_MIN_V\".\nKOAG4AO1 : не задан параметр (доп. поле 3) \"P_MAX_V\".\n";

            Assert.AreEqual(expectedResult, dev.Check());
        }


        [Test]
        public void Check_NameLimit()
        {
            IODevice dev = new DO("QWERTYUIOP123456789DO1234567890", "+QWERTYUIOP123456789-DO1234567890", "Test device", 1234567890, "QWERTYUIOP", 123456789);
            dev.SetSubType("DO");

            string expectedResult = $"QWERTYUIOP123456789DO1234567890 : превышена длина названия устройства ({IODevice.DeviceNameLimit} символов).\n" +
                "QWERTYUIOP123456789DO1234567890 : не привязанный канал DO \"\".\n";

            Assert.AreEqual(expectedResult, dev.Check());
        }
    }
}
