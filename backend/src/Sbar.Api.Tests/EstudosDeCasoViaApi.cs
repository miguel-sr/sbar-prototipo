using System.Net.Http.Json;
using Sbar.Api.Dtos;
using Sbar.Api.Requests;
using Sbar.Domain.Enums;

namespace Sbar.Api.Tests;

// Montagem de Estudos de Caso de referência pela API real (HTTP), compartilhada pelos testes de ponta a ponta.
public static class EstudosDeCasoViaApi
{
    // Estudo de Caso 1 completo — docs/dominio/estudos-de-caso.md — os mesmos 15 Nós/21 Arestas
    // usados em EstudosDeCasoDeReferencia (Sbar.Application.Tests), reconstruídos aqui via HTTP.
    public static async Task<(Guid CasoId, Dictionary<string, Guid> Nos)> CriarEstudoDeCaso1Async(HttpClient client, string nomeDoCaso = "Sistema de Pedidos Digital")
    {
        var respostaCaso = await client.PostAsJsonAsync("/api/casos", new CriarEstudoDeCasoRequest(nomeDoCaso, null), JsonTestOptions.Padrao);
        respostaCaso.EnsureSuccessStatusCode();
        var caso = (await respostaCaso.Content.ReadFromJsonAsync<EstudoDeCasoDto>(JsonTestOptions.Padrao))!;

        (string Chave, string Nome, TipoDeNo Tipo)[] nosSpec =
        [
            ("A", "Cliente", TipoDeNo.Ator),
            ("TP1", "Cardápio digital", TipoDeNo.Touchpoint),
            ("TP2", "Carrinho/Checkout", TipoDeNo.Touchpoint),
            ("TP3", "Acompanhamento de status", TipoDeNo.Touchpoint),
            ("TP4", "Canal de notificação", TipoDeNo.Touchpoint),
            ("P1", "Validação do pedido", TipoDeNo.Processo),
            ("P2", "Envio à cozinha", TipoDeNo.Processo),
            ("P3", "Preparo do pedido", TipoDeNo.Processo),
            ("P4", "Expedição/Roteamento", TipoDeNo.Processo),
            ("P5", "Fechamento de conta", TipoDeNo.Processo),
            ("S1", "Gestão de cardápio/estoque", TipoDeNo.Suporte),
            ("S2", "Processamento de pagamento", TipoDeNo.Suporte),
            ("DS", "Repositório de pedidos", TipoDeNo.Dados),
            ("E1", "Gateway de pagamento", TipoDeNo.Externo),
            ("E2", "Plataforma de entrega", TipoDeNo.Externo),
        ];

        var nos = new Dictionary<string, Guid>();
        foreach (var (chave, nome, tipo) in nosSpec)
        {
            var resposta = await client.PostAsJsonAsync($"/api/casos/{caso.Id}/nos", new CriarNoRequest(nome, tipo), JsonTestOptions.Padrao);
            resposta.EnsureSuccessStatusCode();
            var no = (await resposta.Content.ReadFromJsonAsync<NoDto>(JsonTestOptions.Padrao))!;
            nos[chave] = no.Id;
        }

        (string Origem, string Destino, TipoDeAresta Tipo, int Peso)[] arestasSpec =
        [
            ("A", "TP1", TipoDeAresta.Sincrona, 3), ("TP1", "S1", TipoDeAresta.Sincrona, 2),
            ("S1", "DS", TipoDeAresta.Sincrona, 2), ("TP1", "TP2", TipoDeAresta.Sincrona, 3),
            ("TP2", "P1", TipoDeAresta.Sincrona, 3), ("P1", "DS", TipoDeAresta.Sincrona, 3),
            ("P1", "P2", TipoDeAresta.Sincrona, 3), ("P2", "DS", TipoDeAresta.Sincrona, 2),
            ("P2", "P3", TipoDeAresta.Critica, 3), ("P3", "P4", TipoDeAresta.Sincrona, 2),
            ("P4", "TP4", TipoDeAresta.Assincrona, 2), ("TP4", "A", TipoDeAresta.Assincrona, 2),
            ("P4", "E2", TipoDeAresta.Assincrona, 1), ("E2", "A", TipoDeAresta.Assincrona, 1),
            ("TP2", "TP3", TipoDeAresta.Sincrona, 2), ("TP3", "DS", TipoDeAresta.Sincrona, 2),
            ("P3", "P5", TipoDeAresta.Assincrona, 1), ("P5", "S2", TipoDeAresta.Sincrona, 3),
            ("S2", "E1", TipoDeAresta.Sincrona, 3), ("E1", "P5", TipoDeAresta.Sincrona, 2),
            ("P5", "DS", TipoDeAresta.Sincrona, 2),
        ];

        foreach (var (origem, destino, tipo, peso) in arestasSpec)
        {
            var resposta = await client.PostAsJsonAsync(
                $"/api/casos/{caso.Id}/arestas",
                new CriarArestaRequest(nos[origem], nos[destino], tipo, peso, "Justificativa de teste"),
                JsonTestOptions.Padrao);
            resposta.EnsureSuccessStatusCode();
        }

        // Fluxos principais F1–F4 — TCC revisado 6.6 (entrada do usuário, RF05)
        (string Nome, string[] Codigos)[] fluxosSpec =
        [
            ("F1 - Consulta de cardápio", ["A", "TP1", "S1", "DS"]),
            ("F2 - Pedido e preparo", ["A", "TP1", "TP2", "P1", "P2", "P3", "P4", "TP4"]),
            ("F3 - Entrega", ["P4", "E2", "A"]),
            ("F4 - Pagamento", ["P3", "P5", "S2", "E1", "P5", "DS"]),
        ];

        foreach (var (nome, codigos) in fluxosSpec)
        {
            var resposta = await client.PostAsJsonAsync(
                $"/api/casos/{caso.Id}/fluxos",
                new CriarFluxoRequest(nome, codigos.Select(c => nos[c]).ToList()),
                JsonTestOptions.Padrao);
            resposta.EnsureSuccessStatusCode();
        }

        return (caso.Id, nos);
    }
}
