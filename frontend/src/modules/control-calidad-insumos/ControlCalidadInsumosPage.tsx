import { useEffect, useRef, useState } from 'react';
import { PaginaModulo } from '../../components/PaginaModulo';
import { listarInspeccionesInsumos, obtenerInspeccionInsumo, obtenerResumenInspeccionesInsumos } from './api';
import { AnularInspeccionInsumoDialog } from './AnularInspeccionInsumoDialog';
import { DictamenInsumoDialog } from './DictamenInsumoDialog';
import { InspeccionesInsumosFiltros, type ValoresFiltros } from './InspeccionesInsumosFiltros';
import { InspeccionesInsumosResumen } from './InspeccionesInsumosResumen';
import { InspeccionesInsumosTabla } from './InspeccionesInsumosTabla';
import { InspeccionInsumoDetallePanel } from './InspeccionInsumoDetallePanel';
import { InspeccionInsumoFormulario } from './InspeccionInsumoFormulario';
import type { InspeccionInsumoDetalle, PaginaInspeccionesInsumos, ResumenInspeccionesInsumos } from './tipos';

const tamanoPagina = 20;
const esperaBusquedaMs = 300;
const filtrosIniciales: ValoresFiltros = { busqueda: '', producto: null, proveedor: null, estado: '', desde: '', hasta: '' };

type Resultado<T> = { datos: T; error?: never } | { datos?: never; error: string };
type Dialogo = { tipo: 'dictaminar' | 'anular'; inspeccion: InspeccionInsumoDetalle };

function consultar<T>(peticion: Promise<T>, recibir: (resultado: Resultado<T>) => void): () => void {
  let vigente = true;
  peticion
    .then((datos) => {
      if (vigente) recibir({ datos });
    })
    .catch((causa: unknown) => {
      if (vigente) recibir({ error: causa instanceof Error ? causa.message : 'No se pudo cargar la información.' });
    });
  return () => {
    vigente = false;
  };
}

