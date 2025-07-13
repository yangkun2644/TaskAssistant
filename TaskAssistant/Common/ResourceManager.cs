using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TaskAssistant.Common
{
    /// <summary>
    /// 通用资源管理器类
    /// 负责管理应用程序的所有资源和生命周期，包括窗口、可释放资源等
    /// 提供统一的资源注册、跟踪和清理机制，确保应用程序的稳定性
    /// </summary>
    public static class ResourceManager
    {
        #region 私有字段

        /// <summary>
        /// 需要释放的资源列表
        /// 存储所有实现了 IDisposable 接口的资源
        /// </summary>
        private static readonly List<IDisposable> _disposableResources = new();
        
        /// <summary>
        /// 被跟踪的窗口列表
        /// 存储所有需要管理生命周期的窗口实例
        /// </summary>
        private static readonly List<Window> _trackedWindows = new();
        
        /// <summary>
        /// 线程同步锁对象
        /// 确保多线程环境下的资源操作安全性
        /// </summary>
        private static readonly object _lockObject = new();
        
        /// <summary>
        /// 应用程序关闭状态标志
        /// 使用 volatile 确保多线程可见性
        /// </summary>
        private static volatile bool _isShuttingDown = false;

        /// <summary>
        /// 全局取消令牌源
        /// 用于协调应用程序关闭过程
        /// </summary>
        private static CancellationTokenSource? _globalCancellationTokenSource;

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取应用程序是否正在关闭
        /// 当应用程序开始关闭流程时，此属性将返回 true
        /// </summary>
        public static bool IsShuttingDown => _isShuttingDown;

        /// <summary>
        /// 获取全局取消令牌
        /// 当应用程序开始关闭时，此令牌将被激活
        /// </summary>
        public static CancellationToken GlobalCancellationToken => 
            _globalCancellationTokenSource?.Token ?? CancellationToken.None;

        #endregion

        #region 初始化和清理

        /// <summary>
        /// 初始化资源管理器
        /// 在应用程序启动时调用，设置必要的全局资源
        /// </summary>
        public static void Initialize()
        {
            try
            {
                // 创建全局取消令牌源
                _globalCancellationTokenSource = new CancellationTokenSource();
                
                // 重置关闭状态
                _isShuttingDown = false;
                
                // 可以在这里添加更多初始化逻辑
                Debug.WriteLine("资源管理器初始化完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"资源管理器初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理所有资源
        /// 在应用程序关闭时调用，释放所有被管理的资源
        /// </summary>
        public static void CleanupAll()
        {
            if (_isShuttingDown) return;
            
            _isShuttingDown = true;

            try
            {
                // 发送全局取消信号
                _globalCancellationTokenSource?.Cancel();

                // 执行同步清理操作
                CloseAllWindowsSync();
                DisposeAllResourcesSync();
                PerformGarbageCollectionSync();
                
                // 清理全局取消令牌源
                _globalCancellationTokenSource?.Dispose();
                _globalCancellationTokenSource = null;
                
                Debug.WriteLine("资源管理器清理完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"资源管理器清理失败: {ex.Message}");
            }
        }

        #endregion

        #region 资源注册

        /// <summary>
        /// 注册需要释放的资源
        /// 将实现 IDisposable 接口的对象添加到管理列表中
        /// </summary>
        /// <param name="resource">要注册的资源对象</param>
        public static void RegisterResource(IDisposable resource)
        {
            if (resource == null || _isShuttingDown) return;

            lock (_lockObject)
            {
                if (!_disposableResources.Contains(resource))
                {
                    _disposableResources.Add(resource);
                }
            }
        }

        /// <summary>
        /// 注册要跟踪的窗口
        /// 将窗口添加到管理列表中，并自动处理窗口关闭事件
        /// </summary>
        /// <param name="window">要注册的窗口实例</param>
        public static void RegisterWindow(Window window)
        {
            if (window == null || _isShuttingDown) return;

            lock (_lockObject)
            {
                if (!_trackedWindows.Contains(window))
                {
                    _trackedWindows.Add(window);
                    // 当窗口关闭时自动从列表中移除
                    window.Closed += (s, e) => UnregisterWindow(window);
                }
            }
        }

        /// <summary>
        /// 取消注册窗口
        /// 从管理列表中移除指定的窗口
        /// </summary>
        /// <param name="window">要取消注册的窗口</param>
        public static void UnregisterWindow(Window window)
        {
            if (window == null) return;

            lock (_lockObject)
            {
                _trackedWindows.Remove(window);
            }
        }

        /// <summary>
        /// 取消注册资源
        /// 从管理列表中移除指定的资源（但不释放它）
        /// </summary>
        /// <param name="resource">要取消注册的资源</param>
        public static void UnregisterResource(IDisposable resource)
        {
            if (resource == null) return;

            lock (_lockObject)
            {
                _disposableResources.Remove(resource);
            }
        }

        #endregion

        #region 异步资源清理方法

        /// <summary>
        /// 异步关闭所有跟踪的窗口
        /// 遍历所有注册的窗口并安全地关闭它们
        /// </summary>
        public static async Task CloseAllWindows()
        {
            var windowsToClose = new List<Window>();

            lock (_lockObject)
            {
                windowsToClose.AddRange(_trackedWindows);
            }

            foreach (var window in windowsToClose)
            {
                try
                {
                    if (window != null && window.IsLoaded)
                    {
                        await window.Dispatcher.InvokeAsync(() =>
                        {
                            try
                            {
                                window.Close();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"关闭窗口时发生异常: {ex.Message}");
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"处理窗口关闭时发生异常: {ex.Message}");
                }
            }

            // 等待窗口关闭完成
            await Task.Delay(200);

            lock (_lockObject)
            {
                _trackedWindows.Clear();
            }
        }

        /// <summary>
        /// 异步释放所有注册的资源
        /// 遍历所有注册的资源并安全地释放它们
        /// </summary>
        public static async Task DisposeAllResources()
        {
            var resourcesToDispose = new List<IDisposable>();

            lock (_lockObject)
            {
                resourcesToDispose.AddRange(_disposableResources);
                _disposableResources.Clear();
            }

            foreach (var resource in resourcesToDispose)
            {
                try
                {
                    resource?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"释放资源时发生异常: {ex.Message}");
                }
            }

            await Task.Delay(50);
        }

        /// <summary>
        /// 异步执行优化的垃圾回收
        /// 执行完整的垃圾回收序列以释放内存
        /// </summary>
        public static async Task PerformGarbageCollection()
        {
            try
            {
                // 执行完整的垃圾回收序列
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // 针对 .NET 8 的优化垃圾回收
                GC.Collect(2, GCCollectionMode.Aggressive, true, true);

                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"执行垃圾回收时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步执行完整的应用程序关闭清理
        /// 按顺序执行所有清理步骤
        /// </summary>
        public static async Task PerformShutdown()
        {
            if (_isShuttingDown) return;

            _isShuttingDown = true;

            try
            {
                // 发送全局取消信号
                _globalCancellationTokenSource?.Cancel();

                // 关闭所有窗口
                await CloseAllWindows();

                // 释放所有资源
                await DisposeAllResources();

                // 执行垃圾回收
                await PerformGarbageCollection();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"执行应用程序关闭时发生异常: {ex.Message}");
            }
        }

        #endregion

        #region 同步资源清理方法（用于应用程序退出）

        /// <summary>
        /// 同步关闭所有窗口
        /// 在应用程序退出阶段使用，不依赖异步操作
        /// </summary>
        private static void CloseAllWindowsSync()
        {
            try
            {
                var windowsToClose = new List<Window>();

                lock (_lockObject)
                {
                    windowsToClose.AddRange(_trackedWindows);
                    _trackedWindows.Clear();
                }

                foreach (var window in windowsToClose)
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"同步关闭所有窗口时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 同步释放所有资源
        /// 在应用程序退出阶段使用，不依赖异步操作
        /// </summary>
        private static void DisposeAllResourcesSync()
        {
            try
            {
                var resourcesToDispose = new List<IDisposable>();

                lock (_lockObject)
                {
                    resourcesToDispose.AddRange(_disposableResources);
                    _disposableResources.Clear();
                }

                foreach (var resource in resourcesToDispose)
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"同步释放所有资源时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 同步执行垃圾回收
        /// 在应用程序退出阶段使用，不依赖异步操作
        /// </summary>
        private static void PerformGarbageCollectionSync()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"同步执行垃圾回收时发生异常: {ex.Message}");
            }
        }

        #endregion

        #region 实用方法

        /// <summary>
        /// 安全执行操作，捕获并记录异常
        /// 用于包装可能抛出异常的操作，确保不会影响整体流程
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="operationName">操作名称（用于日志记录）</param>
        public static void SafeExecute(Action action, string operationName = "未知操作")
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"执行{operationName}时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全执行异步操作，捕获并记录异常
        /// 用于包装可能抛出异常的异步操作，确保不会影响整体流程
        /// </summary>
        /// <param name="asyncAction">要执行的异步操作</param>
        /// <param name="operationName">操作名称（用于日志记录）</param>
        public static async Task SafeExecuteAsync(Func<Task> asyncAction, string operationName = "未知异步操作")
        {
            try
            {
                if (asyncAction != null)
                {
                    await asyncAction();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"执行{operationName}时发生异常: {ex.Message}");
            }
        }

        #endregion
    }
}