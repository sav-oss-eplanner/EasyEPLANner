using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace EasyEPlanner.Binding.ViewModel
{
    public static class BindingCheckHelper
    {
        public static void SetRecursive(BindingFilterableViewItemBase node,
            CheckState state)
        {
            node.SetCheckStateInternal(state);
            foreach (var child in node.Items.OfType<BindingFilterableViewItemBase>())
                SetRecursive(child, state);
        }

        public static void UpdateParents(BindingFilterableViewItemBase node)
        {
            var parent = node.ParentItem;
            while (parent is not null)
            {
                var children = parent.Items
                    .OfType<BindingFilterableViewItemBase>()
                    .ToList();
                if (children.Count == 0)
                    break;

                int checkedCount = children.Count(c =>
                    c.CheckState == CheckState.Checked);
                int indeterminateCount = children.Count(c =>
                    c.CheckState == CheckState.Indeterminate);

                if (checkedCount == children.Count)
                    parent.SetCheckStateInternal(CheckState.Checked);
                else if (checkedCount == 0 && indeterminateCount == 0)
                    parent.SetCheckStateInternal(CheckState.Unchecked);
                else
                    parent.SetCheckStateInternal(CheckState.Indeterminate);

                parent = parent.ParentItem;
            }
        }

        public static void UncheckAll(IEnumerable<IBindingRoot> roots)
        {
            foreach (var root in roots.OfType<BindingFilterableViewItemBase>())
                SetRecursive(root, CheckState.Unchecked);
        }

        public static void UncheckSiblings(BindingFilterableViewItemBase item)
        {
            var parent = item?.ParentItem;
            if (parent is null)
                return;

            foreach (var sibling in parent.Items.OfType<BindingFilterableViewItemBase>())
            {
                if (ReferenceEquals(sibling, item))
                    continue;

                SetRecursive(sibling, CheckState.Unchecked);
            }
        }

        public static IEnumerable<BindingFilterableViewItemBase> Enumerate(
            BindingFilterableViewItemBase node)
        {
            yield return node;
            foreach (var child in node.Items.OfType<BindingFilterableViewItemBase>())
            {
                foreach (var nested in Enumerate(child))
                    yield return nested;
            }
        }
    }
}
