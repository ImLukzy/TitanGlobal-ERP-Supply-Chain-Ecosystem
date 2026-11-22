# Propuesta: módulo 47 — Control de combustible (backend)

## 1. Objetivo
Responsable Kelvin Pinto, según asignaciones. Orden 41 → 42 → 48 → 43 → 44 → 45 → 46 → **47** → 49 → 50 → 73. El enunciado pide «vales, rendimiento por km». Proponer registro manual de vales y cálculo de importe, distancia y rendimiento de un intervalo de odómetro declarado.

Kelvin autoriza continuar localmente como los módulos anteriores. **La revisión y aprobación de Lukas permanecen pendientes**. Rama independiente `kelvin/modulo47-control-combustible` desde origin/main actualizado; sin push, PR, integración, despliegue ni cambios en main o las otras ramas.

## 2. Fuera de alcance y decisiones propuestas
- Placa como texto histórico normalizado a mayúsculas, sin FK al 41 ni cambios en vehículos, conductores o inventarios. No generar correlativos del76 ni asientos/pagos de Finanzas. No modificar entidades congeladas, middleware, pipeline, dependencias o pantallas ajenas.
- Combustibles Diesel/Gasolina, ambos medidos en litros. GLP/GNV y conversiones de unidades quedan fuera hasta definir su contrato. Moneda ingresada como tres letras ASCII, normalizada a mayúsculas; no acredita existencia en un catálogo ni consulta el74.
- Un vale vincula los litros declarados con un intervalo manual de odómetro. El rendimiento es **orientativo**, no medición automática de combustible consumido ni método de tanque lleno. Sin inferir recorridos entre vales, impedir solapamientos, validar capacidad, detectar fraude o calificar rendimientos.
- Borrador permite odómetro final pendiente. Confirmado requiere intervalo completo; Confirmado y Anulado son de solo lectura. Anular desde cualquiera de los dos primeros estados conserva datos y confirmación previa. Solo borrar Borrador; sin reabrir.
- Guardar una operación con una llamada CompleteAsync; índice SQL protege duplicados concurrentes. No garantizar resolución de ediciones/transiciones simultáneas. Todas las decisiones de este apartado quedan propuestas para Lukas.

