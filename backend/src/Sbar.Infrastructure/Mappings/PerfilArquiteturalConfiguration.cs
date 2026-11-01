using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sbar.Domain.Entities;

namespace Sbar.Infrastructure.Mappings;

public class PerfilArquiteturalConfiguration : IEntityTypeConfiguration<PerfilArquitetural>
{
    public void Configure(EntityTypeBuilder<PerfilArquitetural> builder)
    {
        builder.ToTable("PerfisArquiteturais");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Nome).IsRequired();
        builder.HasIndex(p => p.DonoId);
    }
}
