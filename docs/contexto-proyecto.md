# Contexto del proyecto TitanGlobal

Documento único con todo lo que hay que saber del proyecto: qué es, qué pide el examen, cómo está construido, qué está implementado, qué decisiones se tomaron y qué falta. **Se actualiza en cada integración** (ver §11). Última actualización: 2026-11-02.

## 1. Qué es
ERP transaccional para una corporación manufacturera y logística ficticia, **TitanGlobal**: 80 módulos en 8 dominios (comercial y CRM, compras e importaciones, inventarios WMS, manufactura MRP, logística TMS, finanzas, capital humano HCM y gobierno y seguridad). La lista completa está en [`arquitectura.md`](arquitectura.md).

- **Demo:** https://titanglobal-erp.vercel.app (la API va detrás de `/api`; tras ~15 min sin uso tarda ~1 min en despertar).
- **Código:** GitHub `ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem` (privado). Rama de integración y despliegue: `main`.

## 2. Qué se evalúa (Lab 08 / Examen)
El enunciado completo está en [`enunciado.md`](enunciado.md). Resumen:
- **Backend en .NET 10** con EF Core y PostgreSQL; el 2026-11-02 se sustituyó la Vertical Slice de un solo proyecto por **cuatro capas** (`Domain`, `Application`, `Infrastructure`, `Api`) según el Lab 10, conservando una carpeta por módulo (`ModuloNN_Nombre`) en cada capa (spec 50).
- **Modelo del enunciado:** `Cliente`, `Producto`, `OrdenCompra`, `DetalleOrdenCompra` (no se modifican).
- **Contratos fijos:** `IGenericRepository<T>` (`AsQueryable`, `AddAsync`, `Update`, `Remove`) e `IUnitOfWork` (`Repository<T>()`, `CompleteAsync()`).
- **12 patrones LINQ** en el módulo de órdenes de compra (`api/v1/ordenes-compra`).
- **3 middlewares** con mensajes literales: 500 «Ocurrió un error interno en el servidor.», 400 «El cuerpo de la petición (Body) no puede estar vacío.», 403 «Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.»
- Además, el equipo decidió (y el humano aprobó) hacer **frontend** en React.

**Estado frente al examen: todo lo que pide el enunciado está implementado y probado contra PostgreSQL real** (specs 01, 02, 05 y 06).

## 3. Stack
| Capa | Tecnología |
|---|---|
| API | .NET 10, ASP.NET Core con controladores, OpenAPI nativo (`/openapi/v1.json`) y Swagger en desarrollo (`/swagger`) |
| Datos | EF Core + Npgsql, PostgreSQL 16 en local (Docker, puerto 5433) y 18 en Neon |
| Tests backend | xUnit + `WebApplicationFactory` + EF InMemory + `ArquitecturaTests`; smoke contra PostgreSQL real |
| Frontend | React 19, React Router, Vite, TypeScript estricto, Vitest, ESLint sin warnings |
| Despliegue | Neon (BD), Render (API en Docker), Vercel (frontend) |

## 4. Arquitectura
- **Cuatro capas con carpeta por módulo (`ModuloNN_Nombre`, donde `NN` sigue el orden de `arquitectura.md`):**
  - `src/TitanGlobal.Domain`: `Models/`, `Repositories/`, `Common/`.
  - `src/TitanGlobal.Application`: `DTOs/ModuloNN_Nombre/`, `UseCases/ModuloNN_Nombre/`, `Exceptions/`, `Services/`.
  - `src/TitanGlobal.Infrastructure`: `Persistence/`, `Migrations/`, `Repositories/Implementation/`, `Configuration/`.
  - `src/TitanGlobal.Api`: `Controllers/ModuloNN_Nombre/`, `Middlewares/`, `Configuration/`, `Services/`, `Program.cs`.
  - Tests en `tests/TitanGlobal.Api.Tests/ModuloNN_Nombre/` y `tests/TitanGlobal.Api.Tests/Arquitectura/`.
