using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Aga.Controls.Tree;
using Editor;

namespace TechObject
{
    /// <summary>
    /// Состояние операции. Содержит группу шагов, выполняемых последовательно
    /// (или в ином порядке).
    /// </summary>
    public interface IState : ITreeViewItem
    {
        /// <summary>
        /// Операция состояния
        /// </summary>
        IMode Owner { get; }

        /// <summary>
        /// Технологический объект, которому принадлежит состояние
        /// </summary>
        TechObject TechObject { get; }

        /// <summary>
        /// Тип состояния
        /// </summary>
        State.StateType Type { get; }

        /// <summary>
        /// Название
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Запускать состояние при старте программы
        /// </summary>
        bool IsRunPoint { get; }
        
        /// <summary>
        /// Установить состояние как точка запуска 
        /// (сбрасывает свойство у других состояний)
        /// </summary>
        void SetRunPoint();

        /// <summary>
        /// Сбросить состояние как точку запуска
        /// </summary>
        void ResetRunPoint();
    }

    /// <summary>
    /// Состояние операции. Содержит группу шагов, выполняемых последовательно
    /// (или в ином порядке).
    /// </summary>
    public class State : TreeViewItem, IState
    {
        /// <summary>
        /// Получение шага по номеру (нумерация с -1 - шаг операции, который 
        /// выполняется пока включена операция).
        /// </summary>
        /// <param name="idx">Номер шага.</param>        
        /// <returns>Шаг с заданным номером.</returns>
        public Step this[int idx]
        {
            get
            {
                CheckMainStep();

                if (steps.Count > 0)
                {
                    if (idx == -1)
                    {
                        return steps[0];
                    }

                    if (idx < steps.Count - 1)
                    {
                        return steps[idx + 1];
                    }
                }

                return null;
            }
        }

        public int GetStepN(object step)
        {
            return steps.IndexOf(step as Step);
        }

        /// <summary>
        /// Создание нового состояния.
        /// </summary>
        /// <param name="stateType">Тип состояния</param>
        /// <param name="needMainStep">Надо ли основной шаг.</param>
        /// <param name="owner">Владелец состояния (Операция)</param>
        public State(StateType stateType, IMode owner, bool needMainStep = false)
        {
            name = stateType.Name();
            Type = stateType;
            this.owner = owner;
            steps = new List<Step>();

            NeedMainStep = needMainStep;

            if (needMainStep)
            {
                modeStep = AddNewStepToItems(needMainStep);
                modeStep.AddParent(this);
            }
        }

        public State Clone(Mode newOwner, string name = "")
        {
            State clone = (State)MemberwiseClone();
            clone.Type = Type;

            if (name != string.Empty)
            {
                clone.name = name;
            }

            clone.owner = newOwner;
            clone.steps = new List<Step>();

            if (modeStep != null)
            {
                clone.modeStep = modeStep.Clone(clone, clone.GetStepN);
                clone.steps.Add(clone.modeStep);
            }

            for (int idx = 1; idx < steps.Count; idx++)
            {
                clone.steps.Add(steps[idx].Clone(clone, clone.GetStepN));
            }

            clone.Steps.ForEach(step => step.ValueChanged += (sender) => clone.OnValueChanged(sender));

            return clone;
        }

        public void ModifyDevNames(IDevModifyOptions options)
        {
            steps.ForEach(step => step.ModifyDevNames(options));
        }

        /// <summary>
        /// Сохранение в виде таблицы Lua.
        /// </summary>
        /// <param name="prefix">Префикс (для выравнивания).</param>
        /// <returns>Описание в виде таблицы Lua.</returns>
        public string SaveAsLuaTable(string prefix)
        {
            if (steps.Count == 0 || modeStep is null) 
                return string.Empty;

            string res = string.Empty;

            res += IsRunPoint ? prefix + "runPoint = true,\n" : "";
            res += modeStep.SaveAsLuaTable(prefix, true);

            if (steps.Count > 1)
            {
                int i = 1;
                res += prefix + "steps =\n" +
                    prefix + "\t{\n";

                for (int j = 1; j < steps.Count; j++)
                {
                    res += prefix + "\t[ " + i++ + " ] =\n";
                    res += steps[j].SaveAsLuaTable(prefix + "\t\t");
                }

                res += prefix + "\t},\n";
            }

            if (res != string.Empty)
            {
                res = prefix + "--\'" + name + "\'\n" + res;
            }

            return res;
        }


