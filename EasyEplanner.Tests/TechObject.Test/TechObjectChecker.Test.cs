using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using TechObject;

namespace TechObjectTests
{
    public class TechObjectCheckerTest
    {
        [Test]
        public void Check_SingleInstanceBaseObjectWithTwoInstances_ReturnsError()
        {
            var baseTechObject = new BaseTechObject()
            {
                Name = "Главный модуль мойки",
                EplanName = "main_cip_module",
                S88Level = 2,
                IsSingleInstance = true,
            };

            var techObject1 = new TechObject.TechObject("Главный модуль мойки", GetN => 1,
                1, 2, "CIP_MODULE_1", -1, "CIP_BC_1", "", baseTechObject);
            var techObject2 = new TechObject.TechObject("Главный модуль мойки", GetN => 2,
                2, 2, "CIP_MODULE_2", -1, "CIP_BC_2", "", baseTechObject);

            var techObjects = new List<TechObject.TechObject>() { techObject1, techObject2 };
            var mock = GetManagerMock(techObjects,
                new List<GenericTechObject>());

            var checker = new TechObjectChecker(mock.Object);

            string errors = checker.Check();

            StringAssert.Contains("main_cip_module", errors);
            StringAssert.Contains("Главный модуль мойки", errors);
            StringAssert.Contains("№1, №2", errors);
        }

        [Test]
        public void Check_SingleInstanceBaseObjectWithTwoGenericObjects_ReturnsError()
        {
            var baseTechObject = new BaseTechObject()
            {
                Name = "Главный модуль мойки",
                EplanName = "main_cip_module",
                S88Level = 2,
                IsSingleInstance = true,
            };

            var genericObject1 = new GenericTechObject("Главный модуль мойки",
                2, "CIP_MODULE_1", -1, "CIP_BC_1", "", baseTechObject);
            var genericObject2 = new GenericTechObject("Главный модуль мойки",
                2, "CIP_MODULE_2", -1, "CIP_BC_2", "", baseTechObject);

            var genericTechObjects = new List<GenericTechObject>()
                { genericObject1, genericObject2 };
            var mock = GetManagerMock(new List<TechObject.TechObject>(),
                genericTechObjects);

            var checker = new TechObjectChecker(mock.Object);

            string errors = checker.Check();

            StringAssert.Contains("main_cip_module", errors);
            StringAssert.Contains("№1, №2", errors);
        }

        [Test]
        public void Check_SingleInstanceBaseObjectWithOneInstance_ReturnsNoError()
        {
            var baseTechObject = new BaseTechObject()
            {
                Name = "Главный модуль мойки",
                EplanName = "main_cip_module",
                S88Level = 2,
                IsSingleInstance = true,
            };

            var techObject1 = new TechObject.TechObject("Главный модуль мойки", GetN => 1,
                1, 2, "CIP_MODULE_1", -1, "CIP_BC_1", "", baseTechObject);

            var techObjects = new List<TechObject.TechObject>() { techObject1 };
            var mock = GetManagerMock(techObjects,
                new List<GenericTechObject>());

            var checker = new TechObjectChecker(mock.Object);

            string errors = checker.Check();

            StringAssert.DoesNotContain("main_cip_module", errors);
        }

        [Test]
        public void Check_NotSingleInstanceBaseObjectWithTwoInstances_ReturnsNoError()
        {
            var baseTechObject = new BaseTechObject()
            {
                Name = "Танк",
                EplanName = "tank",
                S88Level = 1,
                IsSingleInstance = false,
            };

            var techObject1 = new TechObject.TechObject("Танк", GetN => 1,
                1, 2, "TANK_1", -1, "TANK_BC_1", "", baseTechObject);
            var techObject2 = new TechObject.TechObject("Танк", GetN => 2,
                2, 2, "TANK_2", -1, "TANK_BC_2", "", baseTechObject);

            var techObjects = new List<TechObject.TechObject>() { techObject1, techObject2 };
            var mock = GetManagerMock(techObjects,
                new List<GenericTechObject>());

            var checker = new TechObjectChecker(mock.Object);

            string errors = checker.Check();

            StringAssert.DoesNotContain("единственный экземпляр", errors);
        }

        private static Mock<ITechObjectManager> GetManagerMock(
            List<TechObject.TechObject> techObjects,
            List<GenericTechObject> genericTechObjects)
        {
            var mock = new Mock<ITechObjectManager>();
            mock.Setup(m => m.TechObjects).Returns(techObjects);
            mock.Setup(m => m.GenericTechObjects).Returns(genericTechObjects);
            mock.Setup(m => m.GetTechObjectN(It.IsAny<object>()))
                .Returns<object>(o => techObjects.IndexOf(o as TechObject.TechObject) + 1);
            mock.Setup(m => m.GetGenericObjectN(It.IsAny<object>()))
                .Returns<object>(o => genericTechObjects.IndexOf(o as GenericTechObject) + 1);
            return mock;
        }
    }
}
