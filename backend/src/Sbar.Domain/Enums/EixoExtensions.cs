using System.ComponentModel;
using System.Reflection;

namespace Sbar.Domain.Enums;

public static class EixoExtensions
{
    public static string ObterDescricao(this Eixo eixo)
    {
        var membro = typeof(Eixo).GetField(eixo.ToString())!;
        var atributo = membro.GetCustomAttribute<DescriptionAttribute>();

        return atributo?.Description ?? eixo.ToString();
    }
}
