using System.Windows;

namespace TaskAssistant.View
{
    /// <summary>
    /// 全屏代码编辑器窗口
    /// 提供一个独立的全屏窗口用于编辑代码，支持字体大小调整、自动换行等功能
    /// 支持快捷键操作：Ctrl+S 保存，ESC 取消
    /// </summary>
    public partial class FullScreenCodeEditorWindow : Window
    {
        #region 属性

        /// <summary>
        /// 获取编辑器中的代码内容
        /// 只有在用户点击保存后才会更新此属性
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// 获取代码是否已保存的状态
        /// true 表示用户已保存代码，false 表示用户取消了编辑
        /// </summary>
        public bool IsSaved { get; private set; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化全屏代码编辑器窗口
        /// </summary>
        /// <param name="initialCode">初始代码内容，默认为空字符串</param>
        public FullScreenCodeEditorWindow(string initialCode = "")
        {
            // 初始化 XAML 中定义的组件
            InitializeComponent();
            
            // 设置代码编辑器的初始内容
            CodeEditor.Text = initialCode;
            
            // 初始化保存状态为未保存
            IsSaved = false;
            
            // 设置焦点到编辑器，方便用户立即开始编辑
            CodeEditor.Focus();
        }

        #endregion

        #region 按钮事件处理

        /// <summary>
        /// 处理保存按钮点击事件
        /// 保存当前编辑的代码并关闭窗口
        /// </summary>
        /// <param name="sender">事件发送者（保存按钮）</param>
        /// <param name="e">按钮点击事件参数</param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取编辑器中的最新代码内容
            Code = CodeEditor.Text;
            
            // 标记为已保存
            IsSaved = true;
            
            // 设置对话框结果为 true，表示用户确认了操作
            this.DialogResult = true;
            
            // 关闭窗口
            this.Close();
        }

        /// <summary>
        /// 处理取消按钮点击事件
        /// 放弃编辑并关闭窗口
        /// </summary>
        /// <param name="sender">事件发送者（取消按钮）</param>
        /// <param name="e">按钮点击事件参数</param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 标记为未保存
            IsSaved = false;
            
            // 设置对话框结果为 false，表示用户取消了操作
            this.DialogResult = false;
            
            // 关闭窗口
            this.Close();
        }

        #endregion

        #region 编辑器功能按钮事件

        /// <summary>
        /// 处理增大字体按钮点击事件
        /// 将代码编辑器的字体大小增加 2 个单位，最大不超过 24
        /// </summary>
        /// <param name="sender">事件发送者（增大字体按钮）</param>
        /// <param name="e">按钮点击事件参数</param>
        private void IncreaseFontSize_Click(object sender, RoutedEventArgs e)
        {
            // 检查当前字体大小是否小于最大限制
            if (CodeEditor.FontSize < 24)
            {
                // 增加字体大小
                CodeEditor.FontSize += 2;
            }
        }

        /// <summary>
        /// 处理减小字体按钮点击事件
        /// 将代码编辑器的字体大小减少 2 个单位，最小不低于 10
        /// </summary>
        /// <param name="sender">事件发送者（减小字体按钮）</param>
        /// <param name="e">按钮点击事件参数</param>
        private void DecreaseFontSize_Click(object sender, RoutedEventArgs e)
        {
            // 检查当前字体大小是否大于最小限制
            if (CodeEditor.FontSize > 10)
            {
                // 减少字体大小
                CodeEditor.FontSize -= 2;
            }
        }

        /// <summary>
        /// 处理切换自动换行按钮点击事件
        /// 在启用和禁用自动换行之间切换
        /// </summary>
        /// <param name="sender">事件发送者（自动换行切换按钮）</param>
        /// <param name="e">按钮点击事件参数</param>
        private void ToggleWordWrap_Click(object sender, RoutedEventArgs e)
        {
            // 切换自动换行状态
            CodeEditor.WordWrap = !CodeEditor.WordWrap;
        }

        #endregion

        #region 键盘快捷键处理

        /// <summary>
        /// 重写键盘按键事件处理方法
        /// 实现快捷键功能：ESC 取消，Ctrl+S 保存
        /// </summary>
        /// <param name="e">键盘事件参数</param>
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            // 检查是否按下 ESC 键
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                // 执行取消操作
                CancelButton_Click(this, new RoutedEventArgs());
            }
            // 检查是否按下 Ctrl+S 组合键
            else if (e.Key == System.Windows.Input.Key.S && 
                     e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                // 执行保存操作
                SaveButton_Click(this, new RoutedEventArgs());
                
                // 标记事件已处理，防止进一步传播
                e.Handled = true;
            }
            
            // 调用基类的键盘事件处理方法
            base.OnKeyDown(e);
        }

        #endregion
    }
}