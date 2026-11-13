using IO;
using IO.ViewModel;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace IOTests
{
    public class NodeFallbackPropertiesTest
    {
        [Test]
        public void Properties_WithoutEplanFunction_UseSafeFallbackAndIgnoreWrites()
        {
            var ioNode = Mock.Of<IIONode>(n =>
                n.N == 1 &&
                n.Name == "A100" &&
                n.TypeStr == "AXC F 3152" &&
                n.Type == IONode.TYPES.T_ETHERNET &&
                n.IP == "192.168.0.10" &&
                n.Function == null &&
                n.IOModules == new List<IIOModule>() &&
                n.ExtensionModules == new List<IIONode>());
            var node = new Node(ioNode, Mock.Of<ILocation>());
            Property[] properties = node.Items.OfType<Property>().ToArray();

            Assert.AreEqual(3, properties.Length);
            CollectionAssert.AreEqual(
                new[] { "192.168.0.10", string.Empty, string.Empty },
                properties.Select(property => property.Description));

            Assert.Multiple(() =>
            {
                Assert.IsTrue(properties[0].SetValue("10.0.0.1"));
                Assert.IsTrue(properties[1].SetValue("255.255.255.0"));
                Assert.IsTrue(properties[2].SetValue("10.0.0.254"));
                CollectionAssert.AreEqual(
                    new[] { "192.168.0.10", string.Empty, string.Empty },
                    properties.Select(property => property.Description));
            });
        }
    }
}
