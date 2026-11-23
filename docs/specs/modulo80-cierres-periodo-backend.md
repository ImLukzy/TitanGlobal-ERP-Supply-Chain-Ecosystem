# Especificación: modulo80-cierres-periodo-backend — Cierres de periodo

## 1. Objetivo
**Problema:** al terminar un mes o un año contable no debería poder cambiarse lo que ya se reportó, pero el ERP no tiene registro de qué periodos están cerrados.
**Resultado esperado:** una entidad nueva, `CierrePeriodo`, con su tabla y `Modulo80_CierresPeriodo` en `api/v1/cierres-periodo`:
- cerrar un mes o un año, y reabrirlo con motivo;
- consultar si una fecha cae en un periodo cerrado, para que otros módulos decidan si aceptan una operación;
- listado con filtros y paginación y resumen.

Sigue el patrón de `Modulo14_Licitaciones` (estados con acciones explícitas). Esta spec **solo registra y consulta** los cierres; no los hace cumplir en los demás módulos.

## 2. Fuera de alcance
- Las 4 entidades del enunciado, los middlewares y el pipeline.
- Bloquear operaciones en otros módulos: ninguno consulta todavía «¿está cerrado?». Cada módulo lo hará con su propia spec, usando el endpoint de estado de §4.
- Asientos contables de cierre, cálculo de saldos o resultados.
- Registrar qué usuario cerró o reabrió: se hará con la bitácora de auditoría (módulo 77).
- Permisos finos por acción: cerrar y reabrir usan los permisos que ya tenga el módulo en la matriz (72).
- El frontend (spec `modulo80-cierres-periodo-frontend.md`, posterior).
- Scripts de `database/` que no sean el de §3.

**Decisiones de producto que requieren aprobación antes de ejecutar:**
1. **Dos tipos de periodo:** `Mensual` (año y mes) y `Anual` (solo año).
2. **Estados:** `Cerrado` y `Reabierto`. Un periodo sin fila nunca se ha cerrado. Cerrar uno `Reabierto` lo vuelve a `Cerrado`.
3. **Solo se cierra un periodo ya terminado:** su último día debe ser anterior a hoy (UTC).
4. **Los meses se cierran en orden:** para cerrar un mes, el mes anterior debe estar `Cerrado`. La excepción es el primer cierre mensual del sistema, que puede ser de cualquier mes.
5. **El cierre anual exige** los 12 meses del año `Cerrados`.
6. **Reabrir exige motivo** y se hace de atrás hacia adelante: no se reabre un mes si el siguiente sigue cerrado, ni un mes si el año está cerrado.
7. **Una fecha está cerrada** si su mes está `Cerrado` o si su año está `Cerrado`.
8. **No hay baja:** los cierres no se eliminan, solo se reabren.
9. **El rango de años** admitido es 2000–2100.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `database/033-cierres-periodo.sql` | crear | `CREATE TABLE "CierresPeriodo"`; número `033` reservado en `docs/equipo/relevo.md` §8 |
| `docs/equipo/relevo.md` | modificar | solo añadir la fila 033, sin reescribir la tabla |
| `src/TitanGlobal.Domain/Models/CierrePeriodo.cs` | regenerar | entidad `partial` generada por `./scripts/scaffold.sh`, sin editarla a mano |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | `DbSet<CierrePeriodo>`, generado |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | modificar | solo si el scaffold nombra mal la entidad (`CierrePeriodo` / `CierresPeriodo`) |
| `src/TitanGlobal.Application/DTOs/Modulo80_CierresPeriodo/*.cs` | crear | DTOs de entrada y salida (§4) |
| `src/TitanGlobal.Application/UseCases/Modulo80_CierresPeriodo/*.cs` | crear | un caso de uso por operación y `ValidacionCierrePeriodo` |
| `src/TitanGlobal.Api/Controllers/Modulo80_CierresPeriodo/CierresPeriodoController.cs` | crear | `[Route("api/v1/cierres-periodo")]`, controlador delgado |
| `tests/TitanGlobal.Api.Tests/Modulo80_CierresPeriodo/CierresPeriodoApiTests.cs` | crear | pruebas de integración con InMemory, datos propios del test |
| `scripts/seed-dev.sql` | modificar | 11 cierres idempotentes al final (§4) |
| `scripts/smoke-postgres.sh` | modificar | rutas del módulo (§5 A6) |

