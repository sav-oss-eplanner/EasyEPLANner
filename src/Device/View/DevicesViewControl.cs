using BrightIdeasSoftware;
using EasyEPlanner.Devices.ViewModel;
using EasyEPlanner.Devices.ViewModel.ViewInterface;
using EasyEPlanner;
using IO;
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
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EasyEPlanner.Devices.View
{
    public partial class DevicesViewControl : Form
    {
        private bool cancelChanges;
        private bool isCellEditing;
        private string searchText = string.Empty;
        private DeviceBinder deviceBinder;
        private System.Windows.Forms.Timer textBoxSearchTypingTimer;
        private TextBox textBoxCellEditor;
        private ComboBox comboBoxCellEditor;
        private IEditable cellEditItem;
        private bool cellEditUsesComboBox;
        private bool cellEditUsesMultiline;
        private readonly DevicesCellEditKeyEngine cellEditKeyEngine = new();
        private ToolStripMenuItem goToFasMenuItem;
        private bool runtimeInitialized;

        public static DevicesViewControl Instance { get; private set; }

        public static IDevicesViewModel DataContext { get; private set; }

        public static void Start()
        {
            EnsureInstanceReady();
            Instance.ShowDlg();
        }

        /// <summary>
        /// Показать дерево устройств в указанном хосте (standalone App).
        /// </summary>
        public static void StartInHost(Control host)
        {
            EnsureInstanceReady();
            Instance.ShowInHost(host);
        }

        private static void EnsureInstanceReady()
        {
            DataContext = new DevicesViewModel(DeviceManager.GetInstance());
            if (Instance is null || Instance.IsDisposed)
            {
                Instance = new DevicesViewControl();
            }

            Instance.EnsureRuntimeInitialized();
            Instance.InitDataDevicesTree();
        }

        public void Clear()
        {
            DataContext = new DevicesViewModel(null);
            devicesTree.BeginUpdate();
            devicesTree.ClearObjects();
            devicesTree.EndUpdate();
        }

        public void RebuildTree()
        {
            EnsureRuntimeInitialized();
            var preservedState = devicesTree.GetItemCount() > 0
                ? SaveTreeViewState()
                : null;
            DataContext.RebuildTree();
            InitDataDevicesTree(preservedState);
        }

        public void RefreshTree() => RefreshTreeAfterBinding(resizeColumns: false);

        public void RefreshTreeAfterBinding(bool resizeColumns = true)
        {
            if (devicesTree.GetItemCount() == 0)
            {
                devicesTree.BeginUpdate();
                devicesTree.ClearObjects();
                devicesTree.EndUpdate();
                return;
            }

            devicesTree.BeginUpdate();
            foreach (var channel in CollectChannelItems(DataContext.Roots.Cast<IExpandable>()))
                devicesTree.RefreshObject(channel);
            devicesTree.EndUpdate();

            if (resizeColumns)
                AutoResizeColumns(devicesTree);
        }

        public DevicesViewControl()
        {
            InitializeComponent();
            if (!IsInDesignEnvironment())
            {
                EnsureRuntimeInitialized();
            }
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
                {
                    return true;
                }

                // WinForms designer can run out-of-process in this host.
                if (processName.IndexOf("DesignToolsServer",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

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
            InitDevicesTree();
            InitSearch();
            InitContextMenu();
            devicesTree.MouseEnter += DevicesTree_MouseEnter;
            devicesTree.MouseLeave += DevicesTree_MouseLeave;
            searchBoxTLP.MouseEnter += SearchInput_MouseEnter;
            searchBoxTLP.MouseLeave += SearchInput_MouseLeave;
        }

        private static string GetToolTipForNameColumn(object obj)
        {
            // For device rows we always show the name tooltip,
            // even when text equals visible cell content.
            if (obj is DevicesDeviceNode deviceNode)
                return deviceNode.Name ?? string.Empty;

            if (obj is not IToolTip toolTip || string.IsNullOrWhiteSpace(toolTip.Name))
                return string.Empty;

            var displayName = (obj as IViewItem)?.Name;
            if (displayName is not null &&
                string.Equals(toolTip.Name, displayName, StringComparison.Ordinal))
            {
                return string.Empty;
            }

            return toolTip.Name;
        }

        private static string GetToolTipForValueColumn(object obj)
        {
            if (obj is IToolTip toolTip)
                return toolTip.Description ?? string.Empty;

            return (obj as IViewItem)?.Description ?? string.Empty;
        }

        private void InitDevicesTree()
        {
            devicesTree.CanExpandGetter = obj =>
                obj is IExpandable exp && (exp.Items?.Any() ?? false);
            devicesTree.ChildrenGetter = obj => (obj as IExpandable)?.Items;

            devicesTree.CellToolTipGetter = (col, obj) => col.Index switch
            {
                0 => GetToolTipForNameColumn(obj),
                1 => GetToolTipForValueColumn(obj),
                _ => string.Empty,
            };

            var nameColumn = new OLVColumn("Название", nameof(IViewItem.Name))
            {
                ImageGetter = obj => (int)((obj as IHasDevicesIcon)?.Icon ?? DevicesIcon.None),
                AspectGetter = obj => (obj as IViewItem)?.Name,
                SearchValueGetter = obj => obj is FilterableViewItemBase item
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
                IsEditable = true,
                Sortable = false,
                MinimumWidth = 100,
            };

            devicesTree.Columns.Add(nameColumn);
            devicesTree.Columns.Add(valueColumn);

            devicesTree.UseAlternatingBackColors = true;
            devicesTree.AlternateRowBackColor = Color.FromArgb(250, 250, 250);
            devicesTree.RowHeight = 20;

            devicesTree.ModelFilter = new ModelFilter(obj =>
                obj is not IFilterableViewItem item ||
                item.Filter(searchText, hideEmptyItems: false));
            devicesTree.CellEditKeyEngine = cellEditKeyEngine;
        }

        private void InitSearch()
        {
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
            devicesTree.ContextMenuStrip = menu;
        }

        private void InitDataDevicesTree(DevicesTreeViewState preservedState = null)
        {
            devicesTree.BeginUpdate();
            devicesTree.Roots = DataContext.Roots.Cast<object>();
            devicesTree.Columns[0].Width = 220;
            devicesTree.Columns[1].Width = 180;

            if (preservedState is null)
            {
                devicesTree.Expand(DataContext.Root);
                devicesTree.SelectObject(DataContext.Root, true);
            }
            else
            {
                RestoreTreeViewState(preservedState);
            }

            devicesTree.EndUpdate();
            AutoResizeColumns(devicesTree);
            UpdateGroupingButtonText();
            UpdateModelFilter();
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
            devicesTree.BeginUpdate();
            devicesTree.Expanded -= ItemExpanded;
            devicesTree.Collapsed -= ItemCollapsed;
            devicesTree.SelectedIndex = 0;
            ExpandToLevel(level, devicesTree.Objects);
            devicesTree.Expanded += ItemExpanded;
            devicesTree.Collapsed += ItemCollapsed;
            devicesTree.EnsureModelVisible(devicesTree.SelectedObject);
            devicesTree.EndUpdate();
            AutoResizeColumns(devicesTree);
        }

        private void ExpandToLevel(int level, IEnumerable items)
        {
            foreach (var item in items.OfType<IExpandable>())
            {
                if (level > 0 && !devicesTree.IsExpanded(item))
                {
                    devicesTree.Expand(item);
                    item.Expanded = true;
                }

                if (level == 0 && devicesTree.IsExpanded(item))
                {
                    devicesTree.Collapse(item);
                    item.Expanded = false;
                }

                ExpandToLevel(level > 0 ? level - 1 : 0, item.Items ?? Array.Empty<IViewItem>());
            }
        }

        private void SyncButton_Click(object sender, EventArgs e)
        {
            if (ProjectManager.GetInstance().IsStandalone)
            {
                ProjectManager.GetInstance().SaveTechObjectsFromContext(false);
            }
            else
            {
                SyncAndSaveFromEplan();
                Editor.Editor.GetInstance().EditorForm?.RefreshTree();
                DFrm.GetInstance().RefreshTree();
            }

            IOViewControl.Instance?.RebuildTree();
            RebuildTree();
        }

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void SyncAndSaveFromEplan() =>
            EProjectManager.GetInstance().SyncAndSave(false);

        private void GroupingToggleButton_Click(object sender, EventArgs e)
        {
            DataContext.GroupingMode = groupingToggleButton.Checked
                ? DevicesGroupingMode.ObjectThenType
                : DevicesGroupingMode.TypeThenObject;
            RebuildTree();
        }

        private void UpdateGroupingButtonText()
        {
            groupingToggleButton.Text = string.Empty;
            groupingToggleButton.ToolTipText = DataContext.GroupingMode is DevicesGroupingMode.ObjectThenType
                ? "Тип → Объект"
                : "Объект → Тип";
            groupingToggleButton.Image = DataContext.GroupingMode is DevicesGroupingMode.ObjectThenType
                ? Properties.Resources.devicesGroupingObjectType
                : Properties.Resources.devicesGroupingTypeObject;
            groupingToggleButton.Checked =
                DataContext.GroupingMode is DevicesGroupingMode.ObjectThenType;
        }

        private void ItemExpanded(object sender, TreeBranchExpandedEventArgs e)
        {
            if (e.Model is IExpandable expandable)
                expandable.Expanded = true;
            AutoResizeColumns(devicesTree);
        }

        private void ItemCollapsed(object sender, TreeBranchCollapsedEventArgs e)
        {
            if (e.Model is IExpandable expandable)
                expandable.Expanded = false;
            AutoResizeColumns(devicesTree);
        }

        private static void AutoResizeColumns(TreeListView tree)
        {
            if (tree is null)
                return;

            foreach (ColumnHeader column in tree.Columns)
                column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        private void CellEditStarting(object sender, CellEditEventArgs e)
        {
            if (e.Column.Index != 1)
            {
                e.Cancel = true;
                return;
            }

            if (devicesTree.SelectedObject is IComboBoxEditable comboItem)
            {
                isCellEditing = true;
                cellEditUsesComboBox = true;
                cellEditItem = comboItem;
                InitComboBoxCellEditor(comboItem.ComboBoxItems);
                string value = e.Value?.ToString() ?? comboItem.Value;
                int index = comboBoxCellEditor.FindStringExact(value);
                if (index >= 0)
                    comboBoxCellEditor.SelectedIndex = index;
                comboBoxCellEditor.Bounds = GetCellEditorBounds(devicesTree, e.CellBounds);
                e.Control = comboBoxCellEditor;
                comboBoxCellEditor.Focus();
                cellEditKeyEngine.MultilineEditActive = false;
                InstallKeyboardHook();
                devicesTree.Freeze();
                return;
            }

            if (devicesTree.SelectedObject is not IEditable editable)
            {
                e.Cancel = true;
                return;
            }

            isCellEditing = true;
            cellEditUsesComboBox = false;
            cellEditUsesMultiline = editable is DevicesDescriptionItem;
            cellEditItem = editable;
            InitTextBoxCellEditor(cellEditUsesMultiline);
            textBoxCellEditor.Text = cellEditUsesMultiline
                ? EplanMultilineText.FormatForEditor(editable.Value)
                : editable.Value;
            textBoxCellEditor.Bounds = cellEditUsesMultiline
                ? GetMultilineEditorBounds(devicesTree, e.CellBounds, textBoxCellEditor.Text)
                : GetCellEditorBounds(devicesTree, e.CellBounds);
            e.Control = textBoxCellEditor;
            textBoxCellEditor.Focus();
            cellEditKeyEngine.MultilineEditActive = cellEditUsesMultiline;
            InstallKeyboardHook();
            devicesTree.Freeze();
        }

        private void CellEditFinishing(object sender, CellEditEventArgs e)
        {
            try
            {
                isCellEditing = false;
                cellEditKeyEngine.MultilineEditActive = false;
                var editable = cellEditItem;
                cellEditItem = null;

                if (cancelChanges || editable is null)
                {
                    RemoveCellEditorControls();
                    e.Cancel = true;
                    cancelChanges = false;
                    cellEditUsesComboBox = false;
                    cellEditUsesMultiline = false;
                    devicesTree.Unfreeze();
                    return;
                }

                bool modified;
                if (cellEditUsesComboBox)
                {
                    var combo = comboBoxCellEditor ?? e.Control as ComboBox;
                    // Как в NewEditorControl: OLV для ComboBox не заполняет NewValue.
                    e.NewValue = combo?.Text ?? string.Empty;
                    modified = editable.SetValue(e.NewValue?.ToString() ?? string.Empty);
                }
                else
                {
                    string text = textBoxCellEditor?.Text
                        ?? e.NewValue?.ToString()
                        ?? string.Empty;
                    if (cellEditUsesMultiline)
                        text = EplanMultilineText.ParseFromEditor(text);
                    modified = editable.SetValue(text);
                }

                RemoveCellEditorControls();
                cellEditUsesComboBox = false;
                cellEditUsesMultiline = false;

                if (modified)
                    devicesTree.RefreshObject(editable);

                e.Cancel = true;
                devicesTree.Unfreeze();
            }
            finally
            {
                MaybeReleaseKeyboardHook();
            }
        }

        private void RemoveCellEditorControls()
        {
            if (comboBoxCellEditor is not null)
            {
                comboBoxCellEditor.LostFocus -= ComboBoxCellEditor_LostFocus;
                comboBoxCellEditor.KeyDown -= CellEditor_KeyDown;
                if (devicesTree.Controls.Contains(comboBoxCellEditor))
                    devicesTree.Controls.Remove(comboBoxCellEditor);
                comboBoxCellEditor = null;
            }

            if (textBoxCellEditor is not null)
            {
                textBoxCellEditor.LostFocus -= CellEditor_LostFocus;
                textBoxCellEditor.KeyDown -= CellEditor_KeyDown;
                if (devicesTree.Controls.Contains(textBoxCellEditor))
                    devicesTree.Controls.Remove(textBoxCellEditor);
                textBoxCellEditor = null;
            }
        }

        private static Rectangle GetCellEditorBounds(TreeListView tree, Rectangle bounds)
        {
            int height = tree.RowHeight > 0 ? tree.RowHeight : 20;
            return new Rectangle(bounds.X, bounds.Y, bounds.Width, height);
        }

        private static Rectangle GetMultilineEditorBounds(
            TreeListView tree,
            Rectangle bounds,
            string text)
        {
            int lineHeight = TextRenderer.MeasureText("Xg", tree.Font).Height;
            int lineCount = string.IsNullOrEmpty(text)
                ? 2
                : text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None).Length;
            int height = Math.Max(tree.RowHeight > 0 ? tree.RowHeight : 20,
                lineCount * lineHeight + 6);
            height = Math.Min(height, 200);
            return new Rectangle(bounds.X, bounds.Y, bounds.Width, height);
        }

        private void InitComboBoxCellEditor(IEnumerable<string> items)
        {
            comboBoxCellEditor = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = devicesTree.Font,
                IntegralHeight = false,
                FlatStyle = FlatStyle.Flat,
            };
            comboBoxCellEditor.Items.AddRange(items.Cast<object>().ToArray());
            comboBoxCellEditor.LostFocus += ComboBoxCellEditor_LostFocus;
            comboBoxCellEditor.KeyDown += CellEditor_KeyDown;
        }

        private void ComboBoxCellEditor_LostFocus(object sender, EventArgs e)
        {
            if (!isCellEditing || sender is not ComboBox combo || combo.DroppedDown)
                return;

            devicesTree.FinishCellEdit();
        }

        private void InitTextBoxCellEditor(bool multiline = false)
        {
            textBoxCellEditor = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                Font = devicesTree.Font,
                Multiline = multiline,
                AcceptsReturn = multiline,
                ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
                WordWrap = multiline,
            };
            textBoxCellEditor.LostFocus += CellEditor_LostFocus;
            textBoxCellEditor.KeyDown += CellEditor_KeyDown;
        }

        private void CellEditor_LostFocus(object sender, EventArgs e)
        {
            if (isCellEditing)
                devicesTree.FinishCellEdit();
        }

        private void CellEditor_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (cellEditUsesMultiline && !e.Control)
                    {
                        InsertNewLineInCellEditor();
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        return;
                    }
                    devicesTree.FinishCellEdit();
                    e.Handled = true;
                    break;
                case Keys.Escape:
                    cancelChanges = true;
                    devicesTree.FinishCellEdit();
                    e.Handled = true;
                    break;
            }
        }

        private void InsertNewLineInCellEditor()
        {
            if (textBoxCellEditor is null)
                return;

            int selectionStart = textBoxCellEditor.SelectionStart;
            int selectionLength = textBoxCellEditor.SelectionLength;
            string text = textBoxCellEditor.Text ?? string.Empty;
            string newLine = Environment.NewLine;

            textBoxCellEditor.Text = text
                .Remove(selectionStart, selectionLength)
                .Insert(selectionStart, newLine);
            textBoxCellEditor.SelectionStart = selectionStart + newLine.Length;
        }

        private void DevicesTree_FormatCell(object sender, FormatCellEventArgs e)
        {
            if (e.Model is IBoldName)
                e.Item.Font = new Font(devicesTree.Font, FontStyle.Bold);
        }

        private void DevicesTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && isCellEditing)
            {
                cancelChanges = true;
                devicesTree.FinishCellEdit();
            }
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

        private void DevicesTree_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle ||
                (e.Button == MouseButtons.Left && ModifierKeys.HasFlag(Keys.Control)))
            {
                if (!ProjectManager.GetInstance().IsStandalone)
                    GoToFasAt(e.Location);
                return;
            }

            if (e.Button != MouseButtons.Right)
                return;

            var item = devicesTree.GetItemAt(e.X, e.Y) as OLVListItem;
            if (item != null && !item.Selected)
                devicesTree.SelectedObject = item.RowObject;
        }

        private void DevicesTree_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            if (ProjectManager.GetInstance().IsStandalone)
                return;

            if (devicesTree.MouseMoveHitTest.Item?.RowObject is not DevicesChannelItem channelItem)
                return;

            var device = channelItem.Device;
            if (device is null)
                return;

            GetDeviceBinder().Bind(device, channelItem.Channel);
        }

        private void ContextMenu_Opening(object sender, CancelEventArgs e)
        {
            if (ProjectManager.GetInstance().IsStandalone)
            {
                goToFasMenuItem.Enabled = false;
                return;
            }

            goToFasMenuItem.Enabled = TryGetSelectedEplanFunction(out _);
        }

        private void GoToFasMenuItem_Click(object sender, EventArgs e)
        {
            if (ProjectManager.GetInstance().IsStandalone)
                return;
            if (!TryGetSelectedEplanFunction(out var function))
                return;

            EplanNavigateHelper.OpenFunctionPageWithError(function);
        }

        private void GoToFasAt(Point location)
        {
            if (ProjectManager.GetInstance().IsStandalone)
                return;

            var rowObject = (devicesTree.GetItemAt(location.X, location.Y) as OLVListItem)
                ?.RowObject;
            if (!TryGetEplanFunction(rowObject, out var function))
                return;

            EplanNavigateHelper.OpenFunctionPageWithError(function);
        }

        private bool TryGetSelectedEplanFunction(out Function function)
        {
            function = null;
            var selected = devicesTree.SelectedObjects?.Cast<object>().ToList() ?? new List<object>();
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
            var rect = e.ClipRectangle;
            rect.Inflate(-1, -1);
            e.Graphics.Clear(Color.White);
            e.Graphics.DrawRectangle(new Pen(new SolidBrush(Color.Black)), rect);
        }

        private void SearchBoxTLP_MouseClick(object sender, MouseEventArgs e)
        {
            textBox_search.Focus();
        }

        private void TextBox_search_TextChanged(object sender, EventArgs e)
        {
            if (textBox_search.Text == "Поиск..." || textBox_search.Text == string.Empty)
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
            if (textBox_search.Text == "Поиск...")
            {
                textBox_search.ForeColor = Color.Black;
                textBox_search.Text = string.Empty;
            }

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
                textBox_search.ForeColor = Color.Gray;
                textBox_search.Text = "Поиск...";
                searchBoxTLP.Visible = false;
                searchTSButton.Visible = true;
            }

        }

        private void TextBox_search_KeyUp(object sender, KeyEventArgs e)
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
                    devicesTree.Focus();
                    break;
            }
        }

        private bool UpdatingModelFilter { get; set; }

        private void UpdateModelFilter()
        {
            UpdatingModelFilter = true;
            bool searchBoxWasFocused = textBox_search.Focused;

            devicesTree.UseFiltering = false;
            DataContext.SearchContext.FoundItems.Clear();
            ResetFilter(DataContext.Roots.Cast<IFilterableViewItem>());

            TextMatchFilter highlightingFilter = null;
            if (searchText != string.Empty)
            {
                devicesTree.UseFiltering = true;
                searchIterator.Maximum = DataContext.SearchContext.FoundItems.Count;
                highlightingFilter = TextMatchFilter.Contains(devicesTree, searchText);
            }

            devicesTree.DefaultRenderer = highlightingFilter is null
                ? null
                : new HighlightTextRenderer(highlightingFilter)
                {
                    FillBrush = new SolidBrush(Color.LightGreen),
                    FramePen = new Pen(Color.DarkGreen),
                };

            devicesTree.TreeColumnRenderer.Filter = highlightingFilter;
            devicesTree.TreeColumnRenderer.FillBrush = new SolidBrush(Color.LightGreen);
            devicesTree.TreeColumnRenderer.FramePen = new Pen(Color.DarkGreen);

            if (searchBoxWasFocused)
                textBox_search.Focus();

            UpdatingModelFilter = false;
        }

        private static void ResetFilter(IEnumerable<IFilterableViewItem> items)
        {
            foreach (var item in items)
            {
                item.ResetFilter();
                if (item is IExpandable expandable && expandable.Items is not null)
                {
                    ResetFilter(expandable.Items.OfType<IFilterableViewItem>());
                }
            }
        }

        private void SearchIterator_IndexChanged(object sender, int index)
        {
            var item = DataContext.SearchContext.FoundItems.ElementAtOrDefault(index - 1);
            if (item is null)
                return;

            RecursiveExpandParent(item);
            if (item is IExpandable expandable && devicesTree.CanExpand(expandable))
                devicesTree.Expand(expandable);

            devicesTree.SelectObject(item, true);
            devicesTree.EnsureModelVisible(item);
        }

        private void RecursiveExpandParent(IFilterableViewItem item)
        {
            if (item is not FilterableViewItemBase node || node.ParentItem is not IExpandable parent)
                return;

            if (node.ParentItem is IFilterableViewItem parentFilterable)
                RecursiveExpandParent(parentFilterable);

            if (devicesTree.CanExpand(parent))
                devicesTree.Expand(parent);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveCfg();
            base.OnFormClosing(e);
        }

        /// <summary>
        /// Пропускает Enter в TextBox при многострочном редактировании.
        /// </summary>
        private sealed class DevicesCellEditKeyEngine : CellEditKeyEngine
        {
            internal bool MultilineEditActive { get; set; }

            public override bool HandleKey(ObjectListView olv, Keys keyData)
            {
                if (MultilineEditActive &&
                    (keyData == Keys.Enter || keyData == Keys.Return))
                {
                    return false;
                }

                return base.HandleKey(olv, keyData);
            }
        }
    }
}
