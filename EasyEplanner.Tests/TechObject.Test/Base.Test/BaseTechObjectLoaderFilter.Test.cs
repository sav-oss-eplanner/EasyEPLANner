using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TechObject;

namespace EasyEplannerTests.TechObjectTest.BaseTest
{
    public class BaseTechObjectLoaderFilterTest
    {
        [Test]
        public void GetDescriptionFilesNames_ExcludesTemplateCaseInsensitively()
        {
            string catalog = Path.Combine(Path.GetTempPath(),
                "EasyEPlannerBaseObjects_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(catalog);
            try
            {
                string template = Path.Combine(catalog,
                    "DescriptionTemplate.lua");
                string upperCaseTemplate = Path.Combine(catalog,
                    "DESCRIPTIONTEMPLATE.LUA");
                string description = Path.Combine(catalog, "Tank.lua");
                File.WriteAllText(template, "-- template");
                File.WriteAllText(upperCaseTemplate, "-- template");
                File.WriteAllText(description, "-- tank description");

                var loader = new BaseTechObjectLoader();
                MethodInfo method = typeof(BaseTechObjectLoader).GetMethod(
                    "GetDescriptionFilesNames",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var files = (string[])method.Invoke(loader,
                    new object[] { catalog });

                CollectionAssert.AreEqual(new[] { description },
                    files.OrderBy(file => file, StringComparer.OrdinalIgnoreCase));
            }
            finally
            {
                Directory.Delete(catalog, true);
            }
        }
    }
}
