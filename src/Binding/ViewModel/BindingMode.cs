namespace EasyEPlanner.Binding.ViewModel
{
    public enum BindingMode
    {
        SignalBinding,
        ObjectBinding,
    }

    public enum BindingContentKind
    {
        None,
        Devices,
        Parameters,
        DevicesAndParameters,
        Operations,
        AttachedObjects,
    }

    public enum BindingAttachedEditType
    {
        None,
        Restriction,
        AttachedAgregatesToUnit,
        AttachedUnitsToObjectGroup,
        AttachedAggregatesToAggregates,
        AttachedObjectToStep,
    }
}
