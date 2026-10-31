# Especificación: 18 - Acciones POST sin cuerpo declaradas con un atributo

## 1. Objetivo
**Problema:** `ParameterValidationMiddleware` devuelve 400 «El cuerpo de la petición (Body) no puede estar vacío.» en todo POST/PUT sin cuerpo. Para las acciones que no lo necesitan (`solicitudes-compra/{id}/aprobar`, `ordenes-abastecimiento/{id}/recibir`), las specs 14 y 16 añadieron excepciones con rutas escritas a mano dentro del middleware. Cada acción nueva obliga a tocar un componente transversal y las rutas pueden desincronizarse de los controladores.
**Resultado esperado:** las acciones declaran que aceptan cuerpo vacío con un atributo `[PermitirCuerpoVacio]`, y el middleware lo lee de los metadatos del endpoint (disponible porque `UseRouting` va antes en el pipeline). El middleware no contiene rutas.

## 2. Fuera de alcance
- Cambiar el orden del pipeline, los mensajes literales o el comportamiento de cualquier otra ruta.
- Cambiar los contratos de las specs 14 y 16.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Middlewares/PermitirCuerpoVacioAttribute.cs` | crear | `[AttributeUsage(AttributeTargets.Method)] public sealed class PermitirCuerpoVacioAttribute : Attribute` |
| `src/TitanGlobal.Api/Middlewares/ParameterValidationMiddleware.cs` | modificar | sustituir la lista de rutas por `context.GetEndpoint()?.Metadata.GetMetadata<PermitirCuerpoVacioAttribute>() != null` |
| `src/TitanGlobal.Api/Modules/Modulo13_SolicitudesCompra/Controllers/*.cs` | modificar | atributo en `aprobar` |
| `src/TitanGlobal.Api/Modules/Modulo15_OrdenesAbastecimiento/Controllers/*.cs` | modificar | atributo en `recibir` |
| `tests/TitanGlobal.Api.Tests/*Middleware*` o el test de pipeline existente | modificar o crear | §5 A2 |
| `docs/arquitectura.md` | no tocar | la integración documenta la convención |

## 4. Diseño y lógica
- Una acción con `[PermitirCuerpoVacio]` pasa el middleware con cuerpo vacío; con cuerpo, se valida igual que siempre.
- Cualquier otro POST/PUT sin cuerpo sigue devolviendo el 400 literal, también los de las rutas `rechazar` y `anular`, que sí requieren cuerpo.
- Una ruta inexistente sin cuerpo se comporta como hoy (no se rompe el 404 ni el orden del pipeline).
- Reglas de `CONTRIBUTING.md`: sin `var`, sin `=>` en miembros, nombres en español.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Comportamiento | tests: `aprobar` y `recibir` sin cuerpo → 200; POST sin cuerpo a `clientes`, `rechazar` y `anular` → 400 literal; ningún test existente cambia lo que comprueba | pasan |
| A3 | Sin rutas fijas | `grep -n '/api/v1' src/TitanGlobal.Api/Middlewares/` | 0 resultados |
| A4 | PostgreSQL | `./scripts/smoke-postgres.sh` | `SMOKE OK` |

## 6. Checklist de ejecución
- [x] Tarea 1: atributo y middleware.
- [x] Tarea 2: atributos en los controladores y tests.
- [x] Tarea 3: A1–A4.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-14 | A1 | ✅ | `./scripts/verify.sh` rc=0 en la rama de integración (`0d4edf0`, mismas líneas que el auditado `edbb0b8`): backend 175/175, frontend 120/120. Un primer intento falló por el test intermitente de Productos (A3, petición duplicada del listado al cargar), ajeno a esta spec |
| 2026-10-14 | A2 | ✅ | tests en `ApiTests.cs`: `aprobar` y `recibir` sin cuerpo → 200; POST sin cuerpo a `clientes`, `rechazar` y `anular` → 400 literal |
| 2026-10-14 | A3 | ✅ | `git grep -n '/api/v1' -- src/TitanGlobal.Api/Middlewares` → 0 resultados |
| 2026-10-14 | A4 | ✅ | `./scripts/smoke-postgres.sh` → `SMOKE OK` |
| 2026-10-14 | Auditoría | ✅ | Auditoría, ciclo 1: APROBADO |
| 2026-10-14 | QA E2E | ✅ | QA en local (Vite 5174 + API + PostgreSQL) y en producción (solo lectura): resumen del seed con plurales correctos; filtros y rango inválido sin petición; atrasada con etiqueta y anulada sin acciones; alta con 3 líneas → `OA-000NNN` y validaciones locales; edición; recibir sin cuerpo → 200; anular con y sin motivo; «Generar orden» desde SC-DEV-004 → navega, repetir → 409, sin botón en pendientes y rechazadas; aprobar sin cuerpo → 200; Productos pide el listado una sola vez; 17 capturas sin recortes ni scroll horizontal |
