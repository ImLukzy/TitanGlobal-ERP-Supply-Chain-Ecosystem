# Módulo 70 — Seguridad y salud ocupacional

QA visual de la pantalla en `alexander/modulo70-seguridad-salud-ocupacional`, sobre la rama final de la cadena HCM (`alexander/modulo70-seguridad-salud-ocupacional`, que contiene esta pantalla sin cambios). Revisión de Lukas pendiente.

## Revisión visual real — 2026-11-24

API (puerto 5080) y PostgreSQL locales con `scripts/seed-dev.sql`, frontend con Vite (puerto 5173) y sesión de la cuenta administrativa del seed. Navegador: Microsoft Edge en modo headless, controlado por el protocolo DevTools; el tema oscuro se emula con `prefers-color-scheme` y el ancho de 375 px como dispositivo móvil. No se interceptan ni se simulan respuestas, y no se modifican datos. Resultado detallado en [resultado-visual.json](resultado-visual.json).

| Escenario | 375 / claro | 375 / oscuro | 1280 / claro | 1280 / oscuro |
|---|---|---|---|---|
| Vista Incidentes: detalle con acciones correctivas | [Captura](375-light-incidentes.png) | [Captura](375-dark-incidentes.png) | [Captura](1280-light-incidentes.png) | [Captura](1280-dark-incidentes.png) |
| Vista EPP: equipos y entregas | [Captura](375-light-epp.png) | [Captura](375-dark-epp.png) | [Captura](1280-light-epp.png) | [Captura](1280-dark-epp.png) |
| Vista Exámenes médicos: formulario «Nuevo examen médico» | [Captura](375-light-examen.png) | [Captura](375-dark-examen.png) | [Captura](1280-light-examen.png) | [Captura](1280-dark-examen.png) |

| Comprobación (12 capturas) | Resultado |
|---|---|
| Cada escenario llega a su estado (fila, botón, formulario o diálogo) | sí |
| Sin desplazamiento horizontal de la página | sí |
| Sin errores de JavaScript ni de consola | sí |

Las tablas anchas se desplazan dentro de su propia región en móvil, como en el resto de pantallas.

## Cómo repetir

Con la base local preparada (`./scripts/aplicar-bd.sh --reiniciar` y el seed), iniciar la API en 5080 y el frontend en 5173, entrar con la cuenta administrativa del seed y abrir `/modulos/70-seguridad-salud-ocupacional`. Repetir los escenarios de la tabla a 375 y 1280 px, en claro y oscuro.
