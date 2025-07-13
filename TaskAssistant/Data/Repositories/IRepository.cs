using System.Linq.Expressions;

namespace TaskAssistant.Data.Repositories
{
    /// <summary>
    /// 通用??接口
    /// 定?基本的CRUD操作和查?方法
    /// </summary>
    /// <typeparam name="T">?体?型</typeparam>
    public interface IRepository<T> where T : class
    {
        #region 查?方法

        /// <summary>
        /// 根据ID?取?体
        /// </summary>
        /// <param name="id">?体ID</param>
        /// <returns>?体?象，如果不存在?返回null</returns>
        Task<T?> GetByIdAsync(int id);

        /// <summary>
        /// ?取所有?体
        /// </summary>
        /// <returns>所有?体的集合</returns>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// 根据?件查找?体
        /// </summary>
        /// <param name="predicate">查??件</param>
        /// <returns>?足?件的?体集合</returns>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 根据?件查找???体
        /// </summary>
        /// <param name="predicate">查??件</param>
        /// <returns>?足?件的第一??体，如果不存在?返回null</returns>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// ?查是否存在?足?件的?体
        /// </summary>
        /// <param name="predicate">查??件</param>
        /// <returns>如果存在?返回true，否?返回false</returns>
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// ?取?足?件的?体?量
        /// </summary>
        /// <param name="predicate">查??件</param>
        /// <returns>?体?量</returns>
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 分?查?
        /// </summary>
        /// <param name="pageIndex">?面索引（?0?始）</param>
        /// <param name="pageSize">?面大小</param>
        /// <param name="predicate">查??件（可?）</param>
        /// <param name="orderBy">排序表?式（可?）</param>
        /// <returns>分??果</returns>
        Task<PagedResult<T>> GetPagedAsync(int pageIndex, int pageSize, 
            Expression<Func<T, bool>>? predicate = null,
            Expression<Func<T, object>>? orderBy = null);

        #endregion

        #region 修改方法

        /// <summary>
        /// 添加?体
        /// </summary>
        /// <param name="entity">要添加的?体</param>
        /// <returns>添加的?体</returns>
        Task<T> AddAsync(T entity);

        /// <summary>
        /// 批量添加?体
        /// </summary>
        /// <param name="entities">要添加的?体集合</param>
        /// <returns>添加的?体集合</returns>
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// 更新?体
        /// </summary>
        /// <param name="entity">要更新的?体</param>
        /// <returns>更新后的?体</returns>
        Task<T> UpdateAsync(T entity);

        /// <summary>
        /// 批量更新?体
        /// </summary>
        /// <param name="entities">要更新的?体集合</param>
        /// <returns>更新后的?体集合</returns>
        Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// ?除?体
        /// </summary>
        /// <param name="entity">要?除的?体</param>
        Task DeleteAsync(T entity);

        /// <summary>
        /// 根据ID?除?体
        /// </summary>
        /// <param name="id">?体ID</param>
        Task DeleteAsync(int id);

        /// <summary>
        /// 批量?除?体
        /// </summary>
        /// <param name="entities">要?除的?体集合</param>
        Task DeleteRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// 根据?件?除?体
        /// </summary>
        /// <param name="predicate">?除?件</param>
        Task DeleteRangeAsync(Expression<Func<T, bool>> predicate);

        #endregion

        #region 事?方法

        /// <summary>
        /// 保存所有更改
        /// </summary>
        /// <returns>受影?的行?</returns>
        Task<int> SaveChangesAsync();

        #endregion
    }

    /// <summary>
    /// 分??果?
    /// </summary>
    /// <typeparam name="T">?体?型</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// ?据集合
        /// </summary>
        public IEnumerable<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// ????
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// ?面索引（?0?始）
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// ?面大小
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// ???
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        /// <summary>
        /// 是否有上一?
        /// </summary>
        public bool HasPreviousPage => PageIndex > 0;

        /// <summary>
        /// 是否有下一?
        /// </summary>
        public bool HasNextPage => PageIndex < TotalPages - 1;
    }
}