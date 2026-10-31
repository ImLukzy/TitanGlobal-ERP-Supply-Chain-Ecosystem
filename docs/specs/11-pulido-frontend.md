# Especificación: 11 - Pulido del frontend (miga de pan, plurales, filtros)

## 1. Objetivo
**Problema:** el QA de las specs 08 y 10 dejó detalles menores de acabado:
- la miga de pan termina en el número del módulo («… / 21») y «Inicio» no es un enlace;
- textos con plural fijo: «1 unidades», «1 órdenes»;
- en Productos, cuando un filtro de precio muestra su error, los campos de la fila de filtros se desalinean;
- dos observaciones de la auditoría de la 10: sentencias `if (…) return;` en una línea y `listarProductos` enviaría «undefined» si una clave llegara con ese valor.

**Resultado esperado:** las tres pantallas (OrdenesCompra, Clientes y Productos) y la página «pendiente» quedan con la miga de pan correcta, plurales bien escritos y filtros alineados, sin cambiar su comportamiento.

## 2. Fuera de alcance
- Backend, contratos de la API y lógica de negocio.
- Rediseño visual, librerías nuevas o cambios de estilos fuera de lo listado.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/components/PaginaModulo.tsx` (+ test) | modificar | miga de pan (§4.1) |
| `frontend/src/lib/formato.ts` (+ test) | modificar | helper de plural (§4.2) |
| `frontend/src/modules/clientes/ClientesPage.tsx` | modificar | usar el helper |
| `frontend/src/modules/productos/{ProductosPage.tsx,api.ts}` (+ tests) | modificar | §4.2–§4.4 |
| `frontend/src/styles/app.css` | modificar | alineación de filtros |

## 4. Diseño y lógica
1. **Miga de pan** (`PaginaModulo`): `Inicio` es un `Link` a `/`; el último elemento es `NN · Nombre` (p. ej. «21 · Productos») con `aria-current="page"`. El resto igual.
2. **Plurales:** `formatearCantidad(n, singular, plural)` en `lib/formato.ts` → «1 unidad», «0 unidades», «4 unidades». Se usa en «Más vendidos» de Productos y en el top de Clientes («1 orden», «4 órdenes»). Revisar que no queden otros plurales fijos con número delante en `frontend/src`.
3. **Filtros de Productos:** con un mensaje de error bajo «Precio mínimo» o «Precio máximo», los cuatro campos de la fila siguen alineados por arriba (los inputs a la misma altura) y el mensaje no empuja a los vecinos. A 375 px se mantiene el apilado actual.
4. **Auditoría de la 10:** las condiciones de una línea pasan a bloque con llaves; `listarProductos` omite claves con `undefined` además de `''`.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | Miga de pan | test de `PaginaModulo`: enlace «Inicio» a `/`, último elemento «NN · Nombre» con `aria-current="page"` | pasa |
| A3 | Plurales | tests del helper (0, 1, 2) y de las pantallas: «1 unidad» y «1 orden» visibles con datos simulados | pasan |
| A4 | API | test: `listarProductos` con una clave `undefined` no la envía | pasa |
| A5 | Sin regresiones | el resto de tests de las tres pantallas en verde sin cambiar lo que comprueban (salvo textos de miga y plural) | pasan |
| A6 | Visual | capturas (`page.route`) a 375 y 1280 px, claro y oscuro: Productos con el error de «Precio mínimo» visible (inputs a la misma coordenada `top` medida) y la miga de pan de las tres pantallas | adjuntas |

## 6. Checklist de ejecución
- [x] Tarea 1: miga de pan y plurales.
- [x] Tarea 2: filtros de Productos y observaciones de la auditoría.
- [x] Tarea 3: tests, capturas y A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-14 | Corrección 1 | ✅ | La integración vio en la captura oscura «Inicio» con el azul por defecto del navegador (contraste insuficiente) y faltaban las capturas de la miga en las tres pantallas. Corregido: `--ink-soft`, contraste 5.12:1 en claro y 8.20:1 en oscuro |
| 2026-10-14 | A1 | ✅ | `./scripts/verify.sh` rc=0 en la rama de integración (`2ea428d`, idéntico al auditado `9140478` según `git patch-id`): backend 81/81, frontend 76/76 |
| 2026-10-14 | A2–A4 | ✅ | test de `PaginaModulo` (enlace a `/`, «NN · Nombre» con `aria-current`); `formatearCantidad` con 0/1/2 y «1 unidad»/«1 orden» en pantallas; `listarProductos` omite `undefined` |
| 2026-10-14 | A5 | ⚠️→✅ | el commit sustituyó el test de `formatearFecha` (UTC, spec 04) por el de plurales. La integración lo restauró en `e792983`; pasa con `TZ=America/Lima` |
| 2026-10-14 | A6 | ✅ | `/tmp/titan-spec11/`: filtros de Productos con error alineados (top 630 px) a 1280; miga de pan de Clientes, OrdenesCompra y Productos en claro y oscuro revisada por la integración |
| 2026-10-14 | Auditoría | ✅ | Auditoría, ciclo 1: APROBADO; menores: el test de fechas quitado (resuelto arriba) y un `if (vivo) setError(…)` en una línea |
