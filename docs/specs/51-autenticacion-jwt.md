# Especificación: 51 - Inicio de sesión con JWT y BCrypt

## 1. Objetivo
**Problema:** el Lab 10 pide «Implementa JWT para el manejo de autenticación» y, como opcional, encriptar contraseñas. Hoy cualquiera puede usar la API: el rol llega en la cabecera `X-User-Role` (`src/TitanGlobal.Api/Services/AuthService.cs`) y los usuarios del módulo 71 no tienen contraseña (`src/TitanGlobal.Domain/Models/Usuario.cs`).

**Resultado esperado:** un usuario inicia sesión con su nombre y contraseña, guardada con BCrypt, y recibe un JWT. Toda la API exige ese token, salvo el inicio de sesión y la salud del sistema. La demo abre con una pantalla de inicio de sesión que muestra un usuario de demostración.

## 2. Fuera de alcance
- **`X-User-Role` y el 403 del enunciado:** se conservan tal cual. `RoleBasedAccessMiddleware` sigue leyendo la cabecera y respondiendo el 403 literal con `Bloqueado` o `SinPermiso`.
- **Permisos por módulo (matriz del 72):** el token no se usa para aplicar la matriz de permisos.
- **Registro público, recuperación de contraseña, refresh tokens, cierre de sesión en el servidor y límite de intentos.**
- **Auditoría (77):** `ContextoSolicitud` sigue leyendo `X-Usuario`; el frontend envía en esa cabecera el usuario que inició sesión.
- **Las cuatro entidades base, otros módulos y sus pantallas.**

**Decisiones del humano (2026-11-05):** toda la API exige token salvo inicio de sesión y salud; la pantalla de inicio de sesión muestra el usuario de demostración.

