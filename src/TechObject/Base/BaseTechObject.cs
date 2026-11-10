using System.Collections.Generic;
using System.Linq;

namespace TechObject
{
    /// <summary>
    /// Класс реализующий базовый аппарат для технологического объекта
    /// </summary>
    public class BaseTechObject
    {
        public BaseTechObject(TechObject owner = null)
        {
            Name = string.Empty;
            EplanName = string.Empty;
            S88Level = 0;
            BaseOperations = new List<BaseOperation>();
            BasicName = string.Empty;
            Owner = owner;
            Equipment = new List<BaseParameter>();
            AggregateParameters = new List<BaseParameter>();
            BindingName = string.Empty;
            SystemParams = new SystemParams();
            ParamsManager = new ParamsManager();
            LuaModuleName = string.Empty;
            MonitorName = string.Empty;
            TechType = 2;
            DefaultNameEplan = string.Empty;
            BaseProperties = new BaseProperties();
            Deprecated = false;
            DenyBindingToUnit = false;
            IsSingleInstance = false;

            objectGroups = new List<AttachedObjects>();
        }

        /// <summary>
        /// Добавить оборудование в базовый объект
        /// </summary>
        /// <param name="luaName">Lua-имя</param>
        /// <param name="name">Имя</param>
        /// <param name="defaultValue">Значение по-умолчанию</param>
        public BaseParameter AddEquipment(string luaName, string name,
            string defaultValue)
        {
            var equipment = new EquipmentParameter(luaName, name, defaultValue)
            {
                Owner = this
            };
            Equipment.Add(equipment);

            return equipment;
        }


        /// <summary>
        /// Добавить активный параметр агрегата
        /// </summary>
        /// <param name="luaName">Lua-имя</param>
        /// <param name="name">Имя</param>
        /// <param name="defaultValue">Значение по-умолчанию</param>
        /// <returns>Добавленный параметр</returns>
        public ActiveParameter AddActiveParameter(string luaName, string name,
            string defaultValue)
        {
            var par = new ActiveParameter(luaName, name, defaultValue);
            par.Owner = this;
            AggregateParameters.Add(par);
            return par;
        }

        /// <summary>
        /// Добавить активный параметр агрегата с привязкой 
        /// </summary>
        /// <param name="luaName">Lua-имя/param>
        /// <param name="name">Имя</param>
        /// <returns>Добавленный параметр</returns>
        public ActiveAggregateParameter AddActiveAggregateParameter(string luaName, string name)
        {
            var parameter = new ActiveAggregateParameter(luaName, name, "",
                new List<BaseParameter.DisplayObject>() { BaseParameter.DisplayObject.Parameters })
            {
                Owner = this
            };

            AggregateParameters.Add(parameter);
            return parameter;
        }

        /// <summary>
        /// Добавить активный булевый параметр агрегата
        /// </summary>
        /// <param name="luaName">Lua-имя</param>
        /// <param name="name">Имя</param>
        /// <param name="defaultValue">Значение по-умолчанию</param>
        public void AddActiveBoolParameter(string luaName, string name,
            string defaultValue)
        {
            var par = new ActiveBoolParameter(luaName, name, defaultValue);
            par.Owner = this;
            AggregateParameters.Add(par);
        }

        /// <summary>
        /// Добавить главный параметр агрегата
        /// </summary>
        /// <param name="luaName"></param>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        public MainAggregateParameter AddMainAggregateParameter(string luaName, string name,
            string defaultValue)
        {
            var par = new MainAggregateParameter(luaName, name, defaultValue)
            {
                Owner = this
            };
            MainAggregateParameter = par;

            return par;
        }

        /// <summary>
        /// Добавить базовую операцию
        /// </summary>
        /// <param name="luaName">Lua-имя</param>
        /// <param name="name">Имя</param>
        /// <param name="defaultPosition">Стандартная позиция операции при
        /// автоматической настройке</param>
        /// <returns></returns>
        public BaseOperation AddBaseOperation(string luaName, string name,
            int defaultPosition)
        {
            if (BaseOperations.Count == 0)
            {
                // Пустой объект, если не должно быть выбрано никаких объектов
                BaseOperations.Add(BaseOperation.EmptyOperation());
            }

            var operation = BaseOperation.EmptyOperation();
            operation.LuaName = luaName;
            operation.Name = name;
            operation.DefaultPosition = defaultPosition;
            BaseOperations.Add(operation);

            return operation;
        }

