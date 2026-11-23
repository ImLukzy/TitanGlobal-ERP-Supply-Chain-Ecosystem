# Evidencia local — módulo 42 Conductores

Revisión del 2026-11-21 en Edge contra API .NET y PostgreSQL 16 locales, con los tres conductores de `scripts/seed-dev.sql`. No se simularon respuestas HTTP ni se utilizó producción. Se seleccionó a Maria Quispe para mostrar explícitamente licencia vencida, habilitación administrativa y activo como estados separados, conforme al contrato existente.

| Vista | 375 px claro | 375 px oscuro | 1280 px claro | 1280 px oscuro |
|---|---|---|---|---|
| Detalle y licencia vencida | [captura](375-light-detalle.png) | [captura](375-dark-detalle.png) | [captura](1280-light-detalle.png) | [captura](1280-dark-detalle.png) |
| Formulario con conflicto real | [captura](375-light-formulario.png) | [captura](375-dark-formulario.png) | [captura](1280-light-formulario.png) | [captura](1280-dark-formulario.png) |
| Confirmación de baja | [captura](375-light-baja.png) | [captura](375-dark-baja.png) | [captura](1280-light-baja.png) | [captura](1280-dark-baja.png) |

`resultado-visual.json` registra los cuatro escenarios: sin desbordamiento horizontal de página ni errores JavaScript; 409 literal al intentar registrar `plt-0001`, valores conservados y foco devuelto tras Escape. La tabla tiene desplazamiento horizontal dentro de su región accesible en móvil.

## Pruebas y flujos observados

- `./scripts/verify.sh`: aprobado; 1090 pruebas backend y 582 frontend, lint/TypeScript/build correctos y compilación .NET con cero errores/advertencias. Vite mantiene el aviso existente de bundle superior a 500 kB; no se cambió ni ocultó la configuración compartida.
- 39 pruebas frontend propias: cinco operaciones HTTP, fechas por día UTC, conservación del instante original, límites/mensajes, consultas, mutaciones, errores, respuestas obsoletas, foco y envíos duplicados.
- Siete casos backend nuevos para unicidad de licencias sin distinguir mayúsculas.
- `aplicar-bd.sh --reiniciar`: 001, 008 y 011; `scaffold.sh` ejecutado y `SCAFFOLD AL DÍA`, sin cambios de modelo ni edición manual.
- `smoke-postgres.sh`: `SMOKE OK`; POST/PUT duplicados → 409, cambio de mayúsculas del documento propio → 200 y escritura SQL directa duplicada → 23505 por el índice esperado.
- Seed idempotente: el bloque real de Conductores vuelve a ejecutarse tras cambiar el documento de un registro a minúsculas; conserva tres filas. Se comprobó en una transacción revertida, sin alterar el seed final.
- Navegador real a 1280 px claro: alta, selección, edición, cambio de mayúsculas del documento propio y baja del conductor temporal; eliminado al terminar. También se desactivó y reactivó el conductor con licencia vencida manteniendo exactamente el instante UTC guardado y su permiso manual; el estado del seed quedó restaurado.
- OpenAPI local: GET/POST de `/api/v1/conductores` y GET/PUT/DELETE de `/api/v1/conductores/{id}` comprobados.

Git Bash utilizó Python y Node 24 locales; la comprobación de puertos consultó las escuchas reales de Windows porque Git Bash no incluye `ss`. No se omitieron controles. La rama parte de `origin/main` y no incorpora los cambios no integrados del 41; el 010 queda reservado a su rama independiente.

Implementación y verificación local completas. La aprobación de Lukas, revisión, PR, integración y despliegue siguen pendientes.

## Preparación de revisión — 2026-11-22

Rama actualizada con `git pull origin main`, conservando la tabla oficial de relevo y modificando solo la línea de Kelvin y la reserva propia. Script vigente: `database/022-conductores-licencia-unica.sql`. Propuesta, pendiente de aprobación de Lukas. Base PostgreSQL local recreada, seed cargado y entidades/contexto regenerados mediante scaffold, sin edición manual ni diferencias de contenido.

`./scripts/verify.sh`: salida0, 1090 pruebas backend y 582 frontend en 74 archivos. `./scripts/smoke-postgres.sh`: salida0, scaffold coincidente y SMOKE OK. Se usó VITEST_MAX_WORKERS=1 temporal, sin modificar pruebas, timeouts ni configuración compartida. Persisten los avisos globales de ESLint9/Vite; no se cambiaron dependencias ni umbrales. El primer smoke se detuvo en su precomprobación porque el contenedor local de preparación ocupaba5433. Se cerró con docker compose down y el smoke completo pasó al repetirlo; se conserva el registro inicial.

No se generaron capturas nuevas ni se cambió la pantalla para estos ajustes. Se conserva la evidencia visual previa. Preparación local: sin nuevos PR, sin push y sin integración. Tras cada fusión anterior habrá que incorporar main y repetir estas verificaciones antes del PR correspondiente.

## Preparación del PR de Conductores — 2026-11-23

Actualizada la rama con main cb9dfe1851428cfa60d2b9b9e47b3d426043bb69 después de integrar los PR #51 y #55. Se conserva `docs/equipo/relevo.md` exactamente como main; asignaciones conserva los estados aprobados del41/72/62 y solo actualiza la fila propia del42. Registro de pantallas y estilos conservan íntegros main y Conductores. No hubo conflicto en TitanGlobalDbContext.cs ni edición manual de archivos generados.

Script propio vigente: `database/022-conductores-licencia-unica.sql`; la aplicación en la demo corresponde a Lukas antes de integrar. Localmente se recreó PostgreSQL, se cargó el seed y se ejecutó scaffold sin diferencias de contenido en modelos/código/pruebas. Los scripts001/008/021/022 están aplicados en la base de prueba. El41 se recibe desde main ya integrado; no se incorporan otras ramas de desarrollo.

`./scripts/verify.sh` terminó con salida0: 1097 pruebas backend y 658 frontend en 81 archivos, sin fallos ni omisiones. Compilación .NET sin errores/advertencias; formato, reglas/capas, lint, tipos y build correctos. `./scripts/smoke-postgres.sh` terminó con salida0, `SCAFFOLD AL DÍA` y `SMOKE OK`, incluidos POST/PUT duplicados normalizados, cambio de mayúsculas propio, UTC y rechazo de duplicado por índice real.

VITEST_MAX_WORKERS=1 fue temporal; sin modificar pruebas/timeouts/configuración compartida. Se conservan las capturas y el QA real previos, sin generar imágenes nuevas para esta actualización. Avisos globales conocidos de ESLint9/Vite documentados, sin cambiar dependencias ni umbrales. Propuesta del42 pendiente de aprobación de Lukas; no se aplica SQL a producción ni se declara integración.
