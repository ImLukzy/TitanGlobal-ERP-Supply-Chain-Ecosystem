# Especificación: modulo20-acuerdos-abastecimiento-frontend — Pantalla de acuerdos de abastecimiento

## 1. Objetivo
**Problema:** `/modulos/20-acuerdos-abastecimiento` muestra «Pantalla pendiente»; la API de acuerdos marco (spec `modulo20-acuerdos-abastecimiento-backend`) no tiene interfaz.
**Resultado esperado:** una pantalla para gestionar acuerdos marco con el mismo patrón y los mismos componentes compartidos que Proveedores y Órdenes de abastecimiento (`PaginaModulo`, `TablaPaginada`, `PanelDetalle`, `DialogoBaja`, `CampoConError`), con:
- resumen, filtros y tabla paginada con el avance de consumo;
- detalle con las líneas, su saldo y las acciones según el estado (editar, agregar y quitar líneas, registrar consumo, suspender, reactivar, cancelar y eliminar);
- formularios de acuerdo, de línea y de consumo con la validación del backend.

## 2. Fuera de alcance
- La API: se trabaja con `fetch` simulado y el contrato de la spec de backend §4.
- Cambiar Proveedores, Productos, Órdenes de abastecimiento u otras pantallas, salvo mejoras de los componentes compartidos que no cambien lo que comprueban sus tests.
- Vincular el consumo con una orden de abastecimiento: el consumo se registra a mano, como en el backend.
- Librerías nuevas ni gráficos: el avance se muestra con cifras, porcentaje y una barra de progreso accesible (`<progress>` o `role="progressbar"` con valores), sin librería.
- Renovación o vencimiento automáticos: la pantalla solo avisa; el estado lo cambia una persona.

