using FluentAssertions;
using Sbar.Domain.Entities;
using Xunit;

namespace Sbar.Domain.Tests;

// A descrição automática de uma versão ("Assinatura '<nome>' criada") herda o nome do perfil, que não tem limite próprio:
// ela precisa caber na coluna (200) em vez de falhar na gravação.
public class VersaoDeParametrosTests
{
    [Fact]
    public void Criar_DeveTruncarADescricao_QuandoPassaDoLimiteDaColuna()
    {
        var versao = VersaoDeParametros.Criar(2, new string('x', 500), "{}");

        versao.Descricao.Should().HaveLength(VersaoDeParametros.TamanhoMaximoDaDescricao);
    }
}
