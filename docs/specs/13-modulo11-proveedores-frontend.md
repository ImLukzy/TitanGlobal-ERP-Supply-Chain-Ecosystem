# Especificación: 13 - Pantalla de Modulo11_Proveedores (frontend)

## 1. Objetivo
**Problema:** `/modulos/11-proveedores` muestra «Pantalla pendiente».
**Resultado esperado:** una pantalla de mantenimiento de proveedores con el mismo patrón y los mismos componentes compartidos que Clientes y Productos (`PaginaModulo`, `TablaPaginada`, `PanelDetalle`, `DialogoBaja`, `CampoConError`), contra el contrato de la spec 12 §4.

## 2. Fuera de alcance
- La API: se trabaja con `fetch` simulado y el contrato de la spec 12 §4.
- Cambiar Clientes, Productos u OrdenesCompra, salvo mejoras de los componentes compartidos que no cambien lo que comprueban sus tests.
- Librerías nuevas.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/proveedores/{tipos,api}.ts` | crear | 7 funciones: `listarProveedores(filtros)`, `obtenerProveedor`, `crearProveedor`, `actualizarProveedor`, `eliminarProveedor`, `listarPaisesProveedor`, `obtenerResumenProveedores`; los filtros vacíos o `undefined` no se envían |
| `frontend/src/modules/proveedores/ProveedoresPage.tsx` + componentes | crear | §4; usa `PaginaModulo` y los componentes compartidos |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `11-proveedores` |
| `frontend/src/modules/proveedores/*.test.ts(x)` | crear | `fetch` simulado |

## 4. Diseño y lógica
- **Resumen:** total, activos, inactivos y sin email; tabla por país (proveedores y activos).
- **Filtros:** búsqueda por razón social o RUC con espera de 300 ms; selector de país (de `GET proveedores/paises`, con «Todos»); selector de estado («Todos», «Activos», «Inactivos»). Cualquier cambio vuelve a la página 1.
- **Tabla:** razón social, RUC, país y una etiqueta de estado («Activo»/«Inactivo», no solo color). Paginación de 10 en 10. Estados de carga, vacío y error.
- **Detalle (lateral en escritorio, debajo en móvil):** todos los campos; email y teléfono ausentes → «Sin email» / «Sin teléfono».
- **Formulario:** razón social, RUC, país (texto con `datalist` de países), email, teléfono y casilla «Activo» (marcada por defecto en el alta). Validación local con los mismos literales de la spec 12 §4, por campo con `aria-describedby`. Los errores 400/409 de la API se muestran con su `Message` sin perder lo escrito.
- **Baja:** diálogo accesible con el nombre del proveedor; Escape cancela y devuelve el foco.
- **Accesibilidad y responsive:** como en las specs 08 y 10 (labels, `aria-live`, `<caption>`, ≥ 16 px de margen, sin scroll horizontal de página ni celdas recortadas a 1280 px).

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 7 funciones con URL, método, query y cuerpo exactos; filtros vacíos y `undefined` fuera de la query | pasan |
| A3 | Filtros y tabla | tests: espera de la búsqueda (temporizadores falsos), país y estado en la query, vuelta a la página 1, estados vacío y error | pasan |
| A4 | Detalle, formulario y baja | tests: «Sin email»/«Sin teléfono»; cada validación local sin petición; 409 de RUC visible conservando lo escrito; edición precargada y PUT con su id; baja OK con refresco; Escape con foco devuelto | pasan |
| A5 | Sin regresiones | tests de Clientes, Productos y OrdenesCompra en verde sin cambiar lo que comprueban | pasan |
| A6 | Visual | capturas (`page.route`) a 375 y 1280 px, claro y oscuro, con detalle abierto y con el diálogo de baja; márgenes ≥ 16 px medidos; sin scroll horizontal ni celdas recortadas | adjuntas |

## 6. Checklist de ejecución
- [ ] Tarea 1: tipos y `api.ts`.
- [ ] Tarea 2: resumen, filtros y tabla.
- [ ] Tarea 3: detalle, formulario y baja.
- [ ] Tarea 4: tests, capturas y A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
