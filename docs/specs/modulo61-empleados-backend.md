# Especificación: 47 - Modulo61_Empleados (backend)

## 1. Objetivo
**Problema:** no existe el legajo digital de los empleados. Los módulos 62 a 70 de Capital Humano dependen de él (`docs/hcm-reglas-negocio.md` §2).
**Resultado esperado:** entidad `Empleado` y `Modules/Modulo61_Empleados/` en `api/v1/empleados`:
- alta con legajo autogenerado (`EMP-000001`), datos personales, laborales y de contacto;
- listado paginado con búsqueda y filtro por estado, detalle, edición de datos y cese;
- estados `Activo` y `Cesado`; sin borrado físico (T1).

Reglas aplicadas: E1, E2, E3, T1, T5 y T6 de `docs/hcm-reglas-negocio.md` (§1 y §2, módulo 61). Todo valor que esas reglas no fijan se marca **[elegido]** en §4 para su revisión.

## 2. Fuera de alcance
- Contratos laborales (módulo 62) y el cierre de contratos al cesar (E4). Ver decisión 1.
- Desactivar el usuario asociado en 71 (U2). Ver decisión 2.
- Reactivar un empleado cesado: ninguna regla aprobada lo prevé.
- Borrado físico y `DELETE` de empleados (T1).
- Auditoría propia: el módulo 77 ya registra cada alta, modificación y baja de cualquier entidad (`Models/TitanGlobalDbContext.Auditoria.cs`). Esta spec no añade código de auditoría.
- Datos de familiares, cuentas bancarias, documentos adjuntos y fotografía.
- Cambios en `Middlewares/`, `Services/`, `Program.cs`, `Configuration/`, `Models/Rol.cs` y `Models/Usuario.cs`.
- El frontend (spec 48).

