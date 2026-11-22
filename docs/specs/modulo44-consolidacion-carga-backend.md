# Especificación propuesta: módulo 44 — Consolidación de carga (backend)

**Estado de revisión: propuesta, pendiente de aprobación de Lukas.**

## 1. Objetivo
Implementar `ConsolidacionCarga (picking, packing, cubicaje)` del enunciado. Responsable Kelvin Pinto; orden 41 → 42 → 48 → 43 → **44** → 45 → 46 → 47 → 49 → 50 → 73.

Rama `kelvin/modulo44-consolidacion-carga`, independiente desde origin/main actualizado. Propuesta, pendiente de aprobación de Lukas; **las decisiones propuestas y la aprobación de Lukas siguen pendientes**. No acredita integración ni despliegue.

## 2. Fuera de alcance y decisiones para revisión
- Preparación manual de carga con SKU descriptivo, sin FK a Producto ni cambios en las cuatro entidades base. No reservar, descontar ni mover existencias, órdenes o inventarios de otros propietarios.
- Una línea representa un SKU/presentación homogénea. Se permiten SKU repetidos con empaques distintos. Picking: cantidad prevista y cantidad recogida. Packing: unidades por bulto, dimensiones exteriores en cm y peso estimado por bulto en kg. No distribuir varios SKU en el mismo bulto ni integrar escáneres, balanzas o vehículos.
- Bultos estimados = techo(cantidad/unidadesPorBulto), o cero cuando no se ha configurado el packing. El último bulto parcial conserva las dimensiones/peso estimados del empaque completo; no se presenta como pesaje real.
- Volumen por línea = bultos × largo × ancho × alto / 1000000, redondeado a seis decimales, mitades hacia fuera de cero. Peso por línea = bultos × peso por bulto, dos decimales. Totales = suma de los valores de las líneas; respuestas decimales textuales exactas. No inferir capacidad ni optimizar cubicaje.
- Borrador permite picking/packing incompletos y editar/reemplazar sus líneas. Confirmar requiere todo recogido y todo empaquetado. Confirmada y Cancelada son de solo lectura; se puede cancelar un borrador o una confirmada con motivo, preservando el historial. Solo borrar borradores.
- No se garantiza resolución de ediciones simultáneas ni cancelación concurrente: no atribuir a las pruebas ese alcance. Las escrituras individuales se guardan atómicamente con UnitOfWork, y el índice SQL protege códigos duplicados concurrentes.

## 3. Archivos afectados
`database/014-consolidacion-carga.sql` nuevo (reservas locales 010–013 separadas); modelos `ConsolidacionCarga` y `LineaConsolidacionCarga` y DbContext generados por scaffold; configuración de nombres propia; DTOs y UseCases en `Modulo44_ConsolidacionCarga`; controlador en esa carpeta de Api; pruebas en `tests/TitanGlobal.Api.Tests/Modulo44_ConsolidacionCarga/`; seed al final antes del COMMIT y request_check/validadores del smoke; docs de asignaciones/relevo/evidencia. No modificar middleware, pipeline, dependencias, scripts fusionados ni módulos ajenos.

## 4. Diseño y contrato
Tabla ConsolidacionesCarga: Id identity, Codigo varchar20 normalizado único lower(btrim), Nombre varchar100 obligatorio, Estado varchar15, Observaciones varchar500 nullable, FechaConfirmacion timestamptz nullable, MotivoCancelacion varchar500 nullable y FechaCancelacion timestamptz nullable. Estados y consistencia de fechas/motivo restringidos en SQL. Fechas de transición del servidor con FechasUtc.

Tabla LineasConsolidacionCarga: Id identity, ConsolidacionCargaId FK con borrado en cascada, Posicion 1..100, Sku varchar40 y Descripcion varchar200 obligatorios; Cantidad 1..1000000; CantidadRecogida 0..Cantidad; UnidadesPorBulto 0..1000000; LargoCm/AnchoCm/AltoCm numeric(6,2), de 0 a 9999.99; PesoBultoKg numeric(8,2), de 0 a 999999.99. Validar siempre antes de guardar, sin redondear entradas. Índice FK y checks en SQL. No hay referencias a tablas ajenas.

Prefijo autenticado `/api/v1/consolidaciones-carga`:
| Operación | Contrato |
|---|---|
| GET / | busqueda por código/nombre, estado opcional, pagina=1, tamanoPagina=20; orden Código/Id, filtros antes de paginar y desplazamiento sin overflow |
| GET /{id} | detalle con líneas ordenadas por Posicion/Id y totales calculados |
| POST / | {codigo,nombre,observaciones?,lineas:[{sku,descripcion,cantidad,cantidadRecogida,unidadesPorBulto,largoCm,anchoCm,altoCm,pesoBultoKg}]}; crea Borrador; 200 {id} |
| PUT /{id} | cuerpo completo como alta; solo Borrador; reemplaza líneas en una sola transacción, sus Id no son estables ni aceptados de entrada; 200 {id} |
| DELETE /{id} | solo Borrador, elimina también líneas; 204 |
| POST /{id}/confirmacion | sin cuerpo, PermitirCuerpoVacio; 200 {id} |
| POST /{id}/cancelacion | {motivo}; 200 {id} |
| GET /resumen | total, borradores, confirmadas, canceladas; conteos, sin sumar unidades heterogéneas |

