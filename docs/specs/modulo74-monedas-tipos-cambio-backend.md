# Especificación: modulo74-monedas-tipos-cambio-backend — Monedas y tipos de cambio

## 1. Objetivo
**Problema:** el ERP compra y vende en varias monedas (importaciones, listas de precios, facturación), pero no existe un catálogo de monedas ni una tasa de cambio diaria; cada módulo tendría que inventar la suya.
**Resultado esperado:** dos entidades nuevas, `Moneda` y `TipoCambio`, con sus tablas y `Modulo74_MonedasTiposCambio` en `api/v1/monedas` y `api/v1/tipos-cambio`:
- catálogo de monedas con una única moneda base;
- tasas diarias de compra y venta de cada moneda frente a la base;
- consultar la tasa vigente en una fecha y convertir un monto entre dos monedas;
- listados con filtros y paginación, y resumen.

Sigue el patrón de `Modulo11_Proveedores` (catálogo con activar y desactivar) y de `Modulo75_ImpuestosTasas` (valor con vigencia). Los demás módulos lo consumirán más adelante; esta spec no los modifica.

## 2. Fuera de alcance
- Las 4 entidades del enunciado, los middlewares y el pipeline.
- Cambios en otros módulos: el costeo de importación (18), las listas de precios (06) y la facturación (51) seguirán en moneda base hasta que tengan su propia spec.
- Carga automática de tasas desde un servicio externo: las tasas se registran a mano.
- Revaluación de saldos y diferencia de cambio contable.
- El frontend (spec `modulo74-monedas-tipos-cambio-frontend.md`, posterior).
- Scripts de `database/` que no sean el de §3.

**Decisiones de producto que requieren aprobación antes de ejecutar:**
1. **Una sola moneda base** (índice único filtrado sobre `EsBase`); la del seed es `PEN`. No admite tasas: vale 1 frente a sí misma.
2. **El código** es el ISO 4217 de 3 letras, único y en mayúsculas.
3. **Las tasas se expresan en unidades de moneda base por una unidad de la moneda** (por ejemplo, 1 USD = 3.7500 PEN), con 6 decimales.
4. **Una tasa por moneda y fecha**, con `compra` y `venta` mayores a 0 y `venta` no menor que `compra`.
5. **No se registran tasas con fecha futura** ni para monedas inactivas.
6. **La tasa vigente de una fecha** es la más reciente con fecha igual o anterior; si no hay ninguna, es un error (no se inventa una).
7. **Conversión:** el que llama indica si usa la tasa `Compra` o `Venta`; convierte pasando siempre por la moneda base y redondea a 2 decimales.
8. **Solo se elimina una moneda sin tasas y que no sea la base;** si no, se desactiva. La base no se desactiva.
9. **Corregir una tasa** (editar o eliminar) está permitido: aún no hay documentos que las referencien.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `database/031-monedas-tipos-cambio.sql` | crear | `CREATE TABLE "Monedas"` y `"TiposCambio"`; número `031` reservado en `docs/equipo/relevo.md` §8 |
| `docs/equipo/relevo.md` | modificar | solo añadir la fila 031, sin reescribir la tabla |
| `src/TitanGlobal.Domain/Models/Moneda.cs`, `TipoCambio.cs` | regenerar | entidades `partial` generadas por `./scripts/scaffold.sh`, sin editarlas a mano |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | `DbSet<Moneda>` y `DbSet<TipoCambio>`, generado |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | modificar | solo si el scaffold nombra mal las entidades (`Moneda`, `TipoCambio`) |
| `src/TitanGlobal.Application/DTOs/Modulo74_MonedasTiposCambio/*.cs` | crear | DTOs de entrada y salida (§4) |
| `src/TitanGlobal.Application/UseCases/Modulo74_MonedasTiposCambio/*.cs` | crear | un caso de uso por operación y `ValidacionMoneda` / `ValidacionTipoCambio` |
| `src/TitanGlobal.Api/Controllers/Modulo74_MonedasTiposCambio/MonedasController.cs`, `TiposCambioController.cs` | crear | controladores delgados; las acciones sin cuerpo llevan `[PermitirCuerpoVacio]` |
| `tests/TitanGlobal.Api.Tests/Modulo74_MonedasTiposCambio/MonedasApiTests.cs`, `TiposCambioApiTests.cs` | crear | pruebas de integración con InMemory, datos propios del test |
| `scripts/seed-dev.sql` | modificar | 4 monedas y 4 tasas idempotentes al final (§4) |
| `scripts/smoke-postgres.sh` | modificar | rutas del módulo (§5 A7) |

## 4. Diseño y lógica
**Tabla `Monedas`:**

| Columna | Tipo | Regla |
|---|---|---|
| `Id` | `integer` identidad | clave primaria |
| `Codigo` | `character varying(3) NOT NULL` | único (`IX_Monedas_Codigo`) |
| `Nombre` | `character varying(60) NOT NULL` | |
| `Simbolo` | `character varying(5) NOT NULL` | |
| `EsBase` | `boolean NOT NULL` | índice único filtrado `IX_Monedas_EsBase` con `WHERE "EsBase"` |
| `Activa` | `boolean NOT NULL` | |