        /// <summary>
        /// Добавить группу объектов в объект
        /// </summary>
        /// <param name="luaName">Lua-имя группы</param>
        /// <param name="name">Отображаемое имя группы</param>
        /// <param name="allowedObjects">Разрешенные типы объектов для
        /// добавления в группу</param>
        public void AddObjectGroup(string luaName, string name,
            string allowedObjects)
        {
            AttachedObjects newGroup = MakeObjectGroup(luaName, name,
                allowedObjects);
            if (newGroup != null)
            {
                objectGroups.Add(newGroup);
            }
        }

        /// <summary>
        /// Добавить системный параметр объекта
        /// </summary>
        /// <param name="luaName">Lua-имя</param>
        /// <param name="name">Отображаемое название</param>
        /// <param name="value">Значение</param>
        /// <param name="meter">Единица измерения</param>
        public void AddSystemParameter(string luaName, string name,
            double value, string meter)
        {
            var param = new SystemParam(SystemParams.GetIdx, name, value,
                meter, luaName);
            SystemParams.AddParam(param);
        }

        /// <summary>
        /// Добавить float параметр объекта
        /// </summary>
        /// <param name="luaName">Lua-имя</param>
        /// <param name="name">Отображаемое название</param>
        /// <param name="value">Значение</param>
        /// <param name="meter">Единица измерения</param>
        public void AddFloatParameter(string luaName, string name, double value,
            string meter)
        {
            ParamsManager.AddFloatParam(name, value, meter, luaName);
        }

        /// <summary>
        /// Добавить float runtime параметр объекта
        /// </summary>
        /// <param name="luaName">Lua-имя</param>
        /// <param name="name">Отображаемое название</param>
        /// <param name="meter">Единица измерения</param>
        public void AddFloatRuntimeParameter(string luaName, string name, string meter)
        {
            ParamsManager.AddFloatRuntimeParam(name, 0, meter, luaName);
        }

        /// <summary>
        /// Создать объект группы объектов.
        /// </summary>
        /// <param name="luaName">Lua-имя группы</param>
        /// <param name="name">Отображаемое имя группы</param>
        /// <param name="allowedObjects">Разрешенные типы объектов
        /// для добавления в группу</param>
        /// <returns></returns>
        private AttachedObjects MakeObjectGroup(string luaName, string name,
            string allowedObjects)
        {
            List<BaseTechObjectManager.ObjectType> allowedObjectsList;
            switch (allowedObjects)
            {
                case "all":
                    allowedObjectsList =
                        new List<BaseTechObjectManager.ObjectType>()
                        {
                            BaseTechObjectManager.ObjectType.Aggregate,
                            BaseTechObjectManager.ObjectType.Unit,
                            BaseTechObjectManager.ObjectType.UserObject
                        };

                    return new AttachedObjects(string.Empty, null,
                        new AttachedObjectStrategy.AttachedWithoutInitStrategy(
                            name, luaName, allowedObjectsList));

                case "units":
                    allowedObjectsList =
                        new List<BaseTechObjectManager.ObjectType>()
                        {
                            BaseTechObjectManager.ObjectType.Unit
                        };

                    return new AttachedObjects(string.Empty, null,
                        new AttachedObjectStrategy.AttachedWithoutInitStrategy(
                            name, luaName, allowedObjectsList));

                case "aggregates":
                    allowedObjectsList =
                        new List<BaseTechObjectManager.ObjectType>()
                        {
                            BaseTechObjectManager.ObjectType.Aggregate,
                        };

                    return new AttachedObjects(string.Empty, null,
                        new AttachedObjectStrategy.AttachedWithoutInitStrategy(
                            name, luaName, allowedObjectsList));

                default:
                    // Default value - aggregate.
                    allowedObjectsList =
                        new List<BaseTechObjectManager.ObjectType>()
                        {
                            BaseTechObjectManager.ObjectType.Aggregate,
                        };
                    return new AttachedObjects(string.Empty, null,
                        new AttachedObjectStrategy.AttachedWithoutInitStrategy(
                            name, luaName, allowedObjectsList));
            }
        }

