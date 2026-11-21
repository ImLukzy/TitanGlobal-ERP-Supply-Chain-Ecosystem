# Especificación propuesta: módulo 43 — Rutas de despacho (frontend)

## 1. Objetivo

Sustituir la pantalla pendiente de `/modulos/43-rutas-despacho` por el maestro de rutas: zonas, distancias y peajes estimados, con alta, consulta, edición, desactivación y baja. Responsable: Kelvin Pinto.

**Estado: implementado y verificado localmente por autorización de Kelvin; revisión de Lukas y aprobación del contrato backend pendientes.** Se ejecutaron pruebas y QA real siguiendo `docs/equipo/crear-pantalla.md`; la evidencia registra el aviso previo de tamaño del paquete compartido de Vite.

## 2. Fuera de alcance y decisiones pendientes

Aplican las cuatro decisiones de `modulo43-rutas-despacho-backend.md` §2. No construir mapas, viajes programados, asignaciones de personal/flota, tarifas o conversiones. No modificar pantallas ajenas, componentes compartidos, autenticación ni dependencias.

Las cifras de peaje son estimaciones manuales por recorrido y moneda; no se presentan como pagos realizados ni se suman monedas diferentes. La implementación local autorizada no sustituye la revisión del contrato exigida para integrarlo.

## 3. Archivos afectados

| Archivo | Acción después de aprobar |
|---|---|
| `frontend/src/modules/rutas-despacho/tipos.ts`, `api.ts` | contratos y cliente de las siete operaciones |
| `RutasDespachoPage.tsx`, `useRutasDespachoData.ts` | composición y sincronización de listado/detalle/resumen/zonas |
| `RutasResumen.tsx`, `RutasFiltros.tsx`, `RutasTabla.tsx` | resumen, búsqueda, filtros y tabla |
| `RutaDetallePanel.tsx`, `RutaFormulario.tsx` | detalle y formulario |
| `validacion.ts`, `presentacion.ts` | reglas de formulario y decimales sin pérdida |
| pruebas y utilidades dentro de esa carpeta | cliente, validación, página y formulario en archivos temáticos |
| `frontend/src/modules/pantallas.ts` | solo registrar `43-rutas-despacho` |
| `frontend/src/styles/app.css` | bloque propio con prefijo `.rutas-despacho-`, sin cambiar selectores compartidos |
| `docs/evidencias/modulo43/` | crear únicamente al ejecutar QA real |

## 4. Diseño y lógica propuestos

- Usar `PaginaModulo`, `TablaPaginada`, `PanelDetalle`, `DialogoBaja` y `CampoConError`, y variables semánticas de `tokens.css`.
- Resumen global de rutas/activas/inactivas y distribución por zona; cantidades grandes y etiquetas descriptivas. Sin suma de peajes de distintas monedas ni total de distancia presentado como recorrido.
- Filtros: búsqueda por código/nombre/origen/destino con espera de 300 ms; selector de zona desde `/zonas`; selector Todos/Activas/Inactivas. Conservar `activo=false`, omitir filtros vacíos, volver a página 1 al cambiar filtros y tamaño de página 10.
- Tabla: código/nombre, zona, origen/destino, distancia en km, peaje estimado con moneda, estado y selección de detalle. Orden/paginación del servidor; no descargar todos los registros para filtrar en el navegador.
- Detalle con todos los campos y observaciones. Acciones Editar y Eliminar; la desactivación se hace con el campo Activo del formulario. Confirmar baja con DialogoBaja; conservar el diálogo y el mensaje literal si el servidor devuelve 409.
- Formulario: código, nombre, zona textual, origen, destino, distancia, peaje estimado, moneda, activo y observaciones. Activo marcado al crear. Límites y mensajes idénticos al backend. Trim al enviar; observaciones vacías omitidas. No introducir reglas sobre ciudades distintas, pertenencia geográfica o distancias calculadas que no estén aprobadas.
- Distancia y peaje se editan como texto decimal. Admitir peaje cero; rechazar valores fuera de rango o con más de dos decimales antes de convertir al contrato numérico. Rechazar cantidades que no puedan enviarse con precisión desde JavaScript con `El peaje estimado no puede enviarse con precisión desde este navegador. Ingresa una cantidad menor.`. Mostrar las cadenas decimales recibidas sin convertirlas a Number.
- Estados independientes de carga/error para listado, detalle, zonas y resumen, con reintento. Una respuesta vieja no reemplaza una selección o filtro posterior. Una única consulta inicial del listado; impedir guardados repetidos y conservar valores en 400/409.
- Después de guardar, refrescar listado, detalle, zonas y resumen. Después de eliminar, limpiar selección y corregir una última página vacía. Mantener el primer foco del formulario, enfocar el primer error, atrapar foco en el diálogo y devolverlo al disparador al cerrar con Escape.
- Escritorio 1280 px: dos columnas, listado a la izquierda y detalle/formulario a la derecha; cabeceras y cifras no partidas. Móvil 375 px: paneles apilados, márgenes de al menos 16 px y scroll de tabla dentro de su región accesible, nunca en toda la página. Texto/estados legibles en claro/oscuro, sin depender solo del color.

## 5. Criterios de aceptación

| Criterio | Verificación requerida |
|---|---|
| A1 Cliente | siete operaciones con métodos/cuerpos/queries exactos; false conservado; errores literales |
| A2 Consulta | temporizadores falsos para 300 ms, filtros, dos páginas, retorno a página 1, carga única y respuestas obsoletas |
| A3 Maestro | alta/edición/desactivación/baja; campos requeridos, longitudes, peaje cero, decimales y precisión; mensajes 400/409 sin perder datos |
| A4 Accesibilidad | etiquetas, errores vinculados, primer foco, Tab/Escape/retorno de foco en baja; reintento de cada carga |
| A5 Gate | verify frontend y completo en verde; repetir la suite propia diez veces según crear-pantalla.md §9 |
| A6 QA real | API/PostgreSQL locales, 375/1280 claro/oscuro; alta/edición/baja, detalle y diálogo; sin desbordamiento, con capturas y resultados reales |

## 6. Checklist de ejecución

- [ ] Lukas aprueba las specs y decisiones del backend.
- [x] Backend y base local preparados mediante los scripts del proyecto.
- [x] Cliente y validaciones con pruebas.
- [x] Pantalla y flujos con pruebas temáticas.
- [x] Suite estable diez ejecuciones; gates y smoke con exit 0, conservando la salvedad de Vite documentada.
- [x] QA real con evidencia en §7 y capturas en `docs/evidencias/modulo43/`.

## 7. Registro de verificación

| Fecha | Estado | Evidencia |
|---|---|---|
| 2026-11-21 | Propuesta, implementación y QA pendientes | Contrato propuesto contrastado con el enunciado, plantilla de specs y guía de pantalla; sin evidencia de ejecución todavía |
| 2026-11-21 | Implementación local y QA real verificados; revisión pendiente | 60 pruebas propias correctas durante diez repeticiones; cuatro escenarios 375/1280 claro/oscuro, contraste medido, fondo de selección uniforme y 12 capturas en `docs/evidencias/modulo43/` |
