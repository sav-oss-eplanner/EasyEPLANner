using System.Windows.Forms;

namespace Editor
{
    /// <summary>    
    /// Интерфейс редактора.
    /// </summary>
    public interface IEditor
    {
        /// <summary>
        /// Открыть редактор и начать редактирование
        /// </summary>
        /// <param name="objectTree">Дерево объектов</param>
        /// <returns></returns>
        void OpenEditor(ITreeViewItem objectTree,
            Control host = null);

        /// <summary>
        /// Показан ли редактор
        /// </summary>
        /// <returns></returns>
        bool IsShown();

        /// <summary>
        /// Закрыть редактор и прекратить редактирование
        /// </summary>
        void CloseEditor();

        /// <summary>
        /// Обновить редактор
        /// </summary>
        void RefreshEditor();

        /// <summary>
        /// Обновить элементв
        /// </summary>
        void RefreshObject(object item);

        /// <summary>
        /// Диалоговое окно с подтверждением сброса доп.свойств агрегатов в операции
        /// </summary>
        /// <returns>
        /// DialogResult:
        /// yes    - сброс доп.свойств для привязанных агрегатов
        /// no     - не сбрасывать доп.свойства
        /// cancel - отменить изменение базовой операции.
        /// </returns>
        DialogResult DialogResetExtraProperties();

        /// <summary>
        /// Диалоговое окно для удаления типовой группы
        /// </summary>
        /// <returns>
        /// /// DialogResult:
        /// yes    - Удалить также все тех. объекты в группе
        /// no     - Перенести все тех. объекты к базовому объекту и
        ///          удалить только группу с типовым объектом
        /// </returns>
        DialogResult DialogDeletingGenericGroup();

        /// <summary>
        /// Режим редактирования
        /// </summary>
        bool Editable { get; }
    }
}
