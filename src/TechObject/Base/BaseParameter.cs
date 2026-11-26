using EasyEPlanner;
using Editor;
using EplanDevice;
using StaticHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using static IO.IONode;

namespace TechObject
{
    /// <summary>    
    /// Свойство для базовой операции.
    /// </summary>
    public abstract class BaseParameter : Editor.ObjectProperty
    {
        /// <summary>
        /// Абстрактный метод копирования объекта.
        /// </summary>
        /// <returns></returns>
        public abstract new BaseParameter Clone();

        public BaseParameter(string luaName, string name,
            string defaultValue = "", List<DisplayObject> displayObjects = null) 
            : base(name, defaultValue, defaultValue)
        {
            LuaName = luaName;
            currentValueType = ValueType.None;
            devicesIndexes = new List<int>();

            if(displayObjects != null)
            {
                displayObjectsFlags = new List<DisplayObject>();
                foreach(var displayObject in displayObjects)
                {
                    displayObjectsFlags.Add(displayObject);
                }
            }
            else
            {
                displayObjectsFlags = new List<DisplayObject>() 
                { DisplayObject.None };
            }

            SetUpDisplayObjects();
        }

        /// <summary>
        /// Настройка отображаемых объектов
        /// </summary>
        private void SetUpDisplayObjects()
        {
            displayParameters = DisplayObjects.Contains(DisplayObject.Parameters);
            deviceTypes = [.. DisplayObjects.SelectMany(displayObject => displayObject switch
            {
                DisplayObject.Signals => // Отображаются в окне устройств только эти типы, но привязать можно любые
                [
                    DeviceType.AI,
                    DeviceType.AO,
                    DeviceType.DI, DeviceType.SB, DeviceType.LS, DeviceType.FS, DeviceType.GS, DeviceType.TS,
                    DeviceType.DO, DeviceType.HL, 
                ],
                DisplayObject.AI => [DeviceType.AI],
                DisplayObject.AO => [DeviceType.AO],
                DisplayObject.DI => [DeviceType.DI, DeviceType.SB, DeviceType.LS, DeviceType.FS, DeviceType.GS, DeviceType.TS],
                DisplayObject.DO => [DeviceType.DO, DeviceType.HL],
                _ => new DeviceType[] { }
            }).Distinct()];
        }

        /// <summary>
        /// Добавить вид отображаемых объектов. Вызывается из LUA.
        /// </summary>
        /// <param name="flagValue">Строковое значение перечисления</param>
        public void AddDisplayObject(string flagValue)
        {
            bool ignoreCase = true;
            var parsed = Enum.TryParse(flagValue, ignoreCase,
                out DisplayObject parsedEnum);
            if(parsed)
            {
                bool replaceNoneValue =
                    displayObjectsFlags.Contains(DisplayObject.None) &&
                    displayObjectsFlags.Count == 1 &&
                    parsedEnum != DisplayObject.None;
                bool setToNoneValue = parsedEnum == DisplayObject.None;
                if (replaceNoneValue || setToNoneValue)
                {
                    displayObjectsFlags.Clear();
                    displayObjectsFlags.Add(parsedEnum);
                }

                if (!displayObjectsFlags.Contains(parsedEnum))
                {
                    displayObjectsFlags.Add(parsedEnum);
                }

                SetUpDisplayObjects();
            }
        }

        public List<DisplayObject> DisplayObjects
        {
            get
            {
                return displayObjectsFlags;
            }
        }

        /// <summary>
        /// Lua имя свойства.
        /// </summary>
        public string LuaName { get; set; }

        override public bool IsUseDevList
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Пустой ли параметр (nil or '')
        /// </summary>
        public virtual bool IsEmpty => !IsFilled;

        /// <summary>
        /// Объект-владелец параметра.
        /// </summary>
        public virtual object Owner { get; set; }

        /// <summary>
        /// Базовая операция - владелец параметра
        /// </summary>
        public virtual BaseOperation BaseOperation { get; set; }

