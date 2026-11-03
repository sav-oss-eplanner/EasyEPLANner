using Eplan.EplApi.ApplicationFramework;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace EasyEPlanner
{
    /// <summary>
    /// Действие для открытия средства обновления надстройки
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class OpenUpdater : IEplAction
    {
        ~OpenUpdater() { }

        public bool Execute(ActionCallingContext oActionCallingContext)
        {
            var path = new FileInfo(AddInModule.OriginalAssemblyPath).Directory.FullName;
            if (File.Exists(path + AddInModule.LauncherPath))
            {
                Process.Start(path + AddInModule.LauncherPath, $"{AddInModule.RunUpdaterFromMenu} {Process.GetCurrentProcess().Id}");
                return true;
            }
            
            if (MessageBox.Show($"Средство обновления {AddInModule.LauncherPath} не найдено!\nОткрыть страницу приложения?",
                "Eplanner", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo("https://github.com/sav-oss-eplanner/EasyEPLANnerUpdater")
                    { UseShellExecute = true });
            }

            return false;
        }

        public bool OnRegister(ref string Name, ref int Ordinal)
        {
            Name = nameof(OpenUpdater);

            return true;
        }

        public void GetActionProperties(ref ActionProperties actionProperties) { }
    }
}
