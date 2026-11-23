# Evidencia local — pantalla 41

Revisión del 2026-11-21 en Edge contra la API .NET y PostgreSQL 16 locales, con los seis vehículos de `scripts/seed-dev.sql`: cinco activos, uno inactivo y 44,050.00 kg de capacidad total. Las capturas anteriores con respuestas simuladas se sustituyeron por evidencia con datos reales. No se utilizó producción.

| Vista | 375 px claro | 375 px oscuro | 1280 px claro | 1280 px oscuro |
|---|---|---|---|---|
| Detalle | [captura](375-light-detalle.png) | [captura](375-dark-detalle.png) | [captura](1280-light-detalle.png) | [captura](1280-dark-detalle.png) |
| Formulario | [captura](375-light-formulario.png) | [captura](375-dark-formulario.png) | [captura](1280-light-formulario.png) | [captura](1280-dark-formulario.png) |
| Confirmación de baja | [captura](375-light-baja.png) | [captura](375-dark-baja.png) | [captura](1280-light-baja.png) | [captura](1280-dark-baja.png) |

`resultado-visual.json` registra ausencia de desbordamiento horizontal de la página y de errores JavaScript en los cuatro escenarios. La tabla mantiene su región de desplazamiento horizontal accesible en móvil. El formulario muestra el conflicto real 409 al intentar registrar `abc-123`, conserva lo escrito y el diálogo devuelve el foco al cancelar con Escape.

## Verificación funcional

- `./scripts/verify.sh`: aprobado, con 1090 pruebas de backend y 582 de frontend (73 archivos). Compilación .NET con cero errores y cero advertencias; lint, TypeScript y build correctos. Vite mantiene el aviso existente de bundle superior a 500 kB; no se ocultó ni se cambió la configuración compartida.
- 39 pruebas frontend del módulo: contrato HTTP, filtros, paginación, detalle, respuestas obsoletas, validación, alta, edición, errores y baja accesible. Siete casos backend nuevos para unicidad sin distinguir mayúsculas.
- `./scripts/aplicar-bd.sh --reiniciar`: scripts 001, 008 y 010 aplicados en PostgreSQL local; `./scripts/scaffold.sh` ejecutado, sin cambios de modelo.
- `./scripts/smoke-postgres.sh`: `SCAFFOLD AL DÍA` y `SMOKE OK`. Además de los casos HTTP, se comprobó una escritura SQL directa con mayúsculas distintas: PostgreSQL la rechaza con SQLSTATE 23505 por `UX_VehiculosFlota_PlacaNormalizada`.
- Navegador real: inicio de sesión, detalle y conflicto de placa en los cuatro escenarios; alta, selección, edición y baja de un vehículo temporal en 1280 px claro. El vehículo temporal se eliminó y el resumen volvió a los seis vehículos del seed.
- OpenAPI local comprobado: las siete operaciones del módulo están documentadas (listado/alta, detalle/edición/baja, tipos y resumen).

## Entorno local

Se instaló Docker Desktop y se actualizó .NET a SDK 10.0.404/runtime 10.0.15. La instalación anterior de .NET tenía vacía la DLL de serialización que impedía compilar las pruebas. Se usó Git Bash con Python y Node 24 locales. Para la comprobación de puertos, Git Bash consultó las escuchas reales de Windows porque no incluye `ss`; no se omitió esa comprobación. El checkout local de arquitectura se normalizó a LF sin diferencias de contenido en Git y sin modificar pruebas ajenas.

La implementación y las verificaciones locales están completas. Estado de las ampliaciones: propuesta, pendiente de aprobación de Lukas; revisión, integración y despliegue pendientes.

## Preparación para integración — 2026-11-22

La rama se actualizó mediante `git pull origin main`. El único conflicto fue `relevo.md`: se conservó la tabla oficial y solo se actualizó la fila 021 y la línea de Kelvin. Se renombró el script, sin cambiar su contenido, a `database/021-vehiculos-flota-placa-unica.sql`. Las menciones a 010 en las verificaciones del 21 corresponden al nombre usado en esa fecha; el número vigente del módulo 41 es 021.

