# Cómo contribuir

Guía de trabajo del equipo. Para dejar el proyecto funcionando en tu máquina, sigue [`docs/equipo/primer-dia.md`](docs/equipo/primer-dia.md). Para la cadencia semanal y la meta de 3 módulos por semana, [`docs/equipo/cadencia.md`](docs/equipo/cadencia.md).

## 1. Qué es este repositorio

ERP transaccional para una corporación manufacturera y logística: 80 módulos en 8 dominios. El núcleo evaluado está en [`docs/enunciado.md`](docs/enunciado.md); la arquitectura, los módulos y las decisiones en [`docs/arquitectura.md`](docs/arquitectura.md). El estado actual y los pendientes, en [`docs/contexto-proyecto.md`](docs/contexto-proyecto.md).

## 2. Stack y estructura

- **Backend:** .NET 10, ASP.NET Core con controladores, EF Core + Npgsql, PostgreSQL 16 (Docker local), xUnit + `WebApplicationFactory` + EF InMemory.
- **Frontend:** React 19, React Router, Vite, TypeScript estricto, Vitest, ESLint sin warnings.
- **Vertical slices:** `src/TitanGlobal.Api/Modules/ModuloNN_Nombre/{Controllers,Dtos,UseCases}`. El namespace sigue a la carpeta y `NN` sigue el orden de [`docs/arquitectura.md`](docs/arquitectura.md).
- **Tests:** `tests/TitanGlobal.Api.Tests/ModuloNN_Nombre/`.
- **Frontend por módulo:** `frontend/src/modules/<modulo>/`.
- **Migraciones:** `src/TitanGlobal.Api/Migrations/`, siempre generadas con `dotnet ef migrations add`.

## 3. Flujo de un módulo

1. **Issue:** crea o toma el issue del módulo en el tablero. Anota owner, revisor y semana.
2. **Spec:** copia [`docs/specs/_TEMPLATE.md`](docs/specs/_TEMPLATE.md) a `docs/specs/NN-moduloXX-nombre-{backend|frontend}.md`. Contrato exacto: rutas, DTOs, mensajes de error literales, reglas en orden y criterios medibles. Lukas la revisa antes de programar.
3. **Rama:** sale de `main` actualizado: `git switch -c tu-nombre/moduloXX-nombre main`.
4. **Implementación:** sigue el módulo de referencia (Proveedores o SolicitudesCompra) y las reglas de §4.
5. **Gate:** `./scripts/verify.sh` en verde. Si tocas base de datos, además `./scripts/smoke-postgres.sh` → `SMOKE OK`.
6. **Pull request:** contra `main`, con la plantilla del repositorio. Incluye la salida del gate y capturas a 375 y 1280 px, en claro y oscuro.
7. **Revisión:** un compañero distinto del owner revisa solo la spec y el diff. Lukas hace la revisión final e integra.
8. **QA:** recorre cada criterio de la spec contra la API y PostgreSQL locales. Adjunta la evidencia.
9. **Integración:** se integra en `main` con el gate en verde y se actualiza la evidencia en §7 de la spec.
10. **Demo:** Lukas despliega y comprueba la pantalla en producción.

## 4. Reglas de código

