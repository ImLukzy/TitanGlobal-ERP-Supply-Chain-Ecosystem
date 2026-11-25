# Especificación propuesta: módulo 48 — Transportistas terceros (frontend)

**Estado:** aprobada por Lukas el 2026-11-23, integrada en `main` por el PR #63 y desplegada en la demo.

## 1. Objetivo
Sustituir la pantalla pendiente de `/modulos/48-transportistas-terceros` por un maestro de transportistas y un registro de fletes, siguiendo Proveedores, Centros de Trabajo y las guías del equipo.

Responsable: Kelvin Pinto. **Propuesta, pendiente de aprobación de Lukas; revisión de Lukas y aprobación del contrato backend pendientes** en `modulo48-transportistas-terceros-backend.md`. Evidencia de ejecución local en `docs/evidencias/modulo48/`.

## 2. Fuera de alcance y decisiones
No modificar pantallas ajenas, autenticación, middleware, pipeline ni dependencias. No calcular tarifas, impuestos o conversiones de moneda; no generar facturas ni pagos. El alcance de fletes manuales, la identificación fiscal por país y la moneda declarativa dependen de las decisiones de la spec backend §2.

## 3. Archivos afectados
- `frontend/src/modules/transportistas-terceros/tipos.ts` y `api.ts`: DTOs y las diez operaciones propuestas.
- `TransportistasTercerosPage.tsx`: página y pestañas mediante `?vista=transportistas|fletes`.
- `TransportistasFiltros.tsx`, `TransportistasResumen.tsx`, `TransportistasTabla.tsx`, `TransportistaDetallePanel.tsx`, `TransportistaFormulario.tsx`.
- `FletesFiltros.tsx`, `FletesTabla.tsx`, `FleteDetallePanel.tsx`, `FleteFormulario.tsx`, `DialogoCancelarFlete.tsx`.
- `validacion.ts`, `presentacion.ts`, pruebas de API/página/formularios/diálogos y utilidades de pruebas en esa carpeta.
- `frontend/src/modules/pantallas.ts`: solo registrar `48-transportistas-terceros`.
- `frontend/src/styles/app.css`: bloque al final con prefijo `.transportistas-terceros-`, sin cambiar selectores compartidos.
- `docs/evidencias/modulo48/`: evidencia real, únicamente después de ejecutar QA.

Los archivos de componentes se mantienen separados y legibles, evitando una página monolítica. No se introduce un CRUD genérico ni una arquitectura diferente.

## 4. Diseño y lógica propuestos
- `PaginaModulo`, `TablaPaginada`, `PanelDetalle`, `CampoConError` y `DialogoBaja` para el maestro. Pestañas navegables con teclado y URL; cambiar vista no publica ni modifica registros.
- **Transportistas:** resumen total/activos/inactivos y por servicio; búsqueda con espera de 300 ms por razón social/documento; filtros país, servicio y activo; página 1 al cambiar filtros, tamaño 10. Tabla con identificación, país, servicio y estado. Selección con detalle lateral en escritorio y apilado en móvil.
- **Formulario maestro:** campos y límites exactos del backend; servicio selector 3PL/Flete/Ambos; activo marcado al crear. Errores por campo con los literales aprobados. Guardar Trim y opcionales vacíos omitidos. 400/409 visibles sin perder valores. Baja con confirmación accesible; si hay fletes, mostrar el 409 y permitir usar la edición para desactivar.
- **Fletes:** búsqueda por referencia/origen/destino, filtros transportista y estado, orden y paginación del backend. Mostrar referencia, transportista, fecha UTC, origen/destino, importe decimal textual exacto con su moneda y estado; sin convertir la respuesta a Number. No sumar importes de monedas distintas.
- **Registro de flete:** selector de transportista activo con búsqueda paginada usando el listado existente; no descargar toda la tabla ni ocultar silenciosamente transportistas después de los primeros cien. Campos referencia, fecha, origen, destino, importe, moneda y observaciones. Validar decimales desde el texto antes de convertir al contrato numérico; informar si una cantidad no puede representarse con precisión. Fecha visible como yyyy-MM-dd, envío con Z y formato compartido UTC.
- **Detalle de flete:** todos los campos; si está cancelado, mostrar motivo y fecha de cancelación. No ofrecer edición o eliminación de un flete ya registrado.
- **Cancelar flete:** diálogo propio del módulo, con el patrón accesible de DialogoBaja, motivo obligatorio mediante CampoConError y botón «Cancelar flete». No reutilizar el texto «Eliminar» para una cancelación. Escape cierra, foco atrapado durante apertura y devuelto al botón de origen; error conserva motivo. Un cancelado muestra su estado y no permite una segunda cancelación.
- Carga única del listado al montar; ignorar respuestas antiguas al cambiar filtros/selección. Impedir envíos repetidos; refrescar listas, resumen y detalle después de guardar, eliminar o cancelar, y corregir la última página si queda vacía.
- Estados de carga, vacío, error y reintento. Foco en el primer campo del formulario y primer error al validar; fechas por UTC. Variables de tokens.css, texto de estado junto al color y región accesible de scroll de tabla en móvil.

