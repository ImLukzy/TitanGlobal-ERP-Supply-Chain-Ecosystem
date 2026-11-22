# Especificación: modulo58-presupuestos-backend — Presupuestos

> Estado: **propuesta pendiente de revisión y aprobación por Lukas**. Responsable: Leonel Portillo. El ciclo, la ejecución y las fórmulas de §4 son candidatos.

## 1. Objetivo

**Problema.** El [enunciado](../enunciado.md#2-los-80-módulos) pide ejecución por partida y contempla OPEX y CAPEX. El modelo revisado carece de un presupuesto y de un historial para relacionar lo planeado y lo ejecutado.

**Resultado esperado.** Registrar un presupuesto con partidas por centro de costo, consultar montos asignados y ejecutados, y conservar el origen de cada ejecución de acuerdo con el criterio que se apruebe.

### Base comprobada y dependencias

- El [59](modulo59-centros-costo-backend.md) aporta `CentroCosto` en el núcleo propuesto. La partida usa su identificador como referencia analítica.
- La ejecución manual es la propuesta inicial. La obligación del [54](modulo54-cuentas-por-pagar-backend.md), un egreso del [55](modulo55-tesoreria-bancos-backend.md), un gasto o rendición del [57](modulo57-caja-chica-backend.md) o un alta del [60](modulo60-activos-fijos-backend.md) son fuentes negociables. Elegir el evento que cuenta antes de enlazar cualquiera de ellas.
- El catálogo incluye CierresPeriodo (80). El alcance candidato usa el propio ciclo de presupuesto. La integración con cierres del ERP requiere un contrato disponible y aprobado.

## 2. Fuera de alcance

- Frontend, asientos contables, consolidación y conversión entre monedas, bloqueo de compras o pagos, y cierres globales automáticos.
- Integraciones automáticas de ejecución hasta aprobar la fuente y el evento en 58.4. Una misma operación económica puede estar documentada como obligación, pago o reposición. El presupuesto registra el evento aprobado y protege su identidad.

| Nº | Tema | Propuesta para revisión | Qué debe confirmar Lukas |
|---|---|---|---|
| 58.1 | Periodo, versión y moneda | Identificar presupuesto por código y ejercicio, con fecha de inicio y fin y una moneda. | Calendario, ámbito por empresa o sucursal, versiones simultáneas, unicidad y moneda por presupuesto o partida. |
| 58.2 | Partidas | Centro de costo y clase `OPEX` o `CAPEX` por partida. Registrar identificador e importe asignado. | Estructura, jerarquía, centro activo en alta o también en ejecución, categorías admitidas y si existe otra dimensión aprobada. |
| 58.3 | Ciclo | Proponer `Borrador` → `Vigente` → `Cerrado`; editar partidas en borrador. | Aprobación, responsables, revisiones, traspasos o ampliaciones, reapertura y relación con CierresPeriodo (80). |
| 58.4 | Ejecución y disponible | Registro explícito por partida, en su moneda, con clave de operación y opción de reversión. | Si cuenta compromiso, devengo, pago u otro evento; fuentes del 54, 55, 57 o 60, identidad, sobrepresupuesto y correcciones. |
| 58.5 | Consultas y acceso | Resumen por centro y clase, con asignado, ejecutado y disponible en la moneda del presupuesto. | Agrupaciones, consulta histórica, zonas de corte, roles y alcance por centro. |

## 3. Archivos afectados

| Archivo | Acción | Nota |
|---|---|---|
| `database/NNN-modulo58-presupuestos.sql` y `docs/equipo/relevo.md` | crear / actualizar tras aprobación | Confirmar número en [Relevo §8](../equipo/relevo.md#8-números-de-los-scripts-de-base-de-datos). |
| `src/TitanGlobal.Domain/Models/{Presupuesto,PartidaPresupuesto,EjecucionPresupuestaria}.cs` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | generar | Revisar navegación inversa generada en `CentroCosto.cs`. |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | ajustar si se necesita | Mapeos de nombres nuevos únicamente. |
| `src/TitanGlobal.Application/DTOs/Modulo58_Presupuestos/` y `src/TitanGlobal.Application/UseCases/Modulo58_Presupuestos/` | crear | Consultas, ciclo y ejecución. |
| `src/TitanGlobal.Api/Controllers/Modulo58_Presupuestos/PresupuestosController.cs` | crear | Rutas acordadas en §4. |
| `tests/TitanGlobal.Api.Tests/Modulo58_Presupuestos/PresupuestosApiTests.cs`, `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | crear / actualizar | Presupuestos y centros de prueba, HTTP y PostgreSQL. |

## 4. Diseño y lógica

### Datos propuestos

| Tabla → entidad | Campos y relaciones candidatos |
|---|---|
| `Presupuestos` → `Presupuesto` | `Id`, `Codigo` (30), `Nombre` (120), `Ejercicio`, `FechaInicio`, `FechaFin`, `Moneda` (3), `Estado`, `FechaActivacion?`, `FechaCierre?`. Unicidad candidata por `(Codigo, Ejercicio)`, con revisión del ámbito si hay versiones. |
| `PartidasPresupuesto` → `PartidaPresupuesto` | `Id`, `PresupuestoId` y `CentroCostoId` como FKs, `Codigo` (30), `Nombre` (120), `Clasificacion`, `ImporteAsignado`. Unicidad por `(PresupuestoId, Codigo)`. |
| `EjecucionesPresupuestarias` → `EjecucionPresupuestaria` | `Id`, `PartidaPresupuestoId` FK, `FechaOperacion`, `Importe`, `ClaveOperacion` (80), `Referencia?` (100), `ReversionDeId?` FK y `Motivo?` (500). Unicidad candidata por partida y clave, y una reversión por ejecución. |

FKs con índice. Importes `numeric(18,2)` como candidato y marcas `timestamptz` normalizadas con `FechasUtc`. El modelo debe conservar el centro de costo de cada partida y, si se aprueba una fuente automática, el vínculo verificable al evento consumido.

### Operaciones y DTOs candidatos

Ruta base `/api/v1/presupuestos`. JWT y proyecciones asíncronas con `AsNoTracking`. Paginación 1 y 20 por defecto, tamaño máximo 100. Orden por ejercicio e `Id` descendentes y por código e `Id` para partidas.

| Método y ruta relativa | Caso de uso | Entrada y respuesta |
|---|---|---|
| `GET /` | `ListarPresupuestosUseCase` | `ejercicio?`, `estado?`, `moneda?` y paginación → `200 PaginaPresupuestosDto`. |
| `GET /{id:int}` | `ObtenerPresupuestoUseCase` | `200 PresupuestoDetalleDto`. |
| `GET /{id:int}/resumen` | `ResumirPresupuestoUseCase` | `centroCostoId?`, `clasificacion?` → `200 ResumenPresupuestoDto`. |
| `POST /` | `CrearPresupuestoUseCase` | `GuardarPresupuestoRequestDto` → `201 PresupuestoDetalleDto` con `Location`. |
| `PUT /{id:int}` | `ActualizarPresupuestoUseCase` | `GuardarPresupuestoRequestDto` → `200 PresupuestoDetalleDto`. |
| `POST /{id:int}/activar` | `ActivarPresupuestoUseCase` | Sin cuerpo con `[PermitirCuerpoVacio]` → `200 PresupuestoDetalleDto`. |
| `POST /{id:int}/cerrar` | `CerrarPresupuestoUseCase` | Sin cuerpo con `[PermitirCuerpoVacio]` → `200 PresupuestoDetalleDto`. |
| `GET /{id:int}/partidas/{partidaId:int}/ejecuciones` | `ListarEjecucionesPresupuestariasUseCase` | Paginación → `200 PaginaEjecucionesPresupuestariasDto`. |
| `POST /{id:int}/partidas/{partidaId:int}/ejecuciones` | `RegistrarEjecucionPresupuestariaUseCase` | `RegistrarEjecucionPresupuestariaRequestDto` → `201 EjecucionPresupuestariaDto`. |
| `POST /{id:int}/partidas/{partidaId:int}/ejecuciones/{ejecucionId:int}/revertir` | `RevertirEjecucionPresupuestariaUseCase` | Si se aprueba, `RevertirEjecucionPresupuestariaRequestDto` → `201 EjecucionPresupuestariaDto`. |

- `GuardarPresupuestoRequestDto { codigo, nombre, ejercicio, fechaInicio, fechaFin, moneda, partidas: GuardarPartidaPresupuestoRequestDto[] }`, con partida `{ codigo, nombre, centroCostoId, clasificacion, importeAsignado }`. La versión candidata acepta editar las partidas como conjunto en borrador.
- `PresupuestoListadoDto { id, codigo, nombre, ejercicio, moneda, estado, importeAsignado, ejecutado, disponible }`. El detalle agrega fechas y partidas `{ id, codigo, nombre, centroCostoId, clasificacion, importeAsignado, ejecutado, disponible }`. `ResumenPresupuestoDto` devuelve moneda, totales y grupos `{ centroCostoId, clasificacion, asignado, ejecutado, disponible }`.
- `RegistrarEjecucionPresupuestariaRequestDto { fechaOperacion, importe, moneda, claveOperacion, referencia? }` y `RevertirEjecucionPresupuestariaRequestDto { claveOperacion, motivo }`. `EjecucionPresupuestariaDto { id, partidaPresupuestoId, fechaOperacion, importe, moneda, referencia?, reversionDeId?, motivo? }`. Las páginas exponen `items`, `pagina`, `tamanoPagina`, `totalRegistros` y `totalPaginas`.

### Reglas candidatas y errores literales

Validar forma, centro y pertenencia de la partida al presupuesto, estado, moneda y luego unicidad o importe. Propuesta de ejecutado neto = ejecuciones − reversiones. Disponible = importe asignado − ejecutado neto. Su uso como límite de escritura queda en 58.4.

| HTTP | Condición | Mensaje literal propuesto |
|---|---|---|
| 400 | Datos, periodo, clasificación, importe, motivo o paginación inválidos | «Los datos del presupuesto no son válidos.» |
| 400 | Partidas vacías o códigos repetidos | «El presupuesto debe contener partidas con códigos distintos.» |
| 404 | Presupuesto, partida del presupuesto, ejecución de la partida o centro inexistente | «El presupuesto no existe.» / «La partida presupuestaria no existe.» / «La ejecución presupuestaria no existe.» / «El centro de costo no existe.» |
| 409 | Código de presupuesto o clave de ejecución duplicados | «Ya existe un presupuesto con ese código y ejercicio.» / «La clave de operación ya fue registrada.» |
| 409 | Centro inelegible, estado incompatible o ejecución fuera del periodo | «El centro de costo no admite esta operación.» / «El estado del presupuesto no permite esta operación.» / «La operación está fuera del periodo del presupuesto.» |
| 409 | Moneda distinta, supera tope o reversión repetida | «La moneda de la ejecución debe coincidir con la del presupuesto.» / «La ejecución supera el disponible de la partida.» / «La ejecución presupuestaria ya fue revertida.» |
| 409 | Referencia eliminada durante el guardado | «No se pudo guardar el presupuesto porque una referencia ya no existe.» |

Guardar presupuesto y partidas juntos. Si se aprueba tope, proteger lectura y escritura por partida. Las operaciones de ejecutar y cerrar deben coordinarse sobre el estado de la cabecera. Si se necesitan garantías sobre ambos registros, establecer un orden de adquisición de cabecera a partida. Reversiones y fuentes futuras requieren su propia unicidad y pruebas de reintento en PostgreSQL.

## 5. Criterios de aceptación (medibles)

| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación y arquitectura | `dotnet build -warnaserror` y `./scripts/verify.sh` | 0 warnings, gate en verde |
| A2 | API | Pruebas HTTP por literal, JWT, roles aprobados, filtros y paginación. Rutas de §4 en OpenAPI y Swagger | Todas pasan |
| A3 | Ciclo y consultas | Presupuesto con OPEX y CAPEX, centros distintos, edición en borrador, activación, ejecución, reversión si aplica, cierre y resumen por centro. Rechazar centro inelegible y moneda distinta | Totales y estados pactados |
| A4 | Integridad | En PostgreSQL, repetir clave, competir por disponible si es tope y ejecutar en paralelo con cierre | Sin duplicado ni escritura parcial |
| A5 | Database First | Aplicar SQL, seed idempotente, scaffold y smoke con alta, ejecución y consulta | `SCAFFOLD AL DÍA` y `SMOKE OK` |

## 6. Checklist de ejecución

- [ ] Resolver §2 y confirmar el contrato del 59.
- [ ] Confirmar numeración, aplicar el SQL y generar el modelo.
- [ ] Implementar, verificar A1–A5 y registrar resultados.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-22 | Revisión documental | Pendiente de aprobación | Partida, OPEX y CAPEX están en el enunciado. El contrato del 59 y el momento de ejecución quedan por aprobar. |
