# Propuesta: módulo 45 — Guías de remisión (backend)

**Estado de revisión: propuesta, pendiente de aprobación de Lukas.**

## 1. Objetivo

Responsable: Kelvin Pinto, según `docs/equipo/asignaciones.md`. Siguiente módulo del orden 41 → 42 → 48 → 43 → 44 → **45** → 46 → 47 → 49 → 50 → 73.

El enunciado solo define `GuiasRemision (documentos de transporte)`; no establece campos, reglas fiscales, numeración ni integración. Esta propuesta concreta un registro interno de documentos de transporte con cabecera, mercancías y estados. Propuesta, pendiente de aprobación de Lukas. No se presenta como especificación aprobada ni habilita integración o despliegue.

Rama independiente: `kelvin/modulo45-guias-remision`, creada desde `origin/main` actualizado el 2026-11-21. Las ramas locales de los módulos anteriores se conservan; no se incorporan sus commits a esta rama ni se modifica main.

## 2. Fuera de alcance y decisiones pendientes

**Todas las decisiones de este apartado requieren revisión de Lukas:**

1. Documento interno de transporte, con estados Borrador → Emitida → Anulada y Borrador → Anulada. «Emitida» solo significa cierre interno del documento. No acredita envío, autorización ni validez tributaria.
2. Serie y número ingresados manualmente, únicos como pareja desde el borrador. No generar correlativos ni modificar el módulo 76 de Sergio.
3. Datos de remitente, destinatario, origen, destino, transportista, conductor, placa y mercancías guardados como texto propio del documento. Evita depender de ramas todavía no integradas y preserva la descripción histórica. Las relaciones con 41/42/43/44/48 se decidirán expresamente antes de incorporarlas.
4. Cantidades por línea y peso bruto declarado en cabecera; no sumar unidades heterogéneas ni inferir pesos, capacidad, inventarios o disponibilidad.

Quedan fuera las cuatro entidades congeladas, tablas/pantallas ajenas, middleware/pipeline, inventario, reservas, rutas, POD (46), facturación (51), SUNAT u otro ente tributario, firma electrónica, XML, QR, PDF y comunicaciones externas. No se declara cumplimiento legal de ningún país. Si el objetivo aprobado exige esas funciones, se revisará el contrato antes de implementar.

## 3. Archivos previstos

| Ruta | Acción local autorizada por Kelvin |
|---|---|
| `database/015-guias-remision.sql` | Crear, comprobando otra vez la numeración reservada |
| `src/TitanGlobal.Domain/Models/GuiaRemision.cs` y `LineaGuiaRemision.cs` | Generar por scaffold, nunca editar a mano |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | Regenerar por scaffold |
| `src/TitanGlobal.Api/Configuration/run/NombresNavegacion.cs` | Añadir únicamente el nombre de la colección propia si el scaffold lo requiere |
| `src/TitanGlobal.Application/DTOs/Modulo45_GuiasRemision/*.cs` | DTOs GuardarGuiaRequestDto, LineaGuiaRequestDto, AnularGuiaRequestDto, GuiaListadoDto, GuiaDetalleDto, LineaGuiaDto, PaginaGuiasDto y ResumenGuiasDto |
| `src/TitanGlobal.Application/UseCases/Modulo45_GuiasRemision/*.cs` | CrearGuiaUseCase, ActualizarGuiaUseCase, EliminarGuiaUseCase, ObtenerGuiaUseCase, ListarGuiasUseCase, EmitirGuiaUseCase, AnularGuiaUseCase, ResumenGuiasUseCase, ValidacionGuia y PresentacionGuia |
| `src/TitanGlobal.Api/Controllers/Modulo45_GuiasRemision/GuiasRemisionController.cs` | Controlador delgado |
| `tests/TitanGlobal.Api.Tests/Modulo45_GuiasRemision/GuiasRemisionApiTests.cs` y `RestriccionesGuiaTests.cs` | Integración de reglas, mensajes y traducción de restricciones |
| `tests/TitanGlobal.Api.Tests/Arquitectura/PluralizadorEspanolTests.cs` | Añadir casos propios si se necesitan para los nombres generados |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | Añadir bloque propio y validadores, sin alterar los de otros módulos |
| `docs/equipo/asignaciones.md`, `docs/equipo/relevo.md` y specs de 45 | Estado y resultados reales |

El 015 considera las reservas 010–014 de las ramas independientes de Kelvin. Aplicación de SQL y generación de entidades exclusivamente en PostgreSQL local; no se utiliza producción.

## 4. Contrato propuesto

### Datos

`GuiasRemision`: Id identity; Serie varchar(8); Numero integer; Estado varchar(10) default Borrador; FechaInicioTraslado timestamptz obligatoria; RemitenteNombre varchar(200), RemitenteDocumento varchar(20), DestinatarioNombre varchar(200), DestinatarioDocumento varchar(20), DireccionOrigen varchar(300), DireccionDestino varchar(300), MotivoTraslado varchar(200), Transportista varchar(200), Conductor varchar(100), Placa varchar(20), todos obligatorios; PesoBrutoKg numeric(12,2); Observaciones varchar(500) nullable; FechaEmision timestamptz nullable; FechaAnulacion timestamptz nullable; MotivoAnulacion varchar(500) nullable.

