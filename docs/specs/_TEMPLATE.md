# Especificación: NN - <Título corto>

> Copia este archivo como `NN-slug.md`. Una spec = un objetivo. Si necesitas "fases", son varias specs.

## 1. Objetivo
**Problema:** qué falla hoy o qué falta (con archivo y líneas si aplica).
**Resultado esperado:** qué podrá hacer el usuario o qué garantiza el sistema al terminar, en 1–3 frases.

## 2. Fuera de alcance
Lo que el agente **no** debe tocar aunque parezca relacionado. Ejemplos:
- Textos legales, de cobro o de comisión (requieren aprobación explícita).
- Eliminar páginas, rutas o funcionalidades existentes.
- Migraciones EF Core no listadas en §3.
- Refactors fuera de los archivos de §3.

**Decisiones de producto que requieren aprobación antes de ejecutar:** (lista o "ninguna").

## 3. Archivos afectados
| Archivo | Acción | Nota |
|---|---|---|
| `apps/web/src/...` | crear / modificar / eliminar | … |
| `apps/api/src/...` | … | … |
| `apps/api/docs/openapi.yaml` | modificar | si cambia un endpoint (gate `docs:check`) |

Rutas verificadas con `ls`/`grep` antes de redactar. Nada de "o equivalente".

## 4. Diseño y lógica
- **UI:** clases del sistema (`docs/specs/13-design-system-rudo.md`): `.btn-*`, `.card*`, `.input`, `.chip`; acentos solo con `primary`/`primary-soft`/`primary-ink`; motion solo con `SPRING` de `lib/motion.ts`.
- **API:** endpoints, contrato `{ data }` / `{ error: { code, message } }`, códigos nuevos.
- **Datos:** modelos y migraciones (nueva, fechada; nunca editar una aplicada).
- **Invariantes:** reglas de negocio del dominio (estados, cálculos, cierres de periodo) solo en UseCases; reglas de código de `CLAUDE.md`.

## 5. Criterios de aceptación (medibles)
Cada criterio debe poder comprobarse con un comando o una medición. Borra los que no apliquen.

| # | Criterio | Cómo se verifica | Umbral |
|---|---|---|---|
| A1 | Compilación | `dotnet build -warnaserror` | 0 errores, 0 warnings |
| A2 | Formato y reglas | `./scripts/verify.sh` (format + reglas de código) | en verde |
| A3 | Tests | `dotnet test` | todos en verde; +N tests nuevos: `<Clase>` › `<caso>` |
| A4 | Contrato API | OpenAPI (`/openapi/v1.json`) incluye las rutas nuevas | presentes |
| A5 | Comportamiento | test de integración: "<petición> → <código y cuerpo>" | pasa |
| A6 | Datos | migración nueva aplica limpia sobre PostgreSQL efímero | sin errores |

## 6. Checklist de ejecución
*(Marca `[x]` solo cuando la tarea esté hecha **y** A1–A3 estén en verde.)*

- [ ] Tarea 1: …
- [ ] Tarea 2: …
- [ ] Tarea N: Verificar A1–A6 aplicables y anotar resultados en §7.

## 7. Registro de verificación
| Fecha | Criterio | Resultado | Evidencia (comando / captura) |
|---|---|---|---|
| AAAA-MM-DD | A1 | ✅ / ❌ | … |
