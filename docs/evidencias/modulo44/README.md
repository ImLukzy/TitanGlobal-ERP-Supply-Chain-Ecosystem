# Módulo 44: consolidación de carga — verificación local

Fecha: 2026-11-21. Responsable: Kelvin Pinto. Rama independiente `kelvin/modulo44-consolidacion-carga`, desde `origin/main` (`670c37f`). Implementación local autorizada por Kelvin; **la propuesta de contrato aún requiere revisión y aprobación de Lukas**. No se ha publicado, integrado ni desplegado.

## Alcance comprobado

Preparación manual de una carga con 1–100 líneas de SKU/presentación, picking, packing, cubicaje y peso estimados. Alta, consulta, filtros/paginación, edición atómica de líneas, confirmación, cancelación con motivo e historial, baja exclusiva de borradores y resumen global. Ocho operaciones autenticadas. Sin integración ni modificaciones de inventarios, Producto, órdenes, vehículos o módulos ajenos.

Los cálculos usan la cantidad prevista y redondean el número de bultos hacia arriba. El último parcial conserva dimensiones y peso del empaque completo: es una estimación, no un pesaje. La API devuelve decimales textuales; la previsualización utiliza enteros BigInt escalados y coincide con el cálculo del servidor. No se acredita resolución de ediciones o cancelaciones concurrentes.

## Resultados

| Comprobación | Resultado |
|---|---|
| SQL y scaffold | `014-consolidacion-carga.sql` aplicado a PostgreSQL local y entidades/DbContext regenerados; coincidencia confirmada por smoke |
| Integración backend | 1140 pruebas correctas; 55 propias del módulo, además de dos casos nuevos del mapeo de nombres |
| Frontend global | 599 pruebas correctas en 75 archivos; 56 propias en cinco archivos |
| Estabilidad propia | Diez ejecuciones consecutivas correctas, 56/56 cada una; sin omitir pruebas ni ampliar tiempos de espera |
| Gate completo | `./scripts/verify.sh` terminó con salida 0: compilación .NET sin warnings, formato, capas/reglas, integración, lint, tipos y build |
| PostgreSQL real | `./scripts/smoke-postgres.sh` terminó en `SMOKE OK` |
| Seed | Dos cargas consecutivas correctas; tres cabeceras de ejemplo y tres líneas, sin duplicados |
| QA real | Cuatro escenarios: 375 y 1280 px, claro y oscuro; 16 capturas, sin errores JavaScript ni desbordamiento de página |

El smoke comprueba CRUD, búsqueda/estado/resumen, rechazo de picking y packing incompletos, reemplazo de líneas, confirmación con UTC, cancelación conservando confirmación y líneas, bloqueos de estado, precisión máxima, unicidad normalizada, límites SQL, FK propia y borrado en cascada. El tratamiento de una posible FK externa futura se prueba con una excepción simulada de UnitOfWork; no existe ni se acredita dicha relación externa.

Caso de dos líneas en QA: **4 bultos, 0.180001 m³ y 6.50 kg**, iguales en previsualización y detalle del servidor. Caso máximo de 100 líneas en pruebas: `99999700000299.999900` m³ y `99999999000000.00` kg. El smoke verifica también el máximo de una línea contra PostgreSQL real.

## Salvedades y reproducción

- Vite mantiene la advertencia del bundle compartido por superar 500 kB (715.42 kB tras el ajuste visual final). No se ha ocultado ni elevado su umbral. Por ello no se acredita cero advertencias globales ni Definition of Done formal mientras falten esta condición y la aprobación de Lukas.
- Un primer gate encontró archivos .NET bloqueados por la API local; se detuvo ese servidor y se repitió. Hubo también timeouts de pruebas existentes con alta concurrencia. El gate final pasó aislado usando `VITEST_MAX_WORKERS=2`, sin cambios en pruebas, configuración ni timeouts del repositorio.
- Después del gate se ajustaron únicamente los espacios entre cifras/unidades y su salto de línea. Se repitieron compilación del frontend, suite propia diez veces y QA real sobre la presentación final; no hubo cambios posteriores en backend.
- Usar Git Bash, Docker Linux, Node 24, Python 3 y .NET 10 disponibles en PATH. Ejecutar `docker compose up -d db`, `dotnet tool restore`, `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh` y cargar `scripts/seed-dev.sql` antes de levantar API y frontend. Ejecutar el gate con `VITEST_MAX_WORKERS=2 ./scripts/verify.sh`; después `./scripts/smoke-postgres.sh`, sin otra instancia simultánea.
- El compose local no conserva volúmenes: después de retirar su contenedor hay que volver a aplicar SQL y seed para QA. No se utilizó producción. Los ayudantes temporales y registros con autenticación no forman parte del commit.

