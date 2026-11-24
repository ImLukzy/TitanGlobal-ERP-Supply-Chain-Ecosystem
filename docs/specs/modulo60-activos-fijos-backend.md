# Especificación: modulo60-activos-fijos-backend — Activos fijos

> Estado: **aprobada** por Lukas el 2026-11-23 para la v0. Responsable: Leonel Portillo. La revisión final del [PR #52](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52) sigue pendiente.

## 1. Objetivo

Registrar activos fijos con categoría y centro de costo, depreciarlos mensualmente por método lineal y registrar de forma definitiva su baja. Mantener el costo, residual, meses depreciados y valor en libros en la moneda del activo.

### Base comprobada y dependencias

- **Previa.** El [59](modulo59-centros-costo-backend.md) aporta el centro obligatorio y activo para el alta. La desactivación posterior conserva la referencia del activo.
- **Transversal.** Permisos de G1. El 60 conserva su propio registro. El 54 y el 58 no son dependencias de la v0.

## 2. Fuera de alcance

- Integraciones de compra y presupuesto, asientos, revalorizaciones, conversiones, métodos distintos del lineal, sucursales y reversión de la baja.
- Cálculo automático por tarea programada. El lote se solicita de forma explícita y escribe dentro del 60.

**Decisiones de producto aprobadas para la v0.** [Respuesta de Lukas](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52#issuecomment-7109010161)

| Nº | Tema | Respuesta aprobada |
|---|---|---|
| 60.1 | Identidad y alta | Generar `AF-000001`. Categorías fijas `Edificios`, `Maquinaria`, `Vehiculos`, `EquiposComputo` y `Muebles`. Centro de costo obligatorio, costo positivo y fecha de puesta en servicio. |
| 60.2 | Depreciación lineal | Vida útil de 1 a 600 meses y residual de 0 al costo. El último mes ajusta la diferencia de redondeo. |
| 60.3 | Periodicidad | Depreciar desde el mes siguiente al de puesta en servicio. Registrar lote mensual sin saltos y con una fila por activo y mes. |
| 60.4 | Baja | Motivo de catálogo `Venta`, `Obsolescencia`, `Siniestro` u `Otro`, con fecha. Incluir el mes de baja en el periodo depreciable. Baja definitiva. |
| 60.5 | Acceso y alcance | Aplicar G1. Prescindir de integraciones con 54 y 58. |

Las [reglas comunes G1 a G9](../equipo/revision-finanzas-52-60.md#reglas-generales-g1-a-g9) se aplican junto con las precisiones de §4.

## 3. Archivos afectados

| Archivo previsto | Acción futura | Alcance |
|---|---|---|
| `database/NNN-modulo60-activos-fijos.sql` y `docs/equipo/relevo.md` | Crear / registrar | Tomar el siguiente número libre al implementar y registrar solo la fila de este módulo. [Relevo §8](../equipo/relevo.md#8-números-de-los-scripts-de-base-de-datos) |
| `src/TitanGlobal.Domain/Models/` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | Generar | Entidades y navegaciones de §4 mediante scaffold. Revisar los nombres que produce el pluralizador. |
| `src/TitanGlobal.Application/DTOs/Modulo60_ActivosFijos/` y `src/TitanGlobal.Application/UseCases/Modulo60_ActivosFijos/` | Crear | Validación, proyecciones y casos de uso concretos en Application. |
| `src/TitanGlobal.Api/Controllers/Modulo60_ActivosFijos/ActivosFijosController.cs` | Crear | Rutas y autorización de §4. |
| `tests/TitanGlobal.Api.Tests/Modulo60_ActivosFijos/ActivosFijosApiTests.cs` | Crear | Pruebas HTTP de §4. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | Ampliar | Fixtures idempotentes y checks HTTP/SQL del módulo. |

## 4. Diseño y lógica

Los nombres, tamaños auxiliares, rutas y mecanismos de persistencia concretan el diseño para la revisión final. Las decisiones de producto aprobadas están en §2 y G1 a G9.

### Convenciones y permisos

Controller → UseCase → IUnitOfWork → Repository → DbContext → PostgreSQL. DTOs en camelCase. Las lecturas requieren JWT. Las escrituras requieren `FINANZAS` (Id 7, seed del 52) o `ADMIN` mediante `[Authorize(Roles = "FINANZAS,ADMIN")]`. Los 401, 403 y el 400 de cuerpo vacío conservan los literales del pipeline. Las acciones sin cuerpo usan `[PermitirCuerpoVacio]`. Las fechas de negocio usan `DateOnly` y `date` con JSON `yyyy-MM-dd`. Los instantes del servidor usan `timestamptz` y `FechasUtc`.

`AF-` sigue G5 con número provisional `TMP-` y código definitivo `AF-{Id:D6}` en la misma transacción. Importes `numeric(18,2)`. En ausencia de documento de origen, el contrato técnico propone capturar `PEN` o `USD` y mantener esa moneda. G9 cede ante el catálogo de baja.

### Datos y persistencia

| Tabla → entidad | Campos y restricciones |
|---|---|
| `ActivosFijos` → `ActivoFijo` | `Id`, `Codigo varchar(30)` único, `Nombre varchar(120)`, `Categoria varchar(20)` del catálogo, `CentroCostoId` FK obligatoria, `Moneda char(3)`, `Costo` positivo, `ValorResidual` entre cero y costo, `VidaUtilMeses integer` entre 1 y 600, `FechaPuestaServicio date`, `FechaRegistro timestamptz`. Estado `Activo` o `Baja` derivado de la existencia de su baja. |
| `DepreciacionesActivoFijo` → `DepreciacionActivoFijo` | `Id`, `ActivoFijoId` FK, `Periodo date` con primer día del mes, `Importe` no negativo, `FechaRegistro timestamptz`. Único `(ActivoFijoId, Periodo)`. Índice para historial ordenado. |
| `BajasActivoFijo` → `BajaActivoFijo` | `Id`, `ActivoFijoId` FK única, `FechaBaja date`, `Motivo varchar(20)` del catálogo y `FechaRegistro timestamptz`. Registro único por activo e irreversible. |

Las PK son identidades `integer`. Las FKs se indexan y conservan referencias con `ON DELETE RESTRICT`. Application valida los límites de columna antes de guardar.

Base depreciable = costo − residual. Cuota ordinaria = `Round(base / vidaUtilMeses, 2, MidpointRounding.AwayFromZero)`. En el último mes de vida útil registrar base − cuotas previas. Cuando la baja antecede ese último mes, incluir la cuota que corresponda al mes de baja y dejar el valor en libros residual de ese momento. Valor en libros = costo − depreciación acumulada. Por ejemplo, base 100 en tres meses produce 33,33, 33,33 y 33,34.

### Casos de uso, rutas y DTOs

Ruta base `/api/v1/activos-fijos`. Las rutas de consulta proyectan con `AsNoTracking`. Filtrar activos por `centroCostoId?`, `categoria?`, `estado?` y `moneda?`. Orden por `Codigo` e `Id`; historial por `Periodo` ascendente. Páginas 1/20, máximo 100.

| Método y ruta relativa | Caso de uso | Entrada → éxito |
|---|---|---|
| `GET /` | `ListarActivosFijosUseCase` | Filtros → `200 PaginaActivosFijosDto`. |
| `GET /{id:int}` | `ObtenerActivoFijoUseCase` | `200 ActivoFijoDetalleDto`. |
| `GET /{id:int}/depreciaciones` | `ListarDepreciacionesActivoFijoUseCase` | Paginación → `200 PaginaDepreciacionesActivoFijoDto`. |
| `POST /` | `CrearActivoFijoUseCase` | `CrearActivoFijoRequestDto` → `201` detalle con `Location`. |
| `POST /depreciaciones` | `RegistrarDepreciacionMensualUseCase` | `RegistrarDepreciacionMensualRequestDto { periodo }` → `200 LoteDepreciacionDto`. |
| `POST /{id:int}/baja` | `DarBajaActivoFijoUseCase` | `DarBajaActivoFijoRequestDto { motivo, fechaBaja }` → `200` detalle. |

- `CrearActivoFijoRequestDto { nombre, categoria, centroCostoId, moneda, costo, valorResidual, vidaUtilMeses, fechaPuestaServicio }`. El código y método lineal son valores de servidor.
- `ActivoFijoListadoDto { id, codigo, nombre, categoria, centroCostoId, moneda, costo, estado, valorEnLibros }`. El detalle añade residual, vida útil, puesta en servicio, depreciación acumulada y baja opcional con fecha y motivo.
- `DepreciacionActivoFijoDto { id, activoFijoId, periodo, importe, fechaRegistro }`. `LoteDepreciacionDto { periodo, cantidadRegistrada, cantidadYaRegistrada }`. Páginas con `items`, `pagina`, `tamanoPagina`, `totalRegistros` y `totalPaginas`.

### Validaciones y mensajes literales

Validar datos, centro activo al alta, categoría, escala, vida útil y residual. La puesta en servicio y la baja son fechas de negocio hasta hoy, al igual que el periodo de depreciación solicitado. Tras el alta, la desactivación del centro de costo conserva la referencia y el cálculo del activo.

Procesar el lote en orden de activo y bloquear cada activo junto con su historial. El primer mes elegible es el posterior al de puesta en servicio. Un periodo posterior a un mes pendiente produce conflicto en el contrato técnico de lote, sin escribir el mes posterior. Un periodo ya registrado conserva su fila. Incluir el mes de baja dentro de la ventana de vida útil y excluir los posteriores. La baja exige fecha y motivo del catálogo, y se coordina con el lote para preservar el historial y el límite por periodo.

Los textos de negocio de la tabla son **propuestas concretas para la revisión final**. El UseCase responde con `ReglaNegocioException` y `CodigosEstado`. El cuerpo conserva `{ StatusCode, Message }`. Las carreras de unicidad y FK se traducen mediante `ViolacionRestriccionException` del `UnitOfWork`.

| HTTP | Condición | Literal propuesto |
|---|---|---|
| 400 | Datos, categoría, vida útil, residual, escala, fechas o filtros inválidos | «Los datos del activo fijo no son válidos.» |
| 400 | Motivo de baja fuera de catálogo | «El motivo de baja debe ser Venta, Obsolescencia, Siniestro u Otro.» |
| 400 | Periodo distinto del primer día del mes o futuro | «El periodo debe indicar el primer día de un mes no futuro.» |
| 404 | Centro inexistente | «El centro de costo no existe.» |
| 404 | Activo inexistente | «El activo fijo no existe.» |
| 409 | Centro inactivo al alta | «El centro de costo debe estar activo para registrar el activo fijo.» |
| 409 | Mes anterior pendiente | «No se puede registrar la depreciación mientras haya meses anteriores pendientes.» |
| 409 | Baja anterior al historial mensual existente | «La fecha de baja es incompatible con las depreciaciones ya registradas.» |
| 409 | Baja repetida | «El activo fijo ya fue dado de baja.» |
| 409 | Referencia perdida al guardar | «No se pudo guardar el activo fijo porque una referencia ya no existe.» |

### Precisiones para la revisión final

- **Moneda y fecha de baja.** Confirmar la moneda de alta sin documento de origen y el tratamiento de una baja retroactiva cuando ya existe depreciación posterior. El contrato de revisión preserva el historial y devuelve conflicto en ese caso.
- **Lote y redondeo.** Confirmar si un activo con un mes pendiente bloquea todo el lote o solo ese activo. El diseño propone rechazo atómico del lote. Acordar cómo acotar la cuota ordinaria cuando su redondeo acumulado agotaría la base antes del último mes. La propuesta limita la cuota al saldo de base sin depreciar y ajusta el último mes.

### Seed, pruebas y smoke

**Seed.** Registrar con códigos y periodos fijos un activo vigente con depreciaciones contiguas, otro para ajuste de redondeo final y un activo dado de baja. Usar un centro del 59 y mantener el seed idempotente por código, periodo y activo de baja.

**Pruebas de integración.**

1. Código `AF-`, categorías, límites de vida útil 1/600, residual en 0 y costo, centro activo y fechas. Probar centro desactivado después del alta sin efecto sobre el historial.
2. Primer mes, ausencia de saltos, repetición de lote, unicidad, dos lotes competidores y ajuste de última cuota. Verificar que el valor en libros no cae bajo el residual.
3. Baja por cada motivo, baja repetida y depreciación incluida en el mes de baja y excluida después. Probar baja en el mes de puesta en servicio y baja concurrente con cálculo.
4. JWT, roles, cada literal, filtros y ausencia de efectos en cuentas por pagar o presupuestos.

**Smoke PostgreSQL.** Dar de alta con centro activo, correr lote mensual en orden, repetir el periodo y verificar el ajuste final. Registrar una baja y comprobar los límites por mes y la unicidad en PostgreSQL. Añadir `request_check`, la validación Python de códigos y cuerpos, y la comprobación de OpenAPI. Ejecutar en una base desechable hasta `SMOKE OK`.

## 5. Criterios de aceptación (medibles)

| # | Criterio | Verificación | Umbral |
|---|---|---|---|
| A1 | Arquitectura y compilación | `dotnet build -warnaserror`, `scripts/check-rules.sh` y `./scripts/verify.sh` | Gate en verde |
| A2 | API y autorización | Pruebas de cada literal, rutas, JWT y roles G1 | Lecturas autenticadas. Escrituras FINANZAS/ADMIN. Cada rechazo conserva los datos. |
| A3 | Alta | Código, catálogo, centro y límites de costo, residual y vida útil | Datos válidos y centro activo al crear |
| A4 | Cálculo mensual | Sin saltos, lote repetido y redondeo final | Una depreciación por mes/activo y base ajustada al fin de vida |
| A5 | Baja | Motivos, fecha, mes incluido y repetición | Baja definitiva e historial conservado |
| A6 | Database First | Aplicar SQL local, repetir seed, comprobar scaffold y ejecutar smoke | SCAFFOLD AL DÍA y SMOKE OK |

## 6. Checklist de ejecución

- [x] Incorporar las decisiones oficiales de §2 y G1 a G9.
- [ ] Terminar la revisión del PR de documentación y cerrar las precisiones de §4.
- [ ] Confirmar dependencias, aplicar el SQL en local y generar entidades y DbContext.
- [ ] Implementar API, pruebas, seed y smoke. Registrar los resultados de §5.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-23 | Decisiones v0 | Aprobadas. Revisión final del PR pendiente. | [Respuesta de Lukas](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52#issuecomment-7109010161) |
| 2026-11-23 | Validación funcional de la spec aprobada | Pendiente | Esta fase revisa documentación. La ejecución de §5 corresponde a la rama de implementación. |
