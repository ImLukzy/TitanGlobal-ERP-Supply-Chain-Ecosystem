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

La implementación y las verificaciones locales están completas. La autorización de Kelvin no se presenta como aprobación de Lukas: quedan pendientes la revisión, el PR, la integración y el despliegue.
