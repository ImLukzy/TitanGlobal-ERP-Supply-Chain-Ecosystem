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
