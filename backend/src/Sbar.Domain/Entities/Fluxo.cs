using Sbar.Domain.Exceptions;
using Sbar.Domain.Validations;

namespace Sbar.Domain.Entities;

// f ∈ F — TCC revisado seções 4.2.7, 4.4.2 (Eixo 10) e 5.3.4 (RF05): fluxo principal CADASTRADO pelo usuário
// (não derivado do grafo), isto é, uma sequência ORDENADA de nós — pode repetir nós (CE1 F4 revisita P5), não
// precisa começar no Ator nem terminar num nó sem saída. O caminho só é válido se cada par consecutivo de nós
// tiver uma aresta origem→destino (a direção importa); a validade é avaliada por EstudoDeCaso.AvaliarFluxo.
public class Fluxo
{
    private readonly List<FluxoPasso> _passos = [];

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public Guid EstudoDeCasoId { get; private set; }

    // Posição de cadastro dentro do Estudo de Caso (ver No.Ordem).
    public int Ordem { get; private set; }

    // Passos em ordem. Os NoId NÃO têm chave estrangeira para Nos: excluir um Nó ou uma Aresta não apaga nem
    // altera o fluxo em silêncio — ele passa a ser avaliado como inválido, com a etapa ausente indicada.
    public IReadOnlyList<FluxoPasso> Passos => _passos.OrderBy(p => p.Posicao).ToList().AsReadOnly();
    public IReadOnlyList<Guid> NosIds => Passos.Select(p => p.NoId).ToList().AsReadOnly();

    private Fluxo()
    {
    }

    private Fluxo(Guid id, string nome, Guid estudoDeCasoId, int ordem, IReadOnlyList<Guid> nosIds)
    {
        Id = id;
        Nome = nome;
        EstudoDeCasoId = estudoDeCasoId;
        Ordem = ordem;
        DefinirPassos(nosIds);
    }

    public static Fluxo Criar(string nome, IReadOnlyList<Guid> nosIds, Guid estudoDeCasoId, int ordem = 0) =>
        new(Guid.NewGuid(), NomeValido(nome), estudoDeCasoId, ordem, PassosValidos(nosIds));

    public void Atualizar(string nome, IReadOnlyList<Guid> nosIds)
    {
        var nomeValido = NomeValido(nome);
        var passosValidos = PassosValidos(nosIds);

        Nome = nomeValido;
        _passos.Clear();
        DefinirPassos(passosValidos);
    }

    private void DefinirPassos(IReadOnlyList<Guid> nosIds)
    {
        for (var posicao = 0; posicao < nosIds.Count; posicao++)
            _passos.Add(new FluxoPasso(Id, posicao + 1, nosIds[posicao]));
    }

    private static IReadOnlyList<Guid> PassosValidos(IReadOnlyList<Guid> nosIds)
    {
        if (nosIds.Count < TextoValidado.QuantidadeMinimaDeNosDoFluxo)
            throw FluxoInvalidoException.PoucosNos(TextoValidado.QuantidadeMinimaDeNosDoFluxo);

        if (nosIds.Count > TextoValidado.QuantidadeMaximaDeNosDoFluxo)
            throw FluxoInvalidoException.NosDemais(TextoValidado.QuantidadeMaximaDeNosDoFluxo);

        return nosIds;
    }

    private static string NomeValido(string nome) =>
        TextoValidado.Nome(nome, "Nome do Fluxo", TextoValidado.TamanhoMaximoDoNomeDoFluxo);
}
