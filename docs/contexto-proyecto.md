# Contexto del proyecto TitanGlobal

Documento único con todo lo que hay que saber del proyecto: qué es, qué pide el examen, cómo está construido, qué está implementado, qué decisiones se tomaron y qué falta. **Se actualiza en cada integración** (ver §11). Última actualización: 2026-10-15.

## 1. Qué es
ERP transaccional para una corporación manufacturera y logística ficticia, **TitanGlobal**: 80 módulos en 8 dominios (comercial y CRM, compras e importaciones, inventarios WMS, manufactura MRP, logística TMS, finanzas, capital humano HCM y gobierno y seguridad). La lista completa está en [`arquitectura.md`](arquitectura.md).

- **Demo:** https://titanglobal-erp.vercel.app (la API va detrás de `/api`; tras ~15 min sin uso tarda ~1 min en despertar).
- **Código:** GitHub `ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem` (privado). Rama de integración y despliegue: `ImLukzy/Claudio`; `main` se avanza con el OK de Lukas.

## 2. Qué se evalúa (Lab 08 / Examen)
El enunciado completo está en [`enunciado.md`](enunciado.md). Resumen:
- **Backend en .NET 10** con EF Core y PostgreSQL, arquitectura por módulos (vertical slices).
- **Modelo del enunciado:** `Cliente`, `Producto`, `OrdenCompra`, `DetalleOrdenCompra` (no se modifican).
- **Contratos fijos:** `IGenericRepository<T>` (`AsQueryable`, `AddAsync`, `Update`, `Remove`) e `IUnitOfWork` (`Repository<T>()`, `CompleteAsync()`).
- **12 patrones LINQ** en el módulo de órdenes de compra (`api/v1/ordenes-compra`).
- **3 middlewares** con mensajes literales: 500 «Ocurrió un error interno en el servidor.», 400 «El cuerpo de la petición (Body) no puede estar vacío.», 403 «Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.»
- Además, el equipo decidió (y el humano aprobó) hacer **frontend** en React.

**Estado frente al examen: todo lo que pide el enunciado está implementado y probado contra PostgreSQL real** (specs 01, 02, 05 y 06).

## 3. Stack
| Capa | Tecnología |
|---|---|
| API | .NET 10, ASP.NET Core con controladores, OpenAPI nativo |
| Datos | EF Core + Npgsql, PostgreSQL 16 en local (Docker, puerto 5433) y 18 en Neon |
| Tests backend | xUnit + `WebApplicationFactory` + EF InMemory; smoke contra PostgreSQL real |
| Frontend | React 19, React Router, Vite, TypeScript estricto, Vitest, ESLint sin warnings |
| Despliegue | Neon (BD), Render (API en Docker), Vercel (frontend) |

## 4. Arquitectura
- **Vertical slices:** `src/TitanGlobal.Api/Modules/ModuloNN_Nombre/{Controllers,Dtos,UseCases}`; `NN` sigue el orden de `arquitectura.md`, y el namespace sigue a la carpeta.
- **UseCases** concretos sin interfaz, registrados por reflexión. Controladores delgados. Validaciones en un helper estático por módulo (`ValidacionX`).
- **Persistencia** solo con `IUnitOfWork` / `IGenericRepository<T>`, consultas asíncronas proyectadas a DTO (sin N+1).
- **Pipeline fijo:** ErrorHandling (500) → Routing → ParameterValidation (400) → RoleBasedAccess (403, cabecera `X-User-Role`: `Bloqueado` y `SinPermiso` se rechazan) → Authorization → Controllers.
- **Errores de negocio:** `ReglaNegocioException(statusCode, mensaje)` → `{ StatusCode, Message }` sin `Detail` (400, 404, 409). Las carreras de PostgreSQL se traducen: `23505` → 409 y `23503` → 400/409.
- **Acciones POST sin cuerpo** (`aprobar`, `recibir`): atributo `[PermitirCuerpoVacio]`; el middleware no contiene rutas.
- **Fechas:** se guardan en UTC (`FechasUtc.Normalizar`); el frontend las muestra en UTC.
- **Números de documento** (`SC-000NNN`, `OA-000NNN`): los asigna el servidor a partir del Id, dentro de una transacción (`TransactionScope`). Los datos de ejemplo usan `SC-DEV-…` y `OA-DEV-…` para no chocar.
- **Reglas de código** (las revisa la auditoría): [`../CLAUDE.md`](../CLAUDE.md) — sin `var`, sin `=>` en miembros, una sentencia por línea, nombres y comentarios en español, migraciones solo con `dotnet ef`.

