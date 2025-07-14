using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TaskAssistant.ViewModels;

namespace TaskAssistant.View
{
    /// <summary>
    /// 脚本管理视图的交互逻辑类
    /// 该类负责处理脚本编辑界面的用户交互，包括代码编辑器的初始化、模板选择等功能
    /// 使用 MVVM 模式，大部分逻辑委托给 ScriptManageViewModel 处理
    /// 注意：现在所有按钮都通过 Command 绑定，只保留 AvalonEdit 必需的事件处理
    /// </summary>
    public partial class ScriptManageView : UserControl
    {
        #region 属性

        /// <summary>
        /// 获取当前视图绑定的脚本管理视图模型
        /// 通过 DataContext 获取，提供类型安全的访问方式
        /// </summary>
        public ScriptManageViewModel ViewModel => (ScriptManageViewModel)DataContext;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化脚本管理视图
        /// 设置用户控件的基本配置并注册加载事件
        /// </summary>
        public ScriptManageView()
        {
            // 初始化 XAML 中定义的组件
            InitializeComponent();
            
            // 注册窗口加载完成事件，用于延迟初始化代码编辑器
            Loaded += OnLoaded;
            
            // 注册 DataContext 变更事件，用于处理 ViewModel 更新
            DataContextChanged += OnDataContextChanged;
            
            // 注册卸载事件，清理资源
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// 处理用户控件卸载事件
        /// 清理事件订阅，避免内存泄漏
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // 清理ViewModel事件订阅
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
        }

        #endregion

        #region 事件处理方法

