using System;
using System.Windows.Controls;
using TaskAssistant.ViewModels;
using Microsoft.Web.WebView2.Core;


namespace TaskAssistant.View
{
    /// <summary>
    /// 主页用户控件的交互逻辑类
    /// 应用程序的欢迎页面，提供基本的导航入口、应用程序信息展示和内嵌浏览器功能
    /// 使用 MVVM 模式，所有数据和逻辑都通过 HomeViewModel 处理
    /// </summary>
    public partial class Home : UserControl
    {
        #region 属性

        /// <summary>
        /// 获取当前视图绑定的主页视图模型
        /// </summary>
        public HomeViewModel ViewModel => (HomeViewModel)DataContext;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化主页用户控件
        /// 设置 XAML 组件并准备数据绑定
        /// </summary>
        public Home()
        {
            // 初始化 XAML 中定义的所有组件
            InitializeComponent();
            
            // 注册事件处理
            this.DataContextChanged += Home_DataContextChanged;
            this.Loaded += Home_Loaded;
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 处理 DataContext 变更事件
        /// </summary>
        private void Home_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            // 如果之前有 ViewModel，先解除事件绑定
            if (e.OldValue is HomeViewModel oldViewModel)
            {
                oldViewModel.OnRefreshRequested -= RefreshWebView;
                oldViewModel.OnGoBackRequested -= GoBackWebView;
                oldViewModel.OnGoForwardRequested -= GoForwardWebView;
            }

            // 为新的 ViewModel 绑定事件
            if (e.NewValue is HomeViewModel newViewModel)
            {
                newViewModel.OnRefreshRequested += RefreshWebView;
                newViewModel.OnGoBackRequested += GoBackWebView;
                newViewModel.OnGoForwardRequested += GoForwardWebView;
            }
        }

        /// <summary>
        /// 处理用户控件加载完成事件
        /// </summary>
        private async void Home_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                // 确保 WebView2 运行时环境已初始化
                await WebView.EnsureCoreWebView2Async(null);
                
                // 注册 WebView2 事件
                WebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
                WebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                WebView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebView2 初始化失败: {ex.Message}");
            }
        }

        #endregion

        #region WebView2 事件处理

        /// <summary>
        /// 导航开始事件处理
        /// </summary>
        private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.IsWebLoading = true;
            }
        }

        /// <summary>
        /// 导航完成事件处理
        /// </summary>
        private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.IsWebLoading = false;
                
                // 更新地址栏
                if (WebView.CoreWebView2 != null)
                {
                    ViewModel.WebUrl = WebView.CoreWebView2.Source;
                }
            }
        }

        /// <summary>
        /// 文档标题变更事件处理
        /// </summary>
        private void CoreWebView2_DocumentTitleChanged(object sender, object e)
        {
            // 可以在这里更新页面标题或其他UI元素
        }

        #endregion

        #region WebView2 操作方法

        /// <summary>
        /// 刷新网页
        /// </summary>
        private void RefreshWebView()
        {
            try
            {
                WebView?.Reload();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新网页失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 后退到上一页
        /// </summary>
        private void GoBackWebView()
        {
            try
            {
                if (WebView?.CoreWebView2?.CanGoBack == true)
                {
                    WebView.CoreWebView2.GoBack();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"后退失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 前进到下一页
        /// </summary>
        private void GoForwardWebView()
        {
            try
            {
                if (WebView?.CoreWebView2?.CanGoForward == true)
                {
                    WebView.CoreWebView2.GoForward();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"前进失败: {ex.Message}");
            }
        }

        #endregion

        #region 资源清理

        /// <summary>
        /// 清理资源
        /// </summary>
        private void CleanupWebView()
        {
            try
            {
                if (WebView?.CoreWebView2 != null)
                {
                    WebView.CoreWebView2.NavigationStarting -= CoreWebView2_NavigationStarting;
                    WebView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
                    WebView.CoreWebView2.DocumentTitleChanged -= CoreWebView2_DocumentTitleChanged;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理 WebView2 资源失败: {ex.Message}");
            }
        }

        #endregion
    }
}
