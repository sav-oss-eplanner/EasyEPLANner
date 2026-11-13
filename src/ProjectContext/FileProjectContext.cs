using System;
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

        public static bool IsValidProjectFolder(string projectFolderPath)
        {
            return TryValidateProjectFolder(projectFolderPath,
                out _, out _);
        }

        public static bool TryValidateProjectFolder(string projectFolderPath,
            out string fullPath, out string errorReason)
        {
            fullPath = projectFolderPath;
            errorReason = null;

            if (string.IsNullOrWhiteSpace(projectFolderPath))
            {
                errorReason = "путь к папке не указан";
                return false;
            }

            try
            {
                fullPath = Path.GetFullPath(projectFolderPath);
            }
            catch (Exception ex) when (ex is ArgumentException ||
                ex is NotSupportedException || ex is PathTooLongException ||
                ex is System.Security.SecurityException)
            {
                errorReason = "указан некорректный путь к папке";
                return false;
            }

            if (!Directory.Exists(fullPath))
            {
                errorReason = "папка не существует или недоступна";
                return false;
            }

            if (!File.Exists(Path.Combine(fullPath,
                ProjectDescriptionSaver.MainTechObjectsFileName)))
            {
                errorReason = "отсутствует обязательный файл " +
                    ProjectDescriptionSaver.MainTechObjectsFileName;
                return false;
            }

            return true;
        }

        public string ProjectName { get; }

        public string ProjectFolderPath { get; }

        public string SystemFilesPath { get; }

        public string OriginalAssemblyPath { get; }
    }
}
