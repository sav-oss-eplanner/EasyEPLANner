using EasyEPlanner.ModbusExchange.Model;
using Eplan.EplApi.DataModel.E3D;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EasyEPlanner.ModbusExchange
{
    /// <summary>
    /// Сохранение файлов обмена сигналами по Modbus.
    /// </summary>
    public static class ModbusExchangeSaver
    {
        /// <summary>
        /// Сохранение обмена сигналами в Lua-файлы.
        /// </summary>
        /// <param name="exchange">Модель обмена</param>
        [ExcludeFromCodeCoverage]
        public static void Save(this IExchange exchange)
        {
            var path = GetPathToSave();

            foreach (var model in exchange.Models)
            {
                using var writer = new StreamWriter(Path.Combine(path, $"gate_{model.Name}.lua"), false);

                writer.Write(model.GetTextToSave(AssemblyVersion.GetVersionAsLuaComment()));
            }

            var modbusExchangePath = Path.Combine(path, $"modbusexchange.lua");
            if (exchange.Models.Any() && !File.Exists(modbusExchangePath))
            {
                using var reader = new StreamReader(GetModbusExchangePatternPath());
                using var writer = new StreamWriter(modbusExchangePath, false);

                writer.Write(reader.ReadToEnd());
            }
        }

        /// <summary>
        /// modbus_exchange_pattern.lua path.
        /// </summary>
        [ExcludeFromCodeCoverage]
        private static string GetModbusExchangePatternPath()
        {
            return Path.Combine(ProjectManager.GetInstance().SystemFilesPath,
                "modbus_exchange_pattern.lua");
        }

        /// <summary>
        /// Путь к папке с файлами проекта.
        /// </summary>
        [ExcludeFromCodeCoverage]
        private static string GetPathToSave()
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
        /// Сохранить <see cref="IGateway">модель шлюза</see> в виде Lua-таблицы.
        /// </summary>
        /// <param name="gateway">Модель шлюза</param>
        public static string GetTextToSave(this IGateway gateway, string versionComment)
        {
            var rslt = new StringBuilder()
                .Append($"{versionComment}\n")
                .Append($"\n")
                .Append($"local gate =\n")
                .Append($"{{\n")
                .Append($"    ip = '{gateway.IP}',\n")
                .Append($"    port = {gateway.Port},\n")
                .Append($"    read =\n")
                .Append(GetTextMainGroup(gateway.Read, "    "))
                .Append($"    write =\n")
                .Append(GetTextMainGroup(gateway.Write, "    "))
                .Append($"}}\n")
                .Append($"\n")
                .Append($"local s, mt = pcall( require, 'modbusexchange' )\n")
                .Append($"mt = s and mt or {{ }}\n")
                .Append($"return setmetatable( gate, mt )");

            return rslt.ToString();
        }

        /// <summary>
        /// Сохранение основной группы (чтение/запись) в виде Lua-таблицы.
        /// </summary>
        /// <param name="group">Группа сигналов</param>
        /// <param name="prefix">Отступ</param>
        private static StringBuilder GetTextMainGroup(IGroup group, string prefix)
        {
            var rslt = new StringBuilder()
                .Append($"{prefix}{{\n")
                .Append($"{prefix}    offset = 0,\n")
                .Append(GetTextGroup(group, prefix + "    "))
                .Append(string.Join("", group.Items.OfType<IGroup>()
                    .Select(g => GetTextGroup(g, prefix + "    "))))
                .Append($"{prefix}}},\n");

            return rslt;
        }

        /// <summary>
        /// Сохранение группы сигналов в виде Lua-таблицы.
        /// </summary>
        /// <param name="group">Группа сигналов</param>
        /// <param name="prefix">Отступ</param>
        private static string GetTextGroup(IGroup group, string prefix)
        {
            var name = group.Description is Gateway.READ or Gateway.WRITE ? 
                string.Empty : group.Description;

            var signals = group.Items.OfType<ISignal>();

            if (!signals.Any())
                return string.Empty;

            var rslt = new StringBuilder()
                .Append($"{prefix}{{\n")
                .Append($"{prefix}    name = '{name}',\n")
                .Append($"{prefix}    offset = {group.Offset},\n")
                .Append(string.Join("", signals
                    .Select(s => GetTextSignal(s, prefix + "    "))))
                .Append($"{prefix}}},\n");

            return rslt.ToString();
        }

        /// <summary>
        /// Сохранение сигнала в виде Lua-таблицы.
        /// </summary>
        /// <param name="signal">Сигнал</param>
        /// <param name="prefix">Отступ</param>
        private static string GetTextSignal(ISignal signal, string prefix)
        {
            return $"{prefix}{{ {signal.Word}," +
                $" {signal.Bit}," +
                $" '{signal.DataType}'," +
                $" '{signal.Device?.Name ?? string.Empty}'," +
                $" '{signal.Description}' }},\n";
        }
    }
}
