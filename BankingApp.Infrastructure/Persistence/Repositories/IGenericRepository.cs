using System.Data;
using System.Linq.Expressions;

namespace BankingApp.Infrastructure.Persistence.Repositories
{
    public interface IGenericRepository<TEntity> where TEntity : class
    {
        ICollection<TEntity> GetAll();
        IEnumerable<TEntity> Get(
        Expression<Func<TEntity, bool>> filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        string includeProperties = "");
        TEntity GetByID(object id);
        void Insert(TEntity entity);
        void InsertMany(List<TEntity> entities);
        void Delete(object id);
        void Delete(TEntity entityToDelete);
        void Update(TEntity entityToUpdate);
        void UpdateRange(List<TEntity> entitiesToUpdate);
        int Count(Expression<Func<TEntity, bool>> predicate, bool includeDeleted = false);

        IQueryable<TEntity> Table { get; }
        IQueryable<TEntity> TableNoTracking { get; }




        void BeginTransaction();
        //void BeginTransaction(IsolationLevel isolationLevel);
        void Commit();
        void RollBack();
        Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);
        IQueryable<TEntity> Queryable();

    }
}
