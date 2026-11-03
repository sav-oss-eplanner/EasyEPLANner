using BrightIdeasSoftware;
using StaticHelper;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Editor
{
    /// <summary>    
    /// Простейший редактируемый объект - свойство.
    /// </summary>
    public class ObjectProperty : ITreeViewItem, IHelperItem
    {
        public ImageIndexEnum ImageIndex { get; set; } =
            ImageIndexEnum.NONE;

        public ImageIndexEnum DescriptionImageIndex { get; set; } =
            ImageIndexEnum.NONE;
        
        /// <param name="name">Имя свойства.</param>
        /// <param name="value">Значение свойства.</param>
        /// <param name="defaultValue">стандартное значение</param>
        public ObjectProperty(string name, object value,
            object defaultValue = null)
        {
            this.name = name;
            this.value = value ?? string.Empty;

            if (defaultValue == null)
            {
                this.defaultValue = "";
            }
            else
            {
                this.defaultValue = defaultValue;
            }

            needDisable = false;
        }

        /// <summary>
        /// Копия объекта
        /// </summary>
        /// <returns></returns>
        public virtual ObjectProperty Clone()
        {
            return (ObjectProperty)MemberwiseClone();
        }

        /// <summary>
        /// Установить значение свойства
        /// </summary>
        /// <param name="val">Значение</param>
        public virtual void SetValue(object val)
        {
            value = val;
            OnValueChanged(this);
        }

        /// <summary>
        /// Значение свойства
        /// </summary>
        public virtual string Value => value.ToString();

        /// <summary>
        /// Имя свойства
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Имя картинки
        /// </summary>
        public string ImageName
        {
            get
            {
                return "Свойство";
            }
        }

        public string DefaultValue
        {
            get
            {
                return defaultValue.ToString();
            }
        }

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

        public virtual string[] DisplayText
        {
            get
            {
                if (value.ToString() == defaultValue.ToString())
                {
                    return new string[] { name,
                        StaticHelper.CommonConst.StubForCells };
                }
                else
                {
                    return new string[] { name, value.ToString() };
                }
            }
        }

        public virtual string[] EditText
        {
            get
            {
                if (value is double)
                {
                    var provider = new System.Globalization.NumberFormatInfo();
                    provider.NumberDecimalSeparator = ".";

                    double v = (double)value;
                    return new string[] { "",
                        string.Format( provider, "{0:0.##}", v ) };
                }

                return new string[] { "", value.ToString() };
            }
        }

        public virtual bool IsDeletable
        {
            get
            {
                return true;
            }
        }

        public virtual bool Delete(object child)
        {
            if (value.GetType() == defaultValue.GetType() && IsDeletable)
            {
                return SetNewValue(DefaultValue);
            }

            return false;
        }

        public virtual bool IsCopyable
        {
            get
            {
                return false;
            }
        }

        public virtual object Copy()
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

        public bool IsMoveable
        {
            get
            {
                return false;
            }
        }

        public bool CanMoveUp(object child) => false;

        public bool CanMoveDown(object child) => false;

        public ITreeViewItem MoveDown(object child)
        {
            return null;
        }

        public ITreeViewItem MoveUp(object child)
        {
            return null;
        }

        public virtual bool IsInsertableCopy
        {
            get
            {
                return false;
            }
        }

        public virtual ITreeViewItem InsertCopy(object obj)
        {
            return null;
        }

        public virtual bool IsReplaceable
        {
            get
            {
                return false;
            }
        }

        public virtual ITreeViewItem Replace(object child, object copyObject)
        {
            return null;
        }

        public virtual ITreeViewItem[] Items => null;

        public virtual int[] EditablePart
        {
            get
            {
                return new int[] { -1, 1 };
            }
        }

        public virtual bool IsEditable
        {
            get
            {
                return true;
            }
        }

        public virtual bool SetNewValue(string newValue)
        {
            bool res = false;

            switch (value.GetType().Name)
            {
                case "String":
                    value = newValue;
                    res = true;
                    break;

                case "Int32":
                case "Int16":
                    try
                    {
                        value = System.Convert.ToInt32(newValue);
                        res = true;
                    }
                    catch (System.Exception)
                    {
                    }
                    break;

                case "Double":
                    try
                    {
                        value = System.Convert.ToDouble(newValue);
                        res = true;
                    }
                    catch (System.Exception)
                    {
                    }
                    break;
            }
            OnValueChanged(this);
            return res;
        }

        public virtual bool SetNewValue(IDictionary<int, List<int>> newDict)
        {
            bool res = false;
            return res;
        }

        public virtual bool SetNewValue(string newValue, bool isExtraValue)
        {
            bool res = false;
            return res;
        }

        virtual public bool SetNewValue(string newValue, int column)
        {
            return SetNewValue(newValue);
        }

        public bool IsInsertable
        {
            get
            {
                return false;
            }
        }

        public ITreeViewItem Insert()
        {
            return null;
        }

        public ITreeViewItem Insert(IDialogFactory dialogFactory) => null;

        public virtual bool IsUseDevList
        {
            get
            {
                return false;
            }
        }

        public bool IsUseRestriction
        {
            get
            {
                return false;
            }
        }

        public bool IsLocalRestrictionUse
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsDrawOnEplanPage => false;

        public virtual List<DrawInfo> GetObjectToDrawOnEplanPage() => new List<DrawInfo>() { };

        public virtual void GetDisplayObjects(out EplanDevice.DeviceType[] devTypes,
            out EplanDevice.DeviceSubType[] devSubTypes, out bool displayParameters)
        {
            devTypes = null;
            devSubTypes = null;
            displayParameters = false;
            return;
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

        public bool IsMainObject
        {
            get
            {
                return false;
            }
        }

        public bool IsMode
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

        /// <summary>
        /// Заполнен или нет элемент дерева.
        /// True - отображать
        /// False - скрывать
        /// </summary>
        public virtual bool IsFilled => EditText[1].Length > 0 &&
            !(EditText[1] == CommonConst.EmptyValue &&
            DisplayText[1] == CommonConst.StubForCells);

        public bool? Filtred { get; private set; } = null;

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

        public bool Filter(string searchString, bool hideEmptyItems)
        {
            if (Filtred.HasValue)
                return Filtred.Value;

            if (string.IsNullOrEmpty(searchString))
            {
                Filtred = IsFilled;
                return Filtred.Value;
            }

            bool isVisibleByFill = IsFilled || !hideEmptyItems;
            if (Contains(searchString) && (IsFilled || !hideEmptyItems))
            {
                if (!NewEditorControl.FoundTreeViewItemsList.Contains(this))
                    NewEditorControl.FoundTreeViewItemsList.Add(this);
                ThisOrParentsContains = true;
                Filtred = true;
            }

            ThisOrParentsContains |= ParentContainsSearch();

            var childsPassedFilter = false;
            if (Items != null)
            {
                foreach (var item in Items)
                {
                    childsPassedFilter |= item.Filter(searchString, hideEmptyItems);
                }
            }

            Filtred = (childsPassedFilter || ThisOrParentsContains) &&
                isVisibleByFill;
            return Filtred.Value;
        }

        private bool ParentContainsSearch() =>
            (Parent as TreeViewItem)?.ThisOrParentsContains ??
            (Parent as ObjectProperty)?.ThisOrParentsContains ??
            false;

        public virtual bool Contains(string value)
        {
            var valueForSearch = $"{DisplayText[0]} {EditText[0]} {DisplayText[1]} {EditText[1]}";
            return Search.Contains(valueForSearch, value);
        }

        public bool ThisOrParentsContains { get; set; } = false;

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

        public ITreeViewItem Cut(ITreeViewItem item)
        {
            return null;
        }

        public  bool IsCuttable { get; } = false;

        public virtual IRenderer[] CellRenderer => new IRenderer[] { null, null };

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
        public void OnValueChanged(object sender)
        {
            valueChanged?.Invoke(sender);
        }

        public virtual List<ITreeViewItem> QuickMultiSelect()
        {
            return Parent?.Items?.Where(adjItem => adjItem.GetType() == GetType()).ToList();
        }
        #endregion

        #region реализация IHelperItem
        public virtual string GetLinkToHelpPage()
        {
            if (string.IsNullOrEmpty(SystemIdentifier))
                return null;

            string ostisLink = EasyEPlanner.ProjectManager.GetInstance()
                .GetOstisHelpSystemLink();
            return $"{ostisLink}?sys_id={SystemIdentifier}";
        }

        public virtual string SystemIdentifier => "";

        public virtual bool Visibility { get; set; } = true;
        #endregion

        public virtual void UpdateOnGenericTechObject(ObjectProperty genericProperty)
        {
            this.value = genericProperty.Value;
            this.defaultValue = genericProperty.DefaultValue;
            this.needDisable = genericProperty.NeedDisable;
        }

        ITreeViewItem parent;
        private string name;  ///Имя свойства.
        protected object value; ///Значение свойства.
        protected object defaultValue; ///Стандартное значение

        private bool needDisable;
    }
}