**Tabla `TiposCambio`:**

| Columna | Tipo | Regla |
|---|---|---|
| `Id` | `integer` identidad | clave primaria |
| `MonedaId` | `integer NOT NULL` | clave foránea a `Monedas("Id")` `ON DELETE RESTRICT` |
| `Fecha` | `date NOT NULL` | |
| `Compra` | `numeric(12,6) NOT NULL` | `CHECK` > 0 |
| `Venta` | `numeric(12,6) NOT NULL` | `CHECK` > 0 |

`CHECK` adicional `CK_TiposCambio_VentaNoMenorQueCompra`. Índice único `IX_TiposCambio_MonedaId_Fecha`. Los `CHECK` siguen la convención `CK_<Tabla>_<Regla>`.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }` sin `Detail`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/monedas?busqueda=&activa=&pagina=1&tamanoPagina=20` | todos opcionales | 200 `PaginaMonedasDto` | 400 |
| GET | `api/v1/monedas/{id:int}` | — | 200 `MonedaDto` | 404 |
| POST | `api/v1/monedas` | `GuardarMonedaRequestDto` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/monedas/{id:int}` | `GuardarMonedaRequestDto` | 200 `{ id }` | 400, 404, 409 |
| POST | `api/v1/monedas/{id:int}/activar` y `/desactivar` | sin cuerpo | 200 `{ id }` | 404, 409 |
| DELETE | `api/v1/monedas/{id:int}` | — | 204 | 404, 409 |
| GET | `api/v1/tipos-cambio?monedaId=&desde=&hasta=&pagina=1&tamanoPagina=20` | todos opcionales | 200 `PaginaTiposCambioDto` | 400 |
| POST | `api/v1/tipos-cambio` | `GuardarTipoCambioRequestDto` | 200 `{ id }` | 400, 404, 409 |
| PUT | `api/v1/tipos-cambio/{id:int}` | `ActualizarTipoCambioRequestDto { compra, venta }` | 200 `{ id }` | 400, 404 |
| DELETE | `api/v1/tipos-cambio/{id:int}` | — | 204 | 404 |
| GET | `api/v1/tipos-cambio/vigente?monedaId=&fecha=` | `fecha` opcional (hoy) | 200 `TipoCambioDto` | 400, 404 |
| GET | `api/v1/tipos-cambio/convertir?monto=&de=&a=&tasa=&fecha=` | `fecha` opcional (hoy) | 200 `ConversionDto` | 400, 404 |
| GET | `api/v1/monedas/resumen` | — | 200 `ResumenMonedasDto` | — |

**DTOs:**
- `GuardarMonedaRequestDto { codigo, nombre, simbolo, esBase }`
- `MonedaDto { id, codigo, nombre, simbolo, esBase, activa, totalTiposCambio }`
- `GuardarTipoCambioRequestDto { monedaId, fecha, compra, venta }`
- `TipoCambioDto { id, monedaId, monedaCodigo, fecha, compra, venta }`
- `ConversionDto { monto, de, a, tasa, fechaTipoCambio, resultado }` (`fechaTipoCambio` es la fecha de la tasa realmente usada; cuando la conversión pasa por dos monedas, la más antigua de las dos).
- `PaginaMonedasDto` y `PaginaTiposCambioDto { items, total, pagina, tamanoPagina }`
- `ResumenMonedasDto { totalMonedas, monedasActivas, monedaBase, totalTiposCambio, ultimaFecha }` (`ultimaFecha` es la fecha de la tasa más reciente o `null`).

**Reglas y mensajes** (400 en validación; 404 si el recurso no existe; 409 en conflictos). La búsqueda de monedas es sobre código y nombre, sin distinguir mayúsculas.

*Moneda (alta y edición):* `codigo` que no sean 3 letras → 400 «El código debe tener 3 letras.» (se guarda en mayúsculas); `nombre` en blanco → 400 «El nombre es obligatorio.» (máximo 60 → «El nombre no puede superar 60 caracteres.»); `simbolo` en blanco → 400 «El símbolo es obligatorio.» (máximo 5). Código repetido → 409 «Ya existe una moneda con el código {codigo}.» (también ante `23505`). Marcar `esBase` con otra moneda ya base → 409 «Ya existe una moneda base ({codigo}).»; quitar la marca base a la que lo es → 409 «Debe existir una moneda base.»; hacer base una moneda con tasas → 409 «La moneda {codigo} tiene tasas y no puede ser la base.» La moneda nace activa. Id inexistente → 404 «La moneda {id} no existe.» Activar una activa → 409 «La moneda {id} ya está activa.»; desactivar una inactiva → 409 «La moneda {id} ya está inactiva.»; desactivar la base → 409 «La moneda base no se puede desactivar.» Eliminar con tasas → 409 «La moneda {id} tiene tasas y no se puede eliminar; desactívela.»; eliminar la base → 409 «La moneda base no se puede eliminar.»

*POST tipo de cambio*, en este orden: `monedaId` inexistente → 404 «La moneda {id} no existe.»; `fecha` futura → 400 «La fecha no puede ser futura.»; moneda base → 409 «La moneda base no admite tipo de cambio.»; moneda inactiva → 409 «La moneda {codigo} está inactiva.»; `compra` o `venta` ≤ 0 → 400 «La compra debe ser mayor a 0.» / «La venta debe ser mayor a 0.»; con más de 6 decimales o más de 6 dígitos enteros → 400 «La compra no es válida.» / «La venta no es válida.»; `venta` menor que `compra` → 400 «La venta no puede ser menor que la compra.»; ya existe esa moneda y fecha → 409 «Ya existe un tipo de cambio de {codigo} para el {fecha:yyyy-MM-dd}.» (también ante `23505`).
*PUT:* 404 «El tipo de cambio {id} no existe.»; luego las reglas de `compra` y `venta`. *DELETE:* 404.
*GET vigente:* 400 «La moneda es obligatoria.»; 404 «La moneda {id} no existe.»; base → 400 «La moneda base no tiene tipo de cambio.»; sin tasa hasta esa fecha → 404 «No hay tipo de cambio de {codigo} hasta el {fecha:yyyy-MM-dd}.»
*GET convertir:* `monto` ≤ 0 → 400 «El monto debe ser mayor a 0.»; `de` o `a` no son códigos de monedas existentes → 404 «La moneda {codigo} no existe.»; `tasa` distinta de `Compra` y `Venta` (sin distinguir mayúsculas) → 400 «La tasa debe ser Compra o Venta.»; `de` igual a `a` → 200 con el mismo monto; una moneda sin tasa vigente → 404 como en vigente. Con `de` extranjera y `a` base: `monto × tasa`; con `de` base y `a` extranjera: `monto / tasa`; entre dos extranjeras: pasa por la base con la misma tasa elegida. Redondea a 2 decimales (hacia el más cercano, mitades hacia arriba).
*Listados:* 400 «La página debe ser mayor o igual a 1.», «El tamaño de página debe estar entre 1 y 100.», «La fecha inicial no puede ser mayor que la final.» Monedas ordenadas por código; tasas por fecha descendente y luego código.

**Consultas.** Desde `Repository<...>().AsQueryable()`, asíncronas y proyectadas a DTO en la consulta (sin N+1); `totalTiposCambio` sale de un conteo en la misma consulta.

**Seed de desarrollo** (`seed-dev.sql`, bloque `TRUNCATE ... RESTART IDENTITY CASCADE` con ids explícitos y `setval`):

| Id | Código | Nombre | Símbolo | Base | Activa |
|---|---|---|---|---|---|
| 1 | PEN | Sol | S/ | sí | sí |
| 2 | USD | Dólar estadounidense | US$ | no | sí |
| 3 | EUR | Euro | € | no | sí |
| 4 | CNY | Yuan renminbi | ¥ | no | no |

| Moneda | Fecha | Compra | Venta |
|---|---|---|---|
| USD | 2026-11-20 | 3.750000 | 3.765000 |
| USD | 2026-11-23 | 3.748000 | 3.761000 |
| EUR | 2026-11-23 | 4.020000 | 4.050000 |
| CNY | 2026-10-01 | 0.520000 | 0.530000 |

Resumen esperado del seed: 4 monedas, 3 activas, base `PEN`, 4 tasas y última fecha 2026-11-23. Conversión de ejemplo: 100 USD a PEN con tasa `Venta` el 2026-11-23 → 376.10.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Gate | `./scripts/verify.sh` (formato, reglas de código y capas) | en verde |
| A3 | Monedas | tests: cada mensaje de alta, edición, activación y baja de §4 con su código, literal y sin `Detail`; código en mayúsculas; una sola base | pasan |
| A4 | Tipos de cambio | tests: cada mensaje del alta y la edición; moneda base, inactiva, fecha futura, duplicado por moneda y fecha, venta menor que compra | pasan |
| A5 | Vigente | tests: el 2026-11-22 devuelve la tasa del 2026-11-20; sin tasa anterior devuelve 404 con el literal | pasan |
| A6 | Conversión | tests con números conocidos: 100 USD → PEN con `Venta` = 376.10; 376.10 PEN → USD = 100.00; USD → EUR pasando por la base; misma moneda; tasa inválida; redondeo | pasan |
| A7 | Datos y PostgreSQL | el script aplica con `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh --comprobar` no da diferencias y `./scripts/smoke-postgres.sh` prueba listados, vigente, convertir, altas y duplicados | `SMOKE OK` |
| A8 | OpenAPI | `/openapi/v1.json` incluye las 14 operaciones | presentes |

## 6. Checklist de ejecución
*(Marca `[x]` solo cuando la tarea esté hecha **y** A1–A2 estén en verde.)*

- [ ] Tarea 1: script SQL, aplicarlo y generar entidades con scaffold.
- [ ] Tarea 2: DTOs, validaciones, casos de uso y controladores.
- [ ] Tarea 3: tests, seed y smoke.
- [ ] Tarea 4: verificar A1–A8 y anotar resultados en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| — | — | pendiente | spec sin aprobar |
