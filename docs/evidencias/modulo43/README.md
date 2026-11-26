# Módulo 43 — Rutas de despacho

Implementación local de Kelvin Pinto en `kelvin/modulo43-rutas-despacho`, sobre el contrato propuesto en las specs del módulo. **La revisión y aprobación de Lukas siguen pendientes**; la autorización de Kelvin para trabajar localmente no se presenta como aprobación ni despliegue.

## Resultados — 2026-11-21

| Comprobación | Resultado |
|---|---|
| Gate completo `./scripts/verify.sh` | Exit 0; .NET sin errores ni advertencias, formato/capas/reglas correctos, ESLint y TypeScript correctos |
| Backend | 1.130 pruebas correctas, incluidas 46 propias del 43 |
| Frontend | 603 pruebas correctas en 76 archivos, incluidas 60 propias del 43 |
| Estabilidad de la suite propia | Diez ejecuciones completas correctas, 60/60 pruebas en cada ejecución |
| PostgreSQL real | `SMOKE OK`; scaffold comprobado sin diferencias |
| Seed idempotente | Dos cargas consecutivas correctas: seis rutas, tres zonas, una inactiva y peajes en PEN/USD |
| QA visual real | Cuatro escenarios 375/1280 claro/oscuro; 12 capturas y resultados detallados en `resultado-visual.json` |

Vite mantiene la advertencia de tamaño del paquete compartido superior a 500 kB, ya presente en la base del proyecto. No se modifica el umbral ni se declara una compilación frontend sin esa advertencia. Tras ajustar únicamente la uniformidad del fondo de la fila seleccionada se repitieron el build y los cuatro escenarios visuales.

El primer smoke detectó un arranque transitorio de PostgreSQL; se repitió sin cambiar ni omitir comprobaciones y terminó correctamente. No se ejecutaron dos smoke simultáneos.

## Persistencia y contrato

Script nuevo `database/013-rutas-despacho.sql`, aplicado en PostgreSQL 16 local. Entidad y contexto generados exclusivamente con scaffold. Siete operaciones autenticadas: listado, detalle, alta, edición, baja, zonas y resumen.

El smoke comprueba el índice único normalizado directamente en SQL y las restricciones de distancia positiva y peaje no negativo. También verifica false persistido, peaje cero, campos normalizados, filtros combinados, página extrema sin desbordar y máximos exactos `99999999.99` km y `9999999999999999.99` de peaje. Los decimales adicionales significativos se rechazan antes del guardado, sin redondearlos.

La tabla no incorpora relaciones con despachos todavía inexistentes. La traducción del conflicto de referencia al eliminar se prueba mediante el contrato `IUnitOfWork`; no se acredita una relación real que el esquema actual no tiene.

La pantalla conserva los decimales recibidos como texto y su moneda original; rechaza los importes de entrada que JavaScript no pueda enviar con precisión. No suma monedas diferentes ni convierte peajes estimados en pagos reales.

## QA con API y PostgreSQL reales

Edge sin interceptar respuestas. En cada escenario se consultó el detalle, se creó una ruta con peaje cero, se editó a `12.30 USD`, se desactivó y se eliminó con confirmación. Se comprobaron Tab, Escape y retorno de foco, y se confirmó que la ruta ya no apareciera al buscarla.

Todas las capturas corresponden a operaciones reales. Sin errores de JavaScript ni desbordamiento horizontal de la página; las tablas tienen su región accesible de desplazamiento. Fondo uniforme en las celdas de la fila seleccionada. Contraste medido en título, texto secundario, estado y enlace de navegación: superior a 4.5:1 en ambos temas; valores concretos y selectores en [resultado-visual.json](resultado-visual.json).

| Ancho / tema | Maestro y detalle | Formulario | Confirmación de baja |
|---|---|---|---|
| 375 / claro | [Captura](375-light-detalle.png) | [Captura](375-light-formulario.png) | [Captura](375-light-baja.png) |
| 375 / oscuro | [Captura](375-dark-detalle.png) | [Captura](375-dark-formulario.png) | [Captura](375-dark-baja.png) |
| 1280 / claro | [Captura](1280-light-detalle.png) | [Captura](1280-light-formulario.png) | [Captura](1280-light-baja.png) |
| 1280 / oscuro | [Captura](1280-dark-detalle.png) | [Captura](1280-dark-formulario.png) | [Captura](1280-dark-baja.png) |

## Repetición

Requisitos utilizados: Git Bash, .NET 10, Node 24.22.1, Python 3 y Docker con PostgreSQL local en 5433. Ejecutar los scripts desde Git Bash, con los ejecutables reales en PATH.

