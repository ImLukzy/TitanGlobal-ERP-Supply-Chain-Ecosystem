# Especificación propuesta: módulo 48 — Transportistas terceros (backend)

**Estado de revisión: propuesta, pendiente de aprobación de Lukas.**

## 1. Objetivo
**Problema:** el enunciado define «TransportistasTerceros (3PL y fletes)», pero no hay tablas, API ni reglas aprobadas para el módulo. Es previo a RutasDespacho según `docs/equipo/asignaciones.md`.
**Resultado propuesto:** mantener proveedores logísticos externos y registrar fletes manuales, con referencia, trayecto e importe declarado. El registro y su cancelación conservan el historial; no calculan tarifas ni generan documentos contables.

Responsable: Kelvin Pinto. Rama: `kelvin/modulo48-transportistas-terceros`, creada desde `origin/main`. **Estado: propuesta, pendiente de aprobación de Lukas.**

## 2. Fuera de alcance y decisiones pendientes
- Rutas, conductores, vehículos, guías, facturas, pagos y demás módulos; ninguna FK hacia las cuatro entidades base.
- Tarifarios, cálculo por km/kg, conversión de moneda, impuestos, liquidaciones e integraciones 3PL externas.
- Middleware, autenticación, pipeline, scripts fusionados y edición manual de archivos generados.
- Aplicar SQL en producción o declarar el módulo integrado/desplegado.

**Decisiones de producto propuestas, pendientes de aprobación de Lukas para su integración:**
1. Alcance propuesto: maestro de transportistas + registro/cancelación manual de fletes; no un motor de tarifas.
2. Identificación fiscal por país y documento libre (no exigir RUC peruano a empresas extranjeras). La unicidad propuesta compara país y documento sin mayúsculas ni espacios exteriores; el país se mantiene como texto, siguiendo el maestro Proveedores. Revisar si se requiere un código de país normalizado.
3. Moneda declarativa de tres letras, sin validar contra el módulo 74 todavía ni sumar importes de monedas diferentes.
4. Fechas de fletes históricas o futuras válidas, siempre UTC. Cancelación administrativa con motivo; sin efectos contables.

Estas decisiones son propuestas, no reglas ya presentes en el enunciado. La implementación local no sustituye la revisión y aprobación de Lukas.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `database/012-transportistas-terceros.sql` | creado y aplicado localmente | dos tablas, FK, índices y restricciones; los scripts 021/022 del 41/42 ya están integrados en main |
| `src/TitanGlobal.Domain/Models/TransportistaTercero.cs` | regenerar | exclusivamente scaffold |
| `src/TitanGlobal.Domain/Models/FleteTercero.cs` | regenerar | exclusivamente scaffold |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | DbSet y configuración; sin edición manual |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | ampliar | mapas explícitos TransportistasTerceros → TransportistaTercero y FletesTerceros → FleteTercero |
| `src/TitanGlobal.Api/Configuration/run/NombresNavegacion.cs` | ampliar si lo requiere el scaffold | solo la colección de fletes del nuevo transportista |
| `src/TitanGlobal.Application/DTOs/Modulo48_TransportistasTerceros/` | crear | entrada/salida, sin exponer entidades |
| `src/TitanGlobal.Application/UseCases/Modulo48_TransportistasTerceros/` | crear | validadores y UseCases públicos concretos en español |
| `src/TitanGlobal.Api/Controllers/Modulo48_TransportistasTerceros/TransportistasTercerosController.cs` | crear | controlador delgado |
| `src/TitanGlobal.Api/Controllers/Modulo48_TransportistasTerceros/FletesTercerosController.cs` | crear | controlador delgado |
| `tests/TitanGlobal.Api.Tests/Modulo48_TransportistasTerceros/` | crear | reglas, mensajes, consultas y estados |
| `tests/TitanGlobal.Api.Tests/Arquitectura/PluralizadorEspanolTests.cs` | ampliar | únicamente los nuevos nombres de tablas/entidades |
| `scripts/seed-dev.sql` | ampliar al final, antes de COMMIT | datos propios idempotentes |
| `scripts/smoke-postgres.sh` | ampliar | request_check y sus validadores, más restricciones reales |
| `docs/equipo/relevo.md` | actualizar | reserva 012 y evidencia, sin marcar integración |

Las carpetas nuevas se ubican en las cuatro capas actuales. Los tres archivos de configuración de scaffolding existen bajo `Configuration/run/`, aunque su namespace es `Configuration.Scaffolding`.

## 4. Diseño y lógica propuestos

