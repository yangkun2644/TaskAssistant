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
    /// 主页视图模型类
    /// 负责管理应用程序主页的数据和交互逻辑，展示系统统计信息和内嵌浏览器功能
    /// </summary>
    public partial class HomeViewModel : ObservableObject, IDisposable
    {
        #region 私有字段

        /// <summary>
        /// 系统统计服务
        /// </summary>
        private readonly ISystemStatisticsService _statisticsService;

        /// <summary>
        /// 统计数据更新定时器
        /// </summary>
        private readonly DispatcherTimer _updateTimer;

        /// <summary>
        /// 是否已释放资源
        /// </summary>
        private bool _disposed = false;

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
        /// 内存使用量（MB）
        /// </summary>
        [ObservableProperty]
        private double _memoryUsageMB = 0;

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
        private string _cpuUsageText = "0%";
        private bool _isUpdateTimerEnabled = true;

        #endregion

        #region 可观察属性 - 浏览器

        /// <summary>
        /// 当前访问的网页URL
        /// </summary>
        [ObservableProperty]
        private string _webUrl = "http://ck23456.cn/";

        /// <summary>
        /// 是否正在加载网页
        /// </summary>
        [ObservableProperty]
        private bool _isWebLoading = false;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化主页视图模型
        /// </summary>
        public HomeViewModel()
        {
            _statisticsService = App.GetService<ISystemStatisticsService>() ?? 
                                new SystemStatisticsService(App.GetService<Data.Services.IDataService>());

            // 初始化定时器，每30秒更新一次数据
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _updateTimer.Tick += async (s, e) => await LoadStatisticsData();

            // 立即加载数据并启动定时器
            _ = Task.Run(LoadStatisticsData);
            _updateTimer.Start();
        }

        #endregion

        #region 命令 - 浏览器

        /// <summary>
        /// 导航到指定URL的命令
        /// </summary>
        [RelayCommand]
        private void Navigate()
        {
            if (!string.IsNullOrWhiteSpace(WebUrl))
            {
                // 确保URL包含协议
                if (!WebUrl.StartsWith("http://") && !WebUrl.StartsWith("https://"))
                {
                    WebUrl = "https://" + WebUrl;
                }
            }
        }

        /// <summary>
        /// 刷新网页的命令
        /// </summary>
        [RelayCommand]
        private void Refresh()
        {
            // 这个命令将在 Code-behind 中处理，因为需要直接操作 WebView2 控件
            OnRefreshRequested?.Invoke();
        }

        /// <summary>
        /// 返回上一页的命令
        /// </summary>
        [RelayCommand]
        private void GoBack()
        {
            OnGoBackRequested?.Invoke();
        }

        /// <summary>
        /// 前进到下一页的命令
        /// </summary>
        [RelayCommand]
        private void GoForward()
        {
            OnGoForwardRequested?.Invoke();
        }

        #endregion

        #region 事件

        /// <summary>
        /// 刷新请求事件
        /// </summary>
        public event Action? OnRefreshRequested;

        /// <summary>
        /// 后退请求事件
        /// </summary>
        public event Action? OnGoBackRequested;

        /// <summary>
        /// 前进请求事件
        /// </summary>
        public event Action? OnGoForwardRequested;

        #endregion

        #region 数据加载方法

        /// <summary>
        /// 加载统计数据
        /// </summary>
        private async Task LoadStatisticsData()
        {
            if (_disposed || IsLoading) return;

            IsLoading = true;

            try
            {
                var statistics = await _statisticsService.GetSystemStatisticsAsync();

                // 更新属性
                TotalScripts = statistics.TotalScripts;
                TodayExecutions = statistics.TodayExecutions;
                TotalTasks = statistics.TotalTasks;
                DatabaseSizeMB = statistics.DatabaseSizeMB;
                MemoryUsageMB = statistics.MemoryUsageMB;
                CpuUsagePercent = statistics.CpuUsagePercent;

                // 更新格式化文本
                DatabaseSizeText = $"{DatabaseSizeMB:F1} MB";
                MemoryUsageText = $"{MemoryUsageMB:F0} MB";
                CpuUsageText = $"{CpuUsagePercent:F1}%";

                LastUpdated = statistics.LastUpdated.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载统计数据失败: {ex.Message}");
                // 设置默认值
                LastUpdated = "加载失败";
            }
            finally
            {
                IsLoading = false;
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
                    // 启动定时器
                    _updateTimer.Start();
                }
                else
                {
                    // 停止定时器
                    _updateTimer.Stop();
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
                _disposed = true;
            }
        }

        #endregion
    }
}