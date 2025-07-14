namespace TaskAssistant.Models
{
    /// <summary>
    /// ?��??�H��
    /// </summary>
    public class ExecutionStatistics
    {
        /// <summary>
        /// ??�榸?
        /// </summary>
        public int TotalExecutions { get; set; }

        /// <summary>
        /// ���\?�榸?
        /// </summary>
        public int SuccessfulExecutions { get; set; }

        /// <summary>
        /// ��??�榸?
        /// </summary>
        public int FailedExecutions { get; set; }

        /// <summary>
        /// ����?�榸?
        /// </summary>
        public int CancelledExecutions { get; set; }

        /// <summary>
        /// ����?��??�]�@��^
        /// </summary>
        public double AverageExecutionTime { get; set; }

        /// <summary>
        /// �̦Z?��??
        /// </summary>
        public DateTime? LastExecutionTime { get; set; }

        /// <summary>
        /// ���\�v
        /// </summary>
        public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions * 100 : 0;

        /// <summary>
        /// ��?�v
        /// </summary>
        public double FailureRate => TotalExecutions > 0 ? (double)FailedExecutions / TotalExecutions * 100 : 0;
    }
}