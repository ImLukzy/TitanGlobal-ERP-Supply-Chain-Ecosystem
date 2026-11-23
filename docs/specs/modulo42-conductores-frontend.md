# Especificación: módulo 42 — pantalla de Conductores

**Estado de revisión: propuesta, pendiente de aprobación de Lukas.**

## 1. Objetivo
Completar `/modulos/42-conductores` con búsqueda, filtro administrativo de habilitación, paginación, detalle, alta, edición y baja, siguiendo Proveedores, Centros de Trabajo y `crear-pantalla.md`. Responsable: Kelvin Pinto.

Propuesta, pendiente de aprobación de Lukas. Contrato: `modulo42-conductores-backend.md` §4.

## 2. Fuera de alcance
Otros módulos, relación con vehículos, entidades base, middleware, pipeline, dependencias nuevas y cambios de permisos automáticos. No se añade un campo `PuedeConducir` al contrato ni se transforma el significado de `Habilitado` sin una decisión aprobada.

## 3. Archivos afectados
- `frontend/src/modules/conductores/{tipos,api,presentacion,validacion}.ts`.
- `ConductoresPage.tsx`, `ConductoresFiltros.tsx`, `ConductoresTabla.tsx`, `ConductorDetallePanel.tsx`, `ConductorFormulario.tsx` en esa carpeta.
- Pruebas de API, página, formulario y fechas, con respuestas HTTP simuladas.
- `frontend/src/modules/pantallas.ts`: registrar únicamente `42-conductores`.
- `frontend/src/styles/app.css`: bloque nuevo con prefijo `.conductores-`, sin alterar selectores existentes.
- `docs/evidencias/modulo42/`: capturas y resultados de verificación real.

## 4. Diseño y lógica
- Las cinco operaciones de `/conductores` utilizan el cliente compartido, autenticación existente y DTOs en camelCase. Omitir filtros vacíos/indefinidos y conservar `habilitado=false`.
- Búsqueda por nombre o licencia con espera de 300 ms; filtro Habilitación: Todos, Habilitados, No habilitados. Cada cambio vuelve a página 1; tamaño 10. Mostrar total de coincidencias del listado, sin inventar un resumen global.
- `PaginaModulo`, `TablaPaginada`, `PanelDetalle`, `CampoConError`, `DialogoBaja` y tokens compartidos. Estados de carga, vacío, error y reintento; ignorar respuestas obsoletas.
- Tabla: licencia, nombre, vencimiento, vigencia, habilitación y activo, con texto accesible. Detalle con todos los campos y observaciones, o «Sin observaciones».
- **Relación entre estados:** `Habilitado` conserva su significado administrativo del backend y del smoke existentes. La vigencia se calcula por día UTC y se presenta por separado como «Vigente» o «Vencida». Un activo/habilitado con licencia vencida muestra una advertencia explícita; no se presenta la habilitación administrativa como autorización efectiva para conducir. No se modifica automáticamente el dato persistido al pasar el tiempo.
- **Formulario:** nombre (100), licencia (20), vencimiento en `yyyy-MM-dd`, habilitado y activo (marcados en el alta), observaciones (500). Trim de textos; validación local con los mensajes literales del backend, sin enviar datos inválidos.
- **UTC y edición:** una fecha nueva se envía con `T00:00:00Z`. Se rechazan fechas inexistentes o anteriores a hoy UTC. En edición se permite conservar el día vencido original y se reenvía su instante ISO original, sin desplazarlo ni perder su hora. Cambiarlo a otra fecha pasada se rechaza, incluso al desactivar. Hoy UTC es válido. `min` de HTML no debe impedir conservar la fecha vencida existente.
- Errores 400/409 visibles sin perder los valores. Impedir envíos duplicados. Apertura con foco en el primer campo y cierre con foco devuelto al origen; baja cancelable con Escape.

## 5. Criterios de aceptación
| # | Criterio | Verificación |
|---|---|---|
| A1 | Gate | `verify.sh` correcto, sin errores ni advertencias de compilación |
| A2 | HTTP | cinco operaciones, query/cuerpo exactos y mensajes conservados |
| A3 | Consultas | carga única, espera 300 ms, filtros, paginación, vacío/error, detalle y respuestas obsoletas |
| A4 | Mutaciones y fechas | límites y literales, UTC sin cambio de día, conservación de vencimiento, POST/PUT/baja y errores |
| A5 | Sin regresiones | suite completa del frontend aprobada, sin debilitar pruebas ajenas |
| A6 | QA real | API y PostgreSQL locales, 375/1280 px en claro/oscuro, capturas y flujos reales |

