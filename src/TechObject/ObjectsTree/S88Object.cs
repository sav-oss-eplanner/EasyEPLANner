using System.Collections.Generic;
using System.Linq;
using Editor;

namespace TechObject
{
    /// <summary>
    /// Объект S88, описывающий Аппарат или Агрегат проекта.
    /// </summary>
    public class S88Object : TreeViewItem
    {
        public S88Object(string name, ITechObjectManager techObjectManager)
        {
            this.name = name;
            objects = new List<ITreeViewItem>();
            this.techObjectManager = techObjectManager;
        }

        /// <summary>
        /// Добавление объекта при загрузке из LUA
        /// </summary>
        /// <param name="obj">Объект</param>
        public void AddObjectWhenLoadFromLua(TechObject obj,
            bool isGeneric = false, int genericObjectNumber = -1)
        {
            var findedBaseObject = objects
                .Where(x => x.EditText[0] == obj.BaseTechObject.Name)
                .FirstOrDefault() as BaseObject;
            if (findedBaseObject == null)
            {
                findedBaseObject = new BaseObject(obj.BaseTechObject.Name,
                    techObjectManager);
                objects.Add(findedBaseObject);
            }

            if(isGeneric)
            {
                findedBaseObject.AddGenericObjectWhenLoadFromLua(obj as GenericTechObject);
            }
            else
            {
                findedBaseObject.AddObjectWhenLoadFromLua(obj, genericObjectNumber);
            }
        }

        #region реализация ITreeViewItem
        public override ITreeViewItem[] Items
        {
            get
            {
                return objects.ToArray();
            }
        }

        public override string[] DisplayText
        {
            get
            {
                if(Items.Length > 0)
                {
                    return new string[] { $"{name} ({Items.Length})", "" };
                }
                else
                {
                    return new string[] { name, "" };
                }
            }
        }

        public override string[] EditText
        {
            get
            {
                return new string[] { name, "" };
            }
        }

        public override bool IsInsertableCopy
        {
            get
            {
                return true;
            }
        }

        public override ITreeViewItem InsertCopy(object obj)
        {
            var techObject = obj as TechObject;
            if (techObject == null)
            {
                return null;
            }

            string selectedSubType = GetSelectedSubType();
            if (selectedSubType != null)
            {
                ITreeViewItem insertedItem = InsertSubType(selectedSubType,
                    techObject);

                if (insertedItem != null)
                {
                    insertedItem.AddParent(this);
                }
                return insertedItem;
            }

            return null;
        }

        public override bool IsInsertable
        {
            get
            {
                return true;
            }
        }

        public override ITreeViewItem Insert()
        {
            string selectedSubType = GetSelectedSubType();
            if (selectedSubType != null)
            {
                ITreeViewItem insertedItem = InsertSubType(selectedSubType);

                if (insertedItem != null)
                {
                    insertedItem.AddParent(this);
                }
                return insertedItem;
            }

            return null;
        }

        public ITreeViewItem Insert(string objectSubType)
        {
            ITreeViewItem insertedItem = InsertSubType(objectSubType);
            if (insertedItem != null)
            {
                insertedItem.AddParent(this);
            }
            return insertedItem;
        }

        /// <summary>
        /// Получить имя базового объекта (подтип S88)
        /// </summary>
        /// <returns></returns>
        private string GetSelectedSubType()
        {
            string selectedSubType = ObjectsAdder.LastSelectedSubType;
            if(selectedSubType == null)
            {
                var objectsAdderForm = new ObjectsAdder(name);
                objectsAdderForm.ShowDialog();
                string subType = ObjectsAdder.LastSelectedSubType;
                if (subType != null)
                {
                    return subType;
                }
            }
            else
            {
                return selectedSubType;
            }

            return null;
        }

        /// <summary>
        /// Вставить подтип в дерево. Выполняется или вставка или 
        /// копирование и вставка.
        /// </summary>
        /// <param name="selectedSubType">Выбранный подтип на форме</param>
        /// <param name="techObj">Объект, который надо вставить (скопировать)
        /// </param>
        /// <returns></returns>
        private ITreeViewItem InsertSubType(string selectedSubType, 
            TechObject techObj = null)
        {
            ITreeViewItem baseTreeItem = GetTreeItem(selectedSubType);
            ITreeViewItem currentObject;

            bool needInsert = techObj == null;
            if(needInsert)
            {
                currentObject = baseTreeItem.Insert();
            }
            else
            {
                currentObject = baseTreeItem.InsertCopy(techObj);
            }

            if (currentObject != null)
            {
                if (!objects.Contains(baseTreeItem))
                {
                    objects.Add(baseTreeItem);
                }

                return baseTreeItem;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Получить объект дерева, описывающий базу по S88.
        /// </summary>
        /// <param name="selectedSubType">Выбранный на форме подтип объекта
        /// </param>
        /// <returns></returns>
        private ITreeViewItem GetTreeItem(string selectedSubType)
        {
            var item = objects
                .Where(x => x.EditText[0] == selectedSubType)
                .FirstOrDefault();
            if (item != null)
            {
                return item;
            }
            else
            {
                return new BaseObject(selectedSubType, techObjectManager);
            }
        }

        override public bool Delete(object child)
        {
            var baseObject = child as BaseObject;
            if (baseObject != null)
            {
                foreach(var baseObjectItem in baseObject.Items)
                {
                    baseObject.Delete(baseObjectItem);
                }

                if(baseObject.Items.Count() == 0)
                {
                    objects.Remove(baseObject);
                }

                if (objects.Count == 0)
                {
                    Parent.Delete(this);
                }

                return true;
            }

            return false;
        }

        override public bool IsDeletable
        {
            get
            {
                return true;
            }
        }

        public override bool ShowWarningBeforeDelete
        {
            get
            {
                return true;
            }
        }
        #endregion

        public override string SystemIdentifier => 
            name == "Аппарат"?
            "unit" : "equipment_module";

        string name;
        List<ITreeViewItem> objects;
        ITechObjectManager techObjectManager;
    }
}
