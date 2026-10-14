import { useParams } from 'react-router';
import { PaginaModulo } from '../components/PaginaModulo';
import { modulos } from '../modules/catalogo';

export function ModuloPendiente() {
  const { slug } = useParams();
  const modulo = modulos.find((item) => item.slug === slug);

  if (modulo === undefined) {
    return (
      <div className="page-content">
        <div className="not-found-state">
          <span aria-hidden="true">?</span>
          <h1>Módulo no encontrado</h1>
          <p>Revisa la dirección o selecciona un módulo desde la navegación.</p>
        </div>
      </div>
    );
  }

  return (
    <PaginaModulo modulo={modulo.slug}>
      <section className="pending-panel">
        <div className="pending-icon" aria-hidden="true">✦</div>
        <div>
          <div className="page-eyebrow">EN PREPARACIÓN</div>
          <h2>Pantalla pendiente</h2>
          <p>La experiencia de este módulo estará disponible en una próxima entrega.</p>
        </div>
      </section>
    </PaginaModulo>
  );
}
