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
- [ ] Tarea 1: tipos, `api.ts` y proxy configurable.
- [ ] Tarea 2: resumen, filtros y tabla.
- [ ] Tarea 3: detalle, formulario y baja (extrayendo lo compartible).
- [ ] Tarea 4: tests, capturas y A1–A7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
