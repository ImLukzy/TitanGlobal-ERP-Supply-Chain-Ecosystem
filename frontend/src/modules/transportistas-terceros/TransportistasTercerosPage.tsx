import { Link, useSearchParams } from 'react-router';
import { PaginaModulo } from '../../components/PaginaModulo';
import { FletesVista } from './FletesVista';
import { TransportistasVista } from './TransportistasVista';
export function TransportistasTercerosPage() {
  const [params] = useSearchParams();
  const fletes = params.get('vista') === 'fletes';
  return (
    <PaginaModulo modulo={48} className="transportistas-terceros-page">
      <p>Gestiona proveedores de transporte y el historial de sus fletes.</p>
      <nav aria-label="Vistas de transportistas terceros" className="transportistas-terceros-tabs">
        <Link
          to="?vista=transportistas"
          aria-current={!fletes ? 'page' : undefined}
          className={!fletes ? 'transportistas-terceros-tab-active' : ''}
        >
          Transportistas
        </Link>
        <Link
          to="?vista=fletes"
          aria-current={fletes ? 'page' : undefined}
          className={fletes ? 'transportistas-terceros-tab-active' : ''}
        >
          Fletes
        </Link>
      </nav>
      {fletes ? <FletesVista /> : <TransportistasVista />}
    </PaginaModulo>
  );
}
