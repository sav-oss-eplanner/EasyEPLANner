using LuaInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using Editor;
using System.Text.RegularExpressions;
using System.Text;
using Spire.Pdf.Exporting.XPS.Schema.Mc;
using System.ComponentModel;
using System.Windows.Forms;

namespace TechObject
{
    /// <summary>
    /// Технологический объект проекта (танк, гребенка).
    /// </summary>
    public class TechObject : TreeViewItem
    {
        /// <summary>
        /// Класс для обозначения устройства (ОУ) в Eplan'е. При изменении
        /// также меняются названия устройств, участвующих в операциях объекта.
        /// </summary>
        public class NameInEplan : ObjectProperty
        {
            /// <param name="nameEplan">Обозначение устройства.</param>
            /// <param name="owner">Технологический объект-родитель.</param>
            public NameInEplan(string nameEplan, TechObject owner)
                : base("OУ", nameEplan)
            {
                this.owner = owner;
                Parent = owner;
            }

            /// <summary>
            /// При изменении обозначения устройства (ОУ) также меняются
            /// названия устройств, участвующих в операциях объекта.
            /// </summary>
            public override bool SetNewValue(string newValue)
            {
                newValue = newValue.ToUpper();
                newValue = Regex.Replace(newValue,
                    StaticHelper.CommonConst.RusAsEngPattern,
                    StaticHelper.CommonConst.RusAsEngEvaluator);

                var oldValue = Value.ToString();

                base.SetNewValue(newValue);
                owner.CompareEplanNames();

                owner.ModifyDevNames(oldValue, null);
                
                return true;
            }

            public override bool NeedRebuildParent
            {
                get { return true; }
            }

            private TechObject owner;
        }

        /// <summary>
        /// Класс внутреннего номера технологического объекта
        /// </summary>
        protected class TechObjectN : ObjectProperty
        {
            public TechObjectN(TechObject techObject, int value)
                : base("Номер", techObject.TechNumber.ToString())
            {
                this.techObject = techObject;
                this.Parent = techObject;

                SetValue(value);
            }

            public override bool IsDeletable
            {
                get
                {
                    return false;
                }
            }

            public override bool SetNewValue(string newValue)
            {
                const int minimalNumber = 0;
                int oldNumber = Convert.ToInt32(EditText[1]);
                bool validNewNum = int.TryParse(newValue, out int newNumber);
                if (validNewNum && newNumber > minimalNumber)
                {
                    bool res = base.SetNewValue(newValue);
                    if (res)
                    {
                        techObject.ModifyDevNames(null, oldNumber);
                    }
                    return true;
                }

                return false;
            }

            public override bool NeedRebuildParent
            {
                get { return true; }
            }

            private TechObject techObject;
        }

