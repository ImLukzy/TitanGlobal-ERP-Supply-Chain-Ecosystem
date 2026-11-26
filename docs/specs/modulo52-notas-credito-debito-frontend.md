# Especificación: modulo52-notas-credito-debito-frontend — Notas de crédito y débito

> Responsable: Leonel Portillo. Propuesta basada en el backend del PR #83 y las decisiones del PR #52. Revisión de Lukas pendiente.

## 1. Objetivo

Consultar y registrar notas por línea de factura o boleta emitida, con historial, importes oficiales y las acciones del estado correspondiente.

## 2. Fuera de alcance

Aplicación en cartera del módulo 53, conversiones, sucursales, envío tributario, integraciones automáticas y cambios al backend. La rama de interfaz sigue la del PR #83, que depende del #48.

## 3. Archivos afectados

- `frontend/src/modules/notas-credito-debito/` contiene pantalla, formulario, tipos, API y pruebas.
- `pantallas.ts` registra el módulo 52. `app.css` y los componentes compartidos proporcionan navegación, paginación, etiquetas y disposición adaptable.

## 4. Diseño y lógica

- Listado de folio NC o ND, comprobante, tipo, estado, emisión y total. Filtros backend por comprobante, tipo y estado. Paginación y estados de carga, vacío y error.
- Alta sobre un comprobante emitido de PEN o USD. Elegir líneas de origen sin duplicados, escribir motivo, cantidad y precio según el contrato. Se muestran la moneda heredada y el impuesto histórico. La API calcula la base, el impuesto y el total. Los topes acumulados de crédito se comprueban en la API.
- Editar solo borradores, con tipo, comprobante y folio visibles. Emitir confirma el cambio. Anular una nota emitida exige motivo de 3 a 300 caracteres. El detalle muestra fechas `DateOnly` sin conversión de zona.
- Cualquier sesión válida lee. FINANZAS y ADMIN escriben. Se conservan los literales 400, 401, 403, 404 y 409. Las seis rutas de `/api/v1/notas-credito-debito` y las consultas de `/api/v1/facturas` corresponden al [backend](modulo52-notas-credito-debito-backend.md).
- Se conserva el precio editable que recibe el contrato vigente. El tratamiento definitivo de descuentos, redondeos y campos editables permanece en revisión en el #83.

## 5. Criterios de aceptación

- Rutas, consultas y payloads cubiertos por tests del cliente HTTP.
- Formularios, permisos, filtros, paginación y transiciones comprobados con tests de la pantalla.
- Navegación y disposición comprobables a 375 y 1280 px en claro y oscuro.
- Gate completo y smoke del contrato del 52.

## 6. Checklist de ejecución

- [x] Implementar los flujos del backend existente.
- [ ] Registrar evidencia de la rama y revisión visual.

## 7. Registro de verificación

| Fecha | Evidencia | Resultado |
|---|---|---|

| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-26 | Formato, reglas y compilación | Correcto | `git diff --check`, `check-rules.sh` y `dotnet build -warnaserror`, 0 advertencias de .NET |
| 2026-11-26 | Gate local | Correcto | `verify.sh`, 1869 pruebas backend y 1467 frontend, lint, TypeScript y build |
| 2026-11-26 | PostgreSQL | Correcto | Scaffold al día, dos cargas idempotentes del seed y `SMOKE OK` en PostgreSQL desechable |
| 2026-11-26 | Edge con API real | Correcto | 375 y 1280 px en claro y oscuro, listado con datos, foco de la acción principal y apertura del formulario y 0 errores de consola |
