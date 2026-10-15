# TitanGlobal ERP & Supply Chain Ecosystem

ERP transaccional para una corporación manufacturera y logística: 80 módulos en 8 dominios. API en .NET 10 con PostgreSQL y frontend en React + Vite + TypeScript.

- **Demo:** https://titanglobal-erp.vercel.app (la API va detrás de `/api`; si no se usa en ~15 min tarda ~1 min en despertar).
- **Lo que se evalúa:** [`docs/enunciado.md`](docs/enunciado.md) (Lab 08 / Examen).

## Empieza aquí
0. Lee el [contexto del proyecto](docs/contexto-proyecto.md): qué es, qué pide el examen, cómo está construido, qué está implementado, decisiones y pendientes.
1. Lee la [guía para compañeros](docs/guia-companeros.md): cómo instalar, levantar el proyecto en tu máquina y entregar trabajo sin pisar el de otros.
2. Mira en [`docs/equipo/asignaciones.md`](docs/equipo/asignaciones.md) qué módulo lleva cada uno y apúntate antes de empezar.
3. Las reglas de código están en [`CLAUDE.md`](CLAUDE.md) y valen para personas y agentes.

## Estado

| Módulo | Backend | Pantalla | QA de extremo a extremo |
|---|---|---|---|
| 04 OrdenesCompra (módulo del enunciado) | ✅ | ✅ | ✅ |
| 01 Clientes | ✅ | ✅ | ✅ |
| 21 Productos | ✅ | ✅ | ✅ |
| 11 Proveedores | ✅ | ✅ | ✅ |
| 13 SolicitudesCompra | ✅ | ✅ | ✅ |
| 15 OrdenesAbastecimiento | ✅ | ✅ | ✅ |
| 23 Almacenes | ✅ | ✅ | en curso |
| 24 ZonasAlmacenaje | en curso | pendiente | — |

El resto de módulos muestra «Pantalla pendiente». La lista completa está en [`docs/arquitectura.md`](docs/arquitectura.md).

## Mapa del repositorio
```
src/TitanGlobal.Api/      API (.NET 10): Middlewares, Models, Repositories, Modules/ModuloNN_Nombre
tests/TitanGlobal.Api.Tests/  tests de integración (xUnit)
frontend/                 SPA React + Vite + TS
scripts/                  verify.sh (gate), smoke-postgres.sh, seed-dev.sql
docs/                     enunciado, arquitectura, specs (una por tarea), despliegue, equipo
```
