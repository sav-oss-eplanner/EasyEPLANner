using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace EasyEPlanner
{
    /// <summary>
    /// Контекст проекта, открытого в EPLAN.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class EplanProjectContext : IProjectContext
    {
        public string ProjectName =>
            EProjectManager.GetInstance().GetCurrentProjectName() ?? string.Empty;

        public string ProjectFolderPath
        {
            get
            {
                if (string.IsNullOrEmpty(ProjectName))
                    return string.Empty;

                return ProjectManager.GetInstance()
                    .GetPtusaProjectsPath(ProjectName) + ProjectName;
            }
        }

        /// <summary>
        /// Путь к системным Lua в теневой копии сборки (скрипты после
        /// CopySystemFiles). Описания базовых объектов читать через
        /// <see cref="ProjectManager.OriginalSystemFilesPath"/>.
        /// </summary>
        public string SystemFilesPath =>
            Path.Combine(ProjectManager.GetInstance().AssemblyPath, "Lua");

        public string OriginalAssemblyPath =>
            Path.GetDirectoryName(AddInModule.OriginalAssemblyPath);
    }
}
