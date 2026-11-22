# Módulo 45: guías de remisión — verificación local

Fecha: 2026-11-21. Responsable: Kelvin Pinto. Rama `kelvin/modulo45-guias-remision`, independiente desde `origin/main` (`670c37f`). Kelvin autorizó expresamente implementar la propuesta localmente. **La revisión y aprobación de Lukas siguen pendientes**; no acredita integración, despliegue ni validez tributaria.

## Alcance implementado

Documentos internos de transporte con serie/número manuales, remitente/destinatario, direcciones, motivo, transportista/conductor/placa, peso declarado y 1–100 líneas de mercancías. Ocho operaciones autenticadas: listado paginado con filtros, detalle, alta, edición, baja de borrador, emisión interna, anulación con motivo y resumen global.

Estado Borrador → Emitida → Anulada, o Borrador → Anulada. Emitida y Anulada no se editan ni eliminan; anular conserva mercancías, datos del traslado y emisión existente. Reemplazo de líneas y cabecera en una única escritura de UnitOfWork. Unicidad SQL por serie normalizada y número; no se garantiza resolución de transiciones o ediciones concurrentes.

Sin relaciones nuevas con entidades congeladas o módulos ajenos. No modifica inventarios, correlativos del módulo 76, rutas ni consolidaciones. No genera PDF/XML/QR, no firma ni transmite documentos a terceros; «Emitida» representa exclusivamente el cierre interno que explica la pantalla.

## Resultados ejecutados

| Comprobación | Resultado |
|---|---|
| SQL y scaffold | `015-guias-remision.sql` aplicado localmente; entidades/DbContext generados, coincidencia confirmada por smoke |
| Backend propio | 60 pruebas correctas: 57 de integración y tres de traducción de restricciones |
| Backend global | 1143 pruebas correctas, cero omitidas |
| Frontend propio | 61 pruebas en cinco archivos, todas correctas |
| Frontend global | 604 pruebas en 75 archivos, todas correctas |
| Estabilidad | Diez repeticiones consecutivas de la suite propia; otra repetición correcta mientras compilaba el frontend |
| Gate completo | `./scripts/verify.sh` terminó con salida 0: .NET sin warnings, formato, reglas/capas, integración, lint, tipos y build |
| PostgreSQL real | `./scripts/smoke-postgres.sh` terminó en `SMOKE OK` |
| Seed idempotente | Doble carga correcta; tres guías de ejemplo y una línea por guía, sin duplicados |
| QA real final | Cuatro escenarios: 375/1280, claro/oscuro; 20 capturas propias y dos de referencia integrada |

El smoke comprueba JWT, las ocho operaciones en OpenAPI, seed/resumen, máximos decimales persistidos, fecha de entrada sin zona convertida a UTC, unicidad de serie/número, rechazo del exceso de escala, reemplazo por dos líneas, emisión sin cuerpo, estados terminales, motivo e historial de anulación, filtro, baja en cascada y 404 posterior. Verifica también unicidad, FK propia, cantidad positiva y coherencia de estado mediante SQL real. La posible FK externa futura se trata defensivamente con una excepción simulada en pruebas; no se acredita que exista dicha relación.

Máximos verificados: peso `9999999999.99` kg y cantidad por línea `999999999.999`, sin pérdida de precisión; extremo inferior `0.001` y límite de 100 líneas en integración. Respuestas decimales textuales. No se suman cantidades con unidades distintas.

## QA y comparación visual

Cada escenario realiza alta de dos líneas con un mismo SKU y unidades diferentes, cantidad exacta `0.001`, peso declarado `12.34`, edición de observaciones, emisión interna, anulación conservando historial y baja real de otro borrador. El navegador usa `America/Lima`: se comprueba que editar conserva exactamente `2026-11-22T10:30:12.345Z`, sin desplazamiento de hora ni pérdida de milisegundos.

Se comprobaron foco inicial, Tab/Shift+Tab, Escape sin escritura y retorno al disparador en los diálogos. Sin errores JavaScript, desbordamiento de página ni fondo desigual en la selección. Cinco muestras de contraste por escenario cumplen al menos 4.5:1; mediciones en [resultado-visual.json](resultado-visual.json).

