# Especificación: 26 - Pantalla de Modulo03_Cotizaciones (frontend)

## 1. Objetivo
**Problema:** `/modulos/03-cotizaciones` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla de cotizaciones contra el contrato de la spec 25 §4, con el patrón y los componentes de SolicitudesCompra (spec 15): líneas dinámicas con descuento, aceptar (genera la orden de compra), rechazar con motivo y resumen.

## 2. Fuera de alcance
- La API: `fetch` simulado y contrato de la spec 25 §4.
- Cambiar otras pantallas, salvo enlazar desde el detalle de una cotización aceptada a `/modulos/04-ordenes-compra`.
- Librerías nuevas.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/cotizaciones/{tipos,api}.ts` | crear | 8 funciones del contrato |
| `frontend/src/modules/cotizaciones/*.tsx` | crear | partida en componentes (≤ ~300 líneas por archivo); reutiliza el formulario de líneas de SolicitudesCompra si se puede compartir sin cambiar sus tests |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `03-cotizaciones` |
| `frontend/src/modules/cotizaciones/*.test.ts(x)` | crear | `fetch` simulado, temporizadores falsos |

## 4. Diseño y lógica
- **Resumen:** emitidas, vencidas, aceptadas, rechazadas, monto vigente, monto aceptado (PEN) y tasa de aceptación («66.67 %»); top de clientes con `formatearCantidad`.
- **Filtros:** búsqueda por número o cliente (300 ms), estado, cliente (de `GET clientes?tamanoPagina=100`), «Solo vencidas» y rango de fechas (validación local). Vuelta a la página 1; listado una sola vez al cargar.
- **Tabla:** número, fecha, cliente, validez con etiqueta «Vencida» (texto), estado con pastilla, total y orden generada (si la hay). Paginación de 10. Carga, vacío y error.
- **Detalle:** cabecera, líneas (producto, cantidad, precio, % descuento, importe), subtotal, descuento y total; motivo si fue rechazada; si fue aceptada, enlace «Ver orden OC-…» a la pantalla de órdenes. Si está Emitida y vigente: «Editar», «Aceptar», «Rechazar», «Eliminar»; si está vencida: «Editar» (para ampliar la validez), «Rechazar» y «Eliminar», sin «Aceptar».
- **Formulario:** cliente, fecha de validez (no anterior a hoy), observaciones y líneas (producto que precarga su precio, cantidad, precio, % descuento) con importes y totales en vivo. Validación local con los literales de la spec 25; errores de la API conservando lo escrito.
- **Aceptar:** confirmación accesible que avisa «Se generará la orden de compra OC-{numero}.»; al aceptar, muestra el número de la orden creada. **Rechazar:** motivo obligatorio. 409 dentro del diálogo. Tras cada acción se refrescan tabla, detalle y resumen.
- **Accesibilidad y responsive:** como en las specs 15 y 17.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 8 funciones con URL, método, query y cuerpo exactos (`aceptar` sin cuerpo) | pasan |
| A3 | Filtros y tabla | tests: espera, filtros y «Solo vencidas» en la query, rango inválido sin petición, página 1, una sola petición al cargar, etiqueta «Vencida», vacío y error | pasan |
| A4 | Formulario | tests: precio precargado al elegir producto, importes con descuento y totales en vivo (redondeo a 2 decimales igual que la API), validaciones locales sin petición, 400 conservando lo escrito, edición con PUT | pasan |
| A5 | Acciones | tests: aceptar → POST sin cuerpo, muestra la orden generada y el enlace; vencida sin «Aceptar»; rechazar sin motivo (error local) y con motivo; 409 en el diálogo; cotización decidida sin acciones; Escape con foco devuelto | pasan |
| A6 | Sin regresiones | demás pantallas en verde sin cambiar lo que comprueban; la suite pasa 10 veces seguidas | pasan |
| A7 | Visual | capturas (`page.route`) a 375 y 1280 px, claro y oscuro: detalle con descuentos, formulario con 3 líneas, diálogo de aceptación y una vencida; márgenes ≥ 16 px, sin scroll horizontal, celdas recortadas ni palabras partidas | adjuntas |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos y `api.ts`.
- [x] Tarea 2: resumen, filtros y tabla.
- [x] Tarea 3: detalle, formulario de líneas con descuento y diálogos.
- [x] Tarea 4: tests, capturas y A1–A7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | Corrección 1 | ✅ | Claudio vio en la captura del detalle «Subtotal 2,500 − Descuento 187.50 = Total 2,812.50»: los mocks eran incoherentes (el código muestra los totales de la API). Corregido: mocks calculados como la API y test de coherencia subtotal − descuento = total; además, observaciones con el estilo común y columnas del detalle separadas |
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` rc=0 en `ImLukzy/Claudio` (`736c7f7`, mismas líneas que el auditado `975c055`): backend 329/329, frontend 222/222 |
| 2026-10-15 | A2–A6 | ✅ | tests de `cotizaciones/` (api con aceptar sin cuerpo, filtros y «Solo vencidas», formulario con descuentos y totales en vivo, aceptar con orden generada, vencida sin «Aceptar», rechazo, 409 en el diálogo, Escape); 10/10 seguidas según Dwight |
| 2026-10-15 | A7 | ✅ | `/tmp/titan-spec26/` (16 capturas) revisadas por Claudio tras la corrección: totales coherentes, detalle sin recortes |
| 2026-10-15 | Auditoría | ✅ | Kelly, ciclo 1: APROBADO sin hallazgos |
| 2026-10-15 | QA E2E | ✅ | Pam en local (Vite 5174 + API + PostgreSQL) y en producción: resumen del seed con tasa 50.00 % y subtotal − descuento = total en cada detalle; filtros, «Solo vencidas», rango inválido sin petición; vencida sin «Aceptar» y rechazada sin acciones; alta con 3 líneas y descuentos con importes en vivo iguales a la API; validaciones locales; edición; aceptar con aviso «Se generará la orden de compra OC-COT-000005.», orden enlazada y visible en la jerarquía del cliente; aceptar de nuevo → 409 en el diálogo; rechazar con y sin motivo; eliminar con Escape; sin regresiones; 16 capturas sin recortes |
