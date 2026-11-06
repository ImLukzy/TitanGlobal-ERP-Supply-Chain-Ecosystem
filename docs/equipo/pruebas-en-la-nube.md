# Pruebas con base de datos en la nube (Neon)

Para desarrollar en el día a día basta con la PostgreSQL local de tu máquina ([`base-de-datos-local.md`](base-de-datos-local.md)). Conectarse a la nube es **totalmente opcional**: sirve si necesitas mostrar avances desde otra máquina sin Docker o si quieres probar tus cambios con una copia real de los datos de la demo.

## Tu rama personal en Neon

Lukas crea para cada persona del equipo una **rama propia de Neon**, llamada `dev-<nombre>` (por ejemplo, `dev-leonel` o `dev-antony`).

- **Es una copia aislada:** se genera a partir de la base de datos de la demo y contiene las mismas tablas y datos del seed.
- **Contraseña propia:** el usuario es `titanglobal`, pero la contraseña es solo de tu rama y no sirve en producción.
- **No necesitas cuenta en Neon:** no tienes que registrarte en la plataforma ni acceder a su consola web.
- **Libertad para probar:** puedes insertar, modificar o borrar registros y probar scripts de base de datos sin afectar a nadie más.

## La cadena de conexión es un secreto

Lukas te enviará la cadena de conexión de tu rama por **mensaje privado**.

- **Nunca la subas al repositorio:** no la escribas en `appsettings.json`, `appsettings.Development.json`, scripts ni archivos markdown.
- **Con herramientas automáticas o asistentes:** nunca pegues la cadena en una ventana de chat ni se la facilites a ningún asistente. Si la pegas en un cuadro de diálogo, saldría de tu máquina hacia servidores externos.
- **Define la variable tú mismo en tu terminal:** nunca pidas a una herramienta que configure la variable ni ejecutes comandos que impriman las variables de entorno en la consola, como `env`, `printenv` o `Get-ChildItem Env:`.
- **Comprobación antes de cada commit:** revisa siempre con `git status` y `git diff` que ningún archivo contenga la cadena o contraseñas antes de confirmar cambios.
- **Si la cadena se filtra:** avisa inmediatamente a Lukas para que regenere la contraseña o vuelva a crear la rama con `scripts/neon-rama.sh`.

---

## Cómo usar tu rama de Neon

No edites `src/TitanGlobal.Api/appsettings.json` (es un archivo compartido). En su lugar, define la cadena en la variable de entorno `ConnectionStrings__DefaultConnection` en tu terminal:

### 1. Definir la variable en tu terminal

En **macOS, Linux o Git Bash**:
```sh
export ConnectionStrings__DefaultConnection="Host=ep-tu-rama.aws.neon.tech;Database=titanglobal;Username=titanglobal;Password=tu_password_secreto;SSL Mode=VerifyFull"
```

En **Windows PowerShell**:
```powershell
$env:ConnectionStrings__DefaultConnection = "Host=ep-tu-rama.aws.neon.tech;Database=titanglobal;Username=titanglobal;Password=tu_password_secreto;SSL Mode=VerifyFull"
```

La API y las herramientas de Entity Framework leerán automáticamente esa variable mientras mantengas abierta esa sesión de terminal.

### 2. Aplicar scripts y arrancar la API

Si tu rama de código tiene scripts nuevos en `database/`, aplícalos a tu rama de Neon con `psql`, en orden y de uno en uno (`psql` te pide la contraseña; no la escribas en el comando):
```sh
psql "host=<host de tu rama> dbname=<base> user=<usuario> sslmode=require" -v ON_ERROR_STOP=1 -f database/NNN-descripcion.sql
```
Las entidades se generan siempre contra tu base local (`./scripts/scaffold.sh`), no contra Neon.

Arranca la API localmente en el puerto 5080:
```sh
dotnet run --project src/TitanGlobal.Api --urls http://localhost:5080
```

### 3. Usar el frontend local

El servidor de desarrollo del frontend (`npm --prefix frontend run dev` en el puerto 5173) no necesita ningún cambio: el proxy de Vite redirige `/api` a `http://localhost:5080`, por lo que tu interfaz consumirá automáticamente la API conectada a tu rama de Neon.

