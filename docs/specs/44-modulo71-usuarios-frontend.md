# Especificación: 44 - Pantalla de Modulo71_Usuarios (frontend)

## 1. Objetivo
**Problema:** `/modulos/71-usuarios` muestra «Pantalla pendiente».
**Resultado esperado:** pantalla de usuarios contra el contrato de la spec 43 §4: mantenimiento con asignación de roles y una vista de **permisos efectivos** por dominio y módulo, con el diseño común (ver la lección «Pantallas que no siguen el diseño común»).

## 2. Fuera de alcance
- La API: `fetch` simulado y contrato de la spec 43 §4.
- El selector «Rol de prueba» de la cabecera y el 403: no cambian.
- Contraseñas o inicio de sesión. Librerías nuevas.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/usuarios/{tipos,api}.ts` | crear | 7 funciones del contrato |
| `frontend/src/modules/usuarios/*.tsx` | crear | partida en componentes (≤ ~300 líneas) |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `71-usuarios` |
| `frontend/src/modules/usuarios/*.test.ts(x)` | crear | `fetch` simulado, temporizadores falsos, `esperarCargaInicial` |

## 4. Diseño y lógica
- **Resumen:** total, activos, inactivos y sin rol; usuarios por rol.
- **Filtros:** búsqueda (300 ms), rol (de `GET roles?tamanoPagina=100`) y estado. Página 1; una sola petición al cargar.
- **Tabla:** usuario, nombre completo, email, roles (pastillas con el código) y estado con pastilla. Paginación de 10. Carga, vacío y error.
- **Detalle (panel lateral):** datos, fecha de alta, roles y **permisos efectivos**: los 80 módulos agrupados por los 8 dominios en secciones plegables, cada módulo con su nivel en texto («Escritura», «Lectura», «Sin acceso») y «por VENTAS, CONSULTA»; resumen «X módulos con lectura, Y con escritura».
- **Formulario:** usuario (minúsculas al escribir), nombre completo, email, «Activo» y roles (casillas con el código y el nombre del rol). Validación local con los literales de la spec 43; 409 (duplicados o último ADMIN) visibles conservando lo escrito. Baja con `DialogoBaja` (409 de último ADMIN en el diálogo).
- **Accesibilidad y responsive:** como en las pantallas anteriores; diseño común comparado con Contratos antes de entregar.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 7 funciones con URL, método, query y cuerpo exactos | pasan |
| A3 | Listado y detalle | tests: filtros en la query, página 1, una sola petición al cargar, roles y estado con texto; permisos efectivos agrupados por dominio con nivel y roles que lo otorgan | pasan |
| A4 | Formulario y baja | tests: validaciones locales sin petición, usuario en minúsculas, casillas de roles en el cuerpo (`rolIds`), 409 de duplicado y de último ADMIN conservando lo escrito, PUT, baja con 409 en el diálogo, Escape | pasan |
| A5 | Sin regresiones | demás pantallas y la cabecera en verde sin cambiar lo que comprueban; la suite pasa 10 veces seguidas con carga | pasan |
| A6 | Visual | capturas (`page.route`) a 375 y 1280 px, claro y oscuro: listado con detalle y permisos efectivos (un dominio abierto), formulario y diálogo de baja; comparación con Contratos adjunta; márgenes ≥ 16 px, sin scroll horizontal ni palabras partidas | adjuntas |

## 6. Checklist de ejecución
- [ ] Tarea 1: tipos y `api.ts`.
- [ ] Tarea 2: resumen, filtros, tabla y detalle con permisos efectivos.
- [ ] Tarea 3: formulario y baja.
- [ ] Tarea 4: tests, capturas y A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
