using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskAssistant.Models
{
    /// <summary>
    /// 脚本信息数据模型类
    /// 用于存储脚本的完整信息，包括元数据和代码内容
    /// 支持数据验证，确保脚本信息的完整性和有效性
    /// </summary>
    [Table("Scripts")]
    public class ScriptInfo
    {
        #region 主键属性

        /// <summary>
        /// 脚本唯一标识符
        /// 数据库主键，自动生成
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        #endregion

        #region 基本信息属性

        /// <summary>
        /// 脚本名称
        /// 脚本的唯一标识名称，用于显示和检索
        /// 使用 Required 特性确保名称不能为空
        /// </summary>
        [Required(ErrorMessage = "脚本名称不能为空")]
        [MaxLength(200)]
        [Column("Name")]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 脚本描述
        /// 详细说明脚本的功能、用途和使用方法
        /// 可选字段，帮助用户理解脚本的作用
        /// </summary>
        [MaxLength(1000)]
        [Column("Description")]
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 脚本版本号
        /// 遵循语义化版本规范（Semantic Versioning）
        /// 格式：主版本号.次版本号.修订号，默认为 "1.0.0"
        /// </summary>
        [MaxLength(50)]
        [Column("Version")]
        public string Version { get; set; } = "1.0.0";
        
        /// <summary>
        /// 脚本作者
        /// 记录脚本的创建者或维护者信息
        /// 可选字段，用于版权和联系信息
        /// </summary>
        [MaxLength(100)]
        [Column("Author")]
        public string Author { get; set; } = string.Empty;

        #endregion

        #region 脚本内容属性

        /// <summary>
        /// 脚本代码内容
        /// 实际的 C# 脚本代码，支持完整的 C# 语法
        /// 使用 Required 特性确保代码内容不能为空
        /// 代码将通过 Microsoft.CodeAnalysis.CSharp.Scripting 执行
        /// </summary>
        [Required(ErrorMessage = "脚本代码不能为空")]
        [Column("Code", TypeName = "TEXT")]
        public string Code { get; set; } = string.Empty;

        #endregion

        #region 时间戳属性

        /// <summary>
        /// 创建时间
        /// 记录脚本首次创建的时间
        /// </summary>
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后修改时间
        /// 记录脚本最后一次修改的时间
        /// </summary>
        [Column("LastModified")]
        public DateTime LastModified { get; set; } = DateTime.Now;

        #endregion

        #region 扩展属性

        /// <summary>
        /// 脚本分类
        /// 用于对脚本进行分类管理
        /// </summary>
        [MaxLength(100)]
        [Column("Category")]
        public string Category { get; set; } = "默认";

        /// <summary>
        /// 脚本标签
        /// JSON格式存储的标签列表，用于搜索和过滤
        /// </summary>
        [Column("Tags", TypeName = "TEXT")]
        public string Tags { get; set; } = "[]";

        /// <summary>
        /// 是否启用
        /// 标记脚本是否可用
        /// </summary>
        [Column("IsEnabled")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 执行次数
        /// 记录脚本被执行的总次数
        /// </summary>
        [Column("ExecutionCount")]
        public int ExecutionCount { get; set; } = 0;

        /// <summary>
        /// 最后执行时间
        /// 记录脚本最后一次执行的时间
        /// </summary>
        [Column("LastExecuted")]
        public DateTime? LastExecuted { get; set; }

        #endregion
    }
}