        /// <summary>
        /// Сохранение в виде таблицы Lua.
        /// </summary>
        /// <param name="prefix">Префикс (для выравнивания).</param>
        /// <param name="globalNum">Глобальный номер объекта</param>
        /// <returns>Описание в виде таблицы Lua.</returns>
        public string SaveAsLuaTable(string prefix, int globalNum,
            string exportAttachedObjectsRefs = null)
        {
            string baseObjectName = "";
            if (baseTechObject != null)
            {
                baseObjectName = baseTechObject.EplanName;
            }

            string base_tech_object_str = (string.IsNullOrEmpty(baseObjectName)) ? string.Empty :
                $"{prefix}base_tech_object = '{baseObjectName}',\n";
            string attached_objects_str = (string.IsNullOrEmpty(AttachedObjects.Value)) ? string.Empty :
                $"{prefix}{AttachedObjects.WorkStrategy.LuaName} = '{AttachedObjects.Value}',\n";
            string attached_objects_refs_str = string.IsNullOrEmpty(exportAttachedObjectsRefs) ?
                string.Empty :
                $"{prefix}{AttachedObjectReferences.LuaFieldName} = '{exportAttachedObjectsRefs}',\n";

            StringBuilder resultBuilder = new StringBuilder();
            resultBuilder.Append($"\t[ {globalNum} ] =\n")
                .Append(prefix).Append($"{{\n")
                .Append(prefix).Append($"n          = {TechNumber},\n")
                .Append(prefix).Append($"tech_type  = {TechType},\n")
                .Append(prefix).Append($"name       = '{name}',\n")
                .Append(prefix).Append($"name_eplan = '{NameEplan}',\n")
                .Append(prefix).Append($"name_BC    = '{NameBC}',\n")
                .Append(prefix).Append($"cooper_param_number = {CooperParamNumber},\n")
                .Append(base_tech_object_str)
                .Append(attached_objects_str)
                .Append(attached_objects_refs_str);

            if (GenericTechObject != null)
            {
                int genericTechObjectIndex = TechObjectManagerInstance.GenericTechObjects.IndexOf(GenericTechObject) + 1;
                resultBuilder.Append($"{prefix}generic_tech_object = {genericTechObjectIndex},\n");
            }

            if (baseTechObject != null &&
                baseTechObject.ObjectGroupsList.Count > 0)
            {
                string objectGroups = string.Empty;
                foreach (var objectGroup in baseTechObject.ObjectGroupsList)
                {
                    if (objectGroup.Value == string.Empty)
                    {
                        continue;
                    }

                    objectGroups += prefix + prefix +
                        objectGroup.WorkStrategy.LuaName + " = \'" +
                        objectGroup.Value + "\',\n";
                }

                if (objectGroups != string.Empty)
                {
                    resultBuilder.Append(prefix).Append("tank_groups =\n")
                        .Append(prefix).Append("\t{\n")
                        .Append(objectGroups)
                        .Append(prefix).Append("\t},\n");
                }
            }

            resultBuilder.Append(paramsManager.SaveAsLuaTable(prefix))
                .Append(systemParams.SaveAsLuaTable(prefix))
                .Append(baseProperties.SaveAsLuaTable(prefix))
                .Append("\n")
                .Append(modes.SaveAsLuaTable(prefix))
                .Append(equipment.SaveAsLuaTable(prefix))
                .Append(prefix)
                .Append("},\n");

            return resultBuilder.ToString();
        }

        /// <summary>
        /// Сохранение в виде таблицы Lua.
        /// </summary>
        /// <param name="prefix">Префикс (для выравнивания).</param>
        /// <param name="globalNum">Глобальный номер объекта</param>
        /// <returns>Описание в виде таблицы Lua.</returns>
        public string SaveRestrictionAsLua(string prefix, int globalNum)
        {
            string res = "";
            string tmp = "";
            string comment = $"\t\t--{this.Name} {this.TechNumber}";

            tmp += modes.SaveRestrictionAsLua(prefix);
            if (tmp != "")
            {
                res += prefix + "[ " + globalNum + " ] =" + comment + "\n" +
                    tmp + "\n";
            }

            return res;
        }

        /// <summary>
        /// Создание технологического объекта.
        /// </summary>
        /// <param name="name">Имя.</param>
        /// <param name="getN">Функция получения порядкового локального номера.
        /// </param>
        /// <param name="technologicalNumber">Технологический номер.</param>
        /// <param name="nameEplan">ОУ объекта в Eplan'е.</param>
        /// <param name="cooperParamNumber">Время совместного перехода шагов 
        /// (параметр).</param>
        /// <param name="attachedObjects">Привязанные объекты</param>
        /// <param name="baseTechObject">Базовый технологический объект</param>
        /// <param name="NameBC">Имя объекта Monitor</param>
        /// <param name="techType">Номер типа</param>
        public TechObject(string name, GetN getLocalNum,
            int technologicalNumber, int techType, string nameEplan,
            int cooperParamNumber, string NameBC, string attachedObjects,
            BaseTechObject baseTechObject)
        {
            this.name = name;
            this.getLocalNum = getLocalNum;

            this.techNumber = new TechObjectN(this, technologicalNumber);
            this.techType = new ObjectProperty("Тип", techType, -1);
            this.techType.ValueChanged += sender => OnValueChanged(sender);

            this.nameBC = new ObjectProperty("Имя объекта Monitor",
                NameBC);
            this.nameBC.ValueChanged += sender => OnValueChanged(sender);

            this.nameEplan = new NameInEplan(nameEplan, this);
            this.nameEplan.ValueChanged += sender => OnValueChanged(sender);

            this.cooperParamNumber = new ObjectProperty(
                "Время совместного перехода шагов (параметр)",
                cooperParamNumber, -1);
            this.cooperParamNumber.ValueChanged += sender => OnValueChanged(sender);

            var allowedObjects = new List<BaseTechObjectManager.ObjectType>()
            {
                BaseTechObjectManager.ObjectType.Aggregate
            };
            string attachObjectsName = "Привязанные агрегаты";
            string attachObjectsLuaName = "attached_objects";
            this.attachedObjects = new AttachedObjects(attachedObjects,
                this, new AttachedObjectStrategy.AttachedWithInitStrategy(
                    attachObjectsName, attachObjectsLuaName, allowedObjects));

            modes = new ModesManager(this);

            paramsManager = new ParamsManager();
            paramsManager.Parent = this;

            systemParams = new SystemParams();
            systemParams.Parent = this;

            equipment = new Equipment(this);

            baseProperties = new BaseProperties();

            InitBaseTechObject(baseTechObject);
            SetItems();
        }

