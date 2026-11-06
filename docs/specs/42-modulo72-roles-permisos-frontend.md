# Especificación: 42 - Pantalla de Modulo72_RolesPermisos (frontend)

## 1. Objetivo
**Problema:** `/modulos/72-roles-permisos` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla de roles contra el contrato de la spec 41 §4: mantenimiento de roles, **editor de permisos** por módulo agrupado por dominio y una vista de **matriz** roles × módulos.

## 2. Fuera de alcance
- La API: `fetch` simulado y contrato de la spec 41 §4.
- El selector «Rol de prueba» de la cabecera y el 403: no cambian.
- Librerías nuevas.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/roles-permisos/{tipos,api}.ts` | crear | 8 funciones del contrato |
| `frontend/src/modules/roles-permisos/*.tsx` | crear | partida en componentes (≤ ~300 líneas) |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `72-roles-permisos` |
| `frontend/src/modules/roles-permisos/*.test.ts(x)` | crear | `fetch` simulado, temporizadores falsos, `esperarCargaInicial` |

## 4. Diseño y lógica
- **Pestañas** «Roles» y «Matriz» (accesibles, reflejadas en `?vista=`).
- **Roles:** búsqueda (300 ms); tabla con código, nombre, «De sistema» (pastilla con texto), módulos con lectura y con escritura; detalle; formulario con los literales de la spec 41; baja con `DialogoBaja` (409 de rol de sistema en el diálogo).
- **Editor de permisos** (desde el detalle): los 80 módulos agrupados por los 8 dominios en secciones plegables; por módulo dos casillas «Leer» y «Escribir» (marcar «Escribir» marca «Leer»; desmarcar «Leer» desmarca «Escribir»); por dominio, atajos «Todo lectura», «Todo escritura» y «Ninguno»; contador de cambios sin guardar y botones «Guardar permisos» / «Descartar». Se envía el conjunto completo con PUT; los errores de la API se muestran sin perder los cambios. En un rol de sistema, las casillas que quitarían permisos están deshabilitadas con explicación.
- **Matriz:** tabla accesible (cabeceras de fila y columna) con los módulos en filas y los roles en columnas; cada celda con texto («Escritura», «Lectura», «—»); filtro por dominio; scroll horizontal **interno** de la tabla si no cabe (nunca de la página) con la columna del módulo fija.
- Una sola petición por listado al cargar; estados de carga, vacío y error.
- **Accesibilidad y responsive:** como en las pantallas anteriores; estilos y componentes compartidos.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 8 funciones con URL, método, query y cuerpo exactos | pasan |
| A3 | Roles | tests: filtros, formulario con validaciones locales, 409 de código conservando lo escrito, PUT, baja con 409 de rol de sistema en el diálogo, Escape | pasan |
| A4 | Editor de permisos | tests: escribir implica leer y quitar leer quita escribir; atajos por dominio; contador de cambios; PUT con los 80 módulos y el cuerpo exacto; descartar; error de la API sin perder cambios; rol de sistema con casillas deshabilitadas | pasan |
| A5 | Matriz | tests: celdas con texto, filtro por dominio, cabeceras accesibles | pasan |
| A6 | Sin regresiones | demás pantallas y la cabecera en verde sin cambiar lo que comprueban; la suite pasa 10 veces seguidas con carga | pasan |
| A7 | Visual | capturas (`page.route`) a 375 y 1280 px, claro y oscuro: editor de permisos con un dominio abierto, matriz y diálogo de baja; márgenes ≥ 16 px, sin scroll horizontal de página ni palabras partidas | adjuntas |

## 6. Checklist de ejecución
- [x] Tarea 1: tipos y `api.ts`.
- [x] Tarea 2: pestaña Roles y formulario.
- [x] Tarea 3: editor de permisos y matriz.
- [ ] Tarea 4: tests, capturas y A1–A7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-05 | A2 | pasa | `npx vitest run src/modules/roles-permisos/api.test.ts`: 9 tests |
| 2026-11-05 | A3 | pasa | `RolesPermisosPage.test.tsx` (21 tests) y `validacion.test.ts` (5 tests) |
| 2026-11-05 | A4 | pasa | `EditorPermisos.test.tsx` (11), `permisos.test.ts` (11) y `usePermisosRol.test.tsx` (6) |
| 2026-11-05 | A5 | pasa | `MatrizPermisos.test.tsx`: 9 tests |
| 2026-11-05 | A6 | pasa | `npx vitest run` en la rama fusionada con `main`: 71 archivos y 534 tests, 10 ejecuciones seguidas sin fallos |
| 2026-11-05 | A7 | adjuntas | capturas con `page.route` a 375 y 1280 px, claro y oscuro (editor con un dominio abierto, matriz, diálogo de baja y lista con rol de sistema): sin scroll horizontal de página, margen lateral mínimo de 34 px (375) y 67 px (1280), sin palabras partidas |
