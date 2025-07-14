using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using TaskAssistant.Common;
using TaskAssistant.Services;
using TaskAssistant.Models;

namespace TaskAssistant.View
{
    /// <summary>
    /// 重构后的脚本运行对话框 - 使用脚本执行服务
    /// 提供脚本执行、实时输出显示和智能滚动功能
    /// 支持执行日志保存到数据库
    /// </summary>
    public partial class ScriptRunDialog : Window
    {
        #region 私有字段

        /// <summary>要执行的脚本代码</summary>
        private readonly string _code;

        /// <summary>执行标题</summary>
        private readonly string _title;

        /// <summary>关联的脚本ID</summary>
        private readonly int? _scriptId;

        /// <summary>关联的任务ID</summary>
        private readonly int? _taskId;

        /// <summary>脚本执行服务</summary>
        private readonly IScriptExecutionService _scriptExecutionService;

        /// <summary>脚本是否正在运行</summary>
        private bool _isRunning = false;

        /// <summary>输出文本缓存</summary>
        private StringBuilder _outputBuilder = new StringBuilder();

        /// <summary>脚本取消令牌源</summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>脚本执行任务</summary>
        private Task<ScriptExecutionResult> _executionTask;

        /// <summary>输出区域的滚动视图控件</summary>
        private ScrollViewer _outputScrollViewer;

        /// <summary>是否启用自动滚动</summary>
        private bool _autoScroll = true;

        /// <summary>用户是否手动滚动过</summary>
        private bool _userScrolled = false;

        /// <summary>是否正在清理资源，防止重复清理</summary>
        private bool _isCleaningUp = false;

        /// <summary>窗口是否已经关闭</summary>
        private bool _isClosed = false;

        /// <summary>脚本是否已经执行完成</summary>
        private bool _scriptCompleted = false;

        /// <summary>执行结果</summary>
        private ScriptExecutionResult? _executionResult;

        /// <summary>最大输出长度限制</summary>
        private const int MaxOutputLength = 500000; // 500KB

        #endregion

        #region 构造函数和初始化

        /// <summary>
        /// 初始化脚本运行对话框
        /// </summary>
        /// <param name="code">要执行的脚本代码</param>
        /// <param name="title">窗口标题</param>
        /// <param name="scriptId">关联的脚本ID</param>
        /// <param name="taskId">关联的任务ID</param>
        public ScriptRunDialog(string code, string title = "脚本运行", int? scriptId = null, int? taskId = null)
        {
            InitializeComponent();

            _code = code;
            _title = title;
            _scriptId = scriptId;
            _taskId = taskId;

            // 从依赖注入容器获取脚本执行服务
            _scriptExecutionService = App.GetRequiredService<IScriptExecutionService>();

            // 设置窗口标题和状态
            TitleBlock.Text = title;
            StatusBlock.Text = "状态：准备执行";
            OutputBox.Text = "";

            // 注册事件处理器
            this.Closing += Window_Closing;
            this.Loaded += OnWindowLoaded;

            // 注册到资源管理器
            ResourceManager.RegisterWindow(this);

            // 更新按钮文本
            UpdateButtonText();

            // 订阅脚本执行服务的事件
            SubscribeToExecutionEvents();
        }

        /// <summary>
        /// 订阅脚本执行服务的事件
        /// </summary>
        private void SubscribeToExecutionEvents()
        {
            _scriptExecutionService.OutputReceived += OnOutputReceived;
            _scriptExecutionService.ErrorReceived += OnErrorReceived;
            _scriptExecutionService.StatusChanged += OnStatusChanged;
        }

        /// <summary>
        /// 取消订阅脚本执行服务的事件
        /// </summary>
        private void UnsubscribeFromExecutionEvents()
        {
            _scriptExecutionService.OutputReceived -= OnOutputReceived;
            _scriptExecutionService.ErrorReceived -= OnErrorReceived;
            _scriptExecutionService.StatusChanged -= OnStatusChanged;
        }

        /// <summary>
        /// 更新按钮文本
        /// </summary>
        private void UpdateButtonText()
        {
            if (_isClosed || ResourceManager.IsShuttingDown) return;

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(() => UpdateButtonText(), DispatcherPriority.Background);
                return;
            }

