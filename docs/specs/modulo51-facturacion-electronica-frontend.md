# Especificación: modulo51-facturacion-electronica-frontend — Facturación electrónica

> Responsable: Leonel Portillo. Propuesta basada en el contrato vigente del PR #48. Revisión de Lukas pendiente.

## 1. Objetivo

Consultar, registrar y gestionar facturas y boletas, con sus líneas, estados y resumen de importes por moneda.

## 2. Fuera de alcance

Intercambio con SUNAT, documentos del 52, cálculo autoritativo de importes en el cliente y cambios al backend. La rama depende del PR #48 mientras la API siga pendiente de integrar.

## 3. Archivos afectados

| Ruta | Cambio |
|---|---|
| `frontend/src/modules/facturacion-electronica/` | Página, formulario, contratos, acceso a API y pruebas. |
| `frontend/src/modules/pantallas.ts` y `frontend/src/styles/app.css` | Navegación y disposición adaptable. |
| `frontend/src/lib/consultaPantalla.ts`, `fechaNegocioPeru.ts`, `frontend/src/components/CampoModulo.tsx` | Utilidades de consulta, día de Perú y campos etiquetados. |

## 4. Diseño y lógica

- Listado con búsqueda por cliente, serie o número, filtros por cliente, tipo, estado, moneda y rango. Paginación del servidor y estados de carga, vacío y error. El resumen se consulta por moneda y rango.
- Alta y edición de borrador con cliente, orden opcional, serie, número, emisión, vencimiento, moneda y líneas. Cada línea selecciona producto e IGV opcional del catálogo y recoge descripción, cantidad, precio y descuento. Se permiten varias líneas. Se muestran las cifras calculadas por el backend.
- La emisión requiere confirmación y está disponible en borrador. La anulación exige motivo en borrador o emitida. El detalle conserva las líneas y el historial de anulación. Los estados disponibles y los mensajes provienen del [contrato backend](modulo51-facturacion-electronica-backend.md).
- Usa los siete endpoints de `/api/v1/facturas`, más consultas de clientes, jerarquía de órdenes, productos e impuestos existentes. Todas las operaciones requieren sesión válida según la política del ERP. Se muestran los errores 400, 401, 403, 404 y 409.
- Las fechas de negocio se muestran en Perú. El alta envía el inicio del día peruano en UTC y la edición conserva el instante cuando el día queda igual. El filtro incluye el intervalo completo de los días seleccionados. El vencimiento futuro sigue admitido.

## 5. Criterios de aceptación

| # | Criterio | Evidencia |
|---|---|---|
| A1 | Rutas, payloads y rangos del contrato | Pruebas de API y calendario. |
| A2 | Listado, carga, vacío, filtros, paginación y detalle | Pruebas de interfaz. |
| A3 | Borrador, edición, emisión y anulación con validaciones y feedback | Pruebas de flujo y errores. |
| A4 | Accesibilidad y disposición a 375 y 1280 px | Navegador en claro y oscuro. |
| A5 | Gate completo del proyecto y smoke del backend | `verify.sh` y `smoke-postgres.sh`. |

## 6. Checklist de ejecución

- [x] Conectar la pantalla al backend publicado del PR #48.
- [ ] Registrar verificación local y visual.

## 7. Registro de verificación

| Fecha | Evidencia | Resultado |
|---|---|---|

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-26 | Formato, reglas y compilación | Correcto | `git diff --check`, `check-rules.sh` y `dotnet build -warnaserror`, 0 advertencias de .NET |
| 2026-11-26 | Gate local | Correcto | `verify.sh`, 1825 pruebas backend y 1470 frontend, lint, TypeScript y build |
| 2026-11-26 | PostgreSQL | Correcto | Scaffold al día, dos cargas idempotentes del seed y `SMOKE OK` en PostgreSQL desechable |
| 2026-11-26 | Edge con API real | Correcto | 375 y 1280 px en claro y oscuro, listado con datos, foco de la acción principal y apertura del formulario y 0 errores de consola |
