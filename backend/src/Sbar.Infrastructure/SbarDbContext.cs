using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sbar.Domain.Entities;
using Sbar.Domain.Interfaces;
using Sbar.Infrastructure.Identidade;

namespace Sbar.Infrastructure;

// O contexto também guarda as contas (Identity) e as chaves do Data Protection: sem as chaves no banco, o cookie de
// sessão deixaria de valer a cada deploy, porque o container da API é recriado (ver docs/projeto/ARQUITETURA.md).
public class SbarDbContext(DbContextOptions<SbarDbContext> options, IUsuarioAtual? usuarioAtual = null)
    : IdentityUserContext<Usuario, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<EstudoDeCaso> EstudosDeCaso => Set<EstudoDeCaso>();
    public DbSet<PerfilArquitetural> PerfisArquiteturais => Set<PerfilArquitetural>();
    public DbSet<ConfiguracaoDeEixo> ConfiguracoesDeEixo => Set<ConfiguracaoDeEixo>();
    public DbSet<VersaoDeParametros> VersoesDeParametros => Set<VersaoDeParametros>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    // Avaliado a cada consulta (o EF o parametriza): só enxerga os casos do usuário da requisição. Defesa em
    // profundidade — esquecer o filtro numa consulta nova não vaza dado. Sem usuário, não enxerga nenhum caso.
    private Guid? DonoIdAtual => usuarioAtual?.DonoId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SbarDbContext).Assembly);

        modelBuilder.Entity<Usuario>().ToTable("Usuarios");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UsuariosClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UsuariosLogins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UsuariosTokens");

        modelBuilder.Entity<EstudoDeCaso>().HasQueryFilter(e => DonoIdAtual != null && e.DonoId == DonoIdAtual);
        modelBuilder.Entity<ConfiguracaoDeEixo>().HasQueryFilter(c => DonoIdAtual != null && c.DonoId == DonoIdAtual);
        modelBuilder.Entity<PerfilArquitetural>().HasQueryFilter(p => DonoIdAtual != null && p.DonoId == DonoIdAtual);
        modelBuilder.Entity<VersaoDeParametros>().HasQueryFilter(v => DonoIdAtual != null && v.DonoId == DonoIdAtual);
    }
}
