using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TaskAssistant.ViewModels;
using TaskAssistant.Common;

namespace TaskAssistant
{
    /// <summary>
    /// 主窗口类 - 应用程序的主要用户界面
    /// 负责管理整个应用程序的显示和用户交互，包括页面容器、窗口控制、子窗口管理等功能
    /// 使用 MVVM 模式，大部分业务逻辑委托给 MainWindowViewModel 处理
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 属性

        /// <summary>
        /// 主窗口的视图模型实例
        /// 提供数据绑定和命令处理，是 MVVM 架构的核心组件
        /// 通过此属性可以访问所有业务逻辑和数据操作
        /// </summary>
        public MainWindowViewModel ViewModel { get; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化主窗口实例
        /// 设置视图模型、数据绑定、事件注册和基本配置
        /// </summary>
        public MainWindow()
        {
            // 初始化 XAML 中定义的所有组件
            InitializeComponent();
            
            // 创建并设置视图模型实例
            ViewModel = new MainWindowViewModel();
            DataContext = ViewModel;
            
            // 设置静态引用，供 ViewModel 和其他组件使用
            // 这种方式在 MVVM 中是一种权衡，用于访问主窗口实例
            MainWindowViewModel.SetMainWindow(this);
            
            // 注册窗口生命周期事件
            this.Closing += MainWindow_Closing;  // 窗口即将关闭
            this.Closed += MainWindow_Closed;    // 窗口已关闭
            
            // 注册应用程序级别的退出事件
            Application.Current.Exit += Application_Exit;
        }

        #endregion

        #region 窗口交互事件处理

        /// <summary>
        /// 处理窗口拖拽移动功能
        /// 允许用户通过点击窗口标题栏区域来拖拽移动窗口
        /// 这对于无边框窗口特别重要
        /// </summary>
        /// <param name="sender">事件发送者（通常是窗口的边框区域）</param>
        /// <param name="e">鼠标按钮事件参数</param>
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 只响应鼠标左键点击
            if (e.ChangedButton == MouseButton.Left)
            {
                // 开始拖拽移动窗口
                this.DragMove();
            }
        }

        #endregion

        #region 窗口生命周期事件处理

        /// <summary>
        /// 处理主窗口即将关闭事件
        /// 在窗口实际关闭前进行必要的清理和确认操作
        /// 使用异步方式处理，避免阻塞 UI 线程
        /// </summary>
        /// <param name="sender">事件发送者（主窗口）</param>
        /// <param name="e">窗口关闭事件参数</param>
        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 取消默认的关闭行为，由 ViewModel 来控制关闭流程
            e.Cancel = true;
            
            // 异步执行关闭流程，包括：
            // - 保存用户数据和设置
            // - 关闭所有子窗口
            // - 停止正在运行的任务
            // - 清理系统资源
            await ViewModel.CloseApplication();
        }

        /// <summary>
        /// 处理主窗口已关闭事件
        /// 在窗口完全关闭后执行最终的清理工作
        /// </summary>
        /// <param name="sender">事件发送者（主窗口）</param>
        /// <param name="e">事件参数</param>
        private async void MainWindow_Closed(object sender, EventArgs e)
        {
            try
            {
                // 执行最终的内存清理
                // 强制进行垃圾回收，释放所有可能的内存
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                // 给清理过程一些时间
                await Task.Delay(100);
            }
            catch (Exception)
            {
                // 忽略清理过程中的异常，确保程序能够正常退出
            }
        }

        /// <summary>
        /// 处理应用程序退出事件
        /// 在整个应用程序即将退出时执行全局清理
        /// </summary>
        /// <param name="sender">事件发送者（Application 实例）</param>
        /// <param name="e">应用程序退出事件参数</param>
        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                // 执行应用程序级别的资源清理
                ResourceManager.SafeExecute(() =>
                {
                    // 清理全局资源
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }, "应用程序退出清理");
                
                // 等待清理完成
                await Task.Delay(50);
            }
            catch (Exception)
            {
                // 忽略清理异常，确保应用程序能够正常退出
            }
        }

        #endregion

        #region 子窗口管理（委托给 ViewModel）

        /// <summary>
        /// 注册子窗口到管理系统
        /// 将子窗口的管理委托给 ViewModel，保持架构的一致性
        /// 这样可以统一管理所有子窗口的生命周期
        /// </summary>
        /// <param name="childWindow">要注册的子窗口实例</param>
        public void RegisterChildWindow(Window childWindow)
        {
            // 委托给 ViewModel 处理子窗口注册
            ViewModel?.RegisterChildWindow(childWindow);
        }

        /// <summary>
        /// 获取应用程序关闭取消令牌
        /// 供子窗口和其他组件订阅应用程序关闭事件
        /// 当应用程序开始关闭流程时，所有订阅者将收到取消信号
        /// </summary>
        /// <returns>应用程序关闭取消令牌，如果 ViewModel 不可用则返回默认令牌</returns>
        public CancellationToken GetApplicationCancellationToken()
        {
            return ViewModel?.GetApplicationCancellationToken() ?? CancellationToken.None;
        }

        #endregion

        #region 扩展功能预留

        // 如果需要处理特定的窗口级别交互，可以在这里添加
        // 例如：
        // - 窗口大小调整事件处理
        // - 键盘全局快捷键处理
        // - 系统托盘图标管理
        // - 窗口状态保存和恢复
        // - 多显示器支持
        // - 主题和样式切换
        
        // 但建议优先使用 MVVM 模式：
        // - 通过 ViewModel 的命令处理用户操作
        // - 通过数据绑定控制窗口状态
        // - 保持 View 的简洁性

        #endregion
    }
}