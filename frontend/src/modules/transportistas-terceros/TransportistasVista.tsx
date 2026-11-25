import { useCallback, useEffect, useRef, useState } from 'react';
import { DialogoBaja } from '../../components/DialogoBaja';
import {
  actualizarTransportista,
  crearTransportista,
  eliminarTransportista,
  listarTransportistas,
  obtenerTransportista,
  resumenTransportistas,
} from './api';
import { mensajeError } from './presentacion';
import { TransportistasFiltros } from './TransportistasFiltros';
import { TransportistasResumen } from './TransportistasResumen';
import { TransportistasTabla } from './TransportistasTabla';
import { TransportistaDetallePanel } from './TransportistaDetallePanel';
import { TransportistaFormulario } from './TransportistaFormulario';
import type {
  GuardarTransportista,
  Pagina,
  Resumen,
  Transportista,
  TransportistaDetalle,
} from './tipos';

export function TransportistasVista() {
  const [filtros, setFiltros] = useState({ busqueda: '', pais: '', servicio: '', activo: '' });
  const [texto, setTexto] = useState({ busqueda: '', pais: '' });
  const [pagina, setPagina] = useState(1);
  const [datos, setDatos] = useState<Pagina<Transportista> | null>(null);
  const [resumen, setResumen] = useState<Resumen | null>(null);
  const [seleccion, setSeleccion] = useState<number | null>(null);
  const [detalle, setDetalle] = useState<TransportistaDetalle | null>(null);
  const [formulario, setFormulario] = useState(false);
  const [baja, setBaja] = useState(false);
  const [ocupado, setOcupado] = useState(false);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorDetalle, setErrorDetalle] = useState('');
  const [errorOperacion, setErrorOperacion] = useState('');
  const [revision, setRevision] = useState(0);
  const candado = useRef(false);
  const origenBaja = useRef<HTMLElement | null>(null);
  useEffect(() => {
    if (texto.busqueda === filtros.busqueda && texto.pais === filtros.pais) return;
    const timer = setTimeout(() => {
      setTexto((prev) =>
        prev.busqueda === filtros.busqueda && prev.pais === filtros.pais
          ? prev
          : { busqueda: filtros.busqueda, pais: filtros.pais },
      );
      setPagina(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [filtros.busqueda, filtros.pais, texto]);
  useEffect(() => {
    let vigente = true;
    listarTransportistas({
      ...texto,
      tipoServicio: filtros.servicio,
      activo: filtros.activo === '' ? undefined : filtros.activo === 'true',
      pagina,
      tamanoPagina: 10,
    })
      .then((respuesta) => {
        if (!vigente) return;
        if (pagina > 1 && respuesta.items.length === 0) {
          setPagina(Math.max(1, Math.ceil(respuesta.total / 10)));
          return;
        }
        setDatos(respuesta);
        setError('');
        setCargando(false);
      })
      .catch((e) => {
        if (vigente) {
          setError(mensajeError(e));
          setCargando(false);
        }
      });
    return () => {
      vigente = false;
    };
  }, [texto, filtros.servicio, filtros.activo, pagina, revision]);
  useEffect(() => {
    let vigente = true;
    resumenTransportistas()
      .then((r) => {
        if (vigente) {
          setResumen(r);
          setErrorResumen('');
        }
      })
      .catch((e) => {
        if (vigente) setErrorResumen(mensajeError(e));
      });
    return () => {
      vigente = false;
    };
  }, [revision]);
  useEffect(() => {
    if (seleccion === null) return;
    let vigente = true;
    obtenerTransportista(seleccion)
      .then((r) => {
        if (vigente) {
          setDetalle(r);
          setErrorDetalle('');
        }
      })
      .catch((e) => {
        if (vigente) setErrorDetalle(mensajeError(e));
      });
    return () => {
      vigente = false;
    };
  }, [seleccion, revision]);
  function seleccionar(id: number) {
    setSeleccion(id);
    setDetalle(null);
    setErrorDetalle('');
    setFormulario(false);
    setErrorOperacion('');
  }
  function cambiar(campo: string, valor: string) {
    setFiltros((prev) => ({ ...prev, [campo]: valor }));
    if (campo !== 'busqueda' && campo !== 'pais') setPagina(1);
    setCargando(true);
  }
  async function guardar(solicitud: GuardarTransportista) {
    if (candado.current) return;
    candado.current = true;
    setOcupado(true);
    setErrorOperacion('');
    try {
      const resultado = detalle
        ? await actualizarTransportista(detalle.id, solicitud)
        : await crearTransportista(solicitud);
      setFormulario(false);
      setSeleccion(resultado.id);
      setDetalle(null);
      setRevision((r) => r + 1);
    } catch (e) {
      setErrorOperacion(mensajeError(e));
    } finally {
      candado.current = false;
      setOcupado(false);
    }
  }
  const cerrarBaja = useCallback(() => {
    if (!candado.current) {
      setBaja(false);
      requestAnimationFrame(() => origenBaja.current?.focus());
    }
  }, []);
  async function confirmarBaja() {
    if (!detalle || candado.current) return;
    candado.current = true;
    setOcupado(true);
    setErrorOperacion('');
    try {
      await eliminarTransportista(detalle.id);
      setBaja(false);
      setDetalle(null);
      setSeleccion(null);
      setRevision((r) => r + 1);
    } catch (e) {
      setErrorOperacion(mensajeError(e));
    } finally {
      candado.current = false;
      setOcupado(false);
    }
  }
  return (
    <>
      {errorResumen ? (
        <p role="alert">
          {errorResumen}{' '}
          <button
            className="secondary-button"
            type="button"
            onClick={() => setRevision((r) => r + 1)}
          >
            Reintentar resumen
          </button>
        </p>
      ) : (
        <TransportistasResumen datos={resumen} />
      )}
      <div className="transportistas-terceros-toolbar">
        <h2>Maestro de transportistas</h2>
        <button
          className="primary-button"
          type="button"
          onClick={() => {
            setFormulario(true);
            setSeleccion(null);
            setDetalle(null);
            setErrorOperacion('');
          }}
        >
          Nuevo transportista
        </button>
      </div>
      <TransportistasFiltros {...filtros} cambiar={cambiar} />
      {error && (
        <p role="alert">
          {error}{' '}
          <button
            className="secondary-button"
            type="button"
            onClick={() => setRevision((r) => r + 1)}
          >
            Reintentar transportistas
          </button>
        </p>
      )}
      {cargando && <p role="status">Cargando transportistas…</p>}
      <div className="transportistas-terceros-layout">
        <section className="transportistas-terceros-card" aria-label="Listado de transportistas">
          {datos && (
            <>
              <p>{datos.total} transportistas</p>
              {datos.items.length === 0 ? (
                <p role="status">Sin transportistas coincidentes</p>
              ) : (
                <TransportistasTabla
                  datos={datos}
                  pagina={pagina}
                  cambiar={setPagina}
                  seleccionar={seleccionar}
                />
              )}
            </>
          )}
        </section>
        <div>
          {formulario ? (
            <TransportistaFormulario
              key={detalle?.id ?? 'nuevo'}
              inicial={detalle}
              ocupado={ocupado}
              error={errorOperacion}
              guardar={guardar}
              cerrar={() => setFormulario(false)}
            />
          ) : errorDetalle ? (
            <p role="alert">
              {errorDetalle}
              <button type="button" onClick={() => setRevision((r) => r + 1)}>
                Reintentar detalle
              </button>
            </p>
          ) : detalle ? (
            <TransportistaDetallePanel
              datos={detalle}
              editar={() => {
                setFormulario(true);
                setErrorOperacion('');
              }}
              eliminar={() => {
                origenBaja.current = document.activeElement as HTMLElement;
                setBaja(true);
                setErrorOperacion('');
              }}
            />
          ) : (
            <p role="status">
              {seleccion ? 'Cargando detalle…' : 'Selecciona un transportista para ver el detalle.'}
            </p>
          )}
        </div>
      </div>
      {baja && detalle && (
        <DialogoBaja
          entidad="transportista"
          nombre={detalle.razonSocial}
          cerrar={cerrarBaja}
          confirmar={confirmarBaja}
          eliminando={ocupado}
          error={errorOperacion}
        />
      )}
    </>
  );
}
