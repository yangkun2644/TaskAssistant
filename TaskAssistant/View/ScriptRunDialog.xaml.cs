using System;
using System.IO;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Runtime.Loader;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using TaskAssistant.Common;
using System.Collections.Concurrent;

namespace TaskAssistant.View
{
    /// <summary>
    /// 优化后的脚本运行对话框 - 提供脚本执行、批量输出显示和智能滚动功能
    /// </summary>
    public partial class ScriptRunDialog : Window
    {
        #region 私有字段

        /// <summary>要执行的脚本代码</summary>
        private readonly string _code;
        
        /// <summary>脚本是否正在运行</summary>
        private bool _isRunning = false;
        
        /// <summary>输出文本缓存</summary>
        private StringBuilder _outputBuilder = new StringBuilder();
        
        /// <summary>脚本取消令牌源</summary>
        private CancellationTokenSource _cancellationTokenSource;
        
        /// <summary>脚本执行任务</summary>
        private Task _executionTask;
        
        /// <summary>编译后的脚本缓存</summary>
        private Script<object> _compiledScript;
        
        /// <summary>输出区域的滚动视图控件</summary>
        private ScrollViewer _outputScrollViewer;
        
        /// <summary>是否启用自动滚动</summary>
        private bool _autoScroll = true;
        
        /// <summary>用户是否手动滚动过</summary>
        private bool _userScrolled = false;

        /// <summary>脚本执行的程序集加载上下文</summary>
        private AssemblyLoadContext _scriptLoadContext;

        /// <summary>实时输出写入器实例</summary>
        private BatchedTextWriter _realTimeOut;
        private BatchedTextWriter _realTimeError;

        /// <summary>是否正在清理资源，防止重复清理</summary>
        private bool _isCleaningUp = false;

        /// <summary>窗口是否已经关闭</summary>
        private bool _isClosed = false;

        /// <summary>脚本是否已经执行完成</summary>
        private bool _scriptCompleted = false;

        /// <summary>输出更新定时器</summary>
        private DispatcherTimer _outputUpdateTimer;

        /// <summary>输出队列</summary>
        private readonly ConcurrentQueue<string> _outputQueue = new ConcurrentQueue<string>();

        /// <summary>错误输出队列</summary>
        private readonly ConcurrentQueue<string> _errorQueue = new ConcurrentQueue<string>();

        /// <summary>UI更新锁</summary>
        private readonly object _uiUpdateLock = new object();

        /// <summary>最大输出长度限制</summary>
        private const int MaxOutputLength = 500000; // 500KB

        /// <summary>批量更新间隔（毫秒）</summary>
        private const int BatchUpdateInterval = 100;

        #endregion

        #region 构造函数和初始化

        /// <summary>
        /// 初始化脚本运行对话框
        /// </summary>
        /// <param name="code">要执行的脚本代码</param>
        /// <param name="title">窗口标题</param>
        public ScriptRunDialog(string code, string title = "脚本运行")
        {
            InitializeComponent();
            
            // 设置窗口标题和状态
            TitleBlock.Text = title;
            StatusBlock.Text = "状态：准备执行";
            OutputBox.Text = "";
            _code = code;
            
            // 初始化输出更新定时器
            InitializeOutputTimer();
            
            // 注册事件处理器
            this.Closing += Window_Closing;
            this.Loaded += OnWindowLoaded;
            
            // 注册到资源管理器
            ResourceManager.RegisterWindow(this);
            
            // 更新按钮文本
            UpdateButtonText();
        }

        /// <summary>
        /// 初始化输出更新定时器
        /// </summary>
        private void InitializeOutputTimer()
        {
            _outputUpdateTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(BatchUpdateInterval)
            };
            _outputUpdateTimer.Tick += ProcessOutputQueue;
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
            _outputUpdateTimer.Start();
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

        #region 优化的输出处理

        /// <summary>
        /// 优化的批量文本写入器
        /// </summary>
        private class BatchedTextWriter : TextWriter
        {
            private readonly Action<string> _writeAction;
            private readonly Encoding _encoding;
            private readonly StringBuilder _buffer = new StringBuilder(1024);
            private readonly object _bufferLock = new object();
            private volatile bool _disposed = false;

            public BatchedTextWriter(Action<string> writeAction, Encoding? encoding = null)
            {
                _writeAction = writeAction;
                _encoding = encoding ?? Encoding.UTF8;
            }

