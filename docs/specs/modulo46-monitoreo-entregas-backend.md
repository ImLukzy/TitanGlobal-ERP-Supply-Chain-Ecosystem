# Propuesta: módulo 46 — Monitoreo de entregas (backend)

## 1. Objetivo
Responsable Kelvin Pinto; orden 41 → 42 → 48 → 43 → 44 → 45 → **46** → 47 → 49 → 50 → 73. El enunciado define «estado en ruta, POD». Implementar seguimiento manual de un despacho desde su programación hasta entrega o cancelación, con constancia textual de recepción e historial de estados.

Kelvin autoriza continuar localmente con una propuesta similar a sus módulos anteriores. **La revisión y aprobación de Lukas permanecen pendientes**; no acreditar integración, publicación ni despliegue. Rama independiente `kelvin/modulo46-monitoreo-entregas` desde origin/main actualizado; no incorporar los commits de las otras ramas.

## 2. Fuera de alcance y decisiones propuestas
- Referencia de guía ingresada como texto histórico, sin FK al módulo 45 todavía no integrado. Código de seguimiento único sin distinguir mayúsculas; no es una guía fiscal ni un correlativo del módulo 76.
- Estados Programada → EnRuta → Entregada; Programada/EnRuta → Cancelada. No reabrir ni alterar terminales. Sin incidencias/reintentos, GPS, mapas, ETA automática, notificaciones, inventario ni cambios en tablas/pantallas ajenas.
- POD manual: receptor, documento y referencia de constancia textuales, observaciones opcionales y fecha de registro del servidor. No subir imágenes/archivos, no abrir enlaces, firmar, validar identidad ni acreditar recepción digital certificada.
- Historial de transiciones, no de ediciones: evento inicial y cada cambio, ordenados por Id de registro. Máximo tres eventos por las transiciones permitidas. Nunca editar ni eliminar eventos directamente. Baja solo de Programada, con cascada de su evento inicial.
- Guardar cada transición y evento en una única llamada CompleteAsync. No prometer resolución de transiciones concurrentes: requiere decisión y mecanismo separado. Todas estas decisiones quedan propuestas para Lukas.

## 3. Archivos afectados
| Ruta | Acción |
|---|---|
| `database/016-monitoreo-entregas.sql` | Crear; reservas 010–015 de ramas independientes contrastadas |
| `src/TitanGlobal.Domain/Models/EntregaDespacho.cs`, `EventoEntrega.cs` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | Generar exclusivamente mediante scaffold |
| `src/TitanGlobal.Api/Configuration/run/NombresNavegacion.cs` | Colección propia Eventos si es necesaria |
| `src/TitanGlobal.Application/DTOs/Modulo46_MonitoreoEntregas/*.cs` | GuardarEntregaRequestDto, RegistrarPodRequestDto, CancelarEntregaRequestDto, EntregaListadoDto, EntregaDetalleDto, EventoEntregaDto, PaginaEntregasDto, ResumenEntregasDto |
| `src/TitanGlobal.Application/UseCases/Modulo46_MonitoreoEntregas/*.cs` | Crear/Actualizar/Eliminar/Obtener/ListarEntrega(s)UseCase, IniciarEntregaUseCase, CompletarEntregaUseCase, CancelarEntregaUseCase, ResumenEntregasUseCase, ValidacionEntrega y PresentacionEntrega |
| `src/TitanGlobal.Api/Controllers/Modulo46_MonitoreoEntregas/MonitoreoEntregasController.cs` | Controlador delgado |
| `tests/TitanGlobal.Api.Tests/Modulo46_MonitoreoEntregas/` | Integración y traducción de restricciones |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | Bloques propios, validadores y SQL real |
| `docs/equipo/asignaciones.md`, `docs/equipo/relevo.md`, specs y `docs/evidencias/modulo46/` | Estado y comprobaciones reales |

## 4. Diseño y lógica
### Datos
EntregasDespacho: Id identity; Codigo varchar20; ReferenciaGuia varchar40; Destinatario varchar200; DireccionDestino varchar300; FechaProgramada timestamptz; Observaciones varchar500 nullable; Estado varchar15 default Programada; FechaSalida/FechaEntrega/FechaCancelacion timestamptz nullable; Receptor varchar100, DocumentoReceptor varchar20, ReferenciaConstancia varchar100, ObservacionesEntrega varchar500 y MotivoCancelacion varchar500, todos nullable según estado.

EventosEntrega: Id identity, EntregaDespachoId FK con índice y cascada, Estado varchar15, Fecha timestamptz, Descripcion varchar500. Claves únicas de código mediante lower(btrim). Checks SQL: textos requeridos recortados/no vacíos, estados válidos y coherencia de fechas/POD/motivo. Programada sin cierre/salida/POD; EnRuta con salida sin cierre/POD; Entregada con salida/entrega y POD obligatorio; Cancelada con cancelación/motivo, conservando salida si existió y sin POD. SQL no transforma textos de escrituras directas.

Application recorta textos; opcionales vacíos a null. DTO FechaProgramada DateTime? para validar ausencia/default, normalizar con FechasUtc preservando el instante; entrada sin zona se interpreta UTC. Fechas de salida, POD, cancelación y eventos del servidor, normalizadas UTC. No imponer comparación con hoy ni modificar la programación al salir.