**Decisiones de producto que requieren aprobación antes de ejecutar:**
1. **Avisos de vigencia:** un acuerdo `Vigente` cuya fecha de fin ya pasó muestra «Vencido», y uno cuya fecha de inicio es futura muestra «Aún no inicia». En ambos casos «Registrar consumo» queda deshabilitado con el motivo escrito. Es solo una ayuda: el backend decide con su 409.
2. **Avance de consumo:** en la tabla y en el detalle se muestra «consumido / comprometido» en monto y como porcentaje (redondeado al entero), y «comprometido / máximo» en el detalle.
3. **«Eliminar acuerdo» solo aparece si el monto consumido es 0;** con consumo se ofrece «Cancelar». Igual con las líneas: «Quitar línea» solo en líneas sin consumo.
4. **Las líneas se pueden agregar y quitar en acuerdos `Vigente` y `Suspendido`;** en `Cancelado` todo es de solo lectura.
5. **Los selectores** de proveedor (activos) y de producto se toman de `GET proveedores` y `GET productos` con `tamanoPagina=100`.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/acuerdos-abastecimiento/{tipos,api}.ts` | crear | 12 funciones del contrato: `listarAcuerdos(filtros)`, `obtenerAcuerdo`, `crearAcuerdo`, `actualizarAcuerdo`, `eliminarAcuerdo`, `crearLineaAcuerdo`, `eliminarLineaAcuerdo`, `registrarConsumo`, `suspenderAcuerdo`, `reactivarAcuerdo`, `cancelarAcuerdo`, `obtenerResumenAcuerdos` |
| `frontend/src/modules/acuerdos-abastecimiento/AcuerdosAbastecimientoPage.tsx` + componentes | crear | §4; partida en archivos de ≤ ~300 líneas (resumen, filtros, tabla, detalle, formularios, diálogos y validación por separado) |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `20-acuerdos-abastecimiento` |
| `frontend/src/styles/app.css` | modificar | bloque nuevo al final con prefijo `.acuerdos-` |
| `frontend/src/modules/acuerdos-abastecimiento/*.test.ts(x)` | crear | `fetch` simulado y temporizadores falsos |

## 4. Diseño y lógica
- **Resumen:** total de acuerdos, monto máximo vigente y monto consumido (PEN); una fila con los 3 conteos por estado (Vigente, Suspendido, Cancelado, siempre en ese orden).
- **Filtros:** búsqueda por número o proveedor (espera de 300 ms), estado («Todos») y proveedor («Todos»). Cualquier cambio vuelve a la página 1.
- **Tabla:** número, proveedor, estado como etiqueta con texto (no solo color), vigencia (inicio – fin) con las etiquetas «Vencido» o «Aún no inicia» de la decisión 1, monto máximo y avance de consumo (decisión 2). Paginación de 10 en 10 tal como la entrega la API. Estados de carga, vacío y error.
- **Detalle (lateral en escritorio, debajo en móvil):** cabecera (número, proveedor, descripción o «Sin descripción», estado, vigencia, monto máximo, comprometido, consumido, registro) y tabla de líneas: producto, precio acordado, cantidad comprometida, consumida y saldo, con las cifras alineadas.
- **Acciones según el estado:**
  - `Vigente`: «Editar», «Agregar línea», «Registrar consumo» y «Quitar línea» por línea (según las decisiones 1 y 3), «Suspender», «Cancelar»; «Eliminar» sin consumo.
  - `Suspendido`: «Editar», «Agregar línea», «Quitar línea», «Reactivar», «Cancelar»; «Eliminar» sin consumo; sin «Registrar consumo».
  - `Cancelado`: solo lectura.
- **Formulario de acuerdo:**
  - Alta: número (a mayúsculas al escribir, máximo 20), proveedor (activos), descripción (máximo 500, con contador), fecha de inicio, fecha de fin (posterior al inicio y futura) y monto máximo (mayor a 0, máximo 2 decimales).
  - Edición: número y proveedor de solo lectura; el resto se edita.
  - Validación local en el mismo orden y con los mismos literales de la spec de backend §4 (por ejemplo «El número es obligatorio.», «El monto máximo debe ser mayor a 0.», «La fecha de fin debe ser posterior a la de inicio.»); nada se envía si hay un error local.
  - Los errores 400/404/409 de la API (por ejemplo «Ya existe un acuerdo con el número AM-2026-001.») se muestran con su `Message` sin perder lo escrito.
- **Formulario de línea:** producto (no repetido en el acuerdo: los ya usados se muestran deshabilitados), precio acordado (mayor a 0, máximo 2 decimales) y cantidad comprometida (entero entre 1 y 1 000 000). Debajo se muestra en vivo «Compromiso tras agregar: X de Y» con el monto máximo; si lo supera, el error del backend se muestra tal cual.
- **Registrar consumo:** diálogo con el producto, el saldo disponible y un campo de cantidad entero mayor a 0; si supera el saldo, error local con «La cantidad supera el saldo de la línea ({saldo}).» sin enviar. El 409 de la API se muestra dentro del diálogo.
- **Suspender, reactivar y cancelar:** diálogo accesible de confirmación con el número del acuerdo; el 409 se muestra dentro del diálogo. **Eliminar:** `DialogoBaja` con el número y el proveedor.
- Tras cualquier acción se refrescan lista, resumen y detalle. Escape cancela un diálogo y devuelve el foco al botón que lo abrió.
- **Accesibilidad y responsive:** como en las specs de Clientes y Proveedores (labels, `aria-live`, `<caption>`, ≥ 16 px de margen, sin scroll horizontal de página ni celdas recortadas a 1280 px; a 375 px la tabla de líneas se apila legible).

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 12 funciones con URL, método, query y cuerpo exactos; filtros vacíos y `undefined` fuera de la query | pasan |
| A3 | Resumen, filtros y tabla | tests: conteos en orden fijo, búsqueda con espera, estado y proveedor en la query, vuelta a la página 1, avance de consumo con porcentaje, etiquetas «Vencido» y «Aún no inicia», estados vacío y error | pasan |
| A4 | Detalle y acciones por estado | tests: líneas con saldo; botones exactos en `Vigente`, `Suspendido` y `Cancelado`; «Eliminar» y «Quitar línea» solo sin consumo; «Registrar consumo» deshabilitado con su motivo en acuerdo vencido, no iniciado o suspendido | pasan |
| A5 | Formularios | tests: cada validación local con su literal y sin petición; 400/409 visibles conservando lo escrito; edición con número y proveedor de solo lectura y PUT con su id; producto repetido deshabilitado; vista previa del compromiso | pasan |
| A6 | Consumo y cambios de estado | tests: consumo con saldo, error local al superarlo, POST con la cantidad exacta y 409 dentro del diálogo; suspender, reactivar y cancelar con su 409; eliminar; foco devuelto con Escape; refresco de lista, resumen y detalle | pasan |
| A7 | Sin regresiones | tests de las demás pantallas en verde sin cambiar lo que comprueban | pasan |
| A8 | Visual y E2E | capturas (`page.route`) a 375 y 1280 px, claro y oscuro, con un acuerdo vigente con consumo, el diálogo de consumo y el formulario de línea; márgenes ≥ 16 px; sin scroll horizontal ni celdas recortadas; y una pasada contra la API y PostgreSQL reales con el seed (4 acuerdos AM-2026-001 a 004 y conteo por estado) | adjuntas |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos y `api.ts`.
- [x] Tarea 2: resumen, filtros y tabla con avance.
- [x] Tarea 3: detalle con líneas y acciones por estado.
- [x] Tarea 4: formularios y diálogos (línea, consumo, cambios de estado y eliminar).
- [ ] Tarea 5: tests, capturas y A1–A8.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-25 | A1 | ⚠️ parcial | frontend: `eslint . --max-warnings 0` sin avisos, `tsc -b` limpio, `vite build` correcto y `scripts/check-rules.sh` en 0; `./scripts/verify.sh` completo pendiente de correrlo en macOS antes del PR |
| 2026-11-25 | A2 | ✅ | `api.test.ts`: 4 tests (query exacta, filtros vacíos fuera, rutas de detalle, resumen, proveedores y productos, método y cuerpo de crear, actualizar, líneas, consumo, suspender, reactivar, cancelar y eliminar) |
| 2026-11-25 | A3 | ✅ | `AcuerdosAbastecimientoPage.test.tsx`: resumen y conteo en orden fijo, avance con porcentaje, «Vencido» y «Aún no inicia», estado y proveedor en la query con página 1, búsqueda a los 300 ms, vacío y error |
| 2026-11-25 | A4 | ✅ | `AcuerdosAbastecimientoPage.test.tsx`: líneas con saldo, botones exactos en `Vigente`, `Suspendido` y `Cancelado`, «Eliminar» y «Quitar línea» solo sin consumo, consumo deshabilitado con motivo en vencido, no iniciado, suspendido y línea sin saldo |
| 2026-11-25 | A5 | ✅ | `AcuerdoAcciones.test.tsx`: literales de validación sin petición, creación con número en mayúsculas y fechas ISO, 409 y 400 sin perder lo escrito, edición con campos de solo lectura y PUT, producto repetido deshabilitado, vista previa del compromiso |
| 2026-11-25 | A6 | ✅ | `AcuerdoAcciones.test.tsx`: consumo con saldo y error local al superarlo, POST con cantidad exacta, 409 dentro del diálogo, suspender con Escape y foco devuelto, reactivar, cancelar con 409, quitar línea y eliminar con `DialogoBaja` |
| 2026-11-25 | A7 | ✅ | suite completa `vitest run`: 1366 tests pasan en 139 archivos sin cambiar lo que comprueban |
| — | A8 | pendiente | capturas a 375 y 1280 px (claro/oscuro) y pasada contra la API y PostgreSQL reales con el seed |