            try
            {
                if (_isRunning)
                {
                    // 脚本运行时，取消按钮显示"停止"
                    var cancelTextBlock = CancelButton?.Content as StackPanel;
                    if (cancelTextBlock?.Children.Count > 1 && cancelTextBlock.Children[1] is TextBlock textBlock)
                    {
                        textBlock.Text = "停止";
                    }

                    // 确定按钮显示"关闭"
                    var okTextBlock = OkButton?.Content as StackPanel;
                    if (okTextBlock?.Children.Count > 1 && okTextBlock.Children[1] is TextBlock okText)
                    {
                        okText.Text = "关闭";
                    }
                }
                else
                {
                    // 脚本未运行时
                    var cancelTextBlock = CancelButton?.Content as StackPanel;
                    if (cancelTextBlock?.Children.Count > 1 && cancelTextBlock.Children[1] is TextBlock textBlock)
                    {
                        textBlock.Text = _scriptCompleted ? "关闭" : "取消";
                    }

                    // 确定按钮显示"确定"
                    var okTextBlock = OkButton?.Content as StackPanel;
                    if (okTextBlock?.Children.Count > 1 && okTextBlock.Children[1] is TextBlock okText)
                    {
                        okText.Text = "确定";
                    }
                }
            }
            catch (Exception)
            {
                // 忽略UI更新异常
            }
        }

