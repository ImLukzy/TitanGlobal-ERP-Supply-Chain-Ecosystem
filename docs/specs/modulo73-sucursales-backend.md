# Propuesta: módulo 73 — Sucursales (backend)

## 1. Objetivo
Kelvin Pinto, según asignaciones. Último en el orden 41 → 42 → 48 → 43 → 44 → 45 → 46 → 47 → 49 → 50 → **73**. El enunciado define «filiales, plantas, oficinas». Mantener un maestro de sucursales con ubicación, contacto, apertura opcional y estado activo/inactivo, sin perder los datos al desactivar.

Continuación local autorizada como los módulos previos. Aprobación de Lukas pendiente. Rama independiente `kelvin/modulo73-sucursales` desde origin/main actualizado; sin incorporar otras ramas ni publicar o integrar.

## 2. Fuera de alcance
Maestro plano, sin jerarquía societaria ni sucursal principal, identificación fiscal o reglas por país. País/ciudad son texto libre como en los maestros existentes; no catálogos geográficos ni mapas. No vincular usuarios, permisos, centros de trabajo, almacenes ni documentos: esas integraciones afectan módulos ajenos. No cambiar autenticación, pipeline, las cuatro entidades base ni el selector de rol. Sin aislamiento de datos por sucursal, reapertura de periodos ni garantía de resolución de escrituras concurrentes.

La activación/desactivación conserva los datos y registra la fecha del último cambio, no un historial completo ni autorización legal para operar. Fecha de apertura opcional/manual; no inferir vigencia a partir de ella ni comparar con hoy. Contactos declarados: formato básico de email, sin verificar entrega o identidad. Baja física solo de una sucursal inactiva; no hay una FK externa nueva en este alcance.

## 3. Archivos
`database/020-sucursales.sql`; entidad `Sucursal.cs` y DbContext generados solo por scaffold; mapeo de pluralización y su teoría. DTOs/UseCases en `Modulo73_Sucursales`; controlador `SucursalesController.cs`; pruebas en `tests/TitanGlobal.Api.Tests/Modulo73_Sucursales/`. Bloques propios al final de seed/smoke; specs, asignaciones, relevo y `docs/evidencias/modulo73/`. Reservas010–019 preservadas como ramas independientes, no integración.

## 4. Diseño y lógica
Sucursales: Id identity; Codigo varchar20 único por lower(btrim), Nombre150, Tipo10 (Filial/Planta/Oficina), Pais100, Ciudad100, Direccion300, obligatorios recortados/no vacíos. Contacto100, Email150, Telefono30 y Observaciones500 opcionales (vacíos→null). FechaApertura timestamptz nullable; FechaRegistro timestamptz obligatoria del servidorUTC; Activa boolean defaulttrue; FechaCambioEstado timestamptz nullable del servidorUTC. Índice de código normalizado y checks de textos/tipo.

Alta activa. PUT modifica datos en cualquier estado, conservando Activa/FechaRegistro/FechaCambioEstado. Activar/desactivar solo cambia Activa y FechaCambioEstado; no permitir repetir el mismo estado. Baja física solo inactiva. Código libre excluyendo Id propio; traducir Unicidad al guardar y FK futura al eliminar. Validar todo antes de modificar entidades.

FechaApertura null admitida; valor por defecto si se proporciona inválido. Normalizar con FechasUtc (sin zona se interpreta UTC), conservando milisegundos. Email opcional con patrón básico idéntico en backend/frontend `^[^\s@<>]+@[^\s@<>]+$`, después del límite de longitud; sin validar dominio real. Tipo sin distinguir mayúsculas, conservar texto canónico.