- **Dirección de dependencias:** `Api` → `Application` e `Infrastructure`; `Infrastructure` → `Application` y `Domain`; `Application` → `Domain`. `Domain` no depende de nadie. Prohibido: EF Core, Npgsql o ASP.NET en `Domain`; Npgsql o ASP.NET en `Application`; ASP.NET en `Infrastructure`. `Application` conserva `Microsoft.EntityFrameworkCore` solo para los operadores asíncronos de LINQ. El gate lo comprueba con las reglas `f` y `g` de `scripts/check-rules.sh` y con `tests/TitanGlobal.Api.Tests/Arquitectura/ArquitecturaTests.cs`.
- **UseCases** concretos sin interfaz en `Application`, registrados por reflexión desde `ServiceRegistrationExtensions.AddApplicationServices`. Controladores delgados en `Api`. Validaciones en un helper estático por módulo (`ValidacionX`).
- **Persistencia** solo con `IUnitOfWork` / `IGenericRepository<T>`, consultas asíncronas proyectadas a DTO (sin N+1).
- **Pipeline fijo:** ErrorHandling (500) → Routing → ParameterValidation (400) → RoleBasedAccess (403, cabecera `X-User-Role`: `Bloqueado` y `SinPermiso` se rechazan) → Authorization → Controllers. La autenticación con login, JWT y BCrypt es una **segunda etapa** todavía no implementada: hoy sigue `X-User-Role`.
- **Errores de negocio y restricciones:** en `Application` se usa `ReglaNegocioException(CodigosEstado.Status..., mensaje)` (`using TitanGlobal.Application.Exceptions;`) → `{ StatusCode, Message }` sin `Detail` (400, 404, 409). `UnitOfWork.CompleteAsync()` traduce las violaciones de PostgreSQL (`23505` y `23503`) a `ViolacionRestriccionException`, que los casos de uso capturan con `catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)` (o `ReferenciaForanea`, y `excepcion.Restriccion` para el nombre de la restricción). Nunca `DbUpdateException` ni `PostgresException` en `Application`.
- **Auditoría desacoplada de HTTP:** `TitanGlobalDbContext` obtiene el usuario (`X-Usuario`) y la ruta mediante el puerto `IContextoSolicitud` (`Application/Services`), implementado por `ContextoSolicitud` (`Api/Services`).
- **Acciones POST sin cuerpo** (`aprobar`, `recibir`): atributo `[PermitirCuerpoVacio]`; el middleware no contiene rutas.
- **Fechas:** se guardan en UTC (`FechasUtc.Normalizar`, en `TitanGlobal.Domain.Common`); el frontend las muestra en UTC.
- **Números de documento** (`SC-000NNN`, `OA-000NNN`): los asigna el servidor a partir del Id, dentro de una transacción (`TransactionScope`). Los datos de ejemplo usan `SC-DEV-…` y `OA-DEV-…` para no chocar.
- **Reglas de código** (las revisa el revisor): [`../CONTRIBUTING.md`](../CONTRIBUTING.md) — sin `var`, sin `=>` en miembros, una sentencia por línea, nombres y comentarios en español, migraciones solo con `dotnet ef migrations add <Nombre> -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api`.
- **Specs nuevas de módulo:** `docs/specs/moduloNN-nombre-{backend|frontend}.md`, sin número global (solo las specs transversales llevan número).

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
| `DevolucionComercial`, `DetalleDevolucion` | 10 | devoluciones de productos de una orden (opcionalmente de un reclamo); nunca más de lo comprado menos lo ya devuelto; Solicitada → Aprobada → Recibida o Rechazada |
| `AlertaDescartada` | 78 | solo guarda los descartes (clave única); las alertas se calculan en cada consulta desde los módulos 03, 05, 09, 13, 15 y 24 |
| `Vendedor`, `AsignacionVenta`, `LiquidacionComision` | 08 | vendedores con % de comisión; una orden se asigna a un vendedor; liquidación mensual que conserva los importes; un mes liquidado no admite cambios de asignación |
| `Rol`, `PermisoRol` | 72 | roles con permisos de lectura/escritura por módulo (80); `ADMIN` de sistema; solo datos, el 403 del enunciado no cambia |
| `ZonaAlmacenaje` | 24 | código único por almacén, tipo y temperatura; las zonas activas no superan la capacidad del almacén |

