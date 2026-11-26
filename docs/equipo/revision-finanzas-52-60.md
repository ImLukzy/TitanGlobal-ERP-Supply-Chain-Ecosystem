# Revisión de Finanzas — módulos 52 a 60

**Estado.** Las nueve specs están aprobadas para la v0 por Lukas el 2026-11-23. La revisión final del [PR #52](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52) sigue pendiente. Responsable de los módulos: Leonel Portillo. [Respuesta oficial](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/52#issuecomment-7109010161) · [Asignaciones](asignaciones.md#resumen-por-persona)

## Base revisada

| Base | Disponible | Uso en la v0 |
|---|---|---|
| `origin/main` en `914a576` | Clientes (01), Proveedores (11), Órdenes de abastecimiento (15), Empleados (61), Impuestos y tasas (75), JWT y auditoría transversal. El seed registra los roles 1 a 6. | El 54 usa 11 y referencia opcional de 15, el 57 usa 61 como custodio y el 59 lo usa como responsable opcional. G1 agrega el rol 7 con el 52. |
| [PR #48](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/pull/48), `fb84529` | Facturas y detalles del 51 mediante el SQL `007`. Estado, cliente, moneda, vencimiento opcional y tasa aplicada por línea. | Disponible como base temporal para 52/53. Verificar el contrato integrado antes de fusionar sus backends. |
| Rama `leonel/modulo52-notas-credito-debito`, `e005686` y `7a2057f` | Avance provisional del 52. El anexo registra 1107 pruebas de backend, 543 de frontend, scaffold al día y smoke previo. | La diferencia con el contrato aprobado se detalla al final. Aún se requiere implementar y validar la spec definitiva. |
| [Spec 62](../specs/modulo62-contratos-laborales-backend.md) | Modelo de estructura, numeración, mensajes y evidencias. | Referencia técnica y documental. |

## Reglas del repositorio

- Controller → UseCase → IUnitOfWork → Repository → DbContext → PostgreSQL. Los casos de uso se registran por reflexión. Application valida mediante `ReglaNegocioException` y `CodigosEstado`, sin dependencias HTTP. Los controladores delegan. [Arquitectura](../arquitectura.md#estilo-y-estructura-en-cuatro-capas)
- Database First. Aplicar solo el SQL local y generar entidades `partial` y DbContext con el scaffold. Revisar las navegaciones e índices generados. Conservar los scripts fusionados. [Spec técnica de scaffold](../specs/52-database-first-scaffold.md)
- JWT, JSON camelCase, errores `{ StatusCode, Message }` y `[PermitirCuerpoVacio]` en acciones sin cuerpo. Los literales de autenticación conservan «Debe iniciar sesión para acceder a este recurso.» (401) y «Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.» (403). Probar también los literales de negocio revisados. `IUnitOfWork` traduce unicidad y FK. Las comprobaciones de saldo, estado y topes comparten la transacción de escritura. [Guía de módulo](crear-modulo.md)
- Añadir seed idempotente y checks con `request_check` y validación Python. Verificar PostgreSQL desechable, con pruebas concurrentes donde la integridad dependa de ello. Exigir `SCAFFOLD AL DÍA`, `SMOKE OK` y gate completo antes de integrar. [Guía de seed y smoke](crear-modulo.md#9-seed-y-smoke)

**Numeración SQL.** El 52 tiene asignado `024-modulo52-notas-credito-debito.sql`. La fila del 52 se registra en su rama de backend. El PR #57 reserva hasta `033`; en la base consultada se toma el siguiente libre a partir de `034`. Del 53 al 60, asignar al crear cada script y registrar solo su propia fila. [Relevo actualizado](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/blob/914a5764d6ae04fd6dd7d081d1679f490963c4a5/docs/equipo/relevo.md#8-números-de-los-scripts-de-base-de-datos)

## Reglas generales G1 a G9

| Regla | Aplicación y excepciones |
|---|---|
| G1 | JWT para toda consulta. Las escrituras de los nueve módulos requieren `FINANZAS` o `ADMIN`. El seed del 52 agrega `FINANZAS` con Id 7. |
| G2 | PEN o USD en la moneda del origen. 52/53 heredan factura, 54 captura la de factura de proveedor, 55 define la de la cuenta y 56 la hereda. 58 usa PEN fijo. 57/60 requieren confirmar moneda al carecer de documento de origen. 59 no usa moneda. Conversión y dependencia del 74 quedan fuera. |
| G3 | Una empresa en los nueve módulos. Sin sucursales. |
| G4 | Montos `numeric(18,2)`, cantidades `numeric(18,4)` donde existan y redondeo de 2 decimales por línea con `MidpointRounding.AwayFromZero` antes de sumar. El 52 conserva la escala de tasa del 51, el 60 ajusta la última cuota y el 59 carece de importes. |
| G5 | El servidor asigna folios internos mediante `TMP-` y código definitivo, sin series propias. 52 usa NC/ND con secuencias independientes y 60 usa AF. 57/58 describen prefijos internos propuestos. 54 conserva serie y número del proveedor, la cuenta bancaria del 55 conserva su número externo y 59 usa un código de catálogo. 53 y 56 conservan claves de origen y periodo. |
| G6 | Escrituras en el módulo dueño. Referencias manuales y FKs del contrato aprobado, incluidas las obligatorias expresas. Las guardas compartidas conservan integridad sin crear operaciones en otro módulo. Sin contabilidad. |
| G7 | Sin `DELETE`. Anulaciones o reversiones lógicas con motivo y sin reapertura. El 59 permite reactivar el maestro por decisión expresa. La baja de 60 es irreversible. |
| G8 | Fechas efectivas `date` hasta hoy e instantes `timestamptz` normalizados con `FechasUtc`. Corte de Lima expreso en el 53. El vencimiento derivado del 53 puede ser futuro. La fecha pactada del 54 requiere la precisión indicada en su spec. |
| G9 | Motivos de texto libre obligatorio entre 3 y 300 caracteres en los flujos que los piden. La baja de activos usa el catálogo del 60. |

## Matriz de dependencias entre módulos

Los requisitos previos corresponden al núcleo. La columna «Posterior» indica guardas o consumos que se completan al existir el módulo dependiente.

| Módulo | Disponible en main | Requisito previo de implementación | Posterior |
|---|---|---|---|
| 52 Notas de crédito y débito | 75 como contexto. Se usa tasa histórica del 51. | 51, disponible en PR #48 | 53 protege la anulación de créditos aplicados |
| 53 Cuentas por cobrar | 01 a través del 51 | 51 y 52 para facturas y notas de crédito | — |
| 54 Cuentas por pagar | 11 requerido, 15 como referencia opcional | — | — |
| 55 Tesorería y bancos | — | — | 56 completa la guarda de reversión de movimiento conciliado |
| 56 Conciliación bancaria | — | 55 para cuenta y movimiento | — |
| 57 Caja chica | 61 como custodio obligatorio | — | — |
| 58 Presupuestos | — | 59 para altas de partidas en centro activo | — |
| 59 Centros de costo | 61 como responsable opcional | — | 58 y 60 consumen el catálogo |
| 60 Activos fijos | — | 59 para el centro obligatorio al alta | — |

El rol de escritura de G1 es transversal. El seed del 52 lo habilita para el conjunto. El 54 puede consultar al 15 por FK, sin cambiar la orden. El 58 y 60 conservan la referencia histórica al centro.

### Guardas entre módulos

| Operación | PR de implementación | Cambio y límite |
|---|---|---|
| Anular factura con nota emitida | 52 | El 51 rechaza con 409. Emisión de nota y anulación de factura comparten bloqueo por factura. |
| Anular factura con cobros o nota con crédito aplicado | 53 | El 51 comprueba cobros y el 52 comprueba aplicaciones del 53 con HTTP 409. Las tablas nacen en el 53. |
| Revertir movimiento con coincidencia | 56 | La reversión del 55 devuelve 409. Coordinar protección de coincidencia y reversión. |
| Crear partida o activo con centro inactivo | 58 y 60 | Comprobar actividad al crear. El 59 conserva las referencias existentes al desactivar el centro. |

## Orden de implementación

1. Integrar el 51 y ajustar el avance del 52 al contrato aprobado. Incorporar G1 y la guarda por notas emitidas.
2. Con 51 y 52, implementar 53. El 54, 55, 57 y 59 pueden prepararse en paralelo. Integrar sus escrituras con G1.
3. Con el 55, implementar 56. Con el 59, implementar 58 y 60 en cualquier orden.

Trabajar cada backend en su propia rama. Separar SQL y entidades en un commit y API, pruebas, seed y smoke en el siguiente.

## Matriz de cumplimiento de las 44 decisiones

Cada fila remite a la decisión en §2 y a su cobertura en §4. Los números T1, T2 y siguientes corresponden al orden de pruebas propuesto en cada spec. La cobertura es documental.

| Nº | Decisión en §2 | Cobertura documentada |
|---|---|---|
| 52.1 | [Comprobante de origen](../specs/modulo52-notas-credito-debito-backend.md#2-fuera-de-alcance) | Origen del 51 y guarda de anulación (T1, T4) |
| 52.2 | [Ciclo](../specs/modulo52-notas-credito-debito-backend.md#2-fuera-de-alcance) | Ciclo, fecha de emisión y guarda al incorporar 53 (T1, T5) |
| 52.3 | [Motivos y acceso](../specs/modulo52-notas-credito-debito-backend.md#2-fuera-de-alcance) | Motivos y autorización (T2) |
| 52.4 | [Correlativos](../specs/modulo52-notas-credito-debito-backend.md#2-fuera-de-alcance) | Folios, secuencias e índice (T1, smoke) |
| 52.5 | [Detalle y cálculo](../specs/modulo52-notas-credito-debito-backend.md#2-fuera-de-alcance) | Líneas, tasa histórica y redondeo (T3) |
| 52.6 | [Topes y orden de validación](../specs/modulo52-notas-credito-debito-backend.md#2-fuera-de-alcance) | Precedencia, acumulados y elegibilidad (T2, T4, T5) |
| 53.1 | [Apertura](../specs/modulo53-cuentas-por-cobrar-backend.md#2-fuera-de-alcance) | Alta, FK única y vencimiento (T1) |
| 53.2 | [Cobros y reversión](../specs/modulo53-cuentas-por-cobrar-backend.md#2-fuera-de-alcance) | Medio, fecha, saldo y reversión (T1, T2) |
| 53.3 | [Aplicaciones y anulaciones](../specs/modulo53-cuentas-por-cobrar-backend.md#2-fuera-de-alcance) | Aplicación y guardas del 51, 52 y 53 (T3, T4) |
| 53.4 | [Cartera](../specs/modulo53-cuentas-por-cobrar-backend.md#2-fuera-de-alcance) | Corte de Lima, clientes y tramos (T4) |
| 53.5 | [Acceso](../specs/modulo53-cuentas-por-cobrar-backend.md#2-fuera-de-alcance) | Autorización y separación bancaria (T5) |
| 54.1 | [Nacimiento](../specs/modulo54-cuentas-por-pagar-backend.md#2-fuera-de-alcance) | Proveedor activo y FK opcional del 15 (T1) |
| 54.2 | [Identidad e importes](../specs/modulo54-cuentas-por-pagar-backend.md#2-fuera-de-alcance) | Clave normalizada y base más impuesto (T1, T2) |
| 54.3 | [Pagos y anulación](../specs/modulo54-cuentas-por-pagar-backend.md#2-fuera-de-alcance) | Saldo, reversión y anulación (T2) |
| 54.4 | [Cartera](../specs/modulo54-cuentas-por-pagar-backend.md#2-fuera-de-alcance) | Vencimiento, tramos y pago directo (T1, T2, T3) |
| 54.5 | [Independencia](../specs/modulo54-cuentas-por-pagar-backend.md#2-fuera-de-alcance) | Datos y efectos propios del módulo (T3) |
| 55.1 | [Cuentas](../specs/modulo55-tesoreria-bancos-backend.md#2-fuera-de-alcance) | Clave banco/número, tipos y moneda (T1) |
| 55.2 | [Ingreso y egreso](../specs/modulo55-tesoreria-bancos-backend.md#2-fuera-de-alcance) | Movimientos, fecha y saldo (T2) |
| 55.3 | [Transferencia](../specs/modulo55-tesoreria-bancos-backend.md#2-fuera-de-alcance) | Dos movimientos atómicos (T3) |
| 55.4 | [Conciliación](../specs/modulo55-tesoreria-bancos-backend.md#2-fuera-de-alcance) | Guarda de reversión con 56 (T3, smoke del 56) |
| 55.5 | [Cheques](../specs/modulo55-tesoreria-bancos-backend.md#2-fuera-de-alcance) | Fuera de alcance y rutas sin cheques |
| 55.6 | [Referencias](../specs/modulo55-tesoreria-bancos-backend.md#2-fuera-de-alcance) | Referencia de texto y datos locales (T4) |
| 56.1 | [Carga y edición](../specs/modulo56-conciliacion-bancaria-backend.md#2-fuera-de-alcance) | JSON, periodos, solapamiento y edición (T1) |
| 56.2 | [Coincidencia](../specs/modulo56-conciliacion-bancaria-backend.md#2-fuera-de-alcance) | Coincidencia manual exacta (T2) |
| 56.3 | [Cierre](../specs/modulo56-conciliacion-bancaria-backend.md#2-fuera-de-alcance) | Cierre, pendientes y diferencia (T3) |
| 56.4 | [Acceso y reversión](../specs/modulo56-conciliacion-bancaria-backend.md#2-fuera-de-alcance) | Acceso y guarda del 55 (T2, T4) |
| 57.1 | [Fondo](../specs/modulo57-caja-chica-backend.md#2-fuera-de-alcance) | Nombre, custodio, monto y cierre (T1, T2) |
| 57.2 | [Movimientos](../specs/modulo57-caja-chica-backend.md#2-fuera-de-alcance) | Comprobante, saldo y reversión (T2) |
| 57.3 | [Rendición](../specs/modulo57-caja-chica-backend.md#2-fuera-de-alcance) | Rendición y reposición manual (T2, T3) |
| 57.4 | [Acceso](../specs/modulo57-caja-chica-backend.md#2-fuera-de-alcance) | Acceso y separación de 58/59 (T4) |
| 58.1 | [Presupuesto](../specs/modulo58-presupuestos-backend.md#2-fuera-de-alcance) | Año calendario, nombre y PEN (T1) |
| 58.2 | [Partidas](../specs/modulo58-presupuestos-backend.md#2-fuera-de-alcance) | Centro activo y categoría de texto (T1, T2) |
| 58.3 | [Ciclo](../specs/modulo58-presupuestos-backend.md#2-fuera-de-alcance) | Ciclo y edición de montos (T2) |
| 58.4 | [Ejecución](../specs/modulo58-presupuestos-backend.md#2-fuera-de-alcance) | Ejecución manual y exceso permitido (T2) |
| 58.5 | [Disponible](../specs/modulo58-presupuestos-backend.md#2-fuera-de-alcance) | Disponible por año y centro (T3) |
| 59.1 | [Identidad y responsable](../specs/modulo59-centros-costo-backend.md#2-fuera-de-alcance) | Código y empleado opcional, sin 33 (T1) |
| 59.2 | [Árbol](../specs/modulo59-centros-costo-backend.md#2-fuera-de-alcance) | Profundidad, ciclos e hijos activos (T2, T3) |
| 59.3 | [Vigencia](../specs/modulo59-centros-costo-backend.md#2-fuera-de-alcance) | Actividad, historial y reactivación (T3) |
| 59.4 | [Acceso y consultas](../specs/modulo59-centros-costo-backend.md#2-fuera-de-alcance) | Autorización y consultas sin importes (T1, T4) |
| 60.1 | [Identidad y alta](../specs/modulo60-activos-fijos-backend.md#2-fuera-de-alcance) | Código, catálogo, centro, costo y fecha (T1) |
| 60.2 | [Depreciación lineal](../specs/modulo60-activos-fijos-backend.md#2-fuera-de-alcance) | Método lineal, vida útil, residual y redondeo (T1, T2) |
| 60.3 | [Periodicidad](../specs/modulo60-activos-fijos-backend.md#2-fuera-de-alcance) | Lote mensual, continuidad y unicidad (T2, smoke) |
| 60.4 | [Baja](../specs/modulo60-activos-fijos-backend.md#2-fuera-de-alcance) | Motivo, fecha y mes de baja (T3) |
| 60.5 | [Acceso y alcance](../specs/modulo60-activos-fijos-backend.md#2-fuera-de-alcance) | Acceso y separación de 54/58 (T4) |

## Precisiones pendientes de la revisión final

| Módulo | Por confirmar o coordinar |
|---|---|
| 52 | Precio, descuentos y orden del redondeo de base e impuesto. Campos editables. Carrera preexistente de edición/emisión del 51. |
| 53 | Factura frente a boleta, fecha de origen, cobros revertidos, cuenta con crédito aplicado y precedencia de guardas del 51. |
| 54 | Vencimiento futuro y relación con la fecha documentaria. Efecto de pagos revertidos en la anulación. |
| 55 | Reversión atómica de transferencias y saldo de apertura. |
| 56 | Fórmula de diferencia, conjunto requerido para cerrar y corrección de coincidencia antes del cierre. |
| 57 | Saldo inicial, tope autorizado, moneda y reversión de gastos rendidos. Selección de gastos. |
| 58 | Ventana y fechas de ejecución. Corrección de ejecuciones. |
| 59 | Edición de código o padre, y alta o reactivación con padre inactivo. |
| 60 | Moneda, baja retroactiva, atomicidad del lote y redondeo con bases pequeñas. |

Los límites de columnas y mecanismos de persistencia documentan el diseño de revisión. Los mensajes de negocio son literales propuestos. Las precisiones listadas quedan abiertas en el PR.

## Diferencias del 52 frente al backend provisional

| Aspecto | Publicado en `e005686` y `7a2057f` | Spec aprobada |
|---|---|---|
| Acceso | JWT general | JWT de lectura. Escritura con `FINANZAS`/Id 7 o `ADMIN` |
| Numeración | Serie y entero capturados, únicos por tipo/serie/número | NC/ND automáticos y correlativos independientes, sin serie |
| Datos y montos | Positivo global informado, sin líneas | FK a `DetalleFacturaId`, cantidad de 4 decimales, tasa histórica y cálculo por línea |
| Límites | Rango y escala del importe global | Topes acumulados de cantidad y total en créditos emitidos por línea. Débitos sin tope |
| Motivos y fechas | Motivos hasta 500 y emisión como instante UTC | Motivos de 3–300 y fecha de negocio al emitir |
| Estado de factura | Comprueba `Emitida` al crear, editar y emitir | La aprobación exige comprobar al crear y emitir |
| Anulación | Ciclo lógico y guarda 409 del 51. La carrera de edición/emisión sigue pendiente | Conserva ciclo y guarda. Añade la protección de nota aplicada al integrar 53 |
| Esquema | SQL `024` del avance provisional | Reserva `024`, con cambios de modelo y pruebas para el contrato definitivo |

[Anexo del avance provisional](https://github.com/ImLukzy/TitanGlobal-ERP-Supply-Chain-Ecosystem/blob/7a2057f4e1d34abdc8a74a2d1605b529b320e063/docs/specs/modulo52-notas-credito-debito-implementacion.md)