        /// <summary>
        /// Инициализация базового объекта
        /// </summary>
        /// <param name="baseTechObject">Базовый объект</param>
        public void InitBaseTechObject(BaseTechObject baseTechObject)
        {
            if (baseTechObject != null)
            {
                this.baseTechObject = baseTechObject.Clone(this);

                systemParams
                    .SetUpFromBaseTechObject(this.baseTechObject.SystemParams);
                equipment.AddItems(this.baseTechObject.Equipment);
                baseProperties = this.baseTechObject.BaseProperties.Clone();
            }

            // Установили новое значение, произошла смена базового объекта
            // Надо сравнить ОУ и изменить его, если требуется
            CompareEplanNames();
            SetItems();
        }

        /// <summary>
        /// Автоматическая настройка технологического объекта на основе его 
        /// базового объекта.
        /// </summary>
        public void SetUpFromBaseTechObject()
        {
            if (BaseTechObject == null)
            {
                return;
            }

            nameBC.SetNewValue(BaseTechObject.MonitorName);
            nameEplan.SetNewValue(BaseTechObject.DefaultNameEplan);
            techType.SetNewValue(BaseTechObject.TechType.ToString());

            systemParams.SetUpFromBaseTechObject(BaseTechObject.SystemParams);
            ModesManager.SetUpFromBaseTechObject(BaseTechObject);
            paramsManager.SetUpFromBaseTechObject(BaseTechObject.ParamsManager);
        }

        public void UpdateOnGenericTechObject(GenericTechObject genericTechObject)
        {            
            name = genericTechObject.Name;

            if (genericTechObject.techType.Value != "-1")
                techType.SetNewValue(genericTechObject.techType.Value);
            if (genericTechObject.nameBC.IsFilled)
                nameBC.SetNewValue(genericTechObject.NameBC + TechNumber);
            if (genericTechObject.nameEplan.IsFilled)
                nameEplan.SetNewValue(genericTechObject.NameEplan);
            if (genericTechObject.cooperParamNumber.IsFilled)
                cooperParamNumber.SetNewValue(genericTechObject.cooperParamNumber.Value);
        }



        /// <summary>
        /// Сравнение имен Eplan базового тех. объекта с текущим.
        /// </summary>
        /// <param name="baseTechObject">Базовый объект</param>
        public void CompareEplanNames()
        {
            // Если не выбран базовый объект, то пустое имя
            // Если выбран, то сравниваем имена
            if (baseTechObject != null && baseTechObject.S88Level >= 0)
            {
                string baseObjectNameEplan = baseTechObject.EplanName
                    .ToLower();
                string thisObjectNameEplan = NameEplan.ToLower();
                // Если тех. объект не содержит базовое ОУ, то добавить его.
                if (thisObjectNameEplan.Contains(baseObjectNameEplan) == false)
                {
                    NameEplanForFile = baseTechObject.EplanName + "_" +
                        NameEplan;
                }
                else
                {
                    NameEplanForFile = NameEplan;
                }
            }
            else
            {
                NameEplanForFile = string.Empty;
            }
        }

        public ITechObjectIdentifyData IdentifyData => 
            new TechObjectIdentifyData(TechNumber, TechType, NameEplan, NameBC);

        public TechObject Clone(GetN getLocalNum, int oldGlobalNum, int newGlobalNum) => 
            Clone(getLocalNum, oldGlobalNum, newGlobalNum, IdentifyData);

        public TechObject Clone(GetN getLocalNum, int newTechNumber, int oldGlobalNum, int newGlobalNum) =>
            Clone(getLocalNum, oldGlobalNum, newGlobalNum, IdentifyData.WithTechNumber(newTechNumber));

