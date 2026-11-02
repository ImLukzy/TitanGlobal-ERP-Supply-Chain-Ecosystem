# Base de datos en tu máquina

Cada persona trabaja con **su propia** PostgreSQL local. Nadie necesita acceso a Neon (la base de la demo) para desarrollar: tu base local tiene las mismas tablas (las crean las migraciones) y los mismos datos de ejemplo (`scripts/seed-dev.sql`). Si la rompes, la borras y la vuelves a crear en un minuto.

| Dato | Valor |
|---|---|
| Servidor | `localhost`, puerto **5433** |
| Base | `titanglobal` |
| Usuario / contraseña | `titanglobal` / `titanglobal_dev` (solo para tu máquina) |

Esos valores ya están en `src/TitanGlobal.Api/appsettings.json`, así que con la opción A no tienes que configurar nada.

## Opción A (recomendada): Docker

Requisitos: Docker Desktop abierto (en Windows, con WSL 2), .NET SDK 10.

**1. Levantar PostgreSQL** (desde la raíz del repositorio):
```sh
docker compose up -d db
docker compose ps            # db debe salir "healthy"
```

**2. Crear las tablas** con las migraciones:
```sh
dotnet tool restore
dotnet ef database update -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api
```

**3. Cargar los datos de ejemplo:**

macOS, Linux o Git Bash:
```sh
docker compose exec -T db psql -U titanglobal -d titanglobal -v ON_ERROR_STOP=1 -f - < scripts/seed-dev.sql
```
Windows PowerShell (no admite `<`):
```powershell
Get-Content scripts/seed-dev.sql -Raw | docker compose exec -T db psql -U titanglobal -d titanglobal -v ON_ERROR_STOP=1 -f -
```

**4. Comprobar:**
```sh
dotnet run --project src/TitanGlobal.Api --urls http://localhost:5080
```
Abre http://localhost:5080/api/v1/sistema/salud → `{"estado":"OK"}` y http://localhost:5080/api/v1/clientes → lista de clientes.

### Día a día
| Quiero… | Comando |
|---|---|
| Parar la base (los datos se conservan) | `docker compose stop db` |
| Volver a arrancarla | `docker compose up -d db` |
| Restaurar los datos de ejemplo | repetir el paso 3 |
| Aplicar migraciones nuevas tras `git pull` | `dotnet ef database update -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api` |
| **Borrar todo y empezar de cero** | `docker compose down -v` y repetir los pasos 1 a 3 |
| Entrar a la consola SQL | `docker compose exec db psql -U titanglobal -d titanglobal` |

Para verla con un cliente gráfico (DBeaver, pgAdmin, DataGrip, extensión de VS Code): host `localhost`, puerto `5433`, base `titanglobal`, usuario `titanglobal`, contraseña `titanglobal_dev`.

## Opción B: PostgreSQL instalado sin Docker

Solo si tu máquina no puede usar Docker. Instala PostgreSQL 16 o superior (https://www.postgresql.org/download/). El instalador suele usar el puerto **5432**.

**1. Crear el usuario y la base** (con el usuario `postgres` que creaste al instalar):
```sh
psql -U postgres -h localhost -p 5432 -c "CREATE ROLE titanglobal LOGIN PASSWORD 'titanglobal_dev';"
psql -U postgres -h localhost -p 5432 -c "CREATE DATABASE titanglobal OWNER titanglobal;"
```

**2. Indicar tu puerto.** No edites `appsettings.json` (es compartido). Define la cadena en una variable de entorno de tu terminal:

macOS, Linux o Git Bash:
```sh
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=titanglobal;Username=titanglobal;Password=titanglobal_dev"
```
Windows PowerShell:
```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=titanglobal;Username=titanglobal;Password=titanglobal_dev"
```
La API y `dotnet ef` la leen automáticamente mientras esa terminal siga abierta.

**3. Migraciones y datos:**
```sh
dotnet tool restore
dotnet ef database update -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api
psql -U titanglobal -h localhost -p 5432 -d titanglobal -v ON_ERROR_STOP=1 -f scripts/seed-dev.sql
```

**4. Comprobar** igual que en la opción A (en la misma terminal donde definiste la variable).

Para empezar de cero: `psql -U postgres -h localhost -p 5432 -c "DROP DATABASE titanglobal WITH (FORCE);"` y repetir desde el paso 1 (el `CREATE ROLE` dará error de que ya existe: es normal).

## Si algo falla

| Síntoma | Causa y solución |
|---|---|
| `port is already allocated` o `address already in use` al hacer `docker compose up` | Otro programa usa el 5433. Ciérralo, o comprueba con `docker ps` que no tengas otro contenedor de PostgreSQL levantado. |
| `Failed to connect to 127.0.0.1:5433` / `Connection refused` | La base no está arrancada: `docker compose up -d db` y espera a que salga `healthy`. |
| `password authentication failed for user "titanglobal"` | Se creó el volumen con otra contraseña. `docker compose down -v` y repetir los pasos. |
| `relation "Clientes" does not exist` al cargar el seed | Faltan las migraciones: haz el paso 2 antes del 3. |
| `The term '<' ...` o `operador '<' reservado` en PowerShell | Usa la versión de PowerShell del paso 3. |
| `dotnet ef` no se reconoce | Ejecuta `dotnet tool restore` en la raíz del repositorio. |
| Tras `git pull` la API falla con columnas o tablas que no existen | Hay migraciones nuevas: `dotnet ef database update -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api`. |
| Docker Desktop no arranca en Windows | Activa WSL 2 (`wsl --install` en PowerShell como administrador) y reinicia. Si no se puede, usa la opción B. |

## Reglas
- Nunca subas al repositorio una cadena que no sea la de desarrollo local (ni de Neon, ni con otra contraseña).
- Si creas una migración, genera con `dotnet ef migrations add <Nombre> -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api` y aplícala **solo a tu base local**. A Neon las aplica Lukas al integrar.
- Si tu módulo necesita datos de ejemplo, añádelos **al final** de `scripts/seed-dev.sql` en tu pull request, para que todos los tengan.
