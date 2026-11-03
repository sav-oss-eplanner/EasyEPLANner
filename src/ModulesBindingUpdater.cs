using Eplan.EplApi.Base;
using Eplan.EplApi.DataModel;
using Eplan.EplApi.HEServices;
using StaticHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace EasyEPlanner
{
    /// <summary>
    /// Класс, реализующий синхронизацию модулей и названий устройств.
    /// </summary>
    class ModulesBindingUpdater
    {
        /// <summary>
        /// Экземпляр класса синхронизации названий и модулей
        /// </summary>
        /// <returns></returns>
        public static ModulesBindingUpdater GetInstance()
        {
            return modulesBindingUpdate;
        }

        /// <summary>
        /// Синхронизация названий устройств и модулей
        /// </summary>
        /// <returns>Возвращает сообщения об ошибках во время выполнения.
        /// </returns>
        [ExcludeFromCodeCoverage]
        public string Execute()
        {
            ProjectConfiguration.GetInstance().ReadIO();
            if (ProjectConfiguration.GetInstance().DevicesIsRead == true)
            {
                ProjectConfiguration.GetInstance().SynchronizeDevices();
            }
            else
            {
                ProjectConfiguration.GetInstance().ReadDevices();
            }
            ProjectConfiguration.GetInstance().ReadBinding();

            ProjectConfiguration.GetInstance().Check();
            IO.IOManager.GetInstance().CalculateIOLinkAdresses();

            Project project = EProjectManager.GetInstance().GetCurrentPrj();
            if (project == null)
                return errorMessage;

            var objectFinder = new DMObjectsFinder(project);

            var functionsFilter = new FunctionsFilter();
            var properties = new FunctionPropertyList();
            properties.FUNC_MAINFUNCTION = true;
            functionsFilter.SetFilteredPropertyList(properties);
            functionsFilter.Category = Function.Enums.Category.PLCBox;
            Function[] devicesFunctions = objectFinder.
                GetFunctions(functionsFilter);

            project.LockAllObjects();

            Dictionary<string, string> deviceConnections =
                CollectIOModulesData(devicesFunctions);

            bool containsNewValveTerminal = GetProjectVersionFromDevices(
                deviceConnections);

            synchronizedDevices.Clear();

            if (!containsNewValveTerminal)
            {
                SynchronizeAsOldProject(deviceConnections, devicesFunctions);
            }
            else
            {
                SynchronizeAsNewProject(deviceConnections, devicesFunctions);
            }

            ClearNotExistingBinding();

            return errorMessage;
        }

        /// <summary>
        /// Сбор данных привязок для обновления
        /// </summary>
        /// <param name="devicesFunctions">Главные функции устройств 
        /// для поиска обновленных данных</param>
        /// <returns></returns>
        private Dictionary<string, string> CollectIOModulesData
            (Function[] devicesFunctions)
        {
            var deviceConnections = new Dictionary<string, string>();

            foreach (EplanDevice.IODevice device in EplanDevice.DeviceManager.
                GetInstance().Devices)
            {
                foreach (EplanDevice.IODevice.IOChannel channel in device.Channels)
                {
                    if (!channel.IsEmpty())
                    {
                        CollectModuleData(device, channel, devicesFunctions,
                            ref deviceConnections);
                    }
                }
            }

            return deviceConnections;
        }

        /// <summary>
        /// Добавление данных привязки конкретного модуля
        /// </summary>
        /// <param name="channel">Канал устройства</param>
        /// <param name="device">Устройство</param>
        /// <param name="deviceConnections">Словарь с собранными данными 
        /// привязок устройств</param>
        /// <param name="deviceFunctions">Главные функции устройств для 
        /// поиска обновленных данных</param>
        private void CollectModuleData(EplanDevice.IODevice device,
            EplanDevice.IODevice.IOChannel channel, Function[] deviceFunctions,
            ref Dictionary<string, string> deviceConnections)
        {
            const string IOModulePrefix = "A";
            const string ASInterfaceModule = "655";

            string deviceVisibleName = IOModulePrefix + channel.FullModule;
            var deviceFunction = deviceFunctions.
                FirstOrDefault(x => x.VisibleName.Contains(deviceVisibleName));
            if (deviceFunction == null)
            {
                return;
            }

            deviceVisibleName += ChannelPostfix + channel.PhysicalClamp.
                ToString();
            string functionalText = device.EplanName;
            string devicePartNumber = deviceFunction.ArticleReferences[0]
                .PartNr;
            // Для модулей ASi не нужно добавлять комментарии 
            // к имени устройств.
            if (!devicePartNumber.Contains(ASInterfaceModule))
            {
                if (device.Description.Contains(PlusSymbol))
                {
                    // Так как в комментариях может использоваться знак 
                    // "плюс", то заменим его на какую-то константу, 
                    // которую при обновлении функционального текста 
                    // преобразуем обратно.
                    string replacedDeviceDescription = device.Description.
                        Replace(PlusSymbol.ToString(),
                        SymbolForPlusReplacing);
                    functionalText += CommonConst.NewLineWithCarriageReturn +
                        replacedDeviceDescription;

                    if (!string.IsNullOrEmpty(channel.Comment))
                    {
                        functionalText += CommonConst.NewLineWithCarriageReturn +
                            channel.Comment;
                    }
                }
                else
                {
                    functionalText += CommonConst.NewLineWithCarriageReturn +
                        device.Description;

                    if (!string.IsNullOrEmpty(channel.Comment))
                    {
                        functionalText += CommonConst.NewLineWithCarriageReturn +
                            channel.Comment;
                    }
                }

                if (IsPhoenixContactIOLinkModule(devicePartNumber) &&
                    device.Channels.Count > 1)
                {
                    functionalText += CommonConst.NewLineWithCarriageReturn +
                        DeviceBindingHelper
                        .GetChannelNameForIOLinkModuleFromString(channel.Name);
                }
            }
            else
            {
                functionalText += WhiteSpace;
            }

            if (deviceConnections.ContainsKey(deviceVisibleName))
            {
                deviceConnections[deviceVisibleName] += functionalText;
            }
            else
            {
                deviceConnections.Add(deviceVisibleName, functionalText);
            }
        }

        /// <summary>
        /// Проверка, является ли данный модуль IO-Link модулем 
        /// от Phoenix Contact
        /// </summary>
        /// <param name="partNumber">Номер детали из API</param>
        /// <returns></returns>
        private bool IsPhoenixContactIOLinkModule(string partNumber)
        {
            var isIOLinkModule = false;

            int PhoenixContactStandard = (int)IO.IOManager.IOLinkModules
                .PhoenixContactStandard;
            int PhoenixContactSmart = (int)IO.IOManager.IOLinkModules
                .PhoenixContactSmart;

            if (partNumber.Contains(PhoenixContactStandard.ToString()) ||
                partNumber.Contains(PhoenixContactSmart.ToString()))
            {
                isIOLinkModule = true;
            }

            return isIOLinkModule;
        }

        /// <summary>
        /// Проверка версии проекта через устройства.
        /// У старых проектов пневмоостров Festo - DEV_VTUG, а у новых - Y.
        /// Остальное не влияет, все одинаково.
        /// </summary>
        /// <param name="deviceConnections">Устройства для проверки</param>
        /// <returns></returns>
        private bool GetProjectVersionFromDevices(Dictionary<string, string>
            deviceConnections)
        {
            var isProjectWithValveTerminal = new bool();

            if (deviceConnections.Values.
                Where(x => x
                .Contains(EplanDevice.DeviceManager.ValveTerminalName))
                .Count() > 0)
            {
                isProjectWithValveTerminal = true;
                return isProjectWithValveTerminal;
            }
            else
            {
                isProjectWithValveTerminal = false;
                return isProjectWithValveTerminal;
            }
        }

        /// <summary>
        /// Синхронизация старого проекта
        /// </summary>
        /// <param name="deviceConnections">Устройства для синхронизации.
        /// Первый ключ - устройство ввода-вывода, 
        /// второй - привязанные устройства</param>
        /// <param name="functions">Функции обновляемых объектов</param>
        /// <returns></returns>
        private void SynchronizeAsOldProject(Dictionary<string, string>
            deviceConnections, Function[] functions)
        {
            foreach (string key in deviceConnections.Keys)
            {
                string deviceVisibleName = key.Substring(0, key.
                    IndexOf(ChannelPostfix));
                var synchronizingDevice = functions.FirstOrDefault(x => x.
                VisibleName.Contains(deviceVisibleName));

                if (synchronizingDevice == null)
                {
                    continue;
                }

                string bindedDevices = deviceConnections[key];
                var errors = string.Empty;
                bool? isASInterface = EplanDevice.DeviceManager.GetInstance().
                    IsASInterfaceDevices(bindedDevices, out errors);
                errorMessage += errors;
                bool deletingComments = NeedDeletingComments(bindedDevices);
                if (deletingComments == true)
                {
                    bindedDevices = SortDevices(bindedDevices);
                    bindedDevices = DeleteDevicesComments(bindedDevices);
                }

                bindedDevices = DeleteRepeatedDevices(bindedDevices,
                    deletingComments);

                if (isASInterface.HasValue && isASInterface.Value)
                {
                    bool isValidASNumbers = EplanDevice.DeviceManager.GetInstance().
                        CheckASNumbers(bindedDevices, out errors);
                    errorMessage += errors;
                    if (isValidASNumbers == true)
                    {
                        bool withoutComments = true;
                        bindedDevices = DeleteRepeatedDevices(bindedDevices,
                            withoutComments);
                        bindedDevices = SortASInterfaceDevices(bindedDevices);
                    }
                    else
                    {
                        // Если некорректные параметры - пропуск модуля.
                        continue;
                    }
                }
                else if (!isASInterface.HasValue)
                {
                    // Если AS-интерфейс привязан некорректно - пропуск модуля.
                    continue;
                }

                string clampNumberAsString = key.Remove(0, key.
                    IndexOf(ChannelPostfix) + ChannelPostfixSize);
                var clamp = Convert.ToInt32(clampNumberAsString);
                SynchronizeIOModule(synchronizingDevice, clamp, bindedDevices,
                    isASInterface);
            }
        }

        /// <summary>
        /// Синхронизация привязки к устройству
        /// </summary>
        /// <param name="functionalText">Обновленный функциональный текст
        /// </param>
        /// <param name="clamp">Номер клеммы</param>
        /// <param name="synchronizingDevice">Обновляемое устройство</param>
        private void SynchronizeIOModule(Function synchronizingDevice,
            int clamp, string functionalText, bool? isASInterface = false)
        {
            const int SequenceNumbersField = 20;
            // Конвертируем символ "плюс" обратно.
            functionalText = functionalText.
                Replace(SymbolForPlusReplacing, PlusSymbol.ToString());

            var placedFunctions = synchronizingDevice.SubFunctions.
                Where(x => x.IsPlaced == true).ToArray();

            var clampFunction = placedFunctions.
                        Where(x => x.Page.PageType ==
                        DocumentTypeManager.DocumentType.Circuit).
                        FirstOrDefault(x => x.Properties.
                        FUNC_ADDITIONALIDENTIFYINGNAMEPART.ToInt() == clamp);

            // Если null - клеммы нет на схеме.
            if (clampFunction != null)
            {
                clampFunction.Properties.FUNC_TEXT = functionalText;
                AddSynchronizedDevice(synchronizingDevice, clamp);
                // Если AS-интерфейс, в доп поле записать нумерацию
                if (isASInterface == true)
                {
                    clampFunction.Properties.
                        FUNC_SUPPLEMENTARYFIELD[SequenceNumbersField] =
                        ASINumbering;
                }
            }
        }

        /// <summary>
        /// Синхронизация нового проекта
        /// </summary>
        /// <param name="deviceConnections">Объекты для обновления.
        /// Первый ключ - устройство ввода-вывода, 
        /// второй - привязанные устройства</param>
        /// <param name="functions">Функции обновляемых объектов</param>
        private void SynchronizeAsNewProject(Dictionary<string, string>
            deviceConnections, Function[] functions)
        {
            foreach (string key in deviceConnections.Keys)
            {
                string deviceVisibleName = key.Substring(
                    0, key.IndexOf(ChannelPostfix));
                var synchronizingDevice = functions.
                    FirstOrDefault(x => x.VisibleName.
                    Contains(deviceVisibleName));

                if (synchronizingDevice == null)
                {
                    continue;
                }

                string clampNumberAsString = key.Remove(0,
                    key.IndexOf(ChannelPostfix) + ChannelPostfixSize);

                // Если нет пневмоострова Y - то синхронизация ничем 
                // не отличается от старого проекта.
                if (!deviceConnections[key]
                    .Contains(EplanDevice.DeviceManager.ValveTerminalName))
                {
                    var connections = new Dictionary<string, string>();
                    connections[key] = deviceConnections[key];
                    SynchronizeAsOldProject(connections, functions);
                }
                else
                {
                    var bindedDevices = deviceConnections[key].
                        Split(PlusSymbol).
                        FirstOrDefault(x => x
                        .Contains(EplanDevice.DeviceManager.ValveTerminalName))
                        ?.Insert(0, PlusSymbol.ToString());
                    var clamp = Convert.ToInt32(clampNumberAsString);
                    // Синхронизация пневмоострова.
                    SynchronizeIOModule(synchronizingDevice, clamp,
                        bindedDevices);

                    // Синхронизация устройств привязанных к пневмоострову.
                    Dictionary<int, string> bindedIOModuleDevices =
                        GetValveTerminalDevices(deviceConnections[key]);
                    deviceVisibleName = GetValveTerminalFunctionName(
                        deviceConnections[key]);
                    var synchronizingIOModuleDevice = functions.
                        FirstOrDefault(x => x.Name.Contains(
                            deviceVisibleName));
                    SynchronizeIOModuleDevices(synchronizingIOModuleDevice,
                        bindedIOModuleDevices);
                }
            }
        }

        /// <summary>
        /// Получение имени функции пневмоострова в котором обновляется 
        /// привязка
        /// </summary>
        /// <param name="bindedDevices">Привязанные устройства</param>
        /// <returns></returns>
        private string GetValveTerminalFunctionName(string bindedDevices)
        {
            const string FunctionNamePattern = "(\\+[A-Z0-9_]+-[Y0-9]+)";
            var functionNameMatch = Regex.Match(bindedDevices,
                FunctionNamePattern);

            if (functionNameMatch.Success)
            {
                return functionNameMatch.Value;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Получение устройств, привязанных к пневмоострову.
        /// </summary>
        /// <param name="functionalText">Функциональный текст 
        /// пневмоострова, к которому привязано устройство</param>
        /// <returns></returns>
        private Dictionary<int, string> GetValveTerminalDevices
            (string functionalText)
        {
            const string ClampNumberParameter = "R_VTUG_NUMBER";

            string[] devicesArray = functionalText.Split(PlusSymbol);
            var valveTerminalDevices = new Dictionary<int, string>();

            foreach (string deviceString in devicesArray)
            {
                if (string.IsNullOrEmpty(deviceString) ||
                    string.IsNullOrWhiteSpace(deviceString) ||
                    deviceString
                    .Contains(EplanDevice.DeviceManager.ValveTerminalName))
                {
                    continue;
                }

                string deviceName = Regex.Match(deviceString.
                    Insert(0, PlusSymbol.ToString()),
                    EplanDevice.DeviceManager.DeviceNamePattern).Value;
                EplanDevice.IODevice device = EplanDevice.DeviceManager.
                    GetInstance().GetDevice(deviceName);
                string clampNumber = device.GetRuntimeParameter(
                    ClampNumberParameter);
                var clamp = Convert.ToInt32(clampNumber);

                if (valveTerminalDevices.ContainsKey(clamp))
                {
                    valveTerminalDevices[clamp] += deviceString.
                        Insert(0, PlusSymbol.ToString());
                }
                else
                {
                    valveTerminalDevices.Add(clamp, deviceString.
                        Insert(0, PlusSymbol.ToString()));
                }
            }

            return valveTerminalDevices;
        }

        /// <summary>
        /// Функция, проверяющая необходимость удаления комментариев в 
        /// функциональном тексте.
        /// </summary>
        /// <param name="devices">Строка со списком устройств</param>
        /// <returns></returns>
        private bool NeedDeletingComments(string devices)
        {
            var deviceMatches = Regex.Matches(devices,
                EplanDevice.DeviceManager.DeviceNamePattern);

            if (deviceMatches.Count > MinimalDevicesCountForCheck)
            {
                // Если первое устройство с подтипом AS-интерфейс, 
                // то все такие 
                for (int i = 0; i < MinimalDevicesCountForCheck; i++)
                {
                    EplanDevice.IODevice device = EplanDevice.DeviceManager.
                        GetInstance().GetDevice(deviceMatches[i].Value);

                    bool needSaveComments = 
                        device.DeviceSubType == EplanDevice.DeviceSubType
                        .V_AS_DO1_DI2 ||
                        device.DeviceSubType == EplanDevice.DeviceSubType
                        .V_AS_MIXPROOF ||
                        device.DeviceSubType == EplanDevice.DeviceSubType
                        .V_IOLINK_MIXPROOF ||
                        device.DeviceSubType == EplanDevice.DeviceSubType
                        .V_IOLINK_DO1_DI2;

                    if (needSaveComments)
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Функция убирает комментарии в функциональном тексте при привязке
        /// нескольких устройств к одной клемме.
        /// </summary>
        /// <param name="devices">Строка со списком устройств</param>
        /// <returns></returns>
        private string DeleteDevicesComments(string devices)
        {
            var devicesMatches = Regex.Matches(devices,
                EplanDevice.DeviceManager.DeviceNamePattern);

            if (devicesMatches.Count <= MinimalDevicesCountForCheck)
            {
                return devices;
            }

            var devicesWithoutComments = "";
            foreach (Match match in devicesMatches)
            {
                devicesWithoutComments += match.Value + WhiteSpace;
            }

            return devicesWithoutComments;
        }

        /// <summary>
        /// Удаляет повторяющиеся привязанные устройства.
        /// </summary>
        /// <param name="devicesString">Устройства</param>
        /// <param name="withoutComments">С комментариями или без</param>
        /// <returns></returns>
        private string DeleteRepeatedDevices(string devicesString,
            bool withoutComments)
        {
            var devicesList = new List<string>();
            var devicesWithoutRepeating = string.Empty;

            if (withoutComments)
            {
                var devicesMatches = Regex.Matches(devicesString,
                    EplanDevice.DeviceManager.DeviceNamePattern);
                foreach (Match match in devicesMatches)
                {
                    devicesList.Add(match.Value);
                }

                devicesList = devicesList.Distinct().ToList();

                if (devicesList.Count > 0)
                {
                    devicesWithoutRepeating = devicesList
                        .Aggregate((a, b) => a + WhiteSpace + b);
                }
            }
            else
            {
                var devices = devicesString.Split(PlusSymbol)
                    .Where(x => x.Length > 0)
                    .Select(x => $"{PlusSymbol}{x}");
                if (devices.Count() > 0)
                {
                    devicesWithoutRepeating = devices.Distinct()
                        .Aggregate((a, b) => $"{a}\n{b}");
                }
            }

            return devicesWithoutRepeating;
        }

        /// <summary>
        /// Сортировка устройств для корректного отображения.
        /// </summary>
        /// <param name="devices">Устройства для сортировки</param>
        /// <returns></returns>
        private string SortDevices(string devices)
        {
            var devicesMatches = Regex.Matches(devices,
                EplanDevice.DeviceManager.DeviceNamePattern);

            if (devicesMatches.Count <= MinimalDevicesCountForCheck)
            {
                return devices;
            }

            // valveTerminal - для вставки VTUG в старых проектах первым.
            EplanDevice.Device valveTerminal = null;
            var devicesList = new List<EplanDevice.Device>();
            foreach (Match match in devicesMatches)
            {
                EplanDevice.Device device = EplanDevice.DeviceManager.GetInstance().
                    GetDevice(match.Value);
                if (device.DeviceType != EplanDevice.DeviceType.DEV_VTUG)
                {
                    devicesList.Add(device);
                }
                else
                {
                    valveTerminal = device;
                }
            }

            devicesList.Sort(DevicesComparer);

            if (valveTerminal != null)
            {
                devicesList.Insert(0, valveTerminal);
            }

            var sortedDevices = "";
            foreach (EplanDevice.Device device in devicesList)
            {
                if (device.Description.Contains(PlusSymbol))
                {
                    // Заменяем символ плюс в комментарии, что бы не было
                    // конфликтов. Потом вернем обратно.
                    string replacedDeviceDescription = device.Description.
                        Replace(PlusSymbol.ToString(), SymbolForPlusReplacing);
                    sortedDevices += device.EplanName +
                        CommonConst.NewLineWithCarriageReturn +
                        replacedDeviceDescription +
                        CommonConst.NewLineWithCarriageReturn;
                }
                else
                {
                    sortedDevices += device.EplanName +
                        CommonConst.NewLineWithCarriageReturn +
                        device.Description +
                        CommonConst.NewLineWithCarriageReturn;
                }
            }

            return sortedDevices;
        }

        /// <summary>
        /// Функция для сортировки AS интерфейса по параметру R_AS_NUMBER
        /// </summary>
        /// <param name="devices">Строка с устройствами</param>
        /// <returns></returns>
        private string SortASInterfaceDevices(string devices)
        {
            var devicesMatches = Regex.Matches(devices,
                EplanDevice.DeviceManager.DeviceNamePattern);

            if (devicesMatches.Count <= MinimalDevicesCountForCheck)
            {
                return devices;
            }

            var devicesList = new List<EplanDevice.IODevice>();
            foreach (Match match in devicesMatches)
            {
                EplanDevice.IODevice device = EplanDevice.DeviceManager.GetInstance().
                    GetDevice(match.Value);
                devicesList.Add(device);
            }

            devicesList.Sort(ASInterfaceDevicesComparer);

            int lastASNumber = 0;
            var devicesWithoutASNumber = CommonConst.NewLineWithCarriageReturn;
            var sortedDevices = "";

            foreach (EplanDevice.IODevice device in devicesList)
            {
                string numberAsString = device.
                    GetRuntimeParameter("R_AS_NUMBER");
                if (numberAsString == null)
                {
                    devicesWithoutASNumber += device.EplanName
                        + CommonConst.NewLineWithCarriageReturn;
                    continue;
                }

                int number;
                int.TryParse(numberAsString, out number);

                const int MinimalASNumber = 1;
                const int MaximalASNumber = 62;
                const int NormalDifference = 1; // Нормальные условия
                if (number is < MinimalASNumber or > MaximalASNumber)
                {
                    devicesWithoutASNumber += device.EplanName
                        + WhiteSpace;
                    continue;
                }

                if (lastASNumber != number)
                {
                    int difference = number - lastASNumber;
                    if (difference > NormalDifference)
                    {
                        var NewLines = "";
                        for (int i = 0; i < difference; i++)
                        {
                            NewLines += CommonConst.NewLineWithCarriageReturn;
                        }

                        sortedDevices += NewLines + device.EplanName;
                    }
                    else
                    {
                        if (lastASNumber == 0 &&
                            number == MinimalASNumber)
                        {
                            sortedDevices += device.EplanName;
                        }
                        else
                        {
                            sortedDevices += CommonConst
                                .NewLineWithCarriageReturn + device.EplanName;

                        }
                    }
                }

                lastASNumber = number;
            }
            sortedDevices += devicesWithoutASNumber;

            return sortedDevices;
        }

        /// <summary>
        /// Компаратор для сравнения устройств с
        /// AS-интерфейсом (сортировки).
        /// </summary>
        /// <param name="device1">Устройство 1</param>
        /// <param name="device2">Устройство 2</param>
        /// <returns></returns>
        private int ASInterfaceDevicesComparer(EplanDevice.IODevice device1,
            EplanDevice.IODevice device2)
        {
            string device1ASNumberString = device1.
                GetRuntimeParameter("R_AS_NUMBER");
            string device2ASNumberString = device2.
                GetRuntimeParameter("R_AS_NUMBER");

            int device1ASNumber;
            int device2ASNumber;

            int.TryParse(device1ASNumberString, out device1ASNumber);
            int.TryParse(device2ASNumberString, out device2ASNumber);

            return device1ASNumber.CompareTo(device2ASNumber);
        }

        /// <summary>
        /// Функция очищающая привязку с модулей ввода/вывода и других
        /// устройств, если устройства нет на ФСА, а привязка есть
        /// </summary>
        private void ClearNotExistingBinding()
        {
            foreach (Function device in synchronizedDevices.Keys)
            {
                string[] clamps = synchronizedDevices[device].
                    Split(WhiteSpace);

                var terminals = device.SubFunctions.
                    Where(x => x.IsPlaced == true &&
                    clamps.Contains(x.Properties.
                    FUNC_ADDITIONALIDENTIFYINGNAMEPART.ToInt().
                    ToString()) == false &&
                    x.Category == Function.Enums.Category.PLCTerminal).
                    ToList();

                if (terminals != null && terminals.Count > 0)
                {
                    var sortedTerminals = new List<Function>();
                    foreach (Function terminal in terminals)
                    {
                        string functionalTextRuru = terminal.Properties.
                            FUNC_TEXT.ToString(ISOCode.Language.L_ru_RU);
                        if (functionalTextRuru == "")
                        {
                            functionalTextRuru = terminal.Properties.FUNC_TEXT.
                                ToString(ISOCode.Language.L___);
                        }

                        if (functionalTextRuru != CommonConst.Reserve)
                        {
                            sortedTerminals.Add(terminal);
                        }
                    }

                    foreach (Function terminal in sortedTerminals)
                    {
                        terminal.Properties.FUNC_TEXT = "";
                    }
                }
                else
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// Добавляет в словарь синхронизированных устройств новое устройство
        /// </summary>
        /// <param name="synchronizedDevice">Функция устройства</param>
        /// <param name="clamp">Номер клеммы</param>
        private void AddSynchronizedDevice(Function synchronizedDevice,
            int clamp)
        {
            if (synchronizedDevices.ContainsKey(synchronizedDevice))
            {
                synchronizedDevices[synchronizedDevice] +=
                    $"{WhiteSpace}{clamp}";
            }
            else
            {
                synchronizedDevices[synchronizedDevice] = clamp.ToString();
            }
        }

        /// <summary>
        /// Синхронизация привязки устройств привязанных к модулю.
        /// Реализовано сразу два типа синхронизации
        /// </summary>
        /// <param name="synchronizingModule">Синхронизируемый модуль</param>
        /// <param name="IOModuleDevices">Устройства для синхронизации</param>
        private void SynchronizeIOModuleDevices(Function synchronizingModule,
            Dictionary<int, string> IOModuleDevices)
        {
            DocumentTypeManager.DocumentType circuitDocument =
                    DocumentTypeManager.DocumentType.Circuit;
            DocumentTypeManager.DocumentType overviewDocument =
                    DocumentTypeManager.DocumentType.Overview;

            foreach (int clamp in IOModuleDevices.Keys)
            {
                string functionalText =
                    SortDevices(IOModuleDevices[clamp]);
                // Конвертируем символ "плюс" обратно.
                functionalText = functionalText.
                    Replace(SymbolForPlusReplacing, PlusSymbol.ToString());

                string functionalTextWithoutComments =
                    DeleteDevicesComments(functionalText);

                // На электрических схемах при множественной привязке
                // комментарий не пишем.
                SynchronizeDevice(synchronizingModule, clamp,
                    functionalTextWithoutComments, circuitDocument);

                // На странице "Обзор" при множественной привязке надо писать
                // комментарии.
                SynchronizeDevice(synchronizingModule, clamp, functionalText,
                    overviewDocument);
            }
        }

        /// <summary>
        /// Синхронизация привязки устройства.
        /// </summary>
        /// <param name="synchronizingModule">Функция обновляемого модуля 
        /// ввода-вывода</param>
        /// <param name="clamp">Номер клеммы</param>
        /// <param name="functionalText">Функциональный текст</param>
        /// <param name="documentType">Тип страницы для поиска подфункции
        /// </param>
        private void SynchronizeDevice(Function synchronizingModule, int clamp,
            string functionalText,
            DocumentTypeManager.DocumentType documentType)
        {
            const string ClampFunctionDefenitionName = "Дискретный выход";

            var placedFunctions = synchronizingModule.SubFunctions.
                Where(x => x.IsPlaced == true).ToArray();

            var clampFunction = placedFunctions.
                Where(x => x.Page.PageType == documentType &&
                x.FunctionDefinition.Name.GetString(ISOCode.Language.L_ru_RU).
                Contains(ClampFunctionDefenitionName)).
                FirstOrDefault(y => y.Properties.
                FUNC_ADDITIONALIDENTIFYINGNAMEPART.ToInt() == clamp);

            // Если null - клеммы нет на схеме.
            if (clampFunction != null)
            {
                clampFunction.Properties.FUNC_TEXT = functionalText;
                AddSynchronizedDevice(synchronizingModule, clamp);
            }
        }

        /// <summary>
        /// Компаратор для сравнения устройств (сортировки)
        /// </summary>
        /// <param name="x">Устройство 1</param>
        /// <param name="y">Устройство 2</param>
        /// <returns></returns>
        private int DevicesComparer(EplanDevice.Device x, EplanDevice.Device y)
        {
            int res;
            res = EplanDevice.Device.Compare(x, y);
            return res;
        }

        const string ChannelPostfix = "_CH"; // Постфикс канала.
        const int ChannelPostfixSize = 3; // Размер постфикса канала.
        const char PlusSymbol = '+'; // Для обратной вставки после Split.
        const char WhiteSpace = ' '; // Пробел для разделения ОУ.
        const string SymbolForPlusReplacing = "plus"; // Замена символа "плюс".
        const int MinimalDevicesCountForCheck = 1; // Минимальное количество
        // устройств в строке, нужное для выполнения функции проверки.

        const string ASINumbering = "1\r\n2\r\n" + // Нумерация AS-i клапанов
            "3\r\n4\r\n5\r\n6\r\n7\r\n8\r\n9\r\n10\r\n11\r\n12\r\n13\r\n" +
            "14\r\n15\r\n16\r\n17\r\n18\r\n19\r\n20\r\n21\r\n22\r\n23\r\n" +
            "24\r\n25\r\n26\r\n27\r\n28\r\n29\r\n30\r\n31\r\n32\r\n33\r\n" +
            "34\r\n35\r\n36\r\n37\r\n38\r\n39\r\n40\r\n41\r\n42\r\n43\r\n" +
            "44\r\n45\r\n46\r\n47\r\n48\r\n49\r\n50\r\n51\r\n52\r\n53\r\n" +
            "54\r\n55\r\n56\r\n57\r\n58\r\n59\r\n60\r\n61\r\n62\r\n";

        static ModulesBindingUpdater modulesBindingUpdate =
            new ModulesBindingUpdater(); // Static экземпляр класса для доступа.
        string errorMessage; // Сообщение об ошибках во время работы.

        // Синхронизированные устройства, Функция - список клемм.
        Dictionary<Function, string> synchronizedDevices =
            new Dictionary<Function, string>();
    }
}
