using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Editor;
using EplanDevice;
using TechObject.ActionProcessingStrategy;
using TechObject.AttachedObjectStrategy;

namespace TechObject
{
    /// <summary>
    /// Шаг технологического объекта. Состоит из параллельно выполняемых 
    /// действий.
    /// </summary>
    public class Step : TreeViewItem
    {
        /// <summary>
        /// Создание нового шага.
        /// </summary>
        /// <param name="name">Имя шага.</param>
        /// <param name="getN">Функция получения номера шага.</param>
        /// <param name="isMainStep">Признак того, является ли шаг 
        /// шагом операции. </param>
        /// <param name="owner">Владелец шага (Состояние)</param>
        public Step(string name, GetN getN, IState owner,
            bool isMainStep = false)
        {
            this.name = name ?? string.Empty;
            this.getN = getN;
            IsMainStep = isMainStep;
            Owner = owner;
            baseStep = new BaseStep(string.Empty, string.Empty);
            baseStep.Owner = this;

            items = new List<ITreeViewItem>();

            actions = new List<IAction>();

            AddDefaultActions(isMainStep);

            foreach (var item in Items)
            {
                item.ValueChanged += (sender) => OnValueChanged(sender);
            }
        }

        /// <summary>
        /// Добавить стандартные действия
        /// </summary>
        /// <param name="isMainStep">Главный шаг</param>
        private void AddDefaultActions(bool isMainStep)
        {
            var checkedDevices = new Action("Проверяемые устройства",
                this, "checked_devices", null, null);
            actions.Add(checkedDevices);

            var openDevices = new Action(openDevicesActionName, this,
                "opened_devices",
                new EplanDevice.DeviceType[]
                {
                    EplanDevice.DeviceType.V,
                    EplanDevice.DeviceType.DO,
                    EplanDevice.DeviceType.M
                });
            openDevices.ImageIndex = ImageIndexEnum.ActionON;
            openDevices.ActionType = DrawInfo.ActionType.ON_DEVICE;
            actions.Add(openDevices);


            var openDevicesActionGroup = new ActionGroupCustom(
                "Включать с задержкой", this, "delay_opened_devices",
                () =>
                {
                    var openedDeviceAction = new ActionCustom("Группа",
                        this, "");
                    var opened = openedDeviceAction.CreateAction(new Action("Включать",
                        this, "",
                        [
                            DeviceType.V,
                            DeviceType.DO,
                            DeviceType.M
                        ]));
                    opened.ActionType = DrawInfo.ActionType.DELAYED_ON_DEVICE;
                    openedDeviceAction.CreateParameter(
                        new ActionParameter("", "Задержка включения"));
                    return openedDeviceAction;
                });
            openDevicesActionGroup.ActionType = DrawInfo.ActionType.DELAYED_ON_DEVICE;
            actions.Add(openDevicesActionGroup);


            var openReverse = new Action("Включать реверс", this,
                "opened_reverse_devices",
                new EplanDevice.DeviceType[]
                {
                    EplanDevice.DeviceType.M
                },
                new EplanDevice.DeviceSubType[]
                {
                    EplanDevice.DeviceSubType.M_REV_FREQ,
                    EplanDevice.DeviceSubType.M_REV_FREQ_2,
                    EplanDevice.DeviceSubType.M_REV_FREQ_2_ERROR,
                    EplanDevice.DeviceSubType.M_ATV,
                    EplanDevice.DeviceSubType.M_ATV_LINEAR,
                    EplanDevice.DeviceSubType.M,
                    EplanDevice.DeviceSubType.M_VIRT,
                });
            actions.Add(openReverse);

            var closeDevices = new Action(closeDevicesActionName, this,
                "closed_devices",
                new EplanDevice.DeviceType[]
                {
                    EplanDevice.DeviceType.V,
                    EplanDevice.DeviceType.DO,
                    EplanDevice.DeviceType.M
                });
            closeDevices.DrawStyle = DrawInfo.Style.GRAY_BOX;
            closeDevices.ActionType = DrawInfo.ActionType.OFF_DEVICE;
            closeDevices.ImageIndex = ImageIndexEnum.ActionOFF;
            actions.Add(closeDevices);

            var closeDevicesActionGroup = new ActionGroupCustom(
                "Выключать с задержкой", this, "delay_closed_devices",
                () =>
                {
                    var closedDeviceAction = new ActionCustom("Группа",
                        this, "");
                    var closed = closedDeviceAction.CreateAction(new Action("Выключать",
                        this, "",
                        [
                            DeviceType.V,
                            DeviceType.DO,
                            DeviceType.M
                        ]));
                    closed.ActionType = DrawInfo.ActionType.DELAYED_OFF_DEVICE;
                    closedDeviceAction.CreateParameter(
                        new ActionParameter("", "Задержка выключения"));
                    return closedDeviceAction;
                });
            actions.Add(closeDevicesActionGroup);
            closeDevicesActionGroup.DrawStyle = DrawInfo.Style.GRAY_BOX;
            closeDevicesActionGroup.ActionType = DrawInfo.ActionType.DELAYED_OFF_DEVICE;

            var openUpperSeats = new ActionGroup("Верхние седла", this,
                "opened_upper_seat_v",
                new EplanDevice.DeviceType[]
                {
                    EplanDevice.DeviceType.V,
                    EplanDevice.DeviceType.DO
                },
                new EplanDevice.DeviceSubType[]
                {
                    EplanDevice.DeviceSubType.V_MIXPROOF,
                    EplanDevice.DeviceSubType.V_AS_MIXPROOF,
                    EplanDevice.DeviceSubType.V_IOLINK_MIXPROOF,
                    EplanDevice.DeviceSubType.V_IOL_TERMINAL_MIXPROOF_DO3,
                    EplanDevice.DeviceSubType.V_VIRT,
                    EplanDevice.DeviceSubType.DO,
                    EplanDevice.DeviceSubType.DO_VIRT
                });
            openUpperSeats.DrawStyle = DrawInfo.Style.GREEN_UPPER_BOX;
            openUpperSeats.ImageIndex = ImageIndexEnum.ActionWashUpperSeats;
            actions.Add(openUpperSeats);

            var openLowerSeats = new ActionGroup("Нижние седла", this,
                "opened_lower_seat_v",
                new EplanDevice.DeviceType[]
                {
                    EplanDevice.DeviceType.V,
                    EplanDevice.DeviceType.DO
                },
                new EplanDevice.DeviceSubType[]
                {
                    EplanDevice.DeviceSubType.V_MIXPROOF,
                    EplanDevice.DeviceSubType.V_AS_MIXPROOF,
                    EplanDevice.DeviceSubType.V_IOLINK_MIXPROOF,
                    EplanDevice.DeviceSubType.V_MINI_FLUSHING,
                    EplanDevice.DeviceSubType.V_IOL_TERMINAL_MIXPROOF_DO3,
                    EplanDevice.DeviceSubType.V_VIRT,
                    EplanDevice.DeviceSubType.DO,
                    EplanDevice.DeviceSubType.DO_VIRT
                });
            openLowerSeats.DrawStyle = DrawInfo.Style.GREEN_LOWER_BOX;
            openLowerSeats.ImageIndex = ImageIndexEnum.ActionWashLowerSeats;
            actions.Add(openLowerSeats);

            var requiredFb = new Action("Сигналы для включения", this,
                "required_FB",
                new EplanDevice.DeviceType[]
                {
                    EplanDevice.DeviceType.DI,
                    EplanDevice.DeviceType.GS
                });
            requiredFb.ImageIndex = ImageIndexEnum.ActionSignals;
            actions.Add(requiredFb);

            var groupWash = new ActionGroupWash("Устройства", this,
                ActionGroupWash.MultiGroupAction);
            groupWash.ImageIndex = ImageIndexEnum.ActionWash;
            actions.Add(groupWash);

            var pairsDiDoAllowedDevTypes = new EplanDevice.DeviceType[]
            {
                EplanDevice.DeviceType.DI,
                EplanDevice.DeviceType.SB,
                EplanDevice.DeviceType.DO,
                EplanDevice.DeviceType.HL,
                EplanDevice.DeviceType.GS,
                EplanDevice.DeviceType.LS,
                EplanDevice.DeviceType.FS
            };

            var pairsDiDoAllowedInputTypes = new EplanDevice.DeviceType[]
            {
                EplanDevice.DeviceType.DI,
                EplanDevice.DeviceType.SB,
                EplanDevice.DeviceType.GS,
                EplanDevice.DeviceType.LS,
                EplanDevice.DeviceType.FS
            };


            // Поля для действий Группа дискретных сигналов
            ActionCustom subgroupDIDO()
            {
                var enableStepBySignalAction = new ActionCustom("Группа", this, "");
                enableStepBySignalAction.CreateAction(
                    new Action("Сигналы", this, "", pairsDiDoAllowedDevTypes,
                    null, new ManyInManyOutActionProcessingStrategy(pairsDiDoAllowedInputTypes)));
                enableStepBySignalAction.CreateParameter(
                    new ComboBoxParameter("logic_type", "Логика", new Dictionary<string, string>() {
                        { "ИЛИ", "0" },
                        { "И", "1" }
                    }, "0"));
                return enableStepBySignalAction;
            }

            // Специальное действие - выдача дискретных сигналов 
            // при наличии входных дискретных сигналов.
            var groupDIDO = new ActionGroupCustom(groupDIDOActionName, this, "DI_DO", subgroupDIDO);
            groupDIDO.ImageIndex = ImageIndexEnum.ActionDIDOPairs;
            actions.Add(groupDIDO);

            // Специальное действие - выдача дискретных сигналов 
            // при пропадании входных дискретных сигналов.
            var groupInvertedDIDO = new ActionGroupCustom(groupDIDOActionNameInverted, this, "inverted_DI_DO", subgroupDIDO);
            groupInvertedDIDO.ImageIndex = ImageIndexEnum.ActionDIDOPairs;
            actions.Add(groupInvertedDIDO);


            var pairsAiAoAllowedInputTypes = new EplanDevice.DeviceType[]
            {
                EplanDevice.DeviceType.AI,
                EplanDevice.DeviceType.PT,
                EplanDevice.DeviceType.LT,
                EplanDevice.DeviceType.FQT,
                EplanDevice.DeviceType.QT,
                EplanDevice.DeviceType.TE,
            };

            // Специальное действие - выдача аналоговых сигналов при
            // наличии входного  аналогового сигнала.
            var groupAiAo = new ActionGroup(groupAIAOActionName, this,
                "AI_AO",
                new EplanDevice.DeviceType[]
                {
                    EplanDevice.DeviceType.AI,
                    EplanDevice.DeviceType.AO,
                    EplanDevice.DeviceType.M,
                    EplanDevice.DeviceType.PT,
                    EplanDevice.DeviceType.LT,
                    EplanDevice.DeviceType.FQT,
                    EplanDevice.DeviceType.QT,
                    EplanDevice.DeviceType.TE,
                    EplanDevice.DeviceType.VC
                },
                null,
                new ManyInManyOutActionProcessingStrategy(pairsAiAoAllowedInputTypes));
            groupAiAo.ImageIndex = ImageIndexEnum.ActionDIDOPairs;
            actions.Add(groupAiAo);

            var enableStepBySignal = new ActionGroupCustom(
                "Сигнал для включения текущего шага", this, "enable_step_by_signal",
                () =>
                {
                    var enableStepBySignalAction = new ActionCustom("Группа",
                        this, "");
                    enableStepBySignalAction.CreateAction(new Action("Сигналы",
                        this, "",
                        new EplanDevice.DeviceType[]
                        {
                            EplanDevice.DeviceType.DI,
                            EplanDevice.DeviceType.LS,
                            EplanDevice.DeviceType.GS,
                            EplanDevice.DeviceType.SB,
                            EplanDevice.DeviceType.FS,
                            EplanDevice.DeviceType.TS,
                            EplanDevice.DeviceType.PDS,
                        }));
                    return enableStepBySignalAction;
                })
            {
                ToolTipText = ("Логика 'И'- внутри группы, 'ИЛИ' - между группами", null),
            };
            enableStepBySignal.CreateParameter(new ActiveBoolParameter("",
                "Выключать шаг по пропаданию сигнала", "true"));

            actions.Add(enableStepBySignal);

            items.AddRange(actions.Cast<ITreeViewItem>().ToArray());

            if (!isMainStep)
            {
                var toStepByConditionAction = new ActionGroupCustom(
                "Переход к следующему шагу по условию", this, "jump_if",
                () =>
                {
                    var toStepByCondition = new ActionCustom("Группа", this, "");
                    toStepByCondition.CreateAction(new Action("Включение устройств",
                        this, "on_devices",
                        jump_if_AllowedDevTypes));
                    toStepByCondition.CreateAction(new Action("Выключение устройств",
                        this, "off_devices",
                        jump_if_AllowedDevTypes));

                    toStepByCondition.CreateParameter(new ActiveParameter("next_step_n",
                       "Шаг"));
                    return toStepByCondition;
                }){
                    ToolTipText = ("Логика 'И'- внутри группы, 'ИЛИ' - между группами", null),
                };
                actions.Add(toStepByConditionAction);
                items.Add(toStepByConditionAction);

                timeParam = new ActionParameter("", "Время (параметр)");
                items.Add(timeParam);

                maxDurationParam = new ActionParameter("step_max_duration_par_n", "Максимальное время шага (параметр)");
                items.Add(maxDurationParam);

                nextStepN = new ObjectProperty("Номер следующего шага", -1, -1);
                items.Add(nextStepN);

                SetAttachedObject();
            }

            if (isMainStep)
            {
                SetJumpToStateIf();
            }
        }

