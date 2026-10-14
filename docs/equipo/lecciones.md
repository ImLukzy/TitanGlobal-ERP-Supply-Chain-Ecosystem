# Lecciones del equipo

Una lección por entrada: qué pasó, por qué y cómo evitarlo.

- **Tests con puertos fijos:** no se ejecutan dos a la vez, porque se cruzan y dan falsos fallos. Antes de lanzarlos hay que comprobar que los puertos están libres.
- **Un fallo reportado por un agente puede venir de su entorno.** Antes de corregir código, se reproduce con las dependencias del repo restauradas.
- **Migraciones en producción:** copia de seguridad, ensayo en una copia y OK humano. Después hay que comprobar que se aplicaron de verdad, porque algunos hostings no ejecutan el paso previo al despliegue.
