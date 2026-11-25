# Relevo: dónde se quedó el trabajo (2026-11-02)

Este documento dice qué está hecho, qué quedó a medias, dónde está cada cosa y cómo seguir. Para el resto del proyecto: [`contexto-proyecto.md`](../contexto-proyecto.md), [`guia-companeros.md`](../guia-companeros.md) y [`asignaciones.md`](asignaciones.md).

## 1. Módulos integrados el 2026-11-02: qué le falta a cada uno

Los módulos de estas ramas **ya están en `main`**, con sus commits a nombre de cada autor (PR #12 a #17). Se adaptaron a las cuatro capas y los arreglos de integración van en commits aparte.

**Qué tienes que hacer:**
1. Actualiza tu `main` local. Se corrigieron sus commits (`git pull` no basta):
   ```sh
   git fetch origin
   git switch main
   git reset --hard origin/main
   ```
   No subas tu `main` antiguo.
2. Empieza siempre desde `main`: `git switch -c tu-nombre/moduloXX-nombre origin/main`. **No sigas trabajando en tu rama antigua**: tiene la estructura vieja y ya no hace falta. Si tienes en ella commits sin subir o posteriores a los de la tabla, sigue [`adaptar-rama-a-capas.md`](adaptar-rama-a-capas.md).
3. Aplica los scripts nuevos de `database/` en tu base local: `./scripts/aplicar-bd.sh`. Si tu base se creó cuando el proyecto aún usaba migraciones, recréala con `./scripts/aplicar-bd.sh --reiniciar` y vuelve a cargar `scripts/seed-dev.sql`.

| Persona | Módulos integrados | Lo que le falta para terminarlos |
|---|---|---|
| Sergio | 72 RolesPermisos: backend y pantalla completa (PR #42, 2026-11-23). 12 EvaluacionProveedores: backend (PR #43, `005` aplicado en la demo el 2026-11-23) | **12:** pantalla, con su spec. Siguen 14 y 20 (PR #44 y #45), que deben actualizarse con `main` |
| Leonel | 75 ImpuestosTasas: backend, pruebas, seed y smoke | Cierre o edición de tasas (hoy una tasa abierta impide registrar otra), tasa de 0 %, `Total` según el tipo de impuesto, paginación, resumen y pantalla |
| Maxi | 22 CategoriasProducto: backend, pruebas, seed y smoke | Pantalla, reactivar categorías, unicidad sin distinguir mayúsculas y decidir la jerarquía de familias, líneas y sublíneas. También el QA visual de §5 |
| Antony | 33 CentrosTrabajo: backend, pruebas, seed y smoke | Pantalla y control de concurrencia en baja y edición |
| Kelvin | 41 VehiculosFlota: backend y pantalla con placa única (PR #51, `021` aplicado en la demo el 2026-11-23). 42 Conductores: backend y pantalla con licencia única y vigencia (PR #59, `022` aplicado en la demo el 2026-11-23). 48 TransportistasTerceros: backend y pantalla (PR #63, `012` aplicado en la demo el 2026-11-23) | Nada del 41, el 42 ni el 48. Siguen 43 (PR #73), 44, 45, 46, 47, 49, 50 y 73, de uno en uno |
| Alexander | 61 Empleados y 71 Usuarios: backend de 61 y ambas pantallas. 62 a 70 (Capital Humano completo): backend y pantalla de cada uno (PR #39 y #64 a #72, `008` y `034` a `041` aplicados en la demo; los 9 PR integrados el 2026-11-24) | QA visual del 61 y el 71 integrado (PR #20). Nada del 62 al 70 |

Un módulo cuenta como terminado solo con todo lo de [`cadencia.md`](cadencia.md) §6. Un backend integrado sin pantalla aún no cuenta.

## 2. Estado en una línea
`main` es la rama de integración y la desplegada (https://titanglobal-erp.vercel.app). Lukas tiene **sus 13 módulos terminados** y hace la integración. Las cinco pantallas que estaban aparcadas en ramas `pendiente/…` (08, 09, 10, 77 y 78, de Maxi) ya están **integradas en `main` y desplegadas**; solo les falta el QA visual. Ver §1 para los módulos integrados el 2026-11-02 y lo que les falta; el resto está por empezar.

## 3. Pantallas pendientes y responsables
**Terminados de Lukas (backend + pantalla + QA + producción):** 01 Clientes, 02 SegmentacionClientes, 03 Cotizaciones, 04 OrdenesCompra, 05 ContratosComerciales, 06 ListasPrecios, 07 PromocionesDescuentos, 11 Proveedores, 13 SolicitudesCompra, 15 OrdenesAbastecimiento, 21 Productos, 23 Almacenes, 24 ZonasAlmacenaje.

**Pendientes (el backend ya está en `main` y en producción):**

| Módulo | Responsable | Spec de la pantalla | Estado | Siguiente paso |
|---|---|---|---|---|
| 72 RolesPermisos | Sergio | [42](../specs/42-modulo72-roles-permisos-frontend.md) | terminada (PR #42, 2026-11-23) | — |
| 71 Usuarios | Alexander | [44](../specs/44-modulo71-usuarios-frontend.md) | integrada (§1) | QA visual hecho (PR #20) |

## 4. Pantallas integradas el 2026-10-31

Las cinco ramas `pendiente/…` se rebasaron sobre `main`, se resolvieron los conflictos de `frontend/src/modules/pantallas.ts` y `frontend/src/styles/app.css`, pasaron el gate y se integraron por pull request. Después se borraron las ramas remotas: el contenido vive en `main`.

| Módulo | Responsable | Spec | PR | QA visual |
|---|---|---|---|---|
| 08 ComisionesVendedores | Maxi | [40](../specs/40-modulo08-comisiones-vendedores-frontend.md) | #3 | pendiente |
| 09 ReclamosPostventa | Maxi | [34](../specs/34-modulo09-reclamos-postventa-frontend.md) | #4 | pendiente |
| 10 DevolucionesComerciales | Maxi | [36](../specs/36-modulo10-devoluciones-comerciales-frontend.md) | #5 | pendiente |
| 77 AuditoriaTransaccional | Maxi | [46](../specs/46-modulo77-auditoria-transaccional-frontend.md) | #6 | pendiente (incluye la cabecera `X-Usuario` en `api/client.ts`) |
| 78 NotificacionesAlertas | Maxi | [38](../specs/38-modulo78-notificaciones-alertas-frontend.md) | #7 | pendiente (incluye el indicador de alertas en la barra superior) |

## 5. QA visual pendiente de las cinco pantallas

Recorrer en la demo, o en local contra PostgreSQL, cada criterio de la spec a 375 y 1280 px, en claro y en oscuro, con la lista de [`lecciones.md`](lecciones.md):

1. **08 ComisionesVendedores:** pestañas Vendedores · Asignación · Comisiones del mes; alta de vendedor y asignación de una orden.
2. **09 ReclamosPostventa:** filtros, resumen por estado y tipo, detalle y transiciones Abierto → EnProceso → Resuelto → Cerrado / Rechazado.
3. **10 DevolucionesComerciales:** devolución parcial y total, tope de lo comprado menos lo ya devuelto, mensajes de error del backend en el diálogo.
4. **77 AuditoriaTransaccional:** resumen, actividad de la semana, filtros, detalle de cambios y que la cabecera `X-Usuario` llegue al backend.
5. **78 NotificacionesAlertas:** indicador de la barra superior, listado, descartar y restaurar, y que no interfiera con el selector de rol ni con el menú móvil.

Al terminar, anotar la evidencia en el §7 de cada spec.

## 6. Pendientes menores (sin dueño, cualquiera puede tomarlos)
- Texto del panel de detalle vacío («Selecciona…») más grande que el resto: corregir en el componente compartido `PanelDetalle`.
- El menú lateral no abre el dominio del módulo actual.
- Cabeceras en dos líneas que ya existían: «Orden generada» (Cotizaciones) y «Monto estimado» (SolicitudesCompra); números `OA-DEV-…` partidos a 1280 px.
- Plural del backend de devoluciones: «Solo quedan 1 unidades…».
- Test intermitente `AlmacenesPage` «muestra capacidades del seed…» (≈1 fallo en 40 ejecuciones con la máquina cargada).
- Partir `ProveedoresPage.tsx` (624 líneas).

## 7. Avisos útiles
- Con la máquina muy cargada, algunos tests del frontend pueden superar los 5 s y fallar por tiempo; repetir antes de investigar.
- No cambies selectores de reglas compartidas de `app.css` (p. ej. `.tabla-paginada th`): pisan las cabeceras de otras pantallas (ver [`lecciones.md`](lecciones.md)).
- El esquema solo con scripts `database/NNN-*.sql` y entidades generadas con `./scripts/scaffold.sh`; en la demo los scripts los aplica Lukas en Neon antes de desplegar.

## 8. Números de los scripts de base de datos
Varias ramas eligieron el mismo número. Para que no choquen al integrar, cada script tiene reservado este número (2026-11-18). Si tu rama usa otro, renómbralo antes de abrir el PR. Que queden huecos no importa: `./scripts/aplicar-bd.sh` aplica en orden los que falten.

| Número | Script | Responsable | Rama | Renombrar |
|---|---|---|---|---|
| 001 | `001-esquema-inicial.sql` | — | `main` | — |
| 002 | `002-lotes-series.sql` | Maxi | `maxi/modulo25-lotes-series` | no |
| 003 | `003-kardex-valorizado.sql` | Maxi | `maxi/modulo26-kardex-valorizado` | no |
| 004 | `004-transferencias-almacen.sql` | Maxi | `maxi/modulo27-transferencias-almacen` | no |
| 005 | `005-evaluaciones-proveedor.sql` | Sergio | `sergio/modulo12-evaluacion-proveedores` | no (en `main` por el PR #43 y aplicado en la demo) |
| 006 | `006-licitaciones.sql` | Sergio | `sergio/modulo14-licitaciones` | sí, desde 005 |
| 007 | `007-modulo51-facturacion-electronica.sql` | Leonel | `leonel/modulo51-facturacion-electronica` | sí, desde 002 |
| 008 | `008-contratos-laborales.sql` | Alexander | `alexander/modulo62-contratos-laborales-backend` | no (en `main` por el PR #39 y aplicado en la demo) |
| 009 | `009-reglas-integridad.sql` | Lukas | `lukas/bd-fase2` (PR #24) | ya renombrado |
| 010 | `010-acuerdos-abastecimiento.sql` | Sergio | `sergio/modulo20-acuerdos-abastecimiento` (PR #45) | no |
| 011 | `011-rutas-produccion.sql` | Antony | `antony/modulo32-rutas-produccion` (PR #49) | no |
| 012 | `012-transportistas-terceros.sql` | Kelvin | `kelvin/modulo48-transportistas-terceros` | no (en `main` por el PR #63 y aplicado en la demo) |
| 013 | `013-rutas-despacho.sql` | Kelvin | `kelvin/modulo43-rutas-despacho` | no |
| 014 | `014-consolidacion-carga.sql` | Kelvin | `kelvin/modulo44-consolidacion-carga` | no |
| 015 | `015-guias-remision.sql` | Kelvin | `kelvin/modulo45-guias-remision` | no |
| 016 | `016-monitoreo-entregas.sql` | Kelvin | `kelvin/modulo46-monitoreo-entregas` (aún sin subir) | no |
| 017 | `017-control-combustible.sql` | Kelvin | `kelvin/modulo47-control-combustible` | no |
| 018 | `018-logistica-inversa.sql` | Kelvin | `kelvin/modulo49-logistica-inversa` | no |
| 019 | `019-siniestros-flota.sql` | Kelvin | `kelvin/modulo50-siniestros-flota` | no |
| 020 | `020-sucursales.sql` | Kelvin | `kelvin/modulo73-sucursales` | no |
| 021 | `021-vehiculos-flota-placa-unica.sql` | Kelvin | `kelvin/modulo41-vehiculos-flota-frontend` | no (en `main` por el PR #51 y aplicado en la demo) |
| 022 | `022-conductores-licencia-unica.sql` | Kelvin | `kelvin/modulo42-conductores` | no (en `main` por el PR #59 y aplicado en la demo) |
| 023 | `023-listas-materiales.sql` | Antony | `antony/modulo31-listas-materiales` (PR #40) | sí, desde 002 |
| 034 | `034-turnos-horarios.sql` | Alexander | `alexander/modulo64-turnos-horarios` | no (en `main` por el PR #65 y aplicado en la demo) |
| 035 | `035-asistencia-biometrica.sql` | Alexander | `alexander/modulo63-asistencia-biometrica` | no (en `main` por el PR #66 y aplicado en la demo) |
| 036 | `036-vacaciones-permisos.sql` | Alexander | `alexander/modulo65-vacaciones-permisos` | no (en `main` por el PR #67 y aplicado en la demo) |
| 037 | `037-adelantos-prestamos.sql` | Alexander | `alexander/modulo67-adelantos-prestamos` | no (en `main` por el PR #68 y aplicado en la demo) |
| 038 | `038-planillas.sql` | Alexander | `alexander/modulo66-planillas` | no (en `main` por el PR #69 y aplicado en la demo) |
| 039 | `039-evaluacion-desempeno.sql` | Alexander | `alexander/modulo68-evaluacion-desempeno` | no (en `main` por el PR #70 y aplicado en la demo) |
| 040 | `040-capacitaciones.sql` | Alexander | `alexander/modulo69-capacitaciones` | no (en `main` por el PR #71 y aplicado en la demo) |
| 041 | `041-seguridad-salud-ocupacional.sql` | Alexander | `alexander/modulo70-seguridad-salud-ocupacional` | no (en `main` por el PR #72 y aplicado en la demo) |
| 044 | `044-control-calidad-producto-terminado.sql` | Antony | `antony/modulo37-control-calidad-producto-terminado` | no |

Reservados además (2026-11-23), con su fila en la rama de cada uno: 024 Leonel (52), 025 y 026 Antony (34 y 39), 027 Sergio (16, PR #56) y 028 a 033 Sergio (sus próximas specs). Un script nuevo toma el siguiente número libre (034 en adelante) y se añade aquí en el mismo PR. Esta tabla es la única fuente: cada rama solo añade **su** fila y no reescribe las demás (2026-11-22).
