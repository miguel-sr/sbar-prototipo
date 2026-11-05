using Sbar.Domain.Enums;
using Sbar.Domain.Exceptions;

namespace Sbar.Domain.Entities;

// Limites de normalização (Quadro 6 do TCC revisado) e peso de comparação de cada eixo — POP-03 seção 3.6:
// ficam num único local de configuração (tabela), nunca hardcoded em cada calculadora.
public class ConfiguracaoDeEixo
{
    public Guid Id { get; private set; }

    // Dono dos parâmetros (PLANO_CONTAS_DE_USUARIO.md 4.3). Nulo só nos dados anteriores às contas, que o dono
    // do legado reivindica ao se cadastrar.
    public Guid? DonoId { get; private set; }
    public Eixo Eixo { get; private set; }
    public double LimiteMinimo { get; private set; }
    public double LimiteMaximo { get; private set; }
    public double Peso { get; private set; }

    private ConfiguracaoDeEixo()
    {
    }

    private ConfiguracaoDeEixo(Eixo eixo, double limiteMinimo, double limiteMaximo, double peso, Guid? donoId)
    {
        if (limiteMaximo <= limiteMinimo)
            throw new LimitesDeNormalizacaoInvalidosException(eixo.ToString(), limiteMinimo, limiteMaximo);

        Id = Guid.NewGuid();
        DonoId = donoId;
        Eixo = eixo;
        LimiteMinimo = limiteMinimo;
        LimiteMaximo = limiteMaximo;
        Peso = peso;
    }

    public static ConfiguracaoDeEixo Criar(Eixo eixo, double limiteMinimo, double limiteMaximo, double peso, Guid? donoId = null) =>
        new(eixo, limiteMinimo, limiteMaximo, peso, donoId);

    // RF11 — edição dos limites (Quadro 6) e do peso (4.5.1) do eixo; a validação do conjunto (Σ p_i = 1) é do ConjuntoDeParametros.
    public void Atualizar(double limiteMinimo, double limiteMaximo, double peso)
    {
        if (limiteMaximo <= limiteMinimo)
            throw new LimitesDeNormalizacaoInvalidosException(Eixo.ToString(), limiteMinimo, limiteMaximo);

        LimiteMinimo = limiteMinimo;
        LimiteMaximo = limiteMaximo;
        Peso = peso;
    }
}