        private void SetAttachedObject()
        { 
            attachedObject = new AttachedObjects(string.Empty, null, 
                new AttachedWithoutInitStrategy("Связанный объект", "attached_object",
                    new List<BaseTechObjectManager.ObjectType>()
                        {
                            BaseTechObjectManager.ObjectType.Unit,
                            BaseTechObjectManager.ObjectType.Aggregate,
                            BaseTechObjectManager.ObjectType.UserObject,
                        }));
            items.Add(attachedObject);
        }

        public void SetJumpToStateIf()
        {
            var CBParameterValues = Owner.Type.StateTransition()
                .ToDictionary(t => t.Name(), t => t.Index().ToString());

            var toStateByConditionAction = new ActionGroupCustom(
            "Переход к состоянию по условию", this, "jump_if",
            () =>
            {
                var toStateByCondition = new ActionCustom("Группа", this, "");
                toStateByCondition.CreateAction(new Action("Включение устройств",
                    this, "on_devices",
                    jump_if_AllowedDevTypes));
                toStateByCondition.CreateAction(new Action("Выключение устройств",
                    this, "off_devices",
                    jump_if_AllowedDevTypes));

                toStateByCondition.CreateParameter(new ComboBoxParameter(
                "next_state_n",
                "К состоянию операции",
                CBParameterValues
                ));
                return toStateByCondition;
            }) {
                ToolTipText = ("Логика 'И'- внутри группы, 'ИЛИ' - между группами", null),
            };
            actions.Add(toStateByConditionAction);
            items.Add(toStateByConditionAction);
        }

