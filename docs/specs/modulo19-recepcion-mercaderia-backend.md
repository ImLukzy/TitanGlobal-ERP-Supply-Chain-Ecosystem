# Especificación: modulo19-recepcion-mercaderia-backend — Recepción de mercadería

## 1. Objetivo
**Problema:** la orden de abastecimiento (módulo 15) solo se puede marcar como recibida de un golpe. No queda constancia de qué llegó al muelle, cuánto se aceptó, cuánto se rechazó y por qué, ni se admiten entregas parciales.
**Resultado esperado:** dos entidades nuevas, `ActaRecepcion` y `LineaActaRecepcion`, con sus tablas y `Modulo19_RecepcionMercaderia` en `api/v1/recepciones-mercaderia`:
- abrir el acta de una orden en un almacén de ingreso y registrar por producto la cantidad recibida y la rechazada con su motivo;
- cerrar el acta: queda `Conforme` si no hay rechazos o `ConObservaciones` si los hay;
- anular un acta;
- saber cuánto falta por recibir de cada producto de la orden;
- listado con filtros y paginación, detalle y resumen.

Sigue el patrón de `Modulo13_SolicitudesCompra` (cabecera con detalle) y de `Modulo14_Licitaciones` (cierre explícito). Depende de `Modulo15_OrdenesAbastecimiento` y de `Modulo23_Almacenes`.

## 2. Fuera de alcance
- Las 4 entidades del enunciado, los middlewares y el pipeline.
- Cambios en `Modulo15_OrdenesAbastecimiento`: cerrar un acta **no cambia** el estado de la orden ni su acción «recibir». Cuando el flujo esté integrado se decidirá si recibir pasa a depender de las actas.
- Movimientos de stock: `Productos` no tiene existencias y el kardex es el módulo 26. Esta spec solo deja constancia; no suma inventario.
- Ubicación dentro del almacén (zonas y racks, módulo 24) y control de calidad de insumos (módulo 36).
- Vínculo con importaciones (módulo 16): el acta se hace sobre la orden, no sobre el expediente.
- El frontend (spec `modulo19-recepcion-mercaderia-frontend.md`, posterior).
- Scripts de `database/` que no sean el de §3.

**Decisiones de producto que requieren aprobación antes de ejecutar:**
1. **Entregas parciales:** una orden puede tener varias actas. El pendiente de un producto es `cantidad pedida − suma de lo recibido en actas no anuladas`.
2. **Lo rechazado no cuenta como recibido:** vuelve a quedar pendiente y debe entregarse de nuevo.
3. **Órdenes elegibles:** solo `Emitida`.
4. **Almacén de ingreso:** debe existir y estar activo.
5. **Estados:** `Borrador` → `Conforme` o `ConObservaciones` (lo decide el cierre); `Borrador` → `Anulada`. Los tres últimos son finales.
6. **El número lo escribe el usuario** (por ejemplo `AR-2026-001`), único, en mayúsculas.
7. **Una línea por producto y acta;** el producto debe estar en la orden.
8. **Cada línea** debe tener `cantidadRecibida + cantidadRechazada > 0`; si rechaza algo, el motivo es obligatorio.
9. **Cerrar exige** al menos una línea. Solo se elimina o se anula un acta `Borrador`: una acta cerrada es constancia y no se modifica.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `database/030-recepcion-mercaderia.sql` | crear | `CREATE TABLE "ActasRecepcion"` y `"LineasActaRecepcion"`; número `030` reservado en `docs/equipo/relevo.md` §8 |
| `docs/equipo/relevo.md` | modificar | solo añadir la fila 030, sin reescribir la tabla |
| `src/TitanGlobal.Domain/Models/ActaRecepcion.cs`, `LineaActaRecepcion.cs`, `OrdenAbastecimiento.cs`, `Almacen.cs`, `Producto.cs` | regenerar | entidades `partial` generadas por `./scripts/scaffold.sh`, sin editarlas a mano |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | `DbSet` de las dos entidades, generado |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | modificar | solo si el scaffold nombra mal las entidades (`ActaRecepcion`, `LineaActaRecepcion`) |
| `src/TitanGlobal.Application/DTOs/Modulo19_RecepcionMercaderia/*.cs` | crear | DTOs de entrada y salida (§4) |
| `src/TitanGlobal.Application/UseCases/Modulo19_RecepcionMercaderia/*.cs` | crear | un caso de uso por operación y `ValidacionActaRecepcion` |
| `src/TitanGlobal.Api/Controllers/Modulo19_RecepcionMercaderia/RecepcionesMercaderiaController.cs` | crear | `[Route("api/v1/recepciones-mercaderia")]`, controlador delgado; las acciones sin cuerpo llevan `[PermitirCuerpoVacio]` |
| `tests/TitanGlobal.Api.Tests/Modulo19_RecepcionMercaderia/RecepcionesMercaderiaApiTests.cs` | crear | pruebas de integración con InMemory, datos propios del test |
| `scripts/seed-dev.sql` | modificar | 4 actas con sus líneas, idempotentes al final (§4) |
| `scripts/smoke-postgres.sh` | modificar | rutas del módulo (§5 A7) |

