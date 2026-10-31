# Equipo y flujo de trabajo

El equipo lo forman 7 personas. Cada módulo lo lleva una sola persona de principio a fin (backend y pantalla); el reparto está en [`asignaciones.md`](asignaciones.md).

Lukas Melgar hace además la integración: revisa cada pull request, integra en `main` y despliega la demo.

## Personas

| Persona | Módulos | Bloque |
|---|---|---|
| Lukas Melgar | 13 (todos terminados) | Integración. Comercial (01–07), Compras (11, 13, 15), Inventarios (21, 23, 24) |
| Leonel Portillo | 11 | Finanzas (51–60) y 75 ImpuestosTasas |
| Antony Huayhua | 11 | Manufactura (31–40) y 79 ParametrosSistema |
| Kelvin Pinto | 11 | Logística (41–50) y 73 Sucursales |
| Alexander | 11 | Capital Humano (61–70) y 71 Usuarios |
| Sergio | 11 | Compras (12, 14, 16–20), 72 RolesPermisos, 74 MonedasTiposCambio, 76 SeriesNumeracion, 80 CierresPeriodo |
| Maxi | 12 | Inventarios WMS (22, 25–30) y 08, 09, 10, 77, 78 |

## Flujo por módulo

1. **Spec** en `docs/specs/NN-*.md` (plantilla `_TEMPLATE.md`). Lukas la revisa antes de programar.
2. **Rama propia** desde la de integración: `git switch -c tu-nombre/moduloXX-nombre main`.
3. **Gate:** `./scripts/verify.sh` en verde antes de pedir revisión.
4. **Auditoría:** revisión por otro compañero, que lee solo la spec y el diff. Máximo 3 ciclos de corrección. Un cambio pequeño sin lógica puede saltarse este paso.
5. **QA** a 375 px y 1280 px, en claro y en oscuro, con la lista de comprobación de [`lecciones.md`](lecciones.md).
6. **Pull request** contra `main`, con la salida del gate, la evidencia en la spec y capturas.
7. **Integración:** Lukas revisa, integra en `main` con el gate en verde, actualiza la evidencia, [`contexto-proyecto.md`](../contexto-proyecto.md), el README y las asignaciones, y despliega.
8. **Publicar en producción** solo con el OK explícito de Lukas para ese cambio.

## Reglas

- Commits con la identidad git de quien los hace, sin trailers ni menciones a herramientas de IA.
- Solo la integración (Lukas) escribe en §6 y §7 de las specs.
- Fuente de requisitos: `docs/enunciado.md` (backend) y `docs/arquitectura.md` (desviaciones decididas y frontend). Ninguna spec cita documentos que no estén en el repo.
- Puertos de desarrollo: API 5080, frontend 5173, PostgreSQL 5433.
- Un fallo reportado puede venir del entorno de quien lo reporta; por ejemplo, de dependencias sin restaurar (`dotnet restore`). Antes de corregir código, se reproduce con las dependencias restauradas.

## Cómo avisar al terminar

Al terminar un módulo o una pantalla, la persona avisa a Lukas en la pull request con uno de estos resultados: `OK`, `BLOQUEADO`, `APROBADO` o `CAMBIOS`, y con la evidencia (salida del gate, capturas). Lukas la anota en §6 y §7 de la spec. No se da el trabajo por entregado hasta que la integración lo haya visto.

`/tmp` se borra al reiniciar la máquina: las capturas y los informes se adjuntan a la pull request o a la spec en cuanto estén listos.
