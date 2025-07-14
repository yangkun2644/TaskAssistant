using TaskAssistant.Data.Repositories;
using TaskAssistant.Data.Services;

namespace TaskAssistant.Data.Utilities
{
    /// <summary>
    /// 数据管理工具
    /// 提供数据统计、备份、还原等实用功能
    /// </summary>
    public static class DatabaseManager
    {
        /// <summary>
        /// 获取数据库统计信息
        /// </summary>
        /// <returns>数据库统计信息</returns>
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
        /// 备份数据库
        /// </summary>
        /// <param name="backupPath">备份文件路径</param>
        /// <returns>是否备份成功</returns>
        public static async Task<bool> BackupAsync(string? backupPath = null)
        {
            try
            {
                var dataService = App.GetRequiredService<IDataService>();

                // 如果没有指定路径，使用默认路径
                backupPath ??= GetDefaultBackupPath();
                
                return await dataService.BackupDatabaseAsync(backupPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"数据库备份失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 还原数据库
        /// </summary>
        /// <param name="backupPath">备份文件路径</param>
        /// <returns>是否还原成功</returns>
        public static async Task<bool> RestoreAsync(string backupPath)
        {
            try
            {
                var dataService = App.GetRequiredService<IDataService>();
                return await dataService.RestoreDatabaseAsync(backupPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"数据库还原失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 清理过期数据
        /// </summary>
        /// <param name="daysToKeep">保留天数</param>
        /// <returns>清理的记录数量</returns>
        public static async Task<int> CleanupAsync(int daysToKeep = 90)
        {
            try
            {
                var dataService = App.GetRequiredService<IDataService>();
                return await dataService.CleanupOldDataAsync(daysToKeep);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"数据库清理失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 获取默认备份路径
        /// </summary>
        /// <returns>默认备份文件路径</returns>
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
    /// 数据统计信息
    /// </summary>
    public class DatabaseStatistics
    {
        /// <summary>
        /// 脚本总数
        /// </summary>
        public int TotalScripts { get; set; }

        /// <summary>
        /// 运行的脚本数
        /// </summary>
        public int EnabledScripts { get; set; }

        /// <summary>
        /// 禁用的脚本数
        /// </summary>
        public int DisabledScripts { get; set; }

        /// <summary>
        /// 任务统计信息
        /// </summary>
        public TaskStatistics TaskStatistics { get; set; } = new();

        /// <summary>
        /// 获取摘要
        /// </summary>
        /// <returns>摘要文本</returns>
        public string GetSummary()
        {
            return $"脚本总数: {TotalScripts} (运行: {EnabledScripts}, 禁用: {DisabledScripts})\n" +
                   $"任务总数: {TaskStatistics.TotalTasks} (待执行: {TaskStatistics.PendingTasks}, " +
                   $"运行中: {TaskStatistics.RunningTasks}, 已完成: {TaskStatistics.CompletedTasks})\n" +
                   $"成功率: {TaskStatistics.SuccessRate:F1}%";
        }
    }
}