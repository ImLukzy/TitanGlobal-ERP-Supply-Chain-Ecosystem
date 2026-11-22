# Módulo 50: siniestros de flota

Implementación local de Kelvin en la rama independiente `kelvin/modulo50-siniestros-flota`, creada desde origin/main actualizado. Propuestas de backend/frontend autorizadas para desarrollo local; aprobación formal de Lukas e integración pendientes. Sin push ni PR desde esta sesión.

## Alcance

Registro del incidente → apertura del reclamo → cierre, o anulación desde registro/reclamo. Placa, póliza, aseguradora y número de reclamo son referencias manuales sin modificar módulos ajenos. El cierre registra Aceptado/Rechazado, resolución e importe reconocido; no acredita pago, cobertura, elegibilidad ni vigencia de póliza. No hay asientos, conversiones, adjuntos ni movimientos de inventario. Sin reapertura ni garantía de resolución de escrituras concurrentes.

Cada reclamo conserva una moneda manual, con formato de tres letras sin validar catálogo. El resumen solo cuenta estados; no suma dinero de monedas distintas. El importe reconocido no puede superar el reclamado: un aceptado exige positivo y un rechazado cero. Rango reclamado 0.01–9999999999.99 y reconocido 0–9999999999.99, máximo dos decimales, sin redondear entradas. Respuestas monetarias como texto F2 invariante; sin cálculos de indemnización en el cliente.

## SQL y backend

`019-siniestros-flota.sql` aplicado a PostgreSQL local y entidad/contexto regenerados por scaffold, sin editar a mano lo generado. Mapeo propio SiniestrosFlota → SiniestroFlota con teoría de pluralización. Checks de unicidad normalizada, importes y coherencia de datos/estados.

Pasaron 49 pruebas propias de backend, incluidas tres traducciones defensivas de restricciones, más 16 casos de pluralización: 65 en el filtro conjunto. Cubren mensajes/límites, UTC con milisegundos, unicidad, reclamos, resultados aceptado/rechazado, conservación de referencias y fechas, estados y consultas. No hay una FK nueva: el caso defensivo futuro de referencia se comprueba con un doble de persistencia.

`./scripts/smoke-postgres.sh` terminó con salida 0 y `SMOKE OK`: base local recreada, scripts aplicados y scaffold coincidente; JWT y nueve operaciones OpenAPI, fecha sin zona, precisión máxima 9999999999.99, errores sin mutación, cierre con importes conservados, rechazo 0.00 USD, anulación y baja. SQL directo comprobó unicidad, monto positivo, límite reconocido, aceptación positiva, rechazo cero, presencia de póliza y coherencia del estado.

## Frontend

62 pruebas propias correctas: contratos de nueve operaciones, filtros/debounce con reloj falso, paginación, respuestas antiguas/reintentos, formularios, UTC, validaciones, estados y accesibilidad. Lint y compilación independientes correctos. Se añadió el selector a la gestión de foco del diálogo de cierre y se bloqueó Tab/Shift+Tab cuando no quedan controles habilitados durante el envío, con comprobaciones de regresión.

El primer arranque de trabajadores fue lento (69 s de suite, concentrado en creación del entorno); la repetición pasó en 10.51 s, sin ampliar timeouts, omitir pruebas ni cambiar la configuración compartida. El primer build independiente mostró aviso de tiempo del callback CSS de Vite, además del aviso global de paquete mayor de 500 kB.

## Comprobaciones finales

El seed se cargó dos veces sobre una base recién creada: permanecieron exactamente cinco siniestros, con cuatro estados, resultados aceptado/rechazado y monedas PEN/USD.

Pasaron diez ejecuciones propias y veinte con build simultáneo: 62 pruebas por ejecución, sin omisiones ni ampliación de timeouts; los veinte builds también terminaron correctamente. Se usó temporalmente `VITEST_MAX_WORKERS=2`, sin cambiar configuraciones compartidas.

QA con Edge, API y PostgreSQL reales, a 375/1280 px en claro/oscuro: alta y edición conservando `2026-11-23T10:30:12.345Z` en America/Lima, normalización de placa, apertura del reclamo, rechazo de cierre excesivo, cierre aceptado 1000.25 PEN y rechazado 0.00 USD, anulación conservando referencias/importes y baja. Una petición real de reclamo se retuvo brevemente en el navegador y luego se continuó a la API, sin simular respuestas: permitió comprobar Tab, Shift+Tab y Escape mientras todos los controles estaban deshabilitados.

Se comprobaron foco inicial (incluido el selector), ciclo de teclado y retorno al disparador, fondo uniforme de fila seleccionada y códigos en una línea, sin errores JavaScript ni desbordamiento de página. Los cinco selectores medidos superaron 4.5:1 en cada escenario; son mediciones puntuales, no una auditoría completa de accesibilidad.

Evidencia: `resultado-visual.json`, 24 capturas propias (detalle, formulario, reclamo, cierre, anulación y baja por tamaño/tema) y dos referencias de Contratos. Se inspeccionaron el cierre móvil oscuro, los detalles de escritorio y las referencias integradas en ambos temas. No se modificó la pantalla de referencia.

`./scripts/verify.sh` terminó con salida 0: 1.133 pruebas de backend y 605 de frontend en 75 archivos, sin omisiones; compilación .NET sin warnings, formato, reglas de código/capas, lint y tipos correctos. Build final: JavaScript 716.86 kB y CSS 137.47 kB antes de gzip. Se normalizaron los finales de línea de la copia local de arquitectura a LF antes del gate, sin cambiar contenido ni pruebas existentes.

Persisten los avisos globales de ESLint9 obsoleto y del paquete de Vite superior a 500 kB. No se alteraron dependencias, umbrales ni pruebas para ocultarlos. Aprobación formal de Lukas e integración pendientes: estos resultados acreditan implementación y verificación locales.

Los servidores y la base temporal se cerraron; se comprobaron libres los puertos 5080, 5173 y 5433. Main y las ramas anteriores conservaron sus referencias. No se hizo push ni PR.