### API autenticada
Prefijo `/api/v1/monitoreo-entregas`:
| Operación | Contrato |
|---|---|
| GET / | busqueda en código/referencia/destinatario; estado opcional; pagina=1, tamanoPagina=20 máximo100; orden Codigo/Id, filtros antes de paginar, sin overflow |
| GET /{id} | Detalle con historial ordenado por Id |
| POST / | `{codigo,referenciaGuia,destinatario,direccionDestino,fechaProgramada,observaciones?}`; Programada y evento `Entrega programada.`; 200 `{id}` |
| PUT /{id} | Mismo cuerpo; solo Programada, sin crear eventos ni perder el inicial; 200 `{id}` |
| DELETE /{id} | Solo Programada; 204 y cascada |
| POST /{id}/salida | Sin cuerpo, PermitirCuerpoVacio; Programada → EnRuta y evento `Salida a ruta registrada.`; 200 `{id}` |
| POST /{id}/entrega | `{receptor,documentoReceptor,referenciaConstancia,observaciones?}`; EnRuta → Entregada, POD y evento `Recepción registrada mediante constancia manual.`; 200 `{id}` |
| POST /{id}/cancelacion | `{motivo}`; Programada/EnRuta → Cancelada; evento con el motivo recortado; 200 `{id}` |
| GET /resumen | `{total,programadas,enRuta,entregadas,canceladas}` agregado en consulta |

Página `{items,total,pagina,tamanoPagina}`; item `{id,codigo,referenciaGuia,destinatario,direccionDestino,fechaProgramada,estado}`. Detalle agrega observaciones, fechas de transición, campos POD/motivo e historial `[{id,estado,fecha,descripcion}]`. No exponer entidades. Sin endpoints públicos nuevos ni modificación de middleware/pipeline.

### Validación y mensajes literales
En actualización/baja/transición, buscar primero (404), después estado (409), después validar datos; en alta/edición comprobar duplicado al final. Validar completamente antes de modificar datos.

404: `La entrega {id} no existe.`

Alta/edición: validar código, referencia, destinatario y destino en ese orden. Mensajes 400 `El campo {etiqueta} es obligatorio.` y `El campo {etiqueta} no puede superar {maximo} caracteres.`:
| Campo | etiqueta | maximo |
|---|---|---|
| codigo | código | 20 |
| referenciaGuia | referencia de guía | 40 |
| destinatario | destinatario | 200 |
| direccionDestino | dirección de destino | 300 |

Después: 400 `La fecha programada es obligatoria.` para null/default; `Las observaciones no pueden superar 500 caracteres.`. Duplicado 409 `Ya existe una entrega con el código {codigo}.`, también al capturar ViolacionRestriccionException Unicidad.

POD: usar los mismos mensajes de campo, con etiquetas `receptor`/100, `documento del receptor`/20, `referencia de constancia`/100, en ese orden; después 400 `Las observaciones de entrega no pueden superar 500 caracteres.`. Cancelación 400: `El motivo de cancelación es obligatorio.` / `El motivo de cancelación no puede superar 500 caracteres.`.

409: `Solo se puede editar una entrega programada.`; `Solo se puede eliminar una entrega programada.`; `Solo se puede iniciar una entrega programada.`; `Solo se puede completar una entrega en ruta.`; `La entrega ya está cancelada.`; `Solo se puede cancelar una entrega programada o en ruta.`; `No se puede eliminar la entrega porque está en uso.` (traducción defensiva de FK externa futura, sin acreditar que exista).

Consulta 400: `El número de página debe ser mayor que cero.`; `El tamaño de página debe estar entre 1 y 100.`; `El estado debe ser Programada, EnRuta, Entregada o Cancelada.`. Estado vacío sin filtro; valores válidos ignorando mayúsculas. Excepciones/CodigosEstado de Application; nunca ASP.NET/Npgsql allí. Tipos explícitos, miembros con bloques, lambdas en la misma línea del operador, UseCases concretos españoles y controladores delgados.

## 5. Criterios medibles
A1: verify completo salida0, .NET sin warnings, formato/capas/reglas. A2: integración por cada literal/límite, sin persistir datos inválidos, unicidad normalizada. A3: nueve operaciones OpenAPI/JWT, filtros/paginación/resumen. A4: SQL y scaffold coinciden, seed idempotente con los cuatro estados. A5: smoke real con fecha sin zona, CRUD, transiciones/POD/historial y restricciones/cascada. A6: pantalla, diez repeticiones más carga y QA real según spec frontend. Registrar cualquier advertencia global existente sin ocultarla; no acreditar cierre formal con revisión pendiente.

## 6. Checklist
- [ ] Revisión y aprobación de Lukas.
- [ ] SQL/scaffold local y API con pruebas.
- [ ] Seed y smoke PostgreSQL.
- [ ] Pantalla, estabilidad y QA.
- [ ] Gates y evidencia reales; revisión de compañero antes de integrar.

## 7. Registro
2026-11-21: asignación y orden comprobados; propuesta de implementación local autorizada por Kelvin al continuar como los módulos anteriores. Revisión de Lukas pendiente. No acreditar pruebas aún; sin push, PR ni cambios en main.
