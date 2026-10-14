#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://localhost:5080"
API_PID=""
COMPOSE_CLEANUP_REQUIRED=0

cleanup() {
    local exit_status=$?
    trap - EXIT INT TERM

    if [[ -n "$API_PID" ]]; then
        kill "$API_PID" 2>/dev/null || true
        wait "$API_PID" 2>/dev/null || true
    fi

    if [[ "$COMPOSE_CLEANUP_REQUIRED" == "1" ]]; then
        if ! docker compose down; then
            printf 'ERROR: docker compose down falló durante la limpieza\n' >&2
            exit_status=1
        fi
    fi

    exit "$exit_status"
}

trap cleanup EXIT
trap 'exit 130' INT TERM

check_port_is_free() {
    local port="$1"
    local listener

    listener="$(ss -H -ltn "sport = :${port}")"
    if [[ -n "$listener" ]]; then
        printf 'ERROR: el puerto %s ya está ocupado; no se iniciará el smoke\n' "$port" >&2
        printf '%s\n' "$listener" >&2
        return 1
    fi
}

check_port_is_free 5080
check_port_is_free 5433

COMPOSE_CLEANUP_REQUIRED=1
docker compose up -d db

DATABASE_READY=0
for attempt in $(seq 1 40); do
    if docker compose exec -T db pg_isready -U titanglobal -d titanglobal >/dev/null 2>&1; then
        DATABASE_READY=1
        break
    fi

    sleep 0.5
done

if [[ "$DATABASE_READY" != "1" ]]; then
    printf 'ERROR: PostgreSQL no quedó listo dentro del tiempo esperado\n' >&2
    exit 1
fi

dotnet tool restore
dotnet ef database update -p src/TitanGlobal.Api
docker compose exec -T db psql -U titanglobal -d titanglobal -v ON_ERROR_STOP=1 -f - < scripts/seed-dev.sql

ASPNETCORE_ENVIRONMENT=Development dotnet run --no-build --project src/TitanGlobal.Api --urls "$BASE_URL" >/dev/null 2>&1 &
API_PID=$!

API_READY=0
for attempt in $(seq 1 40); do
    if curl -sS --max-time 2 -o /dev/null "$BASE_URL/api/v1/sistema/salud" 2>/dev/null; then
        API_READY=1
        break
    fi

    if ! kill -0 "$API_PID" 2>/dev/null; then
        printf 'ERROR: la API terminó antes de responder en %s\n' "$BASE_URL" >&2
        exit 1
    fi

    sleep 0.5
done

if [[ "$API_READY" != "1" ]]; then
    printf 'ERROR: la API no respondió salud en %s dentro del tiempo esperado\n' "$BASE_URL" >&2
    exit 1
fi

validate_response() {
    local body="$1"
    local validation="$2"

    python3 - "$body" "$validation" <<'PY'
import json
import sys

body = json.loads(sys.argv[1])
validation = sys.argv[2]
checks = {
    "filter": lambda data: [item["id"] for item in data] == [1, 2, 7],
    "sum": lambda data: data["totalVentas"] == 10750.0,
    "top": lambda data: data["id"] == 7 and data["nombre"] == "Laptop Air" and data["precio"] == 3200.0,
    "average": lambda data: data["promedioCompra"] == 2500.0,
    "no-description": lambda data: [item["id"] for item in data] == [4, 6, 9],
    "frequency": lambda data: [(item["clienteId"], item["totalOrdenes"]) for item in data] == [(1, 4), (2, 2), (3, 1), (4, 1)],
    "hierarchy": lambda data: len(data) == 4 and data[0]["numeroOrden"] == "OC-001" and [item["nombreProducto"] for item in data[0]["detalles"]] == ["Laptop Pro", "Mouse Óptico"],
    "categories": lambda data: data == ["Computo", "Pantallas", "Perifericos"],
    "buyers": lambda data: data == ["Acme Industrial", "Beta Comercio"],
    "created": lambda data: data["id"] > 0,
    "empty-body": lambda data: data["StatusCode"] == 400 and data["Message"] == "El cuerpo de la petición (Body) no puede estar vacío.",
    "blocked": lambda data: data["StatusCode"] == 403 and data["Message"] == "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación." and "operación" in sys.argv[1] and "operaci\\u00F3n" not in sys.argv[1],
}

if validation not in checks or not checks[validation](body):
    raise SystemExit(f"la respuesta no coincide con la validación {validation}")
PY
}

