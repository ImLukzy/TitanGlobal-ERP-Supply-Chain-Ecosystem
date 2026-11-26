# Implementación del módulo 52 — Notas de crédito y débito

La [spec del 52](modulo52-notas-credito-debito-backend.md) recoge las decisiones aprobadas para la v0. El [PR #52](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52) ya se integró. El PR #83 usa como base temporal la rama actualizada del PR #48 en `7d131c7`. Tras integrar el 51, se revisará su base para apuntar a `main`.

## Contrato implementado

- La factura o boleta debe estar emitida al crear y emitir. La nota conserva su moneda y guarda líneas con `DetalleFacturaId`, cantidad de cuatro decimales, precio unitario y porcentaje histórico. El servidor redondea la base por línea, calcula y redondea su impuesto a dos decimales con `MidpointRounding.AwayFromZero` y suma los resultados en la cabecera.
- Cada tipo usa su secuencia PostgreSQL. El alta guarda `TMP-` y asigna `NC-` o `ND-` en la misma transacción. El índice único cubre el folio definitivo.
- Las seis rutas de `/api/v1/notas-credito-debito` permiten lectura con JWT y escritura con ADMIN o FINANZAS. SQL 024 crea FINANZAS con Id 7 antes del seed, conserva los roles existentes y aborta cuando el Id o el código están ocupados. Solo avanza la secuencia de roles cuando corresponde.
- Solo se editan borradores. La emisión fija el día de negocio de Perú en una columna `date`. Se comprueba el cambio de día a las 05:00 UTC. La anulación es lógica con motivo de 3 a 300 caracteres. Los instantes de creación y anulación se guardan en UTC.
- El tope de crédito al emitir usa cantidad y total facturados por línea y acumula únicamente otras notas en `Emitida`. Los débitos carecen de tope acumulado. La guarda para anular un crédito aplicado se incorporará con el 53.

## Transición local desde el 024 provisional

El aplicador registra los scripts por nombre en `control."ScriptsAplicados"`. Una base nueva usa `./scripts/aplicar-bd.sh`. Una base local que ya registró el 024 provisional y tiene **cero notas** puede actualizar su esquema ejecutando este archivo de forma explícita, en una transacción y con la API detenida:

```sh
docker compose exec -T db psql -U titanglobal -d titanglobal -v ON_ERROR_STOP=1 --single-transaction -f - < database/024-modulo52-notas-credito-debito.sql
```

El script bloquea la tabla provisional al comprobar que está vacía. Si encuentra notas globales, aborta y conserva todos los datos. Esas notas carecen de líneas de origen, cantidades y desglose histórico. Una conversión que preserve su significado necesita una asignación de esos datos autorizada. Para fixtures de una base desechable, el flujo del repositorio es recrear con `./scripts/aplicar-bd.sh --reiniciar` y cargar el seed definitivo.

Cuando una base ya tiene registrada la versión definitiva del SQL 024, el aplicador omite el archivo por nombre. Si ese entorno todavía carece de FINANZAS, Lukas debe autorizar un nuevo script de actualización numerado. El bloque de catálogo es idempotente por sí mismo.

## Concurrencia del 51 y el 52

La edición, emisión y anulación del 51 adquieren `FOR UPDATE` sobre la fila de la factura antes de su primera lectura con seguimiento. El bloqueo se mantiene hasta confirmar la operación. La emisión de notas y el cálculo acumulado de créditos adquieren el mismo bloqueo. La anulación de una factura con notas emitidas conserva el HTTP 409 y el literal del 51.

El [script de regresión](../../scripts/repro-edicion-factura-modulo52.py) se ejecuta en el smoke normal. Fuerza a la edición a esperar por `Productos` y comprueba que la emisión del 51 espere a su vez el bloqueo sobre `Facturas`. En el orden inverso, la edición devuelve el 409 de estado. Incluye las carreras entre emisión de nota y anulación de factura, entre dos emisiones de crédito y entre altas concurrentes sobre facturas distintas.

## Precisiones pendientes

- El contrato técnico del PR recibe `precioUnitario` como dato de la línea y calcula el impuesto sobre la base ya redondeada. Sigue pendiente confirmar qué precio corresponde a líneas con descuento y si el impuesto debe tomar la base antes de redondear. El tope de importe compara el crédito con el total realmente facturado, incluido el descuento.
- El DTO de edición conserva tipo, factura y folio asignado. La revisión final debe cerrar si se permite cambiar el tipo o el origen de un borrador.
- La protección de notas aplicadas requiere el modelo del 53. La conversión de notas globales con datos reales requiere una asignación autorizada a sus líneas de origen.

## Verificación local

| Comprobación | Resultado |
|---|---|
| `dotnet build -warnaserror` y `scripts/check-rules.sh` | Compilación con cero errores y advertencias. Reglas en verde. |
| `scripts/verify.sh` | Salida 0. Pasaron 1123 pruebas de backend, incluidas 40 del módulo 52, y 543 de frontend. Se observaron avisos de npm y del tamaño de un chunk de Vite. |
| Escritura, consulta y contratos | Se verificaron los 401 y 403 en PostgreSQL y la escritura por FINANZAS y ADMIN en las pruebas HTTP. OpenAPI y Swagger publican las seis operaciones y los DTOs definitivos. |
| PostgreSQL desechable | `SCAFFOLD AL DÍA` y `SMOKE OK`. Se verificaron FKs, unicidad, rechazo de `NaN`, folios concurrentes, ambos topes de crédito y las carreras del 51 y del 52. |
| Transición del 024 y seed | Se actualizó un esquema provisional vacío. Un esquema con dos notas globales conservó los datos al rechazarse la conversión. Se probó la recreación oficial en una base desechable. Dos cargas del seed produjeron tres facturas, tres líneas de factura, dos notas, dos líneas de nota y un rol FINANZAS. Un conflicto simulado de Id 7 abortó el seed y conservó las filas. |

### Verificación del 2026-11-26

- `git diff --check`, reglas y compilación verificados. `verify.sh` con 1.869 casos backend, 44 del módulo, y 1.455 frontend.
- `SCAFFOLD AL DÍA` y `SMOKE OK` sobre PostgreSQL desechable, incluida la concurrencia de facturas y notas.
- Aplicación oficial sin seed, rol Id 7 sin usuarios asignados, secuencia adelantada y dos tipos de conflicto rechazados. El aplicador omitió el SQL 024 en una segunda pasada y el catálogo mantuvo las filas al ejecutar su bloque dos veces. Dos cargas del seed conservaron los registros e identificadores de prueba.
