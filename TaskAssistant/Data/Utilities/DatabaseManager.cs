using TaskAssistant.Data.Repositories;
using TaskAssistant.Data.Services;

namespace TaskAssistant.Data.Utilities
{
    /// <summary>
    /// ?据?管理工具?
    /// 提供?据???、?份、?原等?用功能
    /// </summary>
    public static class DatabaseManager
    {
        /// <summary>
        /// ?取?据???信息
        /// </summary>
        /// <returns>?据???信息</returns>
        public static async Task<DatabaseStatistics> GetStatisticsAsync()
        {
            var dataService = App.GetRequiredService<IDataService>();
            
            var scriptStats = await dataService.Scripts.CountAsync(s => true);
            var enabledScripts = await dataService.Scripts.CountAsync(s => s.IsEnabled);
            var taskStats = await dataService.GetTaskStatisticsAsync();
            
            return new DatabaseStatistics
            {
                TotalScripts = scriptStats,
                EnabledScripts = enabledScripts,
                DisabledScripts = scriptStats - enabledScripts,
                TaskStatistics = taskStats
            };
        }

        /// <summary>
        /// ?份?据?
        /// </summary>
        /// <param name="backupPath">?份文件路?</param>
        /// <returns>是否?份成功</returns>
        public static async Task<bool> BackupAsync(string? backupPath = null)
        {
            try
            {
                var dataService = App.GetRequiredService<IDataService>();
                
                // 如果?有指定路?，使用默?路?
                backupPath ??= GetDefaultBackupPath();
                
                return await dataService.BackupDatabaseAsync(backupPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"?据??份失?: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ?原?据?
        /// </summary>
        /// <param name="backupPath">?份文件路?</param>
        /// <returns>是否?原成功</returns>
        public static async Task<bool> RestoreAsync(string backupPath)
        {
            try
            {
                var dataService = App.GetRequiredService<IDataService>();
                return await dataService.RestoreDatabaseAsync(backupPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"?据??原失?: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 清理?期?据
        /// </summary>
        /// <param name="daysToKeep">保留天?</param>
        /// <returns>清理的???量</returns>
        public static async Task<int> CleanupAsync(int daysToKeep = 90)
        {
            try
            {
                var dataService = App.GetRequiredService<IDataService>();
                return await dataService.CleanupOldDataAsync(daysToKeep);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"?据?清理失?: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// ?取默??份路?
        /// </summary>
        /// <returns>默??份文件路?</returns>
        private static string GetDefaultBackupPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var backupFolder = System.IO.Path.Combine(appDataPath, "TaskAssistant", "Backups");
            
            if (!System.IO.Directory.Exists(backupFolder))
            {
                System.IO.Directory.CreateDirectory(backupFolder);
            }
            
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return System.IO.Path.Combine(backupFolder, $"TaskAssistant_Backup_{timestamp}.db");
        }
    }

    /// <summary>
    /// ?据???信息
    /// </summary>
    public class DatabaseStatistics
    {
        /// <summary>
        /// ?本??
        /// </summary>
        public int TotalScripts { get; set; }

        /// <summary>
        /// ?用的?本?
        /// </summary>
        public int EnabledScripts { get; set; }

        /// <summary>
        /// 禁用的?本?
        /// </summary>
        public int DisabledScripts { get; set; }

        /// <summary>
        /// 任???信息
        /// </summary>
        public TaskStatistics TaskStatistics { get; set; } = new();

        /// <summary>
        /// ?取??摘要
        /// </summary>
        /// <returns>??摘要文本</returns>
        public string GetSummary()
        {
            return $"?本: {TotalScripts} ? (?用: {EnabledScripts}, 禁用: {DisabledScripts})\n" +
                   $"任?: {TaskStatistics.TotalTasks} ? (待?行: {TaskStatistics.PendingTasks}, " +
                   $"?行中: {TaskStatistics.RunningTasks}, 已完成: {TaskStatistics.CompletedTasks})\n" +
                   $"成功率: {TaskStatistics.SuccessRate:F1}%";
        }
    }
}