# Especificación: modulo66-planillas-backend — Planillas (backend)

> Estado: **aprobada** por Lukas el 2026-11-24 en la revisión final, con las opciones recomendadas de §2; integrada en `main` por el PR #69 y desplegada en la demo.
> Depende de 62 (contratos y adendas), 63 (jornadas), 65 (ausencias) y 67 (cuotas), ya implementados en las ramas anteriores.

## 1. Objetivo
**Problema:** no hay cálculo de remuneraciones.
**Resultado esperado:** `Modulo66_Planillas` en `api/v1/planillas`, `api/v1/bonos-planilla` y `api/v1/afiliaciones-pension`: planilla mensual (y complementaria) por periodo con estados `Borrador` → `Calculada` → `Aprobada` → `Pagada` (P1, P2), cálculo por empleado de ingresos y descuentos con parámetros en tablas (P3, P4, S4), neto e incidencias (P5) y boleta por empleado al aprobar (P6).

## 2. Fuera de alcance
- Gratificaciones, CTS, liquidación final, vacaciones truncas, asignación familiar, planilla quincenal (S1, S3).
- Edición de parámetros legales desde la API: se leen y se cambian por script SQL hasta que exista ParametrosSistema (79).
- PDF de la boleta: la API devuelve los datos y la pantalla la imprime.
- Cambios en `Middlewares/`, `Services/`, `Program.cs`, `UnitOfWork` y en los módulos 61 a 65 (solo lectura). Del 67 se usan `DescontarCuotasAsync` y `RevertirDescuentosAsync`.

**Decisiones de producto (opción recomendada aplicada):**
1. **Mes comercial de 30 días.** `DiasContrato` = 30 si los contratos cubren todo el mes natural; si no, los días naturales cubiertos (máximo 30). Cubre un contrato no `Anulado` entre `FechaInicio` y `FechaCierre ?? FechaFin ?? infinito`, sin pasar de `FechaCese`.
2. **Días laborados (P3).** `DiasContrato − DiasSinGoce (65, PermisoSinGoce Aprobada) − DiasFalta (63, jornadas Falta)`, mínimo 0.
3. **Sueldo.** El vigente el último día cubierto del mes (contrato más adendas, como `SueldoVigente` del 62). El prorrateo por tramos de adendas queda fuera de v0.
4. **Horas extra (P3).** Jornadas del mes con horas extra `Aprobada`. Valor hora = sueldo / 30 / `HORAS_JORNADA`; por jornada, los primeros `MINUTOS_RECARGO_25` minutos con 25 % y el resto con 35 % (D. S. 007-2002-TR).
5. **Pensión (P4).** Tabla `AfiliacionesPension` por empleado: `ONP` (13 % de los ingresos) o una AFP (aporte + comisión sobre ingresos, más prima de seguro sobre `min(ingresos, RemuneracionMaximaAsegurable)`). Sin afiliación, el empleado se excluye con la incidencia «Sin afiliación a un sistema de pensiones.».
6. **Salud.** EsSalud (9 %) lo paga el empleador en Perú: se calcula y se muestra como `AporteEssalud`, pero **no se descuenta** al trabajador. P4 lo lista entre los descuentos; esta es la desviación consciente. Confirmado por Lukas el 2026-11-24: es aporte del empleador.
7. **Quinta categoría (P4), método simplificado.** Proyección anual = ingresos del mes × 12; base = proyección − 7 UIT; impuesto anual por tramos de `TramosQuintaCategoria` (hasta 5, 20, 35 y 45 UIT y el resto: 8, 14, 17, 20 y 30 %); retención = impuesto / 12. Sin ajustes por meses anteriores.
8. **Préstamos (P4, L3).** Después de pensión y quinta, con el saldo restante como tope (`DescontarCuotasAsync` del 67). Si se recalcula, primero `RevertirDescuentosAsync`.
9. **Neto negativo (P5).** Si pensión más quinta superan los ingresos, el empleado se excluye con la incidencia «Neto negativo: revisar ingresos y descuentos.».
10. **Complementaria (P2).** Solo si la mensual del periodo está `Aprobada` o `Pagada`. Incluye solo los bonos registrados para la complementaria, con su pensión y quinta; sin básico, horas extra ni préstamos.
11. **Roles.** Toda escritura `RRHH` o `ADMIN`; lectura autenticado.

