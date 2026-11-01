using System.Text.Json;
using System.Text.Json.Serialization;
using Sbar.Domain.Enums;
using Sbar.Domain.ValueObjects;

namespace Sbar.Application.Parametros;

// Serialização do snapshot imutável de uma VersaoDeParametros (RF11): { eixos:[{eixo,minimo,maximo,peso}],
// assinaturas:[{id,nome,valores{CO..DEP}}] }. Ordem canônica (eixos na ordem do enum; assinaturas por nome e id) para
// que o mesmo estado produza sempre o mesmo texto.
public static class SnapshotDeParametros
{
    private static readonly JsonSerializerOptions Opcoes = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = false,
    };

    public static string Serializar(IEnumerable<ParametroDeEixo> eixos, IEnumerable<AssinaturaDeReferencia> assinaturas)
    {
        var snapshot = new Snapshot(
            eixos.OrderBy(e => e.Eixo).Select(e => new EixoNoSnapshot(e.Eixo, e.Minimo, e.Maximo, e.Peso)).ToList(),
            assinaturas
                .OrderBy(a => a.Nome, StringComparer.Ordinal).ThenBy(a => a.Id)
                .Select(a => new AssinaturaNoSnapshot(
                    a.Id, a.Nome, a.Valores.OrderBy(v => v.Key).ToDictionary(v => v.Key.ToString(), v => v.Value)))
                .ToList());

        return JsonSerializer.Serialize(snapshot, Opcoes);
    }

    public static (IReadOnlyList<ParametroDeEixo> Eixos, IReadOnlyList<AssinaturaDeReferencia> Assinaturas) Desserializar(string json)
    {
        var snapshot = JsonSerializer.Deserialize<Snapshot>(json, Opcoes)
            ?? throw new InvalidOperationException("Snapshot de parâmetros vazio.");

        var eixos = snapshot.Eixos.Select(e => new ParametroDeEixo(e.Eixo, e.Minimo, e.Maximo, e.Peso)).ToList();
        var assinaturas = snapshot.Assinaturas
            .Select(a => new AssinaturaDeReferencia(
                a.Id, a.Nome, a.Valores.ToDictionary(v => Enum.Parse<Eixo>(v.Key), v => v.Value)))
            .ToList();

        return (eixos, assinaturas);
    }

    private sealed record Snapshot(List<EixoNoSnapshot> Eixos, List<AssinaturaNoSnapshot> Assinaturas);

    private sealed record EixoNoSnapshot(Eixo Eixo, double Minimo, double Maximo, double Peso);

    private sealed record AssinaturaNoSnapshot(Guid Id, string Nome, Dictionary<string, double> Valores);
}
