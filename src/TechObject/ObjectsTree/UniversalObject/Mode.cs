using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EasyEPlanner;
using Editor;

namespace TechObject
{
    /// <summary>
    /// Операция технологического объекта. Состоит из последовательно 
    /// (или в ином порядке) выполняемых шагов.
    /// </summary>
    public interface IMode : ITreeViewItem
    {
        /// <summary>
        /// Операции тех.объекта
        /// </summary>
        ModesManager Owner { get; }

        /// <summary>
        /// Тех.объект, которому принадлежит операция
        /// </summary>
        TechObject TechObject { get; }

        /// <summary>
        /// Базовая операция
        /// </summary>
        BaseOperation BaseOperation { get; }
        
        /// <summary>
        /// Название операции
        /// </summary>
        string Name { get; }
    }


    /// <summary>
    /// Операция технологического объекта. Состоит из последовательно 
    /// (или в ином порядке) выполняемых шагов.
    /// </summary>
    public class Mode : TreeViewItem, IMode, IAutocompletable
    {
        /// <summary>
        /// Получение состояния номеру (нумерация с 0).
        /// </summary>
        /// <param name="stateTypeIndex">Номер состояния.</param>        
        /// <returns>Состояние с заданным номером.</returns>
        public State this[int stateTypeIndex]
        {
            get
            {
                return stepsMngr.FirstOrDefault(state => (int)state.Type == stateTypeIndex);
            }
        }

        /// <summary>
        /// Создание новой операции.
        /// </summary>
        /// <param name="name">Имя операции.</param>
        /// <param name="getN">Функция получения номера операции.</param>
        /// <param name="newOwner">Владелец операции (Менеджер операций)
        /// </param>
        public Mode(string name, GetN getN, ModesManager newOwner,
            IBaseOperation baseOperation = null)
        {
            this.name = name;
            this.getN = getN;
            this.owner = newOwner;

            restrictionMngr = new RestrictionManager();

            stepsMngr = new List<State>();

            foreach (State.StateType state in State.GetOrderedStates())
            {
                switch (state)
                {

                    case State.StateType.IDLE:
                    case State.StateType.RUN:
                        stepsMngr.Add(new State(state, this, true));
                        break;
                    default:
                        stepsMngr.Add(new State(state, this));
                        break;
                }
            }

            foreach (var state in stepsMngr)
            {
                state.ValueChanged += (sender) => OnValueChanged(sender);
            }

            operPar = new OperationParams();

            // Экземпляр класса базовой операции
            this.baseOperation = baseOperation ?? new BaseOperation(this);

            if (this.baseOperation is ITreeViewItem itviBaseOperation)
                itviBaseOperation.ValueChanged += (sender) => OnValueChanged(sender);
        }

        public OperationParams GetOperationParams()
        {
            return operPar;
        }

        public Mode Clone(GetN getN, ModesManager newOwner, string name = "")
        {
            Mode clone = (Mode)MemberwiseClone();
            clone.getN = getN;
            clone.owner = newOwner;
            clone.baseOperation = baseOperation.Clone(clone);

            if (name != string.Empty)
            {
                clone.name = name;
            }

            clone.stepsMngr = new List<State>();
            for (int idx = 0; idx < stepsMngr.Count; idx++)
            {
                clone.stepsMngr.Add(stepsMngr[idx].Clone(clone));
            }

            clone.operPar = operPar.Clone(clone);

            clone.restrictionMngr = restrictionMngr.Clone();

            clone.States.ForEach(state => state.ValueChanged += sender => clone.OnValueChanged(sender));
            clone.stepsMngr.ForEach(step => step.ValueChanged += sender => clone.OnValueChanged(sender));

            return clone;
        }

        public void ModifyDevNames(IDevModifyOptions options)
        {
            BaseOperation.Properties.ForEach(p => p.ModifyDevNames(options));
            stepsMngr.ForEach(state => state.ModifyDevNames(options));
        }

        /// <summary>
        /// Сохранение в виде таблицы Lua.
        /// </summary>
        /// <param name="prefix">Префикс (для выравнивания).</param>
        /// <returns>Описание в виде таблицы Lua.</returns>
        public string SaveAsLuaTable(string prefix)
        {
            var resBuilder = new StringBuilder();

            string base_operation_str = (string.IsNullOrEmpty(baseOperation.LuaName)) ? string.Empty :
                $"{prefix}base_operation = '{baseOperation.LuaName}',\n";

            resBuilder.Append($"{prefix}{{\n")
                .Append($"{prefix}name = \'{name}\',\n")
                .Append(base_operation_str)
                .Append(baseOperation.SaveAsLuaTable(prefix));

            var statesBuilder = new StringBuilder();
            foreach (State state in stepsMngr)
            {
                var stateLuaTable = state.SaveAsLuaTable(prefix + "\t\t");
                int stateTypeNumber = (int)state.Type;
                if (!string.IsNullOrEmpty(stateLuaTable))
                {
                    statesBuilder.Append($"{prefix}\t[ {stateTypeNumber} ] =\n")
                        .Append($"{prefix}\t\t{{\n")
                        .Append(stateLuaTable)
                        .Append($"{prefix}\t\t}},\n");
                }
            }
            if (statesBuilder.Length > 0)
            {
                resBuilder.Append($"{prefix}states =\n")
                    .Append($"{prefix}\t{{\n")
                    .Append(statesBuilder)
                    .Append($"{prefix}\t}},\n");
            }
            resBuilder.Append($"{prefix}}},\n");
           
            return resBuilder.ToString();
        }

