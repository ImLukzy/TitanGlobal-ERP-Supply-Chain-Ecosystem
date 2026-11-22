# Módulo 73: sucursales

Implementación local de Kelvin en `kelvin/modulo73-sucursales`, rama independiente desde origin/main actualizado. Propuestas backend/frontend autorizadas para desarrollo local; aprobación formal de Lukas e integración pendientes. Sin push ni PR desde esta sesión.

## Alcance

Maestro plano de filiales, plantas y oficinas con ubicación, contacto, apertura opcional y estado activo/inactivo. País y ciudad son referencias manuales: no se validan catálogos geográficos ni reglas fiscales. Sin jerarquía, asignaciones a usuarios, permisos, almacenes o centros de trabajo; no se modifican módulos ajenos.

Las sucursales se crean activas, se pueden editar en ambos estados y solo se eliminan estando inactivas. Activación/desactivación separadas conservan sus datos y registran UTC del último cambio; repetir un estado produce conflicto sin cambiar fechas. PUT no permite modificar estado ni fechas del sistema. No hay historial completo ni garantía de resolución de escrituras concurrentes.

Apertura vacía se guarda como null; una apertura informada conserva UTC y milisegundos. Los contactos opcionales vacíos se convierten a null. El email tiene validación básica de formato, sin comprobar DNS o entrega.

## SQL y backend

`020-sucursales.sql` aplicado a PostgreSQL local y entidad/contexto regenerados por scaffold, sin editar a mano lo generado. Mapeo Sucursales → Sucursal con su caso de pluralización. Validación de obligatorios, longitudes, tipo, email, UTC y unicidad normalizada; cuatro capas y ocho operaciones HTTP protegidas por JWT.

Pasaron 34 casos propios de backend y 16 casos de pluralización (50 en el filtro conjunto). Incluyen reglas y mensajes literales, límites, errores sin mutación, filtros combinados, paginación, resumen, estados y campos protegidos. Tres casos usan un doble de persistencia para traducción de restricciones; no se acredita una FK real nueva.

`./scripts/smoke-postgres.sh` terminó con salida 0 y `SMOKE OK`: scripts/scaffold coincidentes, seed, JWT/OpenAPI, alta con apertura sin zona convertida a UTC, opcionales null, errores sin cambios, edición que limpia apertura e ignora estado/fechas del sistema, desactivación, filtro activa=false con país/tipo, reactivación y eliminación de inactiva. SQL directo comprobó unicidad normalizada, tipo válido, nombre no vacío, registro obligatorio y longitud de contacto. Se corrigió un identificador de validación del nuevo smoke que comparaba la desactivación correcta con el error de estado repetido, sin cambiar pruebas existentes.

Dos cargas del seed en una base recién creada conservaron exactamente seis sucursales: dos por tipo, cuatro activas y dos inactivas.

## Pantalla y estabilidad

48 pruebas propias correctas: contratos HTTP de ocho operaciones, cuerpos vacíos en cambios de estado, false preservado, filtros/debounce, consultas/paginación, formularios y errores, UTC, activación/desactivación, baja, foco y doble envío. Lint, tipos y compilación independientes correctos.

Pasaron diez repeticiones propias y veinte con build simultáneo, 48 pruebas por ejecución; los veinte builds también terminaron correctamente. Sin omitir pruebas, ampliar timeouts ni cambiar configuración compartida. Se usó temporalmente VITEST_MAX_WORKERS=2.

QA en Edge con API/PostgreSQL reales, a375/1280px en claro/oscuro y zona America/Lima: alta con apertura vacía, edición con `2026-11-23T10:30:12.345Z`, desactivación conservando datos, filtro inactivas/Oficina/Peru, edición de inactiva limpiando contacto, reactivación y baja. Se comprobó igualdad exacta de FechaRegistro y conservación de FechaCambioEstado durante PUT.

Una petición real de desactivación se retuvo brevemente y después continuó hacia la API, sin respuestas simuladas. Se verificaron Tab, Shift+Tab y Escape con todos los controles deshabilitados, además del foco inicial, ciclo de teclado y retorno al disparador en confirmaciones. El recorrido automático se corrigió para localizar los selectores de filtro; no requirió cambios en la pantalla.

Sin errores JavaScript ni desbordamiento de página; códigos en una línea y selección de fila uniforme. Cinco selectores medidos superaron 4.5:1 en los cuatro escenarios: son mediciones puntuales, no una auditoría completa de accesibilidad. Componentes compartidos, tokens y CSS limitado al módulo.

Evidencia: `resultado-visual.json`, veinte capturas propias (detalle, formulario, desactivación, activación y baja por tamaño/tema) y dos referencias de Contratos. Se inspeccionaron la confirmación móvil clara, el formulario móvil oscuro, el detalle de escritorio y Contratos en ambos temas. No se modificó la referencia.

## Comprobación final

`./scripts/verify.sh` terminó con salida 0: 1.118 pruebas de backend y 591 de frontend en 75 archivos, sin fallos ni omisiones. Compilación .NET con cero advertencias; formato, reglas/capas, lint y tipos correctos. Build final: JavaScript709.87kB y CSS137.21kB antes de gzip. El arranque de la suite general fue lento (213.01s en total); terminó correctamente sin modificar pruebas ni tiempos de espera. Se normalizaron los finales de línea de la copia local de arquitectura a LF antes del gate, sin cambios de contenido.

Persisten los avisos globales de ESLint9 obsoleto y paquete de Vite superior a500kB; no se alteraron dependencias ni umbrales para ocultarlos. Los servidores y la base temporal se cerraron y se comprobaron libres los puertos5080,5173 y5433. Main y las ramas anteriores conservaron sus referencias. Se retiraron los auxiliares temporales de comprobación.

La aprobación formal de Lukas y la integración siguen pendientes. Estos resultados acreditan únicamente el desarrollo y las verificaciones locales.
