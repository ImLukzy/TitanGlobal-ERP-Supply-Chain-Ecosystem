# Arquitectura — TitanGlobal ERP & Supply Chain Ecosystem

Sistema empresarial transaccional para una corporación multinacional manufacturera y logística. Cubre de extremo a extremo la cadena de suministro, las compras internacionales, la manufactura, los almacenes (WMS), la distribución (TMS), la facturación electrónica, las finanzas y el capital humano.

Fuente: `docs/enunciado.md` (Lab 08 / Examen, backend). Además se requiere **frontend** (§ Frontend). Donde este documento y el enunciado difieren, manda § Desviaciones decididas.

## Estilo y estructura en cuatro capas
- **Cuatro capas con carpeta por módulo** (decidido el 2026-11-02 según el Lab 10, spec 50):
  - `src/TitanGlobal.Domain`: `Models/` (entidades), `Repositories/` (`IGenericRepository<T>`, `IUnitOfWork`), `Common/` (`FechasUtc`).
  - `src/TitanGlobal.Application`: `DTOs/ModuloNN_Nombre/`, `UseCases/ModuloNN_Nombre/`, `Exceptions/` (`ReglaNegocioException`, `CodigosEstado`, `ViolacionRestriccionException`, `TipoViolacionRestriccion`), `Services/` (`IContextoSolicitud`).
  - `src/TitanGlobal.Infrastructure`: `Persistence/` (solo `TitanGlobalDbContext.cs`, generado), `DbContextManual/` (`TitanGlobalDbContext.Constructor.cs` y `TitanGlobalDbContext.Auditoria.cs`, escritos a mano), `Repositories/Implementation/` (`GenericRepository<T>`, `UnitOfWork`), `Configuration/` (`InfrastructureServicesExtensions`).
  - `src/TitanGlobal.Api`: `Controllers/ModuloNN_Nombre/`, `Middlewares/`, `Configuration/` (`ServiceRegistrationExtensions`), `Services/` (`IAuthService`, `AuthService`, `ContextoSolicitud`), `Program.cs`.
  - Las pruebas viven en `tests/TitanGlobal.Api.Tests/ModuloNN_Nombre/` y `tests/TitanGlobal.Api.Tests/Arquitectura/`.
- **Dirección de dependencias:** `Api` → `Application` e `Infrastructure`; `Infrastructure` → `Application` y `Domain`; `Application` → `Domain`. `Domain` no depende de nadie.
  ```
  Api ──────────► Application ──► Domain
   │                  ▲             ▲
   └─► Infrastructure ┴─────────────┘
  ```
  - **Prohibido:** EF Core, Npgsql o ASP.NET en `Domain`; Npgsql o ASP.NET en `Application`; ASP.NET en `Infrastructure`.
  - `Application` conserva `Microsoft.EntityFrameworkCore` únicamente para los operadores asíncronos de LINQ (`ToListAsync`, `AnyAsync`, `SumAsync`, etc.).
  - El gate comprueba estas fronteras con las reglas `f` y `g` de `scripts/check-rules.sh` y con `tests/TitanGlobal.Api.Tests/Arquitectura/ArquitecturaTests.cs`.
- **Traducción de errores y puertos entre capas:**
  - En `Application` se usa `CodigosEstado.Status400BadRequest` / `Status404NotFound` / `Status409Conflict` (en lugar de `StatusCodes` de ASP.NET) con `ReglaNegocioException` (`using TitanGlobal.Application.Exceptions;`).
  - `UnitOfWork.CompleteAsync()` traduce las violaciones de PostgreSQL (`23505` y `23503`) a `ViolacionRestriccionException`, con `TipoViolacionRestriccion.Unicidad` o `TipoViolacionRestriccion.ReferenciaForanea` y `excepcion.Restriccion` para el nombre de la restricción. En `Application` nunca se capturan `DbUpdateException` ni `PostgresException`.
  - La auditoría del `DbContext` obtiene el usuario (`X-Usuario`) y la ruta mediante el puerto `IContextoSolicitud` (`Application/Services`), implementado por `ContextoSolicitud` en `Api/Services`.
