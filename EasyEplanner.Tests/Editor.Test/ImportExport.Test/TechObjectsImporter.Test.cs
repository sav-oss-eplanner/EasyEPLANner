using Editor;
using NUnit.Framework;
using System;
using System.Reflection;
using TechObject;

namespace EditorTest.ImportExportTest
{
    public class TechObjectsImporterTest
    {
        private static Type importerType;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            importerType = typeof(TechObjectsExporter).Assembly
                .GetType("Editor.TechObjectsImporter", throwOnError: true);
            ResetSingleton(importerType, "techObjectsImporter");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ResetSingleton(importerType, "techObjectsImporter");
        }

        [Test]
        public void LoadObjects_StoresAttachedObjectsRefsOnObject()
        {
            object importer = importerType
                .GetMethod("GetInstance")
                .Invoke(null, null);

            var importedObject = (TechObject.TechObject)importerType
                .GetMethod("LoadObjects")
                .Invoke(importer, new object[]
                {
                    1, 1, "Tank", 2, "TANK", -1, "TANK1", "TANK",
                    string.Empty, "PUMP:1", -1, false
                });

            Assert.AreEqual("PUMP:1", importedObject.AttachedObjectsRefs);
        }

        private static void ResetSingleton(Type type, string fieldName)
        {
            FieldInfo field = type.GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Static);
            field.SetValue(null, null);
        }
    }
}
