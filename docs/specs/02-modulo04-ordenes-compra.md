# Especificación: 02 - Módulo de referencia Modulo04_OrdenesCompra (backend)

## 1. Objetivo
**Problema:** la base técnica (spec 01) no tiene ningún módulo de negocio. El enunciado (`docs/enunciado.md` §3) exige un módulo completo de ejemplo con un caso de uso de escritura, los 12 patrones LINQ numerados y un controlador delgado.
**Resultado esperado:** `Modules/Modulo04_OrdenesCompra/` implementa exactamente `docs/enunciado.md` §3.3–§3.5 sobre la base de la spec 01. Hay un test de integración por endpoint y datos de demostración para QA y frontend. Es la plantilla para los otros 79 módulos.

## 2. Fuera de alcance
- Otros módulos (Clientes, Productos…), aunque usen las mismas entidades.
- Cambiar las entidades, el repositorio genérico, los middlewares o `Program.cs` (spec 01). Si algo de la base impide cumplir esta spec, se reporta BLOQUEADO; no se toca.
- Pantallas del frontend (spec 04).
- Validaciones de negocio que el enunciado no pide (por ejemplo, comprobar que el cliente existe antes de crear la orden).

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Modules/Modulo04_OrdenesCompra/Dtos/*.cs` | crear | un archivo por DTO: `ProductoResumenDto`, `DetalleOrdenItemDto`, `OrdenCompraJerarquicaDto`, `ClienteFrecuenciaDto`, `CrearOrdenCompraRequestDto` (enunciado §3.3) |
| `src/TitanGlobal.Api/Modules/Modulo04_OrdenesCompra/UseCases/CrearOrdenCompraUseCase.cs` | crear | enunciado §3.4 |
| `src/TitanGlobal.Api/Modules/Modulo04_OrdenesCompra/UseCases/AnaliticaOrdenesCompraUseCase.cs` | crear | 12 patrones, cada método con su comentario `// PATRÓN LINQ N:` |
| `src/TitanGlobal.Api/Modules/Modulo04_OrdenesCompra/Controllers/OrdenesCompraController.cs` | crear | `[Route("api/v1/ordenes-compra")]`, 10 acciones de enunciado §3.5 |
| `scripts/seed-dev.sql` | crear | datos de demostración para la BD local: ≥5 clientes (con y sin `Descripcion`), ≥10 productos en ≥3 categorías (algunos sin `Descripcion`), ≥8 órdenes con detalles y fechas repartidas. Idempotente (`TRUNCATE … RESTART IDENTITY CASCADE` al inicio) |
| `tests/TitanGlobal.Api.Tests/Modulo04_OrdenesCompra/*.cs` | crear | tests de integración con BD InMemory sembrada |

Namespace = carpeta: `TitanGlobal.Api.Modules.Modulo04_OrdenesCompra.{Dtos,UseCases,Controllers}`.

## 4. Diseño y lógica
- **Código:** el de `docs/enunciado.md` §3, adaptado solo en el namespace (desviación decidida en `docs/arquitectura.md`) y en las reglas de `CLAUDE.md`: sin `var`, sin `=>` en miembros, una sentencia por línea.
- **UseCases** sin interfaz; los registra la reflexión de la spec 01, nunca a mano. Todas las consultas parten de `_unitOfWork.Repository<T>().AsQueryable()` y terminan en un operador asíncrono de EF Core.
- **Controlador** delgado: solo llama al UseCase y devuelve `Ok(...)`; campos `private readonly _camelCase`.
- **Patrón 7:** si el cliente no tiene órdenes, devuelve `0` sin llamar a `AverageAsync`.
- **Patrón 5:** sin productos, responde 200 con cuerpo vacío o `null` (lo que haga `Ok(null)`; documentarlo en el test).
- **Contrato JSON:** el de ASP.NET por defecto (camelCase). El frontend lo consumirá así.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Gate | `./scripts/verify.sh` | en verde |
| A3 | Tests por endpoint | un test por cada una de las 10 rutas de enunciado §3.5 con datos InMemory conocidos; se comprueban los valores exactos (sumas, promedios, orden del ranking, `Distinct`, jerarquía padre-hijos) | 10+ tests nuevos en verde |
| A4 | Casos límite | promedio de un cliente sin órdenes → `0`; filtro de texto con mayúsculas (`"LAPTOP"` encuentra `"Laptop"`); POST con cuerpo vacío → 400; POST con `X-User-Role: Bloqueado` y cuerpo válido → 403 | pasan |
| A5 | 12 patrones | `grep -c "PATRÓN LINQ" AnaliticaOrdenesCompraUseCase.cs` y lectura: cada patrón 1–12 aparece numerado | 12 patrones cubiertos |
| A6 | PostgreSQL real | `docker compose up -d db`, `dotnet ef database update`, `psql … -f scripts/seed-dev.sql`, API en marcha y `curl` a las 10 rutas → 200 con datos coherentes (en especial, patrones 9–12, que son los que Npgsql traduce distinto de InMemory) | sin errores de traducción LINQ |
| A7 | OpenAPI | `/openapi/v1.json` incluye las 10 rutas de `api/v1/ordenes-compra` | presentes |

## 6. Checklist de ejecución
- [ ] Tarea 1: DTOs.
- [ ] Tarea 2: `CrearOrdenCompraUseCase` y `AnaliticaOrdenesCompraUseCase`.
- [ ] Tarea 3: controlador.
- [ ] Tarea 4: `scripts/seed-dev.sql` y tests.
- [ ] Tarea 5: A1–A7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
