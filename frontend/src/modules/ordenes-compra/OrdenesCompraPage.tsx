import { Analitica } from './Analitica';
import { NuevaOrdenForm } from './NuevaOrdenForm';
import { PaginaModulo } from '../../components/PaginaModulo';

export function OrdenesCompraPage() {
  return (
    <PaginaModulo modulo="04-ordenes-compra">
      <p className="module-intro">Gestión de pedidos de clientes y análisis de ventas.</p>
      <NuevaOrdenForm />
      <Analitica />
    </PaginaModulo>
  );
}
