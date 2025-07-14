using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskAssistant.Models
{
    /// <summary>
    /// ?本?行日志?据模型?
    /// 用于存??本?行的完整??，包括?出、??、?行??等信息
    /// 支持?本管理和任?管理中的?行日志查看
    /// </summary>
    [Table("ScriptExecutionLogs")]
    public class ScriptExecutionLog
    {
        #region 主??性

        /// <summary>
        /// ?行日志唯一??符
        /// ?据?主?，自?生成
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        #endregion

        #region ???性

        /// <summary>
        /// ??的?本ID（可?）
        /// ???本管理?行?使用
        /// </summary>
        [Column("ScriptId")]
        public int? ScriptId { get; set; }

        /// <summary>
        /// ??的?本信息
        /// ?航?性
        /// </summary>
        [ForeignKey("ScriptId")]
        public virtual ScriptInfo? Script { get; set; }

        /// <summary>
        /// ??的任?ID（可?）
        /// ??任?管理?行?使用
        /// </summary>
        [Column("TaskId")]
        public int? TaskId { get; set; }

        /// <summary>
        /// ??的任?信息
        /// ?航?性
        /// </summary>
        [ForeignKey("TaskId")]
        public virtual TaskInfo? Task { get; set; }

        #endregion

        #region ?行信息

        /// <summary>
        /// ?行??
        /// 用于???次?行的?短描述
        /// </summary>
        [Required]
        [MaxLength(200)]
        [Column("Title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// ?行的?本代?
        /// 存????行的代??容
        /// </summary>
        [Required]
        [Column("Code", TypeName = "TEXT")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// ?行??
        /// Success, Failed, Cancelled, CompilationError
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("Status")]
        public string Status { get; set; } = "Running";

        /// <summary>
        /// ?行?始??
        /// </summary>
        [Column("StartTime")]
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>
        /// ?行?束??
        /// </summary>
        [Column("EndTime")]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// ?行耗?（毫秒）
        /// </summary>
        [Column("Duration")]
        public long Duration { get; set; }

        #endregion

        #region ?行?果

        /// <summary>
        /// ?准?出?容
        /// ?本?行?程中的所有?准?出
        /// </summary>
        [Column("Output", TypeName = "TEXT")]
        public string Output { get; set; } = string.Empty;

        /// <summary>
        /// ???出?容
        /// ?本?行?程中的所有???出
        /// </summary>
        [Column("ErrorOutput", TypeName = "TEXT")]
        public string ErrorOutput { get; set; } = string.Empty;

        /// <summary>
        /// 返回值
        /// ?本?行的返回值（序列化?字符串）
        /// </summary>
        [Column("ReturnValue", TypeName = "TEXT")]
        public string? ReturnValue { get; set; }

        /// <summary>
        /// 返回值?型
        /// ??返回值的???型
        /// </summary>
        [MaxLength(200)]
        [Column("ReturnValueType")]
        public string? ReturnValueType { get; set; }

        /// <summary>
        /// 异常信息
        /// 如果?行失?，????的异常信息
        /// </summary>
        [Column("Exception", TypeName = "TEXT")]
        public string? Exception { get; set; }

        #endregion

        #region ?行?境

        /// <summary>
        /// 使用的NuGet包信息
        /// JSON格式存?使用的NuGet包列表
        /// </summary>
        [Column("NuGetPackages", TypeName = "TEXT")]
        public string NuGetPackages { get; set; } = "[]";

        /// <summary>
        /// ?行机器名?
        /// </summary>
        [MaxLength(100)]
        [Column("MachineName")]
        public string MachineName { get; set; } = Environment.MachineName;

        /// <summary>
        /// ?行用?名
        /// </summary>
        [MaxLength(100)]
        [Column("UserName")]
        public string UserName { get; set; } = Environment.UserName;

        #endregion

        #region ?助?性

        /// <summary>
        /// 是否成功?行
        /// ?算?性，基于Status判?
        /// </summary>
        [NotMapped]
        public bool IsSuccess => Status == "Success";

        /// <summary>
        /// ?行耗?的友好?示
        /// ?算?性，格式化?示?行耗?
        /// </summary>
        [NotMapped]
        public string DurationDisplay
        {
            get
            {
                if (Duration < 1000) return $"{Duration}ms";
                if (Duration < 60000) return $"{Duration / 1000.0:F2}s";
                return $"{Duration / 60000.0:F2}min";
            }
        }

        /// <summary>
        /// ?行??范?的友好?示
        /// </summary>
        [NotMapped]
        public string TimeRangeDisplay
        {
            get
            {
                var start = StartTime.ToString("yyyy-MM-dd HH:mm:ss");
                if (EndTime.HasValue)
                {
                    var end = EndTime.Value.ToString("HH:mm:ss");
                    return $"{start} - {end}";
                }
                return $"{start} - ?行中";
            }
        }

        /// <summary>
        /// ?出?容的摘要
        /// ?算?性，用于列表?示
        /// </summary>
        [NotMapped]
        public string OutputSummary
        {
            get
            {
                if (string.IsNullOrEmpty(Output)) return "??出";
                
                var lines = Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 0) return "??出";
                
                var firstLine = lines[0].Trim();
                return firstLine.Length > 100 ? firstLine.Substring(0, 100) + "..." : firstLine;
            }
        }

        #endregion
    }
}