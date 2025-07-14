using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskAssistant.Common;
using TaskAssistant.Models;
using TaskAssistant.Services;
using TaskAssistant.View;
using TaskAssistant.ViewModels;

namespace TaskAssistant.ViewModels
{
    /// <summary>
    /// 主窗口视图模型类
    /// 负责管理整个应用程序的主要功能，包括页面导航、窗口状态管理、子窗口管理和应用程序生命周期控制
    /// 使用 MVVM 模式实现与主窗口视图的分离，提供清晰的架构设计
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        #region 私有字段

        /// <summary>
        /// 页面工厂字典
        /// 存储页面键和对应的页面创建委托，用于延迟加载和页面管理
        /// </summary>
        private readonly Dictionary<string, Func<UserControl>> _pageFactory;
        
        /// <summary>
        /// 带参数页面工厂字典
        /// 存储页面键和对应的带参数页面创建委托
        /// </summary>
        private readonly Dictionary<string, Func<Dictionary<string, object>, UserControl>> _pageWithParametersFactory;
        
        /// <summary>
        /// 页面缓存字典
        /// 缓存已创建的页面实例，避免重复创建，提高性能
        /// </summary>
        private readonly Dictionary<string, UserControl> _pageCache;
        
        /// <summary>
        /// 子窗口列表
        /// 管理所有打开的子窗口，用于统一控制和清理
        /// </summary>
        private readonly List<Window> _childWindows;
        
        /// <summary>
        /// 应用程序关闭取消令牌源
        /// 用于向所有子组件发送应用程序关闭信号，支持优雅关闭
        /// </summary>
        private readonly CancellationTokenSource _applicationCancellationTokenSource;
        
        /// <summary>
        /// 应用程序是否正在关闭的标志
        /// 防止重复执行关闭逻辑
        /// </summary>
        private bool _isClosing = false;

        #endregion

        #region 可观察属性

        /// <summary>
        /// 当前显示的页面
        /// 绑定到主窗口的内容区域，控制页面切换
        /// </summary>
        [ObservableProperty]
        private UserControl? _currentPage;

        /// <summary>
        /// 窗口状态属性
        /// 控制窗口的最小化、最大化、正常状态
        /// </summary>
        [ObservableProperty]
        private WindowState _windowState = WindowState.Normal;

        /// <summary>
        /// 当前活动的页面键
        /// 用于确定哪个导航按钮应该显示为活动状态
        /// </summary>
        [ObservableProperty]
        private string _currentPageKey = "HomeButton";

        #endregion

        #region 导航状态属性

        /// <summary>
        /// 首页按钮是否为活动状态
        /// </summary>
        [ObservableProperty]
        private bool _isHomeActive = true;

        /// <summary>
        /// 任务管理按钮是否为活动状态
        /// </summary>
        [ObservableProperty]
        private bool _isTasksManageActive = false;

        /// <summary>
        /// 脚本管理按钮是否为活动状态
        /// </summary>
        [ObservableProperty]
        private bool _isScriptManageActive = false;

        /// <summary>
        /// 脚本引用设置按钮是否为活动状态
        /// </summary>
        [ObservableProperty]
        private bool _isScriptReferenceSettingsActive = false;

        // 导航按钮样式属性（替代转换器）

        /// <summary>
        /// 首页按钮样式
        /// </summary>
        [ObservableProperty]
        private Style? _homeButtonStyle;

        /// <summary>
        /// 任务管理按钮样式
        /// </summary>
        [ObservableProperty]
        private Style? _tasksManageButtonStyle;

        /// <summary>
        /// 脚本管理按钮样式
        /// </summary>
        [ObservableProperty]
        private Style? _scriptManageButtonStyle;

