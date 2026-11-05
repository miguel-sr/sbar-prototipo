using Sbar.Domain.Enums;
using Sbar.Domain.Exceptions;
using Sbar.Domain.Validations;

namespace Sbar.Domain.Entities;

// RF-01 — EstudoDeCaso é o aggregate root: No e Aresta só existem dentro do agregado de um Estudo de Caso
public class EstudoDeCaso
{
    private readonly List<No> _nos = [];
    private readonly List<Aresta> _arestas = [];
    private readonly List<Fluxo> _fluxos = [];

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string? DominioDescricao { get; private set; }
    public DateTime DataCriacao { get; private set; }

    // Dono do Estudo de Caso (PLANO_CONTAS_DE_USUARIO.md 4.2). Só o Guid: a conta em si vive em Infrastructure.
    // Anulável enquanto existirem casos publicados antes das contas (sem dono, reivindicados no cadastro).
    public Guid? DonoId { get; private set; }

    // Teto de Estudos de Caso por usuário, para o banco da instância pequena não ser esgotado por uma conta só.
    public const int LimiteDeCasosPorUsuario = 20;

    // Ordenados por Ordem (posição de cadastro) e, em empate (dados antigos, Ordem = 0), por Id —
    // o Guid é aleatório, então só a Ordem dá uma listagem estável para a tela.
    public IReadOnlyList<No> Nos => _nos.OrderBy(n => n.Ordem).ThenBy(n => n.Id).ToList().AsReadOnly();
    public IReadOnlyList<Aresta> Arestas => _arestas.OrderBy(a => a.Ordem).ThenBy(a => a.Id).ToList().AsReadOnly();

    // Fluxos principais cadastrados (F), em ordem de cadastro — ver Fluxo.
    public IReadOnlyList<Fluxo> Fluxos => _fluxos.OrderBy(f => f.Ordem).ThenBy(f => f.Id).ToList().AsReadOnly();

    private EstudoDeCaso()
    {
    }

    private EstudoDeCaso(Guid id, string nome, string? dominioDescricao, DateTime dataCriacao, Guid? donoId)
    {
        DonoId = donoId;
        Id = id;
        Nome = nome;
        DominioDescricao = dominioDescricao;
        DataCriacao = dataCriacao;
    }

    public static EstudoDeCaso Criar(string nome, string? dominioDescricao = null, Guid? donoId = null) =>
        new(Guid.NewGuid(), NomeValido(nome), DescricaoValida(dominioDescricao), DateTime.UtcNow, donoId);

    // RF-01 — edição do nome e da descrição do domínio
    public void Atualizar(string nome, string? dominioDescricao)
    {
        Nome = NomeValido(nome);
        DominioDescricao = DescricaoValida(dominioDescricao);
    }

    // RF-02 — cadastro de nós; banda sempre derivada do tipo (ver No.Criar/RegraDeBanda)
    public No AdicionarNo(string nome, TipoDeNo tipo)
    {
        var ordem = _nos.Count == 0 ? 1 : _nos.Max(n => n.Ordem) + 1;
        var no = No.Criar(nome, tipo, Id, ordem);
        _nos.Add(no);
        return no;
    }

    // RF-02 — edição de nó; a Banda é re-derivada do novo Tipo
    public No AtualizarNo(Guid noId, string nome, TipoDeNo tipo)
    {
        var no = ObterNo(noId);
        no.Atualizar(nome, tipo);
        return no;
    }

    // RF-02 — exclusão de nó; as arestas que entram ou saem dele deixam de fazer sentido e saem junto
    public void RemoverNo(Guid noId)
    {
        var no = ObterNo(noId);

        _arestas.RemoveAll(a => a.NoOrigemId == noId || a.NoDestinoId == noId);
        _nos.Remove(no);
    }

    // RF03/RN03 — cadastro de arestas (com justificativa obrigatória); origem e destino precisam pertencer a este Estudo de Caso
    public Aresta AdicionarAresta(Guid noOrigemId, Guid noDestinoId, TipoDeAresta tipo, int peso, string justificativa)
    {
        GarantirQueONoPertenceAoCaso(noOrigemId);
        GarantirQueONoPertenceAoCaso(noDestinoId);

        var ordem = _arestas.Count == 0 ? 1 : _arestas.Max(a => a.Ordem) + 1;
        var aresta = Aresta.Criar(noOrigemId, noDestinoId, tipo, peso, justificativa, Id, ordem);
        _arestas.Add(aresta);
        return aresta;
    }

    // RF-03 — edição de aresta, com as mesmas validações da criação
    public Aresta AtualizarAresta(Guid arestaId, Guid noOrigemId, Guid noDestinoId, TipoDeAresta tipo, int peso, string justificativa)
    {
        var aresta = ObterAresta(arestaId);

        GarantirQueONoPertenceAoCaso(noOrigemId);
        GarantirQueONoPertenceAoCaso(noDestinoId);

        aresta.Atualizar(noOrigemId, noDestinoId, tipo, peso, justificativa);
        return aresta;
    }

