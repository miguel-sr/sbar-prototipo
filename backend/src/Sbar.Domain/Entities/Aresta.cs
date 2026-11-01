using Sbar.Domain.Enums;
using Sbar.Domain.Exceptions;
using Sbar.Domain.Validations;
using Sbar.Domain.ValueObjects;

namespace Sbar.Domain.Entities;

public class Aresta
{
    // Texto-padrão das arestas cadastradas ANTES de a justificativa virar obrigatória (RN03, TCC revisado): a
    // migration AdicionaJustificativaDaAresta preenche as arestas existentes com ele e a interface as marca como
    // pendentes de revisão (decisão D11, opção 1).
    public const string JustificativaPendente = "Justificativa pendente (aresta cadastrada antes da exigência do TCC revisado)";

    public Guid Id { get; private set; }
    public Guid NoOrigemId { get; private set; }
    public Guid NoDestinoId { get; private set; }
    public TipoDeAresta Tipo { get; private set; }
    public Peso Peso { get; private set; } = null!;

    // RN03 (TCC 5.3.4 e 4.2.3): toda aresta tem justificativa rastreável a uma interação do Blueprint (ou uma
    // inferência justificada pelos pesquisadores). Obrigatória, sem espaços nas pontas, até 500 caracteres.
    public string Justificativa { get; private set; } = string.Empty;
    public Guid EstudoDeCasoId { get; private set; }

    // Posição de cadastro dentro do Estudo de Caso (ver No.Ordem).
    public int Ordem { get; private set; }

    private Aresta()
    {
    }

    private Aresta(Guid id, Guid noOrigemId, Guid noDestinoId, TipoDeAresta tipo, Peso peso, string justificativa, Guid estudoDeCasoId, int ordem)
    {
        Id = id;
        NoOrigemId = noOrigemId;
        NoDestinoId = noDestinoId;
        Tipo = tipo;
        Peso = peso;
        Justificativa = justificativa;
        EstudoDeCasoId = estudoDeCasoId;
        Ordem = ordem;
    }

    public static Aresta Criar(Guid noOrigemId, Guid noDestinoId, TipoDeAresta tipo, int peso, string justificativa, Guid estudoDeCasoId, int ordem = 0)
    {
        if (noOrigemId == noDestinoId)
            throw new ArestaAutoReferenciadaException(noOrigemId);

        return new Aresta(Guid.NewGuid(), noOrigemId, noDestinoId, tipo, Peso.Criar(peso), JustificativaValida(justificativa), estudoDeCasoId, ordem);
    }

    // Mesmas validações da criação (self-loop e peso em {1, 2, 3}); a checagem de que os Nós pertencem
    // ao Estudo de Caso fica em EstudoDeCaso.AtualizarAresta.
    public void Atualizar(Guid noOrigemId, Guid noDestinoId, TipoDeAresta tipo, int peso, string justificativa)
    {
        if (noOrigemId == noDestinoId)
            throw new ArestaAutoReferenciadaException(noOrigemId);

        var novoPeso = Peso.Criar(peso);
        var novaJustificativa = JustificativaValida(justificativa);

        NoOrigemId = noOrigemId;
        NoDestinoId = noDestinoId;
        Tipo = tipo;
        Peso = novoPeso;
        Justificativa = novaJustificativa;
    }

    private static string JustificativaValida(string justificativa) =>
        TextoValidado.Nome(justificativa, "Justificativa da Aresta", TextoValidado.TamanhoMaximoDaJustificativaDaAresta);
}
