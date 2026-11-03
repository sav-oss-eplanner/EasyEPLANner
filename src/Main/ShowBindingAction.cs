using EasyEPlanner.Binding.View;
using Eplan.EplApi.ApplicationFramework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace EasyEPlanner.Main
{
    [ExcludeFromCodeCoverage]
    public class ShowBindingAction : IEplAction
    {
        public bool Execute(ActionCallingContext oActionCallingContext)
        {
            try
            {
                if (EProjectManager.GetInstance().GetCurrentPrj() is null)
                {
                    MessageBox.Show("Нет открытого проекта!", "EPlaner",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                else
                {
                    BindingViewControl.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return true;
        }

        public bool OnRegister(ref string Name, ref int Ordinal)
        {
            Name = nameof(ShowBindingAction);
            Ordinal = 23;
            return true;
        }

        public void GetActionProperties(ref ActionProperties actionProperties)
        {
        }
    }
}
