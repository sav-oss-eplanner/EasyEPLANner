using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TechObject;

namespace Editor
{
    public interface IDialogFactory
    {
        /// <summary>
        /// Получитть диалог создания шага состояния
        /// </summary>
        /// <returns></returns>
        IInsertDialog<State.StateType, Mode> GetStatesCreatorDialog();
    }

    public class DialogFactory(Control parent = null) : IDialogFactory
    {
        public IInsertDialog<State.StateType, Mode> GetStatesCreatorDialog()
            => new StatesCreatorDialog(parent);
    }
}