## 4. Diseño y lógica
**Tabla `ActasRecepcion`:**

| Columna | Tipo | Regla |
|---|---|---|
| `Id` | `integer` identidad | clave primaria |
| `Numero` | `character varying(20) NOT NULL` | único (`IX_ActasRecepcion_Numero`) |
| `OrdenAbastecimientoId` | `integer NOT NULL` | clave foránea a `OrdenesAbastecimiento("Id")` `ON DELETE RESTRICT` |
| `AlmacenId` | `integer NOT NULL` | clave foránea a `Almacenes("Id")` `ON DELETE RESTRICT` |
| `FechaRecepcion` | `timestamp with time zone NOT NULL` | UTC |
| `Estado` | `character varying(20) NOT NULL` | `CHECK` en (`Borrador`, `Conforme`, `ConObservaciones`, `Anulada`) |
| `Observaciones` | `character varying(500)` | opcional |
| `MotivoAnulacion` | `character varying(500)` | solo con estado `Anulada` |
| `FechaRegistro` | `timestamp with time zone NOT NULL` | UTC, la pone el servidor |

**Tabla `LineasActaRecepcion`:**

| Columna | Tipo | Regla |
|---|---|---|
| `Id` | `integer` identidad | clave primaria |
| `ActaRecepcionId` | `integer NOT NULL` | clave foránea a `ActasRecepcion("Id")` `ON DELETE RESTRICT` |
| `ProductoId` | `integer NOT NULL` | clave foránea a `Productos("Id")` `ON DELETE RESTRICT` |
| `CantidadRecibida` | `integer NOT NULL` | `CHECK` entre 0 y 1000000 |
| `CantidadRechazada` | `integer NOT NULL` | `CHECK` entre 0 y 1000000 |
| `MotivoRechazo` | `character varying(300)` | obligatorio si hay rechazo |

`CHECK` adicional `CK_LineasActaRecepcion_AlgunaCantidad`: `CantidadRecibida + CantidadRechazada > 0`. Índice único `IX_LineasActaRecepcion_ActaRecepcionId_ProductoId`. Los `CHECK` siguen la convención `CK_<Tabla>_<Regla>`.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }` sin `Detail`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/recepciones-mercaderia?busqueda=&estado=&ordenAbastecimientoId=&pagina=1&tamanoPagina=20` | todos opcionales | 200 `PaginaActasDto` | 400 |
| GET | `api/v1/recepciones-mercaderia/{id:int}` | — | 200 `ActaDetalleDto` | 404 |
| POST | `api/v1/recepciones-mercaderia` | `CrearActaRequestDto` | 200 `{ id }` | 400, 404, 409 |
| PUT | `api/v1/recepciones-mercaderia/{id:int}` | `ActualizarActaRequestDto` | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/recepciones-mercaderia/{id:int}` | — | 204 | 404, 409 |
| POST | `api/v1/recepciones-mercaderia/{id:int}/lineas` | `CrearLineaActaRequestDto` | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/recepciones-mercaderia/{id:int}/lineas/{lineaId:int}` | — | 204 | 404, 409 |
| POST | `api/v1/recepciones-mercaderia/{id:int}/cerrar` | sin cuerpo | 200 `{ id, estado }` | 404, 409 |
| POST | `api/v1/recepciones-mercaderia/{id:int}/anular` | `AnularActaRequestDto { motivo }` | 200 `{ id }` | 400, 404, 409 |
| GET | `api/v1/recepciones-mercaderia/pendientes/{ordenId:int}` | — | 200 `PendienteOrdenDto[]` | 404 |
| GET | `api/v1/recepciones-mercaderia/resumen` | — | 200 `ResumenActasDto` | — |

