using BrightIdeasSoftware;
using EasyEPlanner.Binding.ViewModel;
using EasyEPlanner.Devices.View;
using EasyEPlanner.Devices.ViewModel;
using EasyEPlanner.Devices.ViewModel.ViewInterface;
using Editor;
using EditorControls;
using Eplan.EplApi.DataModel;
using EplanDevice;
using IO.View;
using IO.ViewModel;
using StaticHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TechObject;

namespace EasyEPlanner.Binding.View
{
    public partial class BindingViewControl : Form
    {
        private const string SearchPlaceholder = "Поиск...";
        private bool isSearchPlaceholder = true;
        private string searchText = string.Empty;
        private DeviceBinder deviceBinder;
        private System.Windows.Forms.Timer textBoxSearchTypingTimer;
        private ToolStripMenuItem goToFasMenuItem;
        private bool runtimeInitialized;
        private bool applyingCheckState;

        public static BindingViewControl Instance { get; private set; }

        public static IBindingViewModel DataContext { get; private set; }

        public static void Start()
        {
            DataContext = new BindingViewModel(DeviceManager.GetInstance());
            if (Instance is null || Instance.IsDisposed)
                Instance = new BindingViewControl();

            Instance.EnsureRuntimeInitialized();
            Instance.InitDataBindingTree();
            Instance.ShowDlg();
            Instance.SyncWithEditor();
        }

        public void Clear()
        {
            DataContext = new BindingViewModel(null);
            bindingTree.BeginUpdate();
            bindingTree.ClearObjects();
            bindingTree.EndUpdate();
        }

        public void CloseEditor()
        {
            Clear();
        }

        public void RebuildTree()
        {
            EnsureRuntimeInitialized();
            var preservedState = bindingTree.GetItemCount() > 0
                ? SaveTreeViewState()
                : null;
            DataContext.RebuildTree();
            InitDataBindingTree(preservedState);
        }

        public void RefreshTree() => RefreshTreeAfterBinding(resizeColumns: false);

        public void RefreshTreeAfterBinding(bool resizeColumns = true)
        {
            if (DataContext?.Root is null)
                return;

            // TreeListView.RefreshObject rebuilds children through the current
            // ModelFilter. After a live bind the filter cache still treats the
            // channel as unbound, so the row neither hides nor shows the clamp.
            // Drop the cache and re-bind roots so IncludeModel sees Channel.IsEmpty().
            var preservedState = bindingTree.GetItemCount() > 0
                ? SaveTreeViewState()
                : null;

            DataContext.SearchContext.FoundItems.Clear();
            ObjectListViewTreeHelper.ResetFilter(
                DataContext.Roots.Cast<IFilterableViewItem>());

            bindingTree.BeginUpdate();
            try
            {
                bindingTree.UseFiltering = false;
                bindingTree.Roots = DataContext.Roots.Cast<object>();
                if (preservedState is not null)
                    RestoreTreeViewState(preservedState);
                else
                {
                    foreach (var root in DataContext.Roots)
                        bindingTree.Expand(root);
                }
            }
            finally
            {
                bindingTree.EndUpdate();
            }

            var selected = bindingTree.SelectedObject;
            UpdateModelFilter();

            bindingTree.BeginUpdate();
            foreach (var channel in CollectChannelItems(
                DataContext.Roots.Cast<IExpandable>()))
            {
                if (channel.Filtered == false)
                    continue;

                bindingTree.RefreshObject(channel);
            }

            bindingTree.EndUpdate();
            RevealModel(selected);

            if (resizeColumns)
                AutoResizeColumns(bindingTree);
        }

        public void ShowSignalBinding()
        {
            if (Instance is null || Instance.IsDisposed)
                return;

            DataContext.ShowSignalBinding();
            InitDataBindingTree();
        }

        public void ShowEmpty()
        {
            if (Instance is null || Instance.IsDisposed)
                return;

            if (DataContext.IsShowingEmptyEditorTree)
                return;

            DataContext.ShowEmpty();
            InitDataBindingTree();
        }

