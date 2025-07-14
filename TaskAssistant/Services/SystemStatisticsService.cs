using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
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
        /// 获取私有内存使用量（MB）
        /// </summary>
        /// <returns>私有内存使用量</returns>
        double GetPrivateMemoryUsage();

        /// <summary>
        /// 获取CPU使用率（百分比）
        /// </summary>
        /// <returns>CPU使用率</returns>
        Task<double> GetCpuUsageAsync();

        /// <summary>
        /// 获取详细的性能报告
        /// </summary>
        /// <returns>性能报告</returns>
        Task<PerformanceReport> GetPerformanceReportAsync();

        /// <summary>
        /// 开始性能监控
        /// </summary>
        void StartPerformanceMonitoring();

        /// <summary>
        /// 停止性能监控
        /// </summary>
        void StopPerformanceMonitoring();

        /// <summary>
        /// 获取监控历史数据
        /// </summary>
        /// <param name="minutes">获取最近几分钟的数据</param>
        /// <returns>监控数据</returns>
        IEnumerable<PerformanceSnapshot> GetMonitoringHistory(int minutes = 60);

        /// <summary>
        /// 获取专用工作集内存使用量（MB）
        /// </summary>
        /// <returns>专用工作集内存使用量</returns>
        double GetPrivateWorkingSetUsage();
    }

    /// <summary>
    /// 性能快照
    /// </summary>
    public record PerformanceSnapshot(
        DateTime Timestamp,
        double MemoryUsageMB,
        double CpuUsagePercent,
        long WorkingSetBytes,
        long GCMemoryBytes,
        int ThreadCount
    );

    /// <summary>
    /// 详细性能报告
    /// </summary>
    public class PerformanceReport
    {
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public double MemoryUsageMB { get; set; }
        public double CpuUsagePercent { get; set; }
        public double DatabaseSizeMB { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public TimeSpan Uptime { get; set; }
        public long WorkingSetBytes { get; set; }
        public long GCMemoryBytes { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
        public bool IsLargeObjectHeapCompacting { get; set; }
        public double AverageMemoryUsageMB { get; set; }
        public double PeakMemoryUsageMB { get; set; }
        public double AverageCpuUsagePercent { get; set; }
        public double PeakCpuUsagePercent { get; set; }
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// 系统统计服务实现（优化版本）
    /// </summary>
    public class SystemStatisticsService : ISystemStatisticsService, IDisposable
    {
        #region Windows API 声明

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_MEMORY_COUNTERS_EX
        {
            public uint cb;
            public uint PageFaultCount;
            public nuint PeakWorkingSetSize;
            public nuint WorkingSetSize;
            public nuint QuotaPeakPagedPoolUsage;
            public nuint QuotaPagedPoolUsage;
            public nuint QuotaPeakNonPagedPoolUsage;
            public nuint QuotaNonPagedPoolUsage;
            public nuint PagefileUsage;
            public nuint PeakPagefileUsage;
            public nuint PrivateUsage; // 这是专用内存集
        }

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS_EX counters, uint size);

        #endregion

        #region 私有字段

        private readonly IDataService _dataService;
        private readonly Process _currentProcess;
        private readonly PerformanceCounter? _cpuCounter;
        private readonly Timer? _monitoringTimer;
        private readonly ConcurrentQueue<PerformanceSnapshot> _performanceHistory = new();
        
        private DateTime _lastCpuTime = DateTime.UtcNow;
        private TimeSpan _lastTotalProcessorTime = TimeSpan.Zero;
        private DateTime _processStartTime;
        private bool _isMonitoring = false;
        private bool _disposed = false;

        // 缓存
        private readonly object _cacheLock = new();
        private DateTime _lastStatisticsUpdate = DateTime.MinValue;
        private SystemStatistics? _cachedStatistics;
        private readonly TimeSpan _cacheValidityPeriod = TimeSpan.FromSeconds(30);

        // 性能计数器
        private double _peakMemoryUsage = 0;
        private double _peakCpuUsage = 0;
        private readonly ConcurrentQueue<double> _memoryReadings = new();
        private readonly ConcurrentQueue<double> _cpuReadings = new();

        #endregion

        #region 构造函数

        public SystemStatisticsService(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _currentProcess = Process.GetCurrentProcess();
            _processStartTime = _currentProcess.StartTime;

            try
            {
                // 尝试创建CPU性能计数器
                _cpuCounter = new PerformanceCounter("Process", "% Processor Time", _currentProcess.ProcessName);
                _cpuCounter.NextValue(); // 第一次调用通常返回0，预热计数器
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建CPU性能计数器失败: {ex.Message}");
                // 如果无法创建性能计数器，将使用备用方法
            }

            // 启动监控定时器
            _monitoringTimer = new Timer(MonitoringCallback, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            // 注册到资源管理器
            TaskAssistant.Common.ResourceManager.RegisterResource(this);
        }

        #endregion

        #region 性能监控

        /// <summary>
        /// 开始性能监控
        /// </summary>
        public void StartPerformanceMonitoring()
        {
            _isMonitoring = true;
            System.Diagnostics.Debug.WriteLine("性能监控已启动");
        }

        /// <summary>
        /// 停止性能监控
        /// </summary>
        public void StopPerformanceMonitoring()
        {
            _isMonitoring = false;
            System.Diagnostics.Debug.WriteLine("性能监控已停止");
        }

        /// <summary>
        /// 监控回调
        /// </summary>
        private async void MonitoringCallback(object? state)
        {
            if (!_isMonitoring || _disposed) return;

            try
            {
                var memoryUsage = GetMemoryUsage();
                var cpuUsage = await GetCpuUsageAsync().ConfigureAwait(false);

                // 更新峰值
                _peakMemoryUsage = Math.Max(_peakMemoryUsage, memoryUsage);
                _peakCpuUsage = Math.Max(_peakCpuUsage, cpuUsage);

                // 记录读数
                _memoryReadings.Enqueue(memoryUsage);
                _cpuReadings.Enqueue(cpuUsage);

                // 限制历史数据大小（保留最近24小时的数据，每10秒一个点）
                const int maxReadings = 24 * 60 * 6; // 24小时 * 60分钟 * 6个点/分钟
                while (_memoryReadings.Count > maxReadings)
                {
                    _memoryReadings.TryDequeue(out _);
                }
                while (_cpuReadings.Count > maxReadings)
                {
                    _cpuReadings.TryDequeue(out _);
                }

                // 创建性能快照
                var snapshot = new PerformanceSnapshot(
                    DateTime.Now,
                    memoryUsage,
                    cpuUsage,
                    _currentProcess.WorkingSet64,
                    GC.GetTotalMemory(false),
                    _currentProcess.Threads.Count
                );

                _performanceHistory.Enqueue(snapshot);

                // 限制历史数据大小
                while (_performanceHistory.Count > maxReadings)
                {
                    _performanceHistory.TryDequeue(out _);
                }

                // 检查性能警告
                CheckPerformanceWarnings(memoryUsage, cpuUsage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"性能监控回调失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查性能警告
        /// </summary>
        private static void CheckPerformanceWarnings(double memoryUsage, double cpuUsage)
        {
            const double MemoryWarningThreshold = 1000; // 1GB
            const double CpuWarningThreshold = 80; // 80%

            if (memoryUsage > MemoryWarningThreshold)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ 内存使用警告: {memoryUsage:F2} MB (阈值: {MemoryWarningThreshold} MB)");
            }

            if (cpuUsage > CpuWarningThreshold)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ CPU使用警告: {cpuUsage:F1}% (阈值: {CpuWarningThreshold}%)");
            }
        }

        /// <summary>
        /// 获取监控历史数据
        /// </summary>
        public IEnumerable<PerformanceSnapshot> GetMonitoringHistory(int minutes = 60)
        {
            var cutoffTime = DateTime.Now.AddMinutes(-minutes);
            return _performanceHistory
                .Where(snapshot => snapshot.Timestamp >= cutoffTime)
                .OrderBy(snapshot => snapshot.Timestamp)
                .ToList();
        }

        #endregion

        #region 系统统计

        /// <summary>
        /// 获取系统统计信息（优化版本，支持缓存）
        /// </summary>
        public async Task<SystemStatistics> GetSystemStatisticsAsync()
        {
            lock (_cacheLock)
            {
                if (_cachedStatistics != null && 
                    DateTime.Now - _lastStatisticsUpdate < _cacheValidityPeriod)
                {
                    return _cachedStatistics;
                }
            }

            var statistics = new SystemStatistics();

            try
            {
                // 并行获取各种统计信息
                var tasks = new[]
                {
                    GetScriptStatisticsAsync(statistics),
                    GetTaskStatisticsAsync(statistics),
                    GetSystemResourcesAsync(statistics)
                };

                await Task.WhenAll(tasks).ConfigureAwait(false);

                statistics.LastUpdated = DateTime.Now;

                // 更新缓存
                lock (_cacheLock)
                {
                    _cachedStatistics = statistics;
                    _lastStatisticsUpdate = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取系统统计信息失败: {ex.Message}");
                
                // 返回默认统计信息
                statistics.LastUpdated = DateTime.Now;
            }

            return statistics;
        }

        /// <summary>
        /// 获取脚本统计信息
        /// </summary>
        private async Task GetScriptStatisticsAsync(SystemStatistics statistics)
        {
            try
            {
                var scripts = await _dataService.GetScriptsAsync().ConfigureAwait(false);
                var scriptList = scripts.ToList();
                
                statistics.TotalScripts = scriptList.Count;

                // 计算今日执行次数
                var today = DateTime.Today;
                statistics.TodayExecutions = scriptList
                    .Where(s => s.LastExecuted.HasValue && s.LastExecuted.Value.Date == today)
                    .Sum(s => s.ExecutionCount);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取脚本统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取任务统计信息
        /// </summary>
        private async Task GetTaskStatisticsAsync(SystemStatistics statistics)
        {
            try
            {
                var taskStats = await _dataService.GetTaskStatisticsAsync().ConfigureAwait(false);
                statistics.TotalTasks = taskStats.TotalTasks;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取任务统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取系统资源信息
        /// </summary>
        private async Task GetSystemResourcesAsync(SystemStatistics statistics)
        {
            try
            {
                // 并行获取资源信息
                var tasks = new[]
                {
                    GetDatabaseSizeAsync(),
                    GetCpuUsageAsync()
                };

                var results = await Task.WhenAll(tasks).ConfigureAwait(false);

                statistics.DatabaseSizeMB = results[0];
                statistics.MemoryUsageMB = GetPrivateWorkingSetUsage(); // 专用工作集内存（与任务管理器一致）
                statistics.WorkingSetMemoryUsageMB = GetMemoryUsage(); // 工作集内存
                statistics.PrivateMemoryUsageMB = GetPrivateMemoryUsage(); // 私有内存
                statistics.PrivateWorkingSetMemoryUsageMB = GetPrivateWorkingSetUsage(); // 专用工作集内存
                statistics.CpuUsagePercent = results[1];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取系统资源信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取详细的性能报告
        /// </summary>
        public async Task<PerformanceReport> GetPerformanceReportAsync()
        {
            var report = new PerformanceReport();

            try
            {
                // 基本信息
                report.MemoryUsageMB = GetMemoryUsage();
                report.CpuUsagePercent = await GetCpuUsageAsync().ConfigureAwait(false);
                report.DatabaseSizeMB = await GetDatabaseSizeAsync().ConfigureAwait(false);

                // 进程信息
                _currentProcess.Refresh();
                report.ThreadCount = _currentProcess.Threads.Count;
                report.HandleCount = _currentProcess.HandleCount;
                report.Uptime = DateTime.Now - _processStartTime;
                report.WorkingSetBytes = _currentProcess.WorkingSet64;

                // GC信息
                report.GCMemoryBytes = GC.GetTotalMemory(false);
                report.Gen0Collections = GC.CollectionCount(0);
                report.Gen1Collections = GC.CollectionCount(1);
                report.Gen2Collections = GC.CollectionCount(2);
                report.IsLargeObjectHeapCompacting = false; // .NET 8 中没有直接的检查方法

                // 平均值和峰值
                var memoryReadings = _memoryReadings.ToArray();
                var cpuReadings = _cpuReadings.ToArray();

                if (memoryReadings.Length > 0)
                {
                    report.AverageMemoryUsageMB = memoryReadings.Average();
                    report.PeakMemoryUsageMB = Math.Max(_peakMemoryUsage, memoryReadings.Max());
                }
                else
                {
                    report.AverageMemoryUsageMB = report.MemoryUsageMB;
                    report.PeakMemoryUsageMB = report.MemoryUsageMB;
                }

                if (cpuReadings.Length > 0)
                {
                    report.AverageCpuUsagePercent = cpuReadings.Average();
                    report.PeakCpuUsagePercent = Math.Max(_peakCpuUsage, cpuReadings.Max());
                }
                else
                {
                    report.AverageCpuUsagePercent = report.CpuUsagePercent;
                    report.PeakCpuUsagePercent = report.CpuUsagePercent;
                }

                // 生成建议
                GenerateRecommendations(report);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"生成性能报告失败: {ex.Message}");
            }

            return report;
        }

        /// <summary>
        /// 生成性能优化建议
        /// </summary>
        private static void GenerateRecommendations(PerformanceReport report)
        {
            var recommendations = report.Recommendations;

            // 内存建议
            if (report.PeakMemoryUsageMB > 1000)
            {
                recommendations.Add("💾 内存使用过高，建议定期执行内存清理或减少同时打开的窗口数量");
            }

            if (report.AverageMemoryUsageMB > 500)
            {
                recommendations.Add("🧹 建议启用自动内存清理功能以优化内存使用");
            }

            // CPU建议
            if (report.PeakCpuUsagePercent > 80)
            {
                recommendations.Add("⚡ CPU使用率过高，建议减少并发脚本执行数量");
            }

            // GC建议
            if (report.Gen2Collections > 100)
            {
                recommendations.Add("🗑️ 第二代垃圾回收频繁，建议优化大对象的使用");
            }

            // 线程建议
            if (report.ThreadCount > 50)
            {
                recommendations.Add("🧵 线程数量较多，建议检查是否有线程泄漏");
            }

            // 句柄建议
            if (report.HandleCount > 1000)
            {
                recommendations.Add("🔗 句柄数量过多，建议检查资源释放是否正确");
            }

            // 数据库建议
            if (report.DatabaseSizeMB > 100)
            {
                recommendations.Add("🗄️ 数据库较大，建议定期清理过期数据");
            }

            // 运行时间建议
            if (report.Uptime.TotalHours > 24)
            {
                recommendations.Add("⏰ 应用程序运行时间较长，建议定期重启以释放资源");
            }

            // 默认建议
            if (recommendations.Count == 0)
            {
                recommendations.Add("✅ 系统运行状况良好，无需特别优化");
            }
        }

        #endregion

        #region 具体指标获取

        /// <summary>
        /// 获取数据库大小（优化版本，支持缓存）
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
                    return await Task.Run(() => Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2))
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取数据库大小失败: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// 获取内存使用量（优化版本）
        /// </summary>
        public double GetMemoryUsage()
        {
            try
            {
                _currentProcess.Refresh();
                return Math.Round(_currentProcess.WorkingSet64 / (1024.0 * 1024.0), 2);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取内存使用量失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 获取私有内存使用量（优化版本，更准确地反映进程实际内存占用）
        /// </summary>
        public double GetPrivateMemoryUsage()
        {
            try
            {
                _currentProcess.Refresh();
                return Math.Round(_currentProcess.PrivateMemorySize64 / (1024.0 * 1024.0), 2);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取私有内存使用量失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 获取专用工作集内存使用量（与任务管理器"专用内存集"一致）
        /// </summary>
        /// <returns>专用工作集内存使用量</returns>
        public double GetPrivateWorkingSetUsage()
        {
            try
            {
                _currentProcess.Refresh();
                
                // 尝试使用Windows API获取专用内存集
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        if (GetProcessMemoryInfo(_currentProcess.Handle, out var memInfo, (uint)Marshal.SizeOf<PROCESS_MEMORY_COUNTERS_EX>()))
                        {
                            // PrivateUsage 就是任务管理器中显示的"专用内存集"
                            return Math.Round((double)memInfo.PrivateUsage / (1024.0 * 1024.0), 2);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"使用Windows API获取专用内存集失败: {ex.Message}");
                    }
                }
                
                // 降级：使用 PrivateMemorySize64 作为备选方案
                return Math.Round(_currentProcess.PrivateMemorySize64 / (1024.0 * 1024.0), 2);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取专用工作集内存使用量失败: {ex.Message}");
                return GetMemoryUsage(); // 最后降级到工作集内存
            }
        }

        /// <summary>
        /// 获取CPU使用率（优化版本）
        /// </summary>
        public async Task<double> GetCpuUsageAsync()
        {
            try
            {
                // 优先使用性能计数器
                if (_cpuCounter != null)
                {
                    return await Task.Run(() =>
                    {
                        try
                        {
                            var usage = _cpuCounter.NextValue() / Environment.ProcessorCount;
                            return Math.Round(Math.Max(0, Math.Min(100, usage)), 1);
                        }
                        catch
                        {
                            return GetCpuUsageFallback();
                        }
                    }).ConfigureAwait(false);
                }

                // 使用备用方法
                return GetCpuUsageFallback();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取CPU使用率失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// CPU使用率备用方法
        /// </summary>
        private double GetCpuUsageFallback()
        {
            try
            {
                var currentTime = DateTime.UtcNow;
                _currentProcess.Refresh();

                var currentTotalProcessorTime = _currentProcess.TotalProcessorTime;
                var cpuUsage = (currentTotalProcessorTime - _lastTotalProcessorTime).TotalMilliseconds /
                               (currentTime - _lastCpuTime).TotalMilliseconds /
                               Environment.ProcessorCount * 100;

                _lastCpuTime = currentTime;
                _lastTotalProcessorTime = currentTotalProcessorTime;

                // 第一次调用时返回0
                if (_lastTotalProcessorTime == TimeSpan.Zero)
                {
                    return 0;
                }

                return Math.Round(Math.Max(0, Math.Min(100, cpuUsage)), 1);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CPU使用率备用方法失败: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region 资源释放

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _isMonitoring = false;
                _monitoringTimer?.Dispose();
                _cpuCounter?.Dispose();
                _currentProcess?.Dispose();
                _disposed = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"释放 SystemStatisticsService 资源时发生异常: {ex.Message}");
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~SystemStatisticsService()
        {
            Dispose();
        }

        #endregion
    }
}