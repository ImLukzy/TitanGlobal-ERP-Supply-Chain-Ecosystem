# Módulo 62 — Contratos laborales

QA visual de la pantalla en `alexander/modulo62-contratos-laborales-frontend`, sobre la rama final de la cadena HCM (`alexander/modulo70-seguridad-salud-ocupacional`, que contiene esta pantalla sin cambios). Revisión de Lukas pendiente.

## Revisión visual real — 2026-11-24

API (puerto 5080) y PostgreSQL locales con `scripts/seed-dev.sql`, frontend con Vite (puerto 5173) y sesión de la cuenta administrativa del seed. Navegador: Microsoft Edge en modo headless, controlado por el protocolo DevTools; el tema oscuro se emula con `prefers-color-scheme` y el ancho de 375 px como dispositivo móvil. No se interceptan ni se simulan respuestas, y no se modifican datos. Resultado detallado en [resultado-visual.json](resultado-visual.json).

| Escenario | 375 / claro | 375 / oscuro | 1280 / claro | 1280 / oscuro |
|---|---|---|---|---|
| Detalle de CTR-000001 `Vigente`: cargo y sueldo vigentes por adenda («Gerente Comercial», S/ 9,500.00) | [Captura](375-light-detalle.png) | [Captura](375-dark-detalle.png) | [Captura](1280-light-detalle.png) | [Captura](1280-dark-detalle.png) |
| Detalle de CTR-000005 `Vencido`, solo renovable | [Captura](375-light-vencido.png) | [Captura](375-dark-vencido.png) | [Captura](1280-light-vencido.png) | [Captura](1280-dark-vencido.png) |
| Detalle de un contrato `Anulado` con fecha y motivo de cierre | [Captura](375-light-anulado.png) | [Captura](375-dark-anulado.png) | [Captura](1280-light-anulado.png) | [Captura](1280-dark-anulado.png) |
| Formulario de alta («Nuevo contrato») | [Captura](375-light-formulario.png) | [Captura](375-dark-formulario.png) | [Captura](1280-light-formulario.png) | [Captura](1280-dark-formulario.png) |
| Diálogo de renovación sobre CTR-000005 | [Captura](375-light-renovacion.png) | [Captura](375-dark-renovacion.png) | [Captura](1280-light-renovacion.png) | [Captura](1280-dark-renovacion.png) |
| Diálogo de adenda sobre CTR-000001 | [Captura](375-light-adenda.png) | [Captura](375-dark-adenda.png) | [Captura](1280-light-adenda.png) | [Captura](1280-dark-adenda.png) |
| Diálogo de anulación sobre CTR-000001 | [Captura](375-light-anulacion.png) | [Captura](375-dark-anulacion.png) | [Captura](1280-light-anulacion.png) | [Captura](1280-dark-anulacion.png) |
| Empleados (61) con el mismo ancho y tema, para comparar el diseño común | [Captura](375-light-empleados.png) | [Captura](375-dark-empleados.png) | [Captura](1280-light-empleados.png) | [Captura](1280-dark-empleados.png) |

| Comprobación (32 capturas) | Resultado |
|---|---|
| Cada escenario llega a su estado (fila, botón, formulario o diálogo) | sí |
| Sin desplazamiento horizontal de la página | sí |
| Sin errores de JavaScript ni de consola | sí |
| Escape cierra el diálogo (renovacion, adenda, anulacion) | sí |
| Datos esperados del seed visibles (detalle, vencido, anulado) | sí |

Las tablas anchas se desplazan dentro de su propia región en móvil, como en el resto de pantallas.

## Cómo repetir

Con la base local preparada (`./scripts/aplicar-bd.sh --reiniciar` y el seed), iniciar la API en 5080 y el frontend en 5173, entrar con la cuenta administrativa del seed y abrir `/modulos/62-contratos-laborales`. Repetir los escenarios de la tabla a 375 y 1280 px, en claro y oscuro.
