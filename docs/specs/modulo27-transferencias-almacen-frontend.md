# Módulo 27 – Transferencias entre Almacenes (Frontend)

## 1. Objetivo
Implementar la interfaz React del Módulo 27 (`27-transferencias-almacen`) para consultar y registrar traslados entre almacenes con trazabilidad de movimientos de kárdex conectada a `/api/v1/transferencias-almacen`.

## 2. Fuera de alcance
- Modificaciones al backend de `Modulo27_TransferenciasAlmacen`.

## 3. Archivos afectados
- `frontend/src/modules/transferencias-almacen/tipos.ts`
- `frontend/src/modules/transferencias-almacen/api.ts`
- `frontend/src/modules/transferencias-almacen/validacion.ts`
- `frontend/src/modules/transferencias-almacen/TransferenciasAlmacenPage.tsx`
- `frontend/src/modules/transferencias-almacen/api.test.ts`
- `frontend/src/modules/transferencias-almacen/TransferenciasAlmacenPage.test.tsx`
- `frontend/src/modules/pantallas.ts`
- `frontend/src/styles/app.css`

## 4. Diseño y lógica
- Tarjetas de resumen con `Total de transferencias`, `Unidades transferidas` y `Valor transferido`.
- Listado paginado con filtro de fechas y panel lateral con detalle y IDs de movimientos de salida y entrada en kárdex.

## 5. Criterios de aceptación
- A1: `lint`, `typecheck`, `test` y `build` del frontend en verde.
- A2: Llamadas HTTP alineadas con `/api/v1/transferencias-almacen`.
- A3: Validación local de almacenes distintos y conservación de datos ante error 409.
- A4: Responsive en 375 px y 1280 px en claro y oscuro.

## 6. Estado
- [x] Spec aprobada.
- [x] Implementación y pruebas completadas.

## 7. Evidencia
- Verificado con Vitest, ESLint, TypeScript y Vite build.
