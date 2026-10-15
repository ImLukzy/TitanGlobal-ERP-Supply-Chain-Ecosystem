# Equipo y flujo de trabajo

Claude es el integrador: trabaja en el worktree `titan-claudio` (rama `ImLukzy/Claudio`), reparte las tareas con Orca, revisa, integra y pide permiso al humano antes de publicar.

| Miembro | Worktree (rama = mismo nombre) | Agente | Rol |
|---|---|---|---|
| Michael | `titan-michael` | Codex | escritor de código |
| Dwight | `titan-dwight` | Codex | escritor de código (tareas que no pisen los archivos de Michael) |
| Kelly | `titan-kelly` | Antigravity | auditora, solo lectura: lee solo la spec y el diff |
| Pam | `titan-pam` | Antigravity | QA, solo lectura: prueba la API, el OpenAPI y los casos de error; en el frontend, capturas a 375 px y 1280 px en claro y oscuro |
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
- Fuente de requisitos: `docs/enunciado.md` (backend) y `docs/arquitectura.md` (desviaciones decididas y frontend). Ningún encargo cita documentos que no estén en el repo.
- Puertos de desarrollo: API 5080, frontend 5173, PostgreSQL 5433.
- Los agentes Codex usan `/home/lukzy/.local/bin/orca-ide` en lugar de `orca`.
- Hay que verificar los reportes de los agentes. Un fallo puede venir de su entorno; por ejemplo, de dependencias sin restaurar (`dotnet restore`).

## Cómo avisar al terminar (para que el mensaje llegue)
El run del equipo es `run_88f9b99f511a`. Al acabar un encargo, escribe el informe en el archivo que indique el encargo y avisa a Claude con UNO de estos comandos (Codex: `/home/lukzy/.local/bin/orca-ide` en lugar de `orca`):
- **Encargo despachado por Orca** (el texto empieza con «You are working inside Orca… dispatched worker» y trae tu handle y el id de la tarea): `orca orchestration send --type worker_done --from <tu handle> --task-id <task> --dispatch-id <dispatch> --outcome succeeded|failed --report-path <informe> --subject "…" --body "…"`. Usa exactamente el `--from` que da el preámbulo.
- **Encargo escrito directamente en tu terminal** (sin preámbulo de Orca): `worker_done` se rechaza porque no hay tarea activa. Usa `orca orchestration send --run run_88f9b99f511a --to run:run_88f9b99f511a --type status --report-path <informe> --subject "…" --body "…"`.
- Comprueba la respuesta del comando: si dice «Rejected», envía de nuevo con `--type status`. No des el trabajo por entregado sin un aviso aceptado.
- El asunto empieza por el resultado: `OK`, `BLOQUEADO`, `APROBADO` o `CAMBIOS`.
- `/tmp` se borra al reiniciar la máquina: el informe y las capturas se entregan en cuanto terminas.
