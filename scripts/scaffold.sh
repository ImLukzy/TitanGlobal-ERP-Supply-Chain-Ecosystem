#!/usr/bin/env bash
# Genera las entidades (src/TitanGlobal.Domain/Models) y el TitanGlobalDbContext desde la base local.
#   ./scripts/scaffold.sh              regenera los archivos del proyecto
#   ./scripts/scaffold.sh --comprobar  genera en una carpeta temporal y falla si el proyecto no está al día
# Antes: docker compose up -d db y ./scripts/aplicar-bd.sh. El proyecto tiene que compilar.
set -euo pipefail
cd "$(dirname "$0")/.."

if ./scripts/aplicar-bd.sh --estado | grep -q '^pendiente'; then
    printf 'ERROR: la base local tiene scripts sin aplicar. Ejecuta antes ./scripts/aplicar-bd.sh\n' >&2
    exit 1
fi

clave="$(sed -nE "s/^\s*POSTGRES_PASSWORD:\s*['\"]?([^'\"]*)['\"]?\s*$/\1/p" docker-compose.yml | head -1 | tr -d '\r')"
conexion="Host=localhost;Port=5433;Database=titanglobal;Username=titanglobal;Password=${clave}"
modelos="../TitanGlobal.Domain/Models"
contexto="Persistence"
temporal=""

if [[ "${1:-}" == "--comprobar" ]]; then
    temporal="$(mktemp -d)"
    trap 'rm -rf "$temporal"' EXIT
    modelos="$temporal/Models"
    contexto="$temporal/Persistence"
fi

dotnet tool restore >/dev/null
marca="$(mktemp)"
dotnet ef dbcontext scaffold "$conexion" Npgsql.EntityFrameworkCore.PostgreSQL \
    -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api \
    --context TitanGlobalDbContext --context-dir "$contexto" --output-dir "$modelos" \
    --context-namespace TitanGlobal.Infrastructure.Persistence --namespace TitanGlobal.Domain.Models \
    --schema public --no-onconfiguring --force >/dev/null

if [[ -z "$temporal" ]]; then
    # El scaffold reescribe todas las entidades de tablas que existen; las que no reescribió son de tablas borradas.
    for archivo in src/TitanGlobal.Domain/Models/*.cs; do
        if grep -q '^// Generado por scripts/scaffold.sh' "$archivo" && [[ ! "$archivo" -nt "$marca" ]]; then
            git rm -q "$archivo" 2>/dev/null || rm -f "$archivo"
            printf 'Borrada (su tabla ya no existe): %s\n' "$archivo"
        fi
    done
    rm -f "$marca"
    dotnet format >/dev/null
    printf 'Entidades y TitanGlobalDbContext regenerados. Revisa el diff con git diff.\n'
    exit 0
fi

# Compara sin el BOM ni los saltos de línea de Windows, que dotnet format normaliza.
normalizar() {
    sed '1s/^\xEF\xBB\xBF//' "$1" | tr -d '\r' | sed -E 's/[[:space:]]+/ /g; s/^ //; s/ $//' | grep -v '^$'
}
distintos=0
for generado in "$temporal"/Models/*.cs "$temporal"/Persistence/TitanGlobalDbContext.cs; do
    nombre="$(basename "$generado")"
    if [[ "$nombre" == "TitanGlobalDbContext.cs" ]]; then
        actual="src/TitanGlobal.Infrastructure/Persistence/$nombre"
    else
        actual="src/TitanGlobal.Domain/Models/$nombre"
    fi
    if [[ ! -f "$actual" ]] || ! diff -q <(normalizar "$generado") <(normalizar "$actual") >/dev/null; then
        printf 'Desactualizado: %s\n' "$actual" >&2
        distintos=$((distintos + 1))
    fi
done
for actual in src/TitanGlobal.Domain/Models/*.cs; do
    if grep -q '^// Generado por scripts/scaffold.sh' "$actual" && [[ ! -f "$temporal/Models/$(basename "$actual")" ]]; then
        printf 'Sobra (su tabla ya no existe): %s\n' "$actual" >&2
        distintos=$((distintos + 1))
    fi
done
for actual in src/TitanGlobal.Domain/Models/*.cs; do
    grep -q '^// Generado por scripts/scaffold.sh' "$actual" && continue
    while read -r clase; do
        if [[ ! -f "$temporal/Models/$clase.cs" ]]; then
            printf 'Entidad sin tabla en database/ (escrita a mano): %s en %s\n' "$clase" "$actual" >&2
            distintos=$((distintos + 1))
        fi
    done < <(grep -oE '\bclass [A-Za-z_][A-Za-z0-9_]*' "$actual" | awk '{print $2}')
    if grep -qE '^\s*public (sealed |abstract )?class ' "$actual"; then
        printf 'Solo se admiten clases partial escritas a mano en Models: %s\n' "$actual" >&2
        distintos=$((distintos + 1))
    fi
done
if [[ "$distintos" -gt 0 ]]; then
    printf 'ERROR: el código generado no coincide con la base de datos. Ejecuta ./scripts/scaffold.sh\n' >&2
    exit 1
fi
printf 'SCAFFOLD AL DÍA: entidades y TitanGlobalDbContext coinciden con database/.\n'
