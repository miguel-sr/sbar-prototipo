using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sbar.Domain.Entities;

namespace Sbar.Infrastructure.Mappings;

public class VersaoDeParametrosConfiguration : IEntityTypeConfiguration<VersaoDeParametros>
{
    public void Configure(EntityTypeBuilder<VersaoDeParametros> builder)
    {
        builder.ToTable("VersoesDeParametros");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        // Numeração sequencial única: é o que identifica a versão nas análises (RF11).
        builder.Property(v => v.Numero).IsRequired();
        builder.HasIndex(v => new { v.DonoId, v.Numero }).IsUnique();

        builder.Property(v => v.CriadaEm).IsRequired();
        builder.Property(v => v.Descricao).IsRequired().HasMaxLength(VersaoDeParametros.TamanhoMaximoDaDescricao);
        builder.Property(v => v.SnapshotJson).IsRequired();
    }
}