## 3. Archivos afectados
| Ruta | Acción |
|---|---|
| `database/017-control-combustible.sql` | Crear; reservas010–016 de ramas independientes contrastadas |
| `src/TitanGlobal.Domain/Models/ValeCombustible.cs`, `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | Generar solo mediante scaffold |
| `src/TitanGlobal.Api/Configuration/run/PluralizadorEspanol.cs` y `tests/TitanGlobal.Api.Tests/Arquitectura/PluralizadorEspanolTests.cs` | Mapeo explícito ValesCombustible → ValeCombustible y prueba propia; no editar la entidad generada |
| `src/TitanGlobal.Application/DTOs/Modulo47_ControlCombustible/*.cs` | GuardarValeRequestDto, AnularValeRequestDto, ValeListadoDto, ValeDetalleDto, PaginaValesDto, ResumenValesDto |
| `src/TitanGlobal.Application/UseCases/Modulo47_ControlCombustible/*.cs` | CrearValeUseCase, ActualizarValeUseCase, EliminarValeUseCase, ObtenerValeUseCase, ListarValesUseCase, ConfirmarValeUseCase, AnularValeUseCase, ResumenValesUseCase, ValidacionVale y CalculoVale |
| `src/TitanGlobal.Api/Controllers/Modulo47_ControlCombustible/ControlCombustibleController.cs` | Controlador delgado |
| `tests/TitanGlobal.Api.Tests/Modulo47_ControlCombustible/` | Integración por literal/límite, cálculos y traducción de restricciones |
| `scripts/seed-dev.sql`, `scripts/smoke-postgres.sh` | Añadir seed y smoke propios, sin alterar otros bloques |
| `docs/equipo/asignaciones.md`, `docs/equipo/relevo.md`, specs y `docs/evidencias/modulo47/` | Estado y resultados reales |

## 4. Contrato y reglas
### Datos
ValesCombustible: Id identity; Codigo varchar20 único lower(btrim), Placa varchar20 recortada/mayúsculas, Estacion varchar150 requerida; FechaCarga timestamptz; TipoCombustible varchar15 (Diesel/Gasolina); Litros y PrecioLitro numeric(10,3), ambos0.001–9999999.999; Moneda varchar3 A–Z; OdometroInicial integer0–999999999; OdometroFinal integer nullable en el mismo rango y, si existe, mayor que Inicial. Observaciones varchar500 nullable. Estado varchar15 default Borrador; FechaConfirmacion/FechaAnulacion timestamptz nullable; MotivoAnulacion varchar500 nullable.

Checks SQL de límites, textos de cabecera recortados/no vacíos, placa/moneda normalizadas, combustibles, intervalo y coherencia de estado/fechas/motivo. Confirmado exige final, fecha de confirmación y sin anulación; Borrador sin fechas de transición/motivo; Anulado con motivo/fecha de anulación, conservando confirmación si existía. No almacenar cálculos redundantes.

Application normaliza mediante Trim, placa/moneda a mayúsculas y combustible a su valor canónico sin distinguir mayúsculas; SQL exige formato, no transforma escrituras directas. Opcionales vacíos a null. FechaCarga DateTime? en DTO: ausencia/default se rechaza; FechasUtc normaliza preservando el instante, sin zona equivale UTC. Fechas de transición del servidor en UTC. No exigir relación con «hoy».

### Cálculos exactos
Importe = Litros × PrecioLitro, redondeado a dos decimales, mitades hacia fuera de cero. KilometrosRecorridos = Final − Inicial; si final es null, distancia y rendimientos null. KmPorLitro = distancia/Litros y LitrosPor100Km = Litros×100/distancia, ambos redondeados a tres decimales con el mismo criterio. Calcular con decimal; no redondear entradas. Salidas textuales exactas: litros/precio3 decimales, importe2 y rendimientos3; distancia y odómetros como enteros.

No sumar importes de monedas distintas ni promediar rendimientos sin ponderación. Resumen solo por conteos de estado. Un rendimiento redondeado0.000 dentro de los límites matemáticos no se transforma en error.

### API autenticada
Prefijo `/api/v1/control-combustible`, ocho operaciones:
| Operación | Contrato |
|---|---|
| GET / | busqueda por código/placa/estación; estado y tipoCombustible opcionales; pagina=1, tamanoPagina=20 máximo100; orden Codigo/Id, filtros antes de paginar y sin overflow |
| GET /{id} | Detalle con cálculos, observaciones, fechas/motivo |
| POST / | `{codigo,placa,estacion,fechaCarga,tipoCombustible,litros,precioLitro,moneda,odometroInicial,odometroFinal?,observaciones?}`; Borrador, 200 `{id}` |
| PUT /{id} | Mismo cuerpo completo, solo Borrador; 200 `{id}` |
| DELETE /{id} | Solo Borrador; 204 |
| POST /{id}/confirmacion | Sin cuerpo, PermitirCuerpoVacio; Borrador con final → Confirmado; 200 `{id}` |
| POST /{id}/anulacion | `{motivo}`; Borrador/Confirmado → Anulado; 200 `{id}` |
| GET /resumen | `{total,borradores,confirmados,anulados}` agregado en consulta |

Página `{items,total,pagina,tamanoPagina}`; item todos los campos editables salvo observaciones, más id/estado/importe/kilometrosRecorridos/kmPorLitro/litrosPor100Km. Detalle agrega observaciones y fechas/motivo de transición. Nunca exponer entidades. Inputs litros/precio numéricos JSON; outputs decimales textuales. Todos los endpoints exigen JWT.

### Validaciones y mensajes literales
En edición/baja/transición: buscar primero (404), luego estado (409), después datos; duplicado al final de alta/edición. Validar todo antes de modificar entidades.

404: `El vale de combustible {id} no existe.`

Texto requerido, en orden Codigo/Placa/Estacion: 400 `El campo {etiqueta} es obligatorio.` y `El campo {etiqueta} no puede superar {maximo} caracteres.`; etiquetas `código`/20, `placa`/20, `estación`/150.

Después, en este orden:
- 400 `La fecha de carga es obligatoria.` (null/default).
- 400 `El combustible debe ser Diesel o Gasolina.`
- 400 `Los litros deben estar entre 0.001 y 9999999.999.`; `Los litros no pueden tener más de 3 decimales.`
- 400 `El precio por litro debe estar entre 0.001 y 9999999.999.`; `El precio por litro no puede tener más de 3 decimales.`
- 400 `La moneda debe contener 3 letras mayúsculas.`
- 400 `El odómetro inicial debe estar entre 0 y 999999999.`
- 400 `El odómetro final debe estar entre 0 y 999999999.`; `El odómetro final debe ser mayor que el inicial.`
- 400 `Las observaciones no pueden superar 500 caracteres.`

409: `Ya existe un vale con el código {codigo}.` (también traducir Unicidad al guardar); `Solo se puede editar un vale en borrador.`; `Solo se puede eliminar un vale en borrador.`; `Solo se puede confirmar un vale en borrador.`; `Registra el odómetro final antes de confirmar el vale.`; `El vale ya está anulado.`; `No se puede eliminar el vale porque está en uso.` (FK externa futura defensiva, sin acreditar que exista).

Anulación400: `El motivo de anulación es obligatorio.` / `El motivo de anulación no puede superar 500 caracteres.`. Mantener intervalo, importes y fecha de confirmación previa; no modificar el vale terminal.

Consulta400: `El número de página debe ser mayor que cero.`; `El tamaño de página debe estar entre 1 y 100.`; `El estado debe ser Borrador, Confirmado o Anulado.`; `El combustible debe ser Diesel o Gasolina.`. Filtros vacíos sin efecto; estado y tipo válidos sin distinguir mayúsculas. Reglas de cuatro capas, tipos explícitos, miembros con bloques, lambdas junto al operador y excepciones/CodigosEstado de Application, sin ASP.NET/Npgsql allí.

## 5. Aceptación medible
A1 verify salida0, .NET sin warnings, formato/capas/reglas. A2 integración de cada literal/límite, precisión/redondeo, nulabilidad y datos inválidos sin cambios. A3 ocho operaciones OpenAPI/JWT, filtros/paginación/resumen. A4 SQL/scaffold coincidentes y seed idempotente con estados/intervalos completo y pendiente. A5 SMOKE OK real con UTC sin zona, máximos, cálculos/estados y restricciones. A6 pantalla con cálculos exactos, diez repeticiones más carga y QA real según spec frontend. Registrar salvedades globales sin ocultar avisos ni debilitar pruebas.

## 6. Checklist
- [ ] Revisión y aprobación de Lukas.
- [ ] SQL/scaffold y API con pruebas.
- [ ] Seed y smoke PostgreSQL.
- [ ] Pantalla, cálculos y estabilidad.
- [ ] Gates y QA con evidencia real.

## 7. Registro
2026-11-21: propuesta local autorizada por Kelvin al continuar como los módulos anteriores. Asignación, orden y reservas comprobados. Sin pruebas acreditadas todavía; revisión de Lukas pendiente.
