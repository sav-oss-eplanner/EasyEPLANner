using System;
using System.Collections.Generic;

namespace EasyEPlanner
{
    /// <summary>
    /// Слепок устройства из main.io.lua (без записи в менеджеры).
    /// </summary>
    public sealed class DeviceLuaSnapshot
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string SubType { get; set; } = string.Empty;

        public Dictionary<string, string> NamedParameters { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public List<string> OrderedParameters { get; } = [];

        public Dictionary<string, string> NamedRuntimeParameters { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public List<string> OrderedRuntimeParameters { get; } = [];

        public Dictionary<string, string> Properties { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
