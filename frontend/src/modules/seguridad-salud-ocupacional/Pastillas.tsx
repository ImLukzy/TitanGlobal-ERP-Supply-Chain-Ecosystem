import { textoValor } from './presentacion';
import type { EstadoIncidente, SeveridadIncidente } from './tipos';

// Todos los estados se muestran con texto, nunca solo con color; Grave y Mortal usan el estilo de alerta.
export function SeveridadPastilla({ severidad }: { severidad: SeveridadIncidente }) {
  const alerta = severidad === 'Grave' || severidad === 'Mortal';
  return <span className={`sst-pastilla ${alerta ? 'is-alerta' : 'is-normal'}`}>{severidad}</span>;
}

export function EstadoIncidentePastilla({ estado }: { estado: EstadoIncidente }) {
  return <span className={`sst-pastilla ${estado === 'Abierto' ? 'is-abierto' : 'is-cerrado'}`}>{estado}</span>;
}

export function ActivoPastilla({ activo }: { activo: boolean }) {
  return <span className={`sst-pastilla ${activo ? 'is-abierto' : 'is-cerrado'}`}>{activo ? 'Activo' : 'Inactivo'}</span>;
}

export function VencimientoPastilla({ vencido, masculino = false }: { vencido: boolean; masculino?: boolean }) {
  return <span className={`sst-pastilla ${vencido ? 'is-alerta' : 'is-vigente'}`}>{vencido ? (masculino ? 'Vencido' : 'Vencida') : 'Vigente'}</span>;
}

export function ResultadoPastilla({ resultado }: { resultado: string }) {
  return <span className={`sst-pastilla ${resultado === 'NoApto' ? 'is-alerta' : 'is-normal'}`}>{textoValor(resultado)}</span>;
}
