using TaskAssistant.Data.Repositories;
using TaskAssistant.Data.Services;

namespace TaskAssistant.Data.Utilities
{
    /// <summary>
    /// ?�u?�޲z�u��?
    /// ����?�u???�B?���B?�쵥?�Υ\��
    /// </summary>
    public static class DatabaseManager
    {
        /// <summary>
        /// ?��?�u???�H��
        /// </summary>
        /// <returns>?�u???�H��</returns>
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
        /// ?��?�u?
        /// </summary>
        /// <param name="backupPath">?������?</param>
        /// <returns>�O�_?�����\</returns>
        public static async Task<bool> BackupAsync(string? backupPath = null)
        {
            try
            {
                var dataService = App.GetRequiredService<IDataService>();
                
                // �p�G?�����w��?�A�ϥ��q?��?
                backupPath ??= GetDefaultBackupPath();
                
                return await dataService.BackupDatabaseAsync(backupPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"?�u??����?: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ?��?�u?
        /// </summary>
        /// <param name="backupPath">?������?</param>
        /// <returns>�O�_?�즨�\</returns>
        public static async Task<bool> RestoreAsync(string backupPath)
        {
            try
            {
                var dataService = App.GetRequiredService<IDataService>();
                return await dataService.RestoreDatabaseAsync(backupPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"?�u??�쥢?: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// �M�z?��?�u
        /// </summary>
        /// <param name="daysToKeep">�O�d��?</param>
        /// <returns>�M�z��???�q</returns>
        public static async Task<int> CleanupAsync(int daysToKeep = 90)
        {
            try
            {
                var dataService = App.GetRequiredService<IDataService>();
                return await dataService.CleanupOldDataAsync(daysToKeep);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"?�u?�M�z��?: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// ?���q??����?
        /// </summary>
        /// <returns>�q??������?</returns>
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
    /// ?�u???�H��
    /// </summary>
    public class DatabaseStatistics
    {
        /// <summary>
        /// ?��??
        /// </summary>
        public int TotalScripts { get; set; }

        /// <summary>
        /// ?�Ϊ�?��?
        /// </summary>
        public int EnabledScripts { get; set; }

        /// <summary>
        /// �T�Ϊ�?��?
        /// </summary>
        public int DisabledScripts { get; set; }

        /// <summary>
        /// ��???�H��
        /// </summary>
        public TaskStatistics TaskStatistics { get; set; } = new();

        /// <summary>
        /// ?��??�K�n
        /// </summary>
        /// <returns>??�K�n�奻</returns>
        public string GetSummary()
        {
            return $"?��: {TotalScripts} ? (?��: {EnabledScripts}, �T��: {DisabledScripts})\n" +
                   $"��?: {TaskStatistics.TotalTasks} ? (��?��: {TaskStatistics.PendingTasks}, " +
                   $"?�椤: {TaskStatistics.RunningTasks}, �w����: {TaskStatistics.CompletedTasks})\n" +
                   $"���\�v: {TaskStatistics.SuccessRate:F1}%";
        }
    }
}