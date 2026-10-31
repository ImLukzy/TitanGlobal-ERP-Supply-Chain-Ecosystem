# Especificación: 37 - Modulo78_NotificacionesAlertas (backend)

## 1. Objetivo
**Problema:** cada módulo calcula situaciones que requieren atención (reclamos vencidos, órdenes de abastecimiento atrasadas, contratos excedidos, almacenes casi llenos…), pero no hay un sitio único donde verlas.
**Resultado esperado:** `Modules/Modulo78_NotificacionesAlertas/` en `api/v1/alertas`:
- **alertas calculadas** a partir de los datos de los módulos ya hechos, con tipo, severidad, mensaje y enlace al registro;
- posibilidad de **descartar** una alerta (y restaurarla), guardando solo el descarte;
- listado con filtros y resumen.

Parte de Lukas (`docs/equipo/asignaciones.md`).

## 2. Fuera de alcance
- Envío por correo, push o tiempo real; usuarios y destinatarios (módulo 71).
- Cambiar la lógica de los módulos de origen: las condiciones se calculan igual que en ellos.
- Cambios en las 4 entidades del enunciado. El frontend (spec 38).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna. Reglas tomadas por la integración: las alertas no se guardan (se recalculan en cada consulta); solo se guarda el descarte por su clave; una alerta descartada reaparece si cambia su clave (otro registro).

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Models/AlertaDescartada.cs` | crear | `Id`, `Clave` (máx. 100, única), `FechaDescarte` (UTC) |
| `src/TitanGlobal.Api/Models/TitanGlobalDbContext.cs` | modificar | `DbSet`, índice único en `Clave` |
| `src/TitanGlobal.Api/Migrations/*_AlertasDescartadas*.cs` + snapshot | crear | `dotnet ef migrations add AlertasDescartadas -p src/TitanGlobal.Api` |
| `src/TitanGlobal.Api/Modules/Modulo78_NotificacionesAlertas/{Controllers,Dtos,UseCases}/` | crear | `[Route("api/v1/alertas")]`; un generador por tipo de alerta; reutiliza las reglas de los módulos de origen (no duplica definiciones como «vencido» o «atrasada» con otra lógica) |
| `scripts/smoke-postgres.sh` | modificar | §5 A5 (el seed de los demás módulos ya produce alertas) |
| `tests/TitanGlobal.Api.Tests/Modulo78_NotificacionesAlertas/*.cs` | crear | InMemory, datos propios |

## 4. Diseño y lógica
**Tipos de alerta** (en este orden), con la misma definición que su módulo:

| Tipo | Origen | Condición | Severidad | Mensaje (literal) | Clave |
|---|---|---|---|---|---|
| `ReclamoVencido` | 09 | reclamo vencido | Alta | «El reclamo {numero} de {cliente} superó su plazo de atención.» | `reclamo-vencido:{id}` |
| `OrdenAbastecimientoAtrasada` | 15 | orden emitida y atrasada | Alta | «La orden {numero} de {proveedor} está atrasada desde el {fechaEntrega dd/MM/yyyy}.» | `orden-atrasada:{id}` |
| `ContratoExcedido` | 05 | contrato vigente con consumo > 100 % | Alta | «El contrato {numero} de {cliente} va al {porcentaje} % de su monto comprometido.» | `contrato-excedido:{id}` |
| `AlmacenCasiLleno` | 24 | almacén activo con ocupación de zonas activas ≥ 90 % | Media | «El almacén {codigo} tiene asignado el {porcentaje} % de su capacidad.» | `almacen-lleno:{id}` |
| `ContratoPorVencer` | 05 | contrato vigente no excedido que termina en los próximos 30 días (incluido hoy) | Media | «El contrato {numero} de {cliente} termina el {fechaFin dd/MM/yyyy}.» | `contrato-por-vencer:{id}` |
| `CotizacionVencida` | 03 | cotización emitida vencida | Baja | «La cotización {numero} de {cliente} venció sin respuesta.» | `cotizacion-vencida:{id}` |
| `SolicitudPendienteAntigua` | 13 | solicitud pendiente con más de 7 días desde su fecha | Baja | «La solicitud {numero} lleva {dias} días pendiente.» | `solicitud-pendiente:{id}` |

Porcentajes con 2 decimales y punto; fechas en UTC con formato `dd/MM/yyyy`.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/alertas?tipo=&severidad=&incluirDescartadas=&pagina=1&tamanoPagina=20` | opcionales salvo paginación; por defecto sin descartadas | 200 `PaginaAlertasDto { items: AlertaDto[], total, pagina, tamanoPagina }` | 400 paginación; «El tipo de alerta no es válido.»; «La severidad no es válida.» |
| GET | `api/v1/alertas/resumen` | — | 200 `ResumenAlertasDto` | — |
| POST | `api/v1/alertas/descartar` | `ClaveAlertaRequestDto { clave }` | 204 | 400, 404, 409 |
| POST | `api/v1/alertas/restaurar` | `ClaveAlertaRequestDto { clave }` | 204 | 400, 404 |

**DTOs.**
- `AlertaDto { clave, tipo, severidad, mensaje, modulo, registroId, ruta, fechaReferencia, descartada }`: `modulo` es el número del módulo de origen («09»); `ruta` es la ruta de la pantalla (p. ej. `/modulos/09-reclamos-postventa`); `fechaReferencia` es la fecha relevante (plazo, entrega, fin…).
- Orden: severidad (Alta, Media, Baja), después `fechaReferencia` ascendente y `clave`.
- `ResumenAlertasDto { total, altas, medias, bajas, descartadas, porTipo: TipoAlertasDto[] }` (sin contar descartadas salvo en `descartadas`), `TipoAlertasDto { tipo, alertas }` en el orden de la tabla.

**Reglas, en `UseCases` con `ReglaNegocioException`:**
1. `clave` tras `Trim()`: vacía → 400 «La clave de la alerta es obligatoria.»; más de 100 → 400 «La clave de la alerta no puede superar 100 caracteres.»
2. Descartar una clave que no corresponde a ninguna alerta activa ahora → 404 «La alerta {clave} no existe o ya no está activa.»; ya descartada → 409 «La alerta {clave} ya está descartada.» (también en la carrera, `23505`).
3. Restaurar una clave que no está descartada → 404 «La alerta {clave} no está descartada.»
4. Los filtros `tipo` y `severidad` se validan sin distinguir mayúsculas.

**Consultas.** Desde `Repository<T>().AsQueryable()`, asíncronas, proyectadas, con un número fijo de consultas por tipo (nunca una por registro). Comentarios `// PATRÓN LINQ N` donde se reutilice un patrón del Lab 08.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Reglas | tests: cada mensaje de §4 (reglas 1–4) con su código, literal y sin `Detail`; descartar, volver a descartar → 409, restaurar, restaurar otra vez → 404 | pasan |
| A3 | Generación | tests: por cada tipo, un caso que sí genera la alerta (con el mensaje literal exacto) y otro justo en el límite que no (p. ej. ocupación 89.99 %, contrato al 100.00 %, solicitud con 7 días, contrato que termina en 31 días); los resultados coinciden con los de sus módulos de origen; descartadas excluidas por defecto e incluidas con `incluirDescartadas=true`; orden y resumen exactos | pasan |
| A4 | Migración | `AlertasDescartadas` generada con `dotnet ef`; aplica sobre la BD con el seed; no altera tablas existentes | evidencia |
| A5 | PostgreSQL | smoke ampliado: listado con las alertas del seed (al menos `ReclamoVencido`, `OrdenAbastecimientoAtrasada` y `AlmacenCasiLleno` de ALM-LIM-01), filtro por severidad, resumen, descartar una → desaparece del listado, descartarla otra vez → 409, restaurarla → reaparece, descartar una clave inexistente → 404 | `SMOKE OK` |
| A6 | OpenAPI | `/openapi/v1.json` incluye las 4 operaciones | presentes |

## 6. Checklist de ejecución
- [x] Tarea 1: entidad, `DbContext` y migración.
- [x] Tarea 2: generadores por tipo, UseCases y controlador.
- [x] Tarea 3: tests y smoke.
- [x] Tarea 4: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-15 | A1 | ✅ | `./scripts/verify.sh` rc=0 en la rama de integración (`f9ca926`, mismas líneas que el auditado `d06da8d`): backend 496/496, frontend 222/222 |
| 2026-10-15 | A2–A3 | ✅ | `AlertasApiTests`: mensajes literales de clave, filtros, descartar/restaurar; los 7 tipos con sus mensajes y límites (89.99 % de ocupación, contrato que vence en 31 días, solicitud de 7 días) iguales a sus módulos de origen; orden y resumen |
| 2026-10-15 | A4 | ✅ | migración `AlertasDescartadas` (con `Designer` y snapshot) solo crea la tabla; aplicada en el smoke y en Neon |
| 2026-10-15 | A5 | ✅ | `./scripts/smoke-postgres.sh` → `SMOKE OK`: alertas del seed (orden OA-DEV-002 atrasada, reclamo vencido, almacén de Lima al 90 %), filtro por severidad, resumen 6 (2 altas), descartar → desaparece, otra vez → 409, restaurar → reaparece, clave inexistente → 404 |
| 2026-10-15 | Auditoría | ✅ | Auditoría, ciclo 1: APROBADO; recomendación menor no bloqueante: separar la generación de los 7 tipos en tests por tipo |
