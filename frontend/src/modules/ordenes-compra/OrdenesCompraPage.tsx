import { Analitica } from './Analitica';
import { NuevaOrdenForm } from './NuevaOrdenForm';

export function OrdenesCompraPage() {
  return (
    <div className="page-content business-page">
      <div className="breadcrumbs"><span>Dominios</span><span aria-hidden="true">/</span><span>Comercial, Ventas y CRM</span></div>
      <div className="business-page-heading">
        <div className="page-eyebrow">MÓDULO 04 · COMERCIAL</div>
        <h1>04 · OrdenesCompra</h1>
        <p>Gestión de pedidos de clientes y análisis de ventas.</p>
      </div>
      <NuevaOrdenForm />
      <Analitica />
    </div>
  );
}
