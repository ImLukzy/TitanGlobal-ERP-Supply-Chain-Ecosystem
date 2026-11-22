# Especificación: modulo52-notas-credito-debito-backend — Notas de crédito y débito

> Estado: **propuesta pendiente de revisión y aprobación por Lukas**. Responsable: Leonel Portillo. Las decisiones de §2, el contrato, los literales y los criterios de aceptación se presentan para revisión.

## 1. Objetivo

**Problema.** El ERP carece de un documento de ajuste que se pueda consultar junto con el comprobante original.

**Resultado esperado.** Registrar y consultar notas de crédito y débito vinculadas a comprobantes del módulo 51, conservando los importes del documento original. El efecto de una nota depende de su tipo, motivo y estado conforme a las decisiones aprobadas.

### Base comprobada y dependencias

- **51 Facturación electrónica.** `database/007-modulo51-facturacion-electronica.sql` crea `Facturas` y `DetallesFactura`. Facturas y boletas guardan cliente, moneda, importes y porcentaje de impuesto por línea. Sus estados son `Borrador`, `Emitida` y `Anulada`. Al editar un borrador, el 51 reemplaza sus líneas. La emisión es interna y el 51 permite anular el comprobante emitido. La evidencia de la dependencia temporal está en [§7](#7-registro-de-verificación).
- **75 Impuestos y tasas.** `Impuesto` y `TasaImpuesto` existen en `main`. El cálculo del 75 selecciona una tasa activa para la fecha de operación. Su uso en una nota se decide en 52.5.
- **53 Cuentas por cobrar.** La [propuesta del 53](modulo53-cuentas-por-cobrar-backend.md) prevé consumir el identificador, el comprobante, el tipo, el estado y el total de una nota elegible.
- **Reglas transversales.** El desarrollo seguirá el flujo Controller → UseCase → IUnitOfWork → Repository → DbContext → PostgreSQL y el esquema Database First de la [spec técnica de scaffold](52-database-first-scaffold.md). La [guía de Finanzas](../equipo/revision-finanzas-52-60.md#reglas-del-repositorio) recoge los contratos compartidos.

## 2. Fuera de alcance

- Frontend, cambios funcionales al 51, cambios a las cuatro entidades base, al middleware y al pipeline.
- Escrituras de cartera desde el 52 e integraciones con devoluciones, stock, contabilidad, series de otros módulos o envío tributario. Cualquier ampliación requiere acordar el alcance y sus dependencias.

**Decisiones de producto pendientes de aprobación**

| Nº | Tema | Propuesta para revisión | Qué debe confirmar Lukas |
|---|---|---|---|
| 52.1 | Tipos y comprobante de origen | Tipos internos `Credito` y `Debito` con un solo comprobante por nota. | Tipos de nota, factura o boleta admitidas, estados elegibles, cuándo se verifica el estado y cómo coordinar la anulación del comprobante con el 51 si existen notas. |
| 52.2 | Estados y transiciones de la nota | Si se requieren borradores, editar en `Borrador`, pasar a `Emitida` al emitir y conservar el historial al pasar a `Anulada`. | Alta directa o borrador, transiciones, fechas, motivo y efectos de anular una nota. |
| 52.3 | Motivos y permisos | Motivo obligatorio. Validar compatibilidad con el tipo y la modalidad de ajuste si se aprueba un catálogo. Usar JWT. | Catálogo o texto libre, códigos, obligatoriedad y longitudes por campo, y roles por operación. |
| 52.4 | Numeración | Una identificación propia por nota. Candidato a índice único `(Tipo, Serie, Numero)` para la numeración interna. | Ámbito de unicidad, series admitidas, captura manual o correlativo automático, momento de asignación y reintentos. |
| 52.5 | Líneas y cálculo | Usar magnitudes y distinguir el efecto por tipo. Evaluar el porcentaje histórico de la línea facturada. | Modalidad global o por línea por motivo, componentes de entrada y calculados, reglas tributarias aplicables, base, exenciones, fuente de tasa, fórmula, escala y orden de redondeo. |
| 52.6 | Topes y aplicación en cartera | Evaluar el límite de emisión sobre el comprobante. El 53 registra la aplicación explícita en cartera. | Qué estados computan, si hay topes por monto o cantidad, efecto de débitos y anulaciones, qué nota es elegible para cartera y cómo se coordina una anulación después de aplicarla. Confirmar la precedencia de validaciones y errores. |

## 3. Archivos afectados

| Archivo | Acción | Nota |
|---|---|---|
| `database/NNN-modulo52-notas-credito-debito.sql` y `docs/equipo/relevo.md` | crear / actualizar tras aprobación | Confirmar y registrar el número definitivo según [Relevo §8](../equipo/relevo.md#8-números-de-los-scripts-de-base-de-datos). |
| `src/TitanGlobal.Domain/Models/NotaCreditoDebito.cs` y, si se aprueban líneas, `DetalleNotaCreditoDebito.cs` | generar | Clases `partial` generadas desde PostgreSQL. |
| `src/TitanGlobal.Domain/Models/Factura.cs`, `DetalleFactura.cs` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar según el esquema | Revisar el diff. En las entidades del 51 se esperan únicamente navegaciones inversas de las FKs nuevas que correspondan. |
| `src/TitanGlobal.Application/DTOs/Modulo52_NotasCreditoDebito/` y `src/TitanGlobal.Application/UseCases/Modulo52_NotasCreditoDebito/` | crear | DTOs, casos de uso concretos y validaciones del 52. |
| `src/TitanGlobal.Api/Controllers/Modulo52_NotasCreditoDebito/NotasCreditoDebitoController.cs` | crear | Controlador delgado con JWT. |
| `tests/TitanGlobal.Api.Tests/Modulo52_NotasCreditoDebito/NotasCreditoDebitoApiTests.cs` | crear | Integración HTTP con fixtures de facturas y notas. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | modificar | Datos idempotentes antes del `COMMIT;` y verificaciones HTTP contra PostgreSQL. |

## 4. Diseño y lógica

### Datos y persistencia

- `NotasCreditoDebito` tendría `Id` con identidad, `FacturaId` obligatorio e indexado con FK `integer` a `Facturas.Id`, tipo, motivo e importes aprobados. Cliente y moneda se obtienen del comprobante. Los campos de estado, fechas de creación, emisión y anulación, y serie y número dependen de 52.2 y 52.4.
- Si 52.5 incluye ajustes por línea, `DetallesNotaCreditoDebito` tendría `Id` con identidad, `NotaCreditoDebitoId` indexado con FK a la cabecera y `DetalleFacturaId` indexado con FK `integer` a `DetallesFactura.Id` para una línea de origen. Application comprueba que su `FacturaId` coincida con el de la nota. Una garantía adicional en SQL o una referencia a líneas que el 51 todavía puede reemplazar requieren acordar su impacto en el 51.
- Se propone `ON DELETE RESTRICT` para conservar las referencias de origen. Normalizar fechas `timestamptz` con `FechasUtc`. Evaluar `numeric(18,2)` para importes, `numeric(18,4)` para cantidades y `numeric(18,6)` para tasas. Las longitudes, los máximos y los `CHECK` siguen el contrato aprobado.
- Usar el repositorio genérico mediante `IUnitOfWork` y guardar cabecera y líneas en una sola operación transaccional.

### API y DTOs candidatos

Ruta base `/api/v1/notas-credito-debito`. JSON camelCase y JWT. Proyectar las lecturas a DTOs con `AsNoTracking` y materializar de forma asíncrona.

| Método y ruta relativa | Caso de uso | Entrada y respuesta |
|---|---|---|
| `GET /` | `ListarNotasCreditoDebitoUseCase` | `facturaId?`, `pagina=1`, `tamanoPagina=20`. `200 PaginaNotasCreditoDebitoDto` con `items`, `pagina`, `tamanoPagina`, `totalRegistros` y `totalPaginas`. |
| `GET /{id:int}` | `ObtenerNotaCreditoDebitoUseCase` | `200 NotaCreditoDebitoDetalleDto`. |
| `POST /` | `CrearNotaCreditoDebitoUseCase` | `GuardarNotaCreditoDebitoRequestDto`. `201` con detalle y `Location`. |
| `PUT /{id:int}` | `ActualizarNotaCreditoDebitoUseCase` | Según 52.2, `GuardarNotaCreditoDebitoRequestDto` y `200` con detalle. |
| `POST /{id:int}/emitir` | `EmitirNotaCreditoDebitoUseCase` | Según 52.2, sin cuerpo con `[PermitirCuerpoVacio]` y `200` con detalle. |
| `POST /{id:int}/anular` | `AnularNotaCreditoDebitoUseCase` | Según 52.2, `AnularNotaCreditoDebitoRequestDto { motivo }` y `200` con detalle. |

- `GuardarNotaCreditoDebitoRequestDto` incluye `facturaId`, `tipo`, `motivo` y los datos del ajuste aprobados. Fecha, serie y número se reciben solo si el contrato los asigna al solicitante. Cliente, moneda e importes calculados son datos de salida.
- `NotaCreditoDebitoListadoDto` incluye `id`, `facturaId`, `tipo`, `moneda`, `total` y los campos aprobados de identificación, estado y fecha. El DTO de detalle añade la identificación del comprobante de origen, motivo, desglose y datos del ciclo de vida aprobados.
- `GuardarLineaNotaCreditoDebitoRequestDto` y `LineaNotaCreditoDebitoDto` se añaden si se aprueban líneas. La entrada contendría `detalleFacturaId` cuando aplique y la cantidad o importe a ajustar, según la modalidad. La salida incluye los importes calculados.
- Para el listado se propone ordenar por fecha descendente e `id` descendente, con página positiva, tamaño de 1 a 100 y validación del desplazamiento antes de consultar. Si se aprueba un listado sin fecha propia, ordenar por `id` descendente.

### Validaciones y mensajes candidatos

Validar primero la forma de la solicitud, luego la existencia y pertenencia de referencias, después el estado y los límites del ajuste. La spec final debe fijar la precedencia exacta, los límites y todos los literales. Usar `ReglaNegocioException` con `CodigosEstado` y conservar el formato `{ StatusCode, Message }`.

| Condición | HTTP | Mensaje literal propuesto |
|---|---|---|
| Identificador de factura no positivo | 400 | «Debe indicar una factura válida.» |
| Tipo ausente o fuera del catálogo candidato | 400 | «El tipo de nota debe ser Credito o Debito.» |
| Motivo vacío | 400 | «El motivo de la nota es obligatorio.» |
| Motivo por encima de la longitud acordada | 400 | «El motivo de la nota supera la longitud permitida.» |
| Tamaño o página inválidos | 400 | «La paginación solicitada no es válida.» |
| Importe fuera de rango o escala | 400 | «El importe del ajuste no es válido.» |
| Serie o número inválidos, si se reciben en la petición | 400 | «La serie o el número de la nota no son válidos.» |
| Motivo de anulación vacío, si se aprueba la anulación con motivo | 400 | «El motivo de anulación es obligatorio.» |
| Línea de otra factura, si se admiten líneas | 400 | «El detalle indicado no pertenece a la factura de la nota.» |
| Factura inexistente | 404 | «La factura de origen no existe.» |
| Línea de origen inexistente, si se admiten líneas | 404 | «El detalle de factura indicado no existe.» |
| Nota inexistente | 404 | «La nota de crédito o débito no existe.» |
| Factura inelegible por estado | 409 | «El estado de la factura no permite registrar la nota.» |
| Transición de nota no permitida | 409 | «El estado de la nota no permite esta operación.» |
| Numeración duplicada en el ámbito aprobado | 409 | «Ya existe una nota con la misma serie y número.» |
| Referencia eliminada durante la operación | 409 | «No se pudo guardar la nota porque una referencia ya no existe.» |
| Ajuste superior al límite, si se aprueba | 409 | «El ajuste supera el importe disponible de la factura.» |

### Concurrencia

- Traducir unicidad y FK mediante `ViolacionRestriccionException`, filtrando por las restricciones del módulo. El `UnitOfWork` traduce `23505` y `23503`. Validar en Application las condiciones de los `CHECK` para devolver el literal acordado.
- Si se aprueba un tope acumulado, un correlativo automático o un ciclo editable, proteger la comprobación y la escritura con una garantía atómica en persistencia dentro del flujo por `IUnitOfWork`. Si la garantía abarca anulaciones concurrentes del comprobante, acordarla con el 51. Si el 52 debe consultar el saldo cobrable o condicionar una anulación a aplicaciones existentes, aprobar una transacción coordinada con el 53.

## 5. Criterios de aceptación (medibles)

| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Base y contrato aprobados | Revisar el contrato del 51 disponible para pruebas. Confirmar en `origin/main` el contrato del 51 antes de integrar el 52. | Dependencia compatible, SQL libre y decisiones de §2 aprobadas |
| A2 | Gate | `dotnet build -warnaserror`, `scripts/check-rules.sh` y `scripts/verify.sh` | 0 warnings, gate en verde |
| A3 | HTTP y reglas | Probar alta y consulta para cada tipo con facturas y, si se admiten, boletas. Cubrir filtro por factura, orden, paginación, JWT, permisos, referencias inexistentes, línea de otra factura, campos obligatorios, longitudes y fechas UTC. | Se respetan los literales y su precedencia. Tras un rechazo, la cantidad de notas y líneas y el total de la factura permanecen iguales. |
| A4 | Ciclo y cálculos | Probar éxito, repetición y transición inválida. Verificar moneda del origen, importe global o por línea, base, tasa histórica o vigente, impuesto, redondeo en punto medio, frontera de escala y topes según alcance. Preparar notas elegibles e inelegibles para las pruebas del 53. | Estados e importes coinciden con los ejemplos aprobados. |
| A5 | PostgreSQL | En una base local desechable, aplicar SQL y comprobar scaffold y seed idempotente. Resolver el comprobante del seed por `(TipoComprobante, Serie, Numero)`, clave única del 51. Probar FKs y carreras de numeración, topes o transiciones con dos contextos cuando apliquen. Añadir `request_check` y validación Python para alta, consulta y errores representativos. | `SCAFFOLD AL DÍA` y `SMOKE OK`. Cada carrera deja solo los efectos permitidos. |
| A6 | API documentada | Verificar las operaciones aprobadas en `/openapi/v1.json` y Swagger. | Todas presentes |

## 6. Checklist de ejecución

- [ ] Resolver las decisiones de §2 y verificar la dependencia según A1.
- [ ] Aplicar el esquema aprobado y generar los modelos. Implementar los casos de uso y la API.
- [ ] Ejecutar y registrar A2–A6.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-22 | A1, dependencia temporal | Parcial | Con `origin/main` en `670c37f` y el PR #48 fuera de `main`, `leonel/modulo52-notas-credito-debito` incorporó el 51 por avance rápido hasta `fb84529`. La aprobación funcional sigue pendiente. |
| 2026-11-22 | Gate de la base con el 51 | Pasa | En `fb84529`, `dotnet build -warnaserror` con 0 warnings, `scripts/check-rules.sh` y `scripts/verify.sh` con salida 0. Pasaron 1083 pruebas de backend y 543 de frontend. npm avisó de requisitos de Node y Vite del tamaño de un chunk. |
| 2026-11-22 | PostgreSQL de la base con el 51 | Pasa | Docker Desktop iniciado. El puerto 5433 ya pertenecía a otra base local. El smoke usó un proyecto Compose temporal con PostgreSQL 16 en memoria y puertos de loopback 15433 y 15080. Aplicó `001`, `007` y `008`, obtuvo `SCAFFOLD AL DÍA` y terminó en `SMOKE OK`. |
| 2026-11-22 | API de la base con el 51 | Pasa | OpenAPI y Swagger publican las rutas de facturas. El listado devuelve 401 sin JWT y 200 con JWT. |
| 2026-11-22 | A2–A6 del módulo 52 | Pendiente | Las verificaciones anteriores corresponden a la base de la rama. SQL, entidades, API y pruebas del 52 quedan condicionados a la aprobación funcional. |
| 2026-11-22 | Base documental | Revisada | `e48a190` incorpora la tabla oficial de reservas del PR #50 y todavía carece del PR #48. La asignación para el 52 se rige por §3. |
