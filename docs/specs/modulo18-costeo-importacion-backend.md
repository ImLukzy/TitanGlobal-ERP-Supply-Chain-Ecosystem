# Especificación: modulo18-costeo-importacion-backend — Costeo de importación

## 1. Objetivo
**Problema:** una importación cuesta más que el precio del proveedor: fletes, seguros, derechos de arancel y gastos de nacionalización. Sin repartir esos costos entre los productos del embarque, no se conoce el costo real de cada unidad (landed cost).
**Resultado esperado:** tres entidades nuevas, `CosteoImportacion`, `CostoImportacion` y `LineaCosteoImportacion`, con sus tablas y `Modulo18_CosteoImportacion` en `api/v1/costeos-importacion`:
- abrir el costeo de un expediente de importación;
- registrar los costos adicionales (flete, seguro, arancel, gastos de agente, almacenaje, otros) y los productos del embarque con su cantidad;
- cerrar el costeo: el servidor prorratea los costos entre los productos según su valor y calcula el costo unitario nacionalizado;
- listado con filtros y paginación, detalle y resumen.

Sigue el patrón de `Modulo13_SolicitudesCompra` (cabecera con detalle) y de `Modulo14_Licitaciones` (cierre explícito). Depende de `Modulo16_Importaciones` y de `Modulo15_OrdenesAbastecimiento`.

## 2. Fuera de alcance
- Las 4 entidades del enunciado, los middlewares y el pipeline.
- Cambios en `Modulo15_OrdenesAbastecimiento`, `Modulo16_Importaciones` ni `Modulo17_AgentesAduaneros`: el costeo no se conecta con la declaración de aduana; el arancel se escribe a mano como un costo más.
- Multimoneda y tipo de cambio: todos los montos están en la moneda base (soles) y se asume que los precios de la orden también. Se revisará cuando exista `Modulo74_MonedasTiposCambio`.
- Actualizar el costo de los productos o el kardex con el resultado (módulo 26 de inventarios).
- Costos prorrateados por peso o volumen: solo por valor.
- El frontend (spec `modulo18-costeo-importacion-frontend.md`, posterior).
- Scripts de `database/` que no sean el de §3.

**Decisiones de producto que requieren aprobación antes de ejecutar:**
1. **Un costeo por expediente** (índice único sobre el expediente).
2. **Expedientes elegibles:** `EnTransito`, `EnAduana` o `Liberado`; no `Programado` ni `Cancelado`.
3. **Las líneas las escribe el usuario:** el expediente no lleva productos y una orden puede tener varios embarques. Cada línea es un producto de la orden del expediente con la cantidad de este embarque, que no puede superar la cantidad pedida. El valor FOB de la línea es `cantidad × precio unitario` de la orden.
4. **Conceptos de costo:** `Flete`, `Seguro`, `DerechosArancel`, `GastosAgenteAduanero`, `Almacenaje` y `Otros`; se pueden repetir.
5. **Prorrateo por valor FOB:** `costo de la línea = total de costos × valor FOB de la línea / suma de valores FOB`. La última línea absorbe la diferencia de redondeo, para que la suma sea exacta.
6. **Costo unitario nacionalizado** = `(valor FOB + costo prorrateado) / cantidad`, con 4 decimales.
7. **Estados:** `Abierto` → `Cerrado`. `Cerrado` es final y congela los resultados.
8. **Cerrar exige** al menos una línea y al menos un costo.
9. **Solo se elimina un costeo `Abierto`.**

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `database/029-costeo-importacion.sql` | crear | `CREATE TABLE "CosteosImportacion"`, `"CostosImportacion"` y `"LineasCosteoImportacion"`; número `029` reservado en `docs/equipo/relevo.md` §8 |
| `docs/equipo/relevo.md` | modificar | solo añadir la fila 029, sin reescribir la tabla |
| `src/TitanGlobal.Domain/Models/CosteoImportacion.cs`, `CostoImportacion.cs`, `LineaCosteoImportacion.cs`, `ExpedienteImportacion.cs`, `Producto.cs` | regenerar | entidades `partial` generadas por `./scripts/scaffold.sh`, sin editarlas a mano |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | `DbSet` de las tres entidades, generado |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | modificar | solo si el scaffold nombra mal las entidades |
| `src/TitanGlobal.Application/DTOs/Modulo18_CosteoImportacion/*.cs` | crear | DTOs de entrada y salida (§4) |
| `src/TitanGlobal.Application/UseCases/Modulo18_CosteoImportacion/*.cs` | crear | un caso de uso por operación y `ValidacionCosteoImportacion` |
| `src/TitanGlobal.Api/Controllers/Modulo18_CosteoImportacion/CosteosImportacionController.cs` | crear | `[Route("api/v1/costeos-importacion")]`, controlador delgado; las acciones sin cuerpo llevan `[PermitirCuerpoVacio]` |
| `tests/TitanGlobal.Api.Tests/Modulo18_CosteoImportacion/CosteosImportacionApiTests.cs` | crear | pruebas de integración con InMemory, datos propios del test |
| `scripts/seed-dev.sql` | modificar | 3 costeos con sus costos y líneas, idempotentes al final (§4) |
| `scripts/smoke-postgres.sh` | modificar | rutas del módulo (§5 A7) |

