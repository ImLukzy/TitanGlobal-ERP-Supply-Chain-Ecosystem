# Especificación: modulo57-caja-chica-backend — Caja chica

> Estado: **aprobada** por Lukas el 2026-11-23 para la v0. Responsable: Leonel Portillo. La revisión final del [PR #52](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52) sigue pendiente.

## 1. Objetivo

Administrar fondos de caja chica con custodio, registrar gastos y reposiciones manuales y rendir gastos sin duplicarlos. Cerrar el fondo una vez que su saldo sea cero.

### Base comprobada y dependencias

- **Disponible en `main`.** El maestro `Empleados` del 61 aporta la FK obligatoria del custodio. [Empleados](modulo61-empleados-backend.md)
- **Transversal.** G1 aporta el rol desde el seed del 52. Los fondos y sus movimientos son propios del 57.

## 2. Fuera de alcance

- Centro de costo, presupuesto, enlaces bancarios, anticipos, cheques, sucursales, conversiones, contabilidad y reposición automática.
- Reapertura de fondos cerrados y borrado físico.

**Decisiones de producto aprobadas para la v0.** [Respuesta de Lukas](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52#issuecomment-7109010161)

| Nº | Tema | Respuesta aprobada |
|---|---|---|
| 57.1 | Fondo | Nombre único, custodio identificado por `EmpleadoId` del 61 y monto autorizado mayor que cero. Cerrar únicamente con saldo cero. |
| 57.2 | Movimientos | Tipos `Gasto` y `Reposicion`, ambos con comprobante de texto obligatorio. El gasto se limita al saldo disponible. Toda reversión exige motivo. |
| 57.3 | Rendición | Agrupar gastos que todavía no se hayan rendido. Avanzar de `Abierta` a `Rendida`. Escribir las reposiciones por solicitud manual. |
| 57.4 | Acceso | Aplicar G1. Prescindir de centros de costo y de presupuestos en la v0. |

Las [reglas comunes G1 a G9](../equipo/revision-finanzas-52-60.md#reglas-generales-g1-a-g9) se aplican junto con las precisiones de §4.

## 3. Archivos afectados

| Archivo previsto | Acción futura | Alcance |
|---|---|---|
| `database/NNN-modulo57-caja-chica.sql` y `docs/equipo/relevo.md` | Crear / registrar | Tomar el siguiente número libre al implementar y registrar solo la fila de este módulo. [Relevo §8](../equipo/relevo.md#8-números-de-los-scripts-de-base-de-datos) |
| `src/TitanGlobal.Domain/Models/` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | Generar | Entidades y navegaciones de §4 mediante scaffold. Revisar los nombres que produce el pluralizador. |
| `src/TitanGlobal.Application/DTOs/Modulo57_CajaChica/` y `src/TitanGlobal.Application/UseCases/Modulo57_CajaChica/` | Crear | Validación, proyecciones y casos de uso concretos en Application. |
| `src/TitanGlobal.Api/Controllers/Modulo57_CajaChica/CajaChicaController.cs` | Crear | Rutas y autorización de §4. |
| `tests/TitanGlobal.Api.Tests/Modulo57_CajaChica/CajaChicaApiTests.cs` | Crear | Pruebas HTTP de §4. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | Ampliar | Fixtures idempotentes y checks HTTP/SQL del módulo. |

## 4. Diseño y lógica

Los nombres, tamaños auxiliares, rutas y mecanismos de persistencia concretan el diseño para la revisión final. Las decisiones de producto aprobadas están en §2 y G1 a G9.

### Convenciones y permisos

Controller → UseCase → IUnitOfWork → Repository → DbContext → PostgreSQL. DTOs en camelCase. Las lecturas requieren JWT. Las escrituras requieren `FINANZAS` (Id 7, seed del 52) o `ADMIN` mediante `[Authorize(Roles = "FINANZAS,ADMIN")]`. Los 401, 403 y el 400 de cuerpo vacío conservan los literales del pipeline. Las acciones sin cuerpo usan `[PermitirCuerpoVacio]`. Las fechas de negocio usan `DateOnly` y `date` con JSON `yyyy-MM-dd`. Los instantes del servidor usan `timestamptz` y `FechasUtc`.

Importes `numeric(18,2)`. Los movimientos heredan la moneda del fondo. El contrato propuesto permite elegir PEN o USD al abrirlo. El código interno de fondo sigue el patrón G5, con `FCH-000001` como prefijo técnico propuesto.

### Datos y persistencia

| Tabla → entidad | Campos y restricciones |
|---|---|
| `FondosCajaChica` → `FondoCajaChica` | `Id`, `Codigo varchar(30)` único del servidor, `Nombre varchar(120)` único tras recorte, `EmpleadoId` FK obligatoria, `Moneda char(3)`, `MontoAutorizado` positivo, `Estado varchar(20)` (`Abierto`, `Cerrado`), `FechaApertura timestamptz`, `FechaCierre timestamptz?`. |
| `MovimientosCajaChica` → `MovimientoCajaChica` | `Id`, `FondoCajaChicaId` FK, `Tipo varchar(20)` (`Gasto`, `Reposicion`), `Importe` positivo, `FechaOperacion date`, `Comprobante varchar(120)` obligatorio, `FechaRegistro`, `FechaReversion?` (instantes), `MotivoReversion varchar(300)?`. |
| `RendicionesCajaChica` y `DetallesRendicionCajaChica` | Cabecera con `FondoCajaChicaId` FK, `Estado varchar(20)` (`Abierta`, `Rendida`), `FechaApertura` y `FechaRendicion?` como instantes. Detalle con FKs a rendición y movimiento de gasto. Unicidad por `MovimientoCajaChicaId`. |

Las PK son identidades `integer`. Las FKs se indexan y conservan referencias con `ON DELETE RESTRICT`. Application valida los límites de columna antes de guardar.

Como contrato técnico de revisión, el saldo parte de cero y las reposiciones manuales lo financian. Saldo = reposiciones vigentes − gastos vigentes. La rendición agrupa los gastos y su total, y conserva el efecto que tuvieron al registrarse. Una reposición de 100 y un gasto de 30 dejan 70, también después de rendir dicho gasto.

### Casos de uso, rutas y DTOs

Ruta base `/api/v1/caja-chica`. Las rutas de consulta proyectan con `AsNoTracking`. Listar fondos por `estado?` y `empleadoId?`, y movimientos por `tipo?` y fecha. Orden por fecha e `Id` descendentes. Paginación 1/20, máximo 100.

| Método y ruta relativa | Caso de uso | Entrada → éxito |
|---|---|---|
| `GET /fondos` | `ListarFondosCajaChicaUseCase` | Filtros → `200 PaginaFondosCajaChicaDto`. |
| `GET /fondos/{id:int}` | `ObtenerFondoCajaChicaUseCase` | `200 FondoCajaChicaDetalleDto`. |
| `POST /fondos` | `CrearFondoCajaChicaUseCase` | `CrearFondoCajaChicaRequestDto` → `201` detalle y `Location`. |
| `GET /fondos/{id:int}/movimientos` | `ListarMovimientosCajaChicaUseCase` | Filtros → `200 PaginaMovimientosCajaChicaDto`. |
| `POST /fondos/{id:int}/movimientos` | `RegistrarMovimientoCajaChicaUseCase` | `RegistrarMovimientoCajaChicaRequestDto` → `201 MovimientoCajaChicaDto`. |
| `POST /fondos/{id:int}/movimientos/{movimientoId:int}/revertir` | `RevertirMovimientoCajaChicaUseCase` | `RevertirMovimientoCajaChicaRequestDto { motivo }` → `200 MovimientoCajaChicaDto`. |
| `POST /fondos/{id:int}/rendiciones` | `AbrirRendicionCajaChicaUseCase` | `AbrirRendicionCajaChicaRequestDto { gastoIds }` → `201 RendicionCajaChicaDto`. |
| `GET /rendiciones/{id:int}` | `ObtenerRendicionCajaChicaUseCase` | `200 RendicionCajaChicaDto`. |
| `POST /rendiciones/{id:int}/rendir` | `RendirCajaChicaUseCase` | Sin cuerpo → `200 RendicionCajaChicaDto`. |
| `POST /fondos/{id:int}/cerrar` | `CerrarFondoCajaChicaUseCase` | Sin cuerpo → `200 FondoCajaChicaDetalleDto`. |

- `CrearFondoCajaChicaRequestDto { nombre, empleadoId, moneda, montoAutorizado }`. `FondoCajaChicaDetalleDto { id, codigo, nombre, empleadoId, moneda, montoAutorizado, saldo, estado, fechaApertura, fechaCierre? }`.
- `RegistrarMovimientoCajaChicaRequestDto { tipo, importe, fechaOperacion, comprobante }`. `MovimientoCajaChicaDto` incluye los datos de entrada, `id`, `fondoCajaChicaId`, `moneda`, `fechaReversion?` y `motivoReversion?`.
- `RendicionCajaChicaDto { id, fondoCajaChicaId, estado, fechaApertura, fechaRendicion?, gastos, total }`. Cada gasto contiene su id, importe y comprobante. Listados paginados con `items`, `pagina`, `tamanoPagina`, `totalRegistros` y `totalPaginas`.

### Validaciones y mensajes literales

Validar el custodio existente, nombre, unicidad, monto autorizado y fecha efectiva hasta hoy. Exigir importe positivo y comprobante recortado de 1–120 caracteres para todo movimiento. El saldo y el estado del fondo se leen bajo bloqueo antes de registrar o revertir. Rechazar gasto superior al saldo. El diseño propone el mismo control al revertir una reposición para mantener un saldo no negativo. Registrar reversión una sola vez con motivo y conservar el movimiento original.

Proteger bajo el mismo bloqueo el cierre con saldo cero y la selección de gastos vigentes del mismo fondo para rendición. Cada gasto figura una vez en el conjunto de rendiciones. El marcado de `Rendida` conserva los registros y los saldos. Los casos de reversiones de gastos rendidos se precisan en la revisión.

Los textos de negocio de la tabla son **propuestas concretas para la revisión final**. El UseCase responde con `ReglaNegocioException` y `CodigosEstado`. El cuerpo conserva `{ StatusCode, Message }`. Las carreras de unicidad y FK se traducen mediante `ViolacionRestriccionException` del `UnitOfWork`.

| HTTP | Condición | Literal propuesto |
|---|---|---|
| 400 | Campos, fecha, importe, comprobante o paginación inválidos | «Los datos de caja chica no son válidos.» |
| 400 | Motivo fuera de 3–300 | «El motivo debe tener entre 3 y 300 caracteres.» |
| 404 | Custodio inexistente | «El empleado custodio no existe.» |
| 404 | Fondo inexistente | «El fondo de caja chica no existe.» |
| 404 | Movimiento inexistente o ajeno | «El movimiento no existe en el fondo.» |
| 404 | Rendición inexistente | «La rendición de caja chica no existe.» |
| 409 | Nombre duplicado | «Ya existe un fondo con ese nombre.» |
| 409 | Saldo insuficiente | «El fondo no tiene saldo suficiente.» |
| 409 | Fondo cerrado | «El fondo de caja chica está cerrado.» |
| 409 | Cierre con saldo distinto de cero | «El fondo solo se puede cerrar con saldo cero.» |
| 409 | Gasto ya elegido, revertido o ajeno | «Solo se pueden rendir gastos vigentes del fondo que todavía no estén incluidos en una rendición.» |
| 409 | Rendición ya rendida | «La rendición ya está cerrada.» |
| 409 | Movimiento ya revertido | «El movimiento de caja chica ya fue revertido.» |
| 409 | Referencia perdida al guardar | «No se pudo guardar la operación de caja chica porque una referencia ya no existe.» |

### Precisiones para la revisión final

- **Apertura y tope.** La respuesta exige monto autorizado positivo y limita el gasto al saldo. No define si ese monto financia por sí solo el fondo o limita las reposiciones. El contrato técnico usa una primera reposición manual y no agrega un límite adicional.
- **Rendición y reversión.** Confirmar cómo registrar una reversión de gasto ya rendido y su efecto histórico en la rendición. Confirmar si la selección de gastos debe ser total o parcial, y si hay que restringir la moneda del fondo.

### Seed, pruebas y smoke

**Seed.** Elegir un custodio sintético del 61. Cargar fondos abierto y cerrado, una reposición, gastos con comprobante y una rendición de prueba. Usar nombre y códigos internos estables para una carga idempotente.

**Pruebas de integración.**

1. Alta, custodio inexistente, nombre duplicado y monto autorizado no positivo. Verificar código del servidor y moneda.
2. Reposición manual, comprobante de ambos movimientos, gasto igual o superior al saldo, reversión completa con motivo y cierre con saldo cero.
3. Rendición de gastos propios sin repetición, cambio a `Rendida`, conservación de saldo, rechazo de movimientos ajenos y escrituras posteriores al cierre.
4. JWT, roles, filtros y cada literal. Confirmar ausencia de datos generados en tesorería o presupuestos.

**Smoke PostgreSQL.** Comprobar alta con custodio, reposición, gasto, rendición, saldo cero y cierre. Probar dos gastos que compiten por el saldo y dos rendiciones del mismo gasto. Añadir `request_check`, la validación Python de códigos y cuerpos, y la comprobación de OpenAPI. Ejecutar en una base desechable hasta `SMOKE OK`.

## 5. Criterios de aceptación (medibles)

| # | Criterio | Verificación | Umbral |
|---|---|---|---|
| A1 | Arquitectura y compilación | `dotnet build -warnaserror`, `scripts/check-rules.sh` y `./scripts/verify.sh` | Gate en verde |
| A2 | API y autorización | Pruebas de cada literal, rutas, JWT y roles G1 | Lecturas autenticadas. Escrituras FINANZAS/ADMIN. Cada rechazo conserva los datos. |
| A3 | Fondo y saldo | Custodio, unicidad del nombre, montos, reversión y cierre | Cierre solo con saldo cero. Los gastos respetan el saldo |
| A4 | Rendición | Selección exclusiva, transición y prueba concurrente | Sin gasto duplicado. El saldo no cambia al rendir |
| A5 | Database First | Aplicar SQL local, repetir seed, comprobar scaffold y ejecutar smoke | SCAFFOLD AL DÍA y SMOKE OK |

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
