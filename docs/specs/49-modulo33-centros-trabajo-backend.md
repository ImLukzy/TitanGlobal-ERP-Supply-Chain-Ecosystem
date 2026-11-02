# Especificación: 49 - Modulo33_CentrosTrabajo (backend)

## 1. Objetivo
**Problema:** el dominio de Manufactura y Producción (MRP, módulos 31–40) no tiene ningún maestro. Sin centros de trabajo no se pueden construir después las rutas de producción (32), las órdenes de producción (34) ni el mantenimiento preventivo y correctivo (39, 40).
**Resultado esperado:** una entidad nueva `CentroTrabajo` y `Modules/Modulo33_CentrosTrabajo/` en `api/v1/centros-trabajo`: listado con filtros y paginación, detalle, alta, modificación, baja, plantas y resumen. Sigue el patrón de `Modulo23_Almacenes` (spec 19).

Owner: Antony Huayhua. Semana 1 según `docs/equipo/cadencia.md` §7.

## 2. Fuera de alcance
- Rutas, órdenes de producción, mantenimiento y cualquier relación con otros módulos (llegarán con sus specs).
- Relación con `Almacen` o con `Sucursales` (módulo 73): la planta es un texto libre hasta que exista el maestro de sucursales.
- Cambios en las 4 entidades del enunciado (`Cliente`, `Producto`, `OrdenCompra`, `DetalleOrdenCompra`).
- El frontend (spec 50).