## 4. Diseño y lógica
**Tabla `CierresPeriodo`:**

| Columna | Tipo | Regla |
|---|---|---|
| `Id` | `integer` identidad | clave primaria |
| `Tipo` | `character varying(10) NOT NULL` | `CHECK` en (`Mensual`, `Anual`) |
| `Anio` | `smallint NOT NULL` | `CHECK` entre 2000 y 2100 |
| `Mes` | `smallint` | `CHECK` entre 1 y 12; obligatorio si `Mensual`, nulo si `Anual` |
| `Estado` | `character varying(10) NOT NULL` | `CHECK` en (`Cerrado`, `Reabierto`) |
| `FechaCierre` | `timestamp with time zone NOT NULL` | UTC; la del último cierre |
| `Observaciones` | `character varying(500)` | opcional |
| `FechaReapertura` | `timestamp with time zone` | UTC; solo si hubo reapertura |
| `MotivoReapertura` | `character varying(500)` | obligatorio si hubo reapertura |

`CHECK` adicional `CK_CierresPeriodo_MesSegunTipo`: `(Tipo = 'Mensual' AND Mes IS NOT NULL) OR (Tipo = 'Anual' AND Mes IS NULL)`. Índice único `IX_CierresPeriodo_Tipo_Anio_Mes` sobre `(Tipo, Anio, Mes)` con `NULLS NOT DISTINCT`, para que haya un solo cierre anual por año. Los `CHECK` siguen la convención `CK_<Tabla>_<Regla>`.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }` sin `Detail`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/cierres-periodo?anio=&tipo=&estado=&pagina=1&tamanoPagina=20` | todos opcionales | 200 `PaginaCierresDto` | 400 |
| GET | `api/v1/cierres-periodo/{id:int}` | — | 200 `CierreDto` | 404 |
| POST | `api/v1/cierres-periodo` | `CerrarPeriodoRequestDto` | 200 `{ id }` | 400, 409 |
| POST | `api/v1/cierres-periodo/{id:int}/reabrir` | `ReabrirPeriodoRequestDto { motivo }` | 200 `{ id }` | 400, 404, 409 |
| GET | `api/v1/cierres-periodo/estado?fecha=` | `fecha` obligatoria | 200 `EstadoFechaDto` | 400 |
| GET | `api/v1/cierres-periodo/resumen` | — | 200 `ResumenCierresDto` | — |

**DTOs:**
- `CerrarPeriodoRequestDto { tipo, anio, mes?, observaciones? }`
- `CierreDto { id, tipo, anio, mes, periodo, estado, fechaCierre, observaciones, fechaReapertura, motivoReapertura }` (`periodo` es `2026-09` para un mes y `2026` para un año).
- `EstadoFechaDto { fecha, cerrada, periodo }` (`periodo` es el cierre que la cubre, o `null` si no está cerrada).
- `PaginaCierresDto { items, total, pagina, tamanoPagina }`
- `ResumenCierresDto { totalCierres, cerrados, reabiertos, ultimoMesCerrado }` (`ultimoMesCerrado` es `{ anio, mes }` del mensual `Cerrado` más reciente, o `null`).

**Reglas y mensajes** (400 en validación; 404 si el recurso no existe; 409 en conflictos de estado).

*POST cerrar*, en este orden:
1. `tipo` distinto de `Mensual` y `Anual` (sin distinguir mayúsculas) → 400 «El tipo debe ser Mensual o Anual.»
2. `anio` fuera de 2000–2100 → 400 «El año debe estar entre 2000 y 2100.»
3. `Mensual` sin `mes` o fuera de 1–12 → 400 «El mes debe estar entre 1 y 12.»; `Anual` con `mes` → 400 «El cierre anual no lleva mes.»
4. `observaciones` más de 500 → 400 «Las observaciones no pueden superar 500 caracteres.» (en blanco → `null`).
5. periodo no terminado (último día no anterior a hoy) → 409 «El periodo {periodo} aún no termina.»
6. periodo ya `Cerrado` → 409 «El periodo {periodo} ya está cerrado.»
7. `Mensual` con mes anterior no `Cerrado`, salvo que no exista ningún cierre mensual → 409 «Primero debe cerrarse el periodo {anterior}.» (el anterior de enero es diciembre del año previo).
8. `Anual` con meses del año sin cerrar → 409 «El año {anio} tiene meses sin cerrar: {lista}.» (meses separados por coma, por ejemplo `2026-10, 2026-11`).
Si el periodo existe `Reabierto`, se actualiza a `Cerrado` con nueva `FechaCierre` (conserva la reapertura anterior); si no existe, se crea.

