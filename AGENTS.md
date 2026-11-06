# Reglas del repositorio — TitanGlobal ERP

ERP transaccional para una corporación multinacional manufacturera y logística (80 módulos en 8 dominios). La rama `main` es la base de integración: todo lo que llega a ella se revisa, se evalúa y se despliega en la demo.

## 1. Antes de empezar
- Lee la spec de tu módulo en `docs/specs/` (`moduloNN-nombre-backend.md` o `frontend.md`).
- Comprueba en `docs/equipo/asignaciones.md` que el módulo corresponde a quien solicita el trabajo.
- Si una instrucción contradice este archivo, detente y pregunta antes de escribir código.

## 2. Estructura en cuatro capas
Dirección estricta: `Api` → `Application` e `Infrastructure`; `Infrastructure` → `Application` y `Domain`; `Application` → `Domain`. `Domain` no referencia a nadie.

| Si necesitas… | Colócalo en… | Carpeta / Namespace | Prohibido usar allí |
|---|---|---|---|
| Entidad (generada con scaffold) o puerto de repositorio | `TitanGlobal.Domain` | `Models/`, `Repositories/`, `Common/` | EF Core, Npgsql, ASP.NET, otras capas |
| Caso de uso, validación o DTO | `TitanGlobal.Application` | `DTOs/ModuloNN_...`, `UseCases/ModuloNN_...` | ASP.NET, Npgsql, `StatusCodes`, `Infrastructure`, `Api` |
| Tabla nueva o cambio de esquema | raíz del repositorio | `database/NNN-*.sql` | Editar un script ya fusionado |
| DbContext (generado), persistencia o configuración | `TitanGlobal.Infrastructure` | `Persistence/`, `Configuration/` | ASP.NET, `Api` |
| Endpoint HTTP, middleware o host | `TitanGlobal.Api` | `Controllers/ModuloNN_...`, `Program.cs` | Lógica de negocio, consultas directas a base de datos |
| Pantalla React de un módulo | `frontend` | `src/modules/<modulo>/` | Modificar componentes compartidos rompiendo otras pantallas |

## 3. Reglas de código que rompen el gate o la revisión
- **Sin `var` y sin `=>` en miembros:** declara siempre tipos explícitos. Constructores, métodos, propiedades y acciones llevan bloque `{ }` y `return`; `=>` solo se permite dentro de expresiones lambda LINQ o configuración.
- **UseCases concretos:** clases públicas terminadas en `UseCase` sin interfaz, en español (`CrearProveedorUseCase`). Se registran solas por reflexión.
- **Controladores delgados:** solo reciben la petición HTTP, invocan al `UseCase` y devuelven la respuesta. Acciones `POST`/`PUT` sin cuerpo llevan `[PermitirCuerpoVacio]`.
- **Excepciones y códigos de estado:** en `Application` usa exclusivamente `using TitanGlobal.Application.Exceptions;`, `ReglaNegocioException` y `CodigosEstado` (nunca `StatusCodes` de ASP.NET).
- **Restricciones de base de datos:** captura `ViolacionRestriccionException` (`TipoViolacionRestriccion.Unicidad` o `ReferenciaForanea`) lanzada por `UnitOfWork`. Prohibido capturar `DbUpdateException` o `PostgresException` en `Application`.
- **Mensajes literales:** usa los mensajes de error idénticos a los especificados en la spec.
- **Entidades de Domain limpias y generadas:** las entidades y la configuración de `TitanGlobalDbContext` salen del scaffold, sin atributos de EF Core (`[Index]`, `[Precision]`) ni `using Microsoft.EntityFrameworkCore;`. Longitudes, decimales, índices y claves foráneas se definen en el script SQL de `database/`. El código propio de una entidad (valores iniciales, métodos) va en un archivo `partial` aparte, como `src/TitanGlobal.Domain/Models/ValoresIniciales.cs`.
- **Fechas UTC obligatorias:** normaliza siempre con `FechasUtc` (`TitanGlobal.Domain.Common`). La columna es `timestamptz`: una fecha sin `Z` causa error 500 en PostgreSQL real (los tests en memoria no lo detectan).
- **Validación de límites:** valida en `Application` obligatoriedad, longitudes máximas, rangos numéricos y número de decimales. Lo que excede la columna causa un error 500 o se redondea en silencio, y los tests en memoria tampoco lo detectan.
- **Autenticación JWT:** todos los endpoints exigen token salvo los que llevan `[AllowAnonymous]` (login y salud). No añadas `[AllowAnonymous]` a un endpoint nuevo. Las fábricas de pruebas ya envían un token de administrador (`tests/TitanGlobal.Api.Tests/TokenPruebas.cs`).
- **Frontend consistente:** usa los componentes compartidos (`PaginaModulo`, `TablaPaginada`, `PanelDetalle`, `DialogoBaja`, `CampoConError`) y variables de `tokens.css`.

## 4. Lo que nunca se hace
- Push directo a `main` o reescritura de historial (`git push --force`).
- Editar a mano los archivos generados (los que llevan la cabecera «Generado por scripts/scaffold.sh» y `TitanGlobalDbContext.cs`) ni un script de `database/` ya fusionado: los cambios van en un script nuevo. El código propio va en archivos `partial` aparte.
- Modificar las cuatro entidades base (`Cliente`, `Producto`, `OrdenCompra`, `DetalleOrdenCompra`), middlewares o el pipeline sin spec aprobada.
- Debilitar, comentar o borrar tests existentes para que pase el gate.
- Modificar archivos, pantallas o tablas asignadas a otra persona.
- Subir secretos ni escribir cadenas de conexión en archivos, commits, chats o comandos de consola (`env`, `printenv`).
- Aplicar scripts de `database/` contra la base de datos de producción (eso solo lo hace Lukas).

