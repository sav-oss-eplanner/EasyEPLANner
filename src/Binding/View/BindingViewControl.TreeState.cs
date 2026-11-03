using EasyEPlanner.Binding.ViewModel;
using IO.ViewModel;
using StaticHelper;
using System.Collections.Generic;
using System.Linq;

namespace EasyEPlanner.Binding.View
{
    public partial class BindingViewControl
    {
        private ObjectListViewTreeHelper.TreeListViewSnapshot SaveTreeViewState() =>
            ObjectListViewTreeHelper.Save(bindingTree,
                BindingTreeViewKeys.GetViewItemKey);

        private void RestoreTreeViewState(
            ObjectListViewTreeHelper.TreeListViewSnapshot state) =>
            ObjectListViewTreeHelper.Restore(bindingTree,
                DataContext.Roots.Cast<IExpandable>(), state,
                BindingTreeViewKeys.GetViewItemKey);

        private static IEnumerable<BindingChannelItem> CollectChannelItems(
            IEnumerable<IExpandable> items) =>
            ObjectListViewTreeHelper.CollectOfType<BindingChannelItem>(items);
    }
}
