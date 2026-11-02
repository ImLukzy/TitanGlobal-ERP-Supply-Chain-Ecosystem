# Módulo 75 - Impuestos y Tasas (Backend)

## 1. Objetivo

Implementar el módulo de Impuestos y Tasas de TitanGlobal ERP, encargado de contemplar la información relacionada con IGV/IVA, retenciones, percepciones y detracciones.

## 2. Alcance

El módulo pertenece al dominio Gobierno Corporativo, Seguridad y Auditoría.

Elementos identificados en el enunciado:

- IGV/IVA.
- Retenciones.
- Percepciones.
- Detracciones.

El módulo servirá como dependencia para otros módulos, especialmente Cotizaciones y Facturación Electrónica.

## 3. Fuera de alcance

No se implementarán funcionalidades que no estén definidas o aprobadas en los requisitos del proyecto.

## 4. Arquitectura

Se seguirá la arquitectura modular Vertical Slice del proyecto, utilizando como referencia `Modules/Modulo11_Proveedores/`.

Ubicación:

`src/TitanGlobal.Api/Modules/Modulo75_ImpuestosTasas/`

Estructura:

- Controllers
- Dtos
- UseCases

Los modelos, repositorios, servicios y persistencia compartidos se ubicarán en las carpetas existentes del proyecto cuando corresponda.

## 5. Diseño funcional

### 5.1. Elementos tributarios

El módulo contempla IGV/IVA, retenciones, percepciones y detracciones.

### 5.2. Dependencias

- Módulo 03 - Cotizaciones.
- Módulo 51 - Facturación Electrónica.

### 5.3. Reglas de negocio pendientes

Se requiere confirmar:

- Tasas tributarias aplicables.
- Tipos y clasificación de impuestos.
- Vigencia de las tasas.
- Condiciones para aplicar retenciones.
- Condiciones para aplicar percepciones.
- Condiciones para aplicar detracciones.
- Fórmulas de cálculo y redondeo.

No se asumirán reglas tributarias sin aprobación.

## 6. Contratos API

Las rutas, métodos HTTP, parámetros, DTOs y mensajes literales de error se definirán después de confirmar las operaciones requeridas.

## 7. Persistencia

El modelo de datos se definirá después de confirmar los atributos y relaciones necesarios.

Las modificaciones se realizarán mediante migraciones generadas con Entity Framework Core.

## 8. Criterios de aceptación

- [ ] Se implementan las funcionalidades aprobadas.
- [ ] Se respetan las reglas tributarias definidas.
- [ ] Se utilizan DTOs para las solicitudes y respuestas.
- [ ] Los controladores delegan la lógica en UseCases.
- [ ] Se utiliza IUnitOfWork para el acceso a datos.
- [ ] Se implementan validaciones y mensajes de error definidos.
- [ ] Se agregan pruebas de los casos válidos e inválidos.
- [ ] La API compila sin advertencias.
- [ ] Se ejecuta correctamente el script de verificación del proyecto.

## 9. Pruebas

Se definirán pruebas unitarias y de integración según las operaciones y reglas aprobadas.

## 10. Archivos previstos

- `src/TitanGlobal.Api/Modules/Modulo75_ImpuestosTasas/`
- `src/TitanGlobal.Api/Models/` (si se requieren nuevas entidades).
- `src/TitanGlobal.Api/Migrations/` (si se requieren cambios de persistencia).
- `scripts/seed-dev.sql` (si se requieren datos de ejemplo).

## 11. Estado

Borrador inicial pendiente de revisión funcional.

Responsable: Leonel Portillo.
Módulo: 75 - Impuestos y Tasas.
