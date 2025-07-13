using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskAssistant.Models
{
    /// <summary>
    /// 任?信息?据模型?
    /// 用于存?任?的完整信息，包括任?配置、??跟?等
    /// </summary>
    [Table("Tasks")]
    public class TaskInfo
    {
        #region 主??性

        /// <summary>
        /// 任?唯一??符
        /// ?据?主?，自?生成
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        #endregion

        #region 基本信息?性

        /// <summary>
        /// 任?名?
        /// </summary>
        [Required(ErrorMessage = "任?名?不能?空")]
        [MaxLength(200)]
        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 任?描述
        /// </summary>
        [MaxLength(1000)]
        [Column("Description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 任??型
        /// </summary>
        [MaxLength(100)]
        [Column("TaskType")]
        public string TaskType { get; set; } = string.Empty;

        /// <summary>
        /// 任???
        /// 待?行、?行中、已完成、已?停、失?等
        /// </summary>
        [MaxLength(50)]
        [Column("Status")]
        public string Status { get; set; } = "待?行";

        #endregion

        #region ???性

        /// <summary>
        /// ??的?本ID
        /// 外?，指向Scripts表
        /// </summary>
        [Column("ScriptId")]
        public int? ScriptId { get; set; }

        /// <summary>
        /// ??的?本信息
        /// ?航?性
        /// </summary>
        [ForeignKey("ScriptId")]
        public virtual ScriptInfo? Script { get; set; }

        #endregion

        #region ?行配置

        /// <summary>
        /// 任?配置
        /// JSON格式存?的任?配置信息
        /// </summary>
        [Column("Configuration", TypeName = "TEXT")]
        public string Configuration { get; set; } = "{}";

        /// <summary>
        /// 优先?
        /// ?值越小优先?越高
        /// </summary>
        [Column("Priority")]
        public int Priority { get; set; } = 5;

        /// <summary>
        /// 是否?用
        /// </summary>
        [Column("IsEnabled")]
        public bool IsEnabled { get; set; } = true;

        #endregion

        #region ???性

        /// <summary>
        /// ?建??
        /// </summary>
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后修改??
        /// </summary>
        [Column("LastModified")]
        public DateTime LastModified { get; set; } = DateTime.Now;

        /// <summary>
        /// ?划?行??
        /// </summary>
        [Column("ScheduledTime")]
        public DateTime? ScheduledTime { get; set; }

        /// <summary>
        /// ?始?行??
        /// </summary>
        [Column("StartedAt")]
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// 完成??
        /// </summary>
        [Column("CompletedAt")]
        public DateTime? CompletedAt { get; set; }

        #endregion

        #region ??信息

        /// <summary>
        /// ?行次?
        /// </summary>
        [Column("ExecutionCount")]
        public int ExecutionCount { get; set; } = 0;

        /// <summary>
        /// 成功次?
        /// </summary>
        [Column("SuccessCount")]
        public int SuccessCount { get; set; } = 0;

        /// <summary>
        /// 失?次?
        /// </summary>
        [Column("FailureCount")]
        public int FailureCount { get; set; } = 0;

        /// <summary>
        /// 最后?行?果
        /// </summary>
        [Column("LastExecutionResult", TypeName = "TEXT")]
        public string? LastExecutionResult { get; set; }

        /// <summary>
        /// 最后??信息
        /// </summary>
        [Column("LastError", TypeName = "TEXT")]
        public string? LastError { get; set; }

        #endregion
    }
}