export function ControlCalidadInsumosPage() {
  const [filtros, setFiltros] = useState(filtrosIniciales);
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [pagina, setPagina] = useState(1);
  const [seleccion, setSeleccion] = useState<number | null>(null);
  const [revision, setRevision] = useState(0);
  const [lectura, setLectura] = useState<{ clave: string; resultado: Resultado<PaginaInspeccionesInsumos> }>();
  const [resumen, setResumen] = useState<{ clave: number; resultado: Resultado<ResumenInspeccionesInsumos> }>();
  const [detalle, setDetalle] = useState<{ clave: string; resultado: Resultado<InspeccionInsumoDetalle> }>();
  const [formulario, setFormulario] = useState<{ inspeccion: InspeccionInsumoDetalle | null }>();
  const [dialogo, setDialogo] = useState<Dialogo>();
  const [aviso, setAviso] = useState('');
  const focoPendiente = useRef<string | null>(null);
  const rangoInvalido = filtros.desde !== '' && filtros.hasta !== '' && filtros.desde > filtros.hasta;
  const busqueda = busquedaAplicada.trim();
  const productoId = filtros.producto?.id;
  const proveedorId = filtros.proveedor?.id;
  const { estado, desde, hasta } = filtros;
  const claveLectura = [busqueda, productoId ?? '', proveedorId ?? '', estado, desde, hasta, pagina, revision].join('/');
  const claveDetalle = `${seleccion ?? ''}/${revision}`;
  const resultadoLista = lectura?.clave === claveLectura ? lectura.resultado : undefined;
  const resultadoResumen = resumen?.clave === revision ? resumen.resultado : undefined;
  const resultadoDetalle = detalle?.clave === claveDetalle ? detalle.resultado : undefined;
  const datos = resultadoLista?.datos;

  useEffect(() => {
    const temporizador = window.setTimeout(() => setBusquedaAplicada(filtros.busqueda), esperaBusquedaMs);
    return () => window.clearTimeout(temporizador);
  }, [filtros.busqueda]);

  useEffect(() => {
    if (rangoInvalido) {
      return undefined;
    }
    const peticion = listarInspeccionesInsumos({ busqueda, productoId, proveedorId, estado, desde, hasta, pagina, tamanoPagina });
    return consultar(peticion, (resultado) => setLectura({ clave: claveLectura, resultado }));
  }, [busqueda, claveLectura, desde, estado, hasta, pagina, productoId, proveedorId, rangoInvalido]);

  useEffect(() => consultar(obtenerResumenInspeccionesInsumos(), (resultado) => setResumen({ clave: revision, resultado })), [revision]);

  useEffect(() => {
    if (seleccion === null) {
      return undefined;
    }
    return consultar(obtenerInspeccionInsumo(seleccion), (resultado) => setDetalle({ clave: claveDetalle, resultado }));
  }, [claveDetalle, seleccion]);

  useEffect(() => {
    if (focoPendiente.current !== null && dialogo === undefined && formulario === undefined) {
      const objetivo = focoPendiente.current;
      focoPendiente.current = null;
      window.requestAnimationFrame(() => {
        (document.getElementById(objetivo) ?? document.getElementById('control-calidad-insumos-detalle'))?.focus();
      });
    }
  }, [dialogo, formulario, resultadoDetalle]);

  function cambiarFiltros(cambios: Partial<ValoresFiltros>): void {
    setFiltros((actuales) => ({ ...actuales, ...cambios }));
    setPagina(1);
  }

  function actualizar(): void {
    setRevision((actual) => actual + 1);
  }

  function cerrarFormulario(objetivo: string): void {
    focoPendiente.current = objetivo;
    setFormulario(undefined);
  }

  function cerrarDialogo(objetivo: string): void {
    focoPendiente.current = objetivo;
    setDialogo(undefined);
  }

  function terminarDialogo(mensaje: string): void {
    setAviso(mensaje);
    cerrarDialogo('control-calidad-insumos-detalle');
    actualizar();
  }

  return (
    <PaginaModulo className="control-calidad-insumos-page" modulo="36-control-calidad-insumos">
      <div inert={dialogo !== undefined}>
        <p className="module-intro">Registra la inspección de cada lote de insumo recibido, dictamínalo según la tolerancia de defectos y sigue la tasa de rechazo por proveedor.</p>
        {aviso && <p className="control-calidad-insumos-aviso" role="status">{aviso}</p>}
        <InspeccionesInsumosResumen datos={resultadoResumen?.datos} error={resultadoResumen?.error} reintentar={actualizar} />
        {formulario ? (
          <InspeccionInsumoFormulario
            cancelar={() => cerrarFormulario(formulario.inspeccion ? 'control-calidad-insumos-editar' : 'control-calidad-insumos-nueva')}
            guardado={(id) => {
              setSeleccion(id);
              setAviso(formulario.inspeccion ? 'Inspección actualizada.' : 'Inspección registrada.');
              cerrarFormulario('control-calidad-insumos-detalle');
              actualizar();
            }}
            inspeccion={formulario.inspeccion}
          />
        ) : (
          <div className="control-calidad-insumos-area">
            <section aria-labelledby="control-calidad-insumos-listado" className="control-calidad-insumos-tarjeta">
              <div className="control-calidad-insumos-encabezado">
                <h2 id="control-calidad-insumos-listado" tabIndex={-1}>Inspecciones</h2>
                <button
                  className="primary-button"
                  id="control-calidad-insumos-nueva"
                  onClick={() => {
                    setAviso('');
                    setFormulario({ inspeccion: null });
                  }}
                  type="button"
                >
                  Nueva inspección
                </button>
              </div>
              <InspeccionesInsumosFiltros cambiar={cambiarFiltros} rangoInvalido={rangoInvalido} valores={filtros} />
              {!rangoInvalido && resultadoLista === undefined && <p role="status">Cargando inspecciones…</p>}
              {resultadoLista?.error && (
                <div className="control-calidad-insumos-error">
                  <p className="field-error" role="alert">{resultadoLista.error}</p>
                  <button className="secondary-button" onClick={actualizar} type="button">Reintentar listado</button>
                </div>
              )}
              {!rangoInvalido && datos?.items.length === 0 && <p role="status">No se encontraron inspecciones.</p>}
              {!rangoInvalido && datos && datos.items.length > 0 && (
                <InspeccionesInsumosTabla
                  cambiarPagina={setPagina}
                  datos={datos}
                  seleccion={seleccion}
                  seleccionar={(id) => {
                    setSeleccion(id);
                    document.getElementById('control-calidad-insumos-detalle')?.focus();
                  }}
                />
              )}
            </section>
            <InspeccionInsumoDetallePanel
              anular={(inspeccion) => {
                setAviso('');
                setDialogo({ tipo: 'anular', inspeccion });
              }}
              dictaminar={(inspeccion) => {
                setAviso('');
                setDialogo({ tipo: 'dictaminar', inspeccion });
              }}
              editar={(inspeccion) => {
                setAviso('');
                setFormulario({ inspeccion });
              }}
              error={resultadoDetalle?.error}
              inspeccion={resultadoDetalle?.datos}
              reintentar={actualizar}
              seleccion={seleccion}
            />
          </div>
        )}
      </div>
      {dialogo?.tipo === 'dictaminar' && (
        <DictamenInsumoDialog
          cerrar={() => cerrarDialogo('control-calidad-insumos-dictaminar')}
          confirmado={() => terminarDialogo('Inspección dictaminada.')}
          inspeccion={dialogo.inspeccion}
        />
      )}
      {dialogo?.tipo === 'anular' && (
        <AnularInspeccionInsumoDialog
          cerrar={() => cerrarDialogo('control-calidad-insumos-anular')}
          confirmado={() => terminarDialogo('Inspección anulada.')}
          inspeccion={dialogo.inspeccion}
        />
      )}
    </PaginaModulo>
  );
}
