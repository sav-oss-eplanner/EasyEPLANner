using TechObject;
using NUnit.Framework;
using System.Linq;
using System.Collections.Generic;

namespace Tests.TechObject
{
    class BaseTechObjectTest
    {
        [Test]
        public void Constructor_CreatingNewObject_ReturnsObjectWithDefaultData()
        {
            const int zeroValue = 0;

            var obj = new BaseTechObject();

            Assert.Multiple(() =>
            {
                Assert.IsFalse(obj.UseGroups);
                Assert.AreEqual(zeroValue, obj.SystemParams.Count);
                Assert.AreEqual(zeroValue, obj.S88Level);
                Assert.AreEqual(zeroValue, obj.ParamsManager.Float.Items.Length);
                Assert.AreEqual(1, obj.ParamsManager.Items.Length);
                Assert.IsNull(obj.Owner);
                Assert.AreEqual(zeroValue, obj.ObjectGroupsList.Count);
                Assert.IsEmpty(obj.Name);
                Assert.IsNull(obj.MainAggregateParameter);
                Assert.IsFalse(obj.IsPID);
                Assert.IsFalse(obj.IsAttachable);
                Assert.AreEqual(zeroValue, obj.Equipment.Count);
                Assert.IsEmpty(obj.EplanName);
                Assert.IsEmpty(obj.BindingName);
                Assert.IsEmpty(obj.BasicName);
                Assert.AreEqual(zeroValue, obj.BaseOperationsList.Count);
                Assert.AreEqual(zeroValue, obj.BaseOperations.Count);
                Assert.AreEqual(zeroValue, obj.AggregateParameters.Count);
                Assert.IsEmpty(obj.LuaModuleName);
                Assert.AreEqual(zeroValue, obj.BaseProperties.Count);
                Assert.IsFalse(obj.Deprecated);
                Assert.IsFalse(obj.IsSingleInstance);
            });
        }

        [TestCase(1)]
        [TestCase(100)]
        public void AddEquipment_EmptyEquipment_AddToList(int parametersCount)
        {
            var obj = GetEmpty();
            string paramValueStr = paramValue.ToString();

            for (int i = 0; i < parametersCount; i++)
            {
                obj.AddEquipment(paramLuaName, paramName,
                    paramValueStr);
            }

            var differentReferencesCount = obj.Equipment
                .Select(x => x.GetHashCode())
                .ToArray()
                .Distinct();
            Assert.Multiple(() =>
            {
                Assert.AreEqual(parametersCount, differentReferencesCount.Count());
                Assert.AreEqual(parametersCount, obj.Equipment.Count);
            });
        }

        [TestCase(1)]
        [TestCase(100)]
        public void AddActiveParameter_EmptyList_AddToList(int parametersCount)
        {
            var obj = GetEmpty();
            string paramValueStr = paramValue.ToString();

            for (int i = 0; i < parametersCount; i++)
            {
                obj.AddActiveParameter(paramLuaName, paramName,
                    paramValueStr);
            }

            var differentReferencesCount = obj.AggregateParameters
                .Select(x => x.GetHashCode())
                .ToArray()
                .Distinct();
            Assert.Multiple(() =>
            {
                Assert.AreEqual(parametersCount, differentReferencesCount.Count());
                Assert.AreEqual(parametersCount, obj.AggregateParameters.Count);
            });
        }

        [TestCase(1)]
        [TestCase(100)]
        public void AddActiveBoolParameter_EmptyList_AddToList(
            int parametersCount)
        {
            string defaultValue = "false";
            var obj = GetEmpty();

            for (int i = 0; i < parametersCount; i++)
            {
                obj.AddActiveBoolParameter(paramLuaName, paramName,
                    defaultValue);
            }

            var differentReferencesCount = obj.AggregateParameters
                .Select(x => x.GetHashCode())
                .ToArray()
                .Distinct();
            Assert.Multiple(() =>
            {
                Assert.AreEqual(parametersCount, differentReferencesCount.Count());
                Assert.AreEqual(parametersCount, obj.AggregateParameters.Count);
            });
        }

