using System;
using System.Windows;

namespace TaskAssistant.View
{
    /// <summary>
    /// 对话框类型枚举
    /// </summary>
    public enum ThemedDialogType
    {
        /// <summary>信息对话框</summary>
        Information,
        /// <summary>警告对话框</summary>
        Warning,
        /// <summary>错误对话框</summary>
        Error,
        /// <summary>确认对话框</summary>
        Confirmation,
        /// <summary>问题对话框</summary>
        Question
    }

    /// <summary>
    /// 对话框按钮组合枚举
    /// </summary>
    public enum ThemedDialogButton
    {
        /// <summary>只有确定按钮</summary>
        OK,
        /// <summary>确定和取消按钮</summary>
        OKCancel,
        /// <summary>是和否按钮</summary>
        YesNo,
        /// <summary>是、否和取消按钮</summary>
        YesNoCancel
    }

    /// <summary>
    /// 对话框结果枚举
    /// </summary>
    public enum ThemedDialogResult
    {
        /// <summary>无结果</summary>
        None,
        /// <summary>确定</summary>
        OK,
        /// <summary>取消</summary>
        Cancel,
        /// <summary>是</summary>
        Yes,
        /// <summary>否</summary>
        No
    }

    /// <summary>
    /// 主题化对话框类
    /// 提供统一样式的消息对话框，替代系统默认的 MessageBox
    /// 支持自定义标题、消息内容、对话框类型、按钮组合和父窗口设置
    /// </summary>
    public partial class ThemedDialog : Window
    {
        #region 私有字段

        /// <summary>
        /// 对话框结果
        /// </summary>
        private ThemedDialogResult _result = ThemedDialogResult.None;

        #endregion

        #region 属性

        /// <summary>
        /// 获取对话框结果
        /// </summary>
        public ThemedDialogResult Result => _result;

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// 创建一个空的主题化对话框实例
        /// </summary>
        public ThemedDialog()
        {
            // 初始化 XAML 中定义的组件
            InitializeComponent();
        }

