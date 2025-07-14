using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TaskAssistant.Common
{
    /// <summary>
    /// 全局资源管理器（优化版本）
    /// 提供统一的资源管理、窗口跟踪、内存清理等功能
    /// 支持高性能的并发操作和自动内存管理
    /// </summary>
    public static class ResourceManager
    {
        #region 私有字段

        /// <summary>
        /// 线程安全的锁对象
        /// </summary>
        private static readonly object _lockObject = new();

        /// <summary>
        /// 跟踪的窗口集合（使用并发集合提高性能）
        /// </summary>
        private static readonly ConcurrentBag<Window> _trackedWindows = new();

        /// <summary>
        /// 已释放的资源集合（使用并发集合提高性能）
        /// </summary>
        private static readonly ConcurrentBag<IDisposable> _disposableResources = new();

        /// <summary>
        /// 全局取消令牌源
        /// </summary>
        private static CancellationTokenSource? _globalCancellationTokenSource;

        /// <summary>
        /// 应用程序是否正在关闭
        /// </summary>
        private static volatile bool _isShuttingDown = false;

        /// <summary>
        /// 内存清理定时器
        /// </summary>
        private static Timer? _memoryCleanupTimer;

        /// <summary>
        /// 上次内存清理时间
        /// </summary>
        private static DateTime _lastMemoryCleanup = DateTime.Now;

        /// <summary>
        /// 内存使用监控器
        /// </summary>
        private static Process? _currentProcess;

        /// <summary>
        /// 内存清理间隔（分钟）
        /// </summary>
        private const int MemoryCleanupIntervalMinutes = 10;

        /// <summary>
        /// 内存使用阈值（MB）
        /// </summary>
        private const long MemoryThresholdMB = 500;

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取应用程序是否正在关闭的状态
        /// </summary>
        public static bool IsShuttingDown => _isShuttingDown;

        /// <summary>
        /// 获取全局取消令牌
        /// </summary>
        public static CancellationToken GlobalCancellationToken =>
            _globalCancellationTokenSource?.Token ?? CancellationToken.None;

        /// <summary>
        /// 获取当前内存使用量（MB）
        /// </summary>
        public static double CurrentMemoryUsageMB
        {
            get
            {
                try
                {
                    _currentProcess ??= Process.GetCurrentProcess();
                    _currentProcess.Refresh();
                    return Math.Round(_currentProcess.WorkingSet64 / (1024.0 * 1024.0), 2);
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// 获取跟踪的窗口数量
        /// </summary>
        public static int TrackedWindowsCount => _trackedWindows.Count;

        /// <summary>
        /// 获取注册的资源数量
        /// </summary>
        public static int RegisteredResourcesCount => _disposableResources.Count;

        #endregion

        #region 初始化

        /// <summary>
        /// 静态构造函数，初始化资源管理器
        /// </summary>
        static ResourceManager()
        {
            InitializeResourceManager();
        }

        /// <summary>
        /// 初始化资源管理器
        /// </summary>
        private static void InitializeResourceManager()
        {
            try
            {
                // 初始化进程监控
                _currentProcess = Process.GetCurrentProcess();

                // 初始化全局取消令牌源
                _globalCancellationTokenSource = new CancellationTokenSource();

                // 启动内存清理定时器
                StartMemoryCleanupTimer();

                // 注册应用程序域卸载事件
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;

                Debug.WriteLine("ResourceManager 初始化完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ResourceManager 初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动内存清理定时器
        /// </summary>
        private static void StartMemoryCleanupTimer()
        {
            _memoryCleanupTimer = new Timer(
                PerformPeriodicMemoryCleanup,
                null,
                TimeSpan.FromMinutes(MemoryCleanupIntervalMinutes),
                TimeSpan.FromMinutes(MemoryCleanupIntervalMinutes));
        }

        #endregion

        #region 窗口管理

        /// <summary>
        /// 注册窗口到管理器
        /// </summary>
        /// <param name="window">要注册的窗口</param>
        public static void RegisterWindow(Window? window)
        {
            if (window == null || _isShuttingDown) return;

            try
            {
                _trackedWindows.Add(window);

                // 监听窗口关闭事件
                window.Closed += (s, e) => UnregisterWindow(window);

                Debug.WriteLine($"窗口已注册: {window.GetType().Name}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"注册窗口失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从管理器中注销窗口
        /// </summary>
        /// <param name="window">要注销的窗口</param>
        public static void UnregisterWindow(Window? window)
        {
            if (window == null) return;

            try
            {
                // 从并发集合中移除窗口（通过重新创建集合）
                var remainingWindows = _trackedWindows.Where(w => w != window).ToList();
                
                // 清空原集合并重新添加
                while (_trackedWindows.TryTake(out _)) { }
                foreach (var w in remainingWindows)
                {
                    _trackedWindows.Add(w);
                }

                Debug.WriteLine($"窗口已注销: {window.GetType().Name}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"注销窗口失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取所有跟踪的窗口
        /// </summary>
        /// <returns>窗口集合</returns>
        public static IEnumerable<Window> GetTrackedWindows()
        {
            return _trackedWindows.ToArray();
        }

        #endregion

        #region 资源管理

        /// <summary>
        /// 注册需要释放的资源
        /// </summary>
        /// <param name="resource">实现 IDisposable 接口的资源</param>
        public static void RegisterResource(IDisposable? resource)
        {
            if (resource == null || _isShuttingDown) return;

            try
            {
                _disposableResources.Add(resource);
                Debug.WriteLine($"资源已注册: {resource.GetType().Name}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"注册资源失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全执行操作
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        public static void SafeExecute(Action action, string operationName = "未知操作")
        {
            if (action == null) return;

            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"安全执行 {operationName} 时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全执行异步操作
        /// </summary>
        /// <param name="action">要执行的异步操作</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        public static async Task SafeExecuteAsync(Func<Task> action, string operationName = "未知异步操作")
        {
            if (action == null) return;

            try
            {
                await action.Invoke().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"安全执行异 Async {operationName} 时发生异常: {ex.Message}");
            }
        }

        #endregion

        #region 内存管理

        /// <summary>
        /// 执行智能内存清理
        /// </summary>
        /// <param name="force">是否强制清理</param>
        /// <returns>清理是否成功</returns>
        public static async Task<bool> PerformSmartMemoryCleanupAsync(bool force = false)
        {
            if (_isShuttingDown) return false;

            try
            {
                var currentMemory = CurrentMemoryUsageMB;
                var timeSinceLastCleanup = DateTime.Now - _lastMemoryCleanup;

                // 检查是否需要清理
                if (!force && currentMemory < MemoryThresholdMB && 
                    timeSinceLastCleanup.TotalMinutes < MemoryCleanupIntervalMinutes)
                {
                    return false;
                }

                Debug.WriteLine($"开始智能内存清理，当前内存使用: {currentMemory:F2} MB");

                // 执行内存清理
                await Task.Run(() =>
                {
                    // 清理未使用的资源
                    CleanupUnusedResources();

                    // 执行垃圾回收
                    GC.Collect(2, GCCollectionMode.Optimized, true, true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Optimized, true, true);

                    // 压缩大对象堆 (.NET 8 的方式)
                    try
                    {
                        GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                    }
                    catch
                    {
                        // 如果压缩失败，继续执行
                    }

                }).ConfigureAwait(false);

                _lastMemoryCleanup = DateTime.Now;

                var newMemory = CurrentMemoryUsageMB;
                var freedMemory = currentMemory - newMemory;

                Debug.WriteLine($"内存清理完成，释放了 {freedMemory:F2} MB，当前使用: {newMemory:F2} MB");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"智能内存清理失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 定期内存清理回调
        /// </summary>
        /// <param name="state">状态对象</param>
        private static void PerformPeriodicMemoryCleanup(object? state)
        {
            if (_isShuttingDown) return;

            _ = Task.Run(async () =>
            {
                await PerformSmartMemoryCleanupAsync().ConfigureAwait(false);
            });
        }

        /// <summary>
        /// 清理未使用的资源
        /// </summary>
        private static void CleanupUnusedResources()
        {
            try
            {
                var resourcesToRemove = new List<IDisposable>();

                // 收集需要移除的资源
                foreach (var resource in _disposableResources)
                {
                    try
                    {
                        // 检查资源是否仍然有效（这里可以添加更复杂的逻辑）
                        if (IsResourceDisposed(resource))
                        {
                            resourcesToRemove.Add(resource);
                        }
                    }
                    catch
                    {
                        resourcesToRemove.Add(resource);
                    }
                }

                // 移除无效资源
                if (resourcesToRemove.Count > 0)
                {
                    var remainingResources = _disposableResources.Except(resourcesToRemove).ToList();
                    
                    while (_disposableResources.TryTake(out _)) { }
                    foreach (var resource in remainingResources)
                    {
                        _disposableResources.Add(resource);
                    }

                    Debug.WriteLine($"清理了 {resourcesToRemove.Count} 个无效资源");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清理未使用资源失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查资源是否已释放
        /// </summary>
        /// <param name="resource">要检查的资源</param>
        /// <returns>资源是否已释放</returns>
        private static bool IsResourceDisposed(IDisposable resource)
        {
            try
            {
                // 针对不同类型的资源进行特定检查
                return resource switch
                {
                    CancellationTokenSource cts => cts.IsCancellationRequested,
                    Timer timer => false, // Timer 没有简单的检查方法
                    Process process => process.HasExited,
                    _ => false // 默认认为资源仍然有效
                };
            }
            catch
            {
                return true; // 如果检查时出现异常，认为资源已无效
            }
        }

        /// <summary>
        /// 获取内存使用情况报告
        /// </summary>
        /// <returns>内存使用报告</returns>
        public static string GetMemoryUsageReport()
        {
            try
            {
                var totalMemory = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
                var workingSet = CurrentMemoryUsageMB;
                var gen0Collections = GC.CollectionCount(0);
                var gen1Collections = GC.CollectionCount(1);
                var gen2Collections = GC.CollectionCount(2);

                return $"""
                内存使用报告:
                - 工作集内存: {workingSet:F2} MB
                - 托管内存: {totalMemory:F2} MB
                - GC 收集次数: Gen0={gen0Collections}, Gen1={gen1Collections}, Gen2={gen2Collections}
                - 跟踪窗口: {TrackedWindowsCount}
                - 注册资源: {RegisteredResourcesCount}
                - 上次清理: {_lastMemoryCleanup:yyyy-MM-dd HH:mm:ss}
                """;
            }
            catch (Exception ex)
            {
                return $"获取内存报告失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 获取详细的内存诊断报告
        /// </summary>
        /// <returns>详细的内存诊断信息</returns>
        public static string GetDetailedMemoryDiagnostics()
        {
            try
            {
                var sb = new StringBuilder();
                _currentProcess ??= Process.GetCurrentProcess();
                _currentProcess.Refresh();

                sb.AppendLine("=== 详细内存诊断报告 ===");
                sb.AppendLine($"报告时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();

                // 进程内存信息
                sb.AppendLine("📊 进程内存信息:");
                sb.AppendLine($"   工作集内存: {_currentProcess.WorkingSet64 / (1024.0 * 1024.0):F2} MB");
                sb.AppendLine($"   私有内存: {_currentProcess.PrivateMemorySize64 / (1024.0 * 1024.0):F2} MB");
                sb.AppendLine($"   虚拟内存: {_currentProcess.VirtualMemorySize64 / (1024.0 * 1024.0):F2} MB");
                sb.AppendLine($"   分页内存: {_currentProcess.PagedMemorySize64 / (1024.0 * 1024.0):F2} MB");
                sb.AppendLine($"   非分页内存: {_currentProcess.NonpagedSystemMemorySize64 / (1024.0 * 1024.0):F2} MB");
                sb.AppendLine();

                // GC 内存信息
                var totalManagedMemory = GC.GetTotalMemory(false);
                sb.AppendLine("🗑️ 垃圾回收信息:");
                sb.AppendLine($"   托管内存总量: {totalManagedMemory / (1024.0 * 1024.0):F2} MB");
                sb.AppendLine($"   Gen 0 回收次数: {GC.CollectionCount(0)}");
                sb.AppendLine($"   Gen 1 回收次数: {GC.CollectionCount(1)}");
                sb.AppendLine($"   Gen 2 回收次数: {GC.CollectionCount(2)}");
                
                // .NET 8 中的内存信息
                var gcInfo = GC.GetGCMemoryInfo();
                sb.AppendLine($"   高内存负载: {gcInfo.HighMemoryLoadThresholdBytes / (1024.0 * 1024.0):F2} MB");
                sb.AppendLine($"   内存负载: {gcInfo.MemoryLoadBytes / (1024.0 * 1024.0):F2} MB");
                sb.AppendLine($"   总可用内存: {gcInfo.TotalAvailableMemoryBytes / (1024.0 * 1024.0):F2} MB");
                sb.AppendLine($"   堆大小: {gcInfo.HeapSizeBytes / (1024.0 * 1024.0):F2} MB");
                sb.AppendLine($"   碎片字节: {gcInfo.FragmentedBytes / (1024.0 * 1024.0):F2} MB");
                sb.AppendLine();

                // 进程信息
                sb.AppendLine("🔧 进程信息:");
                sb.AppendLine($"   进程ID: {_currentProcess.Id}");
                sb.AppendLine($"   线程数: {_currentProcess.Threads.Count}");
                sb.AppendLine($"   句柄数: {_currentProcess.HandleCount}");
                sb.AppendLine($"   运行时间: {DateTime.Now - _currentProcess.StartTime:dd\\.hh\\:mm\\:ss}");
                sb.AppendLine();

                // 资源管理信息
                sb.AppendLine("📋 资源管理信息:");
                sb.AppendLine($"   跟踪窗口数: {TrackedWindowsCount}");
                sb.AppendLine($"   注册资源数: {RegisteredResourcesCount}");
                sb.AppendLine($"   上次内存清理: {_lastMemoryCleanup:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"   距离上次清理: {DateTime.Now - _lastMemoryCleanup:hh\\:mm\\:ss}");
                sb.AppendLine();

                // 内存压力评估
                sb.AppendLine("⚖️ 内存压力评估:");
                var workingSetMB = _currentProcess.WorkingSet64 / (1024.0 * 1024.0);
                var managedMB = totalManagedMemory / (1024.0 * 1024.0);
                
                if (workingSetMB > 1000)
                    sb.AppendLine("   ⚠️ 工作集内存过高 (>1GB)");
                else if (workingSetMB > 500)
                    sb.AppendLine("   ⚡ 工作集内存较高 (>500MB)");
                else
                    sb.AppendLine("   ✅ 工作集内存正常");

                if (managedMB > 200)
                    sb.AppendLine("   ⚠️ 托管内存过高 (>200MB)");
                else if (managedMB > 100)
                    sb.AppendLine("   ⚡ 托管内存较高 (>100MB)");
                else
                    sb.AppendLine("   ✅ 托管内存正常");

                if (GC.CollectionCount(2) > 50)
                    sb.AppendLine("   ⚠️ Gen2 GC频繁，可能有内存泄漏");
                else
                    sb.AppendLine("   ✅ GC频率正常");

                sb.AppendLine();
                sb.AppendLine("=== 报告结束 ===");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"获取内存诊断报告失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 输出内存诊断报告到Debug
        /// </summary>
        public static void LogMemoryDiagnostics()
        {
            try
            {
                var report = GetDetailedMemoryDiagnostics();
                Debug.WriteLine(report);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"输出内存诊断报告失败: {ex.Message}");
            }
        }

        #endregion

        #region 清理方法

        /// <summary>
        /// 异步关闭所有窗口
        /// </summary>
        public static async Task CloseAllWindowsAsync()
        {
            if (_isShuttingDown) return;

            try
            {
                var windows = _trackedWindows.ToArray();
                Debug.WriteLine($"开始关闭 {windows.Length} 个窗口");

                var closeTasks = windows.Select(window => Task.Run(() =>
                {
                    try
                    {
                        if (Application.Current?.Dispatcher.CheckAccess() == true)
                        {
                            window?.Close();
                        }
                        else
                        {
                            Application.Current?.Dispatcher.Invoke(() => window?.Close());
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"关闭窗口时发生异常: {ex.Message}");
                    }
                })).ToArray();

                await Task.WhenAll(closeTasks).ConfigureAwait(false);
                await Task.Delay(200).ConfigureAwait(false); // 等待窗口关闭完成

                // 清空窗口集合
                while (_trackedWindows.TryTake(out _)) { }

                Debug.WriteLine("所有窗口关闭完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"关闭所有窗口时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步释放所有注册的资源
        /// </summary>
        public static async Task DisposeAllResourcesAsync()
        {
            try
            {
                var resources = _disposableResources.ToArray();
                Debug.WriteLine($"开始释放 {resources.Length} 个资源");

                var disposeTasks = resources.Select(resource => Task.Run(() =>
                {
                    try
                    {
                        resource?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"释放资源时发生异常: {ex.Message}");
                    }
                })).ToArray();

                await Task.WhenAll(disposeTasks).ConfigureAwait(false);

                // 清空资源集合
                while (_disposableResources.TryTake(out _)) { }

                Debug.WriteLine("所有资源释放完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"释放所有资源时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行完整的应用程序关闭清理
        /// </summary>
        public static async Task PerformShutdownAsync()
        {
            if (_isShuttingDown) return;

            _isShuttingDown = true;

            try
            {
                Debug.WriteLine("开始应用程序关闭清理");

                // 停止内存清理定时器
                _memoryCleanupTimer?.Dispose();
                _memoryCleanupTimer = null;

                // 发送全局取消信号
                _globalCancellationTokenSource?.Cancel();

                // 关闭所有窗口
                await CloseAllWindowsAsync().ConfigureAwait(false);

                // 释放所有资源
                await DisposeAllResourcesAsync().ConfigureAwait(false);

                // 执行最后一次内存清理
                await PerformSmartMemoryCleanupAsync(force: true).ConfigureAwait(false);

                // 释放全局资源
                _globalCancellationTokenSource?.Dispose();
                _globalCancellationTokenSource = null;

                _currentProcess?.Dispose();
                _currentProcess = null;

                Debug.WriteLine("应用程序关闭清理完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"执行应用程序关闭时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理所有资源（同步版本，用于应用程序退出）
        /// </summary>
        public static void CleanupAll()
        {
            if (_isShuttingDown) return;

            _isShuttingDown = true;

            try
            {
                // 停止定时器
                _memoryCleanupTimer?.Dispose();

                // 取消全局令牌
                _globalCancellationTokenSource?.Cancel();

                // 同步关闭窗口
                CloseAllWindowsSync();

                // 同步释放资源
                DisposeAllResourcesSync();

                // 释放全局资源
                _globalCancellationTokenSource?.Dispose();
                _currentProcess?.Dispose();

                Debug.WriteLine("同步清理完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"同步清理时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 同步关闭所有窗口
        /// </summary>
        private static void CloseAllWindowsSync()
        {
            try
            {
                var windows = _trackedWindows.ToArray();
                foreach (var window in windows)
                {
                    try
                    {
                        window?.Close();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"同步关闭窗口时发生异常: {ex.Message}");
                    }
                }

                while (_trackedWindows.TryTake(out _)) { }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"同步关闭所有窗口时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 同步释放所有资源
        /// </summary>
        private static void DisposeAllResourcesSync()
        {
            try
            {
                var resources = _disposableResources.ToArray();
                foreach (var resource in resources)
                {
                    try
                    {
                        resource?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"同步释放资源时发生异常: {ex.Message}");
                    }
                }

                while (_disposableResources.TryTake(out _)) { }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"同步释放所有资源时发生异常: {ex.Message}");
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 进程退出事件处理
        /// </summary>
        private static void OnProcessExit(object? sender, EventArgs e)
        {
            CleanupAll();
        }

        /// <summary>
        /// 应用程序域卸载事件处理
        /// </summary>
        private static void OnDomainUnload(object? sender, EventArgs e)
        {
            CleanupAll();
        }

        #endregion

        #region 扩展方法

        /// <summary>
        /// 执行脚本后内存清理（优化版本）
        /// 在脚本执行完成后立即执行全面的内存清理
        /// </summary>
        public static async Task PerformPostScriptExecutionCleanupAsync()
        {
            if (_isShuttingDown) return;

            try
            {
                Debug.WriteLine("开始脚本执行后内存清理...");

                var currentMemory = CurrentMemoryUsageMB;
                Debug.WriteLine($"清理前内存使用: {currentMemory:F2} MB");

                // 执行内存清理
                await Task.Run(() =>
                {
                    try
                    {
                        // 1. 清理未使用的资源
                        CleanupUnusedResources();

                        // 2. 清理智能引用管理器缓存
                        try
                        {
                            var smartRefManagerType = AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(a => a.GetTypes())
                                .FirstOrDefault(t => t.Name == "SmartReferenceManager");
                            
                            if (smartRefManagerType != null)
                            {
                                var instance = smartRefManagerType
                                    .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
                                    ?.GetValue(null);
                                
                                instance?.GetType()
                                    .GetMethod("ClearCache", BindingFlags.Public | BindingFlags.Instance)
                                    ?.Invoke(instance, null);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"清理智能引用管理器缓存失败: {ex.Message}");
                        }

                        // 3. 强制垃圾回收（分层执行）
                        GC.Collect(0, GCCollectionMode.Forced, true, false);
                        GC.WaitForPendingFinalizers();
                        
                        GC.Collect(1, GCCollectionMode.Forced, true, false);
                        GC.WaitForPendingFinalizers();
                        
                        GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                        GC.WaitForPendingFinalizers();
                        
                        // 4. 最终全面回收
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

                        // 5. 尝试压缩大对象堆
                        try
                        {
                            // .NET 8 中的大对象堆压缩
                            System.Runtime.GCSettings.LargeObjectHeapCompactionMode = 
                                System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
                            GC.Collect();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"压缩大对象堆失败: {ex.Message}");
                        }

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"内存清理过程中出现异常: {ex.Message}");
                    }
                }).ConfigureAwait(false);

                var newMemory = CurrentMemoryUsageMB;
                var freedMemory = currentMemory - newMemory;

                Debug.WriteLine($"脚本执行后内存清理完成，释放了 {freedMemory:F2} MB，当前使用: {newMemory:F2} MB");

                // 更新最后清理时间
                _lastMemoryCleanup = DateTime.Now;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"脚本执行后内存清理失败: {ex.Message}");
            }
        }

        #endregion
    }
}