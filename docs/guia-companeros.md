# Guía para compañeros

Todo lo necesario para trabajar en TitanGlobal sin perderse ni pisar el trabajo de otros. Si algo no funciona como dice aquí, avisa a Lukas y se corrige la guía.

Para el arranque en una máquina nueva, sigue [`equipo/primer-dia.md`](equipo/primer-dia.md). Para la organización semanal y la meta de 3 módulos por semana, [`equipo/cadencia.md`](equipo/cadencia.md).

## 1. Accesos
| Servicio | Para qué | Quién lo da |
|---|---|---|
| GitHub `ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem` (privado) | código, ramas y pull requests | Lukas te invita como colaborador |
| Neon (PostgreSQL en la nube) | solo si necesitas probar contra una base en la nube: se te da **una rama propia** de la base, nunca la de la demo | Lukas |
| Render (API) y Vercel (frontend) | la demo pública | los despliega la integración; no hace falta acceso para desarrollar |

Para desarrollar **no necesitas ninguna cuenta en la nube**: todo funciona en tu máquina con Docker.

## 2. Instalar
- .NET SDK 10 (`dotnet --version` → 10.x)
- Node.js 22 LTS y npm (`nvm use` lee el `.nvmrc`)
- Docker con Docker Compose
- Git configurado con **tu** nombre y correo (`git config --global user.name "…"` y `user.email "…"`)

## 3. Levantar el proyecto en local
```sh
git clone https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem.git
cd TitanGlobal-ERP-Supply-Chain-Ecosystem
git switch main                       # rama de integración (la que está desplegada)

docker compose up -d db               # PostgreSQL 16 en el puerto 5433
dotnet tool restore
dotnet ef database update -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api
docker compose exec -T db psql -U titanglobal -d titanglobal -f - < scripts/seed-dev.sql   # datos de ejemplo

dotnet run --project src/TitanGlobal.Api --urls http://localhost:5080   # API
npm --prefix frontend ci && npm --prefix frontend run dev               # http://localhost:5173 (en otra terminal)
```
Puertos: API 5080, frontend 5173, PostgreSQL 5433. La contraseña de `docker-compose.yml` es solo para tu máquina.

Detalle de la base local (Windows/PowerShell, sin Docker, reiniciar datos, errores frecuentes): [`equipo/base-de-datos-local.md`](equipo/base-de-datos-local.md).

Comprobación rápida: http://localhost:5080/api/v1/sistema/salud → `{"estado":"OK"}`, http://localhost:5080/swagger (Swagger UI en desarrollo; OpenAPI en `/openapi/v1.json`) y http://localhost:5173/modulos/01-clientes muestra clientes.

### Probar contra una base en la nube (opcional)
Para desarrollar basta con Docker. Si necesitas una base en la nube (por ejemplo, para enseñar algo desde otra máquina), pide a Lukas tu **rama de Neon** (`dev-<tu-nombre>`): es una copia de la base de la demo solo para ti, y puedes romperla sin afectar a nadie. No necesitas cuenta en Neon. Lukas te enviará por mensaje privado una cadena de conexión, que usas así:
```sh
dotnet run --project src/TitanGlobal.Api --urls http://localhost:5080 \
  --ConnectionStrings:DefaultConnection="<tu cadena>"
dotnet ef database update -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api --connection "<tu cadena>"   # si tu rama tiene migraciones nuevas
```
Esa cadena es un secreto: no la pegues en el repositorio, en `appsettings*.json`, en issues ni en el chat del grupo. Si se filtra, avisa a Lukas para que la rote.

Las vistas previas de Vercel (el enlace que aparece en tu pull request) sirven para revisar **pantallas**, pero llaman a la API de `main` con la base de la demo: no sirven para probar endpoints ni migraciones nuevos.

## 4. Cómo se trabaja (en orden)
La meta del equipo es **3 módulos terminados por semana**. El detalle de roles, tablero y límites está en [`equipo/cadencia.md`](equipo/cadencia.md).

