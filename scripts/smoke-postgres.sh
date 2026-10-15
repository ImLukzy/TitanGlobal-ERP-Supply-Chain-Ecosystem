#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://localhost:5080"
API_PID=""
COMPOSE_CLEANUP_REQUIRED=0
CLIENTE_CREADO_ID=""
PRODUCTO_CREADO_ID=""
PROVEEDOR_CREADO_ID=""
SOLICITUD_CREADA_ID=""
ORDEN_ABASTECIMIENTO_CREADA_ID=""
ALMACEN_CREADO_ID=""
ZONA_CREADA_ID=""
SEGMENTO_CREADO_ID=""
COTIZACION_CREADA_ID=""

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
import re
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
    "clients-list": lambda data: data["total"] == 1 and [item["id"] for item in data["items"]] == [1] and data["items"][0]["ruc"] == "20111111111",
    "cliente-detail": lambda data: data["id"] == 1 and data["nombre"] == "Acme Industrial" and data["ruc"] == "20111111111" and data["totalOrdenes"] == 4 and data["montoTotal"] == 10000.0 and data["ultimaOrdenFecha"] == "2026-03-10T00:00:00Z",
    "clientes-summary": lambda data: data["totalClientes"] == 5 and data["clientesConOrdenes"] == 4 and data["clientesSinDescripcion"] == 3 and [(item["clienteId"], item["montoTotal"]) for item in data["topClientesPorMonto"]] == [(1, 10000.0), (2, 2000.0), (4, 750.0), (3, 500.0)],
    "cliente-created": lambda data: data["id"] > 0,
    "cliente-duplicate": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe un cliente con el RUC 20111111111.",
    "cliente-delete-protected": lambda data: data["StatusCode"] == 409 and data["Message"] == "No se puede eliminar el cliente 1 porque tiene órdenes de compra.",
    "products-category": lambda data: data["total"] == 3 and [item["id"] for item in data["items"]] == [7, 2, 1] and all(item["categoria"] == "Computo" for item in data["items"]),
    "products-price": lambda data: data["total"] == 5 and [item["id"] for item in data["items"]] == [10, 5, 8, 9, 4],
    "product-detail": lambda data: data["id"] == 1 and data["nombre"] == "Laptop Pro" and data["unidadesVendidas"] == 1 and data["montoVendido"] == 2500.0 and data["clientesDistintos"] == 1,
    "product-categories": lambda data: data == ["Accesorios", "Computo", "Oficina", "Pantallas", "Perifericos"],
    "products-summary": lambda data: data["totalProductos"] == 10 and data["productosSinDescripcion"] == 3 and data["precioPromedio"] == 994.5 and [(item["categoria"], item["totalProductos"], item["precioMaximo"]) for item in data["porCategoria"]] == [("Accesorios", 1, 25.0), ("Computo", 3, 3200.0), ("Oficina", 1, 800.0), ("Pantallas", 2, 900.0), ("Perifericos", 3, 200.0)] and [item["productoId"] for item in data["masVendidos"]] == [3, 7, 1, 5, 9],
    "product-created": lambda data: data["id"] > 0,
    "product-delete-protected": lambda data: data["StatusCode"] == 409 and data["Message"] == "No se puede eliminar el producto 1 porque tiene ventas registradas.",
    "providers-list": lambda data: data["total"] == 3 and [item["id"] for item in data["items"]] == [1, 3, 2] and all(item["pais"] == "Peru" for item in data["items"]),
    "providers-inactive": lambda data: data["total"] == 1 and data["items"][0]["id"] == 5 and data["items"][0]["activo"] is False,
    "provider-detail": lambda data: data["id"] == 1 and data["razonSocial"] == "Andes Suministros S.A.C." and data["ruc"] == "20600000001" and data["pais"] == "Peru" and data["activo"] is True,
    "provider-countries": lambda data: data == ["Chile", "China", "Peru"],
    "providers-summary": lambda data: data["totalProveedores"] == 6 and data["activos"] == 5 and data["inactivos"] == 1 and data["sinEmail"] == 2 and [(item["pais"], item["totalProveedores"], item["activos"]) for item in data["porPais"]] == [("Peru", 3, 3), ("Chile", 2, 1), ("China", 1, 1)],
    "provider-created": lambda data: data["id"] > 0,
    "openapi-segmentos": lambda data: all(path in data["paths"] for path in ["/api/v1/segmentos-clientes", "/api/v1/segmentos-clientes/{id}", "/api/v1/segmentos-clientes/{id}/clientes", "/api/v1/segmentos-clientes/clasificacion", "/api/v1/segmentos-clientes/resumen"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/segmentos-clientes", "get"), ("/api/v1/segmentos-clientes", "post"), ("/api/v1/segmentos-clientes/{id}", "get"), ("/api/v1/segmentos-clientes/{id}", "put"), ("/api/v1/segmentos-clientes/{id}", "delete"), ("/api/v1/segmentos-clientes/{id}/clientes", "get"), ("/api/v1/segmentos-clientes/clasificacion", "get"), ("/api/v1/segmentos-clientes/resumen", "get")]),
    "cotizaciones-list": lambda data: data["total"] == 2 and [item["numero"] for item in data["items"]] == ["COT-DEV-001", "COT-DEV-002"] and data["items"][0]["total"] == 180.0,
    "cotizaciones-expired": lambda data: data["total"] == 1 and data["items"][0]["numero"] == "COT-DEV-002" and data["items"][0]["vencida"] is True,
    "cotizacion-detail": lambda data: data["numero"] == "COT-DEV-001" and data["clienteNombre"] == "Acme Industrial" and data["subtotal"] == 200.0 and data["descuento"] == 20.0 and data["total"] == 180.0 and data["lineas"][0]["importe"] == 180.0,
    "cotizaciones-summary": lambda data: data["total"] == 4 and data["emitidas"] == 2 and data["vencidas"] == 1 and data["aceptadas"] == 1 and data["rechazadas"] == 1 and data["montoVigente"] == 180.0 and data["montoAceptado"] == 80.0 and data["tasaAceptacion"] == 50.0,
    "cotizacion-created": lambda data: data["id"] > 4 and data["numero"] == f"COT-{data['id']:06d}",
    "cotizacion-accepted": lambda data: data["estado"] == "Aceptada" and data["ordenCompraNumero"] == f"OC-{data['numero']}" and data["ordenCompraId"] > 0 and data["total"] == 180.0,
    "cotizacion-already-accepted": lambda data: data["StatusCode"] == 409 and re.fullmatch(r"La cotización COT-\d{6} ya está aceptada\.", data["Message"]) is not None,
    "cotizacion-expired-conflict": lambda data: data["StatusCode"] == 409 and data["Message"] == "La cotización COT-DEV-002 está vencida.",
    "cotizacion-empty-reason": lambda data: data["StatusCode"] == 400 and data["Message"] == "El motivo de rechazo es obligatorio.",
    "cotizacion-order-hierarchy": lambda data: len(data) == 1 and re.fullmatch(r"OC-COT-\d{6}", data[0]["numeroOrden"]) is not None and data[0]["nombreCliente"] == "Cliente sin órdenes",
    "openapi-cotizaciones": lambda data: all(path in data["paths"] for path in ["/api/v1/cotizaciones", "/api/v1/cotizaciones/{id}", "/api/v1/cotizaciones/{id}/aceptar", "/api/v1/cotizaciones/{id}/rechazar", "/api/v1/cotizaciones/resumen"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/cotizaciones", "get"), ("/api/v1/cotizaciones", "post"), ("/api/v1/cotizaciones/{id}", "get"), ("/api/v1/cotizaciones/{id}", "put"), ("/api/v1/cotizaciones/{id}", "delete"), ("/api/v1/cotizaciones/{id}/aceptar", "post"), ("/api/v1/cotizaciones/{id}/rechazar", "post"), ("/api/v1/cotizaciones/resumen", "get")]),
    "segments-list": lambda data: data["total"] == 4 and [item["codigo"] for item in data["items"]] == ["VIP", "FRECUENTE", "OCASIONAL", "PREMIUM-OLD"] and [item["clientes"] for item in data["items"]] == [1, 1, 2, 0],
    "segments-classification": lambda data: data["total"] == 5 and [(item["clienteId"], item["segmentoCodigo"]) for item in data["items"]] == [(1, "VIP"), (2, "FRECUENTE"), (4, "OCASIONAL"), (3, "OCASIONAL"), (5, None)],
    "segments-unassigned": lambda data: data["total"] == 1 and data["items"][0]["clienteId"] == 5 and data["items"][0]["segmentoId"] is None,
    "segment-customers": lambda data: data["total"] == 1 and data["items"][0]["clienteId"] == 2,
    "segments-summary": lambda data: data["totalClientes"] == 5 and data["clientesSegmentados"] == 4 and data["clientesSinSegmento"] == 1 and [(item["codigo"], item["clientes"], item["montoTotal"]) for item in data["porSegmento"]] == [("VIP", 1, 10000.0), ("FRECUENTE", 1, 2000.0), ("OCASIONAL", 2, 1250.0)],
    "segment-created": lambda data: data["id"] > 4,
    "segment-priority-duplicate": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe un segmento con la prioridad 1.",
    "warehouses-city": lambda data: data["total"] == 2 and [item["codigo"] for item in data["items"]] == ["ALM-LIM-01", "ALM-LIM-02"],
    "warehouses-type": lambda data: data["total"] == 3 and [item["codigo"] for item in data["items"]] == ["ALM-ARE-01", "ALM-LIM-02", "ALM-TRU-01"],
    "warehouses-inactive": lambda data: data["total"] == 1 and data["items"][0]["codigo"] == "ALM-TRU-01" and data["items"][0]["activo"] is False,
    "warehouse-detail": lambda data: data["id"] == 1 and data["codigo"] == "ALM-LIM-01" and data["tipo"] == "Central" and data["capacidadM3"] == 500.0,
    "warehouse-cities": lambda data: data == ["Arequipa", "Callao", "Lima", "Trujillo"],
    "warehouses-summary": lambda data: data["totalAlmacenes"] == 5 and data["activos"] == 4 and data["inactivos"] == 1 and data["capacidadTotalM3"] == 1025.0 and data["capacidadActivaM3"] == 925.0 and [(item["tipo"], item["totalAlmacenes"], item["capacidadM3"]) for item in data["porTipo"]] == [("Central", 1, 500.0), ("Regional", 3, 450.0), ("Transito", 1, 75.0)] and [item["ciudad"] for item in data["porCiudad"]] == ["Lima", "Arequipa", "Callao", "Trujillo"],
    "warehouse-created": lambda data: data["id"] > 0,
    "warehouse-duplicate": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe un almacén con el código ALM-LIM-01.",
    "zones-warehouse": lambda data: data["total"] == 4 and [item["codigo"] for item in data["items"]] == ["ALM-A", "DES-01", "PIC-01", "REC-01"],
    "zones-picking": lambda data: data["total"] == 1 and data["items"][0]["codigo"] == "PIC-01",
    "zone-detail": lambda data: data["almacenId"] == 1 and data["almacenCodigo"] == "ALM-LIM-01" and data["codigo"] == "REC-01" and data["capacidadM3"] == 50.0,
    "zones-summary": lambda data: data["totalZonas"] == 4 and data["activas"] == 4 and [(item["tipo"], item["totalZonas"], item["capacidadM3"]) for item in data["porTipo"]] == [("Recepcion", 1, 50.0), ("Almacenamiento", 1, 300.0), ("Picking", 1, 60.0), ("Despacho", 1, 40.0)] and len(data["ocupacion"]) == 1 and data["ocupacion"][0]["almacenCodigo"] == "ALM-LIM-01" and data["ocupacion"][0]["capacidadAsignadaM3"] == 450.0 and data["ocupacion"][0]["porcentajeAsignado"] == 90.0,
    "zone-created": lambda data: data["id"] > 0,
    "zone-capacity": lambda data: data["StatusCode"] == 409 and data["Message"] == "La capacidad de las zonas activas (510.00 m³) superaría la del almacén ALM-LIM-01 (500.00 m³).",
    "zone-duplicate": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe la zona REC-01 en el almacén ALM-LIM-01.",
    "warehouse-delete-protected": lambda data: data["StatusCode"] == 409 and data["Message"] == "No se puede eliminar el almacén 1 porque tiene zonas.",
    "provider-duplicate": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe un proveedor con el RUC 20600000001.",
    "solicitudes-pendientes": lambda data: data["total"] == 2 and [item["estado"] for item in data["items"]] == ["Pendiente", "Pendiente"],
    "solicitudes-rango": lambda data: data["total"] == 2 and [item["numero"] for item in data["items"]] == ["SC-DEV-003", "SC-DEV-002"],
    "solicitud-detalle": lambda data: data["numero"] == "SC-DEV-001" and data["estado"] == "Pendiente" and data["proveedorNombre"] == "Andes Suministros S.A.C." and data["montoEstimado"] == 4760.0 and [linea["productoId"] for linea in data["lineas"]] == [1, 3] and [linea["subtotal"] for linea in data["lineas"]] == [4600.0, 160.0],
    "solicitudes-resumen": lambda data: data["total"] == 5 and data["pendientes"] == 2 and data["aprobadas"] == 2 and data["rechazadas"] == 1 and data["montoPendiente"] == 16760.0 and data["montoAprobado"] == 10200.0 and [item["productoId"] for item in data["topProductosSolicitados"]] == [2, 3, 7, 1, 5] and [item["unidades"] for item in data["topProductosSolicitados"]] == [10, 4, 3, 2, 2],
    "solicitud-created": lambda data: data["id"] > 0 and data["numero"] == f"SC-{data['id']:06d}",
    "solicitud-approved": lambda data: data["estado"] == "Aprobada" and data["fechaDecision"].endswith("Z"),
    "solicitud-already-approved": lambda data: data["StatusCode"] == 409 and re.fullmatch(r"La solicitud SC-\d{6} ya está aprobada\.", data["Message"]) is not None,
    "solicitud-inactive-provider": lambda data: data["StatusCode"] == 400 and data["Message"] == "El proveedor 5 está inactivo.",
    "solicitud-delete-approved": lambda data: data["StatusCode"] == 409 and re.fullmatch(r"La solicitud SC-\d{6} ya está aprobada\.", data["Message"]) is not None,
    "ordenes-emitidas": lambda data: data["total"] == 2 and [item["numero"] for item in data["items"]] == ["OA-DEV-001", "OA-DEV-002"] and all(item["estado"] == "Emitida" for item in data["items"]),
    "orden-detalle": lambda data: data["numero"] == "OA-DEV-001" and data["estado"] == "Emitida" and data["solicitudNumero"] == "SC-DEV-003" and data["montoTotal"] == 8700.0 and [linea["productoId"] for linea in data["lineas"]] == [7] and [linea["subtotal"] for linea in data["lineas"]] == [8700.0],
    "ordenes-resumen": lambda data: data["total"] == 4 and data["emitidas"] == 2 and data["recibidas"] == 1 and data["anuladas"] == 1 and data["atrasadas"] == 1 and data["montoPorRecibir"] == 9700.0 and data["montoRecibido"] == 250.0 and [(item["proveedorId"], item["ordenes"], item["monto"]) for item in data["topProveedores"]] == [(1, 1, 8700.0), (2, 1, 1000.0), (3, 1, 250.0)],
    "orden-created": lambda data: data["id"] > 0 and data["numero"] == f"OA-{data['id']:06d}",
    "orden-repeated": lambda data: data["StatusCode"] == 409 and re.fullmatch(r"La solicitud SC-DEV-004 ya tiene la orden OA-\d{6}\.", data["Message"]) is not None,
    "orden-received": lambda data: data["estado"] == "Recibida" and data["fechaRecepcion"].endswith("Z"),
    "orden-already-received": lambda data: data["StatusCode"] == 409 and re.fullmatch(r"La orden OA-\d{6} ya está recibida\.", data["Message"]) is not None,
    "orden-pending-request": lambda data: data["StatusCode"] == 409 and data["Message"] == "La solicitud SC-DEV-001 no está aprobada.",
    "orden-missing-cancel-reason": lambda data: data["StatusCode"] == 400 and data["Message"] == "El motivo de anulación es obligatorio.",
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
        if ! response="$(curl -sS --max-time 10 -X "$method" -w $'\n%{http_code}' "$BASE_URL$path")"; then
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

    if [[ "$validation" == "cliente-created" ]]; then
        CLIENTE_CREADO_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "product-created" ]]; then
        PRODUCTO_CREADO_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "provider-created" ]]; then
        PROVEEDOR_CREADO_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "solicitud-created" ]]; then
        SOLICITUD_CREADA_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "cotizacion-created" ]]; then
        COTIZACION_CREADA_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "segment-created" ]]; then
        SEGMENTO_CREADO_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "orden-created" ]]; then
        ORDEN_ABASTECIMIENTO_CREADA_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "warehouse-created" ]]; then
        ALMACEN_CREADO_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "zone-created" ]]; then
        ZONA_CREADA_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
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
request_check 'Clientes listado por nombre' GET '/api/v1/clientes?busqueda=acme&pagina=1&tamanoPagina=20' 200 clients-list
request_check 'Detalle de cliente 1' GET '/api/v1/clientes/1' 200 cliente-detail
request_check 'Resumen de clientes' GET '/api/v1/clientes/resumen' 200 clientes-summary
request_check 'POST cliente válido' POST '/api/v1/clientes' 200 cliente-created '{"nombre":"Cliente Smoke","ruc":"20987654321","descripcion":"Alta de smoke"}'
request_check 'POST cliente con RUC existente' POST '/api/v1/clientes' 409 cliente-duplicate '{"nombre":"Duplicado Smoke","ruc":"20111111111"}'
request_check 'DELETE cliente con órdenes' DELETE '/api/v1/clientes/1' 409 cliente-delete-protected
request_check 'DELETE cliente recién creado' DELETE "/api/v1/clientes/${CLIENTE_CREADO_ID}" 204 ''
request_check 'Productos por categoría computo' GET '/api/v1/productos?categoria=computo' 200 products-category
request_check 'Productos por rango de precio' GET '/api/v1/productos?precioMin=100&precioMax=1000' 200 products-price
request_check 'Detalle del producto 1' GET '/api/v1/productos/1' 200 product-detail
request_check 'Categorías de productos' GET '/api/v1/productos/categorias' 200 product-categories
request_check 'Resumen de productos' GET '/api/v1/productos/resumen' 200 products-summary
request_check 'POST producto válido' POST '/api/v1/productos' 200 product-created '{"nombre":"Producto Smoke","categoria":"Pruebas","precio":12.50,"descripcion":"Alta de smoke"}'
request_check 'DELETE producto con ventas' DELETE '/api/v1/productos/1' 409 product-delete-protected
request_check 'DELETE producto recién creado' DELETE "/api/v1/productos/${PRODUCTO_CREADO_ID}" 204 ''
request_check 'Proveedores por país peru' GET '/api/v1/proveedores?pais=peru' 200 providers-list
request_check 'Proveedores inactivos' GET '/api/v1/proveedores?activo=false' 200 providers-inactive
request_check 'Detalle del proveedor 1' GET '/api/v1/proveedores/1' 200 provider-detail
request_check 'Países de proveedores' GET '/api/v1/proveedores/paises' 200 provider-countries
request_check 'Resumen de proveedores' GET '/api/v1/proveedores/resumen' 200 providers-summary
request_check 'POST proveedor válido' POST '/api/v1/proveedores' 200 provider-created '{"razonSocial":"Proveedor Smoke","ruc":"20987654321","pais":"Peru","email":"smoke@proveedor.example","telefono":"+51 1 555-0199","activo":true}'
request_check 'POST proveedor RUC del seed' POST '/api/v1/proveedores' 409 provider-duplicate '{"razonSocial":"Proveedor duplicado","ruc":"20600000001","pais":"Peru","activo":true}'
request_check 'DELETE proveedor recién creado' DELETE "/api/v1/proveedores/${PROVEEDOR_CREADO_ID}" 204 ''
request_check 'Almacenes por ciudad Lima' GET '/api/v1/almacenes?ciudad=lima' 200 warehouses-city
request_check 'Almacenes por tipo Regional' GET '/api/v1/almacenes?tipo=regional' 200 warehouses-type
request_check 'Almacenes inactivos' GET '/api/v1/almacenes?activo=false' 200 warehouses-inactive
request_check 'Detalle del almacén 1' GET '/api/v1/almacenes/1' 200 warehouse-detail
request_check 'Ciudades de almacenes' GET '/api/v1/almacenes/ciudades' 200 warehouse-cities
request_check 'Resumen de almacenes' GET '/api/v1/almacenes/resumen' 200 warehouses-summary
request_check 'POST almacén válido' POST '/api/v1/almacenes' 200 warehouse-created '{"codigo":"ALM-SMK-01","nombre":"Almacén Smoke","tipo":"Regional","direccion":"Calle Smoke 1","ciudad":"Lima","capacidadM3":20,"responsable":"QA","activo":true}'
request_check 'POST almacén con código del seed' POST '/api/v1/almacenes' 409 warehouse-duplicate '{"codigo":"ALM-LIM-01","nombre":"Duplicado","tipo":"Central","direccion":"Calle Smoke 2","ciudad":"Lima","capacidadM3":20}'
request_check 'DELETE almacén recién creado' DELETE "/api/v1/almacenes/${ALMACEN_CREADO_ID}" 204 ''
request_check 'OpenAPI de segmentación' GET '/openapi/v1.json' 200 openapi-segmentos
request_check 'Segmentos listados y clientes asignados' GET '/api/v1/segmentos-clientes' 200 segments-list
request_check 'Clasificación de clientes' GET '/api/v1/segmentos-clientes/clasificacion' 200 segments-classification
request_check 'Clientes sin segmento' GET '/api/v1/segmentos-clientes/clasificacion?sinSegmento=true' 200 segments-unassigned
request_check 'Clientes del segmento frecuente' GET '/api/v1/segmentos-clientes/2/clientes' 200 segment-customers
request_check 'Resumen de segmentación' GET '/api/v1/segmentos-clientes/resumen' 200 segments-summary
request_check 'POST segmento válido' POST '/api/v1/segmentos-clientes' 200 segment-created '{"codigo":"SMOKE-SEG","nombre":"Segmento Smoke","prioridad":10,"montoMinimo":100,"ordenesMinimas":1,"activo":true}'
request_check 'POST prioridad repetida' POST '/api/v1/segmentos-clientes' 409 segment-priority-duplicate '{"codigo":"SMOKE-DUP","nombre":"Duplicado","prioridad":1,"montoMinimo":1,"ordenesMinimas":1,"activo":true}'
request_check 'DELETE segmento recién creado' DELETE "/api/v1/segmentos-clientes/${SEGMENTO_CREADO_ID}" 204 ''
request_check 'Zonas del almacén 1' GET '/api/v1/zonas-almacenaje?almacenId=1' 200 zones-warehouse
request_check 'Zonas de tipo picking' GET '/api/v1/zonas-almacenaje?tipo=picking' 200 zones-picking
request_check 'Detalle de zona 1' GET '/api/v1/zonas-almacenaje/1' 200 zone-detail
request_check 'Resumen de zonas del almacén 1' GET '/api/v1/zonas-almacenaje/resumen?almacenId=1' 200 zones-summary
request_check 'POST zona válida' POST '/api/v1/zonas-almacenaje' 200 zone-created '{"almacenId":1,"codigo":"SMK-01","nombre":"Zona Smoke","tipo":"Picking","temperatura":"Ambiente","capacidadM3":25,"activa":true}'
request_check 'POST zona que supera capacidad' POST '/api/v1/zonas-almacenaje' 409 zone-capacity '{"almacenId":1,"codigo":"SMK-02","nombre":"Zona Excedida","tipo":"Picking","temperatura":"Ambiente","capacidadM3":35,"activa":true}'
request_check 'POST zona con código repetido' POST '/api/v1/zonas-almacenaje' 409 zone-duplicate '{"almacenId":1,"codigo":"REC-01","nombre":"Zona Duplicada","tipo":"Recepcion","temperatura":"Ambiente","capacidadM3":1,"activa":true}'
request_check 'DELETE almacén con zonas' DELETE '/api/v1/almacenes/1' 409 warehouse-delete-protected
request_check 'DELETE zona creada' DELETE "/api/v1/zonas-almacenaje/${ZONA_CREADA_ID}" 204 ''
request_check 'Solicitudes pendientes' GET '/api/v1/solicitudes-compra?estado=pendiente' 200 solicitudes-pendientes
request_check 'Solicitudes por rango de fechas' GET '/api/v1/solicitudes-compra?fechaDesde=2026-02-15&fechaHasta=2026-03-10' 200 solicitudes-rango
request_check 'Detalle de solicitud 1' GET '/api/v1/solicitudes-compra/1' 200 solicitud-detalle
request_check 'Resumen de solicitudes' GET '/api/v1/solicitudes-compra/resumen' 200 solicitudes-resumen
request_check 'POST solicitud válida' POST '/api/v1/solicitudes-compra' 200 solicitud-created '{"solicitante":"Solicitud Smoke","proveedorId":1,"observaciones":"Prueba de smoke","lineas":[{"productoId":1,"cantidad":2,"precioEstimado":250}]}'
request_check 'Aprobar solicitud creada' POST "/api/v1/solicitudes-compra/${SOLICITUD_CREADA_ID}/aprobar" 200 solicitud-approved
request_check 'Aprobar solicitud aprobada' POST "/api/v1/solicitudes-compra/${SOLICITUD_CREADA_ID}/aprobar" 409 solicitud-already-approved
request_check 'POST solicitud con proveedor inactivo' POST '/api/v1/solicitudes-compra' 400 solicitud-inactive-provider '{"solicitante":"Solicitud Smoke","proveedorId":5,"lineas":[{"productoId":1,"cantidad":1,"precioEstimado":250}]}'
request_check 'DELETE solicitud aprobada' DELETE "/api/v1/solicitudes-compra/${SOLICITUD_CREADA_ID}" 409 solicitud-delete-approved
request_check 'Órdenes de abastecimiento emitidas' GET '/api/v1/ordenes-abastecimiento?estado=emitida' 200 ordenes-emitidas
request_check 'Detalle de orden de abastecimiento 1' GET '/api/v1/ordenes-abastecimiento/1' 200 orden-detalle
request_check 'Resumen de órdenes de abastecimiento' GET '/api/v1/ordenes-abastecimiento/resumen' 200 ordenes-resumen
request_check 'Generar orden desde solicitud SC-DEV-004' POST '/api/v1/ordenes-abastecimiento/desde-solicitud/4' 200 orden-created '{"proveedorId":2,"fechaEntregaEstimada":"2099-01-01T00:00:00Z","observaciones":"Orden smoke"}'
request_check 'Repetir orden desde solicitud SC-DEV-004' POST '/api/v1/ordenes-abastecimiento/desde-solicitud/4' 409 orden-repeated '{"proveedorId":2,"fechaEntregaEstimada":"2099-01-01T00:00:00Z"}'
request_check 'Recibir orden creada' POST "/api/v1/ordenes-abastecimiento/${ORDEN_ABASTECIMIENTO_CREADA_ID}/recibir" 200 orden-received
request_check 'Recibir orden ya recibida' POST "/api/v1/ordenes-abastecimiento/${ORDEN_ABASTECIMIENTO_CREADA_ID}/recibir" 409 orden-already-received
request_check 'Generar orden desde solicitud pendiente' POST '/api/v1/ordenes-abastecimiento/desde-solicitud/1' 409 orden-pending-request '{"fechaEntregaEstimada":"2099-01-01T00:00:00Z"}'
request_check 'Anular orden sin motivo' POST '/api/v1/ordenes-abastecimiento/2/anular' 400 orden-missing-cancel-reason '{"motivo":" "}'
request_check 'POST correcto' POST '/api/v1/ordenes-compra' 200 created '{"numeroOrden":"OC-SMOKE-001","clienteId":1,"total":1}'

