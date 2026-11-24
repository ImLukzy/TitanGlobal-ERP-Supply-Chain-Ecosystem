# Implementación del módulo 52 — Notas de crédito y débito

La [spec del 52](modulo52-notas-credito-debito-backend.md) recoge las decisiones aprobadas para la v0. Su revisión de conjunto se sigue en el [PR #52](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52). Se conserva `fb84529` como dependencia temporal mientras el PR #48 del 51 siga abierto.

## Contrato implementado

- La factura o boleta debe estar emitida al crear y emitir. La nota conserva su moneda y guarda líneas con `DetalleFacturaId`, cantidad de cuatro decimales, precio unitario y porcentaje histórico. El servidor redondea la base por línea, calcula y redondea su impuesto a dos decimales con `MidpointRounding.AwayFromZero` y suma los resultados en la cabecera.
- Cada tipo usa su secuencia PostgreSQL. El alta guarda `TMP-` y asigna `NC-` o `ND-` en la misma transacción. El índice único cubre el folio definitivo.
- Las seis rutas de `/api/v1/notas-credito-debito` permiten lectura con JWT y escritura con ADMIN o FINANZAS. El seed agrega FINANZAS con Id 7 y aborta antes de escribir cuando se detecta un conflicto.
- Solo se editan borradores. La emisión fija el día de negocio de Lima en una columna `date`. La anulación es lógica con motivo de 3 a 300 caracteres. Los instantes de creación y anulación se guardan en UTC.
- El tope de crédito al emitir usa cantidad y total facturados por línea y acumula únicamente otras notas en `Emitida`. Los débitos carecen de tope acumulado. La guarda para anular un crédito aplicado se incorporará con el 53.

## Transición local desde el 024 provisional

El aplicador registra los scripts por nombre en `control."ScriptsAplicados"`. Una base nueva usa `./scripts/aplicar-bd.sh`. Una base local que ya registró el 024 provisional y tiene **cero notas** puede actualizar su esquema ejecutando este archivo de forma explícita, en una transacción y con la API detenida:

```sh
docker compose exec -T db psql -U titanglobal -d titanglobal -v ON_ERROR_STOP=1 --single-transaction -f - < database/024-modulo52-notas-credito-debito.sql
```

El script bloquea la tabla provisional al comprobar que está vacía. Si encuentra notas globales, aborta y conserva todos los datos. Esas notas carecen de líneas de origen, cantidades y desglose histórico. Una conversión que preserve su significado necesita una asignación de esos datos autorizada. Para fixtures de una base desechable, el flujo del repositorio es recrear con `./scripts/aplicar-bd.sh --reiniciar` y cargar el seed definitivo.

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
