# Especificación propuesta: módulo 44 — Consolidación de carga (frontend)

**Estado de revisión: propuesta, pendiente de aprobación de Lukas.**

## 1. Objetivo
Página `/modulos/44-consolidacion-carga` de Kelvin: preparar líneas, registrar picking, configurar packing y consultar cubicaje/peso estimados. Implementación local autorizada al continuar el siguiente módulo; **revisión de Lukas y decisiones backend pendientes**.

## 2. Fuera de alcance
Aplican las decisiones backend. Sin integración de inventarios, Producto, pedidos, mapas, escáneres o vehículos. No modificar pantallas ajenas, autenticación, componentes compartidos ni dependencias. Los cálculos son estimaciones; no prometer pesajes o asignaciones reales.

## 3. Archivos
Carpeta `frontend/src/modules/consolidacion-carga/`: tipos.ts, api.ts, validacion.ts, calculos.ts, useConsolidacionesData.ts, ConsolidacionCargaPage.tsx, ConsolidacionesResumen.tsx, ConsolidacionesFiltros.tsx, ConsolidacionesTabla.tsx, ConsolidacionDetallePanel.tsx, ConsolidacionFormulario.tsx, LineasCargaEditor.tsx, DialogoCancelarCarga.tsx y pruebas temáticas/utilidades. Registro propio en pantallas.ts y estilos añadidos con prefijo `.consolidacion-carga-`. Evidencia real en docs/evidencias/modulo44/ después de ejecutar QA.

## 4. Diseño y lógica
- Componentes compartidos PaginaModulo, TablaPaginada, PanelDetalle, CampoConError y DialogoBaja. Variables tokens.css; estado con texto y color, foco visible.
- Resumen global con conteos Borrador/Confirmada/Cancelada; búsqueda con 300ms, filtro estado, pagina1 al cambiar y tamaño10. Una carga inicial; descartar respuestas antiguas. Errores independientes de lista/detalle/resumen, reintento y página vacía corregida después de baja.
- Tabla con código/nombre, estado, picking recogido/previsto, bultos, m³ y kg; valores recibidos textuales sin Number, sin cabeceras/cifras partidas y selección homogénea.
- Detalle con subtabla de líneas, packing, totales estimados y fechas UTC/motivo. Borrador permite editar, confirmar y eliminar; Confirmada solo cancelar; Cancelada sin acciones de escritura.
- Formulario de cabecera y colección de 1..100 líneas. Agregar/quitar, SKU/descripción, cantidades y packing; entradas decimales como texto, máximo dos decimales significativos. Incompleto se puede guardar; al confirmar exigir picking y packing completos con los mensajes exactos de backend. Etiquetas únicas por línea, primer error con foco, no perder valores ante 400/409.
- Cálculo orientativo por línea y total exacto con enteros BigInt escalados: volumen redondeado a seis decimales por línea y luego sumado; peso a dos. Bultos con redondeo hacia arriba; el parcial conserva el empaque completo según la decisión backend. Mostrar la advertencia de estimación, no un pesaje real. El servidor sigue siendo autoridad del cálculo y estado.
- Impedir doble envío. Confirmar con acción inequívoca y mantener 409 visible. Baja con DialogoBaja y cancelación con diálogo de motivo accesible, Tab atrapado, Escape y retorno del foco; sin borrar historial terminal.
- Escritorio1280: tabla a la izquierda, detalle/formulario a la derecha. Móvil375: apilado, márgenes >=16px, tablas/editor con scroll interno, sin overflow de página; claro/oscuro y contraste >=4.5:1.

## 5. Aceptación
A1: cliente de ocho operaciones y validaciones con literales/límites. A2: consultas con temporizadores falsos, filtros, paginación y respuestas antiguas. A3: alta/edición de líneas, picking/packing, totales exactos, confirmación/cancelación/baja, errores/foco/doble envío. A4: suite propia repetida diez veces y gates en verde, salvedades registradas. A5: QA con API/PostgreSQL reales, 375/1280 claro/oscuro, capturas de detalle/formulario/diálogos; no simular evidencia.

## 6. Checklist
- [ ] Aprobación de Lukas.
- [x] API y datos locales preparados por scripts.
- [x] Cliente, cálculos, editor, estados y diálogos con pruebas temáticas.
- [x] Diez repeticiones, gates, smoke y QA real, con salvedades documentadas.

## 7. Registro
2026-11-21: implementación y verificaciones locales en rama propia. Suite propia de 56 pruebas repetida diez veces; suite global de 599 correcta. QA con API/PostgreSQL reales en 375/1280 claro/oscuro, 16 capturas y mediciones en [evidencia](../evidencias/modulo44/README.md). Revisión de Lukas pendiente y advertencia global de tamaño del bundle registrada; sin integración ni despliegue.

### Preparación de revisión — 2026-11-22

Rama actualizada con `git pull origin main`, conservando la tabla oficial de relevo y modificando solo la línea de Kelvin y la reserva propia. Script vigente: `database/014-consolidacion-carga.sql`. Propuesta, pendiente de aprobación de Lukas. Base PostgreSQL local recreada, seed cargado y entidades/contexto regenerados mediante scaffold, sin edición manual ni diferencias de contenido.

`./scripts/verify.sh`: salida0, 1140 pruebas backend y 599 frontend en 75 archivos. `./scripts/smoke-postgres.sh`: salida0, scaffold coincidente y SMOKE OK. Se usó VITEST_MAX_WORKERS=1 temporal, sin modificar pruebas, timeouts ni configuración compartida. Persisten los avisos globales de ESLint9/Vite; no se cambiaron dependencias ni umbrales.

No se generaron capturas nuevas ni se cambió la pantalla para estos ajustes. Se conserva la evidencia visual previa. Preparación local: sin nuevos PR, sin push y sin integración. Tras cada fusión anterior habrá que incorporar main y repetir estas verificaciones antes del PR correspondiente.

### Preparación del PR del 44 — 2026-11-25

- Incorporado `origin/main` (a0a5df67698e2ed97c6a33213de53a86405d1a17) después de integrar el PR #73 del módulo 43; se conserva todo lo integrado en main.
- Conflictos de pantallas, estilos, seed, smoke y pruebas de pluralización resueltos conservando main y los bloques originales del 44. `relevo.md` idéntico a main; asignaciones modifica únicamente la fila propia del 44.
- Contexto tomado de main, base PostgreSQL local recreada, scripts aplicados, seed cargado y entidades/contexto regenerados con `./scripts/scaffold.sh`, sin edición manual. Script propio: `014-consolidacion-carga.sql`.
- `./scripts/verify.sh`: exit 0; 1759 pruebas backend y 1455 frontend en 147 archivos; .NET sin warnings, lint, tipos y build correctos.
- `./scripts/smoke-postgres.sh`: exit 0, `SCAFFOLD AL DÍA` y `SMOKE OK`, incluidos los flujos del 44 y los existentes de main.
- `VITEST_MAX_WORKERS=1` temporal, sin modificar pruebas, timeouts ni configuración compartida. Se mantienen los avisos globales de ESLint 9 y tamaño del bundle de Vite.
- Evidencia visual real del 2026-11-21 conservada, sin cambios al bloque visual propio. Propuesta, pendiente de aprobación de Lukas. El script para la demo lo aplica Lukas; no se ejecutó SQL en producción.
