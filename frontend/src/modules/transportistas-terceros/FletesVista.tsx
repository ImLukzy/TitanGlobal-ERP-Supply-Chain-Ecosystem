import { useEffect, useRef, useState } from 'react';
import { cancelarFlete, listarFletes, obtenerFlete, registrarFlete } from './api';
import { DialogoCancelarFlete } from './DialogoCancelarFlete';
import { FleteDetallePanel } from './FleteDetallePanel';
import { FleteFormulario } from './FleteFormulario';
import { FletesFiltros } from './FletesFiltros';
import { FletesTabla } from './FletesTabla';
import { mensajeError } from './presentacion';
import { SelectorTransportista } from './SelectorTransportista';
import type { Flete, FleteDetalle, Pagina, RegistrarFlete } from './tipos';

export function FletesVista() {
  const [busqueda, setBusqueda] = useState('');
  const [consulta, setConsulta] = useState('');
  const [estado, setEstado] = useState('');
  const [transportistaId, setTransportistaId] = useState(0);
  const [pagina, setPagina] = useState(1);
  const [revision, setRevision] = useState(0);
  const [datos, setDatos] = useState<Pagina<Flete> | null>(null);
  const [seleccion, setSeleccion] = useState<number | null>(null);
  const [detalle, setDetalle] = useState<FleteDetalle | null>(null);
  const [formulario, setFormulario] = useState(false);
  const [cancelacion, setCancelacion] = useState(false);
  const [ocupado, setOcupado] = useState(false);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState('');
  const [errorDetalle, setErrorDetalle] = useState('');
  const [errorOperacion, setErrorOperacion] = useState('');
  const candado = useRef(false);
  useEffect(() => {
    if (busqueda === consulta) return;
    const timer = setTimeout(() => {
      setConsulta(busqueda);
      setPagina(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [busqueda, consulta]);
  useEffect(() => {
    let vigente = true;
    listarFletes({
      busqueda: consulta,
      estado,
      transportistaId: transportistaId || undefined,
      pagina,
      tamanoPagina: 10,
    })
      .then((r) => {
        if (!vigente) return;
        if (pagina > 1 && r.items.length === 0) {
          setPagina(Math.max(1, Math.ceil(r.total / 10)));
          return;
        }
        setDatos(r);
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
  }, [consulta, estado, transportistaId, pagina, revision]);
  useEffect(() => {
    if (seleccion === null) return;
    let vigente = true;
    obtenerFlete(seleccion)
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
  async function guardar(solicitud: RegistrarFlete) {
    if (candado.current) return;
    candado.current = true;
    setOcupado(true);
    setErrorOperacion('');
    try {
      const resultado = await registrarFlete(solicitud);
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
  async function confirmar(motivo: string) {
    if (!detalle || candado.current) return;
    candado.current = true;
    setOcupado(true);
    setErrorOperacion('');
    try {
      await cancelarFlete(detalle.id, motivo);
      setCancelacion(false);
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
      <div className="transportistas-terceros-toolbar">
        <div>
          <h2>Fletes de terceros</h2>
          <p>Importes en su moneda original, sin conversión ni sumas entre monedas.</p>
        </div>
        <button
          className="primary-button"
          type="button"
          onClick={() => {
            setFormulario(true);
            setErrorOperacion('');
          }}
        >
          Registrar flete
        </button>
      </div>
      {!formulario && (
        <>
          <FletesFiltros
            busqueda={busqueda}
            estado={estado}
            cambiarBusqueda={setBusqueda}
            cambiarEstado={(valor) => {
              setEstado(valor);
              setPagina(1);
            }}
          />
          <SelectorTransportista
            valor={transportistaId}
            cambiar={(id) => {
              setTransportistaId(id);
              setPagina(1);
            }}
          />
        </>
      )}
      {error && (
        <p role="alert">
          {error}{' '}
          <button
            className="secondary-button"
            type="button"
            onClick={() => setRevision((r) => r + 1)}
          >
            Reintentar fletes
          </button>
        </p>
      )}
      {cargando && <p role="status">Cargando fletes…</p>}
      <div className="transportistas-terceros-layout">
        <section className="transportistas-terceros-card" aria-label="Listado de fletes">
          {datos && (
            <>
              <p>{datos.total} fletes</p>
              {datos.items.length === 0 ? (
                <p role="status">Sin fletes coincidentes</p>
              ) : (
                <FletesTabla
                  datos={datos}
                  pagina={pagina}
                  cambiar={setPagina}
                  seleccionar={(id) => {
                    setSeleccion(id);
                    setDetalle(null);
                    setFormulario(false);
                    setErrorDetalle('');
                  }}
                />
              )}
            </>
          )}
        </section>
        <div>
          {formulario ? (
            <FleteFormulario
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
            <FleteDetallePanel
              datos={detalle}
              cancelar={() => {
                setCancelacion(true);
                setErrorOperacion('');
              }}
            />
          ) : (
            <p role="status">
              {seleccion ? 'Cargando detalle…' : 'Selecciona un flete para ver el detalle.'}
            </p>
          )}
        </div>
      </div>
      {cancelacion && detalle && (
        <DialogoCancelarFlete
          referencia={detalle.referencia}
          ocupado={ocupado}
          error={errorOperacion}
          cerrar={() => {
            if (!candado.current) setCancelacion(false);
          }}
          confirmar={confirmar}
        />
      )}
    </>
  );
}
