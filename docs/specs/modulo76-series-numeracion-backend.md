# Especificación: modulo76-series-numeracion-backend — Series de numeración

## 1. Objetivo
**Problema:** los documentos del ERP (facturas, boletas, notas, guías) necesitan un correlativo sin saltos ni repetidos, y hoy cada módulo tendría que llevar su propio contador.
**Resultado esperado:** una entidad nueva, `SerieNumeracion`, con su tabla y `Modulo76_SeriesNumeracion` en `api/v1/series-numeracion`:
- catálogo de series por tipo de documento (por ejemplo `F001` para facturas);
- entregar el siguiente número de una serie de forma atómica: dos peticiones simultáneas nunca reciben el mismo;
- activar, desactivar y eliminar series;
- listado con filtros y paginación y resumen.

Sigue el patrón de `Modulo11_Proveedores` (catálogo con activar y desactivar). Los módulos de documentos (por ejemplo, facturación electrónica, módulo 51) lo consumirán más adelante; esta spec no los modifica.

## 2. Fuera de alcance
- Las 4 entidades del enunciado, los middlewares y el pipeline.
- Cambios en otros módulos: ninguno llama todavía a «siguiente número».
- Series por sucursal (módulo 73) y por punto de emisión.
- Numeración de los documentos de compras (`OA-…`, `SC-…`, `AR-…`): esos módulos reciben el número escrito por el usuario.
- Reservar o anular números individuales, o reutilizar números anulados.
- El frontend (spec `modulo76-series-numeracion-frontend.md`, posterior).
- Scripts de `database/` que no sean el de §3.

**Decisiones de producto que requieren aprobación antes de ejecutar:**
1. **Tipos de documento:** `Factura`, `Boleta`, `NotaCredito`, `NotaDebito` y `GuiaRemision`.
2. **La serie** tiene 4 caracteres alfanuméricos en mayúsculas (`F001`, `B001`, `T001`); es única por tipo de documento.
3. **El correlativo** se entrega como `{serie}-{número a 8 dígitos}` (por ejemplo `F001-00001251`); el máximo es 99 999 999.
4. **El número inicial** se puede indicar al crear la serie (para continuar una numeración que ya existía); por defecto empieza en 0 y el primer número entregado es el 1. Después **no se edita**: solo avanza al pedir el siguiente.
5. **Solo series activas** entregan números.
6. **Solo se elimina una serie que no ha emitido ningún número** (`ultimoNumero = 0`); si ya emitió, se desactiva.
7. **Concurrencia:** el incremento es atómico y se confirma contra PostgreSQL en el smoke. La forma de configurarlo sin editar el `TitanGlobalDbContext.cs` generado se define al implementar y se acuerda con el integrador.
8. **Pedir el siguiente número consume ese número** aunque el documento no se llegue a emitir; no hay forma de devolverlo.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `database/032-series-numeracion.sql` | crear | `CREATE TABLE "SeriesNumeracion"`; número `032` reservado en `docs/equipo/relevo.md` §8 |
| `docs/equipo/relevo.md` | modificar | solo añadir la fila 032, sin reescribir la tabla |
| `src/TitanGlobal.Domain/Models/SerieNumeracion.cs` | regenerar | entidad `partial` generada por `./scripts/scaffold.sh`, sin editarla a mano |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | `DbSet<SerieNumeracion>`, generado |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | modificar | solo si el scaffold nombra mal la entidad (`SerieNumeracion` / `SeriesNumeracion`) |
| `src/TitanGlobal.Application/DTOs/Modulo76_SeriesNumeracion/*.cs` | crear | DTOs de entrada y salida (§4) |
| `src/TitanGlobal.Application/UseCases/Modulo76_SeriesNumeracion/*.cs` | crear | un caso de uso por operación y `ValidacionSerieNumeracion` |
| `src/TitanGlobal.Api/Controllers/Modulo76_SeriesNumeracion/SeriesNumeracionController.cs` | crear | `[Route("api/v1/series-numeracion")]`, controlador delgado; las acciones sin cuerpo llevan `[PermitirCuerpoVacio]` |
| `tests/TitanGlobal.Api.Tests/Modulo76_SeriesNumeracion/SeriesNumeracionApiTests.cs` | crear | pruebas de integración con InMemory, datos propios del test |
| `scripts/seed-dev.sql` | modificar | 6 series idempotentes al final (§4) |
| `scripts/smoke-postgres.sh` | modificar | rutas del módulo (§5 A6) |

