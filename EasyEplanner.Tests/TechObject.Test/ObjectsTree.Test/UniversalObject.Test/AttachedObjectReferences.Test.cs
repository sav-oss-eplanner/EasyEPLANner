using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using TechObject;

namespace TechObjectTests
{
    public class AttachedObjectReferencesTest
    {
        [Test]
        public void FromIndices_FormatsBaseAndNumber()
        {
            var baseTechObject = new BaseTechObject { EplanName = "MIX_NODE" };
            var aggregate = new TechObject.TechObject("Agg", _ => 1, 2, 3,
                "MIX1", -1, "MIX1", "", baseTechObject);
            var manager = CreateManager(aggregate, globalIndex: 5);

            string refs = AttachedObjectReferences.FromIndices(manager, "5");

            Assert.AreEqual("MIX_NODE:2", refs);
        }

        [Test]
        public void ToIndices_ResolvesReferenceToGlobalIndex()
        {
            var baseTechObject = new BaseTechObject { EplanName = "PUMP" };
            var aggregate = new TechObject.TechObject("Pump", _ => 1, 1, 1,
                "PUMP1", -1, "PUMP1", "", baseTechObject);
            var manager = CreateManager(aggregate, globalIndex: 8);

            List<int> indices = AttachedObjectReferences.ToIndices(manager,
                "PUMP:1");

            CollectionAssert.AreEqual(new[] { 8 }, indices);
        }

        [Test]
        public void RoundTrip_PreservesBindingAcrossDifferentGlobalNumbers()
        {
            var baseTechObject = new BaseTechObject { EplanName = "VALVE" };
            var aggregate = new TechObject.TechObject("Valve", _ => 1, 4, 2,
                "V1", -1, "V1", "", baseTechObject);
            var managerA = CreateManager(aggregate, globalIndex: 3);
            var managerB = CreateManager(aggregate, globalIndex: 17);

            string refs = AttachedObjectReferences.FromIndices(managerA, "3");
            List<int> restored = AttachedObjectReferences.ToIndices(managerB,
                refs);

            CollectionAssert.AreEqual(new[] { 17 }, restored);
        }

        [Test]
        public void ToIndices_LegacyFormatIgnoresTechType()
        {
            var baseTechObject = new BaseTechObject { EplanName = "PUMP" };
            var aggregate = new TechObject.TechObject("Pump", _ => 1, 1, 1,
                "PUMP1", -1, "PUMP1", "", baseTechObject);
            var manager = CreateManager(aggregate, globalIndex: 8);

            List<int> indices = AttachedObjectReferences.ToIndices(manager,
                "PUMP:99:1");

            CollectionAssert.AreEqual(new[] { 8 }, indices);
        }

        [Test]
        public void FromIndices_SkipsInvalidAndUnresolvedIndices()
        {
            var manager = new Mock<ITechObjectManager>();
            manager.Setup(m => m.GetTObject(2)).Returns((TechObject.TechObject)null);
            manager.Setup(m => m.GetTObject(3)).Returns(
                new TechObject.TechObject("NoBase", _ => 1, 1, 1, "X", -1, "X",
                    "", null));

            string refs = AttachedObjectReferences.FromIndices(manager.Object,
                "abc 2 3");

            Assert.AreEqual(string.Empty, refs);
        }

        [Test]
        public void ToIndices_SkipsInvalidAndUnresolvedReferences()
        {
            var manager = new Mock<ITechObjectManager>();
            manager.Setup(m => m.GetTechObjectN("PUMP", 1)).Returns(0);

            List<int> indices = AttachedObjectReferences.ToIndices(manager.Object,
                "invalid :1 PUMP:1");

            Assert.IsEmpty(indices);
        }

        [Test]
        public void ToIndices_PrefersObjectsFromImportBatch()
        {
            var mixBase = new BaseTechObject { EplanName = "MIX_NODE" };
            var existingNode = new TechObject.TechObject("Old", _ => 2, 3, 1,
                "MIX1", -1, "M1", "", mixBase);
            var importedNode = new TechObject.TechObject("New", _ => 5, 3, 1,
                "MIX2", -1, "M2", "", mixBase);

            var manager = new Mock<ITechObjectManager>();
            manager.Setup(m => m.GetTechObjectN(existingNode)).Returns(2);
            manager.Setup(m => m.GetTechObjectN(importedNode)).Returns(5);
            manager.Setup(m => m.GetTechObjectN("MIX_NODE", 3)).Returns(2);

            List<int> indices = AttachedObjectReferences.ToIndices(
                manager.Object, "MIX_NODE:3", new[] { importedNode });

            CollectionAssert.AreEqual(new[] { 5 }, indices);
        }

        [Test]
        public void ToIndices_FallsBackToProjectWhenRefNotInImportBatch()
        {
            var mixBase = new BaseTechObject { EplanName = "MIX_NODE" };
            var importedTank = new TechObject.TechObject("Tank", _ => 4, 3, 2,
                "TANK", -1, "T1", "", new BaseTechObject { EplanName = "TANK" });

            var manager = new Mock<ITechObjectManager>();
            manager.Setup(m => m.GetTechObjectN("MIX_NODE", 3)).Returns(2);

            List<int> indices = AttachedObjectReferences.ToIndices(
                manager.Object, "MIX_NODE:3", new[] { importedTank });

            CollectionAssert.AreEqual(new[] { 2 }, indices);
        }

        [Test]
        public void ApplyTo_SkipsWhenRefsAreEmpty()
        {
            var manager = new Mock<ITechObjectManager>();
            var owner = new TechObject.TechObject("Tank", _ => 1, 1, 2,
                "TANK", -1, "TANK", "3", null);

            AttachedObjectReferences.ApplyTo(manager.Object, owner);

            Assert.AreEqual("3", owner.AttachedObjects.Value);
            manager.Verify(m => m.GetTechObjectN(It.IsAny<string>(), It.IsAny<int>()),
                Times.Never);
        }

        private static ITechObjectManager CreateManager(
            TechObject.TechObject techObject, int globalIndex)
        {
            var manager = new Mock<ITechObjectManager>();
            manager.Setup(m => m.GetTObject(globalIndex)).Returns(techObject);
            manager.Setup(m => m.GetTechObjectN("MIX_NODE", 2))
                .Returns(globalIndex);
            manager.Setup(m => m.GetTechObjectN("PUMP", 1))
                .Returns(globalIndex);
            manager.Setup(m => m.GetTechObjectN("VALVE", 4))
                .Returns(globalIndex);
            return manager.Object;
        }
    }
}