        /// <summary>
        /// 窗口加载完成后的初始化操作
        /// </summary>
        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            InitializeScrollViewer();
            await StartScriptExecution();
        }

        /// <summary>
        /// 初始化滚动视图控件和事件监听
        /// </summary>
        private void InitializeScrollViewer()
        {
            // 查找输出文本框的父级滚动视图
            _outputScrollViewer = FindParentControl<ScrollViewer>(OutputBox);

            if (_outputScrollViewer != null)
            {
                // 使用节流的滚动事件处理
                _outputScrollViewer.ScrollChanged += OnScrollChanged;
            }
        }

        /// <summary>
        /// 在可视化树中查找指定类型的父级控件
        /// </summary>
        private static T FindParentControl<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
            if (parent == null) return null;

            return parent is T parentControl ? parentControl : FindParentControl<T>(parent);
        }

        #endregion

        #region 脚本执行逻辑

        /// <summary>
        /// 启动脚本执行流程
        /// </summary>
        private async Task StartScriptExecution()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            ResourceManager.RegisterResource(_cancellationTokenSource);

            _isRunning = true;
            UpdateButtonText();
            ClearOutput();

            try
            {
                // 使用脚本执行服务执行脚本并保存日志
                _executionTask = _scriptExecutionService.ExecuteAsync(_code, _title, _cancellationTokenSource.Token, _scriptId, _taskId);
                _executionResult = await _executionTask;

                // 保存执行日志到数据库
                if (_executionResult != null)
                {
                    try
                    {
                        await _scriptExecutionService.SaveExecutionLogAsync(_executionResult, _scriptId, _taskId);
                    }
                    catch (Exception ex)
                    {
                        OnErrorReceived($"保存执行日志失败: {ex.Message}\n");
                    }
                }

                _scriptCompleted = true;
                UpdateButtonText();
            }
            catch (OperationCanceledException)
            {
                OnOutputReceived("\n脚本执行已被用户取消\n");
                OnStatusChanged("已取消");
                _scriptCompleted = true;
                UpdateButtonText();
            }
            catch (Exception ex)
            {
                OnErrorReceived($"\n执行任务异常: {ex.Message}\n");
                OnStatusChanged("执行异常");
                _scriptCompleted = true;
                UpdateButtonText();
            }
            finally
            {
                _isRunning = false;
                UpdateButtonText();
                
                // 执行内存清理
                if (!_isClosed && !ResourceManager.IsShuttingDown)
                {
                    await PerformSilentMemoryCleanup();
                }
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 处理输出接收事件
        /// </summary>
        /// <param name="text">输出文本</param>
        private void OnOutputReceived(string text)
        {
            if (string.IsNullOrEmpty(text) || _isClosed || ResourceManager.IsShuttingDown) return;

            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    // 添加 null 检查，确保 _outputBuilder 不为 null
                    if (_outputBuilder == null)
                    {
                        _outputBuilder = new StringBuilder();
                    }
                    
                    _outputBuilder.Append(text);

                    // 检查并优化缓冲区大小
                    if (_outputBuilder.Length > MaxOutputLength)
                    {
                        OptimizeOutputBuffer();
                    }

                    // 更新UI文本
                    OutputBox.Text = _outputBuilder.ToString();

                    // 执行自动滚动
                    PerformAutoScroll();
                }
                catch (Exception)
                {
                    // 忽略UI更新异常
                }
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// 处理错误输出接收事件
        /// </summary>
        /// <param name="text">错误文本</param>
        private void OnErrorReceived(string text)
        {
            if (string.IsNullOrEmpty(text) || _isClosed || ResourceManager.IsShuttingDown) return;

            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    // 添加 null 检查，确保 _outputBuilder 不为 null
                    if (_outputBuilder == null)
                    {
                        _outputBuilder = new StringBuilder();
                    }
                    
                    _outputBuilder.Append($"[错误] {text}");

                    // 检查并优化缓冲区大小
                    if (_outputBuilder.Length > MaxOutputLength)
                    {
                        OptimizeOutputBuffer();
                    }

                    // 更新UI文本
                    OutputBox.Text = _outputBuilder.ToString();

                    // 执行自动滚动
                    PerformAutoScroll();
                }
                catch (Exception)
                {
                    // 忽略UI更新异常
                }
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// 处理状态变更事件
        /// </summary>
        /// <param name="status">新状态</param>
        private void OnStatusChanged(string status)
        {
            if (_isClosed || ResourceManager.IsShuttingDown) return;

            Dispatcher.BeginInvoke(() =>
            {
                if (!_isClosed && !ResourceManager.IsShuttingDown && StatusBlock != null)
                {
                    StatusBlock.Text = $"状态：{status}";
                }
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// 优化输出缓冲区，防止内存过度增长
        /// </summary>
        private void OptimizeOutputBuffer()
        {
            if (_outputBuilder?.Length > MaxOutputLength)
            {
                var content = _outputBuilder.ToString();
                var keepLength = MaxOutputLength / 2; // 保留一半内容
                var startIndex = Math.Max(0, content.Length - keepLength);

                _outputBuilder.Clear();
                _outputBuilder.Append("... [前部分内容已清理，避免内存溢出] ...\n");
                _outputBuilder.Append(content.Substring(startIndex));
            }
        }

        #endregion

        #region 滚动控制逻辑

        private DateTime _lastScrollCheck = DateTime.MinValue;
        private const int ScrollCheckInterval = 50; // 50ms节流

        /// <summary>
        /// 处理滚动事件，实现智能自动滚动功能（节流优化）
        /// </summary>
        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 节流处理，避免过度频繁的滚动检查
            var now = DateTime.Now;
            if ((now - _lastScrollCheck).TotalMilliseconds < ScrollCheckInterval)
                return;

            _lastScrollCheck = now;

            // 检查是否是用户手动滚动（内容高度未变化）
            if (e.ExtentHeightChange == 0)
            {
                // 标记用户已手动滚动，停止自动滚动
                _userScrolled = true;
                _autoScroll = false;

                // 检查用户是否滚动到了底部附近
                if (IsScrolledToBottom())
                {
                    // 用户滚动到底部，重新启用自动滚动
                    _autoScroll = true;
                    _userScrolled = false;
                }
            }
            else
            {
                // 内容高度发生变化（新内容添加），如果用户在底部附近，重新启用自动滚动
                if (IsScrolledToBottom() && _userScrolled)
                {
                    _autoScroll = true;
                    _userScrolled = false;
                }
            }
        }

        /// <summary>
        /// 检查滚动条是否在底部位置
        /// </summary>
        private bool IsScrolledToBottom()
        {
            if (_outputScrollViewer == null) return true;

            const double tolerance = 5.0; // 增加容错范围到5像素
            return _outputScrollViewer.VerticalOffset >= (_outputScrollViewer.ScrollableHeight - tolerance);
        }

        /// <summary>
        /// 执行自动滚动到底部（优化版本）
        /// </summary>
        private void PerformAutoScroll()
        {
            // 只有在启用自动滚动且用户未手动滚动时才执行
            if (_autoScroll && !_userScrolled)
            {
                // 使用较高优先级确保滚动及时响应
                Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        if (_outputScrollViewer != null)
                        {
                            _outputScrollViewer.ScrollToEnd();
                        }
                        else
                        {
                            OutputBox.ScrollToEnd();
                        }
                    }
                    catch (Exception)
                    {
                        // 忽略滚动异常
                    }
                }, DispatcherPriority.Normal); // 提高优先级
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 清空输出
        /// </summary>
        private void ClearOutput()
        {
            if (_isClosed || ResourceManager.IsShuttingDown) return;

            Dispatcher.BeginInvoke(() =>
            {
                if (!_isClosed && !ResourceManager.IsShuttingDown && OutputBox != null)
                {
                    OutputBox.Text = "";
                    _outputBuilder?.Clear();
                    _outputBuilder = new StringBuilder(1024);
                    _autoScroll = true;
                    _userScrolled = false;
                }
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// 执行静默内存清理
        /// </summary>
        private async Task PerformSilentMemoryCleanup()
        {
            _isCleaningUp = true;

            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                await Task.Delay(50);
            }
            finally
            {
                _isCleaningUp = false;
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private async Task CleanupResources()
        {
            if (_isCleaningUp || ResourceManager.IsShuttingDown) return;
            _isCleaningUp = true;

            try
            {
                // 取消订阅事件
                UnsubscribeFromExecutionEvents();

                // 移除事件监听
                if (_outputScrollViewer != null)
                {
                    _outputScrollViewer.ScrollChanged -= OnScrollChanged;
                    _outputScrollViewer = null;
                }

                // 释放取消令牌源
                ResourceManager.SafeExecute(() =>
                {
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                }, "释放取消令牌源");

                // 清理输出缓冲区
                ResourceManager.SafeExecute(() =>
                {
                    _outputBuilder?.Clear();
                    _outputBuilder = null;
                }, "清理输出缓冲区");

                // 执行静默内存清理
                await PerformSilentMemoryCleanup();
            }
            catch (Exception)
            {
                // 忽略清理时的异常
            }
            finally
            {
                _isCleaningUp = false;
            }
        }

        #endregion

        #region 事件处理器

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                await ForceStopScript();
            }
            else
            {
                DialogResult = false;
                await CleanupAndClose();
            }
        }

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        private async void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                var result = MessageBox.Show("脚本正在运行中，确定要关闭吗？", "确认",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;

                await ForceStopScript();
            }

            DialogResult = true;
            await CleanupAndClose();
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isClosed || ResourceManager.IsShuttingDown) return;

            if (_isRunning)
            {
                var result = MessageBox.Show("脚本正在运行中，确定要关闭吗？", "确认",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

                _cancellationTokenSource?.Cancel();
                _isRunning = false;
            }

            _ = Task.Run(async () => await CleanupResources());
            _isClosed = true;
        }

        /// <summary>
        /// 清理并关闭窗口
        /// </summary>
        private async Task CleanupAndClose()
        {
            _isClosed = true;
            await CleanupResources();
            Close();
        }

        /// <summary>
        /// 强制停止脚本
        /// </summary>
        private async Task ForceStopScript()
        {
            OnStatusChanged("正在强制停止...");
            OnOutputReceived("\n正在强制停止脚本执行...\n");

            _cancellationTokenSource?.Cancel();

            try
            {
                if (_executionTask != null)
                {
                    await Task.WhenAny(_executionTask, Task.Delay(1000));
                }
            }
            catch (Exception ex)
            {
                OnErrorReceived($"停止脚本时发生异常: {ex.Message}\n");
            }

            if (_isRunning)
            {
                _isRunning = false;
                OnOutputReceived("\n脚本已强制停止\n");
                OnStatusChanged("已强制停止");
                _scriptCompleted = true;
                UpdateButtonText();
            }

            await CleanupResources();
        }

        /// <summary>
        /// 处理鼠标左键按下事件，允许拖拽窗口
        /// </summary>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
                // 忽略拖拽异常
            }
        }

        /// <summary>
        /// 窗口关闭后的清理
        /// </summary>
        protected override async void OnClosed(EventArgs e)
        {
            if (!_isClosed)
            {
                _isClosed = true;
                await CleanupResources();
            }
            base.OnClosed(e);
        }

        /// <summary>
        /// 最小化按钮点击事件
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                var result = MessageBox.Show("脚本正在运行中，确定要关闭吗？", "确认",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;

                await ForceStopScript();
            }

            await CleanupAndClose();
        }

        #endregion
    }
}
