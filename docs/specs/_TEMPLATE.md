# Especificación: moduloNN-nombre-{backend|frontend} — <Título corto>

> Para un módulo nuevo, copia este archivo como `docs/specs/moduloNN-nombre-backend.md` o `docs/specs/moduloNN-nombre-frontend.md`, **sin número global** (evita choques entre ramas; solo las specs transversales llevan número global `NN-slug.md`). Una spec = un objetivo.

## 1. Objetivo
**Problema:** qué falla hoy o qué falta (con archivo y líneas si aplica).
**Resultado esperado:** qué podrá hacer el usuario o qué garantiza el sistema al terminar, en 1–3 frases.

## 2. Fuera de alcance
Lo que quien implementa **no** debe tocar aunque parezca relacionado. Ejemplos:
- Las 4 entidades del enunciado (`Cliente`, `Producto`, `OrdenCompra`, `DetalleOrdenCompra`), los middlewares y el pipeline.
- Eliminar páginas, rutas o funcionalidades existentes.
- Migraciones EF Core no listadas en §3.
- Refactors fuera de los archivos de §3.

**Decisiones de producto que requieren aprobación antes de ejecutar:** (lista o "ninguna").

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Domain/Models/<Entidad>.cs` | crear / modificar | entidad `partial` en `TitanGlobal.Domain.Models` |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | modificar | `DbSet<Entidad>` y configuración en `OnModelCreating` |
| `src/TitanGlobal.Infrastructure/Migrations/*_<Nombre>*.cs` + `TitanGlobalDbContextModelSnapshot.cs` | crear | generada con `dotnet ef migrations add <Nombre> -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api` |
| `src/TitanGlobal.Application/DTOs/ModuloNN_Nombre/*.cs` | crear | DTOs de entrada y salida |
| `src/TitanGlobal.Application/UseCases/ModuloNN_Nombre/*.cs` | crear | casos de uso concretos sin interfaz y helper `Validacion<Entidad>` |
| `src/TitanGlobal.Api/Controllers/ModuloNN_Nombre/<Recurso>Controller.cs` | crear | controlador delgado en `api/v1/<recurso>` |
| `tests/TitanGlobal.Api.Tests/ModuloNN_Nombre/<Recurso>ApiTests.cs` | crear | pruebas de integración con InMemory |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | modificar | datos de ejemplo al final y pruebas contra PostgreSQL real |
| `frontend/src/modules/<modulo>/...` | crear / modificar | si es spec de pantalla |

Rutas comprobadas en el repositorio antes de redactar. Nada de "o equivalente".

## 4. Diseño y lógica
- **UI (si aplica):** componentes compartidos (`PaginaModulo`, `TablaPaginada`, `PanelDetalle`, `DialogoBaja`, `CampoConError`) y clases de `frontend/src/styles/app.css`.
- **API:** endpoints `api/v1/<recurso-en-kebab-case>`, respuestas JSON en camelCase y errores `{ StatusCode, Message }` con mensajes literales.
- **Datos:** entidades en `src/TitanGlobal.Domain/Models/`, configuración en `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` y migración generada con `dotnet ef migrations add <Nombre> -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api` (nunca editar una migración aplicada ni el snapshot a mano).
- **Invariantes:** reglas de negocio solo en `src/TitanGlobal.Application/UseCases/ModuloNN_Nombre/`, lanzando `ReglaNegocioException` con `CodigosEstado` (`using TitanGlobal.Application.Exceptions;`) y traduciendo conflictos de base de datos mediante `ViolacionRestriccionException` (`TipoViolacionRestriccion.Unicidad` / `ReferenciaForanea`); reglas de código de `CONTRIBUTING.md`.

## 5. Criterios de aceptación (medibles)
Cada criterio debe poder comprobarse con un comando o una medición. Borra los que no apliquen.

| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Formato y reglas | `./scripts/verify.sh` (format + reglas de código y capas) | en verde |
| A3 | Tests | `dotnet test` | todos en verde; +N tests nuevos: `<Clase>` › `<caso>` |
| A4 | Contrato API | OpenAPI (`/openapi/v1.json`) y Swagger (`/swagger`) incluyen las rutas nuevas | presentes |
| A5 | Comportamiento | test de integración: "<petición> → <código y cuerpo>" | pasa |
| A6 | Datos y PostgreSQL | migración aplica con `dotnet ef database update -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api` y `./scripts/smoke-postgres.sh` | `SMOKE OK` |

## 6. Checklist de ejecución
*(Marca `[x]` solo cuando la tarea esté hecha **y** A1–A3 estén en verde.)*

- [ ] Tarea 1: …
- [ ] Tarea 2: …
- [ ] Tarea N: Verificar A1–A6 aplicables y anotar resultados en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| AAAA-MM-DD | A1 | ✅ / ❌ | … |
