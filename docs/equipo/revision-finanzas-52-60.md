# Revisión de propuestas de Finanzas — módulos 52 a 60

**Estado.** Nueve propuestas para revisión de Lukas. Responsable de los módulos: Leonel Portillo, según [asignaciones](asignaciones.md#resumen-por-persona). Las decisiones de producto se responden por número en §2 de cada spec.

## Base revisada

| Fuente | Hecho comprobado | Relevancia |
|---|---|---|
| [Enunciado](../enunciado.md#2-los-80-módulos) y [arquitectura](../arquitectura.md#los-80-módulos-8-dominios) | Identifican los nueve módulos del dominio y mencionan su alcance general. | Los ciclos, fórmulas y permisos de las propuestas requieren aprobación. |
| 51 Facturación electrónica, rama publicada `fb84529` | El SQL `007` crea `Facturas` y `DetallesFactura`. La factura o boleta pertenece a un cliente, guarda moneda, total, estado y vencimiento opcional. Las líneas guardan cantidades, importes y porcentaje de impuesto. La emisión y la anulación son estados internos. El PR #48 está fuera de la base documental `e48a190`. | 52 y 53 deben revisar el contrato que se integre. La evidencia local con el 51 temporal está en [52 §7](../specs/modulo52-notas-credito-debito-backend.md#7-registro-de-verificación). |
| 75 Impuestos y tasas, [esquema inicial](../../database/001-esquema-inicial.sql) y [cálculo vigente](../../src/TitanGlobal.Application/UseCases/Modulo75_ImpuestosTasas/CalcularImpuestoUseCase.cs) | `Impuesto` y `TasaImpuesto` guardan identidad y vigencia. El 75 calcula con una tasa activa para la fecha de operación. El 51 guarda el porcentaje que aplicó a cada línea. | Aprobar cuándo usar una tasa histórica o vigente antes de definir importes del 52. Autorizar por separado cualquier cálculo tributario adicional del 54. |
| [Proveedores](../specs/12-modulo11-proveedores-backend.md) y [Órdenes de abastecimiento](../specs/16-modulo15-ordenes-abastecimiento-backend.md) | El 11 tiene un maestro con RUC único. El 15 tiene proveedor, estados y cantidades/precios. Recibir una orden registra el cambio de estado. | El 54 requiere el maestro. La orden puede servir de referencia documental si se aprueba. |
| [Centros de trabajo](../specs/modulo33-centros-trabajo-backend.md) y [Contratos laborales](../specs/modulo62-contratos-laborales-backend.md) | Existen maestros de manufactura y empleados. La spec 62 muestra cómo registrar decisiones aprobadas y evidencias de aceptación. | El 59 propone un catálogo analítico propio. El 57 podría vincular custodio o beneficiario con el maestro de empleados. |
| [Auditoría](../specs/45-modulo77-auditoria-transaccional-backend.md) y [autenticación JWT](../specs/51-autenticacion-jwt.md) | Hay trazabilidad de cambios y autenticación obligatoria para los nuevos recursos. | Cada spec debe indicar qué operaciones requieren roles y qué eventos conservan historial funcional. |

El [borrador del 75](../specs/modulo75-impuestos-tasas-backend.md) aporta el contexto funcional. Para la arquitectura y persistencia de este conjunto rigen las reglas vigentes del repositorio y los contratos del backend integrado.

## Reglas del repositorio

- La implementación aprobada seguirá Controller → UseCase → IUnitOfWork → Repository → DbContext → PostgreSQL. Los casos de uso concretos terminados en `UseCase` se registran por reflexión. Application contiene las validaciones y usa `ReglaNegocioException` y `CodigosEstado`. Los controladores se limitan a HTTP. [Arquitectura](../arquitectura.md#estilo-y-estructura-en-cuatro-capas) · [Guía de módulo](crear-modulo.md)
- Cada módulo agrega su SQL numerado, lo aplica a PostgreSQL local y genera las entidades `partial` y `TitanGlobalDbContext` con el scaffold oficial. Los archivos ya fusionados y los generados se rigen por la política Database First. Los nombres candidatos deben comprobarse con el pluralizador. Si se necesita un mapeo nuevo, documentar el alcance y revisar el diff de las navegaciones inversas. [Spec técnica de scaffold](../specs/52-database-first-scaffold.md)
- Normalizar instantes con `FechasUtc`. Separar el instante de auditoría de la fecha que el negocio usa para corte, vencimiento o periodo. Validar escala, rango y desbordamiento antes de persistir. Los tamaños y escalas candidatos de cada spec se ratifican según los datos requeridos.
- Usar JWT, JSON en camelCase y errores `{ StatusCode, Message }`. Las acciones `POST` y `PUT` sin cuerpo llevan `[PermitirCuerpoVacio]`. Cada literal en las propuestas espera aprobación. La pertenencia del módulo al catálogo de roles sirve de referencia. La autorización de escritura requiere decidir permisos concretos.
- El `IUnitOfWork` actual ofrece `Repository<T>()` y `CompleteAsync()`. El guardado traduce unicidad y FK de PostgreSQL mediante `ViolacionRestriccionException`. Un control previo en LINQ carece de protección contra carreras por sí solo. Donde se aprueben topes, secuencias o cierre concurrente, establecer la garantía en persistencia y probarla con dos contextos reales antes de dar esa operación por cubierta. Si se necesita ampliar el puerto, acordar el cambio y su traducción de errores antes de implementarlo. [Puerto](../../src/TitanGlobal.Domain/Repositories/IUnitOfWork.cs) · [Implementación](../../src/TitanGlobal.Infrastructure/Repositories/Implementation/UnitOfWork.cs)
- El futuro gate incluye compilación sin warnings, reglas de arquitectura, pruebas y `verify.sh`. Los casos HTTP cubren uso nominal, límites, cada literal, JWT, permisos aprobados e inexistencias. PostgreSQL verifica constraints, precisión, carreras y atomicidad. Cada módulo agrega seed idempotente y comprobaciones con `request_check` y su validación Python, hasta `SMOKE OK`. [Guía de seed, smoke y gate](crear-modulo.md#9-seed-y-smoke)

**Numeración.** La [tabla de relevo §8](relevo.md#8-números-de-los-scripts-de-base-de-datos), actualizada por el PR #50, reserva del `010` al `023`. El siguiente número general es `024`. Las nueve specs usan `NNN` y mantienen la numeración definitiva pendiente. Cada rama funcional verifica el siguiente número libre y registra su propia fila al incorporar su script.

## Matriz de dependencias entre módulos

La fila consume al módulo de la columna. **Base** es requisito del núcleo propuesto. **Opcional** corresponde a una integración que espera aprobación. Los guiones indican que el alcance propuesto no requiere esa relación.

| Consumidor | 52 | 53 | 54 | 55 | 56 | 57 | 58 | 59 | 60 | Dependencias externas |
|---|---|---|---|---|---|---|---|---|---|---|
| 52 Notas de crédito y débito | — | — | — | — | — | — | — | — | — | 51 base, 75 según la fuente de tasa aprobada, 76 si se usa numeración común |
| 53 Cuentas por cobrar | Base | — | — | Opcional | — | — | — | — | — | 01 mediante el 51, 51 base |
| 54 Cuentas por pagar | — | — | — | Opcional | — | — | — | Opcional | — | 11 base, 15 o 19 si se aprueban los vínculos de origen, 73 y 75 según alcance |
| 55 Tesorería y bancos | — | — | — | — | — | — | — | — | — | 73 y 74 opcionales |
| 56 Conciliación bancaria | — | — | — | Base | — | — | — | — | — | — |
| 57 Caja chica | — | — | — | Opcional | — | — | — | Opcional | — | 61 y 73 opcionales |
| 58 Presupuestos | — | — | Opcional | Opcional | — | Opcional | — | Base | Opcional | 73 y 80 opcionales |
| 59 Centros de costo | — | — | — | — | — | — | — | — | — | 33, 61 y 73 opcionales |
| 60 Activos fijos | — | — | Opcional | — | — | — | — | Base | — | 80 opcional |

### Contratos que necesitan revisión conjunta

| Relación | Límite del contrato propuesto | Decisiones vinculadas |
|---|---|---|
| 51 → 52 → 53 | El 51 conserva venta y totales originales. El 52 registra la nota. El 53 aplica su efecto sobre una sola cuenta mediante la identidad de la nota. Una nota emitida y una nota aplicada son eventos distintos. | 52.1, 52.2, 52.5, 52.6 y 53.1, 53.3 |
| 53, 54, 57 con 55 | Cada módulo conserva su obligación, aplicación o fondo. El 55 mantiene el movimiento bancario. Antes de añadir un enlace, decidir qué operación crea los registros, cómo se revierte y cómo evitar un efecto duplicado ante reintentos. | 53.5, 54.5, 55.6 y 57.4 |
| 55 → 56 | El 55 conserva los movimientos registrados y sus reversiones. El 56 administra extractos y coincidencias. Acordar la elegibilidad y el efecto de una reversión durante o después del cierre. | 55.2, 55.4 y 56.2, 56.3, 56.4 |
| 59 → 58, 60 y consumidores opcionales | El 59 mantiene la identidad y vigencia del centro. Los consumidores validan elegibilidad e historial según su contrato. Acordar las garantías si la baja bloquea nuevas asignaciones concurrentes. | 59.1, 59.3, 58.2, 60.1, 54.5 y 57.4 |
| 54, 55, 57 o 60 → 58 | La partida y su ejecución pertenecen al 58. Si se conecta una fuente, acordar la identidad del evento que ejecuta. El documento, su pago y una reposición pueden reflejar el mismo hecho económico. | 54.5, 57.4, 58.4 y 60.5 |

## Orden de implementación propuesto

1. **52, 59, 55 y 54.** Empezar el 52 al confirmar el contrato del 51. El maestro 59, la tesorería 55 y la deuda con proveedor 54 se pueden preparar en paralelo tras sus aprobaciones.
2. **53, 56 y 57.** El 53 avanza con el 51 y el 52. El 56 espera las cuentas y movimientos del 55. Caja chica puede avanzar con su propio registro y sumar vínculos al 55 o al 59 cuando se aprueben.
3. **58 y 60.** Ambos se apoyan en el 59. Presupuestos puede iniciar con ejecución explícita. Si el alta de activos se aprueba como fuente automática inicial, terminar el contrato necesario del 60 antes de esa integración.

Una secuencia posible para un solo responsable es **52 → 59 → 55 → 54 → 53 → 56 → 57 → 58 → 60**. Mientras el 51 esté pendiente de integración, la primera tarea aprobada sin esa dependencia puede avanzar.

Después de la revisión de este conjunto, cada módulo parte de su rama de implementación. El primer commit reúne SQL, entidades y persistencia. El segundo reúne DTOs, casos de uso, API y pruebas. Los enlaces opcionales entran en el alcance del módulo consumidor después de acordar el contrato compartido.

## Respuestas que se solicitan a Lukas

Las 44 entradas reproducen el número, el tema y el texto de «Qué debe confirmar Lukas» en §2 de cada propuesta. Se puede responder por número. Para cálculos, adjuntar entradas y resultados esperados. Para permisos, indicar quién puede operar en cada caso.

### [52 Notas de crédito y débito](../specs/modulo52-notas-credito-debito-backend.md#2-fuera-de-alcance)

| Nº | Tema | Qué debe confirmar Lukas |
|---|---|---|
| 52.1 | Tipos y comprobante de origen | Tipos de nota, factura o boleta admitidas, estados elegibles, cuándo se verifica el estado y cómo coordinar la anulación del comprobante con el 51 si existen notas. |
| 52.2 | Estados y transiciones de la nota | Alta directa o borrador, transiciones, fechas, motivo y efectos de anular una nota. |
| 52.3 | Motivos y permisos | Catálogo o texto libre, códigos, obligatoriedad y longitudes por campo, y roles por operación. |
| 52.4 | Numeración | Ámbito de unicidad, series admitidas, captura manual o correlativo automático, momento de asignación y reintentos. |
| 52.5 | Líneas y cálculo | Modalidad global o por línea por motivo, componentes de entrada y calculados, reglas tributarias aplicables, base, exenciones, fuente de tasa, fórmula, escala y orden de redondeo. |
| 52.6 | Topes y aplicación en cartera | Qué estados computan, si hay topes por monto o cantidad, efecto de débitos y anulaciones, qué nota es elegible para cartera y cómo se coordina una anulación después de aplicarla. Confirmar la precedencia de validaciones y errores. |

### [53 Cuentas por cobrar](../specs/modulo53-cuentas-por-cobrar-backend.md#2-fuera-de-alcance)

| Nº | Tema | Qué debe confirmar Lukas |
|---|---|---|
| 53.1 | Origen | Factura o boleta, estados y condiciones de venta elegibles, apertura explícita o automática, vencimiento ausente y cardinalidad si hay cuotas o letras. |
| 53.2 | Cobros | Medios y comprobantes exigidos, sobrepago y saldo a favor, reversión parcial o total, fecha admitida y respuesta a un reintento. |
| 53.3 | Notas y anulaciones | Estado de nota elegible, crédito mayor al saldo, efectos de anular factura o nota ya aplicada, reaplicación y anulación de la cuenta. |
| 53.4 | Cartera | Fecha y zona de corte, vencimiento en el día, agrupaciones, tramos y alcance de letras o mora. |
| 53.5 | Acceso e integración | Roles para consultar, registrar y revertir, y si debe existir contabilización bancaria coordinada en la misma operación. |

### [54 Cuentas por pagar](../specs/modulo54-cuentas-por-pagar-backend.md#2-fuera-de-alcance)

| Nº | Tema | Qué debe confirmar Lukas |
|---|---|---|
| 54.1 | Fuente | Clases de documento, proveedores admisibles, cuándo nace la deuda, relación con órdenes o recepciones y si se permiten cuotas. |
| 54.2 | Identidad e importe | Unicidad y normalización, series opcionales, ámbito por emisor o sucursal, moneda, componentes, impuestos y retenciones si se incluyen. |
| 54.3 | Pagos y anulaciones | Anticipos, sobrepagos, pagos agrupados, formas y evidencias requeridas, reversión parcial, efecto de la anulación y reintentos. |
| 54.4 | Vencimiento y acceso | Tratamiento de vencimiento ausente, tramos, roles de lectura y escritura y si se exige aprobación antes de pagar. |
| 54.5 | Integraciones | Si se requiere pago coordinado con 55, clasificación por centro y uso de una obligación como fuente para el 58 o el 60. |

### [55 Tesorería y bancos](../specs/modulo55-tesoreria-bancos-backend.md#2-fuera-de-alcance)

| Nº | Tema | Qué debe confirmar Lukas |
|---|---|---|
| 55.1 | Cuenta | Ámbito de unicidad, uso de sucursal, datos a almacenar o mostrar, catálogo de monedas y cambios permitidos al haber movimientos. |
| 55.2 | Movimientos y saldo | Tipos, saldo negativo, fechas admitidas, matriz de roles, aprobaciones y sentido de cada evento. |
| 55.3 | Transferencias | Transferencias entre monedas, comisiones, quién puede confirmar y si se admite reversión. |
| 55.4 | Identidad y conciliación | Ámbito de reintentos, qué efectos tiene revertir un movimiento ya conciliado y si requiere coordinación con 56. |
| 55.5 | Cheques | Si entran en esta entrega, numeración, estados, cuándo afectan saldo, rechazo, anulación y relación con 56. |
| 55.6 | Vínculos | Qué canales enlazar, creación coordinada o manual, cardinalidad y orden transaccional de los cambios. |

### [56 Conciliación bancaria](../specs/modulo56-conciliacion-bancaria-backend.md#2-fuera-de-alcance)

| Nº | Tema | Qué debe confirmar Lukas |
|---|---|---|
| 56.1 | Extractos | Formatos admitidos, identidad de extracto e ítem, duplicados entre extractos, periodos superpuestos y correcciones a una carga. |
| 56.2 | Coincidencias | Criterio de signos, tolerancias, ventana de fechas, agrupaciones, coincidencia automática y elegibilidad de los movimientos del 55, incluidos los vinculados a una reversión. |
| 56.3 | Cierre | Qué pendientes bloquean, fórmula y tolerancia, quién cierra y si hay reapertura o cierre parcial. |
| 56.4 | Correcciones y acceso | Política frente a reversión posterior, autorización de registrar, asociar, retirar y cerrar, y efectos sobre conciliaciones cerradas. |

### [57 Caja chica](../specs/modulo57-caja-chica-backend.md#2-fuera-de-alcance)

| Nº | Tema | Qué debe confirmar Lukas |
|---|---|---|
| 57.1 | Fondo | Ámbito, sucursal, custodio como texto o empleado, monto autorizado, cambio de responsable y condiciones de cierre. |
| 57.2 | Movimientos | Tipos, gastos y comprobantes admitidos, límites, saldo negativo, reversión, devolución parcial y tratamiento de sobrantes o faltantes. |
| 57.3 | Rendiciones | Qué operaciones se rinden, plazos, anticipos pendientes, estados, corrección o reapertura y si se puede volver a incluir un egreso. |
| 57.4 | Acceso e integración | Roles y separación de funciones, validez de una reposición bancaria, obligatoriedad del centro de costo y evento que cuenta como ejecución presupuestaria. |

### [58 Presupuestos](../specs/modulo58-presupuestos-backend.md#2-fuera-de-alcance)

| Nº | Tema | Qué debe confirmar Lukas |
|---|---|---|
| 58.1 | Periodo, versión y moneda | Calendario, ámbito por empresa o sucursal, versiones simultáneas, unicidad y moneda por presupuesto o partida. |
| 58.2 | Partidas | Estructura, jerarquía, centro activo en alta o también en ejecución, categorías admitidas y si existe otra dimensión aprobada. |
| 58.3 | Ciclo | Aprobación, responsables, revisiones, traspasos o ampliaciones, reapertura y relación con CierresPeriodo (80). |
| 58.4 | Ejecución y disponible | Si cuenta compromiso, devengo, pago u otro evento; fuentes del 54, 55, 57 o 60, identidad, sobrepresupuesto y correcciones. |
| 58.5 | Consultas y acceso | Agrupaciones, consulta histórica, zonas de corte, roles y alcance por centro. |

### [59 Centros de costo](../specs/modulo59-centros-costo-backend.md#2-fuera-de-alcance)

| Nº | Tema | Qué debe confirmar Lukas |
|---|---|---|
| 59.1 | Identidad y organización | Unicidad global o por empresa/sucursal, formato y cambios de código, datos y fuente del responsable, y si se requiere relación con los centros de trabajo. |
| 59.2 | Jerarquía | Árbol, profundidad, tipos de centro, movilidad entre padres, reglas contra ciclos y descendientes de un centro inactivo. |
| 59.3 | Vigencia y consumidores | Qué consumos requieren centro activo (58, 60 y opcionalmente 54 o 57), qué hacer con operaciones vigentes al desactivar, cómo conservar historia y si habrá reactivación. |
| 59.4 | Acceso y reportes | Roles de mantenimiento o lectura y si el 59 debe reportar importes o dejar su agregación a cada módulo que los origina. |

### [60 Activos fijos](../specs/modulo60-activos-fijos-backend.md#2-fuera-de-alcance)

| Nº | Tema | Qué debe confirmar Lukas |
|---|---|---|
| 60.1 | Alta y asignación | Qué se capitaliza, datos y evidencia, categorías, unicidad por ámbito, obligatoriedad y cambios del centro de costo. |
| 60.2 | Base, método y moneda | Base capitalizable, valores admitidos, método, vida útil, residual, escala, redondeo y tratamiento del último periodo. |
| 60.3 | Calendario | Inicio desde puesta en servicio u otro hito, prorrateo, periodos omitidos, cálculo en lote, cierres y fechas de corte. |
| 60.4 | Baja y corrección | Motivos y estados, última depreciación, reversión o reapertura de bajas y corrección de periodos ya registrados. |
| 60.5 | Acceso e integraciones | Quién registra, deprecia y autoriza la baja. Si 54 prueba el origen y si el alta, la compra u otro evento alimenta el 58. |
