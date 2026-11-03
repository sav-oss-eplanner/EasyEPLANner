using IO;
using IO.ViewModel;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOTests
{
    public class NodeTest
    {
        [Test]
        public void Getters_WithoutEplanFunction_UsesNodeIp()
        {
            var ioNode = Mock.Of<IIONode>(n =>
                n.N == 1 &&
                n.Name == "A100" &&
                n.TypeStr == "AXC F 3152" &&
                n.IP == "192.168.0.10" &&
                n.Function == null &&
                n.IOModules == new List<IIOModule>() &&
                n.ExtensionModules == new List<IIONode>());

            var node = new Node(ioNode, Mock.Of<ILocation>());

            CollectionAssert.AreEqual(
                new List<string>()
                {
                    "192.168.0.10",
                    string.Empty,
                    string.Empty
                },
                node.Items.Select(i => i.Description));
        }

        [Test]
        public void Getters()
        {
            var expanded = true;

            var ioNode = Mock.Of<IIONode>(n =>
                n.N == 1 &&
                n.Name == "A100" &&
                n.TypeStr == "AO" &&
                n.IOModules == new List<IIOModule>() &&
                n.ExtensionModules == new List<IIONode>() &&
                n.Function.IP == "ip" &&
                n.Function.SubnetMask == "mask" &&
                n.Function.Gateway == "gateway" && 
                n.Function.Expanded == expanded);

            var location = Mock.Of<ILocation>();

            var node = new Node(ioNode, location);

            Assert.Multiple(() =>
            {
                Assert.AreEqual("1. A100", node.Name);
                Assert.AreEqual("AO", node.Description);
                Assert.IsTrue(node.Expanded);

                node.Expanded = false;
                Assert.IsFalse(node.Expanded);

                CollectionAssert.AreEqual(
                    new List<string>()
                    {
                        "IP-адрес",
                        "Маска подсети",
                        "Сетевой шлюз"
                    },
                    node.Items.Select(i => i.Name));

                CollectionAssert.AreEqual(
                    new List<string>()
                    {
                        "ip",
                        "mask",
                        "gateway"
                    },
                    node.Items.Select(i => i.Description));
            });
        }

        [Test]
        public void Name_ExtensionNode_ReturnsFullNameWithoutPrefix()
        {
            var ioNode = Mock.Of<IIONode>(n =>
                n.N == 1 &&
                n.Name == "A100.1" &&
                n.TypeStr == "AXC F XT ETH 1TX" &&
                n.IOModules == new List<IIOModule>() &&
                n.ExtensionModules == new List<IIONode>());

            var node = new Node(ioNode, Mock.Of<ILocation>());

            Assert.AreEqual("A100.1", node.Name);
        }

        [Test]
        public void Name_NullNodeName_ReturnsNumberWithEmptyName()
        {
            var ioNode = Mock.Of<IIONode>(n =>
                n.N == 1 &&
                n.Name == null &&
                n.TypeStr == "AO" &&
                n.IOModules == new List<IIOModule>() &&
                n.ExtensionModules == new List<IIONode>());

            var node = new Node(ioNode, Mock.Of<ILocation>());

            Assert.AreEqual("1. ", node.Name);
        }

        [Test]
        public void Items_NodeWithExtension_AddsExtensionNode()
        {
            var extensionNode = Mock.Of<IIONode>(n =>
                n.N == 1 &&
                n.Name == "A100.1" &&
                n.TypeStr == "AXC F XT ETH 1TX" &&
                n.IOModules == new List<IIOModule>() &&
                n.ExtensionModules == new List<IIONode>());

            var ioNode = Mock.Of<IIONode>(n =>
                n.N == 1 &&
                n.Name == "A100" &&
                n.TypeStr == "AO" &&
                n.IOModules == new List<IIOModule>() &&
                n.ExtensionModules == new List<IIONode>() { extensionNode });

            var node = new Node(ioNode, Mock.Of<ILocation>());

            CollectionAssert.AreEqual(
                new List<string>()
                {
                    "IP-адрес",
                    "Маска подсети",
                    "Сетевой шлюз",
                    "A100.1"
                },
                node.Items.Select(i => i.Name));
        }

        [Test]
        public void Items_Node_DoesNotAddAppendModuleTargetByDefault()
        {
            var ioNode = Mock.Of<IIONode>(n =>
                n.N == 1 &&
                n.Name == "A100" &&
                n.TypeStr == "AO" &&
                n.IOModules == new List<IIOModule>() &&
                n.ExtensionModules == new List<IIONode>());

            var node = new Node(ioNode, Mock.Of<ILocation>());

            Assert.IsFalse(node.Items.OfType<AppendModuleTarget>().Any());
        }

        [Test]
        public void AppendModuleTarget_Getters_ReturnsAddModuleTargetData()
        {
            var ioNode = Mock.Of<IIONode>(n =>
                n.N == 1 &&
                n.Name == "A100" &&
                n.TypeStr == "AO" &&
                n.IOModules == new List<IIOModule>() &&
                n.ExtensionModules == new List<IIONode>());

            var appendModuleTarget = new AppendModuleTarget(ioNode);

            Assert.Multiple(() =>
            {
                Assert.AreSame(ioNode, appendModuleTarget.IONode);
                Assert.AreEqual("Добавить исключенный модуль в конец",
                    appendModuleTarget.Name);
                Assert.AreEqual(Icon.AddModule,
                    (appendModuleTarget as IHasIcon).Icon);
            });
        }

        [Test]
        public void HasBindingError_WithInvalidClamp_ReturnsTrue()
        {
            var ioNode = BindingErrorTestHelper.CreateIoNode(
                modules: new List<IIOModule>
                {
                    BindingErrorTestHelper.CreateIoModuleWithInvalidClamp(),
                });

            var node = new Node(ioNode, Mock.Of<ILocation>());

            Assert.IsTrue(node.HasBindingError);
        }

        [Test]
        public void HasBindingError_PropagatesFromExtensionNode()
        {
            var extensionNode = BindingErrorTestHelper.CreateIoNode(
                modules: new List<IIOModule>
                {
                    BindingErrorTestHelper.CreateIoModuleWithInvalidClamp(),
                },
                location: "",
                locationDescription: "");
            var parentNode = BindingErrorTestHelper.CreateIoNode(
                extensions: new List<IIONode> { extensionNode });

            var node = new Node(parentNode, Mock.Of<ILocation>());

            Assert.IsTrue(node.HasBindingError);
        }

        [Test]
        public void HasBindingError_WhenTreeValid_ReturnsFalse()
        {
            var ioNode = BindingErrorTestHelper.CreateIoNode(
                modules: new List<IIOModule>
                {
                    BindingErrorTestHelper.CreateIoModuleWithValidClamp(),
                });

            var node = new Node(ioNode, Mock.Of<ILocation>());

            Assert.IsFalse(node.HasBindingError);
        }

        [Test]
        public void Icon_WithInvalidClamp_ReturnsError()
        {
            var ioNode = BindingErrorTestHelper.CreateIoNode(
                modules: new List<IIOModule>
                {
                    BindingErrorTestHelper.CreateIoModuleWithInvalidClamp(),
                });

            var node = new Node(ioNode, Mock.Of<ILocation>());

            Assert.AreEqual(Icon.Error, (node as IHasDescriptionIcon).Icon);
        }

        [Test]
        public void Icon_WhenTreeValid_ReturnsNone()
        {
            var ioNode = BindingErrorTestHelper.CreateIoNode(
                modules: new List<IIOModule>
                {
                    BindingErrorTestHelper.CreateIoModuleWithValidClamp(),
                });

            var node = new Node(ioNode, Mock.Of<ILocation>());

            Assert.Multiple(() =>
            {
                Assert.AreEqual(Icon.Node, (node as IHasIcon).Icon);
                Assert.AreEqual(Icon.None, (node as IHasDescriptionIcon).Icon);
            });
        }

        [Test]
        public void Items_CouplerNode_AddsEnabledProperty()
        {
            var ioNode = Mock.Of<IIONode>(n =>
                n.N == 2 &&
                n.Name == "A200" &&
                n.TypeStr == "750-352" &&
                n.CanDisableNtype == true &&
                n.NtypeEnabled == true &&
                n.IOModules == new List<IIOModule>() &&
                n.ExtensionModules == new List<IIONode>() &&
                n.Function.IP == "ip" &&
                n.Function.SubnetMask == "mask" &&
                n.Function.Gateway == "gateway");

            var node = new Node(ioNode, Mock.Of<ILocation>());
            var enabledProperty = node.Items.OfType<ComboBoxProperty>().First();

            Assert.Multiple(() =>
            {
                Assert.AreEqual("Состояние", enabledProperty.Name);
                Assert.AreEqual("Включен", enabledProperty.Value);
                CollectionAssert.AreEqual(new[] { "Включен", "Выключен" },
                    enabledProperty.ComboBoxItems);
            });
        }

        [Test]
        public void Items_ControllerNode_DoesNotAddEnabledProperty()
        {
            var ioNode = Mock.Of<IIONode>(n =>
                n.N == 1 &&
                n.Name == "A100" &&
                n.TypeStr == "AXC F 2152" &&
                n.CanDisableNtype == false &&
                n.IOModules == new List<IIOModule>() &&
                n.ExtensionModules == new List<IIONode>() &&
                n.Function.IP == "ip" &&
                n.Function.SubnetMask == "mask" &&
                n.Function.Gateway == "gateway");

            var node = new Node(ioNode, Mock.Of<ILocation>());

            Assert.IsFalse(node.Items.OfType<ComboBoxProperty>().Any());
        }

        [Test]
        public void IsNtypeDisabled_WhenCouplerOff_ReturnsTrue()
        {
            var ioNode = Mock.Of<IIONode>(n =>
                n.N == 2 &&
                n.Name == "A200" &&
                n.TypeStr == "750-352" &&
                n.CanDisableNtype == true &&
                n.NtypeEnabled == false &&
                n.IOModules == new List<IIOModule>() &&
                n.ExtensionModules == new List<IIONode>() &&
                n.Function.IP == "ip" &&
                n.Function.SubnetMask == "mask" &&
                n.Function.Gateway == "gateway");

            var node = new Node(ioNode, Mock.Of<ILocation>());

            Assert.IsTrue(node.IsNtypeDisabled);
        }

        [Test]
        public void EnabledProperty_SetValue_UpdatesNtypeEnabled()
        {
            var mock = new Mock<IIONode>();
            mock.SetupGet(n => n.N).Returns(2);
            mock.SetupGet(n => n.Name).Returns("A200");
            mock.SetupGet(n => n.TypeStr).Returns("750-352");
            mock.SetupGet(n => n.CanDisableNtype).Returns(true);
            mock.SetupProperty(n => n.NtypeEnabled, true);
            mock.SetupGet(n => n.IOModules).Returns(new List<IIOModule>());
            mock.SetupGet(n => n.ExtensionModules).Returns(new List<IIONode>());
            mock.SetupGet(n => n.Function.IP).Returns("ip");
            mock.SetupGet(n => n.Function.SubnetMask).Returns("mask");
            mock.SetupGet(n => n.Function.Gateway).Returns("gateway");

            var node = new Node(mock.Object, Mock.Of<ILocation>());
            var enabledProperty = node.Items.OfType<ComboBoxProperty>().First();

            enabledProperty.SetValue("Выключен");

            Assert.Multiple(() =>
            {
                Assert.IsFalse(mock.Object.NtypeEnabled);
                Assert.IsTrue(node.IsNtypeDisabled);
                Assert.AreEqual("Выключен", enabledProperty.Value);
            });
        }
    }
}
