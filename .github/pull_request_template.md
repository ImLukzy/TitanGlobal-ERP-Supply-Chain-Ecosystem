## Qué cambia

- Módulo:
- Spec:
- Issue:
- Semana:

## Checklist

- [ ] Spec aprobada y §6 marcado.
- [ ] `./scripts/verify.sh` en verde.
- [ ] Si cambia la base de datos: migración generada con `dotnet ef` y `./scripts/smoke-postgres.sh` → `SMOKE OK`.
- [ ] Tests nuevos para las reglas y los errores del módulo.
- [ ] QA a 375 y 1280 px, en claro y en oscuro.
- [ ] Evidencia en §7 de la spec.
- [ ] Sin secretos, archivos generados ni cambios ajenos al módulo.

## Gate

```text
(pegar la salida de ./scripts/verify.sh)
```

## Capturas

- 375 px, claro:
- 375 px, oscuro:
- 1280 px, claro:
- 1280 px, oscuro:

## Notas para el revisor

...
