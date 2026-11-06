# Lecciones del equipo

Una lección por entrada: qué pasó, por qué y cómo evitarlo.

- **Tests con puertos fijos:** no se ejecutan dos a la vez, porque se cruzan y dan falsos fallos. Antes de lanzarlos hay que comprobar que los puertos están libres.
- **Un fallo reportado por un compañero puede venir de su entorno.** Antes de corregir código, se reproduce con las dependencias del repo restauradas.
- **Una spec no puede citar documentos que el equipo no tiene.** La spec 01 decía «del documento original» y la implementación inventó campos y contratos. Toda referencia externa se copia al repo (`docs/enunciado.md`) antes de despachar.
- **Código generado editado a mano:** una entidad o el `TitanGlobalDbContext` retocados a mano se pierden o chocan en el siguiente scaffold, y el código deja de reflejar la base. El esquema se cambia en un script `database/NNN-*.sql`, se aplica con `./scripts/aplicar-bd.sh` y se regenera con `./scripts/scaffold.sh`; el smoke avisa si no coincide. (Antes el proyecto usaba migraciones de EF Core y la lección era no escribirlas a mano.)
- **Gate de reglas con huecos:** `check-rules.sh` dejaba pasar `public int X => 1;`, `get => …` y `foreach (var …)`. Cada regla necesita fixtures buenos y malos que cubran sus variantes.
- **Entorno de la máquina:** hacían falta `aspnet-runtime-10.0` (tests y API) y `docker-compose` (BD). Instalados el 2026-10-14.
- **Capturas con animaciones:** la captura del menú móvil de QA salía cortada porque se tomó a mitad de la transición de 0,2 s. Antes de capturar hay que esperar a que termine (`waitForTimeout` ≥ duración o `prefers-reduced-motion`), y las capturas de página completa no sirven para elementos `fixed`.
- **InMemory oculta fallos de Npgsql:** `ventas/suma?fechaInicio=2026-01-01` daba 500 en PostgreSQL (`DateTime` sin zona contra `timestamptz`) con todos los tests en verde, y el QA no lo vio porque probó con `…Z`. Las fechas se normalizan a UTC (`FechasUtc`) y cada módulo pasa `scripts/smoke-postgres.sh` con fechas sin zona.
- **Scripts de base de datos en producción:** copia de seguridad, ensayo en una copia y OK humano. Después hay que comprobar que se aplicaron de verdad, porque algunos hostings no ejecutan el paso previo al despliegue.

## Avisos de fin de tarea que no llegan (2026-10-14)
- **Qué pasó:** tras reiniciar una sesión de trabajo, los avisos de fin de tarea de un colaborador se rechazaron (sin tarea activa) y la integración, que solo miraba la cola de avisos, no los vio.
- **Causa principal:** la sesión de integración seguía apuntando a la anterior (ya cerrada), así que los avisos no le llegaban a la nueva.
- **Regla:** si cambia la sesión de integración, lo primero es confirmar que las tareas y los avisos apuntan a la sesión nueva. Después de despachar, la espera va en segundo plano y la integración termina su turno con un resumen breve para el humano. Al recibir el aviso: procesarlo, confirmarlo, despachar lo siguiente y volver a esperar. Nunca una espera en primer plano: bloquea al humano durante decenas de minutos. Sin tarea activa, el aviso se hace como mensaje de estado en la pull request (ver `equipo.md`, «Cómo avisar al terminar»).

## Un test de regresión sustituido en un cambio de pulido (2026-10-14)
- **Qué pasó:** en la spec 11, el test de `formatearFecha` (protege el bug de fechas UTC de la spec 04) se reemplazó por el del helper nuevo en vez de añadirse. La revisión lo marcó como menor.
- **Regla:** los tests existentes no se borran ni se sustituyen; los nuevos se añaden. Quitar un test es un hallazgo mayor salvo que la spec lo pida.

## Tests de frontend que dependían del tiempo real (2026-10-14)
- **Qué pasó:** dos tests (espera de 300 ms en Clientes; «sin peticiones» en Productos) fallaron en el gate con la máquina cargada y pasaban al repetir. Usaban `setTimeout` reales y exigían cero llamadas a `fetch` mientras aún llegaban cargas iniciales.
- **Regla:** esperas con temporizadores falsos (`vi.advanceTimersByTimeAsync`); antes de `mockClear`, esperar a que termine toda la carga inicial; comprobar la ausencia de una petición concreta filtrando por URL. Antes de entregar, repetir la suite 10 veces y otra vez con carga.
- **Ampliación (2026-10-15):** aparecieron dos más (`SolicitudesGenerarOrden`, Productos A2) con el mismo origen: buscar elementos con `findBy` mientras la carga inicial seguía en curso. Cada pantalla tiene ahora una función `esperarCargaInicial…` que espera listado, resumen y catálogos y comprueba que el listado se pidió una sola vez; los tests nuevos deben usarla. Prueba de estabilidad: 20 ejecuciones de la suite con carga.

