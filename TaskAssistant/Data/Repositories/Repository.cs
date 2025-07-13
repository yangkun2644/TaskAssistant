using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace TaskAssistant.Data.Repositories
{
    /// <summary>
    /// 通用?????
    /// 提供基本的CRUD操作和查?功能
    /// </summary>
    /// <typeparam name="T">?体?型</typeparam>
    public class Repository<T> : IRepository<T> where T : class
    {
        #region 私有字段

        /// <summary>
        /// ?据?上下文
        /// </summary>
        protected readonly AppDbContext _context;

        /// <summary>
        /// ?体?据集
        /// </summary>
        protected readonly DbSet<T> _dbSet;

        #endregion

        #region 构造函?

        /// <summary>
        /// 初始化??
        /// </summary>
        /// <param name="context">?据?上下文</param>
        public Repository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        #endregion

        #region 查?方法??

        /// <summary>
        /// 根据ID?取?体
        /// </summary>
        /// <param name="id">?体ID</param>
        /// <returns>?体?象，如果不存在?返回null</returns>
        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <summary>
        /// ?取所有?体
        /// </summary>
        /// <returns>所有?体的集合</returns>
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        /// <summary>
        /// 根据?件查找?体
        /// </summary>
        /// <param name="predicate">查??件</param>
        /// <returns>?足?件的?体集合</returns>
        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        /// <summary>
        /// 根据?件查找???体
        /// </summary>
        /// <param name="predicate">查??件</param>
        /// <returns>?足?件的第一??体，如果不存在?返回null</returns>
        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        /// <summary>
        /// ?查是否存在?足?件的?体
        /// </summary>
        /// <param name="predicate">查??件</param>
        /// <returns>如果存在?返回true，否?返回false</returns>
        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        /// <summary>
        /// ?取?足?件的?体?量
        /// </summary>
        /// <param name="predicate">查??件</param>
        /// <returns>?体?量</returns>
        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.CountAsync(predicate);
        }

        /// <summary>
        /// 分?查?
        /// </summary>
        /// <param name="pageIndex">?面索引（?0?始）</param>
        /// <param name="pageSize">?面大小</param>
        /// <param name="predicate">查??件（可?）</param>
        /// <param name="orderBy">排序表?式（可?）</param>
        /// <returns>分??果</returns>
        public virtual async Task<PagedResult<T>> GetPagedAsync(int pageIndex, int pageSize,
            Expression<Func<T, bool>>? predicate = null,
            Expression<Func<T, object>>? orderBy = null)
        {
            var query = _dbSet.AsQueryable();

            // ?用???件
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // ?取??
            var totalCount = await query.CountAsync();

            // ?用排序
            if (orderBy != null)
            {
                query = query.OrderBy(orderBy);
            }

            // ?用分?
            var items = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        #endregion

        #region 修改方法??

        /// <summary>
        /// 添加?体
        /// </summary>
        /// <param name="entity">要添加的?体</param>
        /// <returns>添加的?体</returns>
        public virtual async Task<T> AddAsync(T entity)
        {
            var result = await _dbSet.AddAsync(entity);
            return result.Entity;
        }

        /// <summary>
        /// 批量添加?体
        /// </summary>
        /// <param name="entities">要添加的?体集合</param>
        /// <returns>添加的?体集合</returns>
        public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            return entities;
        }

        /// <summary>
        /// 更新?体
        /// </summary>
        /// <param name="entity">要更新的?体</param>
        /// <returns>更新后的?体</returns>
        public virtual async Task<T> UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            return await Task.FromResult(entity);
        }

        /// <summary>
        /// 批量更新?体
        /// </summary>
        /// <param name="entities">要更新的?体集合</param>
        /// <returns>更新后的?体集合</returns>
        public virtual async Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.UpdateRange(entities);
            return await Task.FromResult(entities);
        }

        /// <summary>
        /// ?除?体
        /// </summary>
        /// <param name="entity">要?除的?体</param>
        public virtual async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 根据ID?除?体
        /// </summary>
        /// <param name="id">?体ID</param>
        public virtual async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                await DeleteAsync(entity);
            }
        }

        /// <summary>
        /// 批量?除?体
        /// </summary>
        /// <param name="entities">要?除的?体集合</param>
        public virtual async Task DeleteRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 根据?件?除?体
        /// </summary>
        /// <param name="predicate">?除?件</param>
        public virtual async Task DeleteRangeAsync(Expression<Func<T, bool>> predicate)
        {
            var entities = await FindAsync(predicate);
            await DeleteRangeAsync(entities);
        }

        #endregion

        #region 事?方法??

        /// <summary>
        /// 保存所有更改
        /// </summary>
        /// <returns>受影?的行?</returns>
        public virtual async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        #endregion
    }
}