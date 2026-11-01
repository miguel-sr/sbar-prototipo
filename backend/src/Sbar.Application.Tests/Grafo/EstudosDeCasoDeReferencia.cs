using Sbar.Domain.Entities;
using Sbar.Domain.Enums;

namespace Sbar.Application.Tests.Grafo;

// Estudo de Caso 1 (Pedidos Digital) e Estudo de Caso 2 (Agendamento de Clínica) completos, exatamente como
// publicados no TCC revisado (Quadros 20/22 e 31/32; fluxos principais das seções 6.6 e 7.6) e em
// docs/dominio/estudos-de-caso.md — golden data do POP-06. Os nós são cadastrados na ordem dos Quadros 20 e 31
// (a ordem de cadastro desempata a detecção de comunidades, D9) e os fluxos principais entram como ENTRADA (RF05).
public static class EstudosDeCasoDeReferencia
{
    public static (EstudoDeCaso EstudoDeCaso, Dictionary<string, No> Nos) CriarEstudoDeCaso1()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Sistema de Pedidos Digital");
        var nos = new Dictionary<string, No>
        {
            ["A"] = estudoDeCaso.AdicionarNo("Cliente", TipoDeNo.Ator),
            ["TP1"] = estudoDeCaso.AdicionarNo("Cardápio digital", TipoDeNo.Touchpoint),
            ["TP2"] = estudoDeCaso.AdicionarNo("Carrinho/Checkout", TipoDeNo.Touchpoint),
            ["TP3"] = estudoDeCaso.AdicionarNo("Acompanhamento de status", TipoDeNo.Touchpoint),
            ["TP4"] = estudoDeCaso.AdicionarNo("Canal de notificação", TipoDeNo.Touchpoint),
            ["P1"] = estudoDeCaso.AdicionarNo("Validação do pedido", TipoDeNo.Processo),
            ["P2"] = estudoDeCaso.AdicionarNo("Envio à cozinha", TipoDeNo.Processo),
            ["P3"] = estudoDeCaso.AdicionarNo("Preparo do pedido", TipoDeNo.Processo),
            ["P4"] = estudoDeCaso.AdicionarNo("Expedição/Roteamento", TipoDeNo.Processo),
            ["P5"] = estudoDeCaso.AdicionarNo("Fechamento de conta", TipoDeNo.Processo),
            ["S1"] = estudoDeCaso.AdicionarNo("Gestão de cardápio/estoque", TipoDeNo.Suporte),
            ["S2"] = estudoDeCaso.AdicionarNo("Processamento de pagamento", TipoDeNo.Suporte),
            ["DS"] = estudoDeCaso.AdicionarNo("Repositório de pedidos", TipoDeNo.Dados),
            ["E1"] = estudoDeCaso.AdicionarNo("Gateway de pagamento", TipoDeNo.Externo),
            ["E2"] = estudoDeCaso.AdicionarNo("Plataforma de entrega", TipoDeNo.Externo),
        };

        void Aresta(string origem, string destino, TipoDeAresta tipo, int peso) =>
            estudoDeCaso.AdicionarAresta(nos[origem].Id, nos[destino].Id, tipo, peso, $"Interação {origem} → {destino} (TCC revisado, Quadro 22)");

        void Fluxo(string nome, params string[] codigos) =>
            estudoDeCaso.AdicionarFluxo(nome, codigos.Select(c => nos[c].Id).ToList());

        Aresta("A", "TP1", TipoDeAresta.Sincrona, 3);
        Aresta("TP1", "S1", TipoDeAresta.Sincrona, 2);
        Aresta("S1", "DS", TipoDeAresta.Sincrona, 2);
        Aresta("TP1", "TP2", TipoDeAresta.Sincrona, 3);
        Aresta("TP2", "P1", TipoDeAresta.Sincrona, 3);
        Aresta("P1", "DS", TipoDeAresta.Sincrona, 3);
        Aresta("P1", "P2", TipoDeAresta.Sincrona, 3);
        Aresta("P2", "DS", TipoDeAresta.Sincrona, 2);
        Aresta("P2", "P3", TipoDeAresta.Critica, 3);
        Aresta("P3", "P4", TipoDeAresta.Sincrona, 2);
        Aresta("P4", "TP4", TipoDeAresta.Assincrona, 2);
        Aresta("TP4", "A", TipoDeAresta.Assincrona, 2);
        Aresta("P4", "E2", TipoDeAresta.Assincrona, 1);
        Aresta("E2", "A", TipoDeAresta.Assincrona, 1);
        Aresta("TP2", "TP3", TipoDeAresta.Sincrona, 2);
        Aresta("TP3", "DS", TipoDeAresta.Sincrona, 2);
        Aresta("P3", "P5", TipoDeAresta.Assincrona, 1);
        Aresta("P5", "S2", TipoDeAresta.Sincrona, 3);
        Aresta("S2", "E1", TipoDeAresta.Sincrona, 3);
        Aresta("E1", "P5", TipoDeAresta.Sincrona, 2);
        Aresta("P5", "DS", TipoDeAresta.Sincrona, 2);

        // Fluxos principais F — TCC 6.6 (cada um atravessa 3 bandas → DEP = 3/5 = 0,6)
        Fluxo("F1 - Consulta de cardápio", "A", "TP1", "S1", "DS");
        Fluxo("F2 - Pedido e preparo", "A", "TP1", "TP2", "P1", "P2", "P3", "P4", "TP4");
        Fluxo("F3 - Entrega", "P4", "E2", "A");
        Fluxo("F4 - Pagamento", "P3", "P5", "S2", "E1", "P5", "DS");

