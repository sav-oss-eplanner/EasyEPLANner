using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace EasyEPlanner
{
    //-------------------------------------------------------------------------
    //-------------------------------------------------------------------------
    /// <summary>
    /// Интерфейс окна для вывода сообщений о ходе обработки.
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// Пусто ли окно логов
        /// </summary>
        /// <returns></returns>
        bool IsEmpty();

        /// <summary>
        /// Показать окно.
        /// </summary>
        void ShowLog();

        /// <summary>
        /// Скрыть окно.
        /// </summary>
        void HideLog();

        /// <summary>
        /// Очистка окна.
        /// </summary>
        void Clear();

        bool Active();

        /// <summary>
        /// Сделать доступным кнопку ОК.
        /// </summary>
        void EnableButtons();

        /// <summary>
        /// Сделать не доступным кнопку ОК.
        /// </summary>
        void DisableButtons();

        /// <summary>
        /// Добавить сообщение.
        /// </summary>
        /// <param name="msg">Сообщение.</param>
        void AddMessage(string msg);

        /// <summary>
        /// Прокрутка до последней строки.
        /// </summary>
        void ShowLastLine();

        /// <summary>
        /// Установить ход выполнения операции.
        /// </summary>
        /// <param name="val">Процент выполнения операции.</param>
        void SetProgress(int val);

        /// <summary>
        /// Является ли окно null
        /// </summary>
        /// <returns></returns>
        bool IsNull();
    }
    //-------------------------------------------------------------------------
    //-------------------------------------------------------------------------
    /// <summary>
    /// Окно для вывода сообщений о ходе обработки.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class LogFrm : Form, ILog
    {
        void ThreadProc(object vindowWrapper)
        {
            try
            {
                ShowDialog(vindowWrapper as WindowWrapper);
            }
            catch (Exception)
            {
            }
        }

        private bool isExist = false;

        /// <summary>
        /// Показать окно.
        /// </summary>
        public void ShowLog()
        {
            if (isExist == false)
            {
                System.Diagnostics.Process oCurrent = System.Diagnostics.Process.GetCurrentProcess();
                WindowWrapper vindowWrapper = new WindowWrapper(oCurrent.MainWindowHandle);

                System.Threading.Thread t = new System.Threading.Thread(
                    new System.Threading.ParameterizedThreadStart(ThreadProc));
                t.Start(vindowWrapper);
                isExist = true;
            }
        }

        public void HideLog()
        {
            this.Close();
            isExist = false;
            this.Clear();
        }

        public bool IsEmpty()
        {
            if (richTextBox.Text == "") return true;

            return false;
        }

        public bool IsNull()
        {
            return this == null;
        }

        public void Clear()
        {
            synchronizationContext.Send(new System.Threading.SendOrPostCallback(
                delegate (object state)
                {
                    richTextBox.Text = "";
                }
            ), null);
        }

        public bool Active()
        {
            return Visible;
        }

        private System.Threading.SynchronizationContext synchronizationContext;

        public LogFrm()
        {
            synchronizationContext = System.Threading.SynchronizationContext.Current;

            InitializeComponent();
        }

        public void ShowLastLine()
        {
            synchronizationContext.Send(new System.Threading.SendOrPostCallback(
                delegate (object state)
                {
                    richTextBox.ScrollToCaret();
                }
            ), null);
        }

        public void AddMessage(string msg)
        {
            synchronizationContext.Send(new System.Threading.SendOrPostCallback(
                delegate (object state)
                {
                    //Удаление последнего символа возврата строки.
                    if (msg.Length > 0)
                    {
                        msg = msg.Remove(msg.Length - 1);
                    }

                    if (!richTextBox.Lines.Contains(msg))
                    {
                        richTextBox.AppendText(msg + "\n");
                    }
                }
            ), null);
        }

        public void DisableButtons()
        {
            synchronizationContext.Send(new System.Threading.SendOrPostCallback(
                delegate (object state)
                {
                    OkButton.Enabled = false;
                    ExportButton.Enabled = false;
                }
            ), null);
        }

        public void EnableButtons()
        {
            synchronizationContext.Send(new System.Threading.SendOrPostCallback(
                delegate (object state)
                {
                    OkButton.Enabled = true;
                    ExportButton.Enabled = true;
                }
            ), null);
        }

        public void SetProgress(int val)
        {
            synchronizationContext.Send(new System.Threading.SendOrPostCallback(
                delegate (object state)
                {
                    progressBar.Value = val;
                }
            ), null);

        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            this.Close();
            isExist = false;
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            synchronizationContext.Post(new SendOrPostCallback(
                delegate (object state)
                {
                    var saveFileDialog = new SaveFileDialog()
                    {
                        Filter = "Text files (*.txt)|*.txt",
                        RestoreDirectory = true,
                        Title = "Выгрузить лог",
                    };


                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        using var streamWriter = new StreamWriter(saveFileDialog.FileName);
                        streamWriter.Write(richTextBox.Text);
                    }
                }
            ), null);

            
        }
    }
}
 