// Avisos de casos-limite devolvidos pela API (sem arestas, comunidade única, Q < 0,3…): o texto vem
// pronto do backend; aqui só é exibido, para que não passe despercebido que FLEX/AUT podem estar
// degenerados naquele Mapa.
export function AvisosDoMapa({ avisos }: { avisos: string[] }) {
  if (avisos.length === 0) return null;
  return (
    <div className="avisos" role="status" aria-label="Avisos sobre o cálculo">
      <p className="avisos__titulo">Atenção ao interpretar este Radar</p>
      <ul>
        {avisos.map((aviso) => (
          <li key={aviso}>{aviso}</li>
        ))}
      </ul>
    </div>
  );
}
