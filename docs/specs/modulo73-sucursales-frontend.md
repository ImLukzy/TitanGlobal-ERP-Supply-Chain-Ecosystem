# Propuesta: módulo 73 — Sucursales (frontend)

## 1. Objetivo
Pantalla Kelvin `/modulos/73-sucursales` para administrar filiales, plantas y oficinas, sus contactos y estado activo/inactivo. Propuesta local autorizada; aprobación de Lukas pendiente.

## 2. Fuera de alcance
Mismo alcance backend: maestro plano, sin jerarquía, mapas, permisos por sucursal ni cambios en módulos ajenos. Referencias geográficas/contactos manuales, sin abrir enlaces de datos ingresados. No inferir actividad por apertura ni activar/desactivar mediante el formulario de edición.

## 3. Archivos
`frontend/src/modules/sucursales/`: tipos, api/api.test, validacion/validacion.test, presentacion, useSucursalesData, SucursalesPage/Resumen/Filtros/Tabla, SucursalDetallePanel/Formulario, DialogoEstadoSucursal, sucursales.testUtils y pruebas temáticas SucursalesConsultas/Formulario/Estados. Registro propio en pantallas.ts; bloque `.sucursales-` al final de app.css; evidencia propia en docs/evidencias/modulo73/.

## 4. Diseño y lógica
PaginaModulo, TablaPaginada, PanelDetalle, DialogoBaja y CampoConError; tokens y controles comunes. Seis métricas total/activas/inactivas/filiales/plantas/oficinas con cifra grande y etiqueta debajo. Búsqueda y país con debounce300ms, filtros tipo/estado y páginas10; reinicio al filtrar, corrección tras baja, cargas iniciales únicas, respuestas antiguas descartadas y reintentos independientes.

Tabla código/nombre, tipo, país/ciudad y estado; detalle lateral a1280/apilado a375 con datos completos, apertura opcional (No registrada) y fechas UTC de registro/último cambio. Botones editar siempre, activar o desactivar según estado; eliminar solo inactiva. Confirmación accesible de cambio de estado con información de conservación de datos, y DialogoBaja para eliminación física.

Formulario validado con mensajes backend, dirección/observaciones en textarea, contactos opcionales vacíos→null y email con el mismo patrón básico. Fecha de apertura vacía→null, sin convertirla en hoy ni perder UTC/milisegundos; label explícita UTC. Conservar datos400/409, enfocar primer error, aria-invalid/describedby, bloqueo sincrónico de doble envío y cierre mientras se guarda. Foco inicial, Tab/Shift+Tab atrapados, Escape sin mutar y retorno al disparador; mantener foco dentro incluso cuando todos los controles se deshabilitan.

375/1280 claro/oscuro, margen móvil≥16px, códigos/fechas sin partir, tabla con scroll interno, sin desbordamiento de página y selección uniforme. Medir textos/enlaces≥4.5:1 y comparar escritorio con Contratos en ambos temas. CSS solo propio.

## 5. Aceptación
A1 contratos HTTP exactos de ocho operaciones, cambios de estado sin cuerpo, filtros vacíos omitidos y false preservado. A2 debounce con reloj falso, carga controlada, filtros/paginación/respuestas obsoletas. A3 límites/email/apertura null yUTC. A4 alta/edición en ambos estados, confirmaciones, errores, foco, doble envío y baja. A5 gate, diez repeticiones propias y veinte con build simultáneo sin ampliar timeouts. A6 QA realAPI/PostgreSQL375/1280claro/oscuro, capturas detalle/formulario/estado/baja, comparación integrada y controles responsive.

## 6. Checklist
- [ ] Aprobación de Lukas.
- [ ] Backend y datos locales por scripts.
- [ ] Pantalla y pruebas.
- [ ] Gate, estabilidad y QA con evidencia.

## 7. Registro
2026-11-22: propuesta local autorizada; sin pruebas ni QA acreditados todavía. Aprobación formal pendiente.
