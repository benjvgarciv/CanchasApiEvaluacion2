using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CanchasApi.Tests
{
    public class UsuarioApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public UsuarioApiTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Registro_ConDatosValidos_Retorna201YUbicacionDelRecurso()
        {
            var request = new
            {
                Nombre = "Ana Test",
                Email = $"ana.{Guid.NewGuid():N}@test.com",
                Password = "ClaveSegura123!",
                Rol = (string?)null
            };

            var response = await _client.PostAsJsonAsync("/api/v1/usuarios/registro", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Contains("/api/v1/usuarios/", response.Headers.Location!.ToString());

            var body = await response.Content.ReadFromJsonAsync<UsuarioResponseDto>();
            Assert.NotNull(body);
            Assert.Equal("Cliente", body!.Rol);
        }

        [Fact]
        public async Task Registro_ConEmailDuplicado_Retorna409Conflict()
        {
            var email = $"dup.{Guid.NewGuid():N}@test.com";
            var request = new { Nombre = "Usuario Uno", Email = email, Password = "ClaveSegura123!", Rol = (string?)null };

            await _client.PostAsJsonAsync("/api/v1/usuarios/registro", request);
            var segundaRespuesta = await _client.PostAsJsonAsync("/api/v1/usuarios/registro", request);

            Assert.Equal(HttpStatusCode.Conflict, segundaRespuesta.StatusCode);
        }

        [Fact]
        public async Task Registro_SinPassword_Retorna400BadRequest()
        {
            var request = new { Nombre = "Sin Password", Email = "sinpass@test.com", Password = "", Rol = (string?)null };

            var response = await _client.PostAsJsonAsync("/api/v1/usuarios/registro", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_ConCredencialesValidas_Retorna200YToken()
        {
            var email = $"login.{Guid.NewGuid():N}@test.com";
            var password = "ClaveSegura123!";
            await _client.PostAsJsonAsync("/api/v1/usuarios/registro",
                new { Nombre = "Login Test", Email = email, Password = password, Rol = (string?)null });

            var response = await _client.PostAsJsonAsync("/api/v1/usuarios/login", new { Email = email, Password = password });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            Assert.False(string.IsNullOrWhiteSpace(body?.Token));
        }

        [Fact]
        public async Task Login_ConCredencialesInvalidas_Retorna401Unauthorized()
        {
            var response = await _client.PostAsJsonAsync("/api/v1/usuarios/login",
                new { Email = "no-existe@test.com", Password = "loquesea" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ListarUsuarios_SinAutenticacion_Retorna401Unauthorized()
        {
            var response = await _client.GetAsync("/api/v1/usuarios");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private record UsuarioResponseDto(int Id, string Nombre, string Email, string Rol, DateTime FechaCreacion);
        private record LoginResponseDto(string Token, UsuarioResponseDto Usuario);
    }
}