        public void ShowEditorBinding(ITreeViewItem item,
            Action<string> setString,
            Action<IDictionary<int, List<int>>> setDict,
            bool rebuildTree)
        {
            if (Instance is null || Instance.IsDisposed)
                return;

            var previousRoot = DataContext.Root;
            DataContext.OnSetStringValue = setString;
            DataContext.OnSetDictValue = setDict;
            DataContext.ShowEditorBinding(item, rebuildTree);

            if (ReferenceEquals(previousRoot, DataContext.Root) &&
                DataContext.CheckBoxesEnabled)
            {
                bindingTree.Refresh();
                return;
            }

            InitDataBindingTree();
        }

        public void RefreshChecks()
        {
            if (Instance is null || Instance.IsDisposed)
                return;

            DataContext.ApplyCheckedValues();
            bindingTree.Refresh();
        }

        public BindingViewControl()
        {
            InitializeComponent();
            if (!IsInDesignEnvironment())
                EnsureRuntimeInitialized();
        }

        private static bool IsInDesignEnvironment()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return true;

            try
            {
                var processName = Process.GetCurrentProcess().ProcessName;
                if (string.Equals(processName, "devenv",
                    StringComparison.OrdinalIgnoreCase))
                    return true;

                if (processName.IndexOf("DesignToolsServer",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                var domainName = AppDomain.CurrentDomain.FriendlyName ?? string.Empty;
                return domainName.IndexOf("DesignToolsServer",
                    StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private void EnsureRuntimeInitialized()
        {
            if (runtimeInitialized)
                return;

            runtimeInitialized = true;
            InitKeyboardHook();
            DevicesIconFactory.Populate(ViewItemImageList);
            InitBindingTree();
            InitSearch();
            InitContextMenu();
            bindingTree.MouseEnter += BindingTree_MouseEnter;
            bindingTree.MouseLeave += BindingTree_MouseLeave;
            searchBoxTLP.MouseEnter += SearchInput_MouseEnter;
            searchBoxTLP.MouseLeave += SearchInput_MouseLeave;
        }

        private void InitBindingTree()
        {
            bindingTree.CanExpandGetter = obj =>
                obj is IExpandable exp && (exp.Items?.Any() ?? false);
            bindingTree.ChildrenGetter = obj => (obj as IExpandable)?.Items;

            bindingTree.CellToolTipGetter = (col, obj) => col.Index switch
            {
                0 => (obj as IO.ViewModel.IToolTip)?.Name ?? (obj as IViewItem)?.Name,
                1 => (obj as IO.ViewModel.IToolTip)?.Description ?? (obj as IViewItem)?.Description,
                _ => string.Empty,
            };

            var nameColumn = new OLVColumn("Название", nameof(IViewItem.Name))
            {
                ImageGetter = obj => (int)((obj as IHasDevicesIcon)?.Icon ?? DevicesIcon.None),
                AspectGetter = obj => (obj as IViewItem)?.Name,
                SearchValueGetter = obj => obj is BindingFilterableViewItemBase item
                    ? new[] { item.GetSearchableText() }
                    : null,
                IsEditable = false,
                Sortable = false,
            };

            var valueColumn = new OLVColumn("Значение", nameof(IViewItem.Description))
            {
                ImageGetter = obj => (int)((obj as IHasDevicesDescriptionIcon)?.DescriptionIcon
                    ?? DevicesIcon.None),
                AspectGetter = obj => (obj as IViewItem)?.Description,
                IsEditable = false,
                Sortable = false,
                MinimumWidth = 100,
            };

            bindingTree.Columns.Add(nameColumn);
            bindingTree.Columns.Add(valueColumn);

            bindingTree.UseAlternatingBackColors = true;
            bindingTree.AlternateRowBackColor = Color.FromArgb(250, 250, 250);
            bindingTree.RowHeight = 20;
            bindingTree.TriStateCheckBoxes = false;
            bindingTree.HierarchicalCheckboxes = false;
            bindingTree.CheckStateGetter = obj =>
                (obj as IBindingCheckable)?.CheckState ?? CheckState.Unchecked;
            bindingTree.CheckStatePutter = PutCheckState;

            bindingTree.ModelFilter = new ModelFilter(obj =>
                obj is not IFilterableViewItem item ||
                item.Filter(searchText, hideEmptyItems: false));
        }

        private CheckState PutCheckState(object rowObject, CheckState newValue)
        {
            if (applyingCheckState)
                return (rowObject as IBindingCheckable)?.CheckState ?? CheckState.Unchecked;

            if (rowObject is not BindingFilterableViewItemBase item ||
                !item.CanCheck)
            {
                return CheckState.Unchecked;
            }

            applyingCheckState = true;
            try
            {
                DataContext.SetItemCheckState(item, newValue);
                RefreshCheckedBranch(item);
                return item.CheckState;
            }
            finally
            {
                applyingCheckState = false;
            }
        }

        private void RefreshCheckedBranch(BindingFilterableViewItemBase item)
        {
            // TreeListView.RefreshObject rebuilds children via the filtered
            // object map. Hidden rows are missing from that map, so RebuildChildren
            // hits ArgumentOutOfRangeException. Checkbox updates only need a redraw.
            bindingTree.BeginUpdate();
            try
            {
                foreach (var node in BindingCheckHelper.Enumerate(item))
                    RefreshVisibleRow(node);

                var parent = item.ParentItem;
                while (parent is not null)
                {
                    RefreshVisibleRow(parent);
                    parent = parent.ParentItem;
                }
            }
            finally
            {
                bindingTree.EndUpdate();
            }
        }

        private void RefreshVisibleRow(object model)
        {
            var listItem = bindingTree.ModelToItem(model);
            if (listItem is null)
                return;

            bindingTree.RefreshItem(listItem);
        }

        private void InitSearch()
        {
            isSearchPlaceholder = true;
            textBox_search.Text = SearchPlaceholder;
            textBox_search.ForeColor = Color.Gray;
            searchIterator.IndexChanged += SearchIterator_IndexChanged;
            searchIterator.SearchSettingsChanged += UpdateModelFilter;
        }

        private void InitContextMenu()
        {
            var menu = new ContextMenuStrip(components);
            goToFasMenuItem = new ToolStripMenuItem(FasNavigationTexts.MenuItem)
            {
                Image = Properties.Resources.go_to_fas,
            };
            goToFasMenuItem.Click += GoToFasMenuItem_Click;
            menu.Items.Add(goToFasMenuItem);
            menu.Opening += ContextMenu_Opening;
            bindingTree.ContextMenuStrip = menu;
        }

        private void InitDataBindingTree(
            ObjectListViewTreeHelper.TreeListViewSnapshot preservedState = null)
        {
            bindingTree.BeginUpdate();
            bindingTree.CheckBoxes = DataContext.CheckBoxesEnabled;
            groupingToggleButton.Visible = DataContext.GroupingToggleVisible;
            noAssignmentBtn.Visible = DataContext.HideBoundChannelsVisible;
            noAssignmentBtn.Checked = DataContext.HideBoundChannels;
            bindingTree.Roots = DataContext.Roots.Cast<object>();
            if (bindingTree.Columns.Count > 1)
            {
                bindingTree.Columns[0].Width = 220;
                bindingTree.Columns[1].Width = 180;
            }

            if (preservedState is null)
            {
                foreach (var root in DataContext.Roots)
                    bindingTree.Expand(root);
                bindingTree.SelectObject(DataContext.Root, true);
            }
            else
            {
                RestoreTreeViewState(preservedState);
            }

            bindingTree.EndUpdate();
            AutoResizeColumns(bindingTree);
            UpdateGroupingButtonText();
            UpdateModelFilter();
        }

        private void SyncWithEditor()
        {
            var editorForm = Editor.Editor.GetInstance().EditorForm;
            if (editorForm is null ||
                !EProjectManager.GetInstance().EnabledEditMode)
            {
                ShowSignalBinding();
                return;
            }

            var item = editorForm.GetActiveItem();
            if (item is null ||
                (!item.IsUseDevList &&
                 item is not Restriction &&
                 item is not AttachedObjects))
            {
                ShowEmpty();
                return;
            }

            ShowEditorBinding(item, editorForm.SetNewVal, editorForm.SetNewVal,
                true);
        }

        private DeviceBinder GetDeviceBinder()
        {
            if (deviceBinder is null)
            {
                IApiHelper apiHelper = new ApiHelper();
                deviceBinder = new DeviceBinder(apiHelper,
                    new IOHelper(new ProjectHelper(apiHelper)));
            }

            return deviceBinder;
        }

        private void Expand_Click(object sender, EventArgs e)
        {
            int level = Convert.ToInt32((sender as ToolStripMenuItem).Tag);
            bindingTree.BeginUpdate();
            bindingTree.Expanded -= ItemExpanded;
            bindingTree.Collapsed -= ItemCollapsed;
            bindingTree.SelectedIndex = 0;
            ExpandToLevel(level, bindingTree.Objects);
            bindingTree.Expanded += ItemExpanded;
            bindingTree.Collapsed += ItemCollapsed;
            bindingTree.EnsureModelVisible(bindingTree.SelectedObject);
            bindingTree.EndUpdate();
            AutoResizeColumns(bindingTree);
        }

        private void ExpandToLevel(int level, IEnumerable items)
        {
            foreach (var item in items.OfType<IExpandable>())
            {
                if (level > 0 && !bindingTree.IsExpanded(item))
                {
                    bindingTree.Expand(item);
                    item.Expanded = true;
                }

                if (level == 0 && bindingTree.IsExpanded(item))
                {
                    bindingTree.Collapse(item);
                    item.Expanded = false;
                }

                ExpandToLevel(level > 0 ? level - 1 : 0,
                    item.Items ?? Array.Empty<IViewItem>());
            }
        }

        [ExcludeFromCodeCoverage]
        private void SyncButton_Click(object sender, EventArgs e)
        {
            EProjectManager.GetInstance().SyncAndSave(false);
            Editor.Editor.GetInstance().EditorForm.RefreshTree();
            DFrm.GetInstance().RefreshTree();
            IOViewControl.Instance?.RebuildTree();
            DevicesViewControl.Instance?.RebuildTree();
            RebuildTree();
        }

        private void GroupingToggleButton_Click(object sender, EventArgs e)
        {
            DataContext.GroupingMode = groupingToggleButton.Checked
                ? DevicesGroupingMode.ObjectThenType
                : DevicesGroupingMode.TypeThenObject;
            RebuildTree();
        }

        private void NoAssignmentBtn_Click(object sender, EventArgs e)
        {
            DataContext.HideBoundChannels = noAssignmentBtn.Checked;
            UpdateModelFilter();
        }

        private void UpdateGroupingButtonText()
        {
            groupingToggleButton.Text = string.Empty;
            groupingToggleButton.ToolTipText =
                DataContext.GroupingMode is DevicesGroupingMode.ObjectThenType
                    ? "Тип → Объект"
                    : "Объект → Тип";
            groupingToggleButton.Image =
                DataContext.GroupingMode is DevicesGroupingMode.ObjectThenType
                    ? Properties.Resources.devicesGroupingObjectType
                    : Properties.Resources.devicesGroupingTypeObject;
            groupingToggleButton.Checked =
                DataContext.GroupingMode is DevicesGroupingMode.ObjectThenType;
        }

        private void ItemExpanded(object sender, TreeBranchExpandedEventArgs e)
        {
            if (e.Model is IExpandable expandable)
                expandable.Expanded = true;
            AutoResizeColumns(bindingTree);
        }

        private void ItemCollapsed(object sender, TreeBranchCollapsedEventArgs e)
        {
            if (e.Model is IExpandable expandable)
                expandable.Expanded = false;
            AutoResizeColumns(bindingTree);
        }

        private static void AutoResizeColumns(TreeListView tree)
        {
            if (tree is null)
                return;

            foreach (ColumnHeader column in tree.Columns)
                column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        private void BindingTree_FormatCell(object sender, FormatCellEventArgs e)
        {
            if (e.Model is IBoldName)
                e.Item.Font = new Font(bindingTree.Font, FontStyle.Bold);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F))
            {
                searchTSButton.PerformClick();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void BindingTree_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle ||
                (e.Button == MouseButtons.Left && ModifierKeys.HasFlag(Keys.Control)))
            {
                GoToFasAt(e.Location);
                return;
            }

            if (e.Button != MouseButtons.Right)
                return;

            var item = bindingTree.GetItemAt(e.X, e.Y) as OLVListItem;
            if (item != null && !item.Selected)
                bindingTree.SelectedObject = item.RowObject;
        }

        private void BindingTree_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            if (DataContext.Mode is not BindingMode.SignalBinding)
                return;
            if (bindingTree.MouseMoveHitTest.Item?.RowObject
                is not BindingChannelItem channelItem)
                return;

            var device = channelItem.Device;
            if (device is null)
                return;

            GetDeviceBinder().Bind(device, channelItem.Channel);
        }

        private void ContextMenu_Opening(object sender, CancelEventArgs e)
        {
            goToFasMenuItem.Enabled = TryGetSelectedEplanFunction(out _);
        }

        private void GoToFasMenuItem_Click(object sender, EventArgs e)
        {
            if (!TryGetSelectedEplanFunction(out var function))
                return;

            EplanNavigateHelper.OpenFunctionPageWithError(function);
        }

        private void GoToFasAt(Point location)
        {
            var rowObject = (bindingTree.GetItemAt(location.X, location.Y) as OLVListItem)
                ?.RowObject;
            if (!TryGetEplanFunction(rowObject, out var function))
                return;

            EplanNavigateHelper.OpenFunctionPageWithError(function);
        }

        private bool TryGetSelectedEplanFunction(out Function function)
        {
            function = null;
            var selected = bindingTree.SelectedObjects?.Cast<object>().ToList()
                ?? [];
            return selected.Count == 1 && TryGetEplanFunction(selected[0], out function);
        }

        private static bool TryGetEplanFunction(object viewObject, out Function function)
        {
            function = null;
            if (viewObject is not IGoToFas goToFas)
                return false;

            return EplanNavigateHelper.TryGetFunction(goToFas.EplanFunction, out function);
        }

        private void SearchTSButton_Click(object sender, EventArgs e)
        {
            searchTSButton.Visible = false;
            searchBoxTLP.Visible = true;
            textBox_search.Focus();
        }

        private void SearchBoxTLP_Paint(object sender, PaintEventArgs e)
        {
            ObjectListViewTreeHelper.PaintSearchBoxBorder(e);
        }

        private void SearchBoxTLP_MouseClick(object sender, MouseEventArgs e)
        {
            textBox_search.Focus();
        }

        private void TextBox_search_TextChanged(object sender, EventArgs e)
        {
            if (isSearchPlaceholder)
                return;

            textBox_search.ForeColor = SystemColors.WindowText;

            if (textBox_search.Text == string.Empty)
            {
                searchIterator.Maximum = 0;
                searchText = string.Empty;
                UpdateModelFilter();
                return;
            }

            if (textBoxSearchTypingTimer is null)
            {
                textBoxSearchTypingTimer = new System.Windows.Forms.Timer
                {
                    Interval = 300,
                };
                textBoxSearchTypingTimer.Tick += TextBoxSearchTypingTimer_Tick;
            }

            textBoxSearchTypingTimer.Stop();
            textBoxSearchTypingTimer.Tag = textBox_search.Text;
            textBoxSearchTypingTimer.Start();
        }

        private void TextBoxSearchTypingTimer_Tick(object sender, EventArgs e)
        {
            if (textBoxSearchTypingTimer is null)
                return;

            searchText = textBoxSearchTypingTimer.Tag.ToString();
            UpdateModelFilter();
            textBoxSearchTypingTimer.Stop();
        }

        private void TextBox_search_GotFocus(object sender, EventArgs e)
        {
            if (!isSearchPlaceholder)
                return;

            isSearchPlaceholder = false;
            textBox_search.ForeColor = SystemColors.WindowText;
            textBox_search.Text = string.Empty;
        }

        private void TextBox_search_LostFocus(object sender, EventArgs e)
        {
            if (searchIterator.SettingsButtonsFocused)
            {
                textBox_search.Focus();
                return;
            }

            if (textBox_search.Text == string.Empty && !UpdatingModelFilter)
            {
                isSearchPlaceholder = true;
                textBox_search.ForeColor = Color.Gray;
                textBox_search.Text = SearchPlaceholder;
                searchBoxTLP.Visible = false;
                searchTSButton.Visible = true;
            }
        }

        private void TextBox_search_KeyUp(object sender, KeyEventArgs e)
        {
            ObjectListViewTreeHelper.HandleSearchKeyUp(sender, e, bindingTree);
        }

        private bool UpdatingModelFilter { get; set; }

        private void UpdateModelFilter()
        {
            UpdatingModelFilter = true;
            bool searchBoxWasFocused = textBox_search.Focused;

            bindingTree.UseFiltering = false;
            DataContext.SearchContext.FoundItems.Clear();
            ObjectListViewTreeHelper.ResetFilter(
                DataContext.Roots.Cast<IFilterableViewItem>());

            bool applyFilter = searchText != string.Empty ||
                (DataContext.HideBoundChannels &&
                 DataContext.Mode is BindingMode.SignalBinding);
            if (applyFilter)
            {
                foreach (var root in DataContext.Roots.OfType<IFilterableViewItem>())
                    root.Filter(searchText, hideEmptyItems: false);

                bindingTree.UseFiltering = true;
                if (searchText != string.Empty)
                    searchIterator.Maximum = DataContext.SearchContext.FoundItems.Count;
            }

            ObjectListViewTreeHelper.ApplySearchHighlight(bindingTree, searchText);

            if (searchBoxWasFocused)
                textBox_search.Focus();

            UpdatingModelFilter = false;
        }

        private void SearchIterator_IndexChanged(object sender, int index)
        {
            var item = DataContext.SearchContext.FoundItems.ElementAtOrDefault(index - 1);
            if (item is null)
                return;

            RecursiveExpandParent(item);
            if (item is IExpandable expandable && bindingTree.CanExpand(expandable))
                bindingTree.Expand(expandable);

            bindingTree.SelectObject(item, true);
            bindingTree.EnsureModelVisible(item);
        }

        private void RevealModel(object model)
        {
            if (model is not IFilterableViewItem item)
                return;

            RecursiveExpandParent(item);
            if (item is IExpandable expandable && bindingTree.CanExpand(expandable))
                bindingTree.Expand(expandable);

            bindingTree.SelectObject(item, true);
            bindingTree.EnsureModelVisible(item);
        }

        private void RecursiveExpandParent(IFilterableViewItem item)
        {
            if (item is not BindingFilterableViewItemBase node ||
                node.ParentItem is not IExpandable parent)
                return;

            if (node.ParentItem is IFilterableViewItem parentFilterable)
                RecursiveExpandParent(parentFilterable);

            if (bindingTree.CanExpand(parent))
                bindingTree.Expand(parent);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveCfg();
            base.OnFormClosing(e);
        }
    }
}
