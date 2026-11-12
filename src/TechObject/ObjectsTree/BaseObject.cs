    using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Security.Cryptography;
using System.Windows.Forms;
using Editor;

namespace TechObject
{
    public class BaseObject : TreeViewItem, IBaseObjChangeable
    {
        public BaseObject(string baseTechObjectName,
            ITechObjectManager techObjectManager)
        {
            localObjects = new List<TechObject>();
            baseTechObject = BaseTechObjectManager.GetInstance()
                .GetTechObjectCopy(baseTechObjectName);
            this.techObjectManager = techObjectManager;
            globalObjectsList = this.techObjectManager.TechObjects;
            globalGenericTechObjects = this.techObjectManager.GenericTechObjects;
        }

        /// <summary>
        /// Изменение номеров владельцев ограничений
        /// </summary>
        public void SetRestrictionOwner()
        {
            foreach (TechObject techObject in globalObjectsList)
            {
                techObject.SetRestrictionOwner();
            }
        }

        /// <summary>
        /// Добавить объект при загрузке из LUA
        /// </summary
        /// <param name="obj">Объект</param>
        public void AddObjectWhenLoadFromLua(TechObject obj,
            int genericObjectNumber)
        {
            obj.SetGetLocalN(GetTechObjectLocalNum);
            var genericObject = techObjectManager
                .GetGenericTObject(genericObjectNumber);
            localObjects.Add(obj);

            if (genericObject is null)
                techObjects.Add(obj);
            else
                genericObject.GenericGroup.AddTechObjectWhenLoadFromLua(obj);
        }

        /// <summary>
        /// Добавить типовой объект при загрузке из LUA
        /// </summary>
        /// <param name="obj">Типовой объект</param>
        public void AddGenericObjectWhenLoadFromLua(GenericTechObject obj)
        {
            var genericGroup = new GenericGroup(obj, this, techObjectManager);
            genericGroups.Add(genericGroup);

            genericGroup.AddParent(this);
        }

        #region реализация ITreeViewItem
        public override ITreeViewItem[] Items => GetItems();

        private ITreeViewItem[] GetItems() => genericGroups.Cast<ITreeViewItem>()
            .Concat(techObjects.Cast<ITreeViewItem>())
            .ToArray();

        public override string[] DisplayText
        {
            get
            {
                var name = baseTechObject.Name;
                if (localObjects.Count == 0)
                {
                    return new string[] { name, "" };
                }
                else
                {
                    return new string[] 
                    { 
                        $"{name} ({localObjects.Count})", 
                        "" 
                    };
                }
            }
        }

        public override string[] EditText
        {
            get
            {
                return new string[] { baseTechObject.Name , "" };
            }
        }

        public override bool IsInsertable
        {
            get
            {
                return !SingleInstanceViolation;
            }
        }

        public void AddObject(TechObject techObject)
        {
            localObjects.Add(techObject);
            globalObjectsList.Add(techObject);

            techObject.SetGetLocalN(GetTechObjectLocalNum);
            techObject.SetUpFromBaseTechObject();
        }

        public override ITreeViewItem Insert()
        {
            if (SingleInstanceViolation)
            {
                ShowSingleInstanceMessage();
                return null;
            }

            ObjectsAdder.Reset();

            var newObject = new TechObject(baseTechObject.Name, 
            GetTechObjectLocalNum, localObjects.Count + 1, GetDefaultTechType(), 
            GetDefaultNameEplan(), cooperParamNum, GetDefaultMonitorName(), "", baseTechObject);

            // Работа со списком в дереве и общим списком объектов.
            localObjects.Add(newObject);
            globalObjectsList.Add(newObject);
            techObjects.Add(newObject);

            // Обозначение начального номера объекта для ограничений.
            SetRestrictionOwner();

            newObject.SetUpFromBaseTechObject();
            newObject.AddParent(this);

            return newObject;
        }