## 5. Criterios de aceptación
| # | Criterio | Cómo se verifica |
|---|---|---|
| A1 | Gate | verify en verde, sin errores ni advertencias de compilación |
| A2 | API | URLs, queries sin vacíos conservando false, métodos/cuerpos exactos, mensajes literales |
| A3 | Consultas | espera 300 ms con temporizadores falsos, filtros, paginación, retorno a página 1, estados vacíos/error, URL de pestañas y respuestas obsoletas |
| A4 | Maestro | validaciones/límites, alta/edición/baja y 409 de fletes históricos sin perder datos |
| A5 | Fletes | registro UTC, importe/moneda sin conversión, cancelación con motivo, 400/409 y foco/Escape; sin controles de edición/borrado |
| A6 | QA real | 375/1280 px claro/oscuro con API/PostgreSQL locales, detalle/formularios/diálogos; sin desbordamiento de página y capturas adjuntas |

Las pruebas existentes se conservan. Las capturas simuladas, si se usan durante desarrollo, no sustituyen la evidencia con PostgreSQL real exigida para cerrar.

## 6. Checklist de ejecución
- [ ] Lukas aprueba las specs y decisiones de producto del backend.
- [x] Backend y base local disponibles para las comprobaciones.
- [x] Contratos/cliente con pruebas.
- [x] Maestro con validación y baja.
- [x] Registro/consulta/cancelación de fletes.
- [x] Registro de ruta, gate, smoke y QA real; evidencia en §7.

## 7. Registro de verificación
| Fecha | Estado | Evidencia |
|---|---|---|
| 2026-11-21 | Propuesta; implementación y pruebas pendientes | Contrato backend redactado para revisión y patrón de componentes contrastado con las pantallas existentes |
| 2026-11-21 | Implementación local y QA real ejecutados; revisión de Lukas pendiente | Pruebas de API, validaciones y pantalla; cuatro escenarios 375/1280 claro/oscuro y 16 capturas con PostgreSQL real en `docs/evidencias/modulo48/` |
| 2026-11-23 | Integración y producción | Spec aprobada por Lukas. `012` aplicado en la demo con copia y ensayo (6 scripts registrados, filas de las 41 tablas existentes sin cambios). PR #63 fusionado con merge commit (`b95dfc7`); gate de GitHub y local, smoke, Vercel y Render correctos; en producción la pantalla está publicada y `/api/v1/transportistas-terceros` y `/api/v1/fletes-terceros` responden 200 |

### Preparación de revisión — 2026-11-22

Rama actualizada con `git pull origin main`, conservando la tabla oficial de relevo y modificando solo la línea de Kelvin y la reserva propia. Script vigente: `database/012-transportistas-terceros.sql`. Propuesta, pendiente de aprobación de Lukas. Base PostgreSQL local recreada, seed cargado y entidades/contexto regenerados mediante scaffold, sin edición manual ni diferencias de contenido.

`./scripts/verify.sh`: salida0, 1138 pruebas backend y 580 frontend en 73 archivos. `./scripts/smoke-postgres.sh`: salida0, scaffold coincidente y SMOKE OK. Se usó VITEST_MAX_WORKERS=1 temporal, sin modificar pruebas, timeouts ni configuración compartida. Persisten los avisos globales de ESLint9/Vite; no se cambiaron dependencias ni umbrales.

No se generaron capturas nuevas ni se cambió la pantalla para estos ajustes. Se conserva la evidencia visual previa. Preparación local: sin nuevos PR, sin push y sin integración. Tras cada fusión anterior habrá que incorporar main y repetir estas verificaciones antes del PR correspondiente.

### Actualización para revisión — 2026-11-23

- Rama actualizada con `origin/main` (0b0324ff0863a4b5ab6c977e649c7840eb199caa) después de integrar el PR #59 del módulo 42.
- `relevo.md` idéntico a main. Se conservaron las pantallas y ambos bloques de CSS, además del seed de evaluaciones de proveedores; contexto regenerado con el scaffold oficial desde la base combinada.
- Base local reiniciada, seis scripts aplicados y seed cargado. Script propio: `012-transportistas-terceros.sql`; aplicación en la demo pendiente de Lukas.
- `./scripts/verify.sh`: exit 0; 1191 pruebas backend, 695 pruebas frontend en 84 archivos; .NET con cero warnings, lint, tipos y build correctos.
- `./scripts/smoke-postgres.sh`: exit 0, `SCAFFOLD AL DÍA` y `SMOKE OK`, incluidos los flujos del módulo 48 y los módulos existentes.
- El frontend conserva los avisos existentes de obsolescencia de ESLint 9 y tamaño del bundle de Vite. La evidencia visual del 2026-11-21 sigue disponible; esta actualización conserva el bloque visual del 48.
- Propuesta, pendiente de aprobación de Lukas. No se han aplicado scripts contra producción.
