using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TechObject;

namespace Editor
{
    /// <summary>
    /// Диалоговое окно для создания шага в состояниях
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class StatesCreatorDialog : Form, IInsertDialog<State.StateType, Mode>
    {
        public StatesCreatorDialog(Control parent)
        {
            InitializeComponent();

            ParentFrom = parent;
        }

        public Control ParentFrom { get; private set; }

        public State.StateType Result { get; private set; }

        public DialogResult ShowDialog(Mode mode)
        {
            var list = State.GetOrderedStates()
                .Select(state => new ListViewItem(state.Name(), state.ToString()) 
                { 
                    Tag = state,
                    ForeColor = mode[(int)state].Empty ? Color.DarkGray : Color.Black
                })
                .ToArray();
            StatesListView.Items.AddRange(list);

            return ShowDialog(ParentFrom);
        }
         
        private void StatesListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            SetResult();
        }

        private void StatesListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Enter)
                SetResult();
        }

        private void CreateBttn_Click(object sender, EventArgs e)
        {
            SetResult();
        }

        /// <summary>
        /// Установить результат диалога и закрыть окно
        /// </summary>
        private void SetResult()
        {
            if (StatesListView.SelectedItems.Count > 0 &&
                StatesListView.SelectedItems[0]?.Tag is State.StateType state)
            {
                Result = state;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void CancelBttn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
