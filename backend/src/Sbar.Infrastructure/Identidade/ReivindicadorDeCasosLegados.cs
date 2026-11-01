using Microsoft.EntityFrameworkCore;

namespace Sbar.Infrastructure.Identidade;

// os Estudos de Caso publicados antes das contas não têm dono. O usuário cujo
// e-mail está configurado como dono do legado recebe todos eles ao se cadastrar. Idempotente: só toca em DonoId nulo.
public class ReivindicadorDeCasosLegados(SbarDbContext contexto)
{
    // Além dos casos, leva os parâmetros, perfis e versões globais de antes das contas (PLANO 4.3 e seção 5).
    public async Task<int> ReivindicarAsync(Guid donoId)
    {
        await contexto.ConfiguracoesDeEixo.IgnoreQueryFilters().Where(c => c.DonoId == null)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.DonoId, donoId));
        await contexto.PerfisArquiteturais.IgnoreQueryFilters().Where(p => p.DonoId == null)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.DonoId, donoId));
        await contexto.VersoesDeParametros.IgnoreQueryFilters().Where(v => v.DonoId == null)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.DonoId, donoId));

        return await contexto.EstudosDeCaso.IgnoreQueryFilters().Where(e => e.DonoId == null)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.DonoId, donoId));
    }
}