### 4.1 Datos
**TransportistaTercero / TransportistasTerceros:** Id entero identidad; RazonSocial varchar(200) obligatoria; DocumentoFiscal varchar(30) obligatorio; Pais varchar(100) obligatorio; TipoServicio varchar(10), uno de `3PL`, `Flete`, `Ambos`; Contacto varchar(100), Email varchar(150), Telefono varchar(20) y Observaciones varchar(500), opcionales; Activo boolean, true por defecto.

Índice único `IX_TransportistasTerceros_PaisDocumentoNormalizados` sobre `lower(btrim(Pais)), lower(btrim(DocumentoFiscal))`. La aplicación recorta textos y conserva el formato solicitado; el índice protege también las carreras. TipoServicio es descriptivo, sin impedir registrar un flete de un proveedor 3PL activo.

**FleteTercero / FletesTerceros:** Id entero identidad; TransportistaTerceroId FK requerida con borrado restringido; Referencia varchar(30); Fecha timestamptz; Origen y Destino varchar(120); Importe numeric(18,2); Moneda varchar(3); Estado varchar(15), `Registrado` por defecto o `Cancelado`; Observaciones varchar(500) opcional; MotivoCancelacion varchar(500) y FechaCancelacion timestamptz, ambos nulos mientras está registrado.

Índice por FK e índice único `IX_FletesTerceros_TransportistaReferenciaNormalizada` sobre transportista y `lower(btrim(Referencia))`. CHECKs: importe positivo; estados/tipos válidos; coherencia de motivo/fecha de cancelación. Los registros no se borran ni editan después del alta: se cancelan con motivo. Los importes siempre se muestran junto a su moneda y no se convierten ni agregan entre monedas.

### 4.2 API y DTOs
JSON camelCase; errores `{ StatusCode, Message }` sin Detail en errores de negocio. JWT obligatorio, sin AllowAnonymous nuevo. POST y PUT reciben DTOs con cuerpo; la cancelación recibe motivo.

| Verbo | Ruta | OK |
|---|---|---|
| GET | `/api/v1/transportistas-terceros?busqueda=&pais=&tipoServicio=&activo=&pagina=1&tamanoPagina=20` | 200 `{ items, total, pagina, tamanoPagina }` |
| GET | `/api/v1/transportistas-terceros/{id}` | 200 detalle |
| POST | `/api/v1/transportistas-terceros` | 200 `{ id }` |
| PUT | `/api/v1/transportistas-terceros/{id}` | 200 `{ id }` |
| DELETE | `/api/v1/transportistas-terceros/{id}` | 204 solo sin fletes históricos |
| GET | `/api/v1/transportistas-terceros/resumen` | 200 `{ totalTransportistas, activos, inactivos, porTipoServicio }` |
| GET | `/api/v1/fletes-terceros?transportistaId=&busqueda=&estado=&pagina=1&tamanoPagina=20` | 200 página |
| GET | `/api/v1/fletes-terceros/{id}` | 200 detalle |
| POST | `/api/v1/fletes-terceros` | 200 `{ id }` |
| POST | `/api/v1/fletes-terceros/{id}/cancelacion` | 200 `{ id }` |

Listado de transportistas: `{ id, razonSocial, documentoFiscal, pais, tipoServicio, activo }`. Detalle añade contacto, email, teléfono y observaciones. Entrada de guardar contiene esos mismos campos salvo Id.

Listado de fletes: `{ id, transportistaId, transportistaRazonSocial, referencia, fecha, origen, destino, importe, moneda, estado }`. Detalle añade observaciones, motivoCancelacion y fechaCancelacion. Entrada de registrar contiene transportistaId, referencia, fecha, origen, destino, importe, moneda y observaciones; Estado se decide en Application, no en el DTO. Entrada de cancelación: `{ motivo }`.

Para conservar la precisión completa de numeric(18,2), el importe de las respuestas se propone como texto decimal invariante de dos posiciones (ej. `"1234.50"`). La entrada usa decimal del backend; el frontend valida que el número enviado represente exactamente los céntimos escritos y muestra un error local si no puede hacerlo. La presentación de respuestas no debe convertir importes grandes a Number ni redondearlos silenciosamente.

Consultas desde repositorios, asíncronas y proyectadas a DTO. Búsqueda sin mayúsculas: razón social/documento fiscal, o referencia/origen/destino del flete. País sin mayúsculas; tipos/estados validados. Orden estable: razón social/Id en maestros, fecha descendente/Id descendente en fletes. Resumen agregado en consulta, sin cargar la tabla completa.

