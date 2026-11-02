# TitanGlobal ERP & Supply Chain Ecosystem

ERP transaccional para una corporación manufacturera y logística: 80 módulos en 8 dominios. API en .NET 10 con PostgreSQL y frontend en React + Vite + TypeScript.

- **Demo:** https://titanglobal-erp.vercel.app (la API va detrás de `/api`; si no se usa en ~15 min tarda ~1 min en despertar).
- **Lo que se evalúa:** [`docs/enunciado.md`](docs/enunciado.md) (Lab 08 / Examen).

## Empieza aquí
0. Lee el [contexto del proyecto](docs/contexto-proyecto.md): qué es, qué pide el examen, cómo está construido, qué está implementado, decisiones y pendientes.
1. Sigue el [primer día](docs/equipo/primer-dia.md): instalación, comprobaciones y primer encargo.
2. Lee la [guía para compañeros](docs/guia-companeros.md): cómo levantar el proyecto y entregar trabajo sin pisar el de otros.
3. Lee [cómo contribuir](CONTRIBUTING.md): reglas de código y flujo de un módulo, y la guía paso a paso para [crear un módulo en cuatro capas](docs/equipo/crear-modulo.md). Si tenías una rama empezada antes del paso a cuatro capas, sigue [cómo adaptar una rama a capas](docs/equipo/adaptar-rama-a-capas.md).
4. Lee la [cadencia](docs/equipo/cadencia.md): meta de **3 módulos por semana** y cómo se organiza la semana.
5. Mira en [`docs/equipo/asignaciones.md`](docs/equipo/asignaciones.md) tu bloque de módulos (7 personas, reparto por dominio) y el orden sugerido.
6. Lee el [relevo](docs/equipo/relevo.md): dónde se quedó el trabajo de Lukas, qué se integró y qué pantallas quedan.

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
| 09 ReclamosPostventa | ✅ | ✅ | pendiente |
| 10 DevolucionesComerciales | ✅ | ✅ | pendiente |
| 78 NotificacionesAlertas | ✅ | ✅ | pendiente |
| 08 ComisionesVendedores | ✅ | ✅ | pendiente |
| 72 RolesPermisos | ✅ | pendiente | — |
| 71 Usuarios | ✅ | pendiente | — |
| 77 AuditoriaTransaccional | ✅ | ✅ | pendiente |

Las pantallas 08, 09, 10, 77 y 78 se integraron en `main` el 2026-10-31; su QA visual de extremo a extremo queda como tarea de la semana 0 del plan ([`docs/equipo/cadencia.md`](docs/equipo/cadencia.md)).

El resto de módulos muestra «Pantalla pendiente». La lista completa está en [`docs/arquitectura.md`](docs/arquitectura.md).

## Mapa del repositorio
```
src/TitanGlobal.Domain/          Models/, Repositories/, Common/
src/TitanGlobal.Application/     DTOs/ModuloNN_Nombre/, UseCases/ModuloNN_Nombre/, Exceptions/, Services/
src/TitanGlobal.Infrastructure/  Persistence/, Migrations/, Repositories/Implementation/, Configuration/
src/TitanGlobal.Api/             Controllers/ModuloNN_Nombre/, Middlewares/, Configuration/, Services/, Program.cs
tests/TitanGlobal.Api.Tests/     tests de integración y de arquitectura (xUnit)
frontend/                        SPA React + Vite + TS
scripts/                         verify.sh (gate), check-rules.sh, smoke-postgres.sh, seed-dev.sql, adaptar-rama-capas.py
docs/                            enunciado, arquitectura, specs (una por tarea), despliegue, equipo
```