        public Step Clone(State newOwner, GetN getN, string name = "")
        {
            Step clone = (Step)MemberwiseClone();
            clone.getN = getN;

            if (name != string.Empty)
            {
                clone.name = name.Substring(3);
            }

            clone.Owner = newOwner;
            clone.actions = new List<IAction>();
            foreach (IAction action in actions)
            {
                var clonedAction = action.Clone();
                AssignOwnerToAction(clonedAction, clone);
                clone.actions.Add(clonedAction);
            }

            clone.items = new List<ITreeViewItem>();
            clone.items.AddRange(clone.actions.Cast<ITreeViewItem>().ToArray());

            if (!IsMainStep)
            {
                clone.timeParam = (timeParam as BaseParameter).Clone();
                clone.maxDurationParam = (maxDurationParam as BaseParameter).Clone();
                clone.nextStepN = nextStepN.Clone();

                clone.items.Add(clone.timeParam);
                clone.items.Add(clone.maxDurationParam);
                clone.items.Add(clone.nextStepN);
            }

            clone.baseStep = baseStep.Clone();
            clone.baseStep.Owner = clone;

            clone.actions.ForEach(
                action => (action as ITreeViewItem).ValueChanged +=
                sender => clone.OnValueChanged(sender));

            return clone;
        }

