# Especificación: modulo67-adelantos-prestamos-backend — AdelantosPrestamos (backend)

> Estado: **implementada con las opciones recomendadas de §2**, por acuerdo con Lukas del 2026-11-22 de implementar los módulos 62 a 70 en secuencia y revisarlos al final. Pendiente de revisión final por Lukas.
> **Orden:** se implementa antes que el 66 Planillas, porque la planilla descuenta sus cuotas (P4); así el 66 se construye ya integrado.

## 1. Objetivo
**Problema:** no se registran adelantos ni préstamos al personal. Planillas (66) debe descontar sus cuotas (L3, P4).
**Resultado esperado:** tablas `Prestamos` y `CuotasPrestamo` y `Modulo67_AdelantosPrestamos` en `api/v1/prestamos`: alta con cronograma de cuotas mensuales (L1), tope de cuota sobre el sueldo neto estimado (L2), estados `Activo` / `Cancelado` / `Anulado` (L4), y una operación pública que el 66 usará para descontar cuotas sin superar el saldo (L3).

## 2. Fuera de alcance
- Liquidación final al cese (L5). Intereses. Descuentos fuera de planilla.
- Cambios en otros módulos, `Middlewares/`, `Services/`, `Program.cs` y `UnitOfWork`.

**Decisiones de producto (opción recomendada aplicada):**
1. **Tipos.** `Adelanto` (siempre 1 cuota, mensaje 6 si no) y `Prestamo` (1 a 24 cuotas **[elegido]**).
2. **Cronograma.** Una cuota por mes desde `PrimerAnio`/`PrimerMes`, que no puede ser anterior al mes de otorgamiento. Cuota = `Math.Round(monto / N, 2, MidpointRounding.AwayFromZero)`; la última = monto − suma de las anteriores (L1).
3. **Sueldo neto estimado (L2).** `SueldoVigente(contrato del empleado a la fecha de otorgamiento) × 0,87` **[elegido]** (descuento previsional típico del 13 %), con la constante `FactorNetoEstimado`, hasta que exista ParametrosSistema (79). Tope 30 % (`PorcentajeMaximoCuota`). Se compara la **suma** de las cuotas pendientes de todos los préstamos activos del empleado en cada mes del cronograma nuevo, no solo la cuota nueva, para que dos préstamos no superen juntos el 30 %.
4. **Requisitos.** Empleado `Activo` con contrato vigente a la fecha de otorgamiento (reutiliza la regla de vigencia del 62: contrato no `Anulado` con `FechaInicio <= fecha <= FechaCierre ?? FechaFin ?? infinito`).
5. **Descuento (L3).** Método público `AdministrarPrestamosUseCase.DescontarCuotasAsync(empleadoId, anio, mes, montoMaximo, planillaId)` que descuenta las cuotas pendientes con mes `<=` (anio, mes) en orden, sin superar `montoMaximo` (lo que queda del neto) ni el saldo, marca las cuotas `Descontada` (o parcialmente, con `MontoDescontado`) y deja el préstamo `Cancelado` cuando el saldo llega a 0. No guarda: el 66 llama a `CompleteAsync` en su transacción. Y `RevertirDescuentosAsync(planillaId)` para cuando el 66 recalcula una planilla en `Borrador`/`Calculada`.
6. **Anulación (L4).** Solo sin ningún descuento; motivo obligatorio.
7. **Roles.** Escritura `RRHH` o `ADMIN`; lectura autenticado.

## 3. Archivos afectados
| Archivo | Acción |
|---|---|
| `database/027-adelantos-prestamos.sql` y su fila en `docs/equipo/relevo.md` §8 | crear / modificar |
| entidades `Prestamo`, `CuotaPrestamo`, `DescuentoCuotaPrestamo`, navegación en `Empleado`, `TitanGlobalDbContext.cs` (y `PluralizadorEspanol.cs`: `CuotasPrestamo` → `CuotaPrestamo`) | regenerar |
| `src/TitanGlobal.Application/DTOs/Modulo67_AdelantosPrestamos/*.cs` | crear |
| `src/TitanGlobal.Application/UseCases/Modulo67_AdelantosPrestamos/AdministrarPrestamosUseCase.cs`, `ValidacionPrestamo.cs`, `CronogramaPrestamo.cs` | crear |
| `src/TitanGlobal.Api/Controllers/Modulo67_AdelantosPrestamos/PrestamosController.cs` | crear |
| `tests/TitanGlobal.Api.Tests/Modulo67_AdelantosPrestamos/PrestamosApiTests.cs` | crear |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | modificar |

