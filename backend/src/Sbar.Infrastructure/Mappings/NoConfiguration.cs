using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sbar.Domain.Entities;
using Sbar.Domain.Validations;

namespace Sbar.Infrastructure.Mappings;

public class NoConfiguration : IEntityTypeConfiguration<No>
{
    public void Configure(EntityTypeBuilder<No> builder)
    {
        builder.ToTable("Nos");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.Nome).IsRequired().HasMaxLength(TextoValidado.TamanhoMaximoDoNomeDoNo);
        builder.Property(n => n.Ordem).IsRequired();
        builder.Property(n => n.Tipo).HasConversion<string>().IsRequired();
        builder.Property(n => n.Banda).HasConversion<string>().IsRequired();
    }
}
