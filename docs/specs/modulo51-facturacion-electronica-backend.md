# Especificación: modulo51-facturacion-electronica-backend — Facturación electrónica

**Estado.** Contrato del backend presentado para revisión de Lukas en el PR #48.

## 1. Objetivo

Registrar facturas y boletas de clientes, asociarlas opcionalmente a una orden de compra y calcular sus importes con la tasa de IGV vigente en la fecha del comprobante. Permitir la edición de borradores, la emisión, la anulación lógica y la consulta del historial y de los totales por moneda.

## 2. Fuera de alcance

- Envío, firma o validación de comprobantes ante SUNAT u otros servicios externos.
- Generación automática de serie o número, conversión de monedas y aplicación automática de descuentos comerciales.
- Cambios de frontend, seguridad transversal, configuración de impuestos o módulos ajenos. Las navegaciones añadidas al modelo existente se generan con el scaffold.
- Notas de crédito y débito, protección de facturas con notas y cambios de concurrencia que se revisan en el PR #83.

Esta revisión documenta el comportamiento del PR #48 y completa sus verificaciones. Las dependencias del backend son Clientes (01), Órdenes de compra (04), Productos (21) e Impuestos y tasas (75).

## 3. Archivos afectados

| Archivo | Acción | Nota |
|---|---|---|
| [`database/007-modulo51-facturacion-electronica.sql`](../../database/007-modulo51-facturacion-electronica.sql) | Incorporar | Crea `Facturas` y `DetallesFactura` con el número reservado en el [relevo](../equipo/relevo.md). |
| `src/TitanGlobal.Domain/Models/{Factura,DetalleFactura,Cliente,OrdenCompra,Producto,Impuesto}.cs` | Regenerar | Entidades y navegaciones inversas. |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | Regenerar | DbSets, claves, índices y precisión. |
| `src/TitanGlobal.Application/DTOs/Modulo51_FacturacionElectronica/` | Incorporar | Contratos de entrada y salida. |
| `src/TitanGlobal.Application/UseCases/Modulo51_FacturacionElectronica/` | Incorporar | Alta, edición, detalle, listado, resumen, emisión, anulación y cálculo. |
| `src/TitanGlobal.Api/Controllers/Modulo51_FacturacionElectronica/FacturasController.cs` | Incorporar | Siete operaciones HTTP. |
| [`tests/TitanGlobal.Api.Tests/Modulo51_FacturacionElectronica/`](../../tests/TitanGlobal.Api.Tests/Modulo51_FacturacionElectronica/) | Crear | Pruebas de integración HTTP. |
| [`scripts/seed-dev.sql`](../../scripts/seed-dev.sql), [`scripts/smoke-postgres.sh`](../../scripts/smoke-postgres.sh) | Ampliar | Ejemplos y verificaciones en PostgreSQL. |

## 4. Diseño y lógica

### 4.1. Persistencia

La API sigue Controller → UseCase → IUnitOfWork → Repository → DbContext → PostgreSQL. El SQL 007 define el esquema. El scaffold oficial genera las entidades y el contexto desde la base.

| Tabla | Campos y restricciones |
|---|---|
| `Facturas` | `Id` entero identidad y PK. `ClienteId` obligatorio y `OrdenCompraId` opcional. `TipoComprobante` `varchar(20)` con valores `Factura` y `Boleta`. `Serie` `varchar(10)` no vacía y `Numero` entero positivo. Unicidad por `(TipoComprobante, Serie, Numero)`. `FechaEmision`, `FechaCreacion`, `FechaVencimiento?` y `FechaAnulacion?` son `timestamptz`. Si se indica vencimiento, debe ser igual o posterior a la emisión. `Moneda` `varchar(3)` con tres letras A–Z. `Estado` `varchar(20)` admite `Borrador` (por defecto), `Emitida` y `Anulada`. `Subtotal`, `TotalDescuento`, `TotalImpuesto` y `Total` son `numeric(18,2)` no negativos. El descuento no supera el subtotal y `Total = Subtotal - TotalDescuento + TotalImpuesto`. `Observaciones?` y `MotivoAnulacion?` son `varchar(500)`. Solo el estado `Anulada` lleva fecha y motivo de anulación, ambos obligatorios en ese estado. |
| `DetallesFactura` | `Id` entero identidad y PK. `FacturaId` y `ProductoId` obligatorios, `ImpuestoId` opcional. `Descripcion` `varchar(300)`. `Cantidad` `numeric(18,4)` positiva y `PrecioUnitario` `numeric(18,2)` no negativo. `DescuentoPorcentaje` `numeric(5,2)` y `PorcentajeImpuesto` `numeric(18,6)`, ambos en 0–100. `Subtotal`, `ImporteDescuento`, `BaseImponible`, `ImporteImpuesto` y `Total` son `numeric(18,2)` no negativos y se comprueban con las fórmulas de §4.4. Sin impuesto, porcentaje e importe son cero. |

