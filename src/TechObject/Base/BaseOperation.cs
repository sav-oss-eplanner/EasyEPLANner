using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Aga.Controls.Tree;
using Editor;

namespace TechObject
{
    /// <summary>
    /// Базовая операция технологического объекта
    /// </summary>
    /// <remarks>
    /// 
    /// Изначально, базовая операция одна, иницилизируется из базового описания. <br/>
    /// Когда в техобъекта создается операция и ей присваивается
    /// базовая операция, тогда создается ее <see cref="Clone(Mode)">Копия</see>. <br/>
    ///
    /// Помимо первого клонирования, клонирование почему-то происходит второй раз в
    /// <see cref="Init(string, Mode)"/>...
    /// 
    /// <br/> <br/>
    /// 
    /// Сам объект базовой операции выступает полем "Доп. свойства", <br/>
    /// хотя и содержит другие операции.
    /// 
    /// <br/> <br/>
    /// 
    /// Содержит два свойства: <br/>
    /// <see cref="Properties"/> - список всех параметров <br/>
    /// <see cref="RootProperties"/> - список корневых параметров 
    /// для отображения параметров в виде иерархии <br/>
    /// Это понадобилос при изменении иерархии в параметрах, <br/>
    /// но сохранения старой логики работы с параметрами.
    /// </remarks>
    public interface IBaseOperation
    {
        /// <summary>
        /// Получить имя операции
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Получить Lua-имя операции
        /// </summary>
        string LuaName { get; set; }

        /// <summary>
        /// Получить свойства базовой операции
        /// </summary>
        List<BaseParameter> Properties { get; }

        /// <summary>
        /// Коренвые свойства базовой операции (поддержка вложенных параметров)
        /// </summary>
        List<BaseParameter> RootProperties { get; set; }

        /// <summary>
        /// Инициализация базовой операции по имени
        /// </summary>
        /// <param name="baseOperName">Имя операции</param>
        /// <param name="mode">Операция владелец</param>
        void Init(string baseOperName, Mode mode);

        /// <summary>
        /// Сбросить базовые шаги базовой операции
        /// </summary>
        void ResetOperationSteps();

        /// <summary>
        /// Копирование объекта
        /// </summary>
        /// <param name="owner">Новая операция-владелец объекта</param>
        /// <returns></returns>
        BaseOperation Clone(Mode owner);

        /// <summary>
        /// Сохранение в виде таблицы Lua
        /// </summary>
        /// <param name="prefix">Префикс (отступ)</param>
        /// <returns></returns>
        string SaveAsLuaTable(string prefix);

        void Synch(int[] array);

        /// <summary>
        /// Сброс доп.свойств привязанных агрегатов
        /// </summary>
        void SetExtraProperties(List<BaseParameter> properties);

        /// <summary>
        /// Установка свойств базовой операции
        /// </summary>
        /// <param name="extraParams">Свойства операции</param>
        void SetExtraProperties(ObjectProperty[] extraParams);

        /// <summary>
        /// Установить свойства базовой операции
        /// </summary>
        void SetExtraProperties(Dictionary<string, string> extraProperties);

        /// <summary>
        /// Установить свойства базовой операции на основе типового объекта
        /// </summary>
        void SetGenericExtraProperties(List<BaseParameter> properties);
    }

    /// <summary>
    /// Класс реализующий базовую операцию для технологического объекта
    /// </summary>
    public class BaseOperation : TreeViewItem , IBaseOperation, IAutocompletable
    {
        public BaseOperation(Mode owner)
        {
            Name = string.Empty;
            LuaName = string.Empty;
            states = new Dictionary<string, List<BaseStep>>();
            this.owner = owner;
        }

        /// <summary>
        /// Возвращает пустой объект - базовая операция.
        /// </summary>
        /// <returns></returns>
        public static BaseOperation EmptyOperation()
        {
            return new BaseOperation(string.Empty, string.Empty,
                new List<BaseParameter>(),
                new Dictionary<string, List<BaseStep>>());
        }

