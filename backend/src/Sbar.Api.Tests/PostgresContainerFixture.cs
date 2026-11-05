using Testcontainers.PostgreSql;
using Xunit;

namespace Sbar.Api.Tests;

// Um único container Postgres, subido uma vez para toda a suíte de Sbar.Api.Tests (mais rápido que
// um container por classe de teste). Cada classe de teste isola seus dados criando/derrubando o
// próprio banco lógico dentro desse container — ver ApiTestFactory.
public class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("postgres")
        .WithUsername("sbar")
        .WithPassword("sbar")
        .Build();

    public string ConnectionStringAdmin => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresContainerFixture>;
