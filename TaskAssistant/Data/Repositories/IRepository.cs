using System.Linq.Expressions;

namespace TaskAssistant.Data.Repositories
{
    /// <summary>
    /// �q��??���f
    /// �w?�򥻪�CRUD�ާ@�M�d?��k
    /// </summary>
    /// <typeparam name="T">?�^?��</typeparam>
    public interface IRepository<T> where T : class
    {
        #region �d?��k

        /// <summary>
        /// ���uID?��?�^
        /// </summary>
        /// <param name="id">?�^ID</param>
        /// <returns>?�^?�H�A�p�G���s�b?��^null</returns>
        Task<T?> GetByIdAsync(int id);

        /// <summary>
        /// ?���Ҧ�?�^
        /// </summary>
        /// <returns>�Ҧ�?�^�����X</returns>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// ���u?��d��?�^
        /// </summary>
        /// <param name="predicate">�d??��</param>
        /// <returns>?��?��?�^���X</returns>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// ���u?��d��???�^
        /// </summary>
        /// <param name="predicate">�d??��</param>
        /// <returns>?��?�󪺲Ĥ@??�^�A�p�G���s�b?��^null</returns>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// ?�d�O�_�s�b?��?��?�^
        /// </summary>
        /// <param name="predicate">�d??��</param>
        /// <returns>�p�G�s�b?��^true�A�_?��^false</returns>
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// ?��?��?��?�^?�q
        /// </summary>
        /// <param name="predicate">�d??��</param>
        /// <returns>?�^?�q</returns>
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// ��?�d?
        /// </summary>
        /// <param name="pageIndex">?�����ޡ]?0?�l�^</param>
        /// <param name="pageSize">?���j�p</param>
        /// <param name="predicate">�d??��]�i?�^</param>
        /// <param name="orderBy">�ƧǪ�?���]�i?�^</param>
        /// <returns>��??�G</returns>
        Task<PagedResult<T>> GetPagedAsync(int pageIndex, int pageSize, 
            Expression<Func<T, bool>>? predicate = null,
            Expression<Func<T, object>>? orderBy = null);

        #endregion

        #region �ק��k

        /// <summary>
        /// �K�[?�^
        /// </summary>
        /// <param name="entity">�n�K�[��?�^</param>
        /// <returns>�K�[��?�^</returns>
        Task<T> AddAsync(T entity);

        /// <summary>
        /// ��q�K�[?�^
        /// </summary>
        /// <param name="entities">�n�K�[��?�^���X</param>
        /// <returns>�K�[��?�^���X</returns>
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// ��s?�^
        /// </summary>
        /// <param name="entity">�n��s��?�^</param>
        /// <returns>��s�Z��?�^</returns>
        Task<T> UpdateAsync(T entity);

        /// <summary>
        /// ��q��s?�^
        /// </summary>
        /// <param name="entities">�n��s��?�^���X</param>
        /// <returns>��s�Z��?�^���X</returns>
        Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// ?��?�^
        /// </summary>
        /// <param name="entity">�n?����?�^</param>
        Task DeleteAsync(T entity);

        /// <summary>
        /// ���uID?��?�^
        /// </summary>
        /// <param name="id">?�^ID</param>
        Task DeleteAsync(int id);

        /// <summary>
        /// ��q?��?�^
        /// </summary>
        /// <param name="entities">�n?����?�^���X</param>
        Task DeleteRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// ���u?��?��?�^
        /// </summary>
        /// <param name="predicate">?��?��</param>
        Task DeleteRangeAsync(Expression<Func<T, bool>> predicate);

        #endregion

        #region ��?��k

        /// <summary>
        /// �O�s�Ҧ����
        /// </summary>
        /// <returns>���v?����?</returns>
        Task<int> SaveChangesAsync();

        #endregion
    }

    /// <summary>
    /// ��??�G?
    /// </summary>
    /// <typeparam name="T">?�^?��</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// ?�u���X
        /// </summary>
        public IEnumerable<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// ????
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// ?�����ޡ]?0?�l�^
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// ?���j�p
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// ???
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        /// <summary>
        /// �O�_���W�@?
        /// </summary>
        public bool HasPreviousPage => PageIndex > 0;

        /// <summary>
        /// �O�_���U�@?
        /// </summary>
        public bool HasNextPage => PageIndex < TotalPages - 1;
    }
}