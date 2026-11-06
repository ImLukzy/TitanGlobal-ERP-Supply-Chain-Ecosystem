#!/usr/bin/env bash
# Aplica a la base local (docker compose, servicio db) los scripts database/NNN-*.sql que falten, en orden.
# Cada script se aplica en una transacción y queda anotado en control."ScriptsAplicados".
#   ./scripts/aplicar-bd.sh             aplica los pendientes
#   ./scripts/aplicar-bd.sh --estado    muestra los aplicados y los pendientes, sin cambiar nada
#   ./scripts/aplicar-bd.sh --reiniciar borra la base local y la crea de nuevo desde database/ (pierdes sus datos)
set -euo pipefail
cd "$(dirname "$0")/.."

psql_local() {
    docker compose exec -T -e PGOPTIONS=--client-min-messages=warning db psql -U titanglobal -d titanglobal -v ON_ERROR_STOP=1 -q "$@"
}

if [[ "${1:-}" == "--reiniciar" ]]; then
    docker compose exec -T db psql -U titanglobal -d postgres -v ON_ERROR_STOP=1 -q \
        -c 'DROP DATABASE IF EXISTS titanglobal WITH (FORCE);' -c 'CREATE DATABASE titanglobal;'
    printf 'Base local borrada y creada vacía.\n'
fi

# Una base creada antes con migraciones de EF tiene las tablas pero no el registro de scripts.
if [[ "$(psql_local -At -c "SELECT to_regclass('control.\"ScriptsAplicados\"') IS NULL AND to_regclass('public.\"Clientes\"') IS NOT NULL;")" == "t" ]]; then
    printf 'ERROR: tu base local se creó con las migraciones antiguas. Recréala con: ./scripts/aplicar-bd.sh --reiniciar\n' >&2
    printf '       y vuelve a cargar los datos: docker compose exec -T db psql -U titanglobal -d titanglobal -f - < scripts/seed-dev.sql\n' >&2
    exit 1
fi

psql_local -c 'CREATE SCHEMA IF NOT EXISTS control;
CREATE TABLE IF NOT EXISTS control."ScriptsAplicados" (
    "Nombre" text NOT NULL,
    "AplicadoEn" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_ScriptsAplicados" PRIMARY KEY ("Nombre")
);' >/dev/null

aplicados="$(psql_local -At -c 'SELECT "Nombre" FROM control."ScriptsAplicados" ORDER BY 1;')"
pendientes=0
for script in database/[0-9][0-9][0-9]-*.sql; do
    nombre="$(basename "$script")"
    if grep -qxF "$nombre" <<< "$aplicados"; then
        [[ "${1:-}" == "--estado" ]] && printf 'aplicado   %s\n' "$nombre"
        continue
    fi
    pendientes=$((pendientes + 1))
    if [[ "${1:-}" == "--estado" ]]; then
        printf 'pendiente  %s\n' "$nombre"
        continue
    fi
    printf 'Aplicando %s\n' "$nombre"
    { cat "$script"; printf '\nINSERT INTO control."ScriptsAplicados" ("Nombre") VALUES (%s);\n' "'$nombre'"; } \
        | psql_local --single-transaction -f - >/dev/null
done

if [[ "${1:-}" != "--estado" ]]; then
    printf 'Base local al día (%s scripts aplicados ahora).\n' "$pendientes"
fi
