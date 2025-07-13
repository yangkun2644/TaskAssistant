using System;
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
            // 延迟初始化，确保控件已经加载完成和 ViewModel 完全初始化
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsLoaded && CodeEditor != null && ViewModel != null)
                {
                    InitializeCodeEditor();
                    
                    // 如果是新建模式且没有选中模板，设置默认模板
                    if (ViewModel.SelectedTemplate == null && ViewModel.ScriptTemplates.Any())
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
            if (CodeEditor != null)
            {
                InitializeCodeEditor();
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

            // 设置代码编辑器的初始内容
            if (!string.IsNullOrWhiteSpace(ViewModel.Code))
            {
                // 已有代码内容：可能是编辑模式或者模板已选择
                CodeEditor.Text = ViewModel.Code;
            }
            else if (ViewModel.SelectedTemplate != null)
            {
                // 新建模式且选择了模板：使用模板代码
                CodeEditor.Text = ViewModel.SelectedTemplate.Code;
                // 同步更新 ViewModel
                ViewModel.Code = ViewModel.SelectedTemplate.Code;
            }
            else
            {
                // 新建模式且无模板：清空编辑器
                CodeEditor.Text = string.Empty;
            }

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
                // 更新代码编辑器内容为新选中模板的代码
                // ViewModel 中已经通过 OnSelectedTemplateChanged 更新了 Code 属性
                CodeEditor.Text = ViewModel.Code;
                
                // 更新代码编辑器边框状态
                UpdateCodeEditorBorder();
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
