# Guía para compañeros

Todo lo necesario para trabajar en TitanGlobal sin perderse ni pisar el trabajo de otros. Si algo no funciona como dice aquí, avisa a Lukas y se corrige la guía.

## 1. Accesos
| Servicio | Para qué | Quién lo da |
|---|---|---|
| GitHub `ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem` (privado) | código, ramas y pull requests | Lukas te invita como colaborador |
| Neon (PostgreSQL en la nube) | solo si necesitas probar contra una base en la nube: se te da **una rama propia** de la base, nunca la de la demo | Lukas |
| Render (API) y Vercel (frontend) | la demo pública | los despliega la integración; no hace falta acceso para desarrollar |

Para desarrollar **no necesitas ninguna cuenta en la nube**: todo funciona en tu máquina con Docker.

## 2. Instalar
- .NET SDK 10 (`dotnet --version` → 10.x)
- Node.js 22 o superior y npm
- Docker con Docker Compose
- Git configurado con **tu** nombre y correo (`git config --global user.name "…"` y `user.email "…"`)

## 3. Levantar el proyecto en local
```sh
git clone https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem.git
cd TitanGlobal-ERP-Supply-Chain-Ecosystem
git switch main                       # rama de integración (la que está desplegada)

docker compose up -d db               # PostgreSQL 16 en el puerto 5433
dotnet tool restore
dotnet ef database update -p src/TitanGlobal.Api
docker compose exec -T db psql -U titanglobal -d titanglobal -f - < scripts/seed-dev.sql   # datos de ejemplo

dotnet run --project src/TitanGlobal.Api --urls http://localhost:5080   # API
npm --prefix frontend ci && npm --prefix frontend run dev               # http://localhost:5173 (en otra terminal)
```
Puertos: API 5080, frontend 5173, PostgreSQL 5433. La contraseña de `docker-compose.yml` es solo para tu máquina.

Comprobación rápida: http://localhost:5080/api/v1/sistema/salud → `{"estado":"OK"}` y http://localhost:5173/modulos/01-clientes muestra clientes.

## 4. Cómo se trabaja (en orden)
1. **Elige un módulo** libre en [`equipo/asignaciones.md`](equipo/asignaciones.md) y apúntate en un PR pequeño o avisando a Lukas. Un módulo = una persona: así nadie toca los mismos archivos.
2. **Spec primero.** Copia [`specs/_TEMPLATE.md`](specs/_TEMPLATE.md) a `docs/specs/NN-moduloXX-nombre-{backend|frontend}.md` (NN = siguiente número libre). Usa como modelo las specs 12 y 13 (Proveedores) o 14 y 15 (SolicitudesCompra): contrato de la API con rutas, DTOs y **mensajes de error literales**, reglas en orden y criterios medibles. Lukas la revisa antes de programar.
3. **Rama propia** desde la de integración: `git switch -c tu-nombre/moduloXX-nombre main`.
4. **Programa siguiendo el módulo de referencia**: backend `src/TitanGlobal.Api/Modules/Modulo11_Proveedores/` (o `Modulo13_SolicitudesCompra` si tu módulo tiene líneas y estados); frontend `frontend/src/modules/proveedores/` (o `solicitudes-compra/`). Reglas de código: [`CLAUDE.md`](../CLAUDE.md) — sin `var`, sin `=>` en miembros, UseCases sin interfaz, nombres en español, mensajes literales.
5. **Gate antes de entregar:** `./scripts/verify.sh` en verde (build sin warnings, formato, reglas, tests de backend y frontend). Si tocaste base de datos, también `./scripts/smoke-postgres.sh` → `SMOKE OK`.
6. **Pull request contra `main`**. Descripción: qué spec cumple, salida del gate y capturas a 375 y 1280 px si hay pantalla. Lukas revisa, integra y despliega.

## 5. Para no pisarse
- **Migraciones:** son el punto de choque más común (todas tocan `TitanGlobalDbContextModelSnapshot.cs`). Antes de generar la tuya, actualiza tu rama (`git fetch && git rebase origin/main`) y genera con `dotnet ef migrations add <Nombre> -p src/TitanGlobal.Api`. **Nunca** edites a mano una migración ni el snapshot; si hay conflicto en el snapshot, borra tu migración, rebasa y vuelve a generarla.
- **Archivos compartidos** (`TitanGlobalDbContext.cs`, `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh`, `frontend/src/modules/pantallas.ts`, `frontend/src/styles/app.css`): añade tu bloque al final y no reordenes lo de otros.
- **No modifiques** las 4 entidades del enunciado (`Cliente`, `Producto`, `OrdenCompra`, `DetalleOrdenCompra`) ni los middlewares sin una spec aprobada.
- **Acciones POST sin cuerpo** (aprobar, recibir…): atributo `[PermitirCuerpoVacio]`, nunca rutas dentro del middleware.
- **Tests:** no borres ni debilites tests existentes; esperas con temporizadores falsos (`vi.useFakeTimers`), nunca `setTimeout` reales.
- **Ramas cortas:** integra a menudo; una rama que vive una semana acaba en conflictos.

## 6. Lo que nunca va al repositorio
- `.env`, cadenas de conexión reales, contraseñas o tokens (Neon, Render, Vercel).
- `appsettings.*.json` con secretos.
Si subes uno por error, avisa enseguida: hay que rotarlo, no basta con borrarlo.

## 7. Dónde está cada cosa
- Qué pide el examen: [`enunciado.md`](enunciado.md)
- Los 80 módulos y decisiones de arquitectura: [`arquitectura.md`](arquitectura.md)
- Todas las specs con su evidencia: [`specs/`](specs/)
- Errores que no hay que repetir: [`equipo/lecciones.md`](equipo/lecciones.md)
- Cómo se despliega la demo: [`despliegue.md`](despliegue.md)
