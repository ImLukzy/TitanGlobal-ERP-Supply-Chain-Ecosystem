# Módulo 22 – Categorías de Producto (Backend)

## 1. Información general

- **Módulo:** 22 – Categorías de Producto.
- **Nombre técnico:** `Modulo22_CategoriasProducto`.
- **Responsable:** Maxi.
- **Revisor:** Lukas.
- **Ruta API:** `api/v1/categorias-producto`.
- **Tecnologías:** .NET 10, ASP.NET Core, Entity Framework Core y PostgreSQL.
- **Arquitectura:** Monolito modular con Vertical Slices.

## 2. Objetivo

Implementar un catálogo independiente para administrar las categorías de los productos de TitanGlobal.

El módulo permitirá registrar, consultar, actualizar, desactivar y visualizar información resumida de las categorías.

La implementación seguirá las convenciones establecidas en los módulos anteriores del proyecto.

## 3. Alcance funcional

El módulo incluirá:

1. Listar las categorías registradas.
2. Buscar categorías por nombre.
3. Consultar el detalle de una categoría.
4. Registrar una nueva categoría.
5. Actualizar una categoría existente.
6. Desactivar una categoría.
7. Consultar un resumen de categorías.

La consulta de categorías permitirá distinguir entre registros activos e inactivos.

## 4. Modelo de datos

### 4.1. Entidad CategoriaProducto

Se creará una nueva entidad con las siguientes propiedades:

| Campo | Tipo | Descripción |
|---|---|---|
| Id | int | Identificador de la categoría. |
| Nombre | string | Nombre de la categoría. |
| Descripcion | string? | Descripción opcional. |
| Activa | bool | Indica si la categoría está activa. |

### 4.2. Reglas de la entidad

- El nombre es obligatorio.
- El nombre tendrá una longitud máxima de 100 caracteres.
- La descripción será opcional y tendrá una longitud máxima de 250 caracteres.
- Una categoría nueva se registrará como activa.
- No se permitirán nombres duplicados, considerando diferencias entre mayúsculas y minúsculas.

### 4.3. Relación con Producto

La entidad `Producto` actualmente contiene la propiedad `Categoria` de tipo `string`.

Para este módulo se mantendrá dicha estructura.

No se agregará `CategoriaId`, clave foránea ni propiedad de navegación a `Producto`.

La posible integración mediante una relación entre ambas entidades queda fuera del alcance de esta implementación.

## 5. Endpoints

Todos los endpoints utilizarán la ruta base:

`/api/v1/categorias-producto`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/v1/categorias-producto` | Listar categorías. |
| GET | `/api/v1/categorias-producto/{id}` | Consultar una categoría. |
| POST | `/api/v1/categorias-producto` | Registrar una categoría. |
| PUT | `/api/v1/categorias-producto/{id}` | Actualizar una categoría. |
| DELETE | `/api/v1/categorias-producto/{id}` | Desactivar una categoría. |
| GET | `/api/v1/categorias-producto/resumen` | Consultar el resumen de categorías. |

### 5.1. Listado

Permitirá buscar categorías por nombre y filtrar por estado.

Parámetros:

- `buscar`: texto opcional para buscar por nombre.
- `incluirInactivas`: indica si también se mostrarán las categorías inactivas.

Por defecto, se mostrarán únicamente las categorías activas.

### 5.2. Detalle

Permitirá consultar una categoría mediante su identificador.

Si no existe, se devolverá el error correspondiente.

### 5.3. Registro

Recibirá el nombre y la descripción opcional.

La categoría se registrará inicialmente con `Activa = true`.

Se validará que el nombre no esté duplicado.

### 5.4. Actualización

Permitirá modificar el nombre y la descripción de una categoría existente.

Se validará que el nombre no esté duplicado en otra categoría.

### 5.5. Eliminación

La operación DELETE realizará una desactivación lógica, cambiando `Activa` a `false`.

No se eliminará físicamente el registro de la base de datos.

### 5.6. Resumen

Devolverá información general:

- Total de categorías.
- Total de categorías activas.
- Total de categorías inactivas.

## 6. DTOs

Los DTOs estarán organizados dentro de:

`Modules/Modulo22_CategoriasProducto/Dtos/`

Se contemplan los siguientes DTOs:

### CategoriaProductoDto

- Id
- Nombre
- Descripcion
- Activa

### CrearCategoriaProductoDto

- Nombre
- Descripcion

### ActualizarCategoriaProductoDto

- Nombre
- Descripcion

### ResumenCategoriasProductoDto

- TotalCategorias
- CategoriasActivas
- CategoriasInactivas

Los controladores no expondrán directamente las entidades de dominio.

## 7. Organización del módulo

La implementación seguirá esta estructura:

```text
src/
└── TitanGlobal.Api/
    ├── Models/
    │   └── CategoriaProducto.cs
    │
    ├── Modules/
    │   └── Modulo22_CategoriasProducto/
    │       ├── Controllers/
    │       │   └── CategoriasProductoController.cs
    │       │
    │       ├── Dtos/
    │       │   └── CategoriaProductoDtos.cs
    │       │
    │       └── UseCases/
    │           ├── ListarCategoriasProductoUseCase.cs
    │           ├── ObtenerCategoriaProductoUseCase.cs
    │           ├── CrearCategoriaProductoUseCase.cs
    │           ├── ActualizarCategoriaProductoUseCase.cs
    │           ├── DesactivarCategoriaProductoUseCase.cs
    │           └── ObtenerResumenCategoriasProductoUseCase.cs
    │
    └── Models/
        └── TitanGlobalDbContext.cs
