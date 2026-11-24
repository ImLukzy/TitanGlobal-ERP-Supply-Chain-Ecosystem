# Especificación: modulo53-cuentas-por-cobrar-backend — Cuentas por cobrar

> Estado: **aprobada** por Lukas el 2026-11-23 para la v0. Responsable: Leonel Portillo. La revisión final del [PR #52](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52) sigue pendiente.

## 1. Objetivo

Abrir por solicitud una cuenta por cobrar para una factura emitida, registrar y revertir cobros completos y aplicar una nota de crédito de forma explícita. Mostrar saldo y vencimiento por cliente y moneda.

### Base comprobada y dependencias

- **Disponible en `main`.** Clientes del 01 se consulta mediante `Factura.ClienteId`.
- **Previas.** El 51 aporta la factura emitida, el total, la moneda, la fecha de emisión y el vencimiento opcional. Su [PR #48](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/48) sigue sin integrarse. El [52](modulo52-notas-credito-debito-backend.md) aportará el crédito emitido, con su total y su factura de origen.
- **Guarda conjunta.** El PR del 53 añade al 51 el rechazo por cobros y al 52 la consulta de aplicaciones para proteger la anulación. La tabla de aplicaciones pertenece al 53.

## 2. Fuera de alcance

- Frontend, cuotas, letras, cargos de mora, sobrepagos, saldo a favor, conversiones y contabilidad.
- Apertura automática al emitir, efecto de notas de débito en cartera y registro de movimientos del 55.

**Decisiones de producto aprobadas para la v0.** [Respuesta de Lukas](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52#issuecomment-7109010161)

| Nº | Tema | Respuesta aprobada |
|---|---|---|
| 53.1 | Apertura | Crear una cuenta por factura `Emitida` mediante `POST` explícito. Conservar su vencimiento; cuando falta, usar la fecha de emisión más 30 días. Sin cuotas ni letras. |
| 53.2 | Cobros y reversión | Aceptar `Efectivo`, `Transferencia`, `Tarjeta` o `Deposito`, con referencia opcional y fecha desde emisión hasta hoy. El sobrepago devuelve 409. Revertir el cobro completo con motivo. |
| 53.3 | Aplicaciones y anulaciones | Aplicar el crédito por solicitud. Si supera el saldo, devolver 409. El 51 rechaza con 409 anular una factura con cobros. Anular la cuenta solo cuando esté sin cobros. |
| 53.4 | Cartera | Cortar a hoy en Lima. Considerar vencida la deuda solo cuando su vencimiento sea anterior al corte. Agrupar por cliente en tramos 0–30, 31–60, 61–90 y más de 90 días. Sin mora. |
| 53.5 | Acceso | Aplicar G1. Registrar los cobros dentro del 53, sin generar movimientos bancarios (G6). |

Las [reglas comunes G1 a G9](../equipo/revision-finanzas-52-60.md#reglas-generales-g1-a-g9) se aplican junto con las precisiones de §4.

## 3. Archivos afectados

| Archivo previsto | Acción futura | Alcance |
|---|---|---|
| `database/NNN-modulo53-cuentas-por-cobrar.sql` y `docs/equipo/relevo.md` | Crear / registrar | Tomar el siguiente número libre al implementar y registrar solo la fila de este módulo. [Relevo §8](../equipo/relevo.md#8-números-de-los-scripts-de-base-de-datos) |
| `src/TitanGlobal.Domain/Models/` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | Generar | Entidades y navegaciones de §4 mediante scaffold. Revisar los nombres que produce el pluralizador. |
| `src/TitanGlobal.Application/DTOs/Modulo53_CuentasPorCobrar/` y `src/TitanGlobal.Application/UseCases/Modulo53_CuentasPorCobrar/` | Crear | Validación, proyecciones y casos de uso concretos en Application. |
| `src/TitanGlobal.Api/Controllers/Modulo53_CuentasPorCobrar/CuentasPorCobrarController.cs` | Crear | Rutas y autorización de §4. |
| `AnularFacturaUseCase.cs` del 51 y anulación de notas del 52 | Ampliar al implementar el 53 | Guardas por cobro de factura y crédito aplicado. Añadir pruebas de esos contratos. |
| `tests/TitanGlobal.Api.Tests/Modulo53_CuentasPorCobrar/CuentasPorCobrarApiTests.cs` | Crear | Pruebas HTTP de §4. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | Ampliar | Fixtures idempotentes y checks HTTP/SQL del módulo. |

## 4. Diseño y lógica

Los nombres, tamaños auxiliares, rutas y mecanismos de persistencia concretan el diseño para la revisión final. Las decisiones de producto aprobadas están en §2 y G1 a G9.

### Convenciones y permisos

Controller → UseCase → IUnitOfWork → Repository → DbContext → PostgreSQL. DTOs en camelCase. Las lecturas requieren JWT. Las escrituras requieren `FINANZAS` (Id 7, seed del 52) o `ADMIN` mediante `[Authorize(Roles = "FINANZAS,ADMIN")]`. Los 401, 403 y el 400 de cuerpo vacío conservan los literales del pipeline. Las acciones sin cuerpo usan `[PermitirCuerpoVacio]`. Las fechas de negocio usan `DateOnly` y `date` con JSON `yyyy-MM-dd`. Los instantes del servidor usan `timestamptz` y `FechasUtc`.

Saldo, pagos y aplicaciones usan `numeric(18,2)` en la moneda `PEN` o `USD` de la factura. El plazo derivado de 30 días puede producir un vencimiento futuro. La prohibición de G8 aplica a fechas efectivas de operación como `FechaCobro`.

### Datos y persistencia

| Tabla → entidad | Campos y restricciones |
|---|---|
| `CuentasPorCobrar` → `CuentaPorCobrar` | `Id`, `FacturaId` FK única a `Facturas`, `ImporteOriginal`, `Moneda char(3)`, `FechaEmision date`, `FechaVencimiento date`, `FechaApertura timestamptz`, `FechaAnulacion timestamptz?` y `MotivoAnulacion varchar(300)?`. Cliente por la factura. Índice por vencimiento. |
| `AplicacionesCobro` → `AplicacionCobro` | `Id`, `CuentaPorCobrarId` FK, `Importe` positivo, `Medio varchar(20)` restringido al catálogo, `Referencia varchar(100)?`, `FechaCobro date`, `FechaRegistro timestamptz`, `FechaReversion timestamptz?` y `MotivoReversion varchar(300)?`. El importe original se conserva al revertir. |
| `AjustesCuentaPorCobrar` → `AjusteCuentaPorCobrar` | `Id`, `CuentaPorCobrarId` FK, `NotaCreditoDebitoId` FK única a la nota de crédito y `ImporteAplicado` positivo igual al total del crédito, `FechaAplicacion timestamptz`. Una aplicación por nota. |

Las PK son identidades `integer`. Las FKs se indexan y conservan referencias con `ON DELETE RESTRICT`. Application valida los límites de columna antes de guardar.

Saldo = importe original − cobros vigentes − créditos aplicados. El reverso restaura el saldo por el importe completo. El resumen incluye saldo pendiente y tramos por `(ClienteId, Moneda)`. Por ejemplo, una deuda de 100 con un cobro de 30 y un crédito de 20 queda en 50. Al revertir el cobro queda en 80. Al vencer hoy, la deuda se muestra al día.

### Casos de uso, rutas y DTOs

Ruta base `/api/v1/cuentas-por-cobrar`. Las rutas de consulta proyectan con `AsNoTracking`. Listar por `clienteId?`, `facturaId?`, `moneda?`, `soloVencidas?`, `pagina=1`, `tamanoPagina=20` y máximo 100. Orden por `FechaApertura` e `Id` descendentes.

| Método y ruta relativa | Caso de uso | Entrada → éxito |
|---|---|---|
| `GET /` | `ListarCuentasPorCobrarUseCase` | Filtros y paginación → `200 PaginaCuentasPorCobrarDto`. |
| `GET /{id:int}` | `ObtenerCuentaPorCobrarUseCase` | `200 CuentaPorCobrarDetalleDto`. |
| `GET /resumen` | `ResumirCuentasPorCobrarUseCase` | `clienteId?`, `moneda?` → `200 ResumenCuentasPorCobrarDto`. |
| `GET /{id:int}/cobros` | `ListarAplicacionesCobroUseCase` | Paginación → `200 PaginaAplicacionesCobroDto`. |
| `POST /` | `AbrirCuentaPorCobrarUseCase` | `AbrirCuentaPorCobrarRequestDto { facturaId }` → `201` detalle con `Location`. |
| `POST /{id:int}/cobros` | `RegistrarAplicacionCobroUseCase` | `RegistrarAplicacionCobroRequestDto` → `201 AplicacionCobroDto`. |
| `POST /{id:int}/cobros/{cobroId:int}/revertir` | `RevertirAplicacionCobroUseCase` | `RevertirAplicacionCobroRequestDto { motivo }` → `200 AplicacionCobroDto`. |
| `POST /{id:int}/ajustes-nota` | `AplicarNotaCuentaPorCobrarUseCase` | `AplicarNotaCuentaPorCobrarRequestDto { notaCreditoDebitoId }` → `201 AjusteCuentaPorCobrarDto`. |
| `POST /{id:int}/anular` | `AnularCuentaPorCobrarUseCase` | `AnularCuentaPorCobrarRequestDto { motivo }` → `200` detalle. |

- `RegistrarAplicacionCobroRequestDto { importe, medio, fechaCobro, referencia? }`. `AplicacionCobroDto { id, cuentaPorCobrarId, importe, moneda, medio, fechaCobro, referencia?, fechaReversion?, motivoReversion? }`.
- `CuentaPorCobrarListadoDto { id, facturaId, clienteId, moneda, importeOriginal, saldo, fechaVencimiento, anulada, vencida }`. Detalle con fecha de emisión, comprobante original, totales netos, apertura y anulación.
- `AjusteCuentaPorCobrarDto { id, cuentaPorCobrarId, notaCreditoDebitoId, importeAplicado, fechaAplicacion }`. `ResumenCuentasPorCobrarDto { fechaCorte, grupos }` con `clienteId`, `moneda`, `saldoAlDia`, `saldoVencido` y los cuatro tramos. Páginas con `items`, `pagina`, `tamanoPagina`, `totalRegistros`, `totalPaginas`.

### Validaciones y mensajes literales

Validar forma, referencias, estado y saldo. El alta consulta una factura `Emitida` y respeta la unicidad por `FacturaId`. Cobro y crédito operan sobre una cuenta vigente y usan saldo calculado bajo bloqueo. El crédito debe ser `Credito`, `Emitida`, de la misma factura y sin aplicación previa. El cobro y su reversión conservan el medio y el importe de origen. Cuando un movimiento llega hasta el saldo exacto, el saldo queda en cero.

La apertura, los cobros, la aplicación, la reversión y las guardas de anulación coordinan el bloqueo por factura y cuenta en orden estable antes de consultar saldo y estado. No se modifican los importes del 51 ni la nota aplicada. El reporte usa la fecha de Lima para calcular `diasVencidos = max(0, corte − vencimiento)` y agrupa por cliente y moneda. Los tramos se calculan sobre saldos vencidos positivos.

Los textos de negocio de la tabla son **propuestas concretas para la revisión final**. El UseCase responde con `ReglaNegocioException` y `CodigosEstado`. El cuerpo conserva `{ StatusCode, Message }`. Las carreras de unicidad y FK se traducen mediante `ViolacionRestriccionException` del `UnitOfWork`.

| HTTP | Condición | Literal propuesto |
|---|---|---|
| 400 | Ids, medio, fecha, escala, filtros o paginación inválidos | «Los datos de la cuenta por cobrar no son válidos.» |
| 400 | Fecha de cobro fuera del intervalo aprobado | «La fecha del cobro debe estar entre la emisión de la factura y hoy.» |
| 400 | Motivo fuera de 3–300 | «El motivo debe tener entre 3 y 300 caracteres.» |
| 404 | Factura inexistente | «La factura de origen no existe.» |
| 404 | Cuenta inexistente | «La cuenta por cobrar no existe.» |
| 404 | Cobro ajeno o inexistente | «El cobro indicado no existe en la cuenta.» |
| 404 | Nota inexistente | «La nota de crédito o débito no existe.» |
| 409 | Factura inelegible | «La factura no admite la apertura de una cuenta por cobrar.» |
| 409 | Factura con cuenta previa | «La factura ya tiene una cuenta por cobrar.» |
| 409 | Cobro mayor al saldo | «El cobro supera el saldo pendiente.» |
| 409 | Crédito mayor al saldo | «La nota de crédito supera el saldo pendiente.» |
| 409 | Nota inelegible o de otra factura | «Solo puede aplicarse una nota de crédito emitida de la misma factura.» |
| 409 | Nota aplicada | «La nota de crédito ya está aplicada.» |
| 409 | Reversión repetida | «El cobro ya fue revertido.» |
| 409 | Anulación con cobros (cuenta) | «No se puede anular una cuenta por cobrar con cobros.» |
| 409 | Anulación con cobros (factura del 51) | «No se puede anular un comprobante con cobros.» |
| 409 | Cuenta anulada | «La cuenta por cobrar está anulada.» |
| 409 | Referencia perdida durante el guardado | «No se pudo guardar la operación porque una referencia ya no existe.» |

### Precisiones para la revisión final

- **Reversiones y anulación.** Falta precisar si un cobro revertido deja de bloquear las anulaciones de cuenta y factura. La restricción se prueba con cobros vigentes. Confirmar el efecto de una factura anulada sobre una cuenta sin cobros, el tratamiento de una cuenta con crédito aplicado sin cobros y la precedencia cuando el 51 detecta cobros y notas emitidas a la vez.
- **Origen y vigencia.** La apertura documentada usa `TipoComprobante = Factura`. Confirmar el uso del término «factura» frente a boleta en el contrato del 51. Mantener la fecha del documento al convertir el valor UTC del 51 a `date` y aplicar Lima al corte.

### Seed, pruebas y smoke

**Seed.** Preparar por claves naturales una factura emitida con vencimiento y otra sin vencimiento, para monedas distintas. Abrir cuentas de prueba, incluir un cobro vigente y otro revertido, y un crédito emitido no aplicado en una tercera factura. Cargar los ejemplos de forma idempotente y conservar los datos de origen.

**Pruebas de integración.**

1. Alta explícita, origen no emitido, factura sin fecha de vencimiento, duplicidad y cobros en ambos extremos del intervalo de fechas.
2. Cobro en cada medio, cobro parcial, saldo cero, sobrepago, reversión completa con motivo, repetición y saldo tras reversión.
3. Aplicar crédito de la misma factura en `Emitida`, duplicidad, nota de débito, crédito superior al saldo y guarda del 52 para anular nota aplicada.
4. Cuentas y facturas con cobros vigentes devuelven el 409 correspondiente al anular. Verificar agrupación por cliente y moneda, día de vencimiento, cortes 30/31/60/61/90/91 días y cambio de fecha en Lima.
5. Ejercitar cada literal, JWT 401/403, consulta autenticada, escritura con ambos roles y ausencia de movimientos del 55.

**Smoke PostgreSQL.** Probar alta y duplicidad, cobro, sobrepago, reversión, aplicación y respuestas de anulación en el 51/52. Con dos conexiones, competir por el saldo de la cuenta y por la misma nota, y comparar los saldos persistidos. Añadir `request_check`, la validación Python de códigos y cuerpos, y la comprobación de OpenAPI. Ejecutar en una base desechable hasta `SMOKE OK`.

## 5. Criterios de aceptación (medibles)

| # | Criterio | Verificación | Umbral |
|---|---|---|---|
| A1 | Arquitectura y compilación | `dotnet build -warnaserror`, `scripts/check-rules.sh` y `./scripts/verify.sh` | Gate en verde |
| A2 | API y autorización | Pruebas de cada literal, rutas, JWT y roles G1 | Lecturas autenticadas. Escrituras FINANZAS/ADMIN. Cada rechazo conserva los datos. |
| A3 | Apertura y cobros | Fechas, medios, reversión y saldo por factura | Una cuenta y ningún cobro por encima del saldo |
| A4 | Créditos y guardas | Aplicación única y anulaciones del 51, 52 y 53 | Saldo consistente y HTTP 409 en los casos definidos |
| A5 | Cartera | Cobertura de límites de tramos y hora Lima | Saldos por cliente y moneda con corte correcto |
| A6 | Database First | Aplicar SQL local, repetir seed, comprobar scaffold y ejecutar smoke | SCAFFOLD AL DÍA y SMOKE OK |

## 6. Checklist de ejecución

- [x] Incorporar las decisiones oficiales de §2 y G1 a G9.
- [ ] Terminar la revisión del PR de documentación y cerrar las precisiones de §4.
- [ ] Confirmar dependencias, aplicar el SQL en local y generar entidades y DbContext.
- [ ] Implementar API, pruebas, seed y smoke. Registrar los resultados de §5.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-22 | Modelo del 51 | Revisado | `fb84529` aporta los contratos de facturas y el vencimiento opcional. |
| 2026-11-23 | Decisiones v0 | Aprobadas. Revisión final del PR pendiente. | [Respuesta de Lukas](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52#issuecomment-7109010161) |
| 2026-11-23 | Validación funcional de la spec aprobada | Pendiente | Esta fase revisa documentación. La ejecución de §5 corresponde a la rama de implementación. |
