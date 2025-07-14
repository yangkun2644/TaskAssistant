using System;
using System.Configuration;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskAssistant.Common;
using TaskAssistant.Data;
using TaskAssistant.Data.Services;
using TaskAssistant.Services;
using TaskAssistant.ViewModels;

namespace TaskAssistant
{
    /// <summary>
    /// 应用程序主类 - 负责应用程序的启动、初始化和全局配置
    /// 继承自 WPF 的 Application 类，提供应用程序级别的事件处理和资源管理
    /// 处理应用程序的生命周期，包括启动、运行和关闭阶段
    /// </summary>
    public partial class App : Application
    {
        /// <summary>全局应用程序取消令牌源</summary>
        private static CancellationTokenSource _globalCancellationTokenSource = new();

        /// <summary>服务提供者</summary>
        private IServiceProvider? _serviceProvider;

        /// <summary>
        /// 获取全局取消令牌
        /// </summary>
        public static CancellationToken GlobalCancellationToken =>
            _globalCancellationTokenSource?.Token ?? CancellationToken.None;

        /// <summary>
        /// 获取服务提供者
        /// </summary>
        public static IServiceProvider? Services { get; private set; }

        #region 构造函数

        /// <summary>
        /// 初始化应用程序实例
        /// 设置基本配置和异常处理
        /// </summary>
        public App()
        {
            // 在构造函数中注册全局异常处理器
            RegisterGlobalExceptionHandlers();
        }

        #endregion

        #region 应用程序启动处理

        /// <summary>
        /// 应用程序启动事件处理方法
        /// 在应用程序启动时执行初始化逻辑，包括全局配置、资源注册等
        /// 重写此方法可以在主窗口显示前进行必要的准备工作
        /// </summary>
        /// <param name="e">启动事件参数，包含命令行参数等信息</param>
        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // 调用基类的启动方法，确保正常的启动流程
                base.OnStartup(e);
                
                // 应用程序启动时的初始化工作
                await InitializeApplicationAsync();
                
