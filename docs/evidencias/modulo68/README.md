# Módulo 68 — Evaluación de desempeño

QA visual de la pantalla en `alexander/modulo68-evaluacion-desempeno`, sobre la rama final de la cadena HCM (`alexander/modulo70-seguridad-salud-ocupacional`, que contiene esta pantalla sin cambios). Revisión de Lukas pendiente.

## Revisión visual real — 2026-11-24

API (puerto 5080) y PostgreSQL locales con `scripts/seed-dev.sql`, frontend con Vite (puerto 5173) y sesión de la cuenta administrativa del seed. Navegador: Microsoft Edge en modo headless, controlado por el protocolo DevTools; el tema oscuro se emula con `prefers-color-scheme` y el ancho de 375 px como dispositivo móvil. No se interceptan ni se simulan respuestas, y no se modifican datos. Resultado detallado en [resultado-visual.json](resultado-visual.json).

| Escenario | 375 / claro | 375 / oscuro | 1280 / claro | 1280 / oscuro |
|---|---|---|---|---|
| Vista Ciclos: detalle del ciclo con competencias y resultados | [Captura](375-light-ciclos.png) | [Captura](375-dark-ciclos.png) | [Captura](1280-light-ciclos.png) | [Captura](1280-dark-ciclos.png) |
| Vista Evaluaciones: detalle con calificaciones | [Captura](375-light-evaluaciones.png) | [Captura](375-dark-evaluaciones.png) | [Captura](1280-light-evaluaciones.png) | [Captura](1280-dark-evaluaciones.png) |
| Diálogo «Nueva evaluación» | [Captura](375-light-nueva-evaluacion.png) | [Captura](375-dark-nueva-evaluacion.png) | [Captura](1280-light-nueva-evaluacion.png) | [Captura](1280-dark-nueva-evaluacion.png) |

| Comprobación (12 capturas) | Resultado |
|---|---|
| Cada escenario llega a su estado (fila, botón, formulario o diálogo) | sí |
| Sin desplazamiento horizontal de la página | sí |
| Sin errores de JavaScript ni de consola | sí |
| Escape cierra el diálogo (nueva-evaluacion) | sí |

Las tablas anchas se desplazan dentro de su propia región en móvil, como en el resto de pantallas.

## Cómo repetir

Con la base local preparada (`./scripts/aplicar-bd.sh --reiniciar` y el seed), iniciar la API en 5080 y el frontend en 5173, entrar con la cuenta administrativa del seed y abrir `/modulos/68-evaluacion-desempeno`. Repetir los escenarios de la tabla a 375 y 1280 px, en claro y oscuro.
