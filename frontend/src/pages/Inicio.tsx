import { useEffect, useState } from 'react';
import { Link } from 'react-router';
import { ApiError, apiGet } from '../api/client';
import { modulosConPantalla } from '../modules/avance';
import { dominios, modulos, type Dominio } from '../modules/catalogo';
import { PanelOperaciones } from './PanelOperaciones';

type EstadoApi = { estado: 'cargando' | 'en-linea' | 'no-disponible'; mensaje: string };

function TarjetaDominio({ dominio, indice }: { dominio: Dominio; indice: number }) {
  const conPantalla = modulosConPantalla(dominio);
  const contenido = (
    <>
      <div className="card-icon" aria-hidden="true">{String(indice + 1).padStart(2, '0')}</div>
      <div className="card-copy">
        <h3>{dominio.nombre}</h3>
        <p>{conPantalla.length} de {dominio.modulos.length} pantallas</p>
        <div aria-hidden="true" className="domain-avance"><span style={{ width: `${(conPantalla.length / dominio.modulos.length) * 100}%` }} /></div>
      </div>
    </>
  );

  if (conPantalla.length === 0) {
    return <article className="domain-card">{contenido}</article>;
  }
  return (
    <Link className="domain-card domain-card-enlace" to={`/modulos/${conPantalla[0].slug}`}>
      {contenido}
      <span aria-hidden="true" className="card-arrow">↗</span>
    </Link>
  );
}

export function Inicio() {
  const [api, setApi] = useState<EstadoApi>({ estado: 'cargando', mensaje: 'Comprobando conexión' });
  const totalConPantalla = dominios.reduce((total, dominio) => total + modulosConPantalla(dominio).length, 0);

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
          <h1>Panel de operaciones</h1>
          <p>Ventas, compras, catálogo y servicio con los datos actuales de la plataforma.</p>
        </div>
        <div className={`api-status ${api.estado}`} role="status">
          <span className="status-dot" />
          <span><strong>{api.estado === 'cargando' ? 'Verificando API' : api.estado === 'en-linea' ? 'API en línea' : 'API no disponible'}</strong><small>{api.mensaje}</small></span>
        </div>
      </div>
      <PanelOperaciones />
      <section aria-labelledby="domains-title" className="domains-section">
        <div className="section-heading">
          <div><div className="page-eyebrow">PLATAFORMA</div><h2 id="domains-title">Dominios del negocio</h2></div>
          <span className="section-total">{totalConPantalla} de {modulos.length} pantallas</span>
        </div>
        <div className="domain-cards">
          {dominios.map((dominio, indice) => (
            <TarjetaDominio dominio={dominio} indice={indice} key={dominio.nombre} />
          ))}
        </div>
      </section>
      <footer className="home-footer"><span>TitanGlobal ERP</span><span>Plataforma de gestión empresarial</span></footer>
    </div>
  );
}
