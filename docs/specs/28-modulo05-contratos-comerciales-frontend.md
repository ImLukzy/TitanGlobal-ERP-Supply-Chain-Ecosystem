# Especificación: 28 - Pantalla de Modulo05_ContratosComerciales (frontend)

## 1. Objetivo
**Problema:** `/modulos/05-contratos-comerciales` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla de contratos comerciales contra el contrato de la spec 27 §4, con el patrón de SolicitudesCompra y OrdenesAbastecimiento (estados y acciones) y la barra de consumo accesible de Zonas (spec 22).

## 2. Fuera de alcance
- La API: `fetch` simulado y contrato de la spec 27 §4.
- Cambiar otras pantallas, salvo enlazar desde las órdenes del periodo a `/modulos/04-ordenes-compra`.
- Librerías nuevas.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/contratos-comerciales/{tipos,api}.ts` | crear | 9 funciones del contrato (`activar` y `finalizar` sin cuerpo) |
| `frontend/src/modules/contratos-comerciales/*.tsx` | crear | partida en componentes (≤ ~300 líneas); reutiliza el medidor de ocupación de Zonas si se puede compartir sin cambiar sus tests |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `05-contratos-comerciales` |
| `frontend/src/modules/contratos-comerciales/*.test.ts(x)` | crear | `fetch` simulado, temporizadores falsos |

## 4. Diseño y lógica
- **Resumen:** borradores, vigentes, vencidos, finalizados y cancelados; comprometido y consumido de los vigentes con barra de consumo; contratos por condición de pago.
- **Filtros:** búsqueda (300 ms), estado, cliente y «Solo vencidos». Página 1; una sola petición al cargar.
- **Tabla:** número, cliente, título, periodo, estado con pastilla (y «Vencido» con texto), comprometido y **barra de consumo** (`role="meter"`, texto «S/ 12,350.00 de S/ 20,000.00 · 61.75 %»; por encima del 100 % la barra se llena y el texto dice «Excedido»). Paginación de 10. Carga, vacío y error.
- **Detalle:** datos, condición de pago, saldo (en rojo y con texto «Excedido» si es negativo), observaciones, motivo de cancelación y fechas de activación y cierre; tabla de órdenes del periodo con enlace a la pantalla de órdenes. Acciones según el estado: Borrador → «Editar», «Activar», «Cancelar», «Eliminar»; Vigente → «Finalizar», «Cancelar»; Finalizado y Cancelado → solo lectura.
- **Formulario (alta y edición de borradores):** cliente, título, fecha de inicio y de fin, monto comprometido, condición de pago y observaciones; validación local con los literales de la spec 27 §4.
- **Diálogos:** activar y finalizar con confirmación; cancelar con motivo obligatorio; 409 (solapamiento, periodo terminado, estado) dentro del diálogo. Tras cada acción se refrescan tabla, detalle y resumen.
- **Accesibilidad y responsive:** como en las specs 17 y 22.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 9 funciones con URL, método, query y cuerpo exactos | pasan |
| A3 | Filtros, tabla y consumo | tests: filtros y «Solo vencidos» en la query, página 1, una sola petición al cargar, medidor con `aria-valuenow` y texto, «Excedido» por encima del 100 %, vacío y error | pasan |
| A4 | Formulario y acciones | tests: validaciones locales sin petición, edición con PUT; activar (sin cuerpo) y 409 de solapamiento en el diálogo; finalizar; cancelar sin motivo (error local) y con motivo; acciones correctas por estado; Escape con foco devuelto | pasan |
| A5 | Sin regresiones | demás pantallas en verde sin cambiar lo que comprueban; la suite pasa 10 veces seguidas | pasan |
| A6 | Visual | capturas (`page.route`) a 375 y 1280 px, claro y oscuro: detalle de un vigente con órdenes del periodo, uno excedido y el diálogo de cancelación; márgenes ≥ 16 px, sin scroll horizontal, celdas recortadas ni palabras partidas | adjuntas |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos y `api.ts`.
- [x] Tarea 2: resumen, filtros y tabla con consumo.
- [x] Tarea 3: detalle, formulario y diálogos.
- [x] Tarea 4: tests, capturas y A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | Corrección 1 | ✅ | Claudio vio en las capturas filtros y paginación con el aspecto del navegador, captions grandes, condiciones de pago en texto suelto y el porcentaje de la barra partido; corregido con los componentes y estilos compartidos; además, el test de «Pantalla pendiente» elige ahora un módulo sin pantalla de forma dinámica |
| 2026-10-15 | Corrección 2 | ✅ | importe de «Comprometido» partido a mitad de número, casilla con letra grande y «Credito30» en el detalle; corregido (importes y fechas sin partir, casilla común, «Crédito 30 días»); de paso, tests de Zonas y SolicitudesGenerarOrden deterministas sin perder comprobaciones |
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` rc=0 en `ImLukzy/Claudio` (`2daa127`, mismas líneas que el auditado `f621df0`): backend 523/523, frontend 232/232 |
| 2026-10-15 | A2–A5 | ✅ | tests de `contratos-comerciales/` (api con activar y finalizar sin cuerpo, filtros y «Solo vencidos», medidor accesible y «Excedido», acciones por estado, 409 en el diálogo, Escape); 10/10 seguidas con carga |
| 2026-10-15 | A6 | ✅ | `/tmp/titan-spec28/` (12 capturas) revisadas por Claudio tras la corrección 2 |
| 2026-10-15 | Auditoría | ✅ | Kelly, ciclo 1: APROBADO sin hallazgos |
