using Sbar.Domain.Enums;
using Sbar.Domain.ValueObjects;

namespace Sbar.Domain.Entities;

// RF-08 — cadastro de Perfis Arquiteturais de referência; do usuário (cada conta tem os seus), não vinculado a Estudo de Caso
public class PerfilArquitetural
{
    public Guid Id { get; private set; }
    public Guid? DonoId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public double ValorCO { get; private set; }
    public double ValorESC { get; private set; }
    public double ValorINT { get; private set; }
    public double ValorUX { get; private set; }
    public double ValorDISP { get; private set; }
    public double ValorFLEX { get; private set; }
    public double ValorTR { get; private set; }
    public double ValorAUT { get; private set; }
    public double ValorVOL { get; private set; }
    public double ValorDEP { get; private set; }

    private PerfilArquitetural()
    {
    }

    private PerfilArquitetural(Guid id, DadosDoPerfilArquitetural dados, Guid? donoId)
    {
        Id = id;
        DonoId = donoId;
        AtribuirDados(dados);
    }

    public static PerfilArquitetural Criar(DadosDoPerfilArquitetural dados, Guid? donoId = null) =>
        new(Guid.NewGuid(), dados, donoId);

    public void Atualizar(DadosDoPerfilArquitetural dados) => AtribuirDados(dados);

    // Forma congelada deste perfil, usada nos snapshots de parâmetros versionados (RF11) e na comparação (4.6).
    public AssinaturaDeReferencia ParaAssinatura() => new(Id, Nome, new Dictionary<Eixo, double>
    {
        [Eixo.CO] = ValorCO, [Eixo.ESC] = ValorESC, [Eixo.INT] = ValorINT, [Eixo.UX] = ValorUX, [Eixo.DISP] = ValorDISP,
        [Eixo.FLEX] = ValorFLEX, [Eixo.TR] = ValorTR, [Eixo.AUT] = ValorAUT, [Eixo.VOL] = ValorVOL, [Eixo.DEP] = ValorDEP,
    });

    private void AtribuirDados(DadosDoPerfilArquitetural dados)
    {
        Nome = dados.Nome;
        ValorCO = dados.ValorCO;
        ValorESC = dados.ValorESC;
        ValorINT = dados.ValorINT;
        ValorUX = dados.ValorUX;
        ValorDISP = dados.ValorDISP;
        ValorFLEX = dados.ValorFLEX;
        ValorTR = dados.ValorTR;
        ValorAUT = dados.ValorAUT;
        ValorVOL = dados.ValorVOL;
        ValorDEP = dados.ValorDEP;
    }
}