        /// <summary>
        /// Имя базового объекта.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Lua-имя (ключ) базового объекта в файле описания.
        /// </summary>
        public string EplanName { get; set; }

        /// <summary>
        /// Уровень по S88 иерархии
        /// </summary>
        public int S88Level { get; set; }

        /// <summary>
        /// Базовые операции объекта
        /// </summary>
        public List<BaseOperation> BaseOperations { get; set; }

        /// <summary>
        /// Имя объекта для базовой функциональности
        /// </summary>
        public string BasicName { get; set; }

        /// <summary>
        /// Владелец объекта.
        /// </summary>
        public TechObject Owner { get; set; }

        /// <summary>
        /// Оборудование базового объекта
        /// </summary>
        public List<BaseParameter> Equipment { get; set; }

        /// <summary>
        /// Получить базовую операцию по имени.
        /// </summary>
        /// <param name="name">Имя</param>
        /// <returns></returns>
        public BaseOperation GetBaseOperationByName(string name)
        {
            var operation = BaseOperations.Where(x => x.Name == name)
                .FirstOrDefault();
            return operation;
        }

        /// <summary>
        /// Получить базовую операцию по Lua-имени
        /// </summary>
        /// <param name="luaName">Lua-имя</param>
        /// <returns></returns>
        public BaseOperation GetBaseOperationByLuaName(string luaName)
        {
            var operation = BaseOperations.Where(x => x.LuaName == luaName)
                .FirstOrDefault();
            return operation;
        }

        /// <summary>
        /// Список операций базового объекта
        /// </summary>
        /// <returns></returns>
        public List<string> BaseOperationsList
        {
            get
            {
                return BaseOperations.Select(x => x.Name).ToList();
            }
        }

        /// <summary>
        /// Копия объекта
        /// </summary>
        /// <param name="techObject">Копируемый объект</param>
        /// <returns></returns>
        public BaseTechObject Clone(TechObject techObject)
        {
            var cloned = Clone();
            cloned.Owner = techObject;
            foreach(var clonedObjectGroup in cloned.objectGroups)
            {
                clonedObjectGroup.Owner = techObject;
            }
            return cloned;
        }

        /// <summary>
        /// Копия объекта
        /// </summary>
        /// <returns></returns>
        public BaseTechObject Clone()
        {
            var cloned = new BaseTechObject(Owner);
            cloned.Name = Name;

            var aggregateParameters = new List<BaseParameter>();
            foreach (var aggrPar in AggregateParameters)
            {
                aggregateParameters.Add(aggrPar.Clone());
            }
            cloned.AggregateParameters = aggregateParameters;
            if (MainAggregateParameter != null)
            {
                cloned.MainAggregateParameter = MainAggregateParameter.Clone()
                    as MainAggregateParameter;
            }

            var baseOperations = new List<BaseOperation>();
            foreach (var baseOperation in BaseOperations)
            {
                baseOperations.Add(baseOperation.Clone());
            }
            cloned.BaseOperations = baseOperations;

            cloned.BasicName = BasicName;
            cloned.EplanName = EplanName;
            cloned.DefaultNameEplan = DefaultNameEplan;

            var equipment = new List<BaseParameter>();
            foreach (var equip in Equipment)
            {
                var newEquip = equip.Clone();
                newEquip.Owner = this;
                equipment.Add(newEquip);
            }
            cloned.Equipment = equipment;

            cloned.S88Level = S88Level;
            cloned.BindingName = BindingName;
            cloned.IsPID = IsPID;

            cloned.objectGroups = new List<AttachedObjects>();
            foreach(var objectGroup in objectGroups)
            {
                var clonedStrategy = new AttachedObjectStrategy
                    .AttachedWithoutInitStrategy(
                    objectGroup.WorkStrategy.Name,
                    objectGroup.WorkStrategy.LuaName,
                    objectGroup.WorkStrategy.AllowedObjects);
                var clonedGroup = new AttachedObjects(objectGroup.Value,
                    objectGroup.Owner, clonedStrategy);
                cloned.objectGroups.Add(clonedGroup);
            }

            cloned.SystemParams = SystemParams.Clone();
            cloned.ParamsManager = ParamsManager.Clone();
            cloned.LuaModuleName = LuaModuleName;
            cloned.MonitorName = MonitorName;
            cloned.TechType = TechType;
            cloned.BaseProperties = BaseProperties.Clone();

            cloned.DenyBindingToUnit = DenyBindingToUnit;
            cloned.IsSingleInstance = IsSingleInstance;

            return cloned;
        }

