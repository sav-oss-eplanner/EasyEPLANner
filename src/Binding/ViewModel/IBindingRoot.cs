using IO.ViewModel;

namespace EasyEPlanner.Binding.ViewModel
{
    public interface IBindingRoot : IViewItem
    {
        IBindingViewModel Context { get; }
    }
}