La base local se recreó con `aplicar-bd.sh --reiniciar`, se cargó el seed y se regeneraron los modelos con `scaffold.sh`, sin edición manual ni diferencias de contenido en los archivos generados. No se cambió la pantalla ni se generaron nuevas capturas para este ajuste documental y de numeración.

El primer gate posterior a la actualización pasó compilación, formato, reglas, 1.090 pruebas backend, lint y tipos. Vitest ejecutó correctamente 577 pruebas de 72 archivos, pero falló al arrancar el trabajador de `SolicitudesFormulario.test.tsx` por `Timeout waiting for worker to respond`; no se acreditó ese gate como correcto. Se repitió con `VITEST_MAX_WORKERS=1`, únicamente en el entorno temporal, sin ampliar timeouts ni modificar pruebas/configuración compartida.

El reintento de `./scripts/verify.sh` terminó con salida 0: 1.090 pruebas backend y 582 frontend en 73 archivos, sin fallos ni omisiones. Formato, reglas/capas, lint y tipos correctos, compilación .NET con cero errores y advertencias. La suite frontend tardó 286.61s y terminó completa; build correcto con JavaScript710.18kB y CSS137.91kB antes de gzip. Persisten los avisos globales de ESLint9 obsoleto y bundle de Vite mayor de500kB; no se cambiaron dependencias ni umbrales.

Después del gate se ejecutó `./scripts/smoke-postgres.sh`: salida 0, `SCAFFOLD AL DÍA` y `SMOKE OK`, con los scripts001/008/021. Pasaron las comprobaciones reales del 41, incluidos duplicados normalizados en POST/PUT y modificación de la propia placa. El script cerró la API y eliminó la base temporal al finalizar. Main remoto seguía incorporado en la rama al comprobarlo antes de publicar; no se hizo push directo a main ni se integraron otras ramas de Kelvin.

## Resolución del conflicto de estilos — 2026-11-23

Actualización con `git pull origin main`, incorporando el merge de Roles/Matriz de permisos de Sergio. El único conflicto fue `frontend/src/styles/app.css`: se conservaron íntegros los dos bloques, primero los estilos de main y después Vehículos de flota. Frente a main solo se añaden las48 líneas originales del41; no se alteran los selectores ni valores de ninguno de los bloques. Resolución guardada en el merge commit1917efc a nombre de Kelvin.

El primer gate se detuvo por finales de línea de la copia local y no se acredita como correcto. Se recreó PostgreSQL local, se cargó el seed y se ejecutó `scaffold.sh`, que regenera y formatea los archivos mediante el flujo oficial. No hubo diferencias de contenido en modelos, código ni pruebas, ni se editaron archivos generados a mano. Se normalizó el checkout local de arquitectura a LF, sin cambios de contenido.

El gate completo posterior terminó con salida0: 1.090 pruebas backend y619frontend en77archivos, sin fallos ni omisiones. Compilación .NET con cero errores y advertencias; formato, reglas/capas, lint, tipos y build correctos. JavaScript721.13kB y CSS142.02kB antes de gzip. `VITEST_MAX_WORKERS=1` fue temporal; no se cambiaron pruebas, timeouts ni configuración compartida. Persisten los avisos globales de ESLint9 obsoleto y bundle de Vite superior a500kB, sin ocultarlos ni cambiar umbrales.

Después se ejecutó `./scripts/smoke-postgres.sh`: salida0, `SCAFFOLD AL DÍA` y `SMOKE OK`, con PostgreSQL real local y el script021. El smoke cerró su API y eliminó la base temporal. No se cambió la pantalla ni se generaron nuevas capturas para esta resolución que conserva íntegros los estilos existentes. La aprobación de la spec, la aplicación de021 en la demo y la integración siguen a cargo de Lukas; no se declara ninguna realizada.
