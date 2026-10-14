# Especificación: 06 - Validación al crear una orden (409 / 400)

## 1. Objetivo
**Problema:** `POST /api/v1/ordenes-compra` con un `NumeroOrden` que ya existe, o con un `ClienteId` inexistente, responde 500 «Ocurrió un error interno en el servidor.» (QA de la spec 02). El usuario no sabe qué ha hecho mal y el error parece un fallo del sistema.
**Resultado esperado:** decisión del humano del 2026-10-14:
- `NumeroOrden` repetido → **409** `{ StatusCode: 409, Message }`.
- `ClienteId` inexistente → **400** `{ StatusCode: 400, Message }`.
- Datos inválidos (número vacío, cliente ≤ 0, total negativo) → 400.

El mecanismo sirve igual para los 79 módulos restantes.

## 2. Fuera de alcance
- Otros endpoints, módulos o validaciones no listadas.
- Cambiar el orden del pipeline o los mensajes literales existentes (400 de cuerpo vacío, 403, 500).
- El frontend: el formulario ya muestra el `Message` de la API.
- `scripts/smoke-postgres.sh` (spec 05); se amplía después si hace falta.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna (tomadas por el humano).

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `src/TitanGlobal.Api/Middlewares/ReglaNegocioException.cs` | crear | `public class ReglaNegocioException : Exception` con `int StatusCode` (400, 404 o 409) y el mensaje. Es un tipo transversal que reutilizan todos los módulos |
| `src/TitanGlobal.Api/Middlewares/ErrorHandlingMiddleware.cs` | modificar | captura `ReglaNegocioException` **antes** que `Exception` → responde su `StatusCode` con `{ StatusCode, Message }` (sin `Detail`), `application/json` y las mismas opciones JSON; registra un `LogWarning` (no `LogError`). El resto sigue igual |
| `src/TitanGlobal.Api/Modules/Modulo04_OrdenesCompra/UseCases/CrearOrdenCompraUseCase.cs` | modificar | validaciones de §4 antes de guardar |
| `tests/TitanGlobal.Api.Tests/Modulo04_OrdenesCompra/CrearOrdenCompraValidacionTests.cs` | crear | archivo nuevo (no tocar `OrdenesCompraApiTests.cs`) |

## 4. Diseño y lógica
- **Orden de las validaciones** en `CrearOrdenCompraUseCase.ExecuteAsync`. Se detiene en la primera que falla:
  1. `NumeroOrden` vacío o en blanco → 400 «El número de orden es obligatorio.»
  2. `ClienteId <= 0` → 400 «El cliente es obligatorio.»
  3. `Total < 0` → 400 «El total no puede ser negativo.»
  4. Cliente inexistente (`Repository<Cliente>().AsQueryable().AnyAsync(...)`) → 400 «El cliente {ClienteId} no existe.»
  5. `NumeroOrden` ya registrado (`AnyAsync` sobre `OrdenCompra`, comparando el número tras `Trim()`) → 409 «Ya existe una orden de compra con el número {NumeroOrden}.»
- **Carrera entre dos peticiones simultáneas:**
  - Si `CompleteAsync` lanza `DbUpdateException` cuya causa interna es una `PostgresException` con `SqlState == "23505"` (índice único) → 409 con el mismo mensaje.
  - Si es `"23503"` (clave foránea) → 400 con el mensaje del cliente inexistente.
  - Cualquier otra excepción se propaga (500).
- El `NumeroOrden` se guarda sin espacios al principio ni al final (`Trim()`).
- **Controlador:** sin cambios. Sigue delgado; la traducción a HTTP la hace el middleware.
- **Reglas de `CLAUDE.md`:** sin `var`, sin `=>` en miembros y una sentencia por línea.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` | en verde |
| A2 | Validaciones | tests: número vacío o en blanco, cliente 0, total −1, cliente 999 → 400 con el mensaje literal; número repetido (`OC-001` del seed de test) y repetido con espacios (`" OC-001 "`) → 409 con el mensaje literal; cuerpos JSON `{ StatusCode, Message }` sin `Detail`; ninguno de esos casos crea filas | pasan |
| A3 | Sin regresiones | un POST válido sigue dando 200 `{ id }`; POST con cuerpo vacío → 400 del middleware; una excepción no controlada → 500 con `Detail` | pasan |
| A4 | PostgreSQL | a mano con la BD local + seed: `OC-001` → 409; cliente 9999 → 400; POST válido → 200 | evidencia `curl` en el reporte |

## 6. Checklist de ejecución
- [ ] Tarea 1: `ReglaNegocioException` y middleware.
- [ ] Tarea 2: validaciones en el UseCase.
- [ ] Tarea 3: tests y A1–A4.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