## Cobertura de tests del backend (2026-10-15)
- **Qué pasó:** la primera entrega de la spec 27 traía 5 tests muy amplios para un módulo con ~20 mensajes de error, transiciones de estado, solapamientos y cálculo por periodo; la spec pedía un caso por mensaje.
- **Regla:** un test por cada mensaje literal de §4 (con `[Theory]` si encaja), los límites que la spec nombra y la comprobación de que los casos inválidos no cambian datos. Como referencia, los módulos con estados tienen 17–25 tests.

## Pantallas que no siguen el diseño común (2026-10-15)
- **Qué pasó:** varias pantallas nuevas (Contratos, ListasPrecios) llegaron con controles con el aspecto del navegador, tarjetas de resumen invertidas, captions grandes, detalle a todo el ancho sin cabecera o importes partidos en dos líneas, aunque la spec pedía los componentes compartidos.
- **Regla:** antes de entregar una pantalla, compararla a 1280 px en claro con una de referencia ya integrada (Contratos o Cotizaciones) con los mismos mocks: tarjetas (etiqueta pequeña, cifra grande), filtros y casillas comunes, `TablaPaginada`, captions pequeños, panel de detalle lateral con cabecera y ficha, importes y fechas sin partir y espaciado de `PaginaModulo` sin sobrescribir. Adjuntar ambas capturas al reporte.
- **Lista de comprobación (añadida tras las specs 30, 32, 34, 36 y 46, que repitieron estos fallos):**
  - comparar también en **modo oscuro**: enlaces con contraste ≥ 4.5:1 (nunca azul oscuro sobre fondo oscuro), pastillas sin contorno blanco y el mismo fondo en todas las celdas de una fila;
  - tarjetas: la cifra es solo el número («80.00 %», «36.00 h»); el texto explicativo va pequeño debajo, nunca dentro de la cifra;
  - todas las cabeceras de tabla con el mismo tamaño y en una línea; ninguna palabra, fecha ni importe partidos;
  - selects y campos con el estilo común también dentro de diálogos y del simulador/evaluador; placeholders que se lean completos;
  - nada se sale de su panel a 1280 px (casillas de filtros, columnas de la derecha);
  - **CSS común** (`app.css`, `.tabla-paginada`): no cambiar selectores de reglas compartidas, porque subir su especificidad (p. ej. `th` → `thead th`) pisa los estilos de cabecera de otras pantallas. Si hay que corregir algo común, hacerlo con una regla mínima nueva y adjuntar capturas de al menos 3 pantallas existentes antes y después.

## Choques de numeración en specs y paso a cuatro capas (2026-11-02)
- **Qué pasó:** al trabajar varias personas en paralelo, cuatro ramas eligieron el mismo número global de spec (`47`) a la vez. Además, en el proyecto único los casos de uso dependían directamente de `Npgsql` (`PostgresException`) y de ASP.NET (`StatusCodes`).
- **Regla:**
  - Las specs nuevas de módulo se nombran `docs/specs/moduloNN-nombre-{backend|frontend}.md`, **sin número global** (el número de módulo `NN` ya es único). Solo las specs transversales llevan número global.
  - En las cuatro capas (`Domain`, `Application`, `Infrastructure`, `Api`), `Application` usa `CodigosEstado` y captura `ViolacionRestriccionException` (`TipoViolacionRestriccion.Unicidad` o `ReferenciaForanea`), nunca `StatusCodes`, `DbUpdateException` ni `PostgresException`. El gate lo verifica con las reglas `f` y `g` de `scripts/check-rules.sh` y con `ArquitecturaTests`.
  - Quien tenga una rama empezada con la estructura anterior no la rehace a mano: crea una rama nueva desde `origin/main` y ejecuta `python3 scripts/adaptar-rama-capas.py rama origin/tu-nombre/moduloXX-nombre` (ver [`adaptar-rama-a-capas.md`](adaptar-rama-a-capas.md)).
