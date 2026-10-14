# Especificación: 04 - Pantallas de Modulo04_OrdenesCompra (frontend)

## 1. Objetivo
**Problema:** el backend del módulo de referencia (spec 02) expone 10 endpoints en `api/v1/ordenes-compra`, pero en el frontend `/modulos/04-ordenes-compra` solo muestra «Pantalla pendiente». Además quedan cuatro detalles menores de la spec 03.
**Resultado esperado:**
- `/modulos/04-ordenes-compra` permite crear una orden y consultar las 10 analíticas. Cada bloque está rotulado con su patrón LINQ y tiene estados de carga, vacío y error.
- Las pantallas se enganchan mediante un registro, para que los otros 79 módulos se añadan igual.

## 2. Fuera de alcance
- Cambios en la API o en `src/`, `tests/` o `scripts/`. Solo se toca `frontend/` y, si hace falta, su `README.md`.
- Pantallas de otros módulos.
- Generación automática del cliente desde el OpenAPI (más adelante, si el humano lo pide).
- Librerías de componentes, de tablas o de formularios. Se usa React y el CSS existente.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/modules/pantallas.ts` | crear | registro `slug → componente`; `/modulos/:slug` muestra la pantalla registrada o, si no hay, `ModuloPendiente` |
| `frontend/src/App.tsx` | modificar | usa el registro |
| `frontend/src/modules/ordenes-compra/tipos.ts` | crear | tipos TS de los DTOs en camelCase (`ProductoResumen`, `DetalleOrdenItem`, `OrdenCompraJerarquica`, `ClienteFrecuencia`, `CrearOrdenCompraRequest`) y de las respuestas `{ id }`, `{ totalVentas }`, `{ clienteId, promedioCompra }` |
| `frontend/src/modules/ordenes-compra/api.ts` | crear | una función por endpoint (10), sobre `apiGet`/`apiPost`; parámetros de consulta con `URLSearchParams` |
| `frontend/src/modules/ordenes-compra/OrdenesCompraPage.tsx` + componentes en la misma carpeta | crear | ver §4 |
| `frontend/src/modules/ordenes-compra/*.test.tsx` | crear | tests con `fetch` simulado |
| `frontend/src/api/client.ts` | modificar | respuesta 5xx con cuerpo no JSON → mensaje «La API no está disponible o no respondió correctamente.» (el proxy de Vite devuelve 500 sin JSON cuando la API está apagada); `apiPost` también para `PUT` si se necesita, nada más |
| `frontend/src/layout/AppLayout.tsx` | modificar | Escape cierra el menú móvil y devuelve el foco al botón; el selector de rol sin `as`: validar con un type guard (`isUserRole`) exportado desde `roleStore.ts` |
| `frontend/src/api/roleStore.ts` | modificar | exportar `isUserRole` |
| `frontend/package.json` | modificar | `dev`: `vite --host 127.0.0.1 --port 5173` (no `0.0.0.0`) |
| `frontend/src/styles/*.css` | modificar | estilos de formularios, tablas y tarjetas de resultado, con las variables existentes |

## 4. Diseño y lógica
- **Página:** cabecera «04 · OrdenesCompra» y dos secciones.
  - **«Nueva orden»:** formulario con NumeroOrden (obligatorio), ClienteId (entero > 0), Total (decimal ≥ 0) y Descripcion (opcional).
    - Validación en cliente con mensajes junto a cada campo.
    - Al enviar: botón deshabilitado durante la petición; éxito → «Orden creada con Id N» y el formulario se limpia; error → `Message` de la API.
  - **«Analítica»:** una tarjeta por consulta, con el título y la etiqueta del patrón («Patrones 1–3», «Patrón 9»…).
    - Cada tarjeta tiene sus propios parámetros (término y precio mínimo; fecha de inicio; clienteId; productoId) y un botón «Consultar».
    - Las que no tienen parámetros (5, 8, 9) cargan al abrir la página.
- **Estados por tarjeta:** cargando (texto accesible con `aria-live="polite"`), vacío («Sin resultados»), error (mensaje de la API) y resultado.
  - Tablas para las listas de productos y el ranking.
  - Lista anidada para la jerarquía (orden → detalles con subtotal = cantidad × precio).
  - Chips para las categorías y los nombres de clientes.
  - Importes con `Intl.NumberFormat('es-PE', { style: 'currency', currency: 'PEN' })` y fechas con `toLocaleDateString('es-PE')`.
- **Robustez:** la respuesta de «producto más caro» puede venir vacía (`Ok(null)` → cuerpo vacío o `null`). Tratarla como «Sin resultados» sin romper, lo que obliga a que `client.ts` tolere un 200 con cuerpo vacío.
- **Accesibilidad:** cada campo tiene `<label>`, los errores van asociados con `aria-describedby` y las tablas tienen `<caption>` o encabezados de tabla.
- **Responsive:** a 375 px, tarjetas en una columna y tablas con scroll horizontal propio (no de la página).

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `./scripts/verify.sh` (incluye `npm ci` + `npm run verify` del frontend) | en verde, 0 warnings |
| A2 | API del módulo | test: cada una de las 10 funciones llama a la URL y el método exactos (`/api/v1/ordenes-compra/...` con sus parámetros) y devuelve el JSON tipado | 10 casos pasan |
| A3 | Formulario | tests: validación (NumeroOrden vacío, ClienteId 0, Total negativo → mensajes y sin petición); éxito → «Orden creada con Id 7» y formulario limpio; 400/403 → `Message` de la API visible | pasan |
| A4 | Analítica | tests: patrones 5, 8 y 9 cargan al abrir; jerarquía muestra órdenes con sus detalles y subtotales; «más caro» con cuerpo vacío → «Sin resultados»; un error 500 en una tarjeta no rompe las demás | pasan |
| A5 | Pendientes de la spec 03 | tests: Escape cierra el menú y devuelve el foco; el selector ignora valores no válidos sin `as`; 5xx sin JSON → mensaje nuevo; `grep -n " as " frontend/src --include=*.tsx` sin casts en `AppLayout` | pasan |
| A6 | Registro | test: `/modulos/04-ordenes-compra` muestra la pantalla nueva; `/modulos/01-clientes` sigue en «Pantalla pendiente» | pasa |
| A7 | Visual | capturas con Playwright y respuestas simuladas (`page.route`, datos realistas como los de `scripts/seed-dev.sql`) del módulo 04 a 375 y 1280 px, en claro y oscuro, con la jerarquía desplegada; esperar a que terminen las transiciones; sin scroll horizontal de página | adjuntas |

## 6. Checklist de ejecución
- [x] Tarea 1: registro de pantallas, tipos y `api.ts`.
- [x] Tarea 2: formulario de nueva orden.
- [x] Tarea 3: tarjetas de analítica.
- [x] Tarea 4: pendientes de la spec 03.
- [x] Tarea 5: tests, capturas y A1–A7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-14 | A1 | ✅ | `./scripts/verify.sh` rc=0 en `ImLukzy/Claudio` (`d86d677`): backend 23/23, frontend 36/36, lint 0 warnings, build |
| 2026-10-14 | A2 | ✅ | `api.test.ts`: 10 funciones con URL, método y parámetros exactos |
| 2026-10-14 | A3 | ✅ | `NuevaOrdenForm.test.tsx`: validación sin petición, éxito «Orden creada con Id 7» y limpieza; 400/403 y (corrección 1) 409 «Ya existe una orden…» y 400 «El cliente 9999 no existe.» visibles, sin limpiar el formulario |
| 2026-10-14 | A4 | ✅ | `Analitica.test.tsx`: carga de los patrones 5, 8 y 9, jerarquía con subtotales, «más caro» vacío → «Sin resultados», un 500 aislado por tarjeta |
| 2026-10-14 | A5 | ✅ | Escape cierra el menú y devuelve el foco; `isUserRole` sin `as`; 5xx sin JSON → mensaje nuevo; dev en 127.0.0.1 |
| 2026-10-14 | A6 | ✅ | `App.test.tsx`: `/modulos/04-ordenes-compra` con la pantalla nueva y `/modulos/01-clientes` pendiente |
| 2026-10-14 | A7 | ✅ | capturas de Dwight en `/tmp/titan-spec04/` (375/1280 × claro/oscuro, respuestas simuladas), revisadas por Claudio |
| 2026-10-14 | Corrección 1 | ✅ | Claudio vio en las capturas que OC-001 (`2026-01-02T00:00:00Z`) salía como 1/1/2026 (hora de Lima); `lib/formato.ts` formatea en UTC; test bajo `TZ=America/Lima` → 2/1/2026 |
| 2026-10-14 | Auditoría | ✅ | Kelly, ciclo 1: APROBADO, sin hallazgos (sobre `d41b8e2`; la corrección 1 la revisó Claudio) |
| 2026-10-14 | QA E2E | ⏳ | pendiente: Pam con frontend + API + PostgreSQL cuando estén integradas las specs 05 y 06 |
