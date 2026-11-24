# Módulo 69 — Capacitaciones

QA visual de la pantalla en `alexander/modulo69-capacitaciones`, sobre la rama final de la cadena HCM (`alexander/modulo70-seguridad-salud-ocupacional`, que contiene esta pantalla sin cambios). Revisión de Lukas pendiente.

## Revisión visual real — 2026-11-24

API (puerto 5080) y PostgreSQL locales con `scripts/seed-dev.sql`, frontend con Vite (puerto 5173) y sesión de la cuenta administrativa del seed. Navegador: Microsoft Edge en modo headless, controlado por el protocolo DevTools; el tema oscuro se emula con `prefers-color-scheme` y el ancho de 375 px como dispositivo móvil. No se interceptan ni se simulan respuestas, y no se modifican datos. Resultado detallado en [resultado-visual.json](resultado-visual.json).

| Escenario | 375 / claro | 375 / oscuro | 1280 / claro | 1280 / oscuro |
|---|---|---|---|---|
| Vista Programaciones: detalle con inscritos | [Captura](375-light-programaciones.png) | [Captura](375-dark-programaciones.png) | [Captura](1280-light-programaciones.png) | [Captura](1280-dark-programaciones.png) |
| Vista Cursos: detalle del curso | [Captura](375-light-cursos.png) | [Captura](375-dark-cursos.png) | [Captura](1280-light-cursos.png) | [Captura](1280-dark-cursos.png) |
| Vista Horas por empleado | [Captura](375-light-horas.png) | [Captura](375-dark-horas.png) | [Captura](1280-light-horas.png) | [Captura](1280-dark-horas.png) |

| Comprobación (12 capturas) | Resultado |
|---|---|
| Cada escenario llega a su estado (fila, botón, formulario o diálogo) | sí |
| Sin desplazamiento horizontal de la página | sí |
| Sin errores de JavaScript ni de consola | sí |

Las tablas anchas se desplazan dentro de su propia región en móvil, como en el resto de pantallas.

## Cómo repetir

Con la base local preparada (`./scripts/aplicar-bd.sh --reiniciar` y el seed), iniciar la API en 5080 y el frontend en 5173, entrar con la cuenta administrativa del seed y abrir `/modulos/69-capacitaciones`. Repetir los escenarios de la tabla a 375 y 1280 px, en claro y oscuro.
