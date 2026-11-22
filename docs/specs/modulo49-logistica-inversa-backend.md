# Propuesta: módulo 49 — Logística inversa (backend)

## 1. Objetivo
Kelvin Pinto según asignaciones. Orden 41 → 42 → 48 → 43 → 44 → 45 → 46 → 47 → **49** → 50 → 73. El enunciado pide «retornables, pallets, devoluciones en ruta». Registrar un retorno por material y unidad: programación, recogida y recepción física manuales, con cantidades verificables y cancelación.

Kelvin autoriza continuar localmente con una propuesta como sus módulos anteriores. La aprobación de Lukas sigue pendiente. Rama independiente `kelvin/modulo49-logistica-inversa` desde origin/main actualizado, sin incorporar las otras ramas ni publicar o integrar.

## 2. Fuera de alcance
Cada retorno representa un solo material y unidad, sin líneas. Tipos Pallet, Retornable, DevolucionRuta. Referencias de origen, contraparte y ubicaciones son texto histórico: sin FK a guías, entregas, clientes, productos ni almacenes; sin modificar tablas ajenas. No sustituye devoluciones comerciales del10, ni genera movimientos de inventario, notas de crédito, contabilidad, correlativos o enlaces. Sin recogidas/recepciones parciales sucesivas, incidencias ni concurrencia garantizada.

Se permite recoger menos de lo programado y recibir menos de lo recogido; el cierre deja las diferencias explícitas, sin ajustes automáticos. Cantidades en una unidad manual por registro: no sumarlas entre unidades/materiales distintos.

## 3. Archivos
`database/018-logistica-inversa.sql`; entidad `RetornoLogistico.cs` y DbContext generados exclusivamente por scaffold; mapeo propio en `Configuration/run/PluralizadorEspanol.cs` y su teoría. DTOs y UseCases en `Modulo49_LogisticaInversa`; controlador `LogisticaInversaController.cs`; pruebas `tests/TitanGlobal.Api.Tests/Modulo49_LogisticaInversa/`. Bloques propios al final de seed y smoke, specs, asignaciones, relevo y evidencia `docs/evidencias/modulo49/`. Reservas010–017 conservadas de ramas independientes, no integración.

## 4. Diseño y lógica
Tabla RetornosLogisticos, Id identity. Campos obligatorios recortados: Codigo20, ReferenciaOrigen40, Contraparte200, PuntoRecogida200, PuntoRecepcion200, Descripcion200, Unidad20; Tipo varchar20 enum; FechaProgramada timestamptz; CantidadProgramada numeric(12,3), 0.001–999999999.999. Observaciones varchar500 nullable. Estado varchar15 default Programado. CantidadRecogida y CantidadRecibida numeric(12,3) nullable, fechas de recogida/recepción/cancelación timestamptz nullable, ResponsableRecogida/ResponsableRecepcion varchar100 nullable, MotivoCancelacion varchar500 nullable. Código único por lower(btrim), checks de textos/estados y coherencia de datos.

Programado → Recogido → Recibido; Programado/Recogido → Cancelado. Solo Programado admite editar/baja. Recogida registra cantidad positiva ≤ programada y responsable; recepción cantidad positiva ≤ recogida y responsable. Fechas de transiciones del servidor UTC; preservar recogida al recibir/cancelar. Terminales solo lectura, sin reaperturas. Cantidades de salida como cadenas F3; diferencias NoRecogida=programada−recogida y NoRecibida=recogida−recibida solo cuando existen ambas, sin redondeos adicionales. Resumen solo cuenta estados.

Application valida antes de modificar; normaliza fechas con FechasUtc (sin zona se interpreta UTC, conserva milisegundos), recorta textos y opcionales vacíos→null. Sin comparación con hoy. Tipos explícitos, miembros con bloques, lambdas en la línea del operador, UseCases concretos españoles, excepciones/CodigosEstado de Application y controlador delgado.

