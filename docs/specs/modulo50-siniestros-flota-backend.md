# Propuesta: módulo 50 — Siniestros de flota (backend)

## 1. Objetivo
Kelvin Pinto, según asignaciones. Orden 41 → 42 → 48 → 43 → 44 → 45 → 46 → 47 → 49 → **50** → 73. El enunciado pide «incidentes, pólizas, reclamos». Registrar un incidente, abrir un reclamo manual con referencia de póliza y registrar su resultado o anulación, conservando cada etapa.

Continuación local autorizada por Kelvin como sus módulos anteriores. La aprobación de Lukas permanece pendiente; no acreditar integración ni despliegue. Rama independiente `kelvin/modulo50-siniestros-flota` desde origin/main actualizado, sin incorporar las otras ramas ni publicar.

## 2. Fuera de alcance
Placa, póliza, aseguradora y número de reclamo son referencias manuales de texto: sin FK a41/42/74/76 ni cambios en tablas ajenas. No valida existencia/vigencia/cobertura de pólizas, responsabilidad, deducibles ni elegibilidad del reclamo. No emite documentos, pagos, asientos ni movimientos de inventario. El importe reconocido no acredita un pago. Sin adjuntos, enlaces externos, reapertura, múltiples reclamos por incidente, historial de ediciones ni garantía de resolución de escrituras concurrentes.

Una moneda manual por reclamo (formato de tres letras, sin validar catálogo). Importes con dos decimales por contrato local, sin conversión de monedas ni sumas monetarias en el resumen. Solo Codigo es único; no imponer unicidad global a una referencia de reclamo de aseguradoras distintas.

## 3. Archivos
`database/019-siniestros-flota.sql`; `SiniestroFlota.cs` y DbContext generados solo por scaffold; mapeo propio de pluralización y su teoría. DTOs y UseCases en `Modulo50_SiniestrosFlota`; controlador `SiniestrosFlotaController.cs`; pruebas en `tests/TitanGlobal.Api.Tests/Modulo50_SiniestrosFlota/`. Bloques propios al final de seed y smoke, specs, asignaciones, relevo y `docs/evidencias/modulo50/`. Reservas010–018 preservadas como ramas independientes, sin implicar integración.

## 4. Diseño y lógica
Tabla SiniestrosFlota: Id identity; Codigo20, Placa20 (mayúsculas), Lugar200, Descripcion500, Tipo20 (Accidente/Robo/DanoMaterial), FechaIncidente timestamptz, Observaciones500 nullable; Estado15 default Registrado. Codigo único por lower(btrim), textos requeridos recortados/no vacíos.

Datos de reclamo nullable hasta abrirlo: NumeroPoliza40, Aseguradora150, NumeroReclamo40, Moneda3 (ASCII mayúsculas), MontoReclamado numeric(12,2), FechaReclamo timestamptz. Datos de cierre nullable: Resultado15 (Aceptado/Rechazado), MontoReconocido numeric(12,2), Resolucion500, FechaCierre timestamptz. Anulación: MotivoAnulacion500 y FechaAnulacion timestamptz nullable. Checks SQL para presencia conjunta y coherencia de datos/estados.

Registrado → EnReclamo → Cerrado; Registrado/EnReclamo → Anulado. Solo Registrado admite edición/baja. Reclamar exige póliza, aseguradora, referencia, moneda y monto positivo. Cerrar exige resultado, importe reconocido entre0 y el reclamado y resolución: Aceptado reconoce>0; Rechazado reconoce0. Cierre/anulación preservan datos del reclamo; terminales solo lectura. No se calcula indemnización automáticamente. Fechas de transición del servidor UTC.

Application valida todo antes de modificar; textos recortados, opcionales vacíos→null; placa/moneda mayúsculas, enums sin distinguir mayúsculas. FechaIncidente DateTime? obligatoria/default inválido, normalizada con FechasUtc; entrada sin zona se interpreta UTC conservando milisegundos. Sin comparación con hoy. MontoReclamado0.01–9999999999.99; reconocido0–9999999999.99, ambos máximo2 decimales, sin redondear entradas. Respuestas monetarias como texto F2 invariante. Cuatro capas, tipos explícitos, miembros con bloques, lambdas junto al operador y excepciones/CodigosEstado de Application.

