# Despliegue de demostración

Entorno para ir viendo el avance. No es producción con datos reales: la base tiene los datos de `scripts/seed-dev.sql`.

| Pieza | Servicio | Detalle |
|---|---|---|
| Código | GitHub `ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem` (privado) | rama desplegada: `ImLukzy/Claudio` |
| Base de datos | Neon, proyecto `titanglobal-erp` (aws-us-east-1, PostgreSQL 18) | base `titanglobal` |
| API | Render, servicio web Docker (`Dockerfile` de la raíz) | salud: `/api/v1/sistema/salud` |
| Frontend | Vercel, raíz `frontend/` | `vercel.json` reenvía `/api/*` a la API de Render |

## Secretos
- La cadena de conexión de Neon solo vive en la variable de entorno `ConnectionStrings__DefaultConnection` de Render (formato Npgsql, `SSL Mode=VerifyFull`). Nunca en el repositorio, en `appsettings*.json` ni en la documentación.

## Migraciones y datos
- Render no aplica migraciones. Tras integrar una migración: `dotnet ef database update -p src/TitanGlobal.Api --connection "<cadena de Neon>"` desde la máquina del integrador, y comprobar después con la API desplegada.
- Datos de demostración: `psql "<URI de Neon>" -v ON_ERROR_STOP=1 -f scripts/seed-dev.sql` (idempotente; reinicia los datos de las tablas del enunciado).