**Decisiones de producto que requieren aprobación antes de ejecutar:**
1. **Cese y contratos (E4).** Al cesar, ¿se cierran automáticamente los contratos vigentes en la fecha de cese, o el cese se bloquea mientras exista un contrato vigente? Depende del módulo 62, que aún no existe. Hasta decidir, el cese solo cambia estado, fecha y motivo.
2. **Usuario asociado (U2).** Al cesar, ¿se desactiva el usuario de 71 vinculado? `Models/Usuario.cs` no tiene ningún vínculo con empleado; hacerlo exige un campo nuevo en 61 o en 71.
3. **Maestro del 08.** ¿61 debe ser el maestro de `ComisionesVendedores`? Hoy 08 tiene su propio maestro (`Models/Vendedor.cs`). **No bloquea 61.**
4. **Aprobadores de horas extra y ausencias.** ¿Las aprueba el rol `Supervisor` o el jefe de área, que no existe en el modelo? (`docs/hcm-reglas-negocio.md` §3, pregunta 4). **No bloquea 61.**
5. **Fecha de cese (E2): discrepancia entre fuentes.** El encargo aprobado dice «fecha de cese ≥ fecha de ingreso». `docs/hcm-reglas-negocio.md` §2 dice «fecha de cese = fecha de ingreso». Esta spec implementa `≥`. Confirmar antes de programar la regla de cese; no afecta a los demás campos.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Domain/Models/Empleado.cs` | crear | entidad (§4) |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | modificar | solo se añade: 1 `DbSet` y un bloque de mapeo (§4). No se editan líneas existentes |
| `src/TitanGlobal.Infrastructure/Migrations/<marca>_Empleados.cs` y `.Designer.cs` | crear | `dotnet ef migrations add Empleados -p src/TitanGlobal.Api` |
| `src/TitanGlobal.Infrastructure/Migrations/TitanGlobalDbContextModelSnapshot.cs` | modificar | lo regenera EF; no se edita a mano |
| `src/TitanGlobal.Api/Controllers/Modulo61_Empleados/EmpleadosController.cs` | crear | `[Route("api/v1/empleados")]` |
| `src/TitanGlobal.Application/DTOs/Modulo61_Empleados/EmpleadosDtos.cs` | crear | `PaginaEmpleadosDto`, `EmpleadoListadoDto`, `EmpleadoDetalleDto`, `EmpleadoIdDto` |
| `src/TitanGlobal.Application/DTOs/Modulo61_Empleados/GuardarEmpleadoRequestDto.cs` | crear | cuerpo de alta y edición |
| `src/TitanGlobal.Application/DTOs/Modulo61_Empleados/CeseEmpleadoRequestDto.cs` | crear | cuerpo de cese |
| `src/TitanGlobal.Application/UseCases/Modulo61_Empleados/AdministrarEmpleadosUseCase.cs` | crear | UseCase concreto, sin interfaz |
| `src/TitanGlobal.Application/UseCases/Modulo61_Empleados/ValidacionEmpleado.cs` | crear | helper estático interno |
| `scripts/seed-dev.sql` | modificar | bloque nuevo justo antes de la última línea `COMMIT;` |
| `scripts/smoke-postgres.sh` | modificar | variable nueva, claves nuevas en el diccionario de validaciones y `request_check` nuevos antes de `printf 'SMOKE OK\n'` |
| `tests/TitanGlobal.Api.Tests/Modulo61_Empleados/EmpleadosApiTests.cs` | crear | pruebas de §5 |

Verificado en el repositorio: todas las rutas marcadas «modificar» existen; las marcadas «crear» no existen todavía. Los UseCases se registran por reflexión (`Configuration/DependencyInjectionExtensions.cs`), así que `Program.cs` no cambia.

## 4. Diseño y lógica
**Convenciones.** Sin `var`, sin miembros con `=>` y sin interfaces de UseCase (`scripts/check-rules.sh`). Controlador delgado: solo llama al UseCase. Las fechas de negocio son `DateOnly` (T5). Es el primer uso de `DateOnly` en `src/TitanGlobal.Api`; Npgsql lo mapea a `date`, así que no hereda el problema de `Kind` de la spec 05.

**Entidad `Empleado`** (`Models/Empleado.cs`, sin propiedades de navegación):
| Propiedad | Tipo | Restricción |
|---|---|---|
| `Id` | `int` | identidad de PostgreSQL |
| `Legajo` | `string` | requerido, máximo 20, único (`IX_Empleados_Legajo`). Valor final `EMP-{Id:D6}` |
| `TipoDocumento` | `string` | requerido, máximo 10; `DNI`, `CE` o `Pasaporte` |
| `NumeroDocumento` | `string` | requerido, máximo 20; único junto con `TipoDocumento` |
| `Nombres` | `string` | requerido, máximo 100 |
| `Apellidos` | `string` | requerido, máximo 100 |
| `FechaNacimiento` | `DateOnly` | requerido, `date` |
| `FechaIngreso` | `DateOnly` | requerido, `date` |
| `Cargo` | `string` | requerido, máximo 100 |
| `Area` | `string` | requerido, máximo 100; texto libre en v0 (E1) |
| `Email` | `string?` | opcional, máximo 150 |
| `Telefono` | `string?` | opcional, máximo 20 |
| `Estado` | `string` | requerido, máximo 20; `Activo` o `Cesado`; el alta siempre crea `Activo` |
| `FechaCese` | `DateOnly?` | `date`; solo con `Estado = Cesado` |
| `MotivoCese` | `string?` | máximo 300; solo con `Estado = Cesado` |

**Legajo (mecanismo elegido: secuencia de base de datos).** Se usa la columna de identidad `Id` de PostgreSQL. Dentro de una misma transacción: (1) se inserta con un legajo temporal `TMP-` + 16 caracteres hexadecimales (20 caracteres, cabe en `MaxLength(20)`); (2) se obtiene el `Id`; (3) se actualiza `Legajo = EMP-{Id:D6}`. Es el mismo patrón que `ContratosComercialesUseCase.CrearAsync` (`contrato.Numero = $"TMP-..."`). No se usa `max + 1` (condición de carrera) ni una secuencia nueva. Consecuencia conocida: si una transacción se revierte, el siguiente legajo deja un hueco.

**Contrato.** JSON en camelCase. Errores `{ StatusCode, Message }` (`ErrorHandlingMiddleware`); `Detail` solo en 500.

| Verbo | Ruta | Entrada | Respuesta OK | Errores |
|---|---|---|---|---|
| GET | `api/v1/empleados?busqueda=&estado=&pagina=1&tamanoPagina=10` | query opcional | 200 `PaginaEmpleadosDto` | 400 |
| GET | `api/v1/empleados/{id:int}` | — | 200 `EmpleadoDetalleDto` | 404 |
| POST | `api/v1/empleados` | `GuardarEmpleadoRequestDto` | 200 `{ id }` | 400, 409 |
| PUT | `api/v1/empleados/{id:int}` | `GuardarEmpleadoRequestDto` | 200 `{ id }` | 400, 404, 409 |
| POST | `api/v1/empleados/{id:int}/cese` | `CeseEmpleadoRequestDto` | 200 `{ id }` | 400, 404, 409 |

- Sin `[PermitirCuerpoVacio]`: todas las operaciones llevan cuerpo. Un cuerpo vacío responde 400 con el literal existente de `ParameterValidationMiddleware` («El cuerpo de la petición (Body) no puede estar vacío.»), que no cambia.
- Respuesta de alta, edición y cese: `{ id }` (`EmpleadoIdDto`), igual que la spec 43 **[elegido]**.
- Editar o cesar a un empleado `Cesado`: **409** **[elegido]**. La petición es válida; el conflicto es con el estado del recurso, mismo criterio que el 409 del último ADMIN en la spec 43.
- Sin `DELETE`: la ruta no existe (T1).

**DTOs** (`Modules/Modulo61_Empleados/Dtos/`):
- `GuardarEmpleadoRequestDto { TipoDocumento, NumeroDocumento, Nombres, Apellidos, FechaNacimiento, FechaIngreso, Cargo, Area, Email, Telefono }`. Las fechas son `DateOnly?`: un campo ausente llega como `null` y no como `0001-01-01`. No incluye `Estado` ni `Legajo`: el cliente no los fija.
- `CeseEmpleadoRequestDto { FechaCese (DateOnly?), Motivo }`.
- `PaginaEmpleadosDto { Items, Total, Pagina, TamanoPagina }` (mismo sobre que la spec 43 §4).
- `EmpleadoListadoDto { Id, Legajo, TipoDocumento, NumeroDocumento, Nombres, Apellidos, Cargo, Area, FechaIngreso, Estado }`.
- `EmpleadoDetalleDto : EmpleadoListadoDto` con `FechaNacimiento, Email, Telefono, FechaCese, MotivoCese`. `Email`, `Telefono`, `FechaCese` y `MotivoCese` son `null` cuando no aplican.
- `EmpleadoIdDto { Id }`.

**Reglas**, en `UseCases` con `ReglaNegocioException`. Gana el primer fallo. Los textos literales están en la tabla de mensajes que sigue a este bloque.

*Validación de campos* (`ValidacionEmpleado.Normalizar`; alta y edición; en este orden):
1. `tipoDocumento`: tras `Trim()`, debe ser `DNI`, `CE` o `Pasaporte` (distingue mayúsculas). Mensaje 1.
2. `numeroDocumento`: tras `Trim()`, obligatorio (mensaje 2); máximo 20 (3); solo letras y dígitos, regex `^[A-Za-z0-9]+$` (4) **[elegido]**.
3. `nombres`: tras `Trim()`, obligatorio (5); máximo 100 (6).
4. `apellidos`: tras `Trim()`, obligatorio (7); máximo 100 (8).
5. `fechaNacimiento` presente (9).
6. `fechaIngreso` presente (10).
7. `cargo`: tras `Trim()`, obligatorio (11) **[elegido]**; máximo 100 (12).
8. `area`: tras `Trim()`, obligatoria (13) **[elegido]**; máximo 100 (14).
9. `email`: opcional **[elegido]**. Si tiene valor, tras `Trim()` y en minúsculas: máximo 150 y formato `^[^@\s]+@[^@\s]+\.[^@\s]+$`, el mismo regex de la spec 43 (15).
10. `telefono`: opcional **[elegido]**. Si tiene valor, tras `Trim()`: máximo 20 (16).

*Edad mínima* (E3; cálculo calendario **[elegido]**): `FechaIngreso >= FechaNacimiento.AddYears(18)`; si no se cumple, 400 (17). Se evalúa en alta y en edición, después de los campos.

*Alta* (`CrearAsync`):
1. Validación de campos (1 a 16).
2. Edad mínima (17).
3. Documento único: si ya existe el par (`TipoDocumento`, `NumeroDocumento`), 409 (18).
4. Transacción: legajo y estado `Activo` según §4 «Legajo».
5. Si PostgreSQL responde `23505` sobre `IX_Empleados_TipoDocumento_NumeroDocumento` (carrera), mismo 409 (18), con la detección de `EsDuplicado` de la spec 43. Cualquier otro `23505` se propaga.

*Edición* (`ActualizarAsync(id, solicitud)`):
1. Validación de campos (1 a 16).
2. Edad mínima (17).
3. Documento único excluyendo `id` (18).
4. Obtener por `id`: si no existe, 404 (28).
5. Si `Estado = Cesado`, 409 (19).
6. Asigna solo los campos editables; no cambia `Legajo`, `Estado`, `FechaCese` ni `MotivoCese`. Guarda; `23505` se traduce a 18.

*Cese* (`CesarAsync(id, solicitud)`):
1. `fechaCese` presente (20).
2. `motivo`: tras `Trim()`, obligatorio (21); máximo 300 (22).
3. Obtener por `id`: si no existe, 404 (28).
4. Si `Estado = Cesado`, 409 (23).
5. Si `FechaCese < FechaIngreso`, 400 (24). Igual a la fecha de ingreso es válido (decisión 5).
6. Guarda `Estado = Cesado`, `FechaCese` y `MotivoCese`. Nada más: E4 queda pendiente (decisión 1).

*Consulta por id* (`ObtenerAsync`): 404 (28).

*Listado* (`ListarAsync(busqueda, estado, pagina, tamanoPagina)`):
1. `pagina` mayor o igual a 1 (25).
2. `tamanoPagina` entre 1 y 100 (26). Por defecto 10.
3. `estado`, si llega con valor, es `Activo` o `Cesado` (27). Un valor vacío se trata como omitido **[elegido]**.

**Mensajes literales** (contrato; las pruebas comparan el texto exacto):
| # | Regla | Código | Mensaje |
|---|---|---|---|
| 1 | tipo de documento fuera de `DNI`, `CE`, `Pasaporte` | 400 | El tipo de documento no es válido. |
| 2 | número de documento vacío | 400 | El número de documento es obligatorio. |
| 3 | número de documento de más de 20 caracteres | 400 | El número de documento no puede superar 20 caracteres. |
| 4 | número de documento con caracteres no alfanuméricos | 400 | El número de documento solo admite letras y números. |
| 5 | nombres vacíos | 400 | Los nombres son obligatorios. |
| 6 | nombres de más de 100 caracteres | 400 | Los nombres no pueden superar 100 caracteres. |
| 7 | apellidos vacíos | 400 | Los apellidos son obligatorios. |
| 8 | apellidos de más de 100 caracteres | 400 | Los apellidos no pueden superar 100 caracteres. |
| 9 | falta fecha de nacimiento | 400 | La fecha de nacimiento es obligatoria. |
| 10 | falta fecha de ingreso | 400 | La fecha de ingreso es obligatoria. |
| 11 | cargo vacío | 400 | El cargo es obligatorio. |
| 12 | cargo de más de 100 caracteres | 400 | El cargo no puede superar 100 caracteres. |
| 13 | área vacía | 400 | El área es obligatoria. |
| 14 | área de más de 100 caracteres | 400 | El área no puede superar 100 caracteres. |
| 15 | email con formato inválido o más de 150 caracteres | 400 | El email no tiene un formato válido. |
| 16 | teléfono de más de 20 caracteres | 400 | El teléfono no puede superar 20 caracteres. |
| 17 | menor de 18 años a la fecha de ingreso (E3) | 400 | El empleado debe tener al menos 18 años a la fecha de ingreso. |
| 18 | documento repetido en alta o edición | 409 | Ya existe un empleado con el documento {tipoDocumento} {numeroDocumento}. |
| 19 | edición de un empleado cesado | 409 | No se puede editar un empleado cesado. |
| 20 | falta fecha de cese | 400 | La fecha de cese es obligatoria. |
| 21 | motivo de cese vacío | 400 | El motivo de cese es obligatorio. |
| 22 | motivo de cese de más de 300 caracteres | 400 | El motivo de cese no puede superar 300 caracteres. |
| 23 | cese de un empleado ya cesado | 409 | El empleado {id} ya está cesado. |
| 24 | fecha de cese anterior a la de ingreso (E2) | 400 | La fecha de cese no puede ser anterior a la fecha de ingreso. |
| 25 | página menor que 1 | 400 | La página debe ser mayor o igual a 1. |
| 26 | tamaño de página fuera de 1 a 100 | 400 | El tamaño de página debe estar entre 1 y 100. |
| 27 | estado distinto de `Activo` o `Cesado` | 400 | El estado debe ser Activo o Cesado. |
| 28 | empleado inexistente | 404 | El empleado {id} no existe. |

Los mensajes 25 y 26 son los de la spec 43, copiados sin cambios. Los 400 de cuerpo vacío son los del middleware (ver «Contrato»).

**Consultas.**
- Listado: `Repository<Empleado>().AsQueryable()`. `busqueda` se normaliza con `Trim().ToLowerInvariant()`; si no está vacía, `Where` con `ToLower().Contains(termino)` sobre `Nombres`, `Apellidos`, `NumeroDocumento` y `Legajo` (`// PATRÓN LINQ 1`, como en `AdministrarUsuariosUseCase`). `estado` por igualdad exacta. Orden: `Apellidos`, `Nombres`, `Id` **[elegido]**. Una consulta `CountAsync` y una consulta paginada proyectada a `EmpleadoListadoDto`: sin cargar entidades ni N+1.
- Detalle, edición y cese: una consulta por `Id` con `SingleOrDefaultAsync` (mismo patrón que `ValidacionUsuario.ObtenerAsync`).
- Unicidad: `AnyAsync` sobre (`TipoDocumento`, `NumeroDocumento`), excluyendo el propio `Id` en edición.
- Todas las consultas son asíncronas (CONTRIBUTING §4).

