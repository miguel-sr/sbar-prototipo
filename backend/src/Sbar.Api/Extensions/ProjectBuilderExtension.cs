using Microsoft.EntityFrameworkCore;
using Sbar.Api.Exportacao;
using Sbar.Application.CasosDeUso;
using Sbar.Application.Calculadoras;
using Sbar.Domain.Interfaces.Repositories;
using Sbar.Domain.Interfaces.Services;
using Sbar.Infrastructure;
using Sbar.Infrastructure.Repositories;

namespace Sbar.Api.Extensions;

public static class ProjectBuilderExtension
{
    public static IServiceCollection BuildProject(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<SbarDbContext>(o => o.UseNpgsql(config.GetConnectionString("Sbar")));

        services.AddScoped<IEstudoDeCasoRepository, EstudoDeCasoRepository>();
        services.AddScoped<IPerfilArquiteturalRepository, PerfilArquiteturalRepository>();
        services.AddScoped<IConfiguracaoDeEixoRepository, ConfiguracaoDeEixoRepository>();
        services.AddScoped<IVersaoDeParametrosRepository, VersaoDeParametrosRepository>();
        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();

        services.AddScoped<IServicoDeCasos, ServicoDeCasos>();
        services.AddScoped<IServicoDeNos, ServicoDeNos>();
        services.AddScoped<IServicoDeArestas, ServicoDeArestas>();
        services.AddScoped<IServicoDeFluxos, ServicoDeFluxos>();
        services.AddScoped<IServicoDeMetricas, ServicoDeMetricas>();
        services.AddScoped<IServicoDePerfis, ServicoDePerfis>();
        services.AddScoped<IServicoDeParametros, ServicoDeParametros>();
        services.AddScoped<MontadorDeExportacao>();
        services.AddScoped<IServicoDeRastreabilidade, ServicoDeRastreabilidade>();

        services.AddScoped<ICalculadoraDeEixo, CalculadoraCO>();
        services.AddScoped<ICalculadoraDeEixo, CalculadoraESC>();
        services.AddScoped<ICalculadoraDeEixo, CalculadoraINT>();
        services.AddScoped<ICalculadoraDeEixo, CalculadoraUX>();
        services.AddScoped<ICalculadoraDeEixo, CalculadoraDISP>();
        services.AddScoped<ICalculadoraDeEixo, CalculadoraFLEX>();
        services.AddScoped<ICalculadoraDeEixo, CalculadoraTR>();
        services.AddScoped<ICalculadoraDeEixo, CalculadoraAUT>();
        services.AddScoped<ICalculadoraDeEixo, CalculadoraVOL>();
        services.AddScoped<ICalculadoraDeEixo, CalculadoraDEP>();

        return services;
    }
}
