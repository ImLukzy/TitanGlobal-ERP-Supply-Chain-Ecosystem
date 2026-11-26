# Módulo 25 – Lotes y Series (Frontend)

## 1. Objetivo
Implementar la interfaz React del Módulo 25 (`25-lotes-series`) para administrar trazabilidad por lotes con vencimiento y unidades serializadas conectada a `/api/v1/lotes-series`.

## 2. Fuera de alcance
- Modificaciones al backend de `Modulo25_LotesSeries`.

## 3. Archivos afectados
- `frontend/src/modules/lotes-series/tipos.ts`
- `frontend/src/modules/lotes-series/api.ts`
- `frontend/src/modules/lotes-series/validacion.ts`
- `frontend/src/modules/lotes-series/LotesSeriesPage.tsx`
- `frontend/src/modules/lotes-series/api.test.ts`
- `frontend/src/modules/lotes-series/LotesSeriesPage.test.tsx`
- `frontend/src/modules/pantallas.ts`
- `frontend/src/styles/app.css`

## 4. Diseño y lógica
- Tarjetas de resumen con totales por estado (`Disponibles`, `En cuarentena`, `Bloqueados`, `Por vencer (30 días)`, `Vencidos`).
- Pestañas `Lotes` y `Números de serie` con búsqueda con debounce de 300 ms, paginación, detalle lateral, formularios validados y transiciones de estado en series.

## 5. Criterios de aceptación
- A1: `lint`, `typecheck`, `test` y `build` del frontend en verde.
- A2: Llamadas HTTP alineadas con `/api/v1/lotes-series/lotes` y `/api/v1/lotes-series/series`.
- A3: Validación local y conservación de datos ante error 409.
- A4: Responsive en 375 px y 1280 px en modo claro y oscuro.

## 6. Estado
- [x] Spec aprobada.
- [x] Implementación y pruebas completadas.

## 7. Evidencia
- Verificado con Vitest, ESLint, TypeScript y Vite build.
