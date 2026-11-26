using NUnit.Framework;
using System.Collections.Generic;
using TechObject;
using Moq;
using System.Reflection;
using EplanDevice;
using System;
using NUnit.Framework.Constraints;

namespace EasyEplanner.Tests
{
    public class BaseParameterTest
    {
        /// <summary>
        /// Реализация абстрактного класса BaseParameter для его тестирования
        /// </summary>
        class BaseParameterImplementation : BaseParameter
        {
            public BaseParameterImplementation(string luaName, string name,
                string defaultValue = "",
                List<DisplayObject> displayObjects = null) 
                : base(luaName, name, defaultValue, displayObjects) { }

            public override BaseParameter Clone()
            {
                return new BaseParameterImplementation(LuaName, Name,
                    DefaultValue, DisplayObjects);
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            typeof(BaseParameter).GetField("deviceManager",
                BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, GetMoqForSetValuesAndDisplayTextTest());
        }

        [Test]
        public void DisplayObjectsFromNewObj_EmptyObject_ReturnsNoDisplayObjects()
        {
            var nonDisplayObjects = new List<BaseParameter.DisplayObject>
            {
                BaseParameter.DisplayObject.None
            };

            var newObj = new BaseParameterImplementation(stub, stub);

            Assert.AreEqual(nonDisplayObjects, newObj.DisplayObjects);
        }

        [Test]
        public void DisplayObjectsFromNewObj_EmptyObject_ReturnsAllDisplayObjects()
        {
            var allDisplayObjects = new List<BaseParameter.DisplayObject>
            {
                BaseParameter.DisplayObject.Signals,
                BaseParameter.DisplayObject.Parameters,
                BaseParameter.DisplayObject.AI,
                BaseParameter.DisplayObject.AO,
                BaseParameter.DisplayObject.DI,
                BaseParameter.DisplayObject.DO,
            };

            var newObj = new BaseParameterImplementation(stub, stub, stub,
                allDisplayObjects);

            Assert.AreEqual(allDisplayObjects, newObj.DisplayObjects);
        }

        [Test]
        public void CreateObject_NoObject_ReturnsObject()
        {
            var obj = new BaseParameterImplementation(stub, stub);
            string[] expectedEditText = new string[] { obj.LuaName, obj.Value };

            Assert.Multiple(() =>
            {
                Assert.AreEqual(obj.LuaName, stub);
                Assert.AreEqual(obj.Name, stub);
                Assert.IsTrue(obj.IsUseDevList);
                Assert.IsTrue(obj.IsEmpty);
                Assert.IsNull(obj.Owner);
                Assert.AreEqual(expectedEditText, obj.EditText);
                Assert.AreEqual(obj.CurrentValueType,
                    BaseParameter.ValueType.None);
            });
        }

        [Test]
        public void Clone_EmptyObject_ReturnsTheSameButWithAnotherHash()
        {
            string name = "Name";
            string luaName = "LuaName";
            string defaultValue = "defaultValue";
            var displayObjects = new List<BaseParameter.DisplayObject>
            {
                BaseParameter.DisplayObject.Parameters
            };
            var obj = new BaseParameterImplementation(luaName, name,
                defaultValue, displayObjects);

            var cloned = obj.Clone();

            Assert.Multiple(() =>
            {
                Assert.AreNotEqual(cloned.GetHashCode(), obj.GetHashCode());
                Assert.AreEqual(cloned.Name, obj.Name);
                Assert.AreEqual(cloned.LuaName, obj.LuaName);
                Assert.AreEqual(cloned.DefaultValue, obj.DefaultValue);
                Assert.AreEqual(cloned.DisplayObjects, obj.DisplayObjects);
                Assert.AreEqual(cloned.CurrentValueType, obj.CurrentValueType);
            });
        }

        [Test]
        public void SaveToPrgLua_ParameterDisplayObjectAndUnknownValue_ReturnsEmpty()
        {
            var obj = new BaseParameterImplementation("property", "Property",
                "", new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.Parameters,
                });
            obj.SetNewValue("UNKNOWN_PARAMETER");

            Assert.AreEqual(string.Empty, obj.SaveToPrgLua(string.Empty));
        }

        [Test]
        public void SaveToPrgLua_WithoutDisplayObjectRestrictions_SkipsRawValue()
        {
            var obj = new BaseParameterImplementation("property", "Property");
            obj.SetNewValue("raw_value");

            Assert.AreEqual(string.Empty, obj.SaveToPrgLua(string.Empty));
        }

        [TestCaseSource(nameof(SaveToPrgLuaCanSaveCaseSource))]
        public void SaveToPrgLua_RespectsDisplayObjects(
            List<BaseParameter.DisplayObject> displayObjects, string value,
            string expected)
        {
            var obj = new BaseParameterImplementation("property", "Property",
                "", displayObjects);
            SetUpParameterBaseOperationOwner(obj);
            obj.SetNewValue(value);

            Assert.AreEqual(expected, obj.SaveToPrgLua(string.Empty));
        }

