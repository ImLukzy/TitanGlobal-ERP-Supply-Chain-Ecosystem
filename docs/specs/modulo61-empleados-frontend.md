# Especificación: 48 - Pantalla de Modulo61_Empleados (frontend)

## 1. Objetivo
**Problema:** `/modulos/61-empleados` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla de empleados contra el contrato de la spec 47 §4: listado con filtros, detalle, alta, edición y cese. Usa el mismo diseño común que Usuarios (spec 44) y Contratos Comerciales. Los textos de validación son los literales de la spec 47 §4.

## 2. Fuera de alcance
- La API: contrato y mensajes de la spec 47 §4. No cambian.
- Contratos laborales (módulo 62), documentos, fotografía y exportaciones.
- Reactivar un empleado cesado y editar su legajo.
- Un endpoint de resumen: el contrato de la spec 47 no lo tiene y esta spec no lo añade.
- `DialogoBaja` (`components/DialogoBaja.tsx`): es el diálogo de borrado y el cese no borra; se crea un diálogo propio.
- Librerías nuevas.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna propia. Aplican las de la spec 47 §2. La decisión 5 (regla de fecha de cese) bloquea solo la validación de cese.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/empleados/tipos.ts` | crear | tipos del contrato de la spec 47 §4 |
| `frontend/src/modules/empleados/api.ts` | crear | 5 funciones (§4 «api.ts») |
| `frontend/src/modules/empleados/presentacion.ts` | crear | textos de estado y mensaje de error por defecto |
| `frontend/src/modules/empleados/validacionEmpleado.ts` | crear | validación local con los mensajes 1 a 17 y 20 a 22, 24 de la spec 47 §4 |
| `frontend/src/modules/empleados/useEmpleadosData.ts` | crear | carga de listado, conteos y detalle |
| `frontend/src/modules/empleados/EmpleadosPage.tsx` | crear | página, estado de filtros y de selección |
| `frontend/src/modules/empleados/EmpleadosResumen.tsx` | crear | resumen (§4) |
| `frontend/src/modules/empleados/EmpleadosFiltros.tsx` | crear | búsqueda y estado |
| `frontend/src/modules/empleados/EmpleadosTabla.tsx` | crear | tabla paginada con `TablaPaginada` |
| `frontend/src/modules/empleados/EmpleadoDetallePanel.tsx` | crear | panel lateral con `PanelDetalle` |
| `frontend/src/modules/empleados/EmpleadoFormulario.tsx` | crear | alta y edición |
| `frontend/src/modules/empleados/DialogoCese.tsx` | crear | diálogo de cese |
| `frontend/src/modules/empleados/api.test.ts`, `EmpleadosPage.test.tsx`, `EmpleadoFormulario.test.tsx`, `empleados.testUtils.tsx` | crear | `fetch` simulado y temporizadores falsos |
| `frontend/src/modules/pantallas.ts` | modificar | añadir el `import` y la entrada `'61-empleados': EmpleadosPage,` al final del objeto, después de `'71-usuarios'` |
| `frontend/src/styles/app.css` | modificar | bloque `.empleados-work-area` (y clases propias) al final del archivo |

Verificado en el repositorio: `pantallas.ts` y `app.css` existen; los componentes compartidos `components/PaginaModulo.tsx`, `TablaPaginada.tsx`, `PanelDetalle.tsx` y `CampoConError.tsx` existen; `api/client.ts` exporta `apiGet`, `apiPost` y `apiPut`; `lib/formato.ts` exporta `formatearFecha`. Los archivos «crear» no existen todavía.

## 4. Diseño y lógica
- **api.ts y tipos.** Cinco funciones con el contrato de la spec 47 §4, con prefijo `/empleados` sobre `API_BASE = '/api/v1'`:
  - `listarEmpleados(filtros)` → `GET /empleados?...`;
  - `obtenerEmpleado(id)` → `GET /empleados/{id}`;
  - `crearEmpleado(request)` → `POST /empleados`;
  - `actualizarEmpleado(id, request)` → `PUT /empleados/{id}`;
  - `registrarCeseEmpleado(id, request)` → `POST /empleados/{id}/cese`.
  - `tipos.ts`: `TipoDocumento = 'DNI' | 'CE' | 'Pasaporte'`; `EstadoEmpleado = 'Activo' | 'Cesado'`; `FiltrosEmpleados { busqueda, estado: '' | EstadoEmpleado, pagina, tamanoPagina }`; `EmpleadoListado`, `EmpleadoDetalle`, `PaginaEmpleados`, `GuardarEmpleadoRequest`, `CeseEmpleadoRequest`, `IdResponse`. Los campos en camelCase, igual que la API.
- **Resumen:** muestra `Activos`, `Cesados` y `Total` (= activos + cesados). Se calcula con dos peticiones de conteo: `listarEmpleados({ estado: 'Activo', pagina: 1, tamanoPagina: 1 })` y la misma con `Cesado`; se toma `total` de cada una **[elegido]**. Si una falla, se muestra el error en el resumen y el resto de la pantalla sigue.
- **Filtros:** búsqueda (espera 300 ms, como Usuarios; cualquier cambio de búsqueda vuelve a página 1) y estado (`Todos`, `Activo`, `Cesado`). Con `Todos`, el parámetro `estado` se omite de la query. Tamaño de página fijo en 10.
- **Tabla:** columnas Legajo, Apellidos y nombres, Documento (tipo y número), Cargo, Área, Ingreso y Estado. El estado es una pastilla con el texto «Activo» o «Cesado», nunca solo color. Paginación con `TablaPaginada`, 10 por página. Estados de carga, vacío («No hay empleados con esos filtros.») y error **[elegido: textos]**.
- **Detalle (panel lateral, `PanelDetalle`):** legajo y nombre completo; datos personales (documento, fecha de nacimiento); datos laborales (cargo, área, fecha de ingreso); contacto (email y teléfono; si no hay dato, «Sin dato»); estado. Si está `Cesado`, muestra fecha de cese y motivo. Botones «Editar» y «Registrar cese». En `Cesado` ambos quedan deshabilitados y un texto visible «Empleado cesado: no admite cambios.» se asocia con `aria-describedby` **[elegido]**.
- **Formulario (alta y edición):** tipo de documento (select: `DNI`, `CE`, `Pasaporte`), número de documento, nombres, apellidos, fecha de nacimiento y fecha de ingreso (`type="date"`), cargo, área, email y teléfono. No tiene campo de estado ni de legajo; el legajo aparece en el detalle tras el alta.
  - Validación local con los textos exactos de los mensajes 1 a 17 de la spec 47 §4. Si falla, no hay petición.
  - Un 409 (documento repetido) o un 400 del servidor se muestran junto al formulario y conservan lo escrito.
  - Tras guardar: se selecciona el id devuelto y se recargan listado y detalle.
- **Cese (`DialogoCese`):** fecha de cese (`type="date"`, obligatoria) y motivo (textarea, máximo 300, con contador «n/300»).
  - Validación local con los mensajes 20, 21, 22 y 24 de la spec 47 §4; la comparación con la fecha de ingreso usa la del detalle abierto.
  - El 409 de «ya está cesado» se muestra dentro del diálogo, que no se cierra.
  - Al confirmar: se recargan listado y detalle y el diálogo se cierra.
  - Diálogo modal: el foco va al primer campo al abrir; Escape lo cierra y devuelve el foco al botón «Registrar cese»; los botones se deshabilitan mientras se envía.
- **Fechas:** se muestran con `formatearFecha` (UTC) y se envían como `yyyy-MM-dd`. No se convierten a `Date` local, para evitar que un día cambie por la zona horaria.
- **Carga y respuestas obsoletas:** los datos del detalle se guardan con el id al que pertenecen, de modo que un error o una respuesta de un empleado anterior no se muestre tras cambiar de selección (mismo criterio que `useUsuariosData`).
- **Accesibilidad y responsive:** cada campo tiene `label` asociado; los errores van con `role="alert"` y `aria-describedby`; la tabla tiene `caption`; el estado tiene texto; el foco es visible y todo se puede usar con teclado. Responsive a 375 y 1280 px, con el mismo criterio que Usuarios.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `npm --prefix frontend run verify` | en verde, 0 warnings |
| A2 | API | tests: las 5 funciones con URL, método, query y cuerpo exactos; `estado` omitido en `Todos` | pasan |
| A3 | Listado, resumen y detalle | tests: filtros en la query; página 1 al filtrar; búsqueda tras 300 ms con una sola consulta del último texto; al cargar, 1 listado y 2 conteos; pastillas con texto; fecha y motivo de cese solo si `Cesado` | pasan |
| A4 | Formulario y cese | tests: validaciones locales sin petición con los textos de la spec 47 §4; fechas en `yyyy-MM-dd`; 409 de documento conservando lo escrito; edición deshabilitada en `Cesado`; cese: 409 «ya está cesado» dentro del diálogo, 400 de fecha, Escape y foco | pasan |
| A5 | Sin regresiones | las demás pantallas y la cabecera sin cambios; en `App.test.tsx`, la prueba de «Pantalla pendiente» elige el primer módulo sin pantalla y sigue en verde; la suite pasa 10 veces seguidas | pasan |
| A6 | Visual | capturas a 375 y 1280 px, claro y oscuro: listado con detalle en `Activo` y en `Cesado`, formulario y diálogo de cese; comparación con Usuarios adjunta; márgenes de al menos 16 px, sin scroll horizontal ni palabras partidas | adjuntas (pendiente) |

## 6. Checklist de ejecución
- [ ] Spec 47 aprobada y la spec 48 revisada por Lukas.
- [x] Tarea 1: tipos y `api.ts`.
- [x] Tarea 2: resumen, filtros, tabla y detalle.
- [x] Tarea 3: formulario y diálogo de cese.
- [ ] Tarea 4: tests, capturas y A1 a A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-02 | A1 | pasa | `npm run verify` en `frontend/`: lint sin warnings, typecheck, 429 tests en 58 archivos y build, exit 0 |
| 2026-11-02 | A2 | pasa | `api.test.ts`: las 5 funciones con URL, método, query y cuerpo exactos; `estado` omitido en `Todos` |
| 2026-11-02 | A3 | pasa | `EmpleadosPage.test.tsx`: búsqueda a los 300 ms con una sola consulta, página 1 al filtrar desde la página 2, 1 listado y 2 conteos, pastillas con texto, cese solo si `Cesado` |
| 2026-11-02 | A4 | pasa, con una desviación | `EmpleadoFormulario.test.tsx` y `EmpleadosPage.test.tsx`. Desviación de §4: los errores de campo salen de `components/CampoConError.tsx` (compartido), que no tiene `role="alert"`; no se cambió |
| 2026-11-02 | A5 | pasa | 10 ejecuciones seguidas de `vitest run` con 429/429. Antes fallaba de forma intermitente la prueba «A4 confirma la baja con DELETE…» de `usuarios/UsuarioFormulario.test.tsx` (comprobaba la recarga sin esperar al efecto); corregida en a51f200 |
| 2026-11-02 | A6 | parcial | 16 capturas sin versionar (se adjuntan al PR). Márgenes: 34 px en panel y 18 px en diálogo a 375; sin scroll horizontal de página a 375 ni 1280. Pendiente: (1) el email se parte en el panel (`ana.torres@titanglobal.exa` / `mple`) a 375 y 1280, lo que incumple «sin palabras partidas»; (2) repetir 3 capturas de 1280 oscuro (diálogo, `Cesado` y formulario), que salieron recortadas; (3) adjuntar la comparación visual con Usuarios: solo se compararon rejilla y medidas |
| 2026-11-02 | QA en local | pasa | PostgreSQL local con el seed: 409 de documento repetido conservando lo escrito; alta de `EMP-000005` (resumen 4/1/5); cese (resumen 3/2/5), con edición bloqueada después. Seed restaurado al terminar |
| 2026-11-03 | A1 | pasa | Sobre a746ace: `npm run verify` con lint sin warnings, typecheck, 470 tests en 61 archivos y build. Resto del gate: build con warnings como error y `dotnet format` en una copia con LF, `check-rules.sh` con alias temporal de `python3`, 908 tests de backend y sin cambios de modelo pendientes |
| 2026-11-03 | A5 | parcial | 5 ejecuciones de `vitest run` con 470/470 sobre a746ace (tandas de 3 y 2). El criterio pide 10; Alexander lo limitó a 5. Antes de a746ace falló 1 de 10 en «A4 vuelve a la última página válida…» de `usuarios/UsuarioFormulario.test.tsx` (comprobaba la petición sin esperar al efecto) |
| 2026-11-03 | A6 | pasa | Email en fila completa (3d39104): `ana.torres@…`, `luis.rojas@…` y `maria.salazar@…` miden 1 línea a 375 y 1280, sin scroll horizontal de página. Comparación con Usuarios hecha: misma rejilla, panel y diálogo. 16 capturas sin versionar (se adjuntan al PR): las 12 de detalle y diálogo son posteriores a la corrección; las 4 de formulario son anteriores y no muestran el email |

**Observaciones para la revisión (no incumplen un criterio):**
- A 1280 px la tabla mide 720 px en un contenedor de 458 px: Área, Ingreso y Estado solo se ven con el scroll interno de la tabla. Usuarios tiene el mismo patrón con menos ancho (560 px en 458 px).
- «Nuevo empleado» abre el formulario debajo de la tabla, fuera de la vista a 1280 x 800, sin mover el scroll ni el foco. Usuarios se comporta igual.
