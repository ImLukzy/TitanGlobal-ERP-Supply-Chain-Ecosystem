# Propuesta: módulo 47 — Control de combustible (frontend)

## 1. Objetivo
Pantalla Kelvin `/modulos/47-control-combustible` para registrar vales e intervalos manuales de odómetro, consultar importe y rendimiento orientativo, confirmar y anular conservando historial de cierre. Implementación local autorizada al continuar como los módulos anteriores; contrato backend propuesto y revisión de Lukas pendiente.

## 2. Fuera de alcance
Aplican las decisiones backend. Sin integración con41/74/76, inventario, contabilidad, geolocalización ni adjuntos. No inferir consumo real/tanque lleno ni sumar monedas distintas. Mostrar `Rendimiento orientativo del intervalo declarado` y unidades explícitas km/L, L/100km, litros y moneda. Referencias como texto, no enlaces.

## 3. Archivos
`frontend/src/modules/control-combustible/`: tipos.ts, api.ts/api.test.ts, validacion.ts/validacion.test.ts, calculos.ts/calculos.test.ts, presentacion.ts, useValesData.ts, ControlCombustiblePage.tsx, ValesResumen.tsx, ValesFiltros.tsx, ValesTabla.tsx, ValeDetallePanel.tsx, ValeFormulario.tsx, DialogoConfirmarVale.tsx, DialogoAnularVale.tsx y contenedor accesible DialogoVale.tsx. Pruebas temáticas ValesConsultas, ValesFormulario, ValesEstados y utilidades vales.testUtils.tsx. Registro propio en pantallas.ts, bloque final `.control-combustible-` en app.css y evidencia en docs/evidencias/modulo47/.

## 4. Diseño y lógica
- PaginaModulo, TablaPaginada, PanelDetalle, CampoConError y DialogoBaja compartidos, tokens, métricas con cifra grande/etiqueta debajo y estados por texto/color.
- Resumen total/borradores/confirmados/anulados, sin importes agregados. Búsqueda300ms, filtros estado/tipo, tamaño10; página1 al cambiar y corrección tras baja. Una carga inicial lista/resumen; descartar respuestas obsoletas y permitir reintentos independientes.
- Tabla de código, placa/estación, carga UTC, combustible, litros, importe/moneda, estado y detalle. Valores decimales del servidor como texto sin Number. Código, números/unidades, fechas y cabeceras sin partir; selección homogénea y scroll interno accesible.
- Detalle de datos, intervalo y rendimientos, con «Pendiente» cuando no hay final; observaciones y fechas/motivo. Borrador puede editar/baja/confirmar/anular, Confirmado solo anular, Anulado solo lectura.
- Formulario con mensajes/límites backend, fecha/hora explícita UTC sin desplazamiento por huso ni pérdida de milisegundos. Litros/precio/odómetros como cadenas editables. Final vacío → null, no0. Validar escala sin redondear y preservar valores ante400/409; primer error con foco/aria-invalid/aria-describedby, sin petición inválida.
- Previsualización exacta mediante BigInt escalados: litros/precio escala1000; importe multiplicado y redondeado a2; km/L y L/100km a3; mitades hacia fuera de cero. Sin aritmética flotante para resultados. Servidor autoridad del cálculo. Mostrar moneda ingresada y advertencia de estimación.
- Confirmación accesible explícita, anulación con motivo1–500 y DialogoBaja. Diálogos con foco inicial, Tab/Shift+Tab atrapados, Escape sin escribir y retorno al disparador; bloqueo sincrónico de doble envío y de cierre mientras se guarda. Mantener400/409 visible sin borrar datos.
- 1280: dos columnas lista/detalle-formulario. 375: apilado, margen≥16px, campos adaptados/tablas con scroll interno, sin overflow de página. Claro/oscuro y contraste≥4.5:1; comparar con pantalla integrada en ambos temas.

## 5. Aceptación
A1 contratos HTTP exactos de ocho operaciones, filtros vacíos omitidos. A2 temporizadores falsos y esperarCargaInicialVales antes de mockClear, filtros/páginas/respuestas antiguas. A3 límites, final nullable, UTC, cálculos/mitades/extremos y coincidencia servidor. A4 alta/edición/estados,400/409, doble envío y accesibilidad. A5 gate y frontend verify, diez repeticiones más una con carga, sin omitir ni ampliar timeouts. A6 API/PostgreSQL reales a375/1280claro/oscuro; capturas detalle/formulario/confirmación/anulación/baja, contraste y ausencia de números partidos/overflow.

## 6. Checklist
- [ ] Revisión de Lukas.
- [x] API y datos locales por scripts.
- [x] Pantalla, cálculos y pruebas.
- [x] Estabilidad, gates y QA reales documentados.

## 7. Registro
2026-11-21: 64 pruebas propias; gate final con 607 pruebas frontend, doble seed y QA real a 375/1280 px en claro/oscuro correctos. Se corrigió la pérdida de foco tras rechazar la confirmación y se repitió el QA en cuatro escenarios. Sobre la corrección pasaron diez ejecuciones propias y veinte con build simultáneo; el gate completo final terminó con salida 0. Avisos globales de ESLint y tamaño de Vite documentados en `docs/evidencias/modulo47/README.md`; revisión de Lukas e integración pendientes.