```

Los archivos de repositorios y Unit of Work existentes se reutilizarán cuando corresponda. No se crearán servicios de negocio adicionales.

## 8. Persistencia

Se agregará la entidad `CategoriaProducto` al contexto `TitanGlobalDbContext`.

También se configurarán explícitamente:

- Nombre de tabla.
- Clave primaria.
- Longitud máxima del nombre.
- Longitud máxima de la descripción.
- Restricciones necesarias para los campos.

Se generará una migración mediante Entity Framework Core.

No se modificarán manualmente los archivos de migración ni el snapshot.

Los cambios en archivos compartidos respetarán la regla de agregar contenido sin reorganizar el código existente.

## 9. Pruebas

Se implementarán pruebas para verificar:

- Listado de categorías.
- Búsqueda por nombre.
- Consulta por identificador.
- Registro correcto.
- Validación de nombre obligatorio.
- Prevención de nombres duplicados.
- Actualización de una categoría.
- Desactivación lógica.
- Consulta del resumen.
- Manejo de categorías inexistentes.

Las pruebas se ubicarán en:

`tests/TitanGlobal.Api.Tests/Modulo22_CategoriasProducto/`

Se utilizará el enfoque de pruebas establecido por el repositorio.

## 10. Criterios de aceptación

- [ ] La entidad CategoriaProducto está implementada.
- [ ] La tabla se encuentra registrada en PostgreSQL.
- [ ] La migración se genera y aplica correctamente.
- [ ] Los endpoints están disponibles en Swagger.
- [ ] Los DTOs están separados de las entidades.
- [ ] Los controladores llaman únicamente a UseCases.
- [ ] Las consultas y operaciones de base de datos son asíncronas.
- [ ] Se reutilizan el repositorio genérico y Unit of Work.
- [ ] Se validan los datos de entrada.
- [ ] Se evita el registro de categorías duplicadas.
- [ ] La desactivación no elimina físicamente los registros.
- [ ] Las pruebas automatizadas pasan correctamente.
- [ ] Se ejecuta la verificación general del proyecto.
- [ ] Se realiza el smoke test de PostgreSQL.
- [ ] Se documentan las evidencias para la revisión.

## 11. Restricciones

- No modificar la entidad Producto.
- No modificar la propiedad Producto.Categoria.
- No agregar relaciones ni claves foráneas entre Producto y CategoriaProducto.
- No modificar middlewares ni el pipeline de Program.cs.
- No implementar lógica de negocio en los controladores.
- No crear interfaces de UseCases.
- No introducir MediatR ni CQRS.
- No modificar código de otros módulos fuera de lo necesario y aprobado.

## 12. Evidencias para revisión

Se adjuntarán:

- Captura de la estructura del módulo.
- Captura de la tabla creada en PostgreSQL.
- Capturas de los endpoints funcionando en Swagger.
- Capturas de las pruebas automatizadas.
- Resultado de la verificación general.
- Resultado del smoke test de PostgreSQL.

## 13. Revisión y aprobación

La especificación será revisada por Lukas antes de comenzar la implementación.

Cualquier modificación de alcance, especialmente una futura relación entre productos y categorías, deberá ser revisada y aprobada antes de incorporarse al código.