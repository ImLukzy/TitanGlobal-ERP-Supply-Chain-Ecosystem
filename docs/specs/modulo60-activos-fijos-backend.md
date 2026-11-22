# Especificación: modulo60-activos-fijos-backend — Activos fijos

> Estado: **propuesta pendiente de revisión y aprobación por Lukas**. Responsable: Leonel Portillo. El reconocimiento, la depreciación y la baja requieren validación funcional antes de implementar.

## 1. Objetivo

**Problema.** El [enunciado](../enunciado.md#2-los-80-módulos) enumera altas, bajas y depreciación. El modelo revisado carece de un registro de activos y del historial de sus periodos de depreciación.

**Resultado esperado.** Registrar el alta y la baja de activos, relacionarlos con un centro de costo y consultar su historia. Registrar depreciaciones conforme al método, fechas y valores que se aprueben.

### Base comprobada y dependencias

- El [59](modulo59-centros-costo-backend.md) proveería el identificador de centro de costo para la asignación inicial. Las existencias de `Productos` y los `VehiculosFlota` pertenecen a dominios con contratos propios.
- El [54](modulo54-cuentas-por-pagar-backend.md) podría aportar un documento del proveedor como evidencia opcional. Una misma obligación podría respaldar varios activos si se aprueba. El [58](modulo58-presupuestos-backend.md) podría consumir un alta como ejecución CAPEX después de aprobar la fuente.
- El catálogo incluye CierresPeriodo (80). La aplicación de cierres corporativos depende de su contrato. Rigen las convenciones de la [revisión de Finanzas](../equipo/revision-finanzas-52-60.md#reglas-del-repositorio).

## 2. Fuera de alcance

- Frontend, asientos, determinación tributaria, revalúos, deterioros, mantenimiento, subcomponentes y traslados en el núcleo candidato.
- Generación automática desde compras, alta desde inventario y ejecución CAPEX hasta acordar el origen y sus efectos.

| Nº | Tema | Propuesta para revisión | Qué debe confirmar Lukas |
|---|---|---|---|
| 60.1 | Alta y asignación | Alta explícita, código interno único, identidad y centro de costo. Si se aprueba, referencia documental al 54. | Qué se capitaliza, datos y evidencia, categorías, unicidad por ámbito, obligatoriedad y cambios del centro de costo. |
| 60.2 | Base, método y moneda | Candidato a depreciación periódica lineal con costo, residual, vida en periodos y moneda fija. | Base capitalizable, valores admitidos, método, vida útil, residual, escala, redondeo y tratamiento del último periodo. |
| 60.3 | Calendario | Un cálculo explícito por periodo mensual elegible, identificado por año y mes. | Inicio desde puesta en servicio u otro hito, prorrateo, periodos omitidos, cálculo en lote, cierres y fechas de corte. |
| 60.4 | Baja y corrección | Baja trazable con fecha y motivo, con vista del valor libro calculado según el método aprobado. | Motivos y estados, última depreciación, reversión o reapertura de bajas y corrección de periodos ya registrados. |
| 60.5 | Acceso e integraciones | JWT con roles pendientes. Alta y depreciación propias del 60. | Quién registra, deprecia y autoriza la baja. Si 54 prueba el origen y si el alta, la compra u otro evento alimenta el 58. |

## 3. Archivos afectados

| Archivo | Acción | Nota |
|---|---|---|
| `database/NNN-modulo60-activos-fijos.sql` y `docs/equipo/relevo.md` | crear / actualizar tras aprobación | Confirmar número en [Relevo §8](../equipo/relevo.md#8-números-de-los-scripts-de-base-de-datos). |
| `src/TitanGlobal.Domain/Models/{ActivoFijo,DepreciacionActivoFijo,BajaActivoFijo}.cs` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | generar | Revisar navegaciones inversas en `CentroCosto.cs` y en el origen aprobado. |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | ajustar si se necesita | Mapeos de nombres nuevos únicamente. |
| `src/TitanGlobal.Application/DTOs/Modulo60_ActivosFijos/` y `src/TitanGlobal.Application/UseCases/Modulo60_ActivosFijos/` | crear | Altas, consultas, depreciaciones y bajas. |
| `src/TitanGlobal.Api/Controllers/Modulo60_ActivosFijos/ActivosFijosController.cs` | crear | Rutas de §4. |
| `tests/TitanGlobal.Api.Tests/Modulo60_ActivosFijos/ActivosFijosApiTests.cs`, `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | crear / actualizar | Fixtures y pruebas HTTP y PostgreSQL. |

## 4. Diseño y lógica

### Datos propuestos

| Tabla → entidad | Campos y relaciones candidatos |
|---|---|
| `ActivosFijos` → `ActivoFijo` | `Id`, `Codigo` (30) único, `Nombre` (120), `Descripcion?` (500), `CentroCostoId` FK, `FechaAlta`, `FechaPuestaServicio?`, `Moneda` (3), `Costo`, `ValorResidual`, `VidaUtilPeriodos`, `Metodo`, `Estado`. Los últimos campos dependen del método confirmado. Agregar `CuentaPorPagarId?` solo si se aprueba el vínculo. |
| `DepreciacionesActivoFijo` → `DepreciacionActivoFijo` | `Id`, `ActivoFijoId` FK, `Anio`, `Mes`, `Importe`, `FechaRegistro`, `BaseAplicada` o datos suficientes para explicar el valor registrado. Unicidad candidata por `(ActivoFijoId, Anio, Mes)` para el registro vigente del periodo. Diseñar historial de corrección solo si se aprueba 60.4. |
| `BajasActivoFijo` → `BajaActivoFijo` | `Id`, `ActivoFijoId` FK y único si la baja es final, `FechaBaja`, `FechaRegistro`, `Motivo` (500) y `ValorLibroAlMomento`, sujeto a la regla de baja aprobada. |

Montos candidatos `numeric(18,2)`. Las fechas de registro, puesta en servicio y baja usan `timestamptz` normalizado con `FechasUtc`. Año y mes identifican un periodo según el calendario aprobado. Indexar FKs y centro/estado para las consultas. Los cambios al catálogo del 59 preservan la referencia histórica del activo.

### Operaciones y DTOs candidatos

Ruta base `/api/v1/activos-fijos`. JWT y proyección con `AsNoTracking`. Paginación 1 y 20 por defecto con máximo 100, orden por fecha de alta e `Id` descendentes. Para depreciaciones, ordenar por año, mes e `Id` descendentes.

| Método y ruta relativa | Caso de uso | Entrada y respuesta |
|---|---|---|
| `GET /` | `ListarActivosFijosUseCase` | `centroCostoId?`, `estado?`, `moneda?`, `busqueda?` y paginación → `200 PaginaActivosFijosDto`. |
| `GET /{id:int}` | `ObtenerActivoFijoUseCase` | `200 ActivoFijoDetalleDto`. |
| `POST /` | `DarAltaActivoFijoUseCase` | `DarAltaActivoFijoRequestDto` → `201 ActivoFijoDetalleDto` con `Location`. |
| `GET /{id:int}/depreciaciones` | `ListarDepreciacionesActivoFijoUseCase` | `anio?` y paginación → `200 PaginaDepreciacionesActivoFijoDto`. |
| `POST /{id:int}/depreciaciones` | `RegistrarDepreciacionActivoFijoUseCase` | `RegistrarDepreciacionActivoFijoRequestDto` → `201 DepreciacionActivoFijoDto` si se aprueba el cálculo. |
| `POST /{id:int}/baja` | `DarBajaActivoFijoUseCase` | `DarBajaActivoFijoRequestDto` → `200 ActivoFijoDetalleDto`. |

- `DarAltaActivoFijoRequestDto { codigo, nombre, descripcion?, centroCostoId, fechaPuestaServicio?, moneda, costo, valorResidual, vidaUtilPeriodos }` para la alternativa lineal. Incorporar método en la entrada si se admiten varios.
- `ActivoFijoListadoDto { id, codigo, nombre, centroCostoId, moneda, costo, estado, fechaAlta }`. El detalle agrega los parámetros aprobados, depreciación acumulada, valor libro y, si corresponde, `baja: { fechaBaja, motivo, valorLibroAlMomento }`.
- `RegistrarDepreciacionActivoFijoRequestDto { anio, mes }`. `DepreciacionActivoFijoDto { id, activoFijoId, anio, mes, moneda, importe, fechaRegistro }`. `DarBajaActivoFijoRequestDto { fechaBaja, motivo }`. Las páginas exponen `items`, `pagina`, `tamanoPagina`, `totalRegistros` y `totalPaginas`.

### Reglas candidatas y errores literales

Validar datos y centro de costo, elegibilidad del activo y luego periodo o unicidad. Proponer alta en estado `Activo` y registrar fecha y motivo al pasar a `Baja`, conservando las depreciaciones previas. Evaluar como fórmula candidata una cuota lineal sobre costo menos residual, dividida entre los periodos de vida útil. El momento de la primera cuota, el prorrateo, el redondeo y el ajuste final requieren ejemplos aprobados. Proponer valor libro informativo como costo menos depreciación neta registrada, según el alcance acordado.

| HTTP | Condición | Mensaje literal propuesto |
|---|---|---|
| 400 | Código, periodo, fecha, motivo o paginación inválidos | «Los datos del activo fijo no son válidos.» |
| 400 | Costo, residual, vida o escala fuera del rango aprobado | «Los parámetros de depreciación no son válidos.» |
| 404 | Activo o centro inexistentes | «El activo fijo no existe.» / «El centro de costo no existe.» |
| 409 | Código repetido o centro inelegible | «El código del activo fijo ya existe.» / «El centro de costo no admite esta asignación.» |
| 409 | Periodo repetido o inelegible | «El activo ya tiene una depreciación para ese periodo.» / «El activo fijo no admite depreciación en ese periodo.» |
| 409 | Baja repetida, incompatibilidad con el historial o estado | «El activo fijo ya está dado de baja.» / «La fecha de baja no es compatible con las depreciaciones registradas.» / «El estado del activo fijo no permite esta operación.» |
| 409 | Referencia eliminada durante el guardado | «No se pudo guardar el activo fijo porque una referencia ya no existe.» |

Registro y baja deben coordinarse transaccionalmente por activo. El índice de periodo evita doble depreciación. Si se aprueba un tope acumulado, serializar el cálculo y la escritura para que dos periodos concurrentes no superen la base aprobada. Guardar el cambio de estado y la baja juntos.

## 5. Criterios de aceptación (medibles)

| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación y arquitectura | `dotnet build -warnaserror` y `./scripts/verify.sh` | 0 warnings, gate en verde |
| A2 | API | Prueba HTTP por literal, JWT, roles aprobados, filtros y paginación. Verificar rutas en OpenAPI y Swagger | Todas pasan |
| A3 | Alta, cálculo y baja | Centro válido, alta duplicada, periodos de inicio y fin, ejemplo aprobado de residual y redondeo, periodo duplicado, baja y depreciación posterior | Cuotas y estados pactados |
| A4 | PostgreSQL y carreras | Dos registros del mismo periodo y depreciación frente a baja simultánea. Validar fechas, escala y persistencia completa | Un efecto por periodo, sin escritura parcial |
| A5 | Database First | Aplicar SQL local, seed idempotente, scaffold y smoke con alta, depreciación autorizada y baja | `SCAFFOLD AL DÍA` y `SMOKE OK` |

## 6. Checklist de ejecución

- [ ] Aprobar §2 y validar ejemplos de depreciación y baja con Lukas.
- [ ] Confirmar el contrato del 59, numeración, SQL y scaffold.
- [ ] Implementar y verificar A1–A5 con los casos aprobados.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-22 | Revisión documental | Pendiente de aprobación | El enunciado identifica altas, bajas y depreciación. Modelo, fórmula y calendario sometidos a revisión. |