**Seed (`scripts/seed-dev.sql`).** Bloque nuevo justo antes de la última línea `COMMIT;`. Sin caracteres no ASCII. Cuatro empleados ficticios: tres `Activo` y uno `Cesado`. Todas las edades al ingreso son de 18 años o más.
```sql
TRUNCATE TABLE "Empleados" RESTART IDENTITY CASCADE;

INSERT INTO "Empleados" ("Id", "Legajo", "TipoDocumento", "NumeroDocumento", "Nombres", "Apellidos", "FechaNacimiento", "FechaIngreso", "Cargo", "Area", "Email", "Telefono", "Estado", "FechaCese", "MotivoCese") VALUES
    (1, 'EMP-000001', 'DNI', '10000001', 'Ana', 'Torres Vega', DATE '1990-05-14', DATE '2021-03-01', 'Gerente de Ventas', 'Ventas', 'ana.torres@titanglobal.example', '999000001', 'Activo', NULL, NULL),
    (2, 'EMP-000002', 'DNI', '10000002', 'Luis', 'Rojas Mendoza', DATE '1988-11-02', DATE '2019-08-16', 'Analista de Compras', 'Compras', 'luis.rojas@titanglobal.example', '999000002', 'Activo', NULL, NULL),
    (3, 'EMP-000003', 'CE', '004512367', 'Maria', 'Salazar Quispe', DATE '1995-02-20', DATE '2024-01-08', 'Almacenero', 'Almacen', 'maria.salazar@titanglobal.example', '999000003', 'Activo', NULL, NULL),
    (4, 'EMP-000004', 'Pasaporte', 'P1234567', 'Carlos', 'Diaz Soto', DATE '1982-07-30', DATE '2018-05-02', 'Auxiliar de Logistica', 'Logistica', NULL, NULL, 'Cesado', DATE '2026-09-30', 'Renuncia voluntaria');

SELECT setval(pg_get_serial_sequence('"Empleados"', 'Id'), (SELECT MAX("Id") FROM "Empleados"));
```
Como el seed hace `TRUNCATE`, cada `smoke-postgres.sh` parte de estos cuatro registros.