### 4.3 Reglas y mensajes literales, en orden
Para GET/PUT/DELETE de un recurso inexistente: 404 «El transportista {id} no existe.» o «El flete {id} no existe.». PUT busca el transportista antes de validar su contenido.

**Guardar transportista**, textos recortados y opcionales vacíos → null:
1. Razón social vacía → 400 «La razón social es obligatoria.»; >200 → «La razón social no puede superar 200 caracteres.»
2. Documento vacío → 400 «El documento fiscal es obligatorio.»; >30 → «El documento fiscal no puede superar 30 caracteres.»
3. País vacío → 400 «El país es obligatorio.»; >100 → «El país no puede superar 100 caracteres.»
4. Tipo distinto de 3PL/Flete/Ambos, aceptando variantes de mayúsculas y guardando valor canónico → 400 «El tipo de servicio debe ser 3PL, Flete o Ambos.»
5. Contacto >100 → 400 «El contacto no puede superar 100 caracteres.»
6. Email >150 → 400 «El email no puede superar 150 caracteres.»; no válido según MailAddress.TryCreate → «El email no es válido.»
7. Teléfono >20 → 400 «El teléfono no puede superar 20 caracteres.»
8. Observaciones >500 → 400 «Las observaciones no pueden superar 500 caracteres.»
9. País/documento duplicados, excluyendo Id propio → 409 «Ya existe un transportista con el documento fiscal {documentoFiscal} en {pais}.», también ante unicidad en UnitOfWork.
10. DELETE con cualquier flete histórico, incluso cancelado → 409 «No se puede eliminar un transportista con fletes registrados.»; misma respuesta ante carrera de FK. La desactivación mediante PUT conserva los fletes y permite cancelarlos.

**Registrar flete:**
1. transportistaId <=0 → 400 «El transportista es obligatorio.»
2. Referencia vacía → 400 «La referencia del flete es obligatoria.»; >30 → «La referencia del flete no puede superar 30 caracteres.»
3. Fecha por defecto o DateTime.MaxValue → 400 «La fecha del flete no es válida.»; las demás fechas válidas se normalizan con FechasUtc, incluida una fecha sin Z.
4. Origen vacío → 400 «El origen es obligatorio.»; >120 → «El origen no puede superar 120 caracteres.»
5. Destino vacío → 400 «El destino es obligatorio.»; >120 → «El destino no puede superar 120 caracteres.»; un trayecto dentro de la misma localidad es válido.
6. Importe <=0 → 400 «El importe debe ser mayor que cero.»; >9999999999999999.99 → «El importe no puede superar 9999999999999999.99.»; más de dos decimales → «El importe no puede tener más de 2 decimales.»
7. Moneda, tras Trim y mayúsculas, no son tres letras ASCII → 400 «La moneda debe tener tres letras.»
8. Observaciones >500 → 400 «Las observaciones no pueden superar 500 caracteres.»
9. Transportista inexistente → 404 literal; inactivo → 409 «No se pueden registrar fletes para un transportista inactivo.»
10. Referencia duplicada para ese transportista, sin mayúsculas → 409 «Ya existe un flete con la referencia {referencia} para este transportista.», también ante carrera de unicidad. La misma referencia con otro transportista es válida. Carrera de FK → 409 «El transportista indicado ya no está disponible.»

**Cancelar flete:** buscar Id; ya cancelado → 409 «El flete {id} ya está cancelado.»; motivo recortado vacío → 400 «El motivo de cancelación es obligatorio.»; >500 → «El motivo de cancelación no puede superar 500 caracteres.». Guardar estado Cancelado, motivo y DateTime.UtcNow. El transportista inactivo no impide cancelar un flete histórico.

**Listados:** pagina <1 → 400 «El número de página debe ser mayor que cero.»; tamaño fuera de 1–100 → «El tamaño de página debe estar entre 1 y 100.». Filtro tipo inválido → mensaje literal del tipo; estado inválido → «El estado debe ser Registrado o Cancelado.»; filtro transportistaId no positivo → «El transportista es obligatorio.»

Solo ReglaNegocioException/CodigosEstado y ViolacionRestriccionException en Application, sin capturar EF/Npgsql ni introducir lógica de negocio en controllers. Sin var ni miembros con cuerpo de expresión. Las lambdas empiezan en la misma línea que Where/Any/Count/configuración.

