# Especificación: 30 - Pantalla de Modulo06_ListasPrecios (frontend)

## 1. Objetivo
**Problema:** `/modulos/06-listas-precios` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla de listas de precios contra el contrato de la spec 29 §4: mantenimiento de listas con sus precios por producto y un **simulador de precio** (producto + cliente + fecha → precio y de dónde sale), con los componentes compartidos y el patrón de las pantallas anteriores.

## 2. Fuera de alcance
- La API: `fetch` simulado y contrato de la spec 29 §4.
- Cambiar otras pantallas o librerías nuevas.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/listas-precios/{tipos,api}.ts` | crear | 7 funciones del contrato |
| `frontend/src/modules/listas-precios/*.tsx` | crear | partida en componentes (≤ ~300 líneas); reutiliza el formulario de líneas si encaja |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `06-listas-precios` |
| `frontend/src/modules/listas-precios/*.test.ts(x)` | crear | `fetch` simulado, temporizadores falsos |

## 4. Diseño y lógica
- **Resumen:** total, activas, vigentes, de segmento y generales; productos con precio especial.
- **Simulador** (arriba): producto, cliente y fecha (por defecto hoy) → tarjeta con el precio en PEN, el precio base, la diferencia y el origen con texto («Lista VIP-2026 (segmento VIP)», «Lista GENERAL-2026», «Precio base»). Se consulta al pulsar «Calcular» (sin peticiones mientras se escribe); errores de la API visibles.
- **Filtros:** búsqueda (300 ms), segmento («Todos», «Generales», cada segmento) y «Solo vigentes». Página 1; una sola petición al cargar.
- **Tabla:** prioridad, código, nombre, segmento («General» si no tiene), vigencia («desde 01/01/2026», «01/01/2026 – 31/12/2026»), productos y estado con pastilla («Vigente», «No vigente», «Inactiva», con texto). Paginación de 10. Carga, vacío y error.
- **Detalle:** datos y tabla de precios (producto, precio base, precio de lista y diferencia «−8.00 %» con texto, no solo color).
- **Formulario:** código (mayúsculas), nombre, segmento (opcional), prioridad, fechas (fin opcional), «Activa» y líneas producto + precio (mostrando el precio base al elegir el producto). Validación local con los literales de la spec 29; 409 de código o prioridad conservando lo escrito. Baja con `DialogoBaja`.
- **Accesibilidad y responsive:** como en las pantallas anteriores.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 7 funciones con URL, método, query y cuerpo exactos | pasan |
| A3 | Simulador | tests: no pide nada hasta «Calcular»; envía producto, cliente y fecha; muestra precio, base, diferencia y origen para los tres orígenes; error de la API visible | pasan |
| A4 | Listado, formulario y baja | tests: filtros en la query, página 1, una sola petición al cargar, estado con texto; validaciones locales sin petición; precio base mostrado al elegir producto; 409 conservando lo escrito; PUT; baja con Escape | pasan |
| A5 | Sin regresiones | demás pantallas en verde sin cambiar lo que comprueban; la suite pasa 10 veces seguidas | pasan |
| A6 | Visual | capturas (`page.route`) a 375 y 1280 px, claro y oscuro: simulador con resultado, detalle de una lista y diálogo de baja; márgenes ≥ 16 px, sin scroll horizontal, celdas recortadas ni palabras partidas | adjuntas |

## 6. Checklist de ejecución
- [ ] Tarea 1: tipos y `api.ts`.
- [ ] Tarea 2: resumen y simulador.
- [ ] Tarea 3: filtros, tabla, detalle, formulario y baja.
- [ ] Tarea 4: tests, capturas y A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
