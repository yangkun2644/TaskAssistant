using TaskAssistant.Models;

namespace TaskAssistant.Data.Repositories
{
    /// <summary>
    /// ?��??���f
    /// �w??����?���S��ާ@
    /// </summary>
    public interface IScriptRepository : IRepository<ScriptInfo>
    {
        #region ?���S��d?

        /// <summary>
        /// ���u�W??��?��
        /// </summary>
        /// <param name="name">?���W?</param>
        /// <returns>?���H���A�p�G���s�b?��^null</returns>
        Task<ScriptInfo?> GetByNameAsync(string name);

        /// <summary>
        /// ?�d?���W?�O�_�w�s�b
        /// </summary>
        /// <param name="name">?���W?</param>
        /// <param name="excludeId">�ư���?��ID�]�Τ_��s??�d�^</param>
        /// <returns>�p�G�W?�w�s�b?��^true�A�_?��^false</returns>
        Task<bool> IsNameExistsAsync(string name, int? excludeId = null);

        /// <summary>
        /// ���u��??��?���]?�q?�A���]�tCode�r�q�^
        /// </summary>
        /// <param name="category">?����?</param>
        /// <returns>���w��?��?�����X</returns>
        Task<IEnumerable<ScriptInfo>> GetByCategoryAsync(string category);

        /// <summary>
        /// ���u��??��?���]���㪩���A�]�tCode�r�q�^
        /// </summary>
        /// <param name="category">?����?</param>
        /// <returns>���w��?������?�����X</returns>
        Task<IEnumerable<ScriptInfo>> GetByCategoryWithCodeAsync(string category);

        /// <summary>
        /// ?���Ҧ���?
        /// </summary>
        /// <returns>�Ҧ���?�W?�����X</returns>
        Task<IEnumerable<string>> GetAllCategoriesAsync();

        /// <summary>
        /// �j��?���]?�q?�A���]�tCode�r�q�^
        /// </summary>
        /// <param name="keyword">�j��??�r</param>
        /// <param name="category">��???�]�i?�^</param>
        /// <param name="isEnabled">�O�_?��??�]�i?�^</param>
        /// <returns>�ǰt��?�����X</returns>
        Task<IEnumerable<ScriptInfo>> SearchAsync(string keyword, string? category = null, bool? isEnabled = null);

        /// <summary>
        /// �j��?���]���㪩���A�]�tCode�r�q�^
        /// </summary>
        /// <param name="keyword">�j��??�r</param>
        /// <param name="category">��???�]�i?�^</param>
        /// <param name="isEnabled">�O�_?��??�]�i?�^</param>
        /// <returns>�ǰt������?�����X</returns>
        Task<IEnumerable<ScriptInfo>> SearchWithCodeAsync(string keyword, string? category = null, bool? isEnabled = null);

        /// <summary>
        /// ?���̪�ϥΪ�?���]?�q?�A���]�tCode�r�q�^
        /// </summary>
        /// <param name="count">?��?�q</param>
        /// <returns>�̪�ϥΪ�?�����X</returns>
        Task<IEnumerable<ScriptInfo>> GetRecentlyUsedAsync(int count = 10);

        /// <summary>
        /// ?���̱`�Ϊ�?���]?�q?�A���]�tCode�r�q�^
        /// </summary>
        /// <param name="count">?��?�q</param>
        /// <returns>�̱`�Ϊ�?�����X</returns>
        Task<IEnumerable<ScriptInfo>> GetMostUsedAsync(int count = 10);

        /// <summary>
        /// ?��?���C��]?�q?�A���]�tCode�r�q�^
        /// </summary>
        /// <param name="category">��???�]�i?�^</param>
        /// <param name="isEnabled">�O�_?��??�]�i?�^</param>
        /// <param name="pageIndex">?�����ޡ]?0?�l�^</param>
        /// <param name="pageSize">?���j�p</param>
        /// <returns>?���C��</returns>
        Task<IEnumerable<ScriptInfo>> GetScriptListAsync(string? category = null, bool? isEnabled = null, int? pageIndex = null, int? pageSize = null);

        #endregion

        #region ?��??��s

        /// <summary>
        /// ��s?��?��??
        /// </summary>
        /// <param name="scriptId">?��ID</param>
        /// <returns>��s�Z��?���H��</returns>
        Task<ScriptInfo?> UpdateExecutionStatsAsync(int scriptId);

        /// <summary>
        /// ��q?��/�T��?��
        /// </summary>
        /// <param name="scriptIds">?��ID���X</param>
        /// <param name="isEnabled">�O�_?��</param>
        /// <returns>���v?����?</returns>
        Task<int> BulkUpdateEnabledStatusAsync(IEnumerable<int> scriptIds, bool isEnabled);

        /// <summary>
        /// �M�z���ϥΪ�?��
        /// </summary>
        /// <param name="daysUnused">���ϥΤ�?</param>
        /// <returns>�Q�M�z��?��?�q</returns>
        Task<int> CleanupUnusedScriptsAsync(int daysUnused = 90);

        #endregion
    }
}