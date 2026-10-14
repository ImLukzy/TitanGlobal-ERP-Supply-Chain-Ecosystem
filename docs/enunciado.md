# Enunciado — TitanGlobal ERP (Lab 08 / Examen)

Texto de referencia entregado por el humano el 2026-10-14. Es lo que «se busca» por parte del backend. Donde el humano decidió otra cosa, manda la decisión; ver `docs/arquitectura.md` § Desviaciones decididas. Además del backend **se requiere frontend** (ver `docs/arquitectura.md` § Frontend).

Requisitos que se evalúan:
- .NET 10 Web API en C#, Vertical Slice Architecture (monolito modular), 80 módulos en 8 dominios.
- Reglas de código institucionales: sin `=>` en miembros (solo en lambdas), bloques `{ }` y `return` explícito, tipos explícitos.
- UseCases concretos **sin interfaz**, registrados por **reflexión**; `Services/` solo para servicios técnicos (auth).
- Persistencia con `IUnitOfWork` + `IGenericRepository<T>` y consultas asíncronas.
- Los **12 patrones LINQ** del Lab 08, numerados en comentarios (§3.4).
- Pipeline estricto: ErrorHandling (500) → UseRouting → ParameterValidation (400, `EnableBuffering()` + `Position = 0`) → RoleBasedAccess (403) → UseAuthorization → MapControllers.

## 1. Árbol de directorios
```
TitanGlobal.Api/
├── Configuration/        DependencyInjectionExtensions.cs
├── Middlewares/          ErrorHandlingMiddleware.cs (500) · ParameterValidationMiddleware.cs (400) · RoleBasedAccessMiddleware.cs (403)
├── Models/               entidades partial (scaffolding): TitanGlobalDbContext, Cliente, OrdenCompra, DetalleOrdenCompra, Producto
├── Repositories/         Interfaces/ (IGenericRepository, IUnitOfWork) · Implementations/ (GenericRepository, UnitOfWork)
├── Services/             RESTRINGIDO: IAuthService, AuthService
├── Modules/              80 módulos: Modulo01_Clientes/{Controllers,Dtos,UseCases}, … hasta el 80
└── Program.cs            registro por reflexión + pipeline estricto
```

## 2. Los 80 módulos
La lista y su numeración están en `docs/arquitectura.md`. Descripción de cada uno, por dominio:

**1. Comercial, Ventas y CRM (01–10):** Clientes (maestro de clientes corporativos y minoristas) · SegmentacionClientes (clasificación crediticia y comercial de carteras) · Cotizaciones (emisión y seguimiento de propuestas) · OrdenesCompra (pedidos y órdenes de clientes) · ContratosComerciales (SLA y precios) · ListasPrecios (tarifarios por moneda, volumen y región) · PromocionesDescuentos (bonificaciones y descuentos escalonados) · ComisionesVendedores (cálculo y liquidación de incentivos) · ReclamosPostventa (tickets RMA, garantías) · DevolucionesComerciales (notas de devolución y reingreso).

**2. Compras y Abastecimiento Global (11–20):** Proveedores (padrón y homologación) · EvaluacionProveedores (scoring de cumplimiento, calidad, plazos) · SolicitudesCompra (requerimientos internos por departamento) · Licitaciones (concursos de precios, subastas inversas) · OrdenesAbastecimiento (órdenes de compra a proveedores) · Importaciones (expedientes de comercio exterior, Incoterms) · AgentesAduaneros (despachantes, aranceles, DUA/DAM) · CosteoImportacion (prorrateo de fletes, seguros y nacionalización, landed cost) · RecepcionMercaderia (ingreso en muelle, actas de conformidad) · AcuerdosAbastecimiento (contratos marco anuales, blanket orders).

**3. Inventarios y Almacenes — WMS (21–30):** Productos (maestro de artículos, insumos y terminados) · CategoriasProducto (familias, líneas, sublíneas) · Almacenes (centros de distribución y sedes) · ZonasAlmacenaje (racks, pasillos, ubicaciones, frío) · LotesSeries (trazabilidad por lote, vencimiento y serie) · KardexValorizado (entradas/salidas, PEPS/promedio) · TransferenciasAlmacen (traslados internos) · TomaInventario (conteos cíclicos y conciliación) · AjustesInventario (mermas, desmedros, robos, sobrantes) · ReabastecimientoAutomatico (stock mínimo, punto de reorden, stock de seguridad).

**4. Manufactura y Producción — MRP (31–40):** ListasMateriales (BOM) · RutasProduccion (secuencias y tiempos estándar) · CentrosTrabajo (líneas y celdas) · OrdenesProduccion (apertura, programación y cierre de lotes) · ConsumoMateriales (despacho de materia prima a planta) · ControlCalidadInsumos (inspección de entradas) · ControlCalidadProductoTerminado (liberación de lotes) · MermasProduccion (desperdicio y scrap) · MantenimientoPreventivo (paradas programadas) · MantenimientoCorrectivo (averías, repuestos, downtime).

