# Propuesta: módulo 49 — Logística inversa (frontend)

## 1. Objetivo
Pantalla Kelvin `/modulos/49-logistica-inversa`, coherente con sus módulos previos: programar retornos de pallets, retornables y devoluciones en ruta, registrar recogida/recepción con diferencias y cancelar conservando lo realizado. Propuesta local autorizada por Kelvin; aprobación de Lukas pendiente.

## 2. Fuera de alcance
Mismo alcance backend: referencias manuales, un material/unidad por retorno, sin inventario ni devolución comercial automática. No sumar cantidades heterogéneas ni inferir un saldo de existencias.

## 3. Archivos
`frontend/src/modules/logistica-inversa/`: tipos, api/api.test, validacion/validacion.test, presentacion, useRetornosData, LogisticaInversaPage, RetornosResumen, RetornosFiltros, RetornosTabla, RetornoDetallePanel, RetornoFormulario, DialogoRetorno, DialogoCantidadRetorno, DialogoCancelarRetorno, retornos.testUtils y pruebas temáticas RetornosConsultas/RetornosFormulario/RetornosEstados. Registro propio en pantallas.ts y bloque `.logistica-inversa-` al final de app.css; evidencia propia en docs/evidencias/modulo49/.

## 4. Diseño y lógica
PaginaModulo, TablaPaginada, PanelDetalle, CampoConError y DialogoBaja; tokens compartidos. Resumen de cinco cifras con etiquetas debajo. Búsqueda300ms con filtros estado/tipo, páginas10, reinicio al filtrar y corrección tras baja. Carga lista/resumen una vez, respuestas antiguas descartadas, reintentos independientes.

Tabla código, tipo, material/contraparte, programación UTC, cantidad/unidad y estado con detalle lateral a1280, apilado a375. Cifras, códigos y fechas en una línea con scroll interno accesible, sin overflow de página, fondo seleccionado uniforme. Detalle preserva referencias, cantidades y responsables/fechas de cada etapa; diferencias solo donde existen ambas cantidades, texto «Pendiente» para las ausentes. Resumen sin sumas entre materiales/unidades.

Formulario de cabecera solo Programado, validaciones idénticas al backend; cadenas editables para cantidades y UTC sin desplazamiento de huso ni pérdida de milisegundos. Cantidades de respuesta como texto sin convertir a Number. Envío numérico solo tras validación de precisión (máximo12 dígitos significativos). Sin cálculo monetario ni dependencia nueva.

Diálogos de recogida/recepción con cantidad y responsable, máximo visible según etapa; cancelación con motivo. Foco inicial, Tab/Shift+Tab atrapados, Escape sin mutar y retorno al disparador; foco permanece dentro mientras se deshabilitan controles y tras error. Bloquear doble envío y cierre durante operación, conservar datos400/409. Baja solo Programado con componente común.

375/1280 claro/oscuro, margen móvil≥16px y contrastes de texto/enlaces medidos≥4.5:1. Comparar escritorio en ambos temas con Contratos integrado. No tocar CSS global compartido.

## 5. Aceptación
A1 contratos HTTP exactos de nueve operaciones. A2 carga inicial controlada, debounce con temporizadores falsos, paginación/filtros y respuestas obsoletas. A3 validaciones de límites/decimales y preservación UTC. A4 flujos completos y errores, accesibilidad y doble envío. A5 gate, diez repeticiones propias y veinte con build simultáneo, sin ampliar timeouts. A6 QA con API/PostgreSQL reales en cuatro escenarios, capturas detalle/formulario/recogida/recepción/cancelación/baja, comparación integrada, sin desbordamiento ni números partidos.

## 6. Checklist
- [ ] Revisión de Lukas.
- [x] Backend/datos locales por scripts.
- [x] Pantalla y pruebas.
- [x] Gate, estabilidad y QA con evidencia, con avisos globales documentados.

## 7. Registro
2026-11-21: 48 pruebas propias y 591 globales correctas, gate completo salida 0, diez ejecuciones propias y veinte con build simultáneo sin ampliar timeouts. QA real repetido a 375/1280 px en claro/oscuro, preservando UTC con milisegundos y comprobando etapas, cantidades, cancelación, baja y foco. 24 capturas propias, dos referencias de Contratos y resultados en `docs/evidencias/modulo49/`. Avisos globales de ESLint/Vite documentados; aprobación de Lukas e integración pendientes.
