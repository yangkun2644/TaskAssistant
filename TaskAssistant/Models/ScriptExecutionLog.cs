using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskAssistant.Models
{
    /// <summary>
    /// ?��?����?�u�ҫ�?
    /// �Τ_�s??��?�檺����??�A�]�A?�X�B??�B?��??���H��
    /// ���?���޲z�M��?�޲z����?���Ӭd��
    /// </summary>
    [Table("ScriptExecutionLogs")]
    public class ScriptExecutionLog
    {
        #region �D??��

        /// <summary>
        /// ?���Ӱߤ@??��
        /// ?�u?�D?�A��?�ͦ�
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        #endregion

        #region ???��

        /// <summary>
        /// ??��?��ID�]�i?�^
        /// ???���޲z?��?�ϥ�
        /// </summary>
        [Column("ScriptId")]
        public int? ScriptId { get; set; }

        /// <summary>
        /// ??��?���H��
        /// ?��?��
        /// </summary>
        [ForeignKey("ScriptId")]
        public virtual ScriptInfo? Script { get; set; }

        /// <summary>
        /// ??����?ID�]�i?�^
        /// ??��?�޲z?��?�ϥ�
        /// </summary>
        [Column("TaskId")]
        public int? TaskId { get; set; }

        /// <summary>
        /// ??����?�H��
        /// ?��?��
        /// </summary>
        [ForeignKey("TaskId")]
        public virtual TaskInfo? Task { get; set; }

        #endregion

        #region ?��H��

        /// <summary>
        /// ?��??
        /// �Τ_???��?�檺?�u�y�z
        /// </summary>
        [Required]
        [MaxLength(200)]
        [Column("Title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// ?�檺?���N?
        /// �s????�檺�N??�e
        /// </summary>
        [Required]
        [Column("Code", TypeName = "TEXT")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// ?��??
        /// Success, Failed, Cancelled, CompilationError
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("Status")]
        public string Status { get; set; } = "Running";

        /// <summary>
        /// ?��?�l??
        /// </summary>
        [Column("StartTime")]
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>
        /// ?��?��??
        /// </summary>
        [Column("EndTime")]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// ?���?�]�@��^
        /// </summary>
        [Column("Duration")]
        public long Duration { get; set; }

        #endregion

        #region ?��?�G

        /// <summary>
        /// ?��?�X?�e
        /// ?��?��?�{�����Ҧ�?��?�X
        /// </summary>
        [Column("Output", TypeName = "TEXT")]
        public string Output { get; set; } = string.Empty;

        /// <summary>
        /// ???�X?�e
        /// ?��?��?�{�����Ҧ�???�X
        /// </summary>
        [Column("ErrorOutput", TypeName = "TEXT")]
        public string ErrorOutput { get; set; } = string.Empty;

        /// <summary>
        /// ��^��
        /// ?��?�檺��^�ȡ]�ǦC��?�r�Ŧ�^
        /// </summary>
        [Column("ReturnValue", TypeName = "TEXT")]
        public string? ReturnValue { get; set; }

        /// <summary>
        /// ��^��?��
        /// ??��^�Ȫ�???��
        /// </summary>
        [MaxLength(200)]
        [Column("ReturnValueType")]
        public string? ReturnValueType { get; set; }

        /// <summary>
        /// �ݱ`�H��
        /// �p�G?�楢?�A????���ݱ`�H��
        /// </summary>
        [Column("Exception", TypeName = "TEXT")]
        public string? Exception { get; set; }

        #endregion

        #region ?��?��

        /// <summary>
        /// �ϥΪ�NuGet�]�H��
        /// JSON�榡�s?�ϥΪ�NuGet�]�C��
        /// </summary>
        [Column("NuGetPackages", TypeName = "TEXT")]
        public string NuGetPackages { get; set; } = "[]";

        /// <summary>
        /// ?���󾹦W?
        /// </summary>
        [MaxLength(100)]
        [Column("MachineName")]
        public string MachineName { get; set; } = Environment.MachineName;

        /// <summary>
        /// ?���?�W
        /// </summary>
        [MaxLength(100)]
        [Column("UserName")]
        public string UserName { get; set; } = Environment.UserName;

        #endregion

        #region ?�U?��

        /// <summary>
        /// �O�_���\?��
        /// ?��?�ʡA��_Status�P?
        /// </summary>
        [NotMapped]
        public bool IsSuccess => Status == "Success";

        /// <summary>
        /// ?���?���ͦn?��
        /// ?��?�ʡA�榡��?��?���?
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
        /// ?��??�S?���ͦn?��
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
                return $"{start} - ?�椤";
            }
        }

        /// <summary>
        /// ?�X?�e���K�n
        /// ?��?�ʡA�Τ_�C��?��
        /// </summary>
        [NotMapped]
        public string OutputSummary
        {
            get
            {
                if (string.IsNullOrEmpty(Output)) return "??�X";
                
                var lines = Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 0) return "??�X";
                
                var firstLine = lines[0].Trim();
                return firstLine.Length > 100 ? firstLine.Substring(0, 100) + "..." : firstLine;
            }
        }

        #endregion
    }
}