        /// <summary>
        /// Является ли базовый объект привязываемым к другому объекту.
        /// </summary>
        public virtual bool IsAttachable
        {
            get
            {
                bool isAttachable = UseGroups ||
                    S88Level == (int)BaseTechObjectManager.ObjectType.Unit ||
                    S88Level == (int)BaseTechObjectManager.ObjectType.Aggregate;
                return isAttachable;
            }
        }

        /// <summary>
        /// Параметры объекта, как агрегата (добавляемые в аппарат).
        /// </summary>
        public List<BaseParameter> AggregateParameters
        {
            get
            {
                if (aggregateProperties == null)
                {
                    return new List<BaseParameter>();
                }
                else
                {
                    return aggregateProperties;
                }
            }

            set
            {
                aggregateProperties = value;
            }
        }

        /// <summary>
        /// Имя агрегата при его привязке к аппарату.
        /// </summary>
        public string BindingName { get; set; }

        /// <summary>
        /// Главный параметр агрегата
        /// </summary>
        public MainAggregateParameter MainAggregateParameter { get; set; }

        /// <summary>
        /// Является ли объект ПИД-регулятором
        /// </summary>
        public bool IsPID { get; set; } = default;

        /// <summary>
        /// Использовать ли группы объектов
        /// </summary>
        public bool UseGroups
        {
            get
            {
                return ObjectGroupsList.Count > 0;
            }
        }

        /// <summary>
        /// Группа объектов
        /// </summary>
        public List<AttachedObjects> ObjectGroupsList
        {
            get => objectGroups;
        }

        /// <summary>
        /// Системные параметры объекта
        /// </summary>
        public SystemParams SystemParams { get; set; }

        /// <summary>
        /// Параметры объекта
        /// </summary>
        public ParamsManager ParamsManager { get; set; }

        /// <summary>
        /// Lua-имя модуля в котором содержится описание логики объекта
        /// </summary>
        public string LuaModuleName { get; set; }

        /// <summary>
        /// Имя объекта Monitor (SCADA)
        /// </summary>
        public string MonitorName { get; set; }

        /// <summary>
        /// ОУ по умолчанию при добавлении технологического объекта.
        /// </summary>
        public string DefaultNameEplan { get; set; }

        /// <summary>
        /// Технологический тип объекта по умолчанию
        /// </summary>
        public int TechType { get; set; }

        /// <summary>
        /// Базовые свойства объекта
        /// </summary>
        public BaseProperties BaseProperties { get; set; }

        public bool Deprecated { get; set; }

        /// <summary>
        /// Агрегату разрешена привязка к аппаратам
        /// </summary>
        public bool DenyBindingToUnit { get; set; }

        /// <summary>
        /// Разрешить только один экземпляр объекта в проекте.
        /// Если true, второй экземпляр того же базового объекта
        /// создавать нельзя. Задаётся в lua-описании базового
        /// объекта полем isSingleInstance.
        /// </summary>
        public bool IsSingleInstance { get; set; }

        private List<BaseParameter> aggregateProperties;
        private List<AttachedObjects> objectGroups;
    }
}