        #region синхронизация устройств
        public virtual void Synch(int[] array)
        {
            if (OnlyDevicesInParameter)
            {
                IDeviceSynchronizeService synchronizer = DeviceSynchronizer
                    .GetSynchronizeService();
                synchronizer.SynchronizeDevices(array, ref devicesIndexes);
                SetValue(GetDevicesString());
            }
        }
        #endregion

        #region реализация ITreeView
        public override bool SetNewValue(string newValue)
        {
            newValue = newValue.Trim();
            ProcessValue(newValue);

            if (OnlyDevicesInParameter)
            {
                base.SetNewValue(GetDevicesString());
            }
            else
            {
                base.SetNewValue(newValue);
            }

            return true;
        }

        public override void SetValue(object val)
        {
            ProcessValue(val);
            
            if (OnlyDevicesInParameter)
            {
                base.SetValue(GetDevicesString());
            }
            else
            {
                base.SetValue(val);
            }
        }

        /// <summary>
        /// Обработать входящее значение
        /// </summary>
        /// <param name="value">Входящее значение</param>
        private void ProcessValue(object value)
        {
            string valueStr = value.ToString();
            SetParameterValueType(valueStr);
            devicesIndexes.Clear();
            if (OnlyDevicesInParameter)
            {
                List<string> splittedDevices = valueStr.Split(' ').ToList();
                devicesIndexes.AddRange(GetDevicesIndexes(splittedDevices));
            }
        }

        private void SetParameterValueType(string valueStr)
        {
            currentValueType = GetParameterValueType(valueStr);
        }

        /// <summary>
        /// Получить индексы устройств
        /// </summary>
        /// <param name="values">Список значений</param>
        /// <returns></returns>
        protected List<int> GetDevicesIndexes(List<string> values)
        {
            var indexes = new List<int>();

            foreach (var value in values)
            {
                int index = deviceManager.GetDeviceIndex(value);
                if (index >= 0)
                {
                    indexes.Add(index);
                }
            }

            return indexes;
        }

        override public string[] EditText => [LuaName, Value];

        override public string[] DisplayText => CurrentValueType switch
        {
            ValueType.Device or ValueType.ManyDevices => [Name, GetDevicesString()],
            ValueType.Parameter when TryGetParameter(Value, out var parameter) =>
                [Name, $"{parameter.GetParameterNumber}. {parameter.GetNameLua()}"],
            ValueType.Number when
                DisplayObjects.Contains(DisplayObject.Operation) &&
                TryGetOperationByNumber(Value, out var operation) => [Name, operation.DisplayText[0]],
            _ => [Name, Value],
        };
        
        /// <summary>
        /// Отображение: true - параметер; false - устройства
        /// </summary>
        public bool DisplayParameters => displayParameters;

        /// <summary>
        /// Получить строку с устройствами
        /// </summary>
        /// <returns></returns>
        protected string GetDevicesString()
        {
            var devices = new List<string>();
            foreach (var devIndex in devicesIndexes)
            {
                IDevice dev = deviceManager.GetDeviceByIndex(devIndex);
                if (dev.Name != StaticHelper.CommonConst.Cap)
                {
                    devices.Add(dev.Name);
                }
            }

            devices = devices.Distinct().ToList();
            return string.Join(" ", devices);
        }

        public override void GetDisplayObjects(out DeviceType[] devTypes, 
            out DeviceSubType[] devSubTypes, out bool displayParameters)
        {
            devSubTypes = null; // Not used;
            devTypes = deviceTypes;
            displayParameters = this.displayParameters;
        }

        public override bool IsDrawOnEplanPage => true;

        virtual public DrawInfo.Style DrawStyle { get; set; } = DrawInfo.Style
            .GREEN_BOX;

        override public List<DrawInfo> GetObjectToDrawOnEplanPage()
        {
            if (!OnlyDevicesInParameter)
                return [];

            var devToDraw = new List<DrawInfo>();
            foreach (int index in devicesIndexes)
            {
                devToDraw.Add(new DrawInfo(DrawStyle,
                    deviceManager.GetDeviceByIndex(index)));
            }

            return devToDraw;
        }