        public TechObject Clone(GetN getLocalNum, int oldGlobalNum, int newGlobalNum,
            ITechObjectIdentifyData dataSource)
        {
            TechObject clone = (TechObject)MemberwiseClone();

            clone.techNumber = new TechObjectN(clone, dataSource.TechNumber);
            clone.techType = new ObjectProperty("Тип", dataSource.TechType);
            clone.nameBC = new ObjectProperty("Имя объекта Monitor", dataSource.NameBC);
            clone.nameEplan = new NameInEplan(dataSource.NameEplan, clone);

            clone.attachedObjects = new AttachedObjects(AttachedObjects.Value,
                clone, AttachedObjects.WorkStrategy);
            clone.cooperParamNumber = cooperParamNumber.Clone();

            clone.getLocalNum = getLocalNum;

            if (baseTechObject != null)
            {
                clone.baseTechObject = baseTechObject.Clone(clone);
            }

            clone.paramsManager = paramsManager.Clone();

            clone.systemParams = systemParams.Clone();

            clone.modes = modes.Clone(clone);
            clone.modes.ModifyDevNames(new DevModifyOptions(clone, NameEplan, TechNumber));

            clone.modes.ModifyRestrictObj(oldGlobalNum, newGlobalNum);

            clone.equipment = equipment.Clone(clone);
            clone.equipment.ModifyDevNames();

            clone.baseProperties = baseProperties.Clone();

            clone.SetItems();
            return clone;
        }

        protected void SetItems()
        {
            var itemsList = new List<ITreeViewItem>
            {
                techNumber,
                techType,
                nameEplan,
                nameBC,
            };

            if (attachedObjects.IsEditable == true)
            {
                itemsList.Add(attachedObjects);
            }

            itemsList.Add(cooperParamNumber); // ??
            itemsList.Add(modes);
            itemsList.Add(paramsManager);

            if (baseTechObject?.SystemParams.Count > 0)
            {
                itemsList.Add(systemParams);
            }

            if (baseTechObject?.Equipment.Count > 0)
            {
                itemsList.Add(equipment);
            }

            if (baseTechObject?.UseGroups == true)
            {
                itemsList.AddRange(baseTechObject.ObjectGroupsList);
            }

            if (baseTechObject?.BaseProperties.Count > 0)
            {
                itemsList.Add(baseProperties);
            }

            items = itemsList;
        }

        #region Установить свойства объектов и добавить их по необходимости
        /// <summary>
        /// Добавление операции.
        /// </summary>
        /// <param name="modeName">Имя операции.</param>
        /// <param name="baseOperationName">Имя базовой операции.</param>
        /// <param name="operExtraParams">Параметры базовой операции</param>
        /// <returns>Добавленная операция.</returns>
        public Mode AddMode(string modeName, string baseOperationName,
            LuaTable operExtraParams)
        {
            if (operExtraParams.Keys.Count > 0)
            {
                var extraParams = StaticHelper.LuaHelper
                    .ConvertLuaTableToCArray(operExtraParams);
                return modes.AddMode(modeName, baseOperationName, extraParams);
            }
            else
            {
                return modes.AddMode(modeName, baseOperationName);
            }
        }

        /// <summary>
        /// Добавить элемент оборудования
        /// </summary>
        /// <param name="equipmentName">Имя</param>
        /// <param name="value">Значение</param>
        public void SetEquipment(string equipmentName, string value)
        {
            equipment.SetEquipmentValue(equipmentName, value);
        }

        /// <summary>
        /// Добавить объекты группы объектов
        /// </summary>
        /// <param name="luaName">Lua-имя группы</param>
        /// <param name="value">Значение</param>
        public void SetGroupObject(string luaName, string value)
        {
            if (baseTechObject?.UseGroups == true)
            {
                var foundGroup = baseTechObject.ObjectGroupsList
                    .Where(x => x.WorkStrategy.LuaName == luaName)
                    .FirstOrDefault();
                if (foundGroup != null)
                {
                    foundGroup.SetValue(value);
                }
            }
        }

        /// <summary>
        /// Установить значение системных параметров
        /// </summary>
        /// <param name="luaName">Lua-имя параметра</param>
        /// <param name="value">Значение параметра</param>
        public void SetSystemParameter(string luaName, string value)
        {
            if (baseTechObject?.SystemParams.Count > 0)
            {
                var foundParam = systemParams.GetParam(luaName);
                foundParam.Value.SetNewValue(value);
            }
        }