            public override Encoding Encoding => _encoding;

            public override void Write(char value)
            {
                if (_disposed || ResourceManager.IsShuttingDown) return;
                
                lock (_bufferLock)
                {
                    _buffer.Append(value);
                    CheckAndFlush();
                }
            }

            public override void Write(string? value)
            {
                if (_disposed || ResourceManager.IsShuttingDown || string.IsNullOrEmpty(value)) return;
                
                lock (_bufferLock)
                {
                    _buffer.Append(value);
                    CheckAndFlush();
                }
            }

            public override void WriteLine(string? value)
            {
                if (_disposed || ResourceManager.IsShuttingDown) return;
                
                lock (_bufferLock)
                {
                    _buffer.AppendLine(value);
                    CheckAndFlush();
                }
            }

            public override void WriteLine()
            {
                if (_disposed || ResourceManager.IsShuttingDown) return;
                
                lock (_bufferLock)
                {
                    _buffer.AppendLine();
                    CheckAndFlush();
                }
            }

            private void CheckAndFlush()
            {
                // 当缓冲区达到一定大小或包含换行符时刷新
                if (_buffer.Length > 512 || _buffer.ToString().Contains('\n'))
                {
                    Flush();
                }
            }

            public override void Flush()
            {
                if (_disposed || ResourceManager.IsShuttingDown) return;
                
                lock (_bufferLock)
                {
                    if (_buffer.Length > 0)
                    {
                        var content = _buffer.ToString();
                        _buffer.Clear();
                        _writeAction?.Invoke(content);
                    }
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing && !_disposed)
                {
                    _disposed = true;
                    Flush();
                }
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// 处理输出队列（批量更新UI）
        /// </summary>
        private void ProcessOutputQueue(object sender, EventArgs e)
        {
            if (_isClosed || ResourceManager.IsShuttingDown) return;

            lock (_uiUpdateLock)
            {
                try
                {
                    bool hasOutput = false;
                    var outputBatch = new StringBuilder(2048);
                    
                    // 批量处理普通输出
                    while (_outputQueue.TryDequeue(out string output) && outputBatch.Length < 10000)
                    {
                        outputBatch.Append(output);
                        hasOutput = true;
                    }

                    // 批量处理错误输出
                    while (_errorQueue.TryDequeue(out string error) && outputBatch.Length < 10000)
                    {
                        outputBatch.Append($"[错误] {error}");
                        hasOutput = true;
                    }

                    if (hasOutput)
                    {
                        UpdateOutputUI(outputBatch.ToString());
                    }
                }
                catch (Exception)
                {
                    // 忽略批量更新异常
                }
            }
        }

        /// <summary>
        /// 更新输出UI（优化版本）
        /// </summary>
        private void UpdateOutputUI(string text)
        {
            if (string.IsNullOrEmpty(text) || _isClosed || ResourceManager.IsShuttingDown) return;

            try
            {
                _outputBuilder.Append(text);
                
                // 检查并优化缓冲区大小
                if (_outputBuilder.Length > MaxOutputLength)
                {
                    OptimizeOutputBuffer();
                }

                // 更新UI文本（减少频率）
                OutputBox.Text = _outputBuilder.ToString();
                
                // 执行自动滚动
                PerformAutoScroll();
            }
            catch (Exception)
            {
                // 忽略UI更新异常
            }
        }

        /// <summary>
        /// 添加普通输出文本（队列方式）
        /// </summary>
        private void AppendOutput(string text)
        {
            if (string.IsNullOrEmpty(text) || _isCleaningUp || _isClosed || ResourceManager.IsShuttingDown) return;
            
            _outputQueue.Enqueue(text);
        }

        /// <summary>
        /// 添加错误输出文本（队列方式）
        /// </summary>
        private void AppendError(string text)
        {
            if (string.IsNullOrEmpty(text) || _isCleaningUp || _isClosed || ResourceManager.IsShuttingDown) return;
            
            _errorQueue.Enqueue(text);
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

        #region 滚动控制逻辑（优化版本）

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
                
                // 检查用户是否滚动到了底部
                if (IsScrolledToBottom())
                {
                    // 用户滚动到底部，重新启用自动滚动
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
            
            const double tolerance = 2.0; // 增加容错范围
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
                // 使用BeginInvoke降低优先级，避免阻塞UI
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
                }, DispatcherPriority.Background);
            }
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
            