        /// <summary>
        /// Добавление нового шага.
        /// </summary>
        /// <param name="stateN">Номер(тип) состояния</param>
        /// <param name="stepName">Имя шага.</param>
        /// <param name="baseStepLuaName">Имя базового шага</param>
        public void AddStep(int stateN, string stepName, 
            string baseStepLuaName = "")
        {
            stepsMngr.FirstOrDefault(step => (int)step.Type == stateN)
                ?.AddStep(stepName, baseStepLuaName);
        }

        #region Синхронизация устройств в объекте.
        public void Synch(int[] array)
        {
            foreach (State stpsMngr in stepsMngr)
            {
                stpsMngr.Synch(array);
            }

            baseOperation.Synch(array);
        }
        #endregion


        /// <summary>
        /// Сохранение в виде таблицы Lua.
        /// </summary>
        /// <param name="prefix">Префикс (для выравнивания).</param>
        /// <returns>Описание в виде таблицы Lua.</returns>
        public string SaveRestrictionAsLua(string prefix)
        {
            string res = string.Empty;
            string tmp = string.Empty;
            tmp += restrictionMngr.SaveRestrictionAsLua(prefix);
            if (tmp != string.Empty)
            {
                res += prefix + "{\n" + tmp + prefix + "},\n";
            }
            return res;
        }

        public RestrictionManager GetRestrictionManager()
        {
            return restrictionMngr;
        }

        /// <summary>
        /// Функция установки ограничений для операции
        /// </summary>
        /// <param name="luaName">Lua имя объекта</param>
        /// <param name="value">Новая строка ограничений</param>
        public void SetRestriction(string luaName, string value)
        {
            foreach (Restriction restrict in restrictionMngr.Restrictions)
            {
                if (restrict.LuaName == luaName)
                {
                    restrict.SetNewValue(value);
                }
            }
        }

        /// <summary>
        /// Функция установки ограничений для операции
        /// </summary>
        /// <param name="luaName">Lua имя объекта</param>
        /// <param name="value">Новая строка ограничений</param>
        public void AddRestriction(string luaName, int ObjNum, int ModeNum)
        {
            if (owner?.Owner is GenericTechObject)
                ObjNum = 0;
            foreach (Restriction restrict in restrictionMngr.Restrictions)
            {
                if (restrict.LuaName == luaName)
                {
                    restrict.AddRestriction(ObjNum, ModeNum);
                }
            }
        }

        /// <summary>
        /// Функция удаления ограничения для операции
        /// </summary>
        public void DelRestriction(string luaName, int ObjNum, int ModeNum)
        {
            foreach (Restriction restrict in restrictionMngr.Restrictions)
            {
                if (restrict.LuaName == luaName)
                {
                    restrict.DelRestriction(ObjNum, ModeNum);
                }
            }
        }

        /// <summary>
        /// Функция для сортировки ограничений после считывания из файла
        /// </summary>
        public void SortRestriction()
        {
            foreach (Restriction restrict in restrictionMngr.Restrictions)
            {
                restrict.SortRestriction();
            }
        }

        /// <summary>
        /// Функция для задания номеров родителей ограничений
        /// </summary>
        public void SetRestrictionOwner(int objNum, int modeNum)
        {
            foreach (Restriction restrict in restrictionMngr.Restrictions)
            {
                restrict.SetRestrictionOwner(objNum, modeNum);
            }
        }

        public void CheckRestriction(int prev, int curr)
        {
            restrictionMngr.CheckRestriction(prev, curr);
        }

        public void ModifyRestrictObj(int oldObjN, int newObjN)
        {

            restrictionMngr.ModifyRestrictObj(oldObjN, newObjN);
        }

        public void ChangeModeNum(TechObject techObject, int prev, int curr)
        {
            restrictionMngr.ChangeModeNum(techObject, prev, curr);
        }

        public void ChangeCrossRestriction(Mode oldMode = null)
        {
            if (oldMode == null)
            {
                restrictionMngr.ChangeCrossRestriction();
            }
            else
            {
                restrictionMngr.ChangeCrossRestriction(oldMode
                    .GetRestrictionManager());
            }
        }