        /// <summary>
        /// Установить значение свойств объекта
        /// </summary>
        /// <param name="luaName">Lua-имя параметра</param>
        /// <param name="value">Значение параметра</param>
        public void SetBaseProperty(string luaName, string value)
        {
            if (baseTechObject.BaseProperties.Count > 0)
            {
                var foundProperty = baseProperties.GetProperty(luaName);
                if (foundProperty != null)
                {
                    foundProperty.SetValue(value);
                }
            }
        }
        #endregion

        // Получение операции. 
        public Mode GetMode(int i)
        {
            if (modes.Modes != null)
            {
                if (modes.Modes.Count > i)
                {
                    return modes.Modes[i];
                }
            }
            return null;
        }

        public void ModifyDevNames(string oldTechObjectName, int? oldTechObjectNumber)
        {
            modes.ModifyDevNames(new DevModifyOptions(this, oldTechObjectName ?? NameEplan, oldTechObjectNumber ?? TechNumber));
            equipment.ModifyDevNames();
        }

        /// <summary>
        /// Получение менеджера параметров.
        /// </summary>
        /// <returns>Менеджер параметров</returns>
        public ParamsManager GetParamsManager()
        {
            return paramsManager;
        }

        public int TechNumber
        {
            get
            {
                if (techNumber == null)
                {
                    return 0;
                }

                try
                {
                    return Convert.ToInt32(techNumber.EditText[1]);
                }
                catch (Exception)
                {
                    return 0;
                }
            }

            set
            {
                techNumber.SetNewValue(value.ToString());
            }
        }

        public int TechType
        {
            get
            {
                return Convert.ToInt32(techType.EditText[1]);
            }
        }

        /// <summary>
        /// Переносимые ссылки на привязанные агрегаты (base:n).
        /// Используются только при экспорте/импорте объектов.
        /// </summary>
        public string AttachedObjectsRefs { get; set; } = string.Empty;

        /// <summary>
        /// Привязанные к аппарату агрегаты.
        /// </summary>
        public AttachedObjects AttachedObjects
        {
            get
            {
                return attachedObjects;
            }
        }

        public string NameBC
        {
            get
            {
                return Convert.ToString(nameBC.EditText[1]);
            }
        }

        /// <summary>
        /// Получение ОУ объекта в Eplan'е.
        /// </summary>
        public string NameEplan
        {
            get
            {
                return nameEplan.EditText[1];
            }
        }

        private NameInEplan nameEplan; /// ОУ объекта в Eplan'е.

        /// <summary>
        /// Получение номера параметра со временем переходного включения 
        /// операций.
        /// </summary>
        public int CooperParamNumber
        {
            get
            {
                return Convert.ToInt32(cooperParamNumber.EditText[1]);
            }
        }

        public ModesManager ModesManager
        {
            get
            {
                return modes;
            }
        }

        #region Синхронизация устройств в объекте.
        public void Synch(int[] array)
        {
            modes.Synch(array);
            equipment.Synch(array);
            baseProperties.Synch(array);
        }
        #endregion

        public void CheckRestriction(int prev, int curr)
        {
            modes.CheckRestriction(prev, curr);
        }

        public void ChangeCrossRestriction(TechObject oldObject = null)
        {
            if (oldObject != null)
            {
                modes.ChangeCrossRestriction(oldObject.ModesManager);
            }
            else
            {
                modes.ChangeCrossRestriction();
            }
        }

        public void ChangeModeNum(TechObject techObject, int prev, int curr)
        {
            modes.ChangeModeNum(techObject, prev, curr);
        }

        public void SetRestrictionOwner()
        {
            modes.SetRestrictionOwner();
        }

        /// <summary>
        /// ОУ для файла main.prg.lua
        /// </summary>
        public string NameEplanForFile { get; set; }

        /// <summary>
        /// Базовый технологический объект
        /// </summary>
        public BaseTechObject BaseTechObject
        {
            get
            {
                return baseTechObject;
            }
        }

        /// <summary>
        /// Номер параметра со временем совместного включения операций 
        /// для шагов.
        /// </summary>
        protected ObjectProperty cooperParamNumber;

        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Получить оборудование объекта.
        /// </summary>
        public Equipment Equipment
        {
            get
            {
                return equipment;
            }
        }

