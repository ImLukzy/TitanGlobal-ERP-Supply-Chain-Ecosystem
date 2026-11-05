import { useEffect, useState } from 'react';
import { Link } from 'react-router';
import './panel-operaciones.css';
import {
  barrasCategorias,
  barrasClientes,
  barrasVendidos,
  cargarPanel,
  construirAtencion,
  construirIndicadores,
  construirProcesos,
  panelIncompleto,
  panelSinDatos,
  type DatosPanel,
  type FilaBarra,
  type Proceso,
} from './datosPanelOperaciones';

function Barras({ id, titulo, slug, filas }: { id: string; titulo: string; slug: string; filas: FilaBarra[] | null }) {
  const maximo = filas === null ? 0 : Math.max(...filas.map((fila) => fila.valor), 0);
  return (
    <section aria-labelledby={id} className="panel-tarjeta">
      <header className="panel-tarjeta-titulo">
        <h3 id={id}>{titulo}</h3>
        <Link to={`/modulos/${slug}`}>Ver módulo</Link>
      </header>
      {filas === null && <p className="panel-vacio">No disponible</p>}
      {filas !== null && filas.length === 0 && <p className="panel-vacio">Sin datos todavía</p>}
      {filas !== null && filas.length > 0 && (
        <ol className="panel-barras">
          {filas.map((fila) => (
            <li key={fila.id} title={`${fila.etiqueta}: ${fila.texto}`}>
              <div className="panel-barra-texto"><span>{fila.etiqueta}</span><strong>{fila.texto}</strong></div>
              <div aria-hidden="true" className="panel-barra-pista">
                <span style={{ width: `${maximo > 0 ? Math.max((fila.valor / maximo) * 100, 2) : 0}%` }} />
              </div>
            </li>
          ))}
        </ol>
      )}
    </section>
  );
}

function FilaProceso({ proceso }: { proceso: Proceso }) {
  const segmentos = [
    { clase: 'completado', nombre: 'Completado', valor: proceso.completado },
    { clase: 'en-curso', nombre: 'En curso', valor: proceso.enCurso },
    { clase: 'descartado', nombre: 'Descartado', valor: proceso.descartado },
  ];
  return (
    <li>
      <Link className="panel-proceso-nombre" to={`/modulos/${proceso.slug}`}>{proceso.nombre}</Link>
      <div aria-hidden="true" className="panel-proceso-pista">
        {segmentos.filter((segmento) => segmento.valor > 0).map((segmento) => (
          <span className={`panel-segmento ${segmento.clase}`} key={segmento.clase} style={{ flexGrow: segmento.valor }} title={`${segmento.nombre}: ${segmento.valor}`} />
        ))}
      </div>
      <dl className="panel-proceso-valores">
        {segmentos.map((segmento) => (
          <div key={segmento.clase}>
            <dt><span aria-hidden="true" className={`panel-muestra ${segmento.clase}`} />{segmento.nombre}</dt>
            <dd>{segmento.valor}</dd>
          </div>
        ))}
      </dl>
    </li>
  );
}

function Tablero({ datos }: { datos: DatosPanel }) {
  const atencion = construirAtencion(datos);
  const procesos = construirProcesos(datos);
  const incompleto = panelIncompleto(datos);
  return (
    <>
      {incompleto && <p className="clientes-status" role="status">Algunos indicadores no están disponibles en este momento.</p>}
      <section aria-label="Indicadores principales" className="panel-indicadores">
        {construirIndicadores(datos).map((indicador) => (
          <Link className="panel-indicador" key={indicador.slug} to={`/modulos/${indicador.slug}`}>
            <span>{indicador.etiqueta}</span>
            <strong>{indicador.valor ?? 'No disponible'}</strong>
            <small>{indicador.detalle}</small>
          </Link>
        ))}
      </section>
      <div className="panel-columnas">
        <section aria-labelledby="panel-atencion-titulo" className="panel-tarjeta">
          <header className="panel-tarjeta-titulo"><h3 id="panel-atencion-titulo">Requiere atención</h3></header>
          {atencion.length === 0 && (
            <p className="panel-vacio">{incompleto ? 'No se pudo comprobar todo; revisa cada módulo.' : 'Nada pendiente por ahora.'}</p>
          )}
          {atencion.length > 0 && (
            <ul className="panel-atencion">
              {atencion.map((punto) => (
                <li key={punto.etiqueta}>
                  <Link to={`/modulos/${punto.slug}`}>
                    <span className={`panel-nivel ${punto.nivel}`}>{punto.nivel === 'critico' ? 'Urgente' : 'Aviso'}</span>
                    <span className="panel-atencion-texto">{punto.etiqueta}</span>
                    <strong>{punto.cantidad}</strong>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </section>
        <section aria-labelledby="panel-procesos-titulo" className="panel-tarjeta">
          <header className="panel-tarjeta-titulo"><h3 id="panel-procesos-titulo">Estado de los procesos</h3></header>
          {procesos.length === 0 && <p className="panel-vacio">No disponible</p>}
          {procesos.length > 0 && <ul className="panel-procesos">{procesos.map((proceso) => <FilaProceso key={proceso.slug} proceso={proceso} />)}</ul>}
        </section>
      </div>
      <div className="panel-graficos">
        <Barras filas={datos.clientes === null ? null : barrasClientes(datos.clientes)} id="panel-clientes-titulo" slug="01-clientes" titulo="Compras por cliente" />
        <Barras filas={datos.productos === null ? null : barrasVendidos(datos.productos)} id="panel-vendidos-titulo" slug="21-productos" titulo="Ventas por producto" />
        <Barras filas={datos.productos === null ? null : barrasCategorias(datos.productos)} id="panel-categorias-titulo" slug="21-productos" titulo="Catálogo por categoría" />
      </div>
    </>
  );
}

export function PanelOperaciones() {
  const [datos, setDatos] = useState<DatosPanel | null>(null);

  useEffect(() => {
    let vigente = true;
    void cargarPanel().then((resultado) => {
      if (vigente) {
        setDatos(resultado);
      }
    });
    return () => {
      vigente = false;
    };
  }, []);

  if (datos === null) {
    return <p className="clientes-status panel-cargando" role="status">Cargando indicadores…</p>;
  }
  if (panelSinDatos(datos)) {
    return <p className="clientes-error panel-cargando" role="alert">No se pudieron cargar los indicadores.</p>;
  }
  return <Tablero datos={datos} />;
}
