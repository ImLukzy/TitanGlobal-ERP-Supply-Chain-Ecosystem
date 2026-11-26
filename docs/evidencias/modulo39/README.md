# Módulo 39: mantenimiento preventivo — verificación de la pantalla

Fecha: 2026-11-25. Responsable: Antony Huayhua. Pantalla de la rama `antony/modulo39-mantenimiento-preventivo-frontend` (PR #104) contra la API de la rama `antony/modulo39-mantenimiento-preventivo` (PR #87), con PostgreSQL local creado con `./scripts/aplicar-bd.sh --reiniciar` y `scripts/seed-dev.sql`.

## A5: prueba con la API real

Peticiones a través del proxy de Vite (`http://localhost:5173/api/v1`, el mismo camino que usa la pantalla), con la sesión de `admin`:

| Paso | Resultado |
|---|---|
| Centros activos para el formulario (`GET /centros-trabajo?activo=true`) | 200 |
| Programar una parada (`POST /mantenimientos-preventivos`) | 201, `MP-000005` |
| Programar otra en el mismo centro y la misma fecha | 409: «El centro de trabajo CT-ARE-C01 ya tiene un mantenimiento programado el 2027-05-10.» |
| Editar descripción, fecha y responsable (`PUT`) | 200 |
| Iniciar sin cuerpo (`POST …/iniciar`) | 200 |
| Completar con 3.25 horas reales (`POST …/completar`) | 200; el detalle queda `Completado` con la siguiente parada sugerida |
| Cancelar otra parada con motivo (`POST …/cancelar`) | 200; el detalle queda `Cancelado` con el motivo |
| Resumen (`GET …/resumen`) | 200, con los totales por estado actualizados |

## A6: control visual

Capturas con Chrome sin ventana a 375 px y 1280 px, en claro (`light`) y oscuro (`dark`), con una parada programada seleccionada (`detalle`) y con el diálogo de cancelación abierto (`cancelacion`). En las ocho, la página no tiene desplazamiento horizontal (a 375 px la tabla se desplaza dentro de su contenedor) y no hay errores de JavaScript; la consola solo registra el 404 de `/favicon.ico` del servidor de desarrollo.

| | Claro | Oscuro |
|---|---|---|
| 375 px, detalle | `375-light-detalle.png` | `375-dark-detalle.png` |
| 375 px, cancelación | `375-light-cancelacion.png` | `375-dark-cancelacion.png` |
| 1280 px, detalle | `1280-light-detalle.png` | `1280-dark-detalle.png` |
| 1280 px, cancelación | `1280-light-cancelacion.png` | `1280-dark-cancelacion.png` |

La primera ronda de capturas mostró que a 1280 px la tabla escondía tras su desplazamiento las columnas de fecha, duración y estado, y que los números `MP-…` se partían. Se corrigió en el commit `style(mantenimiento-preventivo)` y estas capturas son posteriores.
