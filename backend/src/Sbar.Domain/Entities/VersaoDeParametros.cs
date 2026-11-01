namespace Sbar.Domain.Entities;

// RF11/US06 — versão imutável dos parâmetros de análise (limites, pesos e assinaturas). O estado vigente fica nas
// tabelas ConfiguracoesDeEixo e PerfisArquiteturais; cada alteração (e a primeira leitura, sob demanda) grava aqui um
// snapshot, para que cada análise informe a versão usada e uma versão antiga possa ser reaplicada (TCC 4.6.1, 5.4).
// Guarda ENTRADA (parâmetros), nunca resultado derivado (RX02).
public class VersaoDeParametros
{
    public const int TamanhoMaximoDaDescricao = 200;

    public Guid Id { get; private set; }
    public Guid? DonoId { get; private set; }
    public int Numero { get; private set; }
    public DateTime CriadaEm { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public string SnapshotJson { get; private set; } = string.Empty;

    private VersaoDeParametros()
    {
    }

    private VersaoDeParametros(Guid id, int numero, DateTime criadaEm, string descricao, string snapshotJson, Guid? donoId)
    {
        Id = id;
        DonoId = donoId;
        Numero = numero;
        CriadaEm = criadaEm;
        Descricao = descricao;
        SnapshotJson = snapshotJson;
    }

    public static VersaoDeParametros Criar(int numero, string descricao, string snapshotJson, Guid? donoId = null) =>
        new(Guid.NewGuid(), numero, DateTime.UtcNow, Truncar(descricao), snapshotJson, donoId);

    private static string Truncar(string descricao) =>
        descricao.Length <= TamanhoMaximoDaDescricao ? descricao : descricao[..(TamanhoMaximoDaDescricao - 1)] + "…";
}
