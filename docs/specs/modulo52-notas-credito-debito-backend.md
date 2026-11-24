# Especificación: modulo52-notas-credito-debito-backend — Notas de crédito y débito

> Estado: **aprobada** por Lukas el 2026-11-23 para la v0. Responsable: Leonel Portillo. La revisión final del [PR #52](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52) sigue pendiente.

## 1. Objetivo

Registrar notas internas de crédito y débito sobre las líneas de una factura o boleta emitida. El documento conserva la moneda y la tasa aplicadas por el comprobante original. Crear, editar, emitir y anular una nota deja intactos los importes de la factura.

### Base comprobada y dependencias

- **Disponible para revisión.** Facturación del 51 en `fb84529` y [PR #48](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/48). Su contrato contiene `Facturas`, `DetallesFactura`, `TipoComprobante`, `Estado`, `Moneda`, `Cantidad`, `PrecioUnitario`, `PorcentajeImpuesto` y los importes calculados. El 51 todavía debe integrarse antes de fusionar el backend definitivo del 52.
- **Disponible en `main`.** Impuestos y tasas del 75. El cálculo de la nota usa el porcentaje ya guardado por el 51 y prescinde de las tasas vigentes del 75.
- **Posterior.** El [53](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/blob/dbd4155fae4be59ab39ab9f3eb2a8bd17cd38534/docs/specs/modulo53-cuentas-por-cobrar-backend.md) administrará la aplicación explícita de créditos. Su integración incorpora la guarda de anulación de la nota aplicada cuando exista la tabla de aplicaciones del 53.

## 2. Fuera de alcance

- Frontend, envío tributario, cambios de moneda, sucursales, devoluciones, stock, contabilidad y ajustes globales sin detalle.
- Aplicación automática en cartera y tablas o FKs anticipadas del 53. El cambio funcional en el 51 se concentra en la guarda de anulación por notas emitidas.

**Decisiones de producto aprobadas para la v0.** [Respuesta de Lukas](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52#issuecomment-7109010161)

| Nº | Tema | Respuesta aprobada |
|---|---|---|
| 52.1 | Comprobante de origen | Una nota `Credito` o `Debito` se vincula con una factura o boleta `Emitida`. Comprobar el estado al crear y al emitir. En el PR del 52, proteger la anulación del 51 con HTTP 409 cuando el comprobante tenga notas emitidas. |
| 52.2 | Ciclo | `Borrador → Emitida → Anulada`. Editar únicamente borradores. La emisión asigna la fecha del día. La anulación lógica exige motivo y devuelve 409 para una nota aplicada en el 53. |
| 52.3 | Motivos y acceso | Motivo libre obligatorio, entre 3 y 300 caracteres. G1 reserva la escritura a `FINANZAS` y `ADMIN`, y permite leer a todo usuario autenticado. |
| 52.4 | Correlativos | El servidor asigna en el alta un número único `NC-000001` o `ND-000001`. Cada tipo lleva su propio correlativo. |
| 52.5 | Detalle y cálculo | Cada línea refiere un `DetalleFacturaId`. Usar la tasa guardada en la línea del 51. La base surge de cantidad por precio unitario, el impuesto del porcentaje sobre la base con redondeo y el total de ambos importes. |
| 52.6 | Topes y orden de validación | Los créditos en `Emitida` se acumulan por línea de factura y se limitan a su cantidad e importe facturados. Los borradores no consumen cupo. El débito carece de ese tope. Cartera exige una nota `Emitida`. Validar forma, existencia, estado y por último el tope. |

Las [reglas comunes G1 a G9](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/blob/dbd4155fae4be59ab39ab9f3eb2a8bd17cd38534/docs/equipo/revision-finanzas-52-60.md#reglas-generales-g1-a-g9) se aplican junto con las precisiones de §4.

## 3. Archivos afectados

| Archivo previsto | Acción futura | Alcance |
|---|---|---|
| `database/024-modulo52-notas-credito-debito.sql` y `docs/equipo/relevo.md` | Crear / registrar | Reserva `024` para el PR del 52. Añadir su fila en esa rama. |
| `src/TitanGlobal.Domain/Models/` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | Generar | Entidades y navegaciones de §4 mediante scaffold. Revisar los nombres que produce el pluralizador. |
| `src/TitanGlobal.Application/DTOs/Modulo52_NotasCreditoDebito/` y `src/TitanGlobal.Application/UseCases/Modulo52_NotasCreditoDebito/` | Crear | Validación, proyecciones y casos de uso concretos en Application. |
| `src/TitanGlobal.Api/Controllers/Modulo52_NotasCreditoDebito/NotasCreditoDebitoController.cs` | Crear | Rutas y autorización de §4. |
| `AnularFacturaUseCase.cs` y pruebas del 51 | Ampliar en el PR del 52 | HTTP 409 cuando existe una nota `Emitida`. Compartir la garantía transaccional de la factura con la emisión de notas. |
| `tests/TitanGlobal.Api.Tests/Modulo52_NotasCreditoDebito/NotasCreditoDebitoApiTests.cs` | Crear | Pruebas HTTP de §4. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | Ampliar | Fixtures idempotentes, rol `FINANZAS` (Id 7) y checks HTTP/SQL del 52. |

## 4. Diseño y lógica

Los nombres, tamaños auxiliares, rutas y mecanismos de persistencia concretan el diseño para la revisión final. Las decisiones de producto aprobadas están en §2 y G1 a G9.

### Convenciones y permisos

Controller → UseCase → IUnitOfWork → Repository → DbContext → PostgreSQL. DTOs en camelCase. Las lecturas requieren JWT. Las escrituras requieren `FINANZAS` (Id 7, seed del 52) o `ADMIN` mediante `[Authorize(Roles = "FINANZAS,ADMIN")]`. Los 401, 403 y el 400 de cuerpo vacío conservan los literales del pipeline. Las acciones sin cuerpo usan `[PermitirCuerpoVacio]`. Las fechas de negocio usan `DateOnly` y `date` con JSON `yyyy-MM-dd`. Los instantes del servidor usan `timestamptz` y `FechasUtc`.

La v0 usa cantidades `numeric(18,4)` e importes `numeric(18,2)`. El servidor copia la moneda `PEN` o `USD` de la factura al alta. `FechaEmision` es una fecha de negocio asignada al emitir y coincide con el día de la operación. Los motivos se validan tras `Trim()`. Esta spec usa corte de Lima como convención técnica propuesta para el día de negocio.

### Datos y persistencia

| Tabla → entidad | Campos y restricciones |
|---|---|
| `NotasCreditoDebito` → `NotaCreditoDebito` | `Id`, `FacturaId` FK obligatoria a `Facturas`, `Tipo varchar(20)`, `Numero varchar(30)` único, `Moneda char(3)`, `Motivo varchar(300)`, `Estado varchar(20)`, `BaseImponible`, `ImporteImpuesto`, `Total`, `FechaEmision date?`, `FechaCreacion timestamptz`, `FechaAnulacion timestamptz?` y `MotivoAnulacion varchar(300)?`. Índice `(FacturaId, Estado)`. Chequear tipo, moneda y coherencia de estado/fechas/motivo. |
| `DetallesNotaCreditoDebito` → `DetalleNotaCreditoDebito` | `Id`, `NotaCreditoDebitoId` FK, `DetalleFacturaId` FK a `DetallesFactura`, `Cantidad numeric(18,4)`, `PrecioUnitario`, `PorcentajeImpuesto` con la escala de la línea de origen, `BaseImponible`, `ImporteImpuesto` y `Total`. Índice de línea de origen para el acumulado. Un detalle por línea de origen en cada nota mediante unicidad `(NotaCreditoDebitoId, DetalleFacturaId)`. |
| Numeración del 52 | Dos secuencias PostgreSQL, una para `Credito` y otra para `Debito`. La inserción con número temporal `TMP-` y la asignación del número definitivo ocurren en la misma transacción. El servidor usa al menos seis dígitos. El índice de `Numero` protege la unicidad y la API solo devuelve el número definitivo. |

Las PK son identidades `integer`. Las FKs se indexan y conservan referencias con `ON DELETE RESTRICT`. Application valida los límites de columna antes de guardar.

`R(valor)` redondea a 2 decimales con `MidpointRounding.AwayFromZero`. El diseño usa `BaseImponible = R(Cantidad × PrecioUnitario)`, `ImporteImpuesto = R(BaseImponible × PorcentajeImpuesto / 100)` y `TotalLinea = BaseImponible + ImporteImpuesto`. Sumar los importes ya redondeados de las líneas para los totales de cabecera. La tasa 18 % sobre una base de 10,05 da impuesto 1,81 y total 11,86. Para un producto cantidad × precio de 10,005, la base queda 10,01.

El SQL valida dominios, escalas de almacenamiento, positividad y estado. El check de importes excluye `NaN`. Application valida la escala de entrada antes de guardar y comprueba que cada `DetalleFacturaId` pertenece al comprobante de la nota.

### Casos de uso, rutas y DTOs

Ruta base `/api/v1/notas-credito-debito`. Las rutas de consulta proyectan con `AsNoTracking`. El listado filtra por `facturaId?`, `tipo?` y `estado?`, ordena por `FechaCreacion` e `Id` descendentes y usa página 1, tamaño 20 y máximo 100.

| Método y ruta relativa | Caso de uso | Entrada → éxito |
|---|---|---|
| `GET /` | `ListarNotasCreditoDebitoUseCase` | Filtros y paginación → `200 PaginaNotasCreditoDebitoDto`. |
| `GET /{id:int}` | `ObtenerNotaCreditoDebitoUseCase` | `200 NotaCreditoDebitoDetalleDto`. |
| `POST /` | `CrearNotaCreditoDebitoUseCase` | `CrearNotaCreditoDebitoRequestDto` → `201` detalle y `Location`. |
| `PUT /{id:int}` | `ActualizarNotaCreditoDebitoUseCase` | `ActualizarNotaCreditoDebitoRequestDto` → `200` detalle. |
| `POST /{id:int}/emitir` | `EmitirNotaCreditoDebitoUseCase` | Sin cuerpo → `200` detalle. |
| `POST /{id:int}/anular` | `AnularNotaCreditoDebitoUseCase` | `AnularNotaCreditoDebitoRequestDto { motivo }` → `200` detalle. |

- `CrearNotaCreditoDebitoRequestDto { facturaId, tipo, motivo, detalles }` y `ActualizarNotaCreditoDebitoRequestDto { motivo, detalles }`. `DetalleNotaCreditoDebitoRequestDto { detalleFacturaId, cantidad, precioUnitario }`. La fuente del precio y los campos editables se precisan en la revisión final.
- `NotaCreditoDebitoListadoDto { id, facturaId, numero, tipo, estado, moneda, total, fechaCreacion, fechaEmision? }`. El detalle añade comprobante y cliente de origen, `motivo`, bases, impuestos, anulación y `detalles { id, detalleFacturaId, cantidad, precioUnitario, porcentajeImpuesto, baseImponible, importeImpuesto, total }`.
- `PaginaNotasCreditoDebitoDto { items, pagina, tamanoPagina, totalRegistros, totalPaginas }`. El número, la moneda, la tasa, los totales, el estado y las fechas del ciclo se generan en el servidor.

### Validaciones y mensajes literales

1. **Forma.** Validar ids positivos, tipo, motivos, detalle no vacío, ids de línea sin duplicados, cantidad positiva con hasta cuatro decimales, precio positivo con hasta dos y paginación. Validar también que el cálculo y la suma quepan en `numeric(18,2)`.
2. **Existencia.** Cargar la nota para consulta o transición, el comprobante y sus detalles. Cada línea pertenece a la factura de la cabecera.
3. **Estado.** Alta en `Borrador` y emisión con comprobante `Emitida`. Edición únicamente para una nota `Borrador`. Anulación únicamente desde `Emitida`, con motivo. La guarda por aplicación al 53 entra cuando el 53 exponga sus aplicaciones.
4. **Tope.** Al emitir un crédito, sumar por `DetalleFacturaId` la cantidad y el total de las notas en `Emitida` y las líneas de esta nota. Rechazar si la cantidad o el importe superan `DetalleFactura.Cantidad` o `DetalleFactura.Total`, respectivamente. Un crédito anulado deja de contar. Para débito se conservan las validaciones de forma, origen y estado.
5. **Concurrencia.** La emisión, el cálculo acumulado y el cambio de estado se confirman en una transacción con bloqueo por factura. La anulación del 51 adquiere el mismo bloqueo, comprueba la condición de notas `Emitida` y conserva su código HTTP 409. La aplicación del 53 y la anulación de la nota deberán compartir esa garantía cuando exista ese módulo.

Los textos de negocio de la tabla son **propuestas concretas para la revisión final**. El UseCase responde con `ReglaNegocioException` y `CodigosEstado`. El cuerpo conserva `{ StatusCode, Message }`. Las carreras de unicidad y FK se traducen mediante `ViolacionRestriccionException` del `UnitOfWork`.

| HTTP | Condición | Literal propuesto |
|---|---|---|
| 400 | Factura no positiva | «Debe indicar una factura válida.» |
| 400 | Tipo ajeno al catálogo | «El tipo de nota debe ser Credito o Debito.» |
| 400 | Motivo de nota o anulación fuera de 3–300 | «El motivo debe tener entre 3 y 300 caracteres.» |
| 400 | Detalle vacío, repetido o con id inválido | «Indique al menos una línea válida y sin duplicados.» |
| 400 | Cantidad, precio o importes fuera de escala o rango | «La cantidad o el importe de la nota no es válido.» |
| 400 | Paginación o filtros inválidos | «Los filtros o la paginación de notas no son válidos.» |
| 400 | Línea de otro comprobante | «El detalle indicado no pertenece a la factura de la nota.» |
| 404 | Factura inexistente | «La factura de origen no existe.» |
| 404 | Línea inexistente | «El detalle de factura indicado no existe.» |
| 404 | Nota inexistente | «La nota de crédito o débito no existe.» |
| 409 | Factura inelegible al crear o emitir | «El estado de la factura no permite registrar la nota.» |
| 409 | Transición inválida | «El estado de la nota no permite esta operación.» |
| 409 | Cantidad de crédito acumulada superior a la facturada | «El crédito supera la cantidad disponible de la línea de factura.» |
| 409 | Importe de crédito acumulado superior al facturado | «El crédito supera el importe disponible de la línea de factura.» |
| 409 | Anulación del comprobante con una nota emitida (51) | «No se puede anular un comprobante con notas emitidas.» |
| 409 | Anulación de una nota aplicada (al incorporar 53) | «No se puede anular una nota aplicada a una cuenta por cobrar.» |
| 409 | Colisión definitiva de numeración | «Ya existe una nota con ese número.» |
| 409 | FK eliminada durante el guardado | «No se pudo guardar la nota porque una referencia ya no existe.» |

### Precisiones para la revisión final

- **Precio y descuentos.** La respuesta fija la fórmula y la tasa histórica. Falta precisar si `precioUnitario` se informa, se hereda del precio nominal o refleja el descuento de la línea. Un crédito completo a precio nominal puede exceder `DetalleFactura.Total` cuando hubo descuento. El DTO anterior define una entrada de revisión. Confirmar también que el impuesto toma la base ya redondeada.
- **Datos editables.** El contrato de revisión conserva tipo, comprobante y número al editar. Lukas no precisó qué hacer con el folio si se permite cambiar el tipo o el origen de un borrador.
- **Carrera preexistente del 51.** Una edición que ya leyó `Borrador` puede guardarse después de la emisión del comprobante y de una nota. Coordinar el bloqueo de edición y emisión del 51. El caso se mantiene documentado en el [anexo del backend provisional](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/blob/7a2057f4e1d34abdc8a74a2d1605b529b320e063/docs/specs/modulo52-notas-credito-debito-implementacion.md#edición-concurrente-del-comprobante-pendiente-de-coordinar).

### Seed, pruebas y smoke

**Seed.** Añadir en el seed del 52 el rol `FINANZAS` con `Id = 7`, sin reasignar usuarios existentes. Revisar la secuencia de `Roles` y los checks de roles del smoke. Usar claves naturales del 51 para obtener los comprobantes y líneas de prueba. Cargar en forma idempotente una factura PEN y una boleta USD emitidas, una nota por tipo, una línea con tasa de origen y un crédito ya emitido debajo del tope. Alinear las secuencias NC y ND con los números del seed.

**Pruebas de integración.**

1. Alta, consulta, edición y ciclo para ambos tipos y ambos comprobantes. `POST` asigna número y `Location`. Los contadores avanzan de forma independiente. Consultar, filtrar y paginar por JWT. Emisión sin cuerpo fija el día y anulación conserva los detalles.
2. Ejercitar cada literal aplicable y el orden forma → existencia → estado → tope. Probar `CONSULTA` para lectura y 403 al escribir, `FINANZAS` y `ADMIN` para escritura, y 401 sin JWT.
3. Probar redondeo de mitad, múltiples líneas, escalas, desbordamiento y discrepancia con una tasa vigente del 75. El resultado usa la tasa guardada por el 51 y su moneda.
4. Probar créditos justo al límite, excesos independientes de cantidad y de importe, liberación por anulación, borradores sin efecto y débitos por encima del facturado. Probar el 409 del 51 con nota emitida y la anulación válida cuando quedan solo borradores o notas anuladas.
5. La integración posterior del 53 prueba aplicación explícita, 409 de anulación por nota aplicada y carrera aplicación/anulación.

**Smoke PostgreSQL.** Probar el `024`, FKs de cabecera y detalle, rechazo de `NaN`, correlativos concurrentes dentro de cada tipo, dos créditos que juntos exceden la misma línea y la carrera emisión de nota/anulación de factura del 51. Comprobar cantidades, códigos, importes y estados persistidos. Añadir `request_check`, la validación Python de códigos y cuerpos, y la comprobación de OpenAPI. Ejecutar en una base desechable hasta `SMOKE OK`.

## 5. Criterios de aceptación (medibles)

| # | Criterio | Verificación | Umbral |
|---|---|---|---|
| A1 | Arquitectura y compilación | `dotnet build -warnaserror`, `scripts/check-rules.sh` y `./scripts/verify.sh` | Gate en verde |
| A2 | API y autorización | Pruebas de cada literal, rutas, JWT y roles G1 | Lecturas autenticadas. Escrituras FINANZAS/ADMIN. Cada rechazo conserva los datos. |
| A3 | Alta, detalle y número | Factura y boleta emitidas, ambos roles de escritura, dos tipos de nota y fechas de negocio | Dos correlativos independientes, detalle consistente y sin serie |
| A4 | Cálculo y tope | Escala, tasa histórica, redondeo por línea, créditos y débitos con dos contextos | Totales esperados. Los créditos emitidos respetan ambos topes |
| A5 | Protecciones de estado | Pruebas del ciclo, guarda 409 en 51 y aplicación/anulación cuando se integre 53 | La factura conserva importes y ninguna transición deja referencias inconsistentes |
| A6 | Database First | Aplicar SQL local, repetir seed, comprobar scaffold y ejecutar smoke | SCAFFOLD AL DÍA y SMOKE OK |

## 6. Checklist de ejecución

- [x] Incorporar las decisiones oficiales de §2 y G1 a G9.
- [ ] Terminar la revisión del PR de documentación y cerrar las precisiones de §4.
- [ ] Confirmar dependencias, aplicar el SQL en local y generar entidades y DbContext.
- [ ] Implementar API, pruebas, seed y smoke. Registrar los resultados de §5.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-22 | Base temporal del 51 | Revisada | `fb84529`, PR #48 fuera de `main`. Gate previo con 1083 pruebas de backend y 543 de frontend. PostgreSQL desechable aplicó `001`, `007` y `008`, con scaffold al día y `SMOKE OK`. OpenAPI y JWT del 51 verificados. |
| 2026-11-22 | Backend provisional del 52 | Publicado | Commits `e005686` y `7a2057f` en `leonel/modulo52-notas-credito-debito`. Evidencia previa de 1107 pruebas de backend, 543 de frontend, `SCAFFOLD AL DÍA` y `SMOKE OK`. El avance provisional tiene un contrato distinto al aprobado en este documento. |
| 2026-11-23 | Reserva y dependencia | Confirmadas en referencias | `origin/main` en `914a576`, relevo con reserva `024` para el 52. El PR #48 sigue fuera de la base de integración. |
| 2026-11-23 | Decisiones v0 | Aprobadas. Revisión final del PR pendiente. | [Respuesta de Lukas](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52#issuecomment-7109010161) |
| 2026-11-23 | Validación funcional de la spec aprobada | Pendiente | Esta fase revisa documentación. La ejecución de §5 corresponde a la rama de implementación. |
