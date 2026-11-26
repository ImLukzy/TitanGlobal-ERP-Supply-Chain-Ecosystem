# Módulo 26 – Kárdex Valorizado (Frontend)

## 1. Objetivo
Implementar la interfaz React del Módulo 26 (`26-kardex-valorizado`) para registrar movimientos de inventario, consultar saldos por almacén y calcular kárdex valorizado (Promedio Ponderado y PEPS) conectada a `/api/v1/kardex-valorizado`.

## 2. Fuera de alcance
- Modificaciones al backend de `Modulo26_KardexValorizado`.

## 3. Archivos afectados
- `frontend/src/modules/kardex-valorizado/tipos.ts`
- `frontend/src/modules/kardex-valorizado/api.ts`
- `frontend/src/modules/kardex-valorizado/validacion.ts`
- `frontend/src/modules/kardex-valorizado/KardexValorizadoPage.tsx`
- `frontend/src/modules/kardex-valorizado/api.test.ts`
- `frontend/src/modules/kardex-valorizado/KardexValorizadoPage.test.tsx`
- `frontend/src/modules/pantallas.ts`
- `frontend/src/styles/app.css`

## 4. Diseño y lógica
- Tarjetas de resumen con totales de movimientos y unidades ingresadas/retiradas.
- Vistas en pestañas para `Movimientos`, `Kárdex valorizado (PEPS / Promedio)` y `Saldos consolidados`.

## 5. Criterios de aceptación
- A1: `lint`, `typecheck`, `test` y `build` del frontend en verde.
- A2: Llamadas HTTP alineadas con `/api/v1/kardex-valorizado`.
- A3: Validación local de rango de fechas y movimientos sin perder datos ante 409.
- A4: Responsive en 375 px y 1280 px en claro y oscuro.

## 6. Estado
- [x] Spec aprobada.
- [x] Implementación y pruebas completadas.

## 7. Evidencia
- Verificado con Vitest, ESLint, TypeScript y Vite build.
