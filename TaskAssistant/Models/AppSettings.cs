using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskAssistant.Models
{
    /// <summary>
    /// ?�ε{��?�m�ҫ�
    /// �Τ_�O�s��?��?�ʤưt�m�A�p�`�ε{�Ƕ��ޥΡBNuGet�]��
    /// </summary>
    [Table("AppSettings")]
    public class AppSettings
    {
        /// <summary>
        /// ?�m?ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// ?�m?�W
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// ?�m�ȡ]JSON�榡�^
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// ?�m�y�z
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// ?��??
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// �̦Z�ק�??
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// ?���ޥΰt�m
    /// </summary>
    public class ScriptReferenceSettings
    {
        /// <summary>
        /// �`�ε{�Ƕ��ޥΦC��
        /// </summary>
        public List<AssemblyReference> CommonAssemblies { get; set; } = new();

        /// <summary>
        /// �`��NuGet�]�ޥΦC��
        /// </summary>
        public List<NuGetReference> CommonNuGetPackages { get; set; } = new();

        /// <summary>
        /// �O�_��?�[?�Ҧ��i�ε{�Ƕ�
        /// </summary>
        public bool AutoLoadAllAssemblies { get; set; } = true;

        /// <summary>
        /// �O�_?�δ���ޥΫ�?
        /// </summary>
        public bool EnableSmartSuggestions { get; set; } = true;

        /// <summary>
        /// �ư����{�Ƕ��Ҧ��C��
        /// </summary>
        public List<string> ExcludedAssemblyPatterns { get; set; } = new();
    }

    /// <summary>
    /// �{�Ƕ��ޥΰt�m
    /// </summary>
    public class AssemblyReference
    {
        /// <summary>
        /// �{�Ƕ��W?
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// �{�Ƕ���?�]�i?�A�p�G?��?�q?�W?�[?�^
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// �O�_?��
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// �y�z
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// NuGet�]�ޥΰt�m
    /// </summary>
    public class NuGetReference
    {
        /// <summary>
        /// �]ID
        /// </summary>
        public string PackageId { get; set; } = string.Empty;

        /// <summary>
        /// ����?�]�i�H�O���^�����Ϊ����S?�^
        /// </summary>
        public string Version { get; set; } = "*";

        /// <summary>
        /// �O�_?��
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// �y�z
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// �O�_?�[?�]�b?��?��e?���U?�^
        /// </summary>
        public bool PreLoad { get; set; } = false;
    }
}