        /// <summary>
        /// Проверить, выбрана ли такая базовая операция или нет
        /// </summary>
        /// <param name="baseOperationName">Проверяемое имя базовой операции
        /// </param>
        /// <returns></returns>
        private bool CheckTheSameBaseOperations(string baseOperationName)
        {
            var objectAlreadyContainsThisOperation = false;
            var modes = this.owner;

            foreach(var mode in modes.Modes)
            {
                if ((mode.BaseOperation.Name == baseOperationName ||
                    mode.BaseOperation.LuaName == baseOperationName) &&
                    baseOperationName != string.Empty)
                {
                    objectAlreadyContainsThisOperation = true;
                }
            }
            
            return objectAlreadyContainsThisOperation;
        }

        // Установка параметров базовой операции
        public void SetBaseOperExtraParams(ObjectProperty[] extraParams)
        {
            baseOperation.SetExtraProperties(extraParams);
        }

        /// <summary>
        /// Получить базовую операцию для этой операции
        /// </summary>
        public BaseOperation BaseOperation => baseOperation as BaseOperation;

        // Получение номера операции
        public int GetModeNumber()
        {
            return getN(this);
        }

        public ModesManager Owner => owner;

        public TechObject TechObject => Owner.TechObject;

        public string Name
        {
            get { return name; }
        }

        public List<State> States
        {
            get
            {
                return stepsMngr;
            }
        }

        /// <summary>
        /// Проверка состояний состоящих из шагов
        /// </summary>
        /// <returns>Строка с ошибками</returns>
        public string Check()
        {
            var errors = string.Empty;
            List<State> stepsManager = stepsMngr;

            foreach (State state in stepsManager)
            {
                errors += state.Check();
            }

            errors += BaseOperation.Check();

            return errors;
        }

        /// <summary>
        /// Очистка базовой операции
        /// </summary>
        public void ClearBaseOperation()
        {
            this.SetNewValue(string.Empty, true);
        }

        #region Реализация ITreeViewItem
        override public string[] DisplayText
        {
            get
            {
                string res = getN(this) + ". " + name;

                return new string[] { res, baseOperation.Name };
            }
        }

        override public ITreeViewItem[] Items =>
        [
            ..stepsMngr.Where(s => !s.Empty),
            operPar,
            restrictionMngr,
            .. (List<ITreeViewItem>)(NeedBaseOperation ? [baseOperation as BaseOperation] : [])
        ];

        private bool NeedBaseOperation =>
            baseOperation.Name != string.Empty &&
            baseOperation.LuaName != string.Empty &&
            baseOperation.Properties.Count > 0; 

        override public bool SetNewValue(string newName)
        {
            name = newName;
            OnValueChanged(this);
            return true;
        }

        public override bool SetNewValue(string newBaseOperationName, 
            bool isBaseOper)
        {
            var reset = DialogResult.Yes;

            if (baseOperation.Name.Equals(newBaseOperationName) ||
                CheckTheSameBaseOperations(newBaseOperationName) is true)
                return false;

            List<BaseParameter> CloneExtraProperties = null;

            if (TechObjectEditor.Editable is true &&
                !string.IsNullOrEmpty(newBaseOperationName) &&
                !string.IsNullOrEmpty(baseOperation.Name))
            {
                reset = TechObjectEditor.DialogResetExtraProperties();
                if (reset is DialogResult.Cancel)
                    return false;
                if (reset is DialogResult.No)
                    CloneExtraProperties = new List<BaseParameter>(
                        baseOperation.Properties);
            }


            if (reset == DialogResult.Yes)
            {
                baseOperation.ResetOperationSteps();
            }

            // Инициализация базовой операции по имени
            baseOperation.Init(newBaseOperationName, this);

            if (CloneExtraProperties != null)
                baseOperation.SetExtraProperties(CloneExtraProperties);

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
                return new string[] { name, baseOperation.Name };
            }
        }

