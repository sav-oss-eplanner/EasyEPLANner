using Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechObject
{
    /// <summary>
    /// Типовой технологический объект.
    /// </summary>
    /// <remarks>
    /// Описывает типовую функциональность для группы технологических объектов.
    /// </remarks>
    public class GenericTechObject : TechObject
    {
        /// <summary>
        /// Инициализация типового объекта (из Lua)
        /// </summary>
        /// <param name="name">Имя</param>
        /// <param name="techType">Тип</param>
        /// <param name="nameEplan">ОУ</param>
        /// <param name="cooperParamNumber">Время совместного перехода шагов</param>
        /// <param name="NameBC">Имя объекта Monitor</param>
        /// <param name="attachedObjects">привязанные агрегаты</param>
        /// <param name="baseTechObject">Базовый тех. объект</param>
        public GenericTechObject(string name, int techType, string nameEplan,
            int cooperParamNumber, string NameBC, string attachedObjects,
            BaseTechObject baseTechObject)
            : base(name, null, 0, techType, nameEplan,
                cooperParamNumber, NameBC, attachedObjects, baseTechObject)
        {
            items.Remove(techNumber);

            if (Editor.Editor.GetInstance().Editable)
                Update();
        }

        /// <summary>
        /// Создание типового объекта на основе тех. объекта
        /// </summary>
        /// <param name="techObject">
        /// Тех. объект, на онове которого
        /// создается типовой объект
        /// </param>
        public GenericTechObject(TechObject techObject)
            : base(techObject.Name, null, 0, techObject.TechType, techObject.NameEplan,
                  techObject.CooperParamNumber, techObject.NameBC,
                  techObject.AttachedObjects.Value, techObject.BaseTechObject)
        {
            modes = techObject.ModesManager.Clone(this);
            paramsManager = techObject.GetParamsManager().Clone();
            equipment = techObject.Equipment.Clone(this);

            SetItems();
            items.Remove(techNumber);

            Update();
        }

        /// <summary>
        /// Установка обработчиков для событий
        /// </summary>
        public void SetUpEventsHandlers()
        {
            ValueChanged += UpdateTechObjectsData;
            Equipment.ValueChanged += UpdateEquipment;
            paramsManager.ValueChanged += UpdateParamsManager;
            ModesManager.ValueChanged += UpdateModesManager;
            AttachedObjects.ValueChanged += UpdateAttachedObjects;
            BaseTechObject.ObjectGroupsList.ForEach(gr => gr.ValueChanged += UpdateObjectGroups);
        }

        /// <summary>
        /// Обновить все данные всех тех.объектов в группе
        /// </summary>
        public void Update()
        {
            SetUpEventsHandlers();

            UpdateAttachedObjects(null);
            UpdateModesManager(null);
            UpdateEquipment(null);
            UpdateParamsManager(null);
            UpdateTechObjectsData(null);
            UpdateObjectGroups(null);
        }

        /// <summary>
        /// Обновить поле "Привязанные агрегаты"
        /// </summary>
        /// <remarks>
        /// При обновлении привязанных агреггатов ведется поиск аггрегатов по
        /// ОУ привязанного агргата и технологическому номеру обновляемого объекта,
        /// и автоматически привязывает группу агргатов к данной группе тех.объектов
        /// в соотвествии с их технологическими номерами
        /// </remarks>
        private void UpdateAttachedObjects(object sender)
        {
            InheritedTechObjects.ForEach(to => to.AttachedObjects.UpdateOnGenericTechObject(AttachedObjects));
        }

        /// <summary>
        /// Обновить поля групп танков: Группа танков, Источники/Приемники
        /// </summary>
        private void UpdateObjectGroups(object sender)
        {
            inheritedTechObjects.ForEach(to =>
            {
                var genericObjGrList = BaseTechObject.ObjectGroupsList;
                var inheritedObjGrList = to.BaseTechObject.ObjectGroupsList;

                var objectsGroups = genericObjGrList.Zip(inheritedObjGrList, (g, i) => new { Generic = g, Inherited = i });
                foreach (var og in objectsGroups)
                {
                    og.Inherited.UpdateOnGenericTechObject(og.Generic);
                }
            });
        }

        /// <summary>
        /// Обновить поле "Операции"
        /// </summary>
        /// <remarks>
        /// При привязке устройства к действию типового объекта, в наследуемом
        /// тех. объекте данно устройство обозначается цветным выделением. <br/>
        /// Типовые устройства в действии наследуемого объекта могут быть отвязаны. <br/>
        /// После установки типовых устройств, в наследуемых объектах обновляются 
        /// номера объектов в соотвествии с технологическим номером тех. объекта.
        /// </remarks>
        private void UpdateModesManager(object sender)
        {
            InheritedTechObjects.ForEach(to => to.ModesManager.UpdateOnGenericTechObject(ModesManager));
            InheritedTechObjects.ForEach(to => to.ModifyDevNames(null, -1));
        }

        /// <summary>
        /// Обновить поле "Оборудование"
        /// </summary>
        private void UpdateEquipment(object sender)
        {
            InheritedTechObjects.ForEach(to => to.Equipment.UpdateOnGenericTechObject(equipment));
            InheritedTechObjects.ForEach(to => to.Equipment.ModifyDevNames());
        }

        /// <summary>
        /// Обновить поле "Параметры"
        /// </summary>
        private void UpdateParamsManager(object sender)
        {
            InheritedTechObjects.ForEach(to => to.GetParamsManager().UpdateOnGenericTechObject(this.paramsManager));
        }

        /// <summary>
        /// Обновить основные данные объекта: тип, имя, имя объекта Monitor,
        /// ОУ, время совместного перехода шагов
        /// </summary>
        private void UpdateTechObjectsData(object sender)
        {
            InheritedTechObjects.ForEach(to => to.UpdateOnGenericTechObject(this));
        }

        /// <summary>
        /// Создать экземпляр тех. объекта
        /// </summary>
        public TechObject CreateTechObject(BaseObject BaseObject,
            ITechObjectManager techObjectManager)
        {
            var techObject = new TechObject(this.Name,
                BaseObject.GetTechObjectLocalNum,
                techObjectManager.GetNewTechNumber(this.TechType),
                this.TechType, this.NameEplan, this.CooperParamNumber,
                this.NameBC, this.AttachedObjects.Value, this.BaseTechObject);

            techObject.GenericTechObject = this;
            BaseObject.AddObject(techObject);
            inheritedTechObjects.Add(techObject);
            return techObject;
        }

        /// <summary>
        /// 
        /// </summary>
        public TechObject SetUpTechObject(TechObject techObject, BaseObject baseObject)
        {
            techObject.GenericTechObject = this;
            inheritedTechObjects.Add(techObject);
            return techObject;
        }

        #region implementation ITreeViewItems

        public override string[] DisplayText
            => new string[] { $"{Name} [#{GlobalNum}]", string.Empty };

        public override ImageIndexEnum ImageIndex 
            => ImageIndexEnum.GenericTechObject;

        public override bool IsInsertable => false;

        public override bool IsReplaceable => false;

        public override bool IsDeletable => false;

        public override bool IsCopyable => false;

        public override bool IsInsertableCopy => false;

        public override bool IsMoveable => false;

        public override bool IsMainObject => false;
        #endregion

        /// <summary>
        /// Группа типового объекта
        /// </summary>
        public GenericGroup GenericGroup { get; set; } = null;

        public override int GlobalNum => TechObjectManagerInstance.GetGenericObjectN(this);

        public List<TechObject> InheritedTechObjects => inheritedTechObjects;

        /// <summary>
        /// Объекты в группе, созданные на основе типового объекта
        /// </summary>
        private readonly List<TechObject> inheritedTechObjects = new List<TechObject>();
    }
}
