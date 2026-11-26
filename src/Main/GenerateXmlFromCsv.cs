using EasyEPlanner.FileSavers.ModbusXML;
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.DataModel;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EasyEPlanner
{
    [ExcludeFromCodeCoverage]
    public class GenerateXmlFromCsv : IEplAction
    {
        ~GenerateXmlFromCsv() { }

        /// <summary>
        ///This function is called when executing the action.
        /// </summary>
        ///<returns>true, if the action performed successfully</returns>
        public bool Execute(ActionCallingContext oActionCallingContext)
        {
            try
            {
                var context = new ModbusChbaseModelView();
                var dialog = new ModbusXmlDialog(context)
                {
                    StartPosition = FormStartPosition.CenterScreen,
                };

                if (dialog.ShowDialog() != DialogResult.OK)
                    return true;


                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Сохранить базу каналов",
                    Filter = "*.cdbx|*.cdbx",
                    FileName = $"{context.Driver.Name}",
                    DefaultExt = "cdbx",
                };

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var saver = new ModbusXmlSaver();
                    saver.Save(saveFileDialog.FileName, context);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return true;
        }

        /// <summary>
        ///This function is called by the application framework,
        ///when registering the add-in.
        /// </summary>
        /// <param name="Name">The action is registered in EPLAN 
        /// under this name.</param>
        /// <param name="Ordinal">The action is registered with 
        /// this overload priority.</param>
        ///<returns>true, if OnRegister succeeds</returns>
        public bool OnRegister(ref string Name, ref int Ordinal)
        {
            Name = nameof(GenerateXmlFromCsv);
            Ordinal = 30;

            return true;
        }

        /// <summary>
        /// Documentation function for the action.
        /// </summary>
        /// <param name="actionProperties"> This object needs to be filled 
        /// with information about the action.</param>
        public void GetActionProperties(ref ActionProperties actionProperties)
        {
        }
    }
}