`Facturas` referencia `Clientes` y `OrdenesCompra`. Cada detalle referencia `Facturas`, `Productos` y opcionalmente `Impuestos`. Todas las referencias usan `ON DELETE RESTRICT`. Hay índices sobre las claves foráneas y sobre `Facturas.FechaEmision` y `Facturas.Estado`.

### 4.2. Endpoints y permisos

Todos los endpoints requieren JWT válido. Cualquier usuario autenticado puede leer y escribir según el control global de acceso. El middleware común devuelve 403 si recibe `X-User-Role: Bloqueado` o `X-User-Role: SinPermiso`. Para POST y PUT, el middleware de cuerpo vacío se ejecuta antes de la autenticación. La emisión admite cuerpo vacío.

| Método y ruta | Caso de uso | Entrada | Éxito |
|---|---|---|---|
| GET `/api/v1/facturas` | `ListarFacturasUseCase` | `busqueda`, `clienteId`, `tipoComprobante`, `estado`, `moneda`, `fechaDesde`, `fechaHasta`, `pagina=1`, `tamanoPagina=20` | 200 `PaginaFacturasDto` |
| GET `/api/v1/facturas/resumen` | `ResumenFacturasUseCase` | `moneda=PEN`, `fechaDesde`, `fechaHasta` | 200 `ResumenFacturasDto` |
| GET `/api/v1/facturas/{id}` | `ObtenerFacturaUseCase` | Id entero | 200 `FacturaDetalleDto` |
| POST `/api/v1/facturas` | `CrearFacturaUseCase` | `GuardarFacturaRequestDto` | 201 `FacturaDetalleDto` y `Location` al detalle |
| PUT `/api/v1/facturas/{id}` | `ActualizarFacturaUseCase` | Id entero y `GuardarFacturaRequestDto` | 200 `FacturaDetalleDto` |
| POST `/api/v1/facturas/{id}/emitir` | `EmitirFacturaUseCase` | Id entero | 200 `FacturaDetalleDto` |
| POST `/api/v1/facturas/{id}/anular` | `AnularFacturaUseCase` | Id entero y `AnularFacturaRequestDto` | 200 `FacturaDetalleDto` |

### 4.3. DTOs

Las propiedades de solicitudes y respuestas exitosas se serializan en camelCase. El signo `?` indica un campo anulable.

