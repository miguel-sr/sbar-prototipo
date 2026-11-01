using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sbar.Domain.Entities;
using Sbar.Domain.Validations;

namespace Sbar.Infrastructure.Mappings;

public class FluxoConfiguration : IEntityTypeConfiguration<Fluxo>
{
    public void Configure(EntityTypeBuilder<Fluxo> builder)
    {
        builder.ToTable("Fluxos");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.Nome).IsRequired().HasMaxLength(TextoValidado.TamanhoMaximoDoNomeDoFluxo);
        builder.Property(f => f.Ordem).IsRequired();

        // Passos é uma coleção privada (_passos) exposta só como IReadOnlyList — EF Core lê/escreve direto no campo.
        builder.HasMany(f => f.Passos)
            .WithOne()
            .HasForeignKey(p => p.FluxoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(f => f.Passos)
            .HasField("_passos")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
