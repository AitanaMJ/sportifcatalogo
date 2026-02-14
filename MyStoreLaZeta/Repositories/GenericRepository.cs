using MyStoreLaZeta.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Linq.Expressions;

namespace MyStoreLaZeta.Repositories
{
    public class GenericRepository<TEntity>(AppDbContext _dbContext) where TEntity : class
    {
        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
           return await  _dbContext.Set<TEntity>().ToListAsync();
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>>[]? conditions = null,
            Expression<Func<TEntity,object>>[]? includes = null
            )
        {
            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (conditions is not null)
                foreach (var condition in conditions) query = query.Where(condition);
            if (includes is not null)

            foreach (var include in includes) query = query.Include(include); 

            return await query.ToListAsync();
        }

        public async Task AddAsync(TEntity entity)
        {
            await _dbContext.Set<TEntity>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<TEntity?> GetByIdAsync(int entityid)
        {
            return await _dbContext.Set<TEntity>().FindAsync(entityid);
        }

        public async Task EditAsync(TEntity entity)
        {
            _dbContext.Set<TEntity>().Update(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(TEntity entity)
        {
            _dbContext.Set<TEntity>().Remove(entity);
            await _dbContext.SaveChangesAsync();
        }
    }
}