## 6. Checklist
- [ ] Revisión de Lukas.
- [x] Contratos, API y pruebas.
- [x] Listado, filtros y detalle.
- [x] Formulario, UTC y baja con pruebas.
- [x] Gate, smoke y QA real, evidencia en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-21 | A1–A5 | Aprobados | `verify.sh`: 1090 pruebas backend y 582 frontend (74 archivos), lint/TypeScript/build correctos, compilación .NET con cero errores/advertencias; Vite conserva el aviso existente de tamaño de bundle |
| 2026-11-21 | A2–A4 | 39 pruebas del módulo aprobadas | contratos HTTP, fechas UTC, paginación, búsqueda/filtros, detalle obsoleto, errores, alta/edición/baja, prevención de envíos duplicados y foco |
| 2026-11-21 | A6 | Aprobado con datos reales | Edge, API y PostgreSQL locales, seed real, 375/1280 px claro/oscuro; sin desbordamiento ni errores JavaScript; conflicto por licencia en minúsculas, CRUD real y desactivación/reactivación de licencia vencida conservando su instante UTC |
| 2026-11-21 | PostgreSQL | Aprobado | `SCAFFOLD AL DÍA` y `SMOKE OK`, sin cambios a pruebas ajenas ni archivos generados editados a mano |

La implementación y verificación local están completas. Revisión de Lukas, PR, integración y despliegue pendientes; no se presentan como concedidos.

### Preparación de revisión — 2026-11-22

Rama actualizada con `git pull origin main`, conservando la tabla oficial de relevo y modificando solo la línea de Kelvin y la reserva propia. Script vigente: `database/022-conductores-licencia-unica.sql`. Propuesta, pendiente de aprobación de Lukas. Base PostgreSQL local recreada, seed cargado y entidades/contexto regenerados mediante scaffold, sin edición manual ni diferencias de contenido.

`./scripts/verify.sh`: salida0, 1090 pruebas backend y 582 frontend en 74 archivos. `./scripts/smoke-postgres.sh`: salida0, scaffold coincidente y SMOKE OK. Se usó VITEST_MAX_WORKERS=1 temporal, sin modificar pruebas, timeouts ni configuración compartida. Persisten los avisos globales de ESLint9/Vite; no se cambiaron dependencias ni umbrales. El primer smoke se detuvo en su precomprobación porque el contenedor local de preparación ocupaba5433. Se cerró con docker compose down y el smoke completo pasó al repetirlo; se conserva el registro inicial.

No se generaron capturas nuevas ni se cambió la pantalla para estos ajustes. Se conserva la evidencia visual previa. Preparación local: sin nuevos PR, sin push y sin integración. Tras cada fusión anterior habrá que incorporar main y repetir estas verificaciones antes del PR correspondiente.

### Preparación del PR de Conductores — 2026-11-23

Actualizada la rama con main cb9dfe1851428cfa60d2b9b9e47b3d426043bb69 después de integrar los PR #51 y #55. Se conserva `docs/equipo/relevo.md` exactamente como main; asignaciones conserva los estados aprobados del41/72/62 y solo actualiza la fila propia del42. Registro de pantallas y estilos conservan íntegros main y Conductores. No hubo conflicto en TitanGlobalDbContext.cs ni edición manual de archivos generados.

Script propio vigente: `database/022-conductores-licencia-unica.sql`; la aplicación en la demo corresponde a Lukas antes de integrar. Localmente se recreó PostgreSQL, se cargó el seed y se ejecutó scaffold sin diferencias de contenido en modelos/código/pruebas. Los scripts001/008/021/022 están aplicados en la base de prueba. El41 se recibe desde main ya integrado; no se incorporan otras ramas de desarrollo.

`./scripts/verify.sh` terminó con salida0: 1097 pruebas backend y 658 frontend en 81 archivos, sin fallos ni omisiones. Compilación .NET sin errores/advertencias; formato, reglas/capas, lint, tipos y build correctos. `./scripts/smoke-postgres.sh` terminó con salida0, `SCAFFOLD AL DÍA` y `SMOKE OK`, incluidos POST/PUT duplicados normalizados, cambio de mayúsculas propio, UTC y rechazo de duplicado por índice real.

VITEST_MAX_WORKERS=1 fue temporal; sin modificar pruebas/timeouts/configuración compartida. Se conservan las capturas y el QA real previos, sin generar imágenes nuevas para esta actualización. Avisos globales conocidos de ESLint9/Vite documentados, sin cambiar dependencias ni umbrales. Propuesta del42 pendiente de aprobación de Lukas; no se aplica SQL a producción ni se declara integración.

## Numeración vigente

El script actual es `database/022-conductores-licencia-unica.sql`. Las verificaciones históricas con011 corresponden al nombre anterior. Preparación local con main actual; propuesta, pendiente de aprobación de Lukas.
