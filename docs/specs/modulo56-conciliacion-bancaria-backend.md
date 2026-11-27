# Especificación: modulo56-conciliacion-bancaria-backend — Conciliación bancaria

> Estado: **aprobada** por Lukas el 2026-11-23 para la v0. Responsable: Leonel Portillo. La revisión final del [PR #52](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52) sigue pendiente.

## 1. Objetivo

Cargar extractos bancarios por JSON, asociar cada ítem con un movimiento interno mediante una coincidencia explícita y cerrar el extracto conciliado con diferencia cero.

### Base comprobada y dependencias

- **Previa.** El [55](modulo55-tesoreria-bancos-backend.md) proporciona cuenta, moneda, tipo e importe de movimientos y marca de reversión. El 55 todavía se implementará y el 56 requiere que existan ese esquema y ese contrato.
- **Guarda compartida.** El PR del 56 completa la consulta de coincidencia en el reverso del 55. Vincular y revertir comparten garantía transaccional, conservando los datos de tesorería.

## 2. Fuera de alcance

- Archivos bancarios, conexión con bancos, coincidencia automática, tolerancias, agrupaciones, conversión y generación de movimientos.
- Reapertura, cierre parcial, borrado físico y contabilidad.

**Decisiones de producto aprobadas para la v0.** [Respuesta de Lukas](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52#issuecomment-7109010161)

| Nº | Tema | Respuesta aprobada |
|---|---|---|
| 56.1 | Carga y edición | Recibir el extracto por JSON. Un extracto por cuenta y periodo, con prohibición de intervalos superpuestos. Los ítems se editan solo en extractos abiertos. |
| 56.2 | Coincidencia | Vinculación manual uno a uno. Coinciden el signo y el importe exacto. Excluir movimientos revertidos. |
| 56.3 | Cierre | El cierre exige conciliación completa con diferencia cero. No se reabre. |
| 56.4 | Acceso y reversión | Aplicar G1. El 55 devuelve 409 al intentar revertir un movimiento conciliado. |

Las [reglas comunes G1 a G9](../equipo/revision-finanzas-52-60.md#reglas-generales-g1-a-g9) se aplican junto con las precisiones de §4.

## 3. Archivos afectados

| Archivo previsto | Acción futura | Alcance |
|---|---|---|
| `database/NNN-modulo56-conciliacion-bancaria.sql` y `docs/equipo/relevo.md` | Crear / registrar | Tomar el siguiente número libre al implementar y registrar solo la fila de este módulo. [Relevo §8](../equipo/relevo.md#8-números-de-los-scripts-de-base-de-datos) |
| `src/TitanGlobal.Domain/Models/` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | Generar | Entidades y navegaciones de §4 mediante scaffold. Revisar los nombres que produce el pluralizador. |
| `src/TitanGlobal.Application/DTOs/Modulo56_ConciliacionBancaria/` y `src/TitanGlobal.Application/UseCases/Modulo56_ConciliacionBancaria/` | Crear | Validación, proyecciones y casos de uso concretos en Application. |
| `src/TitanGlobal.Api/Controllers/Modulo56_ConciliacionBancaria/ConciliacionBancariaController.cs` | Crear | Rutas y autorización de §4. |
| Reversión de movimiento del 55 y sus pruebas | Completar con el 56 | Aplicar la guarda 55.4 bajo la misma transacción usada para asociar movimientos. |
| `tests/TitanGlobal.Api.Tests/Modulo56_ConciliacionBancaria/ConciliacionBancariaApiTests.cs` | Crear | Pruebas HTTP de §4. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | Ampliar | Fixtures idempotentes y checks HTTP/SQL del módulo. |

## 4. Diseño y lógica

Los nombres, tamaños auxiliares, rutas y mecanismos de persistencia concretan el diseño para la revisión final. Las decisiones de producto aprobadas están en §2 y G1 a G9.

### Convenciones y permisos

Controller → UseCase → IUnitOfWork → Repository → DbContext → PostgreSQL. DTOs en camelCase. Las lecturas requieren JWT. Las escrituras requieren `FINANZAS` (Id 7, seed del 52) o `ADMIN` mediante `[Authorize(Roles = "FINANZAS,ADMIN")]`. Los 401, 403 y el 400 de cuerpo vacío conservan los literales del pipeline. Las acciones sin cuerpo usan `[PermitirCuerpoVacio]`. Las fechas de negocio usan `DateOnly` y `date` con JSON `yyyy-MM-dd`. Los instantes del servidor usan `timestamptz` y `FechasUtc`.

Los importes usan `numeric(18,2)` en la moneda de la cuenta. Las fechas de extracto e ítems son fechas efectivas y su extremo superior no supera hoy. Los extractos se identifican por cuenta/periodo y se consultan por id.

### Datos y persistencia

| Tabla → entidad | Campos y restricciones |
|---|---|
| `ExtractosBancarios` → `ExtractoBancario` | `Id`, `CuentaBancariaId` FK, `FechaDesde date`, `FechaHasta date`, `SaldoInicial`, `SaldoFinal`, `Estado varchar(20)` (`Abierto`, `Cerrado`), `FechaRegistro timestamptz`, `FechaCierre timestamptz?`. Índice único por cuenta y ambos extremos. Comprobar `FechaDesde <= FechaHasta` y ausencia de superposición por cuenta. |
| `LineasExtractoBancario` → `LineaExtractoBancario` | `Id`, `ExtractoBancarioId` FK, `Posicion integer`, `FechaOperacion date`, `Tipo varchar(20)` (`Ingreso`/`Egreso`), `Importe` positivo y `Referencia varchar(100)?`. Unicidad por extracto/posición. |
| `CoincidenciasConciliacion` → `CoincidenciaConciliacion` | `Id`, `LineaExtractoBancarioId` FK única, `MovimientoBancarioId` FK única y `FechaVinculacion timestamptz`. Cada fila asocia una línea con un movimiento del 55. |

Las PK son identidades `integer`. Las FKs se indexan y conservan referencias con `ON DELETE RESTRICT`. Application valida los límites de columna antes de guardar.

Contrato técnico de diferencia propuesto para revisión: saldo final declarado − saldo inicial declarado − suma con signo de los movimientos vinculados (`Ingreso` positivo, `Egreso` negativo). Con apertura 100, ingreso 20 y egreso 5, el cierre requiere saldo final 115 y todas las líneas vinculadas. Un saldo final declarado de 116 deja diferencia 1.

### Casos de uso, rutas y DTOs

Ruta base `/api/v1/conciliacion-bancaria`. Las rutas de consulta proyectan con `AsNoTracking`. Listar extractos por `cuentaBancariaId?` y `estado?`, ordenados por `FechaHasta` e `Id` descendentes. Ordenar los ítems por posición. Paginación 1/20, máximo 100.

| Método y ruta relativa | Caso de uso | Entrada → éxito |
|---|---|---|
| `GET /extractos` | `ListarExtractosBancariosUseCase` | Filtros y paginación → `200 PaginaExtractosBancariosDto`. |
| `POST /extractos` | `RegistrarExtractoBancarioUseCase` | `RegistrarExtractoBancarioRequestDto` → `201` detalle y `Location`. |
| `GET /extractos/{id:int}` | `ObtenerExtractoBancarioUseCase` | `200 ExtractoBancarioDetalleDto`. |
| `PUT /extractos/{id:int}/lineas/{lineaId:int}` | `ActualizarLineaExtractoBancarioUseCase` | `GuardarLineaExtractoBancarioRequestDto` → `200 LineaExtractoBancarioDto`. |
| `GET /extractos/{id:int}/pendientes` | `ListarPendientesConciliacionUseCase` | `lado=Extracto` o `Banco` y paginación → `200 PaginaPendientesConciliacionDto`. |
| `POST /extractos/{id:int}/coincidencias` | `VincularCoincidenciaConciliacionUseCase` | `VincularCoincidenciaRequestDto { lineaExtractoBancarioId, movimientoBancarioId }` → `201 CoincidenciaConciliacionDto`. |
| `POST /extractos/{id:int}/cerrar` | `CerrarConciliacionBancariaUseCase` | Sin cuerpo → `200 ExtractoBancarioDetalleDto`. |

- `RegistrarExtractoBancarioRequestDto { cuentaBancariaId, fechaDesde, fechaHasta, saldoInicial, saldoFinal, lineas }`. `GuardarLineaExtractoBancarioRequestDto { posicion, fechaOperacion, tipo, importe, referencia? }`.
- `ExtractoBancarioListadoDto { id, cuentaBancariaId, moneda, fechaDesde, fechaHasta, estado, diferencia }`. El detalle añade saldos declarados, `fechaCierre`, ítems y sus coincidencias. `LineaExtractoBancarioDto` agrega `id` y `movimientoBancarioId?` a los campos de entrada.
- `CoincidenciaConciliacionDto { id, lineaExtractoBancarioId, movimientoBancarioId, fechaVinculacion }`. Un pendiente expone `lado`, `id`, `fechaOperacion`, `tipo`, `importe` y `referencia?`. Páginas con `items`, `pagina`, `tamanoPagina`, `totalRegistros`, `totalPaginas`.

### Validaciones y mensajes literales

Validar posiciones sin duplicados, escalas y fechas de las líneas dentro del intervalo. Dos rangos inclusivos de la misma cuenta se solapan cuando uno comienza antes o en el fin del otro y termina después o en su comienzo. Proteger la consulta de solapamiento y el alta bajo bloqueo de la cuenta, y guardar cabecera e ítems en una transacción.

La coincidencia requiere extracto abierto, línea de ese extracto, movimiento de la misma cuenta e importe y signo idénticos. Rechazar movimientos revertidos y líneas o movimientos ya vinculados. La edición de una línea abierta conserva la coherencia del vínculo existente. Compartir bloqueo por cuenta entre vincular y revertir, y por extracto entre editar, vincular y cerrar, con un orden fijo. El cierre comprueba todos los ítems y la diferencia en la misma transacción. Ninguna operación escribe importes en los movimientos de tesorería.

Los textos de negocio de la tabla son **propuestas concretas para la revisión final**. El UseCase responde con `ReglaNegocioException` y `CodigosEstado`. El cuerpo conserva `{ StatusCode, Message }`. Las carreras de unicidad y FK se traducen mediante `ViolacionRestriccionException` del `UnitOfWork`.

| HTTP | Condición | Literal propuesto |
|---|---|---|
| 400 | Datos, intervalo, posiciones o paginación inválidos | «Los datos del extracto bancario no son válidos.» |
| 404 | Cuenta inexistente | «La cuenta bancaria no existe.» |
| 404 | Extracto inexistente | «El extracto bancario no existe.» |
| 404 | Línea ajena o inexistente | «La línea indicada no existe en el extracto.» |
| 404 | Movimiento inexistente | «El movimiento bancario no existe.» |
| 409 | Periodo existente o superpuesto | «La cuenta ya tiene un extracto para un periodo que se superpone.» |
| 409 | Extracto cerrado | «El extracto bancario está cerrado.» |
| 409 | Cuenta, signo o importe incompatibles | «La línea y el movimiento deben tener la misma cuenta, signo e importe.» |
| 409 | Movimiento revertido | «No se puede conciliar un movimiento revertido.» |
| 409 | Línea o movimiento ya vinculado | «La línea o el movimiento ya tiene una coincidencia.» |
| 409 | Edición incompatible con el vínculo | «La edición no puede invalidar la coincidencia registrada.» |
| 409 | Cierre con pendientes o diferencia | «El extracto debe estar totalmente conciliado y con diferencia cero.» |
| 409 | Reversión en 55 | «No se puede revertir un movimiento conciliado.» |
| 409 | Referencia perdida al guardar | «No se pudo guardar la conciliación porque una referencia ya no existe.» |

### Precisiones para la revisión final

- **Diferencia y completitud.** Confirmar la fórmula propuesta con saldos del extracto y si «todo conciliado» exige además vincular cada movimiento interno elegible del periodo. El criterio de cierre documentado comprueba todos los ítems del extracto y expone pendientes de ambos lados.
- **Corrección antes del cierre.** Lukas autorizó editar ítems de extractos abiertos. Falta precisar si se puede retirar y reemplazar un vínculo y qué historial debe conservarse. El contrato evita invalidar coincidencias existentes.

### Seed, pruebas y smoke

**Seed.** A partir de una cuenta del 55 y sus movimientos, cargar un extracto abierto con un ingreso y un egreso, un movimiento revertido no elegible y un extracto cerrado de otro periodo. Identificar las filas por cuenta/periodo y posición para repetir la carga.

**Pruebas de integración.**

1. JSON válido, intervalo inválido, mismo periodo, superposición parcial y extremo compartido. Editar un ítem abierto y rechazar la edición tras cierre.
2. Asociación exacta de ingreso y egreso, rechazos por importe, signo, cuenta, reversión o vínculo duplicado. Intentar revertir en el 55 después de conciliar y verificar 409.
3. Cierre con un ítem pendiente, con diferencia distinta de cero y válido. Verificar que no hay reapertura y que el registro de tesorería del 55 conserva sus datos.
4. JWT, roles de escritura, listados y cada literal.

**Smoke PostgreSQL.** Cargar JSON de una cuenta del 55, editar, vincular, cerrar y verificar códigos y estado. Probar intervalos superpuestos concurrentes, dos asociaciones del mismo movimiento y asociación contra cierre o reversión en PostgreSQL. Añadir `request_check`, la validación Python de códigos y cuerpos, y la comprobación de OpenAPI. Ejecutar en una base desechable hasta `SMOKE OK`.

## 5. Criterios de aceptación (medibles)

| # | Criterio | Verificación | Umbral |
|---|---|---|---|
| A1 | Arquitectura y compilación | `dotnet build -warnaserror`, `scripts/check-rules.sh` y `./scripts/verify.sh` | Gate en verde |
| A2 | API y autorización | Pruebas de cada literal, rutas, JWT y roles G1 | Lecturas autenticadas. Escrituras FINANZAS/ADMIN. Cada rechazo conserva los datos. |
| A3 | Extractos | Carga, rango, superposición y edición | Una cabecera por intervalo permitido. Solo abiertos admiten edición |
| A4 | Coincidencia y reversión | Casos de signo, monto, duplicidad y carrera con el 55 | Un vínculo válido por ítem y por movimiento. Reversión indebida rechazada |
| A5 | Cierre | Casos de diferencia, pendientes y cierre definitivo | Cerrado solo con todos los ítems vinculados y diferencia cero |
| A6 | Database First | Aplicar SQL local, repetir seed, comprobar scaffold y ejecutar smoke | SCAFFOLD AL DÍA y SMOKE OK |

## 6. Checklist de ejecución

- [x] Incorporar las decisiones oficiales de §2 y G1 a G9.
- [ ] Terminar la revisión del PR de documentación y cerrar las precisiones de §4.
- [ ] Confirmar dependencias, aplicar el SQL en local y generar entidades y DbContext.
- [ ] Implementar API, pruebas, seed y smoke. Registrar los resultados de §5.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-23 | Decisiones v0 | Aprobadas. Revisión final del PR pendiente. | [Respuesta de Lukas](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52#issuecomment-7109010161) |
| 2026-11-23 | Validación funcional de la spec aprobada | Pendiente | Esta fase revisa documentación. La ejecución de §5 corresponde a la rama de implementación. |
