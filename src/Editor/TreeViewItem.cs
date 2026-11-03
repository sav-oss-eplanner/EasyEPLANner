using BrightIdeasSoftware;
using StaticHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Editor
{
    /// <summary>
    /// Редактируемый объект - дерево элемента.
    /// </summary>
    public abstract class TreeViewItem : ITreeViewItem, IHelperItem
    {
        private ITreeViewItem parent;

        #region Реализация ITreeViewItem
        public ITreeViewItem Parent
        {
            get
            {
                return parent;
            }
            set
            {
                parent = value;
            }
        }

        virtual public string[] DisplayText
        {
            get
            {
                return new string[] { "", "" };
            }
        }

        virtual public ITreeViewItem[] Items
        {
            get
            {
                return null;
            }
        }

        virtual public bool SetNewValue(string newName)
        {
            return false;
        }

        virtual public bool SetNewValue(IDictionary<int, List<int>> newDict)
        {
            return false;
        }

        virtual public bool SetNewValue(string newValue, bool isExtraValue)
        {
            return false;
        }

        virtual public bool SetNewValue(string newValue, int column)
        {
            return SetNewValue(newValue);
        }

        virtual public bool IsEditable
        {
            get
            {
                return false;
            }
        }

        virtual public int[] EditablePart
        {
            get
            {
                return new int[] { -1, -1 };
            }
        }

        virtual public string[] EditText
        {
            get
            {
                return new string[] { "", "" };
            }
        }

        virtual public bool IsDeletable
        {
            get
            {
                return false;
            }
        }

        virtual public bool IsCopyable
        {
            get
            {
                return false;
            }
        }

        virtual public object Copy()
        {
            if (IsCopyable)
            {
                return this;
            }
            else
            {
                return null;
            }
        }

        virtual public bool IsMoveable
        {
            get
            {
                return false;
            }
        }

        virtual public bool CanMoveUp(object child)
        => child is ITreeViewItem item
            && item.IsMoveable is true
            && Items.FirstOrDefault() != item;

        virtual public bool CanMoveDown(object child)
        => child is ITreeViewItem item
            && item.IsMoveable is true
            && Items.LastOrDefault() != item;

        virtual public bool IsReplaceable
        {
            get
            {
                return false;
            }
        }

        virtual public ITreeViewItem MoveDown(object child)
        {
            return null;
        }

        virtual public ITreeViewItem MoveUp(object child)
        {
            return null;
        }

        virtual public bool IsInsertableCopy
        {
            get
            {
                return false;
            }
        }

        virtual public ITreeViewItem InsertCopy(object obj)
        {
            return null;
        }

        virtual public ITreeViewItem Replace(object child, object copyObject)
        {
            return null;
        }

        virtual public bool Delete(object child)
        {
            return false;
        }

        virtual public bool IsInsertable
        {
            get
            {
                return false;
            }
        }

        virtual public ITreeViewItem Insert()
        {
            return null;
        }

        virtual public ITreeViewItem Insert(IDialogFactory dialogFactory)                                               
            => Insert();

        virtual public bool IsUseDevList
        {
            get
            {
                return false;
            }
        }

        virtual public bool IsLocalRestrictionUse
        {
            get
            {
                return false;
            }
        }

        virtual public bool IsDrawOnEplanPage
        {
            get { return false; }
        }

        virtual public List<DrawInfo> GetObjectToDrawOnEplanPage()
        {
            return null;
        }

        virtual public void GetDisplayObjects(out EplanDevice.DeviceType[] devTypes,
            out EplanDevice.DeviceSubType[] devSubTypes, out bool displayParameters)
        {
            devTypes = null;
            devSubTypes = null;
            displayParameters = false;
        }

        public virtual bool NeedRebuildParent
        {
            get { return false; }
        }

        public virtual bool IsBoolParameter
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsMainObject
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsMode
        {
            get
            {
                return false;
            }
        }

        public virtual bool ShowWarningBeforeDelete
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Установка родителя для элемента
        /// </summary>
        /// <param name="parent">Родительский элемент</param>
        public void AddParent(ITreeViewItem parent)
        {
            Parent = parent;
            if (Items != null)
            {
                foreach (ITreeViewItem item in Items)
                {
                    item.AddParent(this);
                }
            }
        }

        /// <summary>
        /// Нужно ли отключить элемент
        /// </summary>
        /// <returns></returns>
        public virtual bool NeedDisable
        {
            get
            {
                return needDisable;
            }
            set
            {
                needDisable = value;
            }
        }

        private bool needDisable = false;

        /// <summary>
        /// Заполнен или нет элемент дерева
        /// True - отображать
        /// False - скрывать
        /// </summary>
        public virtual bool IsFilled
        {
            get
            {
                return (EditText[1].Length > 0 && EditText[1] != CommonConst.EmptyValue) ||
                    (Items != null && Array.Exists(Items, item => item.IsFilled));
            }
        }


        public void ResetFilter()
        {
            Filtred = null;
            ThisOrParentsContains = false;
            if (Items != null)
            {
                foreach (var item in Items)
                {
                    item.ResetFilter();
                }
            }
        }

        public bool? Filtred { get; private set; } = null;

        public bool Filter(string searchString, bool hideEmptyItems)
        {
            if (Filtred.HasValue)
                return Filtred.Value;

            if (string.IsNullOrEmpty(searchString))
            {
                Filtred = IsFilled;
                return Filtred.Value;
            }

            if (Contains(searchString) && (IsFilled || !hideEmptyItems))
            {
                if (!NewEditorControl.FoundTreeViewItemsList.Contains(this))
                    NewEditorControl.FoundTreeViewItemsList.Add(this);
                ThisOrParentsContains = true;
                Filtred = true;
            }

            ThisOrParentsContains |= ((Parent as TreeViewItem)?.ThisOrParentsContains ?? false);

            var childsPassedFilter = false;

            if (Items != null)
            {
                foreach (var item in Items)
                {
                    childsPassedFilter |= item.Filter(searchString, hideEmptyItems);
                }
            }

            Filtred = (childsPassedFilter || ThisOrParentsContains) && (IsFilled || !hideEmptyItems);
            return Filtred.Value;
        }

        public virtual bool Contains(string value)
        {
            var valueForSearch = $"{DisplayText[0].ToUpper()} {EditText[0].ToUpper()} {DisplayText[1].ToUpper()} {EditText[1].ToUpper()}";
            return Search.Contains(valueForSearch, value);
        }

        /// <summary>
        /// Флаг, указывающий что данный элемент или его родительские элементы,
        /// содержат искомую строку
        /// </summary>
        public bool ThisOrParentsContains { get; set; } = false;


        public virtual ImageIndexEnum ImageIndex { get; set; } =
            ImageIndexEnum.NONE;

        public virtual ImageIndexEnum DescriptionImageIndex { get; set; } =
            ImageIndexEnum.NONE;

        public virtual bool ContainsBaseObject
        {
            get
            {
                return false;
            }
        }

        public virtual IEnumerable<string> BaseObjectsList
        {
            get
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Отключено или нет свойство
        /// </summary>
        public bool Disabled { get; set; }

        public bool MarkToCut { get; set; }

        public virtual ITreeViewItem Cut(ITreeViewItem item)
        {
            return null;
        }

        public virtual bool IsCuttable { get; } = false;

        private OnValueChanged valueChanged;
        public event OnValueChanged ValueChanged
        {
            add
            {
                valueChanged = null;
                valueChanged += value;
            }
            remove
            {
                valueChanged -= value;
            }
        }

        public virtual IRenderer[] CellRenderer => new IRenderer[] { null, null };

        public void OnValueChanged(object sender)
        {
            valueChanged?.Invoke(sender);
        }

        public virtual List<ITreeViewItem> QuickMultiSelect()
        {
            return Parent?.Items?.Where(adjItem => adjItem.GetType() == GetType()).ToList();
        }
        #endregion

        /// <summary>
        /// Обновление объекта на основе типового объекта
        /// </summary>
        /// <param name="genericObject"></param>
        public virtual void UpdateOnGenericTechObject(ITreeViewItem genericObject) { }

        /// <summary>
        /// Создание типового объекта на основе множества объектов
        /// </summary>
        /// <param name="itemList"></param>
        public virtual void CreateGenericByTechObjects(IEnumerable<ITreeViewItem> itemList) { }

        /// <summary>
        /// Обновление после удаление типовго объекта
        /// </summary>
        /// <remarks>
        /// Используется для обновления AttachedObjects и Action,
        /// так как типовые значения в них устанавливаются в отдельные поля
        /// </remarks>
        public virtual void UpdateOnDeleteGeneric() { }

        #region реализация IHelperItem
        /// <summary>
        /// Получить ссылку на страницу с справкой
        /// </summary>
        /// <returns></returns>
        public virtual string GetLinkToHelpPage()
        {
            if (string.IsNullOrEmpty(SystemIdentifier))
                return null;

            string ostisLink = EasyEPlanner.ProjectManager.GetInstance()
                .GetOstisHelpSystemLink();
            return $"{ostisLink}?sys_id={SystemIdentifier}";
        }

        public virtual string SystemIdentifier => "";

        public bool Visibility { get; set; } = true;
        #endregion
    }

    public delegate void OnValueChanged(object sender);

    public static class Search
    {
        public static bool Contains(string valueForSearch, string searchedValue)
        {
            if (UseRegex is false && SearchWholeWord is false)
                return valueForSearch.IndexOf(searchedValue, StringComparison.OrdinalIgnoreCase) >= 0;

            if (UseRegex is false)
            {
                // слово целиком
                try
                {
                    return Regex.IsMatch(valueForSearch, $"(^| ){searchedValue}($| )",
                        RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
                }
                catch
                {
                    return false;
                }
            }

            var pattern = string.Join("|", searchedValue
                .Split(new string[] { "||" }, StringSplitOptions.None)
                .Select(valueOR =>
                {
                    var valuesAND = valueOR.Trim().Split(new string[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);

                    return string.Join("", valuesAND
                        .Select(value => SearchWholeWord ? $"(?=.*(^| ){value.Trim()}($| ))" : value.Trim()));
                }));

            try
            {
                return Regex.IsMatch(valueForSearch, pattern,
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
            }
            catch
            {
                return false;
            }
        }

        public static bool SearchWholeWord { get; set; } = false;

        public static bool UseRegex { get; set; } = false;
    }
}