        #endregion

        /// <summary>
        /// Получить тип параметра в зависимости от введенного значения в поле
        /// </summary>
        /// <param name="value">Значение свойства</param>
        /// <returns></returns>
        private ValueType GetParameterValueType(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return ValueType.None;
            }

            if (IsStubValue(value))
            {
                return ValueType.Stub;
            }

            if (IsBoolParameter)
            {
                return ValueType.Boolean;
            }

            if (TryGetParameter(value, out _) || IsUnresolvedParameterNumber(value))
            {
                return ValueType.Parameter;
            }

            if (int.TryParse(value, out _))
            {
                return ValueType.Number;
            }

            bool isDevice = deviceManager.IsExistingDeviceByEplanName(value);
            if (isDevice)
            {
                return ValueType.Device;
            }

            string[] devices = value.Split(' ');
            if (devices.Length > 1)
            {
                bool haveBadDevices = false;
                var validDevices = new List<bool>();
                foreach (var device in devices)
                {
                    isDevice = deviceManager.IsExistingDeviceByEplanName(device);
                    if (isDevice == false)
                    {
                        haveBadDevices = true;
                    }
                    validDevices.Add(isDevice);
                }

                validDevices = validDevices.Distinct().ToList();
                if (validDevices.Count == 1 && haveBadDevices == false)
                {
                    return ValueType.ManyDevices;
                }
            }

            bool isParameter = GetCurrentTechObject()?.GetParamsManager() is { } pm &&
                (pm.Float.GetParam(value) != null || !pm.Initialized);
            if (isParameter)
            {
                return ValueType.Parameter;
            }

            return ValueType.Other;
        }

