# Especificación: 05 - Fechas UTC en consultas y prueba de humo contra PostgreSQL

## 1. Objetivo
**Problema:** `GET /api/v1/ordenes-compra/ventas/suma?fechaInicio=2026-01-01` (y `…T00:00:00`) responde 500 contra PostgreSQL. El mensaje de Npgsql es «Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone', only UTC is supported». Solo funciona con `…Z`. Los tests no lo detectan porque usan EF InMemory. El frontend (spec 04) envía justo `2026-01-01`, desde un `<input type="date">`. Lo reprodujo la integración el 2026-10-14 sobre `ImLukzy/Claudio` (`183f153`).
**Resultado esperado:**
- Toda fecha que entra a una consulta se normaliza a UTC antes de llegar a EF, de modo que la fecha simple, la fecha con hora sin zona y la fecha con `Z` u offset responden 200.
- Un script de humo reproducible ejecuta las rutas reales contra PostgreSQL, para detectar fallos que InMemory oculta.

## 2. Fuera de alcance
- Cambiar el tipo de columna (`timestamp with time zone`) o activar `Npgsql.EnableLegacyTimestampBehavior`.
- Validaciones de negocio nuevas (NumeroOrden repetido, cliente inexistente), que están pendientes de decisión del humano.
- El frontend.
- Meter el script de humo en `verify.sh` (usa puertos fijos y Docker; se ejecuta a mano y en QA).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Una fecha sin zona se interpreta como UTC.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Modules/Modulo04_OrdenesCompra/UseCases/AnaliticaOrdenesCompraUseCase.cs` | modificar | `ObtenerSumaVentasDesdeFechaAsync` normaliza `fechaInicio` a UTC antes de la consulta: `Unspecified` → `DateTime.SpecifyKind(…, Utc)`; `Local` → `ToUniversalTime()`; `Utc` → sin cambios |
| `src/TitanGlobal.Api/Configuration/FechasUtc.cs` | crear | clase estática técnica `FechasUtc` con `public static DateTime Normalizar(DateTime fecha)`. Es transversal: los 79 módulos restantes la reutilizarán. Con llaves y `return` explícito |
| `tests/TitanGlobal.Api.Tests/FechasUtcTests.cs` | crear | `Normalizar` con `Unspecified`, `Local` y `Utc` → `Kind == Utc` y el instante correcto |
| `tests/TitanGlobal.Api.Tests/Modulo04_OrdenesCompra/OrdenesCompraApiTests.cs` | modificar | `ventas/suma?fechaInicio=2026-01-01` → 200 y el total exacto esperado con los datos InMemory |
| `scripts/smoke-postgres.sh` | crear | ver §4 |
| `docs/equipo/lecciones.md` | no tocar | lo escribe la integración |

## 4. Diseño y lógica
- **Normalización:** el UseCase llama a `FechasUtc.Normalizar(fechaInicio)`; nunca en el controlador. El controlador sigue siendo delgado.
- **`scripts/smoke-postgres.sh`** (`set -euo pipefail`):
  1. Comprueba que 5080 y 5433 están libres; si no, sale con un error claro.
  2. `docker compose up -d db` y espera a `pg_isready`.
  3. `dotnet tool restore`, `dotnet ef database update -p src/TitanGlobal.Api` y aplica `scripts/seed-dev.sql`.
  4. Arranca la API en 5080 (Development) en segundo plano y espera a `/api/v1/sistema/salud`.
  5. Ejecuta con `curl` las 10 rutas de `api/v1/ordenes-compra` con los datos del seed y comprueba el status 200 y un valor esperado por ruta (con `jq` si está instalado o con `python3 -c`). La suma de ventas se comprueba con las tres variantes de fecha: `2026-01-01`, `2026-01-01T00:00:00` y `2026-01-01T00:00:00Z`.
  6. Comprueba también que un POST vacío da 400 y que `X-User-Role: Bloqueado` da 403.
  7. Siempre, también si falla (`trap … EXIT`): para la API y ejecuta `docker compose down`.
  8. Imprime `SMOKE OK` o la ruta que falló con su status y su cuerpo.
- `DOCKER_CONFIG` se respeta si viene del entorno.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Tests nuevos | `FechasUtcTests` (3 casos) + test de `ventas/suma` con fecha simple | pasan |
| A3 | PostgreSQL | `./scripts/smoke-postgres.sh` | `SMOKE OK`; las 3 variantes de fecha → 200 con el mismo total |
| A4 | Limpieza | después del smoke, también si se fuerza un fallo: `ss -ltn` sin 5080 ni 5433 y sin contenedor `db` | limpio |

## 6. Checklist de ejecución
- [x] Tarea 1: `FechasUtc` y normalización en el UseCase.
- [x] Tarea 2: tests.
- [x] Tarea 3: `smoke-postgres.sh`.
- [x] Tarea 4: A1–A4.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-14 | A1 | ✅ | `./scripts/verify.sh` rc=0 en `ImLukzy/Claudio` (`1f9eb7a`): backend 28/28, frontend 36/36 |
| 2026-10-14 | A2 | ✅ | `FechasUtcTests` (Utc, Local, Unspecified) y teoría de `ventas/suma` con las 3 variantes de fecha |
| 2026-10-14 | A3 | ✅ | La integración ejecutó `./scripts/smoke-postgres.sh` → `SMOKE OK`; `2026-01-01`, `2026-01-01T00:00:00` y `…Z` → 200 `{"totalVentas":10750.00}` (antes: 500 con fecha sin zona) |
| 2026-10-14 | A4 | ✅ | tras el smoke: 5080 y 5433 libres, 0 contenedores; la implementación forzó un fallo y comprobó la misma limpieza |
| 2026-10-14 | Auditoría | ✅ | Auditoría, ciclo 1: APROBADO, sin hallazgos (sobre `69f95aa`, idéntico a `a59bf8a`) |