        [Test]
        public void AddMainAggregateParameter_NullParameter_AddNewParameter()
        {
            var obj = GetEmpty();
            string defaultValue = paramValue.ToString();

            obj.AddMainAggregateParameter(paramLuaName, paramName,
                defaultValue);

            var mainParam = obj.MainAggregateParameter;
            Assert.Multiple(() =>
            {
                Assert.AreEqual(paramLuaName, mainParam.LuaName);
                Assert.AreEqual(paramName, mainParam.Name);
                Assert.AreEqual(defaultValue, mainParam.Value);
                Assert.AreEqual(defaultValue, mainParam.DefaultValue);
            });
        }

        [TestCase(2, 1)]
        [TestCase(101, 100)]
        public void AddBaseOperation_EmptyList_AddBaseOperationToList(
            int expectedOperationsCount, int operationsCount)
        {
            var obj = GetEmpty();

            for (int i = 0; i < operationsCount; i++)
            {
                obj.AddBaseOperation(paramName, paramName, i + 1);
            }
            int baseOperationsCount = obj.BaseOperations.Count;

            var differentReferencesCount = obj.BaseOperations
                .Select(x => x.GetHashCode())
                .ToArray()
                .Distinct();
            Assert.Multiple(() =>
            {
                Assert.AreEqual(expectedOperationsCount, differentReferencesCount.Count());
                Assert.AreEqual(expectedOperationsCount, baseOperationsCount);
            });
        }

        [TestCaseSource(nameof(TestCaseSourceForAddObjectGroup))]
        public void AddObjectGroup_EmptyList_AddObjectGroupToList(
            int groupsCount, string allowedObjects,
            List<BaseTechObjectManager.ObjectType> expectedAllowedObjects)
        {
            var obj = GetEmpty();

            for (int i = 0; i < groupsCount; i++)
            {
                obj.AddObjectGroup(paramLuaName, paramName, allowedObjects);
            }

            var group = obj.ObjectGroupsList.First();
            var differentReferencesCount = obj.ObjectGroupsList
                .Select(x => x.GetHashCode())
                .ToArray()
                .Distinct();
            Assert.Multiple(() =>
            {
                Assert.AreEqual(groupsCount, differentReferencesCount.Count());
                Assert.AreEqual(groupsCount, obj.ObjectGroupsList.Count);
                Assert.AreEqual(expectedAllowedObjects, group.WorkStrategy.AllowedObjects);
            });
        }

        private static object[] TestCaseSourceForAddObjectGroup()
        {
            return new object[]
            {
                new object[]
                {
                    1,
                    "all",
                    new List<BaseTechObjectManager.ObjectType>
                    {
                        BaseTechObjectManager.ObjectType.Aggregate,
                        BaseTechObjectManager.ObjectType.Unit,
                        BaseTechObjectManager.ObjectType.UserObject
                    },
                },
                new object[]
                {
                    5,
                    "aggregates",
                    new List<BaseTechObjectManager.ObjectType>
                    {
                        BaseTechObjectManager.ObjectType.Aggregate,
                    },
                },
                new object[]
                {
                    3,
                    "units",
                    new List<BaseTechObjectManager.ObjectType>
                    {
                        BaseTechObjectManager.ObjectType.Unit
                    },
                },
                new object[]
                {
                    2,
                    "",
                    new List<BaseTechObjectManager.ObjectType>
                    {
                        BaseTechObjectManager.ObjectType.Aggregate,
                    },
                },
            };
        }

        [TestCase("Операция 1", false)]
        [TestCase("Операция 3", false)]
        [TestCase("Операция 5", true)]
        [TestCase("", false)]
        [TestCase(null, true)]
        public void GetBaseOperationByName_ListOperations_ReturnsBaseOperationOrNull(
            string expectedName, bool isOperationNull)
        {
            var obj = GetEmpty();
            var operations = TestCaseSourceForGetBaseOperation();
            foreach (var operation in operations)
            {
                obj.AddBaseOperation(operation.LuaName, operation.Name,
                    operation.DefaultPosition);
            }

            var baseOperation = obj.GetBaseOperationByName(expectedName);
            if (baseOperation == null)
            {
                Assert.IsTrue(isOperationNull);
            }
            else
            {
                Assert.Multiple(() =>
                {
                    Assert.IsFalse(isOperationNull);
                    Assert.AreEqual(expectedName, baseOperation.Name);
                });
            }
        }

