using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Sbar.Api.Extensions;
using Sbar.Api.ErrorHandling;
using Sbar.Api.Swagger;
using Sbar.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SBAR API",
        Version = "v1",
        Description = "Service Blueprint Architectural Recommendation — TCC II (UNIALFA, 2026). " +
            "Mapeia o fluxo de negócio de um sistema como um Service Blueprint (Nós/Arestas), calcula " +
            "dez indicadores (Radar Arquitetural) e recomenda um estilo arquitetural comparando o " +
            "perfil calculado com Perfis Arquiteturais de referência. Exige login (cookie de sessão): " +
            "crie a conta em POST /api/account/register e entre em POST /api/account/login."
    });

    var arquivoXml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var caminhoXml = Path.Combine(AppContext.BaseDirectory, arquivoXml);
    if (File.Exists(caminhoXml))
        options.IncludeXmlComments(caminhoXml);

    options.SchemaFilter<EnumSchemaFilter>();
    options.SchemaFilter<ExemploDeRequestSchemaFilter>();
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.BuildProject(builder.Configuration);
builder.Services.AddContasDeUsuario(builder.Configuration);

var app = builder.Build();

// Não há um passo separado de "rodar migration" no deploy (EC2 + Docker Compose, sem
// orquestração gerenciada) — a própria API aplica as migrations pendentes ao subir. Idempotente
// (EF Core não reaplica o que já rodou) e é exatamente o que os testes de integração também
// esperam encontrar num banco novo (Sbar.Api.Tests/ApiTestFactory.cs).
using (var escopoDeInicializacao = app.Services.CreateScope())
    escopoDeInicializacao.ServiceProvider.GetRequiredService<SbarDbContext>().Database.Migrate();

// Disponível em todos os ambientes (não só Development) — é assim que os especialistas
// batem na API antes do front existir (ver docs/projeto/ARQUITETURA.md).
// Protótipo acadêmico, sem dado sensível — mesma decisão de risco aceito já registrada para D4.
app.UseSwagger();
app.UseSwaggerUI();

// Atrás do proxy com TLS (Caddy), a API enxerga HTTP; o X-Forwarded-Proto devolve o esquema original,
// necessário para o cookie sair com Secure (ver docs/projeto/ARQUITETURA.md, seção 6).
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