## 5. Modelo de datos
| Entidad | Módulo | Relaciones y notas |
|---|---|---|
| `Cliente` | enunciado / 01 | RUC único (11 dígitos) |
| `Producto` | enunciado / 21 | categoría como texto; sin navegaciones nuevas |
| `OrdenCompra`, `DetalleOrdenCompra` | enunciado / 04 | pedidos de clientes; número único |
| `Proveedor` | 11 | RUC único, país, email y teléfono opcionales, `Activo` |
| `SolicitudCompra`, `DetalleSolicitudCompra` | 13 | estados Pendiente → Aprobada / Rechazada; proveedor sugerido opcional |
| `OrdenAbastecimiento`, `DetalleOrdenAbastecimiento` | 15 | estados Emitida → Recibida / Anulada; origen manual o desde una solicitud aprobada (una por solicitud) |
| `Almacen` | 23 | código único en mayúsculas, tipo Central / Regional / Transito, capacidad en m³; no se borra si tiene zonas |
| `SegmentoCliente` | 02 | código y prioridad únicos, criterios de monto y órdenes mínimas; el segmento de cada cliente se calcula, no se guarda |
| `Cotizacion`, `DetalleCotizacion` | 03 | estados Emitida → Aceptada / Rechazada; descuento por línea; «vencida» calculada; al aceptar crea una `OrdenCompra` y guarda su Id |
| `ContratoComercial` | 05 | Borrador → Vigente → Finalizado / Cancelado; consumo calculado con las órdenes del cliente en el periodo; sin vigentes solapados por cliente |
| `ListaPrecios`, `ItemListaPrecios` | 06 | vigencia, prioridad y segmento opcional; un precio por producto y lista; la resolución elige segmento > general > precio base |
| `Promocion` | 07 | por producto o por monto mínimo, porcentaje, vigencia y segmento opcional; la evaluación aplica la mejor por línea y una por compra |
| `ReclamoPostventa` | 09 | sobre una orden del cliente (y opcionalmente un producto de ella); plazo de 2/5/10 días según prioridad; Abierto → EnProceso → Resuelto → Cerrado o Rechazado |
| `ZonaAlmacenaje` | 24 | código único por almacén, tipo y temperatura; las zonas activas no superan la capacidad del almacén |

Migraciones, en orden: `Inicial`, `ClientesRucUnico`, `Proveedores`, `SolicitudesCompra`, `OrdenesAbastecimiento`, `Almacenes`, `ZonasAlmacenaje`, `SegmentosClientes`, `Cotizaciones`, `ContratosComerciales`, `ListasPrecios`, `Promociones`, `ReclamosPostventa`.

