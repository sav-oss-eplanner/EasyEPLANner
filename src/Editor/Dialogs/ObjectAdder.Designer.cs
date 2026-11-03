namespace Editor
{
    partial class ObjectsAdder
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
            this.acceptButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.objectTypes = new System.Windows.Forms.ListBox();
            this.objectSubTypes = new System.Windows.Forms.ListBox();
            this.objectSubTypesLabel = new System.Windows.Forms.Label();
            this.objectTypesLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // acceptButton
            // 
            this.acceptButton.Location = new System.Drawing.Point(303, 233);
            this.acceptButton.Name = "acceptButton";
            this.acceptButton.Size = new System.Drawing.Size(95, 23);
            this.acceptButton.TabIndex = 0;
            this.acceptButton.Text = "Добавить";
            this.acceptButton.UseVisualStyleBackColor = true;
            this.acceptButton.Click += new System.EventHandler(this.acceptButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(202, 233);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(95, 23);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "Отмена";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // objectTypes
            // 
            this.objectTypes.FormattingEnabled = true;
            this.objectTypes.Location = new System.Drawing.Point(12, 28);
            this.objectTypes.Name = "objectTypes";
            this.objectTypes.Size = new System.Drawing.Size(188, 199);
            this.objectTypes.TabIndex = 2;
            this.objectTypes.SelectedValueChanged += new System.EventHandler(this.ObjectTypes_SelectedValueChanged);
            // 
            // objectSubTypes
            // 
            this.objectSubTypes.FormattingEnabled = true;
            this.objectSubTypes.Location = new System.Drawing.Point(210, 28);
            this.objectSubTypes.Name = "objectSubTypes";
            this.objectSubTypes.Size = new System.Drawing.Size(188, 199);
            this.objectSubTypes.Sorted = true;
            this.objectSubTypes.TabIndex = 3;
            // 
            // objectSubTypesLabel
            // 
            this.objectSubTypesLabel.AutoSize = true;
            this.objectSubTypesLabel.Location = new System.Drawing.Point(212, 9);
            this.objectSubTypesLabel.Name = "objectSubTypesLabel";
            this.objectSubTypesLabel.Size = new System.Drawing.Size(143, 13);
            this.objectSubTypesLabel.TabIndex = 4;
            this.objectSubTypesLabel.Text = "Выберите подтип объекта:";
            // 
            // objectTypesLabel
            // 
            this.objectTypesLabel.AutoSize = true;
            this.objectTypesLabel.Location = new System.Drawing.Point(12, 9);
            this.objectTypesLabel.Name = "objectTypesLabel";
            this.objectTypesLabel.Size = new System.Drawing.Size(125, 13);
            this.objectTypesLabel.TabIndex = 5;
            this.objectTypesLabel.Text = "Выберите тип объекта:";
            // 
            // ObjectsAdder
            // 
            this.AcceptButton = this.acceptButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(411, 263);
            this.ControlBox = false;
            this.Controls.Add(this.objectTypesLabel);
            this.Controls.Add(this.objectSubTypesLabel);
            this.Controls.Add(this.objectSubTypes);
            this.Controls.Add(this.objectTypes);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.acceptButton);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(427, 302);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(427, 302);
            this.Name = "ObjectsAdder";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Добавление объекта в редактор";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button acceptButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.ListBox objectTypes;
        private System.Windows.Forms.ListBox objectSubTypes;
        private System.Windows.Forms.Label objectSubTypesLabel;
        private System.Windows.Forms.Label objectTypesLabel;
    }
}