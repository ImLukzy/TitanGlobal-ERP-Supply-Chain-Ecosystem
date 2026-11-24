# Módulo 63 — Asistencia biométrica

QA visual de la pantalla en `alexander/modulo63-asistencia-biometrica`, sobre la rama final de la cadena HCM (`alexander/modulo70-seguridad-salud-ocupacional`, que contiene esta pantalla sin cambios). Revisión de Lukas pendiente.

## Revisión visual real — 2026-11-24

API (puerto 5080) y PostgreSQL locales con `scripts/seed-dev.sql`, frontend con Vite (puerto 5173) y sesión de la cuenta administrativa del seed. Navegador: Microsoft Edge en modo headless, controlado por el protocolo DevTools; el tema oscuro se emula con `prefers-color-scheme` y el ancho de 375 px como dispositivo móvil. No se interceptan ni se simulan respuestas, y no se modifican datos. Resultado detallado en [resultado-visual.json](resultado-visual.json).

| Escenario | 375 / claro | 375 / oscuro | 1280 / claro | 1280 / oscuro |
|---|---|---|---|---|
| Vista Jornadas desde 2026-11-01: detalle de una jornada con tardanza | [Captura](375-light-jornadas.png) | [Captura](375-dark-jornadas.png) | [Captura](1280-light-jornadas.png) | [Captura](1280-dark-jornadas.png) |
| Vista Marcaciones: formulario «Registrar marcación» | [Captura](375-light-marcacion.png) | [Captura](375-dark-marcacion.png) | [Captura](1280-light-marcacion.png) | [Captura](1280-dark-marcacion.png) |
| Diálogo «Importar archivo del reloj» (CSV) | [Captura](375-light-importacion.png) | [Captura](375-dark-importacion.png) | [Captura](1280-light-importacion.png) | [Captura](1280-dark-importacion.png) |

| Comprobación (12 capturas) | Resultado |
|---|---|
| Cada escenario llega a su estado (fila, botón, formulario o diálogo) | sí |
| Sin desplazamiento horizontal de la página | sí |
| Sin errores de JavaScript ni de consola | sí |
| Escape cierra el diálogo (importacion) | sí |

Las tablas anchas se desplazan dentro de su propia región en móvil, como en el resto de pantallas.

## Cómo repetir

Con la base local preparada (`./scripts/aplicar-bd.sh --reiniciar` y el seed), iniciar la API en 5080 y el frontend en 5173, entrar con la cuenta administrativa del seed y abrir `/modulos/63-asistencia-biometrica`. Repetir los escenarios de la tabla a 375 y 1280 px, en claro y oscuro.
