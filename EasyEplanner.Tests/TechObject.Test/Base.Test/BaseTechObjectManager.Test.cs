using System.Collections.Generic;
using TechObject;
using NUnit.Framework;
using System.Linq;

namespace Tests.TechObject
{
    class BaseTechObjectManagerTest
    {
        [TestCaseSource(nameof(GetTechObjectCopyCaseSource))]
        public void GetTechObjectCopy_EmptyManager_ReturnsCopyOfObjectOrNull(
            IEnumerable<BaseTechObject> objectsToAdd,
            string searchingNameOrEplanName, bool successSearch)
        {
            IBaseTechObjectManager manager = BaseTechObjectManager
                .GetInstance();
            manager.Objects.Clear();
            manager.Objects.AddRange(objectsToAdd);

            BaseTechObject obj = manager
                .GetTechObjectCopy(searchingNameOrEplanName);

            bool isSearchedOk = obj != null &&
                (searchingNameOrEplanName == obj.Name ||
                searchingNameOrEplanName == obj.EplanName);
            Assert.AreEqual(successSearch, isSearchedOk);
        }

        private static object[] GetTechObjectCopyCaseSource()
        {
            var objectsList = new List<BaseTechObject>()
            {
                new BaseTechObject()
                    {
                        Name = "Объект 1",
                        EplanName = "Base_Object_One",
                        S88Level = 2,
                        BasicName = "base_object_one",
                        BindingName = "base_Object_one",
                        IsPID = true,
                    },
                    new BaseTechObject()
                    {
                        Name = "Объект 2",
                        EplanName = "Base_Object_Two",
                        S88Level = 1,
                        BasicName = "base_object_two",
                        BindingName = "base_Object_two",
                        IsPID = false,
                    },
                    new BaseTechObject()
                    {
                        Name = "Объект 3",
                        EplanName = "Base_Object_Three",
                        S88Level = 3,
                        BasicName = "base_object_three",
                        BindingName = "base_Object_three",
                        IsPID = false,
                    },
            };

            var findByName = new object[] { objectsList, "Объект 2", true };
            var findByEplanName =
                new object[] { objectsList, "Base_Object_Three", true };
            var nonExistedName =
                new object[] { objectsList, "Объект 100", false };
            var emptyObjName = new object[] { objectsList, "", false };
            var nullObjName = new object[] { objectsList, null, false };

            return new object[]
            {
                findByName,
                findByEplanName,
                nonExistedName,
                emptyObjName,
                nullObjName
            };
        }

        [TestCaseSource(nameof(AddBaseObjectTestCaseSource))]
        public void AddBaseObject_EmptyManager_AddObjectsExceptTheSame(
            IEnumerable<BaseTechObject> objectsToAdd, int expectedObjectsCount,
            bool addResult)
        {
            IBaseTechObjectManager manager = BaseTechObjectManager
                .GetInstance();
            manager.Objects.Clear();

            bool actualAddResult = false;
            foreach (var obj in objectsToAdd)
            {
                manager.AddBaseObject(obj.Name, obj.EplanName, obj.S88Level,
                    obj.BasicName, obj.BindingName, obj.IsPID,
                    obj.LuaModuleName, obj.MonitorName, obj.Deprecated,
                    obj.DenyBindingToUnit);

                BaseTechObject addedObj = manager.Objects
                    .Where(x => x.Name == obj.Name ||
                    x.EplanName == obj.EplanName)
                    .FirstOrDefault();
                if (addedObj != null)
                {
                    actualAddResult = addedObj.Name == obj.Name &&
                        addedObj.EplanName == obj.EplanName &&
                        addedObj.S88Level == obj.S88Level &&
                        addedObj.BasicName == obj.BasicName &&
                        addedObj.BindingName == obj.BindingName &&
                        addedObj.IsPID == obj.IsPID &&
                        addedObj.LuaModuleName == obj.LuaModuleName &&
                        addedObj.MonitorName == obj.MonitorName &&
                        addedObj.Deprecated == obj.Deprecated;
                }
            }

            Assert.Multiple(() =>
            {
                Assert.AreEqual(expectedObjectsCount, manager.Objects.Count);
                Assert.AreEqual(addResult, actualAddResult);
            });
        }

