using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskAssistant.Models
{
    /// <summary>
    /// ?用程序?置模型
    /// 用于保存用?的?性化配置，如常用程序集引用、NuGet包等
    /// </summary>
    [Table("AppSettings")]
    public class AppSettings
    {
        /// <summary>
        /// ?置?ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// ?置?名
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// ?置值（JSON格式）
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// ?置描述
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// ?建??
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后修改??
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// ?本引用配置
    /// </summary>
    public class ScriptReferenceSettings
    {
        /// <summary>
        /// 常用程序集引用列表
        /// </summary>
        public List<AssemblyReference> CommonAssemblies { get; set; } = new();

        /// <summary>
        /// 常用NuGet包引用列表
        /// </summary>
        public List<NuGetReference> CommonNuGetPackages { get; set; } = new();

        /// <summary>
        /// 是否自?加?所有可用程序集
        /// </summary>
        public bool AutoLoadAllAssemblies { get; set; } = true;

        /// <summary>
        /// 是否?用智能引用建?
        /// </summary>
        public bool EnableSmartSuggestions { get; set; } = true;

        /// <summary>
        /// 排除的程序集模式列表
        /// </summary>
        public List<string> ExcludedAssemblyPatterns { get; set; } = new();
    }

    /// <summary>
    /// 程序集引用配置
    /// </summary>
    public class AssemblyReference
    {
        /// <summary>
        /// 程序集名?
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 程序集路?（可?，如果?空?通?名?加?）
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// 是否?用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// NuGet包引用配置
    /// </summary>
    public class NuGetReference
    {
        /// <summary>
        /// 包ID
        /// </summary>
        public string PackageId { get; set; } = string.Empty;

        /// <summary>
        /// 版本?（可以是具体版本或版本范?）
        /// </summary>
        public string Version { get; set; } = "*";

        /// <summary>
        /// 是否?用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 是否?加?（在?本?行前?先下?）
        /// </summary>
        public bool PreLoad { get; set; } = false;
    }
}