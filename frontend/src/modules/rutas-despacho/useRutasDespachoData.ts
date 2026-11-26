import { useEffect, useState } from 'react';
import { listarRutas, listarZonas, obtenerRuta, resumenRutas } from './api';
import { mensajeError } from './presentacion';
import type { PaginaRutas, ResumenRutas, RutaDetalle } from './tipos';

interface Carga<T> {
  datos: T | null;
  cargando: boolean;
  error: string;
}
const inicio = { datos: null, cargando: true, error: '' };
export function useRutasDespachoData() {
  const [filtros, setFiltros] = useState({ busqueda: '', zona: '', activo: '' });
  const [consulta, setConsulta] = useState('');
  const [pagina, setPagina] = useState(1);
  const [revision, setRevision] = useState(0);
  const [seleccion, setSeleccion] = useState<number | null>(null);
  const [listado, setListado] = useState<Carga<PaginaRutas>>(inicio);
  const [resumen, setResumen] = useState<Carga<ResumenRutas>>(inicio);
  const [zonas, setZonas] = useState<Carga<string[]>>(inicio);
  const [detalle, setDetalle] = useState<Carga<RutaDetalle>>({ ...inicio, cargando: false });
  useEffect(() => {
    if (consulta === filtros.busqueda) return;
    const timer = setTimeout(() => {
      setConsulta(filtros.busqueda);
      setPagina(1);
      setListado((prev) => ({ ...prev, cargando: true }));
    }, 300);
    return () => clearTimeout(timer);
  }, [consulta, filtros.busqueda]);
  useEffect(() => {
    let vigente = true;
    listarRutas({
      busqueda: consulta,
      zona: filtros.zona,
      activo: filtros.activo === '' ? undefined : filtros.activo === 'true',
      pagina,
      tamanoPagina: 10,
    })
      .then((datos) => {
        if (!vigente) return;
        if (pagina > 1 && datos.items.length === 0) {
          setPagina(Math.max(1, Math.ceil(datos.total / 10)));
          return;
        }
        setListado({ datos, cargando: false, error: '' });
      })
      .catch((error) => {
        if (vigente) setListado({ datos: null, cargando: false, error: mensajeError(error) });
      });
    return () => {
      vigente = false;
    };
  }, [consulta, filtros.zona, filtros.activo, pagina, revision]);
  useEffect(() => {
    let vigente = true;
    listarZonas()
      .then((datos) => {
        if (vigente) setZonas({ datos, cargando: false, error: '' });
      })
      .catch((error) => {
        if (vigente) setZonas({ datos: null, cargando: false, error: mensajeError(error) });
      });
    return () => {
      vigente = false;
    };
  }, [revision]);
  useEffect(() => {
    let vigente = true;
    resumenRutas()
      .then((datos) => {
        if (vigente) setResumen({ datos, cargando: false, error: '' });
      })
      .catch((error) => {
        if (vigente) setResumen({ datos: null, cargando: false, error: mensajeError(error) });
      });
    return () => {
      vigente = false;
    };
  }, [revision]);
  useEffect(() => {
    if (seleccion === null) return;
    let vigente = true;
    obtenerRuta(seleccion)
      .then((datos) => {
        if (vigente) setDetalle({ datos, cargando: false, error: '' });
      })
      .catch((error) => {
        if (vigente) setDetalle({ datos: null, cargando: false, error: mensajeError(error) });
      });
    return () => {
      vigente = false;
    };
  }, [seleccion, revision]);
  function seleccionar(id: number | null) {
    if (id === seleccion) return;
    setSeleccion(id);
    setDetalle({ ...inicio, cargando: id !== null });
  }
  function cambiarFiltro(campo: 'busqueda' | 'zona' | 'activo', valor: string) {
    if (filtros[campo] === valor) return;
    setFiltros((prev) => ({ ...prev, [campo]: valor }));
    if (campo !== 'busqueda') {
      setPagina(1);
      setListado((prev) => ({ ...prev, cargando: true }));
    }
  }
  function cambiarPagina(valor: number) {
    setPagina(valor);
    setListado((prev) => ({ ...prev, cargando: true }));
  }
  function refrescar() {
    setListado((prev) => ({ ...prev, cargando: true }));
    setResumen((prev) => ({ ...prev, cargando: true }));
    setZonas((prev) => ({ ...prev, cargando: true }));
    setRevision((prev) => prev + 1);
  }
  return {
    filtros,
    pagina,
    seleccion,
    listado,
    resumen,
    zonas,
    detalle,
    seleccionar,
    cambiarFiltro,
    cambiarPagina,
    refrescar,
  };
}
