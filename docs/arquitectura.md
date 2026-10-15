# Arquitectura — TitanGlobal ERP & Supply Chain Ecosystem

Sistema empresarial transaccional para una corporación multinacional manufacturera y logística. Cubre de extremo a extremo la cadena de suministro, las compras internacionales, la manufactura, los almacenes (WMS), la distribución (TMS), la facturación electrónica, las finanzas y el capital humano.

Fuente: `docs/enunciado.md` (Lab 08 / Examen, backend). Además se requiere **frontend** (§ Frontend). Donde este documento y el enunciado difieren, manda § Desviaciones decididas.

## Estilo
- **Vertical slices:** cada módulo vive en `Modules/ModuloNN_Nombre/` con `Controllers/`, `Dtos/` y `UseCases/`.
- **Capas transversales:** solo `Middlewares/`, `Models/` (EF Core), `Repositories/` (repositorio genérico + unidad de trabajo), `Services/` (exclusivamente técnicos: auth) y `Configuration/`.
- **Casos de uso concretos sin interfaz**, registrados por reflexión (toda clase concreta terminada en `UseCase`).
- **Pipeline fijo:** ErrorHandling (500) → Routing → ParameterValidation (400) → RoleBasedAccess (403) → Authorization → Controllers. Como Routing va antes, ParameterValidation lee los metadatos del endpoint: las acciones sin cuerpo se declaran con `[PermitirCuerpoVacio]` (spec 18).

Las reglas de código están en `CLAUDE.md` § Reglas de código.

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

> **Numeración (decidida por el humano el 2026-10-14):** vale el orden de esta lista. Las carpetas llevan número de dos dígitos, `Modules/ModuloNN_Nombre/`; por ejemplo, `Modules/Modulo04_OrdenesCompra/`. El namespace sigue a la carpeta: `TitanGlobal.Api.Modules.Modulo04_OrdenesCompra.UseCases`. `Modulo00_Sistema` queda reservado para los endpoints técnicos (salud).

## Módulo de referencia
`Modulo04_OrdenesCompra` es la plantilla de todos los módulos (código en `docs/enunciado.md` §3). Incluye:
- Un caso de uso transaccional de escritura: `CrearOrdenCompraUseCase`.
- Un caso de uso analítico con los 12 patrones LINQ obligatorios, numerados en comentarios (`AnaliticaOrdenesCompraUseCase`).
- Un controlador delgado (`api/v1/ordenes-compra`).

## Contratos fijados por el enunciado
- `IGenericRepository<T>`: `IQueryable<T> AsQueryable()`, `Task AddAsync(T)`, `void Update(T)`, `void Remove(T)`.
- `IUnitOfWork : IDisposable`: `IGenericRepository<T> Repository<T>()`, `Task<int> CompleteAsync()`.
- Entidades `Cliente`, `Producto`, `OrdenCompra`, `DetalleOrdenCompra` con exactamente los campos de `docs/enunciado.md` §3.1 (`Cantidad` es `int`; importes `decimal(18,2)`).
- Mensajes de error literales de `docs/enunciado.md` §4.

## Desviaciones decididas (mandan sobre el enunciado)
| Enunciado | Proyecto | Decisión |
|---|---|---|
| Árbol de ejemplo con `Modulo02_OrdenesCompra` | `Modulo04_OrdenesCompra` (orden de la lista) | humano, 2026-10-14 |
| Namespace `TitanGlobal.Api.Modules.OrdenesCompra` | namespace = carpeta: `TitanGlobal.Api.Modules.Modulo04_OrdenesCompra` | humano, 2026-10-14 |
| `RoleBasedAccessMiddleware` lee la cabecera directamente | lee el rol vía `IAuthService` (misma cabecera `X-User-Role`) | spec 01 |
| Registro de servicios dentro de `Program.cs` | métodos de extensión en `Configuration/DependencyInjectionExtensions.cs`, llamados desde `Program.cs` | spec 01 (el árbol del enunciado ya lo incluye) |
| Solo backend | backend + frontend | humano, 2026-10-14 |

## Frontend
Decidido por el humano el 2026-10-14: **React + Vite + TypeScript**, en la carpeta `frontend/` de este mismo repo.
- SPA que consume la API `api/v1/...`; en desarrollo, proxy de Vite hacia la API.
- Navegación por los 8 dominios y 80 módulos con la misma numeración (`docs/arquitectura.md`).
- Errores de la API (`{ StatusCode, Message }`) mostrados al usuario; la cabecera `X-User-Role` la pone el cliente HTTP.
- Tipos de la API alineados con el OpenAPI (`/openapi/v1.json`).
- Gate propio (`lint`, `typecheck`, `test` con Vitest, `build`) que `scripts/verify.sh` llama.
- Las pantallas de cada módulo van en specs aparte, después del backend de ese módulo.