- **Sin `=>` en miembros:** constructores, métodos, propiedades y acciones usan bloques `{ }` y `return` explícito. `=>` solo dentro de lambdas (LINQ, opciones de configuración).
- **Tipos explícitos:** nada de `var`.
- **UseCases concretos sin interfaz**, nombre terminado en `UseCase`, registrados por reflexión en `Program.cs`. Nunca registrar un UseCase a mano.
- **Controladores delgados:** cero lógica de negocio; solo llaman a UseCases. Campos `private readonly _camelCase`.
- **Services/** solo para servicios técnicos transversales (auth, etc.). La lógica de negocio va en `Modules/*/UseCases`.
- **Persistencia** solo con `IUnitOfWork` / `IGenericRepository<T>`; consultas asíncronas (`ToListAsync`, `SumAsync`, `AnyAsync`…).
- **DTOs** por módulo en `Modules/*/Dtos`; nunca exponer entidades en controladores.
- **Pipeline fijo** en `Program.cs`: `ErrorHandlingMiddleware` → `UseRouting` → `ParameterValidationMiddleware` → `RoleBasedAccessMiddleware` → `UseAuthorization` → `MapControllers`.
- **Rutas** `api/v1/<recurso-en-kebab-case>`.
- **Acciones POST/PUT sin cuerpo** (aprobar, recibir…): atributo `[PermitirCuerpoVacio]`. Nunca añadas rutas al `ParameterValidationMiddleware`.
- **Errores en JSON** `{ StatusCode, Message }` (más `Detail` solo en 500), con los mensajes literales de [`docs/enunciado.md`](docs/enunciado.md) §4.
- **Contratos** de repositorio y entidades: los de [`docs/enunciado.md`](docs/enunciado.md) §3.
- **Código legible:** una sentencia por línea, un miembro por línea; nada de clases o métodos comprimidos.
- **Migraciones** siempre con `dotnet ef migrations add` (con `Designer` y `ModelSnapshot`); nunca escritas a mano.

## 5. Comandos

```sh
docker compose up -d db
dotnet tool restore
dotnet ef database update -p src/TitanGlobal.Api
dotnet run --project src/TitanGlobal.Api --urls http://localhost:5080
npm --prefix frontend ci
npm --prefix frontend run dev

./scripts/verify.sh          # gate completo
./scripts/smoke-postgres.sh  # prueba contra PostgreSQL real
```

## 6. Antes de pedir revisión

- [ ] La spec está aprobada y el §6 marcado.
- [ ] `./scripts/verify.sh` en verde, sin warnings.
- [ ] Si cambió la base de datos: migración generada con `dotnet ef` y `SMOKE OK`.
- [ ] Tests nuevos para las reglas y los errores del módulo.
- [ ] QA a 375 y 1280 px, en claro y en oscuro, con capturas adjuntas.
- [ ] Evidencia en §7 de la spec: fecha, criterio, resultado y comando o captura.
- [ ] El PR no incluye secretos, archivos generados ni cambios ajenos al módulo.
- [ ] El PR tiene owner, revisor y la semana anotados.

## 7. Convivencia

- **Un módulo, una persona.** Así nadie toca los mismos archivos.
- **Ramas cortas:** integra a menudo; una rama que vive una semana acaba en conflictos.
- **Migraciones serializadas:** solo una persona genera migración a la vez; avisa en el canal, actualiza `main` y vuelve a generar si hay conflicto. Nunca edites el snapshot a mano.
- **Archivos compartidos** (`TitanGlobalDbContext.cs`, `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh`, `frontend/src/modules/pantallas.ts`, `frontend/src/styles/app.css`): añade tu bloque al final y no reordenes lo de otros.
- **Núcleo congelado:** las 4 entidades del enunciado (`Cliente`, `Producto`, `OrdenCompra`, `DetalleOrdenCompra`), los middlewares y el pipeline no se tocan sin una spec aprobada por Lukas.
- **Commits con tu identidad git**, sin trailers ni menciones a herramientas externas.
- **Nada de secretos** en el repositorio: ni `.env`, ni cadenas de conexión, ni tokens.
- **Bloqueos:** si llevas más de medio día atascado, pide ayuda en el canal; no sigas a ciegas.

## 8. Dónde está cada cosa

- Qué se evalúa: [`docs/enunciado.md`](docs/enunciado.md)
- Módulos, contratos y frontend: [`docs/arquitectura.md`](docs/arquitectura.md)
- Estado, decisiones y pendientes: [`docs/contexto-proyecto.md`](docs/contexto-proyecto.md)
- Instalación y primer día: [`docs/equipo/primer-dia.md`](docs/equipo/primer-dia.md)
- Cadencia y meta semanal: [`docs/equipo/cadencia.md`](docs/equipo/cadencia.md)
- Reparto de módulos: [`docs/equipo/asignaciones.md`](docs/equipo/asignaciones.md)
- Errores que no hay que repetir: [`docs/equipo/lecciones.md`](docs/equipo/lecciones.md)
- Specs: [`docs/specs/`](docs/specs/)