            _executionTask = ExecuteScript(_cancellationTokenSource.Token);
            
            try
            {
                await _executionTask;
                _scriptCompleted = true;
                UpdateButtonText();
            }
            catch (OperationCanceledException)
            {
                AppendOutput("\n脚本执行已被用户取消\n");
                UpdateStatus("状态：已取消");
                _scriptCompleted = true;
                UpdateButtonText();
            }
            catch (Exception ex)
            {
                AppendError($"\n执行任务异常: {ex.Message}\n");
                UpdateStatus("状态：执行异常");
                _scriptCompleted = true;
                UpdateButtonText();
            }
            finally
            {
                // 执行一次性内存清理，不显示清理信息
                if (!_isClosed && !ResourceManager.IsShuttingDown)
                {
                    await PerformSilentMemoryCleanup();
                }
            }
        }

        /// <summary>
        /// 执行脚本的核心方法
        /// </summary>
        private async Task ExecuteScript(CancellationToken cancellationToken)
        {
            if (_isRunning) return;
            
            _isRunning = true;
            UpdateStatus("状态：正在执行...");
            UpdateButtonText();
            ClearOutput();
            
            // 保存原始控制台输出流
            var originalOut = Console.Out;
            var originalError = Console.Error;
            
            ScriptState<object> scriptState = null;
            
            try
            {
                // 设置批量输出重定向
                SetupConsoleRedirection();
                
                // 输出脚本执行信息
                ShowScriptExecutionInfo();
                
                // 检查取消请求
                cancellationToken.ThrowIfCancellationRequested();
                
                // 编译并执行脚本
                var result = await CompileAndExecuteScript(cancellationToken);
                
                // 显示执行结果
                ShowExecutionResult(result);
                UpdateStatus("状态：执行成功");
            }
            catch (OperationCanceledException)
            {
                throw; // 重新抛出取消异常
            }
            catch (CompilationErrorException ex)
            {
                HandleCompilationError(ex);
            }
            catch (Exception ex)
            {
                HandleRuntimeError(ex);
            }
            finally
            {
                // 恢复控制台输出流
                Console.SetOut(originalOut);
                Console.SetError(originalError);
                
                // 清理资源
                CleanupConsoleRedirection();
                scriptState = null;
                _isRunning = false;
                UpdateButtonText();
            }
        }

        /// <summary>
        /// 设置控制台输出重定向（批量处理版本）
        /// </summary>
        private void SetupConsoleRedirection()
        {
            _realTimeOut = new BatchedTextWriter(text => AppendOutput(text));
            _realTimeError = new BatchedTextWriter(text => AppendError(text));
            
            // 注册到资源管理器
            ResourceManager.RegisterResource(_realTimeOut);
            ResourceManager.RegisterResource(_realTimeError);
            
            Console.SetOut(_realTimeOut);
            Console.SetError(_realTimeError);
        }

        /// <summary>
        /// 清理控制台输出重定向
        /// </summary>
        private void CleanupConsoleRedirection()
        {
            ResourceManager.SafeExecute(() =>
            {
                _realTimeOut?.Flush();
                _realTimeOut?.Dispose();
                _realTimeError?.Flush();
                _realTimeError?.Dispose();
                _realTimeOut = null;
                _realTimeError = null;
            }, "清理控制台重定向");
        }