## 3. Archivos afectados
| Archivo | Acción |
|---|---|
| `database/038-planillas.sql` (tablas de §4; al final, las filas de `ParametrosPlanilla`, `ParametrosPension` y `TramosQuintaCategoria` con `ON CONFLICT DO NOTHING`, porque son catálogos y la demo de producción no carga el seed; `ALTER TABLE "DescuentosCuotaPrestamo" ADD CONSTRAINT "FK_DescuentosCuotaPrestamo_Planillas_PlanillaId" FOREIGN KEY ("PlanillaId") REFERENCES "Planillas" ("Id") ON DELETE RESTRICT` (tabla del 67 con un registro por descuento)) y su fila en `docs/equipo/relevo.md` §8 | crear / modificar |
| entidades nuevas y navegaciones regeneradas, `TitanGlobalDbContext.cs`, `PluralizadorEspanol.cs` si hace falta | regenerar |
| `src/TitanGlobal.Application/DTOs/Modulo66_Planillas/*.cs` | crear |
| `src/TitanGlobal.Application/UseCases/Modulo66_Planillas/AdministrarPlanillasUseCase.cs`, `CalcularPlanillaUseCase.cs`, `AdministrarBonosPlanillaUseCase.cs`, `AdministrarAfiliacionesPensionUseCase.cs`, `ValidacionPlanilla.cs`, `CalculadoraRemuneracion.cs` (estática y pública, pura) | crear |
| `src/TitanGlobal.Api/Controllers/Modulo66_Planillas/PlanillasController.cs`, `BonosPlanillaController.cs`, `AfiliacionesPensionController.cs` | crear |
| `tests/TitanGlobal.Api.Tests/Modulo66_Planillas/*ApiTests.cs` y pruebas de `CalculadoraRemuneracion` | crear |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | modificar |

## 4. Diseño y lógica
**Tablas** (importes `numeric(12,2)`, tasas `numeric(6,4)`):
- `ParametrosPlanilla`: `Id`, `Clave varchar(50)` única, `Valor numeric(14,4)`, `Descripcion varchar(200)`. Claves que carga el script: `UIT` 5350, `TASA_ESSALUD` 0.09, `HORAS_JORNADA` 8, `MINUTOS_RECARGO_25` 120, `RECARGO_EXTRA_25` 0.25, `RECARGO_EXTRA_35` 0.35, `DEDUCCION_UIT_QUINTA` 7.
- `ParametrosPension`: `Id`, `Codigo varchar(20)` único (`ONP`, `INTEGRA`, `PRIMA`, `PROFUTURO`, `HABITAT`), `Sistema varchar(10)` (`ONP`/`AFP`), `TasaAporte`, `TasaPrimaSeguro`, `TasaComision`, `RemuneracionMaximaAsegurable` (nula en ONP). Valores de ejemplo que carga el script (ONP 0.13; AFP 0.10 de aporte, 0.0137 de prima, comisión 0.0155 / 0.0160 / 0.0169 / 0.0147, tope 12 027.10).
- `TramosQuintaCategoria`: `Id`, `Orden` único, `HastaUit numeric(8,2)` (nulo en el último), `Tasa`.
- `AfiliacionesPension`: `Id`, `EmpleadoId` único (FK), `ParametroPensionId` (FK), `Cuspp varchar(12)` (obligatorio en AFP), `FechaAfiliacion date`.
- `BonosPlanilla`: `Id`, `EmpleadoId` (FK), `Anio`, `Mes`, `TipoPlanilla varchar(20)`, `Concepto varchar(100)`, `Monto > 0`, `PlanillaId` (FK nula; la fija el cálculo).
- `Planillas`: `Id`, `Numero varchar(20)` único `PLA-{anio}{mes:D2}-M` o `-C`, `Anio`, `Mes`, `Tipo` (`Mensual`/`Complementaria`), `Estado`, `Empleados`, `Incidencias`, `TotalIngresos`, `TotalDescuentos`, `TotalNeto`, `TotalAportesEmpleador`, `FechaCalculo timestamptz`, `CalculadoPor`, `AprobadoPor`, `FechaAprobacion timestamptz`, `FechaPago date`; único (`Anio`, `Mes`, `Tipo`) (P1).
- `PlanillaDetalles`: `Id`, `PlanillaId` (FK), `EmpleadoId` (FK), `ContratoLaboralId` (FK nula), `Cargo`, `SueldoBasico`, `DiasContrato`, `DiasSinGoce`, `DiasFalta`, `DiasLaborados`, `IngresoBasico`, `MinutosExtra`, `IngresoHorasExtra`, `IngresoBonos`, `TotalIngresos`, `CodigoPension varchar(20)`, `DescuentoPension`, `DescuentoQuinta`, `DescuentoPrestamos`, `TotalDescuentos`, `Neto`, `AporteEssalud`, `Incidencia varchar(300)`, `NumeroBoleta varchar(30)` único nulo; único (`PlanillaId`, `EmpleadoId`).