        private static object[] AddBaseObjectTestCaseSource()
        {
            var addTwoObjectsNoEquals = new object[]
            {
                new List<BaseTechObject>
                {
                    new BaseTechObject()
                    {
                        Name = "Объект 1",
                        EplanName = "Base_Object_One",
                        S88Level = 2,
                        BasicName = "base_object_one",
                        BindingName = "base_Object_one",
                        IsPID = true,
                        LuaModuleName = "LuaModuleNameOne",
                        MonitorName = "MonitorName1",
                        Deprecated = true,
                    },
                    new BaseTechObject()
                    {
                        Name = "Объект 2",
                        EplanName = "Base_Object_Two",
                        S88Level = 1,
                        BasicName = "base_object_two",
                        BindingName = "base_Object_two",
                        IsPID = false,
                        LuaModuleName = "SecondModule",
                        MonitorName = "MonitorName2",
                        Deprecated= false,
                    },
                },
                2,
                true
            };

            var addFourObjectsWithOneTheSame = new object[]
            {
                new List<BaseTechObject>
                {
                    new BaseTechObject()
                    {
                        Name = "Объект 1",
                        EplanName = "Base_Object_One",
                        S88Level = 2,
                        BasicName = "base_object_one",
                        BindingName = "base_Object_one",
                        IsPID = true,
                        LuaModuleName = "ModuleOne",
                        MonitorName = "MonitorName1",
                        Deprecated = false,
                    },
                    new BaseTechObject()
                    {
                        Name = "Ячейка процесса",
                        EplanName = "Base_Second_Obj",
                        S88Level = 0,
                        BasicName = "process_cell_obj",
                        BindingName = "Process_cell_Obj",
                        IsPID = false,
                        LuaModuleName = "ModuleSecond",
                        MonitorName = "MonitorName2",
                        Deprecated = false,
                    },
                    new BaseTechObject()
                    {
                        Name = "Объект 1",
                        EplanName = "Base_Object_One",
                        S88Level = 2,
                        BasicName = "base_object_one",
                        BindingName = "base_Object_one",
                        IsPID = true,
                        LuaModuleName = "ModuleOne",
                        MonitorName = "MonitorName1",
                        Deprecated = false,
                    },
                    new BaseTechObject()
                    {
                        Name = "Базовый объект 3",
                        EplanName = "Third_base_Object",
                        S88Level = 1,
                        BasicName = "third_BASE_obj",
                        BindingName = "The_third_base_Obj",
                        IsPID = false,
                        LuaModuleName = "ThirdModule",
                        MonitorName = "ThirdMonitorName",
                        Deprecated = false,
                    },
                },
                3,
                true
            };

            var addThreeAndTwoAreIncorrect = new object[]
            {
                new List<BaseTechObject>
                {
                    new BaseTechObject()
                    {
                        Name = "Ячейка процесса",
                        EplanName = "Base_Second_Obj",
                        S88Level = 0,
                        BasicName = "process_cell_obj",
                        BindingName = "Process_cell_Obj",
                        IsPID = false,
                        LuaModuleName = "FirstModule",
                        MonitorName = "MonitorName1",
                        Deprecated = false,
                    },
                    new BaseTechObject()
                    {
                        Name = "",
                        EplanName = "Base_Object_One",
                        S88Level = 2,
                        BasicName = "base_object_one",
                        BindingName = "base_Object_one",
                        IsPID = true,
                        LuaModuleName = "InvalidModule",
                        MonitorName = "InvalidNameMonitor",
                        Deprecated = false,
                    },
                    new BaseTechObject()
                    {
                        Name = "Базовый объект 3",
                        EplanName = null,
                        S88Level = 1,
                        BasicName = "third_BASE_obj",
                        BindingName = "The_third_base_Obj",
                        IsPID = false,
                        LuaModuleName = "ThirdModule",
                        MonitorName = "ThirdMonitorName",
                        Deprecated = false,
                    },
                },
                1,
                true
            };

