# Módulo 22 – Categorías de Producto (Frontend)

## 1. Objetivo
Implementar la interfaz React del Módulo 22 (`22-categorias-producto`) para administrar el catálogo de categorías de producto (listado, búsqueda con debounce, filtro de inactivas, detalle, alta, edición y baja lógica) conectada a `/api/v1/categorias-producto`.

## 2. Fuera de alcance
- Modificaciones al backend de `Modulo22_CategoriasProducto` (ya integrado en `main`).
- Relación por clave foránea con `Producto`.

## 3. Archivos afectados
- `frontend/src/modules/categorias-producto/tipos.ts`
- `frontend/src/modules/categorias-producto/api.ts`
- `frontend/src/modules/categorias-producto/CategoriasProductoPage.tsx`
- `frontend/src/modules/categorias-producto/api.test.ts`
- `frontend/src/modules/categorias-producto/CategoriasProductoPage.test.tsx`
- `frontend/src/modules/pantallas.ts`
- `frontend/src/styles/app.css`

## 4. Diseño y lógica
- Tarjetas de resumen con `totalCategorias`, `categoriasActivas` y `categoriasInactivas`.
- Filtro por nombre con debounce de 300 ms y casilla `incluirInactivas`.
- Tabla paginada accesible (`TablaPaginada`), ficha lateral (`PanelDetalle`), formulario con validación local (`CampoConError`) y baja lógica accesible (`DialogoBaja`).

## 5. Criterios de aceptación
- A1: `npm --prefix frontend run verify` y `./scripts/verify.sh` en verde.
- A2: Llamadas HTTP alineadas con `/api/v1/categorias-producto`.
- A3: Debounce de 300 ms sin consultas intermedias.
- A4: Validación local y conservación de inputs ante error 409.
- A5: Diálogo de baja accesible con cierre por `Escape` y devolución de foco.
- A6: Responsive en 375 px y 1280 px en claro y oscuro.

## 6. Estado
- [x] Spec aprobada.
- [x] Implementación y pruebas completadas.

## 7. Evidencia
- Verificación ejecutada con `npm --prefix frontend run verify`.
