# Especificación: Pantalla de Modulo41_VehiculosFlota (frontend)

## 1. Objetivo
**Problema:** el backend de vehículos está integrado, pero `/modulos/41-vehiculos-flota` muestra la pantalla pendiente.
**Resultado esperado:** consultar, filtrar, crear, editar y dar de baja vehículos desde una pantalla consistente con Proveedores y Centros de Trabajo.

Responsable: Kelvin Pinto. Contrato: `modulo41-vehiculos-flota-backend.md` §4. Guía: `../equipo/crear-pantalla.md`.

**Estado:** propuesta, pendiente de aprobación de Lukas. Pantalla implementada y verificada localmente con gate, smoke y PostgreSQL real; revisión, integración y despliegue pendientes.

## 2. Fuera de alcance
- Cambios de API, tablas, entidades generadas, autenticación o dependencias.
- Relación entre vehículos y conductores.
- Modificar pantallas ajenas o selectores compartidos de estilos.
- La implementación de unicidad de placas sin distinguir mayúsculas se especifica y verifica por separado en `modulo41-vehiculos-flota-integridad.md`; no queda resuelta solo con validación de pantalla.

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
- [x] Tarea 1: contratos, cliente HTTP y pruebas de las siete operaciones.
- [x] Tarea 2: resumen, filtros, listado, paginación y detalle.
- [x] Tarea 3: formulario, validaciones, edición y baja accesible con sus pruebas.
- [x] Tarea 4: registrar pantalla, pasar el gate, realizar QA y registrar evidencia.

Los commits se agrupan por cambios funcionales revisables, con las pruebas correspondientes; no por archivo ni por una cantidad fija. Se usa la identidad Git de Kelvin, sin trailers de atribución.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-19 | Preparación | Propuesta de spec; implementación pendiente | Contrato del backend y guías del repositorio consultados; sin declarar aprobación ni pruebas ejecutadas |
| 2026-11-19 | A2–A5 | Verificación completa del frontend aprobada: 73 archivos, 582 pruebas | `npm --prefix frontend run verify`, con Node 24.22.1 desde Git Bash; lint y TypeScript aprobados, build correcto con aviso de bundle superior a 500 kB |
| 2026-11-19 | A3–A4 adicional | 39 pruebas del módulo aprobadas, incluidos detalle obsoleto y error de baja | `npm --prefix frontend test -- src/modules/vehiculos-flota --maxWorkers=1` |
| 2026-11-19 | A6 parcial | Capturas de detalle, formulario y baja a 375/1280 px en claro/oscuro; sin desbordamiento de página ni errores JavaScript | `../evidencias/modulo41/README.md`; API simulada, no acredita el seed ni PostgreSQL real |
| 2026-11-19 | A1 bloqueado | El gate se detiene al compilar el proyecto de tests en `GenerateMvcTestManifestTask`: no se puede cargar `System.Private.DataContractSerialization, Version=10.0.0.0` | `./scripts/verify.sh`, repetido tras `dotnet build-server shutdown`, con el mismo resultado; sin cambiar tests, paquetes ni pipeline para eludirlo |
| 2026-11-19 | PostgreSQL pendiente | Docker no está instalado/disponible en esta máquina | No se ejecutaron `aplicar-bd.sh --reiniciar`, scaffold ni smoke; tampoco se modificó el esquema |

| 2026-11-21 | A1–A5 | Aprobados | `./scripts/verify.sh` finaliza correctamente: 1090 pruebas backend, 582 frontend, lint y TypeScript aprobados y compilación .NET con cero errores/advertencias; Vite mantiene su aviso existente de tamaño de bundle |
| 2026-11-21 | PostgreSQL | Aprobado | PostgreSQL 16 en Docker, scripts 001/008/010 aplicados; scaffold ejecutado y `SCAFFOLD AL DÍA`; `./scripts/smoke-postgres.sh` → `SMOKE OK` |
| 2026-11-21 | A6 | Aprobado con datos reales | Sustituidas las capturas simuladas por las de API/PostgreSQL local: 375 y 1280 px, claro y oscuro, detalle, formulario con 409 literal y baja; sin desbordamiento ni errores JavaScript; Escape devuelve el foco. Alta, selección, edición y baja reales verificadas, sin dejar el vehículo de QA |

| 2026-11-22 | Preparación de PR y A1–A5 | Aprobados tras actualizar main | Script renombrado a `021-vehiculos-flota-placa-unica.sql`, base local recreada, seed y scaffold ejecutados sin diferencias de contenido generadas. `verify.sh` salida0:1090backend y582frontend/73archivos. Primer intento con fallo de arranque de un trabajador de Vitest; reintento completo con un trabajador correcto, sin modificar tests ni timeouts. Detalle en evidencia local |
| 2026-11-22 | PostgreSQL tras actualizar main | Aprobado | `smoke-postgres.sh` salida0: `SCAFFOLD AL DÍA` y `SMOKE OK`; script021 aplicado. Se conserva el QA visual real del21 porque este ajuste no cambia la pantalla |
| 2026-11-23 | Resolución del conflicto de estilos del PR #51 | Verificado localmente | `git pull origin main`: conservados íntegros los bloques de Vehículos de flota y Roles/Matriz de permisos; frente a main solo se añaden las48 líneas propias. Tras regenerar por scaffold para corregir finales de línea locales, `verify.sh` salida0:1090backend y619frontend en77archivos; .NET sin errores/advertencias, formato/reglas/lint/tipos/build correctos. `smoke-postgres.sh` salida0, `SCAFFOLD AL DÍA` y `SMOKE OK`. Sin editar modelos generados a mano ni cambiar pruebas/timeouts; detalles en evidencia local |

Los bloqueos del entorno registrados el 19 quedaron resueltos el 21. La implementación y verificación local están completas. Permanece abierta la aprobación de Lukas; no se declara integración ni despliegue.
