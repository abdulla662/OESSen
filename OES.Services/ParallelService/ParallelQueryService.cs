using Microsoft.EntityFrameworkCore;
using OES.Helper.Dtos.Sync;
using OES.Helper.General;
using OES.Infrastructure.Contexts;
using OES.Interface.Repository;
using OES.Interface.UnitOfWork;
using OES.Services.Repository;
using SharedHelper.General;

namespace OES.Services.ParallelService
{
    public class ParallelQueryService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly FilterParamsValues _filterParamsValues;

        public ParallelQueryService(IDbContextFactory<AppDbContext> contextFactory, FilterParamsValues filterParamsValues)
        {
            _contextFactory = contextFactory;
            _filterParamsValues = filterParamsValues;
        }

        public async Task<TResult> ExecuteReadAsync<TEntity, TKey, TResult>(Func<IRepository<TEntity, TKey>, Task<TResult>> operation) where TEntity : class where TKey : struct
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var repository = new RepositoryService<TEntity, TKey>(context);

            return await operation(repository);
        }

        public async Task<TResult> ExecuteWriteAsync<TResult>(
            Func<IUnitOfWork, Task<TResult>> operation,
            AuditContextDto audit = null
        )
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var resolvedAudit = audit ?? AuditContextDto.FromHttpContext(_filterParamsValues);

            context.ExplicitAudit = (
                resolvedAudit.User ?? DefaultSystemUser.Name,
                resolvedAudit.OrganizationId,
                resolvedAudit.OrganizationSignature ?? DefaultSystemUser.Name,
                DateTimeHelper.Now
            );

            var unitOfWork = new UnitOfWork.UnitOfWork(context);

            return await operation(unitOfWork);
        }
    }
}