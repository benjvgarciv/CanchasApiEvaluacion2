using CanchasApi.Models;

namespace CanchasApi.Repositories
{
    public interface IUsuarioRepository : IRepository<Usuario>
    {
        Task<bool> ExisteEmailAsync(string email);
        Task<Usuario?> GetByEmailAsync(string email);
    }
}
