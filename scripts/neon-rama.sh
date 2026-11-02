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
#   NEON_ROLE      rol de la base (por defecto: titanglobal)
#   NEON_DATABASE  base de datos  (por defecto: titanglobal)
#   NEON_PARENT    nombre de la rama de origen (por defecto: la rama principal del proyecto)
#
# Una rama nueva hereda la contraseña de su origen, que es la de producción. Por eso el script
# la cambia nada más crearla: la cadena que recibe el compañero solo sirve para su rama.
#
# Cada cadena se envía al compañero por mensaje privado. Nunca se sube al repositorio.
# Si una rama ya existe, se reutiliza y solo se vuelve a imprimir su cadena.
# Para refrescar una rama, bórrala y vuelve a crearla con este script: no uses «reset» desde
# el origen, porque podría devolverle la contraseña de producción.
set -euo pipefail

if [[ $# -eq 0 ]]; then
    echo "Uso: $0 <nombre> [<nombre> ...]" >&2
    exit 1
fi
: "${NEON_API_KEY:?Falta NEON_API_KEY}"
: "${NEON_PROJECT_ID:?Falta NEON_PROJECT_ID}"
NEON_ROLE="${NEON_ROLE:-titanglobal}"
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

esperar_rama() {
    local intento pendientes
    for intento in $(seq 1 60); do
        pendientes="$(neon "${API}/operations?limit=20" | json "sum(1 for o in d['operations'] if o.get('branch_id')=='$1' and o['status'] in ('scheduling','running'))")"
        [[ "$pendientes" == "0" ]] && return 0
        sleep 2
    done
    echo "La rama $1 sigue ocupada tras 2 minutos" >&2
    return 1
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
        esperar_rama "$branch_id"
        neon -X POST "${API}/branches/${branch_id}/roles/${NEON_ROLE}/reset_password" > /dev/null
        esperar_rama "$branch_id"
        echo "Contraseña propia asignada a ${rama}" >&2
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
