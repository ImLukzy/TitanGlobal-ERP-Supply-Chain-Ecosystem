# Especificación: modulo55-tesoreria-bancos-frontend — Tesorería y bancos

> Responsable: Leonel Portillo. Propuesta de interfaz para el PR #85. Revisión de Lukas pendiente.

## 1. Objetivo

Mantener las cuentas bancarias y operar ingresos, egresos y transferencias, consultando los saldos e historial que calcula la API.

## 2. Fuera de alcance

Cheques, conciliación persistida del 56, conversión de monedas, integración bancaria y agregación de saldos PEN y USD. El saldo de apertura y la reversión completa de transferencias siguen pendientes. La pantalla depende del PR #85 y del rol provisto por el SQL 024.

## 3. Archivos afectados

Pantalla, tipos, cliente y pruebas en `frontend/src/modules/tesoreria-bancos/`. Registro en `pantallas.ts`, estilos adaptables en `app.css` y utilidades de consulta, fecha y sesión del frontend.

## 4. Diseño y lógica

- Listado paginado por moneda y tipo de cuenta. Alta y edición con banco, número textual, nombre, Corriente o Ahorros, PEN o USD. La edición consulta el historial vigente y mantiene la moneda fija desde el primer movimiento.
- Detalle con saldo de la API, historial paginado, filtros de tipo y rango de fechas, referencias, transferencias asociadas y estado de reversión.
- Ingreso y egreso reciben importe positivo, fecha de Perú y referencia opcional. Transferencia selecciona destino distinto en la misma moneda. La API protege saldo, atomicidad y concurrencia.
- La reversión exige motivo y se ofrece para movimientos registrados fuera de una transferencia. El servidor conserva la guarda provisional de conciliación. Las fechas `DateOnly` se leen sin conversiones, con hoy de Perú como valor inicial para altas.
- Las nueve rutas de `/api/v1/tesoreria-bancos` corresponden al [backend](modulo55-tesoreria-bancos-backend.md). Cualquier sesión válida lee. FINANZAS y ADMIN escriben. Los errores 400, 401, 403, 404 y 409 exponen el mensaje del servidor.

## 5. Criterios de aceptación

- Listado, filtros, carga, vacío, contratos, permisos y operaciones cubiertos por Vitest.
- Validación del día de Perú a las 05:00 UTC, pantalla a 375 y 1280 px en claro y oscuro.
- Gate y smoke del backend de la rama.

## 6. Checklist de ejecución

- [x] Conectar cuentas, movimientos y transferencias al API real.
- [ ] Registrar los resultados de la rama.

## 7. Registro de verificación

| Fecha | Evidencia | Resultado |
|---|---|---|

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-26 | Formato, reglas y compilación | Correcto | `git diff --check`, `check-rules.sh` y `dotnet build -warnaserror`, 0 advertencias de .NET |
| 2026-11-26 | Gate local | Correcto | `verify.sh`, 1811 pruebas backend y 1473 frontend, lint, TypeScript y build |
| 2026-11-26 | PostgreSQL | Correcto | Scaffold al día, dos cargas idempotentes del seed y `SMOKE OK` en PostgreSQL desechable |
| 2026-11-26 | Edge con API real | Correcto | 375 y 1280 px en claro y oscuro, listado con datos, foco de la acción principal y apertura del formulario y 0 errores de consola |
