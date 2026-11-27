namespace EasyEPlanner
{
    partial class XMLReporterDialog
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
            this.newOrNotChBaseGroup = new System.Windows.Forms.GroupBox();
            this.oldChBaseBut = new System.Windows.Forms.RadioButton();
            this.newChBaseBut = new System.Windows.Forms.RadioButton();
            this.reviewPathBut = new System.Windows.Forms.Button();
            this.pathTextBoxLabel = new System.Windows.Forms.Label();
            this.pathTextBox = new System.Windows.Forms.TextBox();
            this.ChBaseFormatGroup = new System.Windows.Forms.GroupBox();
            this.newFormatBut = new System.Windows.Forms.RadioButton();
            this.oldFormatBut = new System.Windows.Forms.RadioButton();
            this.combineTagsGroup = new System.Windows.Forms.GroupBox();
            this.nonCombineTagBut = new System.Windows.Forms.RadioButton();
            this.combineTagBut = new System.Windows.Forms.RadioButton();
            this.splitDatabasesGroup = new System.Windows.Forms.GroupBox();
            this.splitDatabasesNoBut = new System.Windows.Forms.RadioButton();
            this.splitDatabasesYesBut = new System.Windows.Forms.RadioButton();
            this.analysisLabel = new System.Windows.Forms.Label();
            this.ExportBut = new System.Windows.Forms.Button();
            this.CancelBut = new System.Windows.Forms.Button();
            this.newOrNotChBaseGroup.SuspendLayout();
            this.ChBaseFormatGroup.SuspendLayout();
            this.combineTagsGroup.SuspendLayout();
            this.splitDatabasesGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // newOrNotChBaseGroup
            // 
            this.newOrNotChBaseGroup.Controls.Add(this.oldChBaseBut);
            this.newOrNotChBaseGroup.Controls.Add(this.newChBaseBut);
            this.newOrNotChBaseGroup.Controls.Add(this.reviewPathBut);
            this.newOrNotChBaseGroup.Controls.Add(this.pathTextBoxLabel);
            this.newOrNotChBaseGroup.Controls.Add(this.pathTextBox);
            this.newOrNotChBaseGroup.Location = new System.Drawing.Point(12, 12);
            this.newOrNotChBaseGroup.Name = "newOrNotChBaseGroup";
            this.newOrNotChBaseGroup.Size = new System.Drawing.Size(366, 87);
            this.newOrNotChBaseGroup.TabIndex = 2;
            this.newOrNotChBaseGroup.TabStop = false;
            this.newOrNotChBaseGroup.Text = "Создать новую базу каналов?";
            // 
            // oldChBaseBut
            // 
            this.oldChBaseBut.AutoSize = true;
            this.oldChBaseBut.Checked = true;
            this.oldChBaseBut.Location = new System.Drawing.Point(52, 19);
            this.oldChBaseBut.Name = "oldChBaseBut";
            this.oldChBaseBut.Size = new System.Drawing.Size(202, 17);
            this.oldChBaseBut.TabIndex = 4;
            this.oldChBaseBut.TabStop = true;
            this.oldChBaseBut.Text = "Нет, использовать существующую";
            this.oldChBaseBut.UseVisualStyleBackColor = true;
            this.oldChBaseBut.CheckedChanged += new System.EventHandler(this.oldChBaseBut_CheckedChanged);
            // 
            // newChBaseBut
            // 
            this.newChBaseBut.AutoSize = true;
            this.newChBaseBut.Location = new System.Drawing.Point(6, 19);
            this.newChBaseBut.Name = "newChBaseBut";
            this.newChBaseBut.Size = new System.Drawing.Size(40, 17);
            this.newChBaseBut.TabIndex = 3;
            this.newChBaseBut.Text = "Да";
            this.newChBaseBut.UseVisualStyleBackColor = true;
            this.newChBaseBut.CheckedChanged += new System.EventHandler(this.newChBaseBut_CheckedChanged);
            // 
            // reviewPathBut
            // 
            this.reviewPathBut.Location = new System.Drawing.Point(300, 60);
            this.reviewPathBut.Name = "reviewPathBut";
            this.reviewPathBut.Size = new System.Drawing.Size(61, 20);
            this.reviewPathBut.TabIndex = 5;
            this.reviewPathBut.Text = "Обзор";
            this.reviewPathBut.UseVisualStyleBackColor = true;
            this.reviewPathBut.Click += new System.EventHandler(this.reviewPathBut_Click);
            // 
            // pathTextBoxLabel
            // 
            this.pathTextBoxLabel.AutoSize = true;
            this.pathTextBoxLabel.Location = new System.Drawing.Point(6, 42);
            this.pathTextBoxLabel.Name = "pathTextBoxLabel";
            this.pathTextBoxLabel.Size = new System.Drawing.Size(161, 13);
            this.pathTextBoxLabel.TabIndex = 4;
            this.pathTextBoxLabel.Text = "Укажите путь к базе каналов:";
            // 
            // pathTextBox
            // 
            this.pathTextBox.Location = new System.Drawing.Point(6, 60);
            this.pathTextBox.Name = "pathTextBox";
            this.pathTextBox.ReadOnly = true;
            this.pathTextBox.Size = new System.Drawing.Size(291, 20);
            this.pathTextBox.TabIndex = 6;
            // 
            // ChBaseFormatGroup
            // 
            this.ChBaseFormatGroup.Controls.Add(this.newFormatBut);
            this.ChBaseFormatGroup.Controls.Add(this.oldFormatBut);
            this.ChBaseFormatGroup.Location = new System.Drawing.Point(12, 219);
            this.ChBaseFormatGroup.Name = "ChBaseFormatGroup";
            this.ChBaseFormatGroup.Size = new System.Drawing.Size(366, 48);
            this.ChBaseFormatGroup.TabIndex = 10;
            this.ChBaseFormatGroup.TabStop = false;
            this.ChBaseFormatGroup.Text = "Формат записи базы каналов?";
            // 
            // newFormatBut
            // 
            this.newFormatBut.AutoSize = true;
            this.newFormatBut.Location = new System.Drawing.Point(143, 20);
            this.newFormatBut.Name = "newFormatBut";
            this.newFormatBut.Size = new System.Drawing.Size(82, 17);
            this.newFormatBut.TabIndex = 12;
            this.newFormatBut.Text = "По именам";
            this.newFormatBut.UseVisualStyleBackColor = true;
            this.newFormatBut.CheckedChanged += new System.EventHandler(this.oldFormatBut_CheckedChanged);
            // 
            // oldFormatBut
            // 
            this.oldFormatBut.AutoSize = true;
            this.oldFormatBut.Checked = true;
            this.oldFormatBut.Location = new System.Drawing.Point(6, 20);
            this.oldFormatBut.Name = "oldFormatBut";
            this.oldFormatBut.Size = new System.Drawing.Size(92, 17);
            this.oldFormatBut.TabIndex = 11;
            this.oldFormatBut.TabStop = true;
            this.oldFormatBut.Text = "По индексам";
            this.oldFormatBut.UseVisualStyleBackColor = true;
            this.oldFormatBut.CheckedChanged += new System.EventHandler(this.newFormatBut_CheckedChanged);
            // 
            // combineTagsGroup
            // 
            this.combineTagsGroup.Controls.Add(this.nonCombineTagBut);
            this.combineTagsGroup.Controls.Add(this.combineTagBut);
            this.combineTagsGroup.Location = new System.Drawing.Point(12, 105);
            this.combineTagsGroup.Name = "combineTagsGroup";
            this.combineTagsGroup.Size = new System.Drawing.Size(366, 48);
            this.combineTagsGroup.TabIndex = 7;
            this.combineTagsGroup.TabStop = false;
            this.combineTagsGroup.Text = "Группировать теги в один подтип?";
            // 
            // nonCombineTagBut
            // 
            this.nonCombineTagBut.AutoSize = true;
            this.nonCombineTagBut.Checked = true;
            this.nonCombineTagBut.Location = new System.Drawing.Point(143, 19);
            this.nonCombineTagBut.Name = "nonCombineTagBut";
            this.nonCombineTagBut.Size = new System.Drawing.Size(44, 17);
            this.nonCombineTagBut.TabIndex = 9;
            this.nonCombineTagBut.TabStop = true;
            this.nonCombineTagBut.Text = "Нет";
            this.nonCombineTagBut.UseVisualStyleBackColor = true;
            this.nonCombineTagBut.CheckedChanged += new System.EventHandler(this.NonCombineTagBut_CheckedChanged);
            // 
            // combineTagBut
            // 
            this.combineTagBut.AutoSize = true;
            this.combineTagBut.Location = new System.Drawing.Point(6, 19);
            this.combineTagBut.Name = "combineTagBut";
            this.combineTagBut.Size = new System.Drawing.Size(40, 17);
            this.combineTagBut.TabIndex = 8;
            this.combineTagBut.Text = "Да";
            this.combineTagBut.UseVisualStyleBackColor = true;
            this.combineTagBut.CheckedChanged += new System.EventHandler(this.combineTagBut_CheckedChanged);
            // 
            // splitDatabasesGroup
            //
            this.splitDatabasesGroup.Controls.Add(this.splitDatabasesNoBut);
            this.splitDatabasesGroup.Controls.Add(this.splitDatabasesYesBut);
            this.splitDatabasesGroup.Location = new System.Drawing.Point(12, 159);
            this.splitDatabasesGroup.Name = "splitDatabasesGroup";
            this.splitDatabasesGroup.Size = new System.Drawing.Size(366, 48);
            this.splitDatabasesGroup.TabIndex = 13;
            this.splitDatabasesGroup.TabStop = false;
            this.splitDatabasesGroup.Text = "Разделить каналы на две базы?";
            //
            // splitDatabasesNoBut
            //
            this.splitDatabasesNoBut.AutoSize = true;
            this.splitDatabasesNoBut.Checked = true;
            this.splitDatabasesNoBut.Location = new System.Drawing.Point(143, 19);
            this.splitDatabasesNoBut.Name = "splitDatabasesNoBut";
            this.splitDatabasesNoBut.Size = new System.Drawing.Size(44, 17);
            this.splitDatabasesNoBut.TabIndex = 15;
            this.splitDatabasesNoBut.TabStop = true;
            this.splitDatabasesNoBut.Text = "Нет";
            this.splitDatabasesNoBut.UseVisualStyleBackColor = true;
            this.splitDatabasesNoBut.CheckedChanged += new System.EventHandler(this.splitDatabasesNoBut_CheckedChanged);
            //
            // splitDatabasesYesBut
            //
            this.splitDatabasesYesBut.AutoSize = true;
            this.splitDatabasesYesBut.Location = new System.Drawing.Point(6, 19);
            this.splitDatabasesYesBut.Name = "splitDatabasesYesBut";
            this.splitDatabasesYesBut.Size = new System.Drawing.Size(40, 17);
            this.splitDatabasesYesBut.TabIndex = 14;
            this.splitDatabasesYesBut.Text = "Да";
            this.splitDatabasesYesBut.UseVisualStyleBackColor = true;
            this.splitDatabasesYesBut.CheckedChanged += new System.EventHandler(this.splitDatabasesYesBut_CheckedChanged);
            // 
            // analysisLabel
            //
            this.analysisLabel.Location = new System.Drawing.Point(12, 270);
            this.analysisLabel.Name = "analysisLabel";
            this.analysisLabel.Size = new System.Drawing.Size(366, 150);
            this.analysisLabel.TabIndex = 15;
            this.analysisLabel.Text = "Предварительный анализ экспорта";
            //
            // ExportBut
            //
            this.ExportBut.Location = new System.Drawing.Point(165, 430);
            this.ExportBut.Name = "ExportBut";
            this.ExportBut.Size = new System.Drawing.Size(105, 23);
            this.ExportBut.TabIndex = 1;
            this.ExportBut.Text = "Экспортировать";
            this.ExportBut.UseVisualStyleBackColor = true;
            this.ExportBut.Click += new System.EventHandler(this.ExportBut_Click);
            // 
            // CancelBut
            // 
            this.CancelBut.Location = new System.Drawing.Point(273, 430);
            this.CancelBut.Name = "CancelBut";
            this.CancelBut.Size = new System.Drawing.Size(105, 23);
            this.CancelBut.TabIndex = 0;
            this.CancelBut.Text = "Отменить";
            this.CancelBut.UseVisualStyleBackColor = true;
            this.CancelBut.Click += new System.EventHandler(this.CancelBut_Click);
            // 
            // ExportChannelBaseDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(390, 465);
            this.Controls.Add(this.analysisLabel);
            this.Controls.Add(this.CancelBut);
            this.Controls.Add(this.ExportBut);
            this.Controls.Add(this.splitDatabasesGroup);
            this.Controls.Add(this.combineTagsGroup);
            this.Controls.Add(this.ChBaseFormatGroup);
            this.Controls.Add(this.newOrNotChBaseGroup);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(406, 504);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(406, 504);
            this.Name = "ExportChannelBaseDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Экспорт базы каналов";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ExportChannelBaseDialog_FormClosed);
            this.Shown += new System.EventHandler(this.XMLReporterDialog_Shown);
            this.newOrNotChBaseGroup.ResumeLayout(false);
            this.newOrNotChBaseGroup.PerformLayout();
            this.ChBaseFormatGroup.ResumeLayout(false);
            this.ChBaseFormatGroup.PerformLayout();
            this.combineTagsGroup.ResumeLayout(false);
            this.combineTagsGroup.PerformLayout();
            this.splitDatabasesGroup.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.RadioButton oldChBaseBut;
        private System.Windows.Forms.RadioButton newChBaseBut;
        private System.Windows.Forms.RadioButton newFormatBut;
        private System.Windows.Forms.RadioButton oldFormatBut;
        private System.Windows.Forms.RadioButton nonCombineTagBut;
        private System.Windows.Forms.RadioButton combineTagBut;
        private System.Windows.Forms.GroupBox splitDatabasesGroup;
        private System.Windows.Forms.RadioButton splitDatabasesNoBut;
        private System.Windows.Forms.RadioButton splitDatabasesYesBut;
        private System.Windows.Forms.Label analysisLabel;
        private System.Windows.Forms.TextBox pathTextBox;
        private System.Windows.Forms.Label pathTextBoxLabel;
        private System.Windows.Forms.Button reviewPathBut;
        private System.Windows.Forms.Button ExportBut;
        private System.Windows.Forms.Button CancelBut;
        public System.Windows.Forms.GroupBox newOrNotChBaseGroup;
        public System.Windows.Forms.GroupBox ChBaseFormatGroup;
        public System.Windows.Forms.GroupBox combineTagsGroup;
    }
}