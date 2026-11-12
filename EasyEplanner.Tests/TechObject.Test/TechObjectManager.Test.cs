using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechObject;
using NUnit.Framework;

namespace TechObjectTests
{
    public class TechObjectManagerTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var instance = typeof(TechObjectManager).GetField("instance",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static);
            instance.SetValue(null, null);

            techObjectManager = TechObjectManager.GetInstance();
            techObjects = techObjectManager.TechObjects;
            genericTechObjects = techObjectManager.GenericTechObjects;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            var instance = typeof(TechObjectManager).GetField("instance",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static);
            instance.SetValue(null, null);
        }

        [SetUp]
        public void SetUp()
        {
            var baseTechObject = new BaseTechObject()
            {
                EplanName = "TANK",
            };

            genericTank = new GenericTechObject("Танк", 2, "TANK", -1, "TANK", "", baseTechObject);
            genericCreamTank = new GenericTechObject("Сливки танк", 3, "CREAM_TANK", -1, "CREAM_TANK", "", baseTechObject);
            genericSkimmilkTank = new GenericTechObject("Обрат танк", 4, "SKIMMILK_TANK", -1, "SKIMMILK_TANK", "", baseTechObject);

            Tank = new TechObject.TechObject("Танк", GetN => 1, 1, 2, "TANK", -1, "TANK1", "", baseTechObject);
            CreamTank = new TechObject.TechObject("Сливки танк", GetN => 2, 1, 3, "CREAM_TANK", -1, "CREAM_TANK1", "", baseTechObject);
            SkimmilkTank = new TechObject.TechObject("Обрат танк", GetN => 3, 1, 4, "SKIMMILK_TANK", -1, "SKIMMILK_TANK1", "", baseTechObject);
            

            genericTechObjects.Add(genericTank);
            genericTechObjects.Add(genericCreamTank);
            genericTechObjects.Add(genericSkimmilkTank);

            techObjects.Add(Tank);
            techObjects.Add(CreamTank);
            techObjects.Add(SkimmilkTank);
        }

        [TearDown]
        public void TearDown()
        {
            genericTechObjects?.Clear();
            techObjects?.Clear();
        }

        [Test]
        public void GetGenericTObject()
        {
            Assert.Multiple(() =>
            {
                var genericTechObject = techObjectManager.GetGenericTObject(1);
                Assert.AreSame(genericTank, genericTechObject);

                genericTechObject = techObjectManager.GetGenericTObject(3);
                Assert.AreSame(genericSkimmilkTank, genericTechObject);

                genericTechObject = techObjectManager.GetGenericTObject(4);
                Assert.IsNull(genericTechObject);
            });
        }

        [Test]
        public void GetTechObjectN_ByDisplayText()
        {
            Assert.Multiple(() =>
            {
                var techObjectN = techObjectManager.GetTechObjectN("1. Танк №1 (#1)");
                Assert.AreEqual(1, techObjectN);

                techObjectN = techObjectManager.GetTechObjectN("2. Сливки танк №1 (#2)");
                Assert.AreEqual(2, techObjectN);

                techObjectN = techObjectManager.GetTechObjectN("qwerty");
                Assert.AreEqual(0, techObjectN);
            });
        }

        [Test]
        public void GetTechObjectN_By_BaseObjectName_EplanName_TechNumber()
        {
            Assert.Multiple(() =>
            {
                var techObjectN = techObjectManager.GetTechObjectN("TANK", "TANK", 1);
                Assert.AreEqual(1, techObjectN);

                techObjectN = techObjectManager.GetTechObjectN("TANK", "CREAM_TANK", 1);
                Assert.AreEqual(2, techObjectN);

                techObjectN = techObjectManager.GetTechObjectN("TANK", "CREAM_TANK", 0);
                Assert.AreEqual(0, techObjectN);

                techObjects.Add(new TechObject.TechObject("TANK", GetN => 4, 2, 2, "TANK", -1, "TANK2", "", null));
                techObjectN = techObjectManager.GetTechObjectN("TANK", "CREAM_TANK", 2);
                Assert.AreEqual(0, techObjectN);
            });
        }

        [Test]
        public void GetTechObjectN_By_BaseObjectName_And_TechNumber()
        {
            Assert.Multiple(() =>
            {
                var techObjectN = techObjectManager.GetTechObjectN("TANK", 1);
                Assert.AreEqual(1, techObjectN);

                techObjectN = techObjectManager.GetTechObjectN("TANK", 0);
                Assert.AreEqual(0, techObjectN);

                techObjectN = techObjectManager.GetTechObjectN("UNKNOWN", 1);
                Assert.AreEqual(0, techObjectN);
            });
        }

        [Test]
        public void ApplyAttachedObjectsReferences_RestoresBindingsByReference()
        {
            var aggregateBase = new BaseTechObject
            {
                EplanName = "PUMP",
                S88Level = (int)BaseTechObjectManager.ObjectType.Aggregate,
            };
            var pump = new TechObject.TechObject("Pump", GetN => 4, 1, 1,
                "PUMP", -1, "P1", "", aggregateBase);
            techObjects.Add(pump);

            var unitBase = new BaseTechObject
            {
                EplanName = "TANK",
                S88Level = (int)BaseTechObjectManager.ObjectType.Unit,
            };
            var unit = new TechObject.TechObject("Unit", GetN => 5, 1, 2,
                "UNIT", -1, "U1", "", unitBase);
            techObjects.Add(unit);
            unit.AttachedObjectsRefs = "PUMP:1";

            techObjectManager.ApplyAttachedObjectsReferences(new[] { unit });

            Assert.AreEqual("4", unit.AttachedObjects.Value);
            Assert.AreEqual(string.Empty, unit.AttachedObjectsRefs);
        }

        [Test]
        public void ApplyAttachedObjectsReferences_PrefersImportedObjectWithSameNumber()
        {
            var mixBase = new BaseTechObject
            {
                EplanName = "MIX_NODE",
                S88Level = (int)BaseTechObjectManager.ObjectType.Aggregate,
            };
            var existingNode = new TechObject.TechObject("Old node", GetN => 1, 3, 1,
                "MIX1", -1, "M1", "", mixBase);
            techObjects.Add(existingNode);

            var importedNode = new TechObject.TechObject("New node", GetN => 5, 3, 1,
                "MIX2", -1, "M2", "", mixBase);
            techObjects.Add(importedNode);

            var unitBase = new BaseTechObject
            {
                EplanName = "TANK",
                S88Level = (int)BaseTechObjectManager.ObjectType.Unit,
            };
            var importedUnit = new TechObject.TechObject("New tank", GetN => 6, 3, 2,
                "TANK", -1, "T3", "", unitBase);
            techObjects.Add(importedUnit);
            importedUnit.AttachedObjectsRefs = "MIX_NODE:3";

            techObjectManager.ApplyAttachedObjectsReferences(
                new[] { importedUnit, importedNode });

            Assert.AreEqual("5", importedUnit.AttachedObjects.Value);
        }

        [Test]
        public void GetTechObjectN_By_BaseObjectName_TechType_TechNumber()
        {
            Assert.Multiple(() =>
            {
                var techObjectN = techObjectManager.GetTechObjectN("TANK", 2, 1);
                Assert.AreEqual(1, techObjectN);

                techObjectN = techObjectManager.GetTechObjectN("TANK", 3, 1);
                Assert.AreEqual(2, techObjectN);

                techObjectN = techObjectManager.GetTechObjectN("TANK", 3, 0);
                Assert.AreEqual(0, techObjectN);

                techObjects.Add(new TechObject.TechObject("TANK", GetN => 4, 2, 2, "TANK", -1, "TANK2", "", null));
                techObjectN = techObjectManager.GetTechObjectN("TANK", 3, 2);
                Assert.AreEqual(0, techObjectN);
            });
        }

        [TestCase(1, 2, 1)]
        [TestCase(2, 3, 2)]
        [TestCase(3, 2, 4)]
        [TestCase(3, 3, 3)]
        public void TypeAdjacentTObjectIdByTNum(int targetObjectId, int techNumber, int expectedResult)
        {
            var baseTechObject = new BaseTechObject()
            {
                EplanName = "TANK",
            };
            SkimmilkTank2 = new TechObject.TechObject("Обрат танк", GetN => 4, 2, 4, "SKIMMILK_TANK", -1, "SKIMMILK_TANK1", "", baseTechObject);
            techObjects.Add(SkimmilkTank2);

            Assert.AreEqual(expectedResult, techObjectManager.TypeAdjacentTObjectIdByTNum(targetObjectId, techNumber));
        }

        [Test]
        public void GetNewTechNumber_UsesBaseObjectAndTechTypeForLocalNumbering()
        {
            techObjects.Clear();

            var tankBase = new BaseTechObject { Name = "Tank", EplanName = "TANK" };
            var mixerBase = new BaseTechObject { Name = "Mixer", EplanName = "MIXER" };
            var emptyBase = new BaseTechObject { Name = "Empty", EplanName = "EMPTY" };

            techObjects.Add(new TechObject.TechObject("Tank 2", _ => 1, 2, 2,
                "TANK_2", -1, "", "", tankBase));
            techObjects.Add(new TechObject.TechObject("Tank 4", _ => 2, 4, 2,
                "TANK_4", -1, "", "", tankBase.Clone()));
            techObjects.Add(new TechObject.TechObject("Tank type 3", _ => 3, 7, 3,
                "TANK_3", -1, "", "", tankBase));
            techObjects.Add(new TechObject.TechObject("Mixer 15", _ => 1, 15, 2,
                "MIXER_15", -1, "", "", mixerBase));

            Assert.Multiple(() =>
            {
                Assert.AreEqual(5, techObjectManager.GetNewTechNumber(tankBase, 2));
                Assert.AreEqual(8, techObjectManager.GetNewTechNumber(tankBase, 3));
                Assert.AreEqual(16, techObjectManager.GetNewTechNumber(mixerBase, 2));
                Assert.AreEqual(1, techObjectManager.GetNewTechNumber(emptyBase, 2));
            });
        }

        [Test]
        public void GetNewTechNumber_UsesGlobalGroupAndReservesNumberOneForSingleInstance()
        {
            var baseTechObjectManager = BaseTechObjectManager.GetInstance();
            var originalBaseTechObjects = baseTechObjectManager.Objects.ToList();
            var originalTechObjects = techObjects.ToList();

            try
            {
                baseTechObjectManager.Objects.Clear();
                techObjects.Clear();

                baseTechObjectManager.AddBaseObject("Main CIP module",
                    "main_cip_module", 1, "main_cip_module", "", false,
                    "", "", false, false, 2, "MAIN_CIP", true,
                    "CIP_MODULES");
                baseTechObjectManager.AddBaseObject("Auxiliary CIP module",
                    "aux_cip_module", 1, "aux_cip_module", "", false,
                    "", "", false, false, 2, "AUX_CIP", false,
                    "CIP_MODULES");
                baseTechObjectManager.AddBaseObject("Other module",
                    "other_module", 1, "other_module", "", false,
                    "", "", false, false, 2, "OTHER", false,
                    "OTHER_MODULES");

                var main = baseTechObjectManager.GetTechObjectCopy("main_cip_module");
                var auxiliary = baseTechObjectManager.GetTechObjectCopy("aux_cip_module");
                var other = baseTechObjectManager.GetTechObjectCopy("other_module");

                Assert.AreEqual(1, techObjectManager.GetNewTechNumber(main, 2));
                Assert.AreEqual(2, techObjectManager.GetNewTechNumber(auxiliary, 2),
                    "The singleton reserves number 1 even before it is instantiated.");
                Assert.AreEqual(1, techObjectManager.GetNewTechNumber(other, 2));
                Assert.AreEqual(1, techObjectManager.GetNewTechNumber(auxiliary, 3),
                    "Number sequences are independent by TechType.");

                techObjects.Add(new TechObject.TechObject("Main CIP module",
                    _ => 1, 1, 2, "MAIN_CIP", -1, "", "", main));
                Assert.AreEqual(2, techObjectManager.GetNewTechNumber(auxiliary, 2));

                techObjects.Add(new TechObject.TechObject("Auxiliary CIP module",
                    _ => 2, 2, 2, "AUX_CIP", -1, "", "", auxiliary));
                Assert.AreEqual(3, techObjectManager.GetNewTechNumber(auxiliary, 2));
            }
            finally
            {
                techObjects.Clear();
                techObjects.AddRange(originalTechObjects);
                baseTechObjectManager.Objects.Clear();
                baseTechObjectManager.Objects.AddRange(originalBaseTechObjects);
            }
        }

        [Test]
        public void Insert_CipModules_NumbersGloballyAndReservesOneForMain()
        {
            var baseTechObjectManager = BaseTechObjectManager.GetInstance();
            var originalBaseTechObjects = baseTechObjectManager.Objects.ToList();
            var originalTechObjects = techObjects.ToList();

            try
            {
                baseTechObjectManager.Objects.Clear();
                techObjects.Clear();

                baseTechObjectManager.AddBaseObject("Main CIP module",
                    "main_cip_module", 1, "main_cip_module", "", false,
                    "", "", false, false, 2, "MAIN_CIP", true,
                    "CIP_MODULES");
                baseTechObjectManager.AddBaseObject("Auxiliary CIP module",
                    "aux_cip_module", 1, "aux_cip_module", "", false,
                    "", "", false, false, 2, "AUX_CIP", false,
                    "CIP_MODULES");

                var mainBaseObject = new BaseObject("main_cip_module", techObjectManager);
                var auxiliaryBaseObject1 = new BaseObject("aux_cip_module", techObjectManager);
                var auxiliaryBaseObject2 = new BaseObject("aux_cip_module", techObjectManager);
                var auxiliaryBaseObject3 = new BaseObject("aux_cip_module", techObjectManager);

                var main = mainBaseObject.Insert() as TechObject.TechObject;
                var auxiliary1 = auxiliaryBaseObject1.Insert() as TechObject.TechObject;
                var auxiliary2 = auxiliaryBaseObject2.Insert() as TechObject.TechObject;
                var auxiliaryCopy = auxiliaryBaseObject3.InsertCopy(auxiliary1) as TechObject.TechObject;

                Assert.Multiple(() =>
                {
                    Assert.AreEqual(1, main.TechNumber);
                    Assert.AreEqual(2, auxiliary1.TechNumber);
                    Assert.AreEqual(3, auxiliary2.TechNumber);
                    Assert.AreEqual(4, auxiliaryCopy.TechNumber);
                });
            }
            finally
            {
                techObjects.Clear();
                techObjects.AddRange(originalTechObjects);
                baseTechObjectManager.Objects.Clear();
                baseTechObjectManager.Objects.AddRange(originalBaseTechObjects);
            }
        }

        [Test]
        public void Insert_ObjectsInDifferentBaseObjectsWithSameTechType_GetUniqueNumbers()
        {
            techObjects.Clear();

            typeof(BaseTechObjectManager).GetField("baseTechObjectManager",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static)
                .SetValue(null, null);
            BaseTechObjectManager.GetInstance().AddBaseObject("TANK", "TANK", 2,
                "TANK", "TANK", false, "TANK", "TANK", false, true);

            var baseObject1 = new BaseObject("TANK", techObjectManager);
            var baseObject2 = new BaseObject("TANK", techObjectManager);

            var obj1 = baseObject1.Insert() as TechObject.TechObject;
            var obj2 = baseObject2.Insert() as TechObject.TechObject;

            Assert.Multiple(() =>
            {
                Assert.IsNotNull(obj1);
                Assert.IsNotNull(obj2);
                Assert.AreEqual(1, obj1.TechNumber);
                Assert.AreEqual(2, obj2.TechNumber);
                Assert.AreNotEqual(obj1.TechNumber, obj2.TechNumber);
            });
        }

        [Test]
        public void SaveAsLuaTable()
        {
            techObjects.Remove(CreamTank);
            techObjects.Remove(SkimmilkTank);

            genericTechObjects.Remove(genericCreamTank);
            genericTechObjects.Remove(genericSkimmilkTank);

            Tank.GenericTechObject = genericTank;            

            var result = techObjectManager.SaveAsLuaTable("");
            

            Assert.AreEqual(string.Join("\n", expectedSaveAsLuaTableResult), result);
        }

        private readonly string[] expectedSaveAsLuaTableResult = new string[]
        {
            "init_generic_tech_objects = function()",
            "    return",
            "    {",
            "    [ 1 ] =",
            "        {",
            "        n          = 0,",
            "        tech_type  = 2,",
            "        name       = 'Танк',",
            "        name_eplan = 'TANK',",
            "        name_BC    = 'TANK',",
            "        cooper_param_number = -1,",
            "        base_tech_object = 'TANK',",
            "",
            "        modes =",
            "            {",
            "            },",
            "        },",
            "    }",
            "end",
            new string('-', 80),
            new string('-', 80),
            "init_tech_objects_modes = function()",
            "    return",
            "    {",
            "    [ 1 ] =",
            "        {",
            "        n          = 1,",
            "        tech_type  = 2,",
            "        name       = 'Танк',",
            "        name_eplan = 'TANK',",
            "        name_BC    = 'TANK1',",
            "        cooper_param_number = -1,",
            "        base_tech_object = 'TANK',",
            "        generic_tech_object = 1,",
            "",
            "        modes =",
            "            {",
            "            },",
            "        },",
            "    }",
            "end",
        };

        [Test]
        public void AddObject()
        {
            var baseObject = new BaseObject("TANK", techObjectManager);

            BaseTechObjectManager.GetInstance().AddBaseObject("TANK", "TANK", 2, "TANK", "TANK", false, "TANK", "TANK", false, true);
            BaseTechObjectManager.GetInstance().AddBaseObject("USER", "USER", 3, "USER", "USER", false, "USER", "USER", false, true);

            var genericGroup = new GenericGroup(genericTank, baseObject, techObjectManager);

            Assert.Multiple(() =>
            {
                var techObject = techObjectManager.AddObject(4, 2, "Танк", 2, "TANK", -1, "TANK2", "TANK", "", "", 1, false);
                Assert.AreSame(techObjectManager.GetTObject(4), techObject);

                var genericTechObject = techObjectManager.AddObject(4, 2, "Танк", 2, "TANK", -1, "TANK", "TANK", "", "", 1, true);
                Assert.AreSame(techObjectManager.GetGenericTObject(4), genericTechObject);

                techObject = techObjectManager.AddObject(5, 3, "Танк", 3, "tank_", -1, "tank_3", "tank_", "", "", 0, false);
                Assert.AreSame(techObjectManager.GetTObject(5), techObject);

                techObject = techObjectManager.AddObject(6, 3, "Пользовательский объект", 2, "USER", -1, "", "USER", "", "", -1, false);
                Assert.AreSame(techObjectManager.GetTObject(6), techObject);

                genericTechObject = techObjectManager.AddObject(7, 3, "Пользовательский объект", 2, "USER", -1, "", "USER", "", "", 2, true);
                Assert.AreSame(techObjectManager.GetGenericTObject(5), genericTechObject);
            });
        }

        [Test]
        public void SaveRestrictionAsLua()
        {
            var operation1 = techObjects[0].ModesManager.AddMode("operation 1", "");
            var operation2 = techObjects[0].ModesManager.AddMode("operation 2", "");

            operation1.SetRestriction("LocalRestriction", "{ 1, 2 }");
            
            var expected = new StringBuilder()
                .Append("restrictions =\n")
                .Append("    {\n")
                .Append("    [ 1 ] =        --Танк 1\n")
                .Append("        {\n")
                .Append("        [ 1 ] =        --operation 1\n")
                .Append("            {\n")
                .Append("                [ 1 ] =\n")
                .Append("                    {        --Ограничения внутри объекта\n")
                .Append("                        [ 2 ] = 1,\n")
                .Append("\n")
                .Append("                    },\n")
                .Append("            },\n")
                .Append("        [ 2 ] =        --operation 2\n")
                .Append("            {\n")
                .Append("                [ 1 ] =\n")
                .Append("                    {        --Ограничения внутри объекта\n")
                .Append("                        [ 1 ] = 1,\n")
                .Append("\n")
                .Append("                    },\n")
                .Append("            },\n")
                .Append("        },\n")
                .Append("    }");

            Assert.AreEqual(expected.ToString(), techObjectManager.SaveRestrictionAsLua(""));
        }

        private TechObjectManager techObjectManager;
        private List<TechObject.TechObject> techObjects;
        private List<GenericTechObject> genericTechObjects;

        private GenericTechObject genericTank;
        private GenericTechObject genericCreamTank;
        private GenericTechObject genericSkimmilkTank;

        private TechObject.TechObject Tank;
        private TechObject.TechObject CreamTank;
        private TechObject.TechObject SkimmilkTank;
        private TechObject.TechObject SkimmilkTank2;
    }
}
