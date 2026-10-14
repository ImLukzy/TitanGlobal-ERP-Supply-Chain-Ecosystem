# Especificación: 03 - Base del frontend (React + Vite + TypeScript)

## 1. Objetivo
**Problema:** el proyecto requiere frontend (decisión del humano, 2026-10-14) y todavía no existe.
**Resultado esperado:** `frontend/` contiene una SPA React + Vite + TypeScript con el armazón del ERP:
- navegación por los 8 dominios y los 80 módulos;
- un cliente HTTP que habla con la API `api/v1` y muestra sus errores `{ StatusCode, Message }`;
- un selector de rol que envía `X-User-Role`;
- una portada con el estado de la API;
- un gate propio (lint, typecheck, test, build).

## 2. Fuera de alcance
- Pantallas de negocio de cualquier módulo. Las de `Modulo04_OrdenesCompra` van en la spec 04; aquí cada módulo muestra una página «Pendiente».
- Cualquier archivo fuera de `frontend/`. En concreto, **no** se tocan `src/`, `tests/`, `scripts/` ni la raíz. El integrador conecta `scripts/verify.sh` al gate del frontend al integrar.
- Autenticación real o login.
- Librerías de componentes pesadas (MUI, Ant, Bootstrap…). Se usa CSS propio con variables.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/package.json`, `package-lock.json` | crear | scripts `dev`, `build`, `lint`, `typecheck`, `test`, `verify` (= lint + typecheck + test + build). Versiones estables actuales (Vite 8, React 19, React Router 8, Vitest 5, TypeScript) |
| `frontend/vite.config.ts` | crear | puerto 5173; proxy `/api` → `http://localhost:5080` (puerto de la API en desarrollo) |
| `frontend/tsconfig*.json` | crear | `strict: true` |
| `frontend/eslint.config.js` | crear | reglas recomendadas de TS y react-hooks; 0 warnings (`--max-warnings 0`) |
| `frontend/index.html`, `frontend/src/main.tsx`, `frontend/src/App.tsx` | crear | router con `/` (portada) y `/modulos/:slug` |
| `frontend/src/modules/catalogo.ts` | crear | los 80 módulos: `{ numero, nombre, slug, dominio }`, con la numeración y los nombres exactos de `docs/arquitectura.md`; `slug` = `NN-kebab` (por ejemplo, `04-ordenes-compra`); los 8 dominios con su nombre |
| `frontend/src/api/client.ts` | crear | `apiGet<T>(ruta)` / `apiPost<T>(ruta, cuerpo)` sobre `fetch`, base `/api/v1`; añade `X-User-Role` si hay rol elegido; ante una respuesta no 2xx lanza `ApiError { statusCode, message }` leyendo `StatusCode`/`Message` del cuerpo, con un mensaje genérico si el cuerpo no es JSON |
| `frontend/src/layout/*` | crear | cabecera (nombre del ERP, selector de rol: «Ninguno», «Administrador», «Bloqueado», «SinPermiso»; se recuerda en `localStorage` con try/catch) + barra lateral con los dominios plegables y sus módulos |
| `frontend/src/pages/Inicio.tsx` | crear | estado de la API (`GET /sistema/salud` → «API en línea» / «API no disponible» con el mensaje de error) y resumen de los 8 dominios |
| `frontend/src/pages/ModuloPendiente.tsx` | crear | título «NN · Nombre», dominio y texto «Pantalla pendiente»; slug inexistente → «Módulo no encontrado» |
| `frontend/src/styles/*.css` | crear | variables CSS (colores, espaciado, tipografía), modo claro y oscuro (`prefers-color-scheme`); diseño usable a 375 px (barra lateral como menú desplegable) y a 1280 px |
| `frontend/src/**/*.test.ts(x)` | crear | Vitest + Testing Library (entorno jsdom) |
| `frontend/README.md` | crear | cómo arrancar (API en 5080 + `npm run dev`) y el gate |

