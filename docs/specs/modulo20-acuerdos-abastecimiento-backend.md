# Especificación: modulo20-acuerdos-abastecimiento-backend — AcuerdosAbastecimiento

## 1. Objetivo
**Problema:** el dominio de Compras tiene proveedores (módulo 11), solicitudes (13), órdenes de abastecimiento (15) y licitaciones (14), pero no hay forma de registrar un contrato marco con un proveedor: qué productos se compran, a qué precio acordado, hasta qué cantidad y cuánto del compromiso ya se consumió.
**Resultado esperado:** dos entidades nuevas, `AcuerdoAbastecimiento` y `LineaAcuerdoAbastecimiento`, con sus tablas y `Modulo20_AcuerdosAbastecimiento` en `api/v1/acuerdos-abastecimiento`:
- crear y editar acuerdos marco (blanket orders) con un monto máximo y un período de vigencia;
- definir las líneas del acuerdo (producto, precio acordado, cantidad comprometida);
- registrar consumos contra una línea, sin pasar de la cantidad comprometida;
- suspender, reactivar o cancelar el acuerdo;
- listado con filtros y paginación, detalle con el saldo de cada línea, y resumen.

Sigue el patrón de `Modulo14_Licitaciones` (cabecera con detalle y cambios de estado) y de `Modulo11_Proveedores`.

## 2. Fuera de alcance
- Las 4 entidades del enunciado, los middlewares y el pipeline.
- Cambios en `Modulo11_Proveedores` ni en el catálogo de productos; la baja de un proveedor o un producto con acuerdos queda bloqueada por la clave foránea `RESTRICT` (se avisa a Lukas aparte, igual que con licitaciones y evaluaciones).
- Vincular el consumo con una orden de abastecimiento (módulo 15): aquí el consumo se registra a mano; la integración llegará con su propia spec.
- Renovación automática, vencimiento automático por fecha o alertas: el estado solo cambia por acciones explícitas.
- Múltiples monedas, escalas de precio por volumen o reajustes.
- El frontend (spec `modulo20-acuerdos-abastecimiento-frontend.md`, posterior).
- Scripts de `database/` que no sean el de §3.

**Decisiones de producto que requieren aprobación antes de ejecutar:**
1. **Estados:** `Vigente`, `Suspendido` y `Cancelado`. `Vigente` ↔ `Suspendido` (suspender y reactivar); `Vigente` o `Suspendido` → `Cancelado`, que es final.
2. **El número lo escribe el usuario** (por ejemplo `AM-2026-001`), único, en mayúsculas.
3. **Un acuerdo es con un solo proveedor activo** y solo ese proveedor figura en el acuerdo.
4. **El compromiso total** (suma de precio acordado × cantidad comprometida de las líneas) **no puede superar el monto máximo** del acuerdo.
5. **Un producto aparece una sola vez por acuerdo.**
6. **Solo se consume con el acuerdo `Vigente` y dentro de su período** (entre fecha de inicio y fecha de fin). Pasada la fecha de fin el acuerdo sigue `Vigente`, pero no admite más consumos.
7. **Solo se eliminan acuerdos o líneas sin consumo;** con consumo, el acuerdo se cancela.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `database/010-acuerdos-abastecimiento.sql` | crear | `CREATE TABLE "AcuerdosAbastecimiento"` y `"LineasAcuerdoAbastecimiento"`; número `010` (siguiente libre de `docs/equipo/relevo.md` §8, que se actualiza en el mismo PR) |
| `docs/equipo/relevo.md` | modificar | añadir la fila 010 a la tabla de §8 |
| `src/TitanGlobal.Domain/Models/AcuerdoAbastecimiento.cs`, `LineaAcuerdoAbastecimiento.cs`, `Proveedor.cs`, `Producto.cs` | regenerar | entidades `partial` generadas por `./scripts/scaffold.sh` (no se editan a mano) |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | `DbSet<AcuerdoAbastecimiento>` y `DbSet<LineaAcuerdoAbastecimiento>`, generados |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | modificar | solo si el scaffold nombra mal las entidades (deben quedar `AcuerdoAbastecimiento` / `AcuerdosAbastecimiento` y `LineaAcuerdoAbastecimiento` / `LineasAcuerdoAbastecimiento`) |
| `src/TitanGlobal.Application/DTOs/Modulo20_AcuerdosAbastecimiento/*.cs` | crear | DTOs de entrada y salida (§4) |
| `src/TitanGlobal.Application/UseCases/Modulo20_AcuerdosAbastecimiento/*.cs` | crear | un caso de uso por operación y `ValidacionAcuerdoAbastecimiento` |
| `src/TitanGlobal.Api/Controllers/Modulo20_AcuerdosAbastecimiento/AcuerdosAbastecimientoController.cs` | crear | `[Route("api/v1/acuerdos-abastecimiento")]`, controlador delgado; las acciones sin cuerpo llevan `[PermitirCuerpoVacio]` (spec 18) |
| `tests/TitanGlobal.Api.Tests/Modulo20_AcuerdosAbastecimiento/AcuerdosAbastecimientoApiTests.cs` | crear | pruebas de integración con InMemory, datos propios del test |
| `scripts/seed-dev.sql` | modificar | 4 acuerdos y 5 líneas idempotentes al final (§4) |
| `scripts/smoke-postgres.sh` | modificar | rutas del módulo (§5 A7) |