## 6. Módulos implementados
| Módulo | API | Lo que hace | Pantalla | Specs |
|---|---|---|---|---|
| 04 OrdenesCompra | `api/v1/ordenes-compra` | alta de orden (409 número repetido, 400 cliente inexistente) y las 9 consultas de los 12 patrones LINQ | formulario y analítica | 01–06 |
| 01 Clientes | `api/v1/clientes` | listado con búsqueda, detalle con monto y última orden, alta, edición, baja (409 si tiene órdenes), resumen | mantenimiento completo | 07, 08 |
| 21 Productos | `api/v1/productos` | listado con categoría y rango de precio, detalle con ventas, alta, edición, baja (409 si tiene ventas), categorías, resumen | mantenimiento completo | 09, 10 |
| 11 Proveedores | `api/v1/proveedores` | listado por país y estado, detalle, alta, edición, baja, países, resumen | mantenimiento completo | 12, 13 |
| 13 SolicitudesCompra | `api/v1/solicitudes-compra` | solicitudes con líneas, aprobar (sin cuerpo), rechazar con motivo, editar y borrar solo pendientes, resumen | líneas dinámicas y diálogos | 14, 15 |
| 15 OrdenesAbastecimiento | `api/v1/ordenes-abastecimiento` | órdenes al proveedor manuales o desde una solicitud aprobada, recibir (sin cuerpo), anular con motivo, «atrasada», resumen | órdenes y botón «Generar orden» en solicitudes | 16, 17, 18 |
| 23 Almacenes | `api/v1/almacenes` | maestro de almacenes con tipo, ciudad y capacidad, ciudades, resumen | mantenimiento completo | 19, 20 |
| 24 ZonasAlmacenaje | `api/v1/zonas-almacenaje` | zonas de cada almacén (tipo, temperatura, capacidad), ocupación por almacén; un almacén con zonas no se borra | zonas con barra de ocupación por almacén («Casi lleno» desde el 90 %); terminado con QA | 21, 22 |
| 02 SegmentacionClientes | `api/v1/segmentos-clientes` | segmentos con criterios (monto y órdenes mínimas) y prioridad; clasificación calculada de cada cliente; resumen | vistas Segmentos y Clasificación; terminado con QA | 23, 24 |
| 03 Cotizaciones | `api/v1/cotizaciones` | presupuestos a clientes con descuento por línea; aceptar genera la orden de compra del módulo 04 (transaccional); rechazar con motivo; vencidas; resumen con tasa de aceptación | cotizaciones con líneas, descuentos y aceptación que genera la orden; terminado con QA | 25, 26 |
| 05 ContratosComerciales | `api/v1/contratos-comerciales` | acuerdos marco con cliente (Borrador → Vigente → Finalizado / Cancelado); consumo calculado con las órdenes del periodo; sin vigentes solapados por cliente | backend integrado; pantalla pendiente (spec 28) | 27, 28 |
| 06 ListasPrecios | `api/v1/listas-precios` | listas con vigencia, prioridad y segmento opcional; resolución del precio de un producto para un cliente y fecha (segmento > general > base) | backend integrado; pantalla pendiente (spec 30) | 29, 30 |
| 07 PromocionesDescuentos | `api/v1/promociones` | promociones por producto o por monto mínimo, con vigencia y segmento opcional; evaluación de una compra (mejor promoción por línea y una por compra) | backend integrado; pantalla pendiente (spec 32) | 31, 32 |
| 09 ReclamosPostventa | `api/v1/reclamos-postventa` | reclamos sobre órdenes del cliente con tipo y prioridad; plazo de atención (2/5/10 días) y vencidos; Abierto → EnProceso → Resuelto → Cerrado o Rechazado; cumplimiento de plazos | backend integrado; pantalla pendiente (spec 34) | 33, 34 |
| 10 DevolucionesComerciales | `api/v1/devoluciones-comerciales` | devoluciones de productos de una orden (opcionalmente de un reclamo); nunca más de lo comprado menos lo ya devuelto; importe con el precio de la orden; tasa de devolución | en desarrollo | 35, 36 |
| 78 NotificacionesAlertas | `api/v1/alertas` | centro de alertas calculadas desde los módulos (reclamos vencidos, órdenes atrasadas, contratos excedidos o por vencer, almacenes casi llenos, cotizaciones vencidas, solicitudes antiguas); descartar y restaurar | en desarrollo | 37, 38 |
| 00 Sistema | `api/v1/sistema/salud` | salud de la API | — | 01 |

Cada spec en [`specs/`](specs/) tiene el contrato exacto (rutas, DTOs y mensajes literales) y su evidencia en §7.

**Frontend:** catálogo con los 80 módulos y menú por dominio; los módulos sin pantalla muestran «Pantalla pendiente». Componentes compartidos: `PaginaModulo` (marco y miga de pan), `TablaPaginada`, `PanelDetalle`, `DialogoBaja`, `CampoConError`; utilidades en `lib/formato.ts` (moneda PEN, fechas en UTC, `formatearCantidad` para plurales). Selector «Rol de prueba» para probar el 403.

## 7. Calidad
- **Gate** `./scripts/verify.sh`: build sin warnings, `dotnet format`, reglas de código (`check-rules.sh`), tests del backend, `npm ci` + lint, typecheck, tests y build del frontend. Último gate integrado (2026-10-15): 422 tests del backend y 222 del frontend en verde.
- **Smoke** `./scripts/smoke-postgres.sh`: levanta PostgreSQL, migra, siembra, arranca la API y prueba las rutas de todos los módulos → `SMOKE OK`.
- **Proceso por tarea:** spec → implementación → auditoría (otra familia de modelo, máximo 3 ciclos) → QA de extremo a extremo con capturas a 375 y 1280 px en claro y oscuro → integración con el gate en verde → evidencia en §7 de la spec → despliegue.

## 8. Despliegue
Detalle y procedimiento en [`despliegue.md`](despliegue.md). Neon `titanglobal-erp` (us-east-1), Render `titanglobal-api` (se redespliega al hacer push a `ImLukzy/Claudio`), Vercel `titanglobal-erp` (se despliega con el CLI). Las migraciones se aplican a Neon a mano antes de subir el código. La demo no tiene autenticación y sus datos se restauran con el seed.