**Migración.** `dotnet ef migrations add Empleados -p src/TitanGlobal.Api`. Nada escrito a mano (CONTRIBUTING §4). Solo una persona genera la migración a la vez (CONTRIBUTING §7). Índices esperados en la migración: `IX_Empleados_Legajo` e `IX_Empleados_TipoDocumento_NumeroDocumento`.

**DbContext** (`Models/TitanGlobalDbContext.cs`). Solo se añade:
1. Después de `public DbSet<LiquidacionComision> LiquidacionesComisiones { get; set; } = null!;`:
```csharp

    public DbSet<Empleado> Empleados { get; set; } = null!;
```
2. Al final de `OnModelCreating`, después del bloque de `Rol.Usuarios` y antes de la llave de cierre del método:
```csharp
        modelBuilder.Entity<Empleado>()
            .Property(empleado => empleado.Legajo)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<Empleado>()
            .Property(empleado => empleado.TipoDocumento)
            .HasMaxLength(10)
            .IsRequired();

        modelBuilder.Entity<Empleado>()
            .Property(empleado => empleado.NumeroDocumento)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<Empleado>()
            .Property(empleado => empleado.Nombres)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Empleado>()
            .Property(empleado => empleado.Apellidos)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Empleado>()
            .Property(empleado => empleado.FechaNacimiento)
            .HasColumnType("date")
            .IsRequired();

        modelBuilder.Entity<Empleado>()
            .Property(empleado => empleado.FechaIngreso)
            .HasColumnType("date")
            .IsRequired();

        modelBuilder.Entity<Empleado>()
            .Property(empleado => empleado.Cargo)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Empleado>()
            .Property(empleado => empleado.Area)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Empleado>()
            .Property(empleado => empleado.Email)
            .HasMaxLength(150);

        modelBuilder.Entity<Empleado>()
            .Property(empleado => empleado.Telefono)
            .HasMaxLength(20);

        modelBuilder.Entity<Empleado>()
            .Property(empleado => empleado.Estado)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<Empleado>()
            .Property(empleado => empleado.FechaCese)
            .HasColumnType("date");

        modelBuilder.Entity<Empleado>()
            .Property(empleado => empleado.MotivoCese)
            .HasMaxLength(300);

        modelBuilder.Entity<Empleado>()
            .HasIndex(empleado => empleado.Legajo)
            .IsUnique();

        modelBuilder.Entity<Empleado>()
            .HasIndex(empleado => new { empleado.TipoDocumento, empleado.NumeroDocumento })
            .IsUnique();
```

