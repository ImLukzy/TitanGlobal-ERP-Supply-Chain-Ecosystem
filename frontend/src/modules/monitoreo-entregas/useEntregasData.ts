import { useEffect, useState } from 'react';
import { listarEntregas, obtenerEntrega, resumenEntregas } from './api';
import { mensajeError } from './presentacion';
import type { EntregaDetalle, PaginaEntregas, ResumenEntregas } from './tipos';
interface Recurso<T> {
  datos: T | null;
  cargando: boolean;
  error: string;
}
const inicio = { datos: null, cargando: true, error: '' };
export function useEntregasData() {
  const [busqueda, setBusqueda] = useState('');
  const [consulta, setConsulta] = useState('');
  const [estado, setEstado] = useState('');
  const [pagina, setPagina] = useState(1);
  const [revision, setRevision] = useState(0);
  const [seleccion, setSeleccion] = useState<number | null>(null);
  const [lista, setLista] = useState<Recurso<PaginaEntregas>>(inicio);
  const [detalle, setDetalle] = useState<Recurso<EntregaDetalle>>({
    ...inicio,
    cargando: false,
  });
  const [resumen, setResumen] = useState<Recurso<ResumenEntregas>>(inicio);
  useEffect(() => {
    if (consulta === busqueda) return;
    const timer = setTimeout(() => {
      setConsulta(busqueda);
      setPagina(1);
      setLista((prev) => ({ ...prev, cargando: true }));
    }, 300);
    return () => clearTimeout(timer);
  }, [busqueda, consulta]);
  useEffect(() => {
    let vigente = true;
    listarEntregas({ busqueda: consulta, estado, pagina, tamanoPagina: 10 })
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
  }, [consulta, estado, pagina, revision]);
  useEffect(() => {
    let vigente = true;
    resumenEntregas()
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
    obtenerEntrega(seleccion)
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
  function cambiarEstado(valor: string) {
    if (valor === estado) return;
    setEstado(valor);
    setPagina(1);
    setLista((prev) => ({ ...prev, cargando: true }));
  }
  function cambiarPagina(valor: number) {
    if (valor === pagina) return;
    setPagina(valor);
    setLista((prev) => ({ ...prev, cargando: true }));
  }
  function refrescar() {
    setRevision((r) => r + 1);
    setLista((prev) => ({ ...prev, cargando: true }));
  }
  return {
    busqueda,
    setBusqueda,
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
