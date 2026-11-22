import { useEffect, useState } from 'react';
import { listarSucursales, obtenerSucursal, resumenSucursales } from './api';
import { mensajeError } from './presentacion';
import type { PaginaSucursales, SucursalDetalle, ResumenSucursales } from './tipos';
interface Recurso<T> {
  datos: T | null;
  cargando: boolean;
  error: string;
}
const inicio = { datos: null, cargando: true, error: '' };
export function useSucursalesData() {
  const [busqueda, setBusqueda] = useState(''),
    [pais, setPais] = useState(''),
    [consulta, setConsulta] = useState({ busqueda: '', pais: '' }),
    [tipo, setTipo] = useState(''),
    [estado, setEstado] = useState(''),
    [pagina, setPagina] = useState(1),
    [revision, setRevision] = useState(0),
    [seleccion, setSeleccion] = useState<number | null>(null);
  const [lista, setLista] = useState<Recurso<PaginaSucursales>>(inicio),
    [detalle, setDetalle] = useState<Recurso<SucursalDetalle>>({ ...inicio, cargando: false }),
    [resumen, setResumen] = useState<Recurso<ResumenSucursales>>(inicio);
  useEffect(() => {
    if (consulta.busqueda === busqueda && consulta.pais === pais) return;
    const timer = setTimeout(() => {
      setConsulta({ busqueda, pais });
      setPagina(1);
      setLista((p) => ({ ...p, cargando: true }));
    }, 300);
    return () => clearTimeout(timer);
  }, [busqueda, pais, consulta]);
  useEffect(() => {
    let vigente = true;
    listarSucursales({
      busqueda: consulta.busqueda,
      pais: consulta.pais,
      tipo,
      activa: estado === '' ? undefined : estado === 'true',
      pagina,
      tamanoPagina: 10,
    })
      .then((datos) => {
        if (!vigente) return;
        if (pagina > 1 && datos.items.length === 0) {
          setPagina(Math.max(1, Math.ceil(datos.total / 10)));
          return;
        }
        setLista({ datos, cargando: false, error: '' });
      })
      .catch((e) => {
        if (vigente) setLista({ datos: null, cargando: false, error: mensajeError(e) });
      });
    return () => {
      vigente = false;
    };
  }, [consulta, tipo, estado, pagina, revision]);
  useEffect(() => {
    let vigente = true;
    resumenSucursales()
      .then((datos) => {
        if (vigente) setResumen({ datos, cargando: false, error: '' });
      })
      .catch((e) => {
        if (vigente) setResumen({ datos: null, cargando: false, error: mensajeError(e) });
      });
    return () => {
      vigente = false;
    };
  }, [revision]);
  useEffect(() => {
    if (seleccion === null) return;
    let vigente = true;
    obtenerSucursal(seleccion)
      .then((datos) => {
        if (vigente) setDetalle({ datos, cargando: false, error: '' });
      })
      .catch((e) => {
        if (vigente) setDetalle({ datos: null, cargando: false, error: mensajeError(e) });
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
  function cambiarEstado(v: string) {
    if (v === estado) return;
    setEstado(v);
    setPagina(1);
    setLista((p) => ({ ...p, cargando: true }));
  }
  function cambiarTipo(v: string) {
    if (v === tipo) return;
    setTipo(v);
    setPagina(1);
    setLista((p) => ({ ...p, cargando: true }));
  }
  function cambiarPagina(v: number) {
    if (v === pagina) return;
    setPagina(v);
    setLista((p) => ({ ...p, cargando: true }));
  }
  function refrescar() {
    setRevision((r) => r + 1);
    setLista((p) => ({ ...p, cargando: true }));
  }
  return {
    busqueda,
    setBusqueda,
    pais,
    setPais,
    tipo,
    cambiarTipo,
    estado,
    cambiarEstado,
    pagina,
    cambiarPagina,
    lista,
    detalle,
    resumen,
    seleccion,
    seleccionar,
    refrescar,
  };
}
