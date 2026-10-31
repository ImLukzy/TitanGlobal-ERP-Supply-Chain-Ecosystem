# Especificación: 22 - Pantalla de Modulo24_ZonasAlmacenaje (frontend)

## 1. Objetivo
**Problema:** `/modulos/24-zonas-almacenaje` muestra «Pantalla pendiente» y no se ve cuánta capacidad de cada almacén está asignada a zonas.
**Resultado esperado:** pantalla de zonas contra el contrato de la spec 21 §4, con el patrón y los componentes de Almacenes (spec 20), y una barra de ocupación por almacén.

## 2. Fuera de alcance
- La API: `fetch` simulado y contrato de la spec 21 §4.
- Cambiar otras pantallas, salvo mostrar en Almacenes el 409 «tiene zonas» al borrar (ya lo muestra el diálogo con el `Message` de la API; solo se añade su test).
- Librerías nuevas (la barra es un `<progress>` o un `div` con `role="meter"`).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/zonas-almacenaje/{tipos,api}.ts` | crear | 6 funciones (`listarZonas(filtros)`, `obtenerZona`, `crearZona`, `actualizarZona`, `eliminarZona`, `obtenerResumenZonas(almacenId?)`) |
| `frontend/src/modules/zonas-almacenaje/*.tsx` | crear | partida en componentes (≤ ~300 líneas por archivo) |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `24-zonas-almacenaje` |
| `frontend/src/modules/almacenes/*.test.tsx` | modificar | test: borrar un almacén con zonas muestra el 409 en el diálogo |
| `frontend/src/modules/zonas-almacenaje/*.test.ts(x)` | crear | `fetch` simulado, temporizadores falsos |

## 4. Diseño y lógica
- **Selector de almacén** arriba (de `GET almacenes?activo=true&tamanoPagina=100`, con «Todos»): filtra la tabla y el resumen. Se puede abrir con `?almacen={id}`.
- **Resumen:** total y activas; zonas por tipo; **ocupación por almacén** con barra accesible (`role="meter"`, `aria-valuenow`, texto «412.00 de 500.00 m³ · 82.40 %»); a partir del 90 % la barra cambia de tono y muestra el texto «Casi lleno» (no solo color).
- **Filtros:** búsqueda (300 ms), tipo, temperatura y estado; vuelta a la página 1; el listado se pide una sola vez al cargar.
- **Tabla:** almacén, código, nombre, tipo, temperatura, capacidad y estado con texto. Paginación de 10. Carga, vacío y error.
- **Detalle y formulario:** almacén (selector de activos), código en mayúsculas al escribir, nombre, tipo, temperatura, capacidad y casilla «Activa». Validación local con los literales de la spec 21 §4; el 409 de capacidad y el de código repetido se muestran conservando lo escrito.
- **Baja:** `DialogoBaja`; Escape y foco devuelto.
- **Accesibilidad y responsive:** como en la spec 20.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 6 funciones con URL, método, query y cuerpo exactos | pasan |
| A3 | Filtros, tabla y ocupación | tests: selector de almacén y `?almacen=`, filtros en la query, página 1, una sola petición al cargar, barra con `aria-valuenow` y texto, «Casi lleno» a partir del 90 %, vacío y error | pasan |
| A4 | Formulario y baja | tests: validaciones locales sin petición, 409 de capacidad y de código conservando lo escrito, edición con PUT, baja con refresco, Escape; en Almacenes, 409 «tiene zonas» en el diálogo | pasan |
| A5 | Sin regresiones | demás pantallas en verde sin cambiar lo que comprueban; la suite pasa 10 veces seguidas | pasan |
| A6 | Visual | capturas (`page.route`) a 375 y 1280 px, claro y oscuro: resumen con un almacén casi lleno, detalle y diálogo de baja; márgenes ≥ 16 px, sin scroll horizontal, celdas recortadas ni códigos partidos | adjuntas |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos y `api.ts`.
- [x] Tarea 2: selector, resumen con ocupación, filtros y tabla.
- [x] Tarea 3: detalle, formulario, baja y test de Almacenes.
- [x] Tarea 4: tests, capturas y A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | Corrección 1 | ✅ | La integración vio en las capturas «Almacenamiento» partido a mitad de palabra y códigos con letra diminuta, y faltaban 5 tests de la spec (medidor accesible, umbral 90.00 % frente a 89.99 %, `?almacen=`, temperatura en la query, 409 de código). Corregido |
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` rc=0 en la rama de integración (`afe234d`, mismas líneas que el auditado `3f796b7`): backend 230/230, frontend 185/185 |
| 2026-10-15 | A2–A5 | ✅ | tests de `zonas-almacenaje/` (api, página, `ZonasResumen.test.tsx` con el medidor y el umbral) y el 409 «tiene zonas» en Almacenes; 10/10 seguidas según la implementación y la auditoría |
| 2026-10-15 | A6 | ✅ | `/tmp/titan-spec22/` (8 capturas) revisadas por la integración tras la corrección: barra «450.00 de 500.00 m³ · 90.00 %» con «Casi lleno», tabla sin palabras partidas |
| 2026-10-15 | Auditoría | ✅ | Auditoría, ciclo 1: APROBADO sin hallazgos |
| 2026-10-15 | QA E2E | ✅ | QA en local (Vite 5174 + API + PostgreSQL) y en producción: ocupación con `role="meter"` (ALM-LIM-01 90.00 % «Casi lleno», ALM-ARE-01 86.67 % sin aviso, ALM-CAL-TR sin contar la inactiva); `?almacen=` y filtros; una sola petición al cargar; alta hasta el 100 % → 200 y 1 m³ más → 409 literal conservando lo escrito; código repetido en el mismo almacén → 409 y permitido en otro; inactivar baja la ocupación; baja con Escape; borrar ALM-LIM-01 en Almacenes → 409 en el diálogo; sin regresiones; 13 capturas sin recortes, palabras partidas ni scroll horizontal |