## 4. Diseño y lógica
**Tabla `CosteosImportacion`:**

| Columna | Tipo | Regla |
|---|---|---|
| `Id` | `integer` identidad | clave primaria |
| `ExpedienteImportacionId` | `integer NOT NULL` | clave foránea a `ExpedientesImportacion("Id")` `ON DELETE RESTRICT`; índice único `IX_CosteosImportacion_ExpedienteImportacionId` |
| `Estado` | `character varying(20) NOT NULL` | `CHECK` en (`Abierto`, `Cerrado`) |
| `TotalCostos` | `numeric(18,2)` | solo con estado `Cerrado` |
| `FechaCierre` | `timestamp with time zone` | UTC; solo con estado `Cerrado` |
| `FechaRegistro` | `timestamp with time zone NOT NULL` | UTC, la pone el servidor |

**Tabla `CostosImportacion`:**

| Columna | Tipo | Regla |
|---|---|---|
| `Id` | `integer` identidad | clave primaria |
| `CosteoImportacionId` | `integer NOT NULL` | clave foránea a `CosteosImportacion("Id")` `ON DELETE RESTRICT` |
| `Concepto` | `character varying(30) NOT NULL` | `CHECK` en los 6 valores de la decisión 4 |
| `Monto` | `numeric(18,2) NOT NULL` | `CHECK` > 0 |
| `Descripcion` | `character varying(200)` | opcional |

**Tabla `LineasCosteoImportacion`:**

| Columna | Tipo | Regla |
|---|---|---|
| `Id` | `integer` identidad | clave primaria |
| `CosteoImportacionId` | `integer NOT NULL` | clave foránea a `CosteosImportacion("Id")` `ON DELETE RESTRICT` |
| `ProductoId` | `integer NOT NULL` | clave foránea a `Productos("Id")` `ON DELETE RESTRICT` |
| `Cantidad` | `integer NOT NULL` | `CHECK` entre 1 y 1000000 |
| `ValorFob` | `numeric(18,2) NOT NULL` | `CHECK` > 0; lo calcula el servidor al agregar la línea |
| `CostoProrrateado` | `numeric(18,2)` | solo con costeo `Cerrado` |
| `CostoUnitario` | `numeric(18,4)` | solo con costeo `Cerrado` |

