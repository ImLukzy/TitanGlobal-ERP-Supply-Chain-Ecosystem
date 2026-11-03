#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://localhost:5080"
API_PID=""
COMPOSE_CLEANUP_REQUIRED=0
CLIENTE_CREADO_ID=""
PRODUCTO_CREADO_ID=""
PROVEEDOR_CREADO_ID=""
IMPUESTO_CREADO_ID=""
CATEGORIA_PRODUCTO_CREADA_ID=""
SOLICITUD_CREADA_ID=""
ORDEN_ABASTECIMIENTO_CREADA_ID=""
ALMACEN_CREADO_ID=""
ZONA_CREADA_ID=""
SEGMENTO_CREADO_ID=""
COTIZACION_CREADA_ID=""
CONTRATO_CREADO_ID=""
CONTRATO_BORRADOR_ID=""
LISTA_PRECIOS_CREADA_ID=""
PROMOCION_CREADA_ID=""
DEVOLUCION_CREADA_ID=""
RECLAMO_CREADO_ID=""
LIQUIDACION_COMISION_ID=""
ROL_CREADO_ID=""
USUARIO_CREADO_ID=""
AUDITORIA_ALTA_ID=""
AUDITORIA_MODIFICACION_ID=""
AUDITORIA_BAJA_ID=""
CENTRO_TRABAJO_SEED_ID=""
CENTRO_TRABAJO_CREADO_ID=""

