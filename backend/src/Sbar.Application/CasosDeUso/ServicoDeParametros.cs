using Sbar.Application.Parametros;
using Sbar.Domain.Entities;
using Sbar.Domain.Exceptions;
using Sbar.Domain.Interfaces;
using Sbar.Domain.Interfaces.Repositories;
using Sbar.Domain.Interfaces.Services;
using Sbar.Domain.Validations;
using Sbar.Domain.ValueObjects;

namespace Sbar.Application.CasosDeUso;

// RF11/US06 — parâmetros de análise versionados. O estado vigente continua nas tabelas ConfiguracoesDeEixo (limites + pesos)
// e PerfisArquiteturais (assinaturas); cada alteração grava, na MESMA transação, um snapshot imutável (VersaoDeParametros).
// A versão 1 nasce sob demanda a partir do estado vigente, antes de qualquer alteração. Um bloqueio consultivo
// (IUnidadeDeTrabalho) serializa as gravações e mantém a numeração sequencial mesmo com chamadas concorrentes.
public class ServicoDeParametros(
    IConfiguracaoDeEixoRepository configuracoes,
    IPerfilArquiteturalRepository perfis,
    IVersaoDeParametrosRepository versoes,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IUsuarioAtual usuarioAtual) : IServicoDeParametros
{
    private const string PrefixoDaChaveDoBloqueio = "parametros-de-analise";

    // O bloqueio é por usuário: um usuário gravando versões não serializa os demais (PLANO 4.3).
    public static string ChaveDoBloqueioDe(Guid? donoId) => $"{PrefixoDaChaveDoBloqueio}:{donoId}";

    private string ChaveDoBloqueio => ChaveDoBloqueioDe(usuarioAtual.DonoId);
    public const string DescricaoDaVersaoInicial = "Versão inicial (estado vigente dos parâmetros)";
    private const string DescricaoPadraoDeLimitesEPesos = "Limites e pesos atualizados";

    public async Task<ConjuntoDeParametros> ObterAtualAsync()
    {
        await GarantirVersaoInicialAsync();

        var ultima = await versoes.ObterUltimaAsync()
            ?? throw new InvalidOperationException("A versão inicial dos parâmetros deveria existir.");
        var (eixos, assinaturas) = await LerEstadoVigenteAsync();

        return new ConjuntoDeParametros(ParaInfo(ultima), eixos, assinaturas);
    }

    public async Task<ConjuntoDeParametros> ObterVersaoAsync(int numero)
    {
        await GarantirVersaoInicialAsync();

        var versao = await versoes.ObterPorNumeroAsync(numero)
            ?? throw new RecursoNaoEncontradoException(nameof(VersaoDeParametros), numero);
        var (eixos, assinaturas) = SnapshotDeParametros.Desserializar(versao.SnapshotJson);

        return new ConjuntoDeParametros(ParaInfo(versao), eixos, assinaturas);
    }

    public Task<ConjuntoDeParametros> ObterAsync(int? numero) =>
        numero is null ? ObterAtualAsync() : ObterVersaoAsync(numero.Value);

    public async Task<IReadOnlyList<VersaoDeParametrosInfo>> ListarVersoesAsync()
    {
        await GarantirVersaoInicialAsync();

        return (await versoes.ListarAsync()).OrderByDescending(v => v.Numero).Select(ParaInfo).ToList();
    }

    public async Task<ConjuntoDeParametros> AtualizarLimitesEPesosAsync(string? descricao, IReadOnlyList<ParametroDeEixo> eixos)
    {
        // Valida antes de abrir a transação: nada é gravado se os parâmetros forem inválidos (RN07, CT10).
        ConjuntoDeParametros.ValidarEixos(eixos);
        var descricaoValidada = TextoValidado.DescricaoOpcional(
            descricao, "A descrição da alteração", VersaoDeParametros.TamanhoMaximoDaDescricao) ?? DescricaoPadraoDeLimitesEPesos;

        await unidadeDeTrabalho.ExecutarComBloqueioAsync(ChaveDoBloqueio, async () =>
        {
            await GarantirVersaoInicialAsync();

            var configuracoesVigentes = await configuracoes.ListarAsync();
            foreach (var configuracao in configuracoesVigentes)
            {
                var novo = eixos.Single(e => e.Eixo == configuracao.Eixo);
                configuracao.Atualizar(novo.Minimo, novo.Maximo, novo.Peso);
            }

            await configuracoes.SalvarAsync();
            await RegistrarVersaoAsync(descricaoValidada);
        });

        return await ObterAtualAsync();
    }

    public async Task GarantirVersaoInicialAsync()
    {
        if (await versoes.ObterUltimaAsync() is not null)
            return;

        await unidadeDeTrabalho.ExecutarComBloqueioAsync(ChaveDoBloqueio, async () =>
        {
            // Reconfere dentro do bloqueio: outra requisição pode ter criado a versão 1 enquanto esta esperava.
            if (await versoes.ObterUltimaAsync() is not null)
                return;

            await SemearPadroesSeNecessarioAsync();
            await GravarVersaoAsync(DescricaoDaVersaoInicial);
        });
    }

    // Quem chama já está dentro da transação de quem alterou o estado (a unidade de trabalho participa dela); se
    // não estiver, abre uma nova.
    public Task RegistrarVersaoAsync(string descricao) =>
        unidadeDeTrabalho.ExecutarComBloqueioAsync(ChaveDoBloqueio, () => GravarVersaoAsync(descricao));

    private async Task GravarVersaoAsync(string descricao)
    {
        var (eixos, assinaturas) = await LerEstadoVigenteAsync();
        var ultima = await versoes.ObterUltimaAsync();
        var numero = (ultima?.Numero ?? 0) + 1;

        await versoes.AdicionarAsync(VersaoDeParametros.Criar(numero, descricao, SnapshotDeParametros.Serializar(eixos, assinaturas), usuarioAtual.DonoId));
        await versoes.SalvarAsync();
    }

    // Conta nova: copia os padrões do sistema (Quadro 6 e Quadro 7) para o usuário, antes da versão 1. Quem já tem
    // parâmetros (ex.: o dono do legado) mantém os seus.
    private async Task SemearPadroesSeNecessarioAsync()
    {
        if ((await configuracoes.ListarAsync()).Count > 0)
            return;

        foreach (var eixo in PadroesDoSistema.Eixos)
            await configuracoes.AdicionarAsync(ConfiguracaoDeEixo.Criar(eixo.Eixo, eixo.Minimo, eixo.Maximo, eixo.Peso, usuarioAtual.DonoId));

        foreach (var perfil in PadroesDoSistema.Perfis)
            await perfis.AdicionarAsync(PerfilArquitetural.Criar(perfil, usuarioAtual.DonoId));

        await configuracoes.SalvarAsync();
    }

    private async Task<(IReadOnlyList<ParametroDeEixo>, IReadOnlyList<AssinaturaDeReferencia>)> LerEstadoVigenteAsync()
    {
        var eixos = (await configuracoes.ListarAsync())
            .OrderBy(c => c.Eixo)
            .Select(c => new ParametroDeEixo(c.Eixo, c.LimiteMinimo, c.LimiteMaximo, c.Peso))
            .ToList();
        var assinaturas = (await perfis.ListarAsync()).Select(p => p.ParaAssinatura()).ToList();

        return (eixos, assinaturas);
    }

    private static VersaoDeParametrosInfo ParaInfo(VersaoDeParametros versao) =>
        new(versao.Numero, versao.CriadaEm, versao.Descricao);
}