### API JWT
Prefijo `/api/v1/siniestros-flota`; GET listado (busqueda en código/placa/lugar, estado/tipo opcionales, pagina1/tamanoPagina20 máximo100; orden Codigo/Id sin overflow); GET /{id}; POST / y PUT /{id} `{codigo,placa,lugar,descripcion,tipo,fechaIncidente,observaciones?}`; DELETE /{id}204; POST /{id}/reclamo `{numeroPoliza,aseguradora,numeroReclamo,moneda,montoReclamado}`; POST /{id}/cierre `{resultado,montoReconocido,resolucion}`; POST /{id}/anulacion `{motivo}`; GET /resumen `{total,registrados,enReclamo,cerrados,anulados}`. Escrituras200 `{id}`. Todas las transiciones tienen cuerpo; sin endpoints públicos nuevos.

Página `{items,total,pagina,tamanoPagina}`. Item datos editables salvo observaciones, más id/estado. Detalle añade observaciones, campos/fechas del reclamo, cierre y anulación. Nunca exponer entidades ni sumar monedas diferentes.

### Validaciones literales y orden
Buscar primero404 `El siniestro {id} no existe.` en edición/baja/transición; después estado409; después datos400. Alta/edición: Codigo/código20, Placa/placa20, Lugar/lugar200, Descripcion/descripción500, en ese orden: `El campo {etiqueta} es obligatorio.` / `El campo {etiqueta} no puede superar {maximo} caracteres.`. Después: `El tipo debe ser Accidente, Robo o DanoMaterial.`; `La fecha del incidente es obligatoria.`; `Las observaciones no pueden superar 500 caracteres.`. Al final duplicado409 `Ya existe un siniestro con el código {codigo}.`, también al traducir Unicidad al guardar.

Reclamo: mismos errores de texto para NumeroPoliza/número de póliza40, Aseguradora/aseguradora150, NumeroReclamo/número de reclamo40; después `La moneda debe contener 3 letras mayúsculas.`; `El monto reclamado debe estar entre 0.01 y 9999999999.99.`; `El monto reclamado no puede tener más de 2 decimales.`.

Cierre, en orden: `El resultado debe ser Aceptado o Rechazado.`; `El monto reconocido debe estar entre 0 y 9999999999.99.`; `El monto reconocido no puede tener más de 2 decimales.`; `El monto reconocido no puede superar el reclamado.`; `Un reclamo aceptado debe reconocer un importe mayor que cero.` / `Un reclamo rechazado debe reconocer un importe de cero.`; texto resolución obligatorio/500 con los mismos mensajes de texto y etiqueta `resolución`.

Anulación400: `El motivo de anulación es obligatorio.` / `El motivo de anulación no puede superar 500 caracteres.`.

409: `Solo se puede editar un siniestro registrado.`; `Solo se puede eliminar un siniestro registrado.`; `Solo se puede reclamar un siniestro registrado.`; `Solo se puede cerrar un siniestro en reclamo.`; `El siniestro ya está anulado.`; `Solo se puede anular un siniestro registrado o en reclamo.`; `No se puede eliminar el siniestro porque está en uso.` (traducción defensiva de FK futura, sin acreditar FK real).

Consulta400: `El número de página debe ser mayor que cero.`; `El tamaño de página debe estar entre 1 y 100.`; `El estado debe ser Registrado, EnReclamo, Cerrado o Anulado.`; tipo inválido mismo literal. Filtros vacíos sin efecto y enums sin distinguir mayúsculas.

## 5. Aceptación
A1 gate completo salida0, .NET sin warnings, formato/capas/reglas. A2 integración por literal/límite, entradas inválidas sin mutación. A3 nueve operaciones JWT/OpenAPI, filtros/paginación/resumen sin dinero agregado. A4 SQL/scaffold coincidentes y seed idempotente con cuatro estados, resultados y monedas. A5 SMOKE OK PostgreSQL real con fecha sin zona, extremos decimales, reclamo/cierre/anulación preservados y restricciones. A6 pantalla, diez repeticiones propias y veinte con build simultáneo, QA real375/1280claro/oscuro y comparación integrada. Documentar avisos globales sin ocultarlos.

## 6. Checklist
- [ ] Aprobación de Lukas.
- [x] SQL/scaffold y backend con pruebas.
- [x] Seed y smoke real.
- [x] Pantalla, estabilidad y QA.
- [x] Gate y evidencia local, con avisos globales documentados.

## 7. Registro
2026-11-22: implementación local autorizada, SQL aplicado y scaffold coincidente. Pasaron 49 pruebas propias de backend y 62 de frontend; gate completo con 1.133 backend y 605 frontend, salida 0 y .NET sin warnings. SMOKE OK real, doble seed con cinco siniestros, diez ejecuciones propias y veinte con build simultáneo, QA real en cuatro escenarios y comparación con Contratos. Se documentaron el saneado local de finales de línea y los avisos globales de ESLint/Vite. Evidencia en `docs/evidencias/modulo50/README.md`. Aprobación de Lukas e integración pendientes; sin push, PR ni cambios en main.
