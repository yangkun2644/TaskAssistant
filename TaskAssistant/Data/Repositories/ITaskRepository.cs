using TaskAssistant.Models;

namespace TaskAssistant.Data.Repositories
{
    /// <summary>
    /// ��???���f
    /// �w?��?��?���S��ާ@
    /// </summary>
    public interface ITaskRepository : IRepository<TaskInfo>
    {
        #region ��?�S��d?

        /// <summary>
        /// ���u???����?
        /// </summary>
        /// <param name="status">��???</param>
        /// <returns>���w??����?���X</returns>
        Task<IEnumerable<TaskInfo>> GetByStatusAsync(string status);

        /// <summary>
        /// ?����?�檺��?
        /// </summary>
        /// <param name="count">?��?�q�]�i?�^</param>
        /// <returns>��?�檺��?���X</returns>
        Task<IEnumerable<TaskInfo>> GetPendingTasksAsync(int? count = null);

        /// <summary>
        /// ?�����b?�檺��?
        /// </summary>
        /// <returns>���b?�檺��?���X</returns>
        Task<IEnumerable<TaskInfo>> GetRunningTasksAsync();

        /// <summary>
        /// ���u?��ID?����?
        /// </summary>
        /// <param name="scriptId">?��ID</param>
        /// <returns>�ϥΫ��w?������?���X</returns>
        Task<IEnumerable<TaskInfo>> GetByScriptIdAsync(int scriptId);

        /// <summary>
        /// ?��?�E?�檺��?
        /// </summary>
        /// <param name="fromTime">?�l??</param>
        /// <param name="toTime">?��??</param>
        /// <returns>�b���w??�S???�E?�檺��?���X</returns>
        Task<IEnumerable<TaskInfo>> GetScheduledTasksAsync(DateTime fromTime, DateTime toTime);

        /// <summary>
        /// �j����?
        /// </summary>
        /// <param name="keyword">�j��???</param>
        /// <param name="status">????�]�i?�^</param>
        /// <param name="taskType">��??��??�]�i?�^</param>
        /// <param name="isEnabled">�O�_?��??�]�i?�^</param>
        /// <returns>�ǰt����?���X</returns>
        Task<IEnumerable<TaskInfo>> SearchAsync(string keyword, string? status = null, string? taskType = null, bool? isEnabled = null);

        /// <summary>
        /// ?����???�H��
        /// </summary>
        /// <returns>��???�H��</returns>
        Task<TaskStatistics> GetStatisticsAsync();

        #endregion

        #region ��???�޲z

        /// <summary>
        /// ��s��???
        /// </summary>
        /// <param name="taskId">��?ID</param>
        /// <param name="status">�s??</param>
        /// <param name="result">?��?�G�]�i?�^</param>
        /// <param name="error">??�H���]�i?�^</param>
        /// <returns>��s�Z����?�H��</returns>
        Task<TaskInfo?> UpdateTaskStatusAsync(int taskId, string status, string? result = null, string? error = null);

        /// <summary>
        /// ??��??�l?��
        /// </summary>
        /// <param name="taskId">��?ID</param>
        /// <returns>��s�Z����?�H��</returns>
        Task<TaskInfo?> MarkTaskStartedAsync(int taskId);

        /// <summary>
        /// ??��?����
        /// </summary>
        /// <param name="taskId">��?ID</param>
        /// <param name="result">?��?�G</param>
        /// <param name="isSuccess">�O�_���\</param>
        /// <returns>��s�Z����?�H��</returns>
        Task<TaskInfo?> MarkTaskCompletedAsync(int taskId, string result, bool isSuccess = true);

        /// <summary>
        /// ??��?��?
        /// </summary>
        /// <param name="taskId">��?ID</param>
        /// <param name="error">??�H��</param>
        /// <returns>��s�Z����?�H��</returns>
        Task<TaskInfo?> MarkTaskFailedAsync(int taskId, string error);

        /// <summary>
        /// ���m��???
        /// </summary>
        /// <param name="taskId">��?ID</param>
        /// <returns>���m�Z����?�H��</returns>
        Task<TaskInfo?> ResetTaskAsync(int taskId);

        #endregion

        #region ��q�ާ@

        /// <summary>
        /// ��q��s��???
        /// </summary>
        /// <param name="taskIds">��?ID���X</param>
        /// <param name="status">�s??</param>
        /// <returns>���v?����?</returns>
        Task<int> BulkUpdateStatusAsync(IEnumerable<int> taskIds, string status);

        /// <summary>
        /// ��q?��/�T�Υ�?
        /// </summary>
        /// <param name="taskIds">��?ID���X</param>
        /// <param name="isEnabled">�O�_?��</param>
        /// <returns>���v?����?</returns>
        Task<int> BulkUpdateEnabledStatusAsync(IEnumerable<int> taskIds, bool isEnabled);

        /// <summary>
        /// �M�z�w��������?
        /// </summary>
        /// <param name="daysOld">�O�d��?</param>
        /// <returns>�Q�M�z����??�q</returns>
        Task<int> CleanupCompletedTasksAsync(int daysOld = 30);

        #endregion
    }

    /// <summary>
    /// ��???�H��
    /// </summary>
    public class TaskStatistics
    {
        /// <summary>
        /// ?��??
        /// </summary>
        public int TotalTasks { get; set; }

        /// <summary>
        /// ��?���??
        /// </summary>
        public int PendingTasks { get; set; }

        /// <summary>
        /// ?�椤��??
        /// </summary>
        public int RunningTasks { get; set; }

        /// <summary>
        /// �w������??
        /// </summary>
        public int CompletedTasks { get; set; }

        /// <summary>
        /// ��?��??
        /// </summary>
        public int FailedTasks { get; set; }

        /// <summary>
        /// ?�Υ�??
        /// </summary>
        public int EnabledTasks { get; set; }

        /// <summary>
        /// �T�Υ�??
        /// </summary>
        public int DisabledTasks { get; set; }

        /// <summary>
        /// ����?���??
        /// </summary>
        public int TodayExecutedTasks { get; set; }

        /// <summary>
        /// ���P?���??
        /// </summary>
        public int WeekExecutedTasks { get; set; }

        /// <summary>
        /// ���\�v�]�ʤ���^
        /// </summary>
        public double SuccessRate { get; set; }
    }
}