Página {items,total,pagina,tamanoPagina}; item id/codigo/nombre/estado/totalUnidades/unidadesRecogidas/totalBultos/volumenM3/pesoKg. Detalle agrega observaciones, fechas/motivo y lineas con los campos guardados, bultos, volumenM3/pesoKg. Decimales de salida como strings (volumen 6 posiciones, peso/dimensiones 2); enteros como números. No exponer entidades.

Orden de validación: en edición/baja/transiciones, buscar primero (404 `La consolidación {id} no existe.`); después estado permitido (409); después cabecera, líneas en orden, y finalmente código duplicado. Trim de textos y opcionales vacíos a null.

400 literales de cabecera: `El código de la consolidación es obligatorio.` / `El código de la consolidación no puede superar 20 caracteres.`; `El nombre de la consolidación es obligatorio.` / `El nombre de la consolidación no puede superar 100 caracteres.`; `Las observaciones no pueden superar 500 caracteres.`; `La consolidación debe tener entre 1 y 100 líneas.`; `La consolidación no puede contener líneas nulas.`.

Por cada línea (i empieza en 1): `Línea {i}: el SKU es obligatorio.` / `Línea {i}: el SKU no puede superar 40 caracteres.`; `Línea {i}: la descripción es obligatoria.` / `Línea {i}: la descripción no puede superar 200 caracteres.`. Campos numéricos, en el orden de la tabla anterior:
- `Línea {i}: la cantidad debe estar entre 1 y 1000000.`
- `Línea {i}: la cantidad recogida debe estar entre 0 y 1000000.`
- `Línea {i}: las unidades por bulto debe estar entre 0 y 1000000.`
- `Línea {i}: el largo debe estar entre 0 y 9999.99.`
- `Línea {i}: el largo no puede tener más de 2 decimales.`
- `Línea {i}: el ancho debe estar entre 0 y 9999.99.`
- `Línea {i}: el ancho no puede tener más de 2 decimales.`
- `Línea {i}: el alto debe estar entre 0 y 9999.99.`
- `Línea {i}: el alto no puede tener más de 2 decimales.`
- `Línea {i}: el peso por bulto debe estar entre 0 y 999999.99.`
- `Línea {i}: el peso por bulto no puede tener más de 2 decimales.`
- `Línea {i}: la cantidad recogida no puede superar la cantidad prevista.` (después del rango de recogida).

409 de estado: `Solo se puede editar una consolidación en borrador.`; `Solo se puede eliminar una consolidación en borrador.`; `Solo se puede confirmar una consolidación en borrador.`; `Completa el picking de todas las líneas antes de confirmar.`; `Completa el packing de todas las líneas antes de confirmar.`; `La consolidación ya está cancelada.`.

Cancelación 400: `El motivo de cancelación es obligatorio.` / `El motivo de cancelación no puede superar 500 caracteres.`. Código duplicado 409: `Ya existe una consolidación con el código {codigo}.`; capturar también Unicidad al guardar. Baja bloqueada por FK futura: 409 `No se puede eliminar la consolidación porque está en uso.`. No inventar ni acreditar esa FK futura.

Listado: 400 `El número de página debe ser mayor que cero.`; luego `El tamaño de página debe estar entre 1 y 100.`; luego `El estado debe ser Borrador, Confirmada o Cancelada.`. Estado normalizado sin distinguir mayúsculas. Application usa únicamente excepciones/CodigosEstado del proyecto; tipos explícitos, UseCases concretos españoles, controladores delgados, sin interfaces ni miembros con flecha.

## 5. Criterios de aceptación
A1: build sin warnings/errores, formato/capas/reglas y verify exit0; registrar cualquier salvedad real de la base (por ejemplo Vite), sin ocultar ni debilitar gates. A2: integración por cada mensaje, límites, totales/redondeo, estado/fechas UTC, reemplazo atómico y conservación histórica. A3: filtros/paginación, ocho operaciones OpenAPI y JWT. A4: SQL aplicado, scaffold coincidente, seed idempotente con borrador incompleto, confirmado y cancelado. A5: smoke real con CRUD, picking/packing incompletos, confirmación, cancelación, precisión y restricciones SQL. A6: pantalla y QA real según spec frontend.

## 6. Checklist
- [ ] Revisión y aprobación de Lukas.
- [x] Esquema y scaffold local.
- [x] API, reglas, integración y datos de ejemplo.
- [x] Pantalla y pruebas.
- [x] Gates, estabilidad, smoke y QA reales documentados, con salvedades en la evidencia.

## 7. Registro
2026-11-21: Propuesta, pendiente de aprobación de Lukas; revisión de Lukas pendiente. SQL aplicado y scaffold regenerado; 1140 pruebas backend (55 propias), 599 frontend (56 propias), verify con salida 0 y smoke con SMOKE OK. Estabilidad y QA documentados en [evidencia](../evidencias/modulo44/README.md). La advertencia de tamaño del bundle compartido de Vite impide acreditar cero advertencias globales. Commits por avances funcionales, identidad Git de Kelvin, sin push ni PR ni cambios en main. No acredita aprobación, integración ni despliegue.