| DTO | Campos |
|---|---|
| `GuardarFacturaRequestDto` | `clienteId`, `ordenCompraId?`, `tipoComprobante`, `serie`, `numero`, `fechaEmision`, `fechaVencimiento?`, `moneda` (por defecto `PEN`), `observaciones?`, `detalles` (`GuardarDetalleFacturaRequestDto[]`). |
| `GuardarDetalleFacturaRequestDto` | `productoId`, `impuestoId?`, `descripcion`, `cantidad`, `precioUnitario`, `descuentoPorcentaje` (por defecto 0). |
| `AnularFacturaRequestDto` | `motivo`. |
| `FacturaDetalleDto` | `id`, `clienteId`, `clienteNombre`, `clienteRuc`, `ordenCompraId?`, `numeroOrdenCompra?`, `tipoComprobante`, `serie`, `numero`, `numeroComprobante`, `fechaEmision`, `fechaVencimiento?`, `moneda`, `estado`, `subtotal`, `totalDescuento`, `totalImpuesto`, `total`, `observaciones?`, `fechaCreacion`, `fechaAnulacion?`, `motivoAnulacion?`, `detalles` (`DetalleFacturaDto[]`). |
| `DetalleFacturaDto` | `id`, `productoId`, `productoNombre`, `impuestoId?`, `impuestoNombre?`, `descripcion`, `cantidad`, `precioUnitario`, `descuentoPorcentaje`, `porcentajeImpuesto`, `subtotal`, `importeDescuento`, `baseImponible`, `importeImpuesto`, `total`. Las líneas se devuelven por `id` ascendente. |
| `FacturaListadoDto` | Campos de `FacturaDetalleDto` salvo `ordenCompraId`, `numeroOrdenCompra`, `observaciones`, `fechaCreacion`, `fechaAnulacion`, `motivoAnulacion` y `detalles`. Añade `cantidadDetalles`. |
| `PaginaFacturasDto` | `items` (`FacturaListadoDto[]`), `pagina`, `tamanoPagina`, `totalRegistros`, `totalPaginas`. |
| `ResumenFacturasDto` | `moneda`, `totalComprobantes`, `totalFacturas`, `totalBoletas`, `totalBorradores`, `totalEmitidas`, `totalAnuladas`, `subtotalFacturado`, `totalDescuentos`, `totalImpuestos`, `totalFacturado`. |

### 4.4. Reglas

- **Alta y edición.** El cliente, los productos y, si se indican, la orden y el impuesto deben existir. La orden debe pertenecer al cliente. Se ignoran espacios exteriores y mayúsculas/minúsculas al interpretar el tipo. Serie y moneda se guardan en mayúsculas y sin espacios exteriores. La serie y el número los indica el usuario y se muestran como `SERIE-NNNNNNNN` con un mínimo de ocho dígitos para el número. Una anulación conserva la numeración.
- **Validación de entrada.** Se exige fecha de emisión, cliente, serie, número positivo y al menos un detalle válido. `FechasUtc` conserva fechas UTC, convierte fechas locales y trata las fechas sin zona como UTC. La descripción de línea es obligatoria y se recorta a efectos de validación. Las observaciones en blanco se guardan como nulas. Se limita cantidad a 14 dígitos enteros y 4 decimales, precio e importes a 16 enteros y 2 decimales, y descuento a 0–100 con 2 decimales. Se valida el subtotal antes de multiplicar y el rango de los importes calculados en línea y cabecera.
- **Cálculo.** Solo se acepta impuesto activo con código `IGV`. Se guarda el porcentaje de la tasa activa más reciente que cubra el día UTC de emisión, incluidos ambos extremos de vigencia. Con `r(x)` como redondeo a centavos usando `MidpointRounding.AwayFromZero`, por línea se calcula `subtotal = r(cantidad × precioUnitario)`, `importeDescuento = r(subtotal × descuentoPorcentaje / 100)`, `baseImponible = subtotal - importeDescuento`, `importeImpuesto = r(baseImponible × porcentajeImpuesto / 100)` y `total = baseImponible + importeImpuesto`. La cabecera suma los subtotales, descuentos e impuestos de sus líneas y deriva su total.
- **Estados.** El alta crea un `Borrador`. La edición se permite en borrador, conserva el Id y la fecha de creación, reemplaza las líneas y recalcula los totales. La emisión exige un borrador con al menos una línea, total mayor que cero y coincidencia entre los importes de cabecera y las sumas de detalle. Cambia el estado a `Emitida` y conserva los importes calculados. Se puede anular un borrador o una emitida con motivo obligatorio de hasta 500 caracteres tras recortar espacios. La anulación conserva las líneas y los importes y registra `FechaAnulacion` UTC y `MotivoAnulacion`.
- **Listado.** La búsqueda admite coincidencias parciales por nombre o RUC del cliente, serie y número. Una consulta `serie-número` con número entero se resuelve por coincidencia exacta. Se aplican los demás filtros en conjunto, con fechas desde/hasta inclusivas. Se ordena por emisión descendente y luego por Id descendente. La página debe ser positiva, el tamaño debe estar entre 1 y 100 y el desplazamiento no debe superar `int.MaxValue`.
- **Resumen.** Cuenta comprobantes por tipo y estado para una única moneda y el rango inclusivo de emisión. Los importes acumulan solo las emitidas. Sin coincidencias devuelve los contadores e importes en cero para la moneda solicitada.

