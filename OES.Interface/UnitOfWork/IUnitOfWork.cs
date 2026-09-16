using Microsoft.EntityFrameworkCore.Storage;
using OES.Interface.Repository;
using System.Data.Common;

namespace OES.Interface.UnitOfWork
{
    public interface IUnitOfWork
    {
        public Task<int> Complete();
        public void Dispose();
        public Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters);
        public DbCommand CreateDbCommand();
        public Task OpenConnectionAsync();
        public Task CloseConnectionAsync();
        public Task<IDbContextTransaction> BeginTransactionAsync();
        public IRepository<TEntity, L> Repository<TEntity, L>() where TEntity : class where L : struct;
        Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(
            string storedProcedureName,
            List<(string Name, object Value)> parameters,
            CancellationToken cancellationToken = default
        ) where TResult : class;
    }
}
