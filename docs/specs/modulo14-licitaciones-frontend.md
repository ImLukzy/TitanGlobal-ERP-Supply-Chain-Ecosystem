# Especificación: modulo14-licitaciones-frontend — Pantalla de licitaciones

## 1. Objetivo
**Problema:** `/modulos/14-licitaciones` muestra «Pantalla pendiente»; la API de licitaciones (spec `modulo14-licitaciones-backend`) no tiene interfaz.
**Resultado esperado:** una pantalla para gestionar licitaciones con el mismo patrón y los mismos componentes compartidos que Proveedores y Órdenes de abastecimiento (`PaginaModulo`, `TablaPaginada`, `PanelDetalle`, `DialogoBaja`, `CampoConError`), con:
- resumen, filtros y tabla paginada;
- detalle con las ofertas comparadas y sus acciones según el estado (editar, ofertar, mejorar oferta, cerrar, adjudicar, cancelar y eliminar);
- formularios de licitación y de oferta con la validación del backend.

## 2. Fuera de alcance
- La API: se trabaja con `fetch` simulado y el contrato de la spec de backend §4.
- Cambiar Proveedores, Órdenes de abastecimiento u otras pantallas, salvo mejoras de los componentes compartidos que no cambien lo que comprueban sus tests.
- Generar una orden de abastecimiento desde la adjudicación (el backend no lo ofrece).
- Librerías nuevas ni gráficos: el resumen usa cifras y tablas.
- Cierre automático por fecha: la pantalla solo avisa cuando el plazo venció; el estado lo cambia una persona.

