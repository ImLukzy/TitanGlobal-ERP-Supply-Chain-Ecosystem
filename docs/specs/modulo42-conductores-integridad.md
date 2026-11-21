# Especificación: módulo 42 — unicidad de licencias

## 1. Objetivo
Impedir documentos de licencia duplicados sin distinguir mayúsculas en alta, edición y escritura directa/concurrente en PostgreSQL, conservando el formato solicitado tras Trim. Responsable: Kelvin Pinto. Desarrollo local autorizado por Kelvin; aprobación de Lukas pendiente.

## 2. Fuera de alcance
Otros módulos, entidades base, pipeline, middleware, edición manual de código generado, scripts fusionados y producción. El permiso administrativo y la fecha mantienen el contrato del backend: una licencia vencida se puede conservar al editar/reactivar, tal como exige el smoke existente; la pantalla presenta su vigencia por separado.

## 3. Archivos afectados
`database/011-conductores-licencia-unica.sql`, los casos de uso Crear/Actualizar de `Modulo42_Conductores`, sus pruebas API, la sección propia del smoke, la reserva de scripts del relevo y la spec backend del 42. Modelos/DbContext solo mediante scaffold si produce cambios reales.

## 4. Diseño y lógica
- Comparar `Trim().ToLower()` en consulta con el documento solicitado en minúsculas invariantes. En PUT excluir el Id propio. Conservar el formato solicitado tras Trim.
- 409 literal: «Ya existe un conductor con el documento de licencia {documentoLicencia}.», sin Detail. Mantener traducción de `ViolacionRestriccionException` y el orden existente de validación, incluida la conservación de fechas vencidas.
- Índice único sobre `lower(btrim("DocumentoLicencia"))`; conservar el índice original para el seed. No borrar ni fusionar datos: si existen duplicados normalizados, la aplicación del script falla y se revierte.
- El índice de expresión puede ser omitido por el scaffold de EF; se verifica su protección en PostgreSQL real.
- El 010 está reservado al 41 en su rama independiente; este módulo parte de `origin/main`, usa 011 y no incorpora código del 41. Los huecos se permiten según relevo §8.

## 5. Criterios de aceptación
POST/PUT con variantes de mayúsculas → 409 literal; cambio de mayúsculas del documento propio → 200; escritura directa duplicada → 23505 por el índice esperado; aplicar-bd y scaffold al día; verify y smoke en verde; pantalla con vigencia y permiso administrativo separados.

## 6. Checklist
- [ ] Aprobación de Lukas.
- [ ] SQL aplicado y scaffold.
- [ ] Casos de uso, integración y smoke.
- [ ] Gate y evidencia en §7.

## 7. Registro de verificación
Pendiente.