API JWT `/api/v1/logistica-inversa`: GET listado con busqueda en código/referencia/contraparte/descripción, estado y tipo opcionales, pagina1/tamanoPagina20 máximo100, orden Codigo/Id sin overflow; GET /{id}; POST / y PUT /{id} cuerpo `{codigo,tipo,referenciaOrigen,contraparte,puntoRecogida,puntoRecepcion,descripcion,unidad,fechaProgramada,cantidadProgramada,observaciones?}`; DELETE /{id}204; POST /{id}/recogida y /recepcion cuerpo `{cantidad,responsable}`; POST /{id}/cancelacion `{motivo}`; GET /resumen `{total,programados,recogidos,recibidos,cancelados}`. Escrituras200 `{id}`. Página `{items,total,pagina,tamanoPagina}`. Listado todos los datos editables salvo observaciones, más Id/Estado; detalle agrega observaciones, cantidades/fechas/responsables/motivo de transiciones y diferencias. Nunca entidades.

### Mensajes literales y orden
Primero buscar en edición/baja/transición:404 `El retorno {id} no existe.`; después estado409, después datos400. Alta/edición: campos de texto en el orden de la tabla siguiente, después tipo, fecha, cantidad y observaciones; duplicado al final. Para cada campo, `El campo {etiqueta} es obligatorio.` / `El campo {etiqueta} no puede superar {máximo} caracteres.`:
- Codigo: `código` / 20.
- ReferenciaOrigen: `referencia de origen` / 40.
- Contraparte: `contraparte` / 200.
- PuntoRecogida: `punto de recogida` / 200.
- PuntoRecepcion: `punto de recepción` / 200.
- Descripcion: `descripción` / 200.
- Unidad: `unidad` / 20.

400: `El tipo debe ser Pallet, Retornable o DevolucionRuta.`; `La fecha programada es obligatoria.`; `La cantidad programada debe estar entre 0.001 y 999999999.999.`; `La cantidad programada no puede tener más de 3 decimales.`; `Las observaciones no pueden superar 500 caracteres.`.

Recogida/recepción, en orden: `La cantidad debe estar entre 0.001 y 999999999.999.`; `La cantidad no puede tener más de 3 decimales.`; `La cantidad recogida no puede superar la programada.` / `La cantidad recibida no puede superar la recogida.`; texto responsable obligatorio/100 con etiqueta `responsable`. Cancelación400: `El motivo de cancelación es obligatorio.` / `El motivo de cancelación no puede superar 500 caracteres.`.

409: `Ya existe un retorno con el código {codigo}.` (también Unicidad al guardar); `Solo se puede editar un retorno programado.`; `Solo se puede eliminar un retorno programado.`; `Solo se puede recoger un retorno programado.`; `Solo se puede recibir un retorno recogido.`; `El retorno ya está cancelado.`; `Solo se puede cancelar un retorno programado o recogido.`; `No se puede eliminar el retorno porque está en uso.` (FK futura defensiva, sin acreditar FK real).

Consulta400: `El número de página debe ser mayor que cero.`; `El tamaño de página debe estar entre 1 y 100.`; `El estado debe ser Programado, Recogido, Recibido o Cancelado.`; tipo inválido mismo literal de alta. Filtros vacíos sin efecto, enums sin distinguir mayúsculas.

## 5. Aceptación
A1 gate completo0, .NET sin warnings, formato/capas/reglas. A2 integración por literal, límites y entradas sin cambios. A3 nueve operaciones JWT/OpenAPI, filtros/paginación/resumen. A4 SQL/scaffold coincidentes, seed idempotente cuatro estados y tipos. A5 SMOKE OK PostgreSQL real, UTC sin zona, decimales máximos, recogida/recepción con diferencias, terminales y restricciones. A6 pantalla consistente, diez repeticiones y veinte con carga, QA real375/1280claro/oscuro y referencia integrada. Registrar avisos globales sin ocultarlos.

## 6. Checklist
- [ ] Aprobación de Lukas.
- [ ] SQL/scaffold y backend con pruebas.
- [ ] Seed y smoke real.
- [ ] Pantalla, estabilidad y QA.
- [ ] Gate y evidencia local.

## 7. Registro
2026-11-21: propuesta local autorizada al continuar como los módulos anteriores. Asignación, orden y base comprobados; revisión formal pendiente. Sin verificaciones acreditadas todavía.
