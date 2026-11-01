using Sbar.Domain.Exceptions;

namespace Sbar.Domain.ValueObjects;

// RF-03 — peso de aresta restrito a {1, 2, 3} (TCC seção 4.2.5)
public sealed class Peso : IEquatable<Peso>
{
    public int Valor { get; }

    private Peso(int valor) => Valor = valor;

    public static Peso Criar(int valor) =>
        valor is 1 or 2 or 3
            ? new Peso(valor)
            : throw new PesoDeArestaInvalidoException(valor);

    public static implicit operator int(Peso peso) => peso.Valor;

    public bool Equals(Peso? outro) => outro is not null && Valor == outro.Valor;
    public override bool Equals(object? obj) => Equals(obj as Peso);
    public override int GetHashCode() => Valor.GetHashCode();
    public override string ToString() => Valor.ToString();
}
