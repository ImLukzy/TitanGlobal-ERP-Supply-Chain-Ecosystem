# Especificación: modulo54-cuentas-por-pagar-backend — Cuentas por pagar

> Estado: **propuesta pendiente de revisión y aprobación por Lukas**. Responsable: Leonel Portillo. El origen de obligaciones, las reglas de pago y los contratos de §4 requieren aprobación.

## 1. Objetivo

**Problema.** El [enunciado](../enunciado.md#2-los-80-módulos) asigna las obligaciones con proveedores al 54. El maestro de proveedores y la orden de abastecimiento ya existen. La deuda, el pago y el saldo no tienen un registro propio en el esquema revisado.

**Resultado esperado.** Registrar deudas documentadas con proveedores, aplicar pagos y consultar saldos y vencimientos en la moneda del documento. Conservar la referencia que justifica cada obligación y pago.

### Base comprobada y dependencias

- `Proveedores` (11) ya ofrece `Id`, RUC único y `Activo`. `OrdenesAbastecimiento` (15) tiene `ProveedorId`, estados y líneas con cantidad y precio unitario. Su spec excluye la facturación y su recepción cambia el estado del pedido. Una orden podría ser referencia de una deuda si se aprueba. El 51 representa comprobantes de venta de clientes.
- La incorporación de recepciones (19), evidencia bancaria ([55](modulo55-tesoreria-bancos-backend.md)) o clasificación analítica ([59](modulo59-centros-costo-backend.md)) es opcional. El [58](modulo58-presupuestos-backend.md) y el [60](modulo60-activos-fijos-backend.md) podrían consultar una obligación como origen en una integración posterior.
- El 75 provee un maestro y cálculo por tasa vigente. El alcance propuesto toma el importe del documento del proveedor. Cualquier desglose, retención u otro cálculo exige decisión funcional.

## 2. Fuera de alcance

- Frontend, autorización o ejecución bancaria, conciliación, contabilidad automática y liquidación tributaria.
- Recepción de mercadería, modificación de órdenes y automatización del alta desde compras hasta que se apruebe la fuente y el momento de reconocimiento.

| Nº | Tema | Propuesta para revisión | Qué debe confirmar Lukas |
|---|---|---|---|
| 54.1 | Fuente | Registrar manualmente un documento del proveedor, con orden opcional y del mismo proveedor. | Clases de documento, proveedores admisibles, cuándo nace la deuda, relación con órdenes o recepciones y si se permiten cuotas. |
| 54.2 | Identidad e importe | Una deuda por documento identificado por proveedor, tipo, serie y número. Capturar importe y moneda del documento. | Unicidad y normalización, series opcionales, ámbito por emisor o sucursal, moneda, componentes, impuestos y retenciones si se incluyen. |
| 54.3 | Pagos y anulaciones | Pagos parciales o totales en la moneda de la deuda. Reversión total del pago con motivo y anulación de deuda sin pagos vigentes. | Anticipos, sobrepagos, pagos agrupados, formas y evidencias requeridas, reversión parcial, efecto de la anulación y reintentos. |
| 54.4 | Vencimiento y acceso | Corte informativo por día UTC y resumen por moneda. JWT según el proyecto. | Tratamiento de vencimiento ausente, tramos, roles de lectura y escritura y si se exige aprobación antes de pagar. |
| 54.5 | Integraciones | El 54 administra la deuda y referencia al proveedor del 11. Puede consultar un egreso del 55 como evidencia o un centro del 59 como clasificación. | Si se requiere pago coordinado con 55, clasificación por centro y uso de una obligación como fuente para el 58 o el 60. |

## 3. Archivos afectados

| Archivo | Acción | Nota |
|---|---|---|
| `database/NNN-modulo54-cuentas-por-pagar.sql` y `docs/equipo/relevo.md` | crear / actualizar tras aprobación | Confirmar el número en [Relevo §8](../equipo/relevo.md#8-números-de-los-scripts-de-base-de-datos). |
| `src/TitanGlobal.Domain/Models/{CuentaPorPagar,AplicacionPago}.cs`, `Proveedor.cs` y, si corresponde, `OrdenAbastecimiento.cs` | generar | Entidades nuevas y navegaciones inversas por FKs. |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | generar | Mapeo desde PostgreSQL con scaffold. |
| `src/TitanGlobal.Application/DTOs/Modulo54_CuentasPorPagar/` y `src/TitanGlobal.Application/UseCases/Modulo54_CuentasPorPagar/` | crear | Contratos y operaciones de §4. |
| `src/TitanGlobal.Api/Controllers/Modulo54_CuentasPorPagar/CuentasPorPagarController.cs` | crear | Controlador delgado. |
| `tests/TitanGlobal.Api.Tests/Modulo54_CuentasPorPagar/CuentasPorPagarApiTests.cs` | crear | Fixtures de proveedores, documentos y pagos. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | actualizar | Seed idempotente y pruebas PostgreSQL. |

## 4. Diseño y lógica

### Datos propuestos

| Tabla → entidad | Campos y relaciones candidatos |
|---|---|
| `CuentasPorPagar` → `CuentaPorPagar` | `Id`, `ProveedorId` FK, `OrdenAbastecimientoId?` FK opcional, `TipoDocumento` (20), `Serie` (10), `NumeroDocumento` (40, texto para conservar ceros), `FechaDocumento`, `FechaVencimiento?`, `Moneda` (3), `ImporteOriginal`, `FechaRegistro`, `FechaAnulacion?`, `MotivoAnulacion?` (500). Índice único candidato `(ProveedorId, TipoDocumento, Serie, NumeroDocumento)`. Si se aprueba serie ausente, definir su representación antes de crear el índice. |
| `AplicacionesPago` → `AplicacionPago` | `Id`, `CuentaPorPagarId` FK, `ClaveOperacion` (80), `Importe`, `FechaOperacion`, `Referencia?` (100), `ReversionDeId?` FK a un pago de la misma cuenta, `Motivo?` (500). Candidatos a unicidad `(CuentaPorPagarId, ClaveOperacion)` y una reversión por pago. |

Importes candidatos `numeric(18,2)`, fechas `timestamptz` normalizadas con `FechasUtc`, índices por proveedor, FKs y vencimiento. Los enlaces a origen conservan la referencia con `ON DELETE RESTRICT` como candidato. Validar que la orden seleccionada pertenezca al proveedor de la deuda.

### Operaciones y DTOs candidatos

Ruta base `/api/v1/cuentas-por-pagar`. JWT. Consultas LINQ asíncronas proyectadas con `AsNoTracking`. Paginación por defecto 1 y 20, tamaño de 1 a 100, orden del listado por registro e `Id` descendentes.

| Método y ruta relativa | Caso de uso | Entrada y respuesta |
|---|---|---|
| `GET /` | `ListarCuentasPorPagarUseCase` | `proveedorId?`, `moneda?`, `soloVencidas?`, `pagina`, `tamanoPagina` → `200 PaginaCuentasPorPagarDto`. |
| `GET /{id:int}` | `ObtenerCuentaPorPagarUseCase` | `200 CuentaPorPagarDetalleDto`. |
| `GET /resumen` | `ResumirCuentasPorPagarUseCase` | `moneda`, `proveedorId?` → `200 ResumenCuentasPorPagarDto`. |
| `GET /{id:int}/pagos` | `ListarAplicacionesPagoUseCase` | Paginación → `200 PaginaAplicacionesPagoDto`. |
| `POST /` | `RegistrarCuentaPorPagarUseCase` | `RegistrarCuentaPorPagarRequestDto` → `201 CuentaPorPagarDetalleDto` con `Location`. |
| `POST /{id:int}/pagos` | `RegistrarAplicacionPagoUseCase` | `RegistrarAplicacionPagoRequestDto` → `201 AplicacionPagoDto`. |
| `POST /{id:int}/pagos/{pagoId:int}/revertir` | `RevertirAplicacionPagoUseCase` | Si se aprueba, `RevertirAplicacionPagoRequestDto` → `201 AplicacionPagoDto`. |
| `POST /{id:int}/anular` | `AnularCuentaPorPagarUseCase` | Si se aprueba, `AnularCuentaPorPagarRequestDto` → `200 CuentaPorPagarDetalleDto`. |

- `RegistrarCuentaPorPagarRequestDto { proveedorId, ordenAbastecimientoId?, tipoDocumento, serie, numeroDocumento, fechaDocumento, fechaVencimiento?, moneda, importeOriginal }`.
- `RegistrarAplicacionPagoRequestDto { importe, moneda, fechaOperacion, claveOperacion, referencia? }`, `RevertirAplicacionPagoRequestDto { claveOperacion, motivo }` y `AnularCuentaPorPagarRequestDto { motivo }`.
- `CuentaPorPagarListadoDto { id, proveedorId, tipoDocumento, serie, numeroDocumento, moneda, importeOriginal, saldo, fechaVencimiento?, estado, vencida }`. El detalle añade nombre de proveedor, orden asociada, fecha del documento y registro, pagos netos y datos de anulación. `AplicacionPagoDto { id, cuentaPorPagarId, importe, moneda, fechaOperacion, claveOperacion, referencia?, reversionDeId?, motivo? }`.
- `ResumenCuentasPorPagarDto { moneda, fechaCorteUtc, cuentasAbiertas, saldoTotal, saldoVencido }`. Las páginas incluyen `items`, `pagina`, `tamanoPagina`, `totalRegistros` y `totalPaginas`.

### Reglas candidatas y errores literales

Validar forma, referencias, estado y luego duplicidad o saldo. Proponer vencimiento igual o posterior a la fecha de documento si se informa. Propuesta de saldo = importe del documento − pagos netos. Una reversión compensa el pago original y conserva ambas filas. Se propone aplicar `Saldada` cuando el saldo llegue a cero y la cuenta no esté anulada. El resumen excluye anuladas y el vencimiento se evalúa en cuentas no anuladas con saldo positivo, según el corte acordado en 54.4.

| HTTP | Condición | Mensaje literal propuesto |
|---|---|---|
| 400 | Documento, moneda, importe, fecha, motivo o paginación inválidos | «Los datos de la cuenta por pagar no son válidos.» |
| 400 | Vencimiento anterior a la fecha del documento, si se adopta | «El vencimiento no puede ser anterior a la fecha del documento.» |
| 404 | Proveedor, orden, cuenta o aplicación de la cuenta inexistente | «El proveedor no existe.» / «La orden de abastecimiento no existe.» / «La cuenta por pagar no existe.» / «La aplicación de pago no existe.» |
| 409 | Proveedor inelegible u orden de otro proveedor | «El proveedor no admite nuevas obligaciones.» / «La orden de abastecimiento no corresponde al proveedor.» |
| 409 | Documento o clave del pago duplicados | «El documento del proveedor ya está registrado.» / «La clave de operación ya fue registrada.» |
| 409 | Moneda distinta o saldo insuficiente | «La moneda del pago debe coincidir con la de la cuenta.» / «El pago supera el saldo pendiente de la cuenta.» |
| 409 | Reversión duplicada o estado incompatible | «La aplicación de pago ya fue revertida.» / «El estado de la cuenta por pagar no permite esta operación.» |
| 409 | Referencia eliminada durante el guardado | «No se pudo guardar la cuenta por pagar porque una referencia ya no existe.» |

Para el límite de pago, serializar la lectura que decide el saldo y la escritura por cuenta dentro de la misma operación aprobada de persistencia. Guardar el pago y los cambios asociados juntos. Usar unicidad en PostgreSQL para documento, clave y reversión. Cualquier enlace con 55 debe tener una identidad estable y registrar un solo efecto por operación.

## 5. Criterios de aceptación (medibles)

| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación y arquitectura | `dotnet build -warnaserror` y `./scripts/verify.sh` | 0 warnings, gate en verde |
| A2 | Contrato HTTP | Pruebas con `WebApplicationFactory` cubren códigos y literales, paginación y orden, JWT y roles aprobados. Comprobar rutas en OpenAPI y Swagger | Todas pasan |
| A3 | Obligaciones y pagos | Proveedor válido e inexistente, orden de otro proveedor, documento repetido, pago parcial y total, reversión, límites y corte, dos monedas en datos de prueba | Saldos y filtros pactados |
| A4 | Concurrencia | Dos altas de un documento y dos pagos que juntos excedan el saldo si se aprueba tope, con contextos PostgreSQL separados | Sin duplicado ni escritura parcial |
| A5 | Database First | Aplicar SQL local, probar seed y scaffold, añadir al smoke alta, detalle, pago y duplicado | `SCAFFOLD AL DÍA` y `SMOKE OK` |

## 6. Checklist de ejecución

- [ ] Aprobar §2 con Lukas y confirmar las fuentes incluidas.
- [ ] Confirmar numeración, aplicar SQL y regenerar entidades.
- [ ] Implementar API, seed y pruebas. Registrar A1–A5.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-22 | Revisión documental | Pendiente de aprobación | `Proveedores` y `OrdenesAbastecimiento` comprobados en el esquema base. Validaciones y contratos de §4 propuestos. |
