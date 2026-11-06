# Especificación: 53 - Reglas de integridad en la base de datos

## 1. Objetivo
**Problema:** la base ya tenía claves primarias, 36 claves foráneas y 34 índices únicos, pero todas las demás reglas (precios no negativos, estados válidos, fechas ordenadas, longitudes en las tablas del enunciado) solo las comprobaba la aplicación.

**Resultado esperado:** la base repite esas reglas con 63 restricciones `CHECK` y 4 longitudes, en `database/002-reglas-integridad.sql`. Un dato inválido cargado por fuera de la API (SQL a mano, otra herramienta) se rechaza.

## 2. Fuera de alcance
- **Reglas que la aplicación no garantiza:** ninguna restricción es más estricta que la validación del código. Quedan sin regla `Clientes.Descripcion`, `Productos.Descripcion`, `OrdenesCompra.NumeroOrden` (longitud), `OrdenesCompra.Descripcion`, `RegistrosAuditoria.Cambios` y el par `Cotizaciones.Fecha`/`FechaValidez`.
- **`> 0` en decimales sin límite de decimales** (Almacenes, Zonas, Productos, Contratos, ItemsListaPrecios, Promociones, DetallesOrdenesAbastecimiento, TasasImpuestos): PostgreSQL redondea antes de comprobar, así que 0,004 se guardaría como 0,00 y daría 500. Queda en `>= 0`; pasar a `> 0` exige antes validar los decimales en Application.
- **`RegistrosAuditoria.EntidadId`:** sin clave foránea a propósito (apunta a cualquier tabla).
- Cambios en la aplicación.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `database/002-reglas-integridad.sql` | crear | 63 `CHECK` y 4 longitudes |
| `src/TitanGlobal.Infrastructure/Persistence/TitanGlobalDbContext.cs` | regenerar | `HasMaxLength` de Clientes y Productos (EF no genera los `CHECK`) |
| `docs/equipo/crear-modulo.md`, `docs/contexto-proyecto.md` | modificar | regla para tablas nuevas y decisión |

## 4. Diseño
- Cada regla sale de una validación existente, con su archivo y línea revisados en todas las rutas de escritura (crear, editar, cambios de estado, cálculos automáticos) y comprobada contra `scripts/seed-dev.sql`.
- Tipos de regla: longitudes y texto no vacío (Clientes, Productos, OrdenesCompra), rangos numéricos, listas de estados y tipos (`IN`), orden de fechas y coherencia entre columnas (Promociones por tipo, permiso de escritura con lectura).
- Nombres: `CK_<Tabla>_<Regla>`.
- Producción: `~/titanglobal-integracion/neon-bd.py` (copia, ensayo, confirmación). Si un dato de la demo no cumple una regla, el ensayo falla y producción no se toca. La primera vez registra `001` como aplicado.

## 5. Criterios de aceptación
| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Se aplica | `./scripts/aplicar-bd.sh --reiniciar` | 001 y 002 aplicados; 63 `CHECK` en `pg_constraint` |
| A2 | Seed | `scripts/seed-dev.sql` sobre la base nueva | sin errores |
| A3 | Bloquea | precio negativo, RUC con letras, estado inventado | `violates check constraint` |
| A4 | La API no se rompe | `./scripts/smoke-postgres.sh` | `SMOKE OK`, 0 respuestas 500 |
| A5 | Gate | `./scripts/verify.sh` | en verde |

## 6. Checklist de ejecución
- [x] Script 002 a partir de las validaciones del código.
- [x] Scaffold regenerado.
- [x] Verificar A1–A5 y anotar en §7.
- [ ] Aplicar en la demo (Lukas, con `neon-bd.py`).

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia |
|---|---|---|---|
| 2026-11-05 | A1 | ✅ | «Aplicando 001…», «Aplicando 002…»; `pg_constraint` con 63 `CHECK` |
| 2026-11-05 | A2 | ✅ | seed cargado sin errores |
| 2026-11-05 | A3 | ✅ | `CK_Productos_PrecioNoNegativo`, `CK_Clientes_RucOnceDigitos` y `CK_Cotizaciones_Estado` rechazan; un producto válido se inserta |
| 2026-11-05 | A4 | ✅ | `SCAFFOLD AL DÍA`, `SMOKE OK`, 272 comprobaciones HTTP y 0 respuestas 500 |
| 2026-11-05 | A5 | ✅ | `./scripts/verify.sh` en verde: 942 + 497 pruebas |
| 2026-11-05 | Revisión de código | ✅ | Revisión independiente: 7 hallazgos, los 7 descartados con evidencia: `Cantidad` es `integer` (sin redondeo); 0 órdenes en una liquidación → 400 antes de guardar (`ComisionesVendedoresUseCase.cs:345`); contrato exige fin posterior (`ValidacionContratoComercial.cs:38`); promoción de producto con monto → 400 (`ValidacionPromocion.cs:32`); `Impuestos.Tipo` se guarda en mayúsculas (`ValidacionImpuesto.cs:14`); los datos antiguos los detecta el ensayo; todas las columnas de estado son `NOT NULL` y las que admiten nulos usan `IS NULL` |
