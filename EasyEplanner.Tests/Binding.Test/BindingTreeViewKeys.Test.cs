using EasyEPlanner.Binding.View;
using EasyEPlanner.Binding.ViewModel;
using EplanDevice;
using NUnit.Framework;
using TechObject;

namespace EasyEPlanner.Binding.Tests
{
    public class BindingTreeViewKeysTest
    {
        [Test]
        public void GetViewItemKey_ReturnsStableKeysForTreeNodes()
        {
            var context = new BindingViewModel(null);
            var root = (BindingRoot)context.Root;
            var device = new DO("TANK2DO1", "+TANK2-DO1", "desc", 1, "TANK", 2);
            device.SetSubType("DO");

            var typeGroup = new BindingTypeGroupNode(context, root, "DO", DeviceType.DO);
            var objectGroup = new BindingObjectGroupNode(context, typeGroup,
                "TANK2", "TANK", 2, "TANK2");
            var deviceNode = new BindingDeviceNode(context, objectGroup, device,
                device.Name);
            var channel = new BindingChannelItem(context, deviceNode,
                device.Channels[0]);
            var parameters = new Params("Параметры", "par", false, "P");
            var param = parameters.AddParam(new Param(parameters.GetIdx,
                "Температура", false, 0, "шт", "T"));
            var paramNode = new BindingParameterNode(context, root, param);
            var folder = new BindingFolderNode(context, root, "Аппарат");
            var tech = new BindingTechObjectNode(context, folder, null, 4, "TANK1");
            var mode = new BindingModeNode(context, tech,
                new Mode("Операция 1", getN => 1, null), 4, 2);

            Assert.AreEqual("root", BindingTreeViewKeys.GetViewItemKey(root));
            Assert.AreEqual("type:DO", BindingTreeViewKeys.GetViewItemKey(typeGroup));
            Assert.AreEqual("object:TANK2",
                BindingTreeViewKeys.GetViewItemKey(objectGroup));
            Assert.AreEqual("device:+TANK2-DO1",
                BindingTreeViewKeys.GetViewItemKey(deviceNode));
            Assert.AreEqual("channel:+TANK2-DO1:" + channel.Name,
                BindingTreeViewKeys.GetViewItemKey(channel));
            Assert.AreEqual("param:T", BindingTreeViewKeys.GetViewItemKey(paramNode));
            Assert.AreEqual("tech:4", BindingTreeViewKeys.GetViewItemKey(tech));
            Assert.AreEqual("mode:4:2", BindingTreeViewKeys.GetViewItemKey(mode));
            Assert.AreEqual("folder:Аппарат",
                BindingTreeViewKeys.GetViewItemKey(folder));
            Assert.IsNull(BindingTreeViewKeys.GetViewItemKey("other"));
        }
    }
}
