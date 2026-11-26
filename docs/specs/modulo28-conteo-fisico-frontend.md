# Módulo 28 – Conteo Físico / Toma de Inventario (Frontend)

## 1. Objetivo
Implementar la interfaz React del Módulo 28 (`28-conteo-fisico`) para gestionar tomas de inventario físico, comparar stock del sistema frente a conteo real y cerrar o anular tomas conectada a `/api/v1/tomas-inventario`.

## 2. Fuera de alcance
- Modificaciones al backend de `Modulo28_TomaInventario`.

## 3. Archivos afectados
- `frontend/src/modules/conteo-fisico/tipos.ts`
- `frontend/src/modules/conteo-fisico/api.ts`
- `frontend/src/modules/conteo-fisico/validacion.ts`
- `frontend/src/modules/conteo-fisico/ConteoFisicoPage.tsx`
- `frontend/src/modules/conteo-fisico/api.test.ts`
- `frontend/src/modules/conteo-fisico/ConteoFisicoPage.test.tsx`
- `frontend/src/modules/pantallas.ts`
- `frontend/src/styles/app.css`

## 4. Diseño y lógica
- Tarjetas de resumen con `Total de tomas`, `Abiertas`, `Cerradas`, `Anuladas` y `Líneas con diferencia`.
- Listado con búsqueda con debounce de 300 ms, filtro por estado y panel de detalle con comparativo de diferencias y acciones de edición, cierre y anulación.

## 5. Criterios de aceptación
- A1: `lint`, `typecheck`, `test` y `build` del frontend en verde.
- A2: Llamadas HTTP alineadas con `/api/v1/tomas-inventario`.
- A3: Validación local y conservación de datos ante error 409.
- A4: Responsive en 375 px y 1280 px en claro y oscuro.

## 6. Estado
- [x] Spec aprobada.
- [x] Implementación y pruebas completadas.

## 7. Evidencia
- Verificado con Vitest, ESLint, TypeScript y Vite build.