**Decisión derivada (propuesta en esta spec):** como las credenciales de demostración son públicas, las acciones que crean, editan o borran usuarios y roles, y el cambio de contraseña, exigen el rol `ADMIN`. El usuario `demo` no lo tiene, así que un visitante no puede desactivarlo ni cambiar contraseñas.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Domain/Models/Usuario.cs` | modificar | `public string? ContrasenaHash { get; set; }` |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | modificar | `ContrasenaHash` con `HasMaxLength(100)`, al final de `OnModelCreating` |
| `src/TitanGlobal.Infrastructure/Migrations/*_UsuariosContrasena*.cs` + snapshot | crear | `dotnet ef migrations add UsuariosContrasena ...` |
| `src/TitanGlobal.Application/Services/IServicioContrasenas.cs`, `IServicioTokens.cs` | crear | puertos |
| `src/TitanGlobal.Application/DTOs/Modulo00_Autenticacion/*.cs` | crear | `IniciarSesionRequestDto`, `SesionDto`, `CambiarContrasenaRequestDto` |
| `src/TitanGlobal.Application/UseCases/Modulo00_Autenticacion/*.cs` | crear | `IniciarSesionUseCase`, `CambiarContrasenaUseCase` |
| `src/TitanGlobal.Infrastructure/Configuration/JwtOpciones.cs` | crear | `Clave`, `Emisor`, `Audiencia`, `MinutosExpiracion` |
| `src/TitanGlobal.Infrastructure/Services/ServicioContrasenas.cs`, `ServicioTokens.cs` | crear | BCrypt y emisión del JWT |
| `src/TitanGlobal.Infrastructure/Configuration/InfrastructureServicesExtensions.cs` | modificar | registra los servicios y `JwtOpciones` |
| `src/TitanGlobal.Infrastructure/TitanGlobal.Infrastructure.csproj` | modificar | `BCrypt.Net-Next`, `Microsoft.IdentityModel.JsonWebTokens` |
| `src/TitanGlobal.Api/TitanGlobal.Api.csproj` | modificar | `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.13 |
| `src/TitanGlobal.Api/Configuration/ServiceRegistrationExtensions.cs` | modificar | JwtBearer, política por defecto y esquema Bearer en Swagger |
| `src/TitanGlobal.Api/Program.cs` | modificar | `app.UseAuthentication()` justo antes de `app.UseAuthorization()`; `MapOpenApi().AllowAnonymous()` |
| `src/TitanGlobal.Api/Controllers/Modulo00_Autenticacion/AutenticacionController.cs` | crear | `login` y `perfil` |
| `src/TitanGlobal.Api/Controllers/Modulo00_Autenticacion/ContrasenasController.cs` | crear | `PUT api/v1/usuarios/{id}/contrasena` |
| `src/TitanGlobal.Api/Controllers/Modulo00_Sistema/HealthController.cs` | modificar | `[AllowAnonymous]` |
| `src/TitanGlobal.Api/Controllers/Modulo71_Usuarios/UsuariosController.cs` y controladores de `Modulo72_RolesPermisos/` | modificar | solo `[Authorize(Roles = "ADMIN")]` en `POST`, `PUT` y `DELETE` |
| `tests/TitanGlobal.Api.Tests/ApiTests.cs`, `Modulo06_ListasPrecios/ListasPreciosApiTests.cs`, `Modulo04_OrdenesCompra/OrdenesCompraApiTests.cs` | modificar | las fábricas añaden un token de administrador con `ConfigureClient` |
| `tests/TitanGlobal.Api.Tests/TokenPruebas.cs` | crear | genera el token con el `IServicioTokens` de la propia aplicación |
| `tests/TitanGlobal.Api.Tests/Modulo00_Autenticacion/AutenticacionApiTests.cs` | crear | pruebas de §5 |
| `scripts/seed-dev.sql` | modificar | contraseña de `admin` y usuario `demo` |
| `scripts/smoke-postgres.sh` | modificar | inicia sesión y envía el token; comprueba 401 y login |
| `frontend/src/api/sesionStore.ts`, `frontend/src/api/client.ts` | crear / modificar | sesión guardada y cabecera `Authorization` |
| `frontend/src/layout/SesionContext.tsx`, `frontend/src/layout/AccesoProtegido.tsx`, `frontend/src/pages/InicioSesion.tsx` | crear | pantalla y puerta de acceso |
| `frontend/src/main.tsx`, `frontend/src/layout/AppLayout.tsx` | modificar | envuelve la app; usuario y «Cerrar sesión» en la barra superior |
| `AGENTS.md`, `docs/equipo/crear-modulo.md`, `docs/equipo/base-de-datos-local.md`, `docs/arquitectura.md`, `docs/contexto-proyecto.md`, `docs/equipo/relevo.md` | modificar | cómo obtener el token en local, Swagger y pruebas |

## 4. Diseño y lógica

### API
| Método y ruta | Acceso | Respuesta |
|---|---|---|
| `POST api/v1/autenticacion/login` | anónimo | `200` con `SesionDto` |
| `GET api/v1/autenticacion/perfil` | token | `200` con `{ id, nombreUsuario, nombreCompleto, roles }` |
| `PUT api/v1/usuarios/{id}/contrasena` | token con rol `ADMIN` | `204` |
| `GET api/v1/sistema/salud` | anónimo | sin cambios |
| resto de rutas | token | sin cambios |

`IniciarSesionRequestDto`: `NombreUsuario`, `Contrasena`. `SesionDto`: `Token`, `ExpiraEn` (UTC), `Usuario` (`Id`, `NombreUsuario`, `NombreCompleto`), `Roles` (códigos de rol).

### Mensajes literales
| Caso | Código | Mensaje |
|---|---|---|
| Falta usuario o contraseña al iniciar sesión | 400 | `El usuario y la contraseña son obligatorios.` |
| Usuario inexistente, inactivo, sin contraseña o contraseña incorrecta | 401 | `Usuario o contraseña incorrectos.` |
| Petición sin token o con token inválido o vencido | 401 | `Debe iniciar sesión para acceder a este recurso.` |
| Token válido sin el rol requerido | 403 | `Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.` |
| Contraseña nueva con menos de 8 o más de 72 caracteres | 400 | `La contraseña debe tener entre 8 y 72 caracteres.` |
| Usuario inexistente al cambiar contraseña | 404 | `El usuario no existe.` |

Todos con el cuerpo `{ StatusCode, Message }` y `JsonSerializationOptions.ErrorResponses`. Los 401 y 403 de autenticación se escriben en los eventos `OnChallenge` y `OnForbidden` de JwtBearer. Para el 401 de credenciales, `CodigosEstado` añade `Status401Unauthorized = 401`.

### Token
- HS256 con `Jwt:Clave`. Emisor `titanglobal-api`, audiencia `titanglobal-web` y 480 minutos de vigencia por defecto.
- Claims: `sub` (id), `name` (`NombreUsuario`) y un `role` por cada código de rol. JwtBearer usa `NameClaimType = "name"`, `RoleClaimType = "role"` y `MapInboundClaims = false`.
- **Clave:** fuera de Development, la API no arranca si falta `Jwt:Clave` o si mide menos de 32 bytes. En Development, si falta, se genera una clave aleatoria al arrancar. Ninguna clave se escribe en el repositorio.
- **Política:** `AddAuthorization` con `FallbackPolicy` que exige usuario autenticado. Salud, login y OpenAPI llevan `AllowAnonymous`. El orden del pipeline del enunciado no cambia: `UseAuthentication` entra justo antes de `UseAuthorization`.

### Contraseñas
- BCrypt con factor 11 (`BCrypt.Net-Next`). El hash nunca sale en ningún DTO.
- El login compara siempre con BCrypt, también si el usuario no existe (contra un hash fijo), para no revelar qué usuarios existen por el tiempo de respuesta.

### Datos de desarrollo y demostración
- `seed-dev.sql`: `admin` con la contraseña `Admin2026!` (solo local) y un usuario nuevo `demo` («Usuario de demostración», activo, sin roles) con `Demo2026!`. Los hashes se generan con BCrypt y se escriben en el seed.
- **Producción (lo aplica Lukas, con copia, ensayo y OK):** migración, usuario `demo` con `Demo2026!` y contraseña de `admin` elegida por Lukas, que no se escribe en ningún archivo. Variable `Jwt__Clave` en Render antes de desplegar.

### Frontend
- `sesionStore.ts`: guarda `{ token, expiraEn, usuario, roles }` en `localStorage` (clave `titanglobal-sesion`), con `try/catch` como `roleStore.ts`. Una sesión vencida se descarta al leerla.
- `client.ts`: añade `Authorization: Bearer <token>` y envía en `X-Usuario` el `nombreUsuario` de la sesión. Si una respuesta es 401 fuera del login, borra la sesión.
- `AccesoProtegido`: sin sesión muestra `InicioSesion`; con sesión, la aplicación. Se monta en `main.tsx`, así que `AppRoutes` y sus pruebas no cambian.
- `InicioSesion`: campos «Usuario» y «Contraseña» con `CampoConError`, botón «Iniciar sesión» y el aviso «Usuario de demostración: demo · Contraseña: Demo2026!». Muestra el mensaje de la API si falla. Responsive a 375 y 1280 px, en claro y oscuro, con `tokens.css`.
- `AppLayout`: en la barra superior, el nombre del usuario y un botón «Cerrar sesión», solo si hay sesión.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Gate | `./scripts/verify.sh` | en verde |
| A3 | Pruebas existentes | `dotnet test` y `npm --prefix frontend run verify` | todas en verde sin borrar ni debilitar ninguna |
| A4 | Login correcto | `POST login` con `admin` → 200 con token que abre `GET perfil` | pasa |
| A5 | Credenciales | contraseña incorrecta, usuario inexistente, inactivo y sin contraseña → 401 con el mensaje literal | pasa |
| A6 | Sin token | `GET api/v1/clientes` sin token y con token de firma inválida → 401 literal; `GET sistema/salud` sin token → 200 | pasa |
| A7 | Rol ADMIN | token sin `ADMIN`: `PUT usuarios/{id}/contrasena`, `POST usuarios` y `POST roles` → 403 literal; con `ADMIN` → 204/201 | pasa |
| A8 | Cambio de contraseña | 7 caracteres → 400; usuario inexistente → 404; tras cambiarla, el login con la nueva → 200 y con la anterior → 401 | pasa |
| A9 | Enunciado | `X-User-Role: Bloqueado` con token válido → 403 literal del enunciado | pasa |
| A10 | Hash | `GET usuarios/{id}` no contiene `contrasenaHash` ni `$2` | pasa |
| A11 | PostgreSQL | `./scripts/smoke-postgres.sh` con login, token y comprobaciones de 401 | `SMOKE OK` |
| A12 | Pantalla | capturas a 375 y 1280 px, claro y oscuro: login, error de credenciales y barra con «Cerrar sesión» | sin desbordes ni errores de consola |

## 6. Checklist de ejecución
- [ ] Backend: entidad, migración, servicios, casos de uso, controladores, JwtBearer y pruebas.
- [ ] Seed y smoke.
- [ ] Frontend: sesión, cliente, pantalla, puerta y barra.
- [ ] Guías del equipo.
- [ ] Verificar A1–A12 y anotar resultados en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
