using Sbar.Domain.Entities;
using Sbar.Domain.Enums;

namespace Sbar.Application.Tests.Grafo;

// Estudo de Caso 1 (Pedidos Digital), Estudo de Caso 2 (Agendamento de Clínica) e Estudo de Caso 3 (Marketplace) completos, exatamente como
// publicados no TCC revisado (Quadros 20/22 e 57/58; fluxos principais das seções 6.6 e 7.2) e em
// docs/dominio/estudos-de-caso.md — golden data do POP-06. Os nós são cadastrados na ordem dos Quadros 20 e 57
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
            estudoDeCaso.AdicionarAresta(nos[origem].Id, nos[destino].Id, tipo, peso, $"Interação {origem} → {destino} (TCC revisado, Quadro 58)");

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

        // Fluxos principais F — TCC 7.2 (4, 5, 5, 3 e 3 bandas → média 4,0 → DEP = 4/5 = 0,8)
        Fluxo("F1 - Agendamento", "A", "TP1", "TP2", "TP3", "P2", "DS");
        Fluxo("F2 - Confirmação", "P2", "P3", "S2", "E1", "TP4", "A");
        Fluxo("F3 - Lembrete", "P2", "P4", "S2", "E1", "TP4", "A");
        Fluxo("F4a - Check-in e atualização interna", "A", "P5", "DS");
        Fluxo("F4b - Sincronização externa", "A", "P5", "E2");

        return (estudoDeCaso, nos);
    }

    // Estudo de Caso 3 (Marketplace de varejo online) completo, como publicado no TCC revisado (Quadros 60 e 61; fluxos
    // principais da seção 8.7): 33 nós, 49 arestas e 7 fluxos. É um cenário construído pelos autores como teste de sanidade
    // da capacidade de discriminar estilos (seção 8.1), não um sistema real. Os nós são cadastrados na ordem do Quadro 60.
    public static (EstudoDeCaso EstudoDeCaso, Dictionary<string, No> Nos) CriarEstudoDeCaso3()
    {
        var estudoDeCaso = EstudoDeCaso.Criar("Plataforma de Marketplace de Varejo Online");
        var nos = new Dictionary<string, No>
        {
            ["A1"] = estudoDeCaso.AdicionarNo("Cliente", TipoDeNo.Ator),
            ["A2"] = estudoDeCaso.AdicionarNo("Vendedor", TipoDeNo.Ator),
            ["TP1"] = estudoDeCaso.AdicionarNo("Busca e vitrine de produtos", TipoDeNo.Touchpoint),
            ["TP2"] = estudoDeCaso.AdicionarNo("Carrinho e checkout", TipoDeNo.Touchpoint),
            ["TP3"] = estudoDeCaso.AdicionarNo("Acompanhamento do pedido", TipoDeNo.Touchpoint),
            ["TP4"] = estudoDeCaso.AdicionarNo("Painel de anúncios", TipoDeNo.Touchpoint),
            ["TP5"] = estudoDeCaso.AdicionarNo("Canal de notificações", TipoDeNo.Touchpoint),
            ["TP6"] = estudoDeCaso.AdicionarNo("Painel de pedidos e repasses", TipoDeNo.Touchpoint),
            ["P1"] = estudoDeCaso.AdicionarNo("Publicação de anúncios", TipoDeNo.Processo),
            ["P2"] = estudoDeCaso.AdicionarNo("Busca e indexação", TipoDeNo.Processo),
            ["P3"] = estudoDeCaso.AdicionarNo("Reserva de estoque", TipoDeNo.Processo),
            ["P4"] = estudoDeCaso.AdicionarNo("Gestão do pedido", TipoDeNo.Processo),
            ["P5"] = estudoDeCaso.AdicionarNo("Autorização de pagamento", TipoDeNo.Processo),
            ["P6"] = estudoDeCaso.AdicionarNo("Análise antifraude", TipoDeNo.Processo),
            ["P7"] = estudoDeCaso.AdicionarNo("Separação e despacho", TipoDeNo.Processo),
            ["P8"] = estudoDeCaso.AdicionarNo("Cotação de frete e etiquetas", TipoDeNo.Processo),
            ["P9"] = estudoDeCaso.AdicionarNo("Atualização de rastreio", TipoDeNo.Processo),
            ["P10"] = estudoDeCaso.AdicionarNo("Repasse ao vendedor", TipoDeNo.Processo),
            ["P11"] = estudoDeCaso.AdicionarNo("Emissão de nota fiscal", TipoDeNo.Processo),
            ["S1"] = estudoDeCaso.AdicionarNo("Identidade e autenticação", TipoDeNo.Suporte),
            ["S2"] = estudoDeCaso.AdicionarNo("Serviço de notificações", TipoDeNo.Suporte),
            ["DS1"] = estudoDeCaso.AdicionarNo("Catálogo de produtos", TipoDeNo.Dados),
            ["DS2"] = estudoDeCaso.AdicionarNo("Estoque", TipoDeNo.Dados),
            ["DS3"] = estudoDeCaso.AdicionarNo("Pedidos", TipoDeNo.Dados),
            ["DS4"] = estudoDeCaso.AdicionarNo("Pagamentos e repasses", TipoDeNo.Dados),
            ["DS5"] = estudoDeCaso.AdicionarNo("Entregas e rastreio", TipoDeNo.Dados),
            ["E1"] = estudoDeCaso.AdicionarNo("Gateway de pagamento", TipoDeNo.Externo),
            ["E2"] = estudoDeCaso.AdicionarNo("Serviço antifraude", TipoDeNo.Externo),
            ["E3"] = estudoDeCaso.AdicionarNo("Transportadoras", TipoDeNo.Externo),
            ["E4"] = estudoDeCaso.AdicionarNo("Provedor de mensagens", TipoDeNo.Externo),
            ["E5"] = estudoDeCaso.AdicionarNo("ERP do vendedor", TipoDeNo.Externo),
            ["E6"] = estudoDeCaso.AdicionarNo("Emissor de nota fiscal", TipoDeNo.Externo),
            ["E7"] = estudoDeCaso.AdicionarNo("Moderação de anúncios", TipoDeNo.Externo),
        };

        void Aresta(string origem, string destino, TipoDeAresta tipo, int peso) =>
            estudoDeCaso.AdicionarAresta(nos[origem].Id, nos[destino].Id, tipo, peso, $"Interação {origem} → {destino} (TCC revisado, Quadro 61)");

        void Fluxo(string nome, params string[] codigos) =>
            estudoDeCaso.AdicionarFluxo(nome, codigos.Select(c => nos[c].Id).ToList());

        Aresta("A2", "TP4", TipoDeAresta.Sincrona, 2);
        Aresta("TP4", "S1", TipoDeAresta.Sincrona, 2);
        Aresta("TP4", "P1", TipoDeAresta.Sincrona, 3);
        Aresta("P1", "DS1", TipoDeAresta.Sincrona, 3);
        Aresta("E5", "P1", TipoDeAresta.Batch, 1);
        Aresta("P1", "P2", TipoDeAresta.Assincrona, 2);
        Aresta("E5", "P3", TipoDeAresta.Batch, 1);
        Aresta("A1", "TP1", TipoDeAresta.Sincrona, 3);
        Aresta("TP1", "S1", TipoDeAresta.Sincrona, 2);
        Aresta("TP1", "P2", TipoDeAresta.Sincrona, 3);
        Aresta("P2", "DS1", TipoDeAresta.Sincrona, 3);
        Aresta("TP1", "TP2", TipoDeAresta.Sincrona, 3);
        Aresta("TP2", "P3", TipoDeAresta.Critica, 3);
        Aresta("P3", "DS2", TipoDeAresta.Sincrona, 3);
        Aresta("TP2", "P8", TipoDeAresta.Sincrona, 2);
        Aresta("P8", "E3", TipoDeAresta.Sincrona, 2);
        Aresta("TP2", "P4", TipoDeAresta.Sincrona, 3);
        Aresta("P4", "DS3", TipoDeAresta.Sincrona, 3);
        Aresta("P4", "P5", TipoDeAresta.Critica, 3);
        Aresta("P5", "P6", TipoDeAresta.Critica, 3);
        Aresta("P6", "E2", TipoDeAresta.Sincrona, 3);
        Aresta("P5", "E1", TipoDeAresta.Sincrona, 3);
        Aresta("E1", "P5", TipoDeAresta.Sincrona, 2);
        Aresta("P5", "DS4", TipoDeAresta.Sincrona, 2);
        Aresta("P5", "P4", TipoDeAresta.Assincrona, 2);
        Aresta("P4", "P7", TipoDeAresta.Assincrona, 3);
        Aresta("TP6", "P7", TipoDeAresta.Sincrona, 2);
        Aresta("P7", "P8", TipoDeAresta.Sincrona, 3);
        Aresta("P8", "DS5", TipoDeAresta.Sincrona, 2);
        Aresta("E3", "P9", TipoDeAresta.Assincrona, 2);
        Aresta("P9", "DS5", TipoDeAresta.Sincrona, 2);
        Aresta("P9", "S2", TipoDeAresta.Assincrona, 2);
        Aresta("S2", "E4", TipoDeAresta.Assincrona, 2);
        Aresta("E4", "TP5", TipoDeAresta.Assincrona, 2);
        Aresta("TP5", "A1", TipoDeAresta.Assincrona, 2);
        Aresta("A1", "TP3", TipoDeAresta.Sincrona, 2);
        Aresta("TP3", "DS5", TipoDeAresta.Sincrona, 2);
        Aresta("P4", "S2", TipoDeAresta.Assincrona, 2);
        Aresta("P9", "P10", TipoDeAresta.Batch, 1);
        Aresta("P10", "DS4", TipoDeAresta.Batch, 1);
        Aresta("P10", "E1", TipoDeAresta.Batch, 1);
        Aresta("P10", "TP6", TipoDeAresta.Assincrona, 1);
        Aresta("A2", "TP6", TipoDeAresta.Sincrona, 2);
        Aresta("P4", "P11", TipoDeAresta.Assincrona, 2);
        Aresta("P11", "E6", TipoDeAresta.Sincrona, 2);
        Aresta("E6", "P11", TipoDeAresta.Assincrona, 2);
        Aresta("P11", "DS3", TipoDeAresta.Sincrona, 2);
        Aresta("P1", "E7", TipoDeAresta.Assincrona, 2);
        Aresta("E7", "P1", TipoDeAresta.Assincrona, 2);

        // Fluxos principais F — TCC 8.7 (4, 4, 3, 5, 5, 3 e 2 bandas → média 26/7 → DEP = 0,7429)
        Fluxo("F1 - Publicação de anúncio", "A2", "TP4", "P1", "DS1");
        Fluxo("F2 - Busca de produtos", "A1", "TP1", "P2", "DS1");
        Fluxo("F3 - Reserva de estoque", "TP2", "P3", "DS2");
        Fluxo("F4 - Compra e pagamento", "A1", "TP1", "TP2", "P4", "P5", "E1", "P5", "DS4");
        Fluxo("F5 - Despacho e notificação", "P4", "P7", "P8", "E3", "P9", "S2", "E4", "TP5", "A1");
        Fluxo("F6 - Acompanhamento", "A1", "TP3", "DS5");
        Fluxo("F7 - Repasse ao vendedor", "P9", "P10", "E1");

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
