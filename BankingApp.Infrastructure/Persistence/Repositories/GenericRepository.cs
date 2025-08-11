
using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;


namespace BankingApp.Infrastructure.Persistence.Repositories
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        internal DbContext dbContext;
        internal DbSet<TEntity> dbSet;

        public GenericRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
            dbSet = dbContext.Set<TEntity>();
        }
        public ICollection<TEntity> GetAll()
        {
            return dbSet.ToList();
        }

        public virtual IEnumerable<TEntity> Get(
            Expression<Func<TEntity, bool>> filter,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
            string includeProperties)
        {
            IQueryable<TEntity> query = dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split
                (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            if (orderBy != null)
            {
                return orderBy(query).ToList();
            }
            else
            {
                return query.ToList();
            }
        }

        public virtual TEntity GetByID(object id)
        {
            return dbSet.Find(id);
        }

        public virtual void Insert(TEntity entity)
        {
            dbSet.Add(entity);
        }
        public virtual void InsertMany(List<TEntity> entities)
        {
            dbSet.AddRange(entities);
        }

        public int Count(Expression<Func<TEntity, bool>> predicate, bool includeDeleted = false)
        {
            var query = dbSet.AsQueryable();

            if (includeDeleted)
            {
                query = query.IgnoreQueryFilters();
            }
            return query.Count(predicate);
        }

        public virtual void Delete(object id)
        {
            TEntity entityToDelete = dbSet.Find(id);
            Delete(entityToDelete);
        }

        public virtual void Delete(TEntity entityToDelete)
        {
            if (dbContext.Entry(entityToDelete).State == EntityState.Detached)
            {
                dbSet.Attach(entityToDelete);
            }
            dbSet.Remove(entityToDelete);
        }

        public virtual void Update(TEntity entityToUpdate)
        {
            dbSet.Attach(entityToUpdate);
            dbContext.Entry(entityToUpdate).State = EntityState.Modified;
        }

        public virtual void UpdateRange(List<TEntity> entitiesToUpdate)
        {
            dbSet.AttachRange(entitiesToUpdate.ToArray());

            foreach (var entity in entitiesToUpdate)
            {
                dbContext.Entry(entity).State = EntityState.Modified;
            }
        }


        public IDbContextTransaction _dbContextTransaction { get; private set; }


        public void BeginTransaction()
        {
            _dbContextTransaction = dbContext.Database.BeginTransaction();
        }

        //public void BeginTransaction(IsolationLevel isolationLevel)
        //{
        //    _dbContextTransaction = dbContext.Database.BeginTransaction(isolationLevel);
        //}



        public async Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await dbSet.FirstOrDefaultAsync(predicate);
        }


        public void Commit()
        {
            if (_dbContextTransaction != null)
            {
                _dbContextTransaction.Commit();
            }
        }

        public void RollBack()
        {
            if (_dbContextTransaction != null)
            {
                _dbContextTransaction.Rollback();
            }
        }

        protected virtual DbSet<TEntity> Entities
        {
            get
            {
                if (dbSet == null)
                    dbSet = dbContext.Set<TEntity>();
                return dbSet;
            }
        }

        public virtual IQueryable<TEntity> Table
        {
            get
            {
                return Entities;
            }
        }

        public virtual IQueryable<TEntity> TableNoTracking
        {
            get
            {
                return Entities.AsNoTracking();
            }
        }

        public virtual IQueryable<TEntity> Queryable()
        {
            return Table;
        }

    }
}
