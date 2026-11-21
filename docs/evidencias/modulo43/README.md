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
