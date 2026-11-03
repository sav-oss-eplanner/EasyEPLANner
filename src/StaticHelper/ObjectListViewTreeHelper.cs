using BrightIdeasSoftware;
using EasyEPlanner.Devices.ViewModel;
using IO.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace StaticHelper
{
    /// <summary>
    /// Общие операции с TreeListView: состояние дерева, фильтр и подсветка поиска.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ObjectListViewTreeHelper
    {
        public sealed class TreeListViewSnapshot
        {
            public int TopItemIndex { get; set; } = -1;

            public Point ScrollPosition { get; set; }

            public HashSet<string> ExpandedKeys { get; set; }

            public string SelectedKey { get; set; }
        }

        public static TreeListViewSnapshot Save(TreeListView tree,
            Func<object, string> getKey)
        {
            var expandedKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var expandedObject in tree.ExpandedObjects)
            {
                var key = getKey(expandedObject);
                if (key is not null)
                    expandedKeys.Add(key);
            }

            return new TreeListViewSnapshot
            {
                TopItemIndex = tree.TopItemIndex,
                ScrollPosition = tree.LowLevelScrollPosition,
                ExpandedKeys = expandedKeys,
                SelectedKey = getKey(tree.SelectedObject),
            };
        }

        public static void Restore(
            TreeListView tree,
            IEnumerable<IExpandable> roots,
            TreeListViewSnapshot state,
            Func<object, string> getKey)
        {
            if (state is null)
                return;

            tree.BeginUpdate();
            try
            {
                RestoreExpandedByKeys(tree, roots, state.ExpandedKeys, getKey);

                if (!string.IsNullOrEmpty(state.SelectedKey))
                {
                    var selected = FindViewItemByKey(roots, state.SelectedKey,
                        getKey);
                    if (selected is not null)
                        tree.SelectedObject = selected;
                }
            }
            finally
            {
                tree.EndUpdate();
            }

            if (state.TopItemIndex >= 0 &&
                state.TopItemIndex < tree.GetItemCount())
            {
                tree.TopItemIndex = state.TopItemIndex;
            }
            else
            {
                tree.LowLevelScroll(state.ScrollPosition.X,
                    state.ScrollPosition.Y);
            }
        }

        public static IEnumerable<T> CollectOfType<T>(
            IEnumerable<IExpandable> items) where T : class
        {
            foreach (var item in items)
            {
                if (item is T match)
                    yield return match;

                if (item.Items is not null)
                {
                    foreach (var child in CollectOfType<T>(
                        item.Items.OfType<IExpandable>()))
                        yield return child;
                }
            }
        }

        public static void ResetFilter(IEnumerable<IFilterableViewItem> items)
        {
            foreach (var item in items)
            {
                item.ResetFilter();
                if (item is IExpandable expandable && expandable.Items is not null)
                    ResetFilter(expandable.Items.OfType<IFilterableViewItem>());
            }
        }

        public static void ApplySearchHighlight(TreeListView tree,
            string searchText)
        {
            TextMatchFilter highlightingFilter = searchText == string.Empty
                ? null
                : TextMatchFilter.Contains(tree, searchText);

            tree.DefaultRenderer = highlightingFilter is null
                ? null
                : new HighlightTextRenderer(highlightingFilter)
                {
                    FillBrush = new SolidBrush(Color.LightGreen),
                    FramePen = new Pen(Color.DarkGreen),
                };

            tree.TreeColumnRenderer.Filter = highlightingFilter;
            tree.TreeColumnRenderer.FillBrush = new SolidBrush(Color.LightGreen);
            tree.TreeColumnRenderer.FramePen = new Pen(Color.DarkGreen);
        }

        public static void PaintSearchBoxBorder(PaintEventArgs e)
        {
            var rect = e.ClipRectangle;
            rect.Inflate(-1, -1);
            e.Graphics.Clear(Color.White);
            e.Graphics.DrawRectangle(new Pen(new SolidBrush(Color.Black)), rect);
        }

        public static void HandleSearchKeyUp(object sender, KeyEventArgs e,
            Control treeToFocus)
        {
            switch (e.KeyData)
            {
                case Keys.V | Keys.Control:
                    (sender as TextBox).Paste();
                    break;
                case Keys.C | Keys.Control:
                    (sender as TextBox).Copy();
                    break;
                case Keys.X | Keys.Control:
                    (sender as TextBox).Cut();
                    break;
                case Keys.Escape:
                    treeToFocus.Focus();
                    break;
            }
        }

        private static void RestoreExpandedByKeys(
            TreeListView tree,
            IEnumerable<IExpandable> items,
            HashSet<string> expandedKeys,
            Func<object, string> getKey)
        {
            foreach (var item in items)
            {
                if (item is object obj)
                {
                    var key = getKey(obj);
                    if (key is not null && expandedKeys.Contains(key) &&
                        tree.CanExpand(item))
                    {
                        tree.Expand(item);
                        item.Expanded = true;
                    }
                }

                if (item.Items is not null)
                {
                    RestoreExpandedByKeys(tree,
                        item.Items.OfType<IExpandable>(), expandedKeys, getKey);
                }
            }
        }

        private static object FindViewItemByKey(
            IEnumerable<IExpandable> items,
            string key,
            Func<object, string> getKey)
        {
            foreach (var item in items)
            {
                if (item is object obj &&
                    string.Equals(getKey(obj), key, StringComparison.Ordinal))
                {
                    return obj;
                }

                if (item.Items is null)
                    continue;

                var found = FindViewItemByKey(
                    item.Items.OfType<IExpandable>(), key, getKey);
                if (found is not null)
                    return found;
            }

            return null;
        }
    }
}