        /// <summary>
        /// Сохранениe действия "Во время операции" в таблицу Excel
        /// </summary>
        /// <returns>Описание в виде таблицы Excel</returns>
        public string[] SaveAsExcel()
        {
            var res = new List<string>();

            res.Add(name);
            foreach (IAction action in Steps.First().GetActions)
            {
                res.Add(action.SaveAsExcel());
            }

            return res.ToArray();
        }

        /// <summary>
        /// Добавление нового шага.
        /// </summary>
        /// <param name="stepName">Имя шага.</param>
        /// <param name="baseStepLuaName">Имя базового шага</param>
        public Step AddStep(string stepName, string baseStepLuaName)
        {
            CheckMainStep();

            Step newStep = AddNewStepToItems(false, stepName);
            newStep.AddParent(this);

            if (!string.IsNullOrEmpty(baseStepLuaName))
            {
                newStep.SetNewValue(baseStepLuaName, true);
            }
                
            newStep.ValueChanged += (sender) => OnValueChanged(sender);
            OnValueChanged(this);

            return newStep;
        }

        public List<Step> Steps
        {
            get
            {
                return steps;
            }
        }

        #region Синхронизация устройств в объекте.
        public void Synch(int[] array)
        {
            foreach (Step step in steps)
            {
                step.Synch(array);
            }
        }
        #endregion

        /// <summary>
        /// Удаление списка шагов.
        /// </summary>
        public void Clear()
        {
            steps.Clear();
            modeStep = null;
        }

        /// <summary>
        /// Имя состояния
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        public IMode Owner => owner;

        public TechObject TechObject => Owner.TechObject;

        /// <summary>
        /// Проверка шагов состояния
        /// </summary>
        /// <returns>Строка с ошибками</returns>
        public string Check()
        {
            var errors = string.Empty;
            List<Step> steps = Steps;

            foreach (Step step in steps)
            {
                errors += step.Check();
            }

            return errors;
        }

        /// <summary>
        /// Добавить новый шаг в состояние
        /// </summary>
        /// <param name="isMain">Главный шаг или нет (первый)</param>
        /// <param name="stepName">Имя шага (дефолт, если пустое)</param>
        /// <returns>Добавленный шаг</returns>
        private Step AddNewStepToItems(bool isMain = false,
            string stepName = "")
        {
            Step step;
            if (isMain)
            {
                step = new Step(Step.MainStepName, GetStepN, this, true);
            }
            else
            {
                string newStepName =
                    stepName == string.Empty ? Step.NewStepName : stepName;
                step = new Step(newStepName, GetStepN, this);
            }

            steps.Add(step);

            step.ValueChanged += (sender) => OnValueChanged(sender);
            OnValueChanged(this);

            return step;
        }

        #region Реализация ITreeViewItem
        override public string[] DisplayText => [
            $"{name}{(steps.Count > 1 ? $"({steps.Count - 1})" : "")}",
            IsRunPoint ? RUNPOINT : ""
            ];

        public override bool IsEditable => true;

        public override int[] EditablePart => [-1, 1];

        public override bool SetNewValue(string newValue, bool isExtraValue)
        {
            if (newValue is RUNPOINT && !IsRunPoint)
                SetRunPoint();

            if (newValue is "" && ReferenceEquals(Owner.Owner.RunPointState, this))
                ResetRunPoint();

            return true;
        }

        public bool IsRunPoint { get; private set; }

        public void SetRunPoint()
        {
            IsRunPoint = true;
            Owner.Owner.RunPointState?.ResetRunPoint();
            Owner.Owner.RunPointState = this;
        }

        public void ResetRunPoint()
        {
            IsRunPoint = false;
            Owner.Owner.RunPointState = null;
        }