    // RF-03 — exclusão de aresta
    public void RemoverAresta(Guid arestaId) =>
        _arestas.Remove(ObterAresta(arestaId));

    // RF05 — cadastro de fluxo principal: sequência ordenada de nós (repetição permitida) do mesmo Estudo de Caso,
    // em que cada par consecutivo tem uma aresta origem→destino (CT09).
    public Fluxo AdicionarFluxo(string nome, IReadOnlyList<Guid> nosIds)
    {
        var ordem = _fluxos.Count == 0 ? 1 : _fluxos.Max(f => f.Ordem) + 1;
        var fluxo = Fluxo.Criar(nome, nosIds, Id, ordem);

        ValidarCaminho(fluxo.Nome, nosIds);
        _fluxos.Add(fluxo);
        return fluxo;
    }

    // RF05 — edição de fluxo: substitui nome e sequência de nós, com as mesmas validações da criação
    public Fluxo AtualizarFluxo(Guid fluxoId, string nome, IReadOnlyList<Guid> nosIds)
    {
        var fluxo = ObterFluxo(fluxoId);
        var copia = Fluxo.Criar(nome, nosIds, Id); // valida nome e quantidade de nós sem alterar o fluxo atual

        ValidarCaminho(copia.Nome, nosIds);
        fluxo.Atualizar(nome, nosIds);
        return fluxo;
    }

    // RF05 — exclusão de fluxo
    public void RemoverFluxo(Guid fluxoId) =>
        _fluxos.Remove(ObterFluxo(fluxoId));

    // US03/CT09 — verifica o fluxo contra o Mapa ATUAL: o primeiro par consecutivo sem aresta origem→destino (ou
    // com um Nó que já foi removido) é a etapa ausente. Excluir Nó ou Aresta nunca apaga o fluxo: só o invalida.
    public AvaliacaoDeFluxo AvaliarFluxo(Fluxo fluxo)
    {
        var nosPorId = _nos.ToDictionary(n => n.Id);
        var ids = fluxo.NosIds;
        EtapaAusente? etapaAusente = null;

        for (var i = 0; i < ids.Count - 1 && etapaAusente is null; i++)
        {
            var (origemId, destinoId) = (ids[i], ids[i + 1]);

            if (!nosPorId.TryGetValue(origemId, out var origem) || !nosPorId.TryGetValue(destinoId, out var destino))
                etapaAusente = new EtapaAusente(i + 1, origemId, destinoId, "Um dos nós desta etapa foi removido do Mapa.");
            else if (!_arestas.Any(a => a.NoOrigemId == origemId && a.NoDestinoId == destinoId))
                etapaAusente = new EtapaAusente(i + 1, origemId, destinoId,
                    $"Não há aresta de '{origem.Nome}' para '{destino.Nome}'.");
        }

        var bandasPercorridas = ids.Where(nosPorId.ContainsKey).Select(id => nosPorId[id].Banda).Distinct().Count();

        return new AvaliacaoDeFluxo(etapaAusente is null, etapaAusente, bandasPercorridas);
    }

    private void ValidarCaminho(string nomeDoFluxo, IReadOnlyList<Guid> nosIds)
    {
        foreach (var noId in nosIds)
            GarantirQueONoPertenceAoCaso(noId);

        var nosPorId = _nos.ToDictionary(n => n.Id);

        for (var i = 0; i < nosIds.Count - 1; i++)
        {
            var (origemId, destinoId) = (nosIds[i], nosIds[i + 1]);

            if (!_arestas.Any(a => a.NoOrigemId == origemId && a.NoDestinoId == destinoId))
                throw FluxoInvalidoException.SemAresta(nomeDoFluxo, i + 1, nosPorId[origemId].Nome, nosPorId[destinoId].Nome);
        }
    }

    private Fluxo ObterFluxo(Guid fluxoId) =>
        _fluxos.FirstOrDefault(f => f.Id == fluxoId)
            ?? throw new RecursoNaoEncontradoException(nameof(Fluxo), fluxoId);

    private No ObterNo(Guid noId) =>
        _nos.FirstOrDefault(n => n.Id == noId)
            ?? throw new RecursoNaoEncontradoException(nameof(No), noId);

    private Aresta ObterAresta(Guid arestaId) =>
        _arestas.FirstOrDefault(a => a.Id == arestaId)
            ?? throw new RecursoNaoEncontradoException(nameof(Aresta), arestaId);

    private void GarantirQueONoPertenceAoCaso(Guid noId)
    {
        if (_nos.All(n => n.Id != noId))
            throw new NoDeOutroEstudoDeCasoException(noId);
    }

    private static string NomeValido(string nome) =>
        TextoValidado.Nome(nome, "Nome do Estudo de Caso", TextoValidado.TamanhoMaximoDoNomeDoEstudoDeCaso);

    private static string? DescricaoValida(string? dominioDescricao) =>
        TextoValidado.DescricaoOpcional(dominioDescricao, "Descrição do domínio", TextoValidado.TamanhoMaximoDaDescricaoDoDominio);
}
