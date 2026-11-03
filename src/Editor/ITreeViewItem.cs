using Aga.Controls.Tree;
using BrightIdeasSoftware;
using System.Collections.Generic;

namespace Editor
{
    /// <summary>    
    /// Интерфейс редактируемого объекта.
    /// </summary>
    public interface ITreeViewItem
    {
        /// <summary>
        /// Получаем индекс той части или колонки, которую можем редактировать.
        /// -1 - не можем редактировать, 1 - можем. Индекс массива - левая или
        /// права колонка (0 - левая, 1 - правая).
        /// </summary>
        int[] EditablePart { get; }

        /// <summary>    
        /// Получение имени, которое отображается на дереве.
        /// </summary>
        string[] DisplayText { get; }

        /// <summary>    
        /// Получение текста, который отображается на дереве при редактирования.
        /// </summary>
        string[] EditText { get; }

        /// <summary>    
        /// Признак возможности удаления.
        /// </summary>
        bool IsDeletable { get; }

        /// <summary>    
        /// Удаление узла. Delete
        /// </summary>
        /// <param name="child">Удаляемый объект.</param>
        /// <returns>Удалять ли объект из дерева.</returns>
        bool Delete(object child);

        /// <summary>
        /// Признак возможности перемещения.
        /// </summary>
        bool IsMoveable { get; }

        /// <summary>
        /// Возможность перемещать дочерний элемент вверх
        /// </summary>
        bool CanMoveUp(object child);

        /// <summary>
        /// Возможность перемещать дочерний элемент вниз
        /// </summary>
        bool CanMoveDown(object child);

        /// <summary>    
        /// Перемещение узла вниз. Shift + Down
        /// </summary>
        /// <param name="child">Перемещаемый объект.</param>
        /// <returns>Перемещенный вниз объект.</returns>
        ITreeViewItem MoveDown(object child);

        /// <summary>    
        /// Перемещение узла вверх. Shift + Up
        /// </summary>
        /// <param name="child">Перемещаемый объект.</param>
        /// <returns>Перемещенный вверх объект.</returns>
        ITreeViewItem MoveUp(object child);

        /// <summary>    
        /// Признак возможности замены.
        /// </summary>
        bool IsReplaceable { get; }

        /// <summary>    
        /// Замена активного узла скопированным. Ctrl + B
        /// </summary>
        /// <param name="child">Заменяемый объект.</param>
        /// <param name="copyObject">Ранее скопированный объект.</param>
        /// <returns>Новый вставленный объект.</returns> 
        ITreeViewItem Replace(object child, object copyObject);

        /// <summary>    
        /// Признак возможности копирования.
        /// </summary>
        bool IsCopyable { get; }

        /// <summary>    
        /// Копирование объекта. Ctrl + C
        /// </summary>
        /// <returns>Копия объекта для последующей вставки.</returns> 
        object Copy();

        /// <summary>    
        /// Признак возможности вставки (ранее скопированного объекта).
        /// </summary>
        bool IsInsertableCopy { get; }

        /// <summary>    
        /// Вставка ранее скопированного объекта. Ctrl + V
        /// </summary>
        ITreeViewItem InsertCopy(object obj); 

        /// <summary>    
        /// Получение составляющих объектов.
        /// </summary>
        ITreeViewItem[] Items { get; }

        /// <summary>    
        /// Признак возможности редактирования.
        /// </summary>
        bool IsEditable { get; }

        /// <summary>    
        /// Установка нового значения после редактирования.
        /// </summary>
        ///  <param name="newValue">Новое значение после редактирования.</param>
        bool SetNewValue(string newValue);

        /// <summary>
        /// Установка нового значения для ограничений
        /// </summary>
        /// <param name="newDict">Словарь ограничений</param>
        /// <returns></returns>
        bool SetNewValue(IDictionary<int, List<int>> newDict);

        /// <summary>
        /// Установка нового значения после редактирования
        /// </summary>
        /// <param name="newValue">Новое значение</param>
        /// <param name="isExtraValue">Является ли свойство расширенным</param>
        /// <returns></returns>
        bool SetNewValue(string newValue, bool isExtraValue);

        /// <summary>
        /// Установка нового значения после редактированию в указанную колонку
        /// </summary>
        /// <param name="newValue">Новое значение</param>
        /// <param name="column">Индекс колонки</param>
        bool SetNewValue(string newValue, int column);

        /// <summary>    
        /// Признак возможности добавления.
        /// </summary>
        bool IsInsertable { get; }

        /// <summary>    
        /// Добавление элемента. Insert
        /// </summary>
        /// <returns>Добавленный элемент. Может быть null, тогда ничего не 
        /// вставляется.</returns>
        ITreeViewItem Insert();

        /// <summary>
        /// Добавление элемента с вызовом диалогового окна. Insert 
        /// </summary>
        /// <returns>
        /// Добавленный элемент. Может быть null, тогда ничего не вставляется.
        /// </returns>
        ITreeViewItem Insert(IDialogFactory dialogFactory);