        return (estudoDeCaso, nos);
    }

    public static (EstudoDeCaso EstudoDeCaso, Dictionary<string, No> Nos) CriarEstudoDeCaso2()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Sistema de Agendamento de Clínica");
        var nos = new Dictionary<string, No>
        {
            ["A"] = estudoDeCaso.AdicionarNo("Paciente", TipoDeNo.Ator),
            ["TP1"] = estudoDeCaso.AdicionarNo("Portal de Agendamento", TipoDeNo.Touchpoint),
            ["TP2"] = estudoDeCaso.AdicionarNo("Tela de Seleção de Horário", TipoDeNo.Touchpoint),
            ["TP3"] = estudoDeCaso.AdicionarNo("Tela de Confirmação", TipoDeNo.Touchpoint),
            ["TP4"] = estudoDeCaso.AdicionarNo("Canal de Notificação", TipoDeNo.Touchpoint),
            ["P1"] = estudoDeCaso.AdicionarNo("Verificação de Disponibilidade", TipoDeNo.Processo),
            ["P2"] = estudoDeCaso.AdicionarNo("Registro do Agendamento", TipoDeNo.Processo),
            ["P3"] = estudoDeCaso.AdicionarNo("Geração da Confirmação", TipoDeNo.Processo),
            ["P4"] = estudoDeCaso.AdicionarNo("Job de Lembrete", TipoDeNo.Processo),
            ["P5"] = estudoDeCaso.AdicionarNo("Check-in da Consulta", TipoDeNo.Processo),
            ["S1"] = estudoDeCaso.AdicionarNo("Autenticação/Cadastro", TipoDeNo.Suporte),
            ["S2"] = estudoDeCaso.AdicionarNo("Serviço de Notificações", TipoDeNo.Suporte),
            ["DS"] = estudoDeCaso.AdicionarNo("Repositório de Agendas", TipoDeNo.Dados),
            ["E1"] = estudoDeCaso.AdicionarNo("Gateway SMS/E-mail", TipoDeNo.Externo),
            ["E2"] = estudoDeCaso.AdicionarNo("Prontuário/Convênio", TipoDeNo.Externo),
        };

        void Aresta(string origem, string destino, TipoDeAresta tipo, int peso) =>
            estudoDeCaso.AdicionarAresta(nos[origem].Id, nos[destino].Id, tipo, peso, $"Interação {origem} → {destino} (TCC revisado, Quadro 32)");

        void Fluxo(string nome, params string[] codigos) =>
            estudoDeCaso.AdicionarFluxo(nome, codigos.Select(c => nos[c].Id).ToList());

        Aresta("A", "TP1", TipoDeAresta.Sincrona, 3);
        Aresta("TP1", "S1", TipoDeAresta.Sincrona, 2);
        Aresta("S1", "DS", TipoDeAresta.Sincrona, 2);
        Aresta("TP1", "TP2", TipoDeAresta.Sincrona, 3);
        Aresta("TP2", "P1", TipoDeAresta.Sincrona, 3);
        Aresta("P1", "DS", TipoDeAresta.Sincrona, 3);
        Aresta("TP2", "TP3", TipoDeAresta.Sincrona, 3);
        Aresta("TP3", "P2", TipoDeAresta.Sincrona, 3);
        Aresta("P2", "DS", TipoDeAresta.Sincrona, 3);
        Aresta("P2", "P3", TipoDeAresta.Assincrona, 2);
        Aresta("P3", "S2", TipoDeAresta.Assincrona, 2);
        Aresta("S2", "E1", TipoDeAresta.Assincrona, 2);
        Aresta("E1", "TP4", TipoDeAresta.Assincrona, 2);
        Aresta("P2", "P4", TipoDeAresta.Batch, 1);
        Aresta("P4", "S2", TipoDeAresta.Batch, 1);
        Aresta("S2", "E1", TipoDeAresta.Batch, 1);
        Aresta("E1", "TP4", TipoDeAresta.Batch, 1);
        Aresta("TP4", "A", TipoDeAresta.Assincrona, 2);
        Aresta("A", "P5", TipoDeAresta.Critica, 3);
        Aresta("P5", "DS", TipoDeAresta.Sincrona, 2);
        Aresta("P5", "E2", TipoDeAresta.Batch, 1);

        // Fluxos principais F — TCC 7.6 (4, 5, 5, 3 e 3 bandas → média 4,0 → DEP = 4/5 = 0,8)
        Fluxo("F1 - Agendamento", "A", "TP1", "TP2", "TP3", "P2", "DS");
        Fluxo("F2 - Confirmação", "P2", "P3", "S2", "E1", "TP4", "A");
        Fluxo("F3 - Lembrete", "P2", "P4", "S2", "E1", "TP4", "A");
        Fluxo("F4a - Check-in e atualização interna", "A", "P5", "DS");
        Fluxo("F4b - Sincronização externa", "A", "P5", "E2");

        return (estudoDeCaso, nos);
    }

    // Monta um Mapa pequeno só com códigos (nós cadastrados na ordem informada, todos Processo; arestas
    // Síncronas). Serve aos testes de casos-limite e de propriedades dos algoritmos de grafo.
    public static (EstudoDeCaso EstudoDeCaso, Dictionary<string, No> Nos) Montar(
        IEnumerable<string> codigosDosNos, IEnumerable<(string Origem, string Destino, int Peso)> arestas)
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Mapa de teste");
        var nos = codigosDosNos.ToDictionary(c => c, c => estudoDeCaso.AdicionarNo(c, TipoDeNo.Processo));

        foreach (var (origem, destino, peso) in arestas)
            estudoDeCaso.AdicionarAresta(nos[origem].Id, nos[destino].Id, TipoDeAresta.Sincrona, peso, $"Ligação {origem} → {destino} do Mapa de teste");

        return (estudoDeCaso, nos);
    }
}
