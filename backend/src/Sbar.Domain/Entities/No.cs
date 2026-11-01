using Sbar.Domain.Enums;
using Sbar.Domain.Services;
using Sbar.Domain.Validations;

namespace Sbar.Domain.Entities;

public class No
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public TipoDeNo Tipo { get; private set; }
    public Banda Banda { get; private set; }
    public Guid EstudoDeCasoId { get; private set; }

    // Posição de cadastro dentro do Estudo de Caso — o Guid é aleatório, então sem isso a listagem
    // não teria uma ordem estável (EstudoDeCaso.Nos ordena por Ordem e depois por Id).
    public int Ordem { get; private set; }

    private No()
    {
    }

    private No(Guid id, string nome, TipoDeNo tipo, Banda banda, Guid estudoDeCasoId, int ordem)
    {
        Id = id;
        Nome = nome;
        Tipo = tipo;
        Banda = banda;
        EstudoDeCasoId = estudoDeCasoId;
        Ordem = ordem;
    }

    public static No Criar(string nome, TipoDeNo tipo, Guid estudoDeCasoId, int ordem = 0) =>
        new(Guid.NewGuid(), NomeValido(nome), tipo, RegraDeBanda.Derivar(tipo), estudoDeCasoId, ordem);

    // A Banda é sempre re-derivada do Tipo (RegraDeBanda é o único ponto que decide a Banda).
    public void Atualizar(string nome, TipoDeNo tipo)
    {
        Nome = NomeValido(nome);
        Tipo = tipo;
        Banda = RegraDeBanda.Derivar(tipo);
    }

    private static string NomeValido(string nome) =>
        TextoValidado.Nome(nome, "Nome do Nó", TextoValidado.TamanhoMaximoDoNomeDoNo);
}
