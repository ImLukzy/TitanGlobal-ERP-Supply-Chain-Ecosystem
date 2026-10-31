# Especificación: 20 - Pantalla de Modulo23_Almacenes (frontend)

## 1. Objetivo
**Problema:** `/modulos/23-almacenes` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla de mantenimiento de almacenes contra el contrato de la spec 19 §4, con el patrón y los componentes compartidos de Proveedores (spec 13), partida en componentes desde el principio.

## 2. Fuera de alcance
- La API: `fetch` simulado y contrato de la spec 19 §4.
- Cambiar otras pantallas, salvo mejoras de componentes compartidos sin cambiar lo que comprueban sus tests.
- Librerías nuevas.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/almacenes/{tipos,api}.ts` | crear | 7 funciones (`listarAlmacenes(filtros)`, `obtenerAlmacen`, `crearAlmacen`, `actualizarAlmacen`, `eliminarAlmacen`, `listarCiudadesAlmacen`, `obtenerResumenAlmacenes`); `''` y `undefined` fuera de la query |
| `frontend/src/modules/almacenes/*.tsx` | crear | página, resumen, filtros, tabla, detalle y formulario en archivos separados (≤ ~300 líneas cada uno) |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `23-almacenes` |
| `frontend/src/modules/almacenes/*.test.ts(x)` | crear | `fetch` simulado, temporizadores falsos |

## 4. Diseño y lógica
- **Resumen:** total, activos, inactivos, capacidad total y capacidad activa (m³ con separador de miles, p. ej. «12,500.00 m³»); tabla por tipo y por ciudad, con plurales vía `formatearCantidad`.
- **Filtros:** búsqueda por código o nombre (300 ms), ciudad (de `GET almacenes/ciudades`, con «Todas»), tipo («Todos», «Central», «Regional», «Tránsito») y estado («Todos», «Activos», «Inactivos»). Cualquier cambio vuelve a la página 1. Al cargar, el listado se pide **una sola vez** (lección de la spec 17).
- **Tabla:** código, nombre, tipo, ciudad, capacidad y estado con texto. Paginación de 10. Carga, vacío y error.
- **Detalle:** todos los campos; sin responsable → «Sin responsable».
- **Formulario:** código (se muestra en mayúsculas al escribir), nombre, tipo (selector), dirección, ciudad (texto con `datalist`), capacidad (m³), responsable y casilla «Activo» (marcada en el alta). Validación local con los literales de la spec 19 §4 por campo (`aria-describedby`). Errores 400/409 de la API visibles conservando lo escrito.
- **Baja:** `DialogoBaja`; Escape cancela y devuelve el foco.
- **Accesibilidad y responsive:** como en las specs 13, 15 y 17.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 7 funciones con URL, método, query y cuerpo exactos | pasan |
| A3 | Filtros y tabla | tests: espera con temporizadores falsos, ciudad, tipo y estado en la query, página 1, una sola petición del listado al cargar, vacío y error | pasan |
| A4 | Detalle, formulario y baja | tests: «Sin responsable»; cada validación local sin petición; 409 de código conservando lo escrito; edición con PUT; baja con refresco; Escape con foco devuelto | pasan |
| A5 | Sin regresiones | tests de las demás pantallas en verde sin cambiar lo que comprueban; la suite pasa 10 veces seguidas | pasan |
| A6 | Visual | capturas (`page.route`) a 375 y 1280 px, claro y oscuro, con detalle abierto y con el diálogo de baja; márgenes ≥ 16 px, sin scroll horizontal ni celdas recortadas ni códigos partidos en dos líneas a 1280 | adjuntas |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos y `api.ts`.
- [x] Tarea 2: resumen, filtros y tabla.
- [x] Tarea 3: detalle, formulario y baja.
- [x] Tarea 4: tests, capturas y A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | Corrección 1 | ✅ | La integración vio en las capturas la cifra de capacidad partida en dos líneas en las tarjetas, el estado del detalle como texto grande sin estilo y mocks con capacidades de miles; corregido (pastillas de estado en detalle y tabla, cifras en una línea, mocks del seed) |
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` rc=0 en la rama de integración (`8037855`, mismas líneas que el auditado `79e2e44`): backend 199/199, frontend 169/169 |
| 2026-10-15 | A2–A5 | ✅ | tests de `almacenes/` (api, página con filtros, una sola petición al cargar, formulario, baja con Escape); la suite pasó 10/10 seguidas según la implementación y la auditoría |
| 2026-10-15 | A6 | ✅ | `/tmp/titan-spec20/` (8 capturas) revisadas por la integración tras la corrección |
| 2026-10-15 | Auditoría | ✅ | Auditoría, ciclo 1: APROBADO sin hallazgos |
| 2026-10-15 | QA E2E | ✅ | QA en local (Vite 5174 + API + PostgreSQL) y en producción (solo lectura): resumen 5/4/1 con 1,025.00 y 925.00 m³ en una línea y plurales correctos; filtros por código, nombre, ciudad, tipo y estado, una sola petición del listado al cargar; detalle con pastilla y «Sin responsable»; alta con código en minúsculas guardado en mayúsculas; validaciones locales sin petición; ALM-LIM-01 → 409 conservando lo escrito; edición; baja con Escape; demás pantallas sin regresiones; 9 capturas sin recortes ni scroll horizontal |
