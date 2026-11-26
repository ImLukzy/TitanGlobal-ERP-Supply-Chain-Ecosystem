# Módulo 48 — Transportistas terceros

Implementación local de Kelvin Pinto en `kelvin/modulo48-transportistas-terceros`. Revisión de las decisiones de producto por Lukas pendiente; este registro no acredita integración ni despliegue.

## Comprobaciones

| Comprobación final | Resultado |
|---|---|
| `./scripts/verify.sh` | Exit 0: .NET sin advertencias ni errores; formato, reglas, ESLint y TypeScript correctos |
| Integración backend | 1.138 pruebas correctas, incluidas 53 propias del módulo 48 |
| Frontend | 580 pruebas correctas en 73 archivos, incluidas 37 propias del módulo 48 |
| PostgreSQL real | `SMOKE OK`, scaffold coincidente y restricciones SQL verificadas |
| QA visual real | Cuatro escenarios correctos, 16 capturas |

- PostgreSQL 16 local, script `012-transportistas-terceros.sql` aplicado y entidades regeneradas mediante scaffold. La comprobación del scaffold forma parte del smoke.
- Datos de ejemplo cargados dos veces sin duplicados: seis transportistas y cuatro fletes, en PEN y USD, incluyendo un transportista inactivo y un flete cancelado.
- El smoke verifica las diez operaciones, autenticación, filtros, resumen, baja protegida, duplicados normalizados, importe máximo exacto, fecha sin zona normalizada a UTC, motivo obligatorio y cancelación repetida. También comprueba directamente en PostgreSQL los índices únicos y la clave foránea.
- La prueba visual usa Edge, API y PostgreSQL locales; no intercepta ni simula respuestas. Resultado detallado en [resultado-visual.json](resultado-visual.json).
- Sin cambios en `main`, push ni PR.

## Revisión visual real — 2026-11-21

Cada escenario comprueba alta, edición y baja de un transportista sin historial; registro de un flete de `1234.50 USD`, consulta y cancelación con motivo; Escape y retorno de foco. No hay errores de JavaScript ni desbordamiento horizontal de la página. Las tablas permiten desplazamiento horizontal dentro de su región accesible en móvil.

| Ancho / tema | Maestro y detalle | Formulario maestro | Registro de flete | Cancelación |
|---|---|---|---|---|
| 375 / claro | [Captura](375-light-transportistas.png) | [Captura](375-light-maestro-formulario.png) | [Captura](375-light-flete-formulario.png) | [Captura](375-light-cancelacion.png) |
| 375 / oscuro | [Captura](375-dark-transportistas.png) | [Captura](375-dark-maestro-formulario.png) | [Captura](375-dark-flete-formulario.png) | [Captura](375-dark-cancelacion.png) |
| 1280 / claro | [Captura](1280-light-transportistas.png) | [Captura](1280-light-maestro-formulario.png) | [Captura](1280-light-flete-formulario.png) | [Captura](1280-light-cancelacion.png) |
| 1280 / oscuro | [Captura](1280-dark-transportistas.png) | [Captura](1280-dark-maestro-formulario.png) | [Captura](1280-dark-flete-formulario.png) | [Captura](1280-dark-cancelacion.png) |

## Cómo repetir

Usar Git Bash, .NET 10, Node 24 y Docker con PostgreSQL local. Ejecutar `./scripts/aplicar-bd.sh --reiniciar`, `./scripts/scaffold.sh`, cargar `scripts/seed-dev.sql` y ejecutar `./scripts/verify.sh`. Con los puertos 5080 y 5433 libres, ejecutar `./scripts/smoke-postgres.sh` por separado.

Para revisión manual, preparar otra vez la base y el seed después del smoke: el contenedor de este repositorio no conserva datos al eliminarse. Iniciar API en 5080 y frontend en 5173, entrar con la cuenta administrativa del seed y abrir `/modulos/48-transportistas-terceros`. Repetir las operaciones anteriores en los cuatro escenarios de la tabla.

Los importes recibidos se muestran como texto exacto con su moneda. El formulario rechaza cantidades que el navegador no pueda enviar con precisión; no convierte ni suma monedas diferentes. Vite conserva la advertencia existente de tamaño del paquete compartido; no se aumenta el umbral para ocultarla.

## Preparación de revisión — 2026-11-22

Rama actualizada con `git pull origin main`, conservando la tabla oficial de relevo y modificando solo la línea de Kelvin y la reserva propia. Script vigente: `database/012-transportistas-terceros.sql`. Propuesta, pendiente de aprobación de Lukas. Base PostgreSQL local recreada, seed cargado y entidades/contexto regenerados mediante scaffold, sin edición manual ni diferencias de contenido.

`./scripts/verify.sh`: salida0, 1138 pruebas backend y 580 frontend en 73 archivos. `./scripts/smoke-postgres.sh`: salida0, scaffold coincidente y SMOKE OK. Se usó VITEST_MAX_WORKERS=1 temporal, sin modificar pruebas, timeouts ni configuración compartida. Persisten los avisos globales de ESLint9/Vite; no se cambiaron dependencias ni umbrales.

No se generaron capturas nuevas ni se cambió la pantalla para estos ajustes. Se conserva la evidencia visual previa. Preparación local: sin nuevos PR, sin push y sin integración. Tras cada fusión anterior habrá que incorporar main y repetir estas verificaciones antes del PR correspondiente.

### Actualización para revisión — 2026-11-23

- Rama actualizada con `origin/main` (0b0324ff0863a4b5ab6c977e649c7840eb199caa) después de integrar el PR #59 del módulo 42.
- `relevo.md` idéntico a main. Se conservaron las pantallas y ambos bloques de CSS, además del seed de evaluaciones de proveedores; contexto regenerado con el scaffold oficial desde la base combinada.
- Base local reiniciada, seis scripts aplicados y seed cargado. Script propio: `012-transportistas-terceros.sql`; aplicación en la demo pendiente de Lukas.
- `./scripts/verify.sh`: exit 0; 1191 pruebas backend, 695 pruebas frontend en 84 archivos; .NET con cero warnings, lint, tipos y build correctos.
- `./scripts/smoke-postgres.sh`: exit 0, `SCAFFOLD AL DÍA` y `SMOKE OK`, incluidos los flujos del módulo 48 y los módulos existentes.
- El frontend conserva los avisos existentes de obsolescencia de ESLint 9 y tamaño del bundle de Vite. La evidencia visual del 2026-11-21 sigue disponible; esta actualización conserva el bloque visual del 48.
- Propuesta, pendiente de aprobación de Lukas. No se han aplicado scripts contra producción.
