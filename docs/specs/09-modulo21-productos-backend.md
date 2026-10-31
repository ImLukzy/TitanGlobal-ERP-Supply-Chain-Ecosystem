# Especificación: 09 - Modulo21_Productos (backend)

## 1. Objetivo
**Problema:** el maestro de productos (módulo 21) no tiene API. Las órdenes referencian productos, pero no se pueden dar de alta, consultar ni mantener.
**Resultado esperado:** `Modules/Modulo21_Productos/` ofrece en `api/v1/productos`:
- listado con búsqueda, filtros por categoría y precio, y paginación;
- detalle con indicadores de venta;
- alta, modificación y baja con reglas de negocio;
- lista de categorías y un resumen analítico.

Sigue el patrón de `Modulo01_Clientes` (spec 07).

## 2. Fuera de alcance
- Stock, lotes, almacenes (módulos 23–30) y un maestro de categorías aparte (módulo 22). `Categoria` sigue siendo texto en `Producto`.
- Nuevos campos en `Producto` o migraciones. No hace falta ningún índice nuevo.
- El frontend (spec 10).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Reglas tomadas por la integración, coherentes con las specs 06 y 07: un producto vendido no se borra → 409; el nombre del producto no es único.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Modules/Modulo21_Productos/Dtos/*.cs` | crear | un archivo por DTO (§4) |
| `src/TitanGlobal.Api/Modules/Modulo21_Productos/UseCases/*.cs` | crear | `ListarProductosUseCase`, `ObtenerProductoUseCase`, `CrearProductoUseCase`, `ActualizarProductoUseCase`, `EliminarProductoUseCase`, `ListarCategoriasProductoUseCase`, `ResumenProductosUseCase` (+ helper estático de validación si hace falta, como `ValidacionCliente`) |
| `src/TitanGlobal.Api/Modules/Modulo21_Productos/Controllers/ProductosController.cs` | crear | `[Route("api/v1/productos")]`, delgado |
| `scripts/smoke-postgres.sh` | modificar | añadir las rutas de productos (§5 A5) |
| `tests/TitanGlobal.Api.Tests/Modulo21_Productos/*.cs` | crear | tests de integración con InMemory |

## 4. Diseño y lógica
**Contrato.** JSON en camelCase; errores con el formato `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/productos?busqueda=&categoria=&precioMin=&precioMax=&pagina=1&tamanoPagina=20` | todos opcionales salvo los de paginación (con esos valores por defecto) | 200 `PaginaProductosDto { items: ProductoListadoDto[], total, pagina, tamanoPagina }` | 400: página < 1, tamaño fuera de 1–100, `precioMin > precioMax`, precio negativo |
| GET | `api/v1/productos/{id:int}` | — | 200 `ProductoDetalleDto` | 404 «El producto {id} no existe.» |
| POST | `api/v1/productos` | `GuardarProductoRequestDto { nombre, categoria, precio, descripcion? }` | 200 `{ id }` | 400 |
| PUT | `api/v1/productos/{id:int}` | `GuardarProductoRequestDto` | 200 `{ id }` | 400, 404 |
| DELETE | `api/v1/productos/{id:int}` | — | 204 | 404; 409 si tiene ventas |
| GET | `api/v1/productos/categorias` | — | 200 `string[]` (categorías distintas, ordenadas) | — |
| GET | `api/v1/productos/resumen` | — | 200 `ResumenProductosDto` | — |

**DTOs.**
- `ProductoListadoDto { id, nombre, categoria, precio, tieneDescripcion }`
- `ProductoDetalleDto { id, nombre, categoria, precio, descripcion, unidadesVendidas, montoVendido, clientesDistintos }`
  - `unidadesVendidas`: suma de `Cantidad` en `DetalleOrdenCompra`.
  - `montoVendido`: suma de `Cantidad × PrecioUnitario`.
  - `clientesDistintos`: número de clientes distintos que lo compraron, navegando `detalle.OrdenCompra.ClienteId`.
  - Sin ventas: 0, 0 y 0.
- `ResumenProductosDto { totalProductos, productosSinDescripcion, precioPromedio, porCategoria: CategoriaResumenDto[], masVendidos: ProductoVentaDto[] }`
- `CategoriaResumenDto { categoria, totalProductos, precioPromedio, precioMaximo }`, ordenado por categoría.
- `ProductoVentaDto { productoId, nombre, unidadesVendidas, montoVendido }`: top 5 por unidades y después por monto, ambos descendentes, y después por `Id`; solo productos con ventas.
- `precioPromedio` es 0 si no hay productos (patrón 7: `AnyAsync` antes de `AverageAsync`).

**Reglas, en `UseCases` con `ReglaNegocioException`:**
1. `nombre`: obligatorio tras `Trim()` y como máximo 200 caracteres. Si falta → 400 «El nombre del producto es obligatorio.»; si es más largo → 400 «El nombre del producto no puede superar 200 caracteres.»
2. `categoria`: obligatoria tras `Trim()` y como máximo 100 caracteres. Si falta → 400 «La categoría es obligatoria.»; si es más larga → 400 «La categoría no puede superar 100 caracteres.»
3. `precio > 0` → si no, 400 «El precio debe ser mayor que 0.»
4. `descripcion` en blanco → se guarda `null`.
5. Baja de un producto con ventas (`AnyAsync` sobre `DetalleOrdenCompra`) → 409 «No se puede eliminar el producto {id} porque tiene ventas registradas.» También si salta la clave foránea en la carrera (`23503`).
6. Id inexistente → 404 «El producto {id} no existe.»
7. Filtros del listado: `precioMin` o `precioMax` negativos → 400 «Los precios del filtro no pueden ser negativos.»; `precioMin > precioMax` → 400 «El precio mínimo no puede ser mayor que el máximo.»

**Consultas.** Todas desde `Repository<T>().AsQueryable()`, asíncronas y proyectadas a DTO en la consulta.
- `busqueda` filtra por `Nombre` sin distinguir mayúsculas.
- `categoria` se compara exacta, sin distinguir mayúsculas.
- Orden por `Nombre` y después `Id`.
- Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08 (filtro de texto, umbral numérico, `GroupBy`, `SelectMany`/`Distinct`, navegación inversa, promedio con `AnyAsync`).

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: cada mensaje de §4 con su código, literal y sin `Detail`; precio 0 → 400; categoría con espacios → se guarda sin ellos | pasan |
| A3 | Consultas | tests con datos conocidos: búsqueda, filtro por categoría (sin distinguir mayúsculas), rango de precio, paginación; detalle con ventas exactas y a 0 si no hay; categorías distintas ordenadas; resumen exacto (por categoría y top vendidos) | pasan |
| A4 | Sin migración | `git diff --stat` no toca `Migrations/` ni `Models/` | 0 archivos |
| A5 | PostgreSQL | `./scripts/smoke-postgres.sh` ampliado: listado con `categoria=computo`, rango `precioMin=100&precioMax=1000`, detalle 1 (unidades y monto), categorías, resumen, POST válido, DELETE del producto 1 → 409, DELETE de uno recién creado → 204 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 7 operaciones | presentes |

## 6. Checklist de ejecución
- [x] Tarea 1: DTOs, UseCases y controlador.
- [x] Tarea 2: tests y smoke.
- [x] Tarea 3: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-14 | A1 | ✅ | `./scripts/verify.sh` rc=0 en `ImLukzy/Claudio` (`c9c23a9`, idéntico al auditado `6b0fe52` según `git patch-id`): backend 81/81, frontend 53/53 |
| 2026-10-14 | A2–A3 | ✅ | `ProductosApiTests`: mensajes literales con su código y sin `Detail`, precio 0 → 400, categoría con espacios guardada sin ellos; búsqueda, categoría sin distinguir mayúsculas, rango de precio, paginación, detalle con ventas y a 0, categorías ordenadas, resumen exacto |
| 2026-10-14 | A4 | ✅ | el commit no toca `Models/` ni `Migrations/` |
| 2026-10-14 | A5 | ✅ | `./scripts/smoke-postgres.sh` → `SMOKE OK`: `categoria=computo`, rango 100–1000, detalle 1, categorías (5), resumen, POST → 200 `{id:11}`, DELETE producto 1 → 409 literal, DELETE recién creado → 204 |
| 2026-10-14 | A6 | ✅ | `/openapi/v1.json`: `productos` (GET, POST), `productos/{id}` (GET, PUT, DELETE), `productos/categorias` (GET), `productos/resumen` (GET) |
| 2026-10-14 | Auditoría | ✅ | Auditoría, ciclo 1: APROBADO; 2 observaciones menores informativas, sin cambios |
| 2026-10-14 | Nota | ℹ️ | En PostgreSQL `precioPromedio` sale con 16 decimales (`994.5000000000000000`); el frontend lo formatea en PEN. Redondear en la API queda fuera de esta spec |
| 2026-10-14 | QA E2E | ✅ | QA con frontend (Vite 5174) + API + PostgreSQL en `22f3215`: resumen del seed (10 productos, 3 sin descripción, S/ 994.50, 5 categorías, top vendidos); búsqueda, categoría, rango y vuelta a la página 1; precios negativos y mínimo > máximo con literales y sin petición; detalle de Laptop Pro y «Sin ventas registradas»; alta, edición, validación local; baja de Laptop Pro → 409 en el diálogo, Escape, baja del creado; Clientes y OrdenesCompra sin regresiones; márgenes ≥ 16 px, sin scroll horizontal ni celdas recortadas a 1280 |