        override public bool IsDeletable
        {
            get
            {
                return true;
            }
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

        override public ITreeViewItem Replace(object child, object copyObject)
        {
            var selectedState = child as State;
            var copiedState = copyObject as State;
            bool statesNotNull = selectedState != null && copiedState != null;
            if (statesNotNull)
            {
                State newState = copiedState.Clone(this);
                if (newState.Name != selectedState.Name)
                {
                    newState.Name = selectedState.Name;
                }
                int index = stepsMngr.IndexOf(selectedState);
                stepsMngr.Remove(selectedState);
                stepsMngr.Insert(index, newState);

                newState.AddParent(this);
                return newState;
            }

            var selectesRestrMan = child as RestrictionManager;
            var copiedRestrMan = copyObject as RestrictionManager;
            bool restrictionNotNull =
                selectesRestrMan != null && copiedRestrMan != null;
            if (restrictionNotNull)
            {
                for (int i = 0; i < selectesRestrMan.Restrictions.Count; i++)
                {
                    selectesRestrMan.Replace(selectesRestrMan.Items[i],
                        copiedRestrMan.Items[i]);
                }

                int objNum = owner.Owner.GlobalNum;
                int modeNum = getN(this);

                foreach (var restrict in selectesRestrMan.Restrictions)
                {
                    restrict.SetRestrictionOwner(objNum, modeNum);
                }

                selectesRestrMan.AddParent(this);
                return selectesRestrMan;
            }

            return null;
        }

        override public bool IsCopyable
        {
            get
            {
                return true;
            }
        }

        override public bool IsDrawOnEplanPage
        {
            get
            {
                return true;
            }
        }

        override public List<DrawInfo> GetObjectToDrawOnEplanPage()
        => DrawInfo.Filter([.. States.SelectMany(a => a.GetObjectToDrawOnEplanPage())]);

        public override IEnumerable<string> BaseObjectsList
        {
            get => Owner.Owner.BaseTechObject.BaseOperationsList
                .Except(from operation in Owner.Modes
                        where operation.BaseOperation.Name != string.Empty && operation != this
                        select operation.BaseOperation.Name);
        }

        public override bool ContainsBaseObject
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

        public override bool NeedRebuildParent
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
                return ImageIndexEnum.Mode;
            }
        }

        public override bool IsMode
        {
            get
            {
                return true;
            }
        }
        #endregion

        public void SetUpFromBaseTechObject(BaseOperation baseOperation)
        {
            bool setBaseOperation = true;
            SetNewValue(baseOperation.Name, setBaseOperation);

            foreach(var state in States)
            {
                var stateSteps = baseOperation
                    .GetStateBaseSteps(state.Type)
                    .Where(x => x.DefaultPosition > 0)
                    .OrderBy(x => x.DefaultPosition);
                foreach(var baseStep in stateSteps)
                {
                    while(state.Steps.Count < baseStep.DefaultPosition)
                    {
                        state.Insert();
                    }

                    var newStep = (Step)state.Insert();
                    newStep.SetUpFromBaseTechObject(baseStep);
                }
            }
        }

        public override void UpdateOnGenericTechObject(ITreeViewItem genericObject)
        {
            if (genericObject is null)
            {
                States.ForEach(state => state.UpdateOnGenericTechObject(null));
                return;
            }

            var genericMode = genericObject as Mode;
            if (genericMode is null)
                return;

            foreach (var stateIndex in Enumerable.Range(0, genericMode.States.Count))
            {

                var genericState = genericMode.States[stateIndex];
                var state = this.States[stateIndex];

                if (genericMode.States[stateIndex] is null || state is null)
                    continue;

                state.UpdateOnGenericTechObject(genericState);
            }

            baseOperation.SetGenericExtraProperties(genericMode.BaseOperation.Properties);
        }

        public override void CreateGenericByTechObjects(IEnumerable<ITreeViewItem> itemList)
        {
            var modes = itemList.Cast<Mode>().ToList();

            foreach (int stateIndex in Enum.GetValues(typeof(State.StateType)))
            {
                this[stateIndex].CreateGenericByTechObjects(modes.Select(mode => mode[stateIndex]));
            }
        }

        public override void UpdateOnDeleteGeneric()
        {
            States.ForEach(state => state.UpdateOnDeleteGeneric());
        }

        bool IAutocompletable.CanExecute => true;

        public void Autocomplete()
        {
            var paramsManager = Owner.Owner.GetParamsManager();

            paramsManager.Float.FillWithStubs();

            paramsManager.AutocompleteByOperation(this);
            (baseOperation as IAutocompletable)?.Autocomplete();

            paramsManager.Float.FillWithStubs();
        }

        public override bool IsInsertable => true;

        public override ITreeViewItem Insert(IDialogFactory dialogFactory)
        {
            var dialog = dialogFactory.GetStatesCreatorDialog();

            if (dialog.ShowDialog(this) is DialogResult.OK)
            {
                return CreateStateStep(dialog.Result);
            }

            return null;
        }

        public ITreeViewItem CreateStateStep(State.StateType state)
            => this[(int)state].Insert();

        public static Editor.IEditor TechObjectEditor { get; set; } = Editor.Editor.GetInstance();

        private GetN getN;

        private string name;           /// Имя операции.
        private List<State> stepsMngr;/// Список шагов операции для состояний.
        private RestrictionManager restrictionMngr;

        private OperationParams operPar;

        private ModesManager owner;

        private IBaseOperation baseOperation; /// Базовая операция

        public const string DefaultModeName = "Новая операция";
    }
}