        private static void AssignOwnerToAction(IAction action, Step owner)
        {
            action.Owner = owner;
            if (action.HasSubActions)
            {
                action.SubActions?.ForEach(subAction => AssignOwnerToAction(subAction, owner));
            }
        }

        public void ModifyDevNames(IDevModifyOptions options)
        {
            actions.ForEach(action => action.ModifyDevNames(options));
        }

        /// <summary>
        /// Сохранение в виде таблицы Lua.
        /// </summary>
        /// <param name="prefix">Префикс (для выравнивания).</param>
        /// <param name="isShortForm">Сохранять ли сокращенном виде (для  
        /// операции без вывода названия шага).</param>
        /// <returns>Описание в виде таблицы Lua.</returns>
        public string SaveAsLuaTable(string prefix, bool isShortForm = false)
        {
            if (isShortForm)
                return string.Join(string.Empty,
                    actions.Select(a => a.SaveAsLuaTable(prefix)));
                
            var resultBuilder = new StringBuilder();

            int timeParamNumber = int.TryParse(timeParam.Value, out timeParamNumber) ? timeParamNumber : -1;
            int maxDurationParamNumber = int.TryParse(maxDurationParam.Value, out maxDurationParamNumber) ? maxDurationParamNumber : -1;

            string next_step_n = nextStepN.EditText[1].Trim();
            string attached_object = attachedObject.Value.ToString();

            // Если не установлены параметры времени шага, то сбрасываем номер следующего шага
            // и сбрасываем эти параметры в -1
            if (timeParamNumber <= 0 && maxDurationParamNumber <= 0)
            {
                next_step_n = "-1";
                timeParamNumber = -1;

                nextStepN.SetNewValue("-1");
                timeParam.SetNewValue("-1");
                maxDurationParam.SetNewValue("-1");
            }

            string timeStr = (string.IsNullOrEmpty(timeParam.Value)) ? string.Empty :
                $"{prefix}time_param_n = {timeParamNumber},\n";
            string maxDurationStr = (maxDurationParamNumber <= 0) ? string.Empty :
                $"{prefix}{(maxDurationParam as ActionParameter).LuaName} = {maxDurationParamNumber},\n";
            string next_step_n_str = (string.IsNullOrEmpty(next_step_n)) ? string.Empty :
                $"{prefix}next_step_n = {next_step_n},\n";
            string baseStep_str = (string.IsNullOrEmpty(baseStep.LuaName)) ? string.Empty :
                $"{prefix}baseStep = '{baseStep.LuaName}',\n";
            string attachedObject_str = (string.IsNullOrEmpty(attached_object)) ? string.Empty :
                $"{prefix}attached_object = {attached_object},\n";

            resultBuilder.Append(prefix).Append("{\n")
                .Append(prefix).Append($"name = '{name}',\n")
                .Append(timeStr)
                .Append(maxDurationStr)
                .Append(next_step_n_str)
                .Append(baseStep_str)
                .Append(attachedObject_str);

            foreach (IAction action in actions)
            {
                resultBuilder.Append(action.SaveAsLuaTable(prefix));
            }

            resultBuilder.Append(prefix).Append("},\n");


            return resultBuilder.ToString();
        }
   