Migraciones, en orden: `Inicial`, `ClientesRucUnico`, `Proveedores`, `SolicitudesCompra`, `OrdenesAbastecimiento`, `Almacenes`, `ZonasAlmacenaje`, `SegmentosClientes`, `Cotizaciones`, `ContratosComerciales`, `ListasPrecios`, `Promociones`, `ReclamosPostventa`, `DevolucionesComerciales`, `AlertasDescartadas`, `ComisionesVendedores`, `RolesPermisos`.

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
| 05 ContratosComerciales | `api/v1/contratos-comerciales` | acuerdos marco con cliente (Borrador → Vigente → Finalizado / Cancelado); consumo calculado con las órdenes del periodo; sin vigentes solapados por cliente | contratos con barra de consumo, órdenes del periodo y acciones por estado; terminado con QA | 27, 28 |
| 06 ListasPrecios | `api/v1/listas-precios` | listas con vigencia, prioridad y segmento opcional; resolución del precio de un producto para un cliente y fecha (segmento > general > base); `segmentoId=0` = listas generales; pantalla con simulador | terminado (backend, pantalla y QA) | 29, 30 |
| 07 PromocionesDescuentos | `api/v1/promociones` | promociones por producto o por monto mínimo, con vigencia y segmento opcional; evaluación de una compra (mejor promoción por línea y una por compra); pantalla con evaluador | terminado (backend, pantalla y QA) | 31, 32 |
| 09 ReclamosPostventa | `api/v1/reclamos-postventa` | reclamos sobre órdenes del cliente con tipo y prioridad; plazo de atención (2/5/10 días) y vencidos; Abierto → EnProceso → Resuelto → Cerrado o Rechazado; cumplimiento de plazos | terminado (backend y pantalla); falta QA visual | 33, 34 |
| 10 DevolucionesComerciales | `api/v1/devoluciones-comerciales` | devoluciones de productos de una orden (opcionalmente de un reclamo); nunca más de lo comprado menos lo ya devuelto; importe con el precio de la orden; tasa de devolución | terminado (backend y pantalla); falta QA visual | 35, 36 |
| 78 NotificacionesAlertas | `api/v1/alertas` | centro de alertas calculadas desde los módulos (reclamos vencidos, órdenes atrasadas, contratos excedidos o por vencer, almacenes casi llenos, cotizaciones vencidas, solicitudes antiguas); descartar y restaurar | terminado (backend y pantalla); falta QA visual | 37, 38 |
| 08 ComisionesVendedores | `api/v1/comisiones-vendedores` | vendedores con % de comisión; asignación de cada orden a un vendedor; comisiones del mes; liquidación mensual y pago | terminado (backend y pantalla); falta QA visual | 39, 40 |
| 72 RolesPermisos | `api/v1/roles` | roles y permisos de lectura/escritura por módulo (80), matriz roles × módulos; solo datos: el 403 del enunciado no cambia | backend integrado; pantalla pendiente (spec 42) | 41, 42 |
| 71 Usuarios | `api/v1/usuarios` | usuarios con varios roles y permisos efectivos por módulo (unión de roles); siempre queda un ADMIN activo; solo datos: el 403 del enunciado no cambia | backend integrado; pantalla pendiente (spec 44) | 43, 44 |
| 77 AuditoriaTransaccional | `api/v1/auditoria` | registro automático de altas, modificaciones y bajas de todas las entidades (antes/después, usuario de `X-Usuario`, ruta) desde `SaveChanges` del `DbContext`; consultas de solo lectura | terminado (backend y pantalla); falta QA visual | 45, 46 |
| 00 Sistema | `api/v1/sistema/salud` | salud de la API | — | 01 |

Cada spec en [`specs/`](specs/) tiene el contrato exacto (rutas, DTOs y mensajes literales) y su evidencia en §7.