1. **Elige un módulo** libre en [`equipo/asignaciones.md`](equipo/asignaciones.md) y apúntate en el issue del módulo. Un módulo = una persona: así nadie toca los mismos archivos.
2. **Spec primero.** Copia [`specs/_TEMPLATE.md`](specs/_TEMPLATE.md) a `docs/specs/moduloNN-nombre-{backend|frontend}.md`, **sin número global** (varias personas elegían el mismo número; solo las specs transversales llevan número). Usa como modelo las specs 12 y 13 (Proveedores) o 14 y 15 (SolicitudesCompra), pero con las rutas de las cuatro capas: contrato de la API con rutas, DTOs y **mensajes de error literales**, reglas en orden y criterios medibles. Lukas la revisa antes de programar.
3. **Rama propia** desde la de integración: `git switch -c tu-nombre/moduloXX-nombre main`. Si tenías una rama empezada antes de pasar a cuatro capas, adáptala con [`equipo/adaptar-rama-a-capas.md`](equipo/adaptar-rama-a-capas.md).
4. **Programa siguiendo el módulo de referencia**: `Modulo11_Proveedores` (o `Modulo13_SolicitudesCompra` si tu módulo tiene líneas y estados), repartido en cuatro capas (`src/TitanGlobal.Domain/Models/`, `src/TitanGlobal.Infrastructure/Persistence/`, `src/TitanGlobal.Application/DTOs/Modulo11_Proveedores/`, `src/TitanGlobal.Application/UseCases/Modulo11_Proveedores/`, `src/TitanGlobal.Api/Controllers/Modulo11_Proveedores/`); frontend `frontend/src/modules/proveedores/` (o `solicitudes-compra/`). Sigue el paso a paso de [`equipo/crear-modulo.md`](equipo/crear-modulo.md) y las reglas de [`CONTRIBUTING.md`](../CONTRIBUTING.md) — sin `var`, sin `=>` en miembros, UseCases sin interfaz, `CodigosEstado` y `ViolacionRestriccionException` en `Application`, nombres en español, mensajes literales.
5. **Gate antes de entregar:** `./scripts/verify.sh` en verde (build sin warnings, formato, reglas incluidas las fronteras de capas, tests de backend y frontend). Si tocaste base de datos, también `./scripts/smoke-postgres.sh` → `SMOKE OK`.
6. **Pull request contra `main`** con la plantilla del repositorio: qué spec cumple, salida del gate y capturas a 375 y 1280 px si hay pantalla. El revisor asignado lo revisa y Lukas integra y despliega.

## 5. Para no pisarse
- **Migraciones:** son el punto de choque más común (todas tocan `TitanGlobalDbContextModelSnapshot.cs` en `src/TitanGlobal.Infrastructure/Migrations/`). Antes de generar la tuya, actualiza tu rama (`git fetch && git rebase origin/main`) y genera con `dotnet ef migrations add <Nombre> -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api`. **Nunca** edites a mano una migración ni el snapshot; si hay conflicto en el snapshot, borra tu migración, rebasa y vuelve a generarla.
- **Archivos compartidos** (`src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs`, `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh`, `frontend/src/modules/pantallas.ts`, `frontend/src/styles/app.css`): añade tu bloque al final y no reordenes lo de otros.
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
- Crear un módulo en cuatro capas: [`equipo/crear-modulo.md`](equipo/crear-modulo.md)
- Crear la pantalla de un módulo: [`equipo/crear-pantalla.md`](equipo/crear-pantalla.md)
- Adaptar una rama anterior a cuatro capas: [`equipo/adaptar-rama-a-capas.md`](equipo/adaptar-rama-a-capas.md)
- Todas las specs con su evidencia: [`specs/`](specs/)
- Errores que no hay que repetir: [`equipo/lecciones.md`](equipo/lecciones.md)
- Cómo se despliega la demo: [`despliegue.md`](despliegue.md)