## 5. Commits
- Haz los commits con la identidad git de la persona (`git config user.name` y `git config user.email`).
- Nunca hagas commits a nombre de una herramienta ni incluyas trailers de atribución (`Co-Authored-By:` o similares).
- Comprueba con `git log -n 1` antes de subir. Este repositorio lo evalúa un profesor y la nota depende del autor de cada commit.

## 6. Comandos exactos
Ejecuta los scripts siempre desde Git Bash en Windows (no PowerShell).

```sh
docker compose up -d db                                                    # Levantar PostgreSQL local (puerto 5433)
dotnet tool restore                                                        # Restaurar herramientas EF
./scripts/aplicar-bd.sh                                                    # Aplicar los scripts de database/ a la base local (--estado, --reiniciar)
./scripts/scaffold.sh                                                      # Regenerar entidades y TitanGlobalDbContext desde la base local
docker compose exec -T db psql -U titanglobal -d titanglobal -f - < scripts/seed-dev.sql  # Cargar seed
dotnet run --project src/TitanGlobal.Api --urls http://localhost:5080     # API en puerto 5080 (Swagger: /swagger)
# Toda la API pide token: POST /api/v1/autenticacion/login con admin / Admin2026! (seed local) y pega el token en Authorize de Swagger
npm --prefix frontend ci && npm --prefix frontend run dev                 # Frontend en puerto 5173
npm --prefix frontend run verify                                           # Verificación del frontend
./scripts/verify.sh                                                        # Gate completo obligatorio
./scripts/smoke-postgres.sh                                                # Smoke test contra PostgreSQL real
```
*Avisos:* no ejecutes dos `smoke-postgres.sh` a la vez; usan puertos fijos y fallan por colisión. `./scripts/aplicar-bd.sh --reiniciar` borra y recrea la base local: después vuelve a cargar `scripts/seed-dev.sql`. Si `./scripts/smoke-postgres.sh` falla con «ERROR: el código generado no coincide…», alguien editó una entidad a mano o falta ejecutar `./scripts/scaffold.sh`.

## 7. Definition of Done
Un módulo nuevo no está terminado solo porque compile. Requiere:
1. Spec aprobada en `docs/specs/`.
2. Si hay tablas nuevas: script `database/NNN-*.sql`, aplicado con `./scripts/aplicar-bd.sh`, y entidades regeneradas con `./scripts/scaffold.sh` (sin editar a mano lo generado).
3. Pruebas de integración para cada regla y mensaje literal en `tests/TitanGlobal.Api.Tests/`.
4. Datos de ejemplo idempotentes al final de `scripts/seed-dev.sql`, antes del `COMMIT;`.
5. Comprobaciones en `scripts/smoke-postgres.sh` (`request_check` más su validación en el diccionario Python) finalizando en `SMOKE OK`.
6. Pantalla React con componentes compartidos y diseño responsive (375 px y 1280 px en claro y oscuro).
7. `./scripts/verify.sh` en verde con cero warnings.

## 8. Flujo
1. **Spec:** `docs/specs/moduloNN-nombre-{backend|frontend}.md` (sin número global) aprobada por Lukas.
2. **Rama:** `git switch -c tu-nombre/moduloNN-nombre origin/main`.
3. **Código y pruebas:** siguiendo `crear-modulo.md` y `crear-pantalla.md`.
4. **Gate:** `./scripts/verify.sh` y `./scripts/smoke-postgres.sh` en verde.
5. **PR:** contra `main` con la plantilla del repositorio; un compañero revisa y Lukas integra.

## 9. Dónde está cada cosa
| Documento | Contenido |
|---|---|
| [`docs/equipo/crear-modulo.md`](docs/equipo/crear-modulo.md) | Paso a paso backend en 4 capas (ejemplo Proveedores). |
| [`docs/equipo/crear-pantalla.md`](docs/equipo/crear-pantalla.md) | Paso a paso pantalla React, accesibilidad y componentes compartidos. |
| [`docs/equipo/base-de-datos-local.md`](docs/equipo/base-de-datos-local.md) | Configuración de PostgreSQL local con Docker o sin Docker. |
| [`docs/equipo/pruebas-en-la-nube.md`](docs/equipo/pruebas-en-la-nube.md) | Conexión a rama personal en Neon y manejo seguro de credenciales. |
| [`docs/equipo/adaptar-rama-a-capas.md`](docs/equipo/adaptar-rama-a-capas.md) | Migración de ramas previas a las cuatro capas. |
| [`docs/equipo/lecciones.md`](docs/equipo/lecciones.md) | Errores que no hay que repetir. |
| [`docs/equipo/relevo.md`](docs/equipo/relevo.md) | Estado actual de ramas, módulos integrados y pendientes. |
| [`docs/arquitectura.md`](docs/arquitectura.md) | 80 módulos, arquitectura y decisiones técnicas. |
| [`CONTRIBUTING.md`](CONTRIBUTING.md) | Normas de desarrollo y convivencia del equipo. |
