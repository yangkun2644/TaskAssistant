using System.Text;

namespace TaskAssistant.Models
{
    /// <summary>
    /// ?��?��?�G?
    /// ��??��?�檺����?�G�H��
    /// </summary>
    public class ScriptExecutionResult
    {
        #region �۳y��?

        /// <summary>
        /// ��l��?��?��?�G
        /// </summary>
        public ScriptExecutionResult()
        {
            StartTime = DateTime.Now;
            NuGetPackages = new List<string>();
            OutputBuilder = new StringBuilder();
            ErrorBuilder = new StringBuilder();
        }

        #endregion

        #region �򥻫H��

        /// <summary>
        /// ?��O�_���\
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// ?��??
        /// Running, Success, Failed, Cancelled, CompilationError
        /// </summary>
        public string Status { get; set; } = "Running";

        /// <summary>
        /// ?��?�l??
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// ?��?��??
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// ?���?�]�@��^
        /// </summary>
        public long Duration => EndTime.HasValue 
            ? (long)(EndTime.Value - StartTime).TotalMilliseconds 
            : (long)(DateTime.Now - StartTime).TotalMilliseconds;

        #endregion

        #region ?��?�G

        /// <summary>
        /// ��^��
        /// </summary>
        public object? ReturnValue { get; set; }

        /// <summary>
        /// ��^��?��
        /// </summary>
        public string? ReturnValueType => ReturnValue?.GetType().FullName;

        /// <summary>
        /// �ݱ`�H��
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// ?�X?�e�۫ؾ�
        /// </summary>
        public StringBuilder OutputBuilder { get; set; }

        /// <summary>
        /// ???�X�۫ؾ�
        /// </summary>
        public StringBuilder ErrorBuilder { get; set; }

        /// <summary>
        /// ?�X?�e
        /// </summary>
        public string Output => OutputBuilder.ToString();

        /// <summary>
        /// ???�X?�e
        /// </summary>
        public string ErrorOutput => ErrorBuilder.ToString();

        #endregion

        #region ?��?��

        /// <summary>
        /// �ϥΪ�NuGet�]�C��
        /// </summary>
        public List<string> NuGetPackages { get; set; }

        /// <summary>
        /// ?�檺?���N?
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// ?��??
        /// </summary>
        public string Title { get; set; } = string.Empty;

        #endregion

        #region ��k

        /// <summary>
        /// �K�[?�X?�e
        /// </summary>
        /// <param name="text">?�X�奻</param>
        public void AppendOutput(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                OutputBuilder.Append(text);
            }
        }

        /// <summary>
        /// �K�[???�X?�e
        /// </summary>
        /// <param name="text">??�奻</param>
        public void AppendError(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                ErrorBuilder.Append(text);
            }
        }

        /// <summary>
        /// �K�[?�X��
        /// </summary>
        /// <param name="text">?�X�奻</param>
        public void AppendOutputLine(string text = "")
        {
            OutputBuilder.AppendLine(text);
        }

        /// <summary>
        /// �K�[???�X��
        /// </summary>
        /// <param name="text">??�奻</param>
        public void AppendErrorLine(string text = "")
        {
            ErrorBuilder.AppendLine(text);
        }

        /// <summary>
        /// ???�榨�\����
        /// </summary>
        /// <param name="returnValue">��^��</param>
        public void MarkSuccess(object? returnValue = null)
        {
            IsSuccess = true;
            Status = "Success";
            ReturnValue = returnValue;
            EndTime = DateTime.Now;
        }

        /// <summary>
        /// ???�楢?
        /// </summary>
        /// <param name="exception">�ݱ`�H��</param>
        /// <param name="status">��???</param>
        public void MarkFailed(Exception exception, string status = "Failed")
        {
            IsSuccess = false;
            Status = status;
            Exception = exception;
            EndTime = DateTime.Now;
        }

        /// <summary>
        /// ???��Q����
        /// </summary>
        public void MarkCancelled()
        {
            IsSuccess = false;
            Status = "Cancelled";
            EndTime = DateTime.Now;
        }

        /// <summary>
        /// ????���Ӽҫ�
        /// </summary>
        /// <param name="scriptId">?��ID</param>
        /// <param name="taskId">��?ID</param>
        /// <returns>?��?����</returns>
        public ScriptExecutionLog ToExecutionLog(int? scriptId = null, int? taskId = null)
        {
            return new ScriptExecutionLog
            {
                ScriptId = scriptId,
                TaskId = taskId,
                Title = Title,
                Code = Code,
                Status = Status,
                StartTime = StartTime,
                EndTime = EndTime,
                Duration = Duration,
                Output = Output,
                ErrorOutput = ErrorOutput,
                ReturnValue = ReturnValue?.ToString(),
                ReturnValueType = ReturnValueType,
                Exception = Exception?.ToString(),
                NuGetPackages = System.Text.Json.JsonSerializer.Serialize(NuGetPackages)
            };
        }

        #endregion
    }
}