**DTOs:**
- `CrearActaRequestDto { numero, ordenAbastecimientoId, almacenId, fechaRecepcion, observaciones? }`
- `ActualizarActaRequestDto { almacenId, fechaRecepcion, observaciones? }` (número y orden no se editan)
- `CrearLineaActaRequestDto { productoId, cantidadRecibida, cantidadRechazada, motivoRechazo? }`
- `ActaListadoDto { id, numero, ordenNumero, proveedorNombre, almacenNombre, fechaRecepcion, estado, totalRecibido, totalRechazado }`
- `ActaDetalleDto { id, numero, ordenAbastecimientoId, ordenNumero, proveedorNombre, almacenId, almacenNombre, fechaRecepcion, estado, observaciones, motivoAnulacion, fechaRegistro, lineas: LineaActaDto[] { id, productoId, productoNombre, cantidadRecibida, cantidadRechazada, motivoRechazo } }`
- `PendienteOrdenDto { productoId, productoNombre, pedida, recibida, pendiente }` (una fila por producto de la orden; cuenta las actas `Conforme` y `ConObservaciones`, no los borradores ni las anuladas).
- `PaginaActasDto { items, total, pagina, tamanoPagina }`
- `ResumenActasDto { totalActas, unidadesRecibidas, unidadesRechazadas, porEstado: EstadoConteoDto[] { estado, total } }` (`porEstado` siempre trae los 4 estados en el orden de la decisión 5; las unidades suman solo actas cerradas).

**Reglas y mensajes** (400 en validación; 404 si el recurso no existe; 409 en conflictos de estado). La búsqueda del listado es sobre número del acta y número de la orden, sin distinguir mayúsculas.

*POST acta*, en este orden:
1. `numero` en blanco → 400 «El número es obligatorio.»; más de 20 → 400 «El número no puede superar 20 caracteres.» (se guarda con `Trim()` y en mayúsculas).
2. `fechaRecepcion` futura → 400 «La fecha de recepción no puede ser futura.»
3. `observaciones` más de 500 → 400 «Las observaciones no pueden superar 500 caracteres.» (en blanco → `null`).
4. `ordenAbastecimientoId` inexistente → 404 «La orden de abastecimiento {id} no existe.»; estado distinto de `Emitida` → 409 «La orden de abastecimiento {id} no está emitida.»
5. `almacenId` inexistente → 404 «El almacén {id} no existe.»; inactivo → 409 «El almacén {id} está inactivo.»
6. número repetido → 409 «Ya existe un acta con el número {numero}.» (también ante `23505`).
Nace `Borrador`.

*PUT acta:* 404 «El acta {id} no existe.»; estado distinto de `Borrador` → 409 «El acta {id} no es un borrador.»; luego las reglas 2, 3 y 5.
*DELETE acta:* 404; estado distinto de `Borrador` → 409 «El acta {id} no es un borrador y no se puede eliminar.»; borra sus líneas.
*POST línea:* 404; estado distinto de `Borrador` → 409 «El acta {id} no es un borrador.»; `productoId` ≤ 0 → 400 «El producto es obligatorio.»; cantidades fuera de 0–1000000 → 400 «Las cantidades deben estar entre 0 y 1000000.»; ambas en 0 → 400 «Indica una cantidad recibida o rechazada.»; rechazo sin motivo → 400 «El motivo de rechazo es obligatorio.» (máximo 300 → «El motivo de rechazo no puede superar 300 caracteres.»); el producto no está en la orden → 400 «El producto {productoId} no pertenece a la orden {ordenNumero}.»; producto ya en el acta → 409 «El producto {productoId} ya está en el acta.»; `cantidadRecibida` mayor al pendiente → 409 «La cantidad recibida supera lo pendiente de la orden ({pendiente}).»
*DELETE línea:* 404 «La línea {lineaId} no existe en el acta {id}.»; acta no borrador → 409.
*POST cerrar:* 404; estado distinto de `Borrador` → 409 «El acta {id} no es un borrador.»; sin líneas → 409 «El acta {id} no tiene líneas.»; vuelve a validar el pendiente de cada línea por si otra acta se cerró antes → 409 «La cantidad recibida de {producto} supera lo pendiente de la orden ({pendiente}).» Pasa a `Conforme` si ninguna línea tiene rechazo, o a `ConObservaciones` si alguna lo tiene.
*POST anular:* 404; `motivo` en blanco → 400 «El motivo de anulación es obligatorio.» (máximo 500); estado distinto de `Borrador` → 409 «El acta {id} no es un borrador y no se puede anular.»
*GET pendientes:* 404 «La orden de abastecimiento {id} no existe.»
*GET detalle:* 404 «El acta {id} no existe.» *Listado:* 400 «La página debe ser mayor o igual a 1.», «El tamaño de página debe estar entre 1 y 100.», «El estado debe ser Borrador, Conforme, ConObservaciones o Anulada.» Orden: `FechaRecepcion` descendente y luego `Id`.

