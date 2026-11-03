namespace Editor
{
    partial class StatesCreatorDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StatesCreatorDialog));
            this.StatesListView = new System.Windows.Forms.ListView();
            this.StatesImageList = new System.Windows.Forms.ImageList(this.components);
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.CreateBttn = new System.Windows.Forms.Button();
            this.CancelBttn = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // StatesListView
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.StatesListView, 3);
            this.StatesListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StatesListView.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.StatesListView.FullRowSelect = true;
            this.StatesListView.HideSelection = false;
            this.StatesListView.Location = new System.Drawing.Point(5, 5);
            this.StatesListView.Margin = new System.Windows.Forms.Padding(5);
            this.StatesListView.MultiSelect = false;
            this.StatesListView.Name = "StatesListView";
            this.StatesListView.Size = new System.Drawing.Size(254, 242);
            this.StatesListView.SmallImageList = this.StatesImageList;
            this.StatesListView.TabIndex = 1;
            this.StatesListView.UseCompatibleStateImageBehavior = false;
            this.StatesListView.View = System.Windows.Forms.View.List;
            this.StatesListView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.StatesListView_KeyDown);
            this.StatesListView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.StatesListView_MouseDoubleClick);
            // 
            // StatesImageList
            // 
            this.StatesImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("StatesImageList.ImageStream")));
            this.StatesImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.StatesImageList.Images.SetKeyName(0, "IDLE");
            this.StatesImageList.Images.SetKeyName(1, "STARTING");
            this.StatesImageList.Images.SetKeyName(2, "RUN");
            this.StatesImageList.Images.SetKeyName(3, "COMPLETING");
            this.StatesImageList.Images.SetKeyName(4, "COMPLETE");
            this.StatesImageList.Images.SetKeyName(5, "PAUSING");
            this.StatesImageList.Images.SetKeyName(6, "PAUSE");
            this.StatesImageList.Images.SetKeyName(7, "UNPAUSING");
            this.StatesImageList.Images.SetKeyName(8, "STOPPING");
            this.StatesImageList.Images.SetKeyName(9, "STOP");
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel1.Controls.Add(this.StatesListView, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.CreateBttn, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.CancelBttn, 2, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(264, 281);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // CreateBttn
            // 
            this.CreateBttn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CreateBttn.Location = new System.Drawing.Point(67, 255);
            this.CreateBttn.Name = "CreateBttn";
            this.CreateBttn.Size = new System.Drawing.Size(94, 23);
            this.CreateBttn.TabIndex = 2;
            this.CreateBttn.Text = "Добавить";
            this.CreateBttn.UseVisualStyleBackColor = true;
            this.CreateBttn.Click += new System.EventHandler(this.CreateBttn_Click);
            // 
            // CancelBttn
            // 
            this.CancelBttn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CancelBttn.Location = new System.Drawing.Point(167, 255);
            this.CancelBttn.Name = "CancelBttn";
            this.CancelBttn.Size = new System.Drawing.Size(94, 23);
            this.CancelBttn.TabIndex = 3;
            this.CancelBttn.Text = "Отмена";
            this.CancelBttn.UseVisualStyleBackColor = true;
            this.CancelBttn.Click += new System.EventHandler(this.CancelBttn_Click);
            // 
            // StatesCreatorDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(264, 281);
            this.Controls.Add(this.tableLayoutPanel1);
            this.MinimumSize = new System.Drawing.Size(280, 320);
            this.Name = "StatesCreatorDialog";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Добавить состояние операции";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ListView StatesListView;
        private System.Windows.Forms.ImageList StatesImageList;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button CreateBttn;
        private System.Windows.Forms.Button CancelBttn;
    }
}