        private bool TryGetParameter(string value, out Param parameter)
        {
            parameter = null;
            if (!DisplayParameters || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            Params parameters = GetCurrentTechObject()?.GetParamsManager()?.Float;
            if (parameters is null)
            {
                return false;
            }

            if (int.TryParse(value, out int parameterNumber))
            {
                if (parameterNumber <= 0)
                {
                    return false;
                }

                parameter = parameters.GetParam(parameterNumber - 1);
            }
            else
            {
                parameter = parameters.GetParam(value);
            }

            return parameter != null;
        }

        private string GetParameterLuaName(string value)
            => TryGetParameter(value, out var parameter)
                ? parameter.GetNameLua()
                : value;

        private bool IsUnresolvedParameterNumber(string value)
        {
            if (!DisplayParameters || !int.TryParse(value, out int parameterNumber) ||
                parameterNumber <= 0)
            {
                return false;
            }

            ParamsManager paramsManager = GetCurrentTechObject()?.GetParamsManager();
            return paramsManager is null || !paramsManager.Initialized;
        }

        protected static bool IsStubValue(string value)
        {
            return string.Equals(value?.Trim(),
                StaticHelper.CommonConst.StubForCells,
                StringComparison.OrdinalIgnoreCase);
        }

        #region сохранение prg.lua
        public string SaveToPrgLua(string prefix)
        {
            if (!CanSaveToPrgLua())
            {
                return string.Empty;
            }

            TechObject obj = GetCurrentTechObject();
            var objName = string.Empty;
            if (obj != null)
            {
                objName = "prg." + obj.NameEplanForFile.ToLower() +
                    obj.TechNumber.ToString();
            }

            return CurrentValueType switch
            {
                ValueType.Boolean or ValueType.Other => $"{prefix}{LuaName} = {Value}",
                ValueType.Device => $"{prefix}{LuaName} = prg.control_modules.{Value}",
                ValueType.ManyDevices => $"{prefix}{SaveMoreThanOneDevice(LuaName, Value)}",
                ValueType.Number => GetNumberParameterStringForSave(prefix),
                ValueType.Parameter => $"{prefix}{LuaName} = {objName}.PAR_FLOAT.{GetParameterLuaName(Value)}",
                _ => string.Empty
            };
        }

        private bool CanSaveToPrgLua()
        {
            SetParameterValueType(Value);

            if (CurrentValueType is ValueType.Stub)
            {
                return false;
            }

            var hasSignal = HasSignalDisplayObject();
            var hasParameter = DisplayObjects.Contains(DisplayObject.Parameters);
            var hasOperation = DisplayObjects.Contains(DisplayObject.Operation);

            if (hasOperation)
            {
                return CurrentValueType is ValueType.Number &&
                    TryGetOperationByNumber(Value, out _);
            }

            if (hasSignal)
            {
                if (hasParameter && CurrentValueType is ValueType.Parameter)
                {
                    return true;
                }

                if (CurrentValueType is not (ValueType.Device or ValueType.ManyDevices))
                {
                    return false;
                }

                var devices = GetDevicesFromValue();
                return devices.Count == GetCorrectSignalNames(devices).Count;
            }

            if (hasParameter)
            {
                return CurrentValueType is ValueType.Parameter;
            }

            return CurrentValueType is not ValueType.Other;
        }

        /// <summary>
        /// Получение строки для сохранения в зависимости от того, кто
        /// владеет параметром. Сам юнит или это параметр агрегата.
        /// </summary>
        /// <param name="prefix">Отступ</param>
        /// <returns></returns>
        private string GetNumberParameterStringForSave(string prefix)
        {
            BaseTechObject baseTechObject = null;
            Mode mainMode = null;
            var modes = new List<Mode>();
            string mainObjDisplayName = "";

            if (Owner is BaseTechObject aggregateBaseTechObject)
            {
                baseTechObject = aggregateBaseTechObject;
                modes = baseTechObject.Owner.ModesManager.Modes;
                
                mainMode = BaseOperation.Owner;
            }
            else if (Owner is BaseOperation baseOperation)
            {
                baseTechObject = baseOperation.Owner.Owner.Owner.BaseTechObject;
                mainMode = baseOperation.Owner;
                modes = mainMode.Owner.Modes;
            }

            mainObjDisplayName = $"{baseTechObject?.Owner.DisplayText[0]}";

            Mode modeInParameter = modes
                .Where(x => x.GetModeNumber().ToString() == Value)
                .FirstOrDefault();
            var res = string.Empty;
            if (modeInParameter != null)
            {
                if (modeInParameter.BaseOperation.Name != string.Empty)
                {
                    string operationLuaName = modeInParameter.BaseOperation
                        .LuaName.ToUpper();
                    TechObject obj = baseTechObject?.Owner;
                    string objVarName =
                        $"prg.{obj?.NameEplanForFile.ToLower()}" +
                        $"{obj?.TechNumber}";
                    res = $"{prefix}{LuaName} = " +
                        $"{objVarName}.operations.{operationLuaName}";
                }
                else
                {
                    string message = $"Ошибка обработки параметра " +
                        $"\"{Name}\"." +
                        $" Не задана базовая операция в операции" +
                        $" \"{modeInParameter.DisplayText[0]}\", объекта " +
                        $"\"{mainObjDisplayName}\".\n";
                    Logs.AddMessage(message);
                }
            }
            else
            {
                string message = $"Ошибка обработки параметра " +
                        $"\"{Name}\"." +
                        $" Указан несуществующий номер операции в операции " +
                        $"\"{mainMode?.DisplayText[0]}\" объекта " +
                        $"\"{mainObjDisplayName}\".\n";
                Logs.AddMessage(message);
            }

            return res;
        }

        /// <summary>
        /// Сохранить более 1 устройства в параметре.
        /// </summary>
        /// <param name="value">Значение параметра</param>
        /// <param name="luaName">lua-имя параметра</param>
        /// <returns></returns>
        private string SaveMoreThanOneDevice(string luaName, string value)
        {
            string res = "";

            string[] devices = value.Split(' ');
            if (devices.Length > 1)
            {
                string[] modifiedDevices = devices
                    .Select(x => "prg.control_modules." + x).ToArray();
                res = $"{luaName} = " +
                    $"{{ {string.Join(", ", modifiedDevices)} }}";
            }
            else
            {
                res = $"{luaName} = prg.control_modules.{value}";
            }

            return res;
        }
        #endregion

        /// <summary>
        /// Получить технологический объект, в котором находится параметр.
        /// </summary>
        /// <returns></returns>
        private TechObject GetCurrentTechObject()
        {
            if (Owner is BaseTechObject)
            {
                return BaseOperation?.Owner?.Owner?.Owner;
            }
            else if (Owner is BaseOperation operation)
            {
                return operation.Owner?.Owner?.Owner;
            }
            else if (Owner is Equipment equipment)
            {
                return equipment.Owner;
            }

            return null;
        }

        public virtual void Check()
        {
            if (IsEmpty || Disabled)
            {
                return;
            }

            SetParameterValueType(Value);

            if (CurrentValueType is ValueType.Stub)
            {
                return;
            }

            if (!TryGetCheckContext(out var operation, out var techObject))
            {
                return;
            }

            var hasSignal = HasSignalDisplayObject();
            var hasParameter = DisplayObjects.Contains(DisplayObject.Parameters);
            var hasOperation = DisplayObjects.Contains(DisplayObject.Operation);
            var errContext = $"{techObject?.DisplayText[0]}: {operation?.DisplayText[0]}: доп. свойство \"{Name}\": поле заполнено неверно: '{Value}'";
            var devTypesContext = $"(допустимые типы: {string.Join(", ", deviceTypes.Select(d => d.ToString()))})";

            if (hasOperation)
            {
                CheckOperations(errContext);
                return;
            }

            if (hasSignal)
            {
                var devices = GetDevicesFromValue();
                var correctSignalNames = GetCorrectSignalNames(devices);
                var hasCorrectSignals = devices.Count == correctSignalNames.Count;

                if (hasParameter)
                {
                    CheckSignalsOrParameters(errContext, devTypesContext,
                        correctSignalNames, hasCorrectSignals);
                    return;
                }

                CheckSignals(errContext, devTypesContext, correctSignalNames,
                    hasCorrectSignals);
                return;
            }

            CheckParameters(errContext);
        }

        private bool TryGetCheckContext(out Mode operation, out TechObject techObject)
        {
            operation = null;
            techObject = null;

            if (Owner is BaseOperation baseOperation)
            {
                operation = baseOperation.Owner;
                techObject = operation?.Owner?.Owner;
                return true;
            }

            if (BaseOperation != null)
            {
                operation = BaseOperation.Owner;
                techObject = (Owner as BaseTechObject)?.Owner ??
                    operation?.Owner?.Owner;
                return true;
            }

            return TryGetCheckContextCore(out operation, out techObject);
        }

        protected virtual bool TryGetCheckContextCore(out Mode operation,
            out TechObject techObject)
        {
            operation = null;
            techObject = null;
            return false;
        }

        private bool HasSignalDisplayObject()
        {
            var signalDisplayObjects = new[]
            {
                DisplayObject.Signals,
                DisplayObject.DI,
                DisplayObject.DO,
                DisplayObject.AI,
                DisplayObject.AO,
            };

            return DisplayObjects.Any(signalDisplayObjects.Contains);
        }

        private List<IODevice> GetDevicesFromValue()
        {
            return [.. Value.Split([' '], StringSplitOptions.RemoveEmptyEntries)
                .Select(deviceManager.GetDevice)];
        }

        private List<string> GetCorrectSignalNames(List<IODevice> devices)
        {
            return devices
                .Where(d => d != null && deviceTypes.Contains(d.DeviceType))
                .Select(d => d.Name)
                .ToList();
        }

        private void CheckSignalsOrParameters(string errContext,
            string devTypesContext, List<string> correctSignalNames,
            bool hasCorrectSignals)
        {
            if (CurrentValueType is ValueType.Parameter)
                return;

            if (CurrentValueType is ValueType.Device or ValueType.ManyDevices)
            {
                if (hasCorrectSignals)
                    return;

                SetNewValue(string.Join(" ", correctSignalNames));
            }
            else
            {
                SetNewValue(string.Empty);
            }

            Logs.AddMessage($"{errContext} - Могут быть установлены только параметры или сигналы {devTypesContext};\n");
        }

        private void CheckSignals(string errContext, string devTypesContext,
            List<string> correctSignalNames, bool hasCorrectSignals)
        {
            if (CurrentValueType is ValueType.Device or ValueType.ManyDevices &&
                hasCorrectSignals)
                return;

            SetNewValue(string.Join(" ", correctSignalNames));
            Logs.AddMessage($"{errContext} - могут быть установлены только сигналы {devTypesContext};\n");
        }

        private void CheckParameters(string errContext)
        {
            if (!DisplayObjects.Contains(DisplayObject.Parameters) ||
                CurrentValueType is ValueType.Parameter)
                return;

            SetNewValue(string.Empty);
            Logs.AddMessage($"{errContext} - могут быть установлены только параметры;\n");
        }

        private void CheckOperations(string errContext)
        {
            if (CurrentValueType is ValueType.Number &&
                TryGetOperationByNumber(Value, out _))
                return;

            Logs.AddMessage($"{errContext} - может быть установлен только номер существующей операции;\n");
        }

        private bool TryGetOperationByNumber(string value, out Mode operation)
        {
            operation = null;

            if (!int.TryParse(value, out int operationNumber))
                return false;

            return TryGetOperationByNumber(operationNumber, out operation);
        }

        private bool TryGetOperationByNumber(int operationNumber,
            out Mode operation)
        {
            operation = GetOperationReferenceTechObject()?.ModesManager.Modes
                .FirstOrDefault(mode => mode.GetModeNumber() == operationNumber);

            return operation != null;
        }

        private TechObject GetOperationReferenceTechObject()
        {
            if (Owner is BaseTechObject baseTechObject)
                return baseTechObject.Owner;

            return GetCurrentTechObject();
        }

        public virtual void ModifyDevNames(IDevModifyOptions options)
        {
            if (!OnlyDevicesInParameter)
                return;

            var devNames = Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var newValues = devNames
                .Select(deviceManager.GetDeviceByEplanName)
                .Select(dev => deviceManager.GetModifiedDevice(dev, options))
                .Select((dev, index) =>
                {
                    // Если устройство не определено,
                    // то оставляем не модифицированный вариант
                    if (dev?.Description == CommonConst.Cap)
                        return deviceManager.GetDeviceByEplanName(devNames[index]);

                    // Оставляем устройство не измененным (MB null)
                    return dev;
                })
                .OfType<IDevice>()
                .Select(dev => dev.Name);

            SetNewValue(string.Join(" ", newValues));
        }


        public virtual List<BaseParameter> GetDescendants() => [ this ]; 

        /// <summary>
        /// Текущий тип значений, принимаемый параметром
        /// </summary>
        public ValueType CurrentValueType => currentValueType;

        /// <summary>
        /// Флаг, указывающий что в параметр записаны только устройства
        /// </summary>
        private bool OnlyDevicesInParameter =>
            CurrentValueType == ValueType.Device ||
            CurrentValueType == ValueType.ManyDevices;

        public enum DisplayObject
        {
            None = 1,
            Signals,
            Parameters,
            AI,
            AO,
            DI,
            DO,
            Operation,
        }

        /// <summary>
        /// Возможные типы значений полей параметров объекта
        /// </summary>
        public enum ValueType
        {
            None,
            Other,
            ManyDevices,
            Device,
            Boolean,
            Parameter,
            Number,
            Stub,
        }

        protected static readonly IDeviceManager deviceManager = DeviceManager.GetInstance();

        private List<DisplayObject> displayObjectsFlags;
        private ValueType currentValueType;
        protected List<int> devicesIndexes;

        private DeviceType[] deviceTypes;
        private bool displayParameters;
    }
}
