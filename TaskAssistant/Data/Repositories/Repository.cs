using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace TaskAssistant.Data.Repositories
{
    /// <summary>
    /// �q��?????
    /// ���Ѱ򥻪�CRUD�ާ@�M�d?�\��
    /// </summary>
    /// <typeparam name="T">?�^?��</typeparam>
    public class Repository<T> : IRepository<T> where T : class
    {
        #region �p���r�q

        /// <summary>
        /// ?�u?�W�U��
        /// </summary>
        protected readonly AppDbContext _context;

        /// <summary>
        /// ?�^?�u��
        /// </summary>
        protected readonly DbSet<T> _dbSet;

        #endregion

        #region �۳y��?

        /// <summary>
        /// ��l��??
        /// </summary>
        /// <param name="context">?�u?�W�U��</param>
        public Repository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        #endregion

        #region �d?��k??

        /// <summary>
        /// ���uID?��?�^
        /// </summary>
        /// <param name="id">?�^ID</param>
        /// <returns>?�^?�H�A�p�G���s�b?��^null</returns>
        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <summary>
        /// ?���Ҧ�?�^
        /// </summary>
        /// <returns>�Ҧ�?�^�����X</returns>
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        /// <summary>
        /// ���u?��d��?�^
        /// </summary>
        /// <param name="predicate">�d??��</param>
        /// <returns>?��?��?�^���X</returns>
        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        /// <summary>
        /// ���u?��d��???�^
        /// </summary>
        /// <param name="predicate">�d??��</param>
        /// <returns>?��?�󪺲Ĥ@??�^�A�p�G���s�b?��^null</returns>
        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        /// <summary>
        /// ?�d�O�_�s�b?��?��?�^
        /// </summary>
        /// <param name="predicate">�d??��</param>
        /// <returns>�p�G�s�b?��^true�A�_?��^false</returns>
        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        /// <summary>
        /// ?��?��?��?�^?�q
        /// </summary>
        /// <param name="predicate">�d??��</param>
        /// <returns>?�^?�q</returns>
        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.CountAsync(predicate);
        }

        /// <summary>
        /// ��?�d?
        /// </summary>
        /// <param name="pageIndex">?�����ޡ]?0?�l�^</param>
        /// <param name="pageSize">?���j�p</param>
        /// <param name="predicate">�d??��]�i?�^</param>
        /// <param name="orderBy">�ƧǪ�?���]�i?�^</param>
        /// <returns>��??�G</returns>
        public virtual async Task<PagedResult<T>> GetPagedAsync(int pageIndex, int pageSize,
            Expression<Func<T, bool>>? predicate = null,
            Expression<Func<T, object>>? orderBy = null)
        {
            var query = _dbSet.AsQueryable();

            // ?��???��
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // ?��??
            var totalCount = await query.CountAsync();

            // ?�αƧ�
            if (orderBy != null)
            {
                query = query.OrderBy(orderBy);
            }

            // ?�Τ�?
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

        #region �ק��k??

        /// <summary>
        /// �K�[?�^
        /// </summary>
        /// <param name="entity">�n�K�[��?�^</param>
        /// <returns>�K�[��?�^</returns>
        public virtual async Task<T> AddAsync(T entity)
        {
            var result = await _dbSet.AddAsync(entity);
            return result.Entity;
        }

        /// <summary>
        /// ��q�K�[?�^
        /// </summary>
        /// <param name="entities">�n�K�[��?�^���X</param>
        /// <returns>�K�[��?�^���X</returns>
        public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            return entities;
        }

        /// <summary>
        /// ��s?�^
        /// </summary>
        /// <param name="entity">�n��s��?�^</param>
        /// <returns>��s�Z��?�^</returns>
        public virtual async Task<T> UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            return await Task.FromResult(entity);
        }

        /// <summary>
        /// ��q��s?�^
        /// </summary>
        /// <param name="entities">�n��s��?�^���X</param>
        /// <returns>��s�Z��?�^���X</returns>
        public virtual async Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.UpdateRange(entities);
            return await Task.FromResult(entities);
        }

        /// <summary>
        /// ?��?�^
        /// </summary>
        /// <param name="entity">�n?����?�^</param>
        public virtual async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await Task.CompletedTask;
        }

        /// <summary>
        /// ���uID?��?�^
        /// </summary>
        /// <param name="id">?�^ID</param>
        public virtual async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                await DeleteAsync(entity);
            }
        }

        /// <summary>
        /// ��q?��?�^
        /// </summary>
        /// <param name="entities">�n?����?�^���X</param>
        public virtual async Task DeleteRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
            await Task.CompletedTask;
        }

        /// <summary>
        /// ���u?��?��?�^
        /// </summary>
        /// <param name="predicate">?��?��</param>
        public virtual async Task DeleteRangeAsync(Expression<Func<T, bool>> predicate)
        {
            var entities = await FindAsync(predicate);
            await DeleteRangeAsync(entities);
        }

        #endregion

        #region ��?��k??

        /// <summary>
        /// �O�s�Ҧ����
        /// </summary>
        /// <returns>���v?����?</returns>
        public virtual async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        #endregion
    }
}