        /// <summary>    
        /// Признак возможности добавления устройств через список устройств.
        /// </summary>
        bool IsUseDevList { get; }

        /// <summary>    
        /// Признак возможности добавления ограничений в текущем объекте.
        /// </summary>
        bool IsLocalRestrictionUse { get; }

        /// <summary>    
        /// Отображение на странице Eplan'a.
        /// </summary>
        bool IsDrawOnEplanPage { get; }

        /// <summary>    
        /// Получение списка устройств для отображения на странице Eplan'a.
        /// </summary>
        List<DrawInfo> GetObjectToDrawOnEplanPage();

        /// <summary>    
        /// Получение типов и подтипов устройств, которые должны отображаться 
        /// в списке устройств при редактировании, а также значения флага,
        /// указывающего на необходимость отображения в списке устройств также
        /// и параметров объекта, в котором редактируется элемент.
        /// </summary>
        /// <param name="devTypes">Типы устройств, допустимые для 
        /// редактирования.</param>
        /// <param name="devSubTypes">Подтипы устройств, допустимые для 
        /// редактирования.</param>
        /// <param name="displayParameters">Отобразить параметры объекта
        /// </param>
        void GetDisplayObjects(out EplanDevice.DeviceType[] devTypes,
            out EplanDevice.DeviceSubType[] devSubTypes, out bool displayParameters);

        /// <summary>    
        /// Признак необходимости обновления отображения родительского узла.
        /// </summary>
        bool NeedRebuildParent { get; }

        /// <summary>
        /// Получение и установка родительского элемента.
        /// </summary>
        ITreeViewItem Parent { get; set; }

        /// <summary>
        /// Является ли элемент булевым параметром.
        /// </summary>
        bool IsBoolParameter { get; }

        /// <summary>
        /// Является ли этот объект технологическим объектом
        /// </summary>
        bool IsMainObject { get; }

        /// <summary>
        /// Является ли этот объект операцией
        /// </summary>
        bool IsMode { get; }

        /// <summary>
        /// Отображать предупреждение перед удалением
        /// </summary>
        bool ShowWarningBeforeDelete { get; }

        /// <summary>
        /// Установка родителя для элемента
        /// </summary>
        /// <param name="parent">Родительский элемент</param>
        void AddParent(ITreeViewItem parent);

        /// <summary>
        /// Нужно ли отключить элемент
        /// </summary>
        /// <returns></returns>
        bool NeedDisable { get; set; }

        /// <summary>
        /// Заполнен или нет элемент дерева
        /// True - отображать
        /// False - скрывать
        /// </summary>
        bool IsFilled { get; }

        /// <summary>
        /// Состояние фильтра элемента:
        /// null - фильтрация не проводилась
        /// true/false - результат фильтрации
        /// </summary>
        bool? Filtred { get; }

        /// <summary>
        /// Фильтрация элементов дерева
        /// </summary>
        /// <param name="searchString"> Искомая строка </param>
        /// <param name="hideEmptyItems"> Скрывать пустые элементы </param>
        /// <returns></returns>
        bool Filter(string searchString, bool hideEmptyItems);

        /// <summary>
        /// Сбросить фильтр
        /// </summary>
        void ResetFilter();

        /// <summary>
        /// Содержит ли элемент искомую строку
        /// </summary>
        /// <param name="value">Искомое значение</param>
        bool Contains(string value);

        /// <summary>
        /// Индекс картинки из формы, для вставки в элемент.
        /// </summary>
        ImageIndexEnum ImageIndex { get; set; }

        /// <summary>
        /// Индекс картинки из формы, для вставки в элемент (2я колонка).
        /// </summary>
        ImageIndexEnum DescriptionImageIndex { get; set; }

        /// <summary>
        /// Содержит ли объект базовый объект/операцию/шаг
        /// </summary>
        bool ContainsBaseObject { get; }

        /// <summary>
        /// Список базовых объектов/операций/шагов объекта.
        /// </summary>
        IEnumerable<string> BaseObjectsList { get; }

        /// <summary>
        /// Отключено или нет свойство
        /// </summary>
        bool Disabled { get; set; }

        /// <summary>
        /// Можно ли вырезать объект/из объекта
        /// </summary>
        bool IsCuttable { get; }

        /// <summary>
        /// Вырезать объект
        /// </summary>
        /// <param name="item">Объект</param>
        ITreeViewItem Cut(ITreeViewItem item);

        /// <summary>
        /// Помечен на вырезание
        /// </summary>
        bool MarkToCut { get; set; }

        /// <summary>
        /// Элемент скрыт
        /// </summary>
        bool Visibility { get; set; }

        /// <summary>
        /// Визуализатор для ячеек
        /// </summary>
        IRenderer[] CellRenderer { get; }

        /// <summary>
        /// Быстрое выделение смежных элементов
        /// </summary>
        List<ITreeViewItem> QuickMultiSelect();

        event OnValueChanged ValueChanged;

        void OnValueChanged(object sender);
    }
}