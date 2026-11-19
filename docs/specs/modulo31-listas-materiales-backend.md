# Especificación: Módulo 31 — Listas de Materiales (backend)

## 1. Objetivo

El dominio de Manufactura necesita registrar qué componentes se requieren para fabricar un producto. El módulo permite consultar listas de materiales (BOM), preparar una versión en borrador, activarla y retirar la versión activa conservando su historial.

Responsable: Antony Huayhua. Estado: aprobación de Lukas confirmada por Antony.

## 2. Fuera de alcance

- Explosión multinivel, planificación de necesidades, cálculo de costos, mermas y conversiones de unidades.
- Consumo de inventario e integración con órdenes y rutas de producción.
- La pantalla React, que tendrá su propia spec una vez aprobado el contrato del backend.
- Cambios manuales al modelo `Producto`, al pipeline y a scripts SQL fusionados.

**Decisiones de alcance:** una lista contiene los componentes directos para una unidad del producto fabricado. Se exige una unidad de medida por línea porque `Producto` aún no tiene ese campo. Se conserva cada versión usada en producción. La activación exige que el producto quede sin otra versión activa, por lo que el retiro y la activación de la versión siguiente son operaciones separadas.

## 3. Archivos afectados

| Archivo | Acción |
|---|---|
| `database/002-listas-materiales.sql` | Crear las tablas `ListasMateriales` y `DetallesListaMaterial` con el siguiente número libre al implementar (`002` al redactar). |
| `src/TitanGlobal.Domain/Models/ListaMateriales.cs` y `DetalleListaMaterial.cs` | Generar con scaffold. Las navegaciones resultantes de las claves foráneas a `Producto` también salen del scaffold. |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | Regenerar con scaffold. |
| `src/TitanGlobal.Infrastructure/config/TitanGlobalDbContext.ListasMateriales.cs` | Configurar la navegación de varias versiones por producto. |
| `src/TitanGlobal.Domain/Repositories/IEscrituraListaMaterial.cs`, `src/TitanGlobal.Infrastructure/Repositories/Implementation/EscrituraListaMaterial.cs` e `InfrastructureServicesExtensions.cs` | Serializar las escrituras de una misma lista con un bloqueo de cabecera en PostgreSQL y registrar el servicio. |
| `src/TitanGlobal.Application/DTOs/Modulo31_ListasMateriales/` y `UseCases/Modulo31_ListasMateriales/` | Crear DTOs, validación y casos de uso. |
| `src/TitanGlobal.Api/Controllers/Modulo31_ListasMateriales/ListasMaterialesController.cs` | Crear el controlador HTTP. |
| `tests/TitanGlobal.Api.Tests/Modulo31_ListasMateriales/` | Crear pruebas de integración del contrato. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | Agregar datos idempotentes y comprobaciones contra PostgreSQL. |

## 4. Diseño y lógica

### Datos

| Tabla | Campos y restricciones |
|---|---|
| `ListasMateriales` | `Id`, `ProductoId` (FK a `Productos`), `Version` (entero positivo), `Estado` (`Borrador`, `Activa`, `Retirada`). Unicidad de `(ProductoId, Version)` y un índice único parcial por `ProductoId` para `Estado = 'Activa'`. |
| `DetallesListaMaterial` | `Id`, `ListaMaterialId` (FK a `ListasMateriales`), `ComponenteProductoId` (FK a `Productos`), `Cantidad` (`numeric(18,6)` positivo), `UnidadMedida` (`varchar(20)`). Unicidad de `(ListaMaterialId, ComponenteProductoId)`. |

La base protege las referencias a `Productos` con borrado restringido. El alta y el reemplazo de líneas se guardan de forma atómica. La edición y las transiciones de estado de la misma cabecera deben serializarse para que un cambio de líneas no se confirme después de la activación. El índice parcial impide que dos activaciones simultáneas de un mismo producto queden confirmadas.

### Contrato HTTP

Base `api/v1/listas-materiales`. JWT obligatorio. JSON en camelCase. Los errores de negocio usan el contrato del middleware vigente.

| Verbo y ruta relativa | Entrada | Respuesta |
|---|---|---|
| `GET /` | `productoId?`, `estado?`, `pagina=1`, `tamanoPagina=20` | 200 `PaginaListasMaterialesDto`. |
| `GET /{id:int}` | — | 200 `ListaMaterialDetalleDto` o 404. |
| `POST /` | `CrearListaMaterialRequestDto` | 201 `{ id }` con `Location` al detalle, 400 o 409. Crea en `Borrador`. |
| `PUT /{id:int}` | `EditarListaMaterialRequestDto` | 200 `{ id }`, 400, 404 o 409. Reemplaza las líneas completas del borrador. |
| `POST /{id:int}/activar` | Sin cuerpo | 200 `{ id }`, 404 o 409. Pasa de `Borrador` a `Activa`. |
| `POST /{id:int}/retirar` | Sin cuerpo | 200 `{ id }`, 404 o 409. Pasa de `Activa` a `Retirada`. |