        /// <summary>
        /// 处理 DataContext 变更事件
        /// 当 ViewModel 更新时，重新初始化代码编辑器
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // 取消之前的订阅
            if (e.OldValue is ScriptManageViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            // 延迟初始化，确保控件已经加载完成和 ViewModel 完全初始化
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsLoaded && CodeEditor != null && ViewModel != null)
                {
                    // 订阅ViewModel属性变更事件
                    ViewModel.PropertyChanged += ViewModel_PropertyChanged;
                    
                    InitializeCodeEditor();
                    
                    // 只有在新建模式下（Code为空且没有选中模板）才设置默认模板
                    // 避免在编辑模式下覆盖从数据库加载的代码
                    if (string.IsNullOrEmpty(ViewModel.Code) && 
                        ViewModel.SelectedTemplate == null && 
                        ViewModel.ScriptTemplates.Any())
                    {
                        ViewModel.SelectedTemplate = ViewModel.ScriptTemplates.FirstOrDefault();
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 用户控件加载完成后的初始化操作
        /// 主要用于设置 AvalonEdit 代码编辑器的初始状态和事件绑定
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (CodeEditor != null && ViewModel != null)
            {
                // 订阅ViewModel属性变更事件
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
                InitializeCodeEditor();
            }
        }

        /// <summary>
        /// 处理ViewModel属性变更事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">属性变更事件参数</param>
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScriptManageViewModel.Code))
            {
                // 当Code属性变更时，更新代码编辑器内容
                UpdateCodeEditorFromViewModel();
            }
        }

        /// <summary>
        /// 初始化代码编辑器
        /// 设置初始内容并绑定事件
        /// </summary>
        private void InitializeCodeEditor()
        {
            if (CodeEditor == null || ViewModel == null) return;

            // 解除之前的事件绑定，避免在设置内容时触发更改事件
            CodeEditor.TextChanged -= CodeEditor_TextChanged;

            try
            {
                // 优先使用ViewModel中的Code（可能来自数据库）
                string codeToShow = ViewModel.Code;
                
                // 如果ViewModel中没有代码，且选择了模板，使用模板代码
                if (string.IsNullOrWhiteSpace(codeToShow) && ViewModel.SelectedTemplate != null)
                {
                    codeToShow = ViewModel.SelectedTemplate.Code;
                    // 同步更新ViewModel（但不通知PropertyChanged，避免循环）
                    ViewModel.UpdateCode(codeToShow);
                }
                
                // 设置代码编辑器内容
                CodeEditor.Text = codeToShow ?? string.Empty;
                
                System.Diagnostics.Debug.WriteLine($"初始化代码编辑器: {CodeEditor.Text.Length} 字符");
            }
            finally
            {
                // 重新绑定代码编辑器的文本更改事件
                CodeEditor.TextChanged += CodeEditor_TextChanged;

                // 初始化代码编辑器边框状态
                UpdateCodeEditorBorder();
                
                // 强制更新按钮状态
                if (ViewModel != null)
                {
                    ViewModel.ExecuteScriptCommand.NotifyCanExecuteChanged();
                    ViewModel.SaveScriptCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 代码编辑器文本变更事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void CodeEditor_TextChanged(object sender, EventArgs e)
        {
            // 当用户编辑代码时，实时更新 ViewModel 中的代码内容
            if (ViewModel != null && CodeEditor != null)
            {
                ViewModel.UpdateCode(CodeEditor.Text);
                
                // 同时更新代码编辑器边框的视觉状态
                UpdateCodeEditorBorder();
            }
        }

        /// <summary>
        /// 处理脚本模板下拉框选择变更事件
        /// 由于 AvalonEdit 控件不支持直接数据绑定，需要手动处理模板切换
        /// </summary>
        /// <param name="sender">事件发送者（模板下拉框）</param>
        /// <param name="e">选择变更事件参数</param>
        private void TemplateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 验证所有必需的控件都存在
            if (TemplateComboBox?.SelectedItem != null && CodeEditor != null && ViewModel != null)
            {
                // 用户选择了模板时才更新
                if (e.AddedItems.Count > 0 && ViewModel.SelectedTemplate != null)
                {
                    System.Diagnostics.Debug.WriteLine($"🎯 用户选择了模板: {ViewModel.SelectedTemplate.Name}");
                    
                    // 更新代码编辑器内容为新选中模板的代码
                    // ViewModel 中已经通过 OnSelectedTemplateChanged 更新了 Code 属性
                    CodeEditor.Text = ViewModel.Code;
                    
                    // 更新代码编辑器边框状态
                    UpdateCodeEditorBorder();
                }
                else if (e.RemovedItems.Count > 0 && ViewModel.SelectedTemplate == null)
                {
                    System.Diagnostics.Debug.WriteLine("🚫 模板选择已清除");
                }
            }
        }

        /// <summary>
        /// 更新代码编辑器边框的视觉状态
        /// 根据代码内容是否为空来设置边框颜色
        /// </summary>
        private void UpdateCodeEditorBorder()
        {
            if (CodeEditor?.Parent is Border border)
            {
                var isCodeEmpty = string.IsNullOrWhiteSpace(CodeEditor.Text);
                
                // 根据代码是否为空设置边框颜色
                border.BorderBrush = isCodeEmpty 
                    ? new SolidColorBrush(Color.FromRgb(255, 107, 107)) // #FF6B6B 红色
                    : new SolidColorBrush(Color.FromRgb(224, 224, 224)); // #e0e0e0 默认颜色
                    
                border.BorderThickness = new Thickness(isCodeEmpty ? 2 : 1);
            }
        }

        /// <summary>
        /// 从ViewModel更新代码编辑器内容
        /// 用于处理异步加载脚本数据的情况
        /// </summary>
        private void UpdateCodeEditorFromViewModel()
        {
            if (CodeEditor == null || ViewModel == null) return;

            // 暂时解除事件绑定，避免循环触发
            CodeEditor.TextChanged -= CodeEditor_TextChanged;

            try
            {
                System.Diagnostics.Debug.WriteLine($"更新代码编辑器 - ViewModel.Code长度: {ViewModel.Code?.Length ?? 0}");
                System.Diagnostics.Debug.WriteLine($"更新代码编辑器 - 当前编辑器内容长度: {CodeEditor.Text?.Length ?? 0}");
                
                // 更新代码编辑器内容
                if (CodeEditor.Text != ViewModel.Code)
                {
                    CodeEditor.Text = ViewModel.Code ?? string.Empty;
                    System.Diagnostics.Debug.WriteLine($"✅ 代码编辑器内容已更新: {CodeEditor.Text.Length} 字符");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("📝 代码编辑器内容无需更新（内容相同）");
                }
            }
            finally
            {
                // 重新绑定事件
                CodeEditor.TextChanged += CodeEditor_TextChanged;
                
                // 更新边框状态
                UpdateCodeEditorBorder();
            }
        }

        #endregion

        #region 说明注释

        // 注意：以下事件处理方法已被移除，因为按钮现在通过 Command 绑定：
        // - ExecuteScriptButton_Click：现在使用 ExecuteScriptCommand
        // - FullScreenButton_Click：现在使用 OpenFullScreenEditorCommand  
        // - SaveButton_Click：现在使用 SaveScriptCommand
        //
        // 这样做的好处：
        // 1. CanExecute 功能正常工作，按钮会根据条件自动启用/禁用
        // 2. 更好的 MVVM 架构分离
        // 3. 更容易进行单元测试
        // 4. 代码更加简洁和维护性更好

        #endregion
    }
}
