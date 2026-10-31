# Especificación: 12 - Modulo11_Proveedores (backend)

## 1. Objetivo
**Problema:** el dominio de Compras y Abastecimiento (módulos 11–20) no tiene ningún maestro. Sin proveedores no se pueden construir después solicitudes de compra, órdenes de abastecimiento ni importaciones.
**Resultado esperado:** una entidad nueva `Proveedor` con su tabla y `Modules/Modulo11_Proveedores/` en `api/v1/proveedores`:
- listado con búsqueda, filtros y paginación;
- detalle;
- alta, modificación y baja;
- resumen.

Sigue el patrón de `Modulo01_Clientes` (spec 07). Decisión del humano (2026-10-14): el siguiente módulo es 11 Proveedores.

## 2. Fuera de alcance
- Cambios en las 4 entidades del enunciado (`Cliente`, `Producto`, `OrdenCompra`, `DetalleOrdenCompra`).
- Relaciones con otros módulos (evaluación, órdenes de abastecimiento, importaciones): llegarán con sus specs.
- El frontend (spec 13).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Campos y reglas tomados por la integración con el criterio de Clientes: RUC único de 11 dígitos; la baja es física mientras ningún módulo dependa del proveedor; `Activo` sirve para retirarlo sin borrarlo.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Models/Proveedor.cs` | crear | entidad `partial`, estilo de `Cliente.cs` |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | `DbSet<Proveedor> Proveedores`; índice único en `Ruc`; longitudes máximas (§4) |
| `src/TitanGlobal.Api/Migrations/*_Proveedores*.cs` + snapshot | crear | generada con `dotnet ef migrations add Proveedores -p src/TitanGlobal.Api` (nunca a mano) |
| `src/TitanGlobal.Api/Modules/Modulo11_Proveedores/{Controllers,Dtos,UseCases}/` | crear | un archivo por clase; `[Route("api/v1/proveedores")]`, controlador delgado |
| `scripts/seed-dev.sql` | modificar | 6 proveedores idempotentes (§4) |
| `scripts/smoke-postgres.sh` | modificar | rutas de proveedores (§5 A5) |
| `tests/TitanGlobal.Api.Tests/Modulo11_Proveedores/*.cs` | crear | tests de integración con InMemory, datos propios del test |

## 4. Diseño y lógica
**Entidad `Proveedor`:** `Id`, `RazonSocial` (requerida, máx. 200), `Ruc` (requerido, 11, índice único), `Pais` (requerido, máx. 100), `Email` (opcional, máx. 150), `Telefono` (opcional, máx. 20), `Activo` (bool, por defecto `true`).

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/proveedores?busqueda=&pais=&activo=&pagina=1&tamanoPagina=20` | todos opcionales salvo paginación (esos valores por defecto); `activo` = `true`/`false` | 200 `PaginaProveedoresDto { items: ProveedorListadoDto[], total, pagina, tamanoPagina }` | 400 si `pagina < 1` o `tamanoPagina` fuera de 1–100 |
| GET | `api/v1/proveedores/{id:int}` | — | 200 `ProveedorDetalleDto` | 404 «El proveedor {id} no existe.» |
| POST | `api/v1/proveedores` | `GuardarProveedorRequestDto { razonSocial, ruc, pais, email?, telefono?, activo }` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/proveedores/{id:int}` | `GuardarProveedorRequestDto` | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/proveedores/{id:int}` | — | 204 | 404 |
| GET | `api/v1/proveedores/paises` | — | 200 `string[]` (países distintos, ordenados) | — |
| GET | `api/v1/proveedores/resumen` | — | 200 `ResumenProveedoresDto` | — |

**DTOs.**
- `ProveedorListadoDto { id, razonSocial, ruc, pais, activo }`
- `ProveedorDetalleDto { id, razonSocial, ruc, pais, email, telefono, activo }`
- `ResumenProveedoresDto { totalProveedores, activos, inactivos, sinEmail, porPais: PaisProveedoresDto[] }`
- `PaisProveedoresDto { pais, totalProveedores, activos }`, ordenado por `totalProveedores` descendente y después `pais`.

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden:**
1. `razonSocial` tras `Trim()`: vacía → 400 «La razón social es obligatoria.»; más de 200 → 400 «La razón social no puede superar 200 caracteres.»
2. `ruc` tras `Trim()`: exactamente 11 dígitos → si no, 400 «El RUC debe tener 11 dígitos numéricos.»
3. `pais` tras `Trim()`: vacío → 400 «El país es obligatorio.»; más de 100 → 400 «El país no puede superar 100 caracteres.»
4. `email` tras `Trim()`, en blanco → `null`; si viene: máx. 150 y con formato `texto@texto.texto` sin espacios → si no, 400 «El email no tiene un formato válido.»
5. `telefono` tras `Trim()`, en blanco → `null`; si viene: 6–20 caracteres de dígitos, espacios, `+` o `-` → si no, 400 «El teléfono no tiene un formato válido.»
6. RUC usado por otro proveedor → 409 «Ya existe un proveedor con el RUC {ruc}.» También en la carrera (`23505`). Un PUT que mantiene su propio RUC es válido.
7. Id inexistente en GET, PUT o DELETE → 404 «El proveedor {id} no existe.»

**Consultas.** Desde `Repository<Proveedor>().AsQueryable()`, asíncronas y proyectadas a DTO en la consulta.
- `busqueda` filtra sin distinguir mayúsculas por `RazonSocial` o `Ruc`; `pais` exacto sin distinguir mayúsculas; `activo` si viene.
- Orden por `RazonSocial` y después `Id`.
- Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08 (filtro de texto, `GroupBy`, `Distinct`), sin forzarlos.

**Seed de desarrollo** (`seed-dev.sql`, idempotente con `ON CONFLICT` sobre el RUC): 6 proveedores en 3 países (Perú ×3, Chile ×2, China ×1), uno inactivo y dos sin email.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: cada mensaje de §4 con su código, literal y sin `Detail`; RUC con espacios aceptado y guardado sin ellos; email y teléfono en blanco → `null`; PUT con su propio RUC → 200 | pasan |
| A3 | Consultas | tests con datos conocidos: búsqueda por razón social y por RUC, filtro por país (sin distinguir mayúsculas) y por `activo`, paginación con `total`, países ordenados, resumen exacto con el orden de `porPais` | pasan |
| A4 | Migración | `Proveedores` generada con `dotnet ef` (`Designer` + snapshot); `dotnet ef database update` sobre la BD con el seed aplica sin errores; no cambia las tablas existentes | evidencia |
| A5 | PostgreSQL | `./scripts/smoke-postgres.sh` ampliado: listado con `pais=peru`, `activo=false`, detalle, países, resumen, POST válido, POST con RUC del seed → 409, DELETE del recién creado → 204 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 7 operaciones | presentes |

## 6. Checklist de ejecución
- [x] Tarea 1: entidad, `DbContext` y migración.
- [x] Tarea 2: DTOs, UseCases y controlador.
- [x] Tarea 3: tests, seed y smoke.
- [x] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-14 | A1 | ✅ | `./scripts/verify.sh` rc=0 en la rama de integración (`30a211f`, idéntico al auditado `4837215` según `git patch-id`): backend 111/111, frontend 77/77. Un primer intento falló por 2 tests intermitentes del frontend (Clientes y Productos) con la máquina cargada; pasan al repetir y se corrigen aparte |
| 2026-10-14 | A2–A3 | ✅ | `ProveedoresApiTests`: mensajes literales con su código y sin `Detail`, RUC con espacios, email y teléfono en blanco → `null`, PUT con su propio RUC; búsqueda, país, `activo`, paginación, países ordenados y resumen exacto |
| 2026-10-14 | A4 | ✅ | `20261015023548_Proveedores` (con `Designer` y snapshot) crea solo `Proveedores` e `IX_Proveedores_Ruc`; el smoke la aplica sobre la BD con el seed sin errores |
| 2026-10-14 | A5 | ✅ | `./scripts/smoke-postgres.sh` → `SMOKE OK`: `pais=peru` → 3, `activo=false` → 1, detalle 1, países `["Chile","China","Peru"]`, resumen 6/5/1/2, POST → `{id:7}` (secuencia bien tras el seed), RUC del seed → 409 literal, DELETE del creado → 204 |
| 2026-10-14 | A6 | ✅ | `/openapi/v1.json`: `proveedores` (GET, POST), `proveedores/{id}` (GET, PUT, DELETE), `proveedores/paises` (GET), `proveedores/resumen` (GET) |
| 2026-10-14 | Auditoría | ✅ | Auditoría, ciclo 1: APROBADO |
| 2026-10-14 | Tests intermitentes | ✅ | La implementación los hizo deterministas (`645b5ff`, `a428841`): 10/10 sin carga y 10/10 con 4 procesos de carga; gate rc=0 (111/111, 96/96) |
| 2026-10-14 | QA E2E | ✅ | QA con frontend (Vite 5174) + API + PostgreSQL en `d0bb258` (migración Proveedores aplicada): resumen del seed 6/5/1/2 y por país; búsqueda por razón social y por RUC, país, «Inactivos» → Servicios Andinos SpA; detalle con y sin email; validación local con literales sin petición; RUC 20600000001 → 409 conservando lo escrito; edición a Inactivo refleja tabla, detalle y resumen; baja con Escape; Clientes, Productos y OrdenesCompra sin regresiones; márgenes ≥ 16 px, sin scroll horizontal ni celdas recortadas |
