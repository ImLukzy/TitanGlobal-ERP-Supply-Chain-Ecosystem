# Relevo: dónde se quedó el trabajo (2026-10-15)

Lukas pausó el desarrollo de su parte el 2026-10-15. Este documento dice exactamente qué está hecho, qué quedó a medias, dónde está cada cosa y cómo seguir. Para el resto del proyecto: [`contexto-proyecto.md`](../contexto-proyecto.md), [`guia-companeros.md`](../guia-companeros.md) y [`asignaciones.md`](asignaciones.md).

## 1. Estado en una línea
`main` está al día con la rama de integración `ImLukzy/Claudio`, que es la desplegada (https://titanglobal-erp.vercel.app). Lukas tiene **sus 13 módulos terminados** y ya no tiene pendientes; los otros **7 tienen el backend terminado y en producción** y solo les falta la pantalla (08, 09, 10, 77 y 78 de Maxi; 71 de Alexander; 72 de Sergio). El resto de módulos está por empezar.

## 2. Pantallas pendientes y responsables
**Terminados de Lukas (backend + pantalla + QA + producción):** 01 Clientes, 02 SegmentacionClientes, 03 Cotizaciones, 04 OrdenesCompra, 05 ContratosComerciales, 06 ListasPrecios, 07 PromocionesDescuentos, 11 Proveedores, 13 SolicitudesCompra, 15 OrdenesAbastecimiento, 21 Productos, 23 Almacenes, 24 ZonasAlmacenaje.

**Pendientes (el backend ya está en `main` y en producción):**

| Módulo | Responsable | Spec de la pantalla | Estado de la pantalla | Rama en GitHub | Commits (en orden) | Siguiente paso |
|---|---|---|---|---|---|---|
| 09 ReclamosPostventa | Maxi | [34](../specs/34-modulo09-reclamos-postventa-frontend.md) | hecha y corregida 1 vez (revisión visual OK) | `pendiente/09-reclamos-postventa` | `968f5ff`, `28d3391` | QA de extremo a extremo → integrar |
| 10 DevolucionesComerciales | Maxi | [36](../specs/36-modulo10-devoluciones-comerciales-frontend.md) | hecha y corregida 1 vez (revisión visual OK) | `pendiente/10-devoluciones-comerciales` | `ef9ecd3`, `ca03095` | QA → integrar |
| 77 AuditoriaTransaccional | Maxi | [46](../specs/46-modulo77-auditoria-transaccional-frontend.md) | hecha y corregida 2 veces (revisión visual OK); añade la cabecera `X-Usuario` en `api/client.ts` | `pendiente/77-auditoria-transaccional` | `73e075b`, `d483abb`, `5a585a4` | QA → integrar |
| 78 NotificacionesAlertas | Maxi | [38](../specs/38-modulo78-notificaciones-alertas-frontend.md) | hecha, sin revisión de la integración | `pendiente/78-notificaciones-alertas` | `f46b6bc` | revisión visual con la lista de [`lecciones.md`](lecciones.md) → QA → integrar |
| 08 ComisionesVendedores | Maxi | [40](../specs/40-modulo08-comisiones-vendedores-frontend.md) | hecha, revisión visual rápida OK (pestañas Vendedores · Asignación · Comisiones del mes) | `pendiente/08-comisiones-vendedores` | `a3e0847` | revisión visual completa con la lista de [`lecciones.md`](lecciones.md) → QA → integrar |
| 72 RolesPermisos | Sergio | [42](../specs/42-modulo72-roles-permisos-frontend.md) | por empezar | — | — | implementar la spec 42 |
| 71 Usuarios | Alexander | [44](../specs/44-modulo71-usuarios-frontend.md) | por empezar | — | — | implementar la spec 44 (usa los roles del 72) |

Cada rama `pendiente/…` sale de una versión anterior de la integración y contiene solo los commits de esa pantalla. Ninguna toca la base de datos.

## 3. Cómo terminar una pantalla pendiente
1. **Rama de trabajo desde la integración con los commits de la tabla:**
   ```sh
   git fetch origin
   git switch -c tu-nombre/09-reclamos origin/ImLukzy/Claudio
   git cherry-pick 968f5ff 28d3391
   ```
   Si da conflicto en `frontend/src/modules/pantallas.ts` o en `frontend/src/styles/app.css`, es porque otra pantalla se registró en el mismo sitio: **conserva las dos líneas** (las de la integración y las de la pantalla) y sigue con `git cherry-pick --continue`.
2. **Gate:** `./scripts/verify.sh` en verde.
3. **QA a mano** contra la API y PostgreSQL locales (guía §3): recorrer cada criterio de la spec (filtros, detalle, acciones, mensajes de error del backend en los diálogos) a 375 y 1280 px, en claro y en oscuro, con la lista de comprobación de [`lecciones.md`](lecciones.md).
4. **Evidencia** en el §7 de la spec (fecha, criterio, resultado, comando o captura) y marca el §6.
5. **Pull request contra `ImLukzy/Claudio`** (como dice la guía, nunca contra `main`) con la salida del gate y capturas. Lukas revisa, integra y despliega.

## 4. Pendientes menores (sin dueño, cualquiera puede tomarlos)
- Texto del panel de detalle vacío («Selecciona…») más grande que el resto: corregir en el componente compartido `PanelDetalle`.
- El menú lateral no abre el dominio del módulo actual.
- Cabeceras en dos líneas que ya existían: «Orden generada» (Cotizaciones) y «Monto estimado» (SolicitudesCompra); números `OA-DEV-…` partidos a 1280 px.
- Plural del backend de devoluciones: «Solo quedan 1 unidades…».
- Test intermitente `AlmacenesPage` «muestra capacidades del seed…» (≈1 fallo en 40 ejecuciones con la máquina cargada).
- Partir `ProveedoresPage.tsx` (624 líneas).

## 5. Avisos útiles
- Con la máquina muy cargada, algunos tests del frontend pueden superar los 5 s y fallar por tiempo; repetir antes de investigar.
- No cambies selectores de reglas compartidas de `app.css` (p. ej. `.tabla-paginada th`): pisan las cabeceras de otras pantallas (ver [`lecciones.md`](lecciones.md)).
- Las migraciones solo con `dotnet ef migrations add`; en la demo las aplica Lukas en Neon antes de desplegar.
