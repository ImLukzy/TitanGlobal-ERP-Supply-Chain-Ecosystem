# Módulo 47: control de combustible

Rama independiente: `kelvin/modulo47-control-combustible`. Trabajo local de Kelvin; revisión de Lukas e integración pendientes. No se ha enviado ni integrado desde esta sesión.

## Alcance comprobable

Vales con estados Borrador, Confirmado y Anulado; alta, consulta, edición y baja de borradores, confirmación con intervalo completo y anulación con motivo. La anulación conserva importes, intervalo y confirmación previa. Cada importe conserva su moneda: el resumen solo cuenta estados.

El rendimiento es orientativo y corresponde al intervalo de odómetro declarado manualmente. No acredita consumo físico real ni método de tanque lleno. Placa y moneda son referencias de texto; no se incorporan dependencias sobre los módulos 41, 74 o 76, movimientos de inventario, contabilidad ni control de concurrencia.

## Precisión y persistencia

SQL `017-control-combustible.sql`, aplicado mediante los scripts del proyecto. Entidad y contexto regenerados con scaffold. Se añadió el mapeo de pluralización `ValesCombustible` → `ValeCombustible` para que el generador produzca el nombre correcto; no se editaron a mano los archivos generados.

El servidor calcula con `decimal`; la previsualización usa enteros `BigInt` escalados. Litros y precio admiten tres decimales; el importe se redondea a dos y los rendimientos a tres, con mitades hacia fuera de cero. Los resultados viajan como texto decimal.

| Caso | Resultado esperado |
| --- | --- |
| 50 L × 5.125 PEN | 256.25 PEN |
| Odómetro 12000 → 12500 con 50 L | 500 km; 10.000 km/L; 10.000 L/100 km |
| Odómetro final pendiente | Distancia y rendimientos nulos; no permite confirmar |
| 9999999.999 L × 9999999.999 | 99999999980000.00 |
| Entrada con decimales no representables | Error 400 antes de guardar, sin redondeo silencioso |
| Fecha sin zona con milisegundos | Normalizada a UTC sin perder .345 |
| Confirmación seguida de anulación | Conserva fecha de confirmación y cálculos |

No hay claves foráneas nuevas. La traducción defensiva de una futura restricción de referencia se prueba con un doble de persistencia; no se presenta como una restricción existente en PostgreSQL.

## Verificación local

`./scripts/verify.sh` terminó con salida 0: compilación .NET sin warnings, formato, reglas de código y capas, 1.133 pruebas de backend y 607 pruebas de frontend en 76 archivos, lint y comprobación de tipos correctos. El gate se repitió completo sobre la versión final corregida, con los mismos resultados. Build final de Vite: 714.32 kB de JavaScript y 137.06 kB de CSS, antes de gzip.

`./scripts/smoke-postgres.sh` terminó con salida 0 y `SMOKE OK`: recreación de la base local, aplicación del SQL y comparación del scaffold, JWT, ocho operaciones OpenAPI, seed con tres estados y dos monedas, máximos decimales, fecha sin zona, intervalo pendiente, errores sin modificar datos, confirmación sin cuerpo, anulación y baja. También se verificaron en SQL la unicidad normalizada y las restricciones de litros, intervalo y coherencia del estado.

El módulo añade 49 casos de backend (incluidas las traducciones defensivas de restricciones), un caso de pluralización y 64 pruebas de frontend. El filtro de backend junto con pluralización pasó 65 casos. Las diez repeticiones iniciales de la suite propia del frontend y una ejecución simultánea con build terminaron correctamente, sin omisiones ni ampliación de timeouts. Se limitó temporalmente Vitest a dos trabajadores con `VITEST_MAX_WORKERS=2`, sin cambiar su configuración en el repositorio.

El seed se cargó dos veces sobre la base local recién creada: permanecieron exactamente tres vales. La revisión visual con Edge, API y PostgreSQL reales pasó los cuatro escenarios (375/1280 px, claro/oscuro), sin mocks de respuestas. Se preservó `2026-11-22T10:30:12.345Z` usando el navegador en America/Lima.

Se comprobaron alta, edición, rechazo por intervalo pendiente, confirmación, anulación con conservación de datos y baja de otro borrador. Los diálogos retienen Tab/Shift+Tab, Escape no escribe y el foco vuelve al disparador. No hubo errores JavaScript ni desbordamiento de página; códigos en una línea y fondo uniforme en la fila seleccionada. Los cinco selectores medidos superaron 4.5:1 en los cuatro escenarios. Son comprobaciones puntuales, no una auditoría completa de accesibilidad.

Evidencia: `resultado-visual.json`, veinte capturas propias (`detalle`, `formulario`, `baja`, `confirmacion`, `anulacion` por tamaño/tema) y dos capturas de referencia de Contratos a 1280 px. Se inspeccionaron visualmente el formulario móvil, los detalles de escritorio en ambos temas y el diálogo móvil oscuro, junto con las referencias integradas. La tabla usa desplazamiento interno para conservar importes y fechas en una línea.

La revisión real descubrió pérdida de foco cuando se deshabilitaba el botón de confirmación: tras un 409, Escape llegaba al documento en lugar del diálogo. Se corrigió enfocando el contenedor durante el envío y permitiendo Shift+Tab desde él; las pruebas nuevas conservan las aserciones de bloqueo y cierre después del rechazo. La revisión se repitió correctamente. El guion visual también se ajustó para esperar el rendimiento recargado tras editar, sin ampliar tiempos de espera ni omitir comprobaciones.

El smoke se ejecutó antes de este ajuste exclusivo del frontend. No cambió el backend, el esquema ni sus comprobaciones; se repitieron el QA real y la verificación del frontend sobre la corrección.

Sobre la versión corregida pasaron otras diez ejecuciones de la suite propia y veinte ejecuciones con build simultáneo: 64 pruebas por ejecución, sin omisiones ni timeouts ampliados. Los veinte builds simultáneos también finalizaron con salida 0. El lint final pasó sin advertencias.

Los servidores locales y la base temporal se cerraron al finalizar el QA. Se comprobaron libres los puertos 5080, 5173 y 5433. Main y las ramas anteriores de Kelvin conservaron sus referencias; no se hizo push, PR ni integración.

## Observaciones

Las especificaciones son propuestas locales autorizadas por Kelvin; su aprobación formal por Lukas sigue pendiente. El proyecto conserva los avisos globales de ESLint obsoleto y del tamaño del paquete de Vite; no se cambiaron dependencias, umbrales ni configuraciones compartidas para ocultarlos.

Durante la preparación, ejecutar pruebas a la vez que el formateador produjo dos errores de arranque de trabajadores. La ejecución aislada pasó. También se corrigió una comprobación nueva para esperar la recarga de datos tras anular, sin eliminar aserciones ni ampliar tiempos de espera. El primer build independiente mostró un aviso de tiempo del callback CSS de Vite.