## 4. Diseño y lógica
- **Catálogo:** es la única fuente de los 80 módulos en el frontend. La barra lateral, las rutas y la portada se generan desde él.
- **Cliente HTTP:**
  - Es el único sitio que llama a `fetch`.
  - El rol elegido lo lee de un módulo de estado simple (contexto de React), no de variables globales.
  - Errores: 400, 403 y 500 llegan con el `Message` de la API.
  - Si hay un fallo de red (API apagada), lanza `ApiError` con `statusCode: 0`.
- **Accesibilidad:**
  - Navegación con teclado.
  - `aria-expanded` en los dominios plegables.
  - Foco visible.
  - Contraste AA.
- **Sin dependencias extra** más allá de React, React Router, Vite y las herramientas de test y lint.

## 5. Criterios de aceptación (medibles)
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Gate | `npm --prefix frontend ci && npm --prefix frontend run verify` | en verde, 0 warnings de ESLint |
| A2 | Catálogo | test: 80 módulos, números 1–80 únicos y consecutivos, 8 dominios con 10 módulos cada uno, `04-ordenes-compra` es «OrdenesCompra» del dominio Comercial | pasa |
| A3 | Cliente HTTP | tests con `fetch` simulado: envía `X-User-Role` solo si hay rol; 400/403/500 → `ApiError` con el `Message` de la API; cuerpo no JSON → mensaje genérico; fallo de red → `statusCode 0` | pasan |
| A4 | Navegación | test: la barra lateral muestra los 8 dominios; al desplegar uno aparecen sus 10 módulos; `/modulos/04-ordenes-compra` muestra «04 · OrdenesCompra»; slug inválido → «Módulo no encontrado» | pasan |
| A5 | Portada | test con `fetch` simulado: salud OK → «API en línea»; error → «API no disponible» | pasan |
| A6 | Visual | capturas a 375 px y 1280 px (portada y una página de módulo) en claro y oscuro, sin scroll horizontal | adjuntas en el reporte |

## 6. Checklist de ejecución
- [x] Tarea 1: proyecto Vite + TS + ESLint + Vitest y scripts.
- [x] Tarea 2: catálogo, router y layout.
- [x] Tarea 3: cliente HTTP, selector de rol y portada.
- [x] Tarea 4: tests y capturas.
- [x] Tarea 5: A1–A6.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| 2026-10-14 | A1 | ✅ | `npm --prefix frontend ci && npm --prefix frontend run verify`: lint con 0 warnings, typecheck, 13/13 tests, build (268 kB, 85 kB gzip). Desde ahora lo ejecuta `scripts/verify.sh` (rc=0 en `ImLukzy/Claudio`) |
| 2026-10-14 | A2 | ✅ | `catalogo.test.ts`; además, un script de Claudio comparó los 80 nombres con `docs/arquitectura.md` y no encontró diferencias |
| 2026-10-14 | A3 | ✅ | `client.test.ts`: rol, POST, 400/403/500 con `Message`, cuerpo no JSON y fallo de red → `statusCode 0` |
| 2026-10-14 | A4 | ✅ | `App.test.tsx` y QA (Pam): 8 dominios × 10 módulos, `/modulos/04-ordenes-compra`, slug inválido → «Módulo no encontrado», teclado y `aria-expanded` |
| 2026-10-14 | A5 | ✅ | test y QA (Pam) con la API real por el proxy: «API en línea» / «API no disponible»; rol «Bloqueado» → 403 con el mensaje literal; `X-User-Role` solo con rol y se recuerda tras recargar |
| 2026-10-14 | A6 | ✅ | capturas de Pam en `/tmp/titan-qa03-capturas/` (portada y módulo 04 × 375/1280 × claro/oscuro + menú móvil), sin scroll horizontal. La del menú móvil salió a mitad de la animación; Claudio la repitió esperando 600 ms → panel de 310 px y capa a toda la altura |
| 2026-10-14 | Auditoría | ✅ | Kelly, ciclo 1: APROBADO, 0 bloqueantes; menor: un `as` de tipo en el selector de rol. Pendientes menores para la spec 04: Escape no cierra el menú móvil, `--host 0.0.0.0` en `npm run dev`, mensaje genérico con la API apagada |
