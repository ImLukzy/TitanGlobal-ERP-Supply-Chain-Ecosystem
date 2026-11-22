# Módulo 46: monitoreo de entregas — verificación local

Fecha: 2026-11-21. Responsable: Kelvin Pinto. Rama independiente `kelvin/modulo46-monitoreo-entregas`, desde `origin/main` (`670c37f`). Kelvin autorizó continuar localmente con la propuesta, como los módulos anteriores. **La revisión y aprobación de Lukas siguen pendientes**; no acredita integración, publicación ni despliegue.

## Alcance

Programación manual de un despacho con código único, referencia textual de guía, destinatario, destino y fecha UTC. Salida a ruta, recepción mediante POD textual, cancelación con motivo y consulta del historial. Nueve operaciones autenticadas; filtros/paginación y resumen global. Sin FK ni modificaciones al módulo45, vehículos, conductores, inventarios, correlativos ni entidades congeladas.

Programada → EnRuta → Entregada; Programada/EnRuta → Cancelada. Solo Programada permite editar o eliminar. La edición no cambia el evento inicial. Cada transición y su evento se guardan con una sola llamada a UnitOfWork; el detalle conserva las fechas y el POD. No se garantiza resolución de peticiones concurrentes. El historial de un flujo secuencial permitido tiene como máximo tres eventos, ordenados por Id de registro.

El POD es una constancia manual: receptor, documento, referencia textual y observaciones. No sube archivos, abre enlaces, verifica identidad ni acredita firma o recepción certificada. No hay GPS, mapas, incidencias/reintentos ni ETA automática en este alcance propuesto.

## Resultados

| Comprobación | Resultado |
|---|---|
| SQL y scaffold | `016-monitoreo-entregas.sql` aplicado localmente; entidades y DbContext generados por scaffold |
| Backend propio | 33 casos correctos: 30 de integración y tres de traducción de restricciones; las tablas parametrizadas comprueban ambos literales de obligatoriedad/longitud por campo |
| Backend global | 1116 pruebas correctas; cero omitidas |
| Frontend propio | 40 pruebas correctas en cinco archivos |
| Gate completo y frontend global | verify salida0; .NET sin warnings, formato/capas/reglas, lint y tipos correctos; 583 pruebas frontend en 75 archivos y compilación correctas |
| Smoke PostgreSQL | `SMOKE OK`: CRUD, UTC sin zona, unicidad normalizada, estado antes de validar, salida, POD, cancelación con historial, FK propia, coherencia SQL y cascada |
| Seed idempotente | Dos cargas consecutivas correctas, comprobadas mediante SQL: cuatro entregas y nueve eventos, sin duplicados |
| Estabilidad | Diez repeticiones consecutivas correctas (40/40 cada una), más una correcta mientras compilaba el frontend |
| QA visual | Cuatro escenarios reales: 375/1280 claro/oscuro; 24 capturas del módulo y dos de referencia integrada |

Las pruebas propias cubren todos los mensajes literales y límites, unicidad normalizada en alta/edición, fechas UTC con milisegundos, búsqueda/estado/paginación sin overflow, resumen, estado antes de validar cuerpo, edición inválida sin cambios, eventos iniciales conservados, salida repetida, POD prematuro/inválido/repetido, historial terminal y cancelación desde Programada/EnRuta. Las pruebas defensivas de FK externa futura simulan la excepción de UnitOfWork: no se acredita que exista esa relación externa.

## QA visual ejecutado

Cada escenario realizó alta y edición conservando el instante `2026-11-22T10:30:12.345Z` en un navegador con zona `America/Lima`, baja descartada mediante Escape, salida confirmada, POD obligatorio y recepción completa con tres eventos. Se canceló otro despacho en ruta conservando su salida y sus tres eventos; se eliminó un tercero todavía programado y se limpió la selección.

Se comprobaron Tab/Shift+Tab, foco inicial/primer error, Escape sin escritura y retorno del foco en los diálogos de salida, POD, cancelación y baja. Las referencias de constancia se muestran como texto. Códigos en una línea, selección homogénea y sin desbordamiento de página ni errores JavaScript. Cinco muestras de contraste por escenario alcanzan al menos4.5:1; datos en [resultado-visual.json](resultado-visual.json).

Se compararon superficies, campos, botones, tabla, disposición y tema con Contratos comerciales integrado: [claro](1280-light-referencia-contratos.png) y [oscuro](1280-dark-referencia-contratos.png). No se modificó esa pantalla ni selectores CSS compartidos; las métricas siguen la guía actual de cifra grande y etiqueta debajo.

| Escenario | Detalle | Formulario | Baja | Salida | POD | Cancelación |
|---|---|---|---|---|---|---|
| 375 claro | [Captura](375-light-detalle.png) | [Captura](375-light-formulario.png) | [Captura](375-light-baja.png) | [Captura](375-light-salida.png) | [Captura](375-light-pod.png) | [Captura](375-light-cancelacion.png) |
| 375 oscuro | [Captura](375-dark-detalle.png) | [Captura](375-dark-formulario.png) | [Captura](375-dark-baja.png) | [Captura](375-dark-salida.png) | [Captura](375-dark-pod.png) | [Captura](375-dark-cancelacion.png) |
| 1280 claro | [Captura](1280-light-detalle.png) | [Captura](1280-light-formulario.png) | [Captura](1280-light-baja.png) | [Captura](1280-light-salida.png) | [Captura](1280-light-pod.png) | [Captura](1280-light-cancelacion.png) |
| 1280 oscuro | [Captura](1280-dark-detalle.png) | [Captura](1280-dark-formulario.png) | [Captura](1280-dark-baja.png) | [Captura](1280-dark-salida.png) | [Captura](1280-dark-pod.png) | [Captura](1280-dark-cancelacion.png) |

## Reproducción y salvedades

- Git Bash, .NET10, Node24, Python3 y Docker Linux en PATH. Ejecutar los scripts y comandos de AGENTS.md: levantar db, restaurar herramientas, aplicar SQL, scaffold y seed; después API y frontend locales.
- Gate con `VITEST_MAX_WORKERS=2 ./scripts/verify.sh`, sin modificar configuración, tests ni timeouts existentes. No ejecutar dos smoke simultáneos. La suite propia usa temporizadores falsos para debounce y espera la carga inicial antes de limpiar mocks.
- El compose local no conserva volúmenes: después de retirar el contenedor se aplican nuevamente SQL y seed para QA. No se usa producción.
- npm avisó de la deprecación de ESLint9.39.5, versión ya fijada en la base del proyecto. Vite avisa del bundle compartido superior a500kB (712.90kB en la compilación propia). No se ocultaron avisos ni se cambiaron dependencias o umbrales. No se acredita cero advertencias globales ni cierre formal con la aprobación pendiente.
- Datos y cuentas de demostración locales; ayudantes temporales y registros de autenticación excluidos del commit. Sin push ni PR, sin cambios en main ni otras ramas.
