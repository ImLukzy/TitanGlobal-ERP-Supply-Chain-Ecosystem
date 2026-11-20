# Evidencia local — pantalla 41

La pantalla se revisó en Edge con respuestas HTTP simuladas de dos vehículos, uno activo y otro inactivo. No se utilizó una base real, producción ni credenciales reales. Estas imágenes acreditan únicamente la presentación de la pantalla y sus estados, no el smoke de PostgreSQL.

| Vista | 375 px claro | 375 px oscuro | 1280 px claro | 1280 px oscuro |
|---|---|---|---|---|
| Detalle | [captura](375-light-detalle.png) | [captura](375-dark-detalle.png) | [captura](1280-light-detalle.png) | [captura](1280-dark-detalle.png) |
| Formulario | [captura](375-light-formulario.png) | [captura](375-dark-formulario.png) | [captura](1280-light-formulario.png) | [captura](1280-dark-formulario.png) |
| Confirmación de baja | [captura](375-light-baja.png) | [captura](375-dark-baja.png) | [captura](1280-light-baja.png) | [captura](1280-dark-baja.png) |

`resultado-visual.json` registra ausencia de desbordamiento horizontal de la página y de errores JavaScript en los cuatro escenarios. La tabla mantiene su región de desplazamiento horizontal accesible en móvil. Las capturas de página completa conservan la posición de las barras fijas al desplazarse hasta el formulario o el diálogo.

## Verificación funcional

- `npm --prefix frontend run verify`: 73 archivos y 582 pruebas aprobados; lint, TypeScript y build correctos. Vite informa de un bundle superior a 500 kB; no se ocultó el aviso ni se cambió la configuración compartida.
- Pruebas de contrato HTTP, filtros, paginación, detalle, validación, alta, edición, errores y baja accesible en `frontend/src/modules/vehiculos-flota/`.
- `./scripts/verify.sh`: bloqueado por carga de una dependencia de .NET en `GenerateMvcTestManifestTask`, incluso después de apagar los servidores de compilación.
- PostgreSQL, aplicación/reinicio de scripts, scaffold y smoke: pendientes porque Docker no está disponible.

La autorización de Kelvin para desarrollar localmente no se presenta como aprobación de Lukas. El módulo no se declara cerrado ni desplegado.
