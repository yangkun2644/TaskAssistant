using System;
using System.Collections.Generic;

namespace TaskAssistant.Services
{
    /// <summary>
    /// 导航服务接口
    /// 定义页面导航的基本功能，实现页面间的解耦和统一管理
    /// 支持基于字符串键的页面导航、参数传递和主窗口访问
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// 导航到指定页面
        /// 根据页面键切换到对应的用户控件页面
        /// </summary>
        /// <param name="pageKey">页面标识键，对应页面工厂中的键值</param>
        void NavigateTo(string pageKey);
        
        /// <summary>
        /// 导航到指定页面并传递参数
        /// 根据页面键切换到对应的用户控件页面，同时传递参数字典
        /// </summary>
        /// <param name="pageKey">页面标识键，对应页面工厂中的键值</param>
        /// <param name="parameters">导航参数字典，键值对形式传递数据</param>
        void NavigateToWithParameters(string pageKey, Dictionary<string, object> parameters);
        
        /// <summary>
        /// 获取当前主窗口实例
        /// 用于显示对话框、设置父窗口等操作
        /// </summary>
        /// <returns>主窗口实例，可能为 null</returns>
        MainWindow? GetMainWindow();
    }
    
    /// <summary>
    /// 导航服务实现类
    /// 实现基于委托的页面导航机制，提供灵活的导航控制
    /// 通过依赖注入的方式接收导航和主窗口访问的委托
    /// 支持无参数和带参数的页面导航
    /// </summary>
    public class NavigationService : INavigationService
    {
        #region 私有字段

        /// <summary>
        /// 页面导航委托
        /// 实际的页面切换逻辑，由主窗口或页面管理器提供
        /// </summary>
        private readonly Action<string> _navigationCallback;
        
        /// <summary>
        /// 带参数的页面导航委托
        /// 实际的页面切换逻辑，支持参数传递，由主窗口或页面管理器提供
        /// </summary>
        private readonly Action<string, Dictionary<string, object>> _navigationWithParametersCallback;
        
        /// <summary>
        /// 主窗口获取委托
        /// 提供对主窗口实例的访问，用于对话框显示等操作
        /// </summary>
        private readonly Func<MainWindow?> _getMainWindow;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化导航服务实例
        /// 通过构造函数注入必要的委托，实现控制反转
        /// </summary>
        /// <param name="navigationCallback">页面导航委托，用于实际的页面切换</param>
        /// <param name="getMainWindow">主窗口获取委托，用于访问主窗口实例</param>
        /// <exception cref="ArgumentNullException">当任何委托参数为 null 时抛出</exception>
        public NavigationService(Action<string> navigationCallback, Func<MainWindow?> getMainWindow)
        {
            // 验证依赖项，确保服务的正确初始化
            _navigationCallback = navigationCallback ?? throw new ArgumentNullException(nameof(navigationCallback));
            _getMainWindow = getMainWindow ?? throw new ArgumentNullException(nameof(getMainWindow));
            
            // 默认的带参数导航实现，如果没有提供专门的带参数委托
            _navigationWithParametersCallback = (pageKey, parameters) => _navigationCallback(pageKey);
        }

        /// <summary>
        /// 初始化导航服务实例（支持参数传递）
        /// 通过构造函数注入必要的委托，实现控制反转
        /// </summary>
        /// <param name="navigationCallback">页面导航委托，用于实际的页面切换</param>
        /// <param name="navigationWithParametersCallback">带参数的页面导航委托，用于带参数的页面切换</param>
        /// <param name="getMainWindow">主窗口获取委托，用于访问主窗口实例</param>
        /// <exception cref="ArgumentNullException">当任何委托参数为 null 时抛出</exception>
        public NavigationService(
            Action<string> navigationCallback, 
            Action<string, Dictionary<string, object>> navigationWithParametersCallback, 
            Func<MainWindow?> getMainWindow)
        {
            // 验证依赖项，确保服务的正确初始化
            _navigationCallback = navigationCallback ?? throw new ArgumentNullException(nameof(navigationCallback));
            _navigationWithParametersCallback = navigationWithParametersCallback ?? throw new ArgumentNullException(nameof(navigationWithParametersCallback));
            _getMainWindow = getMainWindow ?? throw new ArgumentNullException(nameof(getMainWindow));
        }

        #endregion

        #region INavigationService 接口实现

        /// <summary>
        /// 执行页面导航操作
        /// 调用注入的导航委托来实现实际的页面切换
        /// </summary>
        /// <param name="pageKey">目标页面的标识键</param>
        public void NavigateTo(string pageKey)
        {
            // 委托给注入的导航回调方法执行实际导航
            _navigationCallback(pageKey);
        }
        
        /// <summary>
        /// 执行带参数的页面导航操作
        /// 调用注入的带参数导航委托来实现实际的页面切换和参数传递
        /// </summary>
        /// <param name="pageKey">目标页面的标识键</param>
        /// <param name="parameters">导航参数字典</param>
        public void NavigateToWithParameters(string pageKey, Dictionary<string, object> parameters)
        {
            // 委托给注入的带参数导航回调方法执行实际导航
            _navigationWithParametersCallback(pageKey, parameters ?? new Dictionary<string, object>());
        }
        
        /// <summary>
        /// 获取主窗口实例
        /// 调用注入的主窗口获取委托来访问主窗口
        /// </summary>
        /// <returns>主窗口实例，如果无法获取则返回 null</returns>
        public MainWindow? GetMainWindow()
        {
            // 委托给注入的获取主窗口回调方法
            return _getMainWindow();
        }

        #endregion

        #region 扩展功能预留

        // 未来可以添加更多导航功能，例如：
        // - 导航历史记录 (NavigationHistory)
        // - 返回上一页功能 (GoBack())
        // - 前进到下一页功能 (GoForward())
        // ✓ 导航参数传递 (NavigateToWithParameters()) - 已实现
        // - 条件导航 (ConditionalNavigate())
        // - 导航确认机制 (NavigateWithConfirmation())
        // - 页面生命周期事件 (PageNavigated, PageNavigating)
        // - 导航缓存管理 (ClearCache(), PreloadPage())

        #endregion
    }
}