# Especificación propuesta: módulo 43 — Rutas de despacho (backend)

**Estado de revisión: propuesta, pendiente de aprobación de Lukas.**

## 1. Objetivo

El enunciado define `RutasDespacho (zonas, distancias, peajes)`. El módulo partía sin spec, tabla ni API; la implementación local proporciona un maestro de rutas consultable por los módulos posteriores de logística.

Responsable: Kelvin Pinto, según `docs/equipo/asignaciones.md`. Orden confirmado: 41 → 42 → 48 → **43** → 44 → 45 → 46 → 47 → 49 → 50 → 73. Rama propia: `kelvin/modulo43-rutas-despacho`, creada desde `origin/main` después de `git fetch origin`.

**Estado: implementado localmente como propuesta, pendiente de aprobación de Lukas. La revisión y aprobación de Lukas siguen pendientes.** Esta autorización de trabajo local no se presenta como revisión de Lukas ni permite integrar o desplegar. El gate termina con exit 0; Vite mantiene su aviso previo de tamaño del paquete compartido, documentado en la evidencia.

## 2. Fuera de alcance y decisiones pendientes

No modificar las cuatro entidades base, autenticación, middlewares, pipeline, dependencias ni módulos de otras personas. No incorporar los commits de implementación del 41, 42 o 48 a esta rama independiente.

No programar viajes, asignar vehículos/conductores/transportistas, optimizar recorridos, integrar mapas, gestionar entregas ni calcular combustible. Estas funcionalidades no están definidas para el 43 en el enunciado y requerirían su propia aprobación.

**Decisiones de producto propuestas que Lukas debe revisar antes de programar:**

Estas decisiones son una propuesta, pendiente de aprobación de Lukas. No se marca la spec como aprobada.

1. El módulo es un maestro reutilizable de rutas, no un despacho programado con fecha, vehículo o conductor. Los módulos posteriores podrán referenciarlo mediante cambios aprobados, sin anticipar sus tablas.
2. `Zona` es una clasificación textual de la ruta; no se crea otro catálogo ni un polígono geográfico. `Origen` y `Destino` son descripciones, sin modificar Cliente ni Sucursales.
3. Los peajes se representan inicialmente como un **importe estimado total por recorrido**, ingresado manualmente y asociado a una moneda declarativa. No hay estaciones, tarifas por eje, cobros reales, impuestos ni conversión de monedas. Si se requiere una lista de estaciones de peaje, esta propuesta debe revisarse antes de crear el esquema.
4. Código único sin distinguir mayúsculas ni espacios exteriores; rutas desactivables mediante edición. La baja física se permite mientras ninguna relación de base de datos la impida. La vinculación con despachos futuros requiere otra spec; no se inventa un historial inexistente.

## 3. Archivos afectados

