import { Link } from 'react-router';
import { EstadoVazio } from '../componentes/Estados';

export function NaoEncontrada() {
  return (
    <EstadoVazio titulo="Página não encontrada">
      <Link to="/">Voltar para os Estudos de Caso</Link>
    </EstadoVazio>
  );
}
