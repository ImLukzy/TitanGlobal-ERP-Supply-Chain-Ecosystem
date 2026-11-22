# Módulo 49: logística inversa

Implementación local de Kelvin, rama independiente `kelvin/modulo49-logistica-inversa` desde origin/main actualizado. Propuestas de backend y frontend pendientes de aprobación formal de Lukas. Sin publicación, PR ni integración desde esta sesión.

## Alcance

Un material y una unidad por retorno; tipos Pallet, Retornable y DevolucionRuta. Programación → recogida → recepción, y cancelación desde programación o recogida. Cantidades positivas con tres decimales; la recogida no puede superar lo programado ni la recepción lo recogido. Las diferencias quedan explícitas: por ejemplo, 10.125 programados, 9.125 recogidos y 8.001 recibidos producen 1.000 no recogidos y 1.124 no recibidos.

La cancelación conserva la recogida previa. Los estados terminales no se editan ni reabren. No hay movimientos de inventario, devolución comercial automática ni cambios sobre módulos ajenos. Referencias y unidades manuales: no implican existencia de una guía, cliente o almacén ni un saldo de inventario. El resumen cuenta estados sin sumar cantidades heterogéneas. No se garantiza resolución de escrituras concurrentes.

## SQL y backend

Script nuevo `018-logistica-inversa.sql`, aplicado a PostgreSQL local. Entidad y contexto regenerados mediante scaffold, sin edición manual del código generado. Mapeo propio RetornosLogisticos → RetornoLogistico con prueba de pluralización. SQL comprueba unicidad normalizada, cantidades y coherencia de estados/datos.

Pasaron 46 pruebas propias de backend (43 de integración y tres de traducción defensiva de restricciones), junto con las 16 de pluralización: 62 en el filtro conjunto. Se cubren mensajes, límites, milisegundos UTC, precisión extrema 999999999.999, diferencias, datos inválidos sin mutación, estados y consultas. No hay una FK nueva: su traducción defensiva futura se comprueba mediante un doble de persistencia.

## Frontend

48 pruebas propias: contratos HTTP de las nueve operaciones, filtros/debounce con reloj falso, paginación, respuestas obsoletas, reintentos, formularios, UTC, límites, estados, foco, doble envío y baja. Lint sin advertencias. Cantidades de respuesta como cadenas; conversión numérica solo de entradas previamente validadas, sin calcular diferencias en coma flotante.

Durante la preparación se corrigieron incompatibilidades con los contratos reales de componentes/cliente HTTP. Dos fallos de las pruebas nuevas procedían de reutilizar una Response consumida y de encontrar el texto del textarea antes del detalle recargado; se usan respuestas independientes y se espera la ficha, sin debilitar las aserciones ni ampliar timeouts.

## Comprobaciones finales

`./scripts/smoke-postgres.sh` terminó con salida 0 y `SMOKE OK`: recreación local, aplicación del esquema y comprobación del scaffold, autenticación/OpenAPI, precisión máxima, UTC sin zona, datos inválidos sin cambios, recogida/recepción con diferencias, estados terminales y cancelación conservando recogida. También se comprobaron directamente en SQL la unicidad y los límites/coherencia del estado. El seed se cargó dos veces sobre una base recién creada y permanecieron exactamente cuatro retornos.

La revisión real con Edge, API y PostgreSQL pasó a 375 y 1280 px en claro/oscuro: alta, edición UTC con milisegundos en America/Lima, rechazo de recogida excesiva, recogida, recepción, cancelación con recogida preservada y baja de un programado. Se hizo un ajuste visual del límite del diálogo y se repitieron correctamente los cuatro escenarios con las capturas de tabla al inicio; no cambia lógica ni contratos.

Se comprobaron Tab/Shift+Tab, Escape sin escrituras y retorno de foco, selección uniforme, códigos en una línea, ausencia de errores JavaScript y de desbordamiento de página. Los cinco selectores medidos superaron 4.5:1 en todos los escenarios; son mediciones puntuales, no una auditoría completa de accesibilidad. La tabla y las cantidades largas usan desplazamiento interno.

`resultado-visual.json` registra los cuatro escenarios. Hay 24 capturas propias (detalle, formulario, recogida, recepción, cancelación y baja por tamaño/tema) y dos referencias de Contratos. Se inspeccionaron el formulario y diálogo móviles, los detalles de escritorio y las referencias de Contratos en ambos temas. No se modificó la pantalla de referencia.

Pasaron diez ejecuciones de la suite propia y veinte con build simultáneo: 48 pruebas en cada una, sin omisiones ni ampliación de timeouts. Los veinte builds también terminaron con salida 0. Se usó temporalmente `VITEST_MAX_WORKERS=2`, sin modificar configuraciones compartidas. El ajuste posterior de presentación se comprobó con el QA repetido y los builds siguientes; no altera las reglas ni los contratos.

El primer gate compiló sin warnings y superó formato/reglas, pero una prueba existente del catálogo falló al conservar `\r` en los nombres leídos de `docs/arquitectura.md` (1 fallo, 1.129 pruebas correctas). Se normalizaron únicamente los finales de línea de la copia local a LF, sin cambiar el contenido ni la prueba.

La repetición completa de `./scripts/verify.sh` terminó con salida 0: 1.130 pruebas de backend y 591 de frontend en 75 archivos, sin omisiones; compilación .NET sin warnings, formato, reglas de código/capas, lint y tipos correctos. Build final: JavaScript 714.69 kB y CSS 137.25 kB antes de gzip.

Persisten los avisos globales de ESLint9 obsoleto y del paquete de Vite superior a 500 kB. No se cambiaron dependencias ni umbrales para ocultarlos. La aprobación formal de Lukas e integración siguen pendientes; estos resultados acreditan implementación y verificación locales.

Los servicios de prueba se cerraron y se comprobaron libres los puertos 5080, 5173 y 5433. Main y las ramas anteriores conservaron sus referencias. No se hizo push ni PR.