**`CalculadoraRemuneracion`** (pura, sin base de datos): recibe los datos del empleado y los parámetros y devuelve el detalle. Redondeo `MidpointRounding.AwayFromZero` a 2 decimales en cada concepto. Todo `decimal` (T4).

**Contrato HTTP:**
| Verbo | Ruta | Respuesta | Notas |
|---|---|---|---|
| GET | `api/v1/planillas?anio=&tipo=&estado=&pagina=1&tamanoPagina=10` | paginado de `PlanillaListadoDto` | |
| GET | `api/v1/planillas/resumen` | `{ UltimoPeriodo, EstadoUltimo, NetoUltimo, EmpleadosUltimo, IncidenciasUltimo, BorradoresYCalculadas }` | |
| GET | `api/v1/planillas/parametros` | parámetros, pensiones y tramos | solo lectura |
| GET | `api/v1/planillas/{id:int}` | `PlanillaDetalleDto` (cabecera y totales) | |
| GET | `api/v1/planillas/{id:int}/detalles?busqueda=&soloIncidencias=&pagina=1&tamanoPagina=10` | paginado de `PlanillaEmpleadoDto` | |
| GET | `api/v1/planillas/boletas/{detalleId:int}` | `BoletaPagoDto` (empresa «TitanGlobal», periodo, empleado, cargo, días, ingresos, descuentos, aportes del empleador, neto, número) | 409 si aún no tiene número |
| POST | `api/v1/planillas` | `{ id, numero }` | `{ anio, mes, tipo }` |
| POST | `api/v1/planillas/{id:int}/calculo` | `{ id, empleados, incidencias }` | `[PermitirCuerpoVacio]` |
| POST | `api/v1/planillas/{id:int}/aprobacion` | `{ id }` | `[PermitirCuerpoVacio]`; numera boletas `BOL-{anio}{mes:D2}-{M|C}-{EmpleadoId:D6}` |
| POST | `api/v1/planillas/{id:int}/pago` | `{ id }` | `{ fechaPago }` |
| GET / POST / DELETE | `api/v1/bonos-planilla?anio=&mes=&empleadoId=` / `api/v1/bonos-planilla` / `api/v1/bonos-planilla/{id:int}` | paginado / `{ id }` / 204 | se borra solo si no está en una planilla `Aprobada` o `Pagada` |
| GET / PUT | `api/v1/afiliaciones-pension?empleadoId=&pagina=` / `api/v1/afiliaciones-pension/{empleadoId:int}` | paginado / `{ id }` | el `PUT` crea o reemplaza la afiliación |

Escrituras con `RRHH` o `ADMIN`; `CalculadoPor` y `AprobadoPor` vienen del JWT, que el controlador pasa al caso de uso.

**Cálculo (`POST .../calculo`)**, en una transacción: 404 (8); `Aprobada` o `Pagada` → 409 (10); se revierten los descuentos de préstamos y se borran los detalles anteriores y el `PlanillaId` de los bonos; se calculan los empleados con algún día cubierto en el mes (decisión 1), empleado por empleado con `CalculadoraRemuneracion`; se guardan los detalles (incluidos los excluidos, con `Incidencia` e importes en 0), los totales (sin los excluidos) y `Estado = Calculada`.

**Mensajes literales:**
| # | Código | Mensaje |
|---|---|---|
| 1 | 400 | El año es obligatorio y debe estar entre 2000 y 2100. |
| 2 | 400 | El mes es obligatorio y debe estar entre 1 y 12. |
| 3 | 400 | El tipo de planilla debe ser Mensual o Complementaria. |
| 4 | 400 | No se puede crear una planilla de un periodo futuro. |
| 5 | 409 | Ya existe la planilla {tipo} del periodo {mes}/{anio}. |
| 6 | 409 | La planilla complementaria necesita la planilla mensual de {mes}/{anio} aprobada. |
| 7 | 400 | La fecha de pago es obligatoria. |
| 8 | 404 | La planilla {id} no existe. |
| 9 | 409 | La planilla {numero} debe estar calculada para aprobarse. |
| 10 | 409 | La planilla {numero} está aprobada y no se modifica. |
| 11 | 409 | La planilla {numero} debe estar aprobada para registrar el pago. |
| 12 | 400 | La fecha de pago no puede ser anterior a la aprobación. |
| 13 | 409 | La planilla {numero} no tiene empleados calculados. |
| 14 | 404 | El detalle de planilla {id} no existe. |
| 15 | 409 | La boleta se genera al aprobar la planilla. |
| 16 | 400 | El empleado es obligatorio. |
| 17 | 404 | El empleado {empleadoId} no existe. |
| 18 | 400 | El concepto es obligatorio y no puede superar 100 caracteres. |
| 19 | 400 | El monto debe ser mayor que cero, con 2 decimales como máximo y no superar 9999999999.99. |
| 20 | 404 | El bono {id} no existe. |
| 21 | 409 | El bono está en una planilla aprobada y no se puede eliminar. |
| 22 | 400 | El sistema de pensiones es obligatorio. |
| 23 | 404 | El sistema de pensiones {codigo} no existe. |
| 24 | 400 | El CUSPP es obligatorio en una AFP y tiene 12 caracteres. |
| 25 | 400 | La fecha de afiliación es obligatoria. |
| 26 | 400 | La página debe ser mayor o igual a 1. |
| 27 | 400 | El tamaño de página debe estar entre 1 y 100. |
| 28 | 400 | El filtro {nombre} no es válido. |

