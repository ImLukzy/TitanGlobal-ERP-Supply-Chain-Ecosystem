# Especificación: 38 - Pantalla de Modulo78_NotificacionesAlertas (frontend)

## 1. Objetivo
**Problema:** `/modulos/78-notificaciones-alertas` muestra «Pantalla pendiente» y las alertas no se ven en ningún sitio.
**Resultado esperado:** un **centro de alertas** contra el contrato de la spec 37 §4 (resumen, filtros, lista con enlace al registro, descartar y restaurar) y un **indicador en la cabecera** con el número de alertas de severidad alta que lleva a esa pantalla.

## 2. Fuera de alcance
- La API: `fetch` simulado y contrato de la spec 37 §4.
- Notificaciones del navegador o actualización en tiempo real.
- Librerías nuevas.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/notificaciones-alertas/{tipos,api}.ts` | crear | 4 funciones del contrato |
| `frontend/src/modules/notificaciones-alertas/*.tsx` | crear | partida en componentes (≤ ~300 líneas) |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `78-notificaciones-alertas` |
| `frontend/src/components/` (cabecera) y su test | modificar | indicador de alertas altas: una petición a `alertas/resumen` al cargar la app; si falla, no se muestra (sin romper la cabecera) |
| `frontend/src/modules/notificaciones-alertas/*.test.ts(x)` | crear | `fetch` simulado, temporizadores falsos |

## 4. Diseño y lógica
- **Indicador en la cabecera:** campana o texto accesible «3 alertas altas» con `aria-label`; enlace a `/modulos/78-notificaciones-alertas?severidad=alta`. Si no hay altas, sin número.
- **Resumen:** total, altas, medias, bajas y descartadas; alertas por tipo con nombres legibles («Reclamo vencido», «Orden de abastecimiento atrasada»…).
- **Filtros:** tipo, severidad y «Mostrar descartadas»; se reflejan en la URL. Página 1; una sola petición al cargar.
- **Lista:** cada alerta con severidad (pastilla con texto «Alta», «Media», «Baja»), mensaje, módulo de origen, fecha de referencia y enlace «Ver» a su pantalla (`ruta`, con `?…={registroId}` cuando la pantalla de destino lo admite). Botón «Descartar» (o «Restaurar» si está descartada) con confirmación accesible; 404/409 visibles en el diálogo; tras la acción se refrescan lista, resumen e indicador.
- Estados de carga, vacío («No hay alertas pendientes») y error.
- **Accesibilidad y responsive:** como en las pantallas anteriores.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde, 0 warnings |
| A2 | API | tests: las 4 funciones con URL, método, query y cuerpo exactos | pasan |
| A3 | Centro de alertas | tests: filtros en la URL y en la query, «Mostrar descartadas», una sola petición al cargar, severidad con texto, enlace «Ver» correcto, vacío y error | pasan |
| A4 | Descartar y restaurar | tests: confirmación, POST con la clave, refresco de lista, resumen e indicador, 404/409 en el diálogo, Escape con foco devuelto | pasan |
| A5 | Indicador | tests: número de alertas altas y enlace; sin altas no muestra número; si la API falla la cabecera sigue funcionando; los tests existentes de la cabecera siguen en verde | pasan |
| A6 | Sin regresiones | demás pantallas en verde sin cambiar lo que comprueban; la suite pasa 10 veces seguidas con carga | pasan |
| A7 | Visual | capturas (`page.route`) a 375 y 1280 px, claro y oscuro: centro de alertas con las tres severidades, diálogo de descarte e indicador en la cabecera; márgenes ≥ 16 px, sin scroll horizontal ni palabras partidas | adjuntas |

## 6. Checklist de ejecución
- [ ] Tarea 1: tipos y `api.ts`.
- [ ] Tarea 2: centro de alertas (resumen, filtros, lista, descartar).
- [ ] Tarea 3: indicador en la cabecera.
- [ ] Tarea 4: tests, capturas y A1–A7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
