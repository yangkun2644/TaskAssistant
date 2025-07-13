namespace TaskAssistant.Models
{
    /// <summary>
    /// 脚本模板数据模型类
    /// 用于存储预定义的脚本示例，帮助用户快速开始编写脚本
    /// 提供常用的代码模板和最佳实践示例
    /// </summary>
    public class ScriptTemplate
    {
        #region 属性

        /// <summary>
        /// 模板名称
        /// 模板的显示名称，用于在下拉框或列表中展示
        /// 应该简洁明了地描述模板的用途
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 模板代码内容
        /// 预定义的 C# 脚本代码，包含完整的可执行示例
        /// 通常包含注释说明和最佳实践代码
        /// </summary>
        public string Code { get; set; } = string.Empty;

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// 创建一个空的脚本模板实例
        /// </summary>
        public ScriptTemplate() 
        { 
        }
        
        /// <summary>
        /// 带参数的构造函数
        /// 使用指定的名称和代码创建脚本模板实例
        /// </summary>
        /// <param name="name">模板名称</param>
        /// <param name="code">模板代码内容</param>
        public ScriptTemplate(string name, string code)
        {
            Name = name;
            Code = code;
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 重写 ToString 方法
        /// 返回模板名称，用于在 UI 控件中正确显示
        /// 特别是在 ComboBox 等控件中绑定时使用
        /// </summary>
        /// <returns>模板名称字符串</returns>
        public override string ToString() => Name;

        #endregion

        #region 扩展功能预留

        // 未来可以添加更多属性和方法，例如：
        // - 模板分类 (string Category)
        // - 模板描述 (string Description)
        // - 难度级别 (TemplateLevel Level)
        // - 模板标签 (List<string> Tags)
        // - 创建时间 (DateTime CreatedAt)
        // - 使用次数 (int UsageCount)
        // - 模板图标 (string IconPath)
        // - 是否为系统内置模板 (bool IsBuiltIn)
        // - 模板作者 (string Author)
        // - 模板版本 (string Version)
        
        // 可能的方法：
        // - 克隆模板 (Clone())
        // - 验证模板代码 (ValidateCode())
        // - 获取模板变量 (GetVariables())
        // - 应用参数替换 (ApplyParameters())

        #endregion
    }
}