Reglas: crear 1, 2, 3, 4, 5, 6. Calcular 8, 10. Aprobar 8, 9 (si no está `Calculada`), 13 (sin ningún detalle incluido). Pago 7, 8, 11, 12. Boleta 14, 15. Bonos 16, 1, 2, 3, 18, 19, 17; eliminar 20, 21. Afiliación 22, 23, 24, 25, 17. Listados 26, 27, 28.

**Seed:** afiliaciones de los empleados 1 (INTEGRA), 2 (ONP) y 3 (PRIMA); un bono del empleado 1 en 2026-10; planilla mensual 2026-10 `Pagada` con sus detalles y boletas calculados con estas reglas (los importes se obtienen ejecutando el cálculo y se copian al seed), y 2026-11 en `Borrador`.

**Parámetros de ejemplo:** los valores de `ParametrosPlanilla`, `ParametrosPension` y de los tramos de quinta categoría que carga `038-planillas.sql` (UIT, tasas de AFP y ONP, comisiones, tope asegurable y tramos) son de ejemplo para la demo y las pruebas, no valores legales vigentes. Para usar valores oficiales se actualizan con un script SQL nuevo (el `ON CONFLICT DO NOTHING` de 038 no pisa filas existentes), hasta que exista ParametrosSistema (módulo 79).

**Pruebas:** un caso por mensaje; `CalculadoraRemuneracion` con casos numéricos a mano: mes completo ONP, AFP con tope de la prima, ingreso parcial por alta a mitad de mes, permiso sin goce, faltas, horas extra con tramo de 25 % y 35 %, quinta con tramos (0 bajo 7 UIT, un tramo, varios tramos), préstamo limitado por el neto, neto negativo, sin afiliación; flujo completo con recálculo (los descuentos de préstamos no se duplican), aprobación inmutable (P2), boletas únicas (P6), complementaria; roles; auditoría.

**Humo:** OpenAPI; parámetros; crear 2026-11 duplicada → 409 con el mensaje 5; calcular la 2026-11 del seed → 200; aprobar → 200; recalcular → 409 con el mensaje 10; boleta de un detalle → 200 con número `BOL-`; pago → 200.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde (salvo el fallo de entorno CRLF conocido) |
| A2 | Cálculo (P3–P5) | pruebas de `CalculadoraRemuneracion` con importes exactos | pasan |
| A3 | Flujo y boletas (P1, P2, P6) | pruebas | pasan |
| A4 | Bonos, afiliaciones, roles y auditoría | pruebas | pasan |
| A5 | PostgreSQL | `aplicar-bd.sh`, `scaffold.sh --comprobar`, `smoke-postgres.sh` | `SMOKE OK` |
| 2026-11-23 | Actualización | Actualizado con main (módulos 12, 41, 42 y 72) y script renumerado a 038: gate y smoke en verde. `dotnet test` 1425 de 1426 (único fallo el CRLF conocido), 0 advertencias, reglas OK, frontend 1096 pruebas en verde; smoke en la rama final (alexander/modulo70-seguridad-salud-ocupacional) | `verify` |

