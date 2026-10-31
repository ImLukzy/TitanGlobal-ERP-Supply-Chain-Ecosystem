# Especificación: 43 - Modulo71_Usuarios (backend)

## 1. Objetivo
**Problema:** los roles y permisos (módulo 72) no se pueden asignar a nadie: no hay usuarios.
**Resultado esperado:** entidades nuevas `Usuario` y `UsuarioRol` y `Modules/Modulo71_Usuarios/` en `api/v1/usuarios`:
- usuarios con nombre de usuario, nombre completo, email, estado y uno o varios roles;
- **permisos efectivos** de un usuario por módulo (unión de los de sus roles);
- listado, detalle, alta, edición, baja y resumen.

Decisión de Lukas (2026-10-15): usuarios y roles se gestionan **como datos**; no hay contraseñas ni inicio de sesión y el middleware de roles del enunciado (cabecera `X-User-Role`, 403 literal) **no cambia**.

## 2. Fuera de alcance
- Contraseñas, inicio de sesión, tokens o cambios en `Middlewares/` y `Services/`.
- Auditoría de acciones de los usuarios (módulo 77).
- El frontend (spec 44).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Reglas tomadas por la integración: siempre debe quedar al menos un usuario activo con el rol `ADMIN`; un rol con usuarios no se puede borrar (regla 8 de la spec 41, que se activa aquí).

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Models/{Usuario,UsuarioRol}.cs` | crear | `Rol` gana la colección `Usuarios` |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | `DbSet`s; índices únicos `NombreUsuario`, `Email` y (`UsuarioId`, `RolId`); FK restrict a `Rol`, cascada de usuario a sus roles |
| `src/TitanGlobal.Api/Migrations/*_Usuarios*.cs` + snapshot | crear | `dotnet ef migrations add Usuarios -p src/TitanGlobal.Api` |
| `src/TitanGlobal.Api/Modules/Modulo71_Usuarios/{Controllers,Dtos,UseCases}/` | crear | `[Route("api/v1/usuarios")]`; helper estático `ValidacionUsuario`; reutiliza `CatalogoModulos` del módulo 72 |
| `src/TitanGlobal.Api/Modules/Modulo72_RolesPermisos/UseCases/EliminarRolUseCase.cs` (+ test) | modificar | regla 8 de la spec 41: 409 si el rol tiene usuarios |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | modificar | §4 y §5 A5 |
| `tests/TitanGlobal.Api.Tests/Modulo71_Usuarios/*.cs` | crear | InMemory, datos propios |

## 4. Diseño y lógica
**Entidades.**
- `Usuario`: `Id`, `NombreUsuario` (requerido, máx. 30, en minúsculas, único), `NombreCompleto` (requerido, máx. 100), `Email` (requerido, máx. 150, en minúsculas, único), `Activo` (bool), `FechaAlta` (UTC, del servidor).
- `UsuarioRol`: `Id`, `UsuarioId`, `RolId`.
- **Permiso efectivo** de un módulo: «Escritura» si algún rol del usuario tiene escritura; si no, «Lectura» si alguno tiene lectura; si no, «Ninguno». Un usuario inactivo tiene «Ninguno» en todo.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/usuarios?busqueda=&rolId=&activo=&pagina=1&tamanoPagina=20` | opcionales salvo paginación | 200 `PaginaUsuariosDto { items: UsuarioListadoDto[], total, pagina, tamanoPagina }` | 400 paginación |
| GET | `api/v1/usuarios/{id:int}` | — | 200 `UsuarioDetalleDto` | 404 «El usuario {id} no existe.» |
| POST | `api/v1/usuarios` | `GuardarUsuarioRequestDto { nombreUsuario, nombreCompleto, email, activo, rolIds: int[] }` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/usuarios/{id:int}` | igual (reemplaza los roles) | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/usuarios/{id:int}` | — | 204 | 404, 409 |
| GET | `api/v1/usuarios/{id:int}/permisos` | — | 200 `PermisoEfectivoDto[]` (80 módulos) | 404 |
| GET | `api/v1/usuarios/resumen` | — | 200 `ResumenUsuariosDto` | — |

**DTOs.**
- `UsuarioListadoDto { id, nombreUsuario, nombreCompleto, email, activo, roles: string[] }` (códigos de rol ordenados); orden por `NombreUsuario`.
- `UsuarioDetalleDto` = listado + `fechaAlta, rolIds: int[], modulosConLectura, modulosConEscritura`.
- `PermisoEfectivoDto { moduloNumero, moduloNombre, dominio, permiso, otorgadoPor: string[] }` (`permiso` = `"Escritura"`, `"Lectura"` o `"Ninguno"`; `otorgadoPor` = códigos de los roles que dan ese nivel).
- `ResumenUsuariosDto { total, activos, inactivos, sinRol, porRol: RolUsuariosDto[] }` con `RolUsuariosDto { rolId, codigo, usuarios }` por código.

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden:**
1. `nombreUsuario` tras `Trim()` y en minúsculas: vacío → 400 «El nombre de usuario es obligatorio.»; no cumple `^[a-z0-9._-]{3,30}$` → 400 «El nombre de usuario solo admite letras minúsculas, números, punto, guion y guion bajo (3 a 30 caracteres).»
2. `nombreCompleto` tras `Trim()`: vacío → 400 «El nombre completo es obligatorio.»; más de 100 → 400 «El nombre completo no puede superar 100 caracteres.»
3. `email` tras `Trim()` y en minúsculas: vacío → 400 «El email es obligatorio.»; más de 150 o sin formato `texto@texto.texto` → 400 «El email no tiene un formato válido.»
4. `rolIds`: repetido → 400 «El rol {id} está repetido.»; inexistente → 400 «El rol {id} no existe.» (sin roles está permitido).
5. Nombre de usuario repetido → 409 «Ya existe el usuario {nombreUsuario}.»; email repetido → 409 «Ya existe un usuario con el email {email}.» (también en la carrera, `23505`; PUT con los suyos válido).
6. Id inexistente → 404 «El usuario {id} no existe.»
7. Si un alta, edición o baja dejaría el sistema sin ningún usuario activo con el rol `ADMIN` → 409 «Debe quedar al menos un usuario activo con el rol ADMIN.»
8. Borrar un rol que tiene usuarios (módulo 72) → 409 «No se puede eliminar el rol {codigo} porque tiene usuarios.»

**Consultas.** Desde `Repository<T>().AsQueryable()`, asíncronas, proyectadas, sin N+1 (los permisos efectivos de un usuario en un número fijo de consultas). Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08.

**Seed:** 5 usuarios (dominio de correo `@titanglobal.example`, ficticio): `admin` (ADMIN), `ana.ventas` (VENTAS), `luis.compras` (COMPRAS y CONSULTA), `maria.almacen` (ALMACEN, inactivo) y `invitado` (sin roles).

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: un caso por cada mensaje de §4 con su código, literal y sin `Detail`; nombre y email en minúsculas; PUT con los suyos válido; ningún caso inválido crea o cambia filas | pasan |
| A3 | Permisos y consultas | tests: unión de roles (lectura + escritura → «Escritura»; `otorgadoPor` correcto), usuario sin roles → «Ninguno» en los 80, usuario inactivo → «Ninguno»; último ADMIN activo (desactivarlo, quitarle el rol o borrarlo → 409; con otro ADMIN activo → permitido); borrar un rol con usuarios → 409; filtros, orden y resumen exactos | pasan |
| A4 | Sin cambios en seguridad | los tests del 403 siguen idénticos y en verde; `git diff` no toca `Middlewares/` ni `Services/` | 0 archivos |
| A5 | PostgreSQL | smoke ampliado: listado, `rolId` de VENTAS, detalle de luis.compras, sus permisos efectivos (80 filas; módulo 11 «Escritura», 01 «Lectura»), resumen, POST válido, email repetido → 409, desactivar admin → 409, borrar el rol VENTAS → 409, DELETE del creado → 204 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 7 operaciones | presentes |

## 6. Checklist de ejecución
- [x] Tarea 1: entidades, `DbContext` y migración.
- [x] Tarea 2: DTOs, UseCases, controlador y regla 8 en el módulo 72.
- [x] Tarea 3: tests, seed y smoke.
- [x] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | Integración | ✅ | `50ba49a` integrado como `dfd4845` en la rama de integración por cherry-pick; `diff` de las líneas cambiadas idéntico al commit auditado |
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` en la rama de integración → exit 0: build sin warnings, formato y reglas OK, 569 tests del backend y 232 del frontend (37 archivos) en verde |
| 2026-10-15 | A2 | ✅ | `UsuariosApiTests` (25 casos): un caso por mensaje de §4 con código y literal sin `Detail`, minúsculas, PUT con los suyos, sin mutación en los inválidos |
| 2026-10-15 | A3 | ✅ | tests de unión de roles, «Ninguno» sin roles o inactivo, último ADMIN (desactivar, quitar rol, borrar → 409; con otro activo → permitido), filtros, orden y resumen; `RolesPermisosApiTests.NoPermiteEliminarRolAsignadoAUsuarios` (regla 8) |
| 2026-10-15 | A4 | ✅ | el commit no toca `Middlewares/` ni `Services/` (0 archivos); los tests del 403 sin cambios y en verde |
| 2026-10-15 | A5 | ✅ | `./scripts/smoke-postgres.sh` → `SMOKE OK` con la migración `20261015173321_Usuarios`: listado por rol, detalle y permisos de luis.compras (80 filas), resumen (5 usuarios, 1 sin rol), POST, email repetido → 409, desactivar admin → 409, borrar VENTAS → 409 literal, DELETE → 204 |
| 2026-10-15 | A6 | ✅ | el smoke valida las 7 operaciones en `/openapi/v1.json` (`openapi-usuarios`) |
| 2026-10-15 | Auditoría | ✅ | Auditoría, ciclo 1: APROBADO sin hallazgos |
