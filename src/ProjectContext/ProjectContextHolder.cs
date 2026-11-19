namespace EasyEPlanner
{
    /// <summary>
    /// Текущий контекст проекта для хоста (EPLAN или App).
    /// </summary>
    public static class ProjectContextHolder
    {
        public static IProjectContext Current { get; set; }
    }
}
