namespace EasyEPlanner.FileSavers.ModbusXML
{
    partial class ModbusXmlDialog
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
            this.ChbaseGB = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.ChbaseNameTB = new System.Windows.Forms.TextBox();
            this.ChbaseDescriptionTB = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.ChbaseIdTB = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.DriverGB = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.SubtypeTimeoutTB = new System.Windows.Forms.TextBox();
            this.SubtypeProtoTB = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.SubtypeIdTB = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.SubtypePortTB = new System.Windows.Forms.TextBox();
            this.SubtypeNameTB = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.SubtypeDescriptionTB = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.SubtypeIpTB = new System.Windows.Forms.TextBox();
            this.ExportBttn = new System.Windows.Forms.Button();
            this.CancelBttn = new System.Windows.Forms.Button();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.CsvFileTB = new System.Windows.Forms.TextBox();
            this.ReviewCsvPathBttn = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.ChbaseGB.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.DriverGB.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.SuspendLayout();
            // 
            // ChbaseGB
            // 
            this.ChbaseGB.Controls.Add(this.tableLayoutPanel1);
            this.ChbaseGB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ChbaseGB.Location = new System.Drawing.Point(5, 65);
            this.ChbaseGB.Margin = new System.Windows.Forms.Padding(5);
            this.ChbaseGB.Name = "ChbaseGB";
            this.ChbaseGB.Size = new System.Drawing.Size(474, 80);
            this.ChbaseGB.TabIndex = 0;
            this.ChbaseGB.TabStop = false;
            this.ChbaseGB.Text = "Драйвер";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tableLayoutPanel1.Controls.Add(this.ChbaseNameTB, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.ChbaseDescriptionTB, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.label1, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.ChbaseIdTB, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(468, 61);
            this.tableLayoutPanel1.TabIndex = 7;
            // 
            // ChbaseNameTB
            // 
            this.ChbaseNameTB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ChbaseNameTB.Location = new System.Drawing.Point(303, 3);
            this.ChbaseNameTB.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.ChbaseNameTB.Name = "ChbaseNameTB";
            this.ChbaseNameTB.Size = new System.Drawing.Size(165, 20);
            this.ChbaseNameTB.TabIndex = 5;
            this.ChbaseNameTB.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxString_Validating);
            // 
            // ChbaseDescriptionTB
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.ChbaseDescriptionTB, 3);
            this.ChbaseDescriptionTB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ChbaseDescriptionTB.Location = new System.Drawing.Point(70, 33);
            this.ChbaseDescriptionTB.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.ChbaseDescriptionTB.Name = "ChbaseDescriptionTB";
            this.ChbaseDescriptionTB.Size = new System.Drawing.Size(398, 20);
            this.ChbaseDescriptionTB.TabIndex = 6;
            this.ChbaseDescriptionTB.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxString_Validating);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(233, 6);
            this.label1.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "Название:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(0, 36);
            this.label3.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(70, 19);
            this.label3.TabIndex = 2;
            this.label3.Text = "Описание:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // ChbaseIdTB
            // 
            this.ChbaseIdTB.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ChbaseIdTB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ChbaseIdTB.Location = new System.Drawing.Point(70, 3);
            this.ChbaseIdTB.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.ChbaseIdTB.Name = "ChbaseIdTB";
            this.ChbaseIdTB.Size = new System.Drawing.Size(163, 20);
            this.ChbaseIdTB.TabIndex = 4;
            this.ChbaseIdTB.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBoxHex_KeyPress);
            this.ChbaseIdTB.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxHexToInt_Validating);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(0, 6);
            this.label2.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(70, 18);
            this.label2.TabIndex = 1;
            this.label2.Text = "ID: $";
            this.label2.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // DriverGB
            // 
            this.DriverGB.Controls.Add(this.tableLayoutPanel2);
            this.DriverGB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DriverGB.Location = new System.Drawing.Point(5, 155);
            this.DriverGB.Margin = new System.Windows.Forms.Padding(5);
            this.DriverGB.Name = "DriverGB";
            this.DriverGB.Size = new System.Drawing.Size(474, 160);
            this.DriverGB.TabIndex = 1;
            this.DriverGB.TabStop = false;
            this.DriverGB.Text = "Подтип (поддрайвер)";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 4;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tableLayoutPanel2.Controls.Add(this.SubtypeTimeoutTB, 3, 4);
            this.tableLayoutPanel2.Controls.Add(this.SubtypeProtoTB, 3, 3);
            this.tableLayoutPanel2.Controls.Add(this.label5, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.SubtypeIdTB, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.label6, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.SubtypePortTB, 1, 4);
            this.tableLayoutPanel2.Controls.Add(this.SubtypeNameTB, 3, 0);
            this.tableLayoutPanel2.Controls.Add(this.label7, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.SubtypeDescriptionTB, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.label10, 0, 4);
            this.tableLayoutPanel2.Controls.Add(this.label8, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.label4, 2, 3);
            this.tableLayoutPanel2.Controls.Add(this.label11, 2, 4);
            this.tableLayoutPanel2.Controls.Add(this.SubtypeIpTB, 1, 3);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 5;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 15F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(468, 141);
            this.tableLayoutPanel2.TabIndex = 15;
            // 
            // SubtypeTimeoutTB
            // 
            this.SubtypeTimeoutTB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SubtypeTimeoutTB.Location = new System.Drawing.Point(303, 108);
            this.SubtypeTimeoutTB.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.SubtypeTimeoutTB.Name = "SubtypeTimeoutTB";
            this.SubtypeTimeoutTB.Size = new System.Drawing.Size(165, 20);
            this.SubtypeTimeoutTB.TabIndex = 12;
            this.SubtypeTimeoutTB.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBoxNumber_KeyPress);
            this.SubtypeTimeoutTB.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxInt_Validating);
            // 
            // SubtypeProtoTB
            // 
            this.SubtypeProtoTB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SubtypeProtoTB.Location = new System.Drawing.Point(303, 78);
            this.SubtypeProtoTB.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.SubtypeProtoTB.Name = "SubtypeProtoTB";
            this.SubtypeProtoTB.Size = new System.Drawing.Size(165, 20);
            this.SubtypeProtoTB.TabIndex = 13;
            this.SubtypeProtoTB.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxString_Validating);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(0, 6);
            this.label5.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(70, 18);
            this.label5.TabIndex = 0;
            this.label5.Text = "ID: $";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // SubtypeIdTB
            // 
            this.SubtypeIdTB.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.SubtypeIdTB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SubtypeIdTB.Location = new System.Drawing.Point(70, 3);
            this.SubtypeIdTB.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.SubtypeIdTB.Name = "SubtypeIdTB";
            this.SubtypeIdTB.Size = new System.Drawing.Size(163, 20);
            this.SubtypeIdTB.TabIndex = 7;
            this.SubtypeIdTB.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBoxHex_KeyPress);
            this.SubtypeIdTB.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxHexToInt_Validating);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Location = new System.Drawing.Point(233, 6);
            this.label6.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(70, 18);
            this.label6.TabIndex = 1;
            this.label6.Text = "Название:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // SubtypePortTB
            // 
            this.SubtypePortTB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SubtypePortTB.Location = new System.Drawing.Point(70, 108);
            this.SubtypePortTB.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.SubtypePortTB.Name = "SubtypePortTB";
            this.SubtypePortTB.Size = new System.Drawing.Size(163, 20);
            this.SubtypePortTB.TabIndex = 11;
            this.SubtypePortTB.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBoxNumber_KeyPress);
            this.SubtypePortTB.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxInt_Validating);
            // 
            // SubtypeNameTB
            // 
            this.SubtypeNameTB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SubtypeNameTB.Location = new System.Drawing.Point(303, 3);
            this.SubtypeNameTB.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.SubtypeNameTB.Name = "SubtypeNameTB";
            this.SubtypeNameTB.Size = new System.Drawing.Size(165, 20);
            this.SubtypeNameTB.TabIndex = 8;
            this.SubtypeNameTB.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxString_Validating);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label7.Location = new System.Drawing.Point(0, 36);
            this.label7.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(70, 18);
            this.label7.TabIndex = 2;
            this.label7.Text = "Описание:";
            this.label7.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // SubtypeDescriptionTB
            // 
            this.tableLayoutPanel2.SetColumnSpan(this.SubtypeDescriptionTB, 3);
            this.SubtypeDescriptionTB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SubtypeDescriptionTB.Location = new System.Drawing.Point(70, 33);
            this.SubtypeDescriptionTB.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.SubtypeDescriptionTB.Name = "SubtypeDescriptionTB";
            this.SubtypeDescriptionTB.Size = new System.Drawing.Size(398, 20);
            this.SubtypeDescriptionTB.TabIndex = 9;
            this.SubtypeDescriptionTB.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxString_Validating);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label10.Location = new System.Drawing.Point(0, 111);
            this.label10.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(70, 24);
            this.label10.TabIndex = 5;
            this.label10.Text = "Port:";
            this.label10.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label8.Location = new System.Drawing.Point(0, 81);
            this.label8.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(70, 18);
            this.label8.TabIndex = 3;
            this.label8.Text = "IP:";
            this.label8.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(233, 81);
            this.label4.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(70, 18);
            this.label4.TabIndex = 14;
            this.label4.Text = "Proto:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label11.Location = new System.Drawing.Point(233, 111);
            this.label11.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(70, 24);
            this.label11.TabIndex = 6;
            this.label11.Text = "Timeout:";
            this.label11.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // SubtypeIpTB
            // 
            this.SubtypeIpTB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SubtypeIpTB.Location = new System.Drawing.Point(70, 78);
            this.SubtypeIpTB.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.SubtypeIpTB.Name = "SubtypeIpTB";
            this.SubtypeIpTB.Size = new System.Drawing.Size(163, 20);
            this.SubtypeIpTB.TabIndex = 15;
            this.SubtypeIpTB.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxIP_Validating);
            // 
            // ExportBttn
            // 
            this.ExportBttn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ExportBttn.Location = new System.Drawing.Point(243, 3);
            this.ExportBttn.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.ExportBttn.Name = "ExportBttn";
            this.ExportBttn.Size = new System.Drawing.Size(110, 29);
            this.ExportBttn.TabIndex = 2;
            this.ExportBttn.Text = "Экспортировать";
            this.ExportBttn.UseVisualStyleBackColor = true;
            this.ExportBttn.Click += new System.EventHandler(this.ExportBttn_Click);
            // 
            // CancelBttn
            // 
            this.CancelBttn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CancelBttn.Location = new System.Drawing.Point(363, 3);
            this.CancelBttn.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.CancelBttn.Name = "CancelBttn";
            this.CancelBttn.Size = new System.Drawing.Size(110, 29);
            this.CancelBttn.TabIndex = 3;
            this.CancelBttn.Text = "Отменить";
            this.CancelBttn.UseVisualStyleBackColor = true;
            this.CancelBttn.Click += new System.EventHandler(this.CancelBttn_Click);
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Controls.Add(this.DriverGB, 0, 2);
            this.tableLayoutPanel3.Controls.Add(this.ChbaseGB, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.tableLayoutPanel4, 0, 3);
            this.tableLayoutPanel3.Controls.Add(this.tableLayoutPanel5, 0, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 4;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 170F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(484, 361);
            this.tableLayoutPanel3.TabIndex = 4;
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 3;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tableLayoutPanel4.Controls.Add(this.ExportBttn, 1, 0);
            this.tableLayoutPanel4.Controls.Add(this.CancelBttn, 2, 0);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(3, 323);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 1;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(478, 35);
            this.tableLayoutPanel4.TabIndex = 2;
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.ColumnCount = 2;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 350F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this.tableLayoutPanel5.Controls.Add(this.CsvFileTB, 0, 1);
            this.tableLayoutPanel5.Controls.Add(this.ReviewCsvPathBttn, 1, 1);
            this.tableLayoutPanel5.Controls.Add(this.label9, 0, 0);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 2;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(478, 54);
            this.tableLayoutPanel5.TabIndex = 3;
            // 
            // CsvFileTB
            // 
            this.CsvFileTB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CsvFileTB.Location = new System.Drawing.Point(3, 28);
            this.CsvFileTB.Name = "CsvFileTB";
            this.CsvFileTB.Size = new System.Drawing.Size(344, 20);
            this.CsvFileTB.TabIndex = 0;
            this.CsvFileTB.Validating += new System.ComponentModel.CancelEventHandler(this.TextBoxString_Validating);
            // 
            // ReviewCsvPathBttn
            // 
            this.ReviewCsvPathBttn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ReviewCsvPathBttn.Location = new System.Drawing.Point(353, 28);
            this.ReviewCsvPathBttn.Name = "ReviewCsvPathBttn";
            this.ReviewCsvPathBttn.Size = new System.Drawing.Size(122, 23);
            this.ReviewCsvPathBttn.TabIndex = 1;
            this.ReviewCsvPathBttn.Text = "Обзор";
            this.ReviewCsvPathBttn.UseVisualStyleBackColor = true;
            this.ReviewCsvPathBttn.Click += new System.EventHandler(this.ReviewCsvPathBttn_Click);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label9.Location = new System.Drawing.Point(0, 6);
            this.label9.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(350, 13);
            this.label9.TabIndex = 2;
            this.label9.Text = "CSV-файл описания проекта:";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ModbusXmlDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 361);
            this.Controls.Add(this.tableLayoutPanel3);
            this.MaximumSize = new System.Drawing.Size(500, 400);
            this.MinimumSize = new System.Drawing.Size(500, 400);
            this.Name = "ModbusXmlDialog";
            this.ShowIcon = false;
            this.Text = "Экспорт XML из CSV";
            this.ChbaseGB.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.DriverGB.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel5.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox ChbaseGB;
        private System.Windows.Forms.GroupBox DriverGB;
        private System.Windows.Forms.TextBox ChbaseDescriptionTB;
        private System.Windows.Forms.TextBox ChbaseNameTB;
        private System.Windows.Forms.TextBox ChbaseIdTB;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox SubtypeTimeoutTB;
        private System.Windows.Forms.TextBox SubtypePortTB;
        private System.Windows.Forms.TextBox SubtypeDescriptionTB;
        private System.Windows.Forms.TextBox SubtypeNameTB;
        private System.Windows.Forms.TextBox SubtypeIdTB;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox SubtypeProtoTB;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button ExportBttn;
        private System.Windows.Forms.Button CancelBttn;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.TextBox SubtypeIpTB;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.TextBox CsvFileTB;
        private System.Windows.Forms.Button ReviewCsvPathBttn;
        private System.Windows.Forms.Label label9;
    }
}