## 4. Diseño y lógica
**Tabla `Prestamos`:** `Id`; `Numero varchar(20)` único `PRE-{Id:D6}` (patrón `TMP-` del 62); `EmpleadoId` (FK, índice); `Tipo varchar(20)`; `Monto numeric(12,2) > 0`; `NumeroCuotas integer` 1–24; `FechaOtorgamiento date`; `PrimerAnio`, `PrimerMes integer`; `Motivo varchar(300) NOT NULL`; `Estado varchar(20)`; `Saldo numeric(12,2)`; `FechaAnulacion date`; `MotivoAnulacion varchar(300)`.
**Tabla `CuotasPrestamo`:** `Id`; `PrestamoId` (FK); `NumeroCuota integer`; `Anio`, `Mes integer`; `Monto numeric(12,2)`; `MontoDescontado numeric(12,2) NOT NULL DEFAULT 0`; `Estado varchar(20)` `Pendiente`/`Descontada`; único (`PrestamoId`, `NumeroCuota`).
**Tabla `DescuentosCuotaPrestamo`** (un registro por cada cuota que toca una planilla): `Id`; `CuotaPrestamoId` (FK, índice); `PlanillaId integer NOT NULL` **sin FK** (la añade el script del 66); `Monto numeric(12,2) > 0`. `RevertirDescuentosAsync(planillaId)` borra solo los de esa planilla y resta exactamente esos importes de `MontoDescontado` y `Saldo` (cuota a `Pendiente` si no queda completa, préstamo `Cancelado` a `Activo`), de modo que una cuota parcial repartida entre dos planillas se revierte bien.

**Contrato HTTP:**
| Verbo | Ruta | Respuesta | Acceso |
|---|---|---|---|
| GET | `api/v1/prestamos?busqueda=&empleadoId=&tipo=&estado=&pagina=1&tamanoPagina=10` | paginado de `PrestamoListadoDto { Id, Numero, EmpleadoId, EmpleadoLegajo, EmpleadoNombre, Tipo, Monto, NumeroCuotas, FechaOtorgamiento, Saldo, Estado }` | autenticado |
| GET | `api/v1/prestamos/resumen` | `{ Activos, SaldoPendienteTotal, OtorgadoMes, CuotasDelMes }` | autenticado |
| GET | `api/v1/prestamos/{id:int}` | detalle con `Motivo`, anulación y `Cuotas[]` | autenticado |
| GET | `api/v1/prestamos/capacidad/{empleadoId:int}?fecha=` | `{ SueldoVigente, NetoEstimado, CuotaMaxima, CuotasComprometidasPorMes[] }` | autenticado |
| POST | `api/v1/prestamos` | `{ id, numero }` | `RRHH`, `ADMIN` |
| POST | `api/v1/prestamos/{id:int}/anulacion` | `{ id }` | `RRHH`, `ADMIN`; `{ motivo }` |

`RegistrarPrestamoRequestDto { EmpleadoId (int?), Tipo, Monto (decimal?), NumeroCuotas (int?), FechaOtorgamiento (DateOnly?), PrimerAnio (int?), PrimerMes (int?), Motivo }`.

**Mensajes literales:**
| # | Código | Mensaje |
|---|---|---|
| 1 | 400 | El empleado es obligatorio. |
| 2 | 400 | El tipo debe ser Adelanto o Prestamo. |
| 3 | 400 | El monto es obligatorio. |
| 4 | 400 | El monto debe ser mayor que cero. |
| 5 | 400 | El monto admite como máximo 2 decimales y no puede superar 9999999999.99. |
| 6 | 400 | Un adelanto se descuenta en una sola cuota. |
| 7 | 400 | El número de cuotas debe estar entre 1 y 24. |
| 8 | 400 | La fecha de otorgamiento es obligatoria. |
| 9 | 400 | El primer mes de descuento es obligatorio y debe ser un mes válido. |
| 10 | 400 | El primer mes de descuento no puede ser anterior al mes de otorgamiento. |
| 11 | 400 | El motivo es obligatorio. |
| 12 | 400 | El motivo no puede superar 300 caracteres. |
| 13 | 404 | El empleado {empleadoId} no existe. |
| 14 | 409 | El empleado {empleadoId} está cesado y no admite préstamos. |
| 15 | 409 | El empleado {empleadoId} no tiene un contrato vigente en la fecha de otorgamiento. |
| 16 | 409 | La cuota de {mes}/{anio} sería {cuota} y supera el máximo de {maximo} (30 % del neto estimado). |
| 17 | 404 | El préstamo {id} no existe. |
| 18 | 409 | El préstamo {numero} no está activo y no se puede anular. |
| 19 | 409 | El préstamo {numero} ya tiene descuentos y no se puede anular. |
| 20 | 400 | La página debe ser mayor o igual a 1. |
| 21 | 400 | El tamaño de página debe estar entre 1 y 100. |
| 22 | 400 | El filtro {nombre} no es válido. |
| 23 | 400 | El motivo de la anulación es obligatorio. |
| 24 | 400 | El motivo de la anulación no puede superar 300 caracteres. |

