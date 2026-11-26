using StaticHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EasyEPlanner.FileSavers.ModbusXML
{
    [ExcludeFromCodeCoverage]
    public partial class ModbusXmlDialog : Form
    {
        public ModbusXmlDialog(IModbusChbaseViewModel context)
        {
            InitializeComponent();

            ChbaseNameTB.Text = context.Driver.Name;
            ChbaseIdTB.Text = $"{context.Driver.ID:X}";
            ChbaseDescriptionTB.Text = context.Driver.Description;

            SubtypeNameTB.Text = context.Subtype.Name;
            SubtypeIdTB.Text = $"{context.Subtype.ID:X}";
            SubtypeDescriptionTB.Text = context.Subtype.Description;

            SubtypeIpTB.Text = context.Subtype.IP;
            SubtypePortTB.Text = context.Subtype.Port.ToString();
            SubtypeProtoTB.Text = context.Subtype.Proto;
            SubtypeTimeoutTB.Text = context.Subtype.TimeOut.ToString();


            CsvFileTB.Tag = new TagReference<string>(() => context.CsvFile, csv => context.CsvFile = csv);

            ChbaseNameTB.Tag = new TagReference<string>(() => context.Driver.Name, name => context.Driver.Name = name);
            ChbaseIdTB.Tag = new TagReference<int>(() => context.Driver.ID, id => context.Driver.ID = id);
            ChbaseDescriptionTB.Tag = new TagReference<string>(() => context.Driver.Description, descr => context.Driver.Description = descr);

            SubtypeNameTB.Tag = new TagReference<string>(() => context.Subtype.Name, name => context.Subtype.Name = name);
            SubtypeIdTB.Tag = new TagReference<int>(() => context.Subtype.ID, id => context.Subtype.ID = id);
            SubtypeDescriptionTB.Tag = new TagReference<string>(() => context.Subtype.Description, descr => context.Subtype.Description = descr);

            SubtypeIpTB.Tag = new TagReference<string>(() => context.Subtype.IP, ip => context.Subtype.IP = ip);
            SubtypePortTB.Tag = new TagReference<int>(() => context.Subtype.Port, port => context.Subtype.Port = port);
            SubtypeProtoTB.Tag = new TagReference<string>(() => context.Subtype.Proto, proto => context.Subtype.Proto = proto);
            SubtypeTimeoutTB.Tag = new TagReference<int>(() => context.Subtype.TimeOut, timeout => context.Subtype.TimeOut = timeout);
        }

        private void ExportBttn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelBttn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void TextBoxHex_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (((e.KeyChar < '0' || e.KeyChar > '9') &&
                (e.KeyChar < 'A' || e.KeyChar > 'F') &&
                (e.KeyChar < 'a' || e.KeyChar > 'f') ||
                (sender as TextBox).Text.Length >= 2) && e.KeyChar != 8)
            {
                e.Handled = true;
            }
        }

        private void TextBoxNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar < '0' || e.KeyChar > '9') && e.KeyChar != 8)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Валидация IP-адреса
        /// </summary>
        private void TextBoxIP_Validating(object sender, CancelEventArgs e)
        {
            var textBox = sender as TextBox;
            
            if (Regex.IsMatch(textBox.Text, CommonConst.IPAddressPattern, RegexOptions.None, TimeSpan.FromMilliseconds(100)))
            {
                (textBox.Tag as TagReference<string>).Value = textBox.Text;
            }
            else
            {
                textBox.Text = (textBox.Tag as TagReference<string>).Value;
            }
        }

        /// <summary>
        /// Валидация 16-ричного числа
        /// </summary>
        private void TextBoxHexToInt_Validating(object sender, CancelEventArgs e)
        {
            var textBox = sender as TextBox;

            try
            {
                (textBox.Tag as TagReference<int>).Value = Convert.ToInt32(textBox.Text, 16);
            }
            catch
            {
                textBox.Text = $"{(textBox.Tag as TagReference<int>).Value:X}";
            }
        }

        /// <summary>
        /// Валидация строк
        /// </summary>
        private void TextBoxString_Validating(object sender, CancelEventArgs e)
        {
            var textBox = sender as TextBox;

            (textBox.Tag as TagReference<string>).Value = textBox.Text;
        }

        /// <summary>
        /// Валидация номера
        /// </summary>
        private void TextBoxInt_Validating(object sender, CancelEventArgs e)
        {
            var textBox = sender as TextBox;

            if (int.TryParse(textBox.Text, out var number))
            {
                (textBox.Tag as TagReference<int>).Value = number;
            }
            else
            {
                textBox.Text = (textBox.Tag as TagReference<int>).Value.ToString();
            }
        }

        /// <summary>
        /// Обзор
        /// </summary>
        private void ReviewCsvPathBttn_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выбрать CSV-файл описания проекта",
                Filter = $"CSV-файлы|*.csv",
                Multiselect = false,
            };
            DialogResult openFileResult = openFileDialog.ShowDialog();

            if (openFileResult == DialogResult.OK)
            {
                CsvFileTB.Text = openFileDialog.FileName;

                // call validating event
                TextBoxString_Validating(CsvFileTB, null);
            }
        }
    }
}
