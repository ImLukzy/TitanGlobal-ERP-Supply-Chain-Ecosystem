# Especificación: modulo58-presupuestos-backend — Presupuestos

> Estado: **aprobada** por Lukas el 2026-11-23 para la v0. Responsable: Leonel Portillo. La revisión final del [PR #52](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52) sigue pendiente.

## 1. Objetivo

Gestionar presupuestos por año calendario en PEN, con partidas por centro de costo y categoría de texto. Registrar ejecución manual e informar disponibilidad y sobreejecución por año y centro.

### Base comprobada y dependencias

- **Previa.** El [59](modulo59-centros-costo-backend.md) proporciona el maestro de centros y la marca `Activo`. El 58 comprueba actividad al crear una partida y conserva su FK histórica.
- **Transversal.** Permisos de G1 con la semilla del 52. Los datos de ejecución se originan dentro del propio 58.

## 2. Fuera de alcance

- Traspasos, jerarquías de partida, versiones paralelas con la misma identidad, reapertura, sucursales y conversión monetaria.
- Ejecución automática desde pagos, tesorería, caja chica o alta de activos, y asientos contables.

**Decisiones de producto aprobadas para la v0.** [Respuesta de Lukas](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52#issuecomment-7109010161)

| Nº | Tema | Respuesta aprobada |
|---|---|---|
| 58.1 | Presupuesto | Un presupuesto por combinación de año calendario y nombre, en `PEN`. |
| 58.2 | Partidas | Asignar centro de costo del 59 y categoría de texto sin jerarquía. El centro debe estar activo al crear la partida. |
| 58.3 | Ciclo | `Borrador → Aprobado → Cerrado`. Cambiar montos solo en borrador. Prescindir de traspasos. |
| 58.4 | Ejecución | Registrar ejecución manual por partida. Permitir exceder lo asignado y marcar la partida como sobreejecutada. |
| 58.5 | Disponible | Agrupar por año y centro. Calcular disponible restando ejecutado de asignado. |

Las [reglas comunes G1 a G9](../equipo/revision-finanzas-52-60.md#reglas-generales-g1-a-g9) se aplican junto con las precisiones de §4.

## 3. Archivos afectados

| Archivo previsto | Acción futura | Alcance |
|---|---|---|
| `database/NNN-modulo58-presupuestos.sql` y `docs/equipo/relevo.md` | Crear / registrar | Tomar el siguiente número libre al implementar y registrar solo la fila de este módulo. [Relevo §8](../equipo/relevo.md#8-números-de-los-scripts-de-base-de-datos) |
| `src/TitanGlobal.Domain/Models/` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | Generar | Entidades y navegaciones de §4 mediante scaffold. Revisar los nombres que produce el pluralizador. |
| `src/TitanGlobal.Application/DTOs/Modulo58_Presupuestos/` y `src/TitanGlobal.Application/UseCases/Modulo58_Presupuestos/` | Crear | Validación, proyecciones y casos de uso concretos en Application. |
| `src/TitanGlobal.Api/Controllers/Modulo58_Presupuestos/PresupuestosController.cs` | Crear | Rutas y autorización de §4. |
| `tests/TitanGlobal.Api.Tests/Modulo58_Presupuestos/PresupuestosApiTests.cs` | Crear | Pruebas HTTP de §4. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | Ampliar | Fixtures idempotentes y checks HTTP/SQL del módulo. |

## 4. Diseño y lógica

Los nombres, tamaños auxiliares, rutas y mecanismos de persistencia concretan el diseño para la revisión final. Las decisiones de producto aprobadas están en §2 y G1 a G9.

### Convenciones y permisos

Controller → UseCase → IUnitOfWork → Repository → DbContext → PostgreSQL. DTOs en camelCase. Las lecturas requieren JWT. Las escrituras requieren `FINANZAS` (Id 7, seed del 52) o `ADMIN` mediante `[Authorize(Roles = "FINANZAS,ADMIN")]`. Los 401, 403 y el 400 de cuerpo vacío conservan los literales del pipeline. Las acciones sin cuerpo usan `[PermitirCuerpoVacio]`. Las fechas de negocio usan `DateOnly` y `date` con JSON `yyyy-MM-dd`. Los instantes del servidor usan `timestamptz` y `FechasUtc`.

Moneda fija `PEN`, excepción a G2. Importes `numeric(18,2)`. Año es un periodo de planificación, no una fecha efectiva. Código interno `PTO-000001` como prefijo técnico propuesto para el patrón G5.

### Datos y persistencia

| Tabla → entidad | Campos y restricciones |
|---|---|
| `Presupuestos` → `Presupuesto` | `Id`, `Codigo varchar(30)` único del servidor, `Anio integer`, `Nombre varchar(120)`, `Moneda char(3)` fija `PEN`, `Estado varchar(20)`, `FechaCreacion`, `FechaAprobacion?` y `FechaCierre?` como instantes. Único `(Anio, Nombre)`. |
| `PartidasPresupuesto` → `PartidaPresupuesto` | `Id`, `PresupuestoId` FK, `CentroCostoId` FK al 59, `Categoria varchar(120)`, `ImporteAsignado`. Índices por presupuesto/centro. La categoría es texto simple. |
| `EjecucionesPresupuestarias` → `EjecucionPresupuestaria` | `Id`, `PartidaPresupuestoId` FK, `FechaOperacion date`, `Importe` positivo, `Referencia varchar(100)?`, `FechaRegistro timestamptz`. La ejecución se escribe por solicitud y conserva su origen en este módulo. |

Las PK son identidades `integer`. Las FKs se indexan y conservan referencias con `ON DELETE RESTRICT`. Application valida los límites de columna antes de guardar.

Por partida, ejecutado = suma de ejecuciones. Disponible = asignado − ejecutado. `Sobreejecutada` se calcula cuando ejecutado supera asignado. Asignar 100 y ejecutar 120 deja disponible −20 y `sobreejecutada=true`. El reporte de año/centro agrega los importes antes de calcular el disponible.

### Casos de uso, rutas y DTOs

Ruta base `/api/v1/presupuestos`. Las rutas de consulta proyectan con `AsNoTracking`. Listados por `anio?` y `estado?`, orden por año e `Id` descendentes. Páginas 1/20 y máximo 100.

| Método y ruta relativa | Caso de uso | Entrada → éxito |
|---|---|---|
| `GET /` | `ListarPresupuestosUseCase` | Filtros → `200 PaginaPresupuestosDto`. |
| `GET /{id:int}` | `ObtenerPresupuestoUseCase` | `200 PresupuestoDetalleDto`. |
| `GET /resumen` | `ResumirPresupuestosUseCase` | `anio`, `centroCostoId?` → `200 ResumenPresupuestarioDto`. |
| `POST /` | `CrearPresupuestoUseCase` | `CrearPresupuestoRequestDto { anio, nombre }` → `201` detalle y `Location`. |
| `POST /{id:int}/partidas` | `CrearPartidaPresupuestoUseCase` | `CrearPartidaPresupuestoRequestDto` → `201 PartidaPresupuestoDto`. |
| `PUT /{id:int}/partidas/{partidaId:int}` | `ActualizarMontoPartidaUseCase` | `ActualizarMontoPartidaRequestDto { importeAsignado }` → `200 PartidaPresupuestoDto`. |
| `POST /{id:int}/aprobar` | `AprobarPresupuestoUseCase` | Sin cuerpo → `200` detalle. |
| `POST /{id:int}/cerrar` | `CerrarPresupuestoUseCase` | Sin cuerpo → `200` detalle. |
| `POST /{id:int}/partidas/{partidaId:int}/ejecuciones` | `RegistrarEjecucionPresupuestariaUseCase` | `RegistrarEjecucionPresupuestariaRequestDto` → `201 EjecucionPresupuestariaDto`. |

- `CrearPartidaPresupuestoRequestDto { centroCostoId, categoria, importeAsignado }`. `RegistrarEjecucionPresupuestariaRequestDto { fechaOperacion, importe, referencia? }`.
- `PresupuestoListadoDto { id, codigo, anio, nombre, moneda, estado, asignado, ejecutado, disponible }`. Detalle con fechas del ciclo y partidas. `PartidaPresupuestoDto { id, centroCostoId, categoria, importeAsignado, ejecutado, disponible, sobreejecutada }`.
- `EjecucionPresupuestariaDto { id, partidaPresupuestoId, fechaOperacion, importe, referencia? }`. `ResumenPresupuestarioDto { anio, moneda, grupos }` con `centroCostoId`, asignado, ejecutado y disponible. Páginas con `items`, `pagina`, `tamanoPagina`, `totalRegistros` y `totalPaginas`.

### Validaciones y mensajes literales

Exigir nombre, año calendario válido, categoría, montos en escala y centro existente y activo para la partida nueva. El alta y la edición de asignaciones requieren borrador. La ejecución registra un importe positivo en una fecha efectiva del ejercicio hasta hoy y calcula el indicador sin rechazar por exceso de asignación. La ventana de registro planteada en el contrato técnico es `Aprobado`.

Bloquear por presupuesto para transiciones, registros de ejecución y cambios de asignación. Al crear una partida, coordinar la lectura de centro activo con la desactivación del 59. Un centro que se desactiva conserva el presupuesto y sus ejecuciones históricas, y la actividad se comprueba solo al crear una referencia nueva.

Los textos de negocio de la tabla son **propuestas concretas para la revisión final**. El UseCase responde con `ReglaNegocioException` y `CodigosEstado`. El cuerpo conserva `{ StatusCode, Message }`. Las carreras de unicidad y FK se traducen mediante `ViolacionRestriccionException` del `UnitOfWork`.

| HTTP | Condición | Literal propuesto |
|---|---|---|
| 400 | Datos, año, fecha, monto, categoría o paginación inválidos | «Los datos del presupuesto no son válidos.» |
| 404 | Presupuesto inexistente | «El presupuesto no existe.» |
| 404 | Partida ajena o inexistente | «La partida no existe en el presupuesto.» |
| 404 | Centro inexistente | «El centro de costo no existe.» |
| 409 | Centro inactivo en nueva partida | «El centro de costo debe estar activo para crear la partida.» |
| 409 | Presupuesto duplicado por año/nombre | «Ya existe un presupuesto con ese nombre para el año.» |
| 409 | Intento de cambiar la asignación fuera de borrador | «Los montos solo se pueden cambiar en borrador.» |
| 409 | Transición incompatible | «El estado del presupuesto no permite esta operación.» |
| 409 | Referencia perdida al guardar | «No se pudo guardar la operación presupuestaria porque una referencia ya no existe.» |

### Precisiones para la revisión final

- **Ventana de ejecución.** Confirmar el registro solo en `Aprobado`, la fecha dentro del año de la partida y cómo corregir una ejecución ya registrada. La v0 aprobada no define un flujo de reversión para estos registros.

### Seed, pruebas y smoke

**Seed.** Crear presupuesto de prueba en PEN y partidas con centros activos del 59. Incluir asignación subejecutada y sobreejecutada, además de borrador y cerrado de claves distintas. Resolver referencias y cargar por claves estables sin duplicar ejecuciones.

**Pruebas de integración.**

1. Alta por año y nombre, duplicidad, moneda fija, alta de partida con centro activo y rechazo de centro inexistente o inactivo.
2. Aprobación y cierre, límite de edición de monto, ejecución manual, exceso permitido y cálculo negativo de disponible. Conservar partidas vinculadas a un centro que se desactiva.
3. Resumen por año/centro con partidas de varias categorías. Ejercitar JWT, roles, paginación, transiciones y literales.

**Smoke PostgreSQL.** Registrar partida de centro activo, aprobar, ejecutar sobre el monto, consultar y cerrar. Probar alta de partida frente a desactivación y cambio de asignación frente a aprobación. Añadir `request_check`, la validación Python de códigos y cuerpos, y la comprobación de OpenAPI. Ejecutar en una base desechable hasta `SMOKE OK`.

## 5. Criterios de aceptación (medibles)

| # | Criterio | Verificación | Umbral |
|---|---|---|---|
| A1 | Arquitectura y compilación | `dotnet build -warnaserror`, `scripts/check-rules.sh` y `./scripts/verify.sh` | Gate en verde |
| A2 | API y autorización | Pruebas de cada literal, rutas, JWT y roles G1 | Lecturas autenticadas. Escrituras FINANZAS/ADMIN. Cada rechazo conserva los datos. |
| A3 | Identidad y partidas | Año calendario, PEN, categoría textual y elegibilidad del centro | Un presupuesto por año/nombre. Partidas solo con centro activo al crearse |
| A4 | Ciclo y ejecución | Transiciones, inmutabilidad de montos aprobados y sobreejecución | Registro explícito con indicador correcto. Sin traspasos |
| A5 | Disponible | Reporte de asignado y ejecutado por año/centro | Diferencia exacta, incluidos disponibles negativos |
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