**Pruebas** (`tests/TitanGlobal.Api.Tests/Modulo61_Empleados/EmpleadosApiTests.cs`). Mismo patrón que `UsuariosApiTests`: `[Collection("Modulo04")]`, fábrica compartida `OrdenesCompraApiFactory` (definida en `Modulo04_OrdenesCompra/OrdenesCompraApiTests.cs`), `SeedDatabase()` y `RestablecerDatos()` que vacía `Empleados`. Cada prueba crea sus propios datos.
- T1: `[Theory]` con un caso por cada mensaje 1 a 16: código y texto exactos, sin `Detail`, y ninguna fila creada ni cambiada.
- T2: E3. Nacida el 2008-03-02 con ingreso 2026-03-01 → 400 (17). Ingreso 2026-03-02 → 200.
- T3: documento. Mismo par (`TipoDocumento`, `NumeroDocumento`) → 409 (18), sin cambios. Mismo número con otro tipo → 200. Edición con su propio documento → 200. Edición con documento ajeno → 409 (18).
- T4: legajo. Tras el alta, `Legajo == "EMP-" + Id.ToString("D6")` y `Estado == "Activo"`.
- T5: listado. Búsqueda por nombres, apellidos, documento y legajo, en mayúsculas y minúsculas; filtro por `estado`; tamaño por defecto 10; orden; `total`; mensajes 25, 26 y 27.
- T6: detalle. Inexistente → 404 (28). Fecha de nacimiento ida y vuelta en formato `yyyy-MM-dd`.
- T7: cese. Válido → 200 `{ id }` con `Estado`, `FechaCese` y `MotivoCese`. Fecha de cese igual al ingreso → 200. Mensajes 20, 21, 22 y 24 con 400, sin cambios. Cese repetido → 409 (23), sin cambios.
- T8: edición de cesado → 409 (19), sin cambios.
- T9: sin borrado. `DELETE api/v1/empleados/{id}` no responde 2xx y la fila sigue existiendo.
- T10: auditoría. Alta y cese generan `RegistroAuditoria` con `Entidad = "Empleado"` y `Accion` `Alta` o `Modificacion`.