        [TestCase("OPERATION1", false)]
        [TestCase("OPERATION3", false)]
        [TestCase("OPERATION5", true)]
        [TestCase("", false)]
        [TestCase(null, true)]
        public void GetBaseOperationByLuaName_ListOperations_ReturnsBaseOperationOrNull(
            string expectedLuaName, bool isOperationNull)
        {
            var obj = GetEmpty();
            var operations = TestCaseSourceForGetBaseOperation();
            foreach (var operation in operations)
            {
                obj.AddBaseOperation(operation.LuaName, operation.Name,
                    operation.DefaultPosition);
            }

            var baseOperation = obj.GetBaseOperationByLuaName(expectedLuaName);
            if (baseOperation == null)
            {
                Assert.Multiple(() =>
                {
                    Assert.IsTrue(isOperationNull);
                });
            }
            else
            {
                Assert.Multiple(() =>
                {
                    Assert.IsFalse(isOperationNull);
                    Assert.AreEqual(expectedLuaName, baseOperation.LuaName);
                });
            }
        }

        private List<BaseOperation> TestCaseSourceForGetBaseOperation()
        {
            return new List<BaseOperation>
            {
                new BaseOperation("Операция 1", "OPERATION1", null, null),
                new BaseOperation("Операция 2", "OPERATION2", null, null),
                new BaseOperation("Операция 3", "OPERATION3", null, null),
            };
        }

        [Test]
        public void CloneWithArgument_NormalBaseObject_ReturnsFullCopy()
        {
            // We can test only data which are existing in BaseTechObject entity.
            // Clone() always called in Clone(args), we haven't to test Clone().
            // Clone() contains a lot of dependencies which have to replace to
            // interfaces but we can't do it now. So, the unit-test will test
            // all except dependencies.

            string expectedName = "Name";
            string expectedBasicName = "BasicName";
            string expectedEplanName = "EplanName";
            int expectedS88Level = 2;
            string expectedBindingName = "BindingName";
            bool expectedIsPID = true;
            string expectedLuaModuleName = "LuaModuleName";
            bool expectedDeprecated = false;
            bool expectedIsSingleInstance = true;

            var obj = new BaseTechObject();
            obj.Name = expectedName;
            obj.BasicName = expectedBasicName;
            obj.EplanName = expectedEplanName;
            obj.S88Level = expectedS88Level;
            obj.BindingName = expectedBindingName;
            obj.IsPID = expectedIsPID;
            obj.LuaModuleName = expectedLuaModuleName;
            obj.Deprecated = expectedDeprecated;
            obj.IsSingleInstance = expectedIsSingleInstance;

            // null - is Owner, dependency
            var clonedObj = obj.Clone(null);

            Assert.Multiple(() =>
            {
                Assert.AreNotEqual(obj.GetHashCode(), clonedObj.GetHashCode());
                Assert.AreEqual(expectedName, clonedObj.Name);
                Assert.AreEqual(expectedBasicName, clonedObj.BasicName);
                Assert.AreEqual(expectedEplanName, clonedObj.EplanName);
                Assert.AreEqual(expectedS88Level, clonedObj.S88Level);
                Assert.AreEqual(expectedBindingName, clonedObj.BindingName);
                Assert.AreEqual(expectedIsPID, clonedObj.IsPID);
                Assert.AreEqual(expectedLuaModuleName, clonedObj.LuaModuleName);
                Assert.AreEqual(expectedDeprecated, clonedObj.Deprecated);
                Assert.AreEqual(expectedIsSingleInstance, clonedObj.IsSingleInstance);
            });
        }

        [Test]
        public void IsAttachable_HaveObjGorups_ReturnsTrueValue()
        {
            var obj = GetEmpty();

            obj.AddObjectGroup(paramLuaName, paramName, string.Empty);

            Assert.IsTrue(obj.IsAttachable);
        }

        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(0, false)]
        [TestCase(4, false)]
        public void IsAttachable_SetRightS88Level_ReturnsValueDependsOnS88level(
            int s88Level, bool expectedValue)
        {
            var obj = GetEmpty();

            obj.S88Level = s88Level;

            Assert.Multiple(() =>
            {
                Assert.AreEqual(expectedValue, obj.IsAttachable);
                Assert.AreEqual(s88Level, obj.S88Level);
            });
            
        }

