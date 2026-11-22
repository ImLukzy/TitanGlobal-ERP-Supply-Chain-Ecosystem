# Especificación: modulo55-tesoreria-bancos-backend — Tesorería y bancos

> Estado: **propuesta pendiente de revisión y aprobación por Lukas**. Responsable: Leonel Portillo. Los contratos de tesorería, saldos y movimientos se proponen para revisión.

## 1. Objetivo

**Problema.** El [enunciado](../enunciado.md#2-los-80-módulos) identifica movimientos, cheques y transferencias. El esquema base carece de un registro de cuentas bancarias y de sus movimientos internos.

**Resultado esperado.** Administrar cuentas bancarias internas, registrar movimientos y transferencias, consultar sus saldos por moneda y ofrecer una fuente estable a Conciliación bancaria. Confirmar el alcance de cheques antes de incluirlos.

### Base comprobada y dependencias

- El 55 parte del flujo y las convenciones de la [revisión de Finanzas](../equipo/revision-finanzas-52-60.md#reglas-del-repositorio). El catálogo incluye Sucursales (73) y MonedasTiposCambio (74). La base documental carece de sus tablas y contratos integrados. El núcleo propuesto puede usar un código de moneda validado y datos internos de la cuenta.
- El [56](modulo56-conciliacion-bancaria-backend.md) necesita `CuentaBancaria.Id`, su moneda y movimientos con cuenta, fecha, importe y sentido. Los módulos [53](modulo53-cuentas-por-cobrar-backend.md), [54](modulo54-cuentas-por-pagar-backend.md) y [57](modulo57-caja-chica-backend.md) podrían enlazar movimientos del 55 con operaciones propias. El registro bancario base se puede implementar sin ellos.

## 2. Fuera de alcance

- Frontend, conexión a bancos, ejecución de transferencias externas, importación de extractos, conciliación y asientos contables. El 56 administra los extractos y sus coincidencias.
- Conversiones, comisiones automáticas, vínculo con cuentas por cobrar o pagar, y cheques en la primera entrega, salvo decisión explícita.

| Nº | Tema | Propuesta para revisión | Qué debe confirmar Lukas |
|---|---|---|---|
| 55.1 | Cuenta | Código interno único, banco como texto, moneda fija y saldo inicial fechado. Guardar solo los datos bancarios operativos necesarios. | Ámbito de unicidad, uso de sucursal, datos a almacenar o mostrar, catálogo de monedas y cambios permitidos al haber movimientos. |
| 55.2 | Movimientos y saldo | `Ingreso` y `Egreso` como sentidos, importe positivo y correcciones por reversión. | Tipos, saldo negativo, fechas admitidas, matriz de roles, aprobaciones y sentido de cada evento. |
| 55.3 | Transferencias | Dos movimientos compensados y atómicos entre cuentas distintas de la misma moneda. | Transferencias entre monedas, comisiones, quién puede confirmar y si se admite reversión. |
| 55.4 | Identidad y conciliación | Clave de operación estable e historial inmutable. El 56 administra las coincidencias. | Ámbito de reintentos, qué efectos tiene revertir un movimiento ya conciliado y si requiere coordinación con 56. |
| 55.5 | Cheques | Incorporación condicionada, con referencia trazable al egreso cuando corresponda. | Si entran en esta entrega, numeración, estados, cuándo afectan saldo, rechazo, anulación y relación con 56. |
| 55.6 | Vínculos | Evidencia bancaria opcional en 53, 54 y 57, con una sola operación por efecto. | Qué canales enlazar, creación coordinada o manual, cardinalidad y orden transaccional de los cambios. |

## 3. Archivos afectados

| Archivo | Acción | Nota |
|---|---|---|
| `database/NNN-modulo55-tesoreria-bancos.sql` y `docs/equipo/relevo.md` | crear / actualizar tras aprobación | Confirmar número en [Relevo §8](../equipo/relevo.md#8-números-de-los-scripts-de-base-de-datos). |
| `src/TitanGlobal.Domain/Models/{CuentaBancaria,MovimientoBancario,TransferenciaBancaria}.cs` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | generar | Modelo del núcleo. Entidad de cheque condicionada a 55.5. |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | ajustar si el scaffold lo requiere | Solo mapeos de nombres nuevos aprobados. |
| `src/TitanGlobal.Application/DTOs/Modulo55_TesoreriaBancos/` y `src/TitanGlobal.Application/UseCases/Modulo55_TesoreriaBancos/` | crear | Consultas y operaciones del 55. |
| `src/TitanGlobal.Api/Controllers/Modulo55_TesoreriaBancos/TesoreriaBancosController.cs` | crear | Casos de uso de §4. |
| `tests/TitanGlobal.Api.Tests/Modulo55_TesoreriaBancos/TesoreriaBancosApiTests.cs`, `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | crear / actualizar | Casos HTTP, fixtures y PostgreSQL. |

## 4. Diseño y lógica

### Datos propuestos

| Tabla → entidad | Campos y relaciones candidatos |
|---|---|
| `CuentasBancarias` → `CuentaBancaria` | `Id`, `Codigo` (30), `Banco` (120), `Nombre` (120), `Moneda` (3), `SaldoInicial`, `FechaSaldoInicial`, `Activo`, `FechaDesactivacion?` y `MotivoDesactivacion?` (500). Código normalizado y único, ámbito sujeto a 55.1. |
| `MovimientosBancarios` → `MovimientoBancario` | `Id`, `CuentaBancariaId` FK, `FechaOperacion`, `Sentido`, `Importe`, `Referencia?` (100), `ClaveOperacion?` (80), `TransferenciaBancariaId?` FK, `ReversionDeId?` FK, `Motivo?` (500). Indexar cuenta y fecha. Unicidad candidata por cuenta y clave para operaciones manuales y en la FK de reversión para impedir doble efecto. |
| `TransferenciasBancarias` → `TransferenciaBancaria` | `Id`, `CuentaOrigenId` y `CuentaDestinoId` FKs, `Importe`, `FechaOperacion`, `ClaveOperacion` (80) única, `Referencia?` (100), `ReversionDeId?` y `Motivo?` si se aprueban. Cada transferencia registra un egreso y un ingreso. Candidato a unicidad `(TransferenciaBancariaId, Sentido)` de sus movimientos. |

Importes candidatos `numeric(18,2)`. Normalizar instantes `timestamptz` con `FechasUtc`. Indexar las FKs y conservar referencias con `ON DELETE RESTRICT` como candidato. Una cuenta adoptaría una sola moneda durante la vigencia del historial. Se propone un saldo informativo = saldo inicial + ingresos registrados − egresos registrados. Las reversiones se reflejan mediante el movimiento compensatorio.

### Operaciones y DTOs candidatos

Ruta base `/api/v1/tesoreria-bancos`. JWT y consultas proyectadas con `AsNoTracking`. Listados paginados con valores por defecto 1 y 20. Orden de cuentas por código e `Id` y de movimientos por fecha e `Id` descendentes.

| Método y ruta relativa | Caso de uso | Entrada y respuesta |
|---|---|---|
| `GET /cuentas` | `ListarCuentasBancariasUseCase` | `moneda?`, `activo?` y paginación → `200 PaginaCuentasBancariasDto`. |
| `POST /cuentas` | `CrearCuentaBancariaUseCase` | `CrearCuentaBancariaRequestDto` → `201 CuentaBancariaDetalleDto` con `Location`. |
| `GET /cuentas/{id:int}` | `ObtenerCuentaBancariaUseCase` | `200 CuentaBancariaDetalleDto`. |
| `POST /cuentas/{id:int}/desactivar` | `DesactivarCuentaBancariaUseCase` | Si se aprueba, `DesactivarCuentaBancariaRequestDto` → `200 CuentaBancariaDetalleDto`. |
| `GET /cuentas/{id:int}/movimientos` | `ListarMovimientosBancariosUseCase` | `sentido?`, `fechaDesde?`, `fechaHasta?` y paginación → `200 PaginaMovimientosBancariosDto`. |
| `POST /cuentas/{id:int}/movimientos` | `RegistrarMovimientoBancarioUseCase` | `RegistrarMovimientoBancarioRequestDto` → `201 MovimientoBancarioDto`. |
| `POST /cuentas/{id:int}/movimientos/{movimientoId:int}/revertir` | `RevertirMovimientoBancarioUseCase` | `RevertirMovimientoBancarioRequestDto` → `201 MovimientoBancarioDto`. |
| `POST /transferencias` | `RegistrarTransferenciaBancariaUseCase` | `RegistrarTransferenciaBancariaRequestDto` → `201 TransferenciaBancariaDto` con `Location`. |
| `GET /transferencias/{id:int}` | `ObtenerTransferenciaBancariaUseCase` | `200 TransferenciaBancariaDto`. |
| `POST /transferencias/{id:int}/revertir` | `RevertirTransferenciaBancariaUseCase` | Si se aprueba, `RevertirMovimientoBancarioRequestDto` → `201 TransferenciaBancariaDto`. |

- `CrearCuentaBancariaRequestDto { codigo, banco, nombre, moneda, saldoInicial, fechaSaldoInicial }` y `DesactivarCuentaBancariaRequestDto { motivo }`. `CuentaBancariaListadoDto { id, codigo, banco, nombre, moneda, saldo, activo }`. El detalle añade `saldoInicial`, `fechaSaldoInicial`, `fechaDesactivacion?` y `motivoDesactivacion?`.
- `RegistrarMovimientoBancarioRequestDto { fechaOperacion, sentido, importe, claveOperacion, referencia? }`, `RevertirMovimientoBancarioRequestDto { claveOperacion, motivo }` y `MovimientoBancarioDto { id, cuentaBancariaId, moneda, sentido, importe, fechaOperacion, referencia?, transferenciaBancariaId?, reversionDeId? }`.
- `RegistrarTransferenciaBancariaRequestDto { cuentaOrigenId, cuentaDestinoId, importe, fechaOperacion, claveOperacion, referencia? }`. `TransferenciaBancariaDto { id, cuentaOrigenId, cuentaDestinoId, moneda, importe, fechaOperacion, movimientoEgresoId, movimientoIngresoId, reversionDeId? }`. Páginas con `items`, `pagina`, `tamanoPagina`, `totalRegistros` y `totalPaginas`.
- Si se aprueba 55.5, el candidato usa `ChequesEmitidos` (`CuentaBancariaId`, `Numero`, `Importe`, `FechaEmision`, `Estado`, `MovimientoBancarioId?`) y `POST /cheques` con `RegistrarChequeEmitidoUseCase`, entrada `{ cuentaBancariaId, numero, importe, fechaEmision }` y salida con id, estado y movimiento. Confirmar el ciclo y los contratos de consulta y anulación antes de incluir esa variante.

### Reglas candidatas y errores literales

Validar forma, referencias, vigencia de las cuentas, moneda y saldo o unicidad. La reversión de un movimiento requiere que pertenezca a la cuenta y sea elegible. Los movimientos de transferencia se revierten con su cabecera. El núcleo del 55 propone movimientos registrados y referencias de reversión. La elegibilidad para conciliarlos se decide con 56.2 y el manejo de coincidencias existentes con 55.4 y 56.4.

| HTTP | Condición | Mensaje literal propuesto |
|---|---|---|
| 400 | Datos, importe, sentido, fecha, motivo o paginación inválidos | «Los datos de tesorería no son válidos.» |
| 400 | Origen igual al destino | «Las cuentas de origen y destino deben ser distintas.» |
| 404 | Cuenta, movimiento de esa cuenta o transferencia inexistente | «La cuenta bancaria no existe.» / «El movimiento bancario no existe.» / «La transferencia bancaria no existe.» |
| 409 | Código, clave o reversión repetidos | «El código de cuenta bancaria ya existe.» / «La clave de operación ya fue registrada.» / «La operación bancaria ya fue revertida.» |
| 409 | Cuenta desactivada o acción incompatible | «La cuenta bancaria está desactivada.» / «El movimiento no admite una reversión individual.» |
| 409 | Transferencia entre monedas o egreso por encima del saldo, si hay tope | «Las cuentas de la transferencia deben tener la misma moneda.» / «La cuenta bancaria no tiene saldo suficiente.» |
| 409 | Referencia eliminada durante el guardado | «No se pudo guardar la operación bancaria porque una referencia ya no existe.» |

Guardar cabecera y ambos movimientos en la misma unidad transaccional. Si existe tope de saldo, serializar la consulta y escritura por cuenta. En operaciones con dos cuentas, adquirir las garantías en el orden estable de sus ids. Probar la carrera de dos egresos, la de transferencias en sentido opuesto y la doble reversión con PostgreSQL.

## 5. Criterios de aceptación (medibles)

| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación y arquitectura | `dotnet build -warnaserror` y `./scripts/verify.sh` | 0 warnings, gate en verde |
| A2 | HTTP | WebApplicationFactory verifica JWT, permisos acordados, cada literal, filtros y paginación. OpenAPI y Swagger exponen las rutas aprobadas | Todas pasan |
| A3 | Saldos y transferencia | Apertura, ingreso, egreso, transferencia de la misma moneda, rechazo de monedas distintas y reversión aprobada | Dos movimientos por transferencia, cero cambios parciales al fallar |
| A4 | Integridad | Competencia por saldo si se aprueba el tope, clave repetida y dos reversiones simultáneas en PostgreSQL | Un efecto por clave, sin saldo indebido |
| A5 | Database First | Aplicar SQL local, seed repetible, scaffold y smoke con movimiento, transferencia y error representativo | `SCAFFOLD AL DÍA` y `SMOKE OK` |

## 6. Checklist de ejecución

- [ ] Aprobar §2 y confirmar el contrato de movimientos con el 56.
- [ ] Confirmar numeración, aplicar SQL y generar el modelo.
- [ ] Implementar y verificar A1–A5 con datos por cuenta y moneda.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-22 | Revisión documental | Pendiente de aprobación | Alcance base derivado del enunciado. Contratos de movimientos y transferencias propuestos para revisión conjunta con el 56. |