`LineasGuiaRemision`: Id identity; GuiaRemisionId FK con índice y borrado en cascada; Posicion integer 1–100; Sku varchar(40), Descripcion varchar(200), UnidadMedida varchar(10), obligatorios; Cantidad numeric(12,3).

Índice único `(Serie, Numero)`. Application recorta la serie y la convierte a mayúsculas; SQL exige ese formato ya normalizado mediante un check, sin transformar escrituras directas. Admite entre 1 y 8 caracteres ASCII A–Z/0–9. Número 1–99999999. Textos recortados y no vacíos en SQL. Cantidad 0.001–999999999.999; peso 0.01–9999999999.99. No redondear entradas con exceso de decimales. Estado y consistencia de fechas/motivo con checks SQL. Borrador sin fechas de cierre; Emitida con FechaEmision y sin anulación; Anulada con motivo/FechaAnulacion y puede conservar FechaEmision.

FechaInicioTraslado se normaliza mediante FechasUtc preservando el instante recibido; sin zona se interpreta como UTC según las reglas del proyecto. No convertirla a día sin hora. DTO de entrada DateTime? para distinguir ausencia/default. No imponer comparación con «hoy» ni una secuencia temporal no aprobada. Fechas de emisión/anulación asignadas por el servidor en UTC.

### HTTP y DTOs

Prefijo `/api/v1/guias-remision`, ocho operaciones autenticadas; no añadir AllowAnonymous:

| Operación | Entrada y respuesta |
|---|---|
| GET / | busqueda opcional en serie, documento/nombre de destinatario; estado opcional; pagina=1, tamanoPagina=20, máximo100; orden Serie/Numero/Id, filtros antes de paginar y sin overflow de desplazamiento |
| GET /{id} | Detalle con líneas ordenadas por Posicion/Id |
| POST / | GuardarGuiaRequestDto; crea Borrador; 200 `{id}` |
| PUT /{id} | Mismo cuerpo completo; solo Borrador; 200 `{id}` |
| DELETE /{id} | Solo Borrador; 204, elimina líneas en cascada |
| POST /{id}/emision | Sin cuerpo, PermitirCuerpoVacio; solo Borrador; 200 `{id}` |
| POST /{id}/anulacion | `{motivo}`; Borrador o Emitida; 200 `{id}` |
| GET /resumen | `{total,borradores,emitidas,anuladas}`, agregado en consulta |

GuardarGuiaRequestDto contiene todos los campos de cabecera editables en camelCase, observaciones opcional y `lineas:[{sku,descripcion,unidadMedida,cantidad}]`. Nunca acepta estado, fechas de transición, Id o posiciones de líneas. El orden del array determina Posicion. Cantidades/peso de entrada numéricos JSON; salidas como strings de tres/dos decimales respectivamente.

Listado `{items,total,pagina,tamanoPagina}`; item `{id,serie,numero,estado,fechaInicioTraslado,destinatarioNombre,destinatarioDocumento,direccionDestino,pesoBrutoKg,totalLineas}`. Detalle agrega los demás campos de cabecera, fechas/motivo de cierre y `lineas:[{id,posicion,sku,descripcion,unidadMedida,cantidad}]`. No exponer entidades ni sumar cantidades de unidades diferentes.

### Reglas y mensajes literales

En edición/baja/transiciones: buscar primero, comprobar estado, validar cuerpo, después comprobar duplicado. Alta/edición: serie, número, fecha, campos de texto en el orden de la tabla siguiente, peso, observaciones, colección y líneas. Validar completamente antes de modificar entidades; reemplazo de líneas y cabecera en una sola llamada CompleteAsync. Los Id de líneas pueden cambiar tras editar; no son identificadores externos estables.

404: `La guía de remisión {id} no existe.`

Serie: 400 `La serie debe contener entre 1 y 8 letras mayúsculas o dígitos.`; normalizar antes de validar. Número: 400 `El número debe estar entre 1 y 99999999.`. Fecha ausente o default: 400 `La fecha de inicio del traslado es obligatoria.`

Para los textos requeridos, mensajes 400 exactamente `El campo {etiqueta} es obligatorio.` y `El campo {etiqueta} no puede superar {maximo} caracteres.`; sustituir según esta tabla, sin cambiar mayúsculas:

| Campo | etiqueta | maximo |
|---|---|---|
| remitenteNombre | nombre del remitente | 200 |
| remitenteDocumento | documento del remitente | 20 |
| destinatarioNombre | nombre del destinatario | 200 |
| destinatarioDocumento | documento del destinatario | 20 |
| direccionOrigen | dirección de origen | 300 |
| direccionDestino | dirección de destino | 300 |
| motivoTraslado | motivo del traslado | 200 |
| transportista | transportista | 200 |
| conductor | conductor | 100 |
| placa | placa | 20 |

