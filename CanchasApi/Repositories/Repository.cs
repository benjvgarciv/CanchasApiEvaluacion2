using CanchasApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CanchasApi.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ReservasCanchasDbContext Db;
        protected readonly DbSet<T> DbSet;

        public Repository(ReservasCanchasDbContext db)
        {
            Db = db;
            DbSet = db.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id) => await DbSet.FindAsync(id);

        public async Task<List<T>> GetAllAsync() => await DbSet.ToListAsync();

        public async Task AddAsync(T entity) => await DbSet.AddAsync(entity);

        public void Update(T entity) => DbSet.Update(entity);

        public void Remove(T entity) => DbSet.Remove(entity);

        public async Task<int> SaveChangesAsync() => await Db.SaveChangesAsync();
    }
}
