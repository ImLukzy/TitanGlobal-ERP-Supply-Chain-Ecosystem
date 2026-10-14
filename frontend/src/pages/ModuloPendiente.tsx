import { useParams } from 'react-router';
import { modulos } from '../modules/catalogo';

export function ModuloPendiente() {
  const { slug } = useParams();
  const modulo = modulos.find((item) => item.slug === slug);

  if (modulo === undefined) {
    return (
      <div className="page-content">
        <div className="not-found-state"><span aria-hidden="true">?</span><h1>Módulo no encontrado</h1><p>Revisa la dirección o selecciona un módulo desde la navegación.</p></div>
      </div>
    );
  }

  return (
    <div className="page-content module-page">
      <div className="breadcrumbs"><span>Dominios</span><span aria-hidden="true">/</span><span>{modulo.dominio}</span></div>
      <div className="page-eyebrow">MÓDULO {String(modulo.numero).padStart(2, '0')}</div>
      <h1>{String(modulo.numero).padStart(2, '0')} · {modulo.nombre}</h1>
      <p className="module-domain">{modulo.dominio}</p>
      <section className="pending-panel">
        <div className="pending-icon" aria-hidden="true">✦</div>
        <div><div className="page-eyebrow">EN PREPARACIÓN</div><h2>Pantalla pendiente</h2><p>La experiencia de este módulo estará disponible en una próxima entrega.</p></div>
      </section>
    </div>
  );
}
