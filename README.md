# TitanGlobal ERP & Supply Chain Ecosystem

ERP transaccional para una corporación manufacturera y logística: 80 módulos en 8 dominios. API en .NET 10 con PostgreSQL y frontend en React + Vite + TypeScript.

- **Demo:** https://titanglobal-erp.vercel.app (la API va detrás de `/api`; si no se usa en ~15 min tarda ~1 min en despertar).
- **Lo que se evalúa:** [`docs/enunciado.md`](docs/enunciado.md) (Lab 08 / Examen).

## Empieza aquí
0. Lee el [contexto del proyecto](docs/contexto-proyecto.md): qué es, qué pide el examen, cómo está construido, qué está implementado, decisiones y pendientes.
1. Lee la [guía para compañeros](docs/guia-companeros.md): cómo instalar, levantar el proyecto en tu máquina y entregar trabajo sin pisar el de otros.
2. Lee el [relevo](docs/equipo/relevo.md): dónde se quedó el trabajo de Lukas y qué pantallas quedan en ramas `pendiente/…`.
3. Mira en [`docs/equipo/asignaciones.md`](docs/equipo/asignaciones.md) tu bloque de módulos (7 personas, reparto por dominio) y el orden sugerido.
4. Las reglas de código están en [`CLAUDE.md`](CLAUDE.md) y valen para todo el equipo.

## Estado

| Módulo | Backend | Pantalla | QA de extremo a extremo |
|---|---|---|---|
| 04 OrdenesCompra (módulo del enunciado) | ✅ | ✅ | ✅ |
| 01 Clientes | ✅ | ✅ | ✅ |
| 21 Productos | ✅ | ✅ | ✅ |
| 11 Proveedores | ✅ | ✅ | ✅ |
| 13 SolicitudesCompra | ✅ | ✅ | ✅ |
| 15 OrdenesAbastecimiento | ✅ | ✅ | ✅ |
| 23 Almacenes | ✅ | ✅ | ✅ |
| 24 ZonasAlmacenaje | ✅ | ✅ | ✅ |
| 02 SegmentacionClientes | ✅ | ✅ | ✅ |
| 03 Cotizaciones | ✅ | ✅ | ✅ |
| 05 ContratosComerciales | ✅ | ✅ | ✅ |
| 06 ListasPrecios | ✅ | ✅ | ✅ |
| 07 PromocionesDescuentos | ✅ | ✅ | ✅ |
| 09 ReclamosPostventa | ✅ | en rama, falta QA | — |
| 10 DevolucionesComerciales | ✅ | en rama, falta QA | — |
| 78 NotificacionesAlertas | ✅ | en rama, falta QA | — |
| 08 ComisionesVendedores | ✅ | en rama, falta QA | — |
| 72 RolesPermisos | ✅ | pendiente | — |
| 71 Usuarios | ✅ | pendiente | — |
| 77 AuditoriaTransaccional | ✅ | en rama, falta QA | — |

El resto de módulos muestra «Pantalla pendiente». La lista completa está en [`docs/arquitectura.md`](docs/arquitectura.md).

## Mapa del repositorio
```
src/TitanGlobal.Api/      API (.NET 10): Middlewares, Models, Repositories, Modules/ModuloNN_Nombre
tests/TitanGlobal.Api.Tests/  tests de integración (xUnit)
frontend/                 SPA React + Vite + TS
scripts/                  verify.sh (gate), smoke-postgres.sh, seed-dev.sql
docs/                     enunciado, arquitectura, specs (una por tarea), despliegue, equipo
```
