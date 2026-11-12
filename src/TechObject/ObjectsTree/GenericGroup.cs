using Aga.Controls.Tree;
using Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechObject
{
    /// <summary>
    /// 
    /// </summary>
    public interface IGenericObject
    {

    }

    /// <summary>
    /// Типовой (логический) объект,
    /// определяющий базовую функциональность
    /// связанных физических объектов
    /// </summary>
    public class GenericGroup : TreeViewItem, IGenericObject
    {
        /// <summary>
        /// Создание новой группы на основе типовго объекта 
        /// (используется при загрузке из LUA)
        /// </summary>
        /// <param name="genericTechObject"></param>
        /// <param name="baseObject"></param>
        public GenericGroup(GenericTechObject genericTechObject, BaseObject baseObject,
            ITechObjectManager techObjectManager)
        {
            this.techObjectManager = techObjectManager;
            this.genericTechObject = genericTechObject;
            genericTechObject.GenericGroup = this;
            this.baseObject = baseObject;
        }

        /// <summary>
        /// Создание новой группы и типового на основе технологического объекта
        /// </summary>
        /// <param name="techObject"> Тех. объект </param>
        /// <param name="baseObject"> базовый объект </param>
        public GenericGroup(TechObject techObject, BaseObject baseObject,
            ITechObjectManager techObjectManager)
        {
            this.techObjectManager = techObjectManager;
            genericTechObject = new GenericTechObject(techObject);
            genericTechObject.GenericGroup = this;
            this.baseObject = baseObject;
        }


        public override string[] DisplayText
            => new string[] { $"{genericTechObject.Name} ({Items.Count() - 1})", string.Empty };

        public override ITreeViewItem[] Items => GetItems();

        private ITreeViewItem[] GetItems()
        {
            return (new List<ITreeViewItem>() { genericTechObject })
                .Concat(InheritedTechObjects).ToArray();
        }


        public override bool IsMoveable => true;

        public override bool IsInsertableCopy
        {
            get
            {
                if (baseObject?.BaseTechObject?.IsSingleInstance == true &&
                    baseObject.Count > 0)
                {
                    return false;
                }
                return true;
            }
        }

        public override ITreeViewItem InsertCopy(object obj)
        {
            if (baseObject == null)
                return null;

            if (baseObject.BaseTechObject?.IsSingleInstance == true &&
                baseObject.Count > 0)
            {
                var cutting = (obj as TechObject)?.MarkToCut == true;
                if (!cutting)
                    return null;
            }

            var techObject = obj as TechObject;

            if (techObject is null)
                return null;

            if (techObject.MarkToCut)
                return InsertCuttedCopy(techObject);

            var clone = techObject.Clone(baseObject.GetTechObjectLocalNum,
                InheritedTechObjects.LastOrDefault()?.TechNumber + 1 ?? 1,
                techObjectManager.TechObjects.IndexOf(techObject) + 1,
                techObjectManager.TechObjects.Count + 1);

            baseObject.AddLocalObject(clone);
            techObjectManager.TechObjects.Add(clone);
            genericTechObject.SetUpTechObject(clone, baseObject);
            clone.AddParent(this);

            genericTechObject.Update();

            return clone;
        }

        public TechObject InsertCuttedCopy(TechObject techObject)
        {
            techObject.Parent.Cut(techObject);

            if (techObject.BaseTechObject == null || techObject.BaseTechObject.Name != baseObject.BaseTechObject.Name)
            {
                // Удаляем из базового объекта локальный объект, если переносим в другой базовый объект
                (techObject.Parent as BaseObject)?.LocalObjects.Remove(techObject);
                (techObject.Parent?.Parent as BaseObject)?.LocalObjects.Remove(techObject);
                
                baseObject.LocalObjects.Add(techObject);
                
                if (techObject.BaseTechObject != null)
                    techObject.ResetBaseTechObject();
                techObject.InitBaseTechObject(baseObject.BaseTechObject);
            }

            techObject.AddParent(this);

            techObject.SetGetLocalN(baseObject.GetTechObjectLocalNum);

            genericTechObject.SetUpTechObject(techObject, baseObject);
            genericTechObject.Update();

            return techObject;
        }

        public override bool IsCuttable => true;

        public override ITreeViewItem Cut(ITreeViewItem item)
        {
            if (item is TechObject techObject && (item is GenericTechObject) is false)
            {
                techObject.GenericTechObject = null;
                InheritedTechObjects.Remove(techObject);
                return techObject;
            }

            return null;
        }

        public override bool IsDeletable => true;

        public override bool ShowWarningBeforeDelete => true;

        public override bool Delete(object child)
        {
            if (child is TechObject techObject)
            {
                if (techObject.BaseTechObject.IsAttachable)
                {
                    techObjectManager.RemoveAttachingToObjects(techObject);
                }

                int globalNum = techObjectManager.GetTechObjectN(techObject);
                techObjectManager.CheckRestriction(globalNum, -1);

                baseObject.RemoveLocalObject(techObject);
                InheritedTechObjects.Remove(techObject);
                techObjectManager.TechObjects.Remove(techObject);

                baseObject.SetRestrictionOwner();

                techObjectManager.ChangeAttachedObjectsAfterDelete(globalNum);

                return true;
            }

            return false;
        }

        public override bool IsInsertable
        {
            get
            {
                if (baseObject?.BaseTechObject?.IsSingleInstance == true &&
                    baseObject.Count > 0)
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Создание тех. объека на основе типового объекта
        /// </summary>
        public override ITreeViewItem Insert()
        {
            if (baseObject?.BaseTechObject?.IsSingleInstance == true &&
                baseObject.Count > 0)
            {
                return null;
            }

            var techObject = genericTechObject.CreateTechObject(baseObject);

            techObject.AddParent(this);

            genericTechObject.Update();
            return techObject;
        }

        public override bool CanMoveUp(object child)
        {
            if (!(child is TechObject) || child is GenericTechObject)
                return false;

            return InheritedTechObjects.FirstOrDefault() != child;
        }

        public override ITreeViewItem MoveUp(object child)
        {
            if (CanMoveUp(child) is false)
                return null;

            var techObject = (TechObject)child;
            var oldID = InheritedTechObjects.IndexOf(techObject);

            SwapTechObjects(techObject, oldID, oldID - 1);

            return techObject;
        }

        public override bool CanMoveDown(object child)
        {
            if (!(child is TechObject) || child is GenericTechObject)
                return false;

            return InheritedTechObjects.LastOrDefault() != child;
        }

        public override ITreeViewItem MoveDown(object child)
        {
            if (CanMoveDown(child) is false)
                return null;

            var techObject = (TechObject)child;
            var oldID = InheritedTechObjects.IndexOf(techObject);

            SwapTechObjects(techObject, oldID, oldID + 1);

            return techObject;
        }

        public override ImageIndexEnum ImageIndex
            => ImageIndexEnum.GenericGroup;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="techObject"></param>
        /// <param name="oldID"></param>
        /// <param name="newID"></param>
        private void SwapTechObjects(TechObject techObject, int oldID, int newID)
        {
            var oldGlobalID = techObjectManager.TechObjects.IndexOf(techObject);
            var newGlobalID = techObjectManager.TechObjects.IndexOf(InheritedTechObjects[newID]);

            techObjectManager.CheckRestriction(oldGlobalID + 1, newGlobalID + 1);

            (InheritedTechObjects[oldID], InheritedTechObjects[newID]) =
                (InheritedTechObjects[newID], InheritedTechObjects[oldID]);

            InheritedTechObjects.Remove(techObject);
            InheritedTechObjects.Insert(newID, techObject);

            (techObjectManager.TechObjects[oldGlobalID], techObjectManager.TechObjects[newGlobalID])
                = (techObjectManager.TechObjects[newGlobalID], techObjectManager.TechObjects[oldGlobalID]);

            baseObject.SetRestrictionOwner();

            techObjectManager.ChangeAttachedObjectsAfterMove(oldGlobalID + 1, newGlobalID + 1);
        }

        /// <summary>
        /// Добавить технологический объект в группу
        /// (при загрузке из LUA)
        /// </summary>
        /// <param name="techObject"></param>
        public void AddTechObjectWhenLoadFromLua(TechObject techObject)
        {
            GenericTechObject.SetUpTechObject(techObject, baseObject);
            techObject.AddParent(this);
        }

        private readonly ITechObjectManager techObjectManager;

        /// <summary>
        /// Список тех. объектов в группе
        /// </summary>
        public List<TechObject> InheritedTechObjects => genericTechObject.InheritedTechObjects;

        /// <summary>
        /// Типовой объект группы
        /// </summary>
        public GenericTechObject GenericTechObject => genericTechObject;

        private readonly GenericTechObject genericTechObject;
        private readonly BaseObject baseObject;
    }
}