**Decisiones de producto que requieren aprobación antes de ejecutar:** los campos y tipos de §4 (propuesta del owner con el criterio de Almacenes): código único; tipos `Linea` y `Celda` (enunciado: «líneas y celdas»); capacidad en horas por día (> 0 y ≤ 24); costo por hora (≥ 0); baja física mientras ningún módulo dependa del centro; `Activo` para retirarlo sin borrarlo.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Models/CentroTrabajo.cs` | crear | entidad `partial`, estilo de `Almacen.cs` |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | bloque al final: `DbSet<CentroTrabajo> CentrosTrabajo`; índice único en `Codigo`; longitudes; `decimal(5,2)` en `CapacidadHorasDia` y `decimal(18,2)` en `CostoHora` |
| `src/TitanGlobal.Api/Migrations/*_CentrosTrabajo*.cs` + snapshot | crear | `dotnet ef migrations add CentrosTrabajo -p src/TitanGlobal.Api` tras `git rebase origin/main`; no altera tablas existentes |
| `src/TitanGlobal.Api/Modules/Modulo33_CentrosTrabajo/{Controllers,Dtos,UseCases}/` | crear | un archivo por clase; `[Route("api/v1/centros-trabajo")]`; validación en helper estático `ValidacionCentroTrabajo` |
| `scripts/seed-dev.sql` | modificar | bloque al final: 5 centros idempotentes (`ON CONFLICT` sobre `Codigo`) y `setval` |
| `scripts/smoke-postgres.sh` | modificar | bloque al final con las rutas de §5 A5 |
| `tests/TitanGlobal.Api.Tests/Modulo33_CentrosTrabajo/*.cs` | crear | InMemory, datos propios del test |

## 4. Diseño y lógica
**Entidad `CentroTrabajo`:** `Id`, `Codigo` (requerido, máx. 20, único, guardado en mayúsculas), `Nombre` (requerido, máx. 200), `Tipo` (requerido, máx. 20: `Linea`, `Celda`), `Planta` (requerida, máx. 100), `CapacidadHorasDia` (`decimal(5,2)`, > 0 y ≤ 24), `CostoHora` (`decimal(18,2)`, ≥ 0), `Responsable` (opcional, máx. 100), `Activo` (bool, por defecto `true`).

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/centros-trabajo?busqueda=&planta=&tipo=&activo=&pagina=1&tamanoPagina=20` | opcionales salvo paginación; `activo` = `true`/`false` | 200 `PaginaCentrosTrabajoDto { items: CentroTrabajoListadoDto[], total, pagina, tamanoPagina }` | 400 si `pagina < 1` o `tamanoPagina` fuera de 1–100; 400 «El tipo de centro de trabajo no es válido.» |
| GET | `api/v1/centros-trabajo/{id:int}` | — | 200 `CentroTrabajoDetalleDto` | 404 «El centro de trabajo {id} no existe.» |
| POST | `api/v1/centros-trabajo` | `GuardarCentroTrabajoRequestDto { codigo, nombre, tipo, planta, capacidadHorasDia, costoHora, responsable?, activo }` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/centros-trabajo/{id:int}` | `GuardarCentroTrabajoRequestDto` | 200 `{ id }` | 400, 404, 409 |
| DELETE | `api/v1/centros-trabajo/{id:int}` | — | 204 | 404 |
| GET | `api/v1/centros-trabajo/plantas` | — | 200 `string[]` (plantas distintas, ordenadas) | — |
| GET | `api/v1/centros-trabajo/resumen` | — | 200 `ResumenCentrosTrabajoDto` | — |

**DTOs.**
- `CentroTrabajoListadoDto { id, codigo, nombre, tipo, planta, capacidadHorasDia, activo }`
- `CentroTrabajoDetalleDto { id, codigo, nombre, tipo, planta, capacidadHorasDia, costoHora, responsable, activo }`
- `ResumenCentrosTrabajoDto { totalCentros, activos, inactivos, capacidadTotalHorasDia, capacidadActivaHorasDia, porTipo: TipoCentrosTrabajoDto[], porPlanta: PlantaCentrosTrabajoDto[] }`
- `TipoCentrosTrabajoDto { tipo, totalCentros, capacidadHorasDia }`, en el orden Linea, Celda (solo tipos con centros).
- `PlantaCentrosTrabajoDto { planta, totalCentros, activos }`, por `totalCentros` desc y después `planta`.

**Reglas, en `UseCases` con `ReglaNegocioException`, en este orden:**
1. `codigo` tras `Trim()` y en mayúsculas: vacío → 400 «El código es obligatorio.»; no cumple `^[A-Z0-9-]{2,20}$` → 400 «El código solo admite letras, números y guiones (2 a 20 caracteres).»
2. `nombre` tras `Trim()`: vacío → 400 «El nombre del centro de trabajo es obligatorio.»; más de 200 → 400 «El nombre del centro de trabajo no puede superar 200 caracteres.»
3. `tipo`: `Linea` o `Celda`, sin distinguir mayúsculas y guardado con su forma canónica → si no, 400 «El tipo de centro de trabajo no es válido.»
4. `planta` tras `Trim()`: vacía → 400 «La planta es obligatoria.»; más de 100 → 400 «La planta no puede superar 100 caracteres.»
5. `capacidadHorasDia <= 0` o `> 24` → 400 «La capacidad debe ser mayor que 0 y como máximo 24 horas por día.»
6. `costoHora < 0` → 400 «El costo por hora no puede ser negativo.»
7. `responsable` tras `Trim()`, en blanco → `null`; más de 100 → 400 «El responsable no puede superar 100 caracteres.»
8. Código usado por otro centro → 409 «Ya existe un centro de trabajo con el código {codigo}.» También en la carrera (`23505`). Un PUT que mantiene su propio código es válido.
9. Id inexistente en GET, PUT o DELETE → 404 «El centro de trabajo {id} no existe.»

**Consultas.** Desde `Repository<CentroTrabajo>().AsQueryable()`, asíncronas y proyectadas a DTO en la consulta.
- `busqueda` por código o nombre sin distinguir mayúsculas; `planta` exacta sin distinguir mayúsculas; `tipo` validado como en la regla 3; `activo` si viene.
- Orden por `Codigo` y después `Id`.
- Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08 (filtro de texto, `GroupBy`, `Distinct`), sin forzarlos.

**Seed de desarrollo** (`seed-dev.sql`, idempotente con `ON CONFLICT` sobre `Codigo`): 5 centros en 2 plantas.

| Código | Nombre | Tipo | Planta | Horas/día | Costo/hora | Responsable | Activo |
|---|---|---|---|---|---|---|---|
| `CT-LIM-L01` | Línea de envasado 1 | Linea | Planta Lima | 16 | 120.00 | Carlos Ramos | sí |
| `CT-LIM-L02` | Línea de envasado 2 | Linea | Planta Lima | 8 | 110.00 | Carlos Ramos | sí |
| `CT-LIM-C01` | Celda de ensamblaje | Celda | Planta Lima | 8 | 85.50 | Rosa Quispe | sí |
| `CT-ARE-L01` | Línea de mezclado | Linea | Planta Arequipa | 24 | 150.00 | Jorge Mamani | no |
| `CT-ARE-C01` | Celda de etiquetado | Celda | Planta Arequipa | 12 | 60.00 | — | sí |

Resumen esperado del seed: 5 centros, 4 activos, 1 inactivo; capacidad total 68 h/día, activa 44 h/día; por tipo Linea 3 / 48 h y Celda 2 / 20 h; por planta Planta Lima 3 / 3 activos y Planta Arequipa 2 / 1 activo.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: cada mensaje de §4 con su código, literal y sin `Detail`; código con espacios y minúsculas guardado en mayúsculas; tipo en minúsculas guardado canónico; capacidad 24 aceptada y 24.01 rechazada; costo 0 aceptado; responsable en blanco → `null`; PUT con su propio código → 200; ningún caso inválido crea o cambia filas | pasan |
| A3 | Consultas | tests con datos conocidos: búsqueda por código y por nombre, planta (sin distinguir mayúsculas), tipo, `activo`, paginación con `total`, plantas ordenadas, resumen exacto con sus órdenes | pasan |
| A4 | Migración | `CentrosTrabajo` generada con `dotnet ef` (`Designer` + snapshot); `dotnet ef database update` sobre la BD con el seed aplica sin errores; no altera tablas existentes | evidencia |
| A5 | PostgreSQL | `./scripts/smoke-postgres.sh` ampliado: listado `planta=planta lima` → 3, `tipo=celda` → 2, `activo=false` → 1, detalle, plantas `["Planta Arequipa","Planta Lima"]`, resumen 5/4/1, POST válido, POST con código del seed → 409, DELETE del creado → 204 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 7 operaciones | presentes |

## 6. Checklist de ejecución
- [x] Tarea 1: entidad, `DbContext` y migración.
- [x] Tarea 2: DTOs, UseCases y controlador.
- [x] Tarea 3: tests, seed y smoke.
- [x] Tarea 4: A1–A6 y evidencia en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-02 | A1 | ✅ | `./scripts/verify.sh` rc=0 en un clon con fines de línea LF de la rama: build sin warnings, formato, reglas, backend 627/627, frontend 323/323 |
| 2026-11-02 | A2–A3 | ✅ | `CentrosTrabajoApiTests` (42 casos): cada mensaje de §4 con su código, literal y sin `Detail`; orden de reglas; código con espacios y minúsculas → mayúsculas; tipo en minúsculas → canónico; capacidad 24 aceptada y 24.01 rechazada; costo 0 aceptado; responsable en blanco → `null`; PUT con su propio código → 200; los casos inválidos no crean ni cambian filas; búsqueda por código y nombre, planta, tipo, `activo`, paginación con `total`, plantas y resumen exactos |
| 2026-11-02 | A4 | ✅ | `dotnet ef migrations add CentrosTrabajo` (con `Designer` y snapshot): solo crea `CentrosTrabajo` y el índice único `IX_CentrosTrabajo_Codigo`; `dotnet ef database update` sobre la BD local con el seed aplicado sin errores; el seed se puede ejecutar dos veces (5 filas) |
| 2026-11-02 | A5 | ✅ | Bloque del módulo 33 de `./scripts/smoke-postgres.sh` (preparación completa del script + solo las llamadas del módulo) → `SMOKE OK`: `planta=planta lima` 3, `tipo=celda` 2, `activo=false` 1, detalle `CT-LIM-C01`, plantas `["Planta Arequipa","Planta Lima"]`, resumen 5/4/1 con 68.00 y 44.00 h/día, POST → `{id:6}`, código del seed → 409 literal, DELETE → 204. El smoke completo no corre en Windows/Git Bash: `curl` envía los acentos de otros payloads (p. ej. «Actualización» en clientes) con la codificación de Windows y no en UTF-8; queda pendiente confirmarlo en Linux/CI |
| 2026-11-02 | A6 | ✅ | `OpenApiContainsSevenWorkCenterOperations` y validación `openapi-centros-trabajo` del smoke: las 7 operaciones presentes |
