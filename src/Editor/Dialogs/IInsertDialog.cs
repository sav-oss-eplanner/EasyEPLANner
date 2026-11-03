using System.Windows.Forms;

namespace Editor
{
    /// <summary>
    /// Диалог создания элемента
    /// </summary>
    /// <typeparam name="TRslt">Результат</typeparam>
    /// <typeparam name="TArg">Аргумент передоваемый в диалог</typeparam>
    public interface IInsertDialog<out TRslt, in TArg>
    {
        /// <summary>
        /// Отобразить диалог
        /// </summary>
        DialogResult ShowDialog(TArg argument);

        /// <summary>
        /// Результат диалога
        /// </summary>
        TRslt Result { get; }
    }
}
