# Especificación: 41 - Modulo72_RolesPermisos (backend)

## 1. Objetivo
**Problema:** no hay forma de definir qué puede hacer cada perfil de usuario en cada uno de los 80 módulos del ERP.
**Resultado esperado:** entidades nuevas `Rol` y `PermisoRol` y `Modules/Modulo72_RolesPermisos/` en `api/v1/roles`:
- roles con código, nombre y descripción;
- **matriz de permisos** por rol y módulo (leer / escribir) sobre el catálogo de los 80 módulos;
- listado, detalle, alta, edición, baja y una vista de la matriz completa.

Decisión de Lukas (2026-10-15): roles y permisos se gestionan **como datos**; el middleware de roles del enunciado (cabecera `X-User-Role`, 403 con su mensaje literal) **no cambia**.

## 2. Fuera de alcance
- Cambiar `RoleBasedAccessMiddleware`, `IAuthService` o el comportamiento del 403 (pieza evaluada).
- Usuarios y asignación de roles a usuarios (módulo 71, spec 43).
- El frontend (spec 42).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Reglas tomadas por la integración: un permiso de escritura implica el de lectura; los roles de sistema (`ADMIN`) no se pueden borrar ni perder permisos; un módulo sin fila de permiso equivale a «sin acceso».

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Models/{Rol,PermisoRol}.cs` | crear | — |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | `DbSet`s; índices únicos `Rol.Codigo` y (`PermisoRol.RolId`, `ModuloNumero`); cascada de rol a permisos |
| `src/TitanGlobal.Api/Migrations/*_RolesPermisos*.cs` + snapshot | crear | `dotnet ef migrations add RolesPermisos -p src/TitanGlobal.Api` |
| `src/TitanGlobal.Api/Modules/Modulo72_RolesPermisos/{Controllers,Dtos,UseCases}/` | crear | `[Route("api/v1/roles")]`; helper estático `ValidacionRol`; catálogo estático de los 80 módulos (número, nombre y dominio) copiado de `docs/arquitectura.md` en `CatalogoModulos.cs` dentro del módulo |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | modificar | §4 y §5 A5 |
| `tests/TitanGlobal.Api.Tests/Modulo72_RolesPermisos/*.cs` | crear | InMemory, datos propios; un test comprueba que `CatalogoModulos` tiene los 80 módulos con los nombres de `docs/arquitectura.md` |

## 4. Diseño y lógica
**Entidades.**
- `Rol`: `Id`, `Codigo` (requerido, máx. 30, mayúsculas, único), `Nombre` (requerido, máx. 100), `Descripcion` (opcional, máx. 300), `EsSistema` (bool; solo el seed lo pone a `true`).
- `PermisoRol`: `Id`, `RolId`, `ModuloNumero` (1–80), `PuedeLeer` (bool), `PuedeEscribir` (bool). Solo se guardan filas con al menos un permiso.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/roles?busqueda=&pagina=1&tamanoPagina=20` | opcionales salvo paginación | 200 `PaginaRolesDto { items: RolListadoDto[], total, pagina, tamanoPagina }` | 400 paginación |
| GET | `api/v1/roles/{id:int}` | — | 200 `RolDetalleDto` | 404 «El rol {id} no existe.» |
| POST | `api/v1/roles` | `GuardarRolRequestDto { codigo, nombre, descripcion? }` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/roles/{id:int}` | igual | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/roles/{id:int}` | — | 204 | 404, 409 |
| GET | `api/v1/roles/{id:int}/permisos` | — | 200 `PermisoModuloDto[]` (los 80 módulos, con `false/false` si no hay fila) | 404 |
| PUT | `api/v1/roles/{id:int}/permisos` | `GuardarPermisosRequestDto { permisos: [{ moduloNumero, puedeLeer, puedeEscribir }] }` (reemplaza todos) | 200 `PermisoModuloDto[]` | 400, 404, 409 |
| GET | `api/v1/roles/matriz` | — | 200 `MatrizPermisosDto` | — |

**DTOs.**
- `RolListadoDto { id, codigo, nombre, esSistema, modulosConLectura, modulosConEscritura }`; `RolDetalleDto` = listado + `descripcion`.
- `PermisoModuloDto { moduloNumero, moduloNombre, dominio, puedeLeer, puedeEscribir }` en orden de módulo.
- `MatrizPermisosDto { roles: RolResumenDto[], modulos: ModuloMatrizDto[] }` con `RolResumenDto { id, codigo }` por código y `ModuloMatrizDto { moduloNumero, moduloNombre, dominio, permisos: string[] }` donde cada elemento, en el orden de `roles`, es `"Escritura"`, `"Lectura"` o `"Ninguno"`.

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden:**
1. `codigo` tras `Trim()` y en mayúsculas: vacío → 400 «El código es obligatorio.»; no cumple `^[A-Z0-9_-]{2,30}$` → 400 «El código solo admite letras, números, guiones y guion bajo (2 a 30 caracteres).»
2. `nombre` tras `Trim()`: vacío → 400 «El nombre del rol es obligatorio.»; más de 100 → 400 «El nombre del rol no puede superar 100 caracteres.»
3. `descripcion` en blanco → `null`; más de 300 → 400 «La descripción no puede superar 300 caracteres.»
4. Código repetido → 409 «Ya existe un rol con el código {codigo}.» (también en la carrera, `23505`; PUT con su propio código válido).
5. Id inexistente → 404 «El rol {id} no existe.»
6. Rol de sistema: cambiar su código, borrarlo o quitarle algún permiso → 409 «El rol {codigo} es de sistema y no se puede modificar de esa forma.» (sí se puede cambiar su nombre y descripción).
7. Permisos: módulo fuera de 1–80 → 400 «El módulo {n} no existe.»; módulo repetido → 400 «El módulo {n} está repetido.»; escritura sin lectura → 400 «El módulo {n} no puede tener escritura sin lectura.»
8. Borrar un rol que tiene usuarios (cuando exista el módulo 71) → 409 «No se puede eliminar el rol {codigo} porque tiene usuarios.» (en esta spec no hay usuarios; la regla la añade la spec 43).

**Consultas.** Desde `Repository<T>().AsQueryable()`, asíncronas, proyectadas, sin N+1 (la matriz con un número fijo de consultas). Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08.

**Seed:** 5 roles: `ADMIN` (de sistema; escritura en los 80 módulos), `VENTAS` (escritura 01–10, lectura 21), `COMPRAS` (escritura 11–20, lectura 21–24), `ALMACEN` (escritura 21–30, lectura 11–20) y `CONSULTA` (lectura en todos).

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: un caso por cada mensaje de §4 (reglas 1–7) con su código, literal y sin `Detail`; ningún caso inválido crea o cambia filas | pasan |
| A3 | Permisos y consultas | tests: `GET permisos` devuelve los 80 módulos; `PUT permisos` reemplaza (los módulos omitidos quedan sin acceso); `modulosConLectura` cuenta también los de escritura; matriz exacta; catálogo de 80 módulos igual a `docs/arquitectura.md` | pasan |
| A4 | Sin cambios en seguridad | los tests existentes del 403 (`X-User-Role: Bloqueado` y `SinPermiso`) siguen en verde sin cambios; `git diff` no toca `Middlewares/` ni `Services/` | 0 archivos |
| A5 | PostgreSQL | smoke ampliado: listado, detalle de VENTAS, permisos de VENTAS (80 filas), matriz, POST válido, PUT permisos con escritura sin lectura → 400, borrar ADMIN → 409, DELETE del creado → 204 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 8 operaciones | presentes |

## 6. Checklist de ejecución
- [x] Tarea 1: entidades, `DbContext`, migración y catálogo de módulos.
- [x] Tarea 2: DTOs, UseCases y controlador.
- [x] Tarea 3: tests, seed y smoke.
- [x] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` rc=0 en `ImLukzy/Claudio` (`d8510ff`, mismas líneas que el auditado `d58712b`): backend 543/543, frontend 232/232 |
| 2026-10-15 | A2–A3 | ✅ | `RolesPermisosApiTests` (20 tests): cada mensaje literal, rol de sistema protegido, escritura sin lectura, PUT que reemplaza, 80 módulos en permisos, matriz y catálogo igual a `docs/arquitectura.md` |
| 2026-10-15 | A4 | ✅ | el commit no toca `Middlewares/` ni `Services/` (0 archivos); el smoke sigue devolviendo 403 literal con `X-User-Role: Bloqueado` |
| 2026-10-15 | A5 | ✅ | `./scripts/smoke-postgres.sh` → `SMOKE OK`: listado (ADMIN 80/80), detalle de VENTAS, permisos de VENTAS (80 filas), matriz, POST → `{id:6}`, borrar ADMIN → 409 literal, DELETE del creado → 204 |
| 2026-10-15 | A6 | ✅ | el smoke valida las operaciones de `/openapi/v1.json` |
| 2026-10-15 | Auditoría | ✅ | Auditoría, ciclo 1: APROBADO sin hallazgos |
