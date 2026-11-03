namespace EasyEPlanner.Binding.View
{
    public static class BindingTreeViewKeys
    {
        public static string GetViewItemKey(object obj) => obj switch
        {
            ViewModel.BindingRoot => "root",
            ViewModel.BindingTypeGroupNode typeGroup => $"type:{typeGroup.TypeKey}",
            ViewModel.BindingObjectGroupNode objectGroup => $"object:{objectGroup.ObjectKey}",
            ViewModel.BindingDeviceNode deviceNode => $"device:{deviceNode.Device.EplanName}",
            ViewModel.BindingChannelItem channel =>
                $"channel:{channel.Device?.EplanName}:{channel.Name}",
            ViewModel.BindingParameterNode param => $"param:{param.LuaName}",
            ViewModel.BindingTechObjectNode tech => $"tech:{tech.ObjectNumber}",
            ViewModel.BindingModeNode mode =>
                $"mode:{mode.ObjectNumber}:{mode.ModeNumber}",
            ViewModel.BindingFolderNode folder => $"folder:{folder.Name}",
            _ => null,
        };
    }
}
