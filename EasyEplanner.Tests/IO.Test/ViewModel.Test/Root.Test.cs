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
    public class RootTest
    {
        [Test]
        public void Getters()
        {
            var context = Mock.Of<IIOViewModel>();

            var root = new Root(context);

            Assert.Multiple(() =>
            {
                Assert.AreEqual("ПЛК", root.Name);
                Assert.AreEqual("", root.Description);
                Assert.IsEmpty(root.Items);
                Assert.AreSame(context, root.Context);
            });
        }

        [Test]
        public void Items_DeletedModulesWithoutLocation_AddsDeletedModulesGroup()
        {
            var deletedModule = Mock.Of<IIOModule>(module =>
                module.Name == "DEL338" &&
                module.PhysicalNumber == 338 &&
                module.Location == string.Empty);
            var ioManager = Mock.Of<IIOManager>(manager =>
                manager.IONodes == new List<IIONode>() &&
                manager.DeletedModules == new List<IIOModule>
                {
                    deletedModule
                });
            var context = Mock.Of<IIOViewModel>(viewModel =>
                viewModel.IOManager == ioManager);

            var root = new Root(context);

            var deletedModulesGroup = root.Items
                .OfType<DeletedModulesGroup>()
                .Single();

            Assert.Multiple(() =>
            {
                Assert.AreEqual("Исключенные модули",
                    deletedModulesGroup.Name);
                Assert.AreEqual("DEL338",
                    deletedModulesGroup.Items.Single().Name);
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
            var context = CreateContext(ioNode);

            var root = new Root(context);

            Assert.IsTrue(root.HasBindingError);
        }

        [Test]
        public void HasBindingError_WhenTreeValid_ReturnsFalse()
        {
            var ioNode = BindingErrorTestHelper.CreateIoNode(
                modules: new List<IIOModule>
                {
                    BindingErrorTestHelper.CreateIoModuleWithValidClamp(),
                });
            var context = CreateContext(ioNode);

            var root = new Root(context);

            Assert.IsFalse(root.HasBindingError);
        }

        [Test]
        public void Icon_WithInvalidClamp_ReturnsError()
        {
            var ioNode = BindingErrorTestHelper.CreateIoNode(
                modules: new List<IIOModule>
                {
                    BindingErrorTestHelper.CreateIoModuleWithInvalidClamp(),
                });
            var context = CreateContext(ioNode);

            var root = new Root(context);

            Assert.AreEqual(Icon.Error, (root as IHasDescriptionIcon).Icon);
        }

        [Test]
        public void Icon_WhenTreeValid_ReturnsNone()
        {
            var ioNode = BindingErrorTestHelper.CreateIoNode(
                modules: new List<IIOModule>
                {
                    BindingErrorTestHelper.CreateIoModuleWithValidClamp(),
                });
            var context = CreateContext(ioNode);

            var root = new Root(context);

            Assert.AreEqual(Icon.None, (root as IHasDescriptionIcon).Icon);
        }

        [Test]
        public void Items_MultipleNodesWithoutLocation_ShowsEachNode()
        {
            var nodeA1 = Mock.Of<IIONode>(n =>
                n.N == 1 &&
                n.Name == "A1" &&
                n.TypeStr == "AXC F 3152" &&
                n.Type == IONode.TYPES.T_PHOENIX_CONTACT_3152 &&
                n.Location == string.Empty &&
                n.LocationDescription == string.Empty &&
                n.IOModules == new List<IIOModule>() &&
                n.ExtensionModules == new List<IIONode>());
            var nodeA100 = Mock.Of<IIONode>(n =>
                n.N == 2 &&
                n.Name == "A100" &&
                n.TypeStr == "AXL F BK ETH" &&
                n.Type == IONode.TYPES.T_ETHERNET &&
                n.Location == string.Empty &&
                n.LocationDescription == string.Empty &&
                n.IOModules == new List<IIOModule>() &&
                n.ExtensionModules == new List<IIONode>());
            var ioManager = Mock.Of<IIOManager>(manager =>
                manager.IONodes == new List<IIONode> { nodeA1, nodeA100 } &&
                manager.DeletedModules == new List<IIOModule>());
            var context = Mock.Of<IIOViewModel>(viewModel =>
                viewModel.IOManager == ioManager);

            var root = new Root(context);

            CollectionAssert.AreEqual(
                new[] { "1. A1", "2. A100" },
                root.Items.Select(i => i.Name).ToArray());
        }

        [Test]
        public void Items_NodesWithLocation_GroupsByCabinet()
        {
            var nodeA1 = Mock.Of<IIONode>(n =>
                n.N == 1 &&
                n.Name == "A1" &&
                n.TypeStr == "AXC F 3152" &&
                n.Type == IONode.TYPES.T_PHOENIX_CONTACT_3152 &&
                n.Location == "+MCC1.1" &&
                n.LocationDescription == "Motor Control Center" &&
                n.IOModules == new List<IIOModule>() &&
                n.ExtensionModules == new List<IIONode>());
            var nodeA100 = Mock.Of<IIONode>(n =>
                n.N == 2 &&
                n.Name == "A100" &&
                n.TypeStr == "AXL F BK ETH" &&
                n.Type == IONode.TYPES.T_ETHERNET &&
                n.Location == "+CAB1" &&
                n.LocationDescription == "Алмикс №1" &&
                n.IOModules == new List<IIOModule>() &&
                n.ExtensionModules == new List<IIONode>());
            var ioManager = Mock.Of<IIOManager>(manager =>
                manager.IONodes == new List<IIONode> { nodeA1, nodeA100 } &&
                manager.DeletedModules == new List<IIOModule>());
            var context = Mock.Of<IIOViewModel>(viewModel =>
                viewModel.IOManager == ioManager);

            var root = new Root(context);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(2, root.Items.Count());
                Assert.AreEqual("+MCC1.1", root.Items.ElementAt(0).Name);
                Assert.AreEqual("Motor Control Center",
                    root.Items.ElementAt(0).Description);
                Assert.AreEqual("+CAB1", root.Items.ElementAt(1).Name);
            });
        }

        private static IIOViewModel CreateContext(IIONode ioNode)
        {
            var ioManager = Mock.Of<IIOManager>(manager =>
                manager.IONodes == new List<IIONode> { ioNode } &&
                manager.DeletedModules == new List<IIOModule>());

            return Mock.Of<IIOViewModel>(viewModel =>
                viewModel.IOManager == ioManager);
        }
    }
}
