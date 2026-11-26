# Especificación: modulo54-cuentas-por-pagar-frontend — Cuentas por pagar

**Responsable:** Leonel Portillo

**Estado:** implementada en la rama frontend, pendiente de revisión por Lukas

**Contrato:** [Cuentas por pagar](modulo54-cuentas-por-pagar-backend.md). Requiere el PR backend [#86](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/86) y el rol FINANZAS provisto por SQL 024 en [#83](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/83).

## 1. Objetivo

La cartera de proveedores del módulo 54 tenía el marcador de pantalla pendiente. El equipo de finanzas puede registrar facturas, aplicar y revertir pagos y consultar saldos y vencimientos desde la misma ruta del ERP.

## 2. Fuera de alcance

La v0 conserva el contrato de la API. Se excluyen anticipos, cuotas, retenciones, edición de facturas y pagos automáticos de tesorería. Las precisiones sobre las bandas y la anulación después de revertir todos los pagos siguen en revisión en la especificación backend.

## 3. Archivos afectados

| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/cuentas-por-pagar/` | Crear | Página, formulario, API, tipos y pruebas |
| `frontend/src/modules/pantallas.ts` | Modificar | Registrar el módulo 54 |
| `frontend/src/styles/app.css` | Modificar | Estilos compartidos de las pantallas financieras |
| `frontend/src/components/CampoModulo.tsx` | Crear | Etiquetas conectadas al control |
| `frontend/src/lib/accionesFinanzas.ts`, `consultaPantalla.ts` y `fechaNegocioPeru.ts` | Crear | Operaciones comunes, carga y calendario local con pruebas |

## 4. Diseño y lógica

### Pantallas

La ruta `/modulos/54-cuentas-por-pagar` ofrece una cartera de facturas, panel de detalle y pagos y reporte de vencimientos. El listado filtra por proveedor, moneda y condición vencida y pagina en el servidor. El detalle conserva el proveedor, referencia, fechas, base, impuesto, total, importe pagado y saldo. El historial muestra pagos aplicados o revertidos y sus motivos. El reporte agrupa por proveedor y moneda en los tramos del backend.

La pantalla usa `PaginaModulo`, `TablaPaginada`, `PanelDetalle` y `CampoConError`. Los controles tienen etiquetas y los mensajes de carga, vacío, error y confirmación se anuncian. Las tablas anchas tienen desplazamiento dentro de su región.

### API

Todas las rutas del módulo tienen prefijo `/api/v1/cuentas-por-pagar` y requieren autenticación JWT.

| Método | Ruta relativa | Flujo |
|---|---|---|
| GET | `/` | Filtros y página de facturas |
| GET | `/{id}` | Detalle y saldo de la cuenta |
| GET | `/{id}/pagos` | Historial paginado |
| GET | `/resumen` | Vencimientos por proveedor y moneda |
| POST | `/` | Registrar factura documentaria |
| POST | `/{id}/pagos` | Registrar pago parcial o total |
| POST | `/{id}/pagos/{pagoId}/revertir` | Revertir con motivo |
| POST | `/{id}/anular` | Anular con motivo |

El alta consume los catálogos existentes de proveedores y órdenes de abastecimiento. El cliente HTTP compartido transmite el JWT y muestra los mensajes literales de la API en errores 400, 401, 403, 404 y 409.

### Formularios y permisos

- Usuarios autenticados consultan la cuenta, sus pagos y el reporte. ADMIN y FINANZAS registran, pagan, revierten y anulan.
- El alta exige proveedor activo, serie y número, fecha documentaria, fecha de vencimiento, moneda, base e impuesto. La orden es opcional y del mismo proveedor. El servidor calcula y devuelve el total.
- La fecha documentaria y la de pago se inicializan al día de negocio de Perú. Se envían como `YYYY-MM-DD`. El calendario admite vencimientos futuros.
- El pago exige saldo pendiente y un importe positivo hasta el saldo. La reversión requiere un pago activo y motivo. La anulación lógica requiere motivo y ausencia de pagos vigentes. El backend decide el resultado y, si acepta, se recargan detalle, historial, cartera y reporte.
- Las bandas de vencimiento, la interpretación de anulaciones tras revertir todos los pagos y las demás precisiones conservan el estado de revisión del contrato backend. La interfaz utiliza las reglas vigentes de la API.

## 5. Criterios de aceptación

- La sesión autenticada puede filtrar y paginar, consultar los pagos y distinguir importes PEN y USD. La interfaz reserva las escrituras a ADMIN y FINANZAS.
- El formulario registra una factura con vencimiento futuro. Bloquea omisiones y evita enviar un sobrepago o un motivo vacío.
- Se conserva el historial y se refrescan el saldo y el reporte tras un pago, una reversión o una anulación.
- Pruebas de Vitest con Testing Library cubren los estados visuales, filtros, paginación, formularios y permisos. Las pruebas del cliente cubren métodos, rutas y datos, y el helper de fechas cubre 23:59:59, 00:00:00, 04:59:59 y 05:00:00 UTC.
- El PR frontend usa temporalmente `leonel/modulo54-cuentas-por-pagar` como base. Tras integrar #86 se comprueba el diff y se actualiza la base del PR a `main`.

## 6. Checklist de ejecución

- [x] Integrar cartera, historial y reporte con los contratos existentes.
- [x] Agregar pruebas de roles, calendario, flujos de escritura y errores HTTP.
- [ ] Registrar la aprobación de Lukas.

## 7. Registro de verificación

Los resultados siguientes corresponden al árbol evaluado para el PR de esta pantalla.

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-26 | Formato, reglas y compilación | Correcto | `git diff --check`, `check-rules.sh` y `dotnet build -warnaserror`, 0 advertencias de .NET |
| 2026-11-26 | Gate local | Correcto | `verify.sh`, 1818 pruebas backend y 1474 frontend, lint, TypeScript y build |
| 2026-11-26 | PostgreSQL | Correcto | Scaffold al día, dos cargas idempotentes del seed y `SMOKE OK` en PostgreSQL desechable |
| 2026-11-26 | Edge con API real | Correcto | 375 y 1280 px en claro y oscuro, listado con datos, foco de la acción principal y apertura del formulario y 0 errores de consola |
