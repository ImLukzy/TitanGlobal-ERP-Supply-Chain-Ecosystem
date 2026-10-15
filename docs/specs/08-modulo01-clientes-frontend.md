# Especificación: 08 - Pantalla de Modulo01_Clientes (frontend)

## 1. Objetivo
**Problema:** `/modulos/01-clientes` muestra «Pantalla pendiente».
**Resultado esperado:** una pantalla de mantenimiento de clientes que consume el contrato de la spec 07:
- resumen arriba;
- tabla con búsqueda y paginación;
- detalle con indicadores;
- alta y edición en un formulario;
- baja con confirmación;
- errores de negocio de la API visibles.

## 2. Fuera de alcance
- La API: se trabaja con `fetch` simulado y el contrato de la spec 07 §4.
- Cambiar la pantalla de OrdenesCompra, salvo reutilizar componentes que se extraigan (ver §3).
- Librerías nuevas (tablas, formularios, modales).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/clientes/tipos.ts` | crear | tipos del contrato de la spec 07 §4 |
| `frontend/src/modules/clientes/api.ts` | crear | `listarClientes(busqueda, pagina, tamanoPagina)`, `obtenerCliente(id)`, `crearCliente(req)`, `actualizarCliente(id, req)`, `eliminarCliente(id)`, `obtenerResumenClientes()` |
| `frontend/src/api/client.ts` | modificar | añadir `apiPut<T>` y `apiDelete` (204 sin cuerpo) con el mismo manejo de errores |
| `frontend/src/modules/clientes/ClientesPage.tsx` + componentes en la carpeta | crear | §4 |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `01-clientes` |
| `frontend/src/components/*` | crear (opcional) | si un componente de OrdenesCompra se reutiliza (tarjeta de resultado, campo con error), se extrae aquí y ambas pantallas lo usan, sin cambiar su comportamiento ni sus tests |
| `frontend/src/modules/clientes/*.test.tsx` | crear | tests con `fetch` simulado |
| `frontend/src/styles/*.css` | modificar | con las variables existentes |

## 4. Diseño y lógica
- **Resumen:** 3 cifras (total de clientes, con órdenes, sin descripción) y el top 5 por monto (nombre, órdenes, monto en PEN).
- **Tabla:**
  - Columnas: nombre, RUC y nº de órdenes.
  - Búsqueda con espera de 300 ms tras dejar de escribir; al cambiar la búsqueda, vuelve a la página 1.
  - Paginación «Anterior/Siguiente» con «Página X de Y» y `tamanoPagina` 10.
  - Fila seleccionable con el teclado (botón o enlace dentro de la fila).
  - Estados: cargando, vacío («No hay clientes que coincidan») y error.
- **Detalle (panel lateral en escritorio, debajo en móvil):** nombre, RUC, descripción, total de órdenes, monto total (PEN) y fecha de la última orden (UTC, con el helper `lib/formato.ts`, o «Sin órdenes»). Botones «Editar» y «Eliminar».
- **Formulario (alta y edición):**
  - Validación en cliente igual que la API: nombre obligatorio y de 200 caracteres como máximo; RUC de 11 dígitos.
  - Los errores 400 y 409 de la API se muestran con su `Message` sin perder lo escrito.
  - Al guardar bien, se refrescan la tabla, el detalle y el resumen.
- **Baja:**
  - Diálogo de confirmación accesible: `<dialog>` nativo o `role="dialog"` con `aria-modal`, el foco atrapado y Escape para cancelar.
  - El texto incluye el nombre del cliente.
  - Un 409 («tiene órdenes de compra») se muestra en el diálogo.
  - Si sale bien: se cierra el detalle y se refrescan la tabla y el resumen.
- **Accesibilidad:** labels, `aria-describedby` en los errores, `aria-live` en los estados y tabla con `<caption>`.
- **Responsive:** sin scroll horizontal de página a 375 px.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 6 funciones con su URL, método, query y cuerpo exactos; `apiDelete` con 204 sin cuerpo | pasan |
| A3 | Tabla | tests: búsqueda con espera (una sola petición tras teclear rápido; temporizadores falsos), vuelta a la página 1, paginación con `total`, estados vacío y error | pasan |
| A4 | Formulario | tests: validación local sin petición; alta OK refresca; 409 «Ya existe un cliente con el RUC …» visible y conserva lo escrito; edición precargada y PUT con su id | pasan |
| A5 | Baja | tests: confirmar → DELETE y refresco; cancelar con Escape → sin petición y foco devuelto; 409 visible dentro del diálogo | pasan |
| A6 | Registro | `/modulos/01-clientes` muestra la pantalla; `/modulos/04-ordenes-compra` sigue igual (sus tests en verde) | pasa |
| A7 | Visual | capturas con Playwright (`page.route`) a 375 y 1280 px, en claro y oscuro: listado con detalle abierto y diálogo de baja; esperar a que terminen las transiciones | adjuntas |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos, `api.ts`, `apiPut`/`apiDelete`.
- [x] Tarea 2: resumen y tabla.
- [x] Tarea 3: detalle, formulario y baja.
- [x] Tarea 4: tests, capturas y A1–A7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-14 | A1 | ✅ | `./scripts/verify.sh` rc=0 en `ImLukzy/Claudio` (`f91a900`, idéntico al auditado `31c8ea4` según `git patch-id`): backend 56/56, frontend 53/53 |
| 2026-10-14 | A2–A6 | ✅ | tests de `frontend/src/modules/clientes/`: 6 funciones de la API con URL, método y cuerpo exactos; búsqueda con temporizadores falsos; validación local; 409 visible conservando lo escrito; baja con Escape y foco devuelto; tests de OrdenesCompra intactos |
| 2026-10-14 | A7 | ✅ | capturas de Dwight (corrección 1, `aa8d3d1`) revisadas por Claudio: márgenes 16/16 px a 375 y detalle lateral a 1280, sin scroll horizontal. Los archivos se perdieron al vaciarse `/tmp`; el QA E2E de Pam con la API real las repetirá |
| 2026-10-14 | Auditoría | ✅ | Kelly, ciclo 1: APROBADO; 2 observaciones menores sin cambio requerido (`maxLength` 220 intencional para mostrar el mensaje de 200; `client.ts` solo lee `Message` en PascalCase, que es el contrato) |
