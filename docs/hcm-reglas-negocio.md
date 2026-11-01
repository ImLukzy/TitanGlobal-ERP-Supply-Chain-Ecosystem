# Capital Humano y Nóminas (61–70): reglas de negocio propuestas

Estado: **propuesta v0, pendiente de aprobación**. Ninguna regla se implementa hasta que Lukas la apruebe y exista la spec del módulo (`docs/specs/_TEMPLATE.md`).

## 0. Supuestos a confirmar

| # | Supuesto | Motivo |
|---|---|---|
| S1 | Legislación peruana (régimen laboral privado): AFP/ONP, quinta categoría, 30 días de vacaciones por año, gratificaciones y CTS fuera de alcance v0. | `enunciado.md` usa IGV, detracciones y percepciones, que son de Perú. |
| S2 | Moneda única PEN en v0. | Evita conversión en planillas; el módulo 74 aún no existe. |
| S3 | Planilla mensual en v0. | Más simple; quincenal se añade después. |
| S4 | Parámetros legales (tasas, topes, tramos) viven en tablas del propio módulo 66 con seed, hasta que exista ParametrosSistema (79). | 79 está pendiente. |
| S5 | Sin autenticación real: el rol llega por `X-User-Role` (`docs/contexto-proyecto.md`). Las reglas de aprobación usan el rol, no un usuario logueado. | Decisión vigente del proyecto. |

## 1. Reglas transversales

- **T1.** Un empleado no se borra. Se da de baja con fecha de cese y motivo.
- **T2.** Un empleado cesado no puede recibir marcaciones, turnos, ausencias, préstamos ni entrar en planilla de un periodo posterior a su cese.
- **T3.** Los datos de un periodo cerrado (planilla aprobada, evaluación cerrada, marcación calculada) no se modifican. Las correcciones se registran como movimiento nuevo.
- **T4.** Todo importe monetario es decimal con 2 decimales. Ningún cálculo usa `double`.
- **T5.** Las fechas de negocio son `DateOnly`. Las marcaciones tienen instante UTC.
- **T6.** Los errores siguen el formato `{ StatusCode, Message }` con mensajes literales definidos en la spec de cada módulo.

## 2. Módulos

### 61 Empleados (base)
- **E1.** Campos: legajo (autogenerado `EMP-000001`, único), tipo y número de documento (único en conjunto), nombres, apellidos, fecha de nacimiento, fecha de ingreso, cargo, área (texto en v0), email, teléfono, estado.
- **E2.** Estados: `Activo`, `Cesado`. Pasar a `Cesado` exige fecha de cese ≥ fecha de ingreso y motivo.
- **E3.** Edad mínima al ingreso: 18 años.
- **E4.** Al cesar se cierran los contratos vigentes en la fecha de cese (efecto en 62).

### 62 ContratosLaborales
- **C1.** Modalidades: `Indeterminado`, `PlazoFijo`, `Formativo`. Solo `PlazoFijo` y `Formativo` tienen fecha de fin; `Indeterminado` no la tiene.
- **C2.** Un empleado tiene como máximo un contrato vigente en cualquier fecha. Sin solapamientos.
- **C3.** Sueldo básico > 0. Fecha de fin > fecha de inicio.
- **C4.** Renovación: crea contrato nuevo con inicio = fin anterior + 1 día. El anterior queda `Renovado`.
- **C5.** Adenda: cambia cargo o sueldo desde una fecha de vigencia dentro del contrato. No cambia modalidad ni fechas.
- **C6.** Estado `Vencido` se asigna cuando la fecha de fin pasa sin renovación (proceso diario).

### 63 AsistenciaBiometrica
- **A1.** Una marcación pertenece a un empleado activo con contrato vigente en esa fecha (T2).
- **A2.** Duplicado: misma persona, mismo tipo y misma hora ±1 minuto se descarta y se cuenta.
- **A3.** Importación desde el archivo del reloj. Idempotente por (dispositivo, número de registro).
- **A4.** Tardanza = minutos de entrada posteriores a la hora del turno (64) más su tolerancia.
- **A5.** Horas extra = horas trabajadas fuera de la jornada programada, con tope diario configurable (propuesta: 4 h).
- **A6.** Jornada sin entrada o sin salida genera incidencia `SinMarcacion`, no cuenta como asistencia.
- **A7.** Las horas extra se calculan, pero requieren aprobación de `Supervisor` antes de ir a planilla.

### 64 TurnosHorarios
- **H1.** Turno: hora de entrada, hora de salida, tolerancia (0–30 min), días laborables, tipo `Fijo` o `Rotativo`.
- **H2.** Turno rotativo: ciclo de N días de trabajo y M de descanso, con fecha de inicio.
- **H3.** Hora de salida posterior a la de entrada; si es menor, el turno cruza medianoche (indicador explícito).
- **H4.** Un empleado tiene un solo turno vigente por fecha.
- **H5.** Cambiar el turno no altera marcaciones ya calculadas (T3).