## Evidencia visual

Se comprobaron alta de dos líneas, edición de picking, rechazo de confirmación incompleta, confirmación, cancelación con historial y baja real de otro borrador. Los diálogos conservan foco, recorrido con Tab, cierre con Escape y retorno al botón de origen. Selección de tabla uniforme. Cinco muestras de contraste por escenario alcanzan al menos 4.5:1; mediciones en [resultado-visual.json](resultado-visual.json).

| Escenario | Detalle | Formulario | Baja | Cancelación |
|---|---|---|---|---|
| 375 claro | [Captura](375-light-detalle.png) | [Captura](375-light-formulario.png) | [Captura](375-light-baja.png) | [Captura](375-light-cancelacion.png) |
| 375 oscuro | [Captura](375-dark-detalle.png) | [Captura](375-dark-formulario.png) | [Captura](375-dark-baja.png) | [Captura](375-dark-cancelacion.png) |
| 1280 claro | [Captura](1280-light-detalle.png) | [Captura](1280-light-formulario.png) | [Captura](1280-light-baja.png) | [Captura](1280-light-cancelacion.png) |
| 1280 oscuro | [Captura](1280-dark-detalle.png) | [Captura](1280-dark-formulario.png) | [Captura](1280-dark-baja.png) | [Captura](1280-dark-cancelacion.png) |

Las capturas corresponden a datos locales de ejemplo y QA. No contienen tokens ni credenciales. Los archivos generados del modelo proceden del scaffold; los commits mantienen la identidad Git de Kelvin, sin trailers de atribución.

## Preparación de revisión — 2026-11-22

Rama actualizada con `git pull origin main`, conservando la tabla oficial de relevo y modificando solo la línea de Kelvin y la reserva propia. Script vigente: `database/014-consolidacion-carga.sql`. Propuesta, pendiente de aprobación de Lukas. Base PostgreSQL local recreada, seed cargado y entidades/contexto regenerados mediante scaffold, sin edición manual ni diferencias de contenido.

`./scripts/verify.sh`: salida0, 1140 pruebas backend y 599 frontend en 75 archivos. `./scripts/smoke-postgres.sh`: salida0, scaffold coincidente y SMOKE OK. Se usó VITEST_MAX_WORKERS=1 temporal, sin modificar pruebas, timeouts ni configuración compartida. Persisten los avisos globales de ESLint9/Vite; no se cambiaron dependencias ni umbrales.

No se generaron capturas nuevas ni se cambió la pantalla para estos ajustes. Se conserva la evidencia visual previa. Preparación local: sin nuevos PR, sin push y sin integración. Tras cada fusión anterior habrá que incorporar main y repetir estas verificaciones antes del PR correspondiente.

### Preparación del PR del 44 — 2026-11-25

- Incorporado `origin/main` (a0a5df67698e2ed97c6a33213de53a86405d1a17) después de integrar el PR #73 del módulo 43; se conserva todo lo integrado en main.
- Conflictos de pantallas, estilos, seed, smoke y pruebas de pluralización resueltos conservando main y los bloques originales del 44. `relevo.md` idéntico a main; asignaciones modifica únicamente la fila propia del 44.
- Contexto tomado de main, base PostgreSQL local recreada, scripts aplicados, seed cargado y entidades/contexto regenerados con `./scripts/scaffold.sh`, sin edición manual. Script propio: `014-consolidacion-carga.sql`.
- `./scripts/verify.sh`: exit 0; 1759 pruebas backend y 1455 frontend en 147 archivos; .NET sin warnings, lint, tipos y build correctos.
- `./scripts/smoke-postgres.sh`: exit 0, `SCAFFOLD AL DÍA` y `SMOKE OK`, incluidos los flujos del 44 y los existentes de main.
- `VITEST_MAX_WORKERS=1` temporal, sin modificar pruebas, timeouts ni configuración compartida. Se mantienen los avisos globales de ESLint 9 y tamaño del bundle de Vite.
- Evidencia visual real del 2026-11-21 conservada, sin cambios al bloque visual propio. Propuesta, pendiente de aprobación de Lukas. El script para la demo lo aplica Lukas; no se ejecutó SQL en producción.
