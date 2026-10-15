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
- [ ] Tarea 1: miga de pan y plurales.
- [ ] Tarea 2: filtros de Productos y observaciones de la auditoría.
- [ ] Tarea 3: tests, capturas y A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
