using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace EasyEPlanner.App
{
    internal static class StandaloneAppState
    {
        private static readonly string settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EasyEPlanner", "Standalone");

        public static string DockLayoutPath =>
            Path.Combine(settingsDirectory, "dock-layout.xml");

        public static void EnsureDockLayoutDirectory()
        {
            EnsureSettingsDirectory();
        }

        private static string LastProjectPath =>
            Path.Combine(settingsDirectory, "last-project.txt");

        private static string WindowBoundsPath =>
            Path.Combine(settingsDirectory, "window-bounds.txt");

        public static string LoadLastProject()
        {
            return File.Exists(LastProjectPath)
                ? File.ReadAllText(LastProjectPath, Encoding.UTF8).Trim()
                : string.Empty;
        }

        public static void SaveLastProject(string projectPath)
        {
            EnsureSettingsDirectory();
            File.WriteAllText(LastProjectPath, projectPath ?? string.Empty,
                Encoding.UTF8);
        }

        public static void ClearLastProject()
        {
            if (File.Exists(LastProjectPath))
                File.Delete(LastProjectPath);
        }

        public static bool TryLoadWindowBounds(out Rectangle bounds,
            out FormWindowState windowState)
        {
            bounds = Rectangle.Empty;
            windowState = FormWindowState.Normal;
            if (!File.Exists(WindowBoundsPath))
                return false;

            string[] values = File.ReadAllText(WindowBoundsPath, Encoding.UTF8)
                .Split('|');
            if (values.Length != 5 ||
                !int.TryParse(values[0], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int x) ||
                !int.TryParse(values[1], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int y) ||
                !int.TryParse(values[2], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int width) ||
                !int.TryParse(values[3], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int height) ||
                width <= 0 || height <= 0 ||
                !int.TryParse(values[4], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int state) ||
                !Enum.IsDefined(typeof(FormWindowState), state))
            {
                return false;
            }

            bounds = new Rectangle(x, y, width, height);
            windowState = (FormWindowState)state;
            if (windowState == FormWindowState.Minimized)
                windowState = FormWindowState.Normal;
            return true;
        }

        public static void SaveWindowBounds(Rectangle bounds,
            FormWindowState windowState)
        {
            EnsureSettingsDirectory();
            string value = string.Join("|",
                bounds.X.ToString(CultureInfo.InvariantCulture),
                bounds.Y.ToString(CultureInfo.InvariantCulture),
                bounds.Width.ToString(CultureInfo.InvariantCulture),
                bounds.Height.ToString(CultureInfo.InvariantCulture),
                ((int)windowState).ToString(CultureInfo.InvariantCulture));
            File.WriteAllText(WindowBoundsPath, value, Encoding.UTF8);
        }

        private static void EnsureSettingsDirectory()
        {
            Directory.CreateDirectory(settingsDirectory);
        }
    }
}
