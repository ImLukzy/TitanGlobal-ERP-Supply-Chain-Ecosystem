# Especificación: módulo 41 — unicidad de placas

## 1. Objetivo
Completar el pendiente de Kelvin en `docs/equipo/relevo.md` §1: impedir placas duplicadas sin distinguir mayúsculas en altas, modificaciones y escrituras concurrentes, conservando los mensajes del contrato y el formato de la placa.

Responsable: Kelvin Pinto. Estado: propuesta, pendiente de aprobación de Lukas.

## 2. Fuera de alcance
Conductores, otros módulos, entidades base, middleware, pipeline, scripts ya integrados y archivos generados editados a mano. No se aplica SQL a producción. No se borran ni fusionan automáticamente vehículos existentes.

## 3. Archivos afectados
- `database/021-vehiculos-flota-placa-unica.sql`: índice único sobre `lower(btrim("Placa"))`, además del índice original que utiliza el seed.
- `docs/equipo/relevo.md` §8: número oficial 021, renombrado desde 010 por indicación de Lukas; 010 corresponde a Acuerdos de abastecimiento.
- `src/TitanGlobal.Application/UseCases/Modulo41_VehiculosFlota/CrearVehiculoFlotaUseCase.cs` y `ActualizarVehiculoFlotaUseCase.cs`: comparación normalizada antes de guardar.
- `tests/TitanGlobal.Api.Tests/Modulo41_VehiculosFlota/VehiculosFlotaApiTests.cs`: pruebas de altas y modificaciones con variantes de mayúsculas.
- `scripts/smoke-postgres.sh`: comprobaciones HTTP con sus validadores para POST/PUT duplicados y cambio de mayúsculas de la propia placa.
- `src/TitanGlobal.Domain/Models/` y `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs`: solo si el scaffold produce diferencias; nunca editar manualmente.

## 4. Diseño y lógica
1. Se mantienen Trim, obligatoriedad, longitud máxima y orden de validación de la spec backend.
2. Una placa con las mismas letras y distintos espacios exteriores o mayúsculas pertenece al mismo identificador. La comparación usa `Trim().ToLower()` en la consulta y `ToLowerInvariant()` para el parámetro. La placa se guarda recortada, conservando sus mayúsculas originales.
3. En PUT se excluye el propio Id. Cambiar exclusivamente las mayúsculas de la propia placa es válido.
4. Duplicado: 409 y mensaje literal `Ya existe un vehículo con la placa {placa}.`, donde placa es el valor solicitado ya recortado. No se expone `Detail`.
5. El índice único protege también las carreras entre escrituras. Se conserva la traducción de `ViolacionRestriccionException` de unicidad; Application no captura excepciones de EF/Npgsql.
6. Si ya existen duplicados normalizados, crear el índice falla y se revierte la transacción de aplicar-bd. Se requiere revisión de esos datos; el script no elimina registros.
7. El índice de expresión puede ser omitido por el scaffold de EF; permanece en PostgreSQL y su funcionamiento se verifica contra la base real. El índice original se mantiene y no cambia la longitud ni el tipo de Placa.

## 5. Criterios de aceptación
| # | Criterio | Verificación |
|---|---|---|
| A1 | POST/PUT duplicados con distintas mayúsculas devuelven 409 literal | integración y smoke |
| A2 | PUT de la propia placa con otras mayúsculas devuelve 200 y conserva lo solicitado | integración y smoke |
| A3 | No se admiten duplicados en escritura directa/concurrente | índice único comprobado en PostgreSQL |
| A4 | Esquema aplicado y código generado actualizado | aplicar-bd y scaffold, sin edición manual |
| A5 | Gate y smoke | verify en verde y SMOKE OK |
| A6 | Pantalla comprobada contra API/PostgreSQL reales | evidencia frontend §7, 375/1280 px y claro/oscuro |

## 6. Checklist de ejecución
- [ ] Aprobación de Lukas.
- [x] Script, aplicación y scaffold.
- [x] Casos de uso, pruebas de integración y smoke.
- [x] Gate y comprobación visual real, evidencia en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-21 | A1–A3 | Aprobados | Siete casos nuevos de integración; smoke con POST/PUT en minúsculas, cambio de la propia placa y escritura directa rechazada por índice/SQLSTATE 23505 |
| 2026-11-21 | A4 | Aprobado | `aplicar-bd.sh --reiniciar`: 001, 008 y 010; `scaffold.sh` ejecutado y `SCAFFOLD AL DÍA`, sin cambios de modelo |
| 2026-11-21 | A5 | Aprobado | `verify.sh`: 1090 pruebas backend, 582 frontend, compilación sin errores ni advertencias; `smoke-postgres.sh`: `SMOKE OK`. Vite conserva el aviso existente de bundle superior a 500 kB |
| 2026-11-21 | A6 | Aprobado | Pantalla contra API y PostgreSQL reales en 375/1280 px, claro/oscuro; conflicto con placa en minúsculas → 409, conserva valores; alta/edición/baja reales correctas; capturas y resultados en `../evidencias/modulo41/` |

El problema del entorno de .NET se resolvió instalando SDK 10.0.404 y runtime 10.0.15: la DLL de serialización de la instalación anterior tenía cero bytes. La prueba del catálogo requirió el checkout local de `docs/arquitectura.md` con LF, idéntico al contenido Git; no se cambió ninguna prueba de otro módulo. Docker Desktop y PostgreSQL 16 se instalaron/iniciaron localmente. No se aplica nada a producción ni se declara integración/despliegue.