## 4. Diseño y lógica
**Tabla `AcuerdosAbastecimiento`:**

| Columna | Tipo | Regla |
|---|---|---|
| `Id` | `integer` identidad | clave primaria |
| `Numero` | `character varying(20) NOT NULL` | único (`IX_AcuerdosAbastecimiento_Numero`) |
| `ProveedorId` | `integer NOT NULL` | clave foránea a `Proveedores("Id")` `ON DELETE RESTRICT` |
| `Descripcion` | `character varying(500)` | opcional |
| `FechaInicio` | `timestamp with time zone NOT NULL` | UTC |
| `FechaFin` | `timestamp with time zone NOT NULL` | UTC; `CHECK` posterior a `FechaInicio` |
| `MontoMaximo` | `numeric(18,2) NOT NULL` | `CHECK` > 0 |
| `Estado` | `character varying(20) NOT NULL` | `CHECK` en (`Vigente`, `Suspendido`, `Cancelado`) |
| `FechaRegistro` | `timestamp with time zone NOT NULL` | UTC, la pone el servidor |

**Tabla `LineasAcuerdoAbastecimiento`:**

| Columna | Tipo | Regla |
|---|---|---|
| `Id` | `integer` identidad | clave primaria |
| `AcuerdoId` | `integer NOT NULL` | clave foránea a `AcuerdosAbastecimiento("Id")` `ON DELETE RESTRICT` |
| `ProductoId` | `integer NOT NULL` | clave foránea a `Productos("Id")` `ON DELETE RESTRICT` |
| `PrecioAcordado` | `numeric(18,2) NOT NULL` | `CHECK` > 0 |
| `CantidadComprometida` | `integer NOT NULL` | `CHECK` entre 1 y 1000000 |
| `CantidadConsumida` | `integer NOT NULL` | `CHECK` >= 0 y <= `CantidadComprometida` |