## 4. Diseño y lógica
**Tabla `SeriesNumeracion`:**

| Columna | Tipo | Regla |
|---|---|---|
| `Id` | `integer` identidad | clave primaria |
| `TipoDocumento` | `character varying(20) NOT NULL` | `CHECK` en los 5 valores de la decisión 1 |
| `Serie` | `character varying(4) NOT NULL` | |
| `Descripcion` | `character varying(100)` | opcional |
| `UltimoNumero` | `integer NOT NULL` | `CHECK` entre 0 y 99999999; empieza en 0 |
| `Activa` | `boolean NOT NULL` | |
| `FechaRegistro` | `timestamp with time zone NOT NULL` | UTC, la pone el servidor |

Índice único `IX_SeriesNumeracion_TipoDocumento_Serie`. Los `CHECK` siguen la convención `CK_<Tabla>_<Regla>`.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }` sin `Detail`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/series-numeracion?busqueda=&tipoDocumento=&activa=&pagina=1&tamanoPagina=20` | todos opcionales | 200 `PaginaSeriesDto` | 400 |
| GET | `api/v1/series-numeracion/{id:int}` | — | 200 `SerieDto` | 404 |
| POST | `api/v1/series-numeracion` | `CrearSerieRequestDto` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/series-numeracion/{id:int}` | `ActualizarSerieRequestDto { descripcion? }` | 200 `{ id }` | 400, 404 |
| POST | `api/v1/series-numeracion/{id:int}/activar` y `/desactivar` | sin cuerpo | 200 `{ id }` | 404, 409 |
| POST | `api/v1/series-numeracion/{id:int}/siguiente` | sin cuerpo | 200 `NumeroEmitidoDto` | 404, 409 |
| DELETE | `api/v1/series-numeracion/{id:int}` | — | 204 | 404, 409 |
| GET | `api/v1/series-numeracion/resumen` | — | 200 `ResumenSeriesDto` | — |

**DTOs:**
- `CrearSerieRequestDto { tipoDocumento, serie, descripcion?, ultimoNumero? }`
- `SerieDto { id, tipoDocumento, serie, descripcion, ultimoNumero, siguienteCorrelativo, activa, fechaRegistro }` (`siguienteCorrelativo` es una vista previa del que se entregaría; **no** lo consume).
- `NumeroEmitidoDto { serieId, tipoDocumento, serie, numero, correlativo }` (por ejemplo `{ …, "numero": 1251, "correlativo": "F001-00001251" }`).
- `PaginaSeriesDto { items, total, pagina, tamanoPagina }`
- `ResumenSeriesDto { totalSeries, seriesActivas, porTipoDocumento: TipoConteoDto[] { tipoDocumento, total } }` (siempre los 5 tipos, en el orden de la decisión 1).

**Reglas y mensajes** (400 en validación; 404 si el recurso no existe; 409 en conflictos). La búsqueda es sobre serie y descripción, sin distinguir mayúsculas.

*POST serie*, en este orden:
1. `tipoDocumento` fuera de los 5 valores (sin distinguir mayúsculas) → 400 «El tipo de documento debe ser Factura, Boleta, NotaCredito, NotaDebito o GuiaRemision.»
2. `serie` que no sean 4 caracteres alfanuméricos → 400 «La serie debe tener 4 caracteres alfanuméricos.» (se guarda con `Trim()` y en mayúsculas).
3. `descripcion` más de 100 → 400 «La descripción no puede superar 100 caracteres.» (en blanco → `null`).
4. `ultimoNumero` fuera de 0–99999999 → 400 «El número inicial debe estar entre 0 y 99999999.» (si no se envía, 0).
5. serie repetida para ese tipo → 409 «Ya existe la serie {serie} de {tipoDocumento}.» (también ante `23505`).
La serie nace activa.

*PUT serie:* 404 «La serie {id} no existe.»; `descripcion` más de 100 → 400 (mismo mensaje). Solo cambia la descripción.
*Activar / desactivar:* 404; activar una activa → 409 «La serie {id} ya está activa.»; desactivar una inactiva → 409 «La serie {id} ya está inactiva.»
*POST siguiente:* 404 «La serie {id} no existe.»; inactiva → 409 «La serie {serie} de {tipoDocumento} está inactiva.»; `ultimoNumero` ya en 99999999 → 409 «La serie {serie} de {tipoDocumento} llegó a su número máximo.» Si el incremento choca con otro simultáneo, se reintenta hasta 3 veces; si aun así falla → 409 «No se pudo reservar el número; intente nuevamente.»
*DELETE:* 404; `ultimoNumero` mayor que 0 → 409 «La serie {id} ya emitió números y no se puede eliminar; desactívela.»
*GET detalle:* 404 «La serie {id} no existe.» *Listado:* 400 «La página debe ser mayor o igual a 1.», «El tamaño de página debe estar entre 1 y 100.», 400 con el mensaje del tipo de documento. Orden: tipo de documento y luego serie.

**Consultas.** Desde `Repository<...>().AsQueryable()`, asíncronas y proyectadas a DTO en la consulta.

**Seed de desarrollo** (`seed-dev.sql`, bloque `TRUNCATE ... RESTART IDENTITY` con ids explícitos y `setval`):

| Id | Tipo | Serie | Último número | Activa |
|---|---|---|---|---|
| 1 | Factura | F001 | 1250 | sí |
| 2 | Factura | F002 | 0 | sí |
| 3 | Boleta | B001 | 3480 | sí |
| 4 | NotaCredito | FC01 | 15 | sí |
| 5 | GuiaRemision | T001 | 892 | sí |
| 6 | Boleta | B002 | 77 | no |

Resumen esperado del seed: 6 series, 5 activas, por tipo Factura 2 / Boleta 2 / NotaCredito 1 / NotaDebito 0 / GuiaRemision 1.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Gate | `./scripts/verify.sh` (formato, reglas de código y capas) | en verde |
| A3 | Reglas de la serie | tests: cada mensaje de alta, edición, activación y baja de §4 con su código, literal y sin `Detail`; serie guardada en mayúsculas; misma serie en tipos distintos | pasan |
| A4 | Siguiente número | tests: de 1250 entrega 1251 con correlativo `F001-00001251`; de 0 entrega 1; la vista previa no consume; serie inactiva; serie en el máximo; 20 peticiones en paralelo reciben 20 números distintos y consecutivos | pasan |
| A5 | Consultas | tests con datos conocidos: filtros, búsqueda, paginación con `total` y resumen exacto del seed | pasan |
| A6 | Datos y PostgreSQL | el script aplica con `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh --comprobar` no da diferencias y `./scripts/smoke-postgres.sh` prueba listado, detalle, alta, siguiente (dos veces seguidas con números consecutivos), desactivar y duplicado | `SMOKE OK` |
| A7 | OpenAPI | `/openapi/v1.json` incluye las 9 operaciones | presentes |

## 6. Checklist de ejecución
*(Marca `[x]` solo cuando la tarea esté hecha **y** A1–A2 estén en verde.)*

- [ ] Tarea 1: script SQL, aplicarlo y generar la entidad con scaffold.
- [ ] Tarea 2: DTOs, `ValidacionSerieNumeracion`, casos de uso y controlador.
- [ ] Tarea 3: tests, seed y smoke.
- [ ] Tarea 4: verificar A1–A7 y anotar resultados en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| — | — | pendiente | spec sin aprobar |