        /// <summary>
        /// Создание нового типового объекта на основе базоваого тех. объекта
        /// </summary>
        /// <returns>Созданная группа с типовым объектом</returns>
        public ITreeViewItem CreateNewGenericGroup()
        {
            if (SingleInstanceViolation || SingleInstanceGenericViolation)
            {
                ShowSingleInstanceMessage();
                return null;
            }

            ObjectsAdder.Reset();

            var newGenericObject = new GenericTechObject(baseTechObject.Name,
                GetDefaultTechType(), GetDefaultNameEplan(), cooperParamNum,
                GetDefaultMonitorName(), "", baseTechObject);
            newGenericObject.SetUpFromBaseTechObject();
            
            var newGenericGroup = new GenericGroup(newGenericObject, this, techObjectManager);

            globalGenericTechObjects.Add(newGenericGroup.GenericTechObject);
            genericGroups.Add(newGenericGroup);
            newGenericGroup.AddParent(this);

            return newGenericGroup;
        }

        /// <summary>
        /// Создать группу с типовым объектом на основе тех. объекта
        /// </summary>
        /// <param name="techObject">Тех. Объект</param>
        public ITreeViewItem CreateGenericGroup(TechObject techObject)
        {
            var genericGroup = new GenericGroup(techObject, this, techObjectManager);

            globalGenericTechObjects.Add(genericGroup.GenericTechObject);
            genericGroups.Add(genericGroup);
            genericGroup.AddParent(this);

            techObjects.Remove(techObject);
            genericGroup.AddTechObjectWhenLoadFromLua(techObject);

            genericGroup.GenericTechObject.Update();

            return genericGroup;
        }

        /// <summary>
        /// Создать группу с типовым объектом на основе выбранного набора тех. объектов
        /// </summary>
        /// <param name="techObjects">Список тех. объектов</param>
        public ITreeViewItem CreateGenericGroup(List<TechObject> techObjects)
        {
            if (techObjects.Count == 1)
            {
                return CreateGenericGroup(techObjects.SingleOrDefault());
            }

            var first = techObjects[0];

            var name = techObjects.TrueForAll(item => item.Name == first.Name)? first.Name : baseTechObject.Name;
            var techType = techObjects.TrueForAll(item => item.TechType == first.TechType) ? first.TechType : -1;
            var nameEplan = techObjects.TrueForAll(item => item.NameEplan == first.NameEplan) ? first.NameEplan : ""; 
            var cooperParamNumber = techObjects.TrueForAll(item => item.CooperParamNumber == first.CooperParamNumber) ? first.CooperParamNumber : -1;

            var refNameBC = first.NameBC.Replace($"{first.TechNumber}", "");
            var nameBC = techObjects.TrueForAll(item => item.NameBC.Replace($"{item.TechNumber}", "") == refNameBC && item.NameBC != "TankObj") ? refNameBC : "";

            var newGenericTechObject = new GenericTechObject(name, techType, nameEplan, cooperParamNumber, nameBC, "", baseTechObject);
            var newGenericGroup = new GenericGroup(newGenericTechObject, this, techObjectManager);


            newGenericTechObject.AttachedObjects.CreateGenericByTechObjects(techObjects.Select(to => to.AttachedObjects));
            newGenericTechObject.ModesManager.CreateGenericByTechObjects(techObjects.Select(to => to.ModesManager));
            newGenericTechObject.GetParamsManager().CreateGenericByTechObjects(techObjects.Select(to => to.GetParamsManager()));
            newGenericTechObject.Equipment.CreateGenericByTechObjects(techObjects.Select(to => to.Equipment));

            globalGenericTechObjects.Add(newGenericGroup.GenericTechObject);
            genericGroups.Add(newGenericGroup);
            newGenericGroup.AddParent(this);

            techObjects.ForEach(item =>
            {
                this.TechObjects.Remove(item);
                newGenericGroup.AddTechObjectWhenLoadFromLua(item);
            });

            newGenericTechObject.Update();

            return newGenericGroup;
        }