- `PaginaListasMaterialesDto { items: ListaMaterialListadoDto[], total, pagina, tamanoPagina }`
- `ListaMaterialListadoDto { id, productoId, productoNombre, version, estado, totalComponentes }`
- `ListaMaterialDetalleDto { id, productoId, productoNombre, version, estado, componentes: ComponenteListaMaterialDto[] }`
- `ComponenteListaMaterialDto { componenteProductoId, componenteProductoNombre, cantidad, unidadMedida }`
- `CrearListaMaterialRequestDto { productoId, version, componentes: ComponenteListaMaterialRequestDto[] }`
- `EditarListaMaterialRequestDto { componentes: ComponenteListaMaterialRequestDto[] }`
- `ComponenteListaMaterialRequestDto { componenteProductoId, cantidad, unidadMedida }`

El listado ordena por `productoId` ascendente y `version` descendente. El detalle ordena los componentes por `componenteProductoId`. El filtro de estado acepta los valores canónicos sin distinguir mayúsculas. Tras el alta, el producto y el número de versión son fijos. Una versión retirada queda disponible para consulta.

### Validaciones y mensajes literales

En las escrituras por ID se verifica primero la existencia y luego el estado. En el alta se valida primero producto y versión. Las líneas se validan en el orden recibido, y los conflictos de unicidad se comprueban al final. Las violaciones concurrentes de las restricciones usan el mismo mensaje que el conflicto detectado en Application.

| Condición | HTTP | Mensaje |
|---|---|---|
| `pagina < 1` | 400 | La página debe ser mayor o igual a 1. |
| `tamanoPagina` fuera de 1–100 | 400 | El tamaño de página debe estar entre 1 y 100. |
| `estado` de filtro inválido | 400 | El estado de la lista de materiales no es válido. |
| `productoId` no positivo en alta o filtro | 400 | El identificador del producto debe ser mayor que 0. |
| Producto fabricado inexistente al crear | 400 | El producto {productoId} no existe. |
| Versión no positiva | 400 | La versión debe ser mayor que 0. |
| Líneas ausentes, vacías o con más de 100 elementos | 400 | La lista de materiales debe tener entre 1 y 100 componentes. |
| Línea nula o identificador de componente no positivo | 400 | El identificador del componente debe ser mayor que 0. |
| Componente igual al producto fabricado | 400 | El producto fabricado no puede ser componente de su propia lista. |
| Componente repetido | 400 | El componente {componenteProductoId} está repetido. |
| Cantidad fuera de `(0, 999999999999.999999]` | 400 | La cantidad debe ser mayor que 0 y no superar 999999999999.999999. |
| Cantidad con más de seis decimales significativos | 400 | La cantidad admite como máximo 6 decimales. |
| Unidad en blanco tras `Trim()` | 400 | La unidad de medida es obligatoria. |
| Unidad de más de 20 caracteres tras `Trim()` | 400 | La unidad de medida no puede superar 20 caracteres. |
| Producto componente inexistente | 400 | El producto componente {componenteProductoId} no existe. |
| Versión repetida para el mismo producto | 409 | Ya existe la versión {version} para el producto {productoId}. |
| Lista inexistente | 404 | La lista de materiales {id} no existe. |
| Edición de una lista que no está en borrador | 409 | Solo se pueden editar listas de materiales en borrador. |
| Activación de una lista que no está en borrador | 409 | Solo se pueden activar listas de materiales en borrador. |
| Activación cuando el producto ya tiene una lista activa | 409 | El producto {productoId} ya tiene una lista de materiales activa. |
| Retiro de una lista que no está activa | 409 | Solo se pueden retirar listas de materiales activas. |

## 5. Criterios de aceptación

| # | Verificación | Umbral |
|---|---|---|
| A1 | `./scripts/verify.sh` | En verde y sin warnings. |
| A2 | OpenAPI y pruebas HTTP de integración | Seis operaciones autenticadas y cobertura de los estados y mensajes de §4. |
| A3 | Pruebas de listado y detalle | Filtros combinables, orden y paginación estables y nombres de productos visibles. |
| A4 | Pruebas de escritura | Alta, edición, activación y retiro conservan el historial. Una validación fallida conserva las líneas anteriores. |
| A5 | PostgreSQL real | Script aplicado, scaffold reproducible y `./scripts/smoke-postgres.sh` termina en `SMOKE OK`. Dos activaciones simultáneas para el mismo producto dejan una sola lista activa. Una edición que pierde la carrera contra la activación conserva las líneas activadas. |
| A6 | Seed local | Dos versiones para un producto del seed, una activa y una en borrador, con al menos dos componentes distintos cada una. Reejecutarlo no duplica filas. |

## 6. Checklist de ejecución

- [x] Aprobación de Lukas sobre el alcance y el contrato.
- [ ] Primer commit de implementación: script SQL y scaffold.
- [ ] Segundo commit de implementación: DTOs, validaciones y casos de uso.
- [ ] Tercer commit de implementación: controlador, pruebas, seed y smoke.
- [ ] Registrar los resultados de A1–A6 y abrir el PR de backend.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-18 | A1 | Terminó con avisos del frontend | `./scripts/verify.sh` salió con código 0. Compilación .NET sin advertencias, 968 pruebas de backend y 497 de frontend aprobadas. npm avisó sobre la versión de Node y la de ESLint. Vite avisó sobre un archivo superior a 500 kB. |
| 2026-11-18 | A2–A4 | Correcto | `dotnet test --no-restore --filter FullyQualifiedName~Modulo31_ListasMateriales`: 26 pruebas aprobadas. OpenAPI confirmado en el smoke. |
| 2026-11-18 | A5 | Correcto | `./scripts/smoke-postgres.sh`: scaffold al día, concurrencia verificada y `SMOKE OK` en un proyecto Docker aislado. |