La inspección inicial detectó números partidos en la primera columna; se corrigió exclusivamente `.guias-remision-table th`. El QA final añade una comprobación de una sola línea para esos números. Después del ajuste se repitieron **frontend verify completo (604 pruebas)**, compilación y QA real; el backend no cambió después del gate completo y el smoke.

Comparación con Contratos comerciales integrado: [1280 claro](1280-light-referencia-contratos.png) y [1280 oscuro](1280-dark-referencia-contratos.png). Se conservaron PaginaModulo, tipografía/tokens, botones, campos, TablaPaginada, superficies y disposición de listado/detalle; las métricas siguen la guía actual de cifra grande con etiqueta debajo. No se modificó la pantalla de referencia.

| Escenario | Detalle | Formulario | Baja | Emisión | Anulación |
|---|---|---|---|---|---|
| 375 claro | [Captura](375-light-detalle.png) | [Captura](375-light-formulario.png) | [Captura](375-light-baja.png) | [Captura](375-light-emision.png) | [Captura](375-light-anulacion.png) |
| 375 oscuro | [Captura](375-dark-detalle.png) | [Captura](375-dark-formulario.png) | [Captura](375-dark-baja.png) | [Captura](375-dark-emision.png) | [Captura](375-dark-anulacion.png) |
| 1280 claro | [Captura](1280-light-detalle.png) | [Captura](1280-light-formulario.png) | [Captura](1280-light-baja.png) | [Captura](1280-light-emision.png) | [Captura](1280-light-anulacion.png) |
| 1280 oscuro | [Captura](1280-dark-detalle.png) | [Captura](1280-dark-formulario.png) | [Captura](1280-dark-baja.png) | [Captura](1280-dark-emision.png) | [Captura](1280-dark-anulacion.png) |

## Reproducción y pendientes

- Git Bash en Windows; Docker Linux, .NET 10, Node 24 y Python 3 en PATH. Ejecutar los comandos de AGENTS.md: levantar db, restaurar herramientas, aplicar SQL, scaffold y seed; después API y frontend locales.
- Gate ejecutado con `VITEST_MAX_WORKERS=2 ./scripts/verify.sh`, sin cambios en configuración, timeouts ni tests existentes. Tras el ajuste CSS: `VITEST_MAX_WORKERS=2 npm --prefix frontend run verify`. Smoke sin otra instancia simultánea y con puertos libres.
- El compose no conserva volúmenes: después de retirar el contenedor, aplicar nuevamente SQL y seed antes del QA. Solo se utilizó la base local.
- Persiste la advertencia de Vite del bundle compartido superior a 500 kB (715.33 kB). No se ocultó ni se elevó el límite. No se acredita cero advertencias globales ni cierre formal mientras falten esa condición y la aprobación de Lukas.
- Datos y cuentas de demostración locales; capturas sin tokens ni credenciales. Ayudantes temporales y registros de autenticación excluidos del commit. Sin push, PR, cambios en main ni en las otras ramas.

## Preparación de revisión — 2026-11-22

Rama actualizada con `git pull origin main`, conservando la tabla oficial de relevo y modificando solo la línea de Kelvin y la reserva propia. Script vigente: `database/015-guias-remision.sql`. Propuesta, pendiente de aprobación de Lukas. Base PostgreSQL local recreada, seed cargado y entidades/contexto regenerados mediante scaffold, sin edición manual ni diferencias de contenido.

`./scripts/verify.sh`: salida0, 1143 pruebas backend y 604 frontend en 75 archivos. `./scripts/smoke-postgres.sh`: salida0, scaffold coincidente y SMOKE OK. Se usó VITEST_MAX_WORKERS=1 temporal, sin modificar pruebas, timeouts ni configuración compartida. Persisten los avisos globales de ESLint9/Vite; no se cambiaron dependencias ni umbrales.

No se generaron capturas nuevas ni se cambió la pantalla para estos ajustes. Se conserva la evidencia visual previa. Preparación local: sin nuevos PR, sin push y sin integración. Tras cada fusión anterior habrá que incorporar main y repetir estas verificaciones antes del PR correspondiente.
