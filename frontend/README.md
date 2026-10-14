# TitanGlobal ERP · Frontend

SPA desarrollada con React, Vite y TypeScript. Para ejecutar la experiencia con la API, inicia el backend en el puerto `5080` y luego ejecuta:

```sh
npm ci
npm run dev
```

Vite expone la aplicación en `http://localhost:5173` y reenvía `/api` a `http://localhost:5080`. Sin la API, la portada informa que no está disponible.

Comandos de calidad:

```sh
npm run lint
npm run typecheck
npm run test
npm run build
npm run verify
```

El catálogo único de navegación contiene los 80 módulos definidos en `docs/arquitectura.md`.
