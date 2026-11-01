using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sbar.Domain.Entities;

namespace Sbar.Infrastructure.Mappings;

public class FluxoPassoConfiguration : IEntityTypeConfiguration<FluxoPasso>
{
    public void Configure(EntityTypeBuilder<FluxoPasso> builder)
    {
        builder.ToTable("FluxosPassos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Posicao).IsRequired();

        // NoId NÃO é chave estrangeira para Nos: excluir um Nó não pode apagar nem alterar o fluxo em silêncio —
        // o fluxo passa a ser avaliado como inválido, com a etapa ausente indicada (EstudoDeCaso.AvaliarFluxo).
        builder.Property(p => p.NoId).IsRequired();
        builder.HasIndex(p => p.FluxoId);
    }
}