        [TestCase(1, true)]
        [TestCase(0, false)]
        [TestCase(4, true)]
        public void UseGroups_BaseObject_ReturnsValueDependsOnObjectGroups(
            int countOfGroups, bool expectedValue)
        {
            var obj = GetEmpty();

            for(int i = 0; i < countOfGroups; i++)
            {
                obj.AddObjectGroup(paramLuaName, paramName, string.Empty);
            }

            Assert.AreEqual(expectedValue, obj.UseGroups);
        }

        [TestCase(1)]
        [TestCase(100)]
        public void AddSystemParameter_EmptyBaseTechObject_AddParametersToList(
            int paramsCount)
        {
            BaseTechObject emptyObject = GetEmpty();
            for (int i = 0; i < paramsCount; i++)
            {
                emptyObject.AddSystemParameter(paramLuaName, paramName,
                    paramValue, paramMeter);
            }

            var firstParam = emptyObject.SystemParams.GetParam(paramLuaName);
            var differentReferencesCount = emptyObject.SystemParams.Items
                .Select(x => x.GetHashCode())
                .ToArray()
                .Distinct();
            Assert.Multiple(() =>
            {
                Assert.AreEqual(paramsCount, differentReferencesCount.Count());
                Assert.AreEqual(paramsCount, emptyObject.SystemParams.Count);
                Assert.AreEqual(paramName, firstParam.Name);
                Assert.AreEqual(paramLuaName, firstParam.LuaName);
                Assert.AreEqual(paramMeter, firstParam.Meter);
                Assert.AreEqual(paramValue.ToString(), firstParam.Value.Value);
            });
        }

        [TestCase(1)]
        [TestCase(100)]
        public void AddFloatParameter_EmptyBaseTechObject_AddparametersToManager(
            int paramsCount)
        {
            BaseTechObject emptyObj = GetEmpty();
            for (int i = 0; i < paramsCount; i++)
            {
                emptyObj.AddFloatParameter(paramLuaName, paramName,
                    paramValue, paramMeter);
            }

            var firstParam = emptyObj.ParamsManager.Float.GetParam(paramLuaName);
            var differentReferencesCount = emptyObj.ParamsManager.Float.Items
                .Select(x => x.GetHashCode())
                .ToArray()
                .Distinct();
            var provider = new System.Globalization.NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";
            Assert.Multiple(() =>
            {
                Assert.AreEqual(paramsCount, differentReferencesCount.Count());
                Assert.AreEqual(paramsCount, emptyObj.ParamsManager.Float.Items.Length);
                Assert.AreEqual(paramName, firstParam.GetName());
                Assert.AreEqual(paramLuaName, firstParam.GetNameLua());
                Assert.AreEqual(paramMeter, firstParam.GetMeter());
                Assert.AreEqual(paramValue.ToString("0.##", provider),
                    firstParam.GetValue());
                Assert.IsNull(emptyObj.ParamsManager.FloatRuntime);
            });
        }

        [TestCase(1)]
        [TestCase(100)]
        public void AddFloatRuntimeParameter_EmptyBaseTechObject_AddparametersToManager(
            int paramsCount)
        {
            BaseTechObject emptyObj = GetEmpty();
            for (int i = 0; i < paramsCount; i++)
            {
                emptyObj.AddFloatRuntimeParameter(paramLuaName, paramName,
                    paramMeter);
            }

            var firstParam = emptyObj.ParamsManager.FloatRuntime.GetParam(paramLuaName);
            var differentReferencesCount = emptyObj.ParamsManager.FloatRuntime.Items
                .Select(x => x.GetHashCode())
                .ToArray()
                .Distinct();
            Assert.Multiple(() =>
            {
                Assert.AreEqual(paramsCount, differentReferencesCount.Count());
                Assert.AreEqual(paramsCount, emptyObj.ParamsManager.FloatRuntime.Items.Length);
                Assert.AreEqual(paramName, firstParam.GetName());
                Assert.AreEqual(paramLuaName, firstParam.GetNameLua());
                Assert.AreEqual(paramMeter, firstParam.GetMeter());
                Assert.AreEqual(0, emptyObj.ParamsManager.Float.Items.Length);
            });
        }

        // Methods which generate LUA code isn't testable because they have too
        // much size and there are lots of dependencies too.

        private BaseTechObject GetEmpty()
        {
            return new BaseTechObject();
        }

        string paramLuaName = "LuaName";
        string paramName = "Name";
        double paramValue = 0.5;
        string paramMeter = "шт";
    }
}
