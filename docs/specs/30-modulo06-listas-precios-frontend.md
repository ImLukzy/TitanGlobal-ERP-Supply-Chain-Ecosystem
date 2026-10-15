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
- [x] Tarea 1: tipos y `api.ts`.
- [x] Tarea 2: resumen y simulador.
- [x] Tarea 3: filtros, tabla, detalle, formulario y baja.
- [x] Tarea 4: tests, capturas y A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | Integración | ✅ | `c21e6a0` + `00bf089` (Dwight) integrados como `3597e39` y `3415fc2` en `ImLukzy/Claudio` por cherry-pick, con líneas idénticas; junto con el ajuste del backend `c48b5ec` (spec 29, `segmentoId=0`) |
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` → exit 0: 572 tests del backend y 242 del frontend en verde; `./scripts/smoke-postgres.sh` → `SMOKE OK` |
| 2026-10-15 | A2–A4 | ✅ | `api.test.ts` (7 funciones con URL, método, query y cuerpo), tests del simulador (sin petición hasta «Calcular», tres orígenes, error visible) y del listado, formulario y baja |
| 2026-10-15 | A5 | ✅ | suite del frontend 10/10 seguidas con 4 procesos `yes` de carga (Dwight, corrección 2) |
| 2026-10-15 | A6 | ✅ | dos correcciones de diseño frente a Contratos (tarjetas, ficha del simulador y del detalle, cabeceras en una línea y del mismo tamaño, fondo uniforme por fila en oscuro, «Solo vigentes» en una línea); capturas `/tmp/titan-spec30/c2-*.png` a 375 y 1280 px en claro y oscuro, revisadas por el integrador (fondo de fila medido por píxel) |
| 2026-10-15 | QA E2E | ✅ | Pam en local (Vite 5174 + API + PostgreSQL) y en producción: resumen, simulador con signo − Unicode, detalle, validaciones y CRUD correctos; hallazgo 1 (el filtro «Generales» devolvía 0 filas) resuelto en el backend con `segmentoId=0` (spec 29), aprobado por Kelly |
| 2026-10-15 | Producción | ✅ | Render `live 3415fc2` y Vercel: `GET /api/v1/listas-precios?segmentoId=0` → 2 (`PROMO-2025`, `GENERAL-2026`); con `vigentes=true` → 1 (`GENERAL-2026`); `/modulos/06-listas-precios` → 200 |