**Gate y humo.**
- `./scripts/verify.sh` (A1).
- `./scripts/smoke-postgres.sh`, obligatorio porque toca la base de datos. Añade a `smoke-postgres.sh`: la variable `EMPLEADO_CREADO_ID`; `openapi-empleados`; y estas llamadas con `request_check`:
  - `GET /api/v1/empleados?estado=Cesado` → 200; `total == 1` y `items[0].legajo == "EMP-000004"`.
  - `GET /api/v1/empleados/1` → 200; `legajo == "EMP-000001"`.
  - `POST /api/v1/empleados` con un empleado nuevo válido (DNI `20000001`, `FechaIngreso` 2026-10-01) → 200; guarda el id en `EMPLEADO_CREADO_ID`.
  - `POST /api/v1/empleados` con DNI `10000001` → 409 con el mensaje 18 literal.
  - `POST /api/v1/empleados` con un menor de edad → 400 con el mensaje 17 literal.
  - `POST /api/v1/empleados/{EMPLEADO_CREADO_ID}/cese` con `fechaCese` 2026-10-31 y un motivo → 200.
  - `PUT /api/v1/empleados/4` con un cuerpo válido completo → 409 con el mensaje 19 literal.
  - `POST /api/v1/empleados/{EMPLEADO_CREADO_ID}/cese` otra vez, con cuerpo válido → 409 con el mensaje 23 literal.