1. `docker compose up -d --wait db`.
2. `dotnet tool restore`, `./scripts/aplicar-bd.sh --reiniciar` y `./scripts/scaffold.sh`.
3. Cargar `scripts/seed-dev.sql` mediante el comando de AGENTS.md; repetir la carga para verificar idempotencia.
4. `./scripts/verify.sh`.
5. Repetir diez veces `npm --prefix frontend run test -- src/modules/rutas-despacho`.
6. Liberar 5080/5433 y ejecutar `./scripts/smoke-postgres.sh` de forma independiente.
7. Para el QA, preparar de nuevo base y seed, iniciar API en 5080 y Vite en 5173, entrar con la cuenta administrativa del seed y abrir `/modulos/43-rutas-despacho`.

El contenedor de este repositorio no conserva los datos al eliminarse; después del smoke hay que preparar otra vez el esquema y el seed. No se aplica SQL en producción, ni se hace push, PR o modificación de main como parte de esta entrega local.

## Preparación de revisión — 2026-11-22

Rama actualizada con `git pull origin main`, conservando la tabla oficial de relevo y modificando solo la línea de Kelvin y la reserva propia. Script vigente: `database/013-rutas-despacho.sql`. Propuesta, pendiente de aprobación de Lukas. Base PostgreSQL local recreada, seed cargado y entidades/contexto regenerados mediante scaffold, sin edición manual ni diferencias de contenido.

`./scripts/verify.sh`: salida0, 1130 pruebas backend y 603 frontend en 76 archivos. `./scripts/smoke-postgres.sh`: salida0, scaffold coincidente y SMOKE OK. Se usó VITEST_MAX_WORKERS=1 temporal, sin modificar pruebas, timeouts ni configuración compartida. Persisten los avisos globales de ESLint9/Vite; no se cambiaron dependencias ni umbrales.

No se generaron capturas nuevas ni se cambió la pantalla para estos ajustes. Se conserva la evidencia visual previa. Preparación local: sin nuevos PR, sin push y sin integración. Tras cada fusión anterior habrá que incorporar main y repetir estas verificaciones antes del PR correspondiente.

### Actualización para revisión — 2026-11-23

- Rama actualizada con `origin/main` (b95dfc787089978e1c5434f6c4d005bfd04713a0) después de integrar el PR #63 del módulo 48.
- `relevo.md` idéntico a main. Se conservaron las pantallas y los estilos del 41/42/48, el seed y las comprobaciones de smoke existentes; únicamente se añadieron los bloques del 43. Persistencia regenerada con el scaffold oficial desde la base combinada.
- Base local reiniciada, seed cargado y script propio aplicado: `013-rutas-despacho.sql`. Su aplicación en la demo corresponde a Lukas.
- `./scripts/verify.sh`: exit 0; 1238 pruebas backend, 755 pruebas frontend en 90 archivos; .NET con cero warnings, lint, tipos y build correctos.
- `./scripts/smoke-postgres.sh`: exit 0, `SCAFFOLD AL DÍA` y `SMOKE OK`, incluidos los flujos del módulo 43 y los módulos existentes.
- `VITEST_MAX_WORKERS=1` temporal, sin modificar pruebas, timeouts ni configuración compartida. Persisten los avisos globales de ESLint 9 y tamaño del bundle de Vite.
- Se conserva la evidencia visual del 2026-11-21 y el bloque visual del 43. Propuesta, pendiente de aprobación de Lukas; la aprobación del 48 no sustituye la del 43. No se ejecutó SQL contra producción.

### Resolución de conflictos del PR #73 — 2026-11-25

- Incorporado `origin/main` (8f2dcf20d9256a22268fbbd70516a5e35624603b) con los módulos integrados de RRHH y manufactura, conservando también 41, 42 y 48.
- Conflictos resueltos en pantallas, estilos y smoke conservando main completo y los bloques originales del 43. Seed combinado sin eliminar datos; `relevo.md` idéntico a main.
- Se aceptó el contexto de main, se recreó PostgreSQL local, se aplicaron los scripts, se cargó el seed y se regeneró con `./scripts/scaffold.sh`; sin edición manual de archivos generados. Script propio: `013-rutas-despacho.sql`.
- `./scripts/verify.sh`: exit 0, 1702 pruebas backend y 1399 frontend en 142 archivos; .NET con cero warnings, lint, tipos y build correctos.
- `./scripts/smoke-postgres.sh`: exit 0, `SCAFFOLD AL DÍA` y `SMOKE OK`, incluidos los flujos del 43 y todos los existentes de main.
- `VITEST_MAX_WORKERS=1` temporal, sin modificar pruebas, timeouts ni configuración del repositorio. Se conservan los avisos globales de ESLint 9 y Vite.
- Evidencia visual del 2026-11-21 conservada; bloque visual del 43 sin cambios. Spec propuesta, pendiente de aprobación de Lukas. SQL de producción exclusivamente a cargo de Lukas.
