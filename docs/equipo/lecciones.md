# Lecciones del equipo

Una lección por entrada: qué pasó, por qué y cómo evitarlo.

- **Tests con puertos fijos:** no se ejecutan dos a la vez, porque se cruzan y dan falsos fallos. Antes de lanzarlos hay que comprobar que los puertos están libres.
- **Un fallo reportado por un agente puede venir de su entorno.** Antes de corregir código, se reproduce con las dependencias del repo restauradas.
- **Una spec no puede citar documentos que el equipo no tiene.** La spec 01 decía «del documento original» y Michael inventó campos y contratos. Toda referencia externa se copia al repo (`docs/enunciado.md`) antes de despachar.
- **Migraciones EF escritas a mano:** sin `Designer` ni `ModelSnapshot`, la siguiente migración vuelve a crear todo. Siempre `dotnet ef migrations add`.
- **Gate de reglas con huecos:** `check-rules.sh` dejaba pasar `public int X => 1;`, `get => …` y `foreach (var …)`. Cada regla necesita fixtures buenos y malos que cubran sus variantes.
- **Entorno de la máquina:** hacían falta `aspnet-runtime-10.0` (tests y API) y `docker-compose` (BD). Instalados el 2026-10-14.
- **Capturas con animaciones:** la captura del menú móvil de QA salía cortada porque se tomó a mitad de la transición de 0,2 s. Antes de capturar hay que esperar a que termine (`waitForTimeout` ≥ duración o `prefers-reduced-motion`), y las capturas de página completa no sirven para elementos `fixed`.
- **InMemory oculta fallos de Npgsql:** `ventas/suma?fechaInicio=2026-01-01` daba 500 en PostgreSQL (`DateTime` sin zona contra `timestamptz`) con todos los tests en verde, y el QA no lo vio porque probó con `…Z`. Las fechas se normalizan a UTC (`FechasUtc`) y cada módulo pasa `scripts/smoke-postgres.sh` con fechas sin zona.
- **Migraciones en producción:** copia de seguridad, ensayo en una copia y OK humano. Después hay que comprobar que se aplicaron de verdad, porque algunos hostings no ejecutan el paso previo al despliegue.

## Avisos de fin de tarea que no llegan (2026-10-14)
- **Qué pasó:** tras reiniciarse Orca, Antigravity no pasaba la comprobación de «listo» y los encargos se enviaron escribiendo en su terminal. Sus `worker_done` se rechazaron (sin tarea activa) y Claude, que solo miraba `orchestration check`, no vio los avisos.
- **Causa principal:** el run seguía teniendo como coordinador la terminal anterior de Claude (`term_ff3b0c37`, ya cerrada), así que `check` no entregaba los avisos a la terminal nueva.
- **Regla:** si Claude cambia de terminal o de sesión, lo primero es `orca orchestration run-use --id <run>` y confirmarlo con `run-current`. Después de despachar, la espera va EN SEGUNDO PLANO (Monitor o Bash con `run_in_background`: bucle con `orca orchestration check --run <run> --wait --types worker_done,escalation,question --timeout-ms 600000 --json` que termina al recibir mensajes) y Claude TERMINA el turno con un resumen breve para el humano. Al despertar por el aviso: procesar, `--ack <deliveryId>`, despachar lo siguiente y volver a esperar en segundo plano. Nunca un `check --wait` en primer plano: bloquea al humano durante decenas de minutos. Sin tarea activa, el agente avisa con `--type status` a `run:<run>` (ver `equipo.md`, «Cómo avisar al terminar»).

## Un test de regresión sustituido en un cambio de pulido (2026-10-14)
- **Qué pasó:** en la spec 11, el test de `formatearFecha` (protege el bug de fechas UTC de la spec 04) se reemplazó por el del helper nuevo en vez de añadirse. La auditoría lo marcó como menor.
- **Regla:** los tests existentes no se borran ni se sustituyen; los nuevos se añaden. Quitar un test es un hallazgo mayor salvo que la spec lo pida.

## Tests de frontend que dependían del tiempo real (2026-10-14)
- **Qué pasó:** dos tests (espera de 300 ms en Clientes; «sin peticiones» en Productos) fallaron en el gate con la máquina cargada y pasaban al repetir. Usaban `setTimeout` reales y exigían cero llamadas a `fetch` mientras aún llegaban cargas iniciales.
- **Regla:** esperas con temporizadores falsos (`vi.advanceTimersByTimeAsync`); antes de `mockClear`, esperar a que termine toda la carga inicial; comprobar la ausencia de una petición concreta filtrando por URL. Antes de entregar, repetir la suite 10 veces y otra vez con carga.
