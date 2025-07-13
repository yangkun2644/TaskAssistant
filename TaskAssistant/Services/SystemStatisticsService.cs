using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TaskAssistant.Data.Services;
using TaskAssistant.Models;

namespace TaskAssistant.Services
{
    /// <summary>
    /// 系统统计服务接口
    /// </summary>
    public interface ISystemStatisticsService
    {
        /// <summary>
        /// 获取系统统计信息
        /// </summary>
        /// <returns>系统统计信息</returns>
        Task<SystemStatistics> GetSystemStatisticsAsync();

        /// <summary>
        /// 获取数据库大小（MB）
        /// </summary>
        /// <returns>数据库大小</returns>
        Task<double> GetDatabaseSizeAsync();

        /// <summary>
        /// 获取内存使用量（MB）
        /// </summary>
        /// <returns>内存使用量</returns>
        double GetMemoryUsage();

        /// <summary>
        /// 获取CPU使用率（百分比）
        /// </summary>
        /// <returns>CPU使用率</returns>
        Task<double> GetCpuUsageAsync();
    }

    /// <summary>
    /// 系统统计服务实现
    /// </summary>
    public class SystemStatisticsService : ISystemStatisticsService
    {
        private readonly IDataService _dataService;
        private readonly Process _currentProcess;
        private DateTime _lastCpuTime = DateTime.UtcNow;
        private TimeSpan _lastTotalProcessorTime = TimeSpan.Zero;

        public SystemStatisticsService(IDataService dataService)
        {
            _dataService = dataService;
            _currentProcess = Process.GetCurrentProcess();
        }

        /// <summary>
        /// 获取系统统计信息
        /// </summary>
        public async Task<SystemStatistics> GetSystemStatisticsAsync()
        {
            var statistics = new SystemStatistics();

            try
            {
                // 获取脚本统计
                var scripts = await _dataService.GetScriptsAsync();
                statistics.TotalScripts = scripts.Count();

                // 计算今日执行次数
                var today = DateTime.Today;
                statistics.TodayExecutions = scripts
                    .Where(s => s.LastExecuted.HasValue && s.LastExecuted.Value.Date == today)
                    .Sum(s => s.ExecutionCount);

                // 获取任务统计
                var taskStats = await _dataService.GetTaskStatisticsAsync();
                statistics.TotalTasks = taskStats.TotalTasks;

                // 获取系统资源信息
                statistics.DatabaseSizeMB = await GetDatabaseSizeAsync();
                statistics.MemoryUsageMB = GetMemoryUsage();
                statistics.CpuUsagePercent = await GetCpuUsageAsync();

                statistics.LastUpdated = DateTime.Now;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取系统统计信息失败: {ex.Message}");
            }

            return statistics;
        }

        /// <summary>
        /// 获取数据库大小
        /// </summary>
        public async Task<double> GetDatabaseSizeAsync()
        {
            try
            {
                // 从应用数据目录获取数据库文件路径
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TaskAssistant");
                var dbPath = Path.Combine(appDataPath, "taskassistant.db");

                if (File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    return Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2); // 转换为MB
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取数据库大小失败: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// 获取内存使用量
        /// </summary>
        public double GetMemoryUsage()
        {
            try
            {
                _currentProcess.Refresh();
                return Math.Round(_currentProcess.WorkingSet64 / (1024.0 * 1024.0), 2); // 转换为MB
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取内存使用量失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 获取CPU使用率
        /// </summary>
        public async Task<double> GetCpuUsageAsync()
        {
            try
            {
                var currentTime = DateTime.UtcNow;
                _currentProcess.Refresh();

                var currentTotalProcessorTime = _currentProcess.TotalProcessorTime;
                var currentCpuUsage = (currentTotalProcessorTime - _lastTotalProcessorTime).TotalMilliseconds /
                                      (currentTime - _lastCpuTime).TotalMilliseconds /
                                      Environment.ProcessorCount * 100;

                _lastCpuTime = currentTime;
                _lastTotalProcessorTime = currentTotalProcessorTime;

                // 第一次调用时返回0，避免异常值
                if (_lastTotalProcessorTime == TimeSpan.Zero)
                {
                    await Task.Delay(100); // 等待一小段时间后再次计算
                    return await GetCpuUsageAsync();
                }

                return Math.Round(Math.Max(0, currentCpuUsage), 1);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取CPU使用率失败: {ex.Message}");
                return 0;
            }
        }
    }
}