**5. Logística, Flota y Distribución — TMS (41–50):** VehiculosFlota (camiones, capacidades) · Conductores (licencias, habilitaciones, turnos) · RutasDespacho (zonas, distancias, peajes) · ConsolidacionCarga (picking, packing, cubicaje) · GuiasRemision (documentos de transporte) · MonitoreoEntregas (estado en ruta, POD) · ControlCombustible (vales, rendimiento por km) · TransportistasTerceros (3PL y fletes) · LogisticaInversa (retornables, pallets, devoluciones en ruta) · SiniestrosFlota (incidentes, pólizas, reclamos).

**6. Finanzas, Contabilidad y Tesorería (51–60):** FacturacionElectronica (facturas, boletas, envío al ente tributario) · NotasCreditoDebito · CuentasPorCobrar (cartera, letras, morosidad) · CuentasPorPagar (obligaciones con proveedores) · TesoreriaBancos (movimientos, cheques, transferencias) · ConciliacionBancaria (extractos contra libros) · CajaChica (fondos fijos, viáticos) · Presupuestos (ejecución por partida, OPEX/CAPEX) · CentrosCosto (distribución analítica de gastos) · ActivosFijos (altas, bajas, depreciación).

**7. Capital Humano y Nóminas — HCM (61–70):** Empleados (legajo digital) · ContratosLaborales (modalidades, renovaciones, adendas) · AsistenciaBiometrica (marcaciones, tardanzas, horas extra) · TurnosHorarios (guardias rotativas, horarios) · VacacionesPermisos (descansos, licencias, récord vacacional) · Planillas (nómina, aportes, retenciones, boletas) · AdelantosPrestamos (descuentos por planilla) · EvaluacionDesempeno (KPI y competencias) · Capacitaciones (plan anual, certificaciones) · SeguridadSaludOcupacional (EPP, exámenes, incidentes SST).

**8. Gobierno Corporativo, Seguridad y Auditoría (71–80):** Usuarios (cuentas, estados, políticas de contraseña) · RolesPermisos (matriz RBAC por módulo y acción) · Sucursales (filiales, plantas, oficinas) · MonedasTiposCambio (tasas diarias compra/venta) · ImpuestosTasas (IGV/IVA, retenciones, percepciones, detracciones) · SeriesNumeracion (correlativos de documentos) · AuditoriaTransaccional (bitácora inmutable) · NotificacionesAlertas (cola de alertas internas) · ParametrosSistema (variables globales) · CierresPeriodo (bloqueo mensual/anual de operaciones).

## 3. Módulo de ejemplo: OrdenesCompra

### 3.1 Modelos (`Models/`, clases partial de scaffolding)
```csharp
public partial class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Ruc { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public virtual ICollection<OrdenCompra> OrdenesCompra { get; set; } = new List<OrdenCompra>();
}

public partial class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public string? Descripcion { get; set; }
    public virtual ICollection<DetalleOrdenCompra> DetallesOrdenCompra { get; set; } = new List<DetalleOrdenCompra>();
}

public partial class OrdenCompra
{
    public int Id { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public string? Descripcion { get; set; }
    public int ClienteId { get; set; }
    public virtual Cliente Cliente { get; set; } = null!;
    public virtual ICollection<DetalleOrdenCompra> Detalles { get; set; } = new List<DetalleOrdenCompra>();
}

public partial class DetalleOrdenCompra
{
    public int Id { get; set; }
    public int OrdenCompraId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public virtual OrdenCompra OrdenCompra { get; set; } = null!;
    public virtual Producto Producto { get; set; } = null!;
}
```

### 3.2 Contratos de persistencia (`Repositories/Interfaces/`)
```csharp
public interface IGenericRepository<T> where T : class
{
    IQueryable<T> AsQueryable();
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
}

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<T> Repository<T>() where T : class;
    Task<int> CompleteAsync();
}
```

### 3.3 DTOs (`Modules/…OrdenesCompra/Dtos/`)
```csharp
public class ProductoResumenDto { int Id; string Nombre; decimal Precio; }
public class DetalleOrdenItemDto { int ProductoId; string NombreProducto; int Cantidad; decimal PrecioUnitario; }
public class OrdenCompraJerarquicaDto { int OrdenId; string NumeroOrden; DateTime Fecha; string NombreCliente; List<DetalleOrdenItemDto> Detalles; }
public class ClienteFrecuenciaDto { int ClienteId; int TotalOrdenes; }
public class CrearOrdenCompraRequestDto { string NumeroOrden; int ClienteId; decimal Total; string? Descripcion; }
```
(Resumen: todas son propiedades públicas `{ get; set; }`; los `string` se inicializan con `string.Empty` y las listas con `new List<…>()`.)

### 3.4 UseCases concretos sin interfaz
**`CrearOrdenCompraUseCase.ExecuteAsync(CrearOrdenCompraRequestDto)`** → crea la `OrdenCompra` (`Fecha = DateTime.UtcNow`), `Repository<OrdenCompra>().AddAsync`, `CompleteAsync()` y devuelve el `Id`.

