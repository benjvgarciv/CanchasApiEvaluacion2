using CanchasApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CanchasApi.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
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
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ReservasCanchasDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(ReservasCanchasDbContext)).ToList();

                foreach (var descriptor in descriptors)
                    services.Remove(descriptor);

                var inMemoryProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.AddDbContext<ReservasCanchasDbContext>(options =>
                {
                    options.UseInMemoryDatabase("CanchasApiTestsDb");
                    options.UseInternalServiceProvider(inMemoryProvider);
                });

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ReservasCanchasDbContext>();
                db.Database.EnsureCreated();
            });
        }
    }
}
