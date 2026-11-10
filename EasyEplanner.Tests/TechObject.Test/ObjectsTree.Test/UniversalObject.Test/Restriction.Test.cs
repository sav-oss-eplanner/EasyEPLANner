using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechObject;
using NUnit.Framework;

namespace TechObjectTests
{
    public class RestrictionTest
    {
        [Test]
        public void ChangeModeNum_Remove()
        {
            var instance = typeof(TechObjectManager).GetField("instance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            instance.SetValue(null, null);

            var techObject = TechObjectManager.GetInstance().AddObject(1, 1, "TANK", 2, "TANK", -1, "", "", "", "", -1, false);

            techObject.ModesManager.AddMode("mode 1", "");
            techObject.ModesManager.AddMode("mode 2", "");
            techObject.ModesManager.AddMode("mode 3", "");

            var restriciton = new Restriction("restricion", "{ 1, 1 } { 1, 2 } { 1, 3 }", "restriction", new SortedDictionary<int, List<int>>());

            restriciton.ChangeModeNum(techObject, 1, -1);

            Assert.AreEqual("{ 1, 1 } { 1, 2 }", restriciton.DisplayText[1]);
        }

    }
}