        /// <summary>
        /// Конструктор для инициализации базовой операции и параметров
        /// </summary>
        /// <param name="name">Имя операции</param>
        /// <param name="luaName">Lua имя операции</param>
        /// <param name="baseOperationProperties">Свойства операции</param>
        /// <param name="baseStates">Состояния операции с базовыми шагами</param>
        public BaseOperation(string name, string luaName, 
            List<BaseParameter> baseOperationProperties, 
            Dictionary<string, List<BaseStep>> baseStates)
        {
            Name = name;
            LuaName = luaName;
            states = baseStates;
            RootProperties = baseOperationProperties;

            foreach (var property in Properties ?? new List<BaseParameter>())
            {
                property.ValueChanged += (sender) => OnValueChanged(sender);
            }
        }

        /// <summary>
        /// Добавить базовый шаг
        /// </summary>
        /// <param name="stateTypeStr">Тип состояния</param>
        /// <param name="luaName">Lua-имя</param>
        /// <param name="name">Имя</param>
        /// <param name="defaultPosition">Стандартная позиция шага в 
        /// базовой операции.</param>
        public void AddStep(string stateTypeStr, string luaName, string name,
            int defaultPosition)
        {
            var step = new BaseStep(name, luaName, defaultPosition);
            step.Owner = this;

            if (states.ContainsKey(stateTypeStr))
            {
                states[stateTypeStr].Add(step);
            }
            else
            {
                var emptyStep = new BaseStep(string.Empty, string.Empty);
                emptyStep.Owner = this;
                
                var stepsList = new List<BaseStep>();
                stepsList.Add(emptyStep);
                stepsList.Add(step);

                states.Add(stateTypeStr, stepsList);
            }
        }

        public GroupableParameters AddGroupParameter(string luaName,
            string name, bool main, bool ignoreCompoundName)
        {
            var par = new GroupableParameters(luaName, name, main, ignoreCompoundName);
            InitParameter(par);
            return par;
        }

        /// <summary>
        /// Добавить активный параметр
        /// </summary>
        /// <param name="luaName">Lua-имя</param>
        /// <param name="name">Имя</param>
        /// <param name="defaultValue">Значение по-умолчанию</param>
        /// <returns>Добавленный параметр</returns>
        public ActiveParameter AddActiveParameter(string luaName, string name, 
            string defaultValue)
        {
            var par = new ActiveParameter(luaName, name, defaultValue);
            InitParameter(par);
            return par;
        }
        
        /// <summary>
        /// Добавить активный булевый параметр
        /// </summary>
        /// <param name="luaName">Lua-имя</param>
        /// <param name="name">Имя</param>
        /// <param name="defaultValue">Значение по-умолчанию</param>
        public void AddActiveBoolParameter(string luaName, string name,
            string defaultValue)
        {
            var par = new ActiveBoolParameter(luaName, name, defaultValue);
            InitParameter(par);
        }

        /// <summary>
        /// Инициализация добавленного из Lua параметра
        /// </summary>
        /// <param name="parameter"></param>
        private void InitParameter(BaseParameter parameter)
        {
            parameter.Owner = this;
            parameter.BaseOperation = this;
            Properties.Add(parameter);
            RootProperties.Add(parameter);
            parameter.ValueChanged += OnValueChanged;
        }

        public void AddFloatParameter(string luaName, string name, double defaultValue, string meter)
        {
            Parameters.Add(new BaseFloatParameter(luaName, name, defaultValue, meter));
        }

        public string Name
        {
            get => operationName;
            set => operationName = value;
        }

        public string LuaName
        {
            get => luaOperationName;
            set => luaOperationName = value;
        }

        /// <summary>
        /// Состояния базовой операции.
        /// Ключ - Lua-имя состояния (из перечисления).
        /// Значение - базовые шаги.
        /// </summary>
        public Dictionary<string, List<BaseStep>> States
        {
            get
            {
                return states;
            }
        }

        public List<BaseStep> GetStateBaseSteps(State.StateType stateType)
        {
            var steps = new List<BaseStep>();
            if (StateExist(stateType))
            {
                steps = states[$"{stateType}"];
            }

            return steps;
        }

        public List<string> GetStateStepsNames(State.StateType stateType)
        {
            var stepsNames = new List<string>();
            if (StateExist(stateType))
            {
                stepsNames = states[$"{stateType}"]
                    .Select(x => x.Name).ToList();
            }

            return stepsNames;
        }

        private bool StateExist(State.StateType stateType)
        {
            string stateTypeStr = stateType.ToString();
            return states.ContainsKey(stateTypeStr);
        }

