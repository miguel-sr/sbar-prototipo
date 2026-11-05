using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Sbar.Api.Identidade;
using Sbar.Domain.Interfaces;
using Sbar.Infrastructure;
using Sbar.Infrastructure.Identidade;

namespace Sbar.Api.Extensions;

public static class ContasDeUsuarioExtension
{
    // PLANO_CONTAS_DE_USUARIO.md 4.1: Identity com cookie HttpOnly, chaves do Data Protection no banco, política
    // padrão exigindo login em todo controller e limite de taxa nas rotas de conta.
    public static IServiceCollection AddContasDeUsuario(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<OpcoesDeConta>(config.GetSection(OpcoesDeConta.Secao));
        var opcoes = config.GetSection(OpcoesDeConta.Secao).Get<OpcoesDeConta>() ?? new OpcoesDeConta();

        services.AddHttpContextAccessor();
        services.AddScoped<IUsuarioAtual, UsuarioAtual>();
        services.AddScoped<ReivindicadorDeCasosLegados>();

        services.AddDataProtection()
            .SetApplicationName("sbar")
            .PersistKeysToDbContext<SbarDbContext>();

        services.AddIdentityCore<Usuario>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 8;
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<SbarDbContext>()
            .AddSignInManager();

        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddCookie(IdentityConstants.ApplicationScheme, o =>
            {
                o.Cookie.Name = "sbar.sessao";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                // SameAsRequest: Secure quando a requisição chegou por HTTPS (inclusive via X-Forwarded-Proto).
                o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                o.ExpireTimeSpan = TimeSpan.FromDays(7);
                o.SlidingExpiration = true;
                // API: sem sessão é 401, nunca um redirecionamento para uma página de login.
                o.Events.OnRedirectToLogin = contexto =>
                {
                    contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                o.Events.OnRedirectToAccessDenied = contexto =>
                {
                    contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy("conta", contexto => RateLimitPartition.GetFixedWindowLimiter(
                contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = opcoes.TentativasPorMinuto, Window = TimeSpan.FromMinutes(1) }));
        });

        services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // O proxy (Caddy) está na mesma máquina/rede Docker, fora das faixas padrão: confia no que vier dele.
            o.KnownNetworks.Clear();
            o.KnownProxies.Clear();
        });

        return services;
    }
}
