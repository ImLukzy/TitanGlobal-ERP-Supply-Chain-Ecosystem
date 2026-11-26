# Especificación: modulo54-cuentas-por-pagar-backend — Cuentas por pagar

> Estado: **aprobada** por Lukas el 2026-11-23 para la v0. Responsable: Leonel Portillo. Las decisiones v0 quedaron integradas por el [PR #52](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52).

## 1. Objetivo

Registrar facturas de proveedores activos como cuentas por pagar, mantener saldos por documento y registrar pagos y su reversión íntegra. Mostrar vencimientos por proveedor y moneda.

### Base comprobada y dependencias

- **Disponibles en `main`.** [Proveedores (11)](12-modulo11-proveedores-backend.md) aporta `ProveedorId` y `Activo`. [Órdenes de abastecimiento (15)](16-modulo15-ordenes-abastecimiento-backend.md) aporta una referencia con `ProveedorId` para el vínculo opcional.
- **Transversal.** El rol `FINANZAS` (Id 7) se provisiona con el SQL 024 del módulo 52. Las pruebas emiten JWT de ese rol con la infraestructura existente. El 54 registra componentes documentarios sin consultar tasas del 75 y aplica la misma convención de reporte que el 53.

## 2. Fuera de alcance

- Frontend, cuotas, retenciones, anticipos, aprobación de pagos, recepción de órdenes y registro de sus cambios de estado.
- Operaciones de tesorería, conciliación, presupuesto, alta de activos, contabilidad y cambios de moneda.

**Decisiones de producto aprobadas para la v0.** [Respuesta de Lukas](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52#issuecomment-7109010161)

| Nº | Tema | Respuesta aprobada |
|---|---|---|
| 54.1 | Nacimiento | Registrar factura de un proveedor activo del 11. La deuda nace con el alta. La orden del 15 es una FK opcional. Trabajar sin cuotas. |
| 54.2 | Identidad e importes | Unicidad por proveedor, serie y número de origen. Normalizar serie y número a mayúsculas sin espacios. Capturar base e impuesto y calcular el total, sin retenciones. |
| 54.3 | Pagos y anulación | Admitir pagos parciales sin anticipos ni sobrepago. Una reversión compensa el pago completo y exige motivo. La anulación del documento requiere ausencia de pagos. |
| 54.4 | Cartera | Exigir fecha de vencimiento. Usar los tramos del 53. El pago se registra sin aprobación previa. |
| 54.5 | Independencia | La v0 conserva sus deudas y pagos en el 54, de acuerdo con G6. |

Las [reglas comunes G1 a G9](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/blob/dbd4155fae4be59ab39ab9f3eb2a8bd17cd38534/docs/equipo/revision-finanzas-52-60.md#reglas-generales-g1-a-g9) se aplican junto con las precisiones de §4.

## 3. Archivos afectados

| Archivo previsto | Acción futura | Alcance |
|---|---|---|
| `database/047-modulo54-cuentas-por-pagar.sql` y `docs/equipo/relevo.md` | Crear / registrar | El 047 es el siguiente número libre en las reservas y scripts publicados al iniciar la rama. [Relevo §8](../equipo/relevo.md#8-números-de-los-scripts-de-base-de-datos) |
| `src/TitanGlobal.Domain/Models/` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | Generar | Entidades y navegaciones de §4 mediante scaffold. Revisar los nombres que produce el pluralizador. |
| `src/TitanGlobal.Application/DTOs/Modulo54_CuentasPorPagar/` y `src/TitanGlobal.Application/UseCases/Modulo54_CuentasPorPagar/` | Crear | Validación, proyecciones y casos de uso concretos en Application. |
| `src/TitanGlobal.Api/Controllers/Modulo54_CuentasPorPagar/CuentasPorPagarController.cs` | Crear | Rutas y autorización de §4. |
| `tests/TitanGlobal.Api.Tests/Modulo54_CuentasPorPagar/CuentasPorPagarApiTests.cs` | Crear | Pruebas HTTP de §4. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | Ampliar | Fixtures idempotentes y checks HTTP/SQL del módulo. |

## 4. Diseño y lógica

Los nombres, tamaños auxiliares, rutas y mecanismos de persistencia concretan el diseño para la revisión final. Las decisiones de producto aprobadas están en §2 y G1 a G9.

### Convenciones y permisos

Controller → UseCase → IUnitOfWork → Repository → DbContext → PostgreSQL. DTOs en camelCase. Las lecturas requieren JWT. Las escrituras requieren `FINANZAS` (Id 7, SQL 024 del módulo 52) o `ADMIN` mediante `[Authorize(Roles = "FINANZAS,ADMIN")]`. Los 401, 403 y el 400 de cuerpo vacío conservan los literales del pipeline. Las acciones sin cuerpo usan `[PermitirCuerpoVacio]`. Las fechas de negocio usan `DateOnly` y `date` con JSON `yyyy-MM-dd`. Los instantes del servidor usan `timestamptz` y `FechasUtc`.

La moneda `PEN` o `USD` se captura del documento externo y se hereda en los pagos. La serie y el número del proveedor identifican un documento externo y conservan su clave con ceros a la izquierda. G5 rige folios internos y no sustituye esa referencia. El 54 usa importes `numeric(18,2)` y los tramos y corte de Lima del 53.

### Datos y persistencia

| Tabla → entidad | Campos y restricciones |
|---|---|
| `CuentasPorPagar` → `CuentaPorPagar` | `Id`, `ProveedorId` FK, `OrdenAbastecimientoId` FK opcional, `Serie varchar(10)`, `NumeroDocumento varchar(40)`, `FechaDocumento date`, `FechaVencimiento date` obligatoria, `Moneda char(3)`, `BaseImponible`, `ImporteImpuesto`, `Total`, `FechaRegistro timestamptz`, `FechaAnulacion timestamptz?`, `MotivoAnulacion varchar(300)?`. Unicidad `(ProveedorId, Serie, NumeroDocumento)` e índice por vencimiento. |
| `AplicacionesPago` → `AplicacionPago` | `Id`, `CuentaPorPagarId` FK, `Importe` positivo, `FechaPago date`, `FechaRegistro timestamptz`, `FechaReversion timestamptz?`, `MotivoReversion varchar(300)?`. Conservar el pago original como evidencia. |

Las PK son identidades `integer`. Las FKs se indexan y conservan referencias con `ON DELETE RESTRICT`. Application valida los límites de columna antes de guardar.

Total = base capturada + impuesto capturado. La validación técnica propuesta exige base e impuesto no negativos y total mayor que cero, con un máximo de dos decimales por valor. El saldo de la cuenta vigente se calcula como total − pagos vigentes. Una cuenta anulada conserva su total original y tiene saldo cero. El estado se refleja en la marca de anulación y en el saldo calculado. El impuesto se toma del documento externo.

### Casos de uso, rutas y DTOs

Ruta base `/api/v1/cuentas-por-pagar`. Las rutas de consulta proyectan con `AsNoTracking`. Filtros `proveedorId?`, `moneda?`, `soloVencidas?` y paginación 1/20, máximo 100. Orden por registro e `Id` descendentes.

| Método y ruta relativa | Caso de uso | Entrada → éxito |
|---|---|---|
| `GET /` | `ListarCuentasPorPagarUseCase` | Filtros → `200 PaginaCuentasPorPagarDto`. |
| `GET /{id:int}` | `ObtenerCuentaPorPagarUseCase` | `200 CuentaPorPagarDetalleDto`. |
| `GET /resumen` | `ResumirCuentasPorPagarUseCase` | `proveedorId?`, `moneda?` → `200 ResumenCuentasPorPagarDto`. |
| `GET /{id:int}/pagos` | `ListarAplicacionesPagoUseCase` | Paginación → `200 PaginaAplicacionesPagoDto`. |
| `POST /` | `RegistrarCuentaPorPagarUseCase` | `RegistrarCuentaPorPagarRequestDto` → `201` detalle y `Location`. |
| `POST /{id:int}/pagos` | `RegistrarAplicacionPagoUseCase` | `RegistrarAplicacionPagoRequestDto` → `201 AplicacionPagoDto`. |
| `POST /{id:int}/pagos/{pagoId:int}/revertir` | `RevertirAplicacionPagoUseCase` | `RevertirAplicacionPagoRequestDto { motivo }` → `200 AplicacionPagoDto`. |
| `POST /{id:int}/anular` | `AnularCuentaPorPagarUseCase` | `AnularCuentaPorPagarRequestDto { motivo }` → `200` detalle. |

- `RegistrarCuentaPorPagarRequestDto { proveedorId, ordenAbastecimientoId?, serie, numeroDocumento, fechaDocumento, fechaVencimiento, moneda, baseImponible, importeImpuesto }`.
- `RegistrarAplicacionPagoRequestDto { importe, fechaPago }`. `AplicacionPagoDto { id, cuentaPorPagarId, importe, moneda, fechaPago, fechaReversion?, motivoReversion? }`.
- `CuentaPorPagarListadoDto { id, proveedorId, serie, numeroDocumento, moneda, total, saldo, fechaVencimiento, anulada, vencida }`. El detalle añade orden, fecha documentaria, base, impuesto, importe pagado vigente y datos de anulación. `ResumenCuentasPorPagarDto { fechaCorte, grupos }`, por proveedor y moneda, con `saldoAlDia`, `saldoVencido`, `vencido0A30`, `vencido31A60`, `vencido61A90` y `vencidoMasDe90`. Páginas con `items`, `pagina`, `tamanoPagina`, `totalRegistros`, `totalPaginas`.
- El alta fija los datos documentarios. Los cambios posteriores registran pagos, reversiones y anulación. El registro del pago recibe importe y fecha.

### Validaciones y mensajes literales

Validar forma y rango antes de consultar proveedor y orden. La fecha del documento y la del pago pueden llegar hasta hoy en Lima. Normalizar la clave externa eliminando espacios y pasando a mayúsculas. Exigir proveedor activo al alta y que la orden elegida pertenezca al proveedor de la factura. Los bloqueos de lectura conservan esas condiciones hasta confirmar el alta. La unicidad se mantiene al anular el documento.

El pago requiere deuda existente y vigente, importe positivo y fecha efectiva desde la fecha documentaria hasta hoy en Lima para evitar anticipos. PostgreSQL bloquea la fila de la cuenta con `FOR UPDATE` antes de consultar estado y saldo. Los pagos, las reversiones y la anulación conservan el bloqueo hasta confirmar o revertir. La reversión registra marca y motivo en el pago original una sola vez. La anulación lógica conserva documentos y pagos históricos.

El resumen usa saldos pendientes positivos de cuentas vigentes y los agrupa por proveedor y moneda. El día de vencimiento se considera al día. Los tramos y el filtro de vencidas aplican a vencimientos anteriores al corte de Lima, conforme al [módulo 53](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/blob/dbd4155fae4be59ab39ab9f3eb2a8bd17cd38534/docs/specs/modulo53-cuentas-por-cobrar-backend.md).

Los textos de negocio de la tabla son **propuestas concretas para la revisión final**. El UseCase responde con `ReglaNegocioException` y `CodigosEstado`. El cuerpo conserva `{ StatusCode, Message }`. Las carreras de unicidad y FK se traducen mediante `ViolacionRestriccionException` del `UnitOfWork`.

| HTTP | Condición | Literal propuesto |
|---|---|---|
| 400 | Id, moneda, fecha, escala o filtros inválidos | «Los datos de la cuenta por pagar no son válidos.» |
| 400 | Serie o número vacío o fuera de longitud | «La serie y el número de la factura del proveedor son obligatorios y deben respetar su longitud.» |
| 400 | Vencimiento ausente | «La fecha de vencimiento es obligatoria.» |
| 400 | Motivo fuera de 3–300 | «El motivo debe tener entre 3 y 300 caracteres.» |
| 404 | Proveedor inexistente | «El proveedor no existe.» |
| 404 | Orden inexistente | «La orden de abastecimiento no existe.» |
| 404 | Cuenta inexistente | «La cuenta por pagar no existe.» |
| 404 | Pago ajeno o inexistente | «El pago indicado no existe en la cuenta.» |
| 409 | Proveedor inactivo al alta | «El proveedor debe estar activo para registrar la factura.» |
| 409 | Orden de otro proveedor | «La orden de abastecimiento no pertenece al proveedor de la factura.» |
| 409 | Clave de factura duplicada | «La factura del proveedor ya está registrada.» |
| 409 | Pago sobre el saldo | «El pago supera el saldo pendiente.» |
| 409 | Reversión repetida | «El pago ya fue revertido.» |
| 409 | Anulación con pagos | «No se puede anular una cuenta por pagar con pagos.» |
| 409 | Cuenta anulada | «La cuenta por pagar está anulada.» |
| 409 | Referencia perdida al guardar | «No se pudo guardar la operación porque una referencia ya no existe.» |

### Precisiones para la revisión final

- **Fechas pactadas.** El vencimiento es obligatorio, puede ser futuro y se trata como fecha pactada independiente de la fecha documentaria. Lukas debe confirmar la relación entre esas fechas y el tratamiento del vencimiento futuro. El 54 usa la fecha documentaria como límite inferior del pago al aplicar la prohibición de anticipos.
- **Historial.** La implementación impide anular mientras exista un pago vigente y permite anular si todos los pagos se revirtieron, conservando su historial. Lukas debe confirmar la interpretación.

### Seed, pruebas y smoke

**Seed.** Registrar por clave proveedor/serie/número documentos pendientes, parciales, pagados y anulados, con pagos vigentes y revertidos en PEN y USD. La carga conserva los identificadores de las órdenes de ejemplo para mantener las FKs.

**Pruebas de integración.**

1. Alta con proveedor activo, orden opcional, orden ajena, proveedor inactivo, vencimiento obligatorio y coincidencia de números normalizados.
2. Base, impuesto y total con dos decimales, incluido impuesto cero. Probar pago parcial y exacto, sobrepago, reversión y anulación con y sin pagos vigentes.
3. Aging al día y límites de tramos como 53. Mantener reportes separados por moneda y conservar las órdenes del 15 y el banco sin escrituras.
4. Ejercitar cada literal, permisos, consultas y paginación.

**Smoke PostgreSQL.** Verificar alta con y sin FK de orden, duplicidad normalizada, anticipos, pagos, sobrepago, reversión y anulación con `request_check`, validación Python y OpenAPI. `smoke-cuentas-por-pagar.py` comprueba las restricciones SQL, los límites de vencimiento, las carreras de pago y anulación, el alta duplicada y el rollback tras un fallo de FK. Ejecutar en una base desechable.

## 5. Criterios de aceptación (medibles)

| # | Criterio | Verificación | Umbral |
|---|---|---|---|
| A1 | Arquitectura y compilación | `dotnet build -warnaserror`, `scripts/check-rules.sh` y `./scripts/verify.sh` | Gate en verde |
| A2 | API y autorización | Pruebas de cada literal, rutas, JWT y roles G1 | Lecturas autenticadas. Escrituras FINANZAS/ADMIN. Cada rechazo conserva los datos. |
| A3 | Registro documentario | Proveedor activo, orden opcional, unicidad y cálculo | Una deuda por factura y total exacto |
| A4 | Pagos y anulación | Pago parcial, sobrepago, reversión, anulación y carrera | Ningún saldo negativo y sin pérdida del historial |
| A5 | Antigüedad | Vencimiento, tramos y monedas | Corte por día y agrupación coherente con el 53 |
| A6 | Database First | Aplicar SQL local, repetir seed, comprobar scaffold y ejecutar smoke | SCAFFOLD AL DÍA y SMOKE OK |

## 6. Checklist de ejecución

- [x] Incorporar las decisiones oficiales de §2 y G1 a G9.
- [ ] Terminar la revisión del PR de documentación y cerrar las precisiones de §4.
- [x] Verificar las dependencias disponibles, aplicar el SQL 047 en PostgreSQL desechable y generar entidades y DbContext.
- [x] Implementar API, pruebas, seed y smoke. Registrar los resultados de §5.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-23 | Decisiones v0 | Aprobadas. Revisión final del PR pendiente. | [Respuesta de Lukas](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52#issuecomment-7109010161) |
| 2026-11-24 | A1–A5 | Compilación sin advertencias ni errores. Reglas y gate correctos. | `dotnet build -warnaserror`, `scripts/check-rules.sh` y `scripts/verify.sh`. Pasaron 57 pruebas del 54 dentro de 1.669 pruebas de backend y 1.314 del frontend. |
| 2026-11-24 | A6 | `SCAFFOLD AL DÍA` y `SMOKE OK`. | PostgreSQL 16 desechable. Repetir el seed conservó cinco facturas, cuatro pagos, sus saldos y la referencia a la orden. Las cuatro carreras y el rollback por FK pasaron. |

## Actualización del 2026-11-26

El despliegue requiere el catálogo `FINANZAS` (Id 7) que crea el SQL 024 del PR #83. Las pruebas de la rama pueden usar ADMIN y tokens de prueba de FINANZAS. La dependencia de despliegue sigue el orden indicado por Lukas.

El mismo corte de Perú se aplica a fecha documentaria, pagos y reportes. El día cambia a las 05:00 UTC. Los vencimientos futuros válidos y los instantes UTC de auditoría conservan su tratamiento.

Árbol sobre main 1835648. `git diff --check`, `dotnet build -warnaserror` (0 advertencias) y `check-rules.sh` correctos. `verify.sh` pasó 1818 pruebas backend, 59 del módulo, y 1455 frontend. PostgreSQL 16 desechable confirmó reconstrucción de 19 scripts, scaffold al día, dos cargas idempotentes del seed, concurrencia, rollback, límites de vencimiento y `SMOKE OK`.