### 4.5. Errores literales

Los errores de negocio y de acceso enumerados devuelven `{ "StatusCode": número, "Message": texto }`. En los mensajes con `{id}` se sustituye el identificador recibido.

| HTTP | Condición | Mensaje |
|---|---|---|
| 400 | POST o PUT sin cuerpo cuando se exige | El cuerpo de la petición (Body) no puede estar vacío. |
| 400 | ClienteId no positivo al guardar | Debe seleccionar un cliente válido. |
| 400 | OrdenCompraId no positivo | La orden de compra no es válida. |
| 400 | Orden de otro cliente | La orden de compra no pertenece al cliente seleccionado. |
| 400 | Tipo inválido en alta, edición o filtro | El tipo de comprobante debe ser Factura o Boleta. |
| 400 | Serie vacía | La serie del comprobante es obligatoria. |
| 400 | Serie de más de 10 caracteres | La serie no puede superar los 10 caracteres. |
| 400 | Número no positivo | El número del comprobante debe ser mayor que cero. |
| 400 | Emisión sin fecha | La fecha de emisión es obligatoria. |
| 400 | Vencimiento anterior a emisión | La fecha de vencimiento no puede ser anterior a la fecha de emisión. |
| 400 | Moneda con formato inválido | La moneda debe tener un código de tres letras. |
| 400 | Observaciones de más de 500 caracteres | El texto no puede superar los 500 caracteres. |
| 400 | Lista de líneas vacía | La factura debe contener al menos un producto. |
| 400 | Línea nula | El detalle de la factura no es válido. |
| 400 | ProductoId no positivo | Debe seleccionar un producto válido. |
| 400 | ImpuestoId no positivo | El impuesto seleccionado no es válido. |
| 400 | Impuesto inactivo | El impuesto seleccionado se encuentra inactivo. |
| 400 | Impuesto distinto de IGV | Por el momento solo se admite el IGV como impuesto de facturación. |
| 400 | Sin tasa que cubra la fecha de emisión | El impuesto no tiene una tasa vigente para la fecha de emisión. |
| 400 | Descripción vacía | La descripción del producto es obligatoria. |
| 400 | Descripción de más de 300 caracteres | La descripción no puede superar los 300 caracteres. |
| 400 | Cantidad fuera de rango | La cantidad del producto no es válida. |
| 400 | Más de cuatro decimales en cantidad | La cantidad admite como máximo cuatro decimales. |
| 400 | Precio unitario fuera de rango | El precio unitario no es válido. |
| 400 | Más de dos decimales en precio | El precio unitario admite como máximo dos decimales. |
| 400 | Descuento fuera de rango | El descuento debe estar entre 0 y 100. |
| 400 | Más de dos decimales en descuento | El descuento admite como máximo dos decimales. |
| 400 | Producto de cantidad y precio fuera de rango | El subtotal del producto supera el importe permitido. |
| 400 | Importe calculado fuera de rango | El importe calculado supera los límites permitidos. |
| 400 | Página no positiva | La página debe ser mayor que cero. |
| 400 | Tamaño de página inválido | El tamaño de página debe estar entre 1 y 100. |
| 400 | Desplazamiento de página fuera de rango | La página solicitada supera el límite permitido. |
| 400 | Filtro de cliente no positivo | El cliente seleccionado no es válido. |
| 400 | Filtro de estado inválido | El estado debe ser Borrador, Emitida o Anulada. |
| 400 | Rango de fechas invertido | La fecha inicial no puede ser posterior a la fecha final. |
| 400 | Emisión sin líneas | No se puede emitir una factura sin productos. |
| 400 | Emisión con total no positivo | El total de la factura debe ser mayor que cero para emitirla. |
| 400 | Motivo en blanco | El motivo de anulación es obligatorio. |
| 400 | Motivo de más de 500 caracteres | El motivo de anulación no puede superar los 500 caracteres. |
| 401 | Petición sin sesión válida | Debe iniciar sesión para acceder a este recurso. |
| 403 | Acceso denegado por el control global | Acceso denegado: Su rol no tiene permisos para ejecutar esta operación. |
| 404 | Comprobante inexistente | La factura solicitada no existe. |
| 404 | Cliente inexistente | El cliente seleccionado no existe. |
| 404 | Orden inexistente | La orden de compra seleccionada no existe. |
| 404 | Producto inexistente | No se encontró el producto con ID {id}. |
| 404 | Impuesto inexistente | No se encontró el impuesto con ID {id}. |
| 409 | Numeración repetida | Ya existe un comprobante con la misma serie y número. |
| 409 | Edición fuera de borrador | Solo se pueden modificar facturas en estado Borrador. |
| 409 | Emisión fuera de borrador | Solo se pueden emitir facturas en estado Borrador. |
| 409 | Diferencia entre cabecera y líneas al emitir | Los importes de la factura no coinciden con sus detalles. |
| 409 | Comprobante ya anulado | La factura ya se encuentra anulada. |
| 409 | Cualquier otro estado inválido al anular | La factura no se encuentra en un estado válido para su anulación. |
| 409 | Clave foránea que dejó de existir durante el alta | No se pudo guardar la factura porque una referencia ya no existe. |
| 409 | Clave foránea que dejó de existir durante la edición | No se pudo actualizar la factura porque una referencia ya no existe. |