### 4. Comprobar que funciona

- Abre `http://localhost:5080/api/v1/sistema/salud` → debe responder `{"estado":"OK"}`.
- Consulta un endpoint con datos (por ejemplo, `http://localhost:5080/api/v1/clientes`) para confirmar la lectura de datos de tu rama.

---

## Cómo volver a la base de datos local

Para desconectarte de Neon y volver a usar tu PostgreSQL local (`localhost:5433`):

- **Opción rápida:** cierra la ventana de la terminal donde definiste la variable.
- **O borra la variable de entorno en la terminal activa:**
  - En Git Bash / Linux / macOS: `unset ConnectionStrings__DefaultConnection`
  - En Windows PowerShell: `$env:ConnectionStrings__DefaultConnection = $null`

La próxima vez que ejecutes `dotnet run`, la API volverá a tomar la cadena de desarrollo local definida en `appsettings.json`.

---

## Qué debes tener en cuenta

1. **Tu rama no es producción:**
   Aplicar un script o modificar datos en tu rama `dev-<nombre>` solo afecta a tu entorno. A la base de datos de producción (la demo desplegada) los scripts de `database/` los aplica únicamente Lukas al integrar tu pull request en `main`.
2. **Las vistas previas de Vercel no usan tu rama:**
   Las URLs de preview que genera Vercel en los pull requests son solo para revisar el diseño y comportamiento visual del frontend. Están conectadas a la API central de la demo, nunca a tu rama personal de Neon.
3. **Si tu rama se desfasó o se dañó:**
   Tu rama de Neon no se actualiza sola cuando otros compañeros integran cambios en la demo. Si tu rama se corrompe o queda muy desfasada respecto a `main`, pídele a Lukas que la vuelva a crear con `./scripts/neon-rama.sh <tu-nombre>`. Esto creará una copia limpia de la demo actual y generará una nueva cadena de conexión. Tus datos de prueba en la rama anterior se perderán, pero tu código en Git estará seguro.
4. **Límites del plan gratuito de Neon:**
   - **Suspensión por inactividad (cold start):** tras un rato sin recibir consultas, la rama se suspende. La primera petición tardará unos segundos más mientras se reactiva.
   - **Límite de tamaño:** cada rama tiene un límite de almacenamiento de 1 GB. No cargues archivos binarios ni ejecutes scripts con bucles infinitos de inserción.

---

## Si algo falla

| Síntoma | Causa probable | Solución |
|---|---|---|
| La primera petición tarda más de lo normal o da un timeout | Cómputo de Neon suspendido por inactividad | Espera unos segundos y recarga. |
| `password authentication failed for user "titanglobal"` | Contraseña errónea o la rama fue recreada | Comprueba que no falten caracteres al copiar la cadena. Si Lukas recreó la rama, pídele la nueva cadena. |
| La API sigue conectándose a `localhost:5433` | La variable de entorno se definió en otra terminal | Define `ConnectionStrings__DefaultConnection` en la misma terminal exacta donde ejecutas `dotnet run`. |
| `relation "..." does not exist` | Faltan tablas en tu rama de Neon | Aplica los scripts de `database/` a tu rama de Neon con `psql` (paso 2). |
| Error SSL / `Authentication failed because the remote party sent a TLS alert` | Cadena sin parámetros SSL adecuados | Asegúrate de que la cadena termine con `;SSL Mode=VerifyFull` tal como te la envió Lukas. |
| Error de sintaxis en PowerShell al asignar la variable | Faltan comillas dobles alrededor de la cadena | En PowerShell usa: `$env:ConnectionStrings__DefaultConnection = "Host=..."` (con comillas dobles obligatorias). |
| Faltan columnas o tablas nuevas que un compañero ya integró en `main` | Tu rama de Neon se creó antes de ese script | Aplica a tu rama los scripts nuevos de `database/` con `psql`, o pide a Lukas que recree tu rama si necesitas refrescar los datos. |
| La variable sigue activa tras querer volver a local | La terminal conserva el valor exportado | Ejecuta `unset ConnectionStrings__DefaultConnection` o abre una terminal nueva limpia. |
