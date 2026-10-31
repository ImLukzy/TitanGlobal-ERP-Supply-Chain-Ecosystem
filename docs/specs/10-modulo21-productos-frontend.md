# Especificación: 10 - Pantalla de Modulo21_Productos (frontend)

## 1. Objetivo
**Problema:** `/modulos/21-productos` muestra «Pantalla pendiente». Además, el proxy de Vite apunta fijo a `localhost:5080`, de modo que solo se puede probar una API a la vez en la máquina.
**Resultado esperado:**
- Una pantalla de mantenimiento de productos, con el mismo patrón que Clientes (spec 08), que consume el contrato de la spec 09 §4: resumen, filtros, tabla paginada, detalle con ventas, alta, edición y baja.
- El destino del proxy se puede configurar.

## 2. Fuera de alcance
- La API: se trabaja con `fetch` simulado y el contrato de la spec 09 §4.
- Cambiar el comportamiento de Clientes u OrdenesCompra, salvo extraer componentes compartidos sin cambiar lo que comprueban sus tests.
- Librerías nuevas.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/productos/{tipos,api}.ts` | crear | tipos y 7 funciones del contrato de la spec 09 §4 (`listarProductos(filtros)`, `obtenerProducto`, `crearProducto`, `actualizarProducto`, `eliminarProducto`, `listarCategoriasProducto`, `obtenerResumenProductos`); los filtros vacíos no se envían |
| `frontend/src/modules/productos/ProductosPage.tsx` + componentes | crear | §4; usa `PaginaModulo` |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `21-productos` |
| `frontend/src/components/*` | crear o modificar | si la tabla paginada, el panel de detalle, el formulario o el diálogo de baja de Clientes se pueden compartir, se extraen y ambas pantallas los usan |
| `frontend/vite.config.ts` | modificar | destino del proxy `/api` = `process.env.TITAN_API_URL ?? 'http://localhost:5080'` |
| `frontend/README.md` | modificar | documentar `TITAN_API_URL` |
| `frontend/src/modules/productos/*.test.tsx` | crear | `fetch` simulado |

## 4. Diseño y lógica
- **Resumen:**
  - total de productos, productos sin descripción y precio promedio en PEN;
  - tabla por categoría (productos, precio promedio, precio máximo);
  - top 5 de más vendidos (unidades y monto).
- **Filtros:**
  - búsqueda por nombre con espera de 300 ms;
  - selector de categoría, alimentado por `GET productos/categorias`, con la opción «Todas»;
  - precio mínimo y máximo, validados en cliente con los mismos mensajes que la API.
  - Cualquier cambio de filtro vuelve a la página 1.
- **Tabla:** nombre, categoría, precio (PEN) y un indicador «Sin descripción». Paginación de 10 en 10. Estados de carga, vacío y error.
- **Detalle (lateral en escritorio, debajo en móvil):** datos del producto y ventas (unidades vendidas, monto vendido y número de clientes compradores). Si no tiene ventas, mostrar «Sin ventas registradas».
- **Formulario:** nombre (obligatorio, máximo 200), categoría (obligatoria, máximo 100; campo de texto con `datalist` de las categorías existentes), precio (> 0) y descripción opcional. Los errores de la API se muestran con su `Message` sin perder lo escrito.
- **Baja:** con confirmación accesible. Un 409 («tiene ventas registradas») se muestra en el diálogo.
- **Accesibilidad y responsive:** como en la spec 08 (labels, `aria-describedby`, `aria-live`, `<caption>`, ≥ 16 px de margen y sin scroll horizontal de página).

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 7 funciones con URL, método, query y cuerpo exactos; los filtros vacíos no aparecen en la query | pasan |
| A3 | Filtros y tabla | tests: espera de la búsqueda, categoría y rango de precio en la query, vuelta a la página 1, validación de precio mínimo mayor que el máximo sin petición, estados vacío y error | pasan |
| A4 | Detalle, formulario y baja | tests: ventas y «Sin ventas registradas»; validación local; 400 de la API visible; baja con 409 en el diálogo; Escape cancela | pasan |
| A5 | Proxy | `TITAN_API_URL=http://localhost:5090 npx vite` dirige `/api` a 5090 (comprobado con un servidor de prueba o leyendo la configuración resuelta); sin la variable, a 5080 | evidencia |
| A6 | Sin regresiones | los tests de Clientes y OrdenesCompra siguen en verde sin cambiar lo que comprueban | pasan |
| A7 | Visual | capturas (`page.route`) a 375 y 1280 px, en claro y oscuro, con detalle abierto; márgenes ≥ 16 px medidos; sin scroll horizontal | adjuntas |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos, `api.ts` y proxy configurable.
- [x] Tarea 2: resumen, filtros y tabla.
- [x] Tarea 3: detalle, formulario y baja (extrayendo lo compartible).
- [x] Tarea 4: tests, capturas y A1–A7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-14 | Corrección 1 | ✅ | La integración devolvió la primera entrega: mensajes distintos de la API, sin «Sin ventas registradas», nombre `listarCategorias`, tests de A2–A4 insuficientes, capturas perdidas. Corregido en la corrección 1 |
| 2026-10-14 | Ajuste visual | ✅ | La integración vio en la captura de 1280 px la columna «Descripción» cortada; la implementación lo ajustó solo en Productos (tabla 469/469 px sin recorte) |
| 2026-10-14 | A1 | ✅ | `./scripts/verify.sh` rc=0 en `ImLukzy/Claudio` (`99a73fd`, idéntico al auditado `ed0a9d9` según `git patch-id`): backend 81/81, frontend 73/73 |
| 2026-10-14 | A2–A4 | ✅ | `api.test.ts` (7 funciones, filtros vacíos fuera de la query) y `ProductosPage.test.tsx` (búsqueda con 300 ms, filtros y vuelta a la página 1, validación de precios sin petición, vacío y error, «Sin ventas registradas», errores literales por campo, 400 conservando lo escrito, 409 en el diálogo, Escape con foco devuelto) |
| 2026-10-14 | A5 | ✅ | `vite.config.ts`: `resolveApiTarget()` usa `TITAN_API_URL` o `http://localhost:5080`; test con 5099 y con el valor por defecto; documentado en `frontend/README.md` |
| 2026-10-14 | A6 | ✅ | tests de Clientes y OrdenesCompra en verde tras extraer `TablaPaginada`, `PanelDetalle`, `DialogoBaja` y `CampoConError` |
| 2026-10-14 | A7 | ✅ | capturas de la implementación en `/tmp/titan-spec10/` revisadas por la integración (375 y 1280, claro y oscuro, detalle abierto): márgenes ≥ 16 px, sin scroll horizontal de página |
| 2026-10-14 | Auditoría | ✅ | Auditoría, ciclo 1: APROBADO; menores: algunos `if (…) return;` en una línea, y `listarProductos` enviaría «undefined» si se pasara `tamanoPagina: undefined` explícito (hoy no ocurre) |
| 2026-10-14 | QA E2E | ✅ | QA con frontend (Vite 5174) + API + PostgreSQL en `22f3215`: resumen del seed (10 productos, 3 sin descripción, S/ 994.50, 5 categorías, top vendidos); búsqueda, categoría, rango y vuelta a la página 1; precios negativos y mínimo > máximo con literales y sin petición; detalle de Laptop Pro y «Sin ventas registradas»; alta, edición, validación local; baja de Laptop Pro → 409 en el diálogo, Escape, baja del creado; Clientes y OrdenesCompra sin regresiones; márgenes ≥ 16 px, sin scroll horizontal ni celdas recortadas a 1280 |
| 2026-10-14 | Pendientes menores | ℹ️ | vistos por la integración en las capturas del QA: con un error de precio los campos del filtro se desalinean; «1 unidades» en Más vendidos; la miga de pan termina en «21» y no en el nombre (viene de `PaginaModulo`, afecta a todas las pantallas). Candidatos a una spec de pulido |