        #region Проверки внутри объекта
        /// <summary>
        /// Проверка операций технологического объекта
        /// </summary>
        /// <returns>Строка с ошибками</returns>
        public string Check()
        {
            var errors = string.Empty;
            string fullobjName = DisplayText[0];
            string shortObjName = EditText[0];

            errors += CheckObjectName(shortObjName, fullobjName);
            errors += CheckBaseTechObject(fullobjName);
            errors += CheckNameEplan(fullobjName);
            errors += CheckModes();
            errors += GetParamsManager().Check(fullobjName);

            return errors;
        }

        /// <summary>
        /// Проверка имени объекта
        /// </summary>
        /// <param name="shortName">Имя объекта без номера и доп. информации
        /// </param>
        /// <param name="fullName">Полное название с номером и доп. информацией
        /// </param>
        /// <returns></returns>
        private string CheckObjectName(string shortName, string fullName)
        {
            const int maxSize = 50;
            int objNameLength = shortName.Length;
            if (objNameLength > maxSize)
            {
                return $"Имя объекта \"{fullName}\" превышает " +
                    $"максимальный размер в {maxSize} символов. " +
                    $"Размер: {objNameLength}.\n";
            }

            return string.Empty;
        }

        /// <summary>
        /// Проверить базовый технологический объект и его зависимости.
        /// </summary>
        /// <param name="objName">Полное название объекта</param>
        /// <returns></returns>
        private string CheckBaseTechObject(string objName)
        {
            string errors = string.Empty;
            bool setBaseTechObj = BaseTechObject != null ? true : false;

            if (setBaseTechObj == false)
            {
                string msg = string.Format("Неопознанный объект - " +
                    "\"{0}\"\n", objName);
                errors += msg;
            }
            else
            {
                errors += Equipment.Check();
                errors += attachedObjects.CheckInit();
                errors += attachedObjects.CheckBindingToUnit();
            }

            return errors;
        }

        /// <summary>
        /// Проверить ОУ объекта
        /// </summary>
        /// <param name="objName">Полное название объекта</param>
        /// <returns></returns>
        private string CheckNameEplan(string objName)
        {
            if (nameEplan.Value == string.Empty)
            {
                return $"Не задано ОУ в объекте \"{objName}\"\n";
            }

            return string.Empty;
        }

        /// <summary>
        /// Проверить операции объекта
        /// </summary>
        /// <returns></returns>
        private string CheckModes()
        {
            string errors = string.Empty;
            ModesManager modesManager = ModesManager;
            List<Mode> modes = modesManager.Modes;
            foreach (var mode in modes)
            {
                errors += mode.Check();
            }

            return errors;
        }
        #endregion

        /// <summary>
        /// Установить делегат для поиска локального номера объекта;
        /// </summary>
        /// <param name="getLocalNumMethod">Метод</param>
        public void SetGetLocalN(GetN getLocalNumMethod)
        {
            getLocalNum = getLocalNumMethod;
        }

        #region Реализация ITreeViewItem
        override public string[] DisplayText
        {
            get
            {
                int localNum = 0;
                int globalNum = 0;
                if (getLocalNum == null)
                {
                    return new string[] {
                        localNum + ". " + name + " №" +
                        techNumber.EditText[ 1 ] + $" (#{globalNum})", "" };
                }
                else
                {
                    globalNum = TechObjectManagerInstance
                        .GetTechObjectN(this);
                    return new string[] {
                        getLocalNum( this ) + ". " + name + " №" +
                        techNumber.EditText[ 1 ] + $" (#{globalNum})", "" };
                }
            }
        }

        override public ITreeViewItem[] Items
        {
            get
            {
                return items.ToArray();
            }
        }

        override public bool SetNewValue(string newName)
        {
            name = newName;
            OnValueChanged(this);
            return true;
        }

        override public bool IsEditable
        {
            get
            {
                return true;
            }
        }

        override public int[] EditablePart
        {
            get
            {
                //Можем редактировать содержимое двух колонок.
                return new int[] { 0, 1 };
            }
        }

        override public string[] EditText
        {
            get
            {
                return new string[] { name, "" };
            }
        }

        override public bool IsDeletable
        {
            get
            {
                return true;
            }
        }

        public override bool Delete(object child)
        {
            ITreeViewItem item = child as ITreeViewItem;
            if (item != null)
            {
                item.Delete(this);
                return true;
            }

            return false;
        }

        override public bool IsMoveable
        {
            get
            {
                return true;
            }
        }

        override public bool IsReplaceable
        {
            get
            {
                return true;
            }
        }

        override public bool IsCopyable
        {
            get
            {
                return true;
            }
        }