**`AnaliticaOrdenesCompraUseCase`** — los 12 patrones LINQ, cada uno numerado en un comentario:
| # | Patrón | Método |
|---|---|---|
| 1 | Filtro de texto insensible a mayúsculas (`ToLower().Contains`) | `BuscarProductosPorFiltroAsync(termino, minPrecio)` |
| 2 | Filtro numérico por umbral (`Precio > minPrecio`) | ídem |
| 3 | Proyección directa a DTO (`Select(x => new Dto {…})`) | ídem |
| 4 | Suma asíncrona (`SumAsync`) | `ObtenerSumaVentasDesdeFechaAsync(fechaInicio)` |
| 5 | Orden descendente y primero (`OrderByDescending().FirstOrDefaultAsync()`) | `ObtenerProductoMasCaroAsync()` |
| 6 | Filtro por rango de fecha (`Fecha > fecha`) | `ObtenerSumaVentasDesdeFechaAsync` |
| 7 | Promedio con validación previa (`if (!await AnyAsync()) return 0; AverageAsync`) | `ObtenerPromedioTotalPorClienteAsync(clienteId)` |
| 8 | Campos nulos o vacíos (`string.IsNullOrEmpty(x.Descripcion)`) | `ObtenerProductosSinDescripcionAsync()` |
| 9 | Agrupación y orden por conteo (`GroupBy(ClienteId).OrderByDescending(g => g.Count())`) | `ObtenerClientesConMasOrdenesAsync()` |
| 10 | Proyección jerárquica (padre con lista de DTOs hijos) | `ObtenerOrdenesJerarquicasAsync(clienteId)` |
| 11 | Aplanado y únicos (`SelectMany().Select().Distinct()`) | `ObtenerCategoriasUnicasCompradasPorClienteAsync(clienteId)` |
| 12 | Navegación inversa (`d.OrdenCompra.Cliente.Nombre`, `Distinct`) | `ObtenerClientesQueCompraronProductoAsync(productoId)` |

Todas las consultas parten de `_unitOfWork.Repository<T>().AsQueryable()` y terminan en un operador asíncrono (`ToListAsync`, `SumAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `AverageAsync`).

### 3.5 Controlador delgado `OrdenesCompraController` — `[Route("api/v1/ordenes-compra")]`
| Verbo | Ruta | UseCase | Respuesta |
|---|---|---|---|
| POST | `` | Crear | `{ Id }` |
| GET | `productos/filtro?termino=&minPrecio=` | patrones 1–3 | `ProductoResumenDto[]` |
| GET | `ventas/suma?fechaInicio=` | 4 y 6 | `{ TotalVentas }` |
| GET | `productos/mas-caro` | 5 | `ProductoResumenDto?` |
| GET | `clientes/{clienteId:int}/promedio` | 7 | `{ ClienteId, PromedioCompra }` |
| GET | `productos/sin-descripcion` | 8 | `ProductoResumenDto[]` |
| GET | `clientes/frecuencia` | 9 | `ClienteFrecuenciaDto[]` |
| GET | `clientes/{clienteId:int}/jerarquia` | 10 | `OrdenCompraJerarquicaDto[]` |
| GET | `clientes/{clienteId:int}/categorias-unicas` | 11 | `string[]` |
| GET | `productos/{productoId:int}/clientes-compradores` | 12 | `string[]` |

Campos `private readonly _camelCase`, constructor con bloque, cada acción con `{ }` y `return` explícito, sin lógica de negocio.

## 4. Middlewares y Program.cs
- **ErrorHandlingMiddleware:** `try { await _next(context); } catch` → log, 500, `application/json`, `{ StatusCode = 500, Message = "Ocurrió un error interno en el servidor.", Detail = ex.Message }`.
- **ParameterValidationMiddleware:** en POST/PUT, `EnableBuffering()`, lee el cuerpo con `StreamReader(…, leaveOpen: true)`, `Body.Position = 0`; si está vacío o en blanco → 400 `{ StatusCode = 400, Message = "El cuerpo de la petición (Body) no puede estar vacío." }`.
- **RoleBasedAccessMiddleware:** cabecera `X-User-Role`; `Bloqueado` o `SinPermiso` (sin distinguir mayúsculas) → 403 `{ StatusCode = 403, Message = "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación." }`.
- **Program.cs:** `AddDbContext` con `UseNpgsql(DefaultConnection)`; `AddScoped<IUnitOfWork, UnitOfWork>`; `AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>))`; `AddScoped<IAuthService, AuthService>`; reflexión sobre el ensamblado: toda clase concreta, no abstracta, cuyo nombre termina en `UseCase` → `AddScoped(tipo)`; `AddControllers`, `AddAuthorization`, `AddOpenApi`; `MapOpenApi` en desarrollo; pipeline en el orden indicado arriba.