        public void Init(string baseOperName, Mode mode)
        {
            TechObject techObject = owner.Owner.Owner;
            BaseTechObject baseTechObject = techObject.BaseTechObject;
            string baseTechObjectName = baseTechObject?.Name ?? string.Empty;

            if (baseTechObjectName != string.Empty)
            {
                BaseOperation operation;
                operation = techObject.BaseTechObject
                    .GetBaseOperationByName(baseOperName);
                if (operation == null)
                {
                    operation = techObject.BaseTechObject
                        .GetBaseOperationByLuaName(baseOperName);
                }

                if (operation != null)
                {
                    Name = operation.Name;
                    LuaName = operation.LuaName;
                    RootProperties = [.. operation.RootProperties.Select(x => x.Clone())];
                    Parameters = new List<IBaseFloatParameter>(operation.Parameters);
                    foreach(var property in Properties)
                    {
                        property.Owner = this;
                        property.Parent = this;
                    }

                    states = operation.States;
                    foreach(var state in states)
                    {
                        foreach(var step in states[state.Key])
                        {
                            step.Owner = this;
                        }
                    }

                    owner = mode;
                    if(mode.Name == Mode.DefaultModeName)
                    {
                        mode.SetNewValue(operation.Name);
                    }
                }
            }
            else
            {
                Name = string.Empty;
                LuaName = string.Empty;
                states = new Dictionary<string, List<BaseStep>>();
            }

            techObject.AttachedObjects.CheckInit();

            Properties.ForEach(prop => prop.ValueChanged += (sender) => OnValueChanged(sender));
        }

        public void ResetOperationSteps()
        {
            foreach (var state in owner.States)
            {
                foreach(var step in state.Steps)
                {
                    step.SetNewValue(string.Empty, true);
                }
            }
        }

        public string SaveAsLuaTable(string prefix)
        {
            var res = string.Empty;
            var propertiesCountForSave = Properties.Count();
            if (Properties == null || propertiesCountForSave <= 0)
            {
                return res;
            }

            string paramsForSave = string.Empty;
            foreach (var operParam in Properties)
            {
                if(!operParam.NeedDisable && !operParam.IsEmpty)
                {
                    paramsForSave += "\t" + prefix + operParam.LuaName +
                        " = \'" + operParam.Value + "\',\n";
                }
            }

            if (paramsForSave != string.Empty)
            {
                res += prefix + "props =\n" + prefix + "\t{\n";
                res += paramsForSave;
                res += prefix + "\t},\n";
            }

            return res;
        }

        public void SetExtraProperties(ObjectProperty[] extraParams)
        {
            SetExtraProperties(extraParams
                .ToDictionary(prop => prop.DisplayText[0], prop => prop.Value));
        }

        public void SetExtraProperties(List<BaseParameter> properties)
        {
            SetExtraProperties(properties.ToDictionary(prop => prop.LuaName, prop => prop.Value));
        }

        public void SetExtraProperties(Dictionary<string, string> extraProperties)
        {
            foreach (var property in Properties.Where(obj => extraProperties.ContainsKey(obj.LuaName)))
                property.SetNewValue(extraProperties[property.LuaName]);
        }

        public void SetGenericExtraProperties(List<BaseParameter> properties)
        {
            SetExtraProperties(properties.Where(property => property.IsFilled)
                .ToDictionary(prop => prop.LuaName, prop => prop.Value));
        }

        public List<BaseParameter> Properties => [.. RootProperties?.SelectMany(r => r.GetDescendants()) ?? []];

        public List<BaseParameter> RootProperties { get; set; } = [];

        public List<IBaseFloatParameter> Parameters { get; set; } = new List<IBaseFloatParameter>();

        public Mode Owner
        {
            get
            {
                return owner;
            }
        }

        /// <summary>
        /// Номер операции в списке операций объекта. Если 0 - не задано.
        /// </summary>
        public int DefaultPosition { get; set; } = 0;
       
