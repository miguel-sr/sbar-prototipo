namespace Sbar.Domain.Entities;

// Um passo de um Fluxo: o nó visitado na posição indicada (1 = primeiro nó do caminho). Persistido sem chave
// estrangeira para o Nó, de propósito: ver Fluxo.Passos.
public class FluxoPasso
{
    public Guid Id { get; private set; }
    public Guid FluxoId { get; private set; }
    public int Posicao { get; private set; }
    public Guid NoId { get; private set; }

    private FluxoPasso()
    {
    }

    public FluxoPasso(Guid fluxoId, int posicao, Guid noId)
    {
        Id = Guid.NewGuid();
        FluxoId = fluxoId;
        Posicao = posicao;
        NoId = noId;
    }
}
