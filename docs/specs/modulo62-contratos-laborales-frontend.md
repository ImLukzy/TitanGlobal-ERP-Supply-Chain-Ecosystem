# Especificación: modulo62-contratos-laborales-frontend — Pantalla de ContratosLaborales

> Estado: **borrador**, pendiente de revisión por Lukas. Las decisiones de §2 necesitan su respuesta antes de programar.

## 1. Objetivo
**Problema:** el backend del módulo 62 está en `main` (PR #39, spec `modulo62-contratos-laborales-backend.md`), pero `/modulos/62-contratos-laborales` muestra «Pantalla pendiente»: `frontend/src/modules/pantallas.ts` no tiene la entrada `62-contratos-laborales`.
**Resultado esperado:** pantalla de contratos laborales contra el contrato HTTP de la spec backend §4: listado con búsqueda y filtros, detalle con historial de adendas y cadena de renovación, alta, renovación, adenda y anulación. Usa el mismo diseño común que Empleados (`modulo61-empleados-frontend.md`) y Contratos Comerciales. Los textos de validación son los literales de la spec backend §4 («Mensajes literales»).

## 2. Fuera de alcance
- La API: contrato, estados y mensajes de la spec backend §4. No cambian.
- La pantalla de Empleados (61): no se añade un enlace «Ver contratos» ni una pestaña de contratos en su detalle.
- Edición libre y borrado de contratos: el backend no tiene `PUT` ni `DELETE` (T3). No se usa `DialogoBaja`.
- Calcular el estado `Vencido` en el navegador: la pantalla muestra el `estado` efectivo que devuelve la API.
- Un endpoint de resumen: el contrato no lo tiene y esta spec no lo añade.
- Planillas (66), documentos adjuntos, firma digital y exportaciones.
- Librerías nuevas y cambios en componentes compartidos (`PaginaModulo`, `TablaPaginada`, `PanelDetalle`, `CampoConError`) o en reglas compartidas de `app.css`.

**Decisiones de producto que requieren aprobación antes de ejecutar:**

1. **Acciones de escritura según el rol.** La lectura es para cualquier usuario autenticado; alta, renovación, adenda y anulación exigen `RRHH` o `ADMIN` (decisión 6 A del backend). La sesión del frontend ya trae `roles: string[]` (`api/sesionStore.ts`, `useSesion()` de `layout/SesionContext.tsx`).
   - **A (recomendada):** sin `RRHH` ni `ADMIN` en `sesion.roles`, los botones de escritura se muestran deshabilitados con el texto visible «Solo RRHH o ADMIN pueden modificar contratos.» asociado con `aria-describedby`. Si aun así el servidor responde 403, se muestra su mensaje literal.
   - B: ocultar los botones. La pantalla queda más limpia, pero el usuario no sabe por qué no puede actuar.
   - C: mostrarlos siempre y dejar que el servidor responda 403.
2. **Selector de empleado en el alta.** No hay un endpoint de opciones de empleados; `GET /empleados` admite `tamanoPagina` hasta 100.
   - **A (recomendada):** un `select` cargado con `listarEmpleados({ estado: 'Activo', pagina: 1, tamanoPagina: 100 })` reutilizando `modules/empleados/api.ts`, con el texto «Legajo · Apellidos, Nombres». Si `total > 100`, se muestra «Se muestran los primeros 100 empleados activos.». Los cesados no se ofrecen (mensaje 14 del backend).
   - B: un campo de búsqueda con sugerencias (consulta con `busqueda` a los 300 ms). Escala mejor, pero es un componente nuevo sin referencia en el proyecto.
3. **Resumen.** Sin endpoint de resumen.
   - **A (recomendada):** tres tarjetas, **Vigentes**, **Vencidos** y **Total**, con tres peticiones de conteo (`tamanoPagina: 1`, se toma `total`): `estado=Vigente`, `estado=Vencido` y sin estado. Mismo criterio que Empleados.
   - B: cinco tarjetas, una por estado. Cinco peticiones para datos que casi no cambian.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/contratos-laborales/tipos.ts` | crear | tipos del contrato de la spec backend §4 |
| `frontend/src/modules/contratos-laborales/api.ts` | crear | 6 funciones (§4 «api.ts») |
| `frontend/src/modules/contratos-laborales/presentacion.ts` | crear | textos de estado y de modalidad, mensaje de error por defecto |
| `frontend/src/modules/contratos-laborales/validacionContratoLaboral.ts` | crear | validación local con los mensajes 1 a 12, 20, 21, 26 y 27 de la spec backend |
| `frontend/src/modules/contratos-laborales/useContratosLaboralesData.ts` | crear | carga de listado, conteos, empleados activos y detalle |
| `frontend/src/modules/contratos-laborales/ContratosLaboralesPage.tsx` | crear | página, estado de filtros, selección y diálogos |
| `frontend/src/modules/contratos-laborales/ContratosLaboralesResumen.tsx` | crear | resumen (decisión 3) |
| `frontend/src/modules/contratos-laborales/ContratosLaboralesFiltros.tsx` | crear | búsqueda, estado y modalidad |
| `frontend/src/modules/contratos-laborales/ContratosLaboralesTabla.tsx` | crear | tabla con `TablaPaginada` |
| `frontend/src/modules/contratos-laborales/ContratoLaboralEstadoPastilla.tsx` | crear | pastilla con texto por estado |
| `frontend/src/modules/contratos-laborales/ContratoLaboralDetallePanel.tsx` | crear | panel lateral con `PanelDetalle` |
| `frontend/src/modules/contratos-laborales/ContratoLaboralFormulario.tsx` | crear | alta |
| `frontend/src/modules/contratos-laborales/DialogoRenovacion.tsx` | crear | renovación |
| `frontend/src/modules/contratos-laborales/DialogoAdenda.tsx` | crear | adenda |
| `frontend/src/modules/contratos-laborales/DialogoAnulacion.tsx` | crear | anulación |
| `frontend/src/modules/contratos-laborales/api.test.ts`, `ContratosLaboralesPage.test.tsx`, `ContratoLaboralFormulario.test.tsx`, `DialogosContrato.test.tsx`, `contratosLaborales.testUtils.tsx` | crear | `fetch` simulado y temporizadores falsos |
| `frontend/src/modules/pantallas.ts` | modificar | añadir el `import` y la entrada `'62-contratos-laborales': ContratosLaboralesPage,` al final del objeto, después de `'33-centros-trabajo'` |
| `frontend/src/styles/app.css` | modificar | bloque `.contratos-laborales-…` al final del archivo, sin tocar selectores existentes |

Verificado en el repositorio (rama desde `origin/main` `e48a190`): existen `pantallas.ts`, `app.css`, `components/PaginaModulo.tsx`, `TablaPaginada.tsx`, `PanelDetalle.tsx` y `CampoConError.tsx`; `api/client.ts` exporta `apiGet` y `apiPost`; `lib/formato.ts` exporta `formateadorMoneda` y `formatearFecha`; `modules/empleados/api.ts` exporta `listarEmpleados`; `modules/catalogo.ts` ya nombra el módulo («Contratos laborales»). La carpeta `modules/contratos-laborales/` no existe todavía.

## 4. Diseño y lógica
- **api.ts y tipos.** Seis funciones, prefijo `/contratos-laborales` sobre `API_BASE = '/api/v1'`:
  - `listarContratosLaborales(filtros)` → `GET /contratos-laborales?...` (omite `busqueda`, `estado`, `modalidad` y `empleadoId` vacíos);
  - `obtenerContratoLaboral(id)` → `GET /contratos-laborales/{id}`;
  - `crearContratoLaboral(request)` → `POST /contratos-laborales`;
  - `renovarContratoLaboral(id, request)` → `POST /contratos-laborales/{id}/renovacion`;
  - `agregarAdendaContrato(id, request)` → `POST /contratos-laborales/{id}/adendas`;
  - `anularContratoLaboral(id, request)` → `POST /contratos-laborales/{id}/anulacion`.
  - `tipos.ts`: `ModalidadContrato = 'Indeterminado' | 'PlazoFijo' | 'Formativo'`; `EstadoContrato = 'Vigente' | 'Renovado' | 'Vencido' | 'Cerrado' | 'Anulado'`; `FiltrosContratosLaborales { busqueda, estado: '' | EstadoContrato, modalidad: '' | ModalidadContrato, empleadoId: number | null, pagina, tamanoPagina }`; `ContratoLaboralListado`, `ContratoLaboralDetalle`, `AdendaContrato`, `PaginaContratosLaborales`, `GuardarContratoLaboralRequest`, `RenovarContratoLaboralRequest`, `AdendaContratoRequest`, `AnularContratoLaboralRequest`, `ContratoLaboralCreado { id, numero }` e `IdResponse`. Campos en camelCase, igual que la API.
- **Resumen:** según la decisión 3. Si una petición falla, el error se muestra en el resumen y el resto de la pantalla sigue.
- **Filtros:** búsqueda (espera 300 ms; número, cargo, nombre, apellido o legajo), estado (`Todos` y los cinco estados) y modalidad (`Todas` y las tres). Cualquier cambio vuelve a la página 1. Tamaño de página fijo en 10. Con `Todos`/`Todas` el parámetro se omite.
- **Tabla:** columnas Número, Empleado (legajo y nombre), Modalidad, Inicio, Fin («Sin fecha de fin» si es nula), Sueldo básico (`formateadorMoneda`, PEN) y Estado. El estado es una pastilla con texto, nunca solo color. Estados de carga, vacío («No hay contratos con esos filtros.») y error.
- **Detalle (`PanelDetalle`):** número y estado; empleado (legajo y nombre); modalidad, inicio y fin; cargo y sueldo originales; **cargo y sueldo vigentes hoy** (`cargoVigente`, `sueldoVigente`) cuando difieren de los originales; contrato anterior (`contratoAnteriorNumero`) si viene de una renovación; fecha y motivo de cierre si está `Cerrado` o `Anulado`; tabla de adendas (vigencia, cargo, sueldo), ordenada como la devuelve la API, o «Sin adendas».
  - Botones «Renovar», «Agregar adenda» y «Anular». Solo se habilitan si el `estado` efectivo es `Vigente`, salvo «Renovar», que también se habilita con `Vencido` mientras el contrato no sea `Indeterminado` (renovación tardía, decisión 5 del backend; la API acepta renovar mientras el estado guardado sea `Vigente`). Si el `Vencido` ya está guardado, el servidor responde 409 (18) y se muestra en el diálogo.
  - «Renovar» se deshabilita en `Indeterminado` con el texto visible «Un contrato indeterminado no se renueva.».
  - Con la decisión 1 A, sin rol de escritura los cuatro botones (incluido «Nuevo contrato») quedan deshabilitados con el texto de la decisión.
- **Alta (`ContratoLaboralFormulario`):** empleado (decisión 2), modalidad (select), fecha de inicio y fecha de fin (`type="date"`; la de fin se deshabilita y se vacía con `Indeterminado`), sueldo básico (`inputMode="decimal"`) y cargo (máximo 100, contador «n/100»).
  - Validación local con los mensajes 1 a 12 en el orden del backend. Si falla, no hay petición.
  - Los 400, 404 y 409 del servidor (13 a 16) se muestran junto al formulario y conservan lo escrito.
  - Tras guardar: se selecciona el `id` devuelto y se recargan listado, resumen y detalle.
- **Renovación (`DialogoRenovacion`):** muestra «Empieza el {fechaFin + 1 día}» (calculado como texto `yyyy-MM-dd` sin `Date` local). Campos: modalidad, fecha de fin (deshabilitada con `Indeterminado`), sueldo básico y cargo, prellenados con los vigentes del contrato pero editables (el backend los exige). Validación local 2, 4 a 11 y la 12 contra la fecha de inicio derivada. El servidor puede responder 18, 19 o 14 dentro del diálogo. Al confirmar, se selecciona el contrato nuevo.
- **Adenda (`DialogoAdenda`):** fecha de vigencia, cargo y sueldo básico, vacíos; ayuda visible «Deja vacío lo que no cambia.». Validación local 20, 21 y, de lo que llegue, 4 a 9. Los mensajes 22 a 25 llegan del servidor y se muestran dentro del diálogo. Solo se envían los campos con valor.
- **Anulación (`DialogoAnulacion`):** motivo (textarea, máximo 300, contador «n/300») y aviso «La anulación no se puede deshacer.». Validación local 26 y 27; el 409 (28) se muestra dentro del diálogo.
- **Diálogos:** modales, con el foco en el primer campo al abrir; Escape los cierra y devuelve el foco al botón que los abrió; los botones se deshabilitan mientras se envía. Mismo patrón que `DialogoCese` del 61.
- **Fechas e importes:** las fechas se muestran con `formatearFecha` (UTC) y se envían como `yyyy-MM-dd`, sin convertirlas a `Date` local. El sueldo se envía como número y se valida con los mismos límites que el backend (mayor que 0, máximo 9999999999.99, dos decimales) antes de convertirlo, para no redondear en silencio.
- **Respuestas obsoletas:** el detalle se guarda con el id al que pertenece, como `useEmpleadosData`, para que una respuesta de un contrato anterior no se muestre tras cambiar la selección.
- **Accesibilidad y responsive:** cada campo con `label`; errores con `aria-describedby` (los de campo los da `CampoConError`); la tabla con `caption`; estados con texto; foco visible y uso completo con teclado. Responsive a 375 y 1280 px en claro y oscuro, con la lista de `docs/equipo/lecciones.md` («Pantallas que no siguen el diseño común»).

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `npm --prefix frontend run verify` y `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | `api.test.ts`: las 6 funciones con URL, método, query y cuerpo exactos; filtros vacíos omitidos | pasan |
| A3 | Listado, resumen y detalle | `ContratosLaboralesPage.test.tsx`: filtros en la query; página 1 al filtrar; búsqueda tras 300 ms con una sola consulta; conteos de la decisión 3; pastillas con texto; adendas, contrato anterior y cierre en el detalle | pasan |
| A4 | Alta | `ContratoLaboralFormulario.test.tsx`: mensajes 1 a 12 sin petición; fecha de fin deshabilitada con `Indeterminado`; sueldo `0.001` y 12 dígitos rechazados sin petición; 409 (16) conservando lo escrito | pasan |
| A5 | Renovación, adenda y anulación | `DialogosContrato.test.tsx`: fecha de inicio derivada mostrada; validaciones locales sin petición; 409 (18, 22, 24, 28) dentro del diálogo; Escape y foco; botones según estado y modalidad | pasan |
| A6 | Roles (decisión 1) | tests: con `roles: ['CONSULTA']` los botones de escritura deshabilitados con el texto; con `RRHH` o `ADMIN`, habilitados | pasan |
| A7 | Sin regresiones | las demás pantallas sin cambios; la prueba de «Pantalla pendiente» de `App.test.tsx` sigue en verde; la suite pasa 10 veces seguidas | pasan |
| A8 | QA contra PostgreSQL | API local con `scripts/seed-dev.sql`: CTR-000005 se ve `Vencido`, CTR-000001 muestra «Gerente Comercial» y 9500.00 vigentes, alta, renovación, adenda y anulación de extremo a extremo; seed restaurado al terminar | anotado en §7 |
| A9 | Visual | capturas a 375 y 1280 px, claro y oscuro: listado con detalle `Vigente` y `Anulado`, alta y los tres diálogos; comparación con Empleados; sin scroll horizontal de página ni palabras partidas | adjuntas al PR |

## 6. Checklist de ejecución
- [ ] Decisiones 1 a 3 de §2 resueltas por Lukas y spec aprobada.
- [ ] Tarea 1: `tipos.ts`, `api.ts` y `api.test.ts`.
- [ ] Tarea 2: resumen, filtros, tabla, pastilla y detalle.
- [ ] Tarea 3: formulario de alta y validación local.
- [ ] Tarea 4: diálogos de renovación, adenda y anulación.
- [ ] Tarea 5: registro en `pantallas.ts` y estilos en `app.css`.
- [ ] Tarea 6: tests, QA contra PostgreSQL, capturas y A1 a A9 en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
