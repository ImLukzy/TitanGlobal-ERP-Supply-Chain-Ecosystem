# Especificación: modulo75-impuestos-tasas-frontend — Impuestos y tasas

> Responsable: Leonel Portillo. Propuesta de pantalla basada en el contrato backend integrado. Revisión de Lukas pendiente.

## 1. Objetivo

Mantener impuestos y vigencias desde la navegación del ERP. Consultar su historial y calcular un importe sobre una base y una fecha.

## 2. Fuera de alcance

Cambios al contrato tributario, tasas nuevas calculadas por el cliente, transmisión fiscal, borrado físico y edición de tasas existentes. La pantalla usa el backend integrado en main. El ajuste de vigencia del listado a la fecha de Perú se revisa por separado en el [PR #110](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/110).

## 3. Archivos afectados

| Ruta | Cambio |
|---|---|
| `frontend/src/modules/impuestos-tasas/` | Pantalla, contratos TypeScript, cliente HTTP y pruebas. |
| `frontend/src/modules/pantallas.ts` | Registro del módulo 75. |
| `frontend/src/lib/consultaPantalla.ts`, `fechaNegocioPeru.ts`, `frontend/src/components/CampoModulo.tsx` | Carga, fechas y etiquetas de formularios. |
| `frontend/src/styles/app.css` | Catálogo y detalle adaptables con tokens del ERP. |

## 4. Diseño y lógica

- El catálogo muestra código, nombre, tipo, país, tasa vigente y actividad. Búsqueda, filtros y paginación se hacen sobre el listado devuelto por la API. Incluye estados de carga, error y lista vacía.
- El detalle muestra actividad, descripción e historial. Permite alta, edición, registro de tasa, cálculo y confirmación de cambio de actividad. La reactivación guarda el mismo impuesto con `activo: true`. La desactivación usa el DELETE lógico del backend.
- Todos los accesos siguen la autenticación del ERP y el contrato existente del módulo. Los formularios validan campos obligatorios, longitudes, porcentaje y vigencias. Los errores 400, 401, 403, 404 y 409 muestran el literal de la API.
- Carga `GET /api/v1/impuestos` y `GET /api/v1/impuestos/{id}`. Escribe mediante POST, PUT y DELETE según el [contrato backend](modulo75-impuestos-tasas-backend.md). Emite POST a `/{id}/tasas` y `/calcular`; presenta el porcentaje, importe y total del servidor.
- Las fechas de operaciones nuevas se inicializan con el día de Perú. Las fechas explícitas se envían como `yyyy-MM-dd` y el historial conserva el día del contrato.

## 5. Criterios de aceptación

| # | Criterio | Evidencia |
|---|---|---|
| A1 | Rutas, métodos y payloads respetan el backend | Pruebas de `api.ts`. |
| A2 | Carga, filtros, página vacía, detalle y paginación operativos | Pruebas de componente. |
| A3 | Alta, edición, tasa, cálculo y cambio de actividad conservan validación y feedback | Pruebas de formulario y errores. |
| A4 | Campos etiquetados, foco de teclado y diseño adaptable | Navegador a 375 y 1280 px en claro y oscuro. |
| A5 | El día cambia a las 05:00 UTC y el gate pasa | Prueba de frontera, `verify.sh` y smoke del backend. |

## 6. Checklist de ejecución

- [x] Contratos y flujos implementados.
- [ ] Registrar verificaciones de la rama y revisión visual.

## 7. Registro de verificación

| Fecha | Evidencia | Resultado |
|---|---|---|

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-26 | Formato, reglas y compilación | Correcto | `git diff --check`, `check-rules.sh` y `dotnet build -warnaserror`, 0 advertencias de .NET |
| 2026-11-26 | Gate local | Correcto | `verify.sh`, 1759 pruebas backend y 1472 frontend, lint, TypeScript y build |
| 2026-11-26 | PostgreSQL | Correcto | Scaffold al día, dos cargas idempotentes del seed y `SMOKE OK` en PostgreSQL desechable |
| 2026-11-26 | Edge con API real | Correcto | 375 y 1280 px en claro y oscuro, listado con datos, foco de la acción principal y apertura del formulario y 0 errores de consola |