        private static object[] SaveToPrgLuaCanSaveCaseSource()
        {
            return new object[]
            {
                new object[]
                {
                    new List<BaseParameter.DisplayObject>
                    {
                        BaseParameter.DisplayObject.Parameters,
                    },
                    "parameter1",
                    "property = prg.techobject1.PAR_FLOAT.parameter1",
                },
                new object[]
                {
                    new List<BaseParameter.DisplayObject>
                    {
                        BaseParameter.DisplayObject.AI,
                    },
                    "NORM1DEV1",
                    "property = prg.control_modules.NORM1DEV1",
                },
                new object[]
                {
                    new List<BaseParameter.DisplayObject>
                    {
                        BaseParameter.DisplayObject.AI,
                    },
                    "NORM1DEV2",
                    string.Empty,
                },
                new object[]
                {
                    new List<BaseParameter.DisplayObject>
                    {
                        BaseParameter.DisplayObject.Parameters,
                        BaseParameter.DisplayObject.Signals,
                    },
                    "parameter1",
                    "property = prg.techobject1.PAR_FLOAT.parameter1",
                },
                new object[]
                {
                    new List<BaseParameter.DisplayObject>
                    {
                        BaseParameter.DisplayObject.Parameters,
                        BaseParameter.DisplayObject.Signals,
                    },
                    "NORM1DEV1",
                    "property = prg.control_modules.NORM1DEV1",
                },
                new object[]
                {
                    new List<BaseParameter.DisplayObject>
                    {
                        BaseParameter.DisplayObject.Parameters,
                        BaseParameter.DisplayObject.Signals,
                    },
                    "UNKNOWN",
                    string.Empty,
                },
            };
        }

        [TestCaseSource(nameof(AddDisplayObjectCaseSource))]
        public void AddDisplayObject_EmptyParameter_AddArgumentOrReplac(
            List<string> actualEnumNames,
            List<BaseParameter.DisplayObject> expected)
        {
            var newObject = new BaseParameterImplementation(stub, stub);

            foreach (var displayObject in actualEnumNames)
            {
                newObject.AddDisplayObject(displayObject);
            }

            Assert.AreEqual(expected, newObject.DisplayObjects);
        }

        private static object[] AddDisplayObjectCaseSource()
        {
            return new object[]
            {
                new object[]
                {
                    new List<string>
                    {
                        "none"
                    },
                    new List<BaseParameter.DisplayObject>
                    {
                        BaseParameter.DisplayObject.None,
                    },
                },
                new object[]
                {
                    new List<string>
                    {
                        "signals"
                    },
                    new List<BaseParameter.DisplayObject>
                    {
                        BaseParameter.DisplayObject.Signals,
                    },
                },
                new object[]
                {
                    new List<string>
                    {
                        "parameters"
                    },
                    new List<BaseParameter.DisplayObject>
                    {
                        BaseParameter.DisplayObject.Parameters,
                    },
                },
                new object[]
                {
                    new List<string>
                    {
                        "operation"
                    },
                    new List<BaseParameter.DisplayObject>
                    {
                        BaseParameter.DisplayObject.Operation,
                    },
                },
                new object[]
                {
                    new List<string>
                    {
                        "parameters",
                        "signals"
                    },
                    new List<BaseParameter.DisplayObject>
                    {
                        BaseParameter.DisplayObject.Parameters,
                        BaseParameter.DisplayObject.Signals,
                    },
                },
            };
        }

