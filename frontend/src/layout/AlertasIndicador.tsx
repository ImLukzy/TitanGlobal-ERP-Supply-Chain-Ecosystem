import { useEffect, useState } from 'react';
import { Link } from 'react-router';
import { obtenerResumenAlertas } from '../modules/notificaciones-alertas/api';

export function AlertasIndicador() {
  const [altas, setAltas] = useState<number | null>(null);

  useEffect(() => {
    let activo = true;
    function actualizar(): void {
      void obtenerResumenAlertas().then((resumen) => {
        if (activo && Number.isInteger(resumen.altas) && resumen.altas >= 0) {
          setAltas(resumen.altas);
        }
      }).catch(() => {
        if (activo) {
          setAltas(null);
        }
      });
    }
    function manejarRefresco(): void {
      actualizar();
    }
    window.addEventListener('alertas-resumen-refrescar', manejarRefresco);
    actualizar();
    return () => {
      activo = false;
      window.removeEventListener('alertas-resumen-refrescar', manejarRefresco);
    };
  }, []);

  if (altas === null) {
    return null;
  }
  const etiqueta = altas === 0 ? 'Alertas, sin alertas altas' : `${altas} ${altas === 1 ? 'alerta alta' : 'alertas altas'}`;
  const texto = altas === 0 ? 'Alertas' : `${altas} alertas altas`;
  return (
    <Link aria-label={etiqueta} className="alertas-indicator-link" to="/modulos/78-notificaciones-alertas?severidad=alta">
      <svg aria-hidden="true" className="alertas-indicator-icon" fill="none" viewBox="0 0 24 24">
        <path d="M18 9a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" />
      </svg>
      <span className="alertas-indicator-text">{texto}</span>
      {altas > 0 && <span aria-hidden="true" className="alertas-indicator-count">{altas}</span>}
    </Link>
  );
}
