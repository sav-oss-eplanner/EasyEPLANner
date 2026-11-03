using BrightIdeasSoftware;

namespace EasyEPlanner.Binding.View
{
    public partial class BindingViewControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.MainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.toolbarPanel = new System.Windows.Forms.TableLayoutPanel();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.Expand = new System.Windows.Forms.ToolStripDropDownButton();
            this.Expand_1 = new System.Windows.Forms.ToolStripMenuItem();
            this.Expand_2 = new System.Windows.Forms.ToolStripMenuItem();
            this.Expand_3 = new System.Windows.Forms.ToolStripMenuItem();
            this.Expand_4 = new System.Windows.Forms.ToolStripMenuItem();
            this.Expand_5 = new System.Windows.Forms.ToolStripMenuItem();
            this.Expand_6 = new System.Windows.Forms.ToolStripMenuItem();
            this.syncButton = new System.Windows.Forms.ToolStripButton();
            this.groupingToggleButton = new System.Windows.Forms.ToolStripButton();
            this.noAssignmentBtn = new System.Windows.Forms.ToolStripButton();
            this.searchBoxTLP = new System.Windows.Forms.TableLayoutPanel();
            this.searchPictureBox = new System.Windows.Forms.PictureBox();
            this.textBox_search = new System.Windows.Forms.TextBox();
            this.searchIterator = new EditorControls.SearchIterator();
            this.searchButtonToolStrip = new System.Windows.Forms.ToolStrip();
            this.searchTSButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparatorSearch = new System.Windows.Forms.ToolStripSeparator();
            this.bindingTree = new BrightIdeasSoftware.TreeListView();
            this.ViewItemImageList = new System.Windows.Forms.ImageList(this.components);
            this.MainTableLayoutPanel.SuspendLayout();
            this.toolbarPanel.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.searchBoxTLP.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.searchPictureBox)).BeginInit();
            this.searchButtonToolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingTree)).BeginInit();
            this.SuspendLayout();
            // 
            // MainTableLayoutPanel
            // 
            this.MainTableLayoutPanel.ColumnCount = 1;
            this.MainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.MainTableLayoutPanel.Controls.Add(this.toolbarPanel, 0, 0);
            this.MainTableLayoutPanel.Controls.Add(this.bindingTree, 0, 1);
            this.MainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.MainTableLayoutPanel.Name = "MainTableLayoutPanel";
            this.MainTableLayoutPanel.RowCount = 2;
            this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.MainTableLayoutPanel.Size = new System.Drawing.Size(800, 600);
            this.MainTableLayoutPanel.TabIndex = 0;
            // 
            // toolbarPanel
            // 
            this.toolbarPanel.ColumnCount = 3;
            this.toolbarPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.toolbarPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.toolbarPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.toolbarPanel.Controls.Add(this.toolStrip1, 0, 0);
            this.toolbarPanel.Controls.Add(this.searchBoxTLP, 1, 0);
            this.toolbarPanel.Controls.Add(this.searchButtonToolStrip, 2, 0);
            this.toolbarPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolbarPanel.Location = new System.Drawing.Point(0, 0);
            this.toolbarPanel.Margin = new System.Windows.Forms.Padding(0);
            this.toolbarPanel.Name = "toolbarPanel";
            this.toolbarPanel.RowCount = 1;
            this.toolbarPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.toolbarPanel.Size = new System.Drawing.Size(800, 30);
            this.toolbarPanel.TabIndex = 0;
            // 
            // toolStrip1
            // 
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Expand,
            this.syncButton,
            this.groupingToggleButton,
            this.noAssignmentBtn});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(548, 30);
            this.toolStrip1.TabIndex = 0;
            // 
            // Expand
            // 
            this.Expand.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.Expand.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Expand_1,
            this.Expand_2,
            this.Expand_3,
            this.Expand_4,
            this.Expand_5,
            this.Expand_6});
            this.Expand.Image = global::EasyEPlanner.Properties.Resources.expand;
            this.Expand.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.Expand.Name = "Expand";
            this.Expand.Size = new System.Drawing.Size(29, 27);
            this.Expand.Text = "Развернуть";
            this.Expand.ToolTipText = "Выбрать уровень развертки";
            // 
            // Expand_1
            // 
            this.Expand_1.Image = global::EasyEPlanner.Properties.Resources.expand1;
            this.Expand_1.Name = "Expand_1";
            this.Expand_1.Size = new System.Drawing.Size(180, 22);
            this.Expand_1.Tag = "1";
            this.Expand_1.Text = "Уровень 1";
            this.Expand_1.Click += new System.EventHandler(this.Expand_Click);
            // 
            // Expand_2
            // 
            this.Expand_2.Image = global::EasyEPlanner.Properties.Resources.expand2;
            this.Expand_2.Name = "Expand_2";
            this.Expand_2.Size = new System.Drawing.Size(180, 22);
            this.Expand_2.Tag = "2";
            this.Expand_2.Text = "Уровень 2";
            this.Expand_2.Click += new System.EventHandler(this.Expand_Click);
            // 
            // Expand_3
            // 
            this.Expand_3.Image = global::EasyEPlanner.Properties.Resources.expand3;
            this.Expand_3.Name = "Expand_3";
            this.Expand_3.Size = new System.Drawing.Size(180, 22);
            this.Expand_3.Tag = "3";
            this.Expand_3.Text = "Уровень 3";
            this.Expand_3.Click += new System.EventHandler(this.Expand_Click);
            // 
            // Expand_4
            // 
            this.Expand_4.Image = global::EasyEPlanner.Properties.Resources.expand4;
            this.Expand_4.Name = "Expand_4";
            this.Expand_4.Size = new System.Drawing.Size(180, 22);
            this.Expand_4.Tag = "4";
            this.Expand_4.Text = "Уровень 4";
            this.Expand_4.Click += new System.EventHandler(this.Expand_Click);
            // 
            // Expand_5
            // 
            this.Expand_5.Image = global::EasyEPlanner.Properties.Resources.expand5;
            this.Expand_5.Name = "Expand_5";
            this.Expand_5.Size = new System.Drawing.Size(180, 22);
            this.Expand_5.Tag = "5";
            this.Expand_5.Text = "Уровень 5";
            this.Expand_5.Click += new System.EventHandler(this.Expand_Click);
            // 
            // Expand_6
            // 
            this.Expand_6.Name = "Expand_6";
            this.Expand_6.Size = new System.Drawing.Size(180, 22);
            this.Expand_6.Tag = "0";
            this.Expand_6.Text = "Свернуть всё";
            this.Expand_6.Click += new System.EventHandler(this.Expand_Click);
            // 
            // syncButton
            // 
            this.syncButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.syncButton.Image = global::EasyEPlanner.Properties.Resources.refresh;
            this.syncButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.syncButton.Name = "syncButton";
            this.syncButton.Size = new System.Drawing.Size(23, 27);
            this.syncButton.Text = "Обновить";
            this.syncButton.ToolTipText = "Синхронизация и сохранение";
            this.syncButton.Click += new System.EventHandler(this.SyncButton_Click);
            // 
            // groupingToggleButton
            // 
            this.groupingToggleButton.CheckOnClick = true;
            this.groupingToggleButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.groupingToggleButton.Image = global::EasyEPlanner.Properties.Resources.devicesGroupingObjectType;
            this.groupingToggleButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.groupingToggleButton.Name = "groupingToggleButton";
            this.groupingToggleButton.Size = new System.Drawing.Size(23, 27);
            this.groupingToggleButton.Click += new System.EventHandler(this.GroupingToggleButton_Click);
            // 
            // noAssignmentBtn
            // 
            this.noAssignmentBtn.Checked = true;
            this.noAssignmentBtn.CheckOnClick = true;
            this.noAssignmentBtn.CheckState = System.Windows.Forms.CheckState.Checked;
            this.noAssignmentBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.noAssignmentBtn.Image = global::EasyEPlanner.Properties.Resources.toCheck;
            this.noAssignmentBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.noAssignmentBtn.Name = "noAssignmentBtn";
            this.noAssignmentBtn.Size = new System.Drawing.Size(23, 27);
            this.noAssignmentBtn.ToolTipText = "Без привязки";
            this.noAssignmentBtn.Click += new System.EventHandler(this.NoAssignmentBtn_Click);
            // 
            // searchBoxTLP
            // 
            this.searchBoxTLP.AutoSize = true;
            this.searchBoxTLP.BackColor = System.Drawing.SystemColors.Window;
            this.searchBoxTLP.ColumnCount = 3;
            this.searchBoxTLP.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 19F));
            this.searchBoxTLP.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.searchBoxTLP.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 85F));
            this.searchBoxTLP.Controls.Add(this.searchPictureBox, 0, 0);
            this.searchBoxTLP.Controls.Add(this.textBox_search, 1, 0);
            this.searchBoxTLP.Controls.Add(this.searchIterator, 2, 0);
            this.searchBoxTLP.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.searchBoxTLP.Dock = System.Windows.Forms.DockStyle.Fill;
            this.searchBoxTLP.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.searchBoxTLP.Location = new System.Drawing.Point(550, 2);
            this.searchBoxTLP.Margin = new System.Windows.Forms.Padding(2);
            this.searchBoxTLP.Name = "searchBoxTLP";
            this.searchBoxTLP.RowCount = 1;
            this.searchBoxTLP.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.searchBoxTLP.Size = new System.Drawing.Size(216, 26);
            this.searchBoxTLP.TabIndex = 1;
            this.searchBoxTLP.Visible = false;
            this.searchBoxTLP.Paint += new System.Windows.Forms.PaintEventHandler(this.SearchBoxTLP_Paint);
            this.searchBoxTLP.MouseClick += new System.Windows.Forms.MouseEventHandler(this.SearchBoxTLP_MouseClick);
            // 
            // searchPictureBox
            // 
            this.searchPictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.searchPictureBox.ErrorImage = global::EasyEPlanner.Properties.Resources.hideEmptyItems;
            this.searchPictureBox.Image = global::EasyEPlanner.Properties.Resources.search;
            this.searchPictureBox.InitialImage = null;
            this.searchPictureBox.Location = new System.Drawing.Point(3, 5);
            this.searchPictureBox.Margin = new System.Windows.Forms.Padding(3, 5, 2, 4);
            this.searchPictureBox.MaximumSize = new System.Drawing.Size(14, 14);
            this.searchPictureBox.Name = "searchPictureBox";
            this.searchPictureBox.Size = new System.Drawing.Size(14, 14);
            this.searchPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.searchPictureBox.TabIndex = 1;
            this.searchPictureBox.TabStop = false;
            this.searchPictureBox.MouseClick += new System.Windows.Forms.MouseEventHandler(this.SearchBoxTLP_MouseClick);
            // 
            // textBox_search
            // 
            this.textBox_search.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox_search.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox_search.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_search.ForeColor = System.Drawing.Color.Gray;
            this.textBox_search.Location = new System.Drawing.Point(20, 5);
            this.textBox_search.Margin = new System.Windows.Forms.Padding(1, 5, 1, 5);
            this.textBox_search.MaximumSize = new System.Drawing.Size(110, 0);
            this.textBox_search.MinimumSize = new System.Drawing.Size(110, 0);
            this.textBox_search.Name = "textBox_search";
            this.textBox_search.ShortcutsEnabled = false;
            this.textBox_search.Size = new System.Drawing.Size(110, 13);
            this.textBox_search.TabIndex = 0;
            this.textBox_search.Text = "Поиск...";
            this.textBox_search.TextChanged += new System.EventHandler(this.TextBox_search_TextChanged);
            this.textBox_search.GotFocus += new System.EventHandler(this.TextBox_search_GotFocus);
            this.textBox_search.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TextBox_search_KeyUp);
            this.textBox_search.LostFocus += new System.EventHandler(this.TextBox_search_LostFocus);
            // 
            // searchIterator
            // 
            this.searchIterator.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.searchIterator.Dock = System.Windows.Forms.DockStyle.Fill;
            this.searchIterator.Index = 0;
            this.searchIterator.Location = new System.Drawing.Point(131, 2);
            this.searchIterator.Margin = new System.Windows.Forms.Padding(0, 2, 1, 1);
            this.searchIterator.Maximum = 0;
            this.searchIterator.Name = "searchIterator";
            this.searchIterator.Size = new System.Drawing.Size(84, 23);
            this.searchIterator.TabIndex = 2;
            // 
            // searchButtonToolStrip
            // 
            this.searchButtonToolStrip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.searchButtonToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.searchButtonToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.searchTSButton,
            this.toolStripSeparatorSearch});
            this.searchButtonToolStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.searchButtonToolStrip.Location = new System.Drawing.Point(768, 0);
            this.searchButtonToolStrip.Name = "searchButtonToolStrip";
            this.searchButtonToolStrip.Size = new System.Drawing.Size(32, 30);
            this.searchButtonToolStrip.TabIndex = 2;
            // 
            // searchTSButton
            // 
            this.searchTSButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.searchTSButton.Image = global::EasyEPlanner.Properties.Resources.search;
            this.searchTSButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.searchTSButton.Name = "searchTSButton";
            this.searchTSButton.Size = new System.Drawing.Size(23, 27);
            this.searchTSButton.ToolTipText = "Поиск (Ctrl + F)";
            this.searchTSButton.Click += new System.EventHandler(this.SearchTSButton_Click);
            // 
            // toolStripSeparatorSearch
            // 
            this.toolStripSeparatorSearch.Name = "toolStripSeparatorSearch";
            this.toolStripSeparatorSearch.Size = new System.Drawing.Size(6, 30);
            // 
            // bindingTree
            // 
            this.bindingTree.AlternateRowBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.bindingTree.CellEditActivation = BrightIdeasSoftware.ObjectListView.CellEditActivateMode.None;
            this.bindingTree.CellEditUseWholeCell = false;
            this.bindingTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bindingTree.FullRowSelect = true;
            this.bindingTree.GridLines = true;
            this.bindingTree.HideSelection = false;
            this.bindingTree.LargeImageList = this.ViewItemImageList;
            this.bindingTree.Location = new System.Drawing.Point(0, 30);
            this.bindingTree.Margin = new System.Windows.Forms.Padding(0);
            this.bindingTree.Name = "bindingTree";
            this.bindingTree.RowHeight = 20;
            this.bindingTree.ShowGroups = false;
            this.bindingTree.Size = new System.Drawing.Size(800, 570);
            this.bindingTree.SmallImageList = this.ViewItemImageList;
            this.bindingTree.TabIndex = 1;
            this.bindingTree.UseAlternatingBackColors = true;
            this.bindingTree.UseCellFormatEvents = true;
            this.bindingTree.UseCompatibleStateImageBehavior = false;
            this.bindingTree.View = System.Windows.Forms.View.Details;
            this.bindingTree.VirtualMode = true;
            this.bindingTree.Expanded += new System.EventHandler<BrightIdeasSoftware.TreeBranchExpandedEventArgs>(this.ItemExpanded);
            this.bindingTree.Collapsed += new System.EventHandler<BrightIdeasSoftware.TreeBranchCollapsedEventArgs>(this.ItemCollapsed);
            this.bindingTree.FormatCell += new System.EventHandler<BrightIdeasSoftware.FormatCellEventArgs>(this.BindingTree_FormatCell);
            this.bindingTree.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.BindingTree_MouseDoubleClick);
            this.bindingTree.MouseDown += new System.Windows.Forms.MouseEventHandler(this.BindingTree_MouseDown);
            // 
            // ViewItemImageList
            // 
            this.ViewItemImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.ViewItemImageList.ImageSize = new System.Drawing.Size(16, 16);
            this.ViewItemImageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // BindingViewControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.MainTableLayoutPanel);
            this.Name = "BindingViewControl";
            this.Text = "Привязка";
            this.MainTableLayoutPanel.ResumeLayout(false);
            this.toolbarPanel.ResumeLayout(false);
            this.toolbarPanel.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.searchBoxTLP.ResumeLayout(false);
            this.searchBoxTLP.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.searchPictureBox)).EndInit();
            this.searchButtonToolStrip.ResumeLayout(false);
            this.searchButtonToolStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingTree)).EndInit();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TableLayoutPanel MainTableLayoutPanel;
        private TreeListView bindingTree;
        private System.Windows.Forms.ImageList ViewItemImageList;
        private System.Windows.Forms.TableLayoutPanel toolbarPanel;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton Expand;
        private System.Windows.Forms.ToolStripMenuItem Expand_1;
        private System.Windows.Forms.ToolStripMenuItem Expand_2;
        private System.Windows.Forms.ToolStripMenuItem Expand_3;
        private System.Windows.Forms.ToolStripMenuItem Expand_4;
        private System.Windows.Forms.ToolStripMenuItem Expand_5;
        private System.Windows.Forms.ToolStripMenuItem Expand_6;
        private System.Windows.Forms.ToolStripButton syncButton;
        private System.Windows.Forms.ToolStripButton groupingToggleButton;
        private System.Windows.Forms.ToolStripButton noAssignmentBtn;
        private System.Windows.Forms.TableLayoutPanel searchBoxTLP;
        private System.Windows.Forms.PictureBox searchPictureBox;
        private System.Windows.Forms.TextBox textBox_search;
        private EditorControls.SearchIterator searchIterator;
        private System.Windows.Forms.ToolStrip searchButtonToolStrip;
        private System.Windows.Forms.ToolStripButton searchTSButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparatorSearch;
    }
}