        /// <summary>
        /// 带参数的构造函数
        /// 创建包含指定标题和消息的主题化对话框
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="message">对话框消息内容</param>
        /// <param name="dialogType">对话框类型</param>
        /// <param name="buttons">按钮组合</param>
        public ThemedDialog(string title, string message, ThemedDialogType dialogType = ThemedDialogType.Information, ThemedDialogButton buttons = ThemedDialogButton.OK)
        {
            // 初始化 XAML 中定义的组件
            InitializeComponent();
            
            // 设置对话框内容
            SetupDialog(title, message, dialogType, buttons);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 设置对话框内容和样式
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息</param>
        /// <param name="dialogType">对话框类型</param>
        /// <param name="buttons">按钮组合</param>
        private void SetupDialog(string title, string message, ThemedDialogType dialogType, ThemedDialogButton buttons)
        {
            // 设置标题和消息
            TitleBlock.Text = title;
            MessageBlock.Text = message;
            
            // 根据对话框类型设置图标
            SetIcon(dialogType);
            
            // 根据按钮组合设置按钮可见性和文本
            SetupButtons(buttons);
            
            // 设置键盘焦点
            SetInitialFocus(buttons);
        }

        /// <summary>
        /// 设置初始键盘焦点
        /// </summary>
        /// <param name="buttons">按钮组合</param>
        private void SetInitialFocus(ThemedDialogButton buttons)
        {
            // 根据按钮类型设置默认焦点
            switch (buttons)
            {
                case ThemedDialogButton.OK:
                    OkButton.Focus();
                    break;
                case ThemedDialogButton.OKCancel:
                    OkButton.Focus();
                    break;
                case ThemedDialogButton.YesNo:
                case ThemedDialogButton.YesNoCancel:
                    OkButton.Focus(); // Yes按钮
                    break;
            }
        }

        /// <summary>
        /// 根据对话框类型设置图标
        /// </summary>
        /// <param name="dialogType">对话框类型</param>
        private void SetIcon(ThemedDialogType dialogType)
        {
            switch (dialogType)
            {
                case ThemedDialogType.Information:
                    IconBlock.Text = "ℹ"; // 信息图标
                    IconBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 168, 255));
                    break;
                case ThemedDialogType.Warning:
                    IconBlock.Text = "⚠"; // 警告图标
                    IconBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7));
                    break;
                case ThemedDialogType.Error:
                    IconBlock.Text = "✖"; // 错误图标
                    IconBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 107, 107));
                    break;
                case ThemedDialogType.Confirmation:
                    IconBlock.Text = "✓"; // 确认图标
                    IconBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));
                    break;
                case ThemedDialogType.Question:
                    IconBlock.Text = "?"; // 问号图标
                    IconBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 39, 176));
                    break;
            }
        }

        /// <summary>
        /// 设置按钮组合
        /// </summary>
        /// <param name="buttons">按钮组合</param>
        private void SetupButtons(ThemedDialogButton buttons)
        {
            // 隐藏所有按钮
            CancelButton.Visibility = Visibility.Collapsed;
            NoButton.Visibility = Visibility.Collapsed;
    
            switch (buttons)
            {
                case ThemedDialogButton.OK:
                    // 只显示确定按钮
                    OkTextBlock.Text = "确定";
                    break;
                    
                case ThemedDialogButton.OKCancel:
                    // 显示确定和取消按钮
                    CancelButton.Visibility = Visibility.Visible;
                    OkTextBlock.Text = "确定";
                    break;
                    
                case ThemedDialogButton.YesNo:
                    // 显示是和否按钮
                    NoButton.Visibility = Visibility.Visible;
                    OkTextBlock.Text = "是";
                    break;
                    
                case ThemedDialogButton.YesNoCancel:
                    // 显示是、否和取消按钮
                    CancelButton.Visibility = Visibility.Visible;
                    NoButton.Visibility = Visibility.Visible;
                    OkTextBlock.Text = "是";
                    break;
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 确定/是按钮点击事件处理方法
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">按钮点击事件参数</param>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // 根据按钮文本设置结果
            _result = OkTextBlock.Text == "是" ? ThemedDialogResult.Yes : ThemedDialogResult.OK;
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// 取消按钮点击事件处理方法
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">按钮点击事件参数</param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _result = ThemedDialogResult.Cancel;
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 否按钮点击事件处理方法
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">按钮点击事件参数</param>
        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            _result = ThemedDialogResult.No;
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 重写键盘事件处理
        /// </summary>
        /// <param name="e">键盘事件参数</param>
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Enter:
                    // Enter键触发确定/是按钮
                    OkButton_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                    
                case System.Windows.Input.Key.Escape:
                    // Esc键触发取消或关闭操作
                    if (CancelButton.Visibility == Visibility.Visible)
                    {
                        CancelButton_Click(this, new RoutedEventArgs());
                    }
                    else if (NoButton.Visibility == Visibility.Visible)
                    {
                        NoButton_Click(this, new RoutedEventArgs());
                    }
                    else
                    {
                        // 如果只有确定按钮，ESC也关闭对话框
                        _result = ThemedDialogResult.Cancel;
                        DialogResult = false;
                        Close();
                    }
                    e.Handled = true;
                    break;
                    
                case System.Windows.Input.Key.Y:
                    // Y键快速选择是
                    if (OkTextBlock.Text == "是")
                    {
                        OkButton_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                    }
                    break;
                    
                case System.Windows.Input.Key.N:
                    // N键快速选择否
                    if (NoButton.Visibility == Visibility.Visible)
                    {
                        NoButton_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                    }
                    break;
            }
            
            base.OnKeyDown(e);
        }

        #endregion

        #region 静态方法

        /// <summary>
        /// 显示信息对话框
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息</param>
        /// <param name="owner">父窗口</param>
        /// <returns>对话框结果</returns>
        public static ThemedDialogResult ShowInformation(string title, string message, Window? owner = null)
        {
            return ShowDialog(title, message, ThemedDialogType.Information, ThemedDialogButton.OK, owner);
        }

        /// <summary>
        /// 显示警告对话框
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息</param>
        /// <param name="owner">父窗口</param>
        /// <returns>对话框结果</returns>
        public static ThemedDialogResult ShowWarning(string title, string message, Window? owner = null)
        {
            return ShowDialog(title, message, ThemedDialogType.Warning, ThemedDialogButton.OK, owner);
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息</param>
        /// <param name="owner">父窗口</param>
        /// <returns>对话框结果</returns>
        public static ThemedDialogResult ShowError(string title, string message, Window? owner = null)
        {
            return ShowDialog(title, message, ThemedDialogType.Error, ThemedDialogButton.OK, owner);
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息</param>
        /// <param name="owner">父窗口</param>
        /// <returns>对话框结果</returns>
        public static ThemedDialogResult ShowConfirmation(string title, string message, Window? owner = null)
        {
            return ShowDialog(title, message, ThemedDialogType.Confirmation, ThemedDialogButton.YesNo, owner);
        }

        /// <summary>
        /// 显示问题对话框
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息</param>
        /// <param name="buttons">按钮组合</param>
        /// <param name="owner">父窗口</param>
        /// <returns>对话框结果</returns>
        public static ThemedDialogResult ShowQuestion(string title, string message, ThemedDialogButton buttons = ThemedDialogButton.YesNo, Window? owner = null)
        {
            return ShowDialog(title, message, ThemedDialogType.Question, buttons, owner);
        }

        /// <summary>
        /// 显示主题化对话框的通用方法
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息</param>
        /// <param name="dialogType">对话框类型</param>
        /// <param name="buttons">按钮组合</param>
        /// <param name="owner">父窗口</param>
        /// <returns>对话框结果</returns>
        public static ThemedDialogResult ShowDialog(string title, string message, ThemedDialogType dialogType = ThemedDialogType.Information, ThemedDialogButton buttons = ThemedDialogButton.OK, Window? owner = null)
        {
            var dialog = new ThemedDialog(title, message, dialogType, buttons);
            
            if (owner != null)
                dialog.Owner = owner;
            
            dialog.ShowDialog();
            return dialog.Result;
        }

        /// <summary>
        /// 显示主题化对话框的静态方法（兼容旧版本）
        /// 提供类似 MessageBox.Show 的便捷调用方式
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="message">对话框消息内容</param>
        /// <param name="owner">父窗口实例，可选参数</param>
        public static void Show(string title, string message, Window? owner = null)
        {
            ShowInformation(title, message, owner);
        }

        #endregion

        #region 使用示例和文档

        /* 
        ## ThemedDialog 使用示例

        ### 1. 信息对话框    ```csharp
    ThemedDialog.ShowInformation("成功", "操作已成功完成！", ownerWindow);```
    ### 2. 警告对话框    ```csharp
ThemedDialog.ShowWarning("警告", "此操作可能有风险，请谨慎操作。", ownerWindow);    ThemedDialog.ShowError("错误", "操作失败：文件不存在。", ownerWindow);```
    ### 4. 确认对话框（删除确认） ```csharp
var result = ThemedDialog.ShowConfirmation(
    "确认删除", 
    "确定要删除选中的项目吗？\n\n此操作不可撤销。", 
    ownerWindow);
    
if (result == ThemedDialogResult.Yes)
{
    // 执行删除操作
    }       ### 5. 问题对话框（多种按钮组合）    ```csharp
    var result = ThemedDialog.ShowQuestion(
        "保存更改", 
        "检测到未保存的更改，是否要保存？", 
        ThemedDialogButton.YesNoCancel,
        ownerWindow);
        
    switch (result)
    {
        case ThemedDialogResult.Yes:
            // 保存并继续
            break;
        case ThemedDialogResult.No:
            // 不保存，直接继续
            break;
        case ThemedDialogResult.Cancel:
            // 取消操作
                break;
        }```
    ### 6. 自定义对话框 ```csharp
var result = ThemedDialog.ShowDialog(
    "自定义标题",
    "自定义消息内容",
    ThemedDialogType.Question,
    ThemedDialogButton.OKCancel,
        ownerWindow);        - **Enter**: 确认/是
        - **Esc**: 取消/否/关闭
        - **Y**: 快速选择"是"（在是/否对话框中）
        - **N**: 快速选择"否"（在是/否对话框中）

        ## 对话框类型和图标
        - **Information**: 蓝色信息图标
        - **Warning**: 黄色警告图标
        - **Error**: 红色错误图标
        - **Confirmation**: 绿色确认图标
        - **Question**: 紫色问号图标

        ## 按钮组合
        - **OK**: 只有确定按钮
        - **OKCancel**: 确定和取消按钮
        - **YesNo**: 是和否按钮
        - **YesNoCancel**: 是、否和取消按钮
        */
        #endregion
    }
}
