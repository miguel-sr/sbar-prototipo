import { lazy, StrictMode, Suspense } from 'react';
import { createRoot } from 'react-dom/client';
import { createBrowserRouter, Navigate, RouterProvider } from 'react-router';
import { CasoLayout } from './componentes/CasoLayout';
import { Carregando } from './componentes/Estados';
import { Layout } from './componentes/Layout';
import { RotaProtegida } from './componentes/RotaProtegida';
import { Cadastrar } from './paginas/Cadastrar';
import { CasoForm } from './paginas/CasoForm';
import { Entrar } from './paginas/Entrar';
import { Fluxos } from './paginas/Fluxos';
import { ListaDeCasos } from './paginas/ListaDeCasos';
import { MapaArestas } from './paginas/MapaArestas';
import { MapaNos } from './paginas/MapaNos';
import { MapaVisual } from './paginas/MapaVisual';
import { MatrizRastreabilidade } from './paginas/MatrizRastreabilidade';
import { NaoEncontrada } from './paginas/NaoEncontrada';
import { Parametros } from './paginas/Parametros';
import { PerfisArquiteturais } from './paginas/PerfisArquiteturais';
import { Recomendacao } from './paginas/Recomendacao';
import { SessaoProvider } from './sessao/ContextoDeSessao';
import './estilos.css';

// O Radar usa a biblioteca de gráficos (Recharts), a parte mais pesada do app: só é baixada
// quando alguém abre a tela T6.
const MetricasRadar = lazy(() => import('./paginas/MetricasRadar').then((m) => ({ default: m.MetricasRadar })));

// As 9 telas do POP-09 (seção 1). T0/T0b (Assistente de Tradução) ficam fora — decisão D5.
const router = createBrowserRouter([
  { path: '/entrar', element: <Entrar /> },
  { path: '/cadastrar', element: <Cadastrar /> },
  {
    element: <RotaProtegida />,
    children: [
      {
        element: <Layout />,
        children: [
          { path: '/', element: <ListaDeCasos /> }, // T1
          { path: '/casos/novo', element: <CasoForm /> }, // T2
          {
            path: '/casos/:casoId',
            element: <CasoLayout />,
            children: [
              { index: true, element: <Navigate to="nos" replace /> },
              { path: 'nos', element: <MapaNos /> }, // T3
              { path: 'arestas', element: <MapaArestas /> }, // T4
              { path: 'fluxos', element: <Fluxos /> }, // T4b — fluxos principais (RF05)
              { path: 'mapa', element: <MapaVisual /> }, // T5
              {
                path: 'radar', // T6
                element: (
                  <Suspense fallback={<Carregando />}>
                    <MetricasRadar />
                  </Suspense>
                ),
              },
              { path: 'recomendacao', element: <Recomendacao /> }, // T7
              { path: 'rastreabilidade', element: <MatrizRastreabilidade /> }, // T8
            ],
          },
          { path: '/perfis', element: <PerfisArquiteturais /> }, // T9
          { path: '/parametros', element: <Parametros /> }, // parâmetros versionados (RF11, 9.5)
          { path: '*', element: <NaoEncontrada /> },
        ],
      },
    ],
  },
]);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <SessaoProvider>
      <RouterProvider router={router} />
    </SessaoProvider>
  </StrictMode>,
);
