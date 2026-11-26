# Especificación: modulo59-centros-costo-frontend — Centros de costo

> Responsable: Leonel Portillo. Propuesta de interfaz para el API del PR #84. Revisión de Lukas pendiente.

## 1. Objetivo

Registrar, consultar y mantener centros con tres niveles de jerarquía y un responsable opcional.

## 2. Fuera de alcance

Importes, presupuestos, centros de trabajo del 33 y cambios de estado fuera del contrato implementado. Depende del PR #84 y del catálogo FINANZAS del SQL 024 para despliegue.

## 3. Archivos afectados

Pantalla, tipos, API y pruebas en `frontend/src/modules/centros-costo/`. Navegación mediante `pantallas.ts`, estilos mediante `app.css` y reutilización de los componentes del ERP.

## 4. Diseño y lógica

- Listado con filtros por actividad y centro padre, paginación y estado vacío. El detalle muestra ruta, nivel, actividad, descripción y responsable.
- Formulario de alta y edición con código de 2 a 20 caracteres, nombre, descripción opcional, selector de padre y selector de empleado. La selección de padre excluye el propio centro, sus descendientes y candidatos del tercer nivel. La API conserva la validación final de jerarquía e hijos activos.
- Desactivar y reactivar requieren confirmación. La lectura se permite con sesión válida. FINANZAS y ADMIN escriben. La pantalla conserva los mensajes literales 400, 401, 403, 404 y 409.
- Usa las seis rutas de `/api/v1/centros-costo` y consulta el catálogo de empleados de la API existente. La edición de código, padre y operaciones bajo padres inactivos conserva el comportamiento actual mientras Lukas revisa las precisiones. [Backend](modulo59-centros-costo-backend.md)

## 5. Criterios de aceptación

- Filtros, estados, consumo HTTP, permisos, jerarquía y formularios ejercitados con Vitest.
- Carga y acciones con etiquetas accesibles y disposición a 375 y 1280 px en claro y oscuro.
- `verify.sh` y smoke del backend en PostgreSQL desechable.

## 6. Checklist de ejecución

- [x] Conectar la pantalla al backend.
- [ ] Completar registro de verificación.

## 7. Registro de verificación

| Fecha | Evidencia | Resultado |
|---|---|---|

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-26 | Formato, reglas y compilación | Correcto | `git diff --check`, `check-rules.sh` y `dotnet build -warnaserror`, 0 advertencias de .NET |
| 2026-11-26 | Gate local | Correcto | `verify.sh`, 1800 pruebas backend y 1466 frontend, lint, TypeScript y build |
| 2026-11-26 | PostgreSQL | Correcto | Scaffold al día, dos cargas idempotentes del seed y `SMOKE OK` en PostgreSQL desechable |
| 2026-11-26 | Edge con API real | Correcto | 375 y 1280 px en claro y oscuro, listado con datos, foco de la acción principal y apertura del formulario y 0 errores de consola |
