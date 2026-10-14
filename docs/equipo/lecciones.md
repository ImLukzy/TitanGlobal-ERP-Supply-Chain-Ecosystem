# Lecciones del equipo

Una lección por entrada: qué pasó, por qué y cómo evitarlo.

- **Tests con puertos fijos:** no se ejecutan dos a la vez, porque se cruzan y dan falsos fallos. Antes de lanzarlos hay que comprobar que los puertos están libres.
- **Un fallo reportado por un agente puede venir de su entorno.** Antes de corregir código, se reproduce con las dependencias del repo restauradas.
- **Una spec no puede citar documentos que el equipo no tiene.** La spec 01 decía «del documento original» y Michael inventó campos y contratos. Toda referencia externa se copia al repo (`docs/enunciado.md`) antes de despachar.
- **Migraciones EF escritas a mano:** sin `Designer` ni `ModelSnapshot`, la siguiente migración vuelve a crear todo. Siempre `dotnet ef migrations add`.
- **Gate de reglas con huecos:** `check-rules.sh` dejaba pasar `public int X => 1;`, `get => …` y `foreach (var …)`. Cada regla necesita fixtures buenos y malos que cubran sus variantes.
- **Entorno de la máquina:** hacían falta `aspnet-runtime-10.0` (tests y API) y `docker-compose` (BD). Instalados el 2026-10-14.
- **Migraciones en producción:** copia de seguridad, ensayo en una copia y OK humano. Después hay que comprobar que se aplicaron de verdad, porque algunos hostings no ejecutan el paso previo al despliegue.
