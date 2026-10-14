import { useEffect, useState } from 'react';
import { ApiError, apiGet } from '../api/client';
import { dominios } from '../modules/catalogo';

type EstadoApi = { estado: 'cargando' | 'en-linea' | 'no-disponible'; mensaje: string };

export function Inicio() {
  const [api, setApi] = useState<EstadoApi>({ estado: 'cargando', mensaje: 'Comprobando conexión' });

  useEffect(() => {
    let vigente = true;
    apiGet<unknown>('/sistema/salud')
      .then(() => {
        if (vigente) {
          setApi({ estado: 'en-linea', mensaje: 'La API responde correctamente.' });
        }
      })
      .catch((error: unknown) => {
        if (vigente) {
          const mensaje = error instanceof ApiError ? error.message : 'No se pudo conectar con la API.';
          setApi({ estado: 'no-disponible', mensaje });
        }
      });
    return () => {
      vigente = false;
    };
  }, []);

  return (
    <div className="page-content home-page">
      <div className="page-eyebrow">CENTRO DE OPERACIONES</div>
      <div className="hero-heading">
        <div>
          <h1>Buenos días</h1>
          <p>Una vista unificada de tu operación empresarial.</p>
        </div>
        <div className={`api-status ${api.estado}`} role="status">
          <span className="status-dot" />
          <span><strong>{api.estado === 'cargando' ? 'Verificando API' : api.estado === 'en-linea' ? 'API en línea' : 'API no disponible'}</strong><small>{api.mensaje}</small></span>
        </div>
      </div>
      <section aria-labelledby="domains-title" className="domains-section">
        <div className="section-heading">
          <div><div className="page-eyebrow">PLATAFORMA</div><h2 id="domains-title">Dominios del negocio</h2></div>
          <span className="section-total">80 módulos</span>
        </div>
        <div className="domain-cards">
          {dominios.map((dominio, indice) => (
            <article className="domain-card" key={dominio.nombre}>
              <div className="card-icon" aria-hidden="true">{String(indice + 1).padStart(2, '0')}</div>
              <div className="card-copy"><h3>{dominio.nombre}</h3><p>10 módulos disponibles</p></div>
              <span aria-hidden="true" className="card-arrow">↗</span>
            </article>
          ))}
        </div>
      </section>
      <footer className="home-footer"><span>TitanGlobal ERP</span><span>Plataforma de gestión empresarial</span></footer>
    </div>
  );
}
