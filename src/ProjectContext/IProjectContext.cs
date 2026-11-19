namespace EasyEPlanner
{
    /// <summary>
    /// Контекст открытого проекта (пути к файлам и системным скриптам).
    /// Общий для EPLAN-надстройки и standalone-приложения.
    /// </summary>
    public interface IProjectContext
    {
        /// <summary>
        /// Имя проекта (имя папки с main.*.lua).
        /// </summary>
        string ProjectName { get; }

        /// <summary>
        /// Полный путь к папке проекта с Lua-файлами.
        /// </summary>
        string ProjectFolderPath { get; }

        /// <summary>
        /// Путь к системным Lua-скриптам (sys.lua и др.).
        /// </summary>
        string SystemFilesPath { get; }

        /// <summary>
        /// Каталог установки/сборки (для eplaner.ini и исходных Lua).
        /// </summary>
        string OriginalAssemblyPath { get; }
    }
}
