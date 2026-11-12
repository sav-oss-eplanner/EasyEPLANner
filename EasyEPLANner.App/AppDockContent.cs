using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace EasyEPlanner.App
{
    /// <summary>
    /// Документ DockPanelSuite с панелью-хостом для встраиваемых view.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class AppDockContent : DockContent
    {
        private readonly string persistString;

        public AppDockContent(string title, string persistString)
        {
            this.persistString = persistString;
            Text = title;
            TabText = title;
            HideOnClose = true;
            CloseButton = true;
            CloseButtonVisible = true;
            DockAreas = DockAreas.Document | DockAreas.Float |
                DockAreas.DockLeft | DockAreas.DockRight |
                DockAreas.DockTop | DockAreas.DockBottom;

            HostPanel = new Panel { Dock = DockStyle.Fill };
            Controls.Add(HostPanel);
        }

        public Panel HostPanel { get; }

        protected override string GetPersistString() => persistString;

        public void ShowOrActivate(DockPanel dockPanel)
        {
            if (DockPanel == null || IsHidden)
                Show(dockPanel, DockState.Document);
            else
                Activate();
        }
    }
}
