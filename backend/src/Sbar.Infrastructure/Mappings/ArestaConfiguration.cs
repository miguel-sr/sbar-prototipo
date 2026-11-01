using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sbar.Domain.Entities;
using Sbar.Domain.Validations;
using Sbar.Domain.ValueObjects;

namespace Sbar.Infrastructure.Mappings;

public class ArestaConfiguration : IEntityTypeConfiguration<Aresta>
{
    public void Configure(EntityTypeBuilder<Aresta> builder)
    {
        builder.ToTable("Arestas");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Ordem).IsRequired();
        builder.Property(a => a.NoOrigemId).IsRequired();
        builder.Property(a => a.NoDestinoId).IsRequired();
        builder.Property(a => a.Tipo).HasConversion<string>().IsRequired();
        builder.Property(a => a.Justificativa).IsRequired().HasMaxLength(TextoValidado.TamanhoMaximoDaJustificativaDaAresta);
        builder.Property(a => a.Peso)
            .HasConversion(peso => peso.Valor, valor => Peso.Criar(valor))
            .IsRequired();
    }
}
