# Especificación: 46 - Pantalla de Modulo77_AuditoriaTransaccional (frontend)

## 1. Objetivo
**Problema:** `/modulos/77-auditoria-transaccional` muestra «Pantalla pendiente».
**Resultado esperado:** visor de auditoría de solo lectura contra el contrato de la spec 45 §4: resumen (incluida la actividad de los últimos 7 días), filtros, tabla y detalle con los cambios campo a campo (antes → después). Además, el frontend envía la cabecera `X-Usuario` para que la auditoría sepa quién hizo cada cambio.

## 2. Fuera de alcance
- La API: `fetch` simulado y contrato de la spec 45 §4.
- Editar o borrar registros. Librerías de gráficos nuevas (la actividad se dibuja con barras HTML/CSS accesibles o SVG simple).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/auditoria-transaccional/{tipos,api}.ts` | crear | 4 funciones del contrato |
| `frontend/src/modules/auditoria-transaccional/*.tsx` | crear | partida en componentes (≤ ~300 líneas) |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `77-auditoria-transaccional` |
| `frontend/src/api/client.ts` (+ su test) | modificar | añadir `X-Usuario` a todas las peticiones con un valor configurable (por defecto `"demo"`); no cambiar el manejo de `X-User-Role` ni de errores |
| `frontend/src/modules/auditoria-transaccional/*.test.ts(x)` | crear | `fetch` simulado, temporizadores falsos, `esperarCargaInicial` |

## 4. Diseño y lógica
- **Resumen:** total, altas, modificaciones y bajas; top de entidades; **actividad de los últimos 7 días** como barras accesibles (cada barra con su número en texto y la fecha).
- **Filtros:** entidad (de `GET auditoria/entidades`), acción, usuario, Id del registro y rango de fechas (validación local). Página 1; una sola petición al cargar. Admite `?entidad=Cliente&entidadId=3` para abrir el historial de un registro.
- **Tabla:** fecha y hora (UTC), entidad, Id, acción con pastilla («Alta», «Modificación», «Baja» con texto), usuario, ruta y nº de campos cambiados. Paginación de 20. Carga, vacío y error.
- **Detalle (panel lateral):** cabecera con la acción, la entidad y el Id; tabla de cambios «Campo · Antes · Después» (los valores largos se envuelven sin desbordar; `null` se muestra como «—»); enlace «Ver historial de este registro» que aplica el filtro de entidad e Id.
- **Accesibilidad y responsive:** diseño común (comparar con Contratos antes de entregar).

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API y cabecera | tests: las 4 funciones con URL y query exactas; todas las peticiones llevan `X-Usuario`; `X-User-Role` y los errores siguen igual (tests existentes del cliente en verde) | pasan |
| A3 | Visor | tests: filtros en la query, `?entidad=&entidadId=`, rango inválido sin petición, página 1, una sola petición al cargar, acción con texto, vacío y error; detalle con la tabla de cambios y «—» para nulos; enlace de historial | pasan |
| A4 | Actividad | tests: 7 barras con su número y fecha en texto, incluidos los días con 0 | pasan |
| A5 | Sin regresiones | demás pantallas en verde sin cambiar lo que comprueban; la suite pasa 10 veces seguidas con carga | pasan |
| A6 | Visual | capturas (`page.route`) a 375 y 1280 px, claro y oscuro: visor con detalle de una modificación y resumen con actividad; comparación con Contratos adjunta; márgenes ≥ 16 px, sin scroll horizontal ni palabras partidas | adjuntas |

## 6. Checklist de ejecución
- [ ] Tarea 1: tipos, `api.ts` y cabecera `X-Usuario`.
- [ ] Tarea 2: resumen con actividad y filtros.
- [ ] Tarea 3: tabla y detalle con cambios.
- [ ] Tarea 4: tests, capturas y A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
