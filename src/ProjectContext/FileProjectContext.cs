using System.IO;

namespace EasyEPlanner
{
    /// <summary>
    /// Контекст проекта, открытого из папки на диске (standalone App).
    /// </summary>
    public class FileProjectContext : IProjectContext
    {
        public FileProjectContext(string projectFolderPath,
            string systemFilesPath, string originalAssemblyPath)
        {
            ProjectFolderPath = Path.GetFullPath(projectFolderPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            ProjectName = Path.GetFileName(ProjectFolderPath);
            SystemFilesPath = systemFilesPath;
            OriginalAssemblyPath = originalAssemblyPath;
        }

        public string ProjectName { get; }

        public string ProjectFolderPath { get; }

        public string SystemFilesPath { get; }

        public string OriginalAssemblyPath { get; }
    }
}