        /// <summary>
        /// 脚本引用设置按钮样式
        /// </summary>
        [ObservableProperty]
        private Style? _scriptReferenceSettingsButtonStyle;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化主窗口视图模型
        /// 设置页面工厂、导航服务，并初始化应用程序的基本组件
        /// </summary>
        public MainWindowViewModel()
        {
            // 初始化应用程序关闭取消令牌源
            _applicationCancellationTokenSource = new CancellationTokenSource();
            ResourceManager.RegisterResource(_applicationCancellationTokenSource);

            // 初始化集合
            _pageCache = new Dictionary<string, UserControl>();
            _childWindows = new List<Window>();

            // 创建导航服务实例
            // 提供页面导航功能和主窗口访问
            var navigationService = new NavigationService(SwitchPage, SwitchPageWithParameters, GetMainWindow);

            // 初始化页面工厂字典
            // 定义各个页面的创建方式，使用工厂模式延迟创建页面实例
            _pageFactory = new Dictionary<string, Func<UserControl>>
            {
                // 主页 - 应用程序的欢迎页面
                ["HomeButton"] = () => CreatePage<Home>(new HomeViewModel()),
                
                // 任务管理页面 - 管理和监控各种任务
                ["TasksManageButton"] = () => CreatePage<TasksManage>(new TasksManageViewModel()),
                
                // 脚本管理列表页面 - 显示已保存的脚本列表
                ["ScriptManageButton"] = () => CreatePage<ScriptManage>(new ScriptManageListViewModel(navigationService)),
                
                // 脚本编辑页面 - 创建和编辑脚本（新建模式）
                ["ScriptManageViewButton"] = () => CreatePage<ScriptManageView>(new ScriptManageViewModel(navigationService)),

                // 脚本引用设置页面 - 配置脚本引用
                ["ScriptReferenceSettings"] = () => CreateScriptReferenceSettingsPage(navigationService)
            };

            // 初始化带参数页面工厂字典
            _pageWithParametersFactory = new Dictionary<string, Func<Dictionary<string, object>, UserControl>>
            {
                // 脚本编辑页面 - 支持编辑现有脚本
                ["ScriptManageViewButton"] = (parameters) => CreateScriptEditPage(navigationService, parameters)
            };

            // 设置默认显示页面为主页
            CurrentPage = GetOrCreatePage("HomeButton");

            // 注册主窗口到资源管理器，用于全局资源管理
            ResourceManager.RegisterWindow(GetMainWindow());

            // 初始化导航按钮样式
            UpdateNavigationButtonStyles();
        }

        #endregion

        #region 页面创建方法

        /// <summary>
        /// 创建脚本编辑页面（支持编辑模式）
        /// </summary>
        /// <param name="navigationService">导航服务</param>
        /// <param name="parameters">页面参数</param>
        /// <returns>脚本编辑页面</returns>
        private UserControl CreateScriptEditPage(NavigationService navigationService, Dictionary<string, object> parameters)
        {
            var viewModel = new ScriptManageViewModel(navigationService);
            var page = new ScriptManageView();
            page.DataContext = viewModel;

            // 检查是否有要编辑的脚本
            if (parameters.TryGetValue("EditScript", out var scriptObj) && scriptObj is ScriptInfo script)
            {
                // 加载脚本进行编辑
                viewModel.LoadScriptForEdit(script);
            }
            else
            {
                // 初始化为新建脚本模式
                viewModel.InitializeForNewScript();
            }

            return page;
        }

        /// <summary>
        /// 创建脚本引用设置页面
        /// </summary>
        /// <param name="navigationService">导航服务</param>
        /// <returns>脚本引用设置页面</returns>
        private UserControl CreateScriptReferenceSettingsPage(NavigationService navigationService)
        {
            var settingsService = App.GetService<IAppSettingsService>();
            if (settingsService == null)
            {
                throw new InvalidOperationException("无法获取应用程序设置服务");
            }

            var viewModel = new ScriptReferenceSettingsViewModel(settingsService, navigationService);
            var page = new ScriptReferenceSettingsView();
            page.DataContext = viewModel;

            return page;
        }

        #endregion

        #region 命令定义

        /// <summary>
        /// 最小化窗口命令
        /// 将窗口最小化到任务栏
        /// </summary>
        [RelayCommand]
        private void MinimizeWindow()
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 关闭窗口命令
        /// 执行应用程序关闭流程，包括资源清理和子窗口管理
        /// </summary>
        [RelayCommand]
        private async Task CloseWindow()
        {
            await CloseApplication();
        }

        /// <summary>
        /// 页面切换命令
        /// 根据按钮名称切换到对应的页面
        /// </summary>
        /// <param name="buttonName">触发导航的按钮名称</param>
        [RelayCommand]
        private void SwitchToPage(string buttonName)
        {
            SwitchPage(buttonName);
        }

        #endregion

        #region 页面管理

        /// <summary>
        /// 创建页面实例的通用方法
        /// 使用泛型约束确保类型安全，并设置数据上下文
        /// </summary>
        /// <typeparam name="TView">页面视图类型</typeparam>
        /// <param name="viewModel">页面对应的视图模型</param>
        /// <returns>创建的页面实例</returns>
        private UserControl CreatePage<TView>(object viewModel) where TView : UserControl, new()
        {
            var page = new TView();
            page.DataContext = viewModel;
            return page;
        }

