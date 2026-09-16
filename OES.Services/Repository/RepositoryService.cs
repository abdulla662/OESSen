using Microsoft.EntityFrameworkCore;
using OES.Infrastructure.Contexts;
using OES.Interface.Repository;
using System.Linq.Expressions;
using System.Reflection;
using SharedHelper.General;

namespace OES.Services.Repository
{
    public class RepositoryService<TEntity, L> : IRepository<TEntity, L> where TEntity : class where L : struct
    {
        private readonly AppDbContext _context;

        internal DbSet<TEntity> _entity;

        public RepositoryService(AppDbContext context)
        {
            _context = context;
            _entity = context.Set<TEntity>();
        }

        public async Task AddAsync(TEntity Obj)
        {
            await _entity.AddAsync(Obj);
        }

        public async Task AddRangeAsync(List<TEntity> entities)
        {
            await _context.Set<TEntity>().AddRangeAsync(entities);
        }

        public void Delete(TEntity Obj)
        {
            _entity.Remove(Obj);
        }

        public void SoftDelete(TEntity Obj)
        {
            typeof(TEntity).GetProperty("IsDeleted")?.SetValue(Obj, true);
            typeof(TEntity).GetProperty("IsActive")?.SetValue(Obj, false);
        }

        public void DeleteRange(IEnumerable<TEntity> entities)
        {
            _entity.RemoveRange(entities);
        }

        public void AddRange(IEnumerable<TEntity> entities)
        {
            _context.Set<TEntity>().AddRange(entities);
        }

        public void SoftDelete(TEntity Obj, bool ApplyIncludes, params string[] includesList)
        {
            Obj.GetType().GetProperty("IsDeleted")?.SetValue(Obj, true);
            Obj.GetType().GetProperty("IsActive")?.SetValue(Obj, true);

            if (ApplyIncludes && includesList != null && includesList.Any())
            {
                foreach (var include in includesList)
                {
                    var _Property = Obj.GetType().GetProperty(include);

                    if (_Property != null)
                    {
                        var includedEntities = _Property.GetValue(Obj);

                        if (includedEntities is IEnumerator<object>)
                        {
                            List<object> includedList = (List<object>)includedEntities;
                            foreach (var relatedEntity in includedList)
                            {
                                relatedEntity.GetType().GetProperty("IsDeleted")?.SetValue(relatedEntity, true);
                                relatedEntity.GetType().GetProperty("IsActive")?.SetValue(relatedEntity, true);
                            }
                        }
                        else if (includedEntities != null)
                        {
                            includedEntities.GetType().GetProperty("IsDeleted")?.SetValue(includedEntities, true);
                            includedEntities.GetType().GetProperty("IsActive")?.SetValue(includedEntities, true);
                        }
                    }
                }
            }
        }

        public void Activate(TEntity Obj)
        {
            typeof(TEntity).GetProperty("IsDeleted")?.SetValue(Obj, false);
            typeof(TEntity).GetProperty("IsActive")?.SetValue(Obj, true);
        }

        public void Deactivate(TEntity Obj) => typeof(TEntity).GetProperty("IsActive")?.SetValue(Obj, false);

