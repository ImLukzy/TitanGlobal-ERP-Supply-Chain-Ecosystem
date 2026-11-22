# Especificación: modulo57-caja-chica-backend — Caja chica

> Estado: **propuesta pendiente de revisión y aprobación por Lukas**. Responsable: Leonel Portillo. Los contratos, reglas de fondo y flujo de rendición se proponen para revisión.

## 1. Objetivo

**Problema.** El [enunciado](../enunciado.md#2-los-80-módulos) incluye fondos fijos y viáticos. Falta un registro de fondos, movimientos y rendiciones con saldo trazable.

**Resultado esperado.** Administrar fondos de caja chica, registrar reposiciones y egresos, y consultar su saldo. Agrupar los egresos y adelantos admitidos en rendiciones con evidencia y el ciclo de aprobación acordado.

### Base comprobada y dependencias

- El 61 ya tiene el maestro `Empleado`. El vínculo a un custodio o beneficiario existente se incluiría si se aprueba. La identidad registrada como auditoría de una petición proviene del flujo ya existente.
- El fondo puede operar sin otros módulos financieros. Un egreso del [55](modulo55-tesoreria-bancos-backend.md) podría probar una reposición. El [59](modulo59-centros-costo-backend.md) podría clasificar el gasto y el [58](modulo58-presupuestos-backend.md) consumirlo como ejecución si se aprueba la integración.
- Las convenciones de arquitectura y persistencia están en la [revisión de Finanzas](../equipo/revision-finanzas-52-60.md#reglas-del-repositorio).

## 2. Fuera de alcance

- Frontend, transferencias bancarias efectivas, nómina, tratamiento tributario de gastos, asientos contables y políticas de viajes.
- Automatización de reposiciones desde el 55 y de ejecución en el 58 hasta aprobar los vínculos y la fuente del gasto.

| Nº | Tema | Propuesta para revisión | Qué debe confirmar Lukas |
|---|---|---|---|
| 57.1 | Fondo | Código único, moneda fija, custodio y alta con saldo cero. Registrar el fondeo como movimiento. | Ámbito, sucursal, custodio como texto o empleado, monto autorizado, cambio de responsable y condiciones de cierre. |
| 57.2 | Movimientos | `Reposicion`, `Gasto`, `AdelantoViatico` y, si se incluye, `Devolucion` con vínculo al adelanto. Conservar correcciones. | Tipos, gastos y comprobantes admitidos, límites, saldo negativo, reversión, devolución parcial y tratamiento de sobrantes o faltantes. |
| 57.3 | Rendiciones | Agrupar egresos del mismo fondo y pasar de `Presentada` a `Aprobada`, con observación si se requiere. | Qué operaciones se rinden, plazos, anticipos pendientes, estados, corrección o reapertura y si se puede volver a incluir un egreso. |
| 57.4 | Acceso e integración | JWT con permisos aún por definir. Caja chica puede consultar 55 y 59 si se aprueban esos vínculos. El 58 puede consumir una operación de caja aprobada como fuente. | Roles y separación de funciones, validez de una reposición bancaria, obligatoriedad del centro de costo y evento que cuenta como ejecución presupuestaria. |

## 3. Archivos afectados

| Archivo | Acción | Nota |
|---|---|---|
| `database/NNN-modulo57-caja-chica.sql` y `docs/equipo/relevo.md` | crear / actualizar tras aprobación | Confirmar número en [Relevo §8](../equipo/relevo.md#8-números-de-los-scripts-de-base-de-datos). |
| `src/TitanGlobal.Domain/Models/{FondoCajaChica,MovimientoCajaChica,RendicionCajaChica,DetalleRendicionCajaChica}.cs` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | generar | Tablas y navegaciones de las relaciones aprobadas. |
| `src/TitanGlobal.Application/DTOs/Modulo57_CajaChica/` y `src/TitanGlobal.Application/UseCases/Modulo57_CajaChica/` | crear | Consultas, movimientos y rendiciones. |
| `src/TitanGlobal.Api/Controllers/Modulo57_CajaChica/CajaChicaController.cs` | crear | Controlador delgado. |
| `tests/TitanGlobal.Api.Tests/Modulo57_CajaChica/CajaChicaApiTests.cs`, `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | crear / actualizar | Cobertura HTTP y PostgreSQL con dos fondos de prueba. |

## 4. Diseño y lógica

### Datos propuestos

| Tabla → entidad | Campos y relaciones candidatos |
|---|---|
| `FondosCajaChica` → `FondoCajaChica` | `Id`, `Codigo` (30) único, `Nombre` (120), `Responsable` (120) o `EmpleadoId` según 57.1, `Moneda` (3), `Activo`, `FechaApertura`. |
| `MovimientosCajaChica` → `MovimientoCajaChica` | `Id`, `FondoCajaChicaId` FK, `Tipo`, `Importe`, `FechaOperacion`, `ClaveOperacion` (80), `Concepto` (200), `Referencia?` (100), `MovimientoOrigenId?` para devolución, `ReversionDeId?` y `MotivoReversion?` si se aprueba reversión. Candidata a unicidad `(FondoCajaChicaId, ClaveOperacion)`. |
| `RendicionesCajaChica` → `RendicionCajaChica` | `Id`, `FondoCajaChicaId` FK, `Estado`, `FechaPresentacion`, `FechaDecision?`, `Observacion?` (500). |
| `DetallesRendicionCajaChica` → `DetalleRendicionCajaChica` | `Id`, `RendicionCajaChicaId` y `MovimientoCajaChicaId` como FKs. Candidata a unicidad por movimiento mientras la rendición lo ocupa. Si hay liberación posterior, persistir cuándo y con qué motivo. |

Importes candidatos `numeric(18,2)`, fechas `timestamptz` con `FechasUtc`, FKs indexadas y borrado restringido como candidato. El saldo del fondo se propone como ingresos registrados menos egresos netos en su moneda, con `Reposicion` y devolución como entradas y gasto y adelanto como salidas. Calcular el total de la rendición desde sus detalles. Presentar o aprobar una rendición conserva el saldo que ya reflejaron los movimientos.

### Operaciones y DTOs candidatos

Ruta base `/api/v1/caja-chica`. JWT y paginación 1 y 20 por defecto, tamaño de 1 a 100. Orden por código e `Id` para fondos y por fecha e `Id` descendentes para movimientos y rendiciones. Proyecciones asíncronas con `AsNoTracking`.

| Método y ruta relativa | Caso de uso | Entrada y respuesta |
|---|---|---|
| `GET /fondos` | `ListarFondosCajaChicaUseCase` | `activo?`, `moneda?` y paginación → `200 PaginaFondosCajaChicaDto`. |
| `GET /fondos/{id:int}` | `ObtenerFondoCajaChicaUseCase` | `200 FondoCajaChicaDto`. |
| `POST /fondos` | `CrearFondoCajaChicaUseCase` | `CrearFondoCajaChicaRequestDto` → `201 FondoCajaChicaDto` con `Location`. |
| `GET /fondos/{id:int}/movimientos` | `ListarMovimientosCajaChicaUseCase` | `tipo?`, `fechaDesde?`, `fechaHasta?` y paginación → `200 PaginaMovimientosCajaChicaDto`. |
| `POST /fondos/{id:int}/movimientos` | `RegistrarMovimientoCajaChicaUseCase` | `RegistrarMovimientoCajaChicaRequestDto` → `201 MovimientoCajaChicaDto`. |
| `GET /rendiciones` | `ListarRendicionesCajaChicaUseCase` | `fondoCajaChicaId?`, `estado?` y paginación → `200 PaginaRendicionesCajaChicaDto`. |
| `GET /rendiciones/{id:int}` | `ObtenerRendicionCajaChicaUseCase` | `200 RendicionCajaChicaDetalleDto`. |
| `POST /rendiciones` | `PresentarRendicionCajaChicaUseCase` | `PresentarRendicionCajaChicaRequestDto` → `201 RendicionCajaChicaDetalleDto` con `Location`. |
| `POST /rendiciones/{id:int}/aprobar` | `AprobarRendicionCajaChicaUseCase` | Sin cuerpo con `[PermitirCuerpoVacio]` → `200 RendicionCajaChicaDetalleDto`. |
| `POST /rendiciones/{id:int}/observar` | `ObservarRendicionCajaChicaUseCase` | Si se aprueba, `ObservarRendicionCajaChicaRequestDto` → `200 RendicionCajaChicaDetalleDto`. |

- `CrearFondoCajaChicaRequestDto { codigo, nombre, responsable, moneda }` para la alternativa de custodio como texto. `FondoCajaChicaDto { id, codigo, nombre, responsable, moneda, saldo, activo, fechaApertura }`.
- `RegistrarMovimientoCajaChicaRequestDto { tipo, importe, fechaOperacion, claveOperacion, concepto, referencia?, movimientoOrigenId? }`. `MovimientoCajaChicaDto { id, fondoCajaChicaId, tipo, importe, moneda, fechaOperacion, concepto, referencia?, movimientoOrigenId? }`.
- `PresentarRendicionCajaChicaRequestDto { fondoCajaChicaId, movimientoIds }` y `ObservarRendicionCajaChicaRequestDto { motivo }`. El listado de rendiciones expone `{ id, fondoCajaChicaId, estado, moneda, total, fechaPresentacion }`. El detalle añade `fechaDecision?`, `observacion?` y los movimientos de la rendición. Limitar los ítems por rendición según 57.3. Las páginas exponen `items`, `pagina`, `tamanoPagina`, `totalRegistros` y `totalPaginas`.

### Reglas candidatas y errores literales

Validar forma, existencia y pertenencia, luego estado, saldo y duplicados. Admitir para rendición solo movimientos cuyo tipo se apruebe y del mismo fondo. Si las devoluciones parciales se incluyen, validar el total ya devuelto contra el adelanto. Las rutas de cierre o reversión se fijan si se aprueban en 57.1 y 57.2.

| HTTP | Condición | Mensaje literal propuesto |
|---|---|---|
| 400 | Campo, escala, fecha, clave o paginación inválidos | «Los datos de caja chica no son válidos.» |
| 400 | Rendición vacía o ids repetidos | «La rendición debe indicar movimientos distintos.» |
| 404 | Fondo, movimiento o rendición inexistentes | «El fondo de caja chica no existe.» / «El movimiento de caja chica no existe.» / «La rendición de caja chica no existe.» |
| 409 | Código o clave duplicados | «El código de fondo de caja chica ya existe.» / «La clave de operación ya fue registrada.» |
| 409 | Fondo inactivo o saldo insuficiente si hay tope | «El fondo de caja chica está inactivo.» / «El fondo de caja chica no tiene saldo suficiente.» |
| 409 | Movimiento de otro fondo, tipo inelegible o rendición duplicada | «El movimiento no pertenece al fondo de la rendición.» / «El movimiento no admite rendición.» / «El movimiento ya está incluido en una rendición.» |
| 409 | Devolución excede el importe pendiente, si se aprueba | «La devolución supera el importe pendiente del adelanto.» |
| 409 | Estado de rendición incompatible | «El estado de la rendición no permite esta operación.» |
| 409 | Referencia eliminada durante el guardado | «No se pudo guardar la operación de caja chica porque una referencia ya no existe.» |

Serializar la decisión de saldo y el registro por fondo para los topes aprobados. Proteger unicidad de clave y del detalle de rendición. Presentación y cambios de estado guardan cabecera y detalles juntos. Una futura reposición enlazada al 55 identifica un traspaso, con un solo movimiento correspondiente en cada registro según 57.4.

## 5. Criterios de aceptación (medibles)

| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación y arquitectura | `dotnet build -warnaserror` y `./scripts/verify.sh` | 0 warnings, gate en verde |
| A2 | API | Pruebas por literal, JWT y permisos aprobados. Paginación, filtros y rutas en OpenAPI y Swagger | Todas pasan |
| A3 | Fondos y rendiciones | Dos fondos y monedas, reposición, egreso, rendición de un gasto, movimiento ajeno o repetido y transición inválida. Devoluciones según alcance | Saldo y estado pactados, rendir conserva el saldo |
| A4 | PostgreSQL y carreras | Dos gastos por encima del saldo si hay tope y dos rendiciones del mismo movimiento, con contextos separados | Sin duplicados ni escrituras parciales |
| A5 | Database First | Script local, seed repetible, scaffold y smoke con fondo, gasto y rendición | `SCAFFOLD AL DÍA` y `SMOKE OK` |

## 6. Checklist de ejecución

- [ ] Aprobar §2 y decidir si entran los vínculos del 55 y el 59.
- [ ] Confirmar numeración, aplicar SQL y generar el modelo.
- [ ] Implementar, verificar A1–A5 y registrar resultados.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-22 | Revisión documental | Pendiente de aprobación | `Empleado` y auditoría existentes. Movimientos, custodio y rendiciones propuestos. |