        /// <summary>
        /// Добавить свойства базовой операции.
        /// </summary>
        /// <param name="properties">Массив свойств</param>
        /// <param name="owner">Объект первоначальный владелец свойств</param>
        public void AddProperties(List<BaseParameter> properties, object owner)
        {
            var main = (MainAggregateParameter)
                properties.Find(p => p is MainAggregateParameter);

            if (main is not null && !Properties.Any(p => p.LuaName == main.LuaName))
            {
                main = main.Clone() as MainAggregateParameter;
                main.Owner = owner;
                main.BaseOperation = this;
                main.Parent = this;
                Properties.Add(main);
                RootProperties.Add(main);
            }

            foreach (var property in properties)
            { 
                /// Если свойство с таким именем уже добавлено - пропускаем
                if (Properties.Any(x => x.LuaName == property.LuaName)
                    || property.LuaName == main?.LuaName)
                    continue;

                var newProperty = property.Clone();
                newProperty.Owner = owner;
                newProperty.BaseOperation = this;

                Properties.Add(newProperty);

                if (main is not null)
                {
                    main.Parameters.Add(newProperty);
                    newProperty.Parent = main;
                }
                else
                {
                    RootProperties.Add(newProperty);
                    newProperty.Parent = this;
                }

                newProperty.ValueChanged += OnValueChanged;
            }
        }

        /// <summary>
        /// Удалить свойства базовой операции.
        /// </summary>
        /// <param name="properties">Массив свойств</param>
        public void RemoveProperties(List<BaseParameter> properties)
        {
            foreach (var property in properties)
            {
                var deletingProperty = Properties
                    .Where(x => x.LuaName == property.LuaName)
                    .FirstOrDefault();
                if (deletingProperty != null)
                {
                    Properties.Remove(deletingProperty);
                    RootProperties.Remove(deletingProperty);
                }
            }
        }

        /// <summary>
        /// Изменить владельца свойств агрегата в аппарате.
        /// </summary>
        /// <param name="oldOwner">Старый базовый объект</param>
        /// <param name="newOwner">Новый базовый объект</param>
        public void ChangePropertiesOwner(BaseTechObject oldOwner,
            BaseTechObject newOwner)
        {
            foreach(var property in Properties)
            {
                if (property.Owner == oldOwner)
                {
                    property.Owner = newOwner;
                }
            }
        }

        /// <summary>
        /// Проверка базовой операции
        /// </summary>
        public string Check()
        {
            string errors = string.Empty;
            foreach (var property in Properties)
            {
                property.Check();

                bool notStub = !property.Value.ToLower()
                    .Contains(StaticHelper.CommonConst.StubForCells
                    .ToLower());
                if (notStub)
                {
                    CheckNotEmptyDisabledAggregateProperties(property,
                        ref errors);
                }
            }

            return errors;
        }

        /// <summary>
        /// Проверка пустых не отключенных параметров агрегатов
        /// </summary>
        /// <param name="property">Свойство</param>
        /// <param name="errors">Список ошибок</param>
        private void CheckNotEmptyDisabledAggregateProperties(
            BaseParameter property, ref string errors)
        {
            // Disabled устанавливается, когда был хотя бы раз запущен UI
            // редактора технологических объектов,
            // а если не был запущен, то надо смотреть NeedDisable.
            bool notEmptyDisabledAggregateProperty =
                property.Owner is BaseTechObject &&
                property.Disabled == false &&
                property.NeedDisable == false &&
                (property.Value == "");
            if (notEmptyDisabledAggregateProperty)
            {
                string modeName = owner.DisplayText[0];
                string techObjName = Owner.Owner.Owner.DisplayText[0];
                string message = $"Свойство \"{property.Name}\" в " +
                    $"операции \"{modeName}\", объекта \"{techObjName}\"" +
                    $" не заполнено.\n";
                errors += message;
            }
        }

        /// <summary>
        /// Копирование объекта
        /// </summary> 
        /// <returns></returns>
        public BaseOperation Clone(Mode owner)
        {
            var operation = EmptyOperation();

            operation.Name = operationName;
            operation.LuaName = luaOperationName;
            operation.owner = owner ?? Owner;
            operation.DefaultPosition = DefaultPosition;

            operation.RootProperties = CloneProperties(operation);

            operation.states = CloneStates(operation);
            operation.Parameters = new List<IBaseFloatParameter>(Parameters);

            Properties.ForEach(prop => prop.ValueChanged +=
                (sender) => OnValueChanged(sender));

            return operation;
        }

        public BaseOperation Clone() => Clone(null);

