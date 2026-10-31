# Relevo: dónde se quedó el trabajo (2026-10-31)

Lukas pausó el desarrollo de su parte el 2026-10-15. Este documento dice exactamente qué está hecho, qué quedó a medias, dónde está cada cosa y cómo seguir. Para el resto del proyecto: [`contexto-proyecto.md`](../contexto-proyecto.md), [`guia-companeros.md`](../guia-companeros.md) y [`asignaciones.md`](asignaciones.md).

## 1. Estado en una línea
`main` es la rama de integración y la desplegada (https://titanglobal-erp.vercel.app). Lukas tiene **sus 13 módulos terminados** y hace la integración. Las cinco pantallas que estaban aparcadas en ramas `pendiente/…` (08, 09, 10, 77 y 78, de Maxi) ya están **integradas en `main` y desplegadas**; solo les falta el QA visual. Quedan por empezar las pantallas 72 RolesPermisos (Sergio) y 71 Usuarios (Alexander). El resto de módulos está por empezar.

## 2. Pantallas pendientes y responsables
**Terminados de Lukas (backend + pantalla + QA + producción):** 01 Clientes, 02 SegmentacionClientes, 03 Cotizaciones, 04 OrdenesCompra, 05 ContratosComerciales, 06 ListasPrecios, 07 PromocionesDescuentos, 11 Proveedores, 13 SolicitudesCompra, 15 OrdenesAbastecimiento, 21 Productos, 23 Almacenes, 24 ZonasAlmacenaje.

**Pendientes (el backend ya está en `main` y en producción):**

| Módulo | Responsable | Spec de la pantalla | Estado | Siguiente paso |
|---|---|---|---|---|
| 72 RolesPermisos | Sergio | [42](../specs/42-modulo72-roles-permisos-frontend.md) | por empezar | implementar la spec 42 |
| 71 Usuarios | Alexander | [44](../specs/44-modulo71-usuarios-frontend.md) | por empezar | implementar la spec 44 (usa los roles del 72) |

## 3. Pantallas integradas el 2026-10-31

Las cinco ramas `pendiente/…` se rebasaron sobre `main`, se resolvieron los conflictos de `frontend/src/modules/pantallas.ts` y `frontend/src/styles/app.css`, pasaron el gate y se integraron por pull request. Después se borraron las ramas remotas: el contenido vive en `main`.

| Módulo | Responsable | Spec | PR | QA visual |
|---|---|---|---|---|
| 08 ComisionesVendedores | Maxi | [40](../specs/40-modulo08-comisiones-vendedores-frontend.md) | #3 | pendiente |
| 09 ReclamosPostventa | Maxi | [34](../specs/34-modulo09-reclamos-postventa-frontend.md) | #4 | pendiente |
| 10 DevolucionesComerciales | Maxi | [36](../specs/36-modulo10-devoluciones-comerciales-frontend.md) | #5 | pendiente |
| 77 AuditoriaTransaccional | Maxi | [46](../specs/46-modulo77-auditoria-transaccional-frontend.md) | #6 | pendiente (incluye la cabecera `X-Usuario` en `api/client.ts`) |
| 78 NotificacionesAlertas | Maxi | [38](../specs/38-modulo78-notificaciones-alertas-frontend.md) | #7 | pendiente (incluye el indicador de alertas en la barra superior) |

## 4. QA visual pendiente de las cinco pantallas

Recorrer en la demo, o en local contra PostgreSQL, cada criterio de la spec a 375 y 1280 px, en claro y en oscuro, con la lista de [`lecciones.md`](lecciones.md):

1. **08 ComisionesVendedores:** pestañas Vendedores · Asignación · Comisiones del mes; alta de vendedor y asignación de una orden.
2. **09 ReclamosPostventa:** filtros, resumen por estado y tipo, detalle y transiciones Abierto → EnProceso → Resuelto → Cerrado / Rechazado.
3. **10 DevolucionesComerciales:** devolución parcial y total, tope de lo comprado menos lo ya devuelto, mensajes de error del backend en el diálogo.
4. **77 AuditoriaTransaccional:** resumen, actividad de la semana, filtros, detalle de cambios y que la cabecera `X-Usuario` llegue al backend.
5. **78 NotificacionesAlertas:** indicador de la barra superior, listado, descartar y restaurar, y que no interfiera con el selector de rol ni con el menú móvil.

Al terminar, anotar la evidencia en el §7 de cada spec.

## 5. Pendientes menores (sin dueño, cualquiera puede tomarlos)
- Texto del panel de detalle vacío («Selecciona…») más grande que el resto: corregir en el componente compartido `PanelDetalle`.
- El menú lateral no abre el dominio del módulo actual.
- Cabeceras en dos líneas que ya existían: «Orden generada» (Cotizaciones) y «Monto estimado» (SolicitudesCompra); números `OA-DEV-…` partidos a 1280 px.
- Plural del backend de devoluciones: «Solo quedan 1 unidades…».
- Test intermitente `AlmacenesPage` «muestra capacidades del seed…» (≈1 fallo en 40 ejecuciones con la máquina cargada).
- Partir `ProveedoresPage.tsx` (624 líneas).

## 6. Avisos útiles
- Con la máquina muy cargada, algunos tests del frontend pueden superar los 5 s y fallar por tiempo; repetir antes de investigar.
- No cambies selectores de reglas compartidas de `app.css` (p. ej. `.tabla-paginada th`): pisan las cabeceras de otras pantallas (ver [`lecciones.md`](lecciones.md)).
- Las migraciones solo con `dotnet ef migrations add`; en la demo las aplica Lukas en Neon antes de desplegar.
