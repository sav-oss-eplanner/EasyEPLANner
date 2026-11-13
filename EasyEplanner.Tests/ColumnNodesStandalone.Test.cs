using EasyEPlanner;
using EplanDevice;
using Moq;
using NUnit.Framework;
using StaticHelper;
using System.Collections.Generic;

namespace EasyEplannerTests
{
    public class ColumnNodesStandaloneTest
    {
        [Test]
        public void DeviceSubTypeNode_UpdateWithoutFunction_AppliesSubtypeDirectly()
        {
            var device = new Mock<IIODevice>();
            device.SetupGet(d => d.Function).Returns((IEplanFunction)null);
            device.SetupGet(d => d.DeviceType).Returns(DeviceType.V);
            var node = new DeviceSubTypeNode("V_DO1");

            node.Update(device.Object, "V_DO2", "V_DO1");

            Assert.AreEqual("V_DO2", node.Value);
            device.Verify(d => d.ApplySubTypeName("V_DO2"), Times.Once);
        }

        [Test]
        public void DeviceDescriptionNode_UpdateWithoutFunction_UpdatesStandaloneDevice()
        {
            var device = new Mock<IIODevice>();
            device.SetupGet(d => d.Function).Returns((IEplanFunction)null);
            var node = new DeviceDescriptionNode("old description");

            node.Update(device.Object, "new description", "old description");

            Assert.AreEqual("new description", node.Value);
            device.Verify(d => d.SetDescription("new description"), Times.Once);
        }
    }
}