        public IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> Filter = null,
                                          Func<IQueryable<TEntity>,
                                          IOrderedQueryable<TEntity>> OrderBy = null,
                                          string Including = null)
        {
            var filterParamsValues = _context.FilterParamsValues;

            IQueryable<TEntity> query = _entity;

            if (filterParamsValues != null && filterParamsValues.Authorize && filterParamsValues.ApplyFilter)
            {
                if (filterParamsValues.ApplyShowDeletedFilter)
                {
                    query = query.Where(e => EF.Property<bool>(e, "IsDeleted"));
                }

                if (filterParamsValues.ApplyIsActiveFilter)
                {
                    query = query.Where(e => EF.Property<bool>(e, "IsActive") == filterParamsValues.IsActive);
                }

                if (filterParamsValues.ApplyOrganizationIdFilter)
                {
                    var organizationId = typeof(TEntity).GetProperty("OrganizationId");

                    if (organizationId != null)
                    {
                        query = query.Where(e => EF.Property<long>(e, "OrganizationId") == filterParamsValues.OrganizationId || EF.Property<long>(e, "OrganizationId") == 0 /* To get system seeded data */);
                    }
                }

                if (filterParamsValues.ApplySignatureFilter)
                {
                    var signature = typeof(TEntity).GetProperty("OrganizationSignature");

                    if (signature != null && typeof(TEntity).Name != "Country" && typeof(TEntity).Name != "City")
                    {
                        query = query.Where(e => EF.Property<string>(e, "OrganizationSignature") == filterParamsValues.Signature || EF.Property<string>(e, "OrganizationSignature") == null /* To get system seeded data */);
                    }
                }
            }

            if (Filter != null)
            {
                query = query.Where(Filter);
            }

            if (Including != null)
            {
                foreach (var item in Including.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(item);
                }
            }

            if (OrderBy != null)
            {
                return OrderBy(query);
            }

            return query;
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> Filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> OrderBy = null, string Including = null, bool asNoTracking = false)
        {
            IQueryable<TEntity> query = _entity;

            var FilterParamssValues = _context.FilterParamsValues;

            if (FilterParamssValues != null && FilterParamssValues.Authorize && FilterParamssValues.ApplyFilter)
            {
                if (FilterParamssValues.ApplyShowDeletedFilter)
                {
                    query = query.Where(e => EF.Property<bool>(e, "IsDeleted"));
                }

                if (FilterParamssValues.ApplyIsActiveFilter)
                {
                    query = query.Where(e => EF.Property<bool>(e, "IsActive") == FilterParamssValues.IsActive);
                }

                if (FilterParamssValues.ApplyOrganizationIdFilter)
                {
                    var organizationId = typeof(TEntity).GetProperty("OrganizationId");

                    if (organizationId != null)
                    {
                        query = query.Where(e => EF.Property<long>(e, "OrganizationId") == FilterParamssValues.OrganizationId || EF.Property<long>(e, "OrganizationId") == 0 /* To get system seeded data */);
                    }
                }

                if (FilterParamssValues.ApplySignatureFilter)
                {
                    var signature = typeof(TEntity).GetProperty("OrganizationSignature");

                    if (signature != null && typeof(TEntity).Name != "Country" && typeof(TEntity).Name != "City")
                    {
                        query = query.Where(e => EF.Property<string>(e, "OrganizationSignature") == FilterParamssValues.Signature || EF.Property<string>(e, "OrganizationSignature") == null /* To get system seeded data */);
                    }
                }
            }

            if (Filter != null)
            {
                query = query.Where(Filter);
            }

            if (Including != null)
            {
                foreach (var item in Including.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(item);
                }
            }

            if (OrderBy != null)
            {
                return await OrderBy(query).ToListAsync();
            }

            if (asNoTracking) // WARNING: It's very risky to let the asNoTracking be true by default, because it can cause bugs in more than one place.
            {
                query = query.AsNoTracking();
            }

            return await query.ToListAsync();
        }

        public async Task<TEntity> GetByIdAsync(L Id)
        {
            return await _entity.FindAsync(Id);
        }

        public async Task<bool> IsExistAsync(Expression<Func<TEntity, bool>> Filter)
        {
            var filterParamsValues = _context.FilterParamsValues;

            IQueryable<TEntity> query = _entity;

            if (filterParamsValues != null && filterParamsValues.Authorize && filterParamsValues.ApplyFilter)
            {
                if (filterParamsValues.ApplyShowDeletedFilter)
                {
                    query = query.Where(e => EF.Property<bool>(e, "IsDeleted"));
                }

                if (filterParamsValues.ApplyIsActiveFilter)
                {
                    query = query.Where(e => EF.Property<bool>(e, "IsActive") == filterParamsValues.IsActive);
                }

                if (filterParamsValues.ApplyOrganizationIdFilter)
                {
                    var organizationId = typeof(TEntity).GetProperty("OrganizationId");

                    if (organizationId != null)
                    {
                        query = query.Where(e => EF.Property<long>(e, "OrganizationId") == filterParamsValues.OrganizationId || EF.Property<long>(e, "OrganizationId") == 0 /* To get system seeded data */);
                    }
                }

                if (filterParamsValues.ApplySignatureFilter)
                {
                    var signature = typeof(TEntity).GetProperty("OrganizationSignature");

                    if (signature != null && typeof(TEntity).Name != "Country" && typeof(TEntity).Name != "City")
                    {
                        query = query.Where(e => EF.Property<string>(e, "OrganizationSignature") == filterParamsValues.Signature || EF.Property<string>(e, "OrganizationSignature") == null /* To get system seeded data */);
                    }
                }
            }

            return await query.AnyAsync(Filter);
        }

        public async Task<TEntity> GetObjAsync(Expression<Func<TEntity, bool>> Filter = null, string Including = null)
        {
            var filterParamsValues = _context.FilterParamsValues;

            IQueryable<TEntity> query = _entity;

            if (filterParamsValues != null && filterParamsValues.Authorize && filterParamsValues.ApplyFilter)
            {
                if (filterParamsValues.ApplyShowDeletedFilter)
                {
                    query = query.Where(e => EF.Property<bool>(e, "IsDeleted"));
                }

                if (filterParamsValues.ApplyIsActiveFilter)
                {
                    query = query.Where(e => EF.Property<bool>(e, "IsActive") == filterParamsValues.IsActive);
                }

                if (filterParamsValues.ApplyOrganizationIdFilter)
                {
                    var organizationId = typeof(TEntity).GetProperty("OrganizationId");

                    if (organizationId != null)
                    {
                        query = query.Where(e => EF.Property<long>(e, "OrganizationId") == filterParamsValues.OrganizationId || EF.Property<long>(e, "OrganizationId") == 0 /* To get system seeded data */);
                    }
                }

                if (filterParamsValues.ApplySignatureFilter)
                {
                    var signature = typeof(TEntity).GetProperty("OrganizationSignature");

                    if (signature != null && typeof(TEntity).Name != "Country" && typeof(TEntity).Name != "City")
                    {
                        query = query.Where(e => EF.Property<string>(e, "OrganizationSignature") == filterParamsValues.Signature || EF.Property<string>(e, "OrganizationSignature") == null /* To get system seeded data */);
                    }
                }
            }

            if (Filter != null)
            {
                query = query.Where(Filter);
            }

            if (Including != null)
            {
                foreach (var item in Including.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(item);
                }
            }

            return await query.FirstOrDefaultAsync();
        }

