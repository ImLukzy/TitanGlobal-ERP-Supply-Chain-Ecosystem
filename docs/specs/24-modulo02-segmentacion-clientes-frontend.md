# Especificación: 24 - Pantalla de Modulo02_SegmentacionClientes (frontend)

## 1. Objetivo
**Problema:** `/modulos/02-segmentacion-clientes` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla con dos vistas contra el contrato de la spec 23 §4: **Segmentos** (mantenimiento con criterios y prioridad) y **Clasificación** (cada cliente con su segmento calculado), con los componentes compartidos y el patrón de Almacenes (spec 20).

## 2. Fuera de alcance
- La API: `fetch` simulado y contrato de la spec 23 §4.
- Cambiar otras pantallas o librerías nuevas.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/segmentacion-clientes/{tipos,api}.ts` | crear | 8 funciones del contrato; `''` y `undefined` fuera de la query |
| `frontend/src/modules/segmentacion-clientes/*.tsx` | crear | partida en componentes (≤ ~300 líneas por archivo) |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `02-segmentacion-clientes` |
| `frontend/src/modules/segmentacion-clientes/*.test.ts(x)` | crear | `fetch` simulado, temporizadores falsos |

## 4. Diseño y lógica
- **Pestañas accesibles** (`role="tablist"`, flechas izquierda/derecha) «Segmentos» y «Clasificación»; la pestaña activa se refleja en la URL (`?vista=clasificacion`).
- **Resumen:** total de clientes, segmentados y sin segmento; tarjeta por segmento activo (código, nombre, clientes con `formatearCantidad`, monto en PEN), en orden de prioridad.
- **Segmentos:** búsqueda (300 ms) y estado; tabla con prioridad, código, nombre, criterios en texto («≥ S/ 5,000.00 y ≥ 3 órdenes», «≥ 2 órdenes»), clientes y estado con pastilla. Detalle con descripción y la lista de sus clientes (`{id}/clientes`, paginada). Formulario: código (mayúsculas), nombre, descripción, prioridad, monto mínimo, órdenes mínimas y «Activo». Validación local con los literales de la spec 23 §4 (incluida «Define al menos un criterio…»); 409 de código o prioridad visible conservando lo escrito. Baja con `DialogoBaja`.
- **Clasificación:** búsqueda por cliente (300 ms), selector de segmento («Todos», cada segmento activo, «Sin segmento»); tabla cliente, RUC, órdenes, monto (PEN) y segmento con pastilla («Sin segmento» con texto). Paginación de 10.
- Cada listado se pide una sola vez al cargar. Estados de carga, vacío y error en ambas vistas.
- **Accesibilidad y responsive:** como en las specs 20 y 22.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 8 funciones con URL, método, query y cuerpo exactos | pasan |
| A3 | Pestañas y listados | tests: cambio de pestaña con teclado y `?vista=`; filtros en la query y página 1; «Sin segmento» envía `sinSegmento=true`; una sola petición al cargar; criterios en texto; vacío y error | pasan |
| A4 | Formulario y baja | tests: cada validación local sin petición (incluido «al menos un criterio»), 409 de código y de prioridad conservando lo escrito, edición con PUT, baja con refresco, Escape con foco devuelto | pasan |
| A5 | Sin regresiones | demás pantallas en verde sin cambiar lo que comprueban; la suite pasa 10 veces seguidas | pasan |
| A6 | Visual | capturas (`page.route`) a 375 y 1280 px, claro y oscuro: vista Segmentos con detalle, vista Clasificación y diálogo de baja; márgenes ≥ 16 px, sin scroll horizontal, celdas recortadas ni palabras partidas | adjuntas |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos y `api.ts`.
- [x] Tarea 2: pestañas, resumen y vista Segmentos.
- [x] Tarea 3: vista Clasificación.
- [x] Tarea 4: tests, capturas y A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | Corrección 1 | ✅ | La integración vio que el cambio en `App.test.tsx` dejaba sin test la «Pantalla pendiente» (usaba el módulo 02), el código «PREMIUM-OLD» partido en dos líneas y la descripción pegada al título. Corregido: test de pantalla pendiente con otro módulo, códigos sin partir, espaciado común |
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` rc=0 en la rama de integración (`5ab2e08`, mismas líneas que el auditado `7ca8ab0`): backend 272/272, frontend 201/201 |
| 2026-10-15 | A2–A5 | ✅ | tests de `segmentacion-clientes/` (api, pestañas con teclado y `?vista=`, filtros, «Sin segmento», formulario con «al menos un criterio», 409 de código y prioridad, baja con Escape); 10/10 seguidas según la implementación y la auditoría |
| 2026-10-15 | A6 | ✅ | `/tmp/titan-spec24/` (12 capturas) revisadas por la integración tras la corrección |
| 2026-10-15 | Auditoría | ✅ | Auditoría, ciclo 1: APROBADO sin hallazgos |
| 2026-10-15 | QA E2E | ✅ | QA en local (Vite 5174 + API + PostgreSQL) y en producción: resumen 5/4/1 y tarjetas con plurales correctos; pestañas con flechas y `?vista=`; criterios en texto y detalle de VIP con Acme; clasificación Acme → VIP, Beta → FRECUENTE, Gamma y Delta → OCASIONAL, cliente sin órdenes → «Sin segmento» (`sinSegmento=true` en la red); un segmento de menor prioridad no cambia la clasificación; ambos criterios en 0 → error local; prioridad 1 repetida → 409 conservando lo escrito; inactivar OCASIONAL reasigna y se restaura; baja con Escape; sin regresiones; 13 capturas sin recortes ni scroll horizontal |