        /// <summary>
        /// 显示脚本执行开始信息
        /// </summary>
        private void ShowScriptExecutionInfo()
        {
            AppendOutput($"开始执行脚本...\n");
            AppendOutput($"代码长度: {_code.Length} 字符\n");
            AppendOutput($"执行时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
            AppendOutput($"提示：在脚本中使用 CancellationToken.ThrowIfCancellationRequested() 来支持取消\n");
            AppendOutput(new string('-', 50) + "\n");
        }

        /// <summary>
        /// 编译并执行脚本（简化版本）
        /// </summary>
        private async Task<object> CompileAndExecuteScript(CancellationToken cancellationToken)
        {
            // 创建脚本选项，添加必要的引用和导入
            var options = ScriptOptions.Default
                .WithReferences(
                    typeof(Console).Assembly,
                    typeof(System.Linq.Enumerable).Assembly,
                    typeof(CancellationToken).Assembly,
                    typeof(Task).Assembly)
                .WithImports(
                    "System",
                    "System.Linq",
                    "System.Collections.Generic",
                    "System.Threading.Tasks",
                    "System.Threading");
            
            // 创建脚本执行环境
            var globals = new ScriptGlobals { CancellationToken = cancellationToken };
            
            // 编译脚本（如果尚未编译）
            _compiledScript ??= CSharpScript.Create(_code, options, typeof(ScriptGlobals));
            
            // 在独立任务中执行脚本
            var result = await Task.Run(async () =>
            {
                var scriptState = await _compiledScript.RunAsync(globals, cancellationToken);
                return scriptState.ReturnValue;
            }, cancellationToken);
            
            return result;
        }

        /// <summary>
        /// 显示脚本执行结果
        /// </summary>
        private void ShowExecutionResult(object result)
        {
            if (result != null)
            {
                AppendOutput($"\n返回值: {result} (类型: {result.GetType().Name})\n");
            }
            else
            {
                AppendOutput($"\n脚本执行完成，无返回值\n");
            }
            
            AppendOutput(new string('-', 50) + "\n");
            AppendOutput($"执行完成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
        }

        private void HandleCompilationError(CompilationErrorException ex)
        {
            AppendError($"\n编译错误:\n");
            foreach (var diagnostic in ex.Diagnostics)
            {
                AppendError($"行 {diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1}: {diagnostic.GetMessage()}\n");
            }
            UpdateStatus("状态：编译失败");
        }

        private void HandleRuntimeError(Exception ex)
        {
            AppendError($"\n运行时错误:\n{ex.GetType().Name}: {ex.Message}\n");
            if (ex.StackTrace != null)
            {
                AppendError($"堆栈跟踪:\n{ex.StackTrace}\n");
            }
            UpdateStatus("状态：执行失败");
        }

        #endregion

        #region 其他保持不变的方法

        public class ScriptGlobals
        {
            public CancellationToken CancellationToken { get; set; }
        }

        private void UpdateStatus(string status)
        {
            if (_isClosed || ResourceManager.IsShuttingDown) return;
            
            Dispatcher.BeginInvoke(() => 
            {
                if (!_isClosed && !ResourceManager.IsShuttingDown && StatusBlock != null)
                {
                    StatusBlock.Text = status;
                }
            }, DispatcherPriority.Background);
        }

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
                    
                    // 清空队列
                    while (_outputQueue.TryDequeue(out _)) { }
                    while (_errorQueue.TryDequeue(out _)) { }
                }
            }, DispatcherPriority.Background);
        }

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

        private async Task CleanupResources()
        {
            if (_isCleaningUp || ResourceManager.IsShuttingDown) return;
            _isCleaningUp = true;
            
            try
            {
                // 停止输出更新定时器
                _outputUpdateTimer?.Stop();
                
                // 移除事件监听
                if (_outputScrollViewer != null)
                {
                    _outputScrollViewer.ScrollChanged -= OnScrollChanged;
                    _outputScrollViewer = null;
                }
                
                // 清理控制台重定向
                CleanupConsoleRedirection();
                
                // 释放取消令牌源
                ResourceManager.SafeExecute(() =>
                {
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                }, "释放取消令牌源");
                
                // 清理编译脚本缓存
                _compiledScript = null;
                
                // 清理输出缓冲区
                ResourceManager.SafeExecute(() =>
                {
                    _outputBuilder?.Clear();
                    _outputBuilder = null;
                    
                    // 清空队列
                    while (_outputQueue.TryDequeue(out _)) { }
                    while (_errorQueue.TryDequeue(out _)) { }
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

        // 保持所有现有的事件处理器不变...
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

        private async Task CleanupAndClose()
        {
            _isClosed = true;
            await CleanupResources();
            Close();
        }

        private async Task ForceStopScript()
        {
            UpdateStatus("状态：正在强制停止...");
            AppendOutput("\n正在强制停止脚本执行...\n");
            
            _cancellationTokenSource?.Cancel();
            
            try
            {
                await Task.WhenAny(_executionTask, Task.Delay(1000));
            }
            catch (Exception ex)
            {
                AppendError($"停止脚本时发生异常: {ex.Message}\n");
            }
            
            if (_isRunning)
            {
                _isRunning = false;
                AppendOutput("\n脚本已强制停止\n");
                UpdateStatus("状态：已强制停止");
                _scriptCompleted = true;
                UpdateButtonText();
            }
            
            await CleanupResources();
        }

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
