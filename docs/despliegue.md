# Despliegue de demostración

Entorno para ir viendo el avance. No es producción con datos reales: la base tiene los datos de `scripts/seed-dev.sql`.

| Pieza | Servicio | Detalle |
|---|---|---|
| URL pública | https://titanglobal-erp.vercel.app | API detrás de `/api` (Render: https://titanglobal-api.onrender.com) |
| Código | GitHub `ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem` (privado) | rama desplegada: `ImLukzy/Claudio` |
| Base de datos | Neon, proyecto `titanglobal-erp` (aws-us-east-1, PostgreSQL 18) | base `titanglobal` |
| API | Render `titanglobal-api` (`srv-db849hivcj2c73859c9g`, plan free, virginia), Docker con el `Dockerfile` de la raíz, despliegue automático al hacer push a `ImLukzy/Claudio` | salud: `/api/v1/sistema/salud` |
| Frontend | Vercel `titanglobal-erp`, conectado a GitHub con *Root Directory* = `frontend` (desde 2026-10-15): cada push a `main` despliega producción y cada push a otra rama crea una vista previa (con su enlace en el pull request). Producción también se puede desplegar a mano desde la **raíz** del repositorio (`npx vercel deploy --prod`; `.vercelignore` sube solo `frontend/`) | `vercel.json` reenvía `/api/*` a la API de Render |

## Secretos
- La cadena de conexión de Neon solo vive en la variable de entorno `ConnectionStrings__DefaultConnection` de Render (formato Npgsql, `SSL Mode=VerifyFull`). Nunca en el repositorio, en `appsettings*.json` ni en la documentación.

## Migraciones y datos
- Render no aplica migraciones. Tras integrar una migración: `dotnet ef database update -p src/TitanGlobal.Api --connection "<cadena de Neon>"` desde la máquina de Lukas (integración), y comprobar después con la API desplegada.
- Datos de demostración: `psql "<URI de Neon>" -v ON_ERROR_STOP=1 -f scripts/seed-dev.sql` (idempotente; reinicia los datos de las tablas del enunciado).

## Tras cada integración (lo hace Lukas)
1. `./scripts/verify.sh` en verde en `ImLukzy/Claudio`.
2. Si hay migraciones nuevas: aplicarlas a Neon (arriba) antes de subir el código.
3. `git push origin ImLukzy/Claudio` → Render reconstruye la API (comprobar `render deploys list srv-db849hivcj2c73859c9g` hasta `live`).
4. En la **raíz** del repositorio (no en `frontend/`): `npx vercel deploy --prod --yes`.
5. Actualizar `docs/contexto-proyecto.md` (§5, §6, §7, §10, §12), el estado del `README.md` y `docs/equipo/asignaciones.md`.
6. Comprobar `https://titanglobal-erp.vercel.app/api/v1/sistema/salud` → 200 y la pantalla nueva en el navegador.

## Limitaciones conocidas
- Render free se duerme tras ~15 min sin tráfico: la primera petición puede tardar ~1 minuto.
- La demo no tiene autenticación (el enunciado no la pide): cualquiera con la URL puede crear o borrar datos de demostración. Se restauran con el seed.