        /// <summary>
        /// 获取或创建页面实例
        /// 实现页面的延迟加载和缓存机制，提高应用程序性能
        /// </summary>
        /// <param name="pageKey">页面标识键</param>
        /// <returns>页面实例</returns>
        private UserControl GetOrCreatePage(string pageKey)
        {
            // 首先检查缓存中是否已存在页面实例
            if (_pageCache.TryGetValue(pageKey, out var cachedPage))
            {
                return cachedPage;
            }

            // 如果缓存中不存在，使用工厂方法创建新实例
            if (_pageFactory.TryGetValue(pageKey, out var factory))
            {
                var newPage = factory();
                _pageCache[pageKey] = newPage; // 缓存新创建的页面
                return newPage;
            }

            // 如果没有找到对应的页面工厂，返回主页作为默认页面
            return GetOrCreatePage("HomeButton");
        }

        /// <summary>
        /// 获取或创建带参数的页面实例
        /// </summary>
        /// <param name="pageKey">页面标识键</param>
        /// <param name="parameters">页面参数</param>
        /// <returns>页面实例</returns>
        private UserControl GetOrCreatePageWithParameters(string pageKey, Dictionary<string, object> parameters)
        {
            // 对于带参数的页面，我们不缓存，每次都创建新实例
            // 这样可以确保参数正确传递

            if (_pageWithParametersFactory.TryGetValue(pageKey, out var factory))
            {
                var newPage = factory(parameters);
                
                // 更新缓存（替换旧的页面）
                _pageCache[pageKey] = newPage;
                
                return newPage;
            }

            // 如果没有找到带参数的工厂，回退到正常工厂
            return GetOrCreatePage(pageKey);
        }

        /// <summary>
        /// 更新导航按钮样式
        /// 根据当前活动状态设置对应的样式
        /// </summary>
        private void UpdateNavigationButtonStyles()
        {
            var activeStyle = Application.Current.TryFindResource("activeMenuButton") as Style;
            var normalStyle = Application.Current.TryFindResource("menuButton") as Style;

            HomeButtonStyle = IsHomeActive ? activeStyle : normalStyle;
            TasksManageButtonStyle = IsTasksManageActive ? activeStyle : normalStyle;
            ScriptManageButtonStyle = IsScriptManageActive ? activeStyle : normalStyle;
            ScriptReferenceSettingsButtonStyle = IsScriptReferenceSettingsActive ? activeStyle : normalStyle;
        }

        /// <summary>
        /// 处理首页按钮活动状态变更
        /// </summary>
        partial void OnIsHomeActiveChanged(bool value)
        {
            UpdateNavigationButtonStyles();
        }

        /// <summary>
        /// 处理任务管理按钮活动状态变更
        /// </summary>
        partial void OnIsTasksManageActiveChanged(bool value)
        {
            UpdateNavigationButtonStyles();
        }

        /// <summary>
        /// 处理脚本管理按钮活动状态变更
        /// </summary>
        partial void OnIsScriptManageActiveChanged(bool value)
        {
            UpdateNavigationButtonStyles();
        }

        /// <summary>
        /// 处理脚本引用设置按钮活动状态变更
        /// </summary>
        partial void OnIsScriptReferenceSettingsActiveChanged(bool value)
        {
            UpdateNavigationButtonStyles();
        }

        /// <summary>
        /// 页面切换的核心方法
        /// 根据页面键切换到对应的页面并更新导航状态
        /// </summary>
        /// <param name="pageKey">页面标识键</param>
        public void SwitchPage(string pageKey)
        {
            UserControl targetPage;
            
            // 对于脚本编辑页面，如果是新建模式（无参数导航），总是创建新实例
            if (pageKey == "ScriptManageViewButton")
            {
                // 清除缓存，强制创建新实例
                _pageCache.Remove(pageKey);
                targetPage = GetOrCreatePage(pageKey);
                
                // 确保新实例初始化为新建模式
                if (targetPage is ScriptManageView scriptView && scriptView.ViewModel != null)
                {
                    scriptView.ViewModel.InitializeForNewScript();
                }
            }
            else
            {
                // 其他页面使用正常的缓存逻辑
                targetPage = GetOrCreatePage(pageKey);
            }
            
            // 切换到目标页面
            CurrentPage = targetPage;
            CurrentPageKey = pageKey;
            
            // 更新导航按钮的活动状态
            UpdateNavigationActiveStates(pageKey);
        }

        /// <summary>
        /// 带参数的页面切换方法
        /// </summary>
        /// <param name="pageKey">页面标识键</param>
        /// <param name="parameters">页面参数</param>
        public void SwitchPageWithParameters(string pageKey, Dictionary<string, object> parameters)
        {
            // 获取或创建目标页面（带参数）
            var targetPage = GetOrCreatePageWithParameters(pageKey, parameters);
            
            // 切换到目标页面
            CurrentPage = targetPage;
            CurrentPageKey = pageKey;
            
            // 更新导航按钮的活动状态
            UpdateNavigationActiveStates(pageKey);
        }

