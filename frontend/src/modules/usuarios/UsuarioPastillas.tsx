import { textoEstado } from './presentacion';

export function RolesPastillas({ codigos }: { codigos: string[] }) {
  if (codigos.length === 0) {
    return <span className="usuario-sin-rol">Sin rol</span>;
  }
  return (
    <>
      {codigos.map((codigo) => <span className="usuario-rol-pill" key={codigo}>{codigo}</span>)}
    </>
  );
}

export function EstadoPastilla({ activo }: { activo: boolean }) {
  return (
    <span className={`usuario-estado${activo ? ' is-active' : ' is-inactive'}`}>
      {textoEstado(activo)}
    </span>
  );
}