        public void RemoveLocalObject(TechObject techObject)
            => localObjects.Remove(techObject);

        public void AddLocalObject(TechObject techObject)
            => localObjects.Add(techObject);

        override public bool Delete(object child)
        {
            const int markAsDelete = -1;
            
            if (child is TechObject techObject)
            {
                if (techObject.BaseTechObject.IsAttachable)
                {
                    techObjectManager.RemoveAttachingToObjects(techObject);
                }

                int globalNum = globalObjectsList.IndexOf(techObject) + 1;
                techObjectManager.CheckRestriction(globalNum, markAsDelete);

                // Работа со списком в дереве и общим списком объектов.
                localObjects.Remove(techObject);
                techObjects.Remove(techObject);
                globalObjectsList.Remove(techObject);

                // Обозначение начального номера объекта для ограничений.
                SetRestrictionOwner();

                techObjectManager.ChangeAttachedObjectsAfterDelete(globalNum);

                if (localObjects.Count == 0)
                {
                    Parent.Delete(this);
                }
                return true;
            }

            if (child is GenericGroup genericGroup)
            {
                genericGroups.Remove(genericGroup);
                globalGenericTechObjects.Remove(genericGroup.GenericTechObject);

                var deleteInherited = editor.DialogDeletingGenericGroup();

                foreach (var TObject in genericGroup.InheritedTechObjects)
                {
                    TObject.AddParent(this);
                    TObject.RemoveGenericTechObject();

                    techObjects.Add(TObject);
                    techObjects.Sort((obj1, obj2) => obj1.GlobalNum - obj2.GlobalNum);

                    if (deleteInherited == DialogResult.Yes)
                    {
                        Delete(TObject);
                    }
                    else
                    {
                        TObject.AttachedObjects.UpdateOnDeleteGeneric();
                        TObject.ModesManager.UpdateOnDeleteGeneric();
                    }
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

        public override bool CanMoveDown(object child)
        {
            if (child is TechObject techObject)
            {
                return techObjects.LastOrDefault() != techObject;
            }

            if (child is GenericGroup genericGroup)
            {
                return genericGroups.LastOrDefault() != genericGroup;
            }

            return false;
        }

        override public ITreeViewItem MoveDown(object child)
        {
            if (CanMoveDown(child) is false)
                return null;

            if (child is TechObject techObject)
            {
                int oldLocalIndex = techObjects.IndexOf(techObject);
                SwapTechObjects(techObject, oldLocalIndex, oldLocalIndex + 1);
                return techObject;
            }

            if (child is GenericGroup genericGroup)
            {
                var oldID = genericGroups.IndexOf(genericGroup);                
                SwapGenericGroups(genericGroup,oldID, oldID + 1);
                return genericGroup;
            }

            return null;
        }

        public override bool CanMoveUp(object child)
        {
            if (child is TechObject techObject)
            {
                return techObjects.FirstOrDefault() != techObject;
            }

            if (child is GenericGroup genericGroup)
            {
                return genericGroups.FirstOrDefault() != genericGroup;
            }

            return false;
        }

        override public ITreeViewItem MoveUp(object child)
        {
            if (CanMoveUp(child) is false)
                return null;

            if (child is TechObject techObject)
            {
                int oldLocalIndex = techObjects.IndexOf(techObject);
                SwapTechObjects(techObject, oldLocalIndex, oldLocalIndex - 1);
                return techObject;
            }

            if (child is GenericGroup genericGroup)
            {
                var oldID = genericGroups.IndexOf(genericGroup);
                SwapGenericGroups(genericGroup, oldID, oldID - 1);
                return genericGroup;
            }

            return null;
        }

        private void SwapTechObjects(TechObject techObject, int oldID, int newID)
        {
            int oldGlobalID = globalObjectsList.IndexOf(techObject);
            int newGlobalID = globalObjectsList.IndexOf(techObjects[newID]);

            techObjectManager.CheckRestriction(oldGlobalID + 1, newGlobalID + 1);

            // Работа со списком в дереве
            (techObjects[oldID], techObjects[newID]) = (techObjects[newID], techObjects[oldID]);

            // Глобальный список
            (globalObjectsList[oldGlobalID], globalObjectsList[newGlobalID]) =
                (globalObjectsList[newGlobalID], globalObjectsList[oldGlobalID]);

            // Обозначение начального номера объекта для ограничений.
            SetRestrictionOwner();

            techObjectManager.ChangeAttachedObjectsAfterMove(
                oldGlobalID + 1, newGlobalID + 1);

            techObjects[newID].AddParent(this);
        }

        private void SwapGenericGroups(GenericGroup genericGroup, int oldID, int newID)
        {
            var oldGlobalGenericID = globalGenericTechObjects
                   .IndexOf(genericGroup.GenericTechObject);
            var newGlobalGenericID = globalGenericTechObjects
                .IndexOf(genericGroups[newID].GenericTechObject);

            (genericGroups[oldID], genericGroups[newID]) = (genericGroups[newID], genericGroups[oldID]);
            (globalGenericTechObjects[oldGlobalGenericID], globalGenericTechObjects[newGlobalGenericID])
                = (globalGenericTechObjects[newGlobalGenericID], globalGenericTechObjects[oldGlobalGenericID]);
        }

        override public bool IsInsertableCopy
        {
            get
            {
                return !SingleInstanceViolation;
            }
        }

        override public ITreeViewItem InsertCopy(object obj)
        {
            if (SingleInstanceViolation)
            {
                var cutting = (obj as TechObject)?.MarkToCut == true;
                if (!cutting)
                {
                    return null;
                }
            }

            var techObj = obj as TechObject;
            if(techObj == null)
            {
                return null;
            }

            if (techObj.BaseTechObject != null &&
                techObj.BaseTechObject.Name == baseTechObject.Name &&
                techObj.MarkToCut is false)
            {
                int newN = 1;
                if (localObjects.Count > 0)
                {
                    newN = localObjects[localObjects.Count - 1].TechNumber + 1;
                }

                int oldObjN = globalObjectsList.IndexOf(techObj) + 1;
                int newObjN = globalObjectsList.Count + 1;

                var newObject = CloneObject(techObj, oldObjN, newObjN, techObj.IdentifyData.WithTechNumber(newN));

                // Работа со списком в дереве и общим списком объектов.
                techObjects.Add(newObject);
                localObjects.Add(newObject);
                globalObjectsList.Add(newObject);

                newObject.ChangeCrossRestriction();
                newObject.Equipment.ModifyDevNames();

                newObject.AddParent(this);
                return newObject;
            }
            else
            {
                ObjectsAdder.Reset();
                if (techObj.MarkToCut)
                {
                    return InsertCuttedCopy(techObj);
                }
            }

            return null;
        }

        /// <summary>
        /// Вставить вырезанный объект
        /// </summary>
        /// <param name="techObject">Объект</param>
        /// <returns></returns>
        private TechObject InsertCuttedCopy(TechObject techObject)
        {
            var techObjParent = techObject.Parent;
            techObjParent.Cut(techObject);

            if (techObject.BaseTechObject == null || techObject.BaseTechObject.Name != BaseTechObject.Name)
            {
                // Удаляем локальный объект из базового объекта, если переносим в другой базовый объект
                (techObject.Parent as BaseObject)?.LocalObjects.Remove(techObject);
                (techObject.Parent?.Parent as BaseObject)?.LocalObjects.Remove(techObject);

                if (techObject.BaseTechObject != null)
                    techObject.ResetBaseTechObject();
                techObject.InitBaseTechObject(BaseTechObject);
            }

            techObjects.Add(techObject);
            localObjects.Add(techObject);

            techObject.SetGetLocalN(GetTechObjectLocalNum);

            techObject.AddParent(this);
            return techObject;
        }

        public override bool IsCuttable => true;

        /// <summary>
        /// Вырезать тех. объект из базового объкта.
        /// </summary>
        public override ITreeViewItem Cut(ITreeViewItem item)
        {
            if (item is TechObject techObject)
            {
                techObjects.Remove(techObject);
                return techObject;
            }

            return null;
        }

        public override ITreeViewItem Replace(object child, object copyObject)
        {
            var techObject = child as TechObject;
            var copiedObject = copyObject as TechObject;
            bool objectsNotNull = (copiedObject != null && techObject != null);
            bool sameBaseObjectName = 
                copiedObject?.BaseTechObject.Name == baseTechObject.Name;
            if (objectsNotNull && sameBaseObjectName)
            {
                //Старый и новый номер объекта - для замены в ограничениях
                int oldObjNum = globalObjectsList.IndexOf(copiedObject) + 1;
                int newObjNum = globalObjectsList.IndexOf(techObject) + 1;

                var newObject = CloneObject(copyObject, oldObjNum,
                    newObjNum, techObject.IdentifyData);

                // Список тех. объектов вне групп
                int toindex = techObjects.IndexOf(techObject);
                techObjects.Remove(techObject);
                techObjects.Insert(toindex, newObject);

                // Список локальных объектов
                int localIndex = localObjects.IndexOf(techObject);
                localObjects.Remove(techObject);
                localObjects.Insert(localIndex, newObject);

                // Глобальный список объектов
                int globalIndex = globalObjectsList.IndexOf(techObject);
                globalObjectsList.Remove(techObject);
                globalObjectsList.Insert(globalIndex, newObject);

                // Для корректного копирования ограничений
                newObject.ChangeCrossRestriction(techObject);

                newObject.AddParent(this);

                // Оставить старые привязанные агрегаты в заменяемом объекте
                newObject.AttachedObjects.SetNewValue(techObject.AttachedObjects.Value);
                return newObject;
            }

            return null;
        }

        /// <summary>
        /// Клонирование объекта (копирование)
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="newN">Номер внутренний объекта</param>
        /// <param name="oldObjNum">Старый глобальный номер</param>
        /// <param name="newObjNum">Новый глобальный номер</param>
        /// <returns></returns>
        private TechObject CloneObject(object obj, int oldObjNum,
            int newObjNum, ITechObjectIdentifyData techObjectIdentifyData)
        {
            var techObject = obj as TechObject;
            var stubObject = techObject.Clone(GetTechObjectLocalNum,
                techObjectIdentifyData.TechNumber, oldObjNum, newObjNum);

            if (newObjNum > globalObjectsList.Count)
            {
                return CloneForInsert(techObject, stubObject, oldObjNum,
                    newObjNum, techObjectIdentifyData);
            }
            else
            {
                return CloneForReplace(techObject, stubObject, oldObjNum,
                    newObjNum, techObjectIdentifyData);
            }
        }

        /// <summary>
        /// Клонирование для вставки
        /// </summary>
        /// <param name="cloningObject">Клонируемый объект</param>
        /// <param name="stubObject">Объект-заглушка</param>
        /// <param name="newN">Номер внутренний объекта</param>
        /// <param name="oldObjNum">Старый глобальный номер</param>
        /// <param name="newObjNum">Новый глобальный номер</param>
        /// <returns></returns>
        private TechObject CloneForInsert(TechObject cloningObject,
            TechObject stubObject, int oldObjNum, int newObjNum,
            ITechObjectIdentifyData techObjectIdentifyData)
        {
            globalObjectsList.Add(stubObject);
            var clonedObject = cloningObject.Clone(GetTechObjectLocalNum,
                oldObjNum, newObjNum, techObjectIdentifyData);
            globalObjectsList.Remove(stubObject);
            return clonedObject;
        }

        /// <summary>
        /// Клонирование для замены
        /// </summary>
        /// <param name="cloningObject">Клонируемый объект</param>
        /// <param name="stubObject">Объект-заглушка</param>
        /// <param name="newN">Номер внутренний объекта</param>
        /// <param name="oldObjNum">Старый глобальный номер</param>
        /// <param name="newObjNum">Новый глобальный номер</param>
        /// <returns></returns>
        private TechObject CloneForReplace(TechObject cloningObject,
            TechObject stubObject, int oldObjNum, int newObjNum,
            ITechObjectIdentifyData techObjectIdentifyData)
        {
            var objectFromGlobalList = globalObjectsList[newObjNum - 1];
            globalObjectsList[newObjNum - 1] = stubObject;
            var clonedObject = cloningObject.Clone(GetTechObjectLocalNum,
                oldObjNum, newObjNum, techObjectIdentifyData);
            globalObjectsList[newObjNum - 1] = objectFromGlobalList;
            return clonedObject;
        }

        public override bool ShowWarningBeforeDelete
        {
            get
            {
                return true;
            }
        }
        #endregion

        /// <summary>
        /// Получить локальный номер технологического объекта
        /// </summary>
        /// <param name="searchingObject">Искомый объект</param>
        /// <returns></returns>
        public int GetTechObjectLocalNum(object searchingObject)
        {
            var techObject = searchingObject as TechObject;
            int num = localObjects.IndexOf(techObject) + 1;
            return num;
        }

        public void ChangeBaseObj(ITreeViewItem treeItem)
        {
            var techObject = treeItem as TechObject;
            bool success = techObjectManager.ChangeBaseObject(techObject);
            if (success)
            {
                localObjects.Remove(techObject);
                techObjects.Remove(techObject);
                if (localObjects.Count == 0)
                {
                    Parent.Delete(this);
                }
            }
        }

        public int Count => localObjects.Count;

        /// <summary>
        /// Группы с типовыми объектами
        /// </summary>
        public List<GenericGroup> GenericGroups => genericGroups;

        /// <summary>
        /// Технологические объекты без групп
        /// </summary>
        public List<TechObject> TechObjects => techObjects;

        /// <summary>
        /// Все технологические объекты в базовом объекте
        /// </summary>
        public List<TechObject> LocalObjects => localObjects;

        public BaseTechObject BaseTechObject => baseTechObject; 

        protected readonly List<GenericGroup> genericGroups = new List<GenericGroup>();
        protected readonly List<TechObject> techObjects = new List<TechObject>();

        protected List<TechObject> localObjects;
        protected BaseTechObject baseTechObject;
        protected readonly List<GenericTechObject> globalGenericTechObjects;
        protected List<TechObject> globalObjectsList;
        protected ITechObjectManager techObjectManager;

        private Editor.IEditor editor { get; set; } = Editor.Editor.GetInstance();

        protected int GetDefaultTechType() =>
            baseTechObject?.TechType ?? techTypeNum;

        protected string GetDefaultNameEplan() =>
            baseTechObject?.DefaultNameEplan ?? string.Empty;

        protected string GetDefaultMonitorName() =>
            baseTechObject?.MonitorName ?? string.Empty;

        /// <summary>
        /// Нарушено ли ограничение единственного экземпляра обычными объектами
        /// </summary>
        private bool SingleInstanceViolation =>
            baseTechObject?.IsSingleInstance == true &&
            localObjects.Count > 0;

        /// <summary>
        /// Нарушено ли ограничение единственного экземпляра типовыми объектами
        /// </summary>
        private bool SingleInstanceGenericViolation =>
            baseTechObject?.IsSingleInstance == true &&
            genericGroups.Count > 0;

        /// <summary>
        /// Показать сообщение о нарушении ограничения единственного экземпляра
        /// </summary>
        protected virtual void ShowSingleInstanceMessage()
        {
            System.Windows.Forms.MessageBox.Show(
                $"Базовый объект \"{baseTechObject.Name}\" допускает только " +
                "один экземпляр в проекте.",
                "Ограничение", System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Warning);
        }

        protected const int techTypeNum = 2;
        protected const int cooperParamNum = -1;
    }
}