*POST reabrir:* 404 «El cierre {id} no existe.»; `motivo` en blanco → 400 «El motivo de reapertura es obligatorio.» (máximo 500 → «El motivo no puede superar 500 caracteres.»); estado `Reabierto` → 409 «El periodo {periodo} ya está reabierto.»; mensual con el año `Cerrado` → 409 «Primero debe reabrirse el cierre anual {anio}.»; mensual con el mes siguiente `Cerrado` → 409 «Primero debe reabrirse el periodo {siguiente}.» Guarda motivo y fecha y pasa a `Reabierto`.
*GET estado:* `fecha` ausente o inválida → 400 «La fecha es obligatoria.» `cerrada` es verdadero si el mes de la fecha está `Cerrado` o si su año está `Cerrado`; `periodo` es el mensual si lo cubre el mes, y si no el anual.
*GET detalle:* 404 «El cierre {id} no existe.» *Listado:* 400 «La página debe ser mayor o igual a 1.», «El tamaño de página debe estar entre 1 y 100.», «El estado debe ser Cerrado o Reabierto.», 400 con el mensaje de tipo. Orden: año descendente, tipo (`Anual` primero) y mes descendente.

**Consultas.** Desde `Repository<...>().AsQueryable()`, asíncronas y proyectadas a DTO en la consulta; las reglas de orden y de meses leen solo los cierres del año involucrado.

**Seed de desarrollo** (`seed-dev.sql`, bloque `TRUNCATE ... RESTART IDENTITY` con ids explícitos y `setval`; las fechas de cierre son fijas, no `now()`; el seed no pasa por las reglas de orden):

| Cierres | Estado |
|---|---|
| Anual 2025 | Cerrado |
| Mensual 2026-01 a 2026-09 (9 filas) | Cerrado |
| Mensual 2026-10 | Reabierto («Ajuste de provisiones») |

Resumen esperado del seed: 11 cierres, 10 cerrados, 1 reabierto y último mes cerrado 2026-09. Fechas: el 2026-09-15 y el 2025-06-10 están cerradas (esta por el cierre anual); el 2026-10-15 y el 2026-11-10 no lo están.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Gate | `./scripts/verify.sh` (formato, reglas de código y capas) | en verde |
| A3 | Reglas de cierre | tests: cada mensaje del cierre de §4 con su código, literal y sin `Detail`; periodo no terminado; cierre en orden y primer cierre libre; cierre anual con meses faltantes y su lista | pasan |
| A4 | Reapertura | tests: motivo obligatorio, mes siguiente cerrado, año cerrado, ya reabierto, y volver a cerrar un periodo reabierto | pasan |
| A5 | Estado de una fecha | tests con las cuatro fechas del seed y el resultado esperado de cada una | pasan |
| A6 | Datos y PostgreSQL | el script aplica con `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh --comprobar` no da diferencias y `./scripts/smoke-postgres.sh` prueba listado, estado de fechas, resumen, cierre de 2026-10 y su reapertura | `SMOKE OK` |
| A7 | OpenAPI | `/openapi/v1.json` incluye las 6 operaciones | presentes |

## 6. Checklist de ejecución
*(Marca `[x]` solo cuando la tarea esté hecha **y** A1–A2 estén en verde.)*

- [ ] Tarea 1: script SQL, aplicarlo y generar la entidad con scaffold.
- [ ] Tarea 2: DTOs, `ValidacionCierrePeriodo`, casos de uso y controlador.
- [ ] Tarea 3: tests, seed y smoke.
- [ ] Tarea 4: verificar A1–A7 y anotar resultados en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| — | — | pendiente | spec sin aprobar |
