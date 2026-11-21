# Especificación del módulo 32 — Rutas de Producción (backend)

## 1. Objetivo

Definir cómo se fabrica un producto mediante una secuencia de operaciones. Cada operación identifica su centro de trabajo y sus tiempos estándar de preparación y ejecución. El usuario puede consultar el historial, preparar una versión en borrador, activarla y retirarla.

**Responsable:** Antony Huayhua. **Estado:** propuesta pendiente de revisión y aprobación de Lukas.

## 2. Alcance y decisiones para revisión

- Una ruta pertenece a un producto. Se propone una sola versión activa por producto y números de versión únicos por producto.
- La ejecución se mide en minutos por unidad y la preparación en minutos por operación y lote. Se permite volver a usar un centro en distintas operaciones de la misma ruta.
- El catálogo de Productos y los centros de trabajo del módulo 33 se consultan mediante sus entidades existentes. Un centro puede quedar inactivo después de la activación de una ruta. La ruta y su historial siguen visibles.
- La pantalla tendrá su propia spec. La programación de capacidad, costos, rutas alternativas simultáneas y la integración con Órdenes de Producción o Listas de Materiales quedan fuera de este contrato.

## 3. Archivos afectados

| Ruta | Cambio |
|---|---|
| `database/NNN-rutas-produccion.sql` | Crear un script con el número libre según el registro de reservas vigente al implementar. |
| `src/TitanGlobal.Domain/Models/` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | Generar las entidades y sus relaciones con `./scripts/scaffold.sh`. |
| `src/TitanGlobal.Domain/Repositories/` y `src/TitanGlobal.Infrastructure/Repositories/Implementation/` | Incorporar el puerto y la persistencia para serializar escrituras de una misma ruta. Registrar la implementación en la configuración de Infrastructure. |
| `src/TitanGlobal.Application/DTOs/Modulo32_RutasProduccion/` y `src/TitanGlobal.Application/UseCases/Modulo32_RutasProduccion/` | Crear DTOs, validaciones y casos de uso. |
| `src/TitanGlobal.Api/Controllers/Modulo32_RutasProduccion/RutasProduccionController.cs` | Publicar el contrato HTTP. |
| `tests/TitanGlobal.Api.Tests/Modulo32_RutasProduccion/` | Cubrir el contrato y sus reglas con pruebas de integración. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | Añadir datos de ejemplo idempotentes y comprobar el contrato en PostgreSQL. |

## 4. Diseño y lógica

### Datos y estados

| Tabla | Campos y restricciones |
|---|---|
| `RutasProduccion` | `Id`, `ProductoId` (FK a `Productos`), `Version` (entero positivo) y `Estado` (`Borrador`, `Activa`, `Retirada`). Unicidad de `(ProductoId, Version)` e índice único parcial de `ProductoId` para el estado `Activa`. |
| `OperacionesRutaProduccion` | `Id`, `RutaProduccionId` (FK a `RutasProduccion`), `Secuencia` (entero entre 1 y 100), `Nombre` (`varchar(120)`), `CentroTrabajoId` (FK a `CentrosTrabajo`), `MinutosPreparacion` y `MinutosPorUnidad` (`numeric(10,2)`). Unicidad de `(RutaProduccionId, Secuencia)` e índice de `CentroTrabajoId`. |

El alta crea una versión en `Borrador` con todas sus operaciones. Producto y versión quedan fijos después del alta. La edición reemplaza las operaciones completas de un borrador en una transacción. La secuencia debe contener los enteros del 1 al número de operaciones, independientemente del orden del JSON recibido. El SQL comprueba versión, estado, rango de secuencia y signo de los tiempos.

La activación exige que todos los centros de la ruta estén activos al validarlos y que el producto tenga libre el estado `Activa`. El retiro pasa la única versión activa a `Retirada`. Las versiones retiradas se conservan para consulta. Las referencias a productos y centros de trabajo tienen borrado restringido.

La edición, activación y retiro se serializan por ruta dentro de una transacción. Se consulta el estado después de obtener el bloqueo de su cabecera. El índice parcial resuelve activaciones concurrentes de versiones diferentes del mismo producto. Una escritura fallida conserva el estado y las operaciones previamente guardadas.

### Contrato HTTP

Base `api/v1/rutas-produccion`. Todas las operaciones requieren JWT. Los errores de negocio usan el contrato vigente del middleware.

| Verbo y ruta relativa | Entrada | Respuesta |
|---|---|---|
| `GET /` | `productoId?`, `estado?`, `pagina=1`, `tamanoPagina=20` | 200 `PaginaRutasProduccionDto` o 400. |
| `GET /{id:int}` | — | 200 `RutaProduccionDetalleDto` o 404. |
| `POST /` | `CrearRutaProduccionRequestDto` | 201 `{ id }` con `Location` al detalle, 400 o 409. |
| `PUT /{id:int}` | `EditarRutaProduccionRequestDto` | 200 `{ id }`, 400, 404 o 409. |
| `POST /{id:int}/activar` | Sin cuerpo | 200 `{ id }`, 404 o 409. |
| `POST /{id:int}/retirar` | Sin cuerpo | 200 `{ id }`, 404 o 409. |

- `PaginaRutasProduccionDto { items: RutaProduccionListadoDto[], total, pagina, tamanoPagina }`
- `RutaProduccionListadoDto { id, productoId, productoNombre, version, estado, totalOperaciones }`
- `RutaProduccionDetalleDto { id, productoId, productoNombre, version, estado, operaciones: OperacionRutaProduccionDto[] }`
- `OperacionRutaProduccionDto { secuencia, nombre, centroTrabajoId, centroTrabajoNombre, minutosPreparacion, minutosPorUnidad }`
- `CrearRutaProduccionRequestDto { productoId, version, operaciones: OperacionRutaProduccionRequestDto[] }`
- `EditarRutaProduccionRequestDto { operaciones: OperacionRutaProduccionRequestDto[] }`
- `OperacionRutaProduccionRequestDto { secuencia, nombre, centroTrabajoId, minutosPreparacion, minutosPorUnidad }`

