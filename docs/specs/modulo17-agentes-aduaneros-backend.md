# Especificación: modulo17-agentes-aduaneros-backend — Agentes aduaneros

## 1. Objetivo
**Problema:** los expedientes de importación (módulo 16) llegan a aduana, pero no hay padrón de los agentes de aduana que los despachan ni registro de la declaración (DAM/DUA) con sus derechos de arancel.
**Resultado esperado:** dos entidades nuevas, `AgenteAduanero` y `DeclaracionAduana`, con sus tablas y `Modulo17_AgentesAduaneros` en `api/v1/agentes-aduaneros` y `api/v1/declaraciones-aduana`:
- padrón de agentes: alta, edición, activación y baja;
- registrar la declaración de un expediente, numerarla (canal de control), levantarla (autorizar la salida de la mercadería) o anularla;
- derechos de arancel calculados por el servidor (valor aduanero × tasa);
- listados con filtros y paginación, detalle y resumen.

Sigue el patrón de `Modulo11_Proveedores` (padrón con activar y desactivar) y de `Modulo14_Licitaciones` (estados con acciones explícitas). Depende de `Modulo16_Importaciones`.

Responsable: Sergio. Estado: implementado, en revisión de Lukas (PR #97).

## 2. Fuera de alcance
- Las 4 entidades del enunciado, los middlewares y el pipeline.
- Cambios en `Modulo16_Importaciones`: liberar un expediente **no exige** una declaración levantada, ni levantar una declaración cambia el expediente. Se podrá exigir cuando ambos módulos estén integrados.
- Catálogo de partidas arancelarias: la tasa se escribe en cada declaración.
- IGV, percepción y demás tributos de la importación; el costo de nacionalización se prorratea en el módulo 18.
- Conexión con el sistema de la autoridad aduanera.
- El frontend (spec `modulo17-agentes-aduaneros-frontend.md`, posterior).
- Scripts de `database/` que no sean el de §3.

**Decisiones de producto que requieren aprobación antes de ejecutar:**
1. **Dos recursos en un módulo:** el padrón de agentes y las declaraciones, cada uno con su controlador.
2. **Agente:** identificado por RUC (11 dígitos, único) y por su número de registro (único). Solo agentes activos pueden recibir declaraciones nuevas.
3. **Una declaración vigente por expediente:** puede haber varias anuladas, pero solo una que no lo esté (índice único filtrado).
4. **Expedientes elegibles:** solo en estado `EnTransito` o `EnAduana`.
5. **Regímenes:** `ImportacionConsumo`, `AdmisionTemporal` y `Deposito`.
6. **Estados:** `Registrada` → `Numerada` → `Levantada`; `Registrada` o `Numerada` → `Anulada`. `Levantada` y `Anulada` son finales.
7. **Canal de control** (`Verde`, `Naranja`, `Rojo`): se asigna al numerar, no antes.
8. **Los derechos de arancel los calcula el servidor:** `valorAduanero × tasaArancelaria / 100`, redondeado a 2 decimales; el cliente no los envía.
9. **Solo se elimina un agente sin declaraciones;** si las tiene, se desactiva. Una declaración no se elimina, se anula.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `database/028-agentes-aduaneros.sql` | crear | `CREATE TABLE "AgentesAduaneros"` y `"DeclaracionesAduana"`; número `028` reservado en `docs/equipo/relevo.md` §8 |
| `docs/equipo/relevo.md` | modificar | solo añadir la fila 028, sin reescribir la tabla |
| `src/TitanGlobal.Domain/Models/AgenteAduanero.cs`, `DeclaracionAduana.cs`, `ExpedienteImportacion.cs` | regenerar | entidades `partial` generadas por `./scripts/scaffold.sh`, sin editarlas a mano |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | `DbSet` de las dos entidades, generado |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | modificar | solo si el scaffold nombra mal las entidades (`AgenteAduanero`, `DeclaracionAduana`) |
| `src/TitanGlobal.Application/DTOs/Modulo17_AgentesAduaneros/*.cs` | crear | DTOs de entrada y salida (§4) |
| `src/TitanGlobal.Application/UseCases/Modulo17_AgentesAduaneros/*.cs` | crear | un caso de uso por operación, `ValidacionAgenteAduanero` y `ValidacionDeclaracionAduana` |
| `src/TitanGlobal.Api/Controllers/Modulo17_AgentesAduaneros/AgentesAduanerosController.cs`, `DeclaracionesAduanaController.cs` | crear | controladores delgados; las acciones sin cuerpo llevan `[PermitirCuerpoVacio]` |
| `tests/TitanGlobal.Api.Tests/Modulo17_AgentesAduaneros/AgentesAduanerosApiTests.cs`, `DeclaracionesAduanaApiTests.cs` | crear | pruebas de integración con InMemory, datos propios del test |
| `scripts/seed-dev.sql` | modificar | 4 agentes y 4 declaraciones idempotentes al final (§4) |
| `scripts/smoke-postgres.sh` | modificar | rutas del módulo (§5 A7) |

## 4. Diseño y lógica
**Tabla `AgentesAduaneros`:**

| Columna | Tipo | Regla |
|---|---|---|
| `Id` | `integer` identidad | clave primaria |
| `Ruc` | `character varying(11) NOT NULL` | único (`IX_AgentesAduaneros_Ruc`) |
| `RazonSocial` | `character varying(200) NOT NULL` | |
| `NumeroRegistro` | `character varying(20) NOT NULL` | único (`IX_AgentesAduaneros_NumeroRegistro`) |
| `Email` | `character varying(150)` | opcional |
| `Telefono` | `character varying(20)` | opcional |
| `Activo` | `boolean NOT NULL` | |
| `FechaRegistro` | `timestamp with time zone NOT NULL` | UTC, la pone el servidor |

**Tabla `DeclaracionesAduana`:**

| Columna | Tipo | Regla |
|---|---|---|
| `Id` | `integer` identidad | clave primaria |
| `Numero` | `character varying(30) NOT NULL` | único (`IX_DeclaracionesAduana_Numero`) |
| `ExpedienteImportacionId` | `integer NOT NULL` | clave foránea a `ExpedientesImportacion("Id")` `ON DELETE RESTRICT` |
| `AgenteAduaneroId` | `integer NOT NULL` | clave foránea a `AgentesAduaneros("Id")` `ON DELETE RESTRICT` |
| `Regimen` | `character varying(30) NOT NULL` | `CHECK` en los 3 valores de la decisión 5 |
| `ValorAduanero` | `numeric(18,2) NOT NULL` | `CHECK` > 0 |
| `TasaArancelaria` | `numeric(5,2) NOT NULL` | `CHECK` entre 0 y 100 |
| `DerechosArancel` | `numeric(18,2) NOT NULL` | `CHECK` >= 0; lo calcula el servidor |
| `Canal` | `character varying(10)` | `CHECK` en (`Verde`, `Naranja`, `Rojo`); solo desde `Numerada` |
| `Estado` | `character varying(20) NOT NULL` | `CHECK` en (`Registrada`, `Numerada`, `Levantada`, `Anulada`) |
| `FechaNumeracion`, `FechaLevante` | `timestamp with time zone` | UTC |
| `MotivoAnulacion` | `character varying(500)` | solo con estado `Anulada` |
| `FechaRegistro` | `timestamp with time zone NOT NULL` | UTC, la pone el servidor |

Índice único filtrado `IX_DeclaracionesAduana_Expediente_Vigente` sobre `ExpedienteImportacionId` con `WHERE "Estado" <> 'Anulada'`. Los `CHECK` siguen la convención `CK_<Tabla>_<Regla>`.

**Contrato.** JSON en camelCase; errores `{ StatusCode, Message }` sin `Detail`.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/agentes-aduaneros?busqueda=&activo=&pagina=1&tamanoPagina=20` | todos opcionales | 200 `PaginaAgentesDto` | 400 |
| GET | `api/v1/agentes-aduaneros/{id:int}` | — | 200 `AgenteDto` | 404 |
| POST | `api/v1/agentes-aduaneros` | `GuardarAgenteRequestDto` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/agentes-aduaneros/{id:int}` | `GuardarAgenteRequestDto` | 200 `{ id }` | 400, 404, 409 |
| POST | `api/v1/agentes-aduaneros/{id:int}/activar` y `/desactivar` | sin cuerpo | 200 `{ id }` | 404, 409 |
| DELETE | `api/v1/agentes-aduaneros/{id:int}` | — | 204 | 404, 409 |
| GET | `api/v1/declaraciones-aduana?busqueda=&estado=&canal=&pagina=1&tamanoPagina=20` | todos opcionales | 200 `PaginaDeclaracionesDto` | 400 |
| GET | `api/v1/declaraciones-aduana/{id:int}` | — | 200 `DeclaracionDetalleDto` | 404 |
| POST | `api/v1/declaraciones-aduana` | `CrearDeclaracionRequestDto` | 200 `{ id }` | 400, 404, 409 |
| PUT | `api/v1/declaraciones-aduana/{id:int}` | `ActualizarDeclaracionRequestDto` | 200 `{ id }` | 400, 404, 409 |
| POST | `api/v1/declaraciones-aduana/{id:int}/numerar` | `NumerarDeclaracionRequestDto { canal }` | 200 `{ id }` | 400, 404, 409 |
| POST | `api/v1/declaraciones-aduana/{id:int}/levantar` | sin cuerpo | 200 `{ id }` | 404, 409 |
| POST | `api/v1/declaraciones-aduana/{id:int}/anular` | `AnularDeclaracionRequestDto { motivo }` | 200 `{ id }` | 400, 404, 409 |
| GET | `api/v1/declaraciones-aduana/resumen` | — | 200 `ResumenDeclaracionesDto` | — |

**DTOs:**
- `GuardarAgenteRequestDto { ruc, razonSocial, numeroRegistro, email?, telefono? }`
- `AgenteDto { id, ruc, razonSocial, numeroRegistro, email, telefono, activo, totalDeclaraciones, fechaRegistro }`
- `CrearDeclaracionRequestDto { numero, expedienteImportacionId, agenteAduaneroId, regimen, valorAduanero, tasaArancelaria }`
- `ActualizarDeclaracionRequestDto { agenteAduaneroId, regimen, valorAduanero, tasaArancelaria }` (número y expediente no se editan)
- `DeclaracionListadoDto { id, numero, expedienteNumero, agenteRazonSocial, regimen, canal, estado, derechosArancel }`
- `DeclaracionDetalleDto { id, numero, expedienteImportacionId, expedienteNumero, agenteAduaneroId, agenteRazonSocial, regimen, valorAduanero, tasaArancelaria, derechosArancel, canal, estado, fechaNumeracion, fechaLevante, motivoAnulacion, fechaRegistro }`
- `ResumenDeclaracionesDto { totalDeclaraciones, derechosArancelVigentes, porEstado: EstadoConteoDto[] { estado, total } }` (`porEstado` siempre trae los 4 estados en el orden de la decisión 6; `derechosArancelVigentes` suma las que no están anuladas).

**Reglas y mensajes** (400 en validación; 404 si el recurso no existe; 409 en conflictos). Las búsquedas son sin distinguir mayúsculas: agentes por RUC y razón social; declaraciones por número y número de expediente.

*Agente (alta y edición):* `ruc` que no sean 11 dígitos → 400 «El RUC debe tener 11 dígitos.»; `razonSocial` en blanco → 400 «La razón social es obligatoria.» (máximo 200 → «La razón social no puede superar 200 caracteres.»); `numeroRegistro` en blanco → 400 «El número de registro es obligatorio.» (máximo 20); `email` con formato inválido → 400 «El correo no es válido.»; `telefono` más de 20 → 400 «El teléfono no puede superar 20 caracteres.» RUC o registro repetidos → 409 «Ya existe un agente con el RUC {ruc}.» / «Ya existe un agente con el número de registro {numeroRegistro}.» (también ante `23505`). El agente nace activo. Id inexistente → 404 «El agente aduanero {id} no existe.» Activar uno activo → 409 «El agente aduanero {id} ya está activo.»; desactivar uno inactivo → 409 «El agente aduanero {id} ya está inactivo.» Eliminar con declaraciones → 409 «El agente aduanero {id} tiene declaraciones y no se puede eliminar; desactívelo.»

*POST declaración*, en este orden:
1. `numero` en blanco → 400 «El número es obligatorio.»; más de 30 → 400 «El número no puede superar 30 caracteres.» (se guarda con `Trim()` y en mayúsculas).
2. `regimen` fuera de los 3 valores → 400 «El régimen debe ser ImportacionConsumo, AdmisionTemporal o Deposito.»
3. `valorAduanero` ≤ 0 → 400 «El valor aduanero debe ser mayor a 0.»; con más de 2 decimales o más de 16 dígitos enteros → 400 «El valor aduanero no es válido.»
4. `tasaArancelaria` fuera de 0–100 → 400 «La tasa arancelaria debe estar entre 0 y 100.»; con más de 2 decimales → 400 «La tasa arancelaria no es válida.»
5. `expedienteImportacionId` inexistente → 404 «El expediente {id} no existe.»; estado distinto de `EnTransito` y `EnAduana` → 409 «El expediente {id} no admite declaraciones en estado {estado}.»
6. `agenteAduaneroId` inexistente → 404 «El agente aduanero {id} no existe.»; inactivo → 409 «El agente aduanero {id} está inactivo.»
7. número repetido → 409 «Ya existe una declaración con el número {numero}.»; expediente con otra declaración no anulada → 409 «El expediente {id} ya tiene la declaración {numero}.»
Nace `Registrada` y calcula `DerechosArancel`.

*PUT declaración:* 404 «La declaración {id} no existe.»; estado distinto de `Registrada` → 409 «La declaración {id} no está registrada.»; luego las reglas 2, 3, 4 y 6, y recalcula los derechos.
*Numerar:* 404; estado distinto de `Registrada` → 409 «La declaración {id} no está registrada.»; `canal` inválido → 400 «El canal debe ser Verde, Naranja o Rojo.» Guarda canal y fecha; pasa a `Numerada`.
*Levantar:* 404; estado distinto de `Numerada` → 409 «La declaración {id} no está numerada.» Guarda la fecha; pasa a `Levantada`.
*Anular:* 404; `motivo` en blanco → 400 «El motivo de anulación es obligatorio.» (máximo 500); estado `Levantada` o `Anulada` → 409 «La declaración {id} no se puede anular porque está {Levantada|Anulada}.»
*Listados:* 400 «La página debe ser mayor o igual a 1.», «El tamaño de página debe estar entre 1 y 100.», «El estado debe ser Registrada, Numerada, Levantada o Anulada.», «El canal debe ser Verde, Naranja o Rojo.» Agentes ordenados por razón social; declaraciones por fecha de registro descendente.

**Consultas.** Desde `Repository<...>().AsQueryable()`, asíncronas y proyectadas a DTO en la consulta (sin N+1); `totalDeclaraciones` del agente sale de un conteo en la misma consulta. Las acciones de estado cargan la declaración, validan y guardan con `CompleteAsync`.

**Seed de desarrollo** (`seed-dev.sql`, bloque `TRUNCATE ... RESTART IDENTITY CASCADE` y `INSERT`s que buscan el expediente por su número; va después del bloque de importaciones):

| Agente | RUC | Razón social | Registro | Activo |
|---|---|---|---|---|
| 1 | 20512345678 | Aduanas del Pacífico S.A.C. | AA-0101 | sí |
| 2 | 20523456789 | Despachos Andinos S.A.C. | AA-0102 | sí |
| 3 | 20534567890 | Logística Aduanera Perú S.A.C. | AA-0103 | sí |
| 4 | 20545678901 | Agencia Norte S.A.C. | AA-0104 | no |

| Declaración | Expediente | Agente | Régimen | Valor | Tasa | Estado |
|---|---|---|---|---|---|---|
| 118-2026-10-000101 | IMP-2026-001 | 1 | ImportacionConsumo | 50000.00 | 6.00 | Registrada |
| 118-2026-10-000102 | IMP-2026-003 | 2 | ImportacionConsumo | 20000.00 | 0.00 | Numerada, canal Naranja |
| 118-2026-02-000045 | IMP-2026-004 | 1 | ImportacionConsumo | 12000.00 | 11.00 | Levantada, canal Verde |
| 118-2026-10-000103 | IMP-2026-005 | 3 | AdmisionTemporal | 8000.00 | 4.00 | Anulada («Expediente cancelado») |

Resumen esperado del seed: 4 agentes (3 activos), 4 declaraciones, derechos vigentes 4320.00 (3000.00 + 0.00 + 1320.00; la anulada no suma) y conteo por estado Registrada 1 / Numerada 1 / Levantada 1 / Anulada 1.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Gate | `./scripts/verify.sh` (formato, reglas de código y capas) | en verde |
| A3 | Padrón de agentes | tests: cada mensaje de alta, edición, activación y baja de §4 con su código, literal y sin `Detail`; RUC y registro repetidos | pasan |
| A4 | Reglas de declaración | tests: cada mensaje del alta y la edición; derechos calculados por el servidor (50000 × 6 % = 3000.00); expediente en estado no elegible; agente inactivo; segunda declaración vigente sobre el mismo expediente y nueva declaración tras anular la anterior | pasan |
| A5 | Estados | tests: registrada → numerada → levantada, anular desde `Registrada` y `Numerada`, y cada transición inválida con su 409 literal; canal solo al numerar | pasan |
| A6 | Consultas | tests con datos conocidos: filtros, búsqueda, paginación con `total`, `totalDeclaraciones` y resumen exacto del seed | pasan |
| A7 | Datos y PostgreSQL | el script aplica con `./scripts/aplicar-bd.sh`, `./scripts/scaffold.sh --comprobar` no da diferencias y `./scripts/smoke-postgres.sh` prueba ambos recursos: listados, filtros, detalle, altas, numerar, levantar, anular y duplicados | `SMOKE OK` |
| A8 | OpenAPI | `/openapi/v1.json` incluye las 15 operaciones | presentes |

## 6. Checklist de ejecución
*(Marca `[x]` solo cuando la tarea esté hecha **y** A1–A2 estén en verde.)*

- [x] Tarea 1: script SQL, aplicarlo y generar entidades con scaffold.
- [x] Tarea 2: DTOs, validaciones, casos de uso y controladores.
- [x] Tarea 3: tests, seed y smoke.
- [x] Tarea 4: verificar A1–A8 y anotar resultados en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-11-25 | A1, A2 | cumple | `./scripts/verify.sh` en verde (formato, reglas de código, compilación con `-warnaserror`, tests de backend y frontend) |
| 2026-11-25 | A3, A4, A5, A6 | cumple | `AgentesAduanerosApiTests` y `DeclaracionesAduanaApiTests` dentro de `./scripts/verify.sh` |
| 2026-11-25 | A7 | cumple | `./scripts/aplicar-bd.sh --reiniciar`, `./scripts/scaffold.sh` y `./scripts/smoke-postgres.sh` terminan en `SMOKE OK` |
| 2026-11-25 | A8 | cumple | el humo valida las 15 operaciones en `/openapi/v1.json` (`openapi-agentes-aduaneros`) |