**Consultas.** Desde `Repository<...>().AsQueryable()`, asíncronas y proyectadas a DTO en la consulta (sin N+1); los totales del listado salen de agregados en la misma consulta. El pendiente se calcula con una sola consulta agrupada por producto.

**Seed de desarrollo** (`seed-dev.sql`, bloque `TRUNCATE ... RESTART IDENTITY CASCADE` y `INSERT`s que buscan la orden y el almacén por número y código; va después del bloque de órdenes de abastecimiento y del de almacenes):

| Acta | Orden | Almacén | Estado | Líneas |
|---|---|---|---|---|
| AR-2026-001 | OA-DEV-001 | ALM-LIM-01 | Conforme | producto 7: recibida 2, rechazada 0 |
| AR-2026-002 | OA-DEV-002 | ALM-LIM-01 | ConObservaciones | producto 1: recibida 0, rechazada 1, motivo «Caja dañada» |
| AR-2026-003 | OA-DEV-002 | ALM-LIM-02 | Borrador | producto 1: recibida 1, rechazada 0 |
| AR-2026-004 | OA-DEV-001 | ALM-LIM-02 | Anulada («Registrada en el almacén equivocado») | producto 7: recibida 1, rechazada 0 |

Resumen esperado del seed: 4 actas, 2 unidades recibidas y 1 rechazada (solo cuentan las cerradas) y conteo por estado Borrador 1 / Conforme 1 / ConObservaciones 1 / Anulada 1. Pendientes de la orden OA-DEV-001: producto 7 pedida 3, recibida 2, pendiente 1; de OA-DEV-002: producto 1 pedida 1, recibida 0, pendiente 1.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Gate | `./scripts/verify.sh` (formato, reglas de código y capas) | en verde |
| A3 | Reglas del acta | tests: cada mensaje de alta, edición y baja de §4 con su código, literal y sin `Detail`; número guardado en mayúsculas; orden no emitida; almacén inactivo | pasan |
| A4 | Líneas | tests: producto fuera de la orden, producto repetido, cantidades inválidas, rechazo sin motivo y recibida sobre el pendiente | pasan |
| A5 | Entregas parciales | tests: dos actas sobre la misma orden suman lo recibido; lo rechazado sigue pendiente; una acta anulada o en borrador no resta pendiente; `pendientes` con números conocidos | pasan |
| A6 | Estados y consultas | tests: cerrar con y sin rechazo → `Conforme` / `ConObservaciones`, cerrar sin líneas, nueva validación del pendiente al cerrar, anular solo un borrador, filtros, paginación y resumen exacto del seed | pasan |
| A7 | Datos y PostgreSQL | el script aplica con `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh --comprobar` no da diferencias y `./scripts/smoke-postgres.sh` prueba listado, detalle, pendientes, alta, línea, cierre, anulación y duplicados | `SMOKE OK` |
| A8 | OpenAPI | `/openapi/v1.json` incluye las 11 operaciones | presentes |

## 6. Checklist de ejecución
*(Marca `[x]` solo cuando la tarea esté hecha **y** A1–A2 estén en verde.)*

- [ ] Tarea 1: script SQL, aplicarlo y generar entidades con scaffold.
- [ ] Tarea 2: DTOs, `ValidacionActaRecepcion`, casos de uso y controlador.
- [ ] Tarea 3: tests, seed y smoke.
- [ ] Tarea 4: verificar A1–A8 y anotar resultados en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| — | — | pendiente | spec sin aprobar |