### 65 VacacionesPermisos
- **V1.** Tipos: `Vacaciones`, `PermisoConGoce`, `PermisoSinGoce`, `LicenciaMaternidad`, `LicenciaPaternidad`, `DescansoMedico`.
- **V2.** Vacaciones: 30 días calendario por año completo de servicio (devengo mensual de 2,5 días).
- **V3.** Saldo vacacional = devengado − gozado − programado. Una solicitud no puede superar el saldo.
- **V4.** Fecha de inicio ≤ fecha de fin. No solapa con otra ausencia aprobada del mismo empleado.
- **V5.** Estados: `Pendiente` → `Aprobada` | `Rechazada`. Solo se cancela antes de la fecha de inicio.
- **V6.** Ausencia sin goce descuenta en planilla (66).

### 66 Planillas
- **P1.** Periodo mensual por tipo de planilla. Un periodo no se duplica.
- **P2.** Estados: `Borrador` → `Calculada` → `Aprobada` → `Pagada`. Una planilla `Aprobada` es inmutable (T3). Correcciones van en periodo complementario.
- **P3.** Ingresos: sueldo básico prorrateado por días laborados, horas extra aprobadas (63), bonos registrados.
- **P4.** Descuentos: sistema de pensión (AFP o ONP, parámetros en tabla), aporte de salud, renta de quinta categoría (tramos en tabla), adelantos y préstamos (67), ausencias sin goce (65).
- **P5.** Neto = ingresos − descuentos. Si el neto es negativo, el empleado no entra al cálculo y se marca como incidencia.
- **P6.** Boleta: un documento por empleado y periodo. Se genera al aprobar.

### 67 AdelantosPrestamos
- **L1.** Monto > 0. Cuotas N ≥ 1. Cuota = monto / N, redondeada a 2 decimales; la última cuota absorbe la diferencia.
- **L2.** Cuota ≤ 30% del sueldo neto estimado (parámetro configurable).
- **L3.** Descuento solo por planilla (66). No se descuenta más que el saldo.
- **L4.** Estados: `Activo`, `Cancelado` (saldo 0), `Anulado` (solo antes del primer descuento).
- **L5.** El saldo pendiente al cese pasa a la liquidación final (fuera de alcance v0).

### 68 EvaluacionDesempeno
- **D1.** Ciclo de evaluación con periodo (semestral o anual) y fechas de apertura y cierre.
- **D2.** Competencias con peso; la suma de pesos del ciclo es 100%.
- **D3.** Calificación en escala 1 a 5. Puntaje final = Σ(peso × calificación).
- **D4.** Una evaluación por empleado y ciclo. El evaluador no puede ser el propio evaluado.
- **D5.** Solo se evalúa a empleados con contrato vigente durante el periodo.
- **D6.** Estados: `Borrador` → `Enviada` → `Cerrada`. Cerrada es inmutable (T3).

### 69 Capacitaciones
- **K1.** Catálogo de cursos: nombre, horas, tipo, proveedor (texto).
- **K2.** Plan anual: curso, fechas, cupo. Inscripciones ≤ cupo.
- **K3.** No se inscribe a empleados cesados.
- **K4.** Certificación: fecha de emisión ≤ fecha de vencimiento (opcional). Vencida se marca como tal.
- **K5.** Horas de capacitación acumuladas por empleado y año.

### 70 SeguridadSaludOcupacional
- **S1.** EPP: catálogo y entrega al empleado con fecha y vida útil. Entrega vencida se marca.
- **S2.** Examen médico: tipo (`Ingreso`, `Periódico`, `Retiro`), resultado (`Apto`, `AptoConRestricciones`, `NoApto`), fecha y vencimiento.
- **S3.** Incidente: tipo (`AccidenteSinBaja`, `AccidenteConBaja`, `IncidenteSinLesion`, `EnfermedadOcupacional`), severidad, fecha, empleado y días de baja.
- **S4.** `AccidenteConBaja` exige días de baja ≥ 1 y fecha ≤ hoy.
- **S5.** Cerrar un incidente exige acción correctiva registrada.

### 71 Usuarios (ya implementado en backend)
- **U1.** Siempre queda un ADMIN activo (`docs/contexto-proyecto.md`).
- **U2.** Propuesta nueva: al cesar a un empleado (E2), su usuario asociado pasa a inactivo. Requiere decisión (ver Preguntas).

## 3. Preguntas abiertas

1. ¿Apruebas S1–S5 como base?
2. ¿Al cesar un empleado (E2) desactivamos su usuario de 71 automáticamente (U2)?
3. ¿El 61 Empleados debe servir de maestro para el 08 ComisionesVendedores, que hoy tiene su propio maestro de vendedores?
4. ¿Quién aprueba horas extra y ausencias: el rol `Supervisor` o el jefe de área (que hoy no existe en el modelo)?
