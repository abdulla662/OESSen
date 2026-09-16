using System.Linq.Expressions;

namespace OES.Interface.Repository
{
    public interface IRepository<TEntity, T> where TEntity : class where T : struct
    {
        IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> Filter = null,
                                   Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> OrderBy = null,
                                   string Including = null);
        Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> Filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> OrderBy = null, string Including = null, bool asNoTracking = false);
        Task<TEntity> GetByIdAsync(T Id);
        Task<TEntity> GetObjAsync(Expression<Func<TEntity, bool>> Filter = null, string Including = null);
        Task<bool> IsExistAsync(Expression<Func<TEntity, bool>> Filter);
        Task AddAsync(TEntity Obj);
        Task AddRangeAsync(List<TEntity> entities);
        void AddRangAsync(List<TEntity> Objs);
        void Update(TEntity Obj);
        void UpdateWithTracking(TEntity Obj);
        void UpdateRange(List<TEntity> Obj);
        void Delete(TEntity Obj);
        void SoftDelete(TEntity Obj);
        void SoftDelete(TEntity Obj, bool ApplyIncludes, params string[] includesList);
        void SoftDeleteRecursive(TEntity entity);
        void SoftDeleteRange(IEnumerable<TEntity> entities);
        void DeleteRange(IEnumerable<TEntity> entities);
        void Activate(TEntity Obj);
        void Deactivate(TEntity Obj);
        void AddRange(IEnumerable<TEntity> entities);
        IQueryable<TEntity> Query(bool applySignature = true, bool applyOrganizationIdFilter = true, string signature = null);
    }
}
