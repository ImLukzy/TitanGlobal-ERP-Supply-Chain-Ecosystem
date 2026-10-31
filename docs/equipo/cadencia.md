# Cadencia del equipo

## 1. Meta

**3 módulos terminados por semana en todo el equipo**, no por persona. Con 6 personas desarrollando, cada una cierra aproximadamente un módulo cada dos semanas.

Un módulo cuenta como terminado solo si tiene:

- Backend y pantalla según la spec.
- Tests y gate en verde.
- QA visual a 375 y 1280 px, en claro y en oscuro.
- Evidencia en §7 de la spec.
- PR revisado e integrado en `main`.
- Demo desplegada y comprobada.

No cuentan: un backend sin pantalla, una pantalla sin QA, un módulo sin integrar o un PR abierto.

## 2. Roles

| Rol | Quién | Qué hace |
|---|---|---|
| Owner | una persona por módulo | Escribe la spec, implementa, prueba y deja el PR listo |
| Revisor | otro compañero | Lee solo la spec y el diff; aprueba o pide cambios |
| QA | el revisor u otra persona | Recorre los criterios contra la API y PostgreSQL locales |
| Integración | Lukas | Revisa la spec, integra en `main` con el gate en verde y despliega |
| Coordinador de migraciones | rota cada semana | Ordena las migraciones y evita que dos personas generen una a la vez |

El owner no revisa su propio módulo. El revisor se acuerda el lunes y se anota en el issue.

## 3. Semana tipo

| Día | Actividad |
|---|---|
| Lunes | Reunión de 30 min: se confirman los 3 módulos, owners, revisores y bloqueos. Lukas revisa las specs de la semana. |
| Martes y miércoles | Implementación. El miércoles por la tarde se abre el PR aunque falten detalles. |
| Jueves | Revisión, correcciones, migraciones e integración. El gate tiene que estar verde. |
| Viernes | QA visual, evidencia en la spec, demo de 30 min y retro de 15 min. |

Las specs de la semana siguiente se revisan el viernes o el lunes, nunca el miércoles.

## 4. Límites de trabajo en curso

- Máximo **3 módulos en curso** y **3 en revisión o QA** al mismo tiempo.
- Un owner no toma otro módulo hasta cerrar el suyo.
- Si un módulo no llega al viernes, pasa a la semana siguiente. **No se abre un cuarto módulo para compensar.**
- Si dos semanas seguidas no se llega a 3, se reduce el alcance o se reasigna; no se añade presión.

## 5. Tablero

Un issue por módulo en GitHub Projects, con estas columnas:

`Backlog` → `Spec` → `En curso` → `En revisión` → `QA` → `Hecho`

Cada issue lleva:

- Número y nombre del módulo.
- Owner, revisor y semana.
- Enlace a la spec.
- Dependencias.
- Criterios de aceptación.
- Checklist de la Definition of Done.

Etiquetas: dominio (`comercial`, `compras`, `wms`, `mrp`, `tms`, `finanzas`, `hcm`, `gobierno`), `backend`, `frontend`, `bloqueado`.

## 6. Definition of Done

Antes de mover un issue a `Hecho`:

- [ ] Spec aprobada y §6 marcado.
- [ ] Backend y pantalla implementados según la spec.
- [ ] Tests nuevos para reglas y errores.
- [ ] `./scripts/verify.sh` en verde.
- [ ] `./scripts/smoke-postgres.sh` → `SMOKE OK` si cambió la base de datos.
- [ ] QA a 375 y 1280 px, en claro y en oscuro, con capturas.
- [ ] Evidencia en §7 de la spec.
- [ ] PR revisado por un compañero y por Lukas.
- [ ] Integrado en `main` y desplegado en la demo.

## 7. Primeras semanas

La semana 0 es de arranque: onboarding, QA de las pantallas ya integradas y arranque de las dos que faltan. No cuenta para la meta.

| Semana | Módulos comprometidos | Owners | Revisores |
|---|---|---|---|
| 0 | QA de las pantallas 08, 09, 10, 77 y 78 (ya integradas) · 72 RolesPermisos · 71 Usuarios | Maxi · Sergio · Alexander | Lukas y un compañero |
| 1 | 75 ImpuestosTasas · 33 CentrosTrabajo · 41 VehiculosFlota | Leonel · Antony · Kelvin | Antony · Kelvin · Leonel |
| 2 | 22 CategoriasProducto · 12 EvaluacionProveedores · 61 Empleados | Maxi · Sergio · Alexander | Sergio · Alexander · Maxi |
| 3 | 51 FacturacionElectronica · 31 ListasMateriales · 42 Conductores | Leonel · Antony · Kelvin | Kelvin · Maxi · Sergio |
| 4 | 25 LotesSeries · 14 Licitaciones · 62 ContratosLaborales | Maxi · Sergio · Alexander | Alexander · Leonel · Antony |

El orden sigue las dependencias de [`asignaciones.md`](asignaciones.md). Si un módulo se adelanta o se atrasa, se ajusta la tabla el lunes, no durante la semana.

## 8. Métricas

Cada viernes se anotan en el issue de la semana:

- Módulos terminados (meta: 3).
- Tiempo medio de PR abierto a integrado (meta: menos de 48 h).
- Gates en rojo y causas.
- Bugs encontrados en QA.

Si la media de dos semanas baja de 3, se recorta el alcance del bloque o se reasigna un owner. No se compensa abriendo más frentes.

## 9. Bloqueos

- **Más de 4 horas atascado:** pide ayuda en el canal con el comando, el error y lo que ya probaste.
- **Más de un día bloqueado:** avisa a Lukas para reasignar o reducir el alcance.
- **Conflicto de migración:** detente, actualiza `main`, borra tu migración y vuelve a generarla. Nunca edites el snapshot a mano.
- **Dos personas con migraciones:** espera tu turno; el coordinador de la semana decide el orden.

## 10. Protección de `main`

GitHub Free no permite proteger ramas en un repositorio privado. Mientras siga así:

- Nadie hace push directo a `main`.
- Todo cambio entra por pull request con el gate de `.github/workflows/verify.yml` en verde.
- Lukas revisa e integra.

Si el owner activa GitHub Education o Pro, se configura `main` con:

- Requerir pull request antes de integrar.
- Requerir el check `Verificación / gate`.
- Descartar aprobaciones obsoletas.
- Requerir al menos una aprobación.