Índice único `IX_LineasCosteoImportacion_CosteoImportacionId_ProductoId`. Los `CHECK` siguen la convención `CK_<Tabla>_<Regla>`.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }` sin `Detail`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/costeos-importacion?busqueda=&estado=&pagina=1&tamanoPagina=20` | todos opcionales | 200 `PaginaCosteosDto` | 400 |
| GET | `api/v1/costeos-importacion/{id:int}` | — | 200 `CosteoDetalleDto` | 404 |
| POST | `api/v1/costeos-importacion` | `CrearCosteoRequestDto { expedienteImportacionId }` | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/costeos-importacion/{id:int}` | — | 204 | 404, 409 |
| POST | `api/v1/costeos-importacion/{id:int}/costos` | `CrearCostoRequestDto` | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/costeos-importacion/{id:int}/costos/{costoId:int}` | — | 204 | 404, 409 |
| POST | `api/v1/costeos-importacion/{id:int}/lineas` | `CrearLineaCosteoRequestDto` | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/costeos-importacion/{id:int}/lineas/{lineaId:int}` | — | 204 | 404, 409 |
| POST | `api/v1/costeos-importacion/{id:int}/cerrar` | sin cuerpo | 200 `{ id }` | 404, 409 |
| GET | `api/v1/costeos-importacion/resumen` | — | 200 `ResumenCosteosDto` | — |

**DTOs:**
- `CrearCostoRequestDto { concepto, monto, descripcion? }`
- `CrearLineaCosteoRequestDto { productoId, cantidad }`
- `CosteoListadoDto { id, expedienteNumero, ordenNumero, estado, totalCostos, totalLineas }` (`totalCostos` es la suma actual de los costos, también mientras está abierto).
- `CosteoDetalleDto { id, expedienteImportacionId, expedienteNumero, ordenAbastecimientoId, ordenNumero, estado, totalCostos, totalValorFob, fechaCierre, fechaRegistro, costos: CostoDto[] { id, concepto, monto, descripcion }, lineas: LineaCosteoDto[] { id, productoId, productoNombre, cantidad, valorFob, costoProrrateado, costoUnitario } }`
- `PaginaCosteosDto { items, total, pagina, tamanoPagina }`
- `ResumenCosteosDto { totalCosteos, montoCostos, porEstado: EstadoConteoDto[] { estado, total } }` (`porEstado` siempre trae `Abierto` y `Cerrado`; `montoCostos` suma todos los costos registrados).

**Reglas y mensajes** (400 en validación; 404 si el recurso no existe; 409 en conflictos de estado). La búsqueda del listado es sobre número de expediente y número de orden, sin distinguir mayúsculas.

*POST costeo:* `expedienteImportacionId` ≤ 0 → 400 «El expediente es obligatorio.»; inexistente → 404 «El expediente {id} no existe.»; estado `Programado` o `Cancelado` → 409 «El expediente {id} no admite costeo en estado {estado}.»; ya tiene costeo → 409 «El expediente {id} ya tiene un costeo.» (también ante `23505`). Nace `Abierto`.
*DELETE costeo:* 404 «El costeo {id} no existe.»; estado `Cerrado` → 409 «El costeo {id} está cerrado.»; borra sus costos y líneas.
*POST costo:* 404 «El costeo {id} no existe.»; estado `Cerrado` → 409 «El costeo {id} está cerrado.»; `concepto` fuera de los 6 valores → 400 «El concepto no es válido.»; `monto` ≤ 0 → 400 «El monto debe ser mayor a 0.»; con más de 2 decimales o más de 16 dígitos enteros → 400 «El monto no es válido.»; `descripcion` más de 200 → 400 «La descripción no puede superar 200 caracteres.» (en blanco → `null`).
*DELETE costo:* 404 «El costeo {id} no existe.» / 404 «El costo {costoId} no existe en el costeo {id}.»; costeo cerrado → 409 «El costeo {id} está cerrado.»
*POST línea:* 404; costeo cerrado → 409; `productoId` ≤ 0 → 400 «El producto es obligatorio.»; `cantidad` fuera de 1–1000000 → 400 «La cantidad debe estar entre 1 y 1000000.»; el producto no está en la orden del expediente → 400 «El producto {productoId} no pertenece a la orden {ordenNumero}.»; cantidad mayor a la pedida → 400 «La cantidad supera lo pedido en la orden ({pedida}).»; producto ya en el costeo → 409 «El producto {productoId} ya está en el costeo.» Calcula `ValorFob`.
*DELETE línea:* igual que costo, con «La línea {lineaId} no existe en el costeo {id}.»
*POST cerrar:* 404; ya cerrado → 409 «El costeo {id} ya está cerrado.»; sin líneas → 409 «El costeo {id} no tiene productos.»; sin costos → 409 «El costeo {id} no tiene costos.» Prorratea según la decisión 5, guarda `CostoProrrateado` y `CostoUnitario` en cada línea, `TotalCostos` y `FechaCierre`, y pasa a `Cerrado`.
*GET detalle:* 404 «El costeo {id} no existe.» *Listado:* 400 «La página debe ser mayor o igual a 1.», «El tamaño de página debe estar entre 1 y 100.», «El estado debe ser Abierto o Cerrado.» Orden: `Id` descendente.

**Consultas.** Desde `Repository<...>().AsQueryable()`, asíncronas y proyectadas a DTO en la consulta (sin N+1); totales del listado en la misma consulta. Para el valor FOB se lee la línea de la orden por `(OrdenAbastecimientoId, ProductoId)`. El cierre carga el costeo con sus costos y líneas, calcula en memoria y guarda con `CompleteAsync`.

**Seed de desarrollo** (`seed-dev.sql`, bloque `TRUNCATE ... RESTART IDENTITY CASCADE` y `INSERT`s que buscan el expediente por su número; va después del bloque de importaciones):

| Costeo | Expediente | Estado | Costos | Líneas |
|---|---|---|---|---|
| 1 | IMP-2026-004 | Cerrado | Flete 100.00, Seguro 25.00, DerechosArancel 27.50, GastosAgenteAduanero 47.50 (total 200.00) | producto 3 × 5, FOB 250.00, prorrateo 200.00, unitario 90.0000 |
| 2 | IMP-2026-003 | Abierto | Flete 150.00 | producto 1 × 1, FOB 1000.00 |
| 3 | IMP-2026-001 | Abierto | — | producto 7 × 3, FOB 8700.00 |

Resumen esperado del seed: 3 costeos, costos por 350.00 y conteo por estado Abierto 2 / Cerrado 1.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Gate | `./scripts/verify.sh` (formato, reglas de código y capas) | en verde |
| A3 | Reglas del costeo | tests: cada mensaje de alta, baja, costos y líneas de §4 con su código, literal y sin `Detail`; un costeo por expediente; expedientes no elegibles | pasan |
| A4 | Líneas | tests: producto fuera de la orden, cantidad sobre lo pedido, producto repetido y `ValorFob` calculado desde la orden | pasan |
| A5 | Prorrateo | tests con números conocidos: dos líneas con FOB 600 y 400 y costos por 100.00 → 60.00 y 40.00; un caso con redondeo donde la suma de líneas es exactamente el total; costo unitario con 4 decimales | pasan |
| A6 | Cierre y consultas | tests: cerrar congela y bloquea cambios, cerrar sin líneas o sin costos, filtros, búsqueda, paginación y resumen exacto del seed | pasan |
| A7 | Datos y PostgreSQL | el script aplica con `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh --comprobar` no da diferencias y `./scripts/smoke-postgres.sh` prueba listado, detalle, alta, costo, línea, cierre y duplicados | `SMOKE OK` |
| A8 | OpenAPI | `/openapi/v1.json` incluye las 10 operaciones | presentes |

## 6. Checklist de ejecución
*(Marca `[x]` solo cuando la tarea esté hecha **y** A1–A2 estén en verde.)*

- [ ] Tarea 1: script SQL, aplicarlo y generar entidades con scaffold.
- [ ] Tarea 2: DTOs, `ValidacionCosteoImportacion`, casos de uso y controlador.
- [ ] Tarea 3: tests, seed y smoke.
- [ ] Tarea 4: verificar A1–A8 y anotar resultados en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| — | — | pendiente | spec sin aprobar |