        override public ITreeViewItem Replace(object child,
            object copyObject)
        {
            var pars = child as ParamsManager;
            var copyPars = copyObject as ParamsManager;
            bool parsNotNull = pars != null && copyPars != null;
            if (parsNotNull)
            {
                pars.Replace(pars, copyPars);

                pars.AddParent(this);
                return pars;
            }

            var equipment = child as Equipment;
            var copyEquipment = copyObject as Equipment;
            bool equipmentNotNull = equipment != null && copyEquipment != null;
            if (equipmentNotNull)
            {
                BaseParameter[] objEquips = [.. equipment.GetDescendantsParameters()];
                BaseParameter[] copyEquips = [.. copyEquipment.GetDescendantsParameters()];
                
                foreach (var objEquip in objEquips)
                {
                    objEquip.SetNewValue(copyEquips.FirstOrDefault(eq => eq.LuaName == objEquip.LuaName)?.Value ?? "");
                }
                equipment.ModifyDevNames();

                equipment.AddParent(this);
                return equipment;
            }

            if (child is AttachedObjects attachedObjectsTarget &&
                copyObject is AttachedObjects attachedObjectsCopy)
            {
                return attachedObjectsTarget.Replace(child, attachedObjectsCopy);
            }

            return null;
        }


        public override bool IsInsertable => GenericTechObject is null;

        /// <summary>
        /// Создать группу с типовым объектом на основе данного объекта.
        /// Объект переносится в созданную группу.
        /// </summary>
        public override ITreeViewItem Insert()
        {
            string message = "Создать группу с типовым объектом на основе текущего тех. объекта?";
            var showWarningResult = MessageBox.Show(message, "Внимание",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (showWarningResult == DialogResult.Yes && Parent is BaseObject baseObject)
            {
                return baseObject.CreateGenericGroup(this);
            }

            return null;
        }

        public override bool ContainsBaseObject
        {
            get
            {
                return true;
            }
        }

        public override bool IsMainObject
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

        public override ImageIndexEnum ImageIndex
        {
            get
            {
                return ImageIndexEnum.TechObject;
            }
        }

        public override List<ITreeViewItem> QuickMultiSelect()
        {
            return Parent?.Items?
                .Where(adjItem => (adjItem as TechObject)?.TechType == TechType 
                                  && !(adjItem is GenericTechObject)).ToList();
        }
        #endregion

        /// <summary>
        /// Сброс базового объекта
        /// </summary>
        public void ResetBaseTechObject()
        {
            ModesManager.ClearBaseOperations();
            systemParams.Clear();
            equipment.Clear();
            baseProperties.Clear();
            attachedObjects.SetNewValue(string.Empty);
            baseTechObject = null;
            CompareEplanNames();
            SetItems();
        }

        /// <summary>
        /// Удалить типовой объект
        /// </summary>
        public void RemoveGenericTechObject()
        {
            GenericTechObject = null;
            ModesManager.UpdateOnGenericTechObject(null);
        }

        public int GetLocalNum
        {
            get
            {
                return getLocalNum(this);
            }
        }

        public virtual int GlobalNum => TechObjectManagerInstance.GetTechObjectN(this);

        public BaseProperties BaseProperties
        {
            get
            {
                return baseProperties;
            }
        }

        public ITechObjectManager TechObjectManagerInstance { get; private set; }
            = TechObjectManager.GetInstance();

        public GenericTechObject GenericTechObject { get; set; }

        protected TechObjectN techNumber; /// Номер объекта технологический.
        protected ObjectProperty techType; /// Тип объекта технологический.

        protected string name; /// Имя объекта.
        protected ObjectProperty nameBC; /// Имя объекта в Monitor.
        protected ModesManager modes; /// Операции объекта.
        protected ParamsManager paramsManager; /// Менеджер параметров объекта.
        protected SystemParams systemParams; /// Системные параметры объекта.

        protected List<ITreeViewItem> items = new List<ITreeViewItem>(); /// Параметры объекта для редактирования.

        protected GetN getLocalNum; /// Делегат для функции поиска номера объекта.

        /// Базовый аппарат (технологический объект)
        protected BaseTechObject baseTechObject;
        protected AttachedObjects attachedObjects; // Привязанные агрегаты
        protected Equipment equipment; // Оборудование объекта
        protected BaseProperties baseProperties; // Доп. свойства объекта
    }
}
