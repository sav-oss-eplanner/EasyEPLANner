using EasyEPlanner.FileSavers.XML;
using EplanDevice;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechObject;

namespace TechObjectTests
{
    public class TechObjectXMLMakerTest
    {
        [Test]
        public void GetObjectForXML_Test()
        {
            var baseTank = new BaseTechObject()
            {
                MonitorName = "TankObj",
                BaseOperations = new List<BaseOperation>()
                {
                    new BaseOperation("операция 1", "mode_1", new List<BaseParameter>(), new Dictionary<string, List<BaseStep>>()
                    {
                        ["RUN"] = new List<BaseStep>()
                        {
                            new BaseStep("Шаг 1", "step_1", 1),
                        }
                    })
                    {
                        DefaultPosition = 1
                    }
                },
            };
            var techObject1 = new TechObject.TechObject("", getN => 1, 1, 2, "Tank", -1, "TankObj", "", baseTank);
            techObject1.SetUpFromBaseTechObject();
            techObject1.GetParamsManager().AddFloatParam("параметр 1", 0, "s", "par_1");
            techObject1.GetParamsManager().AddFloatParam("параметр 2", 0, "s", "par_2");

            var basePID = new BaseTechObject()
            {
                MonitorName = "PidNode",
                IsPID = true,
            };
            var PID = new TechObject.TechObject("", getN => 2, 2, 2, "pid", -1, "PidNode", "", basePID);
            PID.SetUpFromBaseTechObject();

            var manager = Mock.Of<ITechObjectManager>(m => m.TechObjects == new List<TechObject.TechObject>() { techObject1, PID});
            var xmlMaker = new TechObjectXMLMaker(manager);
            var root = new Driver(Mock.Of<IDeviceManager>(dm => dm.Devices == new List<IODevice>()));

            xmlMaker.BuildObjectsForXML(root, true, false);

            Assert.Multiple(() => {
                var system = root["SYSTEM"];
                Assert.AreEqual(14, system.Channels.Count);
                Assert.NotNull(system["SYSTEM.UP_TIME"]);
                Assert.NotNull(system["SYSTEM.WASH_VALVE_SEAT_PERIOD"]);
                Assert.NotNull(system["SYSTEM.P_V_OFF_DELAY_TIME"]);
                Assert.NotNull(system["SYSTEM.WASH_VALVE_UPPER_SEAT_TIME"]);
                Assert.NotNull(system["SYSTEM.WASH_VALVE_LOWER_SEAT_TIME"]);
                Assert.NotNull(system["SYSTEM.CMD"]);
                Assert.NotNull(system["SYSTEM.CMD_ANSWER"]);
                Assert.NotNull(system["SYSTEM.P_RESTRICTIONS_MODE"]);
                Assert.NotNull(system["SYSTEM.P_RESTRICTIONS_MANUAL_TIME"]);
                Assert.NotNull(system["SYSTEM.P_AUTO_PAUSE_OPER_ON_DEV_ERR"]);
                Assert.NotNull(system["SYSTEM.VERSION"]);
                Assert.NotNull(system["SYSTEM.NODES_COMM_ERROR"]);
                Assert.NotNull(system["SYSTEM.WATCHDOG_ERROR"]);
                Assert.NotNull(system["SYSTEM.COMMUN_ERROR"]);

                var tankObj = root["TankObj1"];
                Assert.AreEqual(11, tankObj.Channels.Count);
                Assert.NotNull(tankObj["OBJECT1.CMD"]);
                Assert.NotNull(tankObj["OBJECT1.ST[ 1 ]"]);
                Assert.NotNull(tankObj["OBJECT1.MODES[ 1 ]"]);
                Assert.NotNull(tankObj["OBJECT1.OPERATIONS[ 1 ]"]);
                Assert.NotNull(tankObj["OBJECT1.AVAILABILITY[ 1 ]"]);
                Assert.NotNull(tankObj["OBJECT1.MODES_STEPS[ 1 ]"]);
                Assert.NotNull(tankObj["OBJECT1.IDLE_STEPS1[ 1 ]"]);
                Assert.NotNull(tankObj["OBJECT1.RUN_STEPS1[ 1 ]"]);
                Assert.NotNull(tankObj["OBJECT1.RUN_STEPS1[ 2 ]"]);
                Assert.NotNull(tankObj["OBJECT1.S_PAR_F[ 1 ]"]);
                Assert.NotNull(tankObj["OBJECT1.S_PAR_F[ 2 ]"]);

                Assert.AreEqual(2, root["PidNode2"].Channels.Count);
                Assert.AreEqual(16, root["PID2"].Channels.Count);
            });
        }
    }
}
