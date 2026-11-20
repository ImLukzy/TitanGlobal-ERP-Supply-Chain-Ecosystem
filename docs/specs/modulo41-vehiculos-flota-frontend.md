# Especificación: Pantalla de Modulo41_VehiculosFlota (frontend)

## 1. Objetivo
**Problema:** el backend de vehículos está integrado, pero `/modulos/41-vehiculos-flota` muestra la pantalla pendiente.
**Resultado esperado:** consultar, filtrar, crear, editar y dar de baja vehículos desde una pantalla consistente con Proveedores y Centros de Trabajo.

Responsable: Kelvin Pinto. Contrato: `modulo41-vehiculos-flota-backend.md` §4. Guía: `../equipo/crear-pantalla.md`.

**Estado:** propuesta pendiente de aprobación de Lukas; no acredita implementación ni QA.

## 2. Fuera de alcance
- Cambios de API, tablas, entidades generadas, autenticación o dependencias.
- Relación entre vehículos y conductores.
- Modificar pantallas ajenas o selectores compartidos de estilos.
- La unicidad de placas sin distinguir mayúsculas, pendiente del backend según el relevo: requiere su propia especificación y script nuevo.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna adicional al contrato existente. Esta spec requiere la revisión previa establecida por el repositorio.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/vehiculos-flota/tipos.ts` | crear | contratos del backend en camelCase, sin `any` |
| `frontend/src/modules/vehiculos-flota/api.ts` | crear | las siete operaciones del backend usando el cliente compartido |
| `frontend/src/modules/vehiculos-flota/VehiculosFlotaPage.tsx` | crear | estados, coordinación de consultas y mutaciones, `PaginaModulo` |
| `frontend/src/modules/vehiculos-flota/VehiculosFlotaResumen.tsx` | crear | métricas y distribución por tipo |
| `frontend/src/modules/vehiculos-flota/VehiculosFlotaFiltros.tsx` | crear | búsqueda, tipo y estado |
| `frontend/src/modules/vehiculos-flota/VehiculosFlotaTabla.tsx` | crear | `TablaPaginada`, selección y acciones |
| `frontend/src/modules/vehiculos-flota/VehiculoFlotaDetallePanel.tsx` | crear | `PanelDetalle`, todos los campos |
| `frontend/src/modules/vehiculos-flota/VehiculoFlotaFormulario.tsx` | crear | alta y edición con `CampoConError` |
| `frontend/src/modules/vehiculos-flota/validacion.ts` | crear | validaciones locales con los mensajes del backend §4 |
| `frontend/src/modules/vehiculos-flota/presentacion.ts` | crear | etiquetas de tipos y formato de capacidad |
| `frontend/src/modules/vehiculos-flota/api.test.ts` | crear | URL, query, método y cuerpo exactos |
| `frontend/src/modules/vehiculos-flota/VehiculosFlotaPage.test.tsx` | crear | consultas, filtros, errores y mutaciones |
| `frontend/src/modules/vehiculos-flota/VehiculoFlotaFormulario.test.tsx` | crear | límites y mensajes literales |
| `frontend/src/modules/vehiculos-flota/vehiculosFlota.testUtils.tsx` | crear | datos y respuestas HTTP simuladas |
| `frontend/src/modules/pantallas.ts` | modificar | registrar `41-vehiculos-flota` sin reordenar otros módulos |
| `frontend/src/styles/app.css` | modificar si hace falta | bloque al final con prefijo `.vehiculos-flota-` |

## 4. Diseño y lógica
- **Cliente HTTP:** `listarVehiculosFlota`, `obtenerVehiculoFlota`, `crearVehiculoFlota`, `actualizarVehiculoFlota`, `eliminarVehiculoFlota`, `listarTiposVehiculoFlota` y `obtenerResumenVehiculosFlota`. Rutas bajo `/vehiculos-flota`; omitir `''` y `undefined` de la query, conservar `false`. Autenticación y errores mediante `../../api/client`.
- **Carga inicial:** listado con página 1 y tamaño 10, tipos y resumen. Una sola petición de listado al montar; ignorar respuestas obsoletas cuando cambien los filtros o la selección.
- **Resumen:** total, activos, inactivos, capacidad total en kg y distribución por tipo, conforme a `ResumenVehiculosFlotaDto`. La capacidad total conserva el significado del backend, sin recalcular solo los activos.
- **Filtros:** búsqueda por placa, marca o modelo con espera de 300 ms; tipo del endpoint `/tipos`; estado Todos, Activos e Inactivos. Cambiar filtros vuelve a página 1. Mostrar Camión y Furgón conservando los valores `Camion` y `Furgon` en el contrato.
- **Tabla:** placa, marca/modelo, año, tipo, capacidad en kg y estado con texto. Paginación de 10 mediante `TablaPaginada`. Estados de carga, vacío y error con posibilidad de reintentar.
- **Detalle:** placa, marca, modelo, año, tipo, capacidad, activo y observaciones. Observaciones ausentes: «Sin observaciones». Selección visible y accesible; ficha lateral en escritorio y apilada en móvil.
- **Formulario:** placa (20), marca (100), modelo (100), año entero entre 1900 y el año actual más uno, tipo (`Camion`, `Furgon`, `Moto`, `Otro`), capacidad positiva de hasta 9999999999999999.99 y dos decimales, activo y observaciones (500). Alta con activo marcado. Recortar textos como el backend y conservar la placa escrita sin introducir una normalización de mayúsculas nueva. Validación en el orden y con los mensajes literales de la spec backend §4; validar los límites decimales desde el texto antes de convertirlo al tipo numérico del contrato.
- **Errores:** conservar lo escrito y mostrar el mensaje devuelto por la API en 400/409; no sustituir «Ya existe un vehículo con la placa {placa}.» por un mensaje genérico. Evitar envíos duplicados mientras se guarda.
- **Baja:** confirmar mediante `DialogoBaja` con la placa del vehículo; Escape cancela y devuelve el foco. Tras una mutación correcta refrescar lista, tipos y resumen; actualizar o limpiar detalle según corresponda y ajustar la página si queda vacía.
- **Accesibilidad y diseño:** etiquetas asociadas, errores por campo, navegación por teclado, estados anunciados y componentes compartidos. Variables de `tokens.css`, sin colores fijos ni cambios globales. A 375 px se apila el contenido; a 1280 px no se parten las placas. El scroll horizontal, si fuera necesario, queda dentro de la región accesible de la tabla.

## 5. Criterios de aceptación
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` desde Git Bash | en verde, cero warnings |
| A2 | Contrato HTTP | `api.test.ts`: siete operaciones, query sin vacíos y `activo=false`, cuerpos y errores | pasan |
| A3 | Listado | tests de carga única, búsqueda con temporizadores falsos, filtros, vuelta a página 1, paginación, vacío, error y respuestas obsoletas | pasan |
| A4 | Mutaciones | tests de detalle, alta, edición, cada validación local, 400/409 conservando valores, baja y devolución del foco | pasan |
| A5 | Integración | ruta registrada, pruebas existentes conservadas y verificación completa del frontend | en verde |
| A6 | QA visual | PostgreSQL local; capturas a 375 y 1280 px, claro y oscuro, con detalle, formulario y diálogo de baja | sin desbordamiento de página ni contenido inaccesible; evidencia en §7 |

## 6. Checklist de ejecución
- [ ] Tarea 0: aprobación de Lukas de esta spec.
- [ ] Tarea 1: contratos, cliente HTTP y pruebas de las siete operaciones.
- [ ] Tarea 2: resumen, filtros, listado, paginación y detalle.
- [ ] Tarea 3: formulario, validaciones, edición y baja accesible con sus pruebas.
- [ ] Tarea 4: registrar pantalla, pasar el gate, realizar QA y registrar evidencia.

Los commits se agrupan por cambios funcionales revisables, con las pruebas correspondientes; no por archivo ni por una cantidad fija. Se usa la identidad Git de Kelvin, sin trailers de atribución.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-19 | Preparación | Propuesta de spec; implementación pendiente | Contrato del backend y guías del repositorio consultados; sin declarar aprobación ni pruebas ejecutadas |