El listado aplica los filtros conjuntamente y ordena por `productoId` ascendente y `version` descendente. El filtro de estado acepta los tres estados sin distinguir mayúsculas. El total se calcula antes de paginar y una página fuera del total devuelve `items: []`. El detalle ordena las operaciones por secuencia ascendente.

### Validaciones y mensajes literales

El listado valida página, tamaño, producto y estado en ese orden. Las escrituras por ID verifican primero existencia y estado. El alta valida producto y versión antes de las operaciones. Las operaciones se validan en el orden de entrada y según el orden de la tabla. Después se verifica que la secuencia sea continua y, en el alta, que la versión sea única. La activación verifica los centros en orden de secuencia antes de comprobar si el producto tiene una ruta activa.

| Condición | HTTP | Mensaje |
|---|---|---|
| `pagina < 1` | 400 | La página debe ser mayor o igual a 1. |
| `tamanoPagina` fuera de 1–100 | 400 | El tamaño de página debe estar entre 1 y 100. |
| `productoId` no positivo en alta o filtro | 400 | El identificador del producto debe ser mayor que 0. |
| Estado de filtro inválido | 400 | El estado de la ruta de producción no es válido. |
| Producto inexistente en el alta | 400 | El producto {productoId} no existe. |
| Versión no positiva | 400 | La versión debe ser mayor que 0. |
| Operaciones ausentes, vacías o con más de 100 elementos | 400 | La ruta de producción debe tener entre 1 y 100 operaciones. |
| Operación nula | 400 | La operación es obligatoria. |
| Secuencia fuera de 1–100 | 400 | La secuencia debe estar entre 1 y 100. |
| Secuencia repetida | 400 | La secuencia {secuencia} está repetida. |
| Nombre vacío después de quitar espacios de los extremos | 400 | El nombre de la operación es obligatorio. |
| Nombre de más de 120 caracteres después de quitar espacios de los extremos | 400 | El nombre de la operación no puede superar 120 caracteres. |
| Identificador de centro no positivo | 400 | El identificador del centro de trabajo debe ser mayor que 0. |
| Centro inexistente en alta o edición | 400 | El centro de trabajo {centroTrabajoId} no existe. |
| Preparación ausente o fuera de 0–99999999.99 | 400 | Los minutos de preparación deben estar entre 0 y 99999999.99. |
| Preparación con más de dos decimales significativos | 400 | Los minutos de preparación admiten como máximo 2 decimales. |
| Ejecución por unidad ausente o fuera de (0, 99999999.99] | 400 | Los minutos por unidad deben ser mayores que 0 y no superar 99999999.99. |
| Ejecución por unidad con más de dos decimales significativos | 400 | Los minutos por unidad admiten como máximo 2 decimales. |
| Secuencia discontinua | 400 | Las secuencias deben ser consecutivas y empezar en 1. |
| Versión repetida para el producto | 409 | Ya existe la versión {version} para el producto {productoId}. |
| Ruta inexistente | 404 | La ruta de producción {id} no existe. |
| Edición fuera de borrador | 409 | Solo se pueden editar rutas de producción en borrador. |
| Activación fuera de borrador | 409 | Solo se pueden activar rutas de producción en borrador. |
| Centro inactivo durante la activación | 409 | El centro de trabajo {centroTrabajoId} está inactivo. |
| Producto con otra ruta activa | 409 | El producto {productoId} ya tiene una ruta de producción activa. |
| Retiro de una ruta que no está activa | 409 | Solo se pueden retirar rutas de producción activas. |

Una operación en borrador admite un centro inactivo. `minutosPreparacion` debe estar presente aunque valga cero. Los tiempos se guardan con los decimales validados, incluidos valores con ceros finales. Las violaciones concurrentes de las restricciones de producto, centro y versión se traducen al mismo código y mensaje del contrato.

## 5. Criterios de aceptación

| # | Verificación | Umbral |
|---|---|---|
| A1 | `./scripts/verify.sh` | Gate completo en verde con cero advertencias. |
| A2 | Pruebas HTTP y OpenAPI | Seis operaciones publicadas y autenticadas. Se comprueban los códigos y mensajes literales de §4. |
| A3 | Consultas | Filtros combinables, paginación estable, nombres de producto y centro, operaciones ordenadas e historial accesible. |
| A4 | Escrituras | Alta, edición, activación y retiro completos. Se acepta el mismo centro en dos secuencias. Una edición inválida conserva las líneas y un centro inactivo bloquea la activación. |
| A5 | PostgreSQL real | Script aplicado y scaffold reproducible. Dos activaciones concurrentes dejan una sola versión activa. Una edición que pierde la carrera contra la activación conserva las operaciones activadas. `./scripts/smoke-postgres.sh` termina en `SMOKE OK`. |
| A6 | Seed local | Dos versiones de un producto con dos operaciones cada una, una activa y una en borrador. Reejecutar el seed conserva el número de filas. |

## 6. Checklist de ejecución

- [ ] Obtener la aprobación de Lukas sobre el alcance y el contrato.
- [ ] Aplicar el script local y regenerar entidades y contexto.
- [ ] Implementar persistencia, casos de uso y endpoints.
- [ ] Incorporar pruebas de integración, seed y smoke.
- [ ] Ejecutar A1–A6 y registrar los resultados antes de abrir el PR.

## 7. Registro de verificación

Pendiente de implementación.
