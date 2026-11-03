using System.Windows.Forms;

namespace EasyEPlanner.Binding.ViewModel
{
    public interface IBindingCheckable
    {
        CheckState CheckState { get; }

        bool CanCheck { get; }
    }
}
