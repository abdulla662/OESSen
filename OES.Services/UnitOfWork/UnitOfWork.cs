using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MySqlConnector;
using OES.Infrastructure.Contexts;
using OES.Interface.Repository;
using OES.Interface.UnitOfWork;
using OES.Services.Repository;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace OES.Services.UnitOfWork
{
    public class UnitOfWork(AppDbContext context) : IUnitOfWork
    {
        private readonly AppDbContext _context = context;
        private Hashtable _repositories;

        public async Task<int> Complete() => await _context.SaveChangesAsync();

        public void Dispose() => _context.Dispose();

        public async Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
        {
            return await _context.Database.ExecuteSqlRawAsync(sql, parameters);
        }

        public DbCommand CreateDbCommand()
        {
            var connection = _context.Database.GetDbConnection();

            var command = connection.CreateCommand();

            return command;
        }

        public async Task OpenConnectionAsync()
        {
            if (_context.Database.GetDbConnection().State != ConnectionState.Open)
            {
                await _context.Database.OpenConnectionAsync();
            }
        }

        public async Task CloseConnectionAsync()
        {
            if (_context.Database.GetDbConnection().State != ConnectionState.Closed)
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public IRepository<TEntity, T> Repository<TEntity, T>() where TEntity : class where T : struct
        {
            _repositories ??= [];

            var type = typeof(TEntity).Name;

            if (!_repositories.ContainsKey(type))
            {
                var repository = new RepositoryService<TEntity, T>(_context);

                _repositories.Add(type, repository);
            }

            return (IRepository<TEntity, T>)_repositories[type]!;
        }

        public async Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(
            string storedProcedureName,
            List<(string Name, object Value)> parameters,
            CancellationToken cancellationToken = default
        ) where TResult : class
        {
            if (string.IsNullOrWhiteSpace(storedProcedureName))
                throw new ArgumentException("Stored procedure name cannot be null or empty", nameof(storedProcedureName));

            if (!Regex.IsMatch(storedProcedureName, @"^[a-zA-Z0-9_]+$"))
                throw new ArgumentException("Invalid stored procedure name", nameof(storedProcedureName));

            var mySqlParameters = parameters?.Select((p, i) =>
            {
                if (string.IsNullOrWhiteSpace(p.Name) || !Regex.IsMatch(p.Name, @"^[a-zA-Z0-9_]+$"))
                    throw new ArgumentException($"Invalid parameter name at index {i}: {p.Name}");

                return new MySqlParameter($"?{p.Name}", p.Value ?? DBNull.Value);
            }).ToArray() ?? [];

            var parameterNames = mySqlParameters.Select(p => p.ParameterName).ToList();
            var commandText = $"CALL {storedProcedureName}({string.Join(", ", parameterNames)})";

            var query = _context.Database.SqlQueryRaw<TResult>(commandText, mySqlParameters);

            return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