- **Casos de uso concretos sin interfaz**, registrados por reflexión sobre el ensamblado de `TitanGlobal.Application` en `ServiceRegistrationExtensions.AddApplicationServices` (toda clase concreta terminada en `UseCase`).
- **Pipeline fijo:** ErrorHandling (500) → Routing → ParameterValidation (400) → RoleBasedAccess (403) → Authorization → Controllers. Como Routing va antes, ParameterValidation lee los metadatos del endpoint: las acciones sin cuerpo se declaran con `[PermitirCuerpoVacio]` (spec 18).
- **Autenticación y roles:** inicio de sesión con contraseña guardada con BCrypt y JWT (spec 51). Toda la API exige token salvo `POST api/v1/autenticacion/login` y la salud. Crear, editar o borrar usuarios y roles, y cambiar contraseñas, exige el rol `ADMIN`. La cabecera `X-User-Role` del enunciado se conserva: `Bloqueado` y `SinPermiso` siguen respondiendo 403.
- **Documentación de la API en desarrollo:** Swagger UI en `http://localhost:5080/swagger` (con token Bearer y `X-User-Role` autorizables) y documento OpenAPI en `/openapi/v1.json`.

Las reglas de código están en `CONTRIBUTING.md` § Reglas de código.

## Los 80 módulos (8 dominios)

**1. Comercial, Ventas y CRM (01–10)**
- 01 Clientes
- 02 SegmentacionClientes
- 03 Cotizaciones
- 04 OrdenesCompra (pedidos de clientes)
- 05 ContratosComerciales
- 06 ListasPrecios
- 07 PromocionesDescuentos
- 08 ComisionesVendedores
- 09 ReclamosPostventa
- 10 DevolucionesComerciales

**2. Compras y Abastecimiento Global (11–20)**
- 11 Proveedores
- 12 EvaluacionProveedores
- 13 SolicitudesCompra
- 14 Licitaciones
- 15 OrdenesAbastecimiento
- 16 Importaciones
- 17 AgentesAduaneros
- 18 CosteoImportacion
- 19 RecepcionMercaderia
- 20 AcuerdosAbastecimiento

**3. Inventarios y Almacenes — WMS (21–30)**
- 21 Productos
- 22 CategoriasProducto
- 23 Almacenes
- 24 ZonasAlmacenaje
- 25 LotesSeries
- 26 KardexValorizado
- 27 TransferenciasAlmacen
- 28 TomaInventario
- 29 AjustesInventario
- 30 ReabastecimientoAutomatico

**4. Manufactura y Producción — MRP (31–40)**
- 31 ListasMateriales
- 32 RutasProduccion
- 33 CentrosTrabajo
- 34 OrdenesProduccion
- 35 ConsumoMateriales
- 36 ControlCalidadInsumos
- 37 ControlCalidadProductoTerminado
- 38 MermasProduccion
- 39 MantenimientoPreventivo
- 40 MantenimientoCorrectivo

**5. Logística, Flota y Distribución — TMS (41–50)**
- 41 VehiculosFlota
- 42 Conductores
- 43 RutasDespacho
- 44 ConsolidacionCarga
- 45 GuiasRemision
- 46 MonitoreoEntregas
- 47 ControlCombustible
- 48 TransportistasTerceros
- 49 LogisticaInversa
- 50 SiniestrosFlota

**6. Finanzas, Contabilidad y Tesorería (51–60)**
- 51 FacturacionElectronica
- 52 NotasCreditoDebito
- 53 CuentasPorCobrar
- 54 CuentasPorPagar
- 55 TesoreriaBancos
- 56 ConciliacionBancaria
- 57 CajaChica
- 58 Presupuestos
- 59 CentrosCosto
- 60 ActivosFijos

**7. Capital Humano y Nóminas — HCM (61–70)**
- 61 Empleados
- 62 ContratosLaborales
- 63 AsistenciaBiometrica
- 64 TurnosHorarios
- 65 VacacionesPermisos
- 66 Planillas
- 67 AdelantosPrestamos
- 68 EvaluacionDesempeno
- 69 Capacitaciones
- 70 SeguridadSaludOcupacional

**8. Gobierno Corporativo, Seguridad y Auditoría (71–80)**
- 71 Usuarios
- 72 RolesPermisos
- 73 Sucursales
- 74 MonedasTiposCambio
- 75 ImpuestosTasas
- 76 SeriesNumeracion
- 77 AuditoriaTransaccional
- 78 NotificacionesAlertas
- 79 ParametrosSistema
- 80 CierresPeriodo

> **Numeración (decidida por el humano el 2026-10-14, adaptada a cuatro capas el 2026-11-02):** vale el orden de esta lista. Las carpetas llevan número de dos dígitos, `ModuloNN_Nombre`, dentro de cada capa (`Controllers/ModuloNN_Nombre/`, `DTOs/ModuloNN_Nombre/`, `UseCases/ModuloNN_Nombre/`); por ejemplo, `Modulo04_OrdenesCompra`. El namespace sigue a la capa y a la carpeta: `TitanGlobal.Api.Controllers.Modulo04_OrdenesCompra`, `TitanGlobal.Application.DTOs.Modulo04_OrdenesCompra` y `TitanGlobal.Application.UseCases.Modulo04_OrdenesCompra`. `Modulo00_Sistema` queda reservado para los endpoints técnicos (salud).