        public void Update(TEntity Obj)
        {
            _context.ChangeTracker.Clear();

            _context.Entry(Obj).State = EntityState.Modified;
        }

        public void UpdateWithTracking(TEntity Obj)
        {
            _context.Entry(Obj).State = EntityState.Modified;
        }

        public async void AddRangAsync(List<TEntity> Objs)
        {
            await _entity.AddRangeAsync(Objs);
        }

        public void UpdateRange(List<TEntity> Obj)
        {
            _entity.UpdateRange(Obj);
        }

        public IQueryable<TEntity> Query(bool applySignature = true, bool applyOrganizationIdFilter = true, string signature = null)
        {
            IQueryable<TEntity> query = _entity;

            var FilterParamssValues = _context.FilterParamsValues;

            if (FilterParamssValues != null && FilterParamssValues.Authorize && FilterParamssValues.ApplyFilter)
            {
                if (FilterParamssValues.ApplyShowDeletedFilter)
                {
                    query = query.Where(e => EF.Property<bool>(e, "IsDeleted"));
                }

                if (FilterParamssValues.ApplyIsActiveFilter)
                {
                    query = query.Where(e => EF.Property<bool>(e, "IsActive") == FilterParamssValues.IsActive);
                }

                if (FilterParamssValues.ApplyOrganizationIdFilter && applyOrganizationIdFilter)
                {
                    var organizationId = typeof(TEntity).GetProperty("OrganizationId");

                    if (organizationId != null)
                    {
                        query = query.Where(e => EF.Property<long>(e, "OrganizationId") == FilterParamssValues.OrganizationId || EF.Property<long>(e, "OrganizationId") == 0 /* To get system seeded data */);
                    }
                }

                if (FilterParamssValues.ApplySignatureFilter && applySignature)
                {
                    var OrganizationSignature = typeof(TEntity).GetProperty("OrganizationSignature");

                    if (OrganizationSignature != null && typeof(TEntity).Name != "Country" && typeof(TEntity).Name != "City")
                    {
                        query = query.Where(e => EF.Property<string>(e, "OrganizationSignature") == FilterParamssValues.Signature || EF.Property<string>(e, "OrganizationSignature") == null /* To get system seeded data */);
                    }
                }
            }

            if (signature != null)
            {
                var OrganizationSignature = typeof(TEntity).GetProperty("OrganizationSignature");

                if (OrganizationSignature != null && typeof(TEntity).Name != "Country" && typeof(TEntity).Name != "City")
                {
                    query = query.Where(e => EF.Property<string>(e, "OrganizationSignature") == signature);
                }
            }

            return query;
        }

        public void SoftDeleteRecursive(TEntity entity)
        {
            var visited = new HashSet<object>();
            RecursiveSoftDelete(entity, visited);
        }

        public void SoftDeleteRange(IEnumerable<TEntity> entities)
        {
            if (entities == null) return;

            foreach (var entity in entities)
            {
                SetSoftDelete(entity);
            }
        }

        private void RecursiveSoftDelete(object entity, HashSet<object> visited)
        {
            if (entity == null || visited.Contains(entity)) return;

            visited.Add(entity);
            SetSoftDelete(entity);

            foreach (var prop in entity.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop.PropertyType == typeof(string)) continue;

                var propValue = prop.GetValue(entity);

                if (propValue is IEnumerable<object> collection)
                {
                    foreach (var item in collection)
                    {
                        RecursiveSoftDelete(item, visited);
                    }
                }
                else if (propValue != null && !prop.PropertyType.IsValueType)
                {
                    RecursiveSoftDelete(propValue, visited);
                }
            }
        }

        private void SetSoftDelete(object obj)
        {
            obj?.GetType().GetProperty("IsDeleted")?.SetValue(obj, true);
            obj?.GetType().GetProperty("IsActive")?.SetValue(obj, false);
            obj?.GetType().GetProperty("DeletedDate")?.SetValue(obj, DateTimeHelper.Now);
        }
    }
}
