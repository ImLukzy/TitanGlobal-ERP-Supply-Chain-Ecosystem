# Especificación: modulo55-tesoreria-bancos-backend — Tesorería y bancos

> Estado: **aprobada** por Lukas el 2026-11-23 para la v0. Responsable: Leonel Portillo. La revisión final del [PR #52](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52) sigue pendiente.

## 1. Objetivo

Administrar cuentas bancarias y su saldo en la moneda de cada cuenta. Registrar ingresos, egresos y transferencias internas, con trazabilidad de los movimientos y sus reversiones.

### Base comprobada y dependencias

- **Base.** El backend del 55 es autónomo en `main` salvo el rol `FINANZAS` (Id 7), que incorpora el 52. Las pruebas emiten JWT de ese rol con la infraestructura existente.
- **Posterior.** El [56](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/blob/dbd4155fae4be59ab39ab9f3eb2a8bd17cd38534/docs/specs/modulo56-conciliacion-bancaria-backend.md) usa las cuentas y movimientos del 55. Su PR registra la consulta a coincidencias. Mientras tanto, la guarda del 55 consulta un proveedor sin coincidencias.

## 2. Fuera de alcance

- Cheques, sucursales, conversiones, comisiones, aprobaciones, sincronización bancaria y contabilidad.
- Registros automáticos desde cobros, pagos o reposiciones y vínculos estructurados a esos documentos.

**Decisiones de producto aprobadas para la v0.** [Respuesta de Lukas](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52#issuecomment-7109010161)

| Nº | Tema | Respuesta aprobada |
|---|---|---|
| 55.1 | Cuentas | Unicidad por banco y número. Tipos `Corriente` y `Ahorros`, moneda `PEN` o `USD`. Bloquear el cambio de moneda una vez que se registran movimientos. |
| 55.2 | Ingreso y egreso | Registrar `Ingreso` o `Egreso` con fecha hasta hoy. Rechazar con 409 cualquier operación que deje saldo negativo. Sin aprobación previa. |
| 55.3 | Transferencia | Mover el importe entre dos cuentas distintas de igual moneda, sin comisión. Registrar egreso e ingreso en una sola transacción. |
| 55.4 | Conciliación | La reversión de un movimiento conciliado devuelve HTTP 409. |
| 55.5 | Cheques | Los cheques quedan fuera de la v0. |
| 55.6 | Referencias | La referencia de origen es texto opcional. Prescindir de vínculos y escrituras automáticas en otros módulos. |

Las [reglas comunes G1 a G9](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/blob/dbd4155fae4be59ab39ab9f3eb2a8bd17cd38534/docs/equipo/revision-finanzas-52-60.md#reglas-generales-g1-a-g9) se aplican junto con las precisiones de §4.

## 3. Archivos afectados

| Archivo previsto | Acción futura | Alcance |
|---|---|---|
| `database/046-modulo55-tesoreria-bancos.sql` y `docs/equipo/relevo.md` | Crear / registrar | El 046 es el siguiente número libre en las reservas y scripts publicados al iniciar esta rama. [Relevo §8](../equipo/relevo.md#8-números-de-los-scripts-de-base-de-datos) |
| `src/TitanGlobal.Domain/Models/` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | Generar | Entidades y navegaciones de §4 mediante scaffold. Revisar los nombres que produce el pluralizador. |
| `src/TitanGlobal.Application/DTOs/Modulo55_TesoreriaBancos/` y `src/TitanGlobal.Application/UseCases/Modulo55_TesoreriaBancos/` | Crear | Validación, proyecciones y casos de uso concretos en Application. |
| `src/TitanGlobal.Api/Controllers/Modulo55_TesoreriaBancos/TesoreriaBancosController.cs` | Crear | Rutas y autorización de §4. |
| `tests/TitanGlobal.Api.Tests/Modulo55_TesoreriaBancos/TesoreriaBancosApiTests.cs` | Crear | Pruebas HTTP de §4. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | Ampliar | Fixtures idempotentes y checks HTTP/SQL del módulo. |

## 4. Diseño y lógica

Los nombres, tamaños auxiliares, rutas y mecanismos de persistencia concretan el diseño para la revisión final. Las decisiones de producto aprobadas están en §2 y G1 a G9.

### Convenciones y permisos

Controller → UseCase → IUnitOfWork → Repository → DbContext → PostgreSQL. DTOs en camelCase. Las lecturas requieren JWT. Las escrituras requieren `FINANZAS` (Id 7, seed del 52) o `ADMIN` mediante `[Authorize(Roles = "FINANZAS,ADMIN")]`. Los 401, 403 y el 400 de cuerpo vacío conservan los literales del pipeline. Las acciones sin cuerpo usan `[PermitirCuerpoVacio]`. Las fechas de negocio usan `DateOnly` y `date` con JSON `yyyy-MM-dd`. Los instantes del servidor usan `timestamptz` y `FechasUtc`.

La cuenta define moneda y los movimientos la heredan. El número de cuenta es la identificación externa del banco. Los registros internos se consultan por su identidad y no reciben serie. Los importes son `numeric(18,2)`.

### Datos y persistencia

| Tabla → entidad | Campos y restricciones |
|---|---|
| `CuentasBancarias` → `CuentaBancaria` | `Id`, `Banco varchar(120)`, `Numero varchar(40)`, `Nombre varchar(120)`, `Tipo varchar(20)`, `Moneda char(3)` y `FechaRegistro timestamptz`. Índice único `(Banco, Numero)` con valores canónicos. Mantener los ceros iniciales del número. |
| `MovimientosBancarios` → `MovimientoBancario` | `Id`, `CuentaBancariaId` FK, `Tipo varchar(20)` (`Ingreso` o `Egreso`), `Importe` positivo, `FechaOperacion date`, `Referencia varchar(100)?`, `TransferenciaBancariaId` FK opcional, `FechaRegistro timestamptz`, `FechaReversion timestamptz?` y `MotivoReversion varchar(300)?`. Índice `(CuentaBancariaId, FechaOperacion)`. Una pata de cada tipo por transferencia. |
| `TransferenciasBancarias` → `TransferenciaBancaria` | `Id`, `CuentaOrigenId` y `CuentaDestinoId` FKs distintas, `Importe`, `FechaOperacion date`, `Referencia varchar(100)?`, `FechaRegistro timestamptz`. La cuenta de origen determina la moneda y el par de movimientos documenta el efecto. |

Las PK son identidades `integer`. Las FKs se indexan y conservan referencias con `ON DELETE RESTRICT`. Application valida los límites de columna antes de guardar.

Saldo = suma de ingresos vigentes − suma de egresos vigentes. Una cuenta empieza con saldo cero y el saldo de apertura se representa mediante un ingreso manual. Una transferencia de 30 desde una cuenta con saldo 100 hacia otra con saldo 20 deja 70 y 50. La reversión lógica preserva el movimiento y lo excluye del saldo.

### Casos de uso, rutas y DTOs

Ruta base `/api/v1/tesoreria-bancos`. Las rutas de consulta proyectan con `AsNoTracking`. Filtrar cuentas por `moneda?` y `tipo?`, ordenadas por banco, número e `Id`. Filtrar movimientos por `tipo?`, `desde?` y `hasta?`, por fecha e `Id` descendentes. Paginación 1/20, máximo 100.

| Método y ruta relativa | Caso de uso | Entrada → éxito |
|---|---|---|
| `GET /cuentas` | `ListarCuentasBancariasUseCase` | Filtros → `200 PaginaCuentasBancariasDto`. |
| `POST /cuentas` | `CrearCuentaBancariaUseCase` | `GuardarCuentaBancariaRequestDto` → `201` detalle y `Location`. |
| `GET /cuentas/{id:int}` | `ObtenerCuentaBancariaUseCase` | `200 CuentaBancariaDetalleDto`. |
| `PUT /cuentas/{id:int}` | `ActualizarCuentaBancariaUseCase` | `GuardarCuentaBancariaRequestDto` → `200` detalle. |
| `GET /cuentas/{id:int}/movimientos` | `ListarMovimientosBancariosUseCase` | Filtros → `200 PaginaMovimientosBancariosDto`. |
| `POST /cuentas/{id:int}/movimientos` | `RegistrarMovimientoBancarioUseCase` | `RegistrarMovimientoBancarioRequestDto` → `201 MovimientoBancarioDto`. |
| `POST /cuentas/{id:int}/movimientos/{movimientoId:int}/revertir` | `RevertirMovimientoBancarioUseCase` | `RevertirMovimientoBancarioRequestDto { motivo }` → `200 MovimientoBancarioDto`. |
| `POST /transferencias` | `RegistrarTransferenciaBancariaUseCase` | `RegistrarTransferenciaBancariaRequestDto` → `201` detalle y `Location`. |
| `GET /transferencias/{id:int}` | `ObtenerTransferenciaBancariaUseCase` | `200 TransferenciaBancariaDto`. |

- `GuardarCuentaBancariaRequestDto { banco, numero, nombre, tipo, moneda }`. `CuentaBancariaDetalleDto { id, banco, numero, nombre, tipo, moneda, saldo, fechaRegistro }`, con listado de los mismos datos sin la fecha de registro.
- `RegistrarMovimientoBancarioRequestDto { tipo, importe, fechaOperacion, referencia? }`. `MovimientoBancarioDto { id, cuentaBancariaId, moneda, tipo, importe, fechaOperacion, referencia?, transferenciaBancariaId?, fechaReversion?, motivoReversion? }`.
- `RegistrarTransferenciaBancariaRequestDto { cuentaOrigenId, cuentaDestinoId, importe, fechaOperacion, referencia? }`. `TransferenciaBancariaDto { id, cuentaOrigenId, cuentaDestinoId, moneda, importe, fechaOperacion, movimientoEgresoId, movimientoIngresoId }`. Páginas con `items`, `pagina`, `tamanoPagina`, `totalRegistros`, `totalPaginas`.

### Validaciones y mensajes literales

Registrar un solo tipo por movimiento, con importe positivo representable con hasta dos decimales, fecha hasta hoy en UTC y referencia opcional. Recortar banco y número y normalizarlos a mayúsculas antes de comprobar unicidad. Se conservan los ceros iniciales y los caracteres internos del número. Con movimientos históricos de cualquier estado, rechazar un cambio de moneda. La v0 opera con cuentas existentes y su saldo se consulta en el listado y en el detalle. El contrato carece de una transición de estado para la cuenta.

Ingresos, egresos, ediciones de cuenta y reversiones bloquean las filas afectadas con `FOR UPDATE` antes de leer los datos o el saldo. Las transferencias toman ambos bloqueos en orden ascendente de `Id` y confirman la cabecera y sus dos movimientos en la misma transacción. Comprobar el saldo resultante también al revertir un ingreso. Al implementar el 56, su escritura de coincidencia usará `IUnitOfWorkTesoreria.ConCuentasBloqueadasAsync`. Su consulta a coincidencias se registrará con `IConsultaConciliacionBancaria` y el mismo `DbContext` de la transacción. La reversión consulta ese puerto después de bloquear la cuenta.

Los textos de negocio de la tabla son **propuestas concretas para la revisión final**. El UseCase responde con `ReglaNegocioException` y `CodigosEstado`. El cuerpo conserva `{ StatusCode, Message }`. Las carreras de unicidad y FK se traducen mediante `ViolacionRestriccionException` del `UnitOfWork`.

| HTTP | Condición | Literal propuesto |
|---|---|---|
| 400 | Datos, importe, fecha, filtros o paginación inválidos | «Los datos de tesorería no son válidos.» |
| 400 | Origen igual a destino | «Las cuentas de origen y destino deben ser distintas.» |
| 400 | Motivo fuera de 3–300 | «El motivo debe tener entre 3 y 300 caracteres.» |
| 404 | Cuenta inexistente | «La cuenta bancaria no existe.» |
| 404 | Movimiento ajeno o inexistente | «El movimiento bancario no existe en la cuenta.» |
| 404 | Transferencia inexistente | «La transferencia bancaria no existe.» |
| 409 | Banco/número duplicado | «La cuenta ya está registrada para ese banco y número.» |
| 409 | Cambio de moneda con movimientos | «No se puede cambiar la moneda de una cuenta con movimientos.» |
| 409 | Saldo insuficiente | «La cuenta bancaria no tiene saldo suficiente.» |
| 409 | Transferencia entre monedas distintas | «Las cuentas de la transferencia deben tener la misma moneda.» |
| 409 | Movimiento conciliado, al integrar 56 | «No se puede revertir un movimiento conciliado.» |
| 409 | Movimiento ya revertido | «El movimiento bancario ya fue revertido.» |
| 409 | Reversión individual de una pata de transferencia | «No se puede revertir por separado un movimiento de transferencia.» |
| 409 | Referencia perdida al guardar | «No se pudo guardar la operación bancaria porque una referencia ya no existe.» |

### Precisiones para la revisión final

- **Transferencias y reversión.** El par se registra atómicamente y sus movimientos quedan excluidos de la reversión individual. Queda pendiente confirmar si se admite el reverso completo, su ruta, las condiciones y el literal propuesto para bloquear el reverso individual. La guarda de movimiento conciliado también se comprueba para movimientos de transferencia.
- **Saldo de apertura.** La propuesta técnica lo registra como ingreso manual en lugar de mantener un saldo editable separado.

### Seed, pruebas y smoke

**Seed.** Usar claves banco/número estables. Cargar dos cuentas PEN y una USD con sus movimientos sintéticos. Incluir una transferencia completa. Resolver FKs por claves de fixture y repetir el bloque sin efectos duplicados.

**Pruebas de integración.**

1. Cuenta por banco y número, ambos tipos, ambas monedas y 409 por duplicado o cambio de moneda con movimientos.
2. Ingreso y egreso, fecha futura rechazada, saldo exacto y sobreconsumo. Reversión íntegra con motivo, reintento y reversión de ingreso que dejaría saldo negativo.
3. Transferencia a moneda igual, destino igual o moneda distinta rechazados, fallo intermedio con rollback y conciliado rechazado al incorporar el 56.
4. JWT, roles, literales y referencias opcionales. Los movimientos solo aparecen en el 55.

**Smoke PostgreSQL.** Verificar unicidad, ingresos, egresos, saldo, transferencia y reversión con `request_check`, validación Python y OpenAPI. Comprobar en `smoke-tesoreria-bancos.py` las restricciones SQL, dos egresos por el mismo saldo, transferencias con origen común y de sentido contrario, doble reversión, cambio de moneda frente al primer movimiento y rollback por una FK en la segunda pata. El 56 añade el caso con una coincidencia persistida.

## 5. Criterios de aceptación (medibles)

| # | Criterio | Verificación | Umbral |
|---|---|---|---|
| A1 | Arquitectura y compilación | `dotnet build -warnaserror`, `scripts/check-rules.sh` y `./scripts/verify.sh` | Gate en verde |
| A2 | API y autorización | Pruebas de cada literal, rutas, JWT y roles G1 | Lecturas autenticadas. Escrituras FINANZAS/ADMIN. Cada rechazo conserva los datos. |
| A3 | Cuenta y moneda | Unicidad, vigencia de moneda y campos válidos | Una cuenta por banco/número |
| A4 | Saldo y transferencia | Carrera por saldo, transferencia de dos patas y rollback | Saldo nunca negativo. Ninguna transferencia parcial |
| A5 | Conciliación futura | Prueba compartida con el 56 | HTTP 409 con conciliación vigente |
| A6 | Database First | Aplicar SQL local, repetir seed, comprobar scaffold y ejecutar smoke | SCAFFOLD AL DÍA y SMOKE OK |

## 6. Checklist de ejecución

- [x] Incorporar las decisiones oficiales de §2 y G1 a G9.
- [ ] Terminar la revisión del PR de documentación y cerrar las precisiones de §4.
- [x] Verificar las dependencias disponibles, aplicar SQL 046 en PostgreSQL desechable y generar entidades y DbContext.
- [x] Implementar API, pruebas, seed y smoke. Registrar los resultados de §5.
- [ ] Conectar la consulta a coincidencias persistidas y ejecutar la validación compartida al integrar el 56.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-23 | Decisiones v0 | Aprobadas. Revisión final del PR pendiente. | [Respuesta de Lukas](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52#issuecomment-7109010161) |
| 2026-11-24 | A1–A4 | Compilación sin advertencias ni errores. Reglas y gate correctos. | `dotnet build -warnaserror`, `scripts/check-rules.sh` y `scripts/verify.sh`. Pasaron 48 pruebas del 55 dentro de 1.239 pruebas de backend y 752 del frontend. |
| 2026-11-24 | A5 | HTTP 409 con proveedor de coincidencias de prueba. | El caso con una coincidencia en PostgreSQL se ejecutará con el 56. |
| 2026-11-24 | A6 | `SCAFFOLD AL DÍA` y `SMOKE OK`. | PostgreSQL 16 desechable. Dos cargas dejaron idénticas 3 cuentas, 7 movimientos y 1 transferencia, con saldos 55 PEN, 50 PEN y 75 USD. Pasaron las cinco carreras y el rollback por fallo en la segunda pata. |
