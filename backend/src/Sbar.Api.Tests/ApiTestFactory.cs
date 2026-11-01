using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Sbar.Api.Tests;

// Cria um WebApplicationFactory<Program> com um banco Postgres isolado (dados só dessa instância),
// dentro do container Postgres compartilhado da suíte (PostgresContainerFixture). Cada classe de
// teste chama IniciarAsync() em IAsyncLifetime.InitializeAsync() e DisposeAsync() no fim.
public sealed class ApiTestFactory : IAsyncDisposable
{
    private readonly string _connectionStringAdmin;
    private string? _nomeBanco;
    public string NomeDoBanco => _nomeBanco!;

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public ApiTestFactory(string connectionStringAdmin) => _connectionStringAdmin = connectionStringAdmin;

    public async Task IniciarAsync()
    {
        _nomeBanco = $"sbar_test_{Guid.NewGuid():N}";

        await using (var conexaoAdmin = new NpgsqlConnection(_connectionStringAdmin))
        {
            await conexaoAdmin.OpenAsync();
            await using var comando = new NpgsqlCommand($"""CREATE DATABASE "{_nomeBanco}" """, conexaoAdmin);
            await comando.ExecuteNonQueryAsync();
        }

        var connectionStringDoTeste = new NpgsqlConnectionStringBuilder(_connectionStringAdmin) { Database = _nomeBanco }.ConnectionString;

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Sbar"] = connectionStringDoTeste,
                ["Sbar:CodigoDeConvite"] = CodigoDeConvite,
                ["Sbar:EmailDoDonoDoLegado"] = EmailDoDonoDoLegado,
            })));

        // CreateClient() dispara o startup do host — inclusive o Database.Migrate() automático
        // que o próprio Program.cs roda antes de app.Run() (mesmo caminho usado em produção).
        Client = Factory.CreateClient();

        // O cliente padrão já entra como um usuário comum: os testes de regra de negócio seguem como antes.
        await CadastrarAsync(Client, EmailPadrao);
    }

    public const string CodigoDeConvite = "convite-de-teste";
    public const string EmailPadrao = "usuario@sbar.test";
    public const string EmailDoDonoDoLegado = "legado@sbar.test";
    public const string SenhaDeTeste = "Senha1234";

    // Cliente novo (cookie próprio) já logado numa conta nova — para testar isolamento entre usuários.
    public async Task<HttpClient> CriarClienteAutenticadoAsync(string email)
    {
        var cliente = Factory.CreateClient();
        await CadastrarAsync(cliente, email);

        return cliente;
    }

    // Cliente novo, sem sessão.
    public HttpClient CriarClienteAnonimo() => Factory.CreateClient();

    private static async Task CadastrarAsync(HttpClient cliente, string email)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/account/register", new { email, senha = SenhaDeTeste, codigoDeConvite = CodigoDeConvite });
        if (!resposta.IsSuccessStatusCode)
            throw new HttpRequestException($"Cadastro de {email} falhou ({(int)resposta.StatusCode}): {await resposta.Content.ReadAsStringAsync()}");
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();

        if (_nomeBanco is null)
            return;

        await using var conexaoAdmin = new NpgsqlConnection(_connectionStringAdmin);
        await conexaoAdmin.OpenAsync();
        await using var comando = new NpgsqlCommand($"""DROP DATABASE IF EXISTS "{_nomeBanco}" WITH (FORCE)""", conexaoAdmin);
        await comando.ExecuteNonQueryAsync();
    }
}