| Archivo | Acción después de aprobar | Nota |
|---|---|---|
| `database/013-rutas-despacho.sql` | crear | número provisional después de las reservas locales 010/011/012 de Kelvin; confirmar contra relevo actualizado antes de crear SQL |
| `src/TitanGlobal.Domain/Models/RutaDespacho.cs` | generar | exclusivamente mediante `./scripts/scaffold.sh` |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | no editar manualmente |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` | modificar si hace falta | únicamente el nombre español del modelo nuevo |
| `tests/TitanGlobal.Api.Tests/Arquitectura/PluralizadorEspanolTests.cs` | ampliar si hace falta | conservar todas las pruebas existentes |
| `src/TitanGlobal.Application/DTOs/Modulo43_RutasDespacho/` | crear | un archivo por DTO |
| `src/TitanGlobal.Application/UseCases/Modulo43_RutasDespacho/` | crear | casos concretos en español y validación común |
| `src/TitanGlobal.Api/Controllers/Modulo43_RutasDespacho/RutasDespachoController.cs` | crear | controlador delgado |
| `tests/TitanGlobal.Api.Tests/Modulo43_RutasDespacho/` | crear | integración de reglas, contratos y conflictos al guardar |
| `scripts/seed-dev.sql` | ampliar al final, antes del COMMIT | datos propios idempotentes |
| `scripts/smoke-postgres.sh` | ampliar | request_check y validadores Python propios |

## 4. Diseño y lógica propuestos

### 4.1 Datos

Tabla `RutasDespacho`, con clave primaria `Id` entera con identidad y estas columnas:

| Campo | Tipo y límite | Regla propuesta |
|---|---|---|
| Codigo | varchar(20) | obligatorio, Trim, índice único `lower(btrim("Codigo"))` |
| Nombre | varchar(100) | obligatorio, Trim |
| Zona | varchar(100) | obligatoria, Trim |
| Origen | varchar(120) | obligatorio, Trim |
| Destino | varchar(120) | obligatorio, Trim |
| DistanciaKm | numeric(10,2) | mayor que cero, máximo 99999999.99; no redondear decimales adicionales |
| PeajeEstimado | numeric(18,2) | desde cero hasta 9999999999999999.99; no redondear decimales adicionales |
| Moneda | varchar(3) | Trim, mayúsculas, tres letras ASCII; sin conversión |
| Activo | boolean | true por defecto; conservar false explícito |
| Observaciones | varchar(500), nullable | Trim; vacío se guarda como null |

Definir obligatoriedad, longitudes, precisión, unicidad y comprobaciones numéricas en SQL. No agregar fechas artificiales: no hay un viaje programado en este alcance. Si se aprueba añadir una fecha, deberá ser `timestamptz` y normalizarse con `FechasUtc`.

### 4.2 API y DTOs

Todas las operaciones exigen JWT. Prefijo `/api/v1/rutas-despacho`:

| Método / ruta | Entrada | Respuesta |
|---|---|---|
| GET `/` | busqueda, zona, activo, pagina=1, tamanoPagina=20 | página de rutas |
| GET `/{id}` | id entero | detalle o 404 |
| POST `/` | GuardarRutaDespachoRequestDto | 200 `{ id }` |
| PUT `/{id}` | id y mismo DTO completo | 200 `{ id }` o 404 |
| DELETE `/{id}` | id | 204, 404 o conflicto de referencia |
| GET `/zonas` | sin parámetros | zonas distintas ordenadas para filtrar |
| GET `/resumen` | sin parámetros | totalRutas, activas, inactivas, porZona |

Entrada: `codigo`, `nombre`, `zona`, `origen`, `destino`, `distanciaKm` (decimal), `peajeEstimado` (decimal), `moneda`, `activo` (por defecto true), `observaciones` opcionales.

Página: `{ items, total, pagina, tamanoPagina }`. Cada item incluye id, código, nombre, zona, origen, destino, distanciaKm, peajeEstimado, moneda y activo. Detalle añade observaciones. **Distancia e importe de salida son cadenas decimales de dos posiciones**, con punto y cultura invariante; no perder precisión al serializar para JavaScript. No exponer entidades ni navegaciones.

Resumen global del maestro, independiente de los filtros: `{ totalRutas, activas, inactivas, porZona: [{ zona, totalRutas, activas }] }`. No sumar peajes de monedas distintas ni presentar la suma de distancias como un viaje real. `/zonas` conserva las etiquetas guardadas y elimina coincidencias con la misma etiqueta exacta; el filtro de zona compara sin distinguir mayúsculas ni espacios exteriores.

### 4.3 Orden de validación y mensajes literales propuestos

En PUT y DELETE, buscar primero la ruta: si falta, 404 `La ruta de despacho {id} no existe.`. Después, en POST y PUT, validar en este orden:

| Orden / regla | 400 literal |
|---|---|
| Código vacío | `El código de la ruta es obligatorio.` |
| Código > 20 | `El código de la ruta no puede superar 20 caracteres.` |
| Nombre vacío | `El nombre de la ruta es obligatorio.` |
| Nombre > 100 | `El nombre de la ruta no puede superar 100 caracteres.` |
| Zona vacía | `La zona es obligatoria.` |
| Zona > 100 | `La zona no puede superar 100 caracteres.` |
| Origen vacío | `El origen es obligatorio.` |
| Origen > 120 | `El origen no puede superar 120 caracteres.` |
| Destino vacío | `El destino es obligatorio.` |
| Destino > 120 | `El destino no puede superar 120 caracteres.` |
| Distancia <= 0 | `La distancia debe ser mayor que cero.` |
| Distancia > 99999999.99 | `La distancia no puede superar 99999999.99 km.` |
| Distancia con más de dos decimales significativos | `La distancia no puede tener más de 2 decimales.` |
| Peaje < 0 | `El peaje estimado no puede ser negativo.` |
| Peaje > 9999999999999999.99 | `El peaje estimado no puede superar 9999999999999999.99.` |
| Peaje con más de dos decimales significativos | `El peaje estimado no puede tener más de 2 decimales.` |
| Moneda fuera de tres letras ASCII | `La moneda debe tener tres letras.` |
| Observaciones > 500 | `Las observaciones no pueden superar 500 caracteres.` |

Después de validar campos, comprobar código duplicado excluyendo el propio id al editar: 409 `Ya existe una ruta de despacho con el código {codigo}.`. Aplicar la misma respuesta al capturar `ViolacionRestriccionException` de Unicidad durante el guardado, sin filtrar detalles internos. Al eliminar, una violación de ReferenciaForanea devuelve 409 `No se puede eliminar la ruta de despacho porque está en uso.`; esto no presupone que exista actualmente otra tabla relacionada.

Listado: validar primero pagina >= 1, con 400 `El número de página debe ser mayor que cero.`; después tamaño entre 1 y 100, con 400 `El tamaño de página debe estar entre 1 y 100.`. Normalizar filtros opcionales; busqueda compara código/nombre/origen/destino sin distinguir mayúsculas, zona es coincidencia completa y activo conserva false. Orden estable por Código y luego Id; contar antes de paginar y evitar desbordamiento del desplazamiento para páginas enteras grandes. Una página fuera de rango devuelve items vacíos conservando total.

Errores `{ StatusCode, Message }`; Application usa `CodigosEstado` y excepciones del proyecto. Sin `var`, miembros con `=>`, interfaces de UseCase ni lógica en el controlador. Los casos de uso previstos son ListarRutasDespacho, ObtenerRutaDespacho, CrearRutaDespacho, ActualizarRutaDespacho, EliminarRutaDespacho, ListarZonasRutaDespacho y ResumenRutasDespacho, todos terminados en `UseCase`.

## 5. Criterios de aceptación

| Criterio | Verificación requerida |
|---|---|
| A1 Reglas y capas | verify completo, sin errores ni advertencias; ninguna prueba existente debilitada |
| A2 Integración | cada mensaje literal en alta/edición, límites exactos y excedidos, normalización, duplicados y conflictos al guardar |
| A3 Consultas | búsqueda y filtros combinados, false, orden, dos páginas, página vacía y desplazamiento grande; zonas y resumen exactos |
| A4 Contrato | siete operaciones en OpenAPI, respuestas con precisión decimal completa y JWT obligatorio |
| A5 Persistencia | SQL aplicado en local, scaffold generado y comprobación sin diferencias; restricciones reales y seed cargado dos veces |
| A6 Smoke | altas, edición, baja, duplicados, false persistido, peaje cero, decimales sin redondeo, límites máximos y restricciones SQL; finalizar en SMOKE OK |

Seed propuesto: seis rutas en al menos tres zonas, una inactiva, peaje cero y positivo, y monedas PEN/USD. Insertar de forma idempotente por el código normalizado, al final del seed y antes del COMMIT. Mantener intactos los datos de otros módulos.

## 6. Checklist de ejecución

- [ ] Lukas revisa las cuatro decisiones de §2 y aprueba esta spec.
- [x] Confirmar número SQL libre y crear el script; aplicar y generar entidades.
- [x] Implementar DTOs, casos de uso y controlador.
- [x] Añadir integración, seed y smoke propios del 43.
- [x] Completar la pantalla según la spec frontend.
- [x] Ejecutar A1–A6 y registrar resultados reales; conservar la salvedad de Vite en A1 y la aprobación de Lukas pendiente.

Commits por avances funcionales con sus pruebas, identidad Git de Kelvin y sin atribución a herramientas. No hacer push ni PR sin que el usuario lo solicite; `main` permanece intacto.

## 7. Registro de verificación

| Fecha | Estado | Evidencia |
|---|---|---|
| 2026-11-21 | Propuesta, pendiente de revisión | Asignación, orden y alcance mínimo contrastados con asignaciones/enunciado; ramas remotas actualizadas y búsqueda de specs previas sin resultados; sin código ni pruebas del 43 |
| 2026-11-21 | Implementación local verificada; revisión pendiente | 46 pruebas propias; gate con 1.130 pruebas backend y 603 frontend, siete operaciones OpenAPI, seed repetido, scaffold coincidente y SMOKE OK; detalle y salvedad de Vite en `docs/evidencias/modulo43/README.md` |

### Preparación de revisión — 2026-11-22

Rama actualizada con `git pull origin main`, conservando la tabla oficial de relevo y modificando solo la línea de Kelvin y la reserva propia. Script vigente: `database/013-rutas-despacho.sql`. Propuesta, pendiente de aprobación de Lukas. Base PostgreSQL local recreada, seed cargado y entidades/contexto regenerados mediante scaffold, sin edición manual ni diferencias de contenido.

`./scripts/verify.sh`: salida0, 1130 pruebas backend y 603 frontend en 76 archivos. `./scripts/smoke-postgres.sh`: salida0, scaffold coincidente y SMOKE OK. Se usó VITEST_MAX_WORKERS=1 temporal, sin modificar pruebas, timeouts ni configuración compartida. Persisten los avisos globales de ESLint9/Vite; no se cambiaron dependencias ni umbrales.

No se generaron capturas nuevas ni se cambió la pantalla para estos ajustes. Se conserva la evidencia visual previa. Preparación local: sin nuevos PR, sin push y sin integración. Tras cada fusión anterior habrá que incorporar main y repetir estas verificaciones antes del PR correspondiente.
