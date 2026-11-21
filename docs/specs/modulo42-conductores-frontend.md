# Especificación: módulo 42 — pantalla de Conductores

## 1. Objetivo
Completar `/modulos/42-conductores` con búsqueda, filtro administrativo de habilitación, paginación, detalle, alta, edición y baja, siguiendo Proveedores, Centros de Trabajo y `crear-pantalla.md`. Responsable: Kelvin Pinto.

Desarrollo local autorizado por Kelvin; revisión de Lukas pendiente. Contrato: `modulo42-conductores-backend.md` §4.

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
- [ ] Contratos, API y pruebas.
- [ ] Listado, filtros y detalle.
- [ ] Formulario, UTC y baja con pruebas.
- [ ] Gate, smoke y QA real, evidencia en §7.

## 7. Registro de verificación
Pendiente; no declarar integración ni despliegue.
