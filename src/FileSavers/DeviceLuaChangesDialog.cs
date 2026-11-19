using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EasyEPlanner
{
    /// <summary>
    /// Диалог отличий устройств main.io.lua от функций на ФСА.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class DeviceLuaChangesDialog : Form
    {
        public DeviceLuaChangesDialog(IReadOnlyList<DeviceLuaChange> changes)
        {
            Text = "Изменения устройств (main.io.lua)";
            Width = 780;
            Height = 520;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowIcon = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;

            var info = new Label
            {
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(8),
                Text = "В main.io.lua найдены отличия от функций на ФСА.\n" +
                    "Если принять изменения, они будут записаны в функции устройств на ФСА.",
            };

            var textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font(FontFamily.GenericMonospace, 9f),
                Text = FormatChanges(changes),
            };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8),
            };

            var applyButton = new Button
            {
                Text = "Применить",
                DialogResult = DialogResult.OK,
                Width = 110,
                Height = 28,
            };
            var skipButton = new Button
            {
                Text = "Пропустить",
                DialogResult = DialogResult.Cancel,
                Width = 110,
                Height = 28,
            };

            buttons.Controls.Add(applyButton);
            buttons.Controls.Add(skipButton);
            AcceptButton = applyButton;
            CancelButton = skipButton;

            Controls.Add(textBox);
            Controls.Add(buttons);
            Controls.Add(info);
        }

        private static string FormatChanges(IReadOnlyList<DeviceLuaChange> changes)
        {
            if (changes is null || changes.Count == 0)
                return "Изменений нет.";

            var builder = new StringBuilder();
            foreach (var group in changes.GroupBy(c => c.DeviceName))
            {
                builder.AppendLine(group.Key);
                foreach (var change in group)
                {
                    builder.Append("  ");
                    builder.Append(change.Caption);
                    builder.Append(": \"");
                    builder.Append(change.OldValue);
                    builder.Append("\" → \"");
                    builder.Append(change.NewValue);
                    builder.AppendLine("\"");
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}
