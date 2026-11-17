# Especificación: Pantalla de Modulo33_CentrosTrabajo (frontend)

## 1. Objetivo
**Problema:** `/modulos/33-centros-trabajo` muestra «Pantalla pendiente». El backend ya está integrado en `main` (`docs/specs/modulo33-centros-trabajo-backend.md`).
**Resultado esperado:** pantalla de mantenimiento de centros de trabajo contra el contrato de `modulo33-centros-trabajo-backend.md` §4, con el patrón y los componentes compartidos de Almacenes (spec 20) y Proveedores (spec 13), partida en componentes desde el principio.

Owner: Antony Huayhua. Guía: `docs/equipo/crear-pantalla.md`.

**Estado:** borrador revisado; pendiente de aprobación de Lukas antes de implementar.

## 2. Fuera de alcance
- La API y la base de datos: `fetch` simulado en los tests y contrato de la spec del backend §4.
- Cambiar otras pantallas, salvo mejoras de componentes compartidos sin cambiar lo que comprueban sus tests.
- Librerías nuevas.
- El control de concurrencia en edición y baja del backend, pendiente según `docs/equipo/relevo.md` §1; se aborda en una tarea separada y no se considera resuelto al terminar esta pantalla.

**Decisiones de producto que requieren aprobación antes de ejecutar:** presentar el costo por hora en soles (`PEN`), siguiendo el formateador compartido del frontend. El contrato del backend no contiene un campo de moneda; esta propuesta no añade conversiones ni modifica ese contrato.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/centros-trabajo/{tipos,api}.ts` | crear | 7 funciones: `listarCentrosTrabajo(filtros)`, `obtenerCentroTrabajo`, `crearCentroTrabajo`, `actualizarCentroTrabajo`, `eliminarCentroTrabajo`, `listarPlantasCentroTrabajo`, `obtenerResumenCentrosTrabajo`; `''` y `undefined` fuera de la query; sin `any` |
| `frontend/src/modules/centros-trabajo/*.tsx` | crear | `CentrosTrabajoPage`, `CentrosTrabajoResumen`, `CentrosTrabajoFiltros`, `CentrosTrabajoTabla`, `CentroTrabajoDetallePanel` y `CentroTrabajoFormulario` en archivos separados (≤ ~300 líneas cada uno), dentro de `<PaginaModulo>` |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `'33-centros-trabajo': CentrosTrabajoPage` (bloque al final, sin reordenar) |
| `frontend/src/styles/app.css` | modificar | solo si hace falta: bloque al final con prefijo `.centros-trabajo-` |
| `frontend/src/modules/centros-trabajo/*.test.ts(x)` | crear | `api.test.ts` y `CentrosTrabajoPage.test.tsx`; `fetch` simulado con `vi.stubGlobal`, temporizadores falsos |

## 4. Diseño y lógica
- **Resumen** (`GET centros-trabajo/resumen`): total, activos, inactivos, capacidad total y capacidad activa en horas por día (p. ej. «68.00 h/día»); tabla por tipo (Línea, Celda) y por planta, con plurales vía `formatearCantidad`.
- **Filtros:** búsqueda por código o nombre (300 ms de espera), planta (de `GET centros-trabajo/plantas`, con «Todas»), tipo («Todos», «Línea», «Celda», que envían `Linea` / `Celda`) y estado («Todos», «Activos», «Inactivos»). Cualquier cambio vuelve a la página 1. Al cargar, el listado se pide **una sola vez**.
- **Tabla:** código, nombre, tipo («Línea» / «Celda»), planta, capacidad (h/día) y estado con texto. Paginación de 10. Estados de carga, vacío y error.
- **Detalle:** todos los campos, con el costo por hora en soles (`PEN`) y dos decimales mediante `formateadorMoneda` de `frontend/src/lib/formato.ts`, sujeto a la aprobación indicada en §2; sin responsable → «Sin responsable».
- **Formulario** (alta y edición): código (se muestra en mayúsculas al escribir), nombre, tipo (selector), planta (texto con `datalist` de las plantas), capacidad en h/día, costo por hora, responsable y casilla «Activo» (marcada en el alta). Validación local con los literales de la spec del backend §4 por campo (`aria-describedby` con `CampoConError`):
  - «El código es obligatorio.» / «El código solo admite letras, números y guiones (2 a 20 caracteres).»
  - «El nombre del centro de trabajo es obligatorio.» / «El nombre del centro de trabajo no puede superar 200 caracteres.»
  - «El tipo de centro de trabajo no es válido.»
  - «La planta es obligatoria.» / «La planta no puede superar 100 caracteres.»
  - «La capacidad debe ser mayor que 0 y como máximo 24 horas por día.» / «La capacidad admite como máximo 2 decimales.»
  - «El costo por hora no puede ser negativo.» / «El costo por hora admite como máximo 2 decimales.» / «El costo por hora no puede superar 9999999999999999.99.»
  - «El responsable no puede superar 100 caracteres.»

  Errores 400 y 409 de la API («Ya existe un centro de trabajo con el código {codigo}.») visibles conservando lo escrito.
- **Baja:** `DialogoBaja`; Escape cancela y devuelve el foco al botón que lo abrió; tras confirmar se refrescan tabla y resumen.
- **Accesibilidad y responsive:** como en las specs 13 y 20. Panel de detalle lateral a 1280 px y apilado a 375 px.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` (Git Bash en Windows) | en verde, 0 warnings |
| A2 | API | `api.test.ts`: las 7 funciones con URL, método, query (sin filtros vacíos) y cuerpo exactos | pasan |
| A3 | Filtros y tabla | `CentrosTrabajoPage.test.tsx`: espera de 300 ms con temporizadores falsos; planta, tipo y estado en la query; vuelta a la página 1; una sola petición del listado al cargar; vacío y error | pasan |
| A4 | Detalle, formulario y baja | tests: «Sin responsable»; costo con la moneda aprobada y dos decimales; cada validación local sin petición, incluidos tipo inválido y costo superior al máximo del backend; errores 400 y 409 conservando lo escrito; edición con PUT; baja con refresco; Escape con foco devuelto | pasan |
| A5 | Sin regresiones | tests de las demás pantallas en verde sin cambiar lo que comprueban; la suite del frontend pasa 10 veces seguidas | pasan |
| A6 | Visual | capturas a 375 y 1280 px, en claro y oscuro, con el detalle abierto y con el diálogo de baja; márgenes ≥ 16 px, sin scroll horizontal ni celdas recortadas ni códigos partidos en dos líneas a 1280 px | adjuntas |

## 6. Checklist de ejecución
- [ ] Tarea 0: aprobación de Lukas de esta spec y de la moneda propuesta en §2.
- [ ] Tarea 1: `tipos.ts` y `api.ts`.
- [ ] Tarea 2: resumen, filtros y tabla.
- [ ] Tarea 3: detalle, formulario y baja.
- [ ] Tarea 4: registro en `pantallas.ts`, tests, capturas y A1–A6 en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