### API JWT
Prefijo `/api/v1/sucursales`. GET listado con busqueda en código/nombre/ciudad, tipo/pais/activa opcionales (pais exacto sin mayúsculas), pagina1/tamanoPagina20 máximo100, orden Codigo/Id sin overflow. GET /{id}; POST / y PUT /{id} `{codigo,nombre,tipo,pais,ciudad,direccion,contacto?,email?,telefono?,observaciones?,fechaApertura?}`; DELETE /{id}204; POST /{id}/activacion y /desactivacion sin cuerpo, con `[PermitirCuerpoVacio]`; GET /resumen `{total,activas,inactivas,filiales,plantas,oficinas}` (tipos incluyen ambos estados). Escrituras200 `{id}`. Ocho operaciones; no endpoints públicos nuevos.

Página `{items,total,pagina,tamanoPagina}`. Item `{id,codigo,nombre,tipo,pais,ciudad,activa}`; detalle agrega dirección, contacto, email, teléfono, observaciones y fechas. No exponer entidades ni activar/desactivar mediante PUT.

### Mensajes y orden
Buscar primero404 `La sucursal {id} no existe.` en lectura/edición/baja/transición. Guardar: Codigo/código20, Nombre/nombre150, Pais/país100, Ciudad/ciudad100, Direccion/dirección300, en ese orden:400 `El campo {etiqueta} es obligatorio.` / `El campo {etiqueta} no puede superar {maximo} caracteres.`. Después tipo400 `El tipo debe ser Filial, Planta u Oficina.`.

Opcionales, en orden: `El contacto no puede superar 100 caracteres.`; `El email no puede superar 150 caracteres.`; `El email no es válido.`; `El teléfono no puede superar 30 caracteres.`; `Las observaciones no pueden superar 500 caracteres.`; apertura si default: `La fecha de apertura no es válida.`. Al final duplicado409 `Ya existe una sucursal con el código {codigo}.`, también al traducir Unicidad.

Estados409: `La sucursal ya está activa.`; `La sucursal ya está inactiva.`. Baja409: `Desactiva la sucursal antes de eliminarla.`; `No se puede eliminar la sucursal porque está en uso.` (FK futura defensiva, sin acreditar FK actual). Buscar antes de revisar estado. Una transición rechazada no modifica fechas.

Consulta400: `El número de página debe ser mayor que cero.`; `El tamaño de página debe estar entre 1 y 100.`; tipo inválido mismo literal. Filtros vacíos sin efecto, activa=false debe filtrar inactivas. Tipos explícitos, miembros con bloques, lambdas en la línea del operador, UseCases concretos españoles y excepciones/CodigosEstado de Application; cuatro capas y controlador delgado.

## 5. Aceptación
A1 gate completo salida0, .NET sin warnings, formato/capas/reglas. A2 integración por literal y límite, datos inválidos/estados repetidos sin mutación, fechaUTC opcional. A3 ocho operaciones JWT/OpenAPI, filtros combinados/paginación y resumen. A4 SQL/scaffold coincidentes, seed idempotente con tres tipos y ambos estados. A5 SMOKE OK PostgreSQL, apertura sin zona/null, CRUD, activación/desactivación/baja y restricciones. A6 pantalla, diez repeticiones propias y veinte con build simultáneo, QA real375/1280claro/oscuro y referencia integrada. Registrar avisos globales sin ocultarlos.

## 6. Checklist
- [ ] Aprobación de Lukas.
- [x] SQL/scaffold y backend con pruebas.
- [x] Seed y smoke real.
- [x] Pantalla, estabilidad y QA.
- [x] Gate y evidencia local.

## 7. Registro
2026-11-22: propuesta local autorizada. SQL020 aplicado y scaffold coincidente; 34 casos propios de backend más16 de pluralización correctos. Seed dos veces: seis registros; smoke PostgreSQL salida0 y SMOKE OK. Pantalla:48 pruebas, diez repeticiones y veinte con build simultáneo correctos. QA real375/1280claro/oscuro con apertura null/UTC, conservación de fechas y contactos, estados, filtros y baja. Gate completo salida0:1.118 pruebas backend y591frontend, .NET sin advertencias; avisos globales de ESLint9/Vite documentados. Detalle en docs/evidencias/modulo73/. Revisión formal de Lukas pendiente.
