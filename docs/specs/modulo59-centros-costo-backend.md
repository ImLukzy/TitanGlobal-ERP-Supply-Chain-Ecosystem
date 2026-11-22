# Especificación: modulo59-centros-costo-backend — Centros de costo

> Estado: **propuesta pendiente de revisión y aprobación por Lukas**. Responsable: Leonel Portillo. La estructura, vigencia y reglas de uso por otros módulos son candidatas.

## 1. Objetivo

**Problema.** El [enunciado](../enunciado.md#2-los-80-módulos) asigna la distribución analítica de gastos al 59. El esquema existente tiene `CentroTrabajo` para manufactura, con planta, capacidad y costo por hora. Falta un catálogo de centros para clasificar operaciones financieras.

**Resultado esperado.** Mantener centros de costo identificables por un código, consultar su vigencia y ponerlos a disposición de presupuestos, activos fijos y otras operaciones que apruebe Lukas.

### Base comprobada y dependencias

- El núcleo candidato parte de un catálogo propio. La [spec del 33](modulo33-centros-trabajo-backend.md) define un recurso de manufactura. Las relaciones con centros de trabajo, empleados (61) o sucursales (73) requieren el contrato que se acuerde en 59.1.
- El [58](modulo58-presupuestos-backend.md) usaría `CentroCostoId` para partidas y el [60](modulo60-activos-fijos-backend.md) para asignación de activos. [54](modulo54-cuentas-por-pagar-backend.md) y [57](modulo57-caja-chica-backend.md) pueden consumirlo si se aprueba clasificación en su módulo. La base documental carece de un contrato integrado de Sucursales (73) para usarlo como dependencia obligatoria.
- El alta, consulta y desactivación siguen las convenciones de la [revisión de Finanzas](../equipo/revision-finanzas-52-60.md#reglas-del-repositorio).

## 2. Fuera de alcance

- Frontend, cálculo de distribuciones, plan de cuentas, asientos y reasignación masiva de operaciones.
- Integración con manufactura, empleados y sucursales hasta que se apruebe la relación y su ámbito.

| Nº | Tema | Propuesta para revisión | Qué debe confirmar Lukas |
|---|---|---|---|
| 59.1 | Identidad y organización | Catálogo plano con código único normalizado, nombre, descripción y vigencia. | Unicidad global o por empresa/sucursal, formato y cambios de código, datos y fuente del responsable, y si se requiere relación con los centros de trabajo. |
| 59.2 | Jerarquía | Incorporar padre solo si se necesitan niveles o reportes acumulados. | Árbol, profundidad, tipos de centro, movilidad entre padres, reglas contra ciclos y descendientes de un centro inactivo. |
| 59.3 | Vigencia y consumidores | Desactivación lógica. Mantener visibles las referencias históricas y evaluar actividad al asignar por primera vez. | Qué consumos requieren centro activo (58, 60 y opcionalmente 54 o 57), qué hacer con operaciones vigentes al desactivar, cómo conservar historia y si habrá reactivación. |
| 59.4 | Acceso y reportes | Búsqueda de catálogo y filtro de vigencia con JWT. | Roles de mantenimiento o lectura y si el 59 debe reportar importes o dejar su agregación a cada módulo que los origina. |

## 3. Archivos afectados

| Archivo | Acción | Nota |
|---|---|---|
| `database/NNN-modulo59-centros-costo.sql` y `docs/equipo/relevo.md` | crear / actualizar tras aprobación | Confirmar número en [Relevo §8](../equipo/relevo.md#8-números-de-los-scripts-de-base-de-datos). |
| `src/TitanGlobal.Domain/Models/CentroCosto.cs` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | generar | Modelo y mapeo obtenidos por scaffold. |
| `src/TitanGlobal.Application/DTOs/Modulo59_CentrosCosto/` y `src/TitanGlobal.Application/UseCases/Modulo59_CentrosCosto/` | crear | Mantenimiento y consultas de catálogo. |
| `src/TitanGlobal.Api/Controllers/Modulo59_CentrosCosto/CentrosCostoController.cs` | crear | Rutas de §4. |
| `tests/TitanGlobal.Api.Tests/Modulo59_CentrosCosto/CentrosCostoApiTests.cs`, `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | crear / actualizar | Fixtures con centros activos e inactivos. |

## 4. Diseño y lógica

### Datos propuestos

| Tabla → entidad | Campos y relaciones candidatos |
|---|---|
| `CentrosCosto` → `CentroCosto` | `Id` con identidad, `Codigo` (30), `Nombre` (120), `Descripcion?` (500), `Activo`, `FechaCreacion`, `FechaDesactivacion?`, `MotivoDesactivacion?` (500). Índice único sobre el código canónico en el ámbito aprobado. |
| Jerarquía condicional en `CentrosCosto` | Si se aprueba 59.2, `CentroCostoPadreId?` con FK a `CentrosCosto.Id`, índice y borrado restringido. Validar pertenencia al ámbito, ascendientes y ciclos en Application. |

Las fechas de auditoría y vigencia son `timestamptz` normalizadas con `FechasUtc`. Proponer código inmutable salvo decisión en 59.1. Los módulos consumidores guardan su FK al centro y consultan su propia información histórica. La decisión de conservar también un nombre histórico corresponde a esos contratos.

### Operaciones y DTOs candidatos

Ruta base `/api/v1/centros-costo`. JWT, consultas asíncronas y paginadas con `AsNoTracking`. Orden por `Codigo` e `Id` ascendentes, página 1, tamaño 20 por defecto y máximo 100.

| Método y ruta relativa | Caso de uso | Entrada y respuesta |
|---|---|---|
| `GET /` | `ListarCentrosCostoUseCase` | `busqueda?` por código o nombre, `activo?`, `pagina`, `tamanoPagina` → `200 PaginaCentrosCostoDto`. |
| `GET /{id:int}` | `ObtenerCentroCostoUseCase` | `200 CentroCostoDetalleDto`. |
| `POST /` | `CrearCentroCostoUseCase` | `CrearCentroCostoRequestDto` → `201 CentroCostoDetalleDto` con `Location`. |
| `PUT /{id:int}` | `ActualizarCentroCostoUseCase` | `ActualizarCentroCostoRequestDto` → `200 CentroCostoDetalleDto`. |
| `POST /{id:int}/desactivar` | `DesactivarCentroCostoUseCase` | `DesactivarCentroCostoRequestDto` → `200 CentroCostoDetalleDto`. |
| `POST /{id:int}/reactivar` | `ReactivarCentroCostoUseCase` | Si se aprueba 59.3, sin cuerpo con `[PermitirCuerpoVacio]` → `200 CentroCostoDetalleDto`. |

- `CrearCentroCostoRequestDto { codigo, nombre, descripcion? }`, `ActualizarCentroCostoRequestDto { nombre, descripcion? }` y `DesactivarCentroCostoRequestDto { motivo }`.
- `CentroCostoListadoDto { id, codigo, nombre, activo }`. El detalle añade `descripcion?`, `fechaCreacion`, `fechaDesactivacion?` y `motivoDesactivacion?`. La página expone `items`, `pagina`, `tamanoPagina`, `totalRegistros` y `totalPaginas`.
- Si se aprueba jerarquía, añadir `centroCostoPadreId?` a alta, edición y detalle, y filtro por padre al listado. La salida conserva el mismo `id`.

### Reglas candidatas y errores literales

Recortar textos, normalizar código a mayúsculas, validar obligatoriedad, tamaños y páginas antes de consultar. Proponer alta activa, baja lógica con motivo y detalle visible para referencias históricas. Definir si se edita un centro inactivo al resolver 59.3.

| HTTP | Condición | Mensaje literal propuesto |
|---|---|---|
| 400 | Código, nombre, descripción, motivo o paginación inválidos | «Los datos del centro de costo no son válidos.» |
| 404 | Centro o padre inexistente | «El centro de costo no existe.» / «El centro de costo padre no existe.» |
| 409 | Código duplicado | «El código de centro de costo ya existe.» |
| 409 | Estado incompatible o baja bloqueada por uso | «El estado del centro de costo no permite esta operación.» / «El centro de costo tiene operaciones vigentes que impiden su desactivación.» |
| 409 | Padre fuera de ámbito, inactivo o ciclo, si hay jerarquía | «El centro de costo padre no es válido para esta asignación.» / «La relación crearía un ciclo de centros de costo.» |
| 409 | Referencia eliminada durante el guardado | «No se pudo guardar el centro de costo porque una referencia ya no existe.» |

El índice único protege altas concurrentes. Si se aprueba árbol editable, la lectura de ancestros y el cambio de padre requieren una garantía transaccional que evite dos reasignaciones válidas individualmente y cíclicas al combinarse. Si desactivar depende de operaciones vigentes, el consumidor y el catálogo deben acordar cómo proteger el alta concurrente de una referencia.

## 5. Criterios de aceptación (medibles)

| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación y arquitectura | `dotnet build -warnaserror` y `./scripts/verify.sh` | 0 warnings, gate en verde |
| A2 | API | Prueba por literal, JWT, roles aprobados, búsqueda, página estable y rutas en OpenAPI y Swagger | Todas pasan |
| A3 | Catálogo | Alta, edición, variante de mayúsculas, baja y reactivación si aplica. Verificar lectura histórica e inelegibilidad para nuevas asignaciones donde se apruebe | Estado y referencias preservados |
| A4 | Integridad | Dos altas de código equivalente en PostgreSQL. Si hay jerarquía, reasignación a descendiente y dos cruces concurrentes | Un solo código válido, ningún ciclo |
| A5 | Database First | Aplicar SQL, seed repetible, scaffold y smoke de alta, filtro y duplicado | `SCAFFOLD AL DÍA` y `SMOKE OK` |

## 6. Checklist de ejecución

- [ ] Aprobar §2 y confirmar la elegibilidad para 58 y 60.
- [ ] Confirmar numeración, aplicar el SQL y generar el modelo.
- [ ] Implementar catálogo y verificar A1–A5.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-22 | Revisión documental | Pendiente de aprobación | Se contrastó con el maestro `CentroTrabajo` y los consumidores propuestos 58 y 60. |
