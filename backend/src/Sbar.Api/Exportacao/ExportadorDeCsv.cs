using System.Globalization;
using System.Text;
using Sbar.Api.Dtos;
using Sbar.Domain.Enums;

namespace Sbar.Api.Exportacao;

// RF13 (D13) — CSV da análise: UTF-8 com BOM (Excel abre os acentos), separador ';', ponto decimal invariante (reimportável sem
// ajuste manual) e uma seção por bloco, separadas por linha em branco e iniciadas por "# Nome da seção".
public static class ExportadorDeCsv
{
    private const char Separador = ';';

    public static byte[] Gerar(ExportacaoDto e)
    {
        var texto = new StringBuilder();
        var nomesDosNos = e.Nos.ToDictionary(n => n.Id, n => n.Nome);
        string Nome(Guid id) => nomesDosNos.TryGetValue(id, out var nome) ? nome : $"(nó removido {id})";

        Secao(texto, "Caso", ["id", "nome", "dominioDescricao", "geradoEm"],
            [[e.Caso.Id, e.Caso.Nome, e.Caso.DominioDescricao, e.GeradoEm]]);

        Secao(texto, "Versão dos parâmetros", ["numero", "criadaEm", "descricao"],
            [[e.VersaoDosParametros.Numero, e.VersaoDosParametros.CriadaEm, e.VersaoDosParametros.Descricao]]);

        Secao(texto, "Limites e pesos", ["eixo", "minimo", "maximo", "peso"],
            e.Parametros.Eixos.Select(p => new object?[] { p.Eixo, p.Minimo, p.Maximo, p.Peso }));

        var eixos = Enum.GetValues<Eixo>().Select(x => x.ToString()).ToList();
        Secao(texto, "Assinaturas de referência", ["nome", .. eixos],
            e.Parametros.Assinaturas.Select(a => new object?[] { a.Nome }.Concat(eixos.Select(x => (object?)a.Valores[x])).ToArray()));

        Secao(texto, "Nós", ["id", "nome", "tipo", "banda"],
            e.Nos.Select(n => new object?[] { n.Id, n.Nome, n.Tipo, n.Banda }));

        Secao(texto, "Arestas", ["id", "origem", "destino", "tipo", "peso", "justificativa"],
            e.Arestas.Select(a => new object?[] { a.Id, Nome(a.NoOrigemId), Nome(a.NoDestinoId), a.Tipo, a.Peso, a.Justificativa }));

        Secao(texto, "Fluxos principais", ["id", "nome", "valido", "bandasPercorridas", "passos", "etapaAusente"],
            e.Fluxos.Select(f => new object?[]
            {
                f.Id, f.Nome, f.Valido, f.BandasPercorridas, string.Join(" > ", f.NosIds.Select(Nome)), f.EtapaAusente?.Motivo,
            }));

        Secao(texto, "Comunidades", ["indice", "nos", "saidasExternas"],
            e.Comunidades.Select(c => new object?[] { c.Indice, string.Join(", ", c.NosIds.Select(Nome)), c.SaidasExternas }));

        Secao(texto, "Modularidade", ["modularidadeQ"], [[e.ModularidadeQ]]);

        Secao(texto, "Avisos", ["aviso"], e.Avisos.Select(a => new object?[] { a }));

        Secao(texto, "Bandas", ["banda", "quantidadeDeNos", "media", "desvioPadrao", "limiar"],
            e.Bandas.Select(b => new object?[] { b.Banda, b.QuantidadeDeNos, b.Media, b.DesvioPadrao, b.Limiar }));

        Secao(texto, "Eixos", ["eixo", "aplicavel", "valorBruto", "valorNormalizado", "motivo"],
            e.Eixos.Select(x => new object?[] { x.Eixo, x.Aplicavel, x.ValorBruto, x.ValorNormalizado, x.Motivo }));

        var distancias = e.Recomendacao?.Distancias ?? [];
        Secao(texto, "Distâncias", ["perfil", "distancia", "percentualDoMaximo", "maisProximo", "margem"],
            distancias.Select(d => new object?[]
            {
                d.PerfilNome, d.Distancia, d.PercentualDoMaximo, d.PerfilNome == e.Recomendacao!.EstiloRecomendado, e.Recomendacao.Margem,
            }));

        Secao(texto, "Contribuições por eixo", ["perfil", "eixo", "contribuicao"],
            distancias.SelectMany(d => d.Contribuicoes.Select(c => new object?[] { d.PerfilNome, c.Eixo, c.Contribuicao })));

        Secao(texto, "Matriz de rastreabilidade", ["categoria", "elemento", "noOuAresta", "metrica", "eixos", "contribuicao"],
            e.Matriz.Select(l => new object?[] { l.Categoria, l.Elemento, l.NoOuAresta, l.Metrica, string.Join(", ", l.Eixos), l.Contribuicao }),
            ultima: true);

        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(texto.ToString())];
    }

    private static void Secao(StringBuilder texto, string titulo, IEnumerable<string> cabecalho, IEnumerable<object?[]> linhas, bool ultima = false)
    {
        texto.Append("# ").Append(titulo).Append("\r\n");
        texto.Append(string.Join(Separador, cabecalho.Select(Escapar))).Append("\r\n");

        foreach (var linha in linhas)
            texto.Append(string.Join(Separador, linha.Select(valor => Escapar(Formatar(valor))))).Append("\r\n");

        if (!ultima)
            texto.Append("\r\n");
    }

    // Ponto decimal invariante e datas ISO 8601: reimportável sem depender da cultura de quem exporta ou de quem abre.
    private static string Formatar(object? valor) => valor switch
    {
        null => string.Empty,
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        bool b => b ? "true" : "false",
        DateTime t => t.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => valor.ToString() ?? string.Empty,
    };

    private static string Escapar(string campo) =>
        campo.IndexOfAny([Separador, '"', '\r', '\n']) >= 0 ? "\"" + campo.Replace("\"", "\"\"") + "\"" : campo;
}
