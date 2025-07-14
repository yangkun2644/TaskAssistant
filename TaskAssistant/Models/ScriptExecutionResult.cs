using System.Text;

namespace TaskAssistant.Models
{
    /// <summary>
    /// ?本?行?果?
    /// 封??本?行的完整?果信息
    /// </summary>
    public class ScriptExecutionResult
    {
        #region 构造函?

        /// <summary>
        /// 初始化?本?行?果
        /// </summary>
        public ScriptExecutionResult()
        {
            StartTime = DateTime.Now;
            NuGetPackages = new List<string>();
            OutputBuilder = new StringBuilder();
            ErrorBuilder = new StringBuilder();
        }

        #endregion

        #region 基本信息

        /// <summary>
        /// ?行是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// ?行??
        /// Running, Success, Failed, Cancelled, CompilationError
        /// </summary>
        public string Status { get; set; } = "Running";

        /// <summary>
        /// ?行?始??
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// ?行?束??
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// ?行耗?（毫秒）
        /// </summary>
        public long Duration => EndTime.HasValue 
            ? (long)(EndTime.Value - StartTime).TotalMilliseconds 
            : (long)(DateTime.Now - StartTime).TotalMilliseconds;

        #endregion

        #region ?行?果

        /// <summary>
        /// 返回值
        /// </summary>
        public object? ReturnValue { get; set; }

        /// <summary>
        /// 返回值?型
        /// </summary>
        public string? ReturnValueType => ReturnValue?.GetType().FullName;

        /// <summary>
        /// 异常信息
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// ?出?容构建器
        /// </summary>
        public StringBuilder OutputBuilder { get; set; }

        /// <summary>
        /// ???出构建器
        /// </summary>
        public StringBuilder ErrorBuilder { get; set; }

        /// <summary>
        /// ?出?容
        /// </summary>
        public string Output => OutputBuilder.ToString();

        /// <summary>
        /// ???出?容
        /// </summary>
        public string ErrorOutput => ErrorBuilder.ToString();

        #endregion

        #region ?行?境

        /// <summary>
        /// 使用的NuGet包列表
        /// </summary>
        public List<string> NuGetPackages { get; set; }

        /// <summary>
        /// ?行的?本代?
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// ?行??
        /// </summary>
        public string Title { get; set; } = string.Empty;

        #endregion

        #region 方法

        /// <summary>
        /// 添加?出?容
        /// </summary>
        /// <param name="text">?出文本</param>
        public void AppendOutput(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                OutputBuilder.Append(text);
            }
        }

        /// <summary>
        /// 添加???出?容
        /// </summary>
        /// <param name="text">??文本</param>
        public void AppendError(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                ErrorBuilder.Append(text);
            }
        }

        /// <summary>
        /// 添加?出行
        /// </summary>
        /// <param name="text">?出文本</param>
        public void AppendOutputLine(string text = "")
        {
            OutputBuilder.AppendLine(text);
        }

        /// <summary>
        /// 添加???出行
        /// </summary>
        /// <param name="text">??文本</param>
        public void AppendErrorLine(string text = "")
        {
            ErrorBuilder.AppendLine(text);
        }

        /// <summary>
        /// ???行成功完成
        /// </summary>
        /// <param name="returnValue">返回值</param>
        public void MarkSuccess(object? returnValue = null)
        {
            IsSuccess = true;
            Status = "Success";
            ReturnValue = returnValue;
            EndTime = DateTime.Now;
        }

        /// <summary>
        /// ???行失?
        /// </summary>
        /// <param name="exception">异常信息</param>
        /// <param name="status">失???</param>
        public void MarkFailed(Exception exception, string status = "Failed")
        {
            IsSuccess = false;
            Status = status;
            Exception = exception;
            EndTime = DateTime.Now;
        }

        /// <summary>
        /// ???行被取消
        /// </summary>
        public void MarkCancelled()
        {
            IsSuccess = false;
            Status = "Cancelled";
            EndTime = DateTime.Now;
        }

        /// <summary>
        /// ????行日志模型
        /// </summary>
        /// <param name="scriptId">?本ID</param>
        /// <param name="taskId">任?ID</param>
        /// <returns>?本?行日志</returns>
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