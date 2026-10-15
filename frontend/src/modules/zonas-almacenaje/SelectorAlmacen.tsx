import type { AlmacenListado } from '../almacenes/tipos';

interface Props {
  almacenes: AlmacenListado[];
  almacenId: number | '';
  cambiar: (almacenId: number | '') => void;
}

export function SelectorAlmacen({ almacenes, almacenId, cambiar }: Props) {
  return (
    <div className="zonas-store-selector">
      <label htmlFor="zonas-almacen-selector">Almacén</label>
      <select
        id="zonas-almacen-selector"
        onChange={(event) => cambiar(event.currentTarget.value === '' ? '' : Number(event.currentTarget.value))}
        value={almacenId === '' ? '' : String(almacenId)}
      >
        <option value="">Todos</option>
        {almacenes.map((almacen) => (
          <option key={almacen.id} value={almacen.id}>{almacen.codigo} · {almacen.nombre}</option>
        ))}
      </select>
    </div>
  );
}
