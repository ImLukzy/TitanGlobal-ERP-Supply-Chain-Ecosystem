# Especificación: modulo56-conciliacion-bancaria-backend — Conciliación bancaria

> Estado: **propuesta pendiente de revisión y aprobación por Lukas**. Responsable: Leonel Portillo. El origen de extractos y las reglas de coincidencia y cierre son candidatos.

## 1. Objetivo

**Problema.** El [enunciado](../enunciado.md#2-los-80-módulos) pide conciliar extractos contra libros. La cuenta y sus movimientos internos se proponen en el 55. Falta un registro separado de la evidencia del banco y de las coincidencias verificadas.

**Resultado esperado.** Registrar un extracto normalizado de una cuenta, contrastar sus líneas con movimientos internos del 55 y conservar coincidencias, partidas pendientes y cierre.

### Base comprobada y dependencias

- El [55](modulo55-tesoreria-bancos-backend.md) es una dependencia de implementación. Su propuesta identifica `CuentaBancaria` y `MovimientoBancario` con cuenta, moneda, fecha, sentido e importe. Confirmar nombres y estados antes de crear FKs.
- El 56 conserva evidencia y correspondencias. Los saldos de tesorería y el registro de movimientos siguen en el 55. La arquitectura y las garantías de persistencia están en la [revisión de Finanzas](../equipo/revision-finanzas-52-60.md#reglas-del-repositorio).

## 2. Fuera de alcance

- Frontend, cargos o ajustes automáticos de tesorería, conexión bancaria y parseadores de archivos bancarios. El formato de entrada de una importación se decide si se incluye.
- Conciliación multimoneda, compensaciones agrupadas y reapertura como extensiones sujetas a aprobación.

| Nº | Tema | Propuesta para revisión | Qué debe confirmar Lukas |
|---|---|---|---|
| 56.1 | Extractos | Carga manual normalizada en JSON con cuenta, referencia, intervalo, saldos y líneas. Conservar dos líneas iguales cuando tienen posiciones distintas. | Formatos admitidos, identidad de extracto e ítem, duplicados entre extractos, periodos superpuestos y correcciones a una carga. |
| 56.2 | Coincidencias | Asociación manual uno a uno, misma cuenta y sentido, importe exacto y moneda de la cuenta. | Criterio de signos, tolerancias, ventana de fechas, agrupaciones, coincidencia automática y elegibilidad de los movimientos del 55, incluidos los vinculados a una reversión. |
| 56.3 | Cierre | Una conciliación por extracto. Cerrar si las líneas requeridas están conciliadas y el extracto cuadra según la validación aprobada. | Qué pendientes bloquean, fórmula y tolerancia, quién cierra y si hay reapertura o cierre parcial. |
| 56.4 | Correcciones y acceso | Retirar una coincidencia antes del cierre con motivo. Preservar la evidencia de un movimiento que después se compense en el 55. | Política frente a reversión posterior, autorización de registrar, asociar, retirar y cerrar, y efectos sobre conciliaciones cerradas. |

## 3. Archivos afectados

| Archivo | Acción | Nota |
|---|---|---|
| `database/NNN-modulo56-conciliacion-bancaria.sql` y `docs/equipo/relevo.md` | crear / actualizar tras aprobación | Confirmar número en [Relevo §8](../equipo/relevo.md#8-números-de-los-scripts-de-base-de-datos). |
| `src/TitanGlobal.Domain/Models/{ExtractoBancario,LineaExtractoBancario,ConciliacionBancaria,CoincidenciaConciliacion}.cs` | generar | Revisar las navegaciones inversas en `CuentaBancaria.cs` y `MovimientoBancario.cs`. |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` y `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | generar / ajustar si aplica | DbContext desde scaffold. Ajustar el pluralizador solo para los nombres nuevos que lo requieran. |
| `src/TitanGlobal.Application/DTOs/Modulo56_ConciliacionBancaria/` y `src/TitanGlobal.Application/UseCases/Modulo56_ConciliacionBancaria/` | crear | Proyecciones y validaciones del 56. |
| `src/TitanGlobal.Api/Controllers/Modulo56_ConciliacionBancaria/ConciliacionBancariaController.cs` | crear | Rutas de §4. |
| `tests/TitanGlobal.Api.Tests/Modulo56_ConciliacionBancaria/ConciliacionBancariaApiTests.cs`, `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | crear / actualizar | Datos deterministas y casos sobre el 55. |

## 4. Diseño y lógica

### Datos propuestos

| Tabla → entidad | Campos y relaciones candidatos |
|---|---|
| `ExtractosBancarios` → `ExtractoBancario` | `Id`, `CuentaBancariaId` FK, `ReferenciaExtracto` (80), `FechaDesde`, `FechaHasta`, `SaldoInicial`, `SaldoFinal`, `FechaRegistro`. Unicidad candidata por cuenta y referencia. Moneda obtenida desde la cuenta. |
| `LineasExtractoBancario` → `LineaExtractoBancario` | `Id`, `ExtractoBancarioId` FK, `Posicion`, `FechaOperacion`, `Sentido`, `Importe`, `Referencia?` (100). Unicidad `(ExtractoBancarioId, Posicion)`. |
| `ConciliacionesBancarias` → `ConciliacionBancaria` | `Id`, `ExtractoBancarioId` FK único para la propuesta inicial, `Estado`, `FechaApertura`, `FechaCierre?`. Se propone `Abierta` → `Cerrada`. |
| `CoincidenciasConciliacion` → `CoincidenciaConciliacion` | `Id`, `ConciliacionBancariaId`, `LineaExtractoBancarioId`, `MovimientoBancarioId` como FKs, `FechaVinculacion`, `FechaRetiro?`, `MotivoRetiro?` (500). Unicidad por línea y por movimiento. Si se permite retirar, aplicar la unicidad a filas con `FechaRetiro` nula. |

Importes candidatos `numeric(18,2)` y fechas `timestamptz` con `FechasUtc`. Indexar FKs y cuenta/periodo. El ámbito de coincidencia exige la misma cuenta y pertenencia de la línea al extracto de la conciliación. Verificarlo en Application. Los saldos de extracto y el saldo del 55 conservan sus fuentes independientes.

### Operaciones y DTOs candidatos

Ruta base `/api/v1/conciliacion-bancaria`. JWT y proyecciones asíncronas. Usar paginación 1 y 20 por defecto con tamaño máximo de 100. Ordenar por fecha del extracto e `Id` descendentes, y por posición para sus líneas.

| Método y ruta relativa | Caso de uso | Entrada y respuesta |
|---|---|---|
| `POST /extractos` | `RegistrarExtractoBancarioUseCase` | `RegistrarExtractoBancarioRequestDto` → `201 ExtractoBancarioDto` con `Location`. |
| `GET /extractos/{extractoId:int}` | `ObtenerExtractoBancarioUseCase` | `200 ExtractoBancarioDto`. |
| `GET /` | `ListarConciliacionesBancariasUseCase` | `cuentaBancariaId?`, `estado?` y paginación → `200 PaginaConciliacionesBancariasDto`. |
| `POST /` | `AbrirConciliacionBancariaUseCase` | `AbrirConciliacionBancariaRequestDto` → `201 ConciliacionBancariaDetalleDto` con `Location`. |
| `GET /{id:int}` | `ObtenerConciliacionBancariaUseCase` | `200 ConciliacionBancariaDetalleDto`. |
| `GET /{id:int}/pendientes` | `ListarPendientesConciliacionUseCase` | `lado=Extracto\|Banco` y paginación → `200 PaginaPendientesConciliacionDto`. |
| `POST /{id:int}/coincidencias` | `VincularCoincidenciaConciliacionUseCase` | `VincularCoincidenciaRequestDto` → `201 CoincidenciaConciliacionDto`. |
| `POST /{id:int}/coincidencias/{coincidenciaId:int}/retirar` | `RetirarCoincidenciaConciliacionUseCase` | `RetirarCoincidenciaRequestDto` → `200 CoincidenciaConciliacionDto`. |
| `POST /{id:int}/cerrar` | `CerrarConciliacionBancariaUseCase` | Sin cuerpo, con `[PermitirCuerpoVacio]` → `200 ConciliacionBancariaDetalleDto`. |

- `RegistrarExtractoBancarioRequestDto { cuentaBancariaId, referenciaExtracto, fechaDesde, fechaHasta, saldoInicial, saldoFinal, lineas: LineaExtractoBancarioRequestDto[] }`, con líneas `{ posicion, fechaOperacion, sentido, importe, referencia? }`. Limitar el número de líneas por carga según 56.1.
- `ExtractoBancarioDto { id, cuentaBancariaId, moneda, referenciaExtracto, fechaDesde, fechaHasta, saldoInicial, saldoFinal, cantidadLineas }`. `AbrirConciliacionBancariaRequestDto { extractoBancarioId }`.
- `ConciliacionBancariaListadoDto { id, extractoBancarioId, cuentaBancariaId, estado, fechaApertura, fechaCierre? }`. El detalle añade saldos del extracto, `diferenciaExtracto`, conteos de coincidencias y pendientes por lado. Un pendiente expone `{ lado, id, fechaOperacion, sentido, importe, referencia? }`. Páginas con `items`, `pagina`, `tamanoPagina`, `totalRegistros` y `totalPaginas`.
- `VincularCoincidenciaRequestDto { lineaExtractoBancarioId, movimientoBancarioId }` y `RetirarCoincidenciaRequestDto { motivo }`. `CoincidenciaConciliacionDto { id, conciliacionBancariaId, lineaExtractoBancarioId, movimientoBancarioId, fechaVinculacion, fechaRetiro?, motivoRetiro? }`.

### Reglas candidatas y errores literales

Validar intervalo y datos de cada línea, pertenencia, estado de la conciliación y unicidad. Con la convención propuesta del 55, un ingreso suma y un egreso resta. El descuadre del extracto sería saldo final − saldo inicial − suma firmada de líneas. Los pendientes del lado de tesorería comprenden los movimientos elegibles del 55 para esa cuenta e intervalo que estén sin coincidencia activa. 56.2 define la elegibilidad y 56.3 decide qué pendientes bloquean el cierre.

| HTTP | Condición | Mensaje literal propuesto |
|---|---|---|
| 400 | Intervalo, referencia, líneas, motivo o paginación inválidos | «Los datos de conciliación bancaria no son válidos.» |
| 400 | Posición repetida dentro del cuerpo | «El extracto contiene posiciones de línea repetidas.» |
| 404 | Cuenta, extracto, conciliación, línea, movimiento o coincidencia inexistentes | «La cuenta bancaria no existe.» / «El extracto bancario no existe.» / «La conciliación bancaria no existe.» / «La línea del extracto no existe.» / «El movimiento bancario no existe.» / «La coincidencia de conciliación no existe.» |
| 409 | Identidad de extracto repetida o extracto ya conciliado/iniciado | «El extracto bancario ya está registrado para esta cuenta.» / «El extracto ya tiene una conciliación bancaria.» |
| 409 | Línea de otro extracto, movimiento de otra cuenta o incompatibilidad | «La línea no pertenece al extracto de la conciliación.» / «El movimiento no pertenece a la cuenta del extracto.» / «La línea y el movimiento no cumplen el criterio de coincidencia.» |
| 409 | Línea o movimiento ya usado | «La línea o el movimiento ya tiene una coincidencia activa.» |
| 409 | Estado incompatible o cierre con diferencias | «El estado de la conciliación no permite esta operación.» / «La conciliación aún tiene diferencias o pendientes de cierre.» |
| 409 | Referencia eliminada durante el guardado | «No se pudo guardar la conciliación porque una referencia ya no existe.» |

La carga guarda extracto y líneas juntos. La unicidad de coincidencias activas protege dos asociaciones simultáneas. Retirar, vincular y cerrar deben compartir la garantía transaccional por conciliación para evitar una coincidencia agregada después de cerrar. Si la aprobación exige considerar una reversión concurrente del 55, definir el contrato entre ambos módulos y su prueba PostgreSQL.

## 5. Criterios de aceptación (medibles)

| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación y arquitectura | `dotnet build -warnaserror` y `./scripts/verify.sh` | 0 warnings, gate en verde |
| A2 | API | Pruebas HTTP por literal, JWT, roles aprobados, filtros y paginación. Validar rutas OpenAPI y Swagger | Todas pasan |
| A3 | Extractos y cierre | Carga, línea repetida, asociación exacta, monto desigual, cuenta ajena, retiro y cierre con y sin pendientes según aprobación | Evidencia conservada, movimientos del 55 intactos |
| A4 | Carreras | En PostgreSQL, dos asociaciones de la misma línea o movimiento y asociación concurrente con cierre | Un vínculo válido, ningún estado parcial |
| A5 | Database First | SQL local, seed repetible, scaffold y smoke con extracto, coincidencia y cierre o conflicto | `SCAFFOLD AL DÍA` y `SMOKE OK` |

## 6. Checklist de ejecución

- [ ] Aprobar §2 y validar el contrato del 55.
- [ ] Confirmar numeración, aplicar SQL y regenerar el modelo.
- [ ] Implementar las operaciones aprobadas y registrar A1–A5.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-22 | Revisión documental | Pendiente de aprobación | El 55 es una dependencia propuesta. Alcance de carga y cierre por confirmar. |