## 9. Equipo
- **Personas:** Lukas Melgar (integrador), Leonel Portillo, Antony Huayhua y Kelvin Pinto. Quién lleva cada módulo: [`equipo/asignaciones.md`](equipo/asignaciones.md). Cómo empezar: [`guia-companeros.md`](guia-companeros.md).
- **Agentes de Lukas** (coordinados con Orca): Claudio (integrador), Michael y Dwight (escriben backend y frontend), Kelly (auditoría), Pam (QA), Jim (exploración). Flujo: [`equipo/equipo.md`](equipo/equipo.md).
- **Errores que no hay que repetir:** [`equipo/lecciones.md`](equipo/lecciones.md).

## 10. Decisiones tomadas
| Fecha | Decisión | Quién |
|---|---|---|
| 2026-10-14 | Frontend en React + Vite + TS dentro del mismo repositorio (`frontend/`) | Lukas |
| 2026-10-14 | Numeración de módulos según la lista de `arquitectura.md` (`Modulo04_OrdenesCompra`, no `Modulo02`) | Lukas |
| 2026-10-14 | Número de orden repetido → 409; cliente inexistente → 400 | Lukas |
| 2026-10-14 | Orden de módulos: 01 Clientes, 21 Productos, 11 Proveedores, 13 SolicitudesCompra, 15 OrdenesAbastecimiento, 23 Almacenes | Lukas (por recomendación del integrador) |
| 2026-10-14 | Repositorio privado en GitHub a nombre de Lukas; despliegue en Neon, Render y Vercel; `main` al día | Lukas |
| 2026-10-14 | Acciones sin cuerpo con atributo `[PermitirCuerpoVacio]` en lugar de rutas en el middleware | integrador (spec 18) |
| 2026-10-15 | Siguiente módulo de los agentes: 24 ZonasAlmacenaje (capacidad de zonas activas limitada a la del almacén) | Lukas («sigue con el siguiente módulo») e integrador |
| 2026-10-15 | Al aceptar una cotización se crea una `OrdenCompra` del enunciado sin modificar su entidad (el enlace vive en `Cotizacion`) | integrador (spec 25) |
| 2026-10-15 | Se adelanta 09 ReclamosPostventa al 08 ComisionesVendedores: el 08 necesita vendedores y los empleados son el módulo 61 (Kelvin) | integrador |
| 2026-10-15 | Reparto equitativo de los 80 módulos, 20 por persona y por bloques de dominio: Lukas (Comercial y seguridad), Leonel (Compras y Finanzas), Antony (Inventarios y Manufactura), Kelvin (Logística y Capital Humano) | Lukas |

## 11. Cómo se mantiene este documento
En cada integración, el integrador actualiza aquí: §5 (entidades y migraciones), §6 (módulos), §7 (último gate), §10 (decisiones nuevas) y §12 (pendientes), junto con el estado del [`README`](../README.md) y de [`equipo/asignaciones.md`](equipo/asignaciones.md). Si algo de aquí no coincide con el código, manda el código y se corrige el documento.

## 12. Pendiente
- **En curso** (parte de Lukas):
  - 05 ContratosComerciales: backend integrado y desplegado; pantalla por empezar.
  - 06 ListasPrecios: backend integrado y desplegado; pantalla por empezar.
  - 07 PromocionesDescuentos: backend integrado y desplegado; pantalla por empezar.
  - 09 ReclamosPostventa: backend integrado y desplegado; pantalla por empezar (se adelanta al 08, que necesita decidir cómo se modelan los vendedores).
  - 10 DevolucionesComerciales: backend en auditoría.
  - 78 NotificacionesAlertas: backend en desarrollo.
- **Reparto:** 20 módulos por persona en [`equipo/asignaciones.md`](equipo/asignaciones.md). Lukas lleva 10 de 20 terminados (01, 02, 03, 04, 11, 13, 15, 21, 23, 24); tiene 4 en curso (05, 06, 07, 09) y 6 por empezar (08, 10, 71, 72, 77, 78). Leonel, Antony y Kelvin tienen 20 cada uno por empezar.
- **Accesos:** invitar a los compañeros al repositorio (falta su usuario de GitHub); Vercel Hobby no admite miembros.
- **Acabado menor:** el menú lateral no abre el dominio del módulo actual; texto del panel de detalle vacío («Selecciona…») más grande que el resto en SolicitudesCompra y Cotizaciones (probablemente en el componente compartido); números `OA-DEV-…` partidos en dos líneas a 1280 px; partir `ProveedoresPage.tsx` (624 líneas).
- **Resto:** ninguno sin dueño.