        public override bool IsBoolParameter => true;

        public const string RUNPOINT = "Запускать при старте программы";

        public override IEnumerable<string> BaseObjectsList => [RUNPOINT, ""];

        public override ImageIndexEnum DescriptionImageIndex 
            => IsRunPoint ? ImageIndexEnum.Run : ImageIndexEnum.NONE;

        override public ITreeViewItem[] Items
        {
            get
            {
                return steps.ToArray();
            }
        }

        override public bool IsReplaceable
        {
            get
            {
                return true;
            }
        }

        override public bool Delete(object child)
        {
            var step = child as Step;
            if (step != null)
            {
                bool ignoreDeleting = steps.IndexOf(step) == 0 && NeedMainStep;
                if (ignoreDeleting)
                {
                    return false;
                }

                steps.Remove(step);
                return true;
            }

            var action = child as IAction;
            if (action != null)
            {
                action.Clear();
                return false;
            }

            return false;
        }

        override public ITreeViewItem MoveUp(object child)
        {
            var step = child as Step;
            if (step is null)
                return null;

            int index = steps.IndexOf(step);

            if (index <= 1)
                return null;


            SwapSteps(index, index - 1);
            OnValueChanged(this);

            return child as ITreeViewItem;
        }

        override public ITreeViewItem MoveDown(object child)
        {
            var step = child as Step;
            if (step is null)
                return null;

            int index = steps.IndexOf(step);
            
            if (index > steps.Count - 2 || index <= 0)
                return null;

            
            SwapSteps(index, index + 1);
            OnValueChanged(this);

            return child as ITreeViewItem;
        }

        private void SwapSteps(int index1, int index2)
        {
            (steps[index1], steps[index2]) = (steps[index2], steps[index1]);
            Editor.Editor.GetInstance().RefreshObject(this);
        }

        override public ITreeViewItem Replace(object child, object copyObject)
        {
            if (!(child is Step targetStep && copyObject is Step copiedStep))
                return null;


            Step newStep = copiedStep.Clone(this, GetStepN);

            if (Owner.BaseOperation.GetStateStepsNames(Type).Count > 0 &&
                !Owner.BaseOperation.GetStateStepsNames(Type).Contains(copiedStep.GetBaseStepName()) ||
                !string.IsNullOrEmpty(copiedStep.GetBaseStepLuaName()) &&
                steps.Except(new List<Step>() { targetStep }).ToList().Exists(s => s.GetBaseStepLuaName() == copiedStep.GetBaseStepLuaName()))
            {
                // reset base object
                newStep.SetNewValue(string.Empty, true);   
            }

            int index = steps.IndexOf(targetStep);
            steps.Remove(targetStep);
            steps.Insert(index, newStep);
            bool isModeStep = index == 0;
            if (isModeStep)
            {
                modeStep = newStep;
            }

            newStep.Owner = this;
            newStep.AddParent(this);

            newStep.ModifyDevNames(new DevModifyOptions(TechObject, copiedStep.TechObject.NameEplan, copiedStep.TechObject.TechNumber));

            return newStep;
        }

