using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sbar.Domain.Entities;
using Sbar.Domain.Validations;

namespace Sbar.Infrastructure.Mappings;

public class EstudoDeCasoConfiguration : IEntityTypeConfiguration<EstudoDeCaso>
{
    public void Configure(EntityTypeBuilder<EstudoDeCaso> builder)
    {
        builder.ToTable("EstudosDeCaso");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Nome).IsRequired().HasMaxLength(TextoValidado.TamanhoMaximoDoNomeDoEstudoDeCaso);
        builder.Property(e => e.DominioDescricao).HasMaxLength(TextoValidado.TamanhoMaximoDaDescricaoDoDominio);
        builder.Property(e => e.DataCriacao).IsRequired();
        builder.Property(e => e.DonoId);
        builder.HasIndex(e => e.DonoId);

        // Nos/Arestas são coleções privadas (_nos/_arestas) expostas só como IReadOnlyList —
        // EF Core precisa ler/escrever direto no campo, nunca pela propriedade (que devolve
        // uma cópia via AsReadOnly()).
        builder.HasMany(e => e.Nos)
            .WithOne()
            .HasForeignKey(n => n.EstudoDeCasoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Nos)
            .HasField("_nos")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.Arestas)
            .WithOne()
            .HasForeignKey(a => a.EstudoDeCasoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Arestas)
            .HasField("_arestas")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.Fluxos)
            .WithOne()
            .HasForeignKey(f => f.EstudoDeCasoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Fluxos)
            .HasField("_fluxos")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