        /// <summary>
        /// Копирование доп. свойств базовой операции
        /// </summary>
        /// <param name="newOwner">Новая базовая операция-владелец</param>
        /// <returns></returns>
        private List<BaseParameter> CloneProperties(BaseOperation newOwner)
        {
            var properties = new List<BaseParameter>();

            foreach (BaseParameter oldProperty in RootProperties)
            {
                BaseParameter newProperty = oldProperty.Clone();
                newProperty.BaseOperation = this;
                if (oldProperty.Owner is BaseTechObject obj && obj.IsAttachable)
                {
                    // Если параметр пренадлежит привязанному агрегату,
                    // то заменяем объекта-владелец на новый базовый объект
                    // привязанного агрегата (Необходимо в случае копирования операции в другой объект)
                    newProperty.Owner = newOwner.Owner.TechObject
                        .AttachedObjects.GetBaseTechObjectByLuaName(obj.EplanName);
                }
                else
                {
                    newProperty.Owner = newOwner;
                }
                
                if (newProperty.Owner is not null)
                    properties.Add(newProperty);
            }

            return properties;
        }

        /// <summary>
        /// Копирование состояний и базовых шагов
        /// </summary>
        /// <param name="newOwner">Новая операция-владелец</param>
        /// <returns></returns>
        private Dictionary<string, List<BaseStep>> CloneStates(
            BaseOperation newOwner)
        {
            var clonedStates = new Dictionary<string, List<BaseStep>>();
            foreach (var state in States)
            {
                var clonedSteps = new List<BaseStep>();
                List<BaseStep> steps = state.Value;
                foreach (var step in steps)
                {
                    BaseStep clonedStep = step.Clone();
                    clonedStep.Owner = newOwner;
                    clonedSteps.Add(clonedStep);
                }

                clonedStates.Add(state.Key, clonedSteps);
            }

            return clonedStates;
        }

        #region синхронизация устройств
        public void Synch(int[] array)
        {
            foreach(var property in Properties)
            {
                property.Synch(array);
            }
        }
        #endregion

        #region Реализация ITreeViewItem
        override public string[] DisplayText
        {
            get
            {
                int visiblePropertiesCount = GetVisiblePropertiesCount();
                return
                [
                    $"Доп. свойства" +
                        (visiblePropertiesCount > 0 ?
                        $" ({visiblePropertiesCount})" : ""),
                    string.Empty
                ];
            }
        }

        private int GetVisiblePropertiesCount()
        {
            var properties = Properties;
            var visibleProperties = new HashSet<BaseParameter>();
            AddVisibleParameters(RootProperties, visibleProperties);

            return properties.Count(visibleProperties.Contains);
        }

        private static void AddVisibleParameters(
            IEnumerable<ITreeViewItem> items,
            ISet<BaseParameter> visibleProperties)
        {
            foreach (var item in items.Where(item => item.Visibility))
            {
                if (item is BaseParameter parameter)
                    visibleProperties.Add(parameter);

                if (item.Items is { } children)
                    AddVisibleParameters(children, visibleProperties);
            }
        }

        public override bool IsCopyable => true;

        public override bool IsInsertableCopy => true;

        /// <summary>
        /// Вставка значений доп. свойств базовой операции:
        /// заполнение доп. свойтсв по Lua-имени
        /// </summary>
        /// <param name="obj">Доп. свойства</param>
        /// <returns></returns>
        public override ITreeViewItem InsertCopy(object obj)
        {
            if (obj is BaseOperation baseOperation)
            {
                SetExtraProperties(baseOperation.Properties);
            }

            return this;
        }

        override public ITreeViewItem[] Items => [.. RootProperties];

        public override bool Delete(object child)
        {
            if (child is ActiveParameter)
            {
                var property = child as ActiveParameter;
                property.SetNewValue(string.Empty);
                return true;
            }
            return false;
        }
        #endregion

        bool IAutocompletable.CanExecute => true;

        public void Autocomplete()
        {
            Properties.OfType<IAutocompletable>()
                .Where(i => i.CanExecute)
                .ToList()
                .ForEach(i => i.Autocomplete());
        }

        private string operationName;
        private string luaOperationName;
        private Dictionary<string, List<BaseStep>> states;

        private Mode owner;
    }
}