                // 创建并显示主窗口
                await CreateAndShowMainWindowAsync();
            }
            catch (InvalidOperationException ex)
            {
                // 专门处理 InvalidOperationException 异常
                HandleInvalidOperationException(ex, "应用程序启动");
                Shutdown(1);
            }
            catch (Exception ex)
            {
                // 处理其他类型的异常
                HandleGlobalException(ex, "应用程序启动");
                Shutdown(1);
            }
        }

        /// <summary>
        /// 创建并显示主窗口
        /// </summary>
        private async Task CreateAndShowMainWindowAsync()
        {
            try
            {
                // 验证服务提供者是否已正确初始化
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException("服务提供者未初始化。这可能是由于依赖注入配置失败造成的。");
                }

                // 尝试获取主窗口服务
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                
                // 验证主窗口是否成功创建
                if (mainWindow == null)
                {
                    throw new InvalidOperationException("主窗口创建失败。无法从服务容器中获取 MainWindow 实例。");
                }

                // 显示主窗口
                mainWindow.Show();
            }
            catch (InvalidOperationException)
            {
                // 重新抛出 InvalidOperationException，让调用者处理
                throw;
            }
            catch (Exception ex)
            {
                // 将其他异常包装成 InvalidOperationException
                throw new InvalidOperationException($"创建主窗口时发生未预期的错误：{ex.Message}", ex);
            }
        }

        #endregion

        #region 应用程序关闭处理

        /// <summary>
        /// 应用程序退出事件处理方法
        /// 在应用程序即将退出时执行清理逻辑，确保资源的正确释放
        /// </summary>
        /// <param name="e">退出事件参数，包含退出代码等信息</param>
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // 执行应用程序退出前的清理工作
                CleanupApplication();
                
                // 调用基类的退出方法
                base.OnExit(e);
            }
            catch (Exception)
            {
                // 忽略退出过程中的异常，确保应用程序能够正常退出
                // 在退出阶段，稳定性比错误报告更重要
            }
        }

        #endregion

        #region 初始化和清理方法

        /// <summary>
        /// 初始化应用程序的私有方法
        /// 执行应用程序启动时需要的各种初始化操作
        /// </summary>
        private async Task InitializeApplicationAsync()
        {
            try
            {
                // 配置依赖注入服务
                ConfigureServices();
                
                // 验证服务提供者是否正确配置
                ValidateServiceProvider();
                
                // 初始化数据库
                await InitializeDatabaseAsync();

                // 初始化智能引用管理器
                await InitializeSmartReferenceManagerAsync();
                
                // 设置全局服务提供者
                Services = _serviceProvider;
                
                // 注册应用程序退出事件
                this.Exit += (sender, e) => CleanupApplication();
            }
            catch (InvalidOperationException)
            {
                // 重新抛出 InvalidOperationException，保持异常类型
                throw;
            }
            catch (Exception ex)
            {
                // 将其他异常包装成 InvalidOperationException
                throw new InvalidOperationException($"应用程序初始化失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 初始化智能引用管理器
        /// </summary>
        private async Task InitializeSmartReferenceManagerAsync()
        {
            try
            {
                var settingsService = _serviceProvider?.GetService<TaskAssistant.Services.IAppSettingsService>();
                if (settingsService != null)
                {
                    await TaskAssistant.Services.SmartReferenceManager.Instance.InitializeAsync(settingsService);
                }
            }
            catch (Exception ex)
            {
                // 智能引用管理器初始化失败不应该阻止应用程序启动
                System.Diagnostics.Debug.WriteLine($"智能引用管理器初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证服务提供者是否正确配置
        /// </summary>
        private void ValidateServiceProvider()
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException("依赖注入容器初始化失败。服务提供者为空。");
            }

            // 验证关键服务是否已注册
            try
            {
                var dataService = _serviceProvider.GetService<IDataService>();
                if (dataService == null)
                {
                    throw new InvalidOperationException("数据服务未正确注册到依赖注入容器中。");
                }

                // 移除 NavigationService 的验证，因为它不再在容器中注册
                // NavigationService 在 MainWindowViewModel 中创建，具有特定的委托依赖
                // var navigationService = _serviceProvider.GetService<INavigationService>();
                // if (navigationService == null)
                // {
                //     throw new InvalidOperationException("导航服务未正确注册到依赖注入容器中。");
                // }
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"验证服务注册时发生错误：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            try
            {
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException("无法初始化数据库：服务提供者未初始化。");
                }

                await _serviceProvider.InitializeDatabaseAsync();
            }
            catch (InvalidOperationException)
            {
                // 重新抛出 InvalidOperationException
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"数据库初始化失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 配置依赖注入服务
        /// </summary>
        private void ConfigureServices()
        {
            try
            {
                var services = new ServiceCollection();

                // 添加数据访问服务
                services.AddDataServices();

                // 添加应用程序设置服务
                services.AddTransient<IAppSettingsService, AppSettingsService>();

                // 注意：不注册 NavigationService 作为全局服务
                // NavigationService 在 MainWindowViewModel 中创建，因为它需要特定的委托参数

                // 只注册不需要 NavigationService 的 ViewModels
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<HomeViewModel>();
                services.AddTransient<TasksManageViewModel>();

                // 不注册需要 NavigationService 的 ViewModels，它们由 MainWindowViewModel 在页面工厂中创建：
                // - ScriptManageViewModel (需要 INavigationService)
                // - ScriptManageListViewModel (需要 INavigationService)
                // - ScriptReferenceSettingsViewModel (需要 INavigationService)

                // 添加 Views (Windows)
                services.AddTransient<MainWindow>();

                // 在此添加系统统计信息服务
                services.AddSingleton<ISystemStatisticsService, SystemStatisticsService>();

                // 构建服务提供者
                _serviceProvider = services.BuildServiceProvider();

                // 验证服务提供者是否成功创建
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException("依赖注入容器构建失败。");
                }
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"配置依赖注入服务时发生错误：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 清理应用程序资源的私有方法
        /// 在应用程序退出时释放所有占用的资源
        /// </summary>
        private void CleanupApplication()
        {
            try
            {
                // 取消全局取消令牌
                _globalCancellationTokenSource?.Cancel();
                
                // 清理资源管理器中的所有资源
                ResourceManager.CleanupAll();
                
                // 释放服务提供者
                if (_serviceProvider is IDisposable disposableServiceProvider)
                {
                    disposableServiceProvider.Dispose();
                }
                
                // 释放取消令牌源
                _globalCancellationTokenSource?.Dispose();
                
                // 强制垃圾回收，确保内存释放
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch (Exception)
            {
                // 忽略清理过程中的异常
                // 在应用程序退出阶段，异常处理应该尽量简单
            }
        }

        #endregion

        #region 全局异常处理

        /// <summary>
        /// 注册全局异常处理器
        /// 捕获应用程序中未处理的异常，提供统一的错误处理机制
        /// </summary>
        private void RegisterGlobalExceptionHandlers()
        {
            // 处理 UI 线程中的未处理异常
            this.DispatcherUnhandledException += (sender, e) =>
            {
                if (e.Exception is InvalidOperationException invalidOpEx)
                {
                    HandleInvalidOperationException(invalidOpEx, "UI 线程");
                }
                else
                {
                    HandleGlobalException(e.Exception, "UI 线程异常");
                }
                e.Handled = true; // 标记为已处理，防止应用程序崩溃
            };
            
            // 处理应用程序域中的未处理异常
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is InvalidOperationException invalidOpEx)
                {
                    HandleInvalidOperationException(invalidOpEx, "应用程序域");
                }
                else
                {
                    HandleGlobalException(e.ExceptionObject as Exception, "应用程序域异常");
                }
            };
            
            // 处理任务调度器中的未处理异常
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                if (e.Exception.InnerException is InvalidOperationException invalidOpEx)
                {
                    HandleInvalidOperationException(invalidOpEx, "任务调度器");
                }
                else
                {
                    HandleGlobalException(e.Exception, "任务调度器异常");
                }
                e.SetObserved(); // 标记为已观察，防止进程终止
            };
        }

        /// <summary>
        /// 专门处理 InvalidOperationException 异常
        /// </summary>
        /// <param name="exception">InvalidOperationException 异常对象</param>
        /// <param name="source">异常来源描述</param>
        private void HandleInvalidOperationException(InvalidOperationException exception, string source)
        {
            if (exception == null) return;

            try
            {
                // 生成详细的错误信息
                var errorMessage = GenerateInvalidOperationErrorMessage(exception, source);
                
                // 显示用户友好的错误对话框
                var result = MessageBox.Show(
                    errorMessage,
                    "操作错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                // 如果是严重的服务相关错误，建议重启应用程序
                if (IsServiceRelatedError(exception))
                {
                    var restartResult = MessageBox.Show(
                        "检测到服务配置相关的严重错误，建议重新启动应用程序以恢复正常功能。\n\n是否现在重新启动？",
                        "建议重启",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (restartResult == MessageBoxResult.Yes)
                    {
                        RestartApplication();
                    }
                }
            }
            catch (Exception)
            {
                // 如果异常处理本身也失败了，回退到基本的异常处理
                HandleGlobalException(exception, source);
            }
        }

        /// <summary>
        /// 生成 InvalidOperationException 的详细错误信息
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <param name="source">异常来源</param>
        /// <returns>格式化的错误信息</returns>
        private string GenerateInvalidOperationErrorMessage(InvalidOperationException exception, string source)
        {
            var message = $"在 {source} 中发生了操作异常：\n\n";

            // 根据异常信息的关键词提供更具体的说明
            if (exception.Message.Contains("服务") || exception.Message.Contains("Service"))
            {
                message += "📋 错误类型：服务配置问题\n";
                message += "🔍 问题描述：应用程序的服务组件未正确配置或注册\n";
                message += $"📝 详细信息：{exception.Message}\n\n";
                message += "💡 建议解决方案：\n";
                message += "   • 重新启动应用程序\n";
                message += "   • 检查应用程序的配置文件\n";
                message += "   • 如果问题持续存在，请联系技术支持";
            }
            else if (exception.Message.Contains("窗口") || exception.Message.Contains("Window"))
            {
                message += "📋 错误类型：窗口创建问题\n";
                message += "🔍 问题描述：应用程序无法正确创建或显示窗口\n";
                message += $"📝 详细信息：{exception.Message}\n\n";
                message += "💡 建议解决方案：\n";
                message += "   • 检查系统资源是否充足\n";
                message += "   • 重新启动应用程序\n";
                message += "   • 确保显示器连接正常";
            }
            else if (exception.Message.Contains("数据库") || exception.Message.Contains("Database"))
            {
                message += "📋 错误类型：数据库连接问题\n";
                message += "🔍 问题描述：应用程序无法连接或初始化数据库\n";
                message += $"📝 详细信息：{exception.Message}\n\n";
                message += "💡 建议解决方案：\n";
                message += "   • 检查数据库文件是否存在\n";
                message += "   • 确保有足够的磁盘空间\n";
                message += "   • 检查文件访问权限";
            }
            else
            {
                message += "📋 错误类型：操作状态异常\n";
                message += "🔍 问题描述：当前操作在此状态下无法执行\n";
                message += $"📝 详细信息：{exception.Message}\n\n";
                message += "💡 建议解决方案：\n";
                message += "   • 请稍后重试此操作\n";
                message += "   • 检查操作的前置条件是否满足\n";
                message += "   • 如果问题持续存在，请重新启动应用程序";
            }

            return message;
        }

        /// <summary>
        /// 判断是否为服务相关的严重错误
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <returns>是否为服务相关错误</returns>
        private bool IsServiceRelatedError(InvalidOperationException exception)
        {
            var message = exception.Message.ToLower();
            return message.Contains("服务") || 
                   message.Contains("service") ||
                   message.Contains("依赖注入") ||
                   message.Contains("dependency") ||
                   message.Contains("容器") ||
                   message.Contains("provider");
        }

        /// <summary>
        /// 重启应用程序
        /// </summary>
        private void RestartApplication()
        {
            try
            {
                // 获取当前执行文件路径
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    // 启动新的应用程序实例
                    System.Diagnostics.Process.Start(exePath);
                }
                
                // 关闭当前实例
                Shutdown(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重启应用程序失败：{ex.Message}\n\n请手动重新启动应用程序。", 
                    "重启失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 处理全局异常的统一方法
        /// 记录异常信息并向用户显示适当的错误消息
        /// </summary>
        /// <param name="exception">捕获的异常对象</param>
        /// <param name="source">异常来源描述</param>
        private void HandleGlobalException(Exception? exception, string source)
        {
            if (exception == null) return;
            
            try
            {
                // 在这里可以添加日志记录逻辑
                // Logger.LogError($"{source}: {exception}");
                
                // 向用户显示友好的错误消息
                var message = $"应用程序遇到了一个错误:\n\n{exception.Message}\n\n" +
                             $"错误来源: {source}\n\n" +
                             "请尝试重新启动应用程序。如果问题持续存在，请联系技术支持。";
                
                MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception)
            {
                // 如果异常处理本身也失败了，就只能忽略
                // 避免无限递归的异常处理
            }
        }

        #endregion

        #region 公共静态方法

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        public static T? GetService<T>() where T : class
        {
            try
            {
                return Services?.GetService<T>();
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException($"获取服务 {typeof(T).Name} 时发生错误：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取必需的服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        public static T GetRequiredService<T>() where T : class
        {
            try
            {
                if (Services == null)
                {
                    throw new InvalidOperationException("服务提供者未初始化。无法获取任何服务实例。请确保应用程序已正确启动。");
                }

                var service = Services.GetRequiredService<T>();
                if (service == null)
                {
                    throw new InvalidOperationException($"服务 {typeof(T).Name} 未正确注册到依赖注入容器中。请检查服务注册配置。");
                }

                return service;
            }
            catch (InvalidOperationException)
            {
                // 重新抛出 InvalidOperationException
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取必需服务 {typeof(T).Name} 时发生未预期的错误：{ex.Message}", ex);
            }
        }

        #endregion
    }
}
