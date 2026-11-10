using Editor;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using TechObject;

namespace EditorTest.ImportExportTest
{
    public class TechObjectsExporterTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            ResetSingleton(typeof(TechObjectManager), "instance");
            ResetSingleton(typeof(TechObjectsExporter), "techObjectsExporter");

            techObjectManager = TechObjectManager.GetInstance();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ResetSingleton(typeof(TechObjectManager), "instance");
            ResetSingleton(typeof(TechObjectsExporter), "techObjectsExporter");
        }

        [SetUp]
        public void SetUp()
        {
            techObjectManager.TechObjects.Clear();
        }

        [Test]
        public void Export_WritesAttachedObjectsRefsInsteadOfIndices()
        {
            var baseTechObject = new BaseTechObject { EplanName = "TANK" };
            var tank = new TechObject.TechObject("Tank", _ => 1, 1, 2, "TANK",
                -1, "TANK1", "", baseTechObject);
            var aggregate = new TechObject.TechObject("Agg", _ => 2, 1, 3,
                "CREAM_TANK", -1, "CREAM1", "", baseTechObject);
            techObjectManager.TechObjects.Add(tank);
            techObjectManager.TechObjects.Add(aggregate);
            tank.AttachedObjects.SetValue("2");

            string path = Path.GetTempFileName();
            try
            {
                TechObjectsExporter.GetInstance().Export(path,
                    new List<int> { 1 });

                string content = File.ReadAllText(path);
                StringAssert.Contains("attached_objects_refs = 'TANK:1'",
                    content);
                Assert.IsFalse(content.Contains("attached_objects = '2'"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static void ResetSingleton(System.Type type, string fieldName)
        {
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static);
            field.SetValue(null, null);
        }

        private TechObjectManager techObjectManager;
    }
}