## 5. Criterios de aceptación
| # | Criterio | Cómo se verifica |
|---|---|---|
| A1 | Compilación, formato, capas y regresiones | `./scripts/verify.sh`, sin errores/advertencias de compilación |
| A2 | Reglas exactas | integración para cada mensaje, límites, Trim, UTC sin Z, duplicados y estados |
| A3 | Consultas | búsqueda, todos los filtros, paginación estable y resumen exacto con datos conocidos |
| A4 | Contrato | OpenAPI documenta las diez operaciones y no expone entidades |
| A5 | Persistencia | aplicar-bd y scaffold; comprobación sin diferencias; seed repetible; FK e índices contra PostgreSQL real |
| A6 | Smoke | request_check y validadores de altas, consultas, edición/baja del maestro, duplicados, cancelación, repetición y carrera SQL → `SMOKE OK` |

Seed propuesto: seis transportistas con servicios y países variados, uno inactivo; cuatro fletes en PEN y USD, uno cancelado. Sin monedas convertidas ni totales combinados. Datos de ejemplo idempotentes al final del seed, antes de COMMIT; ningún secreto.

## 6. Checklist de ejecución
- [ ] Lukas revisa las cuatro decisiones de §2 y aprueba esta spec.
- [x] Script nuevo, base local aplicada y scaffold, sin edición manual.
- [x] DTOs, casos de uso y controllers.
- [x] Integración, seed y smoke propios del 48.
- [x] A1–A6 verificados localmente; evidencia en §7, sin sustituir la revisión de Lukas.

Commits por avances funcionales con sus pruebas, no por archivo, usando la identidad Git de Kelvin. Sin push a main ni reescritura de historial.

## 7. Registro de verificación
| Fecha | Estado | Evidencia |
|---|---|---|
| 2026-11-21 | Propuesta, sin aprobación ni implementación | Asignación y orden confirmados; alcance disponible en el enunciado limitado a «3PL y fletes»; rutas y capas contrastadas con el repositorio |
| 2026-11-21 | Implementado y verificado localmente como propuesta, pendiente de aprobación de Lukas; aprobación de Lukas pendiente | 53 pruebas propias del backend, diez operaciones OpenAPI, seed repetido sin duplicados, scaffold comprobado y smoke contra PostgreSQL; detalles en `docs/evidencias/modulo48/README.md` |

### Preparación de revisión — 2026-11-22

Rama actualizada con `git pull origin main`, conservando la tabla oficial de relevo y modificando solo la línea de Kelvin y la reserva propia. Script vigente: `database/012-transportistas-terceros.sql`. Propuesta, pendiente de aprobación de Lukas. Base PostgreSQL local recreada, seed cargado y entidades/contexto regenerados mediante scaffold, sin edición manual ni diferencias de contenido.

`./scripts/verify.sh`: salida0, 1138 pruebas backend y 580 frontend en 73 archivos. `./scripts/smoke-postgres.sh`: salida0, scaffold coincidente y SMOKE OK. Se usó VITEST_MAX_WORKERS=1 temporal, sin modificar pruebas, timeouts ni configuración compartida. Persisten los avisos globales de ESLint9/Vite; no se cambiaron dependencias ni umbrales.

No se generaron capturas nuevas ni se cambió la pantalla para estos ajustes. Se conserva la evidencia visual previa. Preparación local: sin nuevos PR, sin push y sin integración. Tras cada fusión anterior habrá que incorporar main y repetir estas verificaciones antes del PR correspondiente.

### Actualización para revisión — 2026-11-23

- Rama actualizada con `origin/main` (0b0324ff0863a4b5ab6c977e649c7840eb199caa) después de integrar el PR #59 del módulo 42.
- `relevo.md` idéntico a main. Se conservaron las pantallas y ambos bloques de CSS, además del seed de evaluaciones de proveedores; contexto regenerado con el scaffold oficial desde la base combinada.
- Base local reiniciada, seis scripts aplicados y seed cargado. Script propio: `012-transportistas-terceros.sql`; aplicación en la demo pendiente de Lukas.
- `./scripts/verify.sh`: exit 0; 1191 pruebas backend, 695 pruebas frontend en 84 archivos; .NET con cero warnings, lint, tipos y build correctos.
- `./scripts/smoke-postgres.sh`: exit 0, `SCAFFOLD AL DÍA` y `SMOKE OK`, incluidos los flujos del módulo 48 y los módulos existentes.
- El frontend conserva los avisos existentes de obsolescencia de ESLint 9 y tamaño del bundle de Vite. La evidencia visual del 2026-11-21 sigue disponible; esta actualización conserva el bloque visual del 48.
- Propuesta, pendiente de aprobación de Lukas. No se han aplicado scripts contra producción.