- Los cuerpos de PUT y cese deben ser válidos: la validación de campos (mensajes 1 a 16 y 20 a 22) se evalúa antes que los 409 de estado.
- El humo no borra el empleado creado (no hay `DELETE`); el siguiente seed lo elimina con `TRUNCATE`.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | salida 0, sin warnings |
| A2 | Reglas de alta y edición (E1, E3, documento único) | T1, T2, T3: un caso por mensaje 1 a 18 con código y texto exactos y sin `Detail`; ningún caso inválido crea ni cambia filas | pasan |
| A3 | Cese (E2) y edición de cesados | T7 y T8: mensajes 19 a 24 con código y texto exactos; sin cambios de filas tras errores | pasan |
| A4 | Listado y detalle | T5 y T6: búsqueda en 4 campos sin distinguir mayúsculas, filtro, paginación, orden, `total`, mensajes 25 a 28 | pasan |
| A5 | Legajo, fechas y auditoría | T4 y T10: formato `EMP-` + 6 dígitos igual a `Id`; `yyyy-MM-dd` ida y vuelta; alta y cese auditados | pasan |
| A6 | Sin borrado físico (T1) | T9 | pasa |
| A7 | PostgreSQL | `./scripts/smoke-postgres.sh`: migración `Empleados` aplicada y checks de §4 «Gate y humo» | `SMOKE OK` |
| A8 | OpenAPI | `/openapi/v1.json` incluye `GET`/`POST` de `/api/v1/empleados`, `GET`/`PUT` de `/api/v1/empleados/{id}` y `POST` de `/api/v1/empleados/{id}/cese` | presentes |
| A9 | Alcance | en el diff contra `main`, los únicos archivos son los de §3; `Middlewares/`, `Services/`, `Program.cs`, `Configuration/`, `Models/Rol.cs` y `Models/Usuario.cs` sin cambios | 0 archivos fuera de §3 |

## 6. Checklist de ejecución
- [ ] Spec aprobada por Lukas. Decisión 5 confirmada antes de programar la regla de cese.
- [ ] Tarea 1: entidad `Empleado`, `DbContext` (solo añadidos) y migración `Empleados`.
- [ ] Tarea 2: DTOs, `AdministrarEmpleadosUseCase`, `ValidacionEmpleado` y controlador.
- [ ] Tarea 3: pruebas T1 a T10, seed y `smoke-postgres.sh`.
- [ ] Tarea 4: A1 a A9.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