        public override ITreeViewItem InsertCopy(object copyObject)
        {
            if (!(copyObject is Step copiedStep))
                return null;

            CheckMainStep();

            var newStep = copiedStep.Clone(this, GetStepN);

            if (!Owner.BaseOperation.GetStateStepsNames(Type).Contains(copiedStep.GetBaseStepName()) ||
                !string.IsNullOrEmpty(copiedStep.GetBaseStepLuaName()) &&
                steps.Exists(s => s.GetBaseStepLuaName() == copiedStep.GetBaseStepLuaName()))
            {
                // reset base object
                newStep.SetNewValue(string.Empty, true);
            }

            steps.Add(newStep);
            newStep.AddParent(this);
            newStep.Owner = this;

            newStep.ModifyDevNames(new DevModifyOptions(TechObject, copiedStep.TechObject.NameEplan, copiedStep.TechObject.TechNumber));

            return newStep;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private ITreeViewItem CheckMainStep()
        {
            if (MissingMainStep)
            {
                modeStep = AddNewStepToItems(true);
                modeStep.AddParent(this);

                return modeStep;
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

        override public bool IsInsertable
        {
            get
            {
                return true;
            }
        }

        public override bool IsInsertableCopy
        {
            get
            {
                return true;
            }
        }

        override public ITreeViewItem Insert()
        {
            var mainStep = CheckMainStep();
            if (mainStep != null)
                return mainStep;

            Step newStep = AddNewStepToItems();
            newStep.AddParent(this);

            newStep.ValueChanged += (sender) => OnValueChanged(sender);
            OnValueChanged(this);

            return newStep;
        }

        override public bool IsDrawOnEplanPage
        {
            get
            {
                return true;
            }
        }

        override public List<DrawInfo> GetObjectToDrawOnEplanPage()
            => DrawInfo.Filter([.. steps.SelectMany(a => a.GetObjectToDrawOnEplanPage())]);
        #endregion

        public override string SystemIdentifier => "state";

        public override void UpdateOnGenericTechObject(ITreeViewItem genericObject)
        {
            if (genericObject is null)
            {
                Steps.ForEach(step => step.UpdateOnGenericTechObject(null));
                return;
            }

            if (!(genericObject is State genericState))
                return;

            foreach (var stepIndex in Enumerable.Range(0, genericState.Steps.Count))
            {
                var genericStep = genericState.Steps.ElementAtOrDefault(stepIndex);

                var step = Steps.ElementAtOrDefault(stepIndex);
                var stepByBaseStep = Steps.Find(s => s.GetBaseStepLuaName() == genericStep.GetBaseStepLuaName() && genericStep.GetBaseStepLuaName() != "");
                var stepsByName = Steps.FindAll(s => s.GetStepName() == genericStep.GetStepName());

                if (step != null)
                {
                    if (stepsByName.Count == 1 && stepsByName[0] != null && stepsByName[0] != step)
                        SwapSteps(stepIndex, Steps.IndexOf(stepsByName[0]));

                    if (stepByBaseStep != null && step != stepByBaseStep)
                    {
                        step.SetNewValue("", true);
                        stepByBaseStep.SetNewValue("", true);
                    }

                    if (!string.IsNullOrEmpty(genericStep.GetBaseStepLuaName()))
                        step.SetNewValue(genericStep.GetBaseStepLuaName(), true);
                    step.SetNewValue(genericStep.GetStepName());
                }
                else step = AddStep(genericStep.GetStepName(), genericStep.GetBaseStepLuaName());

                step.UpdateOnGenericTechObject(genericStep);
            }
        }

        public override void CreateGenericByTechObjects(IEnumerable<ITreeViewItem> itemList)
        {
            var states = itemList.Cast<State>().ToList();

            var refState = states.OrderBy(state => state.Steps.Count).FirstOrDefault();
            if (refState is null)
                return;

            foreach (var stepIndex in Enumerable.Range(0, refState.steps.Count))
            {
                var refStep = refState.steps[stepIndex];
                foreach (var stateIndex in Enumerable.Range(0, states.Count))
                {
                    var step = states.ElementAtOrDefault(stateIndex)?.steps.ElementAtOrDefault(stepIndex);
                    if (step is null || step.GetBaseStepName() != refStep.GetBaseStepName())
                        return;
                }

                Step newGenericStep;

                if (stepIndex == 0 && MissingMainStep)
                    newGenericStep = Insert() as Step;
                else if (stepIndex == 0)
                    newGenericStep = Steps[0];
                else
                    newGenericStep = AddStep(refStep.GetStepName(), refStep.GetBaseStepLuaName());

                newGenericStep
                    .CreateGenericByTechObjects(states.Select(state => state.Steps[stepIndex]));
            }
        }

        public override void UpdateOnDeleteGeneric()
        {
            Steps.ForEach(step => step.UpdateOnDeleteGeneric());
        }

        public bool Empty
        {
            get
            {
                if(steps.Where(x => x.Empty == true).Count() == steps.Count)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Отсутствует главный шаг (Во время операции)
        /// </summary>
        public bool MissingMainStep => modeStep is null || Items.Any() is false;

        /// <summary>
        /// Нужен главный шаг
        /// </summary>
        public bool NeedMainStep { get; private set; }

        public StateType Type { get; private set; }

        [ExcludeFromCodeCoverage]
        public override ImageIndexEnum ImageIndex => Type switch
        {
            StateType.IDLE => ImageIndexEnum.STATE_IDLE,
            StateType.STARTING => ImageIndexEnum.STATE_STARTING,
            StateType.RUN => ImageIndexEnum.STATE_RUN,
            StateType.COMPLETING => ImageIndexEnum.STATE_COMPLETING,
            StateType.COMPLETE => ImageIndexEnum.STATE_COMPLETE,
            StateType.PAUSING => ImageIndexEnum.STATE_PAUSING,
            StateType.PAUSE => ImageIndexEnum.STATE_PAUSE,
            StateType.UNPAUSING => ImageIndexEnum.STATE_UNPAUSING,
            StateType.STOPPING => ImageIndexEnum.STATE_STOPPING,
            StateType.STOP => ImageIndexEnum.STATE_STOP,
            _ => ImageIndexEnum.NONE,
        };

        public enum StateType : int
        {
            IDLE = 0,   // Простой
            RUN,        // Выполнение
            PAUSE,      // Пауза
            STOP,       // Остановка
            COMPLETE,   // Выполнено

            STARTING = 10,  // Запускается
            PAUSING,        // Становится в паузу
            UNPAUSING,      // Выходит из паузы
            STOPPING,       // Останавливается
            COMPLETING      // Завершается
        }

        public static List<StateType> GetOrderedStates()
        => [ 
            StateType.IDLE,
            StateType.STARTING,
            StateType.RUN,
            StateType.COMPLETING,
            StateType.COMPLETE,
            StateType.PAUSING,
            StateType.PAUSE,
            StateType.UNPAUSING,
            StateType.STOPPING,
            StateType.STOP
        ];

        public static readonly ReadOnlyDictionary<StateType, string> StateNames = 
            new(new Dictionary<StateType, string>()
            {
                [StateType.IDLE] = "Простой",
                [StateType.RUN] = "Выполнение",
                [StateType.PAUSE] = "Пауза",
                [StateType.STOP] = "Остановлен",
                [StateType.COMPLETE] = "Выполнено",
                [StateType.STARTING] = "Запускается",
                [StateType.PAUSING] = "Становится в паузу",
                [StateType.UNPAUSING] = "Выходит из паузы",
                [StateType.STOPPING] = "Остановка",
                [StateType.COMPLETING] = "Завершение",
            });

        private string name;        ///< Имя.
        private List<Step> steps;   ///< Список шагов.
        private Step modeStep;      ///< Шаг.
        private IMode owner;///< Владелец элемента
    }


    /// <summary>
    /// Таблица переходов между состояниями
    /// </summary>
    public static class StateTransitionMapExtension
    {
        public static string Name(this State.StateType type) 
            => State.StateNames[type];

        public static int Index(this State.StateType type)
            => (int)type;

        public static List<State.StateType> StateTransition(this State.StateType type) => type switch
        {
            State.StateType.IDLE => [State.StateType.RUN],
            State.StateType.STARTING => [State.StateType.RUN, State.StateType.STOP],
            State.StateType.RUN => [State.StateType.COMPLETE, State.StateType.PAUSE, State.StateType.STOP],
            State.StateType.COMPLETING => [State.StateType.COMPLETE, State.StateType.STOP],
            State.StateType.COMPLETE => [State.StateType.IDLE],
            State.StateType.PAUSING => [State.StateType.PAUSE, State.StateType.STOP],
            State.StateType.PAUSE => [State.StateType.RUN, State.StateType.STOP],
            State.StateType.UNPAUSING => [State.StateType.RUN, State.StateType.STOP],
            State.StateType.STOPPING => [State.StateType.STOP],
            State.StateType.STOP => [State.StateType.IDLE],
            _ => []
        };
    }
}
