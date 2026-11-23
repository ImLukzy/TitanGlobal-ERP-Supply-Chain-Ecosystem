# Implementación provisional del módulo 52 — Notas de crédito y débito

> Alcance de la autorización de Lukas comunicada para esta implementación. La [propuesta principal](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/blob/052e1f0c8ae2eeefeeede0eac805a1fada307f16/docs/specs/modulo52-notas-credito-debito-backend.md) sigue pendiente de aprobación definitiva.

## Decisiones aplicadas

| Decisión | Alcance provisional |
|---|---|
| 52.1 | Una nota interna de tipo `Credito` o `Debito` referencia una factura o boleta `Emitida` del 51. Se comprueba el estado del comprobante al crear, editar y emitir. El comprobante original se conserva al editar. |
| 52.2 | El alta guarda `Borrador`. La edición se permite en ese estado. Emitir pasa a `Emitida` y anular pasa de `Emitida` a `Anulada` con motivo. Las fechas de creación, emisión y anulación son UTC y las asigna el servidor. |
| 52.3 | Motivo libre obligatorio, recortado y de hasta 500 caracteres. El mismo límite aplica al motivo de anulación. Los endpoints heredan JWT. |
| 52.4 | Serie manual de hasta 10 caracteres, recortada y en mayúsculas, y entero positivo. El índice único cubre `(Tipo, Serie, Numero)`. |
| 52.5 | Ajuste global positivo informado en `importe`. Se guarda y se muestra como `total`. Admite hasta dos decimales dentro de `numeric(18,2)`. El cliente y la moneda proceden del comprobante. |
| 52.6 | El flujo termina en el documento interno. La aprobación de topes acumulados, líneas, impuestos, redondeos y efectos en cartera queda pendiente. |

El recurso es `/api/v1/notas-credito-debito`. `POST` y `PUT /{id}` reciben `facturaId`, `tipo`, `serie`, `numero`, `motivo` e `importe`. El listado admite `facturaId`, `pagina` y `tamanoPagina`, con orden por creación e id descendentes. Las acciones de emisión y anulación siguen las rutas de la propuesta.

La edición con otro `facturaId` reutiliza el literal «Debe indicar una factura válida.». El exceso de longitud en cualquiera de los motivos produce «El motivo de la nota supera la longitud permitida.». El resto de validaciones del 52 utiliza los literales de la propuesta principal.

## Protección del comprobante del 51

La anulación del comprobante devuelve HTTP 409 con el mensaje «No se puede anular un comprobante con notas emitidas.» si existe una nota en `Emitida`. Conservar la validación del estado y motivo del 51. El borrador y la nota ya anulada dejan de ser relevantes para esta comprobación.

En PostgreSQL, los cambios de la nota y la anulación del comprobante adquieren un bloqueo sobre la misma fila de `Facturas` mediante `IUnitOfWork`. La comprobación de estado y la escritura ocurren en la misma transacción. La serialización también evita que dos cambios de estado sobre una nota pasen juntos. La unicidad entre comprobantes queda respaldada por el índice de la nota.

## Edición concurrente del comprobante pendiente de coordinar

La carrera entre la edición y la emisión del módulo 51 sigue pendiente en este avance provisional. Su corrección se realizará posteriormente, en coordinación con Lukas.

La edición del 51 lee el estado `Borrador` antes de calcular sus detalles. Si la factura se emite mientras la edición espera, el guardado de la entidad puede sobrescribir ese estado y sus importes después de emitir una nota.

La [prueba de reproducción](../../scripts/repro-edicion-factura-modulo52.py) se ejecuta con `./scripts/smoke-postgres.sh --repro-edicion-factura` sobre un proyecto Compose desechable. Es una comprobación opcional que devuelve un código distinto de cero mientras el problema se reproduzca.

1. Crear por API una factura en `Borrador` con total 100. Bloquear la tabla `Productos` en otra sesión de PostgreSQL.
2. Solicitar una edición por `PUT` que cambie el total a 180. Confirmar en `pg_stat_activity` que leyó el borrador y quedó esperando al consultar `Productos`.
3. Solicitar la emisión de la factura. Confirmar en PostgreSQL que la factura quedó `Emitida` y crear y emitir una nota antes de liberar el bloqueo.
4. Liberar el bloqueo de `Productos` y esperar a que termine el `PUT`. La edición y la emisión de la factura devuelven HTTP 200. La factura acaba en `Borrador` con total 180 y la nota queda `Emitida`.

La corrección propuesta para coordinar con Lukas es adquirir el mismo bloqueo sobre la fila de `Facturas` en los casos de uso de edición y emisión del 51, antes de leer y validar el estado. La transacción debe abarcar la escritura. La prueba exige conservar el estado `Emitida` en ambos documentos y los importes que correspondan al orden en que se completaron las operaciones.

## Integración prevista

El número asignado es `024`. El PR #48 sigue fuera de la base revisada de `origin/main` y `fb84529` se conserva como dependencia temporal del 51. Al integrar el 52, incorporar su única fila del relevo después de las reservas existentes en `main`.

## Verificación local

- `dotnet build -warnaserror` sin errores ni advertencias. `scripts/check-rules.sh` y `scripts/verify.sh` terminaron con salida 0. Pasaron 1107 pruebas de backend, incluidas 24 del módulo 52, y 543 de frontend. La prueba del 409 comprueba el literal de la clave foránea y que no se persiste la nota.
- PostgreSQL 16 aislado aplicó el `024`. El scaffold oficial generó los modelos y la comprobación reportó `SCAFFOLD AL DÍA`. Dos cargas del seed dejaron tres comprobantes, tres detalles de factura y dos notas del módulo en ambas ejecuciones.
- `scripts/smoke-postgres.sh` terminó en `SMOKE OK`. Verificó las restricciones en PostgreSQL, incluido el rechazo de `NaN` por `CK_NotasCreditoDebito_Total`, y la carrera entre emisión de nota y anulación del comprobante. En la ejecución registrada la nota se emitió con HTTP 200 y la anulación devolvió HTTP 409. OpenAPI y Swagger expusieron las seis operaciones.
