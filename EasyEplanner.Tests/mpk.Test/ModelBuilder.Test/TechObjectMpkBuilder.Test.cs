using EasyEPlanner.mpk.ModelBuilder;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechObject;

namespace EasyEplannerTests.mpkTest.ModelBuilderTest
{
    public class TechObjectMpkBuilderTest
    {
        [Test]
        public void BuildTest()
        {
            string bcName = nameof(bcName);
            var techObject1 = new TechObject.TechObject("", getN => 1, 1, 2, "", -1, bcName, "", new BaseTechObject(null));
            var techObject2 = new TechObject.TechObject("", getN => 2, 2, 2, "", -1, bcName, "", new BaseTechObject(null));

            techObject1.ModesManager.AddMode("Операция", "");
            techObject1.GetParamsManager().Float.AddParam(new Param(getN => 1, "параметр"));

            techObject2.ModesManager.AddMode("Операция", "");
            techObject2.GetParamsManager().Float.AddParam(new Param(getN => 1, "параметр"));

            var manager = Mock.Of<ITechObjectManager>(to => 
                to.TechObjects == new List<TechObject.TechObject>() 
                {
                    techObject1, techObject2
                });

            var builder = new TechObjectMpkBuilder(manager, "container");
            var mpk = builder.Build();


            Assert.Multiple(() =>
            {
                Assert.IsNotNull(mpk);
                Assert.AreEqual(2, mpk.Components.Count);

                Assert.AreEqual("SYSTEM", mpk.Components[0].Name);
                Assert.AreEqual(10, mpk.Components[0].Properties.Count);
                CollectionAssert.AreEqual(new List<string>()
                {
                    "CMD",
                    "CMD_ANSWER",
                    "PAUSE",
                    "REST_MAN_TIME",
                    "REST_MODE",
                    "SP1",
                    "SP2",
                    "SP3",
                    "SP4",
                    "UP_TIME",
                }, mpk.Components[0].Properties.Select(p => p.Name));

                Assert.AreEqual(bcName, mpk.Components[1].Name);
                Assert.AreEqual(14, mpk.Components[1].Properties.Count);
                CollectionAssert.AreEqual(new List<string>()
                {
                    "cmd",
                    "Ladder",
                    "Nmr",
                    "ST",
                    "ST_Str",
                    "STEX_STR",
                    "Step_Str",
                    "Step_Time_Str",
                    "Time_Str",
                    "WorkCenter",
                    "mode1",
                    "OPERATIONS1",
                    "step1",
                    "S_PAR_F1",
                }, mpk.Components[1].Properties.Select(p => p.Name));
            });
        }

        [Test]
        public void BuildTest_DifferentObjectTypesWithSameNumberHaveSeparateComponents()
        {
            var tank = new TechObject.TechObject("Танк", getN => 1, 1, 2,
                "TANK", -1, "TankObj", "", new BaseTechObject());
            var line = new TechObject.TechObject("Линия", getN => 2, 1, 3,
                "LINE", -1, "LineInObjX", "", new BaseTechObject());
            var manager = Mock.Of<ITechObjectManager>(to =>
                to.TechObjects == new List<TechObject.TechObject> { tank, line });

            var mpk = new TechObjectMpkBuilder(manager, "container").Build();

            CollectionAssert.AreEqual(new[] { "SYSTEM", "TankObj", "LineInObjX" },
                mpk.Components.Select(component => component.Name));
        }
    }
}
