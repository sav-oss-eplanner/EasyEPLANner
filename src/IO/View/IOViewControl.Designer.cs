using BrightIdeasSoftware;

namespace IO.View
{
    partial class IOViewControl
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IOViewControl));
            this.MainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.StructPLC = new BrightIdeasSoftware.TreeListView();
            this.ViewItemImageList = new System.Windows.Forms.ImageList(this.components);
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.Expand = new System.Windows.Forms.ToolStripDropDownButton();
            this.Expand_1 = new System.Windows.Forms.ToolStripMenuItem();
            this.Expand_2 = new System.Windows.Forms.ToolStripMenuItem();
            this.Expand_3 = new System.Windows.Forms.ToolStripMenuItem();
            this.Expand_4 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.searchBoxTLP = new System.Windows.Forms.TableLayoutPanel();
            this.searchPictureBox = new System.Windows.Forms.PictureBox();
            this.textBox_search = new System.Windows.Forms.TextBox();
            this.searchIterator = new EditorControls.SearchIterator();
            this.searchButtonToolStrip = new System.Windows.Forms.ToolStrip();
            this.searchTSButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparatorSearch = new System.Windows.Forms.ToolStripSeparator();
            this.MainTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.StructPLC)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.searchBoxTLP.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.searchPictureBox)).BeginInit();
            this.searchButtonToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainTableLayoutPanel
            // 
            this.MainTableLayoutPanel.ColumnCount = 1;
            this.MainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.MainTableLayoutPanel.Controls.Add(this.StructPLC, 0, 1);
            this.MainTableLayoutPanel.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.MainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.MainTableLayoutPanel.Name = "MainTableLayoutPanel";
            this.MainTableLayoutPanel.RowCount = 2;
            this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.MainTableLayoutPanel.Size = new System.Drawing.Size(597, 521);
            this.MainTableLayoutPanel.TabIndex = 1;
            // 
            // StructPLC
            // 
            this.StructPLC.AlternateRowBackColor = System.Drawing.Color.White;
            this.StructPLC.CellEditActivation = BrightIdeasSoftware.ObjectListView.CellEditActivateMode.DoubleClick;
            this.StructPLC.CellEditUseWholeCell = false;
            this.StructPLC.CopySelectionOnControlCUsesDragSource = false;
            this.StructPLC.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StructPLC.FullRowSelect = true;
            this.StructPLC.GridLines = true;
            this.StructPLC.HideSelection = false;
            this.StructPLC.LabelWrap = false;
            this.StructPLC.LargeImageList = this.ViewItemImageList;
            this.StructPLC.Location = new System.Drawing.Point(0, 30);
            this.StructPLC.Margin = new System.Windows.Forms.Padding(0);
            this.StructPLC.Name = "StructPLC";
            this.StructPLC.OwnerDrawnHeader = true;
            this.StructPLC.PersistentCheckBoxes = false;
            this.StructPLC.RowHeight = 20;
            this.StructPLC.SelectAllOnControlA = false;
            this.StructPLC.SelectColumnsMenuStaysOpen = false;
            this.StructPLC.SelectColumnsOnRightClick = false;
            this.StructPLC.SelectColumnsOnRightClickBehaviour = BrightIdeasSoftware.ObjectListView.ColumnSelectBehaviour.None;
            this.StructPLC.ShowFilterMenuOnRightClick = false;
            this.StructPLC.ShowGroups = false;
            this.StructPLC.ShowImagesOnSubItems = true;
            this.StructPLC.ShowSortIndicators = false;
            this.StructPLC.Size = new System.Drawing.Size(597, 491);
            this.StructPLC.SmallImageList = this.ViewItemImageList;
            this.StructPLC.TabIndex = 0;
            this.StructPLC.TriggerCellOverEventsWhenOverHeader = false;
            this.StructPLC.UseAlternatingBackColors = true;
            this.StructPLC.UseCellFormatEvents = true;
            this.StructPLC.UseCompatibleStateImageBehavior = false;
            this.StructPLC.UseHotControls = false;
            this.StructPLC.View = System.Windows.Forms.View.Details;
            this.StructPLC.VirtualMode = true;
            this.StructPLC.Expanded += new System.EventHandler<BrightIdeasSoftware.TreeBranchExpandedEventArgs>(this.ItemExpanded);
            this.StructPLC.Collapsed += new System.EventHandler<BrightIdeasSoftware.TreeBranchCollapsedEventArgs>(this.ItemCollapsed);
            this.StructPLC.CellEditFinishing += new BrightIdeasSoftware.CellEditEventHandler(this.CellEditFinishing);
            this.StructPLC.CellEditStarting += new BrightIdeasSoftware.CellEditEventHandler(this.CellEditStarting);
            this.StructPLC.FormatCell += new System.EventHandler<BrightIdeasSoftware.FormatCellEventArgs>(this.StructPLC_FormatCell);
            this.StructPLC.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.SelectionChanged);
            this.StructPLC.KeyDown += new System.Windows.Forms.KeyEventHandler(this.KeyDownHandler);
            this.StructPLC.MouseEnter += new System.EventHandler(this.StructPLC_MouseEnter);
            this.StructPLC.MouseLeave += new System.EventHandler(this.StructPLC_MouseLeave);
            // 
            // ViewItemImageList
            // 
            this.ViewItemImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ViewItemImageList.ImageStream")));
            this.ViewItemImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.ViewItemImageList.Images.SetKeyName(0, "cab.png");
            this.ViewItemImageList.Images.SetKeyName(1, "node.png");
            this.ViewItemImageList.Images.SetKeyName(2, "module_black.png");
            this.ViewItemImageList.Images.SetKeyName(3, "module_blue.png");
            this.ViewItemImageList.Images.SetKeyName(4, "module_gray.png");
            this.ViewItemImageList.Images.SetKeyName(5, "module_green.png");
            this.ViewItemImageList.Images.SetKeyName(6, "module_lime.png");
            this.ViewItemImageList.Images.SetKeyName(7, "module_orange.png");
            this.ViewItemImageList.Images.SetKeyName(8, "module_red.png");
            this.ViewItemImageList.Images.SetKeyName(9, "module_violet.png");
            this.ViewItemImageList.Images.SetKeyName(10, "module_yellow.png");
            this.ViewItemImageList.Images.SetKeyName(11, "module_empty.png");
            this.ViewItemImageList.Images.SetKeyName(12, "clamp.png");
            this.ViewItemImageList.Images.SetKeyName(13, "cable.png");
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 3;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.Controls.Add(this.toolStrip1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.searchBoxTLP, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.searchButtonToolStrip, 2, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(597, 30);
            this.tableLayoutPanel2.TabIndex = 1;
            // 
            // toolStrip1
            // 
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Expand,
            this.toolStripButton1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(597, 30);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
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
            this.searchBoxTLP.Location = new System.Drawing.Point(0, 0);
            this.searchBoxTLP.Margin = new System.Windows.Forms.Padding(0);
            this.searchBoxTLP.Name = "searchBoxTLP";
            this.searchBoxTLP.RowCount = 1;
            this.searchBoxTLP.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.searchBoxTLP.Size = new System.Drawing.Size(0, 30);
            this.searchBoxTLP.TabIndex = 1;
            this.searchBoxTLP.Visible = false;
            this.searchBoxTLP.Paint += new System.Windows.Forms.PaintEventHandler(this.SearchBoxTLP_Paint);
            this.searchBoxTLP.MouseClick += new System.Windows.Forms.MouseEventHandler(this.SearchBoxTLP_MouseClick);
            //
            // searchPictureBox
            //
            this.searchPictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.searchPictureBox.Image = global::EasyEPlanner.Properties.Resources.search;
            this.searchPictureBox.InitialImage = null;
            this.searchPictureBox.Location = new System.Drawing.Point(3, 8);
            this.searchPictureBox.Margin = new System.Windows.Forms.Padding(3, 8, 2, 5);
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
            this.textBox_search.Location = new System.Drawing.Point(20, 8);
            this.textBox_search.Margin = new System.Windows.Forms.Padding(1, 8, 1, 5);
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
            this.searchIterator.Size = new System.Drawing.Size(84, 27);
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
            this.searchButtonToolStrip.Location = new System.Drawing.Point(0, 0);
            this.searchButtonToolStrip.Name = "searchButtonToolStrip";
            this.searchButtonToolStrip.Size = new System.Drawing.Size(30, 30);
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
            // Expand
            // 
            this.Expand.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.Expand.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Expand_1,
            this.Expand_2,
            this.Expand_3,
            this.Expand_4});
            this.Expand.Image = ((System.Drawing.Image)(resources.GetObject("Expand.Image")));
            this.Expand.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.Expand.Name = "Expand";
            this.Expand.Size = new System.Drawing.Size(29, 27);
            this.Expand.Text = "toolStripDropDownButton1";
            this.Expand.ToolTipText = "Выбрать уровень развертки";
            // 
            // Expand_1
            // 
            this.Expand_1.Image = ((System.Drawing.Image)(resources.GetObject("Expand_1.Image")));
            this.Expand_1.Name = "Expand_1";
            this.Expand_1.Size = new System.Drawing.Size(183, 22);
            this.Expand_1.Tag = "1";
            this.Expand_1.Text = "Шкафы управления";
            this.Expand_1.ToolTipText = "Уровнь 1: шкафы управления";
            this.Expand_1.Click += new System.EventHandler(this.Expand_Click);
            // 
            // Expand_2
            // 
            this.Expand_2.Image = ((System.Drawing.Image)(resources.GetObject("Expand_2.Image")));
            this.Expand_2.Name = "Expand_2";
            this.Expand_2.Size = new System.Drawing.Size(183, 22);
            this.Expand_2.Tag = "2";
            this.Expand_2.Text = "Узлы";
            this.Expand_2.ToolTipText = "Уровень 2: узлы";
            this.Expand_2.Click += new System.EventHandler(this.Expand_Click);
            // 
            // Expand_3
            // 
            this.Expand_3.Image = ((System.Drawing.Image)(resources.GetObject("Expand_3.Image")));
            this.Expand_3.Name = "Expand_3";
            this.Expand_3.Size = new System.Drawing.Size(183, 22);
            this.Expand_3.Tag = "3";
            this.Expand_3.Text = "Модули";
            this.Expand_3.ToolTipText = "Уровень 3: модули";
            this.Expand_3.Click += new System.EventHandler(this.Expand_Click);
            // 
            // Expand_4
            // 
            this.Expand_4.Image = ((System.Drawing.Image)(resources.GetObject("Expand_4.Image")));
            this.Expand_4.Name = "Expand_4";
            this.Expand_4.Size = new System.Drawing.Size(183, 22);
            this.Expand_4.Tag = "4";
            this.Expand_4.Text = "Клеммы";
            this.Expand_4.ToolTipText = "Уровень 4: клеммы";
            this.Expand_4.Click += new System.EventHandler(this.Expand_Click);
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(23, 27);
            this.toolStripButton1.Text = "toolStripButton1";
            this.toolStripButton1.ToolTipText = "Синхронизация и сохранение";
            this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // IOViewControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(597, 521);
            this.Controls.Add(this.MainTableLayoutPanel);
            this.Name = "IOViewControl";
            this.Text = "IOViewControl";
            this.MainTableLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.StructPLC)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.searchBoxTLP.ResumeLayout(false);
            this.searchBoxTLP.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.searchPictureBox)).EndInit();
            this.searchButtonToolStrip.ResumeLayout(false);
            this.searchButtonToolStrip.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TableLayoutPanel MainTableLayoutPanel;
        private TreeListView StructPLC;
        private System.Windows.Forms.ImageList ViewItemImageList;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton Expand;
        private System.Windows.Forms.ToolStripMenuItem Expand_1;
        private System.Windows.Forms.ToolStripMenuItem Expand_2;
        private System.Windows.Forms.ToolStripMenuItem Expand_3;
        private System.Windows.Forms.ToolStripMenuItem Expand_4;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.TableLayoutPanel searchBoxTLP;
        private System.Windows.Forms.PictureBox searchPictureBox;
        private System.Windows.Forms.TextBox textBox_search;
        private EditorControls.SearchIterator searchIterator;
        private System.Windows.Forms.ToolStrip searchButtonToolStrip;
        private System.Windows.Forms.ToolStripButton searchTSButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparatorSearch;
    }
}
