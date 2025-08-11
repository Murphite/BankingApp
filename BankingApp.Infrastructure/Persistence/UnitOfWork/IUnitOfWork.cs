

using BankingApp.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BankingApp.Infrastructure.Persistence.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        void Save();
        Task<int> SaveAsync(CancellationToken cancellationToken = default);

        IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : class;
        IDatabaseTransaction BeginTransaction();
    }


    public interface IUnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
    {
        TContext Context { get; }
    }


    public interface IDatabaseTransaction : IDisposable
    {
        void Commit();

        void Rollback();
    }
}