## 6. Checklist de ejecución
- [x] Decisiones de §2 con la opción recomendada (acuerdo con Lukas del 2026-11-22).
- [x] Tarea 1: script 038, scaffold y fila en `relevo.md`.
- [x] Tarea 2: `CalculadoraRemuneracion` y sus pruebas.
- [x] Tarea 3: casos de uso, controladores y pruebas de API.
- [x] Tarea 4: seed y humo; A1 a A5 en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-22 | A1 | `verify.sh` llega al `dotnet test`: 1372 de 1373 pruebas en verde; solo falla `CatalogoCoincideConLosOchentaModulosDeArquitectura` (CRLF del entorno, ajeno al módulo); formato, reglas y compilación sin warnings | `PATH=shim ./scripts/verify.sh`; `npm --prefix frontend run verify` en verde |
| 2026-11-22 | A2, A3, A4 | 13 pruebas de `CalculadoraRemuneracion` con importes a mano y 23 de API del módulo 66 en verde (mensajes 1 a 28, flujo con recálculo sin duplicar préstamos, aprobación inmutable, boletas únicas, complementaria, roles, auditoría) | `dotnet test --filter FullyQualifiedName~Modulo66` |
| 2026-11-22 | A5 | `SMOKE OK` con `038` aplicado; `scaffold.sh --comprobar`: SCAFFOLD AL DÍA | `./scripts/smoke-postgres.sh`, `./scripts/scaffold.sh --comprobar` |
| 2026-11-24 | Actualización con main | OK (fallo ambiental conocido en `CatalogoCoincideConLosOchentaModulosDeArquitectura`) | Actualizado con main (módulo 48): build sin warnings, 1480 pruebas backend y 1133 frontend |
| 2026-11-24 | Catálogos en el script (pedido de Lukas) | OK (fallo ambiental conocido en `CatalogoCoincideConLosOchentaModulosDeArquitectura`) | `ParametrosPlanilla`, `ParametrosPension` y `TramosQuintaCategoria` al final de `038-planillas.sql` con `ON CONFLICT DO NOTHING` y fuera del seed: con solo los scripts quedan 7/5/5 filas, 038 se reaplica sin errores, el seed corre dos veces sin duplicar, la secuencia sigue en 6; build sin warnings, 1480 pruebas backend, `SCAFFOLD AL DÍA` y `SMOKE OK` |
| 2026-11-24 | Integración y producción | Aprobado | Revisión final de Lukas. `038` aplicado en la demo con copia y ensayo (11 scripts registrados, filas de las 51 tablas existentes sin cambios). PR #69 fusionado con merge commit (`bc93887`); gate de GitHub y local, smoke, Vercel y Render correctos; en producción `/api/v1/planillas` responde 200 y `/api/v1/planillas/parametros` devuelve los 7 parámetros, los 5 sistemas de pensiones y los 5 tramos cargados por el script |

### Decisiones de detalle tomadas al implementar (la spec no las fijaba)
- **Totales y contadores.** `Empleados`, `TotalIngresos`, `TotalDescuentos`, `TotalNeto` y `TotalAportesEmpleador` cuentan solo los empleados incluidos; `Incidencias` cuenta los excluidos (sin afiliación o neto negativo). Un excluido se guarda con días y sueldo informativos pero importes en 0.
- **Préstamos y tope.** La calculadora devuelve `SaldoParaPrestamos` (neto tras pensión y quinta); `DescontarCuotasAsync` lo usa como `montoMaximo` y `AplicarPrestamos` vuelve a acotar al neto.
- **Faltas y permisos.** `DiasFalta` cuenta fechas distintas con jornada `Falta` del mes; `DiasSinGoce` cuenta los días naturales del mes cubiertos por ausencias `PermisoSinGoce` aprobadas, aunque caigan fuera de los días de contrato.
- **Parámetros ausentes.** Si falta una clave de `ParametrosPlanilla` o no hay tramos, se usan los valores del script (UIT 5350, EsSalud 9 %, jornada de 8 h, 120 min al 25 %, recargos 0.25/0.35, 7 UIT y los cinco tramos). Estos valores de respaldo son también de ejemplo.
- **Quinta en la complementaria.** Se calcula con la misma fórmula sobre los ingresos de los bonos (sin acumular lo de la mensual).
- **Bonos sin empleado calculado.** Un bono mensual de un empleado sin días cubiertos o excluido queda sin `PlanillaId`.
- **Resumen.** `UltimoPeriodo` es `AAAA-MM` de la última planilla mensual (nulo si no hay) y `BorradoresYCalculadas` cuenta todas las planillas en ambos estados.
- **Mensaje 5.** `{mes}` va sin cero a la izquierda (`11/2026`).
- **CUSPP.** Solo se guarda en una AFP (12 caracteres exactos); al pasar a ONP se borra.
- **Humo.** Tras aprobar las horas extra de la jornada 8 (paso del módulo 63), el cálculo de 2026-11 incluye 90 min extra del empleado 2: ingresos 13578.49, descuentos 2701.00, neto 10877.49 y EsSalud 1222.06.