        /// <summary>
        /// 更新导航按钮的活动状态
        /// 根据当前页面键设置相应的按钮为活动状态
        /// </summary>
        /// <param name="pageKey">当前页面键</param>
        private void UpdateNavigationActiveStates(string pageKey)
        {
            // 重置所有按钮的活动状态
            IsHomeActive = false;
            IsTasksManageActive = false;
            IsScriptManageActive = false;
            IsScriptReferenceSettingsActive = false;
            
            // 根据页面键设置对应按钮为活动状态
            switch (pageKey)
            {
                case "HomeButton":
                    IsHomeActive = true;
                    break;
                case "TasksManageButton":
                    IsTasksManageActive = true;
                    break;
                case "ScriptManageButton":
                case "ScriptManageViewButton": // 脚本编辑页面属于脚本管理
                    IsScriptManageActive = true;
                    break;
                case "ScriptReferenceSettings": // 脚本引用设置独立显示
                    IsScriptReferenceSettingsActive = true;
                    break;
            }
        }

        #endregion

        #region 子窗口管理

        /// <summary>
        /// 注册子窗口到管理列表
        /// 用于统一管理所有子窗口的生命周期
        /// </summary>
        /// <param name="childWindow">要注册的子窗口实例</param>
        public void RegisterChildWindow(Window childWindow)
        {
            // 验证参数并避免重复注册
            if (childWindow == null || _childWindows.Contains(childWindow))
                return;

            // 添加到管理列表
            _childWindows.Add(childWindow);
            
            // 当子窗口关闭时自动从列表中移除
            childWindow.Closed += (s, e) => _childWindows.Remove(childWindow);
        }

        #endregion

        #region 应用程序生命周期管理

        /// <summary>
        /// 关闭应用程序的主要方法
        /// 执行完整的关闭流程，包括取消操作、关闭子窗口、清理资源等
        /// </summary>
        public async Task CloseApplication()
        {
            // 防止重复执行关闭逻辑
            if (_isClosing) return;
            _isClosing = true;

            try
            {
                // 1. 发送取消信号给所有正在进行的操作
                _applicationCancellationTokenSource.Cancel();

                // 2. 关闭所有注册的子窗口
                foreach (var childWindow in _childWindows.ToArray())
                {
                    try
                    {
                        childWindow.Close();
                    }
                    catch (Exception)
                    {
                        // 忽略关闭子窗口时的异常，确保程序能够继续关闭
                    }
                }

                // 3. 清理页面资源
                await CleanupPageResources();

                // 4. 执行垃圾回收，释放内存
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // 5. 关闭整个应用程序
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                // 关闭过程中出现异常时的处理
                MessageBox.Show($"关闭应用程序时发生错误: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                
                // 强制关闭应用程序
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// 清理页面资源的方法
        /// 释放所有缓存的页面和相关的视图模型资源
        /// </summary>
        private async Task CleanupPageResources()
        {
            try
            {
                // 遍历所有缓存的页面
                foreach (var page in _pageCache.Values)
                {
                    // 如果页面的 DataContext 实现了 IDisposable，则调用 Dispose 方法
                    if (page?.DataContext is IDisposable disposableViewModel)
                    {
                        disposableViewModel.Dispose();
                    }
                }
                
                // 清空页面缓存
                _pageCache.Clear();
            }
            catch (Exception)
            {
                // 忽略清理过程中的异常
            }

            // 给资源清理一些时间
            await Task.Delay(100);
        }

        /// <summary>
        /// 获取应用程序关闭取消令牌
        /// 供其他组件订阅应用程序关闭事件
        /// </summary>
        /// <returns>应用程序关闭取消令牌</returns>
        public CancellationToken GetApplicationCancellationToken()
        {
            return _applicationCancellationTokenSource.Token;
        }

        #endregion

        #region 主窗口引用管理

        /// <summary>
        /// 静态主窗口实例引用
        /// 用于在 ViewModel 中访问主窗口，支持对话框显示等操作
        /// </summary>
        private static MainWindow? _mainWindowInstance;

        /// <summary>
        /// 设置主窗口实例的静态方法
        /// 由主窗口在初始化时调用，建立 ViewModel 与 View 的连接
        /// </summary>
        /// <param name="mainWindow">主窗口实例</param>
        public static void SetMainWindow(MainWindow mainWindow)
        {
            _mainWindowInstance = mainWindow;
        }

        /// <summary>
        /// 获取主窗口实例的私有方法
        /// 提供对主窗口的安全访问
        /// </summary>
        /// <returns>主窗口实例，可能为 null</returns>
        private MainWindow? GetMainWindow()
        {
            return _mainWindowInstance;
        }

        #endregion
    }
}