cleanup() {
    local exit_status=$?
    trap - EXIT INT TERM

    if [[ -n "$API_PID" ]]; then
        if [[ -n "$USUARIO_CREADO_ID" ]]; then
            curl -sS --max-time 3 -X DELETE "$BASE_URL/api/v1/usuarios/${USUARIO_CREADO_ID}" >/dev/null 2>&1 || true
        fi
        if [[ -n "$ROL_CREADO_ID" ]]; then
            curl -sS --max-time 3 -X DELETE "$BASE_URL/api/v1/roles/${ROL_CREADO_ID}" >/dev/null 2>&1 || true
        fi
        if [[ -n "$PROMOCION_CREADA_ID" ]]; then
            curl -sS --max-time 3 -X DELETE "$BASE_URL/api/v1/promociones/${PROMOCION_CREADA_ID}" >/dev/null 2>&1 || true
        fi
        if [[ -n "$LISTA_PRECIOS_CREADA_ID" ]]; then
            curl -sS --max-time 3 -X DELETE "$BASE_URL/api/v1/listas-precios/${LISTA_PRECIOS_CREADA_ID}" >/dev/null 2>&1 || true
        fi
        if [[ -n "$CENTRO_TRABAJO_CREADO_ID" ]]; then
            curl -sS --max-time 3 -X DELETE "$BASE_URL/api/v1/centros-trabajo/${CENTRO_TRABAJO_CREADO_ID}" >/dev/null 2>&1 || true
        fi
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
dotnet ef database update -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api
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
    local response_file

    response_file="$(mktemp)"
    printf '%s' "$body" > "$response_file"
    if python3 - "$response_file" "$validation" <<'PY'
import json
import re
import sys

with open(sys.argv[1], encoding="utf-8") as archivo:
    body = json.load(archivo)
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
    "blocked": lambda data: data["StatusCode"] == 403 and data["Message"] == "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación." and "Detail" not in data,
    "clients-list": lambda data: data["total"] == 1 and [item["id"] for item in data["items"]] == [1] and data["items"][0]["ruc"] == "20111111111",
    "cliente-detail": lambda data: data["id"] == 1 and data["nombre"] == "Acme Industrial" and data["ruc"] == "20111111111" and data["totalOrdenes"] == 4 and data["montoTotal"] == 10000.0 and data["ultimaOrdenFecha"] == "2026-03-10T00:00:00Z",
    "clientes-summary": lambda data: data["totalClientes"] == 5 and data["clientesConOrdenes"] == 4 and data["clientesSinDescripcion"] == 3 and [(item["clienteId"], item["montoTotal"]) for item in data["topClientesPorMonto"]] == [(1, 10000.0), (2, 2000.0), (4, 750.0), (3, 500.0)],
    "cliente-created": lambda data: data["id"] > 0,
    "cliente-updated": lambda data: data["id"] > 0,
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
    "openapi-contracts": lambda data: all(path in data["paths"] for path in ["/api/v1/contratos-comerciales", "/api/v1/contratos-comerciales/{id}", "/api/v1/contratos-comerciales/{id}/activar", "/api/v1/contratos-comerciales/{id}/finalizar", "/api/v1/contratos-comerciales/{id}/cancelar", "/api/v1/contratos-comerciales/resumen"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/contratos-comerciales", "get"), ("/api/v1/contratos-comerciales", "post"), ("/api/v1/contratos-comerciales/{id}", "get"), ("/api/v1/contratos-comerciales/{id}", "put"), ("/api/v1/contratos-comerciales/{id}", "delete"), ("/api/v1/contratos-comerciales/{id}/activar", "post"), ("/api/v1/contratos-comerciales/{id}/finalizar", "post"), ("/api/v1/contratos-comerciales/{id}/cancelar", "post"), ("/api/v1/contratos-comerciales/resumen", "get")]),
    "contracts-list": lambda data: data["total"] == 1 and data["items"][0]["numero"] == "CC-DEV-001" and data["items"][0]["montoConsumido"] == 9000.0 and data["items"][0]["porcentajeConsumido"] == 45.0,
    "contract-detail": lambda data: data["numero"] == "CC-DEV-001" and data["clienteNombre"] == "Acme Industrial" and data["montoConsumido"] == 9000.0 and data["saldo"] == 11000.0 and [item["numeroOrden"] for item in data["ordenesPeriodo"]] == ["OC-004", "OC-002", "OC-001"],
    "contracts-summary": lambda data: data["total"] == 3 and data["borradores"] == 1 and data["vigentes"] == 1 and data["cancelados"] == 1 and data["montoComprometidoVigente"] == 20000.0 and data["montoConsumidoVigente"] == 9000.0 and data["porcentajeConsumoVigente"] == 45.0 and data["porCondicion"] == [{"condicionPago":"Contado","contratos":1},{"condicionPago":"Credito30","contratos":1},{"condicionPago":"Credito60","contratos":1}],
    "contract-created": lambda data: data["id"] > 3 and data["numero"] == f"CC-{data['id']:06d}",
    "contract-overlap": lambda data: data["StatusCode"] == 409 and data["Message"] == "El cliente ya tiene el contrato vigente CC-DEV-001 en ese periodo.",
    "contract-activated": lambda data: data["estado"] == "Vigente" and data["numero"] == "CC-DEV-002" and data["fechaActivacion"] is not None,
    "contract-finalize-draft": lambda data: data["StatusCode"] == 409 and re.fullmatch(r"El contrato CC-\d{6} ya está borrador\.", data["Message"]) is not None,
    "contract-cancel-reason": lambda data: data["StatusCode"] == 400 and data["Message"] == "El motivo de cancelación es obligatorio.",
    "contract-delete-active": lambda data: data["StatusCode"] == 409 and data["Message"] == "El contrato CC-DEV-001 ya está vigente.",
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
    "categorias-producto-list": lambda data: [item["nombre"] for item in data] == ["Computo", "Pantallas", "Perifericos"] and all(item["activa"] is True for item in data),
    "categoria-producto-detail": lambda data: data["id"] == 1 and data["nombre"] == "Computo" and data["descripcion"] == "Laptops y equipos de cómputo" and data["activa"] is True,
    "categorias-producto-summary": lambda data: data["totalCategorias"] == 4 and data["categoriasActivas"] == 3 and data["categoriasInactivas"] == 1,
    "categoria-producto-created": lambda data: data["id"] > 0,
    "categoria-producto-duplicate": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe una categoría con el nombre computo.",
    "categoria-producto-deactivated": lambda data: data["nombre"] == "Categoría Smoke" and data["descripcion"] == "Alta de smoke" and data["activa"] is False,
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
    "openapi-listas-precios": lambda data: all(path in data["paths"] for path in ["/api/v1/listas-precios", "/api/v1/listas-precios/{id}", "/api/v1/listas-precios/precio", "/api/v1/listas-precios/resumen"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/listas-precios", "get"), ("/api/v1/listas-precios", "post"), ("/api/v1/listas-precios/{id}", "get"), ("/api/v1/listas-precios/{id}", "put"), ("/api/v1/listas-precios/{id}", "delete"), ("/api/v1/listas-precios/precio", "get"), ("/api/v1/listas-precios/resumen", "get")]),
    "listas-vigentes": lambda data: data["total"] == 2 and all(item["vigente"] for item in data["items"]),
    "lista-vip": lambda data: data["codigo"] == "VIP-2026" and data["segmentoCodigo"] == "VIP" and data["prioridad"] == 1 and [(item["productoId"], item["precio"]) for item in data["items"]] == [(1, 2300.0), (5, 820.0)],
    "precio-vip": lambda data: data["precio"] == 2300.0 and data["origen"] == "Segmento" and data["listaCodigo"] == "VIP-2026",
    "precio-general": lambda data: data["precio"] == 2450.0 and data["origen"] == "General" and data["listaCodigo"] == "GENERAL-2026",
    "precio-base": lambda data: data["precio"] == data["precioBase"] == 200.0 and data["origen"] == "Base" and data["listaId"] is None,
    "listas-summary": lambda data: data["total"] == 3 and data["activas"] == 3 and data["vigentes"] == 2 and data["deSegmento"] == 1 and data["generales"] == 2 and data["productosConPrecioEspecial"] == 3,
    "lista-created": lambda data: data["id"] > 0,
    "lista-priority-duplicate": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe una lista de precios con la prioridad 1.",
    "openapi-promociones": lambda data: all(path in data["paths"] for path in ["/api/v1/promociones", "/api/v1/promociones/{id}", "/api/v1/promociones/evaluar", "/api/v1/promociones/resumen"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/promociones", "get"), ("/api/v1/promociones", "post"), ("/api/v1/promociones/{id}", "get"), ("/api/v1/promociones/{id}", "put"), ("/api/v1/promociones/{id}", "delete"), ("/api/v1/promociones/evaluar", "post"), ("/api/v1/promociones/resumen", "get")]),
    "promos-list": lambda data: data["total"] == 4 and [item["codigo"] for item in data["items"]] == ["COMPRA-3000", "LAPTOP-10", "LAPTOP-VIP-15", "VERANO-2025"],
    "promos-vigentes": lambda data: data["total"] == 3 and all(item["vigente"] for item in data["items"]),
    "promo-detail": lambda data: data["codigo"] == "LAPTOP-10" and data["tipo"] == "Producto" and data["productoId"] == 1 and data["porcentajeDescuento"] == 10.0,
    "promo-acme": lambda data: data["subtotal"] == 5000.0 and data["totalTrasLineas"] == 4250.0 and data["descuentoCompra"] == 212.5 and data["total"] == 4037.5 and data["lineas"][0]["promocion"]["codigo"] == "LAPTOP-VIP-15" and data["promocionCompra"]["codigo"] == "COMPRA-3000",
    "promo-beta": lambda data: data["subtotal"] == 5000.0 and data["totalTrasLineas"] == 4500.0 and data["descuentoCompra"] == 225.0 and data["total"] == 4275.0 and data["lineas"][0]["promocion"]["codigo"] == "LAPTOP-10",
    "openapi-devoluciones": lambda data: all(path in data["paths"] for path in ["/api/v1/devoluciones-comerciales", "/api/v1/devoluciones-comerciales/{id}", "/api/v1/devoluciones-comerciales/ordenes/{ordenId}/disponible", "/api/v1/devoluciones-comerciales/{id}/aprobar", "/api/v1/devoluciones-comerciales/{id}/recibir", "/api/v1/devoluciones-comerciales/{id}/rechazar", "/api/v1/devoluciones-comerciales/resumen"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/devoluciones-comerciales", "get"), ("/api/v1/devoluciones-comerciales", "post"), ("/api/v1/devoluciones-comerciales/{id}", "get"), ("/api/v1/devoluciones-comerciales/{id}", "put"), ("/api/v1/devoluciones-comerciales/{id}", "delete"), ("/api/v1/devoluciones-comerciales/{id}/aprobar", "post"), ("/api/v1/devoluciones-comerciales/{id}/recibir", "post"), ("/api/v1/devoluciones-comerciales/{id}/rechazar", "post"), ("/api/v1/devoluciones-comerciales/ordenes/{ordenId}/disponible", "get"), ("/api/v1/devoluciones-comerciales/resumen", "get")]),
    "devoluciones-list": lambda data: data["total"] == 4 and [item["numero"] for item in data["items"]] == ["DEV-DEV-001", "DEV-DEV-002", "DEV-DEV-003", "DEV-DEV-004"],
    "devolucion-detail": lambda data: data["numero"] == "DEV-DEV-001" and data["clienteNombre"] == "Acme Industrial" and data["numeroOrden"] == "OC-001" and data["reclamoNumero"] == "REC-DEV-001" and data["estado"] == "Recibida" and data["total"] == 2500.0 and data["lineas"][0]["importe"] == 2500.0,
    "devolucion-disponible": lambda data: len(data) == 1 and data[0]["productoId"] == 9 and data[0]["productoNombre"] == "Silla Ejecutiva" and data[0]["comprado"] == 1 and data[0]["devuelto"] == 0 and data[0]["disponible"] == 1 and data[0]["precioUnitario"] == 800.0,
    "devoluciones-summary": lambda data: data["total"] == 4 and data["solicitadas"] == 1 and data["aprobadas"] == 1 and data["recibidas"] == 1 and data["rechazadas"] == 1 and data["montoReembolsado"] == 2500.0 and data["tasaDevolucion"] == 8.33 and [(item["motivo"], item["devoluciones"], item["unidades"]) for item in data["porMotivo"]] == [("Defecto", 1, 1), ("ErrorPedido", 1, 1), ("Arrepentimiento", 1, 1), ("Otro", 1, 1)],
    "devolucion-created": lambda data: data["id"] > 4 and data["numero"] == f"DEV-{data['id']:06d}",
    "devolucion-approved": lambda data: data["estado"] == "Aprobada" and data["fechaAprobacion"] is not None,
    "devolucion-received": lambda data: data["estado"] == "Recibida" and data["fechaRecepcion"] is not None,
    "devolucion-already-received": lambda data: data["StatusCode"] == 409 and data["Message"] == "La devolución DEV-DEV-001 está recibida." and "Detail" not in data,
    "devolucion-over-available": lambda data: data["StatusCode"] == 409 and data["Message"] == "Solo quedan 1 unidades de Silla Ejecutiva por devolver en la orden OC-008." and "Detail" not in data,
    "devolucion-product-not-order": lambda data: data["StatusCode"] == 400 and data["Message"] == "El producto 1 no está en la orden OC-008." and "Detail" not in data,
    "devolucion-rejection-reason": lambda data: data["StatusCode"] == 400 and data["Message"] == "El motivo de rechazo es obligatorio." and "Detail" not in data,
    "openapi-alertas": lambda data: all(path in data["paths"] for path in ["/api/v1/alertas", "/api/v1/alertas/resumen", "/api/v1/alertas/descartar", "/api/v1/alertas/restaurar"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/alertas", "get"), ("/api/v1/alertas/resumen", "get"), ("/api/v1/alertas/descartar", "post"), ("/api/v1/alertas/restaurar", "post")]),
    "openapi-roles": lambda data: all(path in data["paths"] for path in ["/api/v1/roles", "/api/v1/roles/{id}", "/api/v1/roles/{id}/permisos", "/api/v1/roles/matriz"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/roles", "get"), ("/api/v1/roles", "post"), ("/api/v1/roles/{id}", "get"), ("/api/v1/roles/{id}", "put"), ("/api/v1/roles/{id}", "delete"), ("/api/v1/roles/{id}/permisos", "get"), ("/api/v1/roles/{id}/permisos", "put"), ("/api/v1/roles/matriz", "get")]),
    "openapi-usuarios": lambda data: all(path in data["paths"] for path in ["/api/v1/usuarios", "/api/v1/usuarios/{id}", "/api/v1/usuarios/{id}/permisos", "/api/v1/usuarios/resumen"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/usuarios", "get"), ("/api/v1/usuarios", "post"), ("/api/v1/usuarios/{id}", "get"), ("/api/v1/usuarios/{id}", "put"), ("/api/v1/usuarios/{id}", "delete"), ("/api/v1/usuarios/{id}/permisos", "get"), ("/api/v1/usuarios/resumen", "get")]),
    "impuestos-list": lambda data: [(item["id"], item["codigo"], item["tipo"], item["porcentajeVigente"]) for item in sorted(data, key=lambda item: item["id"])] == [(1, "IGV", "IGV", 18.0), (2, "RET-4TA", "RETENCION", 8.0)],
    "impuesto-detail": lambda data: data["id"] == 1 and data["codigo"] == "IGV" and data["pais"] == "PER" and data["activo"] is True and [(tasa["porcentaje"], tasa["fechaInicio"], tasa["fechaFin"]) for tasa in data["tasas"]] == [(18.0, "2020-01-01T00:00:00Z", None)],
    "impuesto-created": lambda data: data["id"] > 0,
    "impuesto-duplicate": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe un impuesto con el código IGV." and "Detail" not in data,
    "tasa-created": lambda data: data["id"] > 0 and data["impuestoId"] > 0,
    "tasa-overlap": lambda data: data["StatusCode"] == 400 and data["Message"] == "Ya existe una tasa activa para ese periodo." and "Detail" not in data,
    "impuesto-smoke-detail": lambda data: data["codigo"] == "SMOKE-IMP" and [(tasa["porcentaje"], tasa["fechaInicio"], tasa["fechaFin"]) for tasa in data["tasas"]] == [(10.0, "2027-01-01T00:00:00Z", None), (5.0, "2026-01-01T00:00:00Z", "2026-12-31T00:00:00Z")],
    "impuesto-calculo": lambda data: data["impuestoId"] == 1 and data["codigo"] == "IGV" and data["baseImponible"] == 100.13 and data["porcentajeAplicado"] == 18.0 and data["importeImpuesto"] == 18.02 and data["total"] == 118.15 and data["fechaOperacion"] == "2026-05-10T00:00:00Z",
    "impuesto-calculo-abierto": lambda data: data["porcentajeAplicado"] == 10.0 and data["importeImpuesto"] == 20.0 and data["total"] == 220.0 and data["fechaOperacion"] == "2028-03-01T00:00:00Z",
    "impuesto-inactive": lambda data: data["StatusCode"] == 404 and data["Message"] == "El impuesto no existe o está inactivo." and "Detail" not in data,
    "openapi-impuestos": lambda data: all(path in data["paths"] for path in ["/api/v1/impuestos", "/api/v1/impuestos/{id}", "/api/v1/impuestos/{id}/tasas", "/api/v1/impuestos/calcular"]),
    "openapi-auditoria": lambda data: all(path in data["paths"] for path in ["/api/v1/auditoria", "/api/v1/auditoria/{id}", "/api/v1/auditoria/entidades", "/api/v1/auditoria/resumen"]) and all(list(data["paths"][path]) == ["get"] for path in ["/api/v1/auditoria", "/api/v1/auditoria/{id}", "/api/v1/auditoria/entidades", "/api/v1/auditoria/resumen"]),
    "auditoria-clientes": lambda data: data["total"] == 3 and [item["accion"] for item in data["items"]] == ["Baja", "Modificacion", "Alta"] and all(item["entidad"] == "Cliente" and item["usuario"] == "smoke" for item in data["items"]) and [item["camposCambiados"] for item in data["items"]] == [4, 2, 4],
    "auditoria-alta": lambda data: data["entidad"] == "Cliente" and data["accion"] == "Alta" and data["entidadId"] == str(data["cambios"][0]["despues"]) and all(campo["antes"] is None for campo in data["cambios"]) and {campo["campo"]: campo["despues"] for campo in data["cambios"]} == {"Id": data["entidadId"], "Nombre": "Cliente Smoke", "Ruc": "20987654321", "Descripcion": "Alta de smoke"},
    "auditoria-modificacion": lambda data: data["entidad"] == "Cliente" and data["accion"] == "Modificacion" and {campo["campo"]: (campo["antes"], campo["despues"]) for campo in data["cambios"]} == {"Nombre": ("Cliente Smoke", "Cliente Smoke Editado"), "Descripcion": ("Alta de smoke", "Actualización de smoke")},
    "auditoria-baja": lambda data: data["entidad"] == "Cliente" and data["accion"] == "Baja" and {campo["campo"]: (campo["antes"], campo["despues"]) for campo in data["cambios"]} == {"Id": (data["entidadId"], None), "Nombre": ("Cliente Smoke Editado", None), "Ruc": ("20987654321", None), "Descripcion": ("Actualización de smoke", None)},
    "auditoria-solicitud": lambda data: data["total"] >= 3 and sorted(item["entidad"] for item in data["items"]).count("SolicitudCompra") >= 1 and sorted(item["entidad"] for item in data["items"]).count("DetalleSolicitudCompra") == 2,
    "auditoria-summary": lambda data: data["total"] >= 3 and data["altas"] > 0 and data["modificaciones"] > 0 and data["bajas"] > 0 and len(data["ultimos7Dias"]) == 7,
    "auditoria-missing": lambda data: data["StatusCode"] == 404 and data["Message"] == "El registro de auditoría 99999999 no existe." and "Detail" not in data,
    "usuarios-list": lambda data: data["total"] == 1 and data["items"][0]["nombreUsuario"] == "ana.ventas" and data["items"][0]["roles"] == ["VENTAS"],
    "usuario-detail": lambda data: data["nombreUsuario"] == "luis.compras" and data["roles"] == ["COMPRAS", "CONSULTA"] and data["activo"] is True,
    "usuario-permisos": lambda data: len(data) == 80 and data[0]["permiso"] == "Lectura" and data[0]["otorgadoPor"] == ["CONSULTA"] and data[10]["permiso"] == "Escritura" and data[10]["otorgadoPor"] == ["COMPRAS"],
    "usuarios-summary": lambda data: data["total"] == 5 and data["activos"] == 4 and data["inactivos"] == 1 and data["sinRol"] == 1 and [(item["codigo"], item["usuarios"]) for item in data["porRol"]] == [("ADMIN", 1), ("ALMACEN", 1), ("COMPRAS", 1), ("CONSULTA", 1), ("VENTAS", 1)],
    "usuario-created": lambda data: data["id"] > 0,
    "usuario-duplicate-email": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe un usuario con el email admin@titanglobal.example." and "Detail" not in data,
    "usuario-last-admin": lambda data: data["StatusCode"] == 409 and data["Message"] == "Debe quedar al menos un usuario activo con el rol ADMIN." and "Detail" not in data,
    "usuario-role-protected": lambda data: data["StatusCode"] == 409 and data["Message"] == "No se puede eliminar el rol VENTAS porque tiene usuarios." and "Detail" not in data,
    "roles-list": lambda data: data["total"] == 5 and [item["codigo"] for item in data["items"]] == ["ADMIN", "ALMACEN", "COMPRAS", "CONSULTA", "VENTAS"],
    "role-detail": lambda data: data["codigo"] == "VENTAS" and data["modulosConLectura"] == 11 and data["modulosConEscritura"] == 10,
    "role-permissions": lambda data: len(data) == 80 and data[20]["puedeLeer"] is True and data[20]["puedeEscribir"] is False,
    "roles-matrix": lambda data: len(data["roles"]) == 5 and len(data["modulos"]) == 80 and data["modulos"][0]["permisos"] == ["Escritura", "Ninguno", "Ninguno", "Lectura", "Escritura"],
    "role-created": lambda data: data["id"] > 0,
    "role-invalid-write": lambda data: data["StatusCode"] == 400 and data["Message"] == "El módulo 1 no puede tener escritura sin lectura." and "Detail" not in data,
    "role-system-delete": lambda data: data["StatusCode"] == 409 and data["Message"] == "El rol ADMIN es de sistema y no se puede modificar de esa forma." and "Detail" not in data,
    "openapi-comisiones": lambda data: all(path in data["paths"] for path in ["/api/v1/comisiones-vendedores/vendedores", "/api/v1/comisiones-vendedores/vendedores/{id}", "/api/v1/comisiones-vendedores/ordenes", "/api/v1/comisiones-vendedores/ordenes/{ordenCompraId}/vendedor", "/api/v1/comisiones-vendedores/comisiones", "/api/v1/comisiones-vendedores/liquidaciones", "/api/v1/comisiones-vendedores/liquidaciones/{id}/pagar"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/comisiones-vendedores/vendedores", "get"), ("/api/v1/comisiones-vendedores/vendedores", "post"), ("/api/v1/comisiones-vendedores/vendedores/{id}", "get"), ("/api/v1/comisiones-vendedores/vendedores/{id}", "put"), ("/api/v1/comisiones-vendedores/vendedores/{id}", "delete"), ("/api/v1/comisiones-vendedores/ordenes", "get"), ("/api/v1/comisiones-vendedores/ordenes/{ordenCompraId}/vendedor", "put"), ("/api/v1/comisiones-vendedores/ordenes/{ordenCompraId}/vendedor", "delete"), ("/api/v1/comisiones-vendedores/comisiones", "get"), ("/api/v1/comisiones-vendedores/liquidaciones", "post"), ("/api/v1/comisiones-vendedores/liquidaciones/{id}/pagar", "post")]),
    "comisiones-vendedores": lambda data: data["total"] == 3 and [item["codigo"] for item in data["items"]] == ["V-ANA", "V-LUIS", "V-OLD"],
    "comisiones-sin-asignar": lambda data: data["total"] == len(data["items"]) and all(item["vendedorId"] is None for item in data["items"]) and any(item["numeroOrden"] == "OC-007" for item in data["items"]),
    "comisiones-enero": lambda data: data["liquidado"] and data["totalVentas"] == 2500.0 and data["totalComision"] == 115.0 and [fila["estado"] for fila in data["filas"]] == ["Pagada", "Pagada"],
    "comisiones-marzo-pendiente": lambda data: not data["liquidado"] and data["totalVentas"] == 4000.0 and data["totalComision"] == 120.0 and data["filas"][0]["estado"] == "Pendiente",
    "comision-asignada": lambda data: data["ordenCompraId"] == 4 and data["vendedorCodigo"] == "V-ANA",
    "comision-locked": lambda data: data["StatusCode"] == 409 and data["Message"] == "El mes 01/2026 ya está liquidado; no se pueden cambiar sus asignaciones.",
    "comisiones-marzo-liquidada": lambda data: data["liquidado"] and data["totalVentas"] == 4000.0 and data["totalComision"] == 200.0 and data["filas"][0]["estado"] == "Calculada" and data["filas"][0]["liquidacionId"] > 0,
    "comisiones-marzo-duplicada": lambda data: data["StatusCode"] == 409 and data["Message"] == "El mes 03/2026 ya está liquidado.",
    "comision-pagada": lambda data: data["estado"] == "Pagada" and data["fechaPago"] is not None,
    "comision-delete-protected": lambda data: data["StatusCode"] == 409 and data["Message"] == "No se puede eliminar el vendedor V-ANA porque tiene ventas asignadas.",
    "alertas-seed": lambda data: data["total"] >= 3 and {item["tipo"] for item in data["items"]}.issuperset({"ReclamoVencido", "OrdenAbastecimientoAtrasada", "AlmacenCasiLleno"}) and any(item["clave"] == "almacen-lleno:1" and item["mensaje"] == "El almacén ALM-LIM-01 tiene asignado el 90.00 % de su capacidad." for item in data["items"]),
    "alertas-altas": lambda data: data["total"] >= 2 and all(item["severidad"] == "Alta" for item in data["items"]),
    "alertas-resumen": lambda data: data["total"] >= 3 and data["altas"] >= 2 and data["medias"] >= 1 and data["descartadas"] == 0,
    "alerta-duplicada": lambda data: data["StatusCode"] == 409 and data["Message"] == "La alerta reclamo-vencido:1 ya está descartada." and "Detail" not in data,
    "alerta-inexistente": lambda data: data["StatusCode"] == 404 and data["Message"] == "La alerta reclamo-vencido:999999 no existe o ya no está activa." and "Detail" not in data,
    "alerta-descartada": lambda data: not any(item["clave"] == "reclamo-vencido:1" for item in data["items"]),
    "alerta-restaurada": lambda data: any(item["clave"] == "reclamo-vencido:1" and item["descartada"] is False for item in data["items"]),
    "promo-empty": lambda data: data["StatusCode"] == 400 and data["Message"] == "La compra debe tener al menos una línea." and "Detail" not in data,
    "promo-created": lambda data: data["id"] > 4,
    "promo-duplicate": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe una promoción con el código LAPTOP-10." and "Detail" not in data,
    "promo-product-required": lambda data: data["StatusCode"] == 400 and data["Message"] == "La promoción de producto requiere un producto." and "Detail" not in data,
    "openapi-reclamos": lambda data: all(path in data["paths"] for path in ["/api/v1/reclamos-postventa", "/api/v1/reclamos-postventa/{id}", "/api/v1/reclamos-postventa/{id}/iniciar", "/api/v1/reclamos-postventa/{id}/resolver", "/api/v1/reclamos-postventa/{id}/cerrar", "/api/v1/reclamos-postventa/{id}/rechazar", "/api/v1/reclamos-postventa/resumen"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/reclamos-postventa", "get"), ("/api/v1/reclamos-postventa", "post"), ("/api/v1/reclamos-postventa/{id}", "get"), ("/api/v1/reclamos-postventa/{id}", "put"), ("/api/v1/reclamos-postventa/{id}/iniciar", "post"), ("/api/v1/reclamos-postventa/{id}/resolver", "post"), ("/api/v1/reclamos-postventa/{id}/cerrar", "post"), ("/api/v1/reclamos-postventa/{id}/rechazar", "post"), ("/api/v1/reclamos-postventa/resumen", "get")]),
    "reclamos-list": lambda data: data["total"] == 5 and [item["numero"] for item in data["items"]] == ["REC-DEV-001", "REC-DEV-004", "REC-DEV-003", "REC-DEV-002", "REC-DEV-005"],
    "reclamos-vencidos": lambda data: data["total"] == 1 and [item["numero"] for item in data["items"]] == ["REC-DEV-001"] and data["items"][0]["vencido"] is True,
    "reclamo-detail": lambda data: data["numero"] == "REC-DEV-001" and data["clienteNombre"] == "Acme Industrial" and data["numeroOrden"] == "OC-001" and data["productoNombre"] == "Laptop Pro" and data["estado"] == "Abierto",
    "reclamos-summary": lambda data: data["total"] == 5 and data["abiertos"] == 1 and data["enProceso"] == 1 and data["resueltos"] == 1 and data["cerrados"] == 1 and data["rechazados"] == 1 and data["vencidos"] == 1 and data["horasPromedioResolucion"] == 60.0 and data["cumplimientoPlazo"] == 50.0 and [(item["tipo"], item["reclamos"]) for item in data["porTipo"]] == [("Defecto", 1), ("Retraso", 1), ("Faltante", 1), ("Facturacion", 1), ("Otro", 1)],
    "reclamo-created": lambda data: data["id"] > 5 and data["numero"] == f"REC-{data['id']:06d}",
    "reclamo-started": lambda data: data["estado"] == "EnProceso" and data["fechaInicio"] is not None,
    "reclamo-resolved": lambda data: data["estado"] == "Resuelto" and data["resolucion"] == "Solución aplicada" and data["fechaResolucion"] is not None and data["atendidoATiempo"] is True,
    "reclamo-closed": lambda data: data["estado"] == "Cerrado" and data["fechaCierre"] is not None,
    "reclamo-close-conflict": lambda data: data["StatusCode"] == 409 and re.fullmatch(r"El reclamo REC-\d{6} está cerrado\.", data["Message"]) is not None and "Detail" not in data,
    "reclamo-product-not-order": lambda data: data["StatusCode"] == 400 and data["Message"] == "El producto 2 no está en la orden OC-001." and "Detail" not in data,
    "reclamo-no-rejection-motive": lambda data: data["StatusCode"] == 400 and data["Message"] == "El motivo de rechazo es obligatorio." and "Detail" not in data,
    "openapi-centros-trabajo": lambda data: all(path in data["paths"] for path in ["/api/v1/centros-trabajo", "/api/v1/centros-trabajo/{id}", "/api/v1/centros-trabajo/plantas", "/api/v1/centros-trabajo/resumen"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/centros-trabajo", "get"), ("/api/v1/centros-trabajo", "post"), ("/api/v1/centros-trabajo/{id}", "get"), ("/api/v1/centros-trabajo/{id}", "put"), ("/api/v1/centros-trabajo/{id}", "delete"), ("/api/v1/centros-trabajo/plantas", "get"), ("/api/v1/centros-trabajo/resumen", "get")]),
    "centros-planta": lambda data: data["total"] == 3 and [item["codigo"] for item in data["items"]] == ["CT-LIM-C01", "CT-LIM-L01", "CT-LIM-L02"] and all(item["planta"] == "Planta Lima" for item in data["items"]),
    "centros-tipo": lambda data: data["total"] == 2 and [item["codigo"] for item in data["items"]] == ["CT-ARE-C01", "CT-LIM-C01"] and all(item["tipo"] == "Celda" for item in data["items"]),
    "centros-inactivos": lambda data: data["total"] == 1 and data["items"][0]["codigo"] == "CT-ARE-L01" and data["items"][0]["activo"] is False,
    "centro-detail": lambda data: data["codigo"] == "CT-LIM-C01" and data["nombre"] == "Celda de ensamblaje" and data["tipo"] == "Celda" and data["planta"] == "Planta Lima" and data["capacidadHorasDia"] == 8.0 and data["costoHora"] == 85.5 and data["responsable"] == "Rosa Quispe" and data["activo"] is True,
    "centro-plantas": lambda data: data == ["Planta Arequipa", "Planta Lima"],
    "centros-summary": lambda data: data["totalCentros"] == 5 and data["activos"] == 4 and data["inactivos"] == 1 and data["capacidadTotalHorasDia"] == 68.0 and data["capacidadActivaHorasDia"] == 44.0 and [(item["tipo"], item["totalCentros"], item["capacidadHorasDia"]) for item in data["porTipo"]] == [("Linea", 3, 48.0), ("Celda", 2, 20.0)] and [(item["planta"], item["totalCentros"], item["activos"]) for item in data["porPlanta"]] == [("Planta Lima", 3, 3), ("Planta Arequipa", 2, 1)],
    "centro-created": lambda data: data["id"] > 0,
    "centro-cost-overflow": lambda data: data["StatusCode"] == 400 and data["Message"] == "El costo por hora no puede superar 9999999999999999.99." and "Detail" not in data,
    "centro-capacity-decimals": lambda data: data["StatusCode"] == 400 and data["Message"] == "La capacidad admite como máximo 2 decimales." and "Detail" not in data,
    "centro-duplicate": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe un centro de trabajo con el código CT-LIM-L01." and "Detail" not in data,
}

if validation not in checks or not checks[validation](body):
    raise SystemExit(f"la respuesta no coincide con la validación {validation}")
PY
    then
        rm -f "$response_file"
        return 0
    fi

    rm -f "$response_file"
    return 1
}

request_check() {
    local label="$1"
    local method="$2"
    local path="$3"
    local expected_status="$4"
    local validation="$5"
    local payload="${6:-}"
    local role="${7:-}"
    local usuario="${8:-}"
    local -a cabecera_usuario=()
    local response
    local status
    local body

    if [[ -n "$usuario" ]]; then
        cabecera_usuario=(-H "X-Usuario: $usuario")
    fi

    if [[ -n "$payload" ]]; then
        if ! response="$(curl -sS --max-time 10 -X "$method" -H 'Content-Type: application/json' -H "X-User-Role: $role" "${cabecera_usuario[@]}" -d "$payload" -w $'\n%{http_code}' "$BASE_URL$path")"; then
            printf 'ERROR: %s no pudo conectar con la API\n' "$label" >&2
            return 1
        fi
    elif [[ "$method" == "POST" ]]; then
        if ! response="$(curl -sS --max-time 10 -X POST -H 'Content-Type: application/json' "${cabecera_usuario[@]}" -d '' -w $'\n%{http_code}' "$BASE_URL$path")"; then
            printf 'ERROR: %s no pudo conectar con la API\n' "$label" >&2
            return 1
        fi
    else
        if ! response="$(curl -sS --max-time 10 -X "$method" "${cabecera_usuario[@]}" -w $'\n%{http_code}' "$BASE_URL$path")"; then
            printf 'ERROR: %s no pudo conectar con la API\n' "$label" >&2
            return 1
        fi
    fi

    status="${response##*$'\n'}"
    body="${response%$'\n'*}"
    if [[ "$path" == /openapi/* ]]; then
        printf '%s HTTP %s (OpenAPI validado)\n' "$label" "$status"
    else
        printf '%s HTTP %s %s\n' "$label" "$status" "$body"
    fi

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

    if [[ "$validation" == "auditoria-clientes" ]]; then
        AUDITORIA_ALTA_ID="$(python3 -c 'import json,sys; print(next(item["id"] for item in json.loads(sys.argv[1])["items"] if item["accion"] == "Alta"))' "$body")"
        AUDITORIA_MODIFICACION_ID="$(python3 -c 'import json,sys; print(next(item["id"] for item in json.loads(sys.argv[1])["items"] if item["accion"] == "Modificacion"))' "$body")"
        AUDITORIA_BAJA_ID="$(python3 -c 'import json,sys; print(next(item["id"] for item in json.loads(sys.argv[1])["items"] if item["accion"] == "Baja"))' "$body")"
    fi

    if [[ "$validation" == "product-created" ]]; then
        PRODUCTO_CREADO_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "impuesto-created" ]]; then
        IMPUESTO_CREADO_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "provider-created" ]]; then
        PROVEEDOR_CREADO_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "categoria-producto-created" ]]; then
        CATEGORIA_PRODUCTO_CREADA_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "solicitud-created" ]]; then
        SOLICITUD_CREADA_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "cotizacion-created" ]]; then
        COTIZACION_CREADA_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "contract-created" ]]; then
        CREATED_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "role-created" ]]; then
        CREATED_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
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

    if [[ "$validation" == "lista-created" ]]; then
        LISTA_PRECIOS_CREADA_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "promo-created" ]]; then
        PROMOCION_CREADA_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "reclamo-created" ]]; then
        RECLAMO_CREADO_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "comisiones-marzo-liquidada" ]]; then
        LIQUIDACION_COMISION_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["filas"][0]["liquidacionId"])' "$body")"
    fi

    if [[ "$validation" == "devolucion-created" ]]; then
        DEVOLUCION_CREADA_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "centros-planta" ]]; then
        CENTRO_TRABAJO_SEED_ID="$(python3 -c 'import json,sys; print(next(item["id"] for item in json.loads(sys.argv[1])["items"] if item["codigo"] == "CT-LIM-C01"))' "$body")"
    fi

    if [[ "$validation" == "centro-created" ]]; then
        CENTRO_TRABAJO_CREADO_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
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
request_check 'POST cliente válido' POST '/api/v1/clientes' 200 cliente-created '{"nombre":"Cliente Smoke","ruc":"20987654321","descripcion":"Alta de smoke"}' '' smoke
request_check 'PUT cliente para auditar' PUT "/api/v1/clientes/${CLIENTE_CREADO_ID}" 200 cliente-updated '{"nombre":"Cliente Smoke Editado","ruc":"20987654321","descripcion":"Actualización de smoke"}' '' smoke
request_check 'POST cliente con RUC existente' POST '/api/v1/clientes' 409 cliente-duplicate '{"nombre":"Duplicado Smoke","ruc":"20111111111"}'
request_check 'DELETE cliente con órdenes' DELETE '/api/v1/clientes/1' 409 cliente-delete-protected
request_check 'DELETE cliente recién creado' DELETE "/api/v1/clientes/${CLIENTE_CREADO_ID}" 204 '' '' '' smoke
request_check 'Auditoría de alta, modificación y baja del cliente smoke' GET "/api/v1/auditoria?entidad=Cliente&entidadId=${CLIENTE_CREADO_ID}&usuario=smoke&pagina=1&tamanoPagina=20" 200 auditoria-clientes
request_check 'Detalle de auditoría de alta del cliente smoke' GET "/api/v1/auditoria/${AUDITORIA_ALTA_ID}" 200 auditoria-alta
request_check 'Detalle de auditoría de modificación del cliente smoke' GET "/api/v1/auditoria/${AUDITORIA_MODIFICACION_ID}" 200 auditoria-modificacion
request_check 'Detalle de auditoría de baja del cliente smoke' GET "/api/v1/auditoria/${AUDITORIA_BAJA_ID}" 200 auditoria-baja
request_check 'Resumen de auditoría' GET '/api/v1/auditoria/resumen' 200 auditoria-summary
request_check 'Detalle inexistente de auditoría' GET '/api/v1/auditoria/99999999' 404 auditoria-missing
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
request_check 'Categorías de producto activas' GET '/api/v1/categorias-producto' 200 categorias-producto-list
request_check 'Detalle de la categoría de producto 1' GET '/api/v1/categorias-producto/1' 200 categoria-producto-detail
request_check 'Resumen de categorías de producto' GET '/api/v1/categorias-producto/resumen' 200 categorias-producto-summary
request_check 'POST categoría de producto válida' POST '/api/v1/categorias-producto' 200 categoria-producto-created '{"nombre":"Categoría Smoke","descripcion":"Alta de smoke"}'
request_check 'POST categoría de producto duplicada sin importar mayúsculas' POST '/api/v1/categorias-producto' 409 categoria-producto-duplicate '{"nombre":"computo"}'
request_check 'DELETE categoría de producto recién creada (baja lógica)' DELETE "/api/v1/categorias-producto/${CATEGORIA_PRODUCTO_CREADA_ID}" 204 ''
request_check 'Categoría de producto recién creada queda inactiva' GET "/api/v1/categorias-producto/${CATEGORIA_PRODUCTO_CREADA_ID}" 200 categoria-producto-deactivated
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
request_check 'POST solicitud válida' POST '/api/v1/solicitudes-compra' 200 solicitud-created '{"solicitante":"Solicitud Smoke","proveedorId":1,"observaciones":"Prueba de smoke","lineas":[{"productoId":1,"cantidad":2,"precioEstimado":250},{"productoId":3,"cantidad":1,"precioEstimado":40}]}' '' solicitud-smoke
request_check 'Auditoría atómica de solicitud y dos líneas' GET '/api/v1/auditoria?usuario=solicitud-smoke&pagina=1&tamanoPagina=20' 200 auditoria-solicitud
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

request_check 'OpenAPI de contratos comerciales' GET '/openapi/v1.json' 200 openapi-contracts
request_check 'Contratos de Acme con consumo' GET '/api/v1/contratos-comerciales?clienteId=1' 200 contracts-list
request_check 'Detalle del contrato vigente de Acme' GET '/api/v1/contratos-comerciales/1' 200 contract-detail
request_check 'Resumen de contratos comerciales' GET '/api/v1/contratos-comerciales/resumen' 200 contracts-summary
request_check 'Crear contrato para probar solapamiento' POST '/api/v1/contratos-comerciales' 200 contract-created '{"clienteId":1,"titulo":"Contrato solapado","fechaInicio":"2026-06-01T00:00:00Z","fechaFin":"2026-12-31T00:00:00Z","montoComprometido":1000,"condicionPago":"Credito30"}'
CONTRATO_CREADO_ID="$CREATED_ID"
request_check 'Activar contrato con periodo solapado' POST "/api/v1/contratos-comerciales/${CONTRATO_CREADO_ID}/activar" 409 contract-overlap
request_check 'Activar contrato de Beta' POST '/api/v1/contratos-comerciales/2/activar' 200 contract-activated
request_check 'Crear borrador para probar transiciones' POST '/api/v1/contratos-comerciales' 200 contract-created '{"clienteId":5,"titulo":"Borrador smoke","fechaInicio":"2026-01-01T00:00:00Z","fechaFin":"2026-12-31T00:00:00Z","montoComprometido":1000,"condicionPago":"Contado"}'
CONTRATO_BORRADOR_ID="$CREATED_ID"
request_check 'Finalizar un borrador' POST "/api/v1/contratos-comerciales/${CONTRATO_BORRADOR_ID}/finalizar" 409 contract-finalize-draft
request_check 'Cancelar contrato sin motivo' POST "/api/v1/contratos-comerciales/${CONTRATO_BORRADOR_ID}/cancelar" 400 contract-cancel-reason '{"motivo":" "}'
request_check 'Eliminar contrato vigente' DELETE '/api/v1/contratos-comerciales/1' 409 contract-delete-active
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
request_check 'OpenAPI de listas de precios' GET '/openapi/v1.json' 200 openapi-listas-precios
request_check 'Listas de precios vigentes' GET '/api/v1/listas-precios?vigentes=true' 200 listas-vigentes
request_check 'Detalle de lista VIP-2026' GET '/api/v1/listas-precios/1' 200 lista-vip
request_check 'Precio de Laptop Pro para Acme' GET '/api/v1/listas-precios/precio?productoId=1&clienteId=1&fecha=2026-06-01' 200 precio-vip
request_check 'Precio de Laptop Pro para Beta' GET '/api/v1/listas-precios/precio?productoId=1&clienteId=2&fecha=2026-06-01' 200 precio-general
request_check 'Precio base del Teclado Mecánico' GET '/api/v1/listas-precios/precio?productoId=4&clienteId=1&fecha=2026-06-01' 200 precio-base
request_check 'Resumen de listas de precios' GET '/api/v1/listas-precios/resumen' 200 listas-summary
request_check 'POST lista de precios válida' POST '/api/v1/listas-precios' 200 lista-created '{"codigo":"SMOKE-LP","nombre":"Lista Smoke","prioridad":99,"fechaInicio":"2026-01-01T00:00:00Z","activa":true,"items":[{"productoId":2,"precio":1400}]}'
request_check 'POST lista con prioridad del seed' POST '/api/v1/listas-precios' 409 lista-priority-duplicate '{"codigo":"SMOKE-DUP-LP","nombre":"Duplicada","prioridad":1,"fechaInicio":"2026-01-01T00:00:00Z","activa":true,"items":[{"productoId":2,"precio":1400}]}'
request_check 'DELETE lista recién creada' DELETE "/api/v1/listas-precios/${LISTA_PRECIOS_CREADA_ID}" 204 ''
request_check 'OpenAPI de promociones' GET '/openapi/v1.json' 200 openapi-promociones
request_check 'Listado de promociones' GET '/api/v1/promociones' 200 promos-list
request_check 'Promociones vigentes' GET '/api/v1/promociones?vigentes=true' 200 promos-vigentes
request_check 'Detalle de promoción LAPTOP-10' GET '/api/v1/promociones/1' 200 promo-detail
request_check 'Evaluar compra de Acme' POST '/api/v1/promociones/evaluar' 200 promo-acme '{"clienteId":1,"fecha":"2026-06-01T00:00:00Z","lineas":[{"productoId":1,"cantidad":2,"precioUnitario":2500}]}'
request_check 'Evaluar compra de Beta' POST '/api/v1/promociones/evaluar' 200 promo-beta '{"clienteId":2,"fecha":"2026-06-01T00:00:00Z","lineas":[{"productoId":1,"cantidad":2,"precioUnitario":2500}]}'
request_check 'Evaluar compra sin líneas' POST '/api/v1/promociones/evaluar' 400 promo-empty '{"clienteId":1,"lineas":[]}'
request_check 'POST promoción válida' POST '/api/v1/promociones' 200 promo-created '{"codigo":"PROMO-SMOKE","nombre":"Promoción Smoke","tipo":"Producto","productoId":2,"porcentajeDescuento":5,"fechaInicio":"2026-01-01T00:00:00Z","fechaFin":"2026-12-31T00:00:00Z","activa":true}'
request_check 'POST código de promoción del seed' POST '/api/v1/promociones' 409 promo-duplicate '{"codigo":"laptop-10","nombre":"Duplicada","tipo":"Producto","productoId":1,"porcentajeDescuento":5,"fechaInicio":"2026-01-01T00:00:00Z","fechaFin":"2026-12-31T00:00:00Z","activa":true}'
request_check 'POST promoción de producto sin producto' POST '/api/v1/promociones' 400 promo-product-required '{"codigo":"PROMO-SIN-PRODUCTO","nombre":"Incompleta","tipo":"Producto","porcentajeDescuento":5,"fechaInicio":"2026-01-01T00:00:00Z","fechaFin":"2026-12-31T00:00:00Z","activa":true}'
request_check 'DELETE promoción recién creada' DELETE "/api/v1/promociones/${PROMOCION_CREADA_ID}" 204 ''
request_check 'OpenAPI de reclamos postventa' GET '/openapi/v1.json' 200 openapi-reclamos
request_check 'Listado de reclamos postventa' GET '/api/v1/reclamos-postventa' 200 reclamos-list
request_check 'Reclamos vencidos' GET '/api/v1/reclamos-postventa?vencidos=true' 200 reclamos-vencidos
request_check 'Detalle del reclamo REC-DEV-001' GET '/api/v1/reclamos-postventa/1' 200 reclamo-detail
request_check 'Resumen de reclamos postventa' GET '/api/v1/reclamos-postventa/resumen' 200 reclamos-summary
request_check 'Crear reclamo postventa' POST '/api/v1/reclamos-postventa' 200 reclamo-created '{"clienteId":1,"ordenCompraId":1,"productoId":1,"tipo":"Defecto","prioridad":"Alta","descripcion":"Prueba de smoke para reclamo"}'
request_check 'Iniciar reclamo creado' POST "/api/v1/reclamos-postventa/${RECLAMO_CREADO_ID}/iniciar" 200 reclamo-started
request_check 'Resolver reclamo creado' POST "/api/v1/reclamos-postventa/${RECLAMO_CREADO_ID}/resolver" 200 reclamo-resolved '{"resolucion":"Solución aplicada"}'
request_check 'Cerrar reclamo creado' POST "/api/v1/reclamos-postventa/${RECLAMO_CREADO_ID}/cerrar" 200 reclamo-closed
request_check 'Cerrar reclamo cerrado' POST "/api/v1/reclamos-postventa/${RECLAMO_CREADO_ID}/cerrar" 409 reclamo-close-conflict
request_check 'Crear reclamo con producto ajeno a la orden' POST '/api/v1/reclamos-postventa' 400 reclamo-product-not-order '{"clienteId":1,"ordenCompraId":1,"productoId":2,"tipo":"Defecto","prioridad":"Alta","descripcion":"Producto no perteneciente a la orden"}'
request_check 'Rechazar reclamo sin motivo' POST '/api/v1/reclamos-postventa/1/rechazar' 400 reclamo-no-rejection-motive '{"motivo":" "}'
request_check 'OpenAPI de devoluciones comerciales' GET '/openapi/v1.json' 200 openapi-devoluciones
request_check 'Listado de devoluciones comerciales' GET '/api/v1/devoluciones-comerciales' 200 devoluciones-list
request_check 'Detalle de devolución DEV-DEV-001' GET '/api/v1/devoluciones-comerciales/1' 200 devolucion-detail
request_check 'Productos disponibles para devolver en la orden 8' GET '/api/v1/devoluciones-comerciales/ordenes/8/disponible' 200 devolucion-disponible
request_check 'Resumen de devoluciones comerciales' GET '/api/v1/devoluciones-comerciales/resumen' 200 devoluciones-summary
request_check 'POST que supera lo disponible' POST '/api/v1/devoluciones-comerciales' 409 devolucion-over-available '{"clienteId":4,"ordenCompraId":8,"motivo":"Defecto","lineas":[{"productoId":9,"cantidad":2}]}'
request_check 'POST con producto que no está en la orden' POST '/api/v1/devoluciones-comerciales' 400 devolucion-product-not-order '{"clienteId":4,"ordenCompraId":8,"motivo":"Defecto","lineas":[{"productoId":1,"cantidad":1}]}'
request_check 'Rechazar devolución solicitada sin motivo' POST '/api/v1/devoluciones-comerciales/3/rechazar' 400 devolucion-rejection-reason '{"motivo":" "}'
request_check 'POST devolución válida' POST '/api/v1/devoluciones-comerciales' 200 devolucion-created '{"clienteId":4,"ordenCompraId":8,"motivo":"Defecto","observaciones":"Devolución smoke","lineas":[{"productoId":9,"cantidad":1}]}'
request_check 'Aprobar devolución creada' POST "/api/v1/devoluciones-comerciales/${DEVOLUCION_CREADA_ID}/aprobar" 200 devolucion-approved
request_check 'Recibir devolución aprobada' POST "/api/v1/devoluciones-comerciales/${DEVOLUCION_CREADA_ID}/recibir" 200 devolucion-received
request_check 'Recibir devolución recibida' POST '/api/v1/devoluciones-comerciales/1/recibir' 409 devolucion-already-received
request_check 'OpenAPI de alertas' GET '/openapi/v1.json' 200 openapi-alertas
request_check 'Alertas del seed y almacén de Lima al 90 %' GET '/api/v1/alertas?incluirDescartadas=true&tamanoPagina=100' 200 alertas-seed
request_check 'Filtrar alertas de severidad alta' GET '/api/v1/alertas?severidad=Alta&tamanoPagina=100' 200 alertas-altas
request_check 'Resumen de alertas' GET '/api/v1/alertas/resumen' 200 alertas-resumen
request_check 'Descartar reclamo vencido del seed' POST '/api/v1/alertas/descartar' 204 '' '{"clave":"reclamo-vencido:1"}'
request_check 'Alerta descartada fuera del listado por defecto' GET '/api/v1/alertas' 200 alerta-descartada
request_check 'Descartar alerta otra vez' POST '/api/v1/alertas/descartar' 409 alerta-duplicada '{"clave":"reclamo-vencido:1"}'
request_check 'Restaurar reclamo vencido del seed' POST '/api/v1/alertas/restaurar' 204 '' '{"clave":"reclamo-vencido:1"}'
request_check 'Alerta restaurada reaparece' GET '/api/v1/alertas?incluirDescartadas=true' 200 alerta-restaurada
request_check 'Descartar clave inexistente' POST '/api/v1/alertas/descartar' 404 alerta-inexistente '{"clave":"reclamo-vencido:999999"}'
request_check 'OpenAPI de comisiones de vendedores' GET '/openapi/v1.json' 200 openapi-comisiones
request_check 'Vendedores del seed' GET '/api/v1/comisiones-vendedores/vendedores' 200 comisiones-vendedores
request_check 'Órdenes de compra sin vendedor asignado' GET '/api/v1/comisiones-vendedores/ordenes?sinAsignar=true' 200 comisiones-sin-asignar
request_check 'Comisiones de enero pagadas' GET '/api/v1/comisiones-vendedores/comisiones?anio=2026&mes=1' 200 comisiones-enero
request_check 'Comisiones pendientes de marzo' GET '/api/v1/comisiones-vendedores/comisiones?anio=2026&mes=3' 200 comisiones-marzo-pendiente
request_check 'Reasignar orden de marzo a V-ANA' PUT '/api/v1/comisiones-vendedores/ordenes/4/vendedor' 200 comision-asignada '{"vendedorId":1}'
request_check 'No modificar asignación del mes de enero liquidado' PUT '/api/v1/comisiones-vendedores/ordenes/1/vendedor' 409 comision-locked '{"vendedorId":1}'
request_check 'Liquidar marzo' POST '/api/v1/comisiones-vendedores/liquidaciones' 200 comisiones-marzo-liquidada '{"anio":2026,"mes":3}'
request_check 'Liquidar marzo por segunda vez' POST '/api/v1/comisiones-vendedores/liquidaciones' 409 comisiones-marzo-duplicada '{"anio":2026,"mes":3}'
request_check 'Pagar liquidación de marzo' POST "/api/v1/comisiones-vendedores/liquidaciones/${LIQUIDACION_COMISION_ID}/pagar" 200 comision-pagada
request_check 'Proteger vendedor con órdenes asignadas' DELETE '/api/v1/comisiones-vendedores/vendedores/1' 409 comision-delete-protected
request_check 'OpenAPI de roles y permisos' GET '/openapi/v1.json' 200 openapi-roles
request_check 'Listado de roles' GET '/api/v1/roles' 200 roles-list
request_check 'Detalle del rol VENTAS' GET '/api/v1/roles/2' 200 role-detail
request_check 'Permisos de VENTAS (80 módulos)' GET '/api/v1/roles/2/permisos' 200 role-permissions
request_check 'Matriz de roles y permisos' GET '/api/v1/roles/matriz' 200 roles-matrix
request_check 'Crear rol smoke' POST '/api/v1/roles' 200 role-created '{"codigo":"SMOKE-ROL","nombre":"Rol smoke"}'
ROL_CREADO_ID="$CREATED_ID"
request_check 'Impedir escritura sin lectura' PUT "/api/v1/roles/${ROL_CREADO_ID}/permisos" 400 role-invalid-write '{"permisos":[{"moduloNumero":1,"puedeLeer":false,"puedeEscribir":true}]}'
request_check 'Proteger borrado de ADMIN' DELETE '/api/v1/roles/1' 409 role-system-delete
request_check 'Eliminar rol smoke' DELETE "/api/v1/roles/${ROL_CREADO_ID}" 204 ''
request_check 'OpenAPI de usuarios' GET '/openapi/v1.json' 200 openapi-usuarios
request_check 'Usuarios con rol VENTAS' GET '/api/v1/usuarios?rolId=2' 200 usuarios-list
request_check 'Detalle de luis.compras' GET '/api/v1/usuarios/3' 200 usuario-detail
request_check 'Permisos efectivos de luis.compras' GET '/api/v1/usuarios/3/permisos' 200 usuario-permisos
request_check 'Resumen de usuarios' GET '/api/v1/usuarios/resumen' 200 usuarios-summary
request_check 'Crear usuario smoke' POST '/api/v1/usuarios' 200 usuario-created '{"nombreUsuario":"smoke.usuario","nombreCompleto":"Usuario Smoke","email":"smoke.usuario@titanglobal.example","activo":true,"rolIds":[]}'
USUARIO_CREADO_ID="$CREATED_ID"
request_check 'Rechazar correo de usuario duplicado' POST '/api/v1/usuarios' 409 usuario-duplicate-email '{"nombreUsuario":"admin.copia","nombreCompleto":"Copia Admin","email":"admin@titanglobal.example","activo":true,"rolIds":[]}'
request_check 'Proteger último ADMIN activo' PUT '/api/v1/usuarios/1' 409 usuario-last-admin '{"nombreUsuario":"admin","nombreCompleto":"Administrador del sistema","email":"admin@titanglobal.example","activo":false,"rolIds":[1]}'
request_check 'Proteger rol VENTAS asignado' DELETE '/api/v1/roles/2' 409 usuario-role-protected
request_check 'Eliminar usuario smoke' DELETE "/api/v1/usuarios/${USUARIO_CREADO_ID}" 204 ''
request_check 'OpenAPI de impuestos y tasas' GET '/openapi/v1.json' 200 openapi-impuestos
request_check 'Listado de impuestos del seed' GET '/api/v1/impuestos' 200 impuestos-list
request_check 'Detalle del IGV' GET '/api/v1/impuestos/1' 200 impuesto-detail
request_check 'POST impuesto válido' POST '/api/v1/impuestos' 201 impuesto-created '{"codigo":"SMOKE-IMP","nombre":"Impuesto smoke","tipo":"IVA","pais":"PER","activo":true}'
request_check 'POST impuesto con código del seed' POST '/api/v1/impuestos' 409 impuesto-duplicate '{"codigo":"igv","nombre":"IGV duplicado","tipo":"IGV","pais":"PER","activo":true}'
request_check 'POST tasa con fechas sin zona' POST "/api/v1/impuestos/${IMPUESTO_CREADO_ID}/tasas" 201 tasa-created '{"porcentaje":5,"fechaInicio":"2026-01-01","fechaFin":"2026-12-31"}'
request_check 'POST tasa sin fecha final y con fecha UTC Z' POST "/api/v1/impuestos/${IMPUESTO_CREADO_ID}/tasas" 201 tasa-created '{"porcentaje":10,"fechaInicio":"2027-01-01T00:00:00Z"}'
request_check 'POST tasa que se solapa con la abierta' POST "/api/v1/impuestos/${IMPUESTO_CREADO_ID}/tasas" 400 tasa-overlap '{"porcentaje":12,"fechaInicio":"2027-06-01"}'
request_check 'Detalle del impuesto smoke con fechas UTC' GET "/api/v1/impuestos/${IMPUESTO_CREADO_ID}" 200 impuesto-smoke-detail
request_check 'Calcular IGV con fecha sin zona y redondeo' POST '/api/v1/impuestos/calcular' 200 impuesto-calculo '{"impuestoId":1,"baseImponible":100.125,"fechaOperacion":"2026-05-10"}'
request_check 'Calcular con la tasa sin fecha final' POST '/api/v1/impuestos/calcular' 200 impuesto-calculo-abierto "{\"impuestoId\":${IMPUESTO_CREADO_ID},\"baseImponible\":200,\"fechaOperacion\":\"2028-03-01T00:00:00Z\"}"
request_check 'DELETE impuesto smoke (desactivar)' DELETE "/api/v1/impuestos/${IMPUESTO_CREADO_ID}" 204 ''
request_check 'Calcular con impuesto desactivado' POST '/api/v1/impuestos/calcular' 404 impuesto-inactive "{\"impuestoId\":${IMPUESTO_CREADO_ID},\"baseImponible\":200,\"fechaOperacion\":\"2028-03-01\"}"
request_check 'OpenAPI de auditoría transaccional' GET '/openapi/v1.json' 200 openapi-auditoria
request_check 'OpenAPI de centros de trabajo' GET '/openapi/v1.json' 200 openapi-centros-trabajo
request_check 'Centros de trabajo de planta lima' GET '/api/v1/centros-trabajo?planta=planta%20lima' 200 centros-planta
request_check 'Centros de trabajo tipo celda' GET '/api/v1/centros-trabajo?tipo=celda' 200 centros-tipo
request_check 'Centros de trabajo inactivos' GET '/api/v1/centros-trabajo?activo=false' 200 centros-inactivos
request_check 'Detalle del centro CT-LIM-C01' GET "/api/v1/centros-trabajo/${CENTRO_TRABAJO_SEED_ID}" 200 centro-detail
request_check 'Plantas de centros de trabajo' GET '/api/v1/centros-trabajo/plantas' 200 centro-plantas
request_check 'Resumen de centros de trabajo' GET '/api/v1/centros-trabajo/resumen' 200 centros-summary
request_check 'POST centro de trabajo válido' POST '/api/v1/centros-trabajo' 200 centro-created '{"codigo":"CT-SMK-01","nombre":"Centro Smoke","tipo":"Celda","planta":"Planta Smoke","capacidadHorasDia":8,"costoHora":10.5,"responsable":"QA","activo":true}'
request_check 'POST centro de trabajo con código del seed' POST '/api/v1/centros-trabajo' 409 centro-duplicate '{"codigo":"CT-LIM-L01","nombre":"Duplicado","tipo":"Linea","planta":"Planta Lima","capacidadHorasDia":8,"costoHora":10}'
request_check 'POST centro de trabajo con costo fuera de rango' POST '/api/v1/centros-trabajo' 400 centro-cost-overflow '{"codigo":"CT-SMK-02","nombre":"Costo excedido","tipo":"Celda","planta":"Planta Smoke","capacidadHorasDia":8,"costoHora":100000000000000000}'
request_check 'POST centro de trabajo con capacidad de tres decimales' POST '/api/v1/centros-trabajo' 400 centro-capacity-decimals '{"codigo":"CT-SMK-03","nombre":"Capacidad con decimales","tipo":"Celda","planta":"Planta Smoke","capacidadHorasDia":0.001,"costoHora":10}'
request_check 'DELETE centro de trabajo recién creado' DELETE "/api/v1/centros-trabajo/${CENTRO_TRABAJO_CREADO_ID}" 204 ''
CENTRO_TRABAJO_CREADO_ID=""
printf 'SMOKE OK\n'