request_check 'OpenAPI de cotizaciones' GET '/openapi/v1.json' 200 openapi-cotizaciones
request_check 'Listado de cotizaciones emitidas' GET '/api/v1/cotizaciones?estado=emitida' 200 cotizaciones-list
request_check 'Cotizaciones vencidas' GET '/api/v1/cotizaciones?vencidas=true' 200 cotizaciones-expired
request_check 'Detalle de cotización vigente' GET '/api/v1/cotizaciones/1' 200 cotizacion-detail
request_check 'Resumen de cotizaciones' GET '/api/v1/cotizaciones/resumen' 200 cotizaciones-summary
request_check 'Rechazar cotización sin motivo' POST '/api/v1/cotizaciones/1/rechazar' 400 cotizacion-empty-reason '{"motivo":" "}'
request_check 'Aceptar cotización vencida' POST '/api/v1/cotizaciones/2/aceptar' 409 cotizacion-expired-conflict
request_check 'POST cotización válida' POST '/api/v1/cotizaciones' 200 cotizacion-created '{"clienteId":5,"fechaValidez":"2099-01-01T00:00:00Z","observaciones":"Cotización smoke","lineas":[{"productoId":1,"cantidad":2,"precioUnitario":100,"descuentoPorcentaje":10}]}'
request_check 'Aceptar cotización y crear orden' POST "/api/v1/cotizaciones/${COTIZACION_CREADA_ID}/aceptar" 200 cotizacion-accepted
request_check 'Orden de cotización en la jerarquía del cliente' GET '/api/v1/ordenes-compra/clientes/5/jerarquia' 200 cotizacion-order-hierarchy
request_check 'Aceptar cotización ya aceptada' POST "/api/v1/cotizaciones/${COTIZACION_CREADA_ID}/aceptar" 409 cotizacion-already-accepted
printf 'SMOKE OK\n'
