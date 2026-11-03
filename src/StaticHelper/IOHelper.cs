using Eplan.EplApi.Base;
using Eplan.EplApi.DataModel;
using EplanDevice;
using IO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace StaticHelper
{
    public interface IIOHelper
    {
        /// <summary>
        /// Получить функцию выбранной клеммы
        /// </summary>
        /// <param name="selectedObject">Выбранный на схеме объект
        /// </param>
        /// <returns>Функция клеммы модуля ввода-вывода</returns>
        Function GetClampFunction(StorableObject selectedObject);

        /// <summary>
        /// Получить функцию выбранной клеммы
        /// </summary>
        /// <param name="IOModuleFunction">Функция модуля ввода-вывода</param>
        /// <param name="deviceName">Привязанное к клемме устройство</param>
        /// <returns></returns>
        Function GetClampFunction(Function IOModuleFunction, string deviceName);

        /// <summary>
        /// Получить функцию модуля ввода-вывода.
        /// Модуль, куда привязывается устройство.
        /// </summary>
        /// <param name="clampFunction">Функция клеммы модуля 
        /// ввода-вывода</param>
        Function GetIOModuleFunction(Function clampFunction);
    }

    public class IOHelper : IIOHelper
    {
        IProjectHelper projectHelper;

        public IOHelper(IProjectHelper projectHelper)
        {
            this.projectHelper = projectHelper;
        }

        /// <summary>
        /// Кэш функции модуля I/O для пневмоострова: полный обход PLCBox
        /// в EPLAN на больших проектах занимает секунды и вызывается
        /// несколько раз за одну привязку.
        /// Создаётся лениво, чтобы конструктор не тянул типы EPLAN в тестах.
        /// </summary>
        private Dictionary<string, Function> valveTerminalIoModuleCache;

        public Function GetClampFunction(StorableObject selectedObject)
        {
            if (selectedObject is Function == false)
            {
                const string Message = "Выбранный на схеме объект не " +
                    "является функцией";
                throw new Exception(Message);
            }

            var clampFunction = selectedObject as Function;

            if (clampFunction.Category != Function.Enums.Category.PLCTerminal)
            {
                const string Message = "Выбранная функция не является " +
                    "клеммой модуля ввода-вывода";
                throw new Exception(Message);
            }

            return clampFunction;
        }

        public Function GetClampFunction(
            Function IOModuleFunction, string deviceName)
        {
            var clampFunction = new Function();

            deviceName = Regex.Match(deviceName, DeviceManager.valveTerminalPattern)
                .Value;
            Function[] subFunctions = IOModuleFunction.SubFunctions;
            if (subFunctions != null)
            {
                foreach (Function subFunction in subFunctions)
                {
                    var functionalText = subFunction.Properties
                        .FUNC_TEXT_AUTOMATIC
                        .ToString(ISOCode.Language.L___);
                    if (functionalText.Contains(deviceName))
                    {
                        clampFunction = subFunction;
                        return clampFunction;
                    }
                }
            }

            if (clampFunction.Category != Function.Enums.Category.PLCTerminal)
            {
                const string Message = "Выбранная функция не является " +
                    "клеммой модуля ввода-вывода";
                throw new Exception(Message);
            }

            return clampFunction;
        }

        public Function GetIOModuleFunction(Function clampFunction)
        {
            var isValveTerminalClampFunction = false;
            Function IOModuleFunction = null;
            if (clampFunction.Name.Contains(DeviceManager.ValveTerminalName) ||
                clampFunction.Name.Contains("-EY"))
            {
                IOModuleFunction = GetValveTerminalIOModuleFunction(
                    clampFunction);
                if (IOModuleFunction != null)
                {
                    isValveTerminalClampFunction = true;
                }
            }

            if (isValveTerminalClampFunction == false)
            {
                IOModuleFunction = clampFunction.ParentFunction;
            }

            if (IOModuleFunction == null)
            {
                MessageBox.Show(
                    "Данная клемма названа некорректно. Измените" +
                    " ее название (пример корректного названия " +
                    "\"===DSS1+CAB4-A409\"), затем повторите " +
                    "попытку привязки устройства.",
                    "EPlaner",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
            }

            return IOModuleFunction;
        }

        /// <summary>
        /// Поиск функции модуля ввода-вывода, к которому привязан пневмоостров.
        /// Сначала берётся из уже прочитанной модели проекта, полный обход
        /// функций EPLAN — только запасной путь.
        /// </summary>
        [ExcludeFromCodeCoverage]
        private Function GetValveTerminalIOModuleFunction(
            Function clampFunction)
        {
            string valveTerminalName = Regex.Match(clampFunction.Name,
                DeviceManager.valveTerminalPattern).Value;
            if (string.IsNullOrEmpty(valveTerminalName))
            {
                const string Message = "Ошибка поиска ОУ пневмоострова";
                throw new Exception(Message);
            }

            var fromModel = TryGetValveTerminalIOModuleFromModel(
                clampFunction.Name);
            if (fromModel != null)
                return ToEplanFunction(fromModel);

            return GetValveTerminalIOModuleFromCacheOrScan(valveTerminalName);
        }

        [ExcludeFromCodeCoverage]
        private Function GetValveTerminalIOModuleFromCacheOrScan(
            string valveTerminalName)
        {
            valveTerminalIoModuleCache ??= new Dictionary<string, Function>();
            if (valveTerminalIoModuleCache.TryGetValue(valveTerminalName,
                out var cached))
            {
                return cached;
            }

            var found = FindValveTerminalIOModuleByEplanScan(valveTerminalName);
            if (found != null)
                valveTerminalIoModuleCache[valveTerminalName] = found;

            return found ?? new Function();
        }

        private static Function ToEplanFunction(IEplanFunction function)
            => (function as EplanFunction)?.Function;

        private static IEplanFunction TryGetValveTerminalIOModuleFromModel(
            string clampName)
        {
            var deviceName = clampName.Contains(":")
                ? clampName.Split(':')[0]
                : clampName;
            var valveTerminal = DeviceManager.GetInstance().GetDevice(deviceName);
            if (valveTerminal.Description == CommonConst.Cap)
                return null;

            bool isValveTerminal =
                valveTerminal.DeviceType == DeviceType.Y ||
                valveTerminal.DeviceType == DeviceType.DEV_VTUG;
            if (!isValveTerminal)
                return null;

            var channel = valveTerminal.Channels.Count > 0
                ? valveTerminal.Channels[0]
                : null;
            if (channel == null || channel.IsEmpty())
                return null;

            try
            {
                var module = IOManager.GetInstance()
                    .GetModuleByPhysicalNumber(channel.FullModule);
                return module?.Function;
            }
            catch
            {
                return null;
            }
        }

        [ExcludeFromCodeCoverage]
        private Function FindValveTerminalIOModuleByEplanScan(
            string valveTerminalName)
        {
            var objectFinder = new DMObjectsFinder(
                (projectHelper as ProjectHelper).GetProject());
            var functionsFilter = new FunctionsFilter();
            var properties = new FunctionPropertyList();
            properties.FUNC_MAINFUNCTION = true;
            functionsFilter.SetFilteredPropertyList(properties);
            functionsFilter.Category = Function.Enums.Category.PLCBox;
            Function[] functions = objectFinder.GetFunctions(functionsFilter);

            foreach (Function function in functions)
            {
                Function[] subFunctions = function.SubFunctions;
                if (subFunctions == null)
                    continue;

                foreach (Function subFunction in subFunctions)
                {
                    var functionalText = subFunction.Properties
                        .FUNC_TEXT_AUTOMATIC
                        .ToString(ISOCode.Language.L___);
                    if (functionalText.Contains(valveTerminalName))
                        return subFunction.ParentFunction;
                }
            }

            return null;
        }
    }
}
