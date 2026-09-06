using CanchasApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CanchasApi.Repositories
{
    public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
    {
        public UsuarioRepository(ReservasCanchasDbContext db) : base(db)
        {
        }

        public async Task<bool> ExisteEmailAsync(string email) =>
            await DbSet.AnyAsync(u => u.Email == email);

        public async Task<Usuario?> GetByEmailAsync(string email) =>
            await DbSet.FirstOrDefaultAsync(u => u.Email == email);
    }
}
