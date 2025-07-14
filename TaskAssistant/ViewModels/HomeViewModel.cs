using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskAssistant.Models;
using TaskAssistant.Services;

namespace TaskAssistant.ViewModels
{
    /// <summary>
    /// 主页视图模型类（优化版本）
    /// 负责管理应用程序主页的数据和交互逻辑，展示系统统计信息
    /// 优化了内存监控和清理机制
    /// </summary>
    public partial class HomeViewModel : ObservableObject, IDisposable
    {
        #region 私有字段

        /// <summary>
        /// 系统统计服务
        /// </summary>
        private ISystemStatisticsService? _statisticsService;

        /// <summary>
        /// 统计数据更新定时器
        /// </summary>
        private readonly DispatcherTimer _updateTimer;

        /// <summary>
        /// 内存清理定时器
        /// </summary>
        private readonly DispatcherTimer _memoryCleanupTimer;

        /// <summary>
        /// 是否已释放资源
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// 内存峰值记录
        /// </summary>
        private double _peakMemoryUsage = 0;

        /// <summary>
        /// 上次内存清理时间
        /// </summary>
        private DateTime _lastMemoryCleanup = DateTime.Now;

        #endregion

        #region 可观察属性 - 统计数据

        /// <summary>
        /// 欢迎消息属性
        /// </summary>
        [ObservableProperty]
        private string _welcomeMessage = "今日数据统计";

        /// <summary>
        /// 脚本总数
        /// </summary>
        [ObservableProperty]
        private int _totalScripts = 0;

        /// <summary>
        /// 今日执行次数
        /// </summary>
        [ObservableProperty]
        private int _todayExecutions = 0;

        /// <summary>
        /// 任务总数
        /// </summary>
        [ObservableProperty]
        private int _totalTasks = 0;

        /// <summary>
        /// 数据库大小（MB）
        /// </summary>
        [ObservableProperty]
        private double _databaseSizeMB = 0;

        /// <summary>
        /// 内存使用量（MB）- 使用专用工作集内存（任务管理器"专用内存集"）
        /// </summary>
        [ObservableProperty]
        private double _memoryUsageMB = 0;

        /// <summary>
        /// 专用工作集内存（MB）- 显示任务管理器中的"专用内存集"
        /// </summary>
        [ObservableProperty]
        private double _privateWorkingSetMB = 0;

        /// <summary>
        /// 工作集内存（MB）- 显示系统分配的工作集内存
        /// </summary>
        [ObservableProperty]
        private double _workingSetMB = 0;

        /// <summary>
        /// 托管内存（MB）- 显示 .NET 托管堆内存
        /// </summary>
        [ObservableProperty]
        private double _managedMemoryMB = 0;

        /// <summary>
        /// 内存峰值（MB）
        /// </summary>
        [ObservableProperty]
        private double _peakMemoryMB = 0;

        /// <summary>
        /// CPU使用率（百分比）
        /// </summary>
        [ObservableProperty]
        private double _cpuUsagePercent = 0;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        [ObservableProperty]
        private string _lastUpdated = "";

        /// <summary>
        /// 是否正在加载数据
        /// </summary>
        [ObservableProperty]
        private bool _isLoading = false;

        // 格式化显示的属性
        [ObservableProperty]
        private string _databaseSizeText = "0 MB";

        [ObservableProperty]
        private string _memoryUsageText = "0 MB";

        [ObservableProperty]
        private string _privateWorkingSetText = "0 MB";

        [ObservableProperty]
        private string _workingSetText = "0 MB";

        [ObservableProperty]
        private string _managedMemoryText = "0 MB";

        [ObservableProperty]
        private string _cpuUsageText = "0%";

        [ObservableProperty]
        private string _memoryStatusText = "正常";

        private bool _isUpdateTimerEnabled = true;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化主页视图模型
        /// </summary>
        public HomeViewModel()
        {
            // 延迟初始化统计服务以提高启动速度
            Task.Run(async () =>
            {
                await Task.Delay(1000); // 延迟1秒
                _statisticsService = App.GetService<ISystemStatisticsService>() ?? 
                                    new SystemStatisticsService(App.GetService<Data.Services.IDataService>()!);
            });

            // 初始化统计数据更新定时器，大幅降低更新频率到 20 秒以提高启动速度
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _updateTimer.Tick += async (s, e) => await LoadStatisticsData();

            // 初始化内存清理定时器，每10分钟执行一次
            _memoryCleanupTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(10)
            };
            _memoryCleanupTimer.Tick += async (s, e) => await PerformMemoryCleanup();

            // 延迟启动性能监控
            Task.Run(async () =>
            {
                await Task.Delay(10000); // 延迟3秒启动
                if (_statisticsService != null)
                {
                    _statisticsService.StartPerformanceMonitoring();
                }
            });