        [TestCaseSource(nameof(GetDisplayObjectsCaseSource))]
        public void GetDisplayObjects_NewObject_ReturnsExpectedValues(
            List<BaseParameter.DisplayObject> displayObjects,
            EplanDevice.DeviceType[] expectedDevTypes,
            EplanDevice.DeviceSubType[] expectedDevSubTypes,
            bool expectedDisplayParameters)
        {
            var newObj = new BaseParameterImplementation(stub, stub, stub,
                displayObjects);

            newObj.GetDisplayObjects(out EplanDevice.DeviceType[] devTypes,
                out EplanDevice.DeviceSubType[] devSubTypes,
                out bool displayParameters);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(expectedDevTypes, devTypes);
                Assert.AreEqual(expectedDevSubTypes, devSubTypes);
                Assert.AreEqual(expectedDisplayParameters, displayParameters);
            });
        }

        private static object[] GetDisplayObjectsCaseSource()
        {
            return new object[]
            {
                new object[]
                {
                    new List<BaseParameter.DisplayObject>
                    {
                        BaseParameter.DisplayObject.Signals,
                        BaseParameter.DisplayObject.Parameters
                    },
                    new DeviceType[]
                    {
                        DeviceType.AI,
                        DeviceType.AO,
                        DeviceType.DI, DeviceType.SB, DeviceType.LS, DeviceType.FS, DeviceType.GS, DeviceType.TS,
                        DeviceType.DO, DeviceType.HL,
                    },
                    null,
                    true,
                },
                new object[]
                {
                    new List<BaseParameter.DisplayObject>
                    {
                        BaseParameter.DisplayObject.Signals,
                    },
                    new DeviceType[]
                    {
                        DeviceType.AI,
                        DeviceType.AO,
                        DeviceType.DI, DeviceType.SB, DeviceType.LS, DeviceType.FS, DeviceType.GS, DeviceType.TS,
                        DeviceType.DO, DeviceType.HL,
                    },
                    null,
                    false,
                },
                new object[]
                {
                    new List<BaseParameter.DisplayObject>
                    {
                        BaseParameter.DisplayObject.DI,
                    },
                    new DeviceType[]
                    {
                        DeviceType.DI, DeviceType.SB, DeviceType.LS, DeviceType.FS, DeviceType.GS, DeviceType.TS,
                    },
                    null,
                    false,
                },
                new object[]
                {
                    new List<BaseParameter.DisplayObject>
                    {
                        BaseParameter.DisplayObject.Parameters
                    },
                    new DeviceType[] { },
                    null,
                    true,
                },
                new object[]
                {
                    new List<BaseParameter.DisplayObject>
                    {
                        BaseParameter.DisplayObject.Operation
                    },
                    new DeviceType[] { },
                    null,
                    false,
                },
                new object[]
                {
                    null,
                    new DeviceType[] { },
                    null,
                    false,
                },
            };
        }


        [TestCase("NORM1DEV1", true)]
        [TestCase("STUB1DEV1", false)]
        public void Highlight(string devname, bool expected)
        {
            string name = "Name";
            string luaName = "LuaName";

            var parameter = new BaseParameterImplementation(luaName, name, stub, new List<BaseParameter.DisplayObject>() { BaseParameter.DisplayObject.Signals });

            parameter.SetNewValue(devname);

            var rslt = parameter.GetObjectToDrawOnEplanPage();

            Assert.IsTrue(parameter.IsDrawOnEplanPage);
            Assert.AreEqual(expected, rslt.Count > 0);
        }

        [TestCaseSource(nameof(SetValueAndSetNewValueTestCaseSource))]
        public void SetNewValue_NewObject_ProcessValueReturnsCorrect(
            string setValue, string expectedValue)
        {
            string name = "Name";
            string luaName = "LuaName";

            var parameter = new BaseParameterImplementation(luaName, name, stub, null);

            parameter.SetNewValue(setValue);

            Assert.AreEqual(expectedValue, parameter.Value);
        }

        [TestCaseSource(nameof(SetValueAndSetNewValueTestCaseSource))]
        public void SetValue_NewObject_ProcessValueReturnsCorrect(
            string setValue, string expectedValue)
        {
            string name = "Name";
            string luaName = "LuaName";
            var parameter = new BaseParameterImplementation(luaName, name, stub, null);

            parameter.SetValue(setValue);

            Assert.AreEqual(expectedValue, parameter.Value);
        }

        private static object[] SetValueAndSetNewValueTestCaseSource()
        {
            return new object[]
            {
                new object[] { "1", "1" },
                new object[] { "200", "200" },
                new object[] { "-300", "-300" },
                new object[] { "EDCBSSWE", "EDCBSSWE" },
                new object[] { "", "" },
                new object[] { "0", "0" },
                new object[] { "Два один четыре", "Два один четыре" },
                new object[] { "4.5", "4.5" },
                new object[] { "Нет", "Нет" },
                new object[] { "NORM1DEV2 NORM1DEV1", "NORM1DEV2 NORM1DEV1" },
                new object[] { "NORM1DEV1 NORM1DEV1", "NORM1DEV1" },
            };
        }

        [TestCase("1", "1")]
        [TestCase("200", "200")]
        [TestCase("-300", "-300")]
        [TestCase("EDCBSSWE", "EDCBSSWE")]
        [TestCase("", "")] // Default value - ""
        [TestCase("0", "0")]
        [TestCase("Два один четыре", "Два один четыре")]
        [TestCase("4.5", "4.5")]
        [TestCase("Нет", "Нет")]
        [TestCase("NORM1DEV2 NORM1DEV1", "NORM1DEV2 NORM1DEV1")]
        [TestCase("NORM1DEV1 NORM1DEV1", "NORM1DEV1")]
        public void DisplayText_NewObjectSetNewvalue_ReturnsCorrectDisplayText(
            string setValue, string expectedParameterValue)
        {
            string name = "Name";
            string luaName = "LuaName";
            var parameter = new BaseParameterImplementation(luaName, name, stub, null);
            parameter.SetNewValue(setValue);

            var expectedDisplayText = new string[]
            {
                name,
                expectedParameterValue
            };
            Assert.AreEqual(expectedDisplayText, parameter.DisplayText);
        }

        [Test]
        public void DisplayText_OperationDisplayObjectWithExistingNumber_ReturnsOperationDisplayText()
        {
            string name = "Name";
            var parameter = new BaseParameterImplementation("LuaName", name,
                stub, new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.Operation
                });
            SetUpParameterBaseTechObjectOwner(parameter);

            parameter.SetNewValue("2");

            Assert.AreEqual(new[] { name, "2. modeName_2" }, parameter.DisplayText);
        }

        [Test]
        public void DisplayText_OperationDisplayObjectWithUnknownNumber_ReturnsRawValue()
        {
            string name = "Name";
            var parameter = new BaseParameterImplementation("LuaName", name,
                stub, new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.Operation
                });
            SetUpParameterBaseTechObjectOwner(parameter);

            parameter.SetNewValue("3");

            Assert.AreEqual(new[] { name, "3" }, parameter.DisplayText);
        }

        [Test]
        public void DisplayText_AggregateParameterWithOperationDisplayObject_ReturnsAggregateOperationDisplayText()
        {
            string name = "Name";
            var parameter = new ActiveAggregateParameter("LuaName", name,
                stub, new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.Operation
                });
            SetUpAggregateParameterWithDifferentOwnerOperations(parameter);

            parameter.SetNewValue("2");

            Assert.AreEqual(new[] { name, "2. aggregateMode_2" },
                parameter.DisplayText);
        }

        [Test]
        public void Check_AggregateParameterWithOperationDisplayObject_ChecksAggregateOperations()
        {
            var logMock = new Mock<EasyEPlanner.ILog>();
            var logMessages = new List<string>();
            logMock.Setup(l => l.AddMessage(It.IsAny<string>()))
                .Callback<string>(msg => logMessages.Add(msg));
            EasyEPlanner.Logs.Init(logMock.Object);

            var parameter = new ActiveAggregateParameter("LuaName", "Name",
                stub, new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.Operation
                });
            SetUpAggregateParameterWithDifferentOwnerOperations(parameter);

            parameter.SetNewValue("3");
            parameter.Check();

            Assert.Multiple(() =>
            {
                Assert.AreEqual("3", parameter.Value);
                Assert.AreEqual(1, logMessages.Count);
                Assert.IsTrue(logMessages[0].Contains(
                    "может быть установлен только номер существующей операции"));
            });
        }

        [TestCase("1", BaseParameter.ValueType.Number)]
        [TestCase("200", BaseParameter.ValueType.Number)]
        [TestCase("-300", BaseParameter.ValueType.Number)]
        [TestCase("EDCBSSWE", BaseParameter.ValueType.Other)]
        [TestCase("", BaseParameter.ValueType.None)]
        [TestCase("0", BaseParameter.ValueType.Number)]
        [TestCase("Два один четыре", BaseParameter.ValueType.Other)]
        [TestCase("4.5", BaseParameter.ValueType.Other)]
        [TestCase("Нет", BaseParameter.ValueType.Stub)]
        [TestCase("NORM1DEV2", BaseParameter.ValueType.Device)]
        [TestCase("STUB1DEV1 NORM1DEV1", BaseParameter.ValueType.Other)]
        [TestCase("NORM1DEV2 NORM1DEV1", BaseParameter.ValueType.ManyDevices)]
        [TestCase("NORM1DEV1 NORM1DEV1", BaseParameter.ValueType.ManyDevices)]
        [TestCase("STUB1DEV1 STUB1DEV2", BaseParameter.ValueType.Other)]
        [TestCase("STUB1DEV1", BaseParameter.ValueType.Other)]
        // Can't be tested yet, have to make DI
        //[TestCase("Param_Name", BaseParameter.ValueType.Parameter)]
        // Can be tested only for Bool parameters (this one is not bool)
        //[TestCase(true, BaseParameter.ValueType.Boolean)]
        public void CurrentValueType_NewObjectSetNewValue(string setValue,
            BaseParameter.ValueType expectedValueType)
        {
            string name = "Name";
            string luaName = "LuaName";
            var parameter = new BaseParameterImplementation(luaName, name, string.Empty, null);

            parameter.SetNewValue(setValue);

            Assert.AreEqual(expectedValueType, parameter.CurrentValueType);
        }

        [TestCase("NORM1DEV1", "\t\t", "\t\tLuaName = prg.control_modules.NORM1DEV1")]
        [TestCase("NORM1DEV1 NORM1DEV2", "\t\t", "\t\tLuaName = { prg.control_modules.NORM1DEV1, prg.control_modules.NORM1DEV2 }")]
        [TestCase("1", "\t", "\tLuaName = prg.0.operations.BASEOPERATIONLUANAME1")]
        [TestCase("2", "\t", "\tLuaName = prg.0.operations.BASEOPERATIONLUANAME2")]
        [TestCase("other", "", "")]
        public void SaveToPrgLua_CheckBaseTechObjectOwner(string value, string prefix, string expected)
        {
            var parameter = new BaseParameterImplementation("LuaName", "Name", stub, null);
            SetUpParameterBaseTechObjectOwner(parameter);

            parameter.SetNewValue(value);
            var res = parameter.SaveToPrgLua(prefix);

            Assert.AreEqual(expected, res);
        }

        private void SetUpParameterBaseTechObjectOwner(BaseParameter parameter)
        {
            var techObject = new TechObject
               .TechObject("techObjectName", getN => 0, 0,
                   0, "techObjectEplanName", 0, "techObjectNameBC",
                   "attachedObjects", null);
            techObject.GetParamsManager().CompleteInit();

            parameter.Owner = new BaseTechObject(techObject);
            var modesManager = (parameter.Owner as BaseTechObject)
                .Owner.ModesManager;
            var modes = new List<Mode>
            {
                new Mode("modeName_1", getN => 1, modesManager),
                new Mode("modeName_2", getN => 2, modesManager),
            };

            modes[0].BaseOperation.Name = "baseOperationName1";
            modes[0].BaseOperation.LuaName = "baseOperationLuaName1";

            modes[1].BaseOperation.Name = "baseOperationName2";
            modes[1].BaseOperation.LuaName = "baseOperationLuaName2";

            parameter.BaseOperation = new BaseOperation(modes[0]);
            parameter.Parent = parameter.BaseOperation;

            modesManager.Modes.AddRange(modes);
        }

        private void SetUpAggregateParameterWithDifferentOwnerOperations(
            BaseParameter parameter)
        {
            var mainTechObject = new TechObject.TechObject("mainTechObject",
                getN => 0, 0, 0, "mainTechObjectEplanName", 0,
                "mainTechObjectNameBC", "attachedObjects", null);
            var aggregateTechObject = new TechObject.TechObject(
                "aggregateTechObject", getN => 0, 0, 0,
                "aggregateTechObjectEplanName", 0,
                "aggregateTechObjectNameBC", "attachedObjects", null);

            var mainModesManager = mainTechObject.ModesManager;
            var aggregateModesManager = aggregateTechObject.ModesManager;
            var mainModes = new List<Mode>
            {
                new Mode("mainMode_1", getN => 1, mainModesManager),
                new Mode("mainMode_2", getN => 2, mainModesManager),
                new Mode("mainMode_3", getN => 3, mainModesManager),
            };
            var aggregateModes = new List<Mode>
            {
                new Mode("aggregateMode_1", getN => 1, aggregateModesManager),
                new Mode("aggregateMode_2", getN => 2, aggregateModesManager),
            };

            mainModesManager.Modes.AddRange(mainModes);
            aggregateModesManager.Modes.AddRange(aggregateModes);

            parameter.Owner = new BaseTechObject(aggregateTechObject);
            parameter.BaseOperation = new BaseOperation(mainModes[0]);
            parameter.Parent = parameter.BaseOperation;
        }

        [TestCase("parameter1", "\t", "\tLuaName = prg.techobject1.PAR_FLOAT.parameter1")]
        [TestCase("parameter2", "\t", "\tLuaName = prg.techobject1.PAR_FLOAT.parameter2")]
        [TestCase("1", "\t", "\tLuaName = prg.techobject1.PAR_FLOAT.parameter1")]
        [TestCase("2", "\t", "\tLuaName = prg.techobject1.PAR_FLOAT.parameter2")]
        [TestCase("other", "", "")]
        public void SaveToPrgLua_CheckBaseOperationOwner(string value, string prefix,
            string expected)
        {
            var parameter = new ActiveParameter("LuaName", "Name", "", new List<BaseParameter.DisplayObject>() { BaseParameter.DisplayObject.Parameters });
            SetUpParameterBaseOperationOwner(parameter);

            parameter.SetNewValue(value);
            var res = parameter.SaveToPrgLua(prefix);
            Assert.AreEqual(expected, res);
        }

        [TestCase("parameter1", "1. parameter1")]
        [TestCase("1", "1. parameter1")]
        [TestCase("2", "2. parameter2")]
        public void DisplayText_OperationParameterByNameOrNumber(
            string value, string expectedDisplayValue)
        {
            var parameter = new ActiveParameter("LuaName", "Name", "",
                new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.Parameters
                });
            SetUpParameterBaseOperationOwner(parameter);

            parameter.SetNewValue(value);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(expectedDisplayValue, parameter.DisplayText[1]);
                Assert.AreEqual(value, parameter.Value,
                    "the value should stay in the existing serialized format");
                Assert.AreEqual(
                    $"\tLuaName = prg.techobject1.PAR_FLOAT.{expectedDisplayValue.Split(' ')[1]}",
                    parameter.SaveToPrgLua("\t"));
            });
        }

        [TestCase("parameter1")]
        [TestCase("1")]
        public void SetExtraPropertiesAndSaveAsLuaTable_LinkedParameter_KeepLegacyFormat(
            string value)
        {
            var parameter = new ActiveParameter("LuaName", "Name", "",
                new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.Parameters
                });
            SetUpParameterBaseOperationOwner(parameter);
            var operation = (BaseOperation)parameter.Owner;
            operation.Properties.Add(parameter);
            operation.RootProperties.Add(parameter);

            operation.SetExtraProperties(new Dictionary<string, string>
            {
                [parameter.LuaName] = value
            });

            Assert.Multiple(() =>
            {
                Assert.AreEqual(value, parameter.Value);
                Assert.AreEqual(
                    $"props =\n\t{{\n\tLuaName = '{value}',\n\t}},\n",
                    operation.SaveAsLuaTable(string.Empty));
            });
        }

        private void SetUpParameterBaseOperationOwner(BaseParameter parameter)
        {
            var techObject = new TechObject.TechObject("techObjectName",
                getN => 1, 1, 1, "techObjectEplanName", 1, "techObjectNameBC",
                "attachedObjects", null);
            techObject.NameEplanForFile = "TechObject";
            techObject.GetParamsManager().AddFloatParam("параметр 1", 1, "unit", "parameter1");
            techObject.GetParamsManager().AddFloatParam("параметр 2", 5, "unit", "parameter2");
            techObject.GetParamsManager().CompleteInit();

            var operation = new Mode("operation1", getN => 1, new ModesManager(techObject));
            
            var baseOperation = new BaseOperation(operation);

            parameter.Owner = baseOperation;
        }

        [TestCase("true", "\t", "\tLuaName = true")]
        [TestCase("false", "\t\t", "\t\tLuaName = false")]
        public void SaveToPrgLua_CheckActiveBoolParameter(string value, string prefix, string expected)
        {
            var parameter = new ActiveBoolParameter("LuaName", "Name", "false", null);

            parameter.SetNewValue(value);
            var res = parameter.SaveToPrgLua(prefix);

            Assert.AreEqual(expected, res);
        }

        [TestCase("NORM1DEV2", "NORM", 2, "NORM2DEV2")]
        [TestCase("NORM1DEV2", "OTHER", 1, "OTHER1DEV2")]
        public void ModifyDevNames(
            string source,
            string newName, int newNumber,
            string expected)
        {
            var parameter = new ActiveParameter("LuaName", "Name");
            parameter.SetNewValue(source);

            var options = Mock.Of<IDevModifyOptions>(o => 
                o.NewTechObjectName == newName &&
                o.NewTechObjectNumber == newNumber);

            parameter.ModifyDevNames(options);

            Assert.AreEqual(expected, parameter.Value);
        }

        /// <summary>
        /// Реальный <see cref="IODevice"/> для тестов: <see cref="IDeviceManager.GetDevice"/>
        /// возвращает именно тип <see cref="IODevice"/>, мок-только <see cref="IDevice"/> нельзя безопасно привести.
        /// </summary>
        private sealed class TestIODevice : IODevice
        {
            public TestIODevice(string name, string description, DeviceType deviceType)
                : base(name, name, description, deviceType.ToString(), 1, "O", 1)
            {
            }
        }

        private EplanDevice.IDeviceManager GetMoqForSetValuesAndDisplayTextTest()
        {
            string stubDev1Name = "STUB1DEV1";
            string stubDev2Name = "STUB1DEV2";
            string norm_1_dev_1_name = "NORM1DEV1";
            string norm_1_dev_2_name = "NORM1DEV2";
            string norm_2_dev_2_name = "NORM2DEV2";
            string other_1_dev_2_name = "OTHER1DEV2";


            int norm_1_dev_1_index = 1;
            int stubDev1Index = 2;
            int norm_1_dev_2_index = 3;
            int stubDev2Index = 4;
            int norm_2_dev_2_index = 5;
            int other_1_dev_2_index = 6;

            var stubDevice1 = new TestIODevice(stubDev1Name, StaticHelper.CommonConst.Cap,
                DeviceType.NONE);
            var stubDevice2 = new TestIODevice(stubDev2Name, StaticHelper.CommonConst.Cap,
                DeviceType.NONE);

            var norm_1_dev_1 = new TestIODevice(norm_1_dev_1_name, "Description 1",
                DeviceType.AI);
            var norm_1_dev_2 = new TestIODevice(norm_1_dev_2_name, "Description 2",
                DeviceType.DO);

            var norm_2_dev_2 = new TestIODevice(norm_2_dev_2_name, "Description",
                DeviceType.DO);

            var other_1_dev_2 = new TestIODevice(other_1_dev_2_name, "Description",
                DeviceType.DO);

            var deviceManagerMock = Mock.Of<EplanDevice.IDeviceManager>(
                d => d.GetDeviceByEplanName(stubDev1Name) == stubDevice1 &&
                d.GetDeviceByEplanName(It.IsAny<string>()) == stubDevice1 &&
                d.GetDeviceByEplanName(stubDev2Name) == stubDevice2 &&
                d.GetDeviceByEplanName(norm_1_dev_1_name) == norm_1_dev_1 &&
                d.GetDeviceByEplanName(norm_1_dev_2_name) == norm_1_dev_2 &&
                d.GetDeviceByEplanName(norm_2_dev_2_name) == norm_2_dev_2 &&
                d.GetDeviceByEplanName(other_1_dev_2_name) == other_1_dev_2 &&
                d.IsExistingDeviceByEplanName(stubDev1Name) == false &&
                d.IsExistingDeviceByEplanName(stubDev2Name) == false &&
                d.IsExistingDeviceByEplanName(norm_1_dev_1_name) == true &&
                d.IsExistingDeviceByEplanName(norm_1_dev_2_name) == true &&
                d.IsExistingDeviceByEplanName(norm_2_dev_2_name) == true &&
                d.IsExistingDeviceByEplanName(other_1_dev_2_name) == true &&
                d.GetDevice(stubDev1Name) == stubDevice1 &&
                d.GetDevice(stubDev2Name) == stubDevice2 &&
                d.GetDevice(norm_1_dev_1_name) == norm_1_dev_1 &&
                d.GetDevice(norm_1_dev_2_name) == norm_1_dev_2 &&
                d.GetDevice(norm_2_dev_2_name) == norm_2_dev_2 &&
                d.GetDevice(other_1_dev_2_name) == other_1_dev_2 &&
                d.GetDeviceByIndex(It.IsAny<int>()) == stubDevice1 &&
                d.GetDeviceByIndex(norm_1_dev_1_index) == norm_1_dev_1 &&
                d.GetDeviceByIndex(norm_1_dev_2_index) == norm_1_dev_2 &&
                d.GetDeviceByIndex(stubDev1Index) == stubDevice1 &&
                d.GetDeviceByIndex(stubDev2Index) == stubDevice2 &&
                d.GetDeviceByIndex(norm_2_dev_2_index) == norm_2_dev_2 &&
                d.GetDeviceByIndex(other_1_dev_2_index) == other_1_dev_2 &&
                d.GetDeviceIndex(It.IsAny<string>()) == -1 &&
                d.GetDeviceIndex(norm_1_dev_1_name) == norm_1_dev_1_index &&
                d.GetDeviceIndex(norm_1_dev_2_name) == norm_1_dev_2_index &&
                d.GetDeviceIndex(stubDev1Name) == stubDev1Index &&
                d.GetDeviceIndex(stubDev2Name) == stubDev2Index &&
                d.GetDeviceIndex(norm_2_dev_2_name) == norm_2_dev_2_index &&
                d.GetDeviceIndex(other_1_dev_2_name) == other_1_dev_2_index &&
                d.GetModifiedDevice(norm_1_dev_2, It.Is<IDevModifyOptions>(o => o.NewTechObjectNumber == 2)) == norm_2_dev_2 &&
                d.GetModifiedDevice(norm_1_dev_2, It.Is<IDevModifyOptions>(o => o.NewTechObjectName == "OTHER")) == other_1_dev_2);

            return deviceManagerMock;
        }

        [Test]
        public void Check()
        {
            var logMock = new Mock<EasyEPlanner.ILog>();
            var logMessages = new List<string>();
            logMock.Setup(l => l.AddMessage(It.IsAny<string>()))
                .Callback<string>(msg => logMessages.Add(msg));
            EasyEPlanner.Logs.Init(logMock.Object);

            // IsEmpty -> ранний выход
            var emptyParameter = new ActiveParameter("bp", "");
            emptyParameter.Check();
            Assert.AreEqual(0, logMessages.Count);

            // Owner не BaseOperation -> ранний выход
            var noOwnerParameter = new ActiveParameter("bp", "");
            noOwnerParameter.SetNewValue("qwe");
            noOwnerParameter.Check();
            Assert.AreEqual(0, logMessages.Count);

            // Disabled -> ранний выход
            var disabledParameter = new ActiveParameter("bp", "",
                "", new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.Parameters
                })
            {
                Disabled = true,
                Owner = new BaseOperation("", "",
                    new List<BaseParameter>(), new Dictionary<string, List<BaseStep>>())
            };
            disabledParameter.SetNewValue("qwe");
            disabledParameter.Check();
            Assert.AreEqual("qwe", disabledParameter.Value);
            Assert.AreEqual(0, logMessages.Count);

            // ValueType.Device + Signals -> без ошибки
            var allowedDeviceParameter = new ActiveParameter("bp", "",
                "", new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.Signals
                });
            allowedDeviceParameter.SetNewValue("NORM1DEV1");
            allowedDeviceParameter.Owner = new BaseOperation("", "",
                new List<BaseParameter>(), new Dictionary<string, List<BaseStep>>());
            allowedDeviceParameter.Check();
            Assert.AreEqual(0, logMessages.Count);

            // ValueType.Device + не Signals -> ошибка типа сигнала
            var badDeviceParameter = new ActiveParameter("bp", "",
                "", new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.AI
                });
            // NORM1DEV2 — DO, при допуске только AI должна быть ошибка типа.
            badDeviceParameter.SetNewValue("NORM1DEV2");
            badDeviceParameter.Owner = new BaseOperation("", "",
                new List<BaseParameter>(), new Dictionary<string, List<BaseStep>>());
            badDeviceParameter.Check();
            Assert.That(logMessages, Is.Not.Empty,
                "ожидалась запись в лог при неверном типе устройства для ограничения только AI");
            Assert.IsTrue(logMessages[logMessages.Count - 1].Contains("могут быть установлены только сигналы"));
            Assert.AreEqual(string.Empty, badDeviceParameter.Value);

            // ManyDevices + DI: неподходящие устройства сбрасываются, подходящие остаются
            var partiallyBadSignalsParameter = new ActiveParameter("bp", "",
                "", new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.AI
                });
            partiallyBadSignalsParameter.SetNewValue("NORM1DEV1 NORM1DEV2");
            partiallyBadSignalsParameter.Owner = new BaseOperation("", "",
                new List<BaseParameter>(), new Dictionary<string, List<BaseStep>>());
            partiallyBadSignalsParameter.Check();
            Assert.IsTrue(logMessages[logMessages.Count - 1].Contains("могут быть установлены только сигналы"));
            Assert.AreEqual("NORM1DEV1", partiallyBadSignalsParameter.Value);

            // ValueType.Parameter + Parameters -> без ошибки
            var allowedParameterValue = new ActiveParameter("bp", "",
                "", new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.Parameters
                });
            SetUpParameterBaseOperationOwner(allowedParameterValue);
            allowedParameterValue.SetNewValue("parameter1");
            var logsCountBeforeAllowedParameter = logMessages.Count;
            allowedParameterValue.Check();
            Assert.AreEqual(logsCountBeforeAllowedParameter, logMessages.Count);

            // Signals + Parameters: параметр разрешен
            var allowedSignalOrParameterValue = new ActiveParameter("bp", "",
                "", new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.AI,
                    BaseParameter.DisplayObject.Parameters
                });
            SetUpParameterBaseOperationOwner(allowedSignalOrParameterValue);
            allowedSignalOrParameterValue.SetNewValue("parameter1");
            var logsCountBeforeAllowedSignalOrParameter = logMessages.Count;
            allowedSignalOrParameterValue.Check();
            Assert.AreEqual(logsCountBeforeAllowedSignalOrParameter, logMessages.Count);

            // Stub -> без ошибки и без сброса значения
            var stubValue = new ActiveParameter("bp", "",
                "", new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.Parameters
                });
            SetUpParameterBaseOperationOwner(stubValue);
            stubValue.SetNewValue("Нет");
            var logsCountBeforeStub = logMessages.Count;
            stubValue.Check();
            Assert.AreEqual(logsCountBeforeStub, logMessages.Count);
            Assert.AreEqual("Нет", stubValue.Value);

            // Signals + Parameters: сигнал неверного типа сбрасывается частично
            var badSignalOrParameterValue = new ActiveParameter("bp", "",
                "", new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.AI,
                    BaseParameter.DisplayObject.Parameters
                });
            badSignalOrParameterValue.SetNewValue("NORM1DEV1 NORM1DEV2");
            badSignalOrParameterValue.Owner = new BaseOperation("", "",
                new List<BaseParameter>(), new Dictionary<string, List<BaseStep>>());
            badSignalOrParameterValue.Check();
            Assert.IsTrue(logMessages[logMessages.Count - 1].Contains("Могут быть установлены только параметры или сигналы"));
            Assert.AreEqual("NORM1DEV1", badSignalOrParameterValue.Value);

            // ValueType.Parameter + не Parameters -> без ошибки
            var parameterValueWithoutDisplayObject = new ActiveParameter("bp", "");
            SetUpParameterBaseOperationOwner(parameterValueWithoutDisplayObject);
            parameterValueWithoutDisplayObject.SetNewValue("parameter1");
            var logsCountBeforeParameterWithoutDisplayObject = logMessages.Count;
            parameterValueWithoutDisplayObject.Check();
            Assert.AreEqual(logsCountBeforeParameterWithoutDisplayObject,
                logMessages.Count);
            Assert.AreEqual("parameter1", parameterValueWithoutDisplayObject.Value);

            // Default + None -> без ошибки
            var noneDisplayObjectParameter = new ActiveParameter("bp", "");
            noneDisplayObjectParameter.SetNewValue("qwe");
            noneDisplayObjectParameter.Owner = new BaseOperation("", "",
                new List<BaseParameter>(), new Dictionary<string, List<BaseStep>>());
            var logsCountBeforeNoneDisplayObject = logMessages.Count;
            noneDisplayObjectParameter.Check();
            Assert.AreEqual(logsCountBeforeNoneDisplayObject, logMessages.Count);

            // Default + не None -> ошибка
            var invalidValueParameter = new ActiveParameter("bp", "",
                "", new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.Parameters
                });
            invalidValueParameter.SetNewValue("qwe");
            invalidValueParameter.Owner = new BaseOperation("", "",
                new List<BaseParameter>(), new Dictionary<string, List<BaseStep>>());
            invalidValueParameter.Check();
            Assert.IsTrue(logMessages[logMessages.Count - 1].Contains("заполнено неверно: 'qwe'"));
            Assert.AreEqual(string.Empty, invalidValueParameter.Value);

            // ActiveAggregateParameter получает контекст через BaseTechObject + BaseOperation
            var aggregateParameter = new ActiveAggregateParameter("bp", "",
                "", new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.Parameters
                });
            SetUpParameterBaseTechObjectOwner(aggregateParameter);
            aggregateParameter.SetNewValue("qwe");
            aggregateParameter.Check();
            Assert.IsTrue(logMessages[logMessages.Count - 1].Contains("techObjectName"));
            Assert.IsTrue(logMessages[logMessages.Count - 1].Contains("modeName_1"));
            Assert.AreEqual(string.Empty, aggregateParameter.Value);

            // Operation: существующий номер операции разрешен
            var allowedOperationParameter = new ActiveAggregateParameter("bp", "",
                "", new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.Operation
                });
            SetUpParameterBaseTechObjectOwner(allowedOperationParameter);
            allowedOperationParameter.SetNewValue("2");
            var logsCountBeforeAllowedOperation = logMessages.Count;
            allowedOperationParameter.Check();
            Assert.AreEqual(logsCountBeforeAllowedOperation, logMessages.Count);
            Assert.AreEqual("2", allowedOperationParameter.Value);

            // Operation: несуществующий номер операции сохраняется
            var unknownOperationParameter = new ActiveAggregateParameter("bp", "",
                "", new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.Operation
                });
            SetUpParameterBaseTechObjectOwner(unknownOperationParameter);
            unknownOperationParameter.SetNewValue("3");
            unknownOperationParameter.Check();
            Assert.IsTrue(logMessages[logMessages.Count - 1].Contains(
                "может быть установлен только номер существующей операции"));
            Assert.AreEqual("3", unknownOperationParameter.Value);

            // Operation: нечисловое значение сохраняется
            var notNumberOperationParameter = new ActiveAggregateParameter("bp", "",
                "", new List<BaseParameter.DisplayObject>
                {
                    BaseParameter.DisplayObject.Operation
                });
            SetUpParameterBaseTechObjectOwner(notNumberOperationParameter);
            notNumberOperationParameter.SetNewValue("qwe");
            notNumberOperationParameter.Check();
            Assert.IsTrue(logMessages[logMessages.Count - 1].Contains(
                "может быть установлен только номер существующей операции"));
            Assert.AreEqual("qwe", notNumberOperationParameter.Value);
        }

        string stub = string.Empty;
    }
}
