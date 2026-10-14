# Equipo y flujo de trabajo

Claude es el integrador: trabaja en el worktree `titan-claudio` (rama `ImLukzy/Claudio`), reparte las tareas con Orca, revisa, integra y pide permiso al humano antes de publicar.

| Miembro | Worktree (rama = mismo nombre) | Agente | Rol |
|---|---|---|---|
| Michael | `titan-michael` | Codex | escritor de código |
| Dwight | `titan-dwight` | Codex | escritor de código (tareas que no pisen los archivos de Michael) |
| Kelly | `titan-kelly` | Antigravity | auditora, solo lectura: lee solo la spec y el diff |
| Pam | `titan-pam` | Antigravity | QA, solo lectura: prueba la API, el OpenAPI y los casos de error |
| Jim | `titan-jim` | OpenCode | explorador, solo lectura |

## Flujo por tarea
1. **Explorar** (Jim): informe breve con archivos y riesgos.
2. **Spec** en `docs/specs/NN-*.md`, la escribe Claude.
3. **Implementar** (Michael o Dwight): un commit, con `./scripts/verify.sh` en verde.
4. **Auditar** (Kelly): máximo 3 ciclos de corrección. Un cambio pequeño sin lógica puede saltarse este paso.
5. **QA** (Pam).
6. **Integrar** en `ImLukzy/Claudio`, con el gate en verde y la evidencia en la spec.
7. **Publicar** en `main` solo con el OK explícito del humano.

## Reglas
- Commits con autor y committer `ImLukzy <lukas.melgar@tecsup.edu.pe>`, sin trailers ni menciones de IA.
- Solo Claude escribe en §6 y §7 de las specs.
- Los encargos son autocontenidos: contexto, archivos, reglas, gate y formato de reporte (OK/BLOQUEADO).
- Los agentes Codex usan `/home/lukzy/.local/bin/orca-ide` en lugar de `orca`.
- Hay que verificar los reportes de los agentes. Un fallo puede venir de su entorno; por ejemplo, de dependencias sin restaurar (`dotnet restore`).
