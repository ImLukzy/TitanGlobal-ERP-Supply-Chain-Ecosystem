# Módulo 66 — Planillas

QA visual de la pantalla en `alexander/modulo66-planillas`, sobre la rama final de la cadena HCM (`alexander/modulo70-seguridad-salud-ocupacional`, que contiene esta pantalla sin cambios). Revisión de Lukas pendiente.

## Revisión visual real — 2026-11-24

API (puerto 5080) y PostgreSQL locales con `scripts/seed-dev.sql`, frontend con Vite (puerto 5173) y sesión de la cuenta administrativa del seed. Navegador: Microsoft Edge en modo headless, controlado por el protocolo DevTools; el tema oscuro se emula con `prefers-color-scheme` y el ancho de 375 px como dispositivo móvil. No se interceptan ni se simulan respuestas, y no se modifican datos. Resultado detallado en [resultado-visual.json](resultado-visual.json).

| Escenario | 375 / claro | 375 / oscuro | 1280 / claro | 1280 / oscuro |
|---|---|---|---|---|
| Vista Planillas: detalle de la planilla 10/2026 `Pagada` con totales y empleados | [Captura](375-light-planillas.png) | [Captura](375-dark-planillas.png) | [Captura](1280-light-planillas.png) | [Captura](1280-dark-planillas.png) |
| Vista Bonos: formulario «Nuevo bono» | [Captura](375-light-bonos.png) | [Captura](375-dark-bonos.png) | [Captura](1280-light-bonos.png) | [Captura](1280-dark-bonos.png) |
| Vista Afiliaciones a pensión | [Captura](375-light-afiliaciones.png) | [Captura](375-dark-afiliaciones.png) | [Captura](1280-light-afiliaciones.png) | [Captura](1280-dark-afiliaciones.png) |
| Boleta de pago de un empleado de la planilla `Pagada` (EsSalud como aporte del empleador) | [Captura](375-light-boleta.png) | [Captura](375-dark-boleta.png) | [Captura](1280-light-boleta.png) | [Captura](1280-dark-boleta.png) |

| Comprobación (16 capturas) | Resultado |
|---|---|
| Cada escenario llega a su estado (fila, botón, formulario o diálogo) | sí |
| Sin desplazamiento horizontal de la página | sí |
| Sin errores de JavaScript ni de consola | sí |
| Escape cierra el diálogo (boleta) | sí |

Las tablas anchas se desplazan dentro de su propia región en móvil, como en el resto de pantallas.

## Cómo repetir

Con la base local preparada (`./scripts/aplicar-bd.sh --reiniciar` y el seed), iniciar la API en 5080 y el frontend en 5173, entrar con la cuenta administrativa del seed y abrir `/modulos/66-planillas`. Repetir los escenarios de la tabla a 375 y 1280 px, en claro y oscuro.
