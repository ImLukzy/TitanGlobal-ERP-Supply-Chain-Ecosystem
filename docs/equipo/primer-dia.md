# Primer día

Checklist para dejar el proyecto funcionando en tu máquina. Si algo falla, avisa a Lukas con el comando exacto y el mensaje de error.

## 1. Accesos

- [ ] Lukas te invitó al repositorio privado de GitHub.
- [ ] Configuraste tu identidad de git:
  ```sh
  git config --global user.name "Tu Nombre"
  git config --global user.email "tu-correo@ejemplo.com"
  ```
- [ ] No necesitas cuenta de Neon, Render ni Vercel para desarrollar. Todo funciona en local.

## 2. Instalar

- [ ] .NET SDK 10 (`dotnet --version` → 10.x).
- [ ] Node.js 22 LTS (`nvm use` lee el `.nvmrc` del repositorio).
- [ ] Docker con Docker Compose.
- [ ] Git.
- [ ] Python 3 (`python3 --version`): lo usan el gate y `scripts/adaptar-rama-capas.py`.

**En Windows** ejecuta los scripts `.sh` y el script de Python desde **Git Bash**, no desde PowerShell, que no puede ejecutar `./scripts/verify.sh`. Si `python3` no existe en Git Bash, comprueba `python --version` y avisa a Lukas con el error.

## 3. Clonar y levantar

```sh
git clone https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem.git
cd TitanGlobal-ERP-Supply-Chain-Ecosystem
git switch main

docker compose up -d db
dotnet tool restore
./scripts/aplicar-bd.sh
docker compose exec -T db psql -U titanglobal -d titanglobal -f - < scripts/seed-dev.sql
```

En dos terminales:

```sh
dotnet run --project src/TitanGlobal.Api --urls http://localhost:5080
npm --prefix frontend ci
npm --prefix frontend run dev
```

Puertos: API `5080`, frontend `5173`, PostgreSQL `5433`.

¿Windows con PowerShell, sin Docker o algún error con la base? Sigue [`base-de-datos-local.md`](base-de-datos-local.md).

## 4. Comprobar

- [ ] `http://localhost:5080/api/v1/sistema/salud` → `{"estado":"OK"}`.
- [ ] `http://localhost:5080/swagger` abre Swagger UI en desarrollo (el documento OpenAPI sigue en `/openapi/v1.json`).
- [ ] `http://localhost:5173/modulos/01-clientes` muestra clientes.
- [ ] `./scripts/verify.sh` termina en verde.
- [ ] `./scripts/smoke-postgres.sh` termina con `SMOKE OK` (solo si tocas base de datos).

## 5. Leer

- [ ] [`CONTRIBUTING.md`](../../CONTRIBUTING.md): reglas de código y flujo de un módulo.
- [ ] [`docs/equipo/crear-modulo.md`](crear-modulo.md): paso a paso para crear un módulo backend en las cuatro capas (`Domain`, `Application`, `Infrastructure`, `Api`).
- [ ] [`docs/equipo/crear-pantalla.md`](crear-pantalla.md): paso a paso para crear la pantalla React de un módulo.
- [ ] [`AGENTS.md`](../../AGENTS.md): resumen de reglas y comandos. Si usas una herramienta automática, esto es lo primero que debe leer.
- [ ] [`docs/equipo/pruebas-en-la-nube.md`](pruebas-en-la-nube.md): solo si necesitas una base en la nube.
- [ ] [`docs/equipo/adaptar-rama-a-capas.md`](adaptar-rama-a-capas.md): solo si ya tenías una rama empezada antes de la migración a cuatro capas.
- [ ] [`docs/equipo/cadencia.md`](cadencia.md): cómo se trabaja cada semana y la meta de 3 módulos.
- [ ] [`docs/contexto-proyecto.md`](../contexto-proyecto.md): qué está hecho y qué falta.
- [ ] [`docs/enunciado.md`](../enunciado.md): lo que se evalúa.
- [ ] [`docs/arquitectura.md`](../arquitectura.md): los 80 módulos y las decisiones.
- [ ] [`docs/equipo/asignaciones.md`](asignaciones.md): tu bloque de módulos.
- [ ] [`docs/equipo/lecciones.md`](lecciones.md): errores que no hay que repetir.
- [ ] La spec de tu primer módulo en [`docs/specs/`](../specs/).

## 6. Tu primer módulo

- [ ] Confirma con Lukas el módulo y la semana.
- [ ] Crea el issue con la plantilla del repositorio y asígnate.
- [ ] Escribe la spec con [`docs/specs/_TEMPLATE.md`](../specs/_TEMPLATE.md) en `docs/specs/moduloNN-nombre-{backend|frontend}.md` (sin número global) y pide revisión.
- [ ] Crea la rama desde `main` y empieza por el backend o por la pantalla, según la spec.
- [ ] Antes de pedir revisión, repasa la lista de [`CONTRIBUTING.md`](../../CONTRIBUTING.md) §6.

## 7. Si algo no funciona

- Revisa que Docker esté corriendo y que los puertos `5080`, `5173` y `5433` estén libres.
- No ejecutes `smoke-postgres.sh` a la vez que otra persona: usa puertos fijos y se cruzan.
- Pregunta en el canal con el comando, el error y lo que ya probaste. No pierdas medio día atascado.