        /// <summary>
        /// Сохарение шага в виде таблицы Excel
        /// </summary>
        /// <returns>Описание в виде списка строк(клеток) Excel</returns>
        public string[] SaveAsExcel()
        {
            var res = new List<string>();

            res.Add(name);
            foreach (IAction action in actions)
            {
                res.Add(action.SaveAsExcel()); 
            }
            res.Add(TimeParam);
            res.Add(TimeParam);
            res.Add(NextStepN);

            return res.ToArray();
        }


        /// <summary>
        /// Добавление параметров.
        /// </summary>
        /// <param name="time_param_n">Номер параметра со временем шага.
        /// </param>
        /// <param name="next_step_n">Номер следующего шага.</param>
        public void SetPar(int timeParamN, int nextStepN, int maxDurationParamNumber)
        {
            this.timeParam.SetNewValue(timeParamN.ToString());
            this.nextStepN.SetNewValue(nextStepN.ToString());
            this.maxDurationParam.SetNewValue(maxDurationParamNumber.ToString());
        }

        public void SetAttachedObject(int attachedObject)
        {
            if (attachedObject != -1)
                this.attachedObject.SetNewValue(attachedObject.ToString());
        }

        /// <summary>
        /// Добавление устройства.
        /// </summary>
        /// <param name="actionLuaName">Имя действия в Lua.</param>
        /// <param name="devName">Имя устройства.</param>
        /// <param name="groupNumber">Номер группы.</param>
        /// <param name="subActionLuaName">Имя поддействия</param>
        public bool AddDev(string actionLuaName, string devName,
            int groupNumber, string subActionLuaName)
        {
            int devId = deviceManager.GetDeviceIndex(devName);
            if (devId == -1)
            {
                return false;
            }

            var action = GetActionByLuaName(actionLuaName, actions);
            bool haveAction = action != null;
            if(haveAction)
            {
                action.AddDev(devId, groupNumber, subActionLuaName);
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Добавление параметра.
        /// Вызывается из Lua-скрипта sys.lua.
        /// </summary>
        /// <param name="actionLuaName">Имя действия в Lua.</param>
        /// <param name="val">Значение параметра.</param>
        /// <param name="groupNumber">Индекс группы в действии
        /// мойки (устройства)</param>
        /// <param name="paramName">Имя параметра</param>
        public bool AddParam(string actionLuaName, object val, string paramName,
            int groupNumber)
        {
            IAction action = GetActionByLuaName(actionLuaName, actions);
            bool haveAction = action != null;
            if (haveAction)
            {
                action.AddParam(val, paramName, groupNumber);
                return true;
            }

            return false;
        }

        private IAction GetActionByLuaName(string name, List<IAction> actions)
        {
            foreach (IAction act in actions)
            {
                if (act.LuaName == string.Empty)
                {
                    break;
                }

                if(act.LuaName == name)
                {
                    return act;
                }

                if (name == ActionGroupWash.SingleGroupAction)
                {
                    return actions.Where(x => x.LuaName ==
                    ActionGroupWash.MultiGroupAction).First();
                }
            }

            return null;
        }

        public List<IAction> GetActions
        {
            get
            {
                return actions;
            }
        }

        #region Синхронизация устройств в объекте.
        public void Synch(int[] array)
        {
            foreach (IAction action in actions)
            {
                action.Synch(array);
            }
        }
        #endregion

        public string GetStepName()
        {
            return this.name;
        }

        public int GetStepNumber()
        {
            return getN(this);
        }

        public IState Owner { get; set; }

        public TechObject TechObject => Owner.TechObject;

        /// <summary>
        /// Lua-имя базового шага
        /// </summary>
        /// <returns></returns>
        public string GetBaseStepLuaName()
        {
            return baseStep.LuaName;
        }

        /// <summary>
        /// Имя базового шага
        /// </summary>
        /// <returns></returns>
        public string GetBaseStepName()
        {
            return baseStep.Name;
        }

        #region Реализация ITreeViewItem
        override public string[] DisplayText
        {
            get
            {
                if (getN(this) == 0)
                {
                    return new string[] { name, string.Empty };
                }

                return new string[] { getN(this) + ". " + name, baseStep.Name };
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

        public override bool SetNewValue(string newVal, bool isExtraValue)
        {
            // сброс базового шага
            if (newVal == string.Empty)
            {
                baseStep = new BaseStep("", "", 0, this);
                OnValueChanged(this);
                return true;
            }

            State state = Owner as State;
            // уже есть такой базовый шаг
            if (state.Steps.Any(x => x.GetBaseStepLuaName() == newVal || x.GetBaseStepName() == newVal))
            {
                return false;
            }

            // Установка базового шага
            BaseStep stateBaseStep = state.Owner.BaseOperation.GetStateBaseSteps(state.Type)
                .FirstOrDefault(x => x.LuaName == newVal || x.Name == newVal);

            if (stateBaseStep is null)
                return false;
            
            baseStep = stateBaseStep.Clone();
            baseStep.Owner = this;
            if (name.Contains(NewStepName) && stateBaseStep.Name != string.Empty)
            {
                name = stateBaseStep.Name;
            }

            OnValueChanged(this);
            return true;
        }

        override public bool IsEditable
        {
            get
            {
                if (IsMode)
                {
                    return false;
                }

                return true;
            }
        }

        override public int[] EditablePart
        {
            get
            {
                // Можем редактировать содержимое левой колонки, или обе.
                return IsMainStep ? new int[] { 0, -1 } : new int[] { 0, 1 };
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

        public override ITreeViewItem Replace(object child, object copyObject)
        {
            if (!(child is IAction targetAction && copyObject is IAction copyAction))
                return null;

            if (copyAction.LuaName != targetAction.LuaName)
                return null;

            var newAction = copyAction.Clone();

            int index = actions.IndexOf(targetAction);
            actions.RemoveAt(index);
            actions.Insert(index, newAction);

            newAction.Owner = this;
            newAction.AddParent(this);

            var childActionAsITreeViewItem = (ITreeViewItem)targetAction;
            var newActionAsITreeViewItem = (ITreeViewItem)newAction;
            index = items.IndexOf(childActionAsITreeViewItem);
            items.RemoveAt(index);
            items.Insert(index, newActionAsITreeViewItem);

            newAction.ModifyDevNames(new DevModifyOptions(Owner.TechObject,
                copyAction.Owner.TechObject.NameEplan,
                copyAction.Owner.TechObject.TechNumber));

            return newAction as ITreeViewItem;
        }

        override public string[] EditText
        {
            get
            {
                return new string[] { name, baseStep.Name };
            }
        }

        override public bool IsCopyable
        {
            get
            {
                return true;
            }
        }

        override public bool IsDeletable
        {
            get
            {
                if (IsMode)
                {
                    return false;
                }

                return true;
            }
        }

        override public bool Delete(object child)
        {
            if (child is IAction action)
            {
                action.Clear();
            }

            if (child is ObjectProperty objectProperty)
            {
                objectProperty.Delete(this);
            }

            return false;
        }

        override public bool IsDrawOnEplanPage
        {
            get { return true; }
        }

        override public List<DrawInfo> GetObjectToDrawOnEplanPage()
            => DrawInfo.FilterByActions([.. actions.SelectMany(a => a.GetObjectToDrawOnEplanPage())]);

        public override IEnumerable<string> BaseObjectsList
        {
            get => Owner.Owner.BaseOperation
                .GetStateStepsNames(Owner.Type)
                .Except(from step in (Owner as State).Steps
                        where step.GetBaseStepName() != string.Empty && step != this
                        select step.GetBaseStepName())
                ;
        }

        public override bool ContainsBaseObject
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
                return ImageIndexEnum.Step;
            }
        }
        #endregion

        #region Проверка действий на ошибки
        /// <summary>
        /// Проверка действий в шаге
        /// </summary>
        /// <returns>Строка с ошибками</returns>
        public string Check()
        {
            var errors = string.Empty;

            State state = Owner as State;
            Mode mode = state.Owner as Mode;
            ModesManager modesManager = mode.Owner;
            TechObject techObject = modesManager.Owner;
            string techObjName = techObject.DisplayText[0];
            string modeName = mode.Name;

            errors += CheckOpenAndCloseActions(techObjName, modeName);
            errors += CheckInOutGroupActions();
            errors += CheckNextStepNumber(state, techObjName, modeName);
            return errors;
        }

        private string CheckNextStepNumber(State state, string techObjName,
            string modeName)
        {
            if (IsMainStep)
            {
                return string.Empty;
            }

            bool isValidNumber = int.TryParse(NextStepN, out int nextStepNumber);
            bool isExistingStep = isValidNumber && (nextStepNumber == -1 ||
                (nextStepNumber >= 0 && nextStepNumber < state.Steps.Count - 1));
            bool isCurrentStep = isExistingStep && nextStepNumber >= 0 &&
                nextStepNumber == state.GetStepN(this) - 1;

            if (isExistingStep && !isCurrentStep)
            {
                return string.Empty;
            }

            string error = isCurrentStep
                ? "переход в текущий шаг недопустим"
                : "указанный шаг не существует";

            return $"\"{techObjName}\" -> " +
                $"операция \"{modeName}\" -> шаг \"{GetStepName()}\" -> " +
                $"св-во \"Номер следующего шага\": Некорректное значение, {error} - \"{NextStepN}\".\n";
        }

        private string CheckOpenAndCloseActions(string techObjName,
            string modeName)
        {
            var errors = string.Empty;
            var devicesInAction = new List<int>();

            var checkingActionsDevs = actions
                .Where(x => x.Name == openDevicesActionName ||
                x.Name == closeDevicesActionName)
                .Select(y => y.DevicesIndex);
            foreach(var devList in checkingActionsDevs)
            {
                devicesInAction.AddRange(devList);
            }

            List<int> FindEqual = devicesInAction.GroupBy(x => x)
                .SelectMany(y => y.Skip(1)).Distinct().ToList();

            foreach (int i in FindEqual)
            {
                EplanDevice.IDevice device = EplanDevice.DeviceManager.GetInstance()
                    .GetDeviceByIndex(i);
                string msg = $"Неправильно задано устройство {device.Name} " +
                    $"в действиях \"{openDevicesActionName}\" и " +
                    $"\"{closeDevicesActionName}\", в шаге " +
                    $"\"{GetStepName()}\", операции \"{modeName}\"," +
                    $"технологического объекта " +
                    $"\"{techObjName}\"\n";
                errors += msg;
            }

            return errors;
        }

        private string CheckInOutGroupActions()
        {
            var errors = new List<string>();

            var checkingActionsGroups = actions
                .Where(x => x.Name == groupAIAOActionName ||
                x.Name == groupDIDOActionName ||
                x.Name == groupDIDOActionNameInverted);

            foreach (var action in checkingActionsGroups.SelectMany(g => g.SubActions).OfType<IAction>())
            {
                if (action.Empty)
                {
                    continue;
                }

                errors.Add(action.GetDeviceProcessingStrategy()
                    .Check(deviceManager));
            }

            return string.Join("", errors.Distinct());
        }
        #endregion

        public override string SystemIdentifier => "phase";

        public void SetUpFromBaseTechObject(BaseStep baseStep)
        {
            bool setBaseStep = true;
            SetNewValue(baseStep.Name, setBaseStep);
        }

        public override void UpdateOnGenericTechObject(ITreeViewItem genericObject)
        {
            if (genericObject is null)
            {
                GetActions.ForEach(action => action.UpdateOnGenericTechObject(null));
                return;
            }

            var genericStep = genericObject as Step;
            if (genericStep is null) return;

            foreach (var actionIndex in Enumerable.Range(0, genericStep.actions.Count))
            {
                var genericAction = genericStep.GetActions.ElementAtOrDefault(actionIndex);
                var action = GetActions.ElementAtOrDefault(actionIndex);

                if (genericAction is null ||
                    action is null ||
                    (genericAction.Empty && action.Empty))
                    continue;

                action.UpdateOnGenericTechObject(genericAction);
            }

            if (genericStep.timeParam?.IsFilled ?? false)
                timeParam?.UpdateOnGenericTechObject(genericStep.timeParam);

            if (genericStep.maxDurationParam?.IsFilled ?? false)
                maxDurationParam?.UpdateOnGenericTechObject(genericStep.maxDurationParam);

            if (genericStep.nextStepN?.IsFilled ?? false)
                nextStepN?.UpdateOnGenericTechObject(genericStep.nextStepN);
        }

        public override void CreateGenericByTechObjects(IEnumerable<ITreeViewItem> itemList)
        {
            var steps = itemList.Cast<Step>().ToList();

            foreach (var actionIndex in Enumerable.Range(0, GetActions.Count))
            {
                GetActions[actionIndex]
                    .CreateGenericByTechObjects(steps.Select(step => step.GetActions[actionIndex]));
            }

            var refStep = steps.FirstOrDefault();
            if (steps.TrueForAll(step => step.timeParam != null && step.timeParam.Value == refStep.timeParam.Value))
                timeParam?.SetNewValue(refStep.timeParam.Value);

            if (steps.TrueForAll(step => step.nextStepN != null && step.nextStepN.Value == refStep.nextStepN.Value))
                nextStepN?.SetNewValue(refStep.nextStepN.Value);
        }

        public override void UpdateOnDeleteGeneric()
        {
            actions.ForEach(action => action.UpdateOnDeleteGeneric());
        }

        public bool Empty
        {
            get
            {
                if(actions.Where(x => x.Empty == true).Count() == actions.Count)
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
        /// Имя нового шага.
        /// </summary>
        public static string NewStepName
        {
            get
            {
                return "Новый шаг";
            }
        }

        /// <summary>
        /// Имя главного шага состояния.
        /// </summary>
        public static string MainStepName
        {
            get
            {
                return "Во время операции";
            }
        }

        /// <summary>
        /// Признак шага операции.
        /// </summary>
        private bool IsMainStep { get; set; }

        public string NextStepN => nextStepN.EditText[1].Trim();

        public string TimeParam => timeParam.Value.Trim();

        public string MaxDurationParam => maxDurationParam.Value.Trim();

        /// <summary>
        /// Устройства отображаемые в действии "Переход к ... по условию"
        /// </summary>
        private static EplanDevice.DeviceType[] jump_if_AllowedDevTypes 
            = new EplanDevice.DeviceType[] 
        {
            EplanDevice.DeviceType.V,
            EplanDevice.DeviceType.GS,
            EplanDevice.DeviceType.DI,
            EplanDevice.DeviceType.DO,
            EplanDevice.DeviceType.SB,
            EplanDevice.DeviceType.LS,
        };

        private static IDeviceManager deviceManager { get; set; } = DeviceManager.GetInstance();

        private GetN getN;

        private ObjectProperty nextStepN; ///< Номер следующего шага.
        private ObjectProperty timeParam; ///< Параметр времени.
        private ObjectProperty maxDurationParam; ///< Параметр максимального времени шага

        private AttachedObjects attachedObject; // Связанный объект
        private List<ITreeViewItem> items;

        private string name; ///< Имя шага.
        internal List<IAction> actions; ///< Список действий шага.

        private string openDevicesActionName = "Включать";
        private string closeDevicesActionName = "Выключать";
        private string groupDIDOActionName = "Группы DI -> DO DO ...";
        private string groupDIDOActionNameInverted =
            "Группы инвертированный DI -> DO DO ...";
        private string groupAIAOActionName = "Группы AI -> AO AO ...";

        private BaseStep baseStep;
    }
}