En el 16, `{mes}` con dos dígitos e importes con 2 decimales y punto. Orden: 1 a 12, después 13, 14, 15 y 16.
Los mensajes 23 y 24 (añadidos en la implementación) se comprueban después del 17, 18 y 19.

**Seed:** préstamo activo del empleado 1 (6 cuotas, 2 descontadas, sin filas en `DescuentosCuotaPrestamo` porque aún no existe ninguna planilla), adelanto cancelado del empleado 2 y préstamo anulado del empleado 3.

**Pruebas:** un caso por mensaje; cronograma (redondeo y última cuota que absorbe la diferencia, cambio de año); tope sumando préstamos; `DescontarCuotasAsync` (tope, parcial, cancelación al llegar a 0, cuotas atrasadas primero) y `RevertirDescuentosAsync`; anulación; roles; auditoría.

**Humo:** OpenAPI; resumen; capacidad del empleado 1; alta que supera el tope → 409 con el mensaje 16; alta válida → 200 con número `PRE-`; anular el préstamo del seed con descuentos → 409 con el mensaje 19.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde (salvo el fallo de entorno CRLF conocido) |
| A2 | L1–L4 | pruebas de mensajes, cronograma, tope, descuento y anulación | pasan |
| A3 | Roles y auditoría | pruebas | pasan |
| A4 | PostgreSQL | `aplicar-bd.sh`, `scaffold.sh --comprobar`, `smoke-postgres.sh` | `SMOKE OK` |

## 6. Checklist de ejecución
- [x] Decisiones de §2 con la opción recomendada (acuerdo con Lukas del 2026-11-22).
- [x] Tarea 1: script 027, scaffold y fila en `relevo.md`.
- [x] Tarea 2: DTOs, validación, cronograma, caso de uso con descuento y controlador.
- [x] Tarea 3: pruebas, seed y humo; A1 a A4 en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-22 | A1 Gate | `./scripts/verify.sh`: build 0 advertencias, reglas OK, 1336 de 1337 pruebas; único fallo el CRLF conocido (`CatalogoCoincideConLosOchentaModulosDeArquitectura`). `npm --prefix frontend run verify` OK | `verify.sh`, `npm run verify` |
| 2026-11-22 | A2 y A3 L1–L4, roles y auditoría | 27 pruebas de `PrestamosApiTests` en verde | `dotnet test --filter Modulo67` |
| 2026-11-22 | A4 PostgreSQL | `aplicar-bd.sh` aplica 027, `scaffold.sh --comprobar` al día, `SMOKE OK` | `smoke-postgres.sh` |

**Decisiones de implementación (detalles abiertos):**
- Mensajes 23 y 24 añadidos a la tabla para el motivo de anulación.
- Adelanto: `NumeroCuotas` nulo se toma como 1; distinto de 1 → mensaje 6. `PrimerAnio` válido entre 2000 y 2100 (mensaje 9).
- Mensaje 16: `{cuota}` es la suma de lo ya comprometido ese mes más la cuota nueva; la igualdad con el máximo se admite. Se evalúan los meses en orden y falla el primero que supera.
- Capacidad: sin contrato vigente devuelve sueldo, neto y máximo en 0; `CuotasComprometidasPorMes` lista (restante de cuotas pendientes de préstamos activos) desde el mes de `fecha`.
- Anulación pone `Saldo` en 0. Contrato vigente: el de `FechaInicio` más reciente que cumpla la regla de vigencia.
- `DescontarCuotasAsync` devuelve el total descontado y `RevertirDescuentosAsync` el total revertido; ambos sin `CompleteAsync`. Orden de descuento: año, mes, préstamo, cuota.
- Se añaden `Prestamos` y `CuotasPrestamo` a `PluralizadorEspanol.cs`.
