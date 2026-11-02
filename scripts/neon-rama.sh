#!/usr/bin/env bash
# Crea una rama de Neon por compañero (copia de la base de la demo) e imprime
# su cadena de conexión en formato Npgsql. Lo ejecuta solo Lukas (integración).
#
# Uso:
#   export NEON_API_KEY=...          # Neon → Account settings → API keys (nunca al repo)
#   export NEON_PROJECT_ID=...       # Neon → proyecto titanglobal-erp → Settings → Project ID
#   ./scripts/neon-rama.sh leonel franco sergio
#
# Variables opcionales:
#   NEON_ROLE      rol de la base (por defecto: neondb_owner)
#   NEON_DATABASE  base de datos  (por defecto: titanglobal)
#   NEON_PARENT    nombre de la rama de origen (por defecto: la rama principal del proyecto)
#
# Cada cadena se envía al compañero por mensaje privado. Nunca se sube al repositorio.
# Si una rama ya existe, se reutiliza y solo se vuelve a imprimir su cadena.
set -euo pipefail

if [[ $# -eq 0 ]]; then
    echo "Uso: $0 <nombre> [<nombre> ...]" >&2
    exit 1
fi
: "${NEON_API_KEY:?Falta NEON_API_KEY}"
: "${NEON_PROJECT_ID:?Falta NEON_PROJECT_ID}"
NEON_ROLE="${NEON_ROLE:-neondb_owner}"
NEON_DATABASE="${NEON_DATABASE:-titanglobal}"
for nombre in "$@"; do
    if [[ ! "$nombre" =~ ^[a-z0-9-]+$ ]]; then
        echo "Nombre no válido: ${nombre} (solo minúsculas, números y guiones)" >&2
        exit 1
    fi
done
API="https://console.neon.tech/api/v2/projects/${NEON_PROJECT_ID}"

neon() {
    curl -sS --fail-with-body -H "Authorization: Bearer ${NEON_API_KEY}" \
        -H "Accept: application/json" -H "Content-Type: application/json" "$@"
}

json() {
    python3 -c "import json,sys; d=json.load(sys.stdin); print($1)"
}

ramas="$(neon "${API}/branches")"

parent_id=""
if [[ -n "${NEON_PARENT:-}" ]]; then
    parent_id="$(printf '%s' "$ramas" | json "next((b['id'] for b in d['branches'] if b['name']=='${NEON_PARENT}'), '')")"
    if [[ -z "$parent_id" ]]; then
        echo "No existe la rama de origen ${NEON_PARENT}" >&2
        exit 1
    fi
fi

for nombre in "$@"; do
    rama="dev-${nombre}"
    branch_id="$(printf '%s' "$ramas" | json "next((b['id'] for b in d['branches'] if b['name']=='${rama}'), '')")"

    if [[ -z "$branch_id" ]]; then
        cuerpo="$(python3 -c "import json,sys; b={'name':sys.argv[1]}; p=sys.argv[2]
if p: b['parent_id']=p
print(json.dumps({'branch':b,'endpoints':[{'type':'read_write'}]}))" "$rama" "$parent_id")"
        branch_id="$(neon -X POST "${API}/branches" -d "$cuerpo" | json "d['branch']['id']")"
        echo "Creada ${rama} (${branch_id})" >&2
    else
        echo "Ya existía ${rama} (${branch_id})" >&2
    fi

    uri="$(neon -G "${API}/connection_uri" \
        --data-urlencode "branch_id=${branch_id}" \
        --data-urlencode "database_name=${NEON_DATABASE}" \
        --data-urlencode "role_name=${NEON_ROLE}" | json "d['uri']")"

    npgsql="$(python3 - "$uri" <<'PY'
import sys
from urllib.parse import urlparse, unquote
u = urlparse(sys.argv[1])
print(f"Host={u.hostname};Database={u.path.lstrip('/')};Username={unquote(u.username)};"
      f"Password={unquote(u.password)};SSL Mode=VerifyFull")
PY
)"
    printf '\n%s:\n  %s\n' "$nombre" "$npgsql"
done