            var noAddAnyObj = new object[]
            {
                new List<BaseTechObject>
                {
                    new BaseTechObject()
                    {
                        Name = "",
                        EplanName = "Base_Object_One",
                        S88Level = 2,
                        BasicName = "base_object_one",
                        BindingName = "base_Object_one",
                        IsPID = true,
                        LuaModuleName = "FirstModule",
                        MonitorName = "MonitorName1",
                        Deprecated = false,
                    },
                    new BaseTechObject()
                    {
                        Name = "Базовый объект 3",
                        EplanName = null,
                        S88Level = 1,
                        BasicName = "third_BASE_obj",
                        BindingName = "The_third_base_Obj",
                        IsPID = false,
                        LuaModuleName = "SecondModule",
                        MonitorName = "MonitorName2",
                        Deprecated = false,
                    },
                },
                0,
                false
            };

            return new object[]
            {
                addTwoObjectsNoEquals,
                addFourObjectsWithOneTheSame,
                addThreeAndTwoAreIncorrect,
                noAddAnyObj
            };
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AddBaseObject_IsSingleInstance_SetsFlagOnObject(bool isSingleInstance)
        {
            IBaseTechObjectManager manager = BaseTechObjectManager
                .GetInstance();
            manager.Objects.Clear();

            var obj = manager.AddBaseObject("Главный модуль мойки",
                "main_cip_module", 2, "cip_module", "", false, "", "",
                false, true, 2, "CIP_MODULE", isSingleInstance);

            Assert.AreEqual(isSingleInstance, obj.IsSingleInstance);
            Assert.AreEqual(isSingleInstance, manager.Objects
                .Single(x => x.EplanName == "main_cip_module").IsSingleInstance);
        }

        [Test]
        public void GetTechObjectCopy_SingleInstanceObject_CopiesFlag()
        {
            IBaseTechObjectManager manager = BaseTechObjectManager
                .GetInstance();
            manager.Objects.Clear();
            manager.AddBaseObject("Главный модуль мойки",
                "main_cip_module", 2, "cip_module", "", false, "", "",
                false, true, 2, "CIP_MODULE", true);

            var copy = manager.GetTechObjectCopy("main_cip_module");

            Assert.IsTrue(copy.IsSingleInstance);
        }

        [TestCase(0, "Ячейка процесса")]
        [TestCase(1, "Аппарат")]
        [TestCase(2, "Агрегат")]
        [TestCase(3, "Пользовательский объект")]
        [TestCase(4, null)]
        [TestCase(-1, null)]
        public void GetS88Name_EmptyManager_ReturnsStringName(
            int actualS88Level, string expectedS88Name)
        {
            IBaseTechObjectManager manager = BaseTechObjectManager
                .GetInstance();

            string actualS88Name = manager.GetS88Name(actualS88Level);

            Assert.AreEqual(expectedS88Name, actualS88Name);
        }

        [TestCase("Ячейка процесса", 0)]
        [TestCase("Аппарат", 1)]
        [TestCase("Агрегат", 2)]
        [TestCase("Пользовательский объект", 3)]
        [TestCase("", -1)]
        [TestCase(null, -1)]
        public void GetS88Level_EmptyManager_ReturnS88NumberOfObject(
            string actualS88Name, int expectedS88Level)
        {
            IBaseTechObjectManager manager = BaseTechObjectManager
                .GetInstance();

            int actualS88level = manager.GetS88Level(actualS88Name);

            Assert.AreEqual(expectedS88Level, actualS88level);
        }
    }
}
