# Especificación: modulo59-centros-costo-backend — Centros de costo

> Estado: **aprobada** por Lukas el 2026-11-23 para la v0. Responsable: Leonel Portillo. La revisión final del [PR #52](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52) sigue pendiente.

## 1. Objetivo

Mantener un catálogo de centros de costo con jerarquía de hasta tres niveles y vigencia administrable. Los consumidores mantienen una referencia histórica y validan la actividad del centro al crear una nueva asignación.

### Base comprobada y dependencias

- **Disponible en `main`.** `Empleados` del 61 para el responsable opcional. [Empleados](modulo61-empleados-backend.md)
- **Permiso pendiente de integración.** El módulo 52 incorpora `FINANZAS` (Id 7). Las pruebas del 59 emiten un JWT con ese rol mediante la infraestructura existente. En `main` el rol aún no está disponible para iniciar sesión.
- **Consumidores posteriores.** [Presupuestos (58)](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/blob/dbd4155fae4be59ab39ab9f3eb2a8bd17cd38534/docs/specs/modulo58-presupuestos-backend.md) y [Activos fijos (60)](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/blob/dbd4155fae4be59ab39ab9f3eb2a8bd17cd38534/docs/specs/modulo60-activos-fijos-backend.md) usan el catálogo y validan actividad al alta. El 59 conserva la identidad y no agrega sus importes.

## 2. Fuera de alcance

- Relaciones con centros de trabajo del 33, sucursales, contabilidad y reportes de importes.
- Borrado físico y cambios retroactivos en partidas o activos ya vinculados.

**Decisiones de producto aprobadas para la v0.** [Respuesta de Lukas](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52#issuecomment-7109010161)

| Nº | Tema | Respuesta aprobada |
|---|---|---|
| 59.1 | Identidad y responsable | Código único en mayúsculas de 2 a 20 caracteres. El responsable del 61 es opcional. Catálogo independiente del 33. |
| 59.2 | Árbol | Padre opcional y máximo tres niveles, sin ciclos. Un centro con hijos activos no puede desactivarse. |
| 59.3 | Vigencia | Comprobar actividad al crear referencias. Desactivar conserva lo existente. El catálogo admite reactivación. |
| 59.4 | Acceso y consultas | Aplicar G1. Las consultas de centros no reportan importes. |

Las [reglas comunes G1 a G9](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/blob/dbd4155fae4be59ab39ab9f3eb2a8bd17cd38534/docs/equipo/revision-finanzas-52-60.md#reglas-generales-g1-a-g9) se aplican junto con las precisiones de §4.

## 3. Archivos afectados

| Archivo previsto | Acción futura | Alcance |
|---|---|---|
| `database/045-modulo59-centros-costo.sql` y `docs/equipo/relevo.md` | Crear / registrar | El 045 es el siguiente número libre en las reservas y scripts publicados al iniciar la rama. [Relevo §8](../equipo/relevo.md#8-números-de-los-scripts-de-base-de-datos) |
| `src/TitanGlobal.Domain/Models/` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | Generar | Entidades y navegaciones de §4 mediante scaffold. Revisar los nombres que produce el pluralizador. |
| `src/TitanGlobal.Application/DTOs/Modulo59_CentrosCosto/` y `src/TitanGlobal.Application/UseCases/Modulo59_CentrosCosto/` | Crear | Validación, proyecciones y casos de uso concretos en Application. |
| `src/TitanGlobal.Api/Controllers/Modulo59_CentrosCosto/CentrosCostoController.cs` | Crear | Rutas y autorización de §4. |
| `tests/TitanGlobal.Api.Tests/Modulo59_CentrosCosto/CentrosCostoApiTests.cs` | Crear | Pruebas HTTP de §4. |
| `scripts/seed-dev.sql` y `scripts/smoke-postgres.sh` | Ampliar | Fixtures idempotentes y checks HTTP/SQL del módulo. |

## 4. Diseño y lógica

Los nombres, tamaños auxiliares, rutas y mecanismos de persistencia concretan el diseño para la revisión final. Las decisiones de producto aprobadas están en §2 y G1 a G9.

### Convenciones y permisos

Controller → UseCase → IUnitOfWork → Repository → DbContext → PostgreSQL. DTOs en camelCase. Las lecturas requieren JWT. Las escrituras requieren `FINANZAS` (Id 7, seed del 52) o `ADMIN` mediante `[Authorize(Roles = "FINANZAS,ADMIN")]`. Los 401, 403 y el 400 de cuerpo vacío conservan los literales del pipeline. Las acciones sin cuerpo usan `[PermitirCuerpoVacio]`. El catálogo solo registra instantes del servidor en `timestamptz`, normalizados con `FechasUtc`.

El código de catálogo es capturado y validado en mayúsculas. Es una identidad maestra y no un folio transaccional de G5. G2 y G4 no tienen objeto en este catálogo. La reactivación expresa la excepción de G7 para la vigencia del maestro.

### Datos y persistencia

| Tabla → entidad | Campos y restricciones |
|---|---|
| `CentrosCosto` → `CentroCosto` | `Id`, `Codigo varchar(20)` único y de longitud mínima 2, `Nombre varchar(120)`, `Descripcion varchar(300)?`, `ResponsableEmpleadoId` FK opcional a `Empleados`, `CentroCostoPadreId` FK opcional a la misma tabla, `Activo boolean`, `FechaRegistro timestamptz` y `FechaDesactivacion timestamptz?`. Índice por padre y actividad. Checks de código recortado, nombre no vacío, padre distinto del propio id y fecha de desactivación presente solo en centros inactivos. |

Las PK son identidades `integer`. Las FKs se indexan y conservan referencias con `ON DELETE RESTRICT`. Application valida los límites de columna antes de guardar.

El nivel de la raíz es 1. Sus hijos están en nivel 2 y sus nietos en nivel 3. El estado inactivo se guarda en la fila y sus consumidores conservan la FK original.

### Casos de uso, rutas y DTOs

Ruta base `/api/v1/centros-costo`. Las rutas de consulta proyectan con `AsNoTracking`. Listar por `activo?` y `centroCostoPadreId?`. Orden por `Codigo` e `Id`. Páginas 1/20 y máximo 100.

Para recorrer el árbol, el filtro `centroCostoPadreId` devuelve los hijos directos. El detalle devuelve el nivel y el id del padre. El listado sin filtros incluye raíces y centros activos e inactivos.

| Método y ruta relativa | Caso de uso | Entrada → éxito |
|---|---|---|
| `GET /` | `ListarCentrosCostoUseCase` | Filtros → `200 PaginaCentrosCostoDto`. |
| `GET /{id:int}` | `ObtenerCentroCostoUseCase` | `200 CentroCostoDetalleDto`. |
| `POST /` | `CrearCentroCostoUseCase` | `GuardarCentroCostoRequestDto` → `201` detalle y `Location`. |
| `PUT /{id:int}` | `ActualizarCentroCostoUseCase` | `GuardarCentroCostoRequestDto` → `200` detalle. |
| `POST /{id:int}/desactivar` | `DesactivarCentroCostoUseCase` | Sin cuerpo → `200` detalle. |
| `POST /{id:int}/reactivar` | `ReactivarCentroCostoUseCase` | Sin cuerpo → `200` detalle. |

- `GuardarCentroCostoRequestDto { codigo, nombre, descripcion?, responsableEmpleadoId?, centroCostoPadreId? }`. El servidor valida el código normalizado, la relación jerárquica y las referencias. La edición de código y padre está sujeta a las precisiones de esta sección.
- `CentroCostoListadoDto { id, codigo, nombre, centroCostoPadreId?, responsableEmpleadoId?, activo }`. El detalle añade descripción, nivel y fechas de vigencia. `PaginaCentrosCostoDto { items, pagina, tamanoPagina, totalRegistros, totalPaginas }`.

### Validaciones y mensajes literales

Recortar y pasar el código a mayúsculas antes de medir su longitud y consultar unicidad. Verificar existencia de padre o responsable cuando se informan. Revisar ancestros, identidad propia y altura del subárbol antes de guardar un vínculo. Ninguna operación puede dejar un ciclo o nivel 4.

Cada escritura adquiere un bloqueo `SHARE ROW EXCLUSIVE` sobre `CentrosCosto` antes de leer sus filas. La transacción mantiene el bloqueo hasta confirmar o revertir. La desactivación verifica hijos activos y la reactivación recupera la vigencia del mismo id. Al implementar 58 y 60, sus altas deben serializar la comprobación de actividad y el guardado frente a una desactivación concurrente. Una opción es leer el centro con `SELECT … FOR SHARE` en la misma transacción que guarda la referencia. Las referencias anteriores conservan el id original.

Los textos de negocio de la tabla son **propuestas concretas para la revisión final**. El UseCase responde con `ReglaNegocioException` y `CodigosEstado`. El cuerpo conserva `{ StatusCode, Message }`. Las carreras de unicidad y FK se traducen mediante `ViolacionRestriccionException` del `UnitOfWork`.

| HTTP | Condición | Literal propuesto |
|---|---|---|
| 400 | Código, datos o filtros inválidos | «El código debe tener entre 2 y 20 caracteres y los datos del centro deben ser válidos.» |
| 404 | Centro inexistente | «El centro de costo no existe.» |
| 404 | Padre inexistente | «El centro de costo padre no existe.» |
| 404 | Empleado inexistente | «El empleado responsable no existe.» |
| 409 | Código normalizado duplicado | «Ya existe un centro de costo con ese código.» |
| 409 | Ciclo o nivel superior a 3 | «La jerarquía de centros debe evitar ciclos y tener como máximo tres niveles.» |
| 409 | Alta, cambio de padre o reactivación bajo un padre inactivo | «El centro de costo padre debe estar activo.» |
| 409 | Desactivar con hijos activos | «No se puede desactivar un centro de costo con hijos activos.» |
| 409 | Vigencia solicitada ya vigente | «El centro de costo ya tiene el estado solicitado.» |
| 409 | Referencia perdida al guardar | «No se pudo guardar el centro de costo porque una referencia ya no existe.» |

### Precisiones para la revisión final

- **Cambios del maestro.** La implementación admite que el `PUT` cambie código o padre y conserva el id y la fecha de registro. Queda pendiente confirmar estos dos campos editables.
- **Padre inactivo.** La implementación exige un padre activo al crear, reasignar o reactivar. Un centro inactivo que conserva su padre puede editar sus otros datos. Queda pendiente confirmar esta precisión y su literal.

### Seed, pruebas y smoke

**Seed.** Crear por códigos estables una raíz con responsable del 61, un hijo y un nieto activos, y una hoja inactiva. Resolver el padre y responsable por claves existentes y conservar identificadores al repetir. La carga vuelve a usar los empleados de ejemplo para evitar el truncado en cascada de los centros.

**Pruebas de integración.**

1. Código de 2 y 20 caracteres, límites, normalización y duplicidad. Padre y responsable opcionales o inexistentes. Confirmar lectura sin importes.
2. Árbol de tres niveles, cuarto nivel rechazado, ciclos, padre propio y altura del subárbol en cambios de vínculo.
3. Desactivación con hijos activos rechazada, hoja desactivada y reactivada con la misma identidad. Verificar las referencias históricas al integrar cada consumidor (58 y 60).
4. JWT, ambos roles de escritura, 403 de consulta al escribir y cada literal.

**Smoke PostgreSQL.** Comprobar tres niveles, duplicidad, rechazo del cuarto nivel, desactivación de un padre con hijos activos y reactivación. Validar con `request_check`, el diccionario Python y OpenAPI. Comprobar restricciones por escritura directa y coordinar en PostgreSQL las carreras entre cambios de padre, alta de hijo y desactivación, y pérdida del empleado responsable. Ejecutar en una base desechable hasta `SMOKE OK`.

## 5. Criterios de aceptación (medibles)

| # | Criterio | Verificación | Umbral |
|---|---|---|---|
| A1 | Arquitectura y compilación | `dotnet build -warnaserror`, `scripts/check-rules.sh` y `./scripts/verify.sh` | Gate en verde |
| A2 | API y autorización | Pruebas de cada literal, rutas, JWT y roles G1 | Lecturas autenticadas. Escrituras FINANZAS/ADMIN. Cada rechazo conserva los datos. |
| A3 | Catálogo | Códigos en mayúsculas, longitud y responsable opcional | Identidades únicas y ausencia de importes |
| A4 | Jerarquía y actividad | Ciclos, profundidad, padre con hijos activos y reactivación | Máximo 3 niveles. Referencias históricas preservadas |
| A5 | Database First | Aplicar SQL local, repetir seed, comprobar scaffold y ejecutar smoke | SCAFFOLD AL DÍA y SMOKE OK |

## 6. Checklist de ejecución

- [x] Incorporar las decisiones oficiales de §2 y G1 a G9.
- [ ] Terminar la revisión del PR de documentación y cerrar las precisiones de §4.
- [x] Verificar las dependencias disponibles, aplicar el SQL 045 en PostgreSQL desechable y generar entidades y DbContext.
- [x] Implementar API, pruebas, seed y smoke. Registrar los resultados de §5.

## 7. Registro de verificación

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-23 | Decisiones v0 | Aprobadas. Revisión final del PR pendiente. | [Respuesta de Lukas](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52#issuecomment-7109010161) |
| 2026-11-24 | A1–A4 | Compilación con cero advertencias. Reglas y gate correctos. | `dotnet build -warnaserror`, `scripts/check-rules.sh` y `scripts/verify.sh`. Pasaron 41 pruebas HTTP del 59 dentro de 1.232 pruebas de backend y 695 del frontend. |
| 2026-11-24 | A5 | `SCAFFOLD AL DÍA` y `SMOKE OK`. | PostgreSQL 16 desechable. El seed repetido conservó ids y relaciones. El smoke verificó restricciones, ciclos concurrentes, alta frente a desactivación y pérdida de FK con HTTP 409. |
