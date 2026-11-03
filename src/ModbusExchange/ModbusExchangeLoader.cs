using EasyEPlanner.ModbusExchange.Model;
using EplanDevice;
using LuaInterface;
using StaticHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EasyEPlanner.ModbusExchange
{
    /// <summary>
    /// Чтение файлов обмена сигналами по Modbus.
    /// </summary>
    public static class ModbusExchangeLoader
    {
        /// <summary>
        /// Загрузить модели обмена из файлов.
        /// </summary>
        /// <param name="exchange">Модель обмена</param>
        [ExcludeFromCodeCoverage]
        public static void Load(this IExchange exchange)
        {
            var lua = PrepareLuaState();

            var path = GetPathToLoad();
            var files = Directory.GetFiles(path, "gate_*.lua",
                SearchOption.TopDirectoryOnly);

            foreach (var filePath in files)
            {
                var modelName = Regex.Replace(
                    Path.GetFileNameWithoutExtension(filePath),
                    "^gate_", string.Empty, RegexOptions.None,
                    TimeSpan.FromMilliseconds(100));

                exchange.AddModel(modelName);

                using var reader = new StreamReader(filePath,
                    EncodingDetector.DetectFileEncoding(filePath), true);

                lua["gate"] = lua.DoString(reader.ReadToEnd())[0];
                lua.GetFunction("init")?.Call(exchange.SelectedModel);
            }
        }

        /// <summary>
        /// Получить путь к файлам проекта.
        /// </summary>
        [ExcludeFromCodeCoverage]
        private static string GetPathToLoad()
        {
            if (!string.IsNullOrEmpty(
                ProjectContextHolder.Current?.ProjectFolderPath))
            {
                return ProjectContextHolder.Current.ProjectFolderPath;
            }

            var projectName = EProjectManager.GetInstance()
                .GetCurrentProjectName();
            return ProjectManager.GetInstance()
                .GetPtusaProjectsPath(projectName) + projectName;
        }

        /// <summary>
        /// Настройка Lua объекта.
        /// </summary>
        [ExcludeFromCodeCoverage]
        private static Lua PrepareLuaState()
        {
            var lua = new Lua();

            lua.RegisterFunction("AddSignal", null,
                typeof(ModbusExchangeLoader).GetMethod(nameof(AddSignal)));

            lua.RegisterFunction("GetGroup", null,
                typeof(ModbusExchangeLoader).GetMethod(nameof(GetGroup)));

            string sysLuaPath = Path.Combine(
                ProjectManager.GetInstance().SystemFilesPath,
                "sys_modbus_exchange.lua");

            using var luaReader = new StreamReader(sysLuaPath,
                EncodingDetector.DetectFileEncoding(sysLuaPath), true);

            var chunk = luaReader.ReadToEnd();
            lua.DoString(chunk);

            return lua;
        }

        /// <summary>
        /// Добавить сигнал в группу (Вызывается из Lua).
        /// </summary>
        /// <param name="group">Группа сигналов</param>
        /// <param name="description">Описание сигнала</param>
        /// <param name="devName">Название устройства</param>
        /// <param name="dataType">Тип сигнала</param>
        /// <param name="word">Адрес (слово)</param>
        /// <param name="bit">Адрес (бит)</param>
        public static void AddSignal(IGroup group, string description,
            string devName, string dataType, int word, int bit) 
        {
            var signal = new Signal(description, dataType, word, bit);
            var dev = DeviceManager.GetInstance().GetDevice(devName);

            if (dev.Description != CommonConst.Cap)
            {
                signal.Device = dev;
            }

            group.Add(signal);
        }

        /// <summary>
        /// Получить подгруппу в группе.
        /// </summary>
        /// <param name="group">Группа</param>
        /// <param name="description">Описание подгруппы</param>
        public static IGroup GetGroup(IGroup group, string description)
        {
            if (description == string.Empty)
                return group;

            var subgroup = group.Items.OfType<IGroup>()
                .FirstOrDefault(g => g.Description == description);

            if (subgroup is null)
            {
                subgroup = new Model.Group(description);
                group.Add(subgroup);
            }

            return subgroup;
        }
    }
}