Peso 400: `El peso bruto debe estar entre 0.01 y 9999999999.99 kg.` y `El peso bruto no puede tener más de 2 decimales.`. Observaciones 400: `Las observaciones no pueden superar 500 caracteres.`; vacías a null.

Líneas 400: `La guía debe tener entre 1 y 100 líneas.` y `La guía no puede contener líneas nulas.`. Para i empezando en 1, validar SKU, descripción, unidad y cantidad. Textos: `Línea {i}: el campo {etiqueta} es obligatorio.` / `Línea {i}: el campo {etiqueta} no puede superar {maximo} caracteres.`; etiquetas exactas `SKU`/40, `descripción`/200 y `unidad de medida`/10. Cantidad: `Línea {i}: la cantidad debe estar entre 0.001 y 999999999.999.` y `Línea {i}: la cantidad no puede tener más de 3 decimales.`. Rango antes de escala. Se permiten SKU repetidos: no se inventa unicidad de presentaciones.

409: `Ya existe una guía con la serie {serie} y el número {numero}.`; `Solo se puede editar una guía en borrador.`; `Solo se puede eliminar una guía en borrador.`; `Solo se puede emitir una guía en borrador.`; `La guía ya está anulada.`; `No se puede eliminar la guía porque está en uso.` (tratamiento defensivo de FK externa futura, sin inventar esa relación).

Anulación 400: `El motivo de anulación es obligatorio.` / `El motivo de anulación no puede superar 500 caracteres.`. Anular preserva líneas, datos de transporte y FechaEmision existente; sin reabrir ni editar documentos terminales. Emitir no genera archivo ni comunicación fiscal. No prometer control de concurrencia de transiciones mientras no exista mecanismo aprobado y probado.

Listado 400: `El número de página debe ser mayor que cero.`; `El tamaño de página debe estar entre 1 y 100.`; `El estado debe ser Borrador, Emitida o Anulada.`. Estado vacío equivale a sin filtro; valores admitidos sin distinguir mayúsculas. Error JSON `{StatusCode,Message}`. Application utiliza CodigosEstado y ViolacionRestriccionException; no Npgsql ni ASP.NET. Índice único protege duplicados concurrentes y su violación se traduce a 409.

## 5. Criterios de aceptación

| # | Criterio medible | Verificación requerida |
|---|---|---|
| A1 | Cuatro capas y convenciones, sin warnings .NET | verify completo, formato y fixtures de reglas |
| A2 | Cada mensaje literal y límite, invalidaciones sin cambios persistidos | Integración: un caso por literal, parametrizado cuando corresponda |
| A3 | Ocho operaciones, JWT, filtros/paginación/resumen y estados | OpenAPI e integración, incluida petición sin token |
| A4 | Precisión y UTC reales, reemplazo atómico, historial y restricciones | Smoke PostgreSQL: fecha sin zona, máximos/escala, unicidad, FK propia y cascada |
| A5 | SQL y modelo coinciden; seed idempotente | aplicar-bd, scaffold, doble carga de seed con Borrador/Emitida/Anulada |
| A6 | Pantalla funcional y accesible | Spec frontend, diez repeticiones y QA real en cuatro escenarios |

No marcar resultados previstos como ejecutados; documentar advertencias reales sin ocultarlas ni debilitar pruebas.

## 6. Checklist de ejecución

- [ ] Lukas revisa y aprueba el alcance y las decisiones de §2.
- [x] SQL local, scaffold y comprobación de nombres generados.
- [x] API, validaciones, transiciones y pruebas de cada literal/límite.
- [x] Seed y smoke PostgreSQL completos.
- [x] Pantalla y verificaciones según spec frontend.
- [x] Gates y evidencia registrados, con salvedad de Vite.
- [ ] Revisión de compañero antes de integrar.

## 7. Registro

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-21 | Asignación y base | Comprobados | Módulo de Kelvin; referencias remotas actualizadas; rama propia desde origin/main |
| 2026-11-21 | Contrato | Propuesta pendiente de revisión | Este archivo y spec frontend; no existe aprobación registrada |
| 2026-11-21 | A1–A6 | Verificados localmente, con salvedades | 1143 pruebas backend (60 propias), 604 frontend (61 propias), gate salida 0, SMOKE OK, doble seed, diez repeticiones más una bajo carga y QA en cuatro escenarios; [evidencia](../evidencias/modulo45/README.md). Aprobación pendiente y advertencia global Vite registradas; no acredita cierre formal |


2026-11-21: Kelvin autoriza expresamente continuar con esta propuesta como implementación local, igual que los módulos anteriores. Esta autorización permite desarrollar y probar en su rama; no sustituye la revisión de Lukas ni autoriza publicación, integración o despliegue.
