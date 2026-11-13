using NUnit.Framework;
using TechObject;

namespace EasyEplannerTests.TechObjectTest.ObjectsTree.UniversalObjectTest
{
    public class TechObjectBasePropertyFallbackTest
    {
        [Test]
        public void SetBaseProperty_WhenBaseObjectWasNotLoaded_DoesNotThrow()
        {
            var techObject = new TechObject.TechObject("Tank", getN => 1,
                1, 2, "TANK", -1, "", "", null);

            Assert.DoesNotThrow(() =>
                techObject.SetBaseProperty("P_MAX_V", "10"));
        }
    }
}
