using CanchasApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CanchasApi.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private static readonly Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot _dbRoot = new();

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "clave-de-pruebas-solo-para-tests-de-integracion-1234567890",
                    ["Jwt:Issuer"] = "CanchasApi.Tests",
                    ["Jwt:Audience"] = "CanchasApi.Tests"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remover cualquier configuración previa de DbContext
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ReservasCanchasDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType.Name.Contains("DbContextOptions") ||
                    d.ServiceType == typeof(ReservasCanchasDbContext)).ToList();

                foreach (var descriptor in descriptors)
                    services.Remove(descriptor);

                // Configurar InMemoryDatabase con raíz estática compartida para persistir datos entre peticiones del mismo test
                services.AddDbContext<ReservasCanchasDbContext>(options =>
                {
                    options.UseInMemoryDatabase("CanchasApiSharedTestDb", _dbRoot);
                });
            });
        }
    }
}
