# Especificación: panel-inicio-frontend — Panel de operaciones en la portada

## 1. Objetivo
**Problema:** la portada (`frontend/src/pages/Inicio.tsx`) solo muestra un saludo fijo y ocho tarjetas sin enlace, con el texto «10 módulos disponibles» aunque casi la mitad de los dominios no tenga pantallas. Los resúmenes que ya expone la API no se ven juntos en ningún sitio.

**Resultado esperado:** al entrar, se ven indicadores reales del negocio, lo que requiere atención y el avance real de pantallas por dominio, con enlaces a cada módulo.

## 2. Fuera de alcance
- Backend, endpoints nuevos, migraciones y datos de ejemplo. Solo se consumen resúmenes existentes.
- Las pantallas de los módulos, incluida `21-productos`, que ya tiene catálogo, filtros, detalle, alta, edición y baja.
- Dependencias nuevas: las barras se dibujan con CSS.

**Decisiones de producto que requieren aprobación antes de ejecutar:** ninguna.

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `frontend/src/pages/datosPanelOperaciones.ts` | crear | Carga y transformación de datos (se llamaba `panelOperaciones.ts`; renombrado porque en macOS y Windows chocaba con `PanelOperaciones.tsx` al importar sin extensión) |
| `frontend/src/pages/PanelOperaciones.tsx` | crear | Indicadores, atención, procesos y barras |
| `frontend/src/pages/panel-operaciones.css` | crear | Archivo propio para no sumar conflictos en `app.css` |
| `frontend/src/pages/PanelOperaciones.test.tsx` | crear | Pruebas del panel y de los dominios |
| `frontend/src/pages/Inicio.tsx` | modificar | Título, panel y tarjetas de dominio con avance real |
| `frontend/src/modules/avance.ts` | crear | Módulos con pantalla registrada por dominio |
| `frontend/src/layout/AppLayout.tsx` | modificar | El menú muestra `n/10` en lugar de un `10` fijo |

## 4. Diseño y lógica
- **Fuentes:** `GET` de `clientes`, `productos`, `contratos-comerciales`, `cotizaciones`, `ordenes-abastecimiento`, `solicitudes-compra`, `reclamos-postventa` y `alertas`, siempre en `/resumen`, con las funciones `obtenerResumen*` de cada módulo.
- **Fallos parciales:** cada fuente es independiente. Si falla o llega incompleta, solo esa parte dice «No disponible» y aparece un aviso general. Si fallan todas, se muestra un error y se conservan los dominios.
- **Requiere atención:** alertas altas, órdenes atrasadas y reclamos vencidos («Urgente»); cotizaciones vencidas y solicitudes por aprobar («Aviso»). Se omiten los ceros. «Nada pendiente por ahora.» solo aparece si todas las fuentes respondieron.
- **Estado de los procesos:** cada registro cae en un solo grupo, de modo que la suma coincide con el total del resumen.

| Proceso | Completado | En curso | Descartado |
|---|---|---|---|
| Cotizaciones | aceptadas | emitidas | rechazadas |
| Solicitudes de compra | aprobadas | pendientes | rechazadas |
| Órdenes de abastecimiento | recibidas | emitidas | anuladas |
| Reclamos | resueltos + cerrados | abiertos + en proceso | rechazados |

- **Barras:** máximo cinco filas, de mayor a menor valor, siempre con la cifra en texto.
- **Color:** rojo y verde no se distinguen con daltonismo (ΔE 4,4 en claro y 2,3 en oscuro), así que los procesos usan verde, azul y gris, con ΔE 22,3 y 17,5. Cada segmento lleva nombre y cifra en texto, y «Urgente» o «Aviso» van escritos.
- **Avance:** se calcula desde `pantallas.ts`, de modo que aumenta solo cuando un módulo registra su pantalla. Una pantalla parcial cuenta como registrada. Un dominio sin pantallas no lleva enlace.

## 5. Criterios de aceptación (medibles)
- **A1.** `./scripts/verify.sh` en verde.
- **A2.** `PanelOperaciones.test.tsx` cubre indicadores y enlaces, orden y omisión en atención, fallo parcial, respuesta incompleta, fallo total, suma de procesos y conteo de dominios.
- **A3.** A 375 px y 1280 px, en claro y oscuro: sin desborde horizontal, sin errores de consola y sin cifras partidas.
- **A4.** Con datos reales, cada cifra coincide con el `/resumen` de su módulo.

## 6. Evidencia
- **A1:** `./scripts/verify.sh` termina con código 0: 908 tests de backend y 479 de frontend, sin cambios de modelo pendientes.
- **A2:** los casos están en `frontend/src/pages/PanelOperaciones.test.tsx`.
- **A3:** comprobado con Chromium a 1280 y 375 px, en claro y oscuro, contra la API desplegada: desborde 0, 0 errores de consola y cifras en una sola línea.
- **A4:** las cifras de la portada se compararon a mano con las respuestas de `/resumen` de cada módulo.
- **No ejecutado:** `smoke-postgres.sh`, porque el cambio no toca backend ni base de datos.