            // 延迟加载数据并启动定时器以提高启动速度
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000); // 延迟2秒
                await LoadStatisticsData();
                App.Current.Dispatcher.Invoke(() =>
                {
                    _updateTimer.Start();
                    _memoryCleanupTimer.Start();
                });
            });
        }

        #endregion

        #region 命令 - 内存管理

        /// <summary>
        /// 手动执行内存清理命令
        /// </summary>
        [RelayCommand]
        private async Task ManualMemoryCleanup()
        {
            await PerformMemoryCleanup(force: true);
            await LoadStatisticsData();
        }

        /// <summary>
        /// 重置内存峰值命令
        /// </summary>
        [RelayCommand]
        private void ResetPeakMemory()
        {
            _peakMemoryUsage = WorkingSetMB; // 使用工作集内存重置峰值
            PeakMemoryMB = _peakMemoryUsage;
            UpdateMemoryStatusText();
        }

        #endregion

        #region 数据加载方法

        /// <summary>
        /// 加载统计数据（优化版本）
        /// </summary>
        private async Task LoadStatisticsData()
        {
            if (_disposed || IsLoading || _statisticsService == null) return;

            IsLoading = true;

            try
            {
                var statistics = await _statisticsService.GetSystemStatisticsAsync();

                // 更新基本统计
                TotalScripts = statistics.TotalScripts;
                TodayExecutions = statistics.TodayExecutions;
                TotalTasks = statistics.TotalTasks;
                DatabaseSizeMB = statistics.DatabaseSizeMB;

                // 更新内存相关统计
                ManagedMemoryMB = Math.Round(GC.GetTotalMemory(false) / (1024.0 * 1024.0), 2); // 托管内存
                PrivateWorkingSetMB = statistics.MemoryUsageMB; // 专用工作集内存（任务管理器"专用内存集"）
                WorkingSetMB = statistics.WorkingSetMemoryUsageMB; // 工作集内存
                
                // 使用专用工作集内存作为主要显示内存
                MemoryUsageMB = PrivateWorkingSetMB;
                
                // 更新峰值（使用工作集内存作为峰值计算基准）
                if (WorkingSetMB > _peakMemoryUsage)
                {
                    _peakMemoryUsage = WorkingSetMB;
                    PeakMemoryMB = _peakMemoryUsage;
                }

                CpuUsagePercent = statistics.CpuUsagePercent;

                // 更新格式化文本
                DatabaseSizeText = $"{DatabaseSizeMB:F1} MB";
                MemoryUsageText = $"{MemoryUsageMB:F1} MB"; // 专用工作集内存
                PrivateWorkingSetText = $"{PrivateWorkingSetMB:F1} MB";   // 专用工作集内存
                WorkingSetText = $"{WorkingSetMB:F1} MB";   // 工作集内存
                ManagedMemoryText = $"{ManagedMemoryMB:F1} MB";
                CpuUsageText = $"{CpuUsagePercent:F1}%";

                // 更新内存状态文本
                UpdateMemoryStatusText();

                LastUpdated = statistics.LastUpdated.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载统计数据失败: {ex.Message}");
                LastUpdated = "加载失败";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 更新内存状态文本
        /// </summary>
        private void UpdateMemoryStatusText()
        {
            // 只显示数值，不显示状态信息
            MemoryStatusText = "";
        }

        /// <summary>
        /// 执行内存清理
        /// </summary>
        /// <param name="force">是否强制清理</param>
        private async Task PerformMemoryCleanup(bool force = false)
        {
            if (_disposed) return;

            try
            {
                var timeSinceLastCleanup = DateTime.Now - _lastMemoryCleanup;
                var currentManagedMemory = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

                // 检查是否需要清理
                if (!force && currentManagedMemory < 100 && timeSinceLastCleanup.TotalMinutes < 5)
                {
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"执行内存清理，当前托管内存: {currentManagedMemory:F2} MB");

                // 使用 ResourceManager 的智能清理
                await TaskAssistant.Common.ResourceManager.PerformSmartMemoryCleanupAsync(force);

                // 额外的托管内存清理
                await Task.Run(() =>
                {
                    // 执行完整的垃圾回收
                    GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                    
                    // 尝试压缩大对象堆
                    try
                    {
                        System.Runtime.GCSettings.LargeObjectHeapCompactionMode = 
                            System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
                        GC.Collect();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"压缩大对象堆失败: {ex.Message}");
                    }
                });

                _lastMemoryCleanup = DateTime.Now;

                var newManagedMemory = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
                var freedMemory = currentManagedMemory - newManagedMemory;

                System.Diagnostics.Debug.WriteLine($"内存清理完成，释放了 {freedMemory:F2} MB 托管内存");

                // 更新显示
                if (force)
                {
                    await LoadStatisticsData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"内存清理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启用或禁用自动更新
        /// </summary>
        /// <param name="isEnabled">是否启用自动更新</param>
        public void SetUpdateTimerEnabled(bool isEnabled)
        {
            if (_isUpdateTimerEnabled != isEnabled)
            {
                _isUpdateTimerEnabled = isEnabled;

                if (_isUpdateTimerEnabled)
                {
                    _updateTimer.Start();
                    _memoryCleanupTimer.Start();
                }
                else
                {
                    _updateTimer.Stop();
                    _memoryCleanupTimer.Stop();
                }
            }
        }

        #endregion

        #region IDisposable 实现

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的具体实现
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _updateTimer?.Stop();
                _memoryCleanupTimer?.Stop();
                
                // 停止性能监控
                _statisticsService?.StopPerformanceMonitoring();
                
                _disposed = true;
            }
        }

        #endregion
    }
}