Índice único `IX_LineasAcuerdoAbastecimiento_AcuerdoId_ProductoId`. Los `CHECK` siguen la convención `CK_<Tabla>_<Regla>`.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }` sin `Detail`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/acuerdos-abastecimiento?busqueda=&estado=&proveedorId=&pagina=1&tamanoPagina=20` | todos opcionales | 200 `PaginaAcuerdosDto` | 400 |
| GET | `api/v1/acuerdos-abastecimiento/{id:int}` | — | 200 `AcuerdoDetalleDto` (con líneas) | 404 |
| POST | `api/v1/acuerdos-abastecimiento` | `CrearAcuerdoRequestDto` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/acuerdos-abastecimiento/{id:int}` | `ActualizarAcuerdoRequestDto` | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/acuerdos-abastecimiento/{id:int}` | — | 204 | 404, 409 |
| POST | `api/v1/acuerdos-abastecimiento/{id:int}/lineas` | `CrearLineaRequestDto` | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/acuerdos-abastecimiento/{id:int}/lineas/{lineaId:int}` | — | 204 | 404, 409 |
| POST | `api/v1/acuerdos-abastecimiento/{id:int}/lineas/{lineaId:int}/consumos` | `RegistrarConsumoRequestDto { cantidad }` | 200 `{ id }` | 400, 404, 409 |
| POST | `api/v1/acuerdos-abastecimiento/{id:int}/suspender` | sin cuerpo | 200 `{ id }` | 404, 409 |
| POST | `api/v1/acuerdos-abastecimiento/{id:int}/reactivar` | sin cuerpo | 200 `{ id }` | 404, 409 |
| POST | `api/v1/acuerdos-abastecimiento/{id:int}/cancelar` | sin cuerpo | 200 `{ id }` | 404, 409 |
| GET | `api/v1/acuerdos-abastecimiento/resumen` | — | 200 `ResumenAcuerdosDto` | — |

**DTOs:**
- `CrearAcuerdoRequestDto { numero, proveedorId, descripcion?, fechaInicio, fechaFin, montoMaximo }`
- `ActualizarAcuerdoRequestDto { descripcion?, fechaInicio, fechaFin, montoMaximo }` (número y proveedor no se editan)
- `CrearLineaRequestDto { productoId, precioAcordado, cantidadComprometida }`
- `RegistrarConsumoRequestDto { cantidad }`
- `AcuerdoListadoDto { id, numero, proveedorNombre, estado, fechaInicio, fechaFin, montoMaximo, montoComprometido, montoConsumido }`
- `AcuerdoDetalleDto { id, numero, proveedorId, proveedorNombre, descripcion, estado, fechaInicio, fechaFin, montoMaximo, montoComprometido, montoConsumido, fechaRegistro, lineas: LineaAcuerdoDto[] }`, con `LineaAcuerdoDto { id, productoId, productoNombre, precioAcordado, cantidadComprometida, cantidadConsumida, saldo }` ordenadas por `id`.
- `PaginaAcuerdosDto { items, total, pagina, tamanoPagina }`
- `ResumenAcuerdosDto { totalAcuerdos, montoMaximoVigente, montoConsumido, porEstado: EstadoConteoDto[] { estado, total } }` (siempre Vigente, Suspendido, Cancelado, en ese orden).

`montoComprometido` = suma de `precioAcordado × cantidadComprometida`; `montoConsumido` = suma de `precioAcordado × cantidadConsumida`; `saldo` = `cantidadComprometida − cantidadConsumida`.

**Reglas y mensajes** (400 en validación; 404 si el recurso no existe; 409 en conflictos de estado). La búsqueda del listado es sobre número y nombre del proveedor, sin distinguir mayúsculas.

*POST acuerdo*, en este orden:
1. `numero` en blanco → 400 «El número es obligatorio.»; más de 20 caracteres → 400 «El número no puede superar 20 caracteres.» (se guarda con `Trim()` y en mayúsculas).
2. `proveedorId` ≤ 0 → 400 «El proveedor es obligatorio.»; inexistente → 400 «El proveedor {id} no existe.»; inactivo → 400 «El proveedor {id} está inactivo.»
3. `descripcion` más de 500 → 400 «La descripción no puede superar 500 caracteres.» (en blanco → `null`).
4. `montoMaximo` ≤ 0 → 400 «El monto máximo debe ser mayor a 0.»; con más de 2 decimales o más de 16 dígitos enteros → 400 «El monto máximo no es válido.»
5. `fechaFin` menor o igual que `fechaInicio` → 400 «La fecha de fin debe ser posterior a la de inicio.»; `fechaFin` no futura → 400 «La fecha de fin debe ser futura.»
6. número repetido → 409 «Ya existe un acuerdo con el número {numero}.» (también ante `23505`).
El acuerdo nace `Vigente`.

*PUT acuerdo:* id inexistente → 404 «El acuerdo {id} no existe.»; estado `Cancelado` → 409 «El acuerdo {id} está cancelado.»; luego las reglas 3, 4 y 5 del alta; si el nuevo monto máximo es menor al compromiso de las líneas → 400 «El monto máximo no puede ser menor al compromiso de las líneas.»

*DELETE acuerdo:* 404 si no existe; con alguna línea consumida → 409 «El acuerdo {id} tiene consumos y no se puede eliminar; cancélelo.» Sin consumos, elimina el acuerdo y sus líneas en un solo guardado.

*POST línea:* 404 «El acuerdo {id} no existe.»; estado `Cancelado` → 409 «El acuerdo {id} está cancelado.»; `productoId` ≤ 0 → 400 «El producto es obligatorio.»; precio ≤ 0 → 400 «El precio acordado debe ser mayor a 0.»; precio con más de 2 decimales o más de 16 dígitos enteros → 400 «El precio acordado no es válido.»; cantidad ≤ 0 → 400 «La cantidad comprometida debe ser mayor a 0.»; cantidad mayor a 1000000 → 400 «La cantidad comprometida no puede superar 1000000.»; producto inexistente → 400 «El producto {id} no existe.»; producto ya en el acuerdo → 409 «El producto {productoId} ya está en el acuerdo {id}.» (también ante `23505`); compromiso total mayor al monto máximo → 400 «El compromiso de las líneas supera el monto máximo del acuerdo.»

*DELETE línea:* 404 «El acuerdo {id} no existe.» / 404 «La línea {lineaId} no existe en el acuerdo {id}.»; estado `Cancelado` → 409 «El acuerdo {id} está cancelado.»; línea con consumo → 409 «La línea {lineaId} tiene consumos y no se puede eliminar.»

*POST consumo:* 404 «El acuerdo {id} no existe.» / 404 «La línea {lineaId} no existe en el acuerdo {id}.»; estado distinto de `Vigente` → 409 «El acuerdo {id} no está vigente.»; `fechaInicio` futura → 409 «El acuerdo {id} aún no inicia.»; `fechaFin` pasada → 409 «El acuerdo {id} ya venció.»; cantidad ≤ 0 → 400 «La cantidad debe ser mayor a 0.»; cantidad mayor al saldo → 409 «La cantidad supera el saldo de la línea ({saldo}).» Suma la cantidad a `CantidadConsumida`.

*POST suspender:* 404; estado distinto de `Vigente` → 409 «El acuerdo {id} no está vigente.»

*POST reactivar:* 404; estado distinto de `Suspendido` → 409 «El acuerdo {id} no está suspendido.»

*POST cancelar:* 404; estado `Cancelado` → 409 «El acuerdo {id} ya está cancelado.»

*GET detalle:* 404 «El acuerdo {id} no existe.» *Listado:* 400 «La página debe ser mayor o igual a 1.», «El tamaño de página debe estar entre 1 y 100.», 400 «El estado debe ser Vigente, Suspendido o Cancelado.» (filtro sin distinguir mayúsculas). Orden: `FechaRegistro` descendente, después `Id` descendente.

**Consultas.** Desde `Repository<...>().AsQueryable()`, asíncronas y proyectadas a DTO en la consulta (sin N+1); `montoComprometido` y `montoConsumido` salen de sumas en la misma consulta. Las acciones de estado y de consumo cargan la entidad, validan y guardan con `CompleteAsync()` (un solo guardado por acción).

**Seed de desarrollo** (`seed-dev.sql`, bloque `TRUNCATE ... RESTART IDENTITY` con ids explícitos y `setval`, igual que el resto del archivo). Las fechas de fin de los acuerdos vigentes están en 2099 para que el smoke no dependa de la fecha de ejecución.

| Id | Número | Proveedor | Estado | Inicio | Fin | Monto máximo |
|---|---|---|---|---|---|---|
| 1 | AM-2026-001 | 1 | Vigente | 2026-01-01 | 2099-12-31 | 100000.00 |
| 2 | AM-2026-002 | 2 | Vigente | 2026-03-01 | 2099-12-31 | 60000.00 |
| 3 | AM-2026-003 | 3 | Suspendido | 2026-06-01 | 2099-12-31 | 40000.00 |
| 4 | AM-2026-004 | 4 | Cancelado | 2026-01-01 | 2026-06-30 | 25000.00 |

| Acuerdo | Producto | Precio acordado | Comprometida | Consumida |
|---|---|---|---|---|
| 1 | 1 | 50.00 | 1000 | 400 |
| 1 | 2 | 80.00 | 500 | 0 |
| 2 | 3 | 120.00 | 300 | 300 |
| 3 | 1 | 55.00 | 200 | 50 |
| 4 | 4 | 30.00 | 500 | 100 |

Resumen esperado del seed: 4 acuerdos, monto máximo vigente 160000.00, monto consumido 61750.00 y conteo por estado Vigente 2 / Suspendido 1 / Cancelado 1.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Gate | `./scripts/verify.sh` (formato, reglas de código y capas) | en verde |
| A3 | Reglas del acuerdo | tests: cada mensaje de alta, edición y baja de §4 con su código, literal y sin `Detail`; número guardado en mayúsculas y sin espacios; descripción en blanco → `null` | pasan |
| A4 | Reglas de líneas | tests: línea válida, producto inexistente, producto repetido, precio y cantidad inválidos, compromiso sobre el monto máximo, acuerdo cancelado, baja de línea con y sin consumo | pasan |
| A5 | Consumos | tests: consumo válido que reduce el saldo, cantidad sobre el saldo, acuerdo suspendido o cancelado, acuerdo no iniciado y acuerdo vencido, cada uno con su literal | pasan |
| A6 | Estados | tests: suspender → reactivar → cancelar y cada transición inválida con su 409 literal | pasan |
| A7 | Consultas | tests con datos conocidos: filtros por estado, proveedor y búsqueda, paginación con `total`, montos comprometido y consumido, saldos del detalle y resumen exacto del seed | pasan |
| A8 | Datos y PostgreSQL | el script aplica con `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh --comprobar` no da diferencias y `./scripts/smoke-postgres.sh` prueba listado, filtro por estado, detalle, POST de acuerdo y de línea, consumo, suspensión, un 409 literal, DELETE del acuerdo creado y resumen del seed | `SMOKE OK` |
| A9 | OpenAPI | `/openapi/v1.json` incluye las 12 operaciones | presentes |

## 6. Checklist de ejecución
*(Marca `[x]` solo cuando la tarea esté hecha **y** A1–A2 estén en verde.)*

- [ ] Tarea 1: script SQL y fila 010 en `relevo.md`, aplicarlo y generar entidades con scaffold.
- [ ] Tarea 2: DTOs, `ValidacionAcuerdoAbastecimiento`, casos de uso y controlador.
- [ ] Tarea 3: tests, seed y smoke.
- [ ] Tarea 4: verificar A1–A9 y anotar resultados en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| | | | |
