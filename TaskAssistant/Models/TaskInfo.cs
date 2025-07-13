using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskAssistant.Models
{
    /// <summary>
    /// ��?�H��?�u�ҫ�?
    /// �Τ_�s?��?������H���A�]�A��?�t�m�B??��?��
    /// </summary>
    [Table("Tasks")]
    public class TaskInfo
    {
        #region �D??��

        /// <summary>
        /// ��?�ߤ@??��
        /// ?�u?�D?�A��?�ͦ�
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        #endregion

        #region �򥻫H��?��

        /// <summary>
        /// ��?�W?
        /// </summary>
        [Required(ErrorMessage = "��?�W?����?��")]
        [MaxLength(200)]
        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// ��?�y�z
        /// </summary>
        [MaxLength(1000)]
        [Column("Description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// ��??��
        /// </summary>
        [MaxLength(100)]
        [Column("TaskType")]
        public string TaskType { get; set; } = string.Empty;

        /// <summary>
        /// ��???
        /// ��?��B?�椤�B�w�����B�w?���B��?��
        /// </summary>
        [MaxLength(50)]
        [Column("Status")]
        public string Status { get; set; } = "��?��";

        #endregion

        #region ???��

        /// <summary>
        /// ??��?��ID
        /// �~?�A���VScripts��
        /// </summary>
        [Column("ScriptId")]
        public int? ScriptId { get; set; }

        /// <summary>
        /// ??��?���H��
        /// ?��?��
        /// </summary>
        [ForeignKey("ScriptId")]
        public virtual ScriptInfo? Script { get; set; }

        #endregion

        #region ?��t�m

        /// <summary>
        /// ��?�t�m
        /// JSON�榡�s?����?�t�m�H��
        /// </summary>
        [Column("Configuration", TypeName = "TEXT")]
        public string Configuration { get; set; } = "{}";

        /// <summary>
        /// ɬ��?
        /// ?�ȶV�pɬ��?�V��
        /// </summary>
        [Column("Priority")]
        public int Priority { get; set; } = 5;

        /// <summary>
        /// �O�_?��
        /// </summary>
        [Column("IsEnabled")]
        public bool IsEnabled { get; set; } = true;

        #endregion

        #region ???��

        /// <summary>
        /// ?��??
        /// </summary>
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// �̦Z�ק�??
        /// </summary>
        [Column("LastModified")]
        public DateTime LastModified { get; set; } = DateTime.Now;

        /// <summary>
        /// ?�E?��??
        /// </summary>
        [Column("ScheduledTime")]
        public DateTime? ScheduledTime { get; set; }

        /// <summary>
        /// ?�l?��??
        /// </summary>
        [Column("StartedAt")]
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// ����??
        /// </summary>
        [Column("CompletedAt")]
        public DateTime? CompletedAt { get; set; }

        #endregion

        #region ??�H��

        /// <summary>
        /// ?�榸?
        /// </summary>
        [Column("ExecutionCount")]
        public int ExecutionCount { get; set; } = 0;

        /// <summary>
        /// ���\��?
        /// </summary>
        [Column("SuccessCount")]
        public int SuccessCount { get; set; } = 0;

        /// <summary>
        /// ��?��?
        /// </summary>
        [Column("FailureCount")]
        public int FailureCount { get; set; } = 0;

        /// <summary>
        /// �̦Z?��?�G
        /// </summary>
        [Column("LastExecutionResult", TypeName = "TEXT")]
        public string? LastExecutionResult { get; set; }

        /// <summary>
        /// �̦Z??�H��
        /// </summary>
        [Column("LastError", TypeName = "TEXT")]
        public string? LastError { get; set; }

        #endregion
    }
}