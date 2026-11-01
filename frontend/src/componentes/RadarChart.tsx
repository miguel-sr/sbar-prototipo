import { PolarAngleAxis, PolarGrid, PolarRadiusAxis, Radar, RadarChart as RadarRecharts, ResponsiveContainer, Tooltip } from 'recharts';
import { formatarNumero } from '../dominio/rotulos';
import { INFO_DO_EIXO } from '../dominio/eixos';
import { EIXOS, type EixoResultado } from '../tipos/Metricas';
import { campoDoEixo, type PerfilArquitetural } from '../tipos/PerfilArquitetural';

interface Props {
  eixos: EixoResultado[];
  perfis: PerfilArquitetural[];
}

const CORES_DOS_PERFIS = ['var(--perfil-1)', 'var(--perfil-2)', 'var(--perfil-3)', 'var(--perfil-4)'];

// Radar Arquitetural (POP-09, componente RadarChart): os dez eixos normalizados (0–5) do
// serviço, sobrepostos às assinaturas dos Perfis Arquiteturais escolhidos.
// Eixo "não aplicável" aparece como 0 no desenho e com o motivo no cartão do eixo.
export function RadarChart({ eixos, perfis }: Props) {
  const dados = EIXOS.map((eixo) => {
    const resultado = eixos.find((e) => e.eixo === eixo);
    const linha: Record<string, string | number | null> = {
      eixo,
      servico: resultado?.aplicavel ? (resultado.valorNormalizado ?? 0) : 0,
    };
    perfis.forEach((p) => (linha[p.id] = p[campoDoEixo(eixo)]));
    return linha;
  });

  return (
    <div className="radar" role="img" aria-label="Radar Arquitetural com os dez eixos normalizados de 0 a 5">
      <ResponsiveContainer width="100%" height={380}>
        <RadarRecharts data={dados} outerRadius="72%" margin={{ top: 24, right: 30, bottom: 16, left: 30 }}>
          <PolarGrid stroke="var(--borda)" />
          <PolarAngleAxis dataKey="eixo" tick={{ fill: 'var(--texto)', fontSize: 12, fontWeight: 600 }} />
          <PolarRadiusAxis domain={[0, 5]} tickCount={6} angle={72} tick={{ fill: 'var(--texto-suave)', fontSize: 10 }} axisLine={false} />
          {perfis.map((p, i) => (
            <Radar
              key={p.id}
              name={p.nome}
              dataKey={p.id}
              stroke={CORES_DOS_PERFIS[i % CORES_DOS_PERFIS.length]}
              strokeDasharray="5 4"
              strokeWidth={1.8}
              fill="none"
              isAnimationActive={false}
            />
          ))}
          <Radar name="Serviço (este Estudo de Caso)" dataKey="servico" stroke="var(--primaria)" fill="var(--primaria)" fillOpacity={0.22} strokeWidth={2.4} isAnimationActive={false} dot={{ r: 3 }} />
          <Tooltip
            formatter={(valor, nome) => [formatarNumero(Number(valor)), nome]}
            labelFormatter={(eixo) => `${eixo} — ${INFO_DO_EIXO[eixo as keyof typeof INFO_DO_EIXO]?.nome ?? ''}`}
            contentStyle={{ borderRadius: 8, border: '1px solid var(--borda)', fontSize: 13 }}
          />
        </RadarRecharts>
      </ResponsiveContainer>
    </div>
  );
}

export { CORES_DOS_PERFIS };