**Decisiones de producto que requieren aprobación antes de ejecutar:**
1. **Aviso de plazo vencido:** una licitación `Abierta` cuya fecha de cierre ya pasó muestra la etiqueta «Plazo vencido» y no ofrece «Presentar oferta» ni «Mejorar oferta»; sigue ofreciendo «Cerrar» y «Cancelar». Es solo una ayuda: el backend decide con su 409.
2. **Adjudicar muestra todas las ofertas** con la más baja preseleccionada y un texto que recuerda que se puede elegir otra (plazo de entrega, evaluación del proveedor).
3. **«Eliminar» solo aparece en una licitación sin ofertas;** con ofertas se ofrece «Cancelar».
4. **El selector de proveedor de la oferta** lista solo proveedores activos, tomados de `GET proveedores` con `tamanoPagina=100`.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/licitaciones/{tipos,api}.ts` | crear | 11 funciones del contrato: `listarLicitaciones(filtros)`, `obtenerLicitacion`, `crearLicitacion`, `actualizarLicitacion`, `eliminarLicitacion`, `crearOferta`, `mejorarOferta`, `cerrarLicitacion`, `adjudicarLicitacion`, `cancelarLicitacion`, `obtenerResumenLicitaciones` |
| `frontend/src/modules/licitaciones/LicitacionesPage.tsx` + componentes | crear | §4; partida en archivos de ≤ ~300 líneas (resumen, filtros, tabla, detalle, formularios, diálogos y validación por separado) |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `14-licitaciones` |
| `frontend/src/styles/app.css` | modificar | bloque nuevo al final con prefijo `.licitaciones-` |
| `frontend/src/modules/licitaciones/*.test.ts(x)` | crear | `fetch` simulado y temporizadores falsos |

## 4. Diseño y lógica
- **Resumen:** total de licitaciones, total de ofertas, presupuesto abierto y monto adjudicado (PEN); una fila con los 4 conteos por estado (Abierta, Cerrada, Adjudicada, Cancelada, siempre en ese orden).
- **Filtros:** búsqueda por número o título (espera de 300 ms), estado («Todos») y tipo («Todos»). Cualquier cambio vuelve a la página 1.
- **Tabla:** número, título, tipo y estado como etiquetas con texto (no solo color), presupuesto, fecha de cierre y número de ofertas; etiqueta «Plazo vencido» según la decisión 1. Paginación de 10 en 10 tal como la entrega la API. Estados de carga, vacío y error.
- **Detalle (lateral en escritorio, debajo en móvil):** cabecera (número, título, descripción o «Sin descripción», tipo, estado, presupuesto, cierre, registro), y si está adjudicada, proveedor y monto adjudicados. Tabla de ofertas ordenadas como las entrega la API (monto ascendente): proveedor, monto, plazo en días, observaciones o «Sin observaciones», y la etiqueta «Ganadora» en texto.
- **Acciones según el estado:**
  - `Abierta`: «Editar», «Presentar oferta», «Mejorar oferta» en cada oferta (solo si es subasta inversa), «Cerrar» y «Cancelar»; «Eliminar» si no tiene ofertas.
  - `Cerrada`: «Adjudicar» y «Cancelar».
  - `Adjudicada` y `Cancelada`: solo lectura.
- **Formulario de licitación:**
  - Alta: número (se convierte a mayúsculas al escribir, máximo 20), título (máximo 150), descripción (máximo 500, con contador), tipo (Concurso de precios o Subasta inversa), presupuesto referencial (mayor a 0, máximo 2 decimales) y fecha de cierre (`datetime-local`, futura).
  - Edición: número y tipo de solo lectura; el resto se edita.
  - Validación local en el mismo orden y con los mismos literales de la spec de backend §4 (por ejemplo «El número es obligatorio.», «El presupuesto referencial debe ser mayor a 0.», «La fecha de cierre debe ser futura.»); nada se envía si hay un error local.
  - Los errores 400/404/409 de la API (por ejemplo «Ya existe una licitación con el número LIC-2026-001.») se muestran con su `Message` sin perder lo escrito.
- **Formulario de oferta:** proveedor (activos), monto (mayor a 0 y no mayor al presupuesto, con el literal «El monto no puede superar el presupuesto referencial.»), plazo entero entre 1 y 365 y observaciones (máximo 500). **Mejorar oferta:** un solo campo de monto nuevo, menor al actual («La nueva oferta debe ser menor a la actual.»).
- **Cerrar y cancelar:** diálogo accesible de confirmación con el número de la licitación; el 409 de la API se muestra dentro del diálogo. **Adjudicar:** diálogo con las ofertas como opciones de radio (decisión 2) y botón «Adjudicar». **Eliminar:** `DialogoBaja` con el número y el título.
- Tras cualquier acción se refrescan lista, resumen y detalle. Escape cancela un diálogo y devuelve el foco al botón que lo abrió.
- **Accesibilidad y responsive:** como en las specs de Clientes y Proveedores (labels, `aria-live`, `<caption>`, ≥ 16 px de margen, sin scroll horizontal de página ni celdas recortadas a 1280 px; a 375 px la tabla de ofertas se apila legible).

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 11 funciones con URL, método, query y cuerpo exactos; filtros vacíos y `undefined` fuera de la query | pasan |
| A3 | Resumen, filtros y tabla | tests: conteos en orden fijo, búsqueda con espera, estado y tipo en la query, vuelta a la página 1, etiqueta «Plazo vencido», estados vacío y error | pasan |
| A4 | Detalle y acciones por estado | tests: ofertas ordenadas con «Ganadora»; botones exactos en cada uno de los 4 estados; «Mejorar oferta» solo en subasta inversa; «Eliminar» solo sin ofertas; plazo vencido sin «Presentar oferta» | pasan |
| A5 | Formularios | tests: cada validación local con su literal y sin petición; 400/409 visibles conservando lo escrito; edición con número y tipo de solo lectura y PUT con su id; oferta con límites de monto y plazo; mejora a la baja | pasan |
| A6 | Cierre, adjudicación y baja | tests: cerrar y cancelar con 409 visible dentro del diálogo; adjudicar con la más baja preseleccionada, POST con `ofertaId` exacto y cambio de opción; eliminar; foco devuelto con Escape; refresco de lista, resumen y detalle | pasan |
| A7 | Sin regresiones | tests de las demás pantallas en verde sin cambiar lo que comprueban | pasan |
| A8 | Visual y E2E | capturas (`page.route`) a 375 y 1280 px, claro y oscuro, con una licitación cerrada con ofertas, el diálogo de adjudicación y el formulario de oferta; márgenes ≥ 16 px; sin scroll horizontal ni celdas recortadas; y una pasada contra la API y PostgreSQL reales con el seed (resumen 5 / 7 / 70000,00 / 28000,00 y conteo por estado 2 / 1 / 1 / 1) | adjuntas |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos y `api.ts`.
- [x] Tarea 2: resumen, filtros y tabla.
- [x] Tarea 3: detalle con ofertas y acciones por estado.
- [x] Tarea 4: formularios y diálogos (cerrar, cancelar, adjudicar y eliminar).
- [ ] Tarea 5: tests, capturas y A1–A8.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-25 | A1 | ⚠️ parcial | frontend: `eslint . --max-warnings 0` sin avisos, `tsc -b` limpio, `vite build` correcto y `scripts/check-rules.sh` en 0; `./scripts/verify.sh` completo pendiente de correrlo en macOS antes del PR |
| 2026-11-25 | A2 | ✅ | `api.test.ts`: 4 tests (query exacta, filtros vacíos fuera, rutas de detalle, resumen y proveedores, método y cuerpo de crear, actualizar, ofertas, mejora, cerrar, adjudicar, cancelar y eliminar) |
| 2026-11-25 | A3 | ✅ | `LicitacionesPage.test.tsx`: resumen y conteo por estado en orden fijo, «Plazo vencido» solo en la abierta vencida, estado y tipo en la query con página 1, búsqueda a los 300 ms, vacío y error |
| 2026-11-25 | A4 | ✅ | `LicitacionesPage.test.tsx`: botones exactos por estado, «Mejorar oferta» por oferta solo en subasta, «Eliminar» solo sin ofertas, «Ganadora», «Sin observaciones», plazo vencido sin «Presentar oferta» |
| 2026-11-25 | A5 | ✅ | `LicitacionAcciones.test.tsx`: literales de validación sin petición, creación con número en mayúsculas y fecha ISO, 409 sin perder lo escrito, edición con campos de solo lectura, oferta con límite de monto |
| 2026-11-25 | A6 | ✅ | `LicitacionAcciones.test.tsx`: mejora a la baja, cerrar con Escape y foco devuelto, adjudicar con la más baja preseleccionada y `ofertaId` exacto, 409 dentro del diálogo, eliminar con `DialogoBaja` |
| 2026-11-25 | A7 | ✅ | suite completa `vitest run`: 1362 tests pasan en 139 archivos sin cambiar lo que comprueban |
| — | A8 | pendiente | capturas a 375 y 1280 px (claro/oscuro) y pasada contra la API y PostgreSQL reales con el seed |
