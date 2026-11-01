using Sbar.Application.Grafo;
using Sbar.Domain.Enums;

namespace Sbar.Application.Calculadoras;

public interface ICalculadoraDeEixo
{
    Eixo Eixo { get; }

    ResultadoDeMetricaBruta Calcular(ContextoDoMapa contexto);
}