### 4.6. Seed y smoke

El seed de desarrollo agrega `F51B-00000001` (borrador), `F51E-00000001` (emitida con descuento e IGV) y `B51A-00000001` (boleta anulada). Usa clientes, productos, órdenes e IGV del seed existente. La numeración identifica cada cabecera y se añade la línea de ejemplo cuando la cabecera carece de detalle. La recarga conserva el flujo de reinicialización de catálogos del seed de desarrollo y reproduce los mismos ejemplos.

El smoke comprueba los siete endpoints, autenticación, historial, errores literales, totales y los tres ejemplos. Verifica directamente en PostgreSQL los importes guardados y el rechazo de numeración duplicada, referencias huérfanas, total incoherente y anulación sin motivo.

## 5. Criterios de aceptación

| # | Criterio | Evidencia | Umbral |
|---|---|---|---|
| A1 | Compila el repositorio | `dotnet build -warnaserror` | 0 errores y 0 advertencias. |
| A2 | Gate general | `./scripts/check-rules.sh` y `./scripts/verify.sh` | Formato, arquitectura y suites completas en verde. |
| A3 | Contrato HTTP y estados | `FacturasApiTests` | Creación de ambos tipos, edición, emisión, anulación, listado, detalle, resumen y errores esperados. |
| A4 | Acceso y OpenAPI | `FacturasApiTests` y smoke | 401 con petición válida sin token, escritura y lectura autenticada, 403 global y siete operaciones publicadas. |
| A5 | Esquema y scaffold | Aplicación del SQL 007 y `./scripts/scaffold.sh --comprobar` | Entidades y contexto coinciden con PostgreSQL. |
| A6 | Ejemplos y persistencia | Dos cargas del seed oficial y `./scripts/smoke-postgres.sh` en PostgreSQL desechable | Tres comprobantes con sus líneas, cifras reproducibles y `SMOKE OK`. |

## 6. Checklist de ejecución

- [x] Contrato HTTP, estados y mensajes contrastados con las pruebas del módulo.
- [x] Esquema y navegaciones regenerados tras integrar `main`.
- [x] Seed, smoke y gate completo verificados.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-25 | A1–A2 | Correcto | `git diff --check`, `dotnet build -warnaserror` (0 advertencias), `./scripts/check-rules.sh` y `./scripts/verify.sh` con Node 22.23.5. |
| 2026-11-25 | A3–A4 | Correcto | 62 pruebas HTTP del módulo 51, 1.717 pruebas backend y 1.339 frontend. El smoke verificó las siete operaciones OpenAPI. |
| 2026-11-25 | A5 | Correcto | SQL 007 aplicado en PostgreSQL desechable. `./scripts/scaffold.sh --comprobar` terminó con `SCAFFOLD AL DÍA`. |
| 2026-11-25 | A6 | Correcto | Las dos cargas del seed produjeron las mismas tres cabeceras y tres líneas. `./scripts/smoke-postgres.sh` terminó con `SMOKE OK`. |