request_check() {
    local label="$1"
    local method="$2"
    local path="$3"
    local expected_status="$4"
    local validation="$5"
    local payload="${6:-}"
    local role="${7:-}"
    local response
    local status
    local body

    if [[ -n "$payload" ]]; then
        if ! response="$(curl -sS --max-time 10 -X "$method" -H 'Content-Type: application/json' -H "X-User-Role: $role" -d "$payload" -w $'\n%{http_code}' "$BASE_URL$path")"; then
            printf 'ERROR: %s no pudo conectar con la API\n' "$label" >&2
            return 1
        fi
    elif [[ "$method" == "POST" ]]; then
        if ! response="$(curl -sS --max-time 10 -X POST -H 'Content-Type: application/json' -d '' -w $'\n%{http_code}' "$BASE_URL$path")"; then
            printf 'ERROR: %s no pudo conectar con la API\n' "$label" >&2
            return 1
        fi
    else
        if ! response="$(curl -sS --max-time 10 -w $'\n%{http_code}' "$BASE_URL$path")"; then
            printf 'ERROR: %s no pudo conectar con la API\n' "$label" >&2
            return 1
        fi
    fi

    status="${response##*$'\n'}"
    body="${response%$'\n'*}"
    printf '%s HTTP %s %s\n' "$label" "$status" "$body"

    if [[ "$status" != "$expected_status" ]]; then
        printf 'ERROR: %s esperaba HTTP %s, recibió HTTP %s; cuerpo: %s\n' "$label" "$expected_status" "$status" "$body" >&2
        return 1
    fi

    if [[ -n "$validation" ]]; then
        if ! validate_response "$body" "$validation"; then
            printf 'ERROR: %s devolvió un cuerpo inesperado: %s\n' "$label" "$body" >&2
            return 1
        fi
    fi
}

request_check 'Filtro de productos' GET '/api/v1/ordenes-compra/productos/filtro?termino=LAPTOP&minPrecio=1000' 200 filter
request_check 'Suma desde fecha simple' GET '/api/v1/ordenes-compra/ventas/suma?fechaInicio=2026-01-01' 200 sum
request_check 'Suma desde fecha sin zona' GET '/api/v1/ordenes-compra/ventas/suma?fechaInicio=2026-01-01T00%3A00%3A00' 200 sum
request_check 'Suma desde fecha UTC Z' GET '/api/v1/ordenes-compra/ventas/suma?fechaInicio=2026-01-01T00%3A00%3A00Z' 200 sum
request_check 'Producto más caro' GET '/api/v1/ordenes-compra/productos/mas-caro' 200 top
request_check 'Promedio del cliente 1' GET '/api/v1/ordenes-compra/clientes/1/promedio' 200 average
request_check 'Productos sin descripción' GET '/api/v1/ordenes-compra/productos/sin-descripcion' 200 no-description
request_check 'Frecuencia de clientes (patrón 9)' GET '/api/v1/ordenes-compra/clientes/frecuencia' 200 frequency
request_check 'Jerarquía de órdenes (patrón 10)' GET '/api/v1/ordenes-compra/clientes/1/jerarquia' 200 hierarchy
request_check 'Categorías únicas (patrón 11)' GET '/api/v1/ordenes-compra/clientes/1/categorias-unicas' 200 categories
request_check 'Compradores del producto (patrón 12)' GET '/api/v1/ordenes-compra/productos/3/clientes-compradores' 200 buyers
request_check 'POST vacío' POST '/api/v1/ordenes-compra' 400 empty-body
request_check 'POST rol bloqueado' POST '/api/v1/ordenes-compra' 403 blocked '{"numeroOrden":"OC-SMOKE-BLOCKED","clienteId":1,"total":1}' Bloqueado
request_check 'POST correcto' POST '/api/v1/ordenes-compra' 200 created '{"numeroOrden":"OC-SMOKE-001","clienteId":1,"total":1}'

printf 'SMOKE OK\n'