## Módulo de referencia
`Modulo04_OrdenesCompra` es la plantilla del enunciado (código original en `docs/enunciado.md` §3), repartida en las cuatro capas:
- Un caso de uso transaccional de escritura: `CrearOrdenCompraUseCase` (`src/TitanGlobal.Application/UseCases/Modulo04_OrdenesCompra/`).
- Un caso de uso analítico con los 12 patrones LINQ obligatorios, numerados en comentarios: `AnaliticaOrdenesCompraUseCase` (`src/TitanGlobal.Application/UseCases/Modulo04_OrdenesCompra/`).
- Un controlador delgado (`api/v1/ordenes-compra`): `OrdenesCompraController` (`src/TitanGlobal.Api/Controllers/Modulo04_OrdenesCompra/`).

Para programar un módulo CRUD completo en cuatro capas, el módulo de referencia es `Modulo11_Proveedores` (o `Modulo13_SolicitudesCompra` si hay líneas y estados); ver [`docs/equipo/crear-modulo.md`](equipo/crear-modulo.md).

## Contratos fijados por el enunciado
- `IGenericRepository<T>` (`TitanGlobal.Domain.Repositories`): `IQueryable<T> AsQueryable()`, `Task AddAsync(T)`, `void Update(T)`, `void Remove(T)`.
- `IUnitOfWork : IDisposable` (`TitanGlobal.Domain.Repositories`): `IGenericRepository<T> Repository<T>()`, `Task<int> CompleteAsync()`.
- Entidades `Cliente`, `Producto`, `OrdenCompra`, `DetalleOrdenCompra` (`TitanGlobal.Domain.Models`) con exactamente los campos de `docs/enunciado.md` §3.1 (`Cantidad` es `int`; importes `decimal(18,2)`).
- Mensajes de error literales de `docs/enunciado.md` §4.

## Desviaciones decididas (mandan sobre el enunciado)
| Enunciado | Proyecto | Decisión |
|---|---|---|
| Árbol de ejemplo con `Modulo02_OrdenesCompra` | `Modulo04_OrdenesCompra` (orden de la lista) | humano, 2026-10-14 |
| Namespace `TitanGlobal.Api.Modules.OrdenesCompra` | namespace = capa + carpeta: `TitanGlobal.Api.Controllers.Modulo04_OrdenesCompra`, `TitanGlobal.Application.DTOs.Modulo04_OrdenesCompra`, `TitanGlobal.Application.UseCases.Modulo04_OrdenesCompra` | humano, 2026-10-14 y 2026-11-02 |
| `RoleBasedAccessMiddleware` lee la cabecera directamente | lee el rol vía `IAuthService` (misma cabecera `X-User-Role`) | spec 01 |
| Registro de servicios dentro de `Program.cs` | métodos de extensión en `ServiceRegistrationExtensions.cs` (`Api/Configuration`) e `InfrastructureServicesExtensions.cs` (`Infrastructure/Configuration`), llamados desde `Program.cs` | spec 01 y spec 50 |
| Solo backend | backend + frontend | humano, 2026-10-14 |
| Vertical Slice en un solo proyecto (`src/TitanGlobal.Api/Modules/...`) | cuatro capas (`Domain`, `Application`, `Infrastructure`, `Api`) según el Lab 10, conservando una carpeta `ModuloNN_Nombre` en cada capa; login, JWT y BCrypt quedan para una segunda etapa (hoy sigue `X-User-Role`) | humano, 2026-11-02 (spec 50) |

## Frontend
Decidido por el humano el 2026-10-14: **React + Vite + TypeScript**, en la carpeta `frontend/` de este mismo repo.
- SPA que consume la API `api/v1/...`; en desarrollo, proxy de Vite hacia la API.
- Navegación por los 8 dominios y 80 módulos con la misma numeración (`docs/arquitectura.md`).
- Errores de la API (`{ StatusCode, Message }`) mostrados al usuario; la cabecera `X-User-Role` la pone el cliente HTTP.
- Tipos de la API alineados con el OpenAPI (`/openapi/v1.json`).
- Gate propio (`lint`, `typecheck`, `test` con Vitest, `build`) que `scripts/verify.sh` llama.
- Las pantallas de cada módulo van en specs aparte, después del backend de ese módulo.