**Frontend:** catálogo con los 80 módulos y menú por dominio; los módulos sin pantalla muestran «Pantalla pendiente». Componentes compartidos: `PaginaModulo` (marco y miga de pan), `TablaPaginada`, `PanelDetalle`, `DialogoBaja`, `CampoConError`; utilidades en `lib/formato.ts` (moneda PEN, fechas en UTC, `formatearCantidad` para plurales). Selector «Rol de prueba» para probar el 403.

## 7. Calidad
- **Gate** `./scripts/verify.sh`: build sin warnings, `dotnet format`, reglas de código (`check-rules.sh`, incluidas las reglas `f` y `g` de separación de capas), tests del backend (incluidos `ArquitecturaTests` y `UnitOfWorkTests`), que el modelo no tenga cambios sin migración, `npm ci` + lint, typecheck, tests y build del frontend.
- **Smoke** `./scripts/smoke-postgres.sh`: levanta PostgreSQL, migra (`-p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api`), siembra, arranca la API y prueba las rutas de todos los módulos → `SMOKE OK`.
- **Proceso por tarea:** spec → implementación → revisión por otro compañero (máximo 3 ciclos) → QA de extremo a extremo con capturas a 375 y 1280 px en claro y oscuro → integración con el gate en verde → evidencia en §7 de la spec → despliegue.

## 8. Despliegue
Detalle y procedimiento en [`despliegue.md`](despliegue.md). Neon `titanglobal-erp` (us-east-1), Render `titanglobal-api` (se redespliega al hacer push a `main`), Vercel `titanglobal-erp` (se despliega con el CLI). Las migraciones se aplican a Neon a mano antes de subir el código. La demo no tiene autenticación (login, JWT y BCrypt son una segunda etapa todavía no implementada; hoy sigue `X-User-Role`) y sus datos se restauran con el seed.

## 9. Equipo
- **Personas:** 7 personas. Lukas Melgar (13 módulos, todos terminados, y la integración), Leonel Portillo, Antony Huayhua, Kelvin Pinto, Alexander, Sergio y Maxi (11 o 12 módulos cada uno). Quién lleva cada módulo: [`equipo/asignaciones.md`](equipo/asignaciones.md). Cómo empezar: [`equipo/primer-dia.md`](equipo/primer-dia.md), [`guia-companeros.md`](guia-companeros.md), [`equipo/crear-modulo.md`](equipo/crear-modulo.md) y [`equipo/adaptar-rama-a-capas.md`](equipo/adaptar-rama-a-capas.md). Cadencia y meta de 3 módulos por semana: [`equipo/cadencia.md`](equipo/cadencia.md).
- **Flujo:** spec, rama propia, gate, revisión por otro compañero, QA, pull request e integración por Lukas: [`equipo/equipo.md`](equipo/equipo.md).
- **Errores que no hay que repetir:** [`equipo/lecciones.md`](equipo/lecciones.md).

