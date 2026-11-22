# Especificación: modulo53-cuentas-por-cobrar-backend — Cuentas por cobrar

> Estado: **propuesta pendiente de revisión y aprobación por Lukas**. Responsable: Leonel Portillo. Las operaciones, reglas de cartera, DTOs y literales de §4 son candidatos.

## 1. Objetivo

**Problema.** El [enunciado](../enunciado.md#2-los-80-módulos) incluye cartera, letras y morosidad. El modelo revisado identifica al cliente y conserva el comprobante de venta, pero la cartera carece de un registro propio de obligaciones, cobros y ajustes.

**Resultado esperado.** Consultar el saldo por comprobante y cliente, registrar cobros y aplicar notas de crédito o débito elegibles. Mostrar vencimientos y totales por moneda conforme al criterio de morosidad que se apruebe.

### Base comprobada y dependencias

- `Clientes` (01) está en el esquema inicial. El 51 publicado en `fb84529` relaciona `Factura.ClienteId` con ese maestro y expone `Total`, `Moneda`, `Estado` y `FechaVencimiento` opcional. Confirmar el contrato definitivo del 51 al integrar su PR #48.
- La [propuesta del 52](modulo52-notas-credito-debito-backend.md) prevé `NotaCreditoDebito` vinculada a un único comprobante. El 53 sería dueño de cuándo y cómo una nota afecta su saldo. El [55](modulo55-tesoreria-bancos-backend.md) podría aportar evidencia bancaria en una fase posterior.
- Los contratos de persistencia, JWT y Database First están en la [revisión de Finanzas](../equipo/revision-finanzas-52-60.md#reglas-del-repositorio).

## 2. Fuera de alcance

- Frontend, cobro bancario efectivo, contabilidad automática, castigos y conversión de moneda. Letras, cuotas y mora financiera requieren decisión expresa para incluirlas.
- Cambios funcionales al 51 o al 52. La coordinación de sus anulaciones se debe aprobar antes de comprometer un flujo integrado.

| Nº | Tema | Propuesta para revisión | Qué debe confirmar Lukas |
|---|---|---|---|
| 53.1 | Origen | Apertura explícita con una cuenta por `FacturaId`. Copiar importe, moneda y vencimiento del comprobante. | Factura o boleta, estados y condiciones de venta elegibles, apertura explícita o automática, vencimiento ausente y cardinalidad si hay cuotas o letras. |
| 53.2 | Cobros | Abonos positivos en la misma moneda, parciales o totales, con clave de operación. Proponer reversión total del abono con motivo. | Medios y comprobantes exigidos, sobrepago y saldo a favor, reversión parcial o total, fecha admitida y respuesta a un reintento. |
| 53.3 | Notas y anulaciones | Aplicación explícita una vez por nota elegible de la misma factura. Evaluar débito como incremento y crédito como disminución. | Estado de nota elegible, crédito mayor al saldo, efectos de anular factura o nota ya aplicada, reaplicación y anulación de la cuenta. |
| 53.4 | Cartera | Vencido calculado para obligaciones con saldo positivo y vencimiento anterior al día de corte UTC. Resumen por una moneda. | Fecha y zona de corte, vencimiento en el día, agrupaciones, tramos y alcance de letras o mora. |
| 53.5 | Acceso e integración | JWT vigente. Si se aprueba enlace al 55, asociar un cobro con un movimiento de ingreso ya registrado. | Roles para consultar, registrar y revertir, y si debe existir contabilización bancaria coordinada en la misma operación. |

## 3. Archivos afectados

| Archivo | Acción | Nota |
|---|---|---|
| `database/NNN-modulo53-cuentas-por-cobrar.sql` y `docs/equipo/relevo.md` | crear / actualizar tras aprobación | Confirmar el número libre en [Relevo §8](../equipo/relevo.md#8-números-de-los-scripts-de-base-de-datos). |
| `src/TitanGlobal.Domain/Models/{CuentaPorCobrar,AplicacionCobro,AjusteCuentaPorCobrar}.cs` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | generar | Incluir las navegaciones inversas que produzcan las FKs en las entidades de origen aprobadas. |
| `src/TitanGlobal.Application/DTOs/Modulo53_CuentasPorCobrar/` y `src/TitanGlobal.Application/UseCases/Modulo53_CuentasPorCobrar/` | crear | Contratos y casos de uso de §4. |
| `src/TitanGlobal.Api/Controllers/Modulo53_CuentasPorCobrar/CuentasPorCobrarController.cs` | crear | Controlador delgado. |
| `tests/TitanGlobal.Api.Tests/Modulo53_CuentasPorCobrar/CuentasPorCobrarApiTests.cs` | crear | Fixtures de facturas, notas y cobranzas. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | actualizar | Datos idempotentes antes de `COMMIT;` y verificaciones con PostgreSQL real. |

## 4. Diseño y lógica

### Datos propuestos

| Tabla → entidad | Campos y relaciones candidatos |
|---|---|
| `CuentasPorCobrar` → `CuentaPorCobrar` | `Id`, `FacturaId` FK a `Facturas.Id` y único si hay una cuenta por factura, `Moneda` de tres letras, `ImporteOriginal`, `FechaApertura`, `FechaVencimiento?`, `FechaAnulacion?`, `MotivoAnulacion?`. Obtener cliente por la factura. |
| `AplicacionesCobro` → `AplicacionCobro` | `Id`, `CuentaPorCobrarId` FK, `ClaveOperacion` (80), `Importe`, `FechaOperacion`, `Referencia?` (100), `ReversionDeId?` FK a un abono de la misma cuenta, `Motivo?` (500). Candidatos a unicidad `(CuentaPorCobrarId, ClaveOperacion)` y una reversión por abono. |
| `AjustesCuentaPorCobrar` → `AjusteCuentaPorCobrar` | `Id`, `CuentaPorCobrarId` FK, `NotaCreditoDebitoId` FK y único para la aplicación inicial, `TipoNota`, `Importe` y `FechaAplicacion`. Guardar los valores aplicados. El modelo de reversión de notas se ajusta a 53.3. |

Importes candidatos `numeric(18,2)` y marcas UTC `timestamptz`. Indexar FKs y vencimiento. Conservar las referencias de origen con `ON DELETE RESTRICT` como candidato. Los borradores, importes solicitados y fechas de negocio se validan en Application según el alcance aprobado.

### Operaciones y DTOs candidatos

Ruta base `/api/v1/cuentas-por-cobrar`. JWT, parámetros en camelCase y proyección asíncrona con `AsNoTracking`. En listados, página positiva, tamaño 1 a 100, por defecto 1 y 20.

| Método y ruta relativa | Caso de uso | Entrada y respuesta |
|---|---|---|
| `GET /` | `ListarCuentasPorCobrarUseCase` | `clienteId?`, `facturaId?`, `moneda?`, `soloVencidas?`, `pagina`, `tamanoPagina` → `200 PaginaCuentasPorCobrarDto`. |
| `GET /{id:int}` | `ObtenerCuentaPorCobrarUseCase` | `200 CuentaPorCobrarDetalleDto`. |
| `GET /resumen` | `ResumirCuentasPorCobrarUseCase` | `moneda`, `clienteId?` → `200 ResumenCuentasPorCobrarDto`. |
| `GET /{id:int}/cobros` | `ListarAplicacionesCobroUseCase` | Paginación → `200 PaginaAplicacionesCobroDto`. |
| `POST /` | `AbrirCuentaPorCobrarUseCase` | `AbrirCuentaPorCobrarRequestDto` → `201 CuentaPorCobrarDetalleDto` con `Location`. |
| `POST /{id:int}/cobros` | `RegistrarAplicacionCobroUseCase` | `RegistrarAplicacionCobroRequestDto` → `201 AplicacionCobroDto`. |
| `POST /{id:int}/cobros/{cobroId:int}/revertir` | `RevertirAplicacionCobroUseCase` | Si se aprueba 53.2, `RevertirAplicacionCobroRequestDto` → `201 AplicacionCobroDto`. |
| `POST /{id:int}/ajustes-nota` | `AplicarNotaCuentaPorCobrarUseCase` | `AplicarNotaCuentaPorCobrarRequestDto` → `201 AjusteCuentaPorCobrarDto`. |
| `POST /{id:int}/anular` | `AnularCuentaPorCobrarUseCase` | Si se aprueba 53.3, `AnularCuentaPorCobrarRequestDto` → `200 CuentaPorCobrarDetalleDto`. |

- Entradas `AbrirCuentaPorCobrarRequestDto { facturaId }`, `RegistrarAplicacionCobroRequestDto { importe, moneda, fechaOperacion, claveOperacion, referencia? }`, `RevertirAplicacionCobroRequestDto { claveOperacion, motivo }`, `AplicarNotaCuentaPorCobrarRequestDto { notaCreditoDebitoId }` y `AnularCuentaPorCobrarRequestDto { motivo }`.
- `CuentaPorCobrarListadoDto { id, facturaId, clienteId, moneda, importeOriginal, saldo, fechaVencimiento?, estado, vencida }`. El detalle añade identificación del comprobante, apertura, anulación y totales netos de cobros y ajustes. `ResumenCuentasPorCobrarDto { moneda, fechaCorteUtc, cuentasAbiertas, saldoTotal, saldoVencido }`. Orden del listado por `FechaApertura` e `Id` descendentes.
- `AplicacionCobroDto { id, cuentaPorCobrarId, importe, moneda, fechaOperacion, claveOperacion, referencia?, reversionDeId?, motivo? }`. `AjusteCuentaPorCobrarDto { id, cuentaPorCobrarId, notaCreditoDebitoId, tipoNota, importe, fechaAplicacion }`. Páginas con `items`, `pagina`, `tamanoPagina`, `totalRegistros` y `totalPaginas`.

### Reglas candidatas y errores literales

Validar forma, referencias, estado y finalmente saldo o unicidad. Propuesta de saldo en la moneda del comprobante = importe original + débitos aplicados − créditos aplicados − cobros netos. Una reversión aprobada compensa el abono original. Se propone aplicar el estado derivado `Saldada` cuando el saldo llegue a cero y la cuenta no esté anulada. El resumen excluye anuladas y una cuenta sin vencimiento se muestra con `vencida=false`. La aplicación de notas se rige por 53.3 y conserva los importes del 51.

| HTTP | Condición | Mensaje literal propuesto |
|---|---|---|
| 400 | Identificador de origen inválido | «Debe indicar una factura válida.» |
| 400 | Campos, moneda, importe, escala, fecha, motivo o paginación inválidos | «Los datos de la cuenta por cobrar no son válidos.» |
| 404 | Factura, cuenta, abono de la cuenta o nota inexistente | «La factura de origen no existe.» / «La cuenta por cobrar no existe.» / «La aplicación de cobro no existe.» / «La nota de crédito o débito no existe.» |
| 409 | Apertura no elegible o duplicada | «La factura no admite la apertura de una cuenta por cobrar.» / «La factura ya tiene una cuenta por cobrar.» |
| 409 | Moneda ajena o saldo insuficiente | «La moneda del cobro debe coincidir con la de la cuenta.» / «La operación supera el saldo pendiente de cobro.» |
| 409 | Nota de otra factura, no elegible o ya aplicada | «La nota no corresponde a la factura de la cuenta.» / «El estado de la nota no permite aplicarla.» / «La nota ya está aplicada a la cuenta por cobrar.» |
| 409 | Reintento, reversión duplicada o estado incompatible | «La clave de operación ya fue registrada.» / «La aplicación de cobro ya fue revertida.» / «El estado de la cuenta por cobrar no permite esta operación.» |
| 409 | La referencia desaparece durante el guardado | «No se pudo guardar la cuenta por cobrar porque una referencia ya no existe.» |

Si se aprueba un límite por saldo, serializar por cuenta la comprobación de abonos, ajustes y reversiones junto con su escritura. Los índices únicos cubren el alta duplicada, la doble aplicación de nota y las claves repetidas. Ejecutar las pruebas concurrentes con dos contextos PostgreSQL. Persistir los cambios de cada operación con una llamada a `CompleteAsync()` dentro del mecanismo transaccional acordado. El bloqueo o control de versión previo requiere un contrato aprobado dentro de la arquitectura.

## 5. Criterios de aceptación (medibles)

| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación y arquitectura | `dotnet build -warnaserror` y `./scripts/verify.sh` | 0 warnings, gate en verde |
| A2 | Contratos HTTP | Pruebas con `WebApplicationFactory`: cada literal, 401 sin JWT, permisos aprobados, filtros, paginación y rutas en OpenAPI y Swagger | Todas pasan |
| A3 | Cartera | Factura elegible, apertura duplicada, abono parcial y total, nota de otra factura, aplicación duplicada y resumen por moneda. Probar vencimiento ausente y el día de corte | Saldos y códigos pactados |
| A4 | Integridad | En PostgreSQL, dos altas para la misma factura y dos abonos que juntos exceden el saldo, si se aprueba el tope. Confirmar filas e importes tras rechazo y reversión | Un solo efecto por clave, sin escritura parcial |
| A5 | Database First | Aplicar SQL en local, seed repetible, `scaffold.sh --comprobar` y `smoke-postgres.sh` con alta, detalle, abono y conflicto | `SCAFFOLD AL DÍA` y `SMOKE OK` |

## 6. Checklist de ejecución

- [ ] Resolver §2 con Lukas y confirmar las dependencias del 51 y el 52.
- [ ] Confirmar numeración, aplicar el SQL y generar los modelos.
- [ ] Implementar API y pruebas. Verificar A1–A5 y registrar evidencia.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-22 | Revisión documental | Pendiente de aprobación | Contratos del 51 examinados en `fb84529`. El enlace con el 52 sigue propuesto. |
