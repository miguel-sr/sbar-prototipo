using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sbar.Domain.Entities;

namespace Sbar.Infrastructure.Mappings;

public class ConfiguracaoDeEixoConfiguration : IEntityTypeConfiguration<ConfiguracaoDeEixo>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoDeEixo> builder)
    {
        builder.ToTable("ConfiguracoesDeEixo");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Eixo).HasConversion<string>();

        // Uma configuração por eixo e por usuário. Os padrões do Quadro 6 vivem em código (PadroesDoSistema) e são
        // copiados para cada conta; as linhas sem dono são o legado anterior às contas.
        builder.HasIndex(c => new { c.DonoId, c.Eixo }).IsUnique();
    }
}