## 10. Decisiones tomadas
| Fecha | Decisión | Quién |
|---|---|---|
| 2026-10-14 | Frontend en React + Vite + TS dentro del mismo repositorio (`frontend/`) | Lukas |
| 2026-10-14 | Numeración de módulos según la lista de `arquitectura.md` (`Modulo04_OrdenesCompra`, no `Modulo02`) | Lukas |
| 2026-10-14 | Número de orden repetido → 409; cliente inexistente → 400 | Lukas |
| 2026-10-14 | Orden de módulos: 01 Clientes, 21 Productos, 11 Proveedores, 13 SolicitudesCompra, 15 OrdenesAbastecimiento, 23 Almacenes | Lukas (por recomendación de la integración) |
| 2026-10-14 | Repositorio privado en GitHub a nombre de Lukas; despliegue en Neon, Render y Vercel; `main` al día | Lukas |
| 2026-10-14 | Acciones sin cuerpo con atributo `[PermitirCuerpoVacio]` en lugar de rutas en el middleware | integración (spec 18) |
| 2026-10-15 | Siguiente módulo: 24 ZonasAlmacenaje (capacidad de zonas activas limitada a la del almacén) | Lukas («sigue con el siguiente módulo») e integración |
| 2026-10-15 | Al aceptar una cotización se crea una `OrdenCompra` del enunciado sin modificar su entidad (el enlace vive en `Cotizacion`) | integración (spec 25) |
| 2026-10-15 | Se adelanta 09 ReclamosPostventa al 08 ComisionesVendedores: el 08 necesita vendedores y los empleados son el módulo 61 (Kelvin) | integración |
| 2026-10-15 | 08 ComisionesVendedores con su propio maestro de vendedores y una asignación de órdenes a vendedores (enlazable más adelante con Empleados, módulo 61) | Lukas |
| 2026-10-15 | 71 Usuarios y 72 RolesPermisos se gestionan como datos; el middleware de roles del enunciado (cabecera `X-User-Role`, 403 literal) no cambia | Lukas |
| 2026-10-15 | La auditoría (77) se captura en `SaveChanges` del `DbContext` (funciona igual en la API y en los tests); el usuario sale de la cabecera opcional `X-Usuario` y no se toca el pipeline del enunciado | integración (spec 45) |
| 2026-10-15 | Reparto equitativo de los 80 módulos, 20 por persona y por bloques de dominio: Lukas (Comercial y seguridad), Leonel (Compras y Finanzas), Antony (Inventarios y Manufactura), Kelvin (Logística y Capital Humano) | Lukas |
| 2026-10-31 | Reparto entre 7 personas (sustituye al de 4): Lukas 13 (terminados, más la integración), Leonel 11, Antony 11, Kelvin 11, Alexander 11, Sergio 11 y Maxi 12 | Lukas |
| 2026-11-02 | Sustituir la Vertical Slice de un solo proyecto por cuatro capas (`Domain`, `Application`, `Infrastructure`, `Api`) según el Lab 10 conservando una carpeta `ModuloNN_Nombre` en cada capa; dejar login, JWT y BCrypt para una segunda etapa (hoy sigue `X-User-Role`); adaptar ramas en curso con `scripts/adaptar-rama-capas.py` y nombrar las specs de módulo sin número global (`moduloNN-nombre-{backend|frontend}.md`) | Lukas (spec 50) |

## 11. Cómo se mantiene este documento
En cada integración, la integración actualiza aquí: §5 (entidades y migraciones), §6 (módulos), §7 (último gate), §10 (decisiones nuevas) y §12 (pendientes), junto con el estado del [`README`](../README.md) y de [`equipo/asignaciones.md`](equipo/asignaciones.md). Si algo de aquí no coincide con el código, manda el código y se corrige el documento.

## 12. Pendiente
- **Pausa del 2026-10-15:** Lukas pausó su parte. Dónde se quedó cada módulo y qué queda: [`equipo/relevo.md`](equipo/relevo.md).
  - Las pantallas 08, 09, 10, 77 y 78 se integraron en `main` el 2026-10-31; falta su QA visual.
  - Pantallas en ramas por adaptar e integrar: 72 RolesPermisos (spec 42) y 71 Usuarios (spec 44). Estado de cada rama: [`equipo/relevo.md`](equipo/relevo.md) §1.
- **Reparto:** 7 personas y 80 módulos en [`equipo/asignaciones.md`](equipo/asignaciones.md). Lukas lleva 13 terminados (01, 02, 03, 04, 05, 06, 07, 11, 13, 15, 21, 23, 24). Las pantallas de 72 RolesPermisos (Sergio) y 71 Usuarios (Alexander) están en sus ramas ([`equipo/relevo.md`](equipo/relevo.md) §1).md`](equipo/relevo.md)). El resto de módulos está por empezar.
- **Accesos:** los seis compañeros ya trabajan en el repositorio; Vercel Hobby no admite miembros.
- **Acabado menor:** el menú lateral no abre el dominio del módulo actual; texto del panel de detalle vacío («Selecciona…») más grande que el resto en SolicitudesCompra, Cotizaciones y Contratos (corregir en el componente compartido); números `OA-DEV-…` partidos en dos líneas a 1280 px; partir `ProveedoresPage.tsx` (624 líneas); plural en «Solo quedan 1 unidades…» (devoluciones); test intermitente `AlmacenesPage` «muestra capacidades del seed…» (1 fallo en ~40 ejecuciones con carga).
- **Resto:** ninguno sin dueño.
