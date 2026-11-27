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
VEHICULO_CREADO_ID=""
CONDUCTOR_CREADO_ID=""
CONDUCTOR_SIN_ZONA_ID=""
EMPLEADO_CREADO_ID=""
CONTRATO_LABORAL_EMPLEADO_ID=""
CONTRATO_LABORAL_CREADO_ID=""
CONTRATO_LABORAL_RENOVADO_ID=""
EVALUACION_CREADA_ID=""
TURNO_CREADO_ID=""
ASIGNACION_TURNO_CREADA_ID=""
RUTA_PRODUCCION_CREADA_ID=""
LICITACION_CREADA_ID=""
OFERTA_LICITACION_CREADA_ID=""
LICITACION_BORRABLE_ID=""

cleanup() {
    local exit_status=$?
    trap - EXIT INT TERM

    if [[ -n "$API_PID" ]]; then
        if [[ -n "$USUARIO_CREADO_ID" ]]; then
            curl -sS --max-time 3 -H "Authorization: Bearer ${TOKEN_ACTUAL:-}" -X DELETE "$BASE_URL/api/v1/usuarios/${USUARIO_CREADO_ID}" >/dev/null 2>&1 || true
        fi
        if [[ -n "$ROL_CREADO_ID" ]]; then
            curl -sS --max-time 3 -H "Authorization: Bearer ${TOKEN_ACTUAL:-}" -X DELETE "$BASE_URL/api/v1/roles/${ROL_CREADO_ID}" >/dev/null 2>&1 || true
        fi
        if [[ -n "$PROMOCION_CREADA_ID" ]]; then
            curl -sS --max-time 3 -H "Authorization: Bearer ${TOKEN_ACTUAL:-}" -X DELETE "$BASE_URL/api/v1/promociones/${PROMOCION_CREADA_ID}" >/dev/null 2>&1 || true
        fi
        if [[ -n "$LISTA_PRECIOS_CREADA_ID" ]]; then
            curl -sS --max-time 3 -H "Authorization: Bearer ${TOKEN_ACTUAL:-}" -X DELETE "$BASE_URL/api/v1/listas-precios/${LISTA_PRECIOS_CREADA_ID}" >/dev/null 2>&1 || true
        fi
        if [[ -n "$CENTRO_TRABAJO_CREADO_ID" ]]; then
            curl -sS --max-time 3 -H "Authorization: Bearer ${TOKEN_ACTUAL:-}" -X DELETE "$BASE_URL/api/v1/centros-trabajo/${CENTRO_TRABAJO_CREADO_ID}" >/dev/null 2>&1 || true
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

# La base se crea desde database/ y el código generado tiene que coincidir con ella (database first).
./scripts/aplicar-bd.sh --reiniciar
./scripts/scaffold.sh --comprobar
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
    "m48-created": lambda d: isinstance(d.get("id"), int) and d["id"] > 0,
    "m48-flete-created": lambda d: isinstance(d.get("id"), int) and d["id"] > 0,
    "m48-list": lambda d: d["total"] == 6 and len(d["items"]) == 6,
    "m48-inactivos": lambda d: d["total"] == 1 and d["items"][0]["activo"] is False,
    "m48-resumen": lambda d: d["totalTransportistas"] == 6 and d["activos"] == 5 and d["inactivos"] == 1 and sum(t["totalTransportistas"] for t in d["porTipoServicio"]) == 6,
    "m48-fletes": lambda d: d["total"] == 4 and all(isinstance(f["importe"], str) for f in d["items"]) and set(f["moneda"] for f in d["items"]) == {"PEN", "USD"},
    "m48-flete-detail": lambda d: d["referencia"] == "SMOKE-48" and d["importe"] == "9999999999999999.99" and d["moneda"] == "USD" and d["fecha"] == "2026-11-21T00:00:00Z" and d["estado"] == "Registrado",
    "m48-cancelado": lambda d: d["estado"] == "Cancelado" and d["motivoCancelacion"] == "Ruta suspendida" and d["fechaCancelacion"].endswith("Z"),
    "m48-filtro": lambda d: d["total"] == 1 and d["items"][0]["referencia"] == "SMOKE-48" and d["items"][0]["estado"] == "Cancelado",
    "m48-inactivo-detail": lambda d: d["activo"] is False and d["tipoServicio"] == "Ambos" and d["contacto"] is None,
    "m48-duplicate": lambda d: d["StatusCode"] == 409 and d["Message"] == "Ya existe un transportista con el documento fiscal smoke-48 en perú.",
    "m48-historial": lambda d: d["StatusCode"] == 409 and d["Message"] == "No se puede eliminar un transportista con fletes registrados.",
    "m48-inactivo": lambda d: d["StatusCode"] == 409 and d["Message"] == "No se pueden registrar fletes para un transportista inactivo.",
    "m48-referencia": lambda d: d["StatusCode"] == 409 and d["Message"] == "Ya existe un flete con la referencia smoke-48 para este transportista.",
    "m48-decimales": lambda d: d["StatusCode"] == 400 and d["Message"] == "El importe no puede tener más de 2 decimales.",
    "m48-motivo": lambda d: d["StatusCode"] == 400 and d["Message"] == "El motivo de cancelación es obligatorio.",
    "m48-repetida": lambda d: d["StatusCode"] == 409 and re.fullmatch(r"El flete [0-9]+ ya está cancelado\.", d["Message"]) is not None,
    "m48-missing": lambda d: d["StatusCode"] == 404 and d["Message"] == "El transportista 999999 no existe.",
    "m48-openapi": lambda d: all(verb in d["paths"][path] for path, verb in [("/api/v1/transportistas-terceros", "get"), ("/api/v1/transportistas-terceros", "post"), ("/api/v1/transportistas-terceros/{id}", "get"), ("/api/v1/transportistas-terceros/{id}", "put"), ("/api/v1/transportistas-terceros/{id}", "delete"), ("/api/v1/transportistas-terceros/resumen", "get"), ("/api/v1/fletes-terceros", "get"), ("/api/v1/fletes-terceros", "post"), ("/api/v1/fletes-terceros/{id}", "get"), ("/api/v1/fletes-terceros/{id}/cancelacion", "post")]) and "delete" not in d["paths"]["/api/v1/fletes-terceros/{id}"] and "put" not in d["paths"]["/api/v1/fletes-terceros/{id}"],
    "m43-created": lambda d: isinstance(d.get("id"), int) and d["id"] > 0,
    "m43-list": lambda d: d["total"] == 6 and len(d["items"]) == 6 and all(isinstance(r["distanciaKm"], str) and isinstance(r["peajeEstimado"], str) for r in d["items"]),
    "m43-inactivas": lambda d: d["total"] == 1 and d["items"][0]["codigo"] == "RTA-43005" and d["items"][0]["activo"] is False,
    "m43-zonas": lambda d: d == ["Centro", "Norte", "Sur"],
    "m43-resumen": lambda d: d["totalRutas"] == 6 and d["activas"] == 5 and d["inactivas"] == 1 and len(d["porZona"]) == 3 and all(z["totalRutas"] == 2 for z in d["porZona"]),
    "m43-filtro": lambda d: d["total"] == 1 and d["items"][0]["codigo"] == "RTA-43004" and d["items"][0]["moneda"] == "USD",
    "m43-maximos": lambda d: d["codigo"] == "SMK-43" and d["distanciaKm"] == "99999999.99" and d["peajeEstimado"] == "9999999999999999.99" and d["moneda"] == "USD" and d["activo"] is False and d["observaciones"] is None,
    "m43-cero": lambda d: d["distanciaKm"] == "25.50" and d["peajeEstimado"] == "0.00" and d["activo"] is True and d["observaciones"] == "Editada",
    "m43-duplicate": lambda d: d["StatusCode"] == 409 and d["Message"] == "Ya existe una ruta de despacho con el código smk-43." and "Detail" not in d,
    "m43-distancia": lambda d: d["StatusCode"] == 400 and d["Message"] == "La distancia debe ser mayor que cero.",
    "m43-distancia-decimales": lambda d: d["StatusCode"] == 400 and d["Message"] == "La distancia no puede tener más de 2 decimales.",
    "m43-peaje-decimales": lambda d: d["StatusCode"] == 400 and d["Message"] == "El peaje estimado no puede tener más de 2 decimales.",
    "m43-peaje-negativo": lambda d: d["StatusCode"] == 400 and d["Message"] == "El peaje estimado no puede ser negativo.",
    "m43-missing": lambda d: d["StatusCode"] == 404 and re.fullmatch(r"La ruta de despacho [0-9]+ no existe\.", d["Message"]) is not None,
    "m43-vacia": lambda d: d["total"] == 6 and d["items"] == [] and d["pagina"] == 2147483647,
    "m43-openapi": lambda d: all(verb in d["paths"]["/api/v1/rutas-despacho" + path] for path, verb in [("", "get"), ("", "post"), ("/{id}", "get"), ("/{id}", "put"), ("/{id}", "delete"), ("/zonas", "get"), ("/resumen", "get")]),
    'm44-created': lambda d: isinstance(d.get("id"),int) and d["id"] > 0,
    'm44-baja-created': lambda d: isinstance(d.get("id"),int) and d["id"] > 0,
    'm44-list': lambda d: d["total"] == 3 and len(d["items"]) == 3 and all(r["volumenM3"] == "0.180000" and r["pesoKg"] == "6.00" for r in d["items"]),
    'm44-resumen': lambda d: d == {"total":3,"borradores":1,"confirmadas":1,"canceladas":1},
    'm44-maximos': lambda d: d["volumenM3"] == "999997000002.999999" and d["pesoKg"] == "999999990000.00" and d["totalBultos"] == 1000000 and d["lineas"][0]["largoCm"] == "9999.99",
    'm44-picking': lambda d: d["StatusCode"] == 409 and d["Message"] == "Completa el picking de todas las líneas antes de confirmar.",
    'm44-packing': lambda d: d["StatusCode"] == 409 and d["Message"] == "Completa el packing de todas las líneas antes de confirmar.",
    'm44-confirmada': lambda d: d["estado"] == "Confirmada" and d["fechaConfirmacion"].endswith("Z") and d["totalBultos"] == 3 and d["volumenM3"] == "0.180000" and d["pesoKg"] == "6.00" and len(d["lineas"]) == 1,
    'm44-cancelada': lambda d: d["estado"] == "Cancelada" and d["fechaConfirmacion"].endswith("Z") and d["fechaCancelacion"].endswith("Z") and d["motivoCancelacion"] == "Cambio de plan" and len(d["lineas"]) == 1,
    'm44-duplicado': lambda d: d["StatusCode"] == 409 and d["Message"] == "Ya existe una consolidación con el código smk-44.",
    'm44-edicion': lambda d: d["StatusCode"] == 409 and d["Message"] == "Solo se puede editar una consolidación en borrador.",
    'm44-baja': lambda d: d["StatusCode"] == 409 and d["Message"] == "Solo se puede eliminar una consolidación en borrador.",
    'm44-confirmacion': lambda d: d["StatusCode"] == 409 and d["Message"] == "Solo se puede confirmar una consolidación en borrador.",
    'm44-motivo': lambda d: d["StatusCode"] == 400 and d["Message"] == "El motivo de cancelación es obligatorio.",
    'm44-repetida': lambda d: d["StatusCode"] == 409 and d["Message"] == "La consolidación ya está cancelada.",
    'm44-decimales': lambda d: d["StatusCode"] == 400 and d["Message"] == "Línea 1: el largo no puede tener más de 2 decimales.",
    'm44-filtro': lambda d: d["total"] == 2 and all(c["estado"] == "Cancelada" for c in d["items"]),
    'm44-missing': lambda d: d["StatusCode"] == 404 and re.fullmatch(r"La consolidación [0-9]+ no existe\.",d["Message"]) is not None,
    'm44-openapi': lambda d: all(verb in d["paths"]["/api/v1/consolidaciones-carga"+path] for path,verb in [("","get"),("","post"),("/{id}","get"),("/{id}","put"),("/{id}","delete"),("/{id}/confirmacion","post"),("/{id}/cancelacion","post"),("/resumen","get")]),
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
    "login-ok": lambda data: len(data["token"]) > 20 and data["usuario"]["nombreUsuario"] == "admin" and data["roles"] == ["ADMIN"] and "contrasenaHash" not in json.dumps(data),
    "login-invalid": lambda data: data["StatusCode"] == 401 and data["Message"] == "Usuario o contraseña incorrectos." and "Detail" not in data,
    "no-session": lambda data: data["StatusCode"] == 401 and data["Message"] == "Debe iniciar sesión para acceder a este recurso." and "Detail" not in data,
    "perfil-admin": lambda data: data["nombreUsuario"] == "admin" and data["roles"] == ["ADMIN"],
    "login-invitado": lambda data: len(data["token"]) > 20 and data["usuario"]["nombreUsuario"] == "invitado" and data["roles"] == [],
    "perfil-invitado": lambda data: data["nombreUsuario"] == "invitado" and data["roles"] == [],
    "forbidden-admin": lambda data: data["StatusCode"] == 403 and data["Message"] == "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación." and "Detail" not in data,
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
    "evaluaciones-proveedor-1": lambda data: data["total"] == 2 and [item["id"] for item in data["items"]] == [2, 1] and all(item["proveedorId"] == 1 for item in data["items"]),
    "evaluaciones-excelente": lambda data: data["total"] == 2 and [item["id"] for item in data["items"]] == [2, 6] and all(item["calificacion"] == "Excelente" for item in data["items"]),
    "evaluacion-detalle": lambda data: data["id"] == 2 and data["proveedorId"] == 1 and data["periodo"] == "2026-10" and data["puntajeFinal"] == 92.6 and data["calificacion"] == "Excelente",
    "evaluaciones-resumen": lambda data: data["totalEvaluaciones"] == 6 and data["proveedoresEvaluados"] == 4 and data["promedioGeneral"] == 78.72 and [(item["calificacion"], item["total"]) for item in data["porCalificacion"]] == [("Excelente", 2), ("Bueno", 2), ("Regular", 1), ("Deficiente", 1)] and [(item["proveedorId"], item["promedio"]) for item in data["ranking"]] == [(6, 95.5), (1, 89.05), (4, 66.6), (2, 65.5)],
    "evaluacion-created": lambda data: data["id"] > 0,
    "evaluacion-duplicate": lambda data: data["StatusCode"] == 409 and data["Message"] == "El proveedor 1 ya tiene una evaluación del periodo 2026-10.",
    "licitaciones-abiertas": lambda data: data["total"] == 2 and [item["id"] for item in data["items"]] == [2, 1] and all(item["estado"] == "Abierta" for item in data["items"]),
    "licitacion-detalle": lambda data: data["id"] == 3 and data["numero"] == "LIC-2026-003" and data["estado"] == "Adjudicada" and data["proveedorAdjudicadoId"] == 1 and data["montoAdjudicado"] == 28000.0 and [(oferta["proveedorId"], oferta["esGanadora"]) for oferta in data["ofertas"]] == [(1, True), (4, False)],
    "licitaciones-resumen": lambda data: data["totalLicitaciones"] == 5 and data["totalOfertas"] == 7 and data["presupuestoAbierto"] == 70000.0 and data["montoAdjudicado"] == 28000.0 and [(item["estado"], item["total"]) for item in data["porEstado"]] == [("Abierta", 2), ("Cerrada", 1), ("Adjudicada", 1), ("Cancelada", 1)],
    "licitacion-created": lambda data: data["id"] > 0,
    "oferta-licitacion-created": lambda data: data["id"] > 0,
    "licitacion-borrable-created": lambda data: data["id"] > 0,
    "licitacion-id": lambda data: data["id"] > 0,
    "licitacion-adjudicada-smoke": lambda data: data["estado"] == "Adjudicada" and data["proveedorAdjudicadoId"] == 1 and data["montoAdjudicado"] == 900.0 and [oferta["esGanadora"] for oferta in data["ofertas"]] == [True],
    "licitacion-duplicate": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe una licitación con el número LIC-2026-001.",
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
    "roles-list": lambda data: data["total"] == 6 and [item["codigo"] for item in data["items"]] == ["ADMIN", "ALMACEN", "COMPRAS", "CONSULTA", "RRHH", "VENTAS"],
    "role-detail": lambda data: data["codigo"] == "VENTAS" and data["modulosConLectura"] == 11 and data["modulosConEscritura"] == 10,
    "role-permissions": lambda data: len(data) == 80 and data[20]["puedeLeer"] is True and data[20]["puedeEscribir"] is False,
    "roles-matrix": lambda data: len(data["roles"]) == 6 and len(data["modulos"]) == 80 and data["modulos"][0]["permisos"] == ["Escritura", "Ninguno", "Ninguno", "Lectura", "Ninguno", "Escritura"],
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
    "vehiculos-camion": lambda data: data["total"] == 2 and all(item["tipo"] == "Camion" for item in data["items"]),
    "vehiculos-inactivo": lambda data: data["total"] == 1 and data["items"][0]["placa"] == "JKL-012" and data["items"][0]["activo"] is False,
    "vehiculo-detail": lambda data: data["id"] == 1 and data["placa"] == "ABC-123" and data["marca"] == "Volvo" and data["tipo"] == "Camion" and data["capacidadKg"] == 20000.0,
    "vehiculos-tipos": lambda data: data == ["Camion", "Furgon", "Moto", "Otro"],
    "vehiculos-resumen": lambda data: data["totalVehiculos"] == 6 and data["activos"] == 5 and data["inactivos"] == 1 and data["capacidadTotalKg"] == 44050.0 and [(item["tipo"], item["totalVehiculos"], item["activos"]) for item in data["porTipo"]] == [("Camion", 2, 2), ("Furgon", 2, 1), ("Moto", 1, 1), ("Otro", 1, 1)],
    "vehiculo-created": lambda data: data["id"] > 0,
    "vehiculo-placa-duplicate": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe un vehículo con la placa ABC-123.",
    "vehiculo-placa-minuscula-duplicate": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe un vehículo con la placa abc-123." and "Detail" not in data,
    "vehiculo-placa-propia-updated": lambda data: data["id"] > 0,
    "vehiculo-placa-propia-minuscula": lambda data: data["placa"] == "smk-001" and data["observaciones"] == "Alta de smoke",
    "vehiculo-capacidad-invalid": lambda data: data["StatusCode"] == 400 and data["Message"] == "La capacidad no puede superar 9999999999999999.99." and "Detail" not in data,
    "vehiculo-observaciones-invalid": lambda data: data["StatusCode"] == 400 and data["Message"] == "Las observaciones no pueden superar 500 caracteres." and "Detail" not in data,
    "conductor-sin-zona-created": lambda data: data["id"] > 0,
    "conductor-sin-zona-detail": lambda data: data["nombre"] == "Conductor Sin Zona" and data["vencimientoLicencia"] == "2027-12-31T00:00:00Z",
    "conductor-updated": lambda data: data["id"] == 2,
    "conductor-2-inactivo": lambda data: data["id"] == 2 and data["activo"] is False and data["vencimientoLicencia"] == "2026-06-30T00:00:00Z",
    "conductor-2-activo": lambda data: data["id"] == 2 and data["activo"] is True and data["vencimientoLicencia"] == "2026-06-30T00:00:00Z",
    "conductor-vencimiento-invalid": lambda data: data["StatusCode"] == 400 and data["Message"] == "La fecha de vencimiento de la licencia no es válida." and "Detail" not in data,
    "conductor-observaciones-invalid": lambda data: data["StatusCode"] == 400 and data["Message"] == "Las observaciones no pueden superar 500 caracteres." and "Detail" not in data,
    "conductores-list": lambda data: data["total"] == 3 and len(data["items"]) == 3,
    "conductor-detail": lambda data: data["id"] == 1 and data["nombre"] == "Carlos Ruiz" and data["documentoLicencia"] == "PLT-0001" and data["habilitado"] is True,
    "conductor-created": lambda data: data["id"] > 0,
    "conductor-duplicate": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe un conductor con el documento de licencia PLT-0001." and "Detail" not in data,
    "conductor-duplicate-minuscula": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe un conductor con el documento de licencia plt-0001." and "Detail" not in data,
    "conductor-licencia-propia-updated": lambda data: data["id"] > 0,
    "conductor-licencia-propia-detail": lambda data: data["documentoLicencia"] == "plt-smk-1" and data["vencimientoLicencia"] == "2027-12-31T00:00:00Z",
    "conductor-missing": lambda data: data["StatusCode"] == 404 and data["Message"] == "El conductor 9999 no existe." and "Detail" not in data,
    "openapi-empleados": lambda data: all(path in data["paths"] for path in ["/api/v1/empleados", "/api/v1/empleados/{id}", "/api/v1/empleados/{id}/cese"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/empleados", "get"), ("/api/v1/empleados", "post"), ("/api/v1/empleados/{id}", "get"), ("/api/v1/empleados/{id}", "put"), ("/api/v1/empleados/{id}/cese", "post")]) and "delete" not in data["paths"]["/api/v1/empleados/{id}"],
    "empleados-cesados": lambda data: data["total"] == 1 and data["items"][0]["legajo"] == "EMP-000004",
    "empleado-detail": lambda data: data["legajo"] == "EMP-000001",
    "empleado-created": lambda data: isinstance(data.get("id"), int) and data["id"] > 0,
    "empleado-duplicate": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe un empleado con el documento DNI 10000001." and "Detail" not in data,
    "empleado-minor": lambda data: data["StatusCode"] == 400 and data["Message"] == "El empleado debe tener al menos 18 años a la fecha de ingreso." and "Detail" not in data,
    "empleado-cesado": lambda data: isinstance(data.get("id"), int) and data["id"] > 0,
    "empleado-ceased-edit": lambda data: data["StatusCode"] == 409 and data["Message"] == "No se puede editar un empleado cesado." and "Detail" not in data,
    "empleado-cese-repeated": lambda data: data["StatusCode"] == 409 and re.fullmatch(r"El empleado [0-9]+ ya está cesado\.", data["Message"]) is not None and "Detail" not in data,
    "contrato-laboral-empleado-created": lambda data: isinstance(data.get("id"), int) and data["id"] > 0 and data["numero"] == "CTR-" + str(data["id"]).zfill(6),
    "contrato-laboral-created-e3": lambda data: isinstance(data.get("id"), int) and data["id"] > 0 and data["numero"] == "CTR-" + str(data["id"]).zfill(6),
    "contrato-laboral-renovado": lambda data: isinstance(data.get("id"), int) and data["id"] > 0 and data["numero"] == "CTR-" + str(data["id"]).zfill(6),
    "openapi-contratos-laborales": lambda data: all(path in data["paths"] for path in ["/api/v1/contratos-laborales", "/api/v1/contratos-laborales/{id}", "/api/v1/contratos-laborales/{id}/renovacion", "/api/v1/contratos-laborales/{id}/adendas", "/api/v1/contratos-laborales/{id}/anulacion"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/contratos-laborales", "get"), ("/api/v1/contratos-laborales", "post"), ("/api/v1/contratos-laborales/{id}", "get"), ("/api/v1/contratos-laborales/{id}/renovacion", "post"), ("/api/v1/contratos-laborales/{id}/adendas", "post"), ("/api/v1/contratos-laborales/{id}/anulacion", "post")]) and "put" not in data["paths"]["/api/v1/contratos-laborales/{id}"] and "delete" not in data["paths"]["/api/v1/contratos-laborales/{id}"],
    "contrato-laboral-vencido": lambda data: data["total"] == 1 and data["items"][0]["numero"] == "CTR-000005" and data["items"][0]["estado"] == "Vencido",
    "contrato-laboral-empleado-2": lambda data: data["total"] == 3,
    "contrato-laboral-detail": lambda data: data["numero"] == "CTR-000001" and data["estado"] == "Vigente" and data["cargoVigente"] == "Gerente Comercial" and data["sueldoVigente"] == 9500.00 and len(data["adendas"]) == 1,
    "contrato-laboral-created": lambda data: isinstance(data.get("id"), int) and data["id"] > 0 and data["numero"] == "CTR-" + str(data["id"]).zfill(6),
    "contrato-laboral-empleado-cerrado": lambda data: data["total"] == 1 and data["items"][0]["estado"] == "Cerrado",
    "contrato-laboral-cese-detail": lambda data: data["estado"] == "Cerrado" and data["fechaCierre"] == "2026-10-31" and data["motivoCierre"] == "Cese del empleado" and data["fechaFin"] == "2027-09-30",
    "contrato-laboral-empleado-cesado": lambda data: data["StatusCode"] == 409 and re.fullmatch(r"El empleado [0-9]+ está cesado y no admite contratos nuevos\.", data["Message"]) is not None and "Detail" not in data,
    "contrato-laboral-vigente-duplicado": lambda data: data["StatusCode"] == 409 and data["Message"] == "El empleado 1 ya tiene un contrato vigente." and "Detail" not in data,
    "contrato-laboral-vencido-empleado-3": lambda data: data["total"] == 1 and data["items"][0]["numero"] == "CTR-000005",
    "contrato-laboral-id": lambda data: isinstance(data.get("id"), int) and data["id"] > 0,
    "contrato-laboral-adenda-duplicada": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe una adenda con fecha de vigencia igual o posterior." and "Detail" not in data,
    "contrato-laboral-adenda-detail": lambda data: data["estado"] == "Vigente" and data["cargo"] == "Almacenero" and data["sueldoBasico"] == 1500.00 and data["cargoVigente"] == "Almacenero" and data["sueldoVigente"] == 1500.00 and len(data["adendas"]) == 1 and data["adendas"][0]["sueldoBasico"] == 1800.00 and data["adendas"][0]["fechaVigencia"] == "2027-03-01",
    "contrato-laboral-renovacion-repetida": lambda data: data["StatusCode"] == 409 and re.fullmatch(r"El contrato CTR-[0-9]{6} no está vigente y no se puede renovar\.", data["Message"]) is not None and "Detail" not in data,
    "contrato-laboral-anulacion-repetida": lambda data: data["StatusCode"] == 409 and re.fullmatch(r"El contrato CTR-[0-9]{6} no está vigente y no se puede anular\.", data["Message"]) is not None and "Detail" not in data,
    "contrato-laboral-sin-rol": lambda data: data["StatusCode"] == 403 and data["Message"] == "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación." and "Detail" not in data,
    "openapi-turnos": lambda data: all(path in data["paths"] for path in ["/api/v1/turnos", "/api/v1/turnos/resumen", "/api/v1/turnos/{id}", "/api/v1/turnos/{id}/desactivacion", "/api/v1/turnos/{id}/activacion", "/api/v1/asignaciones-turno", "/api/v1/asignaciones-turno/programacion", "/api/v1/asignaciones-turno/{id}/finalizacion", "/api/v1/asignaciones-turno/{id}"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/turnos", "get"), ("/api/v1/turnos", "post"), ("/api/v1/turnos/resumen", "get"), ("/api/v1/turnos/{id}", "get"), ("/api/v1/turnos/{id}", "put"), ("/api/v1/turnos/{id}/desactivacion", "post"), ("/api/v1/turnos/{id}/activacion", "post"), ("/api/v1/asignaciones-turno", "get"), ("/api/v1/asignaciones-turno", "post"), ("/api/v1/asignaciones-turno/programacion", "get"), ("/api/v1/asignaciones-turno/{id}/finalizacion", "post"), ("/api/v1/asignaciones-turno/{id}", "delete")]),
    "turnos-rotativos": lambda data: data["total"] == 1 and data["items"][0]["codigo"] == "ROT4X2" and data["items"][0]["diasTrabajo"] == 4 and data["items"][0]["diasDescanso"] == 2,
    "turnos-resumen": lambda data: data["totalTurnos"] == 4 and data["activos"] == 3 and data["fijos"] == 3 and data["rotativos"] == 1,
    "programacion-empleado-2": lambda data: len(data) == 6 and all(dia["laborable"] and dia["observacion"] is None for dia in data[:4]) and all((not dia["laborable"]) and dia["observacion"] == "Descanso" for dia in data[4:]) and data[0]["fecha"] == "2026-01-05" and data[0]["horaEntrada"] == "07:00:00",
    "turno-codigo-duplicado": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe un turno con el código DIA." and "Detail" not in data,
    "turno-created": lambda data: isinstance(data.get("id"), int) and data["id"] > 0,
    "asignacion-turno-created": lambda data: isinstance(data.get("id"), int) and data["id"] > 0,
    "asignacion-turno-empleado-cesado": lambda data: data["StatusCode"] == 409 and data["Message"] == "El empleado 4 está cesado y no admite turnos." and "Detail" not in data,
    "openapi-asistencia": lambda data: all(path in data["paths"] for path in ["/api/v1/marcaciones", "/api/v1/marcaciones/importacion", "/api/v1/marcaciones/{id}", "/api/v1/jornadas-asistencia", "/api/v1/jornadas-asistencia/resumen", "/api/v1/jornadas-asistencia/calculo", "/api/v1/jornadas-asistencia/{id}/horas-extra/aprobacion", "/api/v1/jornadas-asistencia/{id}/horas-extra/rechazo"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/marcaciones", "get"), ("/api/v1/marcaciones", "post"), ("/api/v1/marcaciones/importacion", "post"), ("/api/v1/marcaciones/{id}", "delete"), ("/api/v1/jornadas-asistencia", "get"), ("/api/v1/jornadas-asistencia/resumen", "get"), ("/api/v1/jornadas-asistencia/calculo", "post"), ("/api/v1/jornadas-asistencia/{id}/horas-extra/aprobacion", "post"), ("/api/v1/jornadas-asistencia/{id}/horas-extra/rechazo", "post")]),
    "asistencia-resumen": lambda data: data["jornadas"] == 13 and data["asistencias"] == 11 and data["tardanzas"] == 1 and data["sinMarcacion"] == 1 and data["faltas"] == 0 and data["minutosTardanza"] == 15 and data["minutosExtraPendientes"] == 90 and data["minutosExtraAprobados"] == 150,
    "marcaciones-importadas": lambda data: data["recibidos"] == 2 and data["importados"] == 2 and data["yaImportados"] == 0 and data["duplicados"] == 0 and data["rechazados"] == [],
    "marcaciones-ya-importadas": lambda data: data["recibidos"] == 2 and data["importados"] == 0 and data["yaImportados"] == 2 and data["duplicados"] == 0 and data["rechazados"] == [],
    "marcacion-empleado-cesado": lambda data: data["StatusCode"] == 409 and data["Message"] == "El empleado 4 no está activo en esa fecha." and "Detail" not in data,
    "asistencia-calculo": lambda data: data["calculadas"] == 12 and data["conservadas"] == 1 and data["eliminadas"] == 0,
    "jornada-id-8": lambda data: data["id"] == 8,
    "jornada-sin-horas-extra-pendientes": lambda data: data["StatusCode"] == 409 and data["Message"] == "La jornada 8 no tiene horas extra pendientes." and "Detail" not in data,
    "openapi-ausencias": lambda data: all(path in data["paths"] for path in ["/api/v1/ausencias", "/api/v1/ausencias/resumen", "/api/v1/ausencias/{id}", "/api/v1/ausencias/saldo-vacacional/{empleadoId}", "/api/v1/ausencias/{id}/aprobacion", "/api/v1/ausencias/{id}/rechazo", "/api/v1/ausencias/{id}/cancelacion"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/ausencias", "get"), ("/api/v1/ausencias", "post"), ("/api/v1/ausencias/resumen", "get"), ("/api/v1/ausencias/{id}", "get"), ("/api/v1/ausencias/saldo-vacacional/{empleadoId}", "get"), ("/api/v1/ausencias/{id}/aprobacion", "post"), ("/api/v1/ausencias/{id}/rechazo", "post"), ("/api/v1/ausencias/{id}/cancelacion", "post")]),
    "ausencias-resumen": lambda data: all(isinstance(data[clave], int) for clave in ["pendientes", "ausentesHoy", "vacacionesHoy", "descansosMedicosHoy", "aprobadasMes"]) and data["pendientes"] == 1,
    "saldo-empleado-1": lambda data: data["empleadoId"] == 1 and data["mesesServicio"] == 68 and data["diasDevengados"] == 170 and data["diasGozados"] == 12 and data["diasProgramados"] == 7 and data["saldo"] == 151,
    "ausencia-saldo-insuficiente": lambda data: data["StatusCode"] == 409 and data["Message"] == "El saldo vacacional es de 180.0 días y la solicitud pide 358." and "Detail" not in data,
    "ausencia-created-8": lambda data: data["id"] == 8 and data["numero"] == "AUS-000008",
    "ausencia-id-8": lambda data: data["id"] == 8,
    "ausencia-no-pendiente": lambda data: data["StatusCode"] == 409 and data["Message"] == "La ausencia AUS-000008 no está pendiente." and "Detail" not in data,
    "openapi-prestamos": lambda data: all(path in data["paths"] for path in ["/api/v1/prestamos", "/api/v1/prestamos/resumen", "/api/v1/prestamos/{id}", "/api/v1/prestamos/capacidad/{empleadoId}", "/api/v1/prestamos/{id}/anulacion"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/prestamos", "get"), ("/api/v1/prestamos", "post"), ("/api/v1/prestamos/resumen", "get"), ("/api/v1/prestamos/{id}", "get"), ("/api/v1/prestamos/capacidad/{empleadoId}", "get"), ("/api/v1/prestamos/{id}/anulacion", "post")]),
    "prestamos-resumen": lambda data: data["activos"] == 1 and data["saldoPendienteTotal"] == 400 and isinstance(data["otorgadoMes"], (int, float)) and isinstance(data["cuotasDelMes"], (int, float)),
    "prestamos-capacidad-1": lambda data: data["sueldoVigente"] == 9500 and data["netoEstimado"] == 8265 and data["cuotaMaxima"] == 2479.5 and [(fila["anio"], fila["mes"], fila["monto"]) for fila in data["cuotasComprometidasPorMes"]] == [(2026, 11, 100), (2026, 12, 100), (2027, 1, 100), (2027, 2, 100)],
    "prestamo-supera-tope": lambda data: data["StatusCode"] == 409 and data["Message"] == "La cuota de 11/2026 sería 2500.00 y supera el máximo de 2479.50 (30 % del neto estimado)." and "Detail" not in data,
    "prestamo-created-4": lambda data: data["id"] == 4 and data["numero"] == "PRE-000004",
    "prestamo-con-descuentos": lambda data: data["StatusCode"] == 409 and data["Message"] == "El préstamo PRE-000001 ya tiene descuentos y no se puede anular." and "Detail" not in data,
    "prestamo-id-4": lambda data: data["id"] == 4,
    "openapi-planillas": lambda data: all(path in data["paths"] for path in ["/api/v1/planillas", "/api/v1/planillas/resumen", "/api/v1/planillas/parametros", "/api/v1/planillas/{id}", "/api/v1/planillas/{id}/detalles", "/api/v1/planillas/boletas/{detalleId}", "/api/v1/planillas/{id}/calculo", "/api/v1/planillas/{id}/aprobacion", "/api/v1/planillas/{id}/pago", "/api/v1/bonos-planilla", "/api/v1/bonos-planilla/{id}", "/api/v1/afiliaciones-pension", "/api/v1/afiliaciones-pension/{empleadoId}"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/planillas", "get"), ("/api/v1/planillas", "post"), ("/api/v1/planillas/{id}/calculo", "post"), ("/api/v1/planillas/{id}/aprobacion", "post"), ("/api/v1/planillas/{id}/pago", "post"), ("/api/v1/bonos-planilla", "post"), ("/api/v1/bonos-planilla/{id}", "delete"), ("/api/v1/afiliaciones-pension/{empleadoId}", "put")]),
    "planillas-parametros": lambda data: len(data["parametros"]) == 7 and len(data["pensiones"]) == 5 and [tramo["hastaUit"] for tramo in data["tramos"]] == [5, 20, 35, 45, None] and [parametro["valor"] for parametro in data["parametros"] if parametro["clave"] == "UIT"] == [5350],
    "planilla-duplicada": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe la planilla Mensual del periodo 11/2026." and "Detail" not in data,
    "planilla-calculada": lambda data: data["id"] == 2 and data["empleados"] == 2 and data["incidencias"] == 0,
    "planilla-2-calculada": lambda data: data["numero"] == "PLA-202611-M" and data["estado"] == "Calculada" and data["totalIngresos"] == 13578.49 and data["totalDescuentos"] == 2701 and data["totalNeto"] == 10877.49 and data["totalAportesEmpleador"] == 1222.06 and data["calculadoPor"] == "admin",
    "planilla-id-2": lambda data: data["id"] == 2,
    "planilla-aprobada-no-modificable": lambda data: data["StatusCode"] == 409 and data["Message"] == "La planilla PLA-202611-M está aprobada y no se modifica." and "Detail" not in data,
    "planilla-boleta": lambda data: data["empresa"] == "TitanGlobal" and data["numeroBoleta"] == "BOL-202611-M-000001" and data["empleadoId"] == 1 and data["neto"] == 7505.11,
    "planilla-pagada": lambda data: data["id"] == 2,
    "ausencia-created-9":lambda data: data["id"] == 9 and data["numero"] == "AUS-000009",
    "ausencia-id-9": lambda data: data["id"] == 9,
    "openapi-evaluaciones": lambda data: all(path in data["paths"] for path in ["/api/v1/ciclos-evaluacion", "/api/v1/ciclos-evaluacion/{id}", "/api/v1/ciclos-evaluacion/{id}/resultados", "/api/v1/ciclos-evaluacion/{id}/competencias", "/api/v1/ciclos-evaluacion/{id}/competencias/{competenciaId}", "/api/v1/ciclos-evaluacion/{id}/apertura", "/api/v1/ciclos-evaluacion/{id}/cierre", "/api/v1/evaluaciones", "/api/v1/evaluaciones/{id}", "/api/v1/evaluaciones/{id}/calificaciones", "/api/v1/evaluaciones/{id}/envio", "/api/v1/evaluaciones/{id}/cierre"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/ciclos-evaluacion", "post"), ("/api/v1/ciclos-evaluacion/{id}/competencias/{competenciaId}", "delete"), ("/api/v1/ciclos-evaluacion/{id}/apertura", "post"), ("/api/v1/evaluaciones", "post"), ("/api/v1/evaluaciones/{id}/calificaciones", "put"), ("/api/v1/evaluaciones/{id}/envio", "post")]),
    "evaluacion-resultados-s1": lambda data: data["evaluadas"] == 3 and data["puntajePromedio"] == 3.63 and [(fila["etiqueta"], fila["cantidad"]) for fila in data["distribucion"]] == [("Insuficiente", 0), ("En desarrollo", 1), ("Competente", 1), ("Destacado", 0), ("Excepcional", 1)] and [(fila["competencia"], fila["promedio"]) for fila in data["porCompetencia"]] == [("Calidad del trabajo", 4), ("Trabajo en equipo", 3.33), ("Cumplimiento de objetivos", 3.67), ("Iniciativa", 3.33)],
    "evaluacion-4-detalle": lambda data: data["cicloCodigo"] == "2026-S2" and data["estado"] == "Enviada" and data["puntajeFinal"] == 4.1 and data["etiqueta"] == "Destacado" and [fila["calificacion"] for fila in data["calificaciones"]] == [4, 5, 4, 3],
    "ciclo-evaluacion-created-3": lambda data: data["id"] == 3,
    "ciclo-evaluacion-id-3": lambda data: data["id"] == 3,
    "competencia-evaluacion-created-9": lambda data: data["id"] == 9,
    "competencia-evaluacion-created-10": lambda data: data["id"] == 10,
    "ciclo-evaluacion-pesos-incompletos": lambda data: data["StatusCode"] == 409 and data["Message"] == "Los pesos de las competencias suman 60.00 y deben sumar 100." and "Detail" not in data,
    "evaluacion-created-6": lambda data: data["id"] == 6,
    "evaluacion-id-6": lambda data: data["id"] == 6,
    "evaluacion-duplicada": lambda data: data["StatusCode"] == 409 and data["Message"] == "El empleado 1 ya tiene una evaluación en el ciclo SMK-2026." and "Detail" not in data,
    "evaluacion-6-enviada": lambda data: data["estado"] == "Enviada" and data["puntajeFinal"] == 4.4 and data["etiqueta"] == "Destacado",
    "evaluacion-resultados-smk": lambda data: data["evaluadas"] == 1 and data["puntajePromedio"] == 4.4,
    "openapi-capacitaciones": lambda data: all(path in data["paths"] for path in ["/api/v1/cursos", "/api/v1/cursos/{id}", "/api/v1/cursos/{id}/desactivacion", "/api/v1/cursos/{id}/activacion", "/api/v1/programaciones-curso", "/api/v1/programaciones-curso/{id}", "/api/v1/programaciones-curso/{id}/inscripciones", "/api/v1/programaciones-curso/{id}/inscripciones/{inscripcionId}/retiro", "/api/v1/programaciones-curso/{id}/inscripciones/{inscripcionId}/certificacion", "/api/v1/programaciones-curso/{id}/realizacion", "/api/v1/programaciones-curso/{id}/cancelacion", "/api/v1/programaciones-curso/{id}/resultados", "/api/v1/capacitaciones/horas", "/api/v1/capacitaciones/empleados/{empleadoId}", "/api/v1/capacitaciones/resumen"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/cursos", "post"), ("/api/v1/cursos/{id}", "put"), ("/api/v1/programaciones-curso", "post"), ("/api/v1/programaciones-curso/{id}/inscripciones", "post"), ("/api/v1/capacitaciones/resumen", "get")]),
    "capacitaciones-resumen-2026": lambda data: data["anio"] == 2026 and data["cursosActivos"] == 3 and data["programadas"] == 0 and data["realizadas"] == 1 and data["inscritos"] == 4 and data["horasTotales"] == 24 and data["certificacionesVencidas"] == 1,
    "capacitaciones-resumen-2027": lambda data: data["anio"] == 2027 and data["programadas"] == 1 and data["realizadas"] == 0 and data["inscritos"] == 2 and data["horasTotales"] == 0,
    "capacitaciones-horas-2026": lambda data: data["anio"] == 2026 and {fila["empleadoId"]: fila["horas"] for fila in data["items"] if fila["empleadoId"] in (1, 2, 3)} == {1: 8, 2: 8, 3: 0} and all(fila["empleadoId"] != 4 for fila in data["items"]),
    "capacitaciones-empleado-2": lambda data: data["empleadoId"] == 2 and data["horas"] == 8 and [fila["cursoCodigo"] for fila in data["cursos"]] == ["SEG-001"] and [(fila["codigo"], fila["vencida"]) for fila in data["certificaciones"]] == [("CERT-SEG-0002", True)],
    "curso-created-5": lambda data: data["id"] == 5,
    "curso-duplicado": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe un curso con el código SMK-001." and "Detail" not in data,
    "curso-inactivo": lambda data: data["StatusCode"] == 409 and data["Message"] == "El curso ISO-900 está inactivo y no admite programaciones." and "Detail" not in data,
    "programacion-created-4": lambda data: data["id"] == 4,
    "programacion-id-4": lambda data: data["id"] == 4,
    "programacion-id-1": lambda data: data["id"] == 1,
    "inscripcion-cesado": lambda data: data["StatusCode"] == 409 and data["Message"] == "El empleado 4 está cesado y no se puede inscribir." and "Detail" not in data,
    "inscripcion-created-8": lambda data: data["id"] == 8,
    "inscripcion-id-8": lambda data: data["id"] == 8,
    "inscripcion-created-9": lambda data: data["id"] == 9,
    "inscripcion-sin-cupo": lambda data: data["StatusCode"] == 409 and data["Message"] == "La programación 4 no tiene cupo libre." and "Detail" not in data,
    "programacion-no-realizable": lambda data: data["StatusCode"] == 409 and data["Message"] == "La programación 4 termina el 2099-06-02 y aún no se puede marcar como realizada." and "Detail" not in data,
    "certificacion-no-aprobada": lambda data: data["StatusCode"] == 409 and data["Message"] == "La inscripción 5 no está aprobada y no admite certificación." and "Detail" not in data,
    "certificacion-created-3": lambda data: data["id"] == 3,
    "certificacion-repetida": lambda data: data["StatusCode"] == 409 and data["Message"] == "La inscripción 4 ya tiene certificación." and "Detail" not in data,
    "programacion-1-detalle": lambda data: data["estado"] == "Realizada" and data["inscritos"] == 4 and data["cupoLibre"] == 6 and [fila["estado"] for fila in data["inscripciones"]] == ["Aprobado", "Aprobado", "Aprobado", "Aprobado"] and [fila["nota"] for fila in data["inscripciones"]] == [17, 15.5, 12.5, 14] and [(fila["certificacion"]["codigo"], fila["certificacion"]["vencida"]) if fila["certificacion"] else None for fila in data["inscripciones"]] == [("CERT-SEG-0001", False), ("CERT-SEG-0002", True), None, ("SMK-0001", False)],
    "openapi-sst": lambda data: all(path in data["paths"] for path in ["/api/v1/equipos-proteccion", "/api/v1/equipos-proteccion/{id}", "/api/v1/equipos-proteccion/{id}/desactivacion", "/api/v1/equipos-proteccion/{id}/activacion", "/api/v1/entregas-epp", "/api/v1/examenes-medicos", "/api/v1/incidentes-sst", "/api/v1/incidentes-sst/{id}", "/api/v1/incidentes-sst/resumen", "/api/v1/incidentes-sst/{id}/acciones", "/api/v1/incidentes-sst/{id}/acciones/{accionId}/completar", "/api/v1/incidentes-sst/{id}/cierre"]) and all(verb in data["paths"][path] for path, verb in [("/api/v1/equipos-proteccion", "post"), ("/api/v1/equipos-proteccion/{id}", "put"), ("/api/v1/entregas-epp", "post"), ("/api/v1/examenes-medicos", "post"), ("/api/v1/incidentes-sst", "post"), ("/api/v1/incidentes-sst/resumen", "get")]),
    "sst-resumen-2026": lambda data: data["anio"] == 2026 and data["incidentesAbiertos"] == 1 and data["incidentesAnio"] == 2 and data["accidentesConBaja"] == 1 and data["diasBajaAnio"] == 5 and data["entregasEppVencidas"] == 1 and data["examenesVencidos"] == 1 and data["empleadosNoAptos"] == 0,
    "sst-incidente-2-abierto": lambda data: data["numero"] == "INC-000002" and data["estado"] == "Abierto" and data["fechaCierre"] is None and [(accion["id"], accion["completada"]) for accion in data["acciones"]] == [(2, False)],
    "equipo-proteccion-created-5": lambda data: data["id"] == 5,
    "equipo-proteccion-duplicado": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe un equipo de protección con el código SMK-EPP." and "Detail" not in data,
    "epp-equipo-inactivo": lambda data: data["StatusCode"] == 409 and data["Message"] == "El equipo de protección ARN-004 está inactivo y no se entrega." and "Detail" not in data,
    "epp-empleado-cesado": lambda data: data["StatusCode"] == 409 and data["Message"] == "El empleado 4 está cesado." and "Detail" not in data,
    "entrega-epp-created-4": lambda data: data["id"] == 4,
    "examen-retiro-fuera-de-plazo": lambda data: data["StatusCode"] == 409 and data["Message"] == "El examen de retiro se registra hasta 30 días después del cese del empleado 4." and "Detail" not in data,
    "examen-medico-created-5": lambda data: data["id"] == 5,
    "incidente-sst-created-3": lambda data: data["id"] == 3 and data["numero"] == "INC-000003",
    "accion-correctiva-created-3": lambda data: data["id"] == 3,
    "incidente-cierre-pendientes": lambda data: data["StatusCode"] == 409 and data["Message"] == "El incidente INC-000002 tiene 1 acciones correctivas sin completar." and "Detail" not in data,
    "accion-correctiva-id-2": lambda data: data["id"] == 2,
    "incidente-sst-id-2": lambda data: data["id"] == 2,
    "incidente-cerrado-sin-cambios": lambda data: data["StatusCode"] == 409 and data["Message"] == "El incidente INC-000002 está cerrado y no admite cambios." and "Detail" not in data,
    "sst-incidente-2-cerrado": lambda data: data["estado"] == "Cerrado" and data["fechaCierre"] is not None and [accion["completada"] for accion in data["acciones"]] == [True],

    "openapi-listas-materiales": lambda data: all(verb in data["paths"].get(path, {}) for path, verb in [
        ("/api/v1/listas-materiales", "get"), ("/api/v1/listas-materiales", "post"),
        ("/api/v1/listas-materiales/{id}", "get"), ("/api/v1/listas-materiales/{id}", "put"),
        ("/api/v1/listas-materiales/{id}/activar", "post"), ("/api/v1/listas-materiales/{id}/retirar", "post"),
    ]),
    "listas-materiales-seed": lambda data: data["total"] == 2 and [
        (item["version"], item["estado"], item["totalComponentes"]) for item in data["items"]
    ] == [(2, "Borrador", 2), (1, "Activa", 2)],
    "lista-material-creada": lambda data: isinstance(data.get("id"), int) and data["id"] > 0,
    "lista-material-retirada": lambda data: data["estado"] == "Retirada" and [
        (item["componenteProductoId"], item["cantidad"], item["unidadMedida"]) for item in data["componentes"]
    ] == [(6, 0.123456, "unidad")],
    "lista-material-version-duplicada": lambda data: data["StatusCode"] == 409 and data["Message"] == "Ya existe la versión 1 para el producto 1.",
    "lista-material-edicion-bloqueada": lambda data: data["StatusCode"] == 409 and data["Message"] == "Solo se pueden editar listas de materiales en borrador.",
    "openapi-rutas-produccion": lambda data: all(verb in data["paths"].get(path, {}) for path, verb in [
        ("/api/v1/rutas-produccion", "get"), ("/api/v1/rutas-produccion", "post"),
        ("/api/v1/rutas-produccion/{id}", "get"), ("/api/v1/rutas-produccion/{id}", "put"),
        ("/api/v1/rutas-produccion/{id}/activar", "post"), ("/api/v1/rutas-produccion/{id}/retirar", "post"),
    ]),
    "rutas-produccion-seed": lambda data: data["total"] == 2 and [
        (item["version"], item["estado"], item["totalOperaciones"]) for item in data["items"]
    ] == [(2, "Borrador", 2), (1, "Activa", 2)],
    "ruta-produccion-creada": lambda data: isinstance(data.get("id"), int) and data["id"] > 0,
    "ruta-produccion-retirada": lambda data: data["estado"] == "Retirada" and data["version"] == 1 and [
        (op["secuencia"], op["centroTrabajoNombre"], op["minutosPreparacion"], op["minutosPorUnidad"])
        for op in data["operaciones"]
    ] == [(1, "Celda de ensamblaje", 0.25, 2.75)],
    "ruta-produccion-version-duplicada": lambda data: (
        data["StatusCode"] == 409 and data["Message"] == "Ya existe la versión 1 para el producto 1."
    ),
    "ruta-produccion-edicion-bloqueada": lambda data: (
        data["StatusCode"] == 409 and data["Message"] == "Solo se pueden editar rutas de producción en borrador."
    ),
    "centro-en-uso-ruta": lambda data: data["StatusCode"] == 409 and re.fullmatch(
        r"No se puede eliminar el centro de trabajo [0-9]+ porque está en uso en una ruta de producción\.",
        data["Message"],
    ) is not None and "Detail" not in data,
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
    if [[ -n "${TOKEN_ACTUAL:-}" ]]; then
        cabecera_usuario+=(-H "Authorization: Bearer $TOKEN_ACTUAL")
    fi

    if [[ -n "$payload" ]]; then
        local payload_file
        payload_file="$(mktemp)"
        printf '%s' "$payload" > "$payload_file"
        if ! response="$(curl -sS --max-time 10 -X "$method" -H 'Content-Type: application/json' -H "X-User-Role: $role" "${cabecera_usuario[@]}" --data-binary "@$payload_file" -w $'\n%{http_code}' "$BASE_URL$path")"; then
            rm -f "$payload_file"
            printf 'ERROR: %s no pudo conectar con la API\n' "$label" >&2
            return 1
        fi
        rm -f "$payload_file"
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

    if [[ "$validation" == "evaluacion-created" ]]; then
        EVALUACION_CREADA_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "licitacion-created" ]]; then
        LICITACION_CREADA_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "oferta-licitacion-created" ]]; then
        OFERTA_LICITACION_CREADA_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "licitacion-borrable-created" ]]; then
        LICITACION_BORRABLE_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
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

    if [[ "$validation" == "role-created" || "$validation" == "usuario-created" ]]; then
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
    if [[ "$validation" == "vehiculo-created" ]]; then
        VEHICULO_CREADO_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "conductor-sin-zona-created" ]]; then
        CONDUCTOR_SIN_ZONA_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "m44-created" ]]; then
        CARGA_44_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "m44-baja-created" ]]; then
        BAJA_44_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "m43-created" ]]; then
        RUTA_43_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi

    if [[ "$validation" == "m48-created" ]]; then
        TRANSPORTISTA_48_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi
    if [[ "$validation" == "m48-flete-created" ]]; then
        FLETE_48_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi
    if [[ "$validation" == "conductor-created" ]]; then
        CONDUCTOR_CREADO_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi
    if [[ "$validation" == "empleado-created" ]]; then
        EMPLEADO_CREADO_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi
    if [[ "$validation" == "contrato-laboral-empleado-created" ]]; then
        CONTRATO_LABORAL_EMPLEADO_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi
    if [[ "$validation" == "contrato-laboral-created-e3" ]]; then
        CONTRATO_LABORAL_CREADO_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi
    if [[ "$validation" == "contrato-laboral-renovado" ]]; then
        CONTRATO_LABORAL_RENOVADO_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi
    if [[ "$validation" == "turno-created" ]]; then
        TURNO_CREADO_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi
    if [[ "$validation" == "asignacion-turno-created" ]]; then
        ASIGNACION_TURNO_CREADA_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi
    if [[ "$validation" == "lista-material-creada" ]]; then
        LISTA_MATERIAL_CREADA_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi
    if [[ "$validation" == "ruta-produccion-creada" ]]; then
        RUTA_PRODUCCION_CREADA_ID="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["id"])' "$body")"
    fi
    if [[ "$validation" == "login-ok" || "$validation" == "login-invitado" ]]; then
        TOKEN_LOGIN="$(python3 -c 'import json,sys; print(json.loads(sys.argv[1])["token"])' "$body")"
    fi
}

# Sin token solo responden la salud y el inicio de sesión; el resto de comprobaciones usa el token del administrador.
TOKEN_ACTUAL=''
TOKEN_LOGIN=''
request_check 'Salud sin token' GET '/api/v1/sistema/salud' 200 ''
request_check 'Clientes sin token' GET '/api/v1/clientes' 401 no-session
request_check 'Login con contraseña incorrecta' POST '/api/v1/autenticacion/login' 401 login-invalid '{"nombreUsuario":"admin","contrasena":"Incorrecta1"}'
request_check 'Login de la cuenta de demostración' POST '/api/v1/autenticacion/login' 200 login-invitado '{"nombreUsuario":"invitado","contrasena":"Demo2026!"}'
TOKEN_ACTUAL="$TOKEN_LOGIN"
request_check 'Perfil de la cuenta de demostración' GET '/api/v1/autenticacion/perfil' 200 perfil-invitado
request_check 'La cuenta de demostración no cambia contraseñas' PUT '/api/v1/usuarios/1/contrasena' 403 forbidden-admin '{"contrasena":"Cambiada2026!"}'
request_check 'Login del administrador' POST '/api/v1/autenticacion/login' 200 login-ok '{"nombreUsuario":"admin","contrasena":"Admin2026!"}'
TOKEN_ACTUAL="$TOKEN_LOGIN"
request_check 'Perfil del administrador' GET '/api/v1/autenticacion/perfil' 200 perfil-admin

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
request_check 'Evaluaciones del proveedor 1' GET '/api/v1/evaluaciones-proveedores?proveedorId=1' 200 evaluaciones-proveedor-1
request_check 'Evaluaciones calificación excelente' GET '/api/v1/evaluaciones-proveedores?calificacion=excelente' 200 evaluaciones-excelente
request_check 'Detalle de la evaluación 2' GET '/api/v1/evaluaciones-proveedores/2' 200 evaluacion-detalle
request_check 'Resumen de evaluaciones' GET '/api/v1/evaluaciones-proveedores/resumen' 200 evaluaciones-resumen
request_check 'POST evaluación válida' POST '/api/v1/evaluaciones-proveedores' 200 evaluacion-created '{"proveedorId":1,"periodo":"2026-08","puntajeCumplimiento":80,"puntajeCalidad":80,"puntajePlazos":80,"observaciones":"Evaluación smoke"}'
request_check 'POST evaluación del periodo repetido' POST '/api/v1/evaluaciones-proveedores' 409 evaluacion-duplicate '{"proveedorId":1,"periodo":"2026-10","puntajeCumplimiento":80,"puntajeCalidad":80,"puntajePlazos":80}'
request_check 'DELETE evaluación recién creada' DELETE "/api/v1/evaluaciones-proveedores/${EVALUACION_CREADA_ID}" 204 ''
request_check 'Licitaciones abiertas' GET '/api/v1/licitaciones?estado=abierta' 200 licitaciones-abiertas
request_check 'Detalle de la licitación adjudicada 3' GET '/api/v1/licitaciones/3' 200 licitacion-detalle
request_check 'Resumen de licitaciones' GET '/api/v1/licitaciones/resumen' 200 licitaciones-resumen
request_check 'POST licitación válida' POST '/api/v1/licitaciones' 200 licitacion-created '{"numero":"LIC-SMOKE-001","titulo":"Licitación smoke","tipo":"ConcursoPrecios","presupuestoReferencial":1000,"fechaCierre":"2099-12-31T00:00:00Z"}'
request_check 'POST oferta válida' POST "/api/v1/licitaciones/${LICITACION_CREADA_ID}/ofertas" 200 oferta-licitacion-created '{"proveedorId":1,"monto":900,"plazoEntregaDias":10}'
request_check 'Cerrar licitación recién creada' POST "/api/v1/licitaciones/${LICITACION_CREADA_ID}/cerrar" 200 licitacion-id ''
request_check 'Adjudicar licitación recién creada' POST "/api/v1/licitaciones/${LICITACION_CREADA_ID}/adjudicar" 200 licitacion-id "{\"ofertaId\":${OFERTA_LICITACION_CREADA_ID}}"
request_check 'Detalle de la licitación adjudicada en el smoke' GET "/api/v1/licitaciones/${LICITACION_CREADA_ID}" 200 licitacion-adjudicada-smoke
request_check 'POST licitación con número del seed' POST '/api/v1/licitaciones' 409 licitacion-duplicate '{"numero":"LIC-2026-001","titulo":"Duplicada","tipo":"ConcursoPrecios","presupuestoReferencial":1000,"fechaCierre":"2099-12-31T00:00:00Z"}'
request_check 'POST licitación para borrar' POST '/api/v1/licitaciones' 200 licitacion-borrable-created '{"numero":"LIC-SMOKE-002","titulo":"Licitación a borrar","tipo":"SubastaInversa","presupuestoReferencial":500,"fechaCierre":"2099-12-31T00:00:00Z"}'
request_check 'DELETE licitación sin ofertas' DELETE "/api/v1/licitaciones/${LICITACION_BORRABLE_ID}" 204 ''
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
OBSERVACIONES_LARGAS="$(printf 'x%.0s' $(seq 1 501))"
request_check 'Vehículos de tipo Camion' GET '/api/v1/vehiculos-flota?tipo=Camion' 200 vehiculos-camion
request_check 'Vehículos inactivos' GET '/api/v1/vehiculos-flota?activo=false' 200 vehiculos-inactivo
request_check 'Detalle del vehículo 1' GET '/api/v1/vehiculos-flota/1' 200 vehiculo-detail
request_check 'Tipos de vehículo' GET '/api/v1/vehiculos-flota/tipos' 200 vehiculos-tipos
request_check 'Resumen de vehículos' GET '/api/v1/vehiculos-flota/resumen' 200 vehiculos-resumen
request_check 'POST vehículo válido' POST '/api/v1/vehiculos-flota' 200 vehiculo-created '{"placa":"SMK-001","marca":"Marca Smoke","modelo":"Modelo Smoke","anio":2024,"tipo":"Camion","capacidadKg":1234.5,"activo":true,"observaciones":"Alta de smoke"}'
request_check 'POST vehículo con placa del seed' POST '/api/v1/vehiculos-flota' 409 vehiculo-placa-duplicate '{"placa":"ABC-123","marca":"Marca","modelo":"Modelo","anio":2024,"tipo":"Camion","capacidadKg":1000,"activo":true}'
request_check 'POST vehículo con placa del seed en minúsculas' POST '/api/v1/vehiculos-flota' 409 vehiculo-placa-minuscula-duplicate '{"placa":"  abc-123  ","marca":"Marca","modelo":"Modelo","anio":2024,"tipo":"Camion","capacidadKg":1000,"activo":true}'
request_check 'PUT vehículo con placa ajena en minúsculas' PUT "/api/v1/vehiculos-flota/${VEHICULO_CREADO_ID}" 409 vehiculo-placa-minuscula-duplicate '{"placa":"abc-123","marca":"Marca","modelo":"Modelo","anio":2024,"tipo":"Camion","capacidadKg":1000,"activo":true}'
request_check 'PUT vehículo con su propia placa en minúsculas' PUT "/api/v1/vehiculos-flota/${VEHICULO_CREADO_ID}" 200 vehiculo-placa-propia-updated '{"placa":"smk-001","marca":"Marca Smoke","modelo":"Modelo Smoke","anio":2024,"tipo":"Camion","capacidadKg":1234.5,"activo":true,"observaciones":"Alta de smoke"}'
request_check 'GET vehículo conserva su placa en minúsculas' GET "/api/v1/vehiculos-flota/${VEHICULO_CREADO_ID}" 200 vehiculo-placa-propia-minuscula
# La escritura directa evita la validación de Application y comprueba el índice real.
if error_placa_duplicada="$(docker compose exec -T db psql -U titanglobal -d titanglobal -v ON_ERROR_STOP=1 -v VERBOSITY=verbose -c \
    "INSERT INTO \"VehiculosFlota\" (\"Placa\", \"Marca\", \"Modelo\", \"Anio\", \"Tipo\", \"CapacidadKg\", \"Activo\") SELECT upper(\"Placa\"), \"Marca\", \"Modelo\", \"Anio\", \"Tipo\", \"CapacidadKg\", \"Activo\" FROM \"VehiculosFlota\" WHERE \"Id\" = ${VEHICULO_CREADO_ID};" 2>&1)"; then
    printf 'ERROR: PostgreSQL admitió una placa duplicada con otras mayúsculas\n' >&2
    exit 1
fi
if [[ "$error_placa_duplicada" != *23505* || "$error_placa_duplicada" != *UX_VehiculosFlota_PlacaNormalizada* ]]; then
    printf 'ERROR: la escritura directa no falló por el índice esperado de placas\n%s\n' "$error_placa_duplicada" >&2
    exit 1
fi
printf 'OK: índice de placas rechaza duplicados sin distinguir mayúsculas\n'
request_check 'POST vehículo con capacidad fuera de decimal(18,2)' POST '/api/v1/vehiculos-flota' 400 vehiculo-capacidad-invalid '{"placa":"SMK-002","marca":"Marca","modelo":"Modelo","anio":2024,"tipo":"Camion","capacidadKg":100000000000000000,"activo":true}'
request_check 'POST vehículo con observaciones de 501 caracteres' POST '/api/v1/vehiculos-flota' 400 vehiculo-observaciones-invalid "{\"placa\":\"SMK-003\",\"marca\":\"Marca\",\"modelo\":\"Modelo\",\"anio\":2024,\"tipo\":\"Camion\",\"capacidadKg\":1000,\"activo\":true,\"observaciones\":\"${OBSERVACIONES_LARGAS}\"}"
request_check 'DELETE vehículo recién creado' DELETE "/api/v1/vehiculos-flota/${VEHICULO_CREADO_ID}" 204 ''
request_check 'Conductores del seed' GET '/api/v1/conductores' 200 conductores-list
request_check 'Detalle del conductor 1' GET '/api/v1/conductores/1' 200 conductor-detail
request_check 'Crear conductor smoke' POST '/api/v1/conductores' 200 conductor-created '{"nombre":"Conductor Smoke","documentoLicencia":"PLT-SMK-1","vencimientoLicencia":"2027-12-31T00:00:00Z","habilitado":true,"activo":true,"observaciones":"Alta de smoke"}'
request_check 'Conductor con documento duplicado' POST '/api/v1/conductores' 409 conductor-duplicate '{"nombre":"Conductor Duplicado","documentoLicencia":"PLT-0001","vencimientoLicencia":"2027-12-31T00:00:00Z","habilitado":true,"activo":true}'
request_check 'POST conductor con licencia del seed en minúsculas' POST '/api/v1/conductores' 409 conductor-duplicate-minuscula '{"nombre":"Conductor Duplicado","documentoLicencia":"  plt-0001  ","vencimientoLicencia":"2027-12-31T00:00:00Z","habilitado":true,"activo":true}'
request_check 'PUT conductor con licencia ajena en minúsculas' PUT "/api/v1/conductores/${CONDUCTOR_CREADO_ID}" 409 conductor-duplicate-minuscula '{"nombre":"Conductor Smoke","documentoLicencia":"plt-0001","vencimientoLicencia":"2027-12-31T00:00:00Z","habilitado":true,"activo":true}'
request_check 'PUT conductor cambia mayúsculas de su propia licencia' PUT "/api/v1/conductores/${CONDUCTOR_CREADO_ID}" 200 conductor-licencia-propia-updated '{"nombre":"Conductor Smoke","documentoLicencia":"plt-smk-1","vencimientoLicencia":"2027-12-31T00:00:00Z","habilitado":true,"activo":true}'
request_check 'GET conductor conserva documento y fecha UTC' GET "/api/v1/conductores/${CONDUCTOR_CREADO_ID}" 200 conductor-licencia-propia-detail
if error_licencia_duplicada="$(docker compose exec -T db psql -U titanglobal -d titanglobal -v ON_ERROR_STOP=1 -v VERBOSITY=verbose -c \
    "INSERT INTO \"Conductores\" (\"Nombre\", \"DocumentoLicencia\", \"VencimientoLicencia\", \"Habilitado\", \"Activo\") SELECT \"Nombre\", upper(\"DocumentoLicencia\"), \"VencimientoLicencia\", \"Habilitado\", \"Activo\" FROM \"Conductores\" WHERE \"Id\" = ${CONDUCTOR_CREADO_ID};" 2>&1)"; then
    printf 'ERROR: PostgreSQL admitió una licencia duplicada con otras mayúsculas\n' >&2
    exit 1
fi
if [[ "$error_licencia_duplicada" != *23505* || "$error_licencia_duplicada" != *UX_Conductores_DocumentoLicenciaNormalizado* ]]; then
    printf 'ERROR: la escritura directa no falló por el índice esperado de licencias\n%s\n' "$error_licencia_duplicada" >&2
    exit 1
fi
printf 'OK: índice de licencias rechaza duplicados sin distinguir mayúsculas\n'
request_check 'Crear conductor con fecha sin Z' POST '/api/v1/conductores' 200 conductor-sin-zona-created '{"nombre":"Conductor Sin Zona","documentoLicencia":"PLT-SMK-2","vencimientoLicencia":"2027-12-31","habilitado":true,"activo":true}'
request_check 'Detalle del conductor con fecha sin Z' GET "/api/v1/conductores/${CONDUCTOR_SIN_ZONA_ID}" 200 conductor-sin-zona-detail
request_check 'Eliminar conductor con fecha sin Z' DELETE "/api/v1/conductores/${CONDUCTOR_SIN_ZONA_ID}" 204 ''
request_check 'Desactivar conductor con licencia vencida' PUT '/api/v1/conductores/2' 200 conductor-updated '{"nombre":"Maria Quispe","documentoLicencia":"PLT-0002","vencimientoLicencia":"2026-06-30T00:00:00Z","habilitado":true,"activo":false}'
request_check 'Detalle del conductor vencido desactivado' GET '/api/v1/conductores/2' 200 conductor-2-inactivo
request_check 'Mover licencia vencida a otra fecha pasada' PUT '/api/v1/conductores/2' 400 conductor-vencimiento-invalid '{"nombre":"Maria Quispe","documentoLicencia":"PLT-0002","vencimientoLicencia":"2020-01-01T00:00:00Z","habilitado":true,"activo":true}'
request_check 'Reactivar conductor vencido para dejar el seed intacto' PUT '/api/v1/conductores/2' 200 conductor-updated '{"nombre":"Maria Quispe","documentoLicencia":"PLT-0002","vencimientoLicencia":"2026-06-30T00:00:00Z","habilitado":true,"activo":true}'
request_check 'Detalle del conductor vencido reactivado' GET '/api/v1/conductores/2' 200 conductor-2-activo
request_check 'Conductor con observaciones de 501 caracteres' POST '/api/v1/conductores' 400 conductor-observaciones-invalid "{\"nombre\":\"Conductor Largo\",\"documentoLicencia\":\"PLT-SMK-3\",\"vencimientoLicencia\":\"2027-12-31\",\"habilitado\":true,\"activo\":true,\"observaciones\":\"${OBSERVACIONES_LARGAS}\"}"
request_check 'Detalle de conductor inexistente' GET '/api/v1/conductores/9999' 404 conductor-missing
request_check 'Eliminar conductor smoke' DELETE "/api/v1/conductores/${CONDUCTOR_CREADO_ID}" 204 ''
request_check 'OpenAPI de empleados' GET '/openapi/v1.json' 200 openapi-empleados
request_check 'Empleados cesados' GET '/api/v1/empleados?estado=Cesado' 200 empleados-cesados
request_check 'Detalle del empleado 1' GET '/api/v1/empleados/1' 200 empleado-detail
request_check 'POST empleado válido' POST '/api/v1/empleados' 200 empleado-created '{"tipoDocumento":"DNI","numeroDocumento":"20000001","nombres":"Empleado","apellidos":"Smoke Prueba","fechaNacimiento":"1995-04-10","fechaIngreso":"2026-10-01","cargo":"Analista de Pruebas","area":"Pruebas","email":"smoke.empleado@titanglobal.example","telefono":"999000100"}'
request_check 'POST empleado con documento del seed' POST '/api/v1/empleados' 409 empleado-duplicate '{"tipoDocumento":"DNI","numeroDocumento":"10000001","nombres":"Duplicado","apellidos":"Smoke Prueba","fechaNacimiento":"1995-04-10","fechaIngreso":"2026-10-01","cargo":"Analista de Pruebas","area":"Pruebas"}'
request_check 'POST empleado menor de edad' POST '/api/v1/empleados' 400 empleado-minor '{"tipoDocumento":"DNI","numeroDocumento":"20000002","nombres":"Menor","apellidos":"Smoke Prueba","fechaNacimiento":"2010-01-01","fechaIngreso":"2026-10-01","cargo":"Analista de Pruebas","area":"Pruebas"}'
request_check 'OpenAPI de contratos laborales' GET '/openapi/v1.json' 200 openapi-contratos-laborales
request_check 'Contratos laborales vencidos (calculado)' GET '/api/v1/contratos-laborales?estado=Vencido' 200 contrato-laboral-vencido
request_check 'Contratos laborales del empleado 2' GET '/api/v1/contratos-laborales?empleadoId=2' 200 contrato-laboral-empleado-2
request_check 'Detalle del contrato laboral 1' GET '/api/v1/contratos-laborales/1' 200 contrato-laboral-detail
request_check 'POST contrato laboral para el empleado creado' POST '/api/v1/contratos-laborales' 200 contrato-laboral-empleado-created '{"empleadoId":'"${EMPLEADO_CREADO_ID}"',"modalidad":"PlazoFijo","fechaInicio":"2026-10-01","fechaFin":"2027-09-30","sueldoBasico":2000.00,"cargo":"Analista de Pruebas"}'
request_check 'POST contrato laboral con contrato vigente' POST '/api/v1/contratos-laborales' 409 contrato-laboral-vigente-duplicado '{"empleadoId":1,"modalidad":"Indeterminado","fechaInicio":"2026-11-01","sueldoBasico":9000.00,"cargo":"Gerente Comercial"}'
TOKEN_ADMIN_SMOKE="$TOKEN_ACTUAL"
request_check 'Login de la cuenta sin rol de escritura' POST '/api/v1/autenticacion/login' 200 login-invitado '{"nombreUsuario":"invitado","contrasena":"Demo2026!"}'
TOKEN_ACTUAL="$TOKEN_LOGIN"
request_check 'POST contrato laboral sin rol RRHH o ADMIN' POST '/api/v1/contratos-laborales' 403 contrato-laboral-sin-rol '{"empleadoId":3,"modalidad":"PlazoFijo","fechaInicio":"2027-01-01","fechaFin":"2027-12-31","sueldoBasico":1500.00,"cargo":"Almacenero"}'
TOKEN_ACTUAL="$TOKEN_ADMIN_SMOKE"
request_check 'POST contrato laboral con vencido calculado' POST '/api/v1/contratos-laborales' 200 contrato-laboral-created-e3 '{"empleadoId":3,"modalidad":"PlazoFijo","fechaInicio":"2027-01-01","fechaFin":"2027-12-31","sueldoBasico":1500.00,"cargo":"Almacenero"}'
request_check 'Contrato vencido del empleado 3 se conserva' GET '/api/v1/contratos-laborales?empleadoId=3&estado=Vencido' 200 contrato-laboral-vencido-empleado-3
request_check 'POST adenda de sueldo' POST "/api/v1/contratos-laborales/${CONTRATO_LABORAL_CREADO_ID}/adendas" 200 contrato-laboral-id '{"fechaVigencia":"2027-03-01","sueldoBasico":1800.00}'
request_check 'POST adenda con vigencia repetida' POST "/api/v1/contratos-laborales/${CONTRATO_LABORAL_CREADO_ID}/adendas" 409 contrato-laboral-adenda-duplicada '{"fechaVigencia":"2027-03-01","sueldoBasico":1900.00}'
request_check 'Detalle del contrato laboral con adenda' GET "/api/v1/contratos-laborales/${CONTRATO_LABORAL_CREADO_ID}" 200 contrato-laboral-adenda-detail
request_check 'POST renovación del contrato laboral' POST "/api/v1/contratos-laborales/${CONTRATO_LABORAL_CREADO_ID}/renovacion" 200 contrato-laboral-renovado '{"modalidad":"PlazoFijo","fechaFin":"2028-12-31","sueldoBasico":1900.00,"cargo":"Almacenero"}'
request_check 'POST renovación repetida' POST "/api/v1/contratos-laborales/${CONTRATO_LABORAL_CREADO_ID}/renovacion" 409 contrato-laboral-renovacion-repetida '{"modalidad":"PlazoFijo","fechaFin":"2028-12-31","sueldoBasico":1900.00,"cargo":"Almacenero"}'
request_check 'POST anulación del contrato renovado' POST "/api/v1/contratos-laborales/${CONTRATO_LABORAL_RENOVADO_ID}/anulacion" 200 contrato-laboral-id '{"motivo":"Anulación de smoke"}'
request_check 'POST anulación repetida' POST "/api/v1/contratos-laborales/${CONTRATO_LABORAL_RENOVADO_ID}/anulacion" 409 contrato-laboral-anulacion-repetida '{"motivo":"Anulación de smoke"}'
request_check 'Cesar empleado creado' POST "/api/v1/empleados/${EMPLEADO_CREADO_ID}/cese" 200 empleado-cesado '{"fechaCese":"2026-10-31","motivo":"Renuncia de smoke"}'
request_check 'Contrato laboral del empleado cesado queda cerrado' GET '/api/v1/contratos-laborales?empleadoId='"${EMPLEADO_CREADO_ID}" 200 contrato-laboral-empleado-cerrado
request_check 'Detalle del contrato cerrado por cese (E4)' GET "/api/v1/contratos-laborales/${CONTRATO_LABORAL_EMPLEADO_ID}" 200 contrato-laboral-cese-detail
request_check 'POST contrato laboral para empleado cesado' POST '/api/v1/contratos-laborales' 409 contrato-laboral-empleado-cesado '{"empleadoId":'"${EMPLEADO_CREADO_ID}"',"modalidad":"PlazoFijo","fechaInicio":"2026-11-01","fechaFin":"2027-10-31","sueldoBasico":2000.00,"cargo":"Analista de Pruebas"}'
request_check 'Editar empleado cesado' PUT '/api/v1/empleados/4' 409 empleado-ceased-edit '{"tipoDocumento":"Pasaporte","numeroDocumento":"P1234567","nombres":"Carlos","apellidos":"Diaz Soto","fechaNacimiento":"1982-07-30","fechaIngreso":"2018-05-02","cargo":"Auxiliar de Logistica","area":"Logistica"}'
request_check 'Cesar empleado ya cesado' POST "/api/v1/empleados/${EMPLEADO_CREADO_ID}/cese" 409 empleado-cese-repeated '{"fechaCese":"2026-10-31","motivo":"Renuncia de smoke"}'
# Módulo 48: contratos y persistencia real, sin modificar módulos ajenos.
TOKEN_48_ADMIN="$TOKEN_ACTUAL"
TOKEN_ACTUAL=''
request_check 'M48 exige sesión' GET '/api/v1/transportistas-terceros' 401 no-session
request_check 'M48 fletes exige sesión' GET '/api/v1/fletes-terceros' 401 no-session
TOKEN_ACTUAL="$TOKEN_48_ADMIN"
request_check 'M48 OpenAPI diez operaciones' GET '/openapi/v1.json' 200 m48-openapi
request_check 'M48 maestro del seed' GET '/api/v1/transportistas-terceros' 200 m48-list
request_check 'M48 inactivos' GET '/api/v1/transportistas-terceros?activo=false' 200 m48-inactivos
request_check 'M48 resumen' GET '/api/v1/transportistas-terceros/resumen' 200 m48-resumen
request_check 'M48 fletes en monedas originales' GET '/api/v1/fletes-terceros' 200 m48-fletes
request_check 'M48 crear inactivo' POST '/api/v1/transportistas-terceros' 200 m48-created '{"razonSocial":"Smoke 48","documentoFiscal":"SMOKE-48","pais":"Perú","tipoServicio":"ambos","contacto":" ","activo":false}'
request_check 'M48 false y normalización persistidos' GET "/api/v1/transportistas-terceros/${TRANSPORTISTA_48_ID}" 200 m48-inactivo-detail
request_check 'M48 duplicado normalizado' POST '/api/v1/transportistas-terceros' 409 m48-duplicate '{"razonSocial":"Otro","documentoFiscal":" smoke-48 ","pais":" perú ","tipoServicio":"Flete"}'
FLETE_48_JSON='{"transportistaId":'"${TRANSPORTISTA_48_ID}"',"referencia":"SMOKE-48","fecha":"2026-11-21","origen":"Lima","destino":"Callao","importe":9999999999999999.99,"moneda":"usd"}'
request_check 'M48 impide flete de inactivo' POST '/api/v1/fletes-terceros' 409 m48-inactivo "$FLETE_48_JSON"
request_check 'M48 activar transportista' PUT "/api/v1/transportistas-terceros/${TRANSPORTISTA_48_ID}" 200 created '{"razonSocial":"Smoke 48","documentoFiscal":"SMOKE-48","pais":"Perú","tipoServicio":"Ambos","activo":true}'
request_check 'M48 flete sin Z y decimal máximo' POST '/api/v1/fletes-terceros' 200 m48-flete-created "$FLETE_48_JSON"
request_check 'M48 fecha UTC e importe exacto' GET "/api/v1/fletes-terceros/${FLETE_48_ID}" 200 m48-flete-detail
request_check 'M48 impide baja con historial' DELETE "/api/v1/transportistas-terceros/${TRANSPORTISTA_48_ID}" 409 m48-historial
request_check 'M48 no redondea' POST '/api/v1/fletes-terceros' 400 m48-decimales '{"transportistaId":1,"referencia":"DEC-48","fecha":"2026-11-21","origen":"Lima","destino":"Callao","importe":1.001,"moneda":"PEN"}'
request_check 'M48 exige motivo' POST "/api/v1/fletes-terceros/${FLETE_48_ID}/cancelacion" 400 m48-motivo '{"motivo":" "}'
request_check 'M48 cancelar con motivo' POST "/api/v1/fletes-terceros/${FLETE_48_ID}/cancelacion" 200 created '{"motivo":"  Ruta suspendida  "}'
request_check 'M48 historial de cancelación' GET "/api/v1/fletes-terceros/${FLETE_48_ID}" 200 m48-cancelado
request_check 'M48 no permite doble cancelación' POST "/api/v1/fletes-terceros/${FLETE_48_ID}/cancelacion" 409 m48-repetida '{"motivo":"Repetida"}'
request_check 'M48 cancelado reserva referencia' POST '/api/v1/fletes-terceros' 409 m48-referencia "${FLETE_48_JSON/SMOKE-48/smoke-48}"
request_check 'M48 filtrar historial' GET "/api/v1/fletes-terceros?transportistaId=${TRANSPORTISTA_48_ID}&estado=Cancelado&busqueda=callao" 200 m48-filtro
request_check 'M48 cancelado impide baja' DELETE "/api/v1/transportistas-terceros/${TRANSPORTISTA_48_ID}" 409 m48-historial
request_check 'M48 inexistente' GET '/api/v1/transportistas-terceros/999999' 404 m48-missing
# Deshacer únicamente los datos de esta comprobación después de validar su historial.
docker compose exec -T db psql -U titanglobal -d titanglobal -v ON_ERROR_STOP=1 <<'SQL'
DO $$
BEGIN
    BEGIN
        INSERT INTO "TransportistasTerceros" ("RazonSocial", "DocumentoFiscal", "Pais", "TipoServicio")
        VALUES ('Duplicado SQL', ' smoke-48 ', ' perú ', 'Flete');
        RAISE EXCEPTION 'M48: falta unicidad normalizada del transportista';
    EXCEPTION WHEN unique_violation THEN NULL;
    END;
    BEGIN
        INSERT INTO "FletesTerceros" ("TransportistaTerceroId", "Referencia", "Fecha", "Origen", "Destino", "Importe", "Moneda")
        SELECT "TransportistaTerceroId", ' smoke-48 ', now(), 'Lima', 'Callao', 1, 'PEN'
        FROM "FletesTerceros" WHERE "Referencia" = 'SMOKE-48';
        RAISE EXCEPTION 'M48: falta unicidad normalizada de la referencia';
    EXCEPTION WHEN unique_violation THEN NULL;
    END;
    BEGIN
        DELETE FROM "TransportistasTerceros" WHERE "DocumentoFiscal" = 'SMOKE-48';
        RAISE EXCEPTION 'M48: falta protección de historial por clave foránea';
    EXCEPTION WHEN foreign_key_violation THEN NULL;
    END;
END $$;
DELETE FROM "FletesTerceros" WHERE "Referencia" = 'SMOKE-48';
DELETE FROM "TransportistasTerceros" WHERE "DocumentoFiscal" = 'SMOKE-48';
SQL
request_check 'OpenAPI de turnos y asignaciones' GET '/openapi/v1.json' 200 openapi-turnos
request_check 'Turnos rotativos' GET '/api/v1/turnos?tipo=Rotativo' 200 turnos-rotativos
request_check 'Resumen de turnos' GET '/api/v1/turnos/resumen' 200 turnos-resumen
request_check 'Programación del empleado 2' GET '/api/v1/asignaciones-turno/programacion?empleadoId=2&desde=2026-01-05&hasta=2026-01-10' 200 programacion-empleado-2
request_check 'POST turno con código repetido' POST '/api/v1/turnos' 409 turno-codigo-duplicado '{"codigo":"DIA","nombre":"Duplicado","tipo":"Fijo","horaEntrada":"08:00:00","horaSalida":"17:00:00","cruzaMedianoche":false,"toleranciaMinutos":10,"diasLaborables":[1,2,3,4,5]}'
request_check 'POST turno válido' POST '/api/v1/turnos' 200 turno-created '{"codigo":"SMOKE","nombre":"Turno de smoke","tipo":"Fijo","horaEntrada":"09:00:00","horaSalida":"18:00:00","cruzaMedianoche":false,"toleranciaMinutos":5,"diasLaborables":[1,2,3,4,5]}'
request_check 'POST asignación futura del empleado 3' POST '/api/v1/asignaciones-turno' 200 asignacion-turno-created '{"empleadoId":3,"turnoId":'"${TURNO_CREADO_ID}"',"fechaInicio":"2030-01-01"}'
request_check 'DELETE asignación futura' DELETE "/api/v1/asignaciones-turno/${ASIGNACION_TURNO_CREADA_ID}" 204 ''
request_check 'POST asignación al empleado cesado' POST '/api/v1/asignaciones-turno' 409 asignacion-turno-empleado-cesado '{"empleadoId":4,"turnoId":'"${TURNO_CREADO_ID}"',"fechaInicio":"2030-01-01"}'
request_check 'OpenAPI de marcaciones y jornadas de asistencia' GET '/openapi/v1.json' 200 openapi-asistencia
request_check 'Resumen de asistencia del 2 al 6 de noviembre' GET '/api/v1/jornadas-asistencia/resumen?desde=2026-11-02&hasta=2026-11-06' 200 asistencia-resumen
request_check 'POST importación de dos marcaciones del reloj' POST '/api/v1/marcaciones/importacion' 200 marcaciones-importadas '{"dispositivo":"RELOJ-01","registros":[{"numeroRegistro":"SMOKE-0001","legajo":"EMP-000001","tipo":"Entrada","instante":"2026-11-09T13:00:00Z"},{"numeroRegistro":"SMOKE-0002","legajo":"EMP-000001","tipo":"Salida","instante":"2026-11-09T22:00:00Z"}]}'
request_check 'POST importación repetida' POST '/api/v1/marcaciones/importacion' 200 marcaciones-ya-importadas '{"dispositivo":"RELOJ-01","registros":[{"numeroRegistro":"SMOKE-0001","legajo":"EMP-000001","tipo":"Entrada","instante":"2026-11-09T13:00:00Z"},{"numeroRegistro":"SMOKE-0002","legajo":"EMP-000001","tipo":"Salida","instante":"2026-11-09T22:00:00Z"}]}'
request_check 'POST marcación manual del empleado cesado' POST '/api/v1/marcaciones' 409 marcacion-empleado-cesado '{"empleadoId":4,"tipo":"Entrada","instante":"2026-11-10T13:00:00Z","observacion":"Marcación de smoke"}'
request_check 'POST cálculo de jornadas del 2 al 6 de noviembre' POST '/api/v1/jornadas-asistencia/calculo' 200 asistencia-calculo '{"desde":"2026-11-02","hasta":"2026-11-06"}'
request_check 'POST aprobar horas extra pendientes' POST '/api/v1/jornadas-asistencia/8/horas-extra/aprobacion' 200 jornada-id-8
request_check 'POST aprobar horas extra repetido' POST '/api/v1/jornadas-asistencia/8/horas-extra/aprobacion' 409 jornada-sin-horas-extra-pendientes
request_check 'OpenAPI de ausencias' GET '/openapi/v1.json' 200 openapi-ausencias
request_check 'Resumen de ausencias' GET '/api/v1/ausencias/resumen' 200 ausencias-resumen
request_check 'Saldo vacacional del empleado 1' GET '/api/v1/ausencias/saldo-vacacional/1?fecha=2026-11-22' 200 saldo-empleado-1
request_check 'POST vacaciones que superan el saldo' POST '/api/v1/ausencias' 409 ausencia-saldo-insuficiente '{"empleadoId":3,"tipo":"Vacaciones","fechaInicio":"2030-01-08","fechaFin":"2030-12-31"}'
request_check 'POST permiso con goce' POST '/api/v1/ausencias' 200 ausencia-created-8 '{"empleadoId":2,"tipo":"PermisoConGoce","fechaInicio":"2030-02-04","fechaFin":"2030-02-05","motivo":"Tramite de smoke"}'
request_check 'POST aprobar ausencia' POST '/api/v1/ausencias/8/aprobacion' 200 ausencia-id-8
request_check 'POST aprobar ausencia repetido' POST '/api/v1/ausencias/8/aprobacion' 409 ausencia-no-pendiente
request_check 'POST segundo permiso con goce' POST '/api/v1/ausencias' 200 ausencia-created-9 '{"empleadoId":2,"tipo":"PermisoConGoce","fechaInicio":"2030-03-04","fechaFin":"2030-03-05","motivo":"Tramite de smoke"}'
request_check 'POST cancelar ausencia futura' POST '/api/v1/ausencias/9/cancelacion' 200 ausencia-id-9
request_check 'OpenAPI de prestamos' GET '/openapi/v1.json' 200 openapi-prestamos
request_check 'Resumen de prestamos' GET '/api/v1/prestamos/resumen' 200 prestamos-resumen
request_check 'Capacidad del empleado 1' GET '/api/v1/prestamos/capacidad/1?fecha=2026-11-22' 200 prestamos-capacidad-1
request_check 'POST adelanto que supera el tope' POST '/api/v1/prestamos' 409 prestamo-supera-tope '{"empleadoId":1,"tipo":"Adelanto","monto":2400,"fechaOtorgamiento":"2026-11-22","primerAnio":2026,"primerMes":11,"motivo":"Adelanto de smoke"}'
request_check 'POST prestamo valido' POST '/api/v1/prestamos' 200 prestamo-created-4 '{"empleadoId":2,"tipo":"Prestamo","monto":300,"numeroCuotas":3,"fechaOtorgamiento":"2026-11-22","primerAnio":2026,"primerMes":12,"motivo":"Prestamo de smoke"}'
request_check 'POST anular prestamo con descuentos' POST '/api/v1/prestamos/1/anulacion' 409 prestamo-con-descuentos '{"motivo":"Prueba de smoke"}'
request_check 'POST anular prestamo sin descuentos' POST '/api/v1/prestamos/4/anulacion' 200 prestamo-id-4 '{"motivo":"Prueba de smoke"}'
request_check 'OpenAPI de planillas' GET '/openapi/v1.json' 200 openapi-planillas
request_check 'Parametros de planilla' GET '/api/v1/planillas/parametros' 200 planillas-parametros
request_check 'POST planilla duplicada del seed' POST '/api/v1/planillas' 409 planilla-duplicada '{"anio":2026,"mes":11,"tipo":"Mensual"}'
request_check 'POST calcular planilla 2026-11' POST '/api/v1/planillas/2/calculo' 200 planilla-calculada
request_check 'Planilla 2026-11 calculada' GET '/api/v1/planillas/2' 200 planilla-2-calculada
request_check 'POST aprobar planilla 2026-11' POST '/api/v1/planillas/2/aprobacion' 200 planilla-id-2
request_check 'POST recalcular planilla aprobada' POST '/api/v1/planillas/2/calculo' 409 planilla-aprobada-no-modificable
request_check 'Boleta del primer detalle' GET '/api/v1/planillas/boletas/3' 200 planilla-boleta
request_check 'POST pago de la planilla 2026-11' POST '/api/v1/planillas/2/pago' 200 planilla-pagada '{"fechaPago":"2099-12-31"}'
# Modulo 68: las fechas del ciclo de humo salen de hoy para que no dependan del calendario.
SMOKE_INICIO_PERIODO="$(date -u -d '-169 days' +%Y-%m-%d)"
SMOKE_FIN_PERIODO="$(date -u -d '+10 days' +%Y-%m-%d)"
SMOKE_APERTURA_CICLO="$(date -u -d '-5 days' +%Y-%m-%d)"
SMOKE_CIERRE_CICLO="$(date -u -d '+30 days' +%Y-%m-%d)"
request_check 'OpenAPI de evaluacion de desempeno' GET '/openapi/v1.json' 200 openapi-evaluaciones
request_check 'Resultados del ciclo 2026-S1' GET '/api/v1/ciclos-evaluacion/1/resultados' 200 evaluacion-resultados-s1
request_check 'Evaluacion enviada del seed' GET '/api/v1/evaluaciones/4' 200 evaluacion-4-detalle
request_check 'POST ciclo de evaluacion' POST '/api/v1/ciclos-evaluacion' 200 ciclo-evaluacion-created-3 "{\"codigo\":\"smk-2026\",\"nombre\":\"Ciclo de smoke\",\"periodo\":\"Semestral\",\"fechaInicioPeriodo\":\"${SMOKE_INICIO_PERIODO}\",\"fechaFinPeriodo\":\"${SMOKE_FIN_PERIODO}\",\"fechaApertura\":\"${SMOKE_APERTURA_CICLO}\",\"fechaCierre\":\"${SMOKE_CIERRE_CICLO}\"}"
request_check 'POST primera competencia (60)' POST '/api/v1/ciclos-evaluacion/3/competencias' 200 competencia-evaluacion-created-9 '{"nombre":"Calidad","descripcion":"Calidad de smoke","peso":60}'
request_check 'POST abrir ciclo con pesos que no suman 100' POST '/api/v1/ciclos-evaluacion/3/apertura' 409 ciclo-evaluacion-pesos-incompletos
request_check 'POST segunda competencia (40)' POST '/api/v1/ciclos-evaluacion/3/competencias' 200 competencia-evaluacion-created-10 '{"nombre":"Equipo","peso":40}'
request_check 'POST abrir ciclo' POST '/api/v1/ciclos-evaluacion/3/apertura' 200 ciclo-evaluacion-id-3
request_check 'POST evaluacion del empleado 1' POST '/api/v1/evaluaciones' 200 evaluacion-created-6 '{"cicloId":3,"empleadoId":1,"evaluadorId":2}'
request_check 'POST segunda evaluacion del mismo empleado' POST '/api/v1/evaluaciones' 409 evaluacion-duplicada '{"cicloId":3,"empleadoId":1,"evaluadorId":2}'
request_check 'PUT calificaciones completas' PUT '/api/v1/evaluaciones/6/calificaciones' 200 evaluacion-id-6 '{"calificaciones":[{"competenciaId":9,"calificacion":4},{"competenciaId":10,"calificacion":5}],"comentario":"Evaluacion de smoke"}'
request_check 'POST enviar evaluacion' POST '/api/v1/evaluaciones/6/envio' 200 evaluacion-id-6
request_check 'Evaluacion enviada con puntaje' GET '/api/v1/evaluaciones/6' 200 evaluacion-6-enviada
request_check 'POST cerrar ciclo de humo' POST '/api/v1/ciclos-evaluacion/3/cierre' 200 ciclo-evaluacion-id-3
request_check 'Resultados del ciclo de humo' GET '/api/v1/ciclos-evaluacion/3/resultados' 200 evaluacion-resultados-smk
# Modulo 69: las programaciones de humo usan fechas de 2099 para que no dependan del calendario.
request_check 'OpenAPI de capacitaciones' GET '/openapi/v1.json' 200 openapi-capacitaciones
request_check 'Resumen de capacitaciones 2026' GET '/api/v1/capacitaciones/resumen?anio=2026' 200 capacitaciones-resumen-2026
request_check 'Resumen de capacitaciones 2027' GET '/api/v1/capacitaciones/resumen?anio=2027' 200 capacitaciones-resumen-2027
request_check 'Horas de capacitacion 2026' GET '/api/v1/capacitaciones/horas?anio=2026' 200 capacitaciones-horas-2026
request_check 'Capacitacion del empleado 2' GET '/api/v1/capacitaciones/empleados/2?anio=2026' 200 capacitaciones-empleado-2
request_check 'POST curso' POST '/api/v1/cursos' 200 curso-created-5 '{"codigo":"smk-001","nombre":"Curso de smoke","horas":4.5,"tipo":"Interno"}'
request_check 'POST curso con codigo repetido' POST '/api/v1/cursos' 409 curso-duplicado '{"codigo":"SMK-001","nombre":"Otro curso","horas":4.5,"tipo":"Virtual"}'
request_check 'POST programacion de un curso inactivo' POST '/api/v1/programaciones-curso' 409 curso-inactivo '{"cursoId":4,"fechaInicio":"2099-06-01","fechaFin":"2099-06-02","cupo":1}'
request_check 'POST programacion con cupo 1' POST '/api/v1/programaciones-curso' 200 programacion-created-4 '{"cursoId":5,"fechaInicio":"2099-06-01","fechaFin":"2099-06-02","cupo":1,"lugar":"Sala de smoke"}'
request_check 'POST inscripcion de un empleado cesado' POST '/api/v1/programaciones-curso/4/inscripciones' 409 inscripcion-cesado '{"empleadoId":4}'
request_check 'POST primera inscripcion' POST '/api/v1/programaciones-curso/4/inscripciones' 200 inscripcion-created-8 '{"empleadoId":1}'
request_check 'POST inscripcion que supera el cupo' POST '/api/v1/programaciones-curso/4/inscripciones' 409 inscripcion-sin-cupo '{"empleadoId":2}'
request_check 'POST realizar una programacion futura' POST '/api/v1/programaciones-curso/4/realizacion' 409 programacion-no-realizable
request_check 'POST retiro de la inscripcion' POST '/api/v1/programaciones-curso/4/inscripciones/8/retiro' 200 inscripcion-id-8
request_check 'POST inscripcion tras el retiro' POST '/api/v1/programaciones-curso/4/inscripciones' 200 inscripcion-created-9 '{"empleadoId":2}'
request_check 'POST cancelar programacion' POST '/api/v1/programaciones-curso/4/cancelacion' 200 programacion-id-4
request_check 'POST resultados de la programacion 1' POST '/api/v1/programaciones-curso/1/resultados' 200 programacion-id-1 '{"resultados":[{"inscripcionId":3,"aprobado":true,"nota":12.5}]}'
request_check 'POST certificacion de inscripcion no aprobada' POST '/api/v1/programaciones-curso/2/inscripciones/5/certificacion' 409 certificacion-no-aprobada '{"codigo":"SMK-0001","fechaEmision":"2026-03-11"}'
request_check 'POST certificacion' POST '/api/v1/programaciones-curso/1/inscripciones/4/certificacion' 200 certificacion-created-3 '{"codigo":"smk-0001","fechaEmision":"2026-03-11","fechaVencimiento":null}'
request_check 'POST certificacion repetida' POST '/api/v1/programaciones-curso/1/inscripciones/4/certificacion' 409 certificacion-repetida '{"codigo":"SMK-0002","fechaEmision":"2026-03-11"}'
request_check 'Programacion 1 con inscritos y certificaciones' GET '/api/v1/programaciones-curso/1' 200 programacion-1-detalle
# Modulo 70: las fechas de humo salen de hoy para que no dependan del calendario.
SMOKE_HOY="$(date -u +%Y-%m-%d)"
request_check 'OpenAPI de seguridad y salud ocupacional' GET '/openapi/v1.json' 200 openapi-sst
request_check 'Resumen SST 2026' GET '/api/v1/incidentes-sst/resumen?anio=2026' 200 sst-resumen-2026
request_check 'Incidente 2 abierto con una accion pendiente' GET '/api/v1/incidentes-sst/2' 200 sst-incidente-2-abierto
request_check 'POST equipo de proteccion' POST '/api/v1/equipos-proteccion' 200 equipo-proteccion-created-5 '{"codigo":"smk-epp","nombre":"Equipo de smoke","vidaUtilDias":30}'
request_check 'POST equipo con codigo repetido' POST '/api/v1/equipos-proteccion' 409 equipo-proteccion-duplicado '{"codigo":"SMK-EPP","nombre":"Otro equipo","vidaUtilDias":30}'
request_check 'POST entrega de un equipo inactivo' POST '/api/v1/entregas-epp' 409 epp-equipo-inactivo "{\"equipoId\":4,\"empleadoId\":1,\"fechaEntrega\":\"${SMOKE_HOY}\",\"cantidad\":1}"
request_check 'POST entrega a un empleado cesado' POST '/api/v1/entregas-epp' 409 epp-empleado-cesado "{\"equipoId\":5,\"empleadoId\":4,\"fechaEntrega\":\"${SMOKE_HOY}\",\"cantidad\":1}"
request_check 'POST entrega de EPP' POST '/api/v1/entregas-epp' 200 entrega-epp-created-4 "{\"equipoId\":5,\"empleadoId\":1,\"fechaEntrega\":\"${SMOKE_HOY}\",\"cantidad\":2}"
request_check 'POST examen de retiro fuera de plazo' POST '/api/v1/examenes-medicos' 409 examen-retiro-fuera-de-plazo "{\"empleadoId\":4,\"tipo\":\"Retiro\",\"fecha\":\"${SMOKE_HOY}\",\"resultado\":\"Apto\"}"
request_check 'POST examen periodico' POST '/api/v1/examenes-medicos' 200 examen-medico-created-5 "{\"empleadoId\":1,\"tipo\":\"Periodico\",\"fecha\":\"${SMOKE_HOY}\",\"resultado\":\"Apto\",\"fechaVencimiento\":\"2099-12-31\",\"clinica\":\"Clinica de smoke\"}"
request_check 'POST incidente' POST '/api/v1/incidentes-sst' 200 incidente-sst-created-3 "{\"empleadoId\":1,\"tipo\":\"IncidenteSinLesion\",\"severidad\":\"Leve\",\"fecha\":\"${SMOKE_HOY}\",\"lugar\":\"Planta de smoke\",\"descripcion\":\"Incidente de smoke\"}"
request_check 'POST accion correctiva' POST '/api/v1/incidentes-sst/3/acciones' 200 accion-correctiva-created-3 '{"descripcion":"Revisar la zona","responsable":"Jefe de smoke","fechaCompromiso":"2099-12-31"}'
request_check 'POST cierre con una accion pendiente' POST '/api/v1/incidentes-sst/2/cierre' 409 incidente-cierre-pendientes
request_check 'POST completar la accion pendiente' POST '/api/v1/incidentes-sst/2/acciones/2/completar' 200 accion-correctiva-id-2 "{\"fechaCompletada\":\"${SMOKE_HOY}\"}"
request_check 'POST cierre del incidente 2' POST '/api/v1/incidentes-sst/2/cierre' 200 incidente-sst-id-2
request_check 'POST accion en un incidente cerrado' POST '/api/v1/incidentes-sst/2/acciones' 409 incidente-cerrado-sin-cambios '{"descripcion":"Otra accion","responsable":"Jefe","fechaCompromiso":"2099-12-31"}'
request_check 'Incidente 2 cerrado' GET '/api/v1/incidentes-sst/2' 200 sst-incidente-2-cerrado

request_check 'OpenAPI de listas de materiales' GET '/openapi/v1.json' 200 openapi-listas-materiales
request_check 'Versiones de la lista de materiales del producto 1' GET '/api/v1/listas-materiales?productoId=1' 200 listas-materiales-seed
request_check 'Rechazar versión repetida' POST '/api/v1/listas-materiales' 409 lista-material-version-duplicada '{"productoId":1,"version":1,"componentes":[{"componenteProductoId":3,"cantidad":1,"unidadMedida":"unidad"}]}'
request_check 'Crear lista de materiales en borrador' POST '/api/v1/listas-materiales' 201 lista-material-creada '{"productoId":3,"version":1,"componentes":[{"componenteProductoId":5,"cantidad":1,"unidadMedida":"unidad"}]}'
LISTA_MATERIAL_SMOKE="/api/v1/listas-materiales/${LISTA_MATERIAL_CREADA_ID}"
LISTA_MATERIAL_EDICION='{"componentes":[{"componenteProductoId":6,"cantidad":0.123456,"unidadMedida":"unidad"}]}'
request_check 'Editar lista de materiales en borrador' PUT "$LISTA_MATERIAL_SMOKE" 200 '' "$LISTA_MATERIAL_EDICION"
request_check 'Activar lista de materiales' POST "$LISTA_MATERIAL_SMOKE/activar" 200 ''
request_check 'Bloquear edición de lista activa' PUT "$LISTA_MATERIAL_SMOKE" 409 lista-material-edicion-bloqueada "$LISTA_MATERIAL_EDICION"
request_check 'Retirar lista de materiales' POST "$LISTA_MATERIAL_SMOKE/retirar" 200 ''
request_check 'Consultar lista de materiales retirada' GET "$LISTA_MATERIAL_SMOKE" 200 lista-material-retirada

TITANGLOBAL_SMOKE_BOM_TOKEN="$TOKEN_ACTUAL" python3 - "$BASE_URL/api/v1/listas-materiales" <<'PY'
import json
import os
import sys
from concurrent.futures import ThreadPoolExecutor
from threading import Barrier
from urllib.error import HTTPError
from urllib.request import Request, urlopen

url = sys.argv[1]
headers = {
    "Authorization": f"Bearer {os.environ['TITANGLOBAL_SMOKE_BOM_TOKEN']}",
    "Content-Type": "application/json",
}

def peticion(metodo, ruta="", datos=None):
    contenido = None if datos is None else json.dumps(datos).encode()
    solicitud = Request(url + ruta, data=contenido, headers=headers, method=metodo)
    try:
        with urlopen(solicitud, timeout=10) as respuesta:
            return respuesta.status, json.load(respuesta)
    except HTTPError as respuesta:
        return respuesta.code, json.load(respuesta)

def componentes(cantidad):
    return [{"componenteProductoId": 6, "cantidad": cantidad, "unidadMedida": "unidad"}]

def crear(producto, version):
    estado, respuesta = peticion("POST", datos={
        "productoId": producto, "version": version, "componentes": componentes(1),
    })
    assert estado == 201, respuesta
    return respuesta["id"]

def en_paralelo(primera, segunda):
    barrera = Barrier(2)
    def ejecutar(argumentos):
        barrera.wait(timeout=10)
        return peticion(*argumentos)
    with ThreadPoolExecutor(max_workers=2) as ejecutor:
        return list(ejecutor.map(ejecutar, [primera, segunda]))

primera = crear(2, 1)
segunda = crear(2, 2)
resultados = en_paralelo(("POST", f"/{primera}/activar"), ("POST", f"/{segunda}/activar"))
assert sorted(estado for estado, _ in resultados) == [200, 409], resultados
conflicto = next(cuerpo for estado, cuerpo in resultados if estado == 409)
assert conflicto["Message"] == "El producto 2 ya tiene una lista de materiales activa.", conflicto
estado, activas = peticion("GET", "?productoId=2&estado=Activa")
assert estado == 200 and activas["total"] == 1, activas

id_lista = crear(4, 1)
activacion, edicion = en_paralelo(
    ("POST", f"/{id_lista}/activar"),
    ("PUT", f"/{id_lista}", {"componentes": componentes(2)}),
)
assert activacion[0] == 200 and edicion[0] in (200, 409), (activacion, edicion)
if edicion[0] == 409:
    assert edicion[1]["Message"] == "Solo se pueden editar listas de materiales en borrador.", edicion
estado, lista = peticion("GET", f"/{id_lista}")
assert estado == 200 and lista["estado"] == "Activa", lista
assert [detalle["cantidad"] for detalle in lista["componentes"]] == [2 if edicion[0] == 200 else 1], lista
print("Concurrencia de listas de materiales OK")
PY


request_check 'OpenAPI de rutas de producción' GET '/openapi/v1.json' 200 openapi-rutas-produccion
request_check 'Versiones de ruta del producto 1' GET '/api/v1/rutas-produccion?productoId=1' 200 rutas-produccion-seed
OPERACION_RUTA_SMOKE='{"secuencia":1,"nombre":"Ensamblar","centroTrabajoId":'"${CENTRO_TRABAJO_SEED_ID}"',"minutosPreparacion":0.25,"minutosPorUnidad":2.75}'
request_check 'Rechazar versión duplicada de ruta' POST '/api/v1/rutas-produccion' 409 ruta-produccion-version-duplicada '{"productoId":1,"version":1,"operaciones":['"$OPERACION_RUTA_SMOKE"']}'
request_check 'Crear borrador de ruta' POST '/api/v1/rutas-produccion' 201 ruta-produccion-creada '{"productoId":3,"version":1,"operaciones":[{"secuencia":1,"nombre":"Ensamblar","centroTrabajoId":'"${CENTRO_TRABAJO_SEED_ID}"',"minutosPreparacion":0,"minutosPorUnidad":1}]}'
RUTA_PRODUCCION_SMOKE="/api/v1/rutas-produccion/${RUTA_PRODUCCION_CREADA_ID}"
RUTA_PRODUCCION_EDICION='{"operaciones":['"$OPERACION_RUTA_SMOKE"']}'
request_check 'Editar operaciones del borrador' PUT "$RUTA_PRODUCCION_SMOKE" 200 created "$RUTA_PRODUCCION_EDICION"
request_check 'Activar ruta de producción' POST "$RUTA_PRODUCCION_SMOKE/activar" 200 created
request_check 'Bloquear edición de ruta activa' PUT "$RUTA_PRODUCCION_SMOKE" 409 ruta-produccion-edicion-bloqueada "$RUTA_PRODUCCION_EDICION"
request_check 'Retirar ruta de producción' POST "$RUTA_PRODUCCION_SMOKE/retirar" 200 created
request_check 'Consultar la ruta retirada' GET "$RUTA_PRODUCCION_SMOKE" 200 ruta-produccion-retirada
request_check 'Proteger el centro usado por rutas' DELETE "/api/v1/centros-trabajo/${CENTRO_TRABAJO_SEED_ID}" 409 centro-en-uso-ruta

TITANGLOBAL_SMOKE_RUTAS_TOKEN="$TOKEN_ACTUAL" python3 - "$BASE_URL/api/v1/rutas-produccion" "$CENTRO_TRABAJO_SEED_ID" <<'PY'
import json
import os
import sys
from concurrent.futures import ThreadPoolExecutor
from threading import Barrier
from urllib.error import HTTPError
from urllib.request import Request, urlopen

url = sys.argv[1]
centro_id = int(sys.argv[2])
headers = {
    "Authorization": f"Bearer {os.environ['TITANGLOBAL_SMOKE_RUTAS_TOKEN']}",
    "Content-Type": "application/json",
}

def peticion(metodo, ruta="", datos=None):
    contenido = None if datos is None else json.dumps(datos).encode()
    solicitud = Request(url + ruta, data=contenido, headers=headers, method=metodo)
    try:
        with urlopen(solicitud, timeout=15) as respuesta:
            return respuesta.status, json.load(respuesta)
    except HTTPError as respuesta:
        return respuesta.code, json.load(respuesta)

def operaciones(minutos):
    return [{
        "secuencia": 1, "nombre": "Montar", "centroTrabajoId": centro_id,
        "minutosPreparacion": 0, "minutosPorUnidad": minutos,
    }]

def crear(producto, version):
    estado, respuesta = peticion("POST", datos={
        "productoId": producto, "version": version, "operaciones": operaciones(1),
    })
    assert estado == 201, respuesta
    return respuesta["id"]

def en_paralelo(primera, segunda):
    barrera = Barrier(2)

    def ejecutar(argumentos):
        barrera.wait(timeout=10)
        return peticion(*argumentos)

    with ThreadPoolExecutor(max_workers=2) as ejecutor:
        return list(ejecutor.map(ejecutar, (primera, segunda)))

primera = crear(2, 1)
segunda = crear(2, 2)
resultados = en_paralelo(("POST", f"/{primera}/activar"), ("POST", f"/{segunda}/activar"))
assert sorted(estado for estado, _ in resultados) == [200, 409], resultados
conflicto = next(cuerpo for estado, cuerpo in resultados if estado == 409)
assert conflicto["Message"] == "El producto 2 ya tiene una ruta de producción activa.", conflicto
estado, activas = peticion("GET", "?productoId=2&estado=Activa")
assert estado == 200 and activas["total"] == 1, activas

id_ruta = crear(4, 1)
activacion, edicion = en_paralelo(
    ("POST", f"/{id_ruta}/activar"),
    ("PUT", f"/{id_ruta}", {"operaciones": operaciones(2)}),
)
assert activacion[0] == 200 and edicion[0] in (200, 409), (activacion, edicion)
if edicion[0] == 409:
    assert edicion[1]["Message"] == "Solo se pueden editar rutas de producción en borrador.", edicion
estado, ruta = peticion("GET", f"/{id_ruta}")
assert estado == 200 and ruta["estado"] == "Activa", ruta
assert [op["minutosPorUnidad"] for op in ruta["operaciones"]] == [2 if edicion[0] == 200 else 1], ruta
print("Concurrencia de rutas de producción OK")
PY
# Módulo 43: maestro independiente de rutas, sin modificar el resto de módulos.
TOKEN_43_ADMIN="$TOKEN_ACTUAL"
TOKEN_ACTUAL=''
request_check 'M43 exige sesión' GET '/api/v1/rutas-despacho' 401 no-session
TOKEN_ACTUAL="$TOKEN_43_ADMIN"
request_check 'M43 OpenAPI siete operaciones' GET '/openapi/v1.json' 200 m43-openapi
request_check 'M43 rutas del seed' GET '/api/v1/rutas-despacho' 200 m43-list
request_check 'M43 filtro false' GET '/api/v1/rutas-despacho?activo=false' 200 m43-inactivas
request_check 'M43 zonas distintas' GET '/api/v1/rutas-despacho/zonas' 200 m43-zonas
request_check 'M43 resumen sin mezclar monedas' GET '/api/v1/rutas-despacho/resumen' 200 m43-resumen
request_check 'M43 filtros combinados' GET '/api/v1/rutas-despacho?zona=norte&activo=true&busqueda=GUAYAQUIL' 200 m43-filtro
request_check 'M43 página enorme sin desbordar' GET '/api/v1/rutas-despacho?pagina=2147483647' 200 m43-vacia
request_check 'M43 crear con máximos y false' POST '/api/v1/rutas-despacho' 200 m43-created '{"codigo":" SMK-43 ","nombre":"Ruta Smoke","zona":"Prueba","origen":"Lima","destino":"Callao","distanciaKm":99999999.99,"peajeEstimado":9999999999999999.99,"moneda":" usd ","activo":false,"observaciones":" "}'
request_check 'M43 precisión y normalización persistidas' GET "/api/v1/rutas-despacho/${RUTA_43_ID}" 200 m43-maximos
request_check 'M43 código duplicado normalizado' POST '/api/v1/rutas-despacho' 409 m43-duplicate '{"codigo":" smk-43 ","nombre":"Duplicada","zona":"Centro","origen":"Lima","destino":"Callao","distanciaKm":1,"peajeEstimado":0,"moneda":"PEN"}'
request_check 'M43 editar código propio y peaje cero' PUT "/api/v1/rutas-despacho/${RUTA_43_ID}" 200 created '{"codigo":"SMK-43","nombre":"Ruta Smoke editada","zona":"Prueba","origen":"Lima","destino":"Callao","distanciaKm":25.50,"peajeEstimado":0,"moneda":"USD","activo":true,"observaciones":" Editada "}'
request_check 'M43 peaje cero conservado' GET "/api/v1/rutas-despacho/${RUTA_43_ID}" 200 m43-cero
request_check 'M43 distancia positiva' POST '/api/v1/rutas-despacho' 400 m43-distancia '{"codigo":"INVALIDA","nombre":"Ruta","zona":"Centro","origen":"Lima","destino":"Callao","distanciaKm":0,"peajeEstimado":0,"moneda":"PEN"}'
request_check 'M43 distancia sin redondeo' POST '/api/v1/rutas-despacho' 400 m43-distancia-decimales '{"codigo":"INVALIDA","nombre":"Ruta","zona":"Centro","origen":"Lima","destino":"Callao","distanciaKm":1.001,"peajeEstimado":0,"moneda":"PEN"}'
request_check 'M43 peaje sin redondeo' POST '/api/v1/rutas-despacho' 400 m43-peaje-decimales '{"codigo":"INVALIDA","nombre":"Ruta","zona":"Centro","origen":"Lima","destino":"Callao","distanciaKm":1,"peajeEstimado":0.001,"moneda":"PEN"}'
request_check 'M43 peaje no negativo' POST '/api/v1/rutas-despacho' 400 m43-peaje-negativo '{"codigo":"INVALIDA","nombre":"Ruta","zona":"Centro","origen":"Lima","destino":"Callao","distanciaKm":1,"peajeEstimado":-1,"moneda":"PEN"}'
# Verificar restricciones sin pasar por las prevalidaciones de Application.
docker compose exec -T db psql -U titanglobal -d titanglobal -v ON_ERROR_STOP=1 <<'SQL'
DO $$
BEGIN
    BEGIN
        INSERT INTO "RutasDespacho" ("Codigo", "Nombre", "Zona", "Origen", "Destino", "DistanciaKm", "PeajeEstimado", "Moneda")
        VALUES (' smk-43 ', 'SQL', 'Centro', 'Lima', 'Callao', 1, 0, 'PEN');
        RAISE EXCEPTION 'M43: falta unicidad normalizada del código';
    EXCEPTION WHEN unique_violation THEN NULL;
    END;
    BEGIN
        INSERT INTO "RutasDespacho" ("Codigo", "Nombre", "Zona", "Origen", "Destino", "DistanciaKm", "PeajeEstimado", "Moneda")
        VALUES ('SQL-INVALIDA', 'SQL', 'Centro', 'Lima', 'Callao', 0, 0, 'PEN');
        RAISE EXCEPTION 'M43: falta restricción de distancia positiva';
    EXCEPTION WHEN check_violation THEN NULL;
    END;
    BEGIN
        INSERT INTO "RutasDespacho" ("Codigo", "Nombre", "Zona", "Origen", "Destino", "DistanciaKm", "PeajeEstimado", "Moneda")
        VALUES ('SQL-INVALIDA', 'SQL', 'Centro', 'Lima', 'Callao', 1, -1, 'PEN');
        RAISE EXCEPTION 'M43: falta restricción de peaje no negativo';
    EXCEPTION WHEN check_violation THEN NULL;
    END;
END $$;
SQL
request_check 'M43 eliminar ruta sin asociaciones' DELETE "/api/v1/rutas-despacho/${RUTA_43_ID}" 204 ''
request_check 'M43 confirmar baja' GET "/api/v1/rutas-despacho/${RUTA_43_ID}" 404 m43-missing
# Módulo 44: picking, packing, precisión e historial real.
TOKEN_44_ADMIN="$TOKEN_ACTUAL"
TOKEN_ACTUAL=''
request_check 'M44 exige sesión' GET '/api/v1/consolidaciones-carga' 401 no-session
TOKEN_ACTUAL="$TOKEN_44_ADMIN"
request_check 'M44 OpenAPI ocho operaciones' GET '/openapi/v1.json' 200 m44-openapi
request_check 'M44 seed y totales' GET '/api/v1/consolidaciones-carga' 200 m44-list
request_check 'M44 resumen' GET '/api/v1/consolidaciones-carga/resumen' 200 m44-resumen
request_check 'M44 máximos exactos' POST '/api/v1/consolidaciones-carga' 200 m44-created '{"codigo":"SMK-44","nombre":"Carga smoke","lineas":[{"sku":"SMK-44","descripcion":"Manual","cantidad":1000000,"cantidadRecogida":0,"unidadesPorBulto":1,"largoCm":9999.99,"anchoCm":9999.99,"altoCm":9999.99,"pesoBultoKg":999999.99}]}'
request_check 'M44 exactitud persistida' GET "/api/v1/consolidaciones-carga/${CARGA_44_ID}" 200 m44-maximos
request_check 'M44 picking incompleto' POST "/api/v1/consolidaciones-carga/${CARGA_44_ID}/confirmacion" 409 m44-picking
request_check 'M44 código normalizado duplicado' POST '/api/v1/consolidaciones-carga' 409 m44-duplicado '{"codigo":"smk-44","nombre":"Carga smoke","lineas":[{"sku":"SMK-44","descripcion":"Manual","cantidad":10,"cantidadRecogida":10,"unidadesPorBulto":4,"largoCm":50,"anchoCm":40,"altoCm":30,"pesoBultoKg":2}]}'
request_check 'M44 editar packing incompleto' PUT "/api/v1/consolidaciones-carga/${CARGA_44_ID}" 200 created '{"codigo":"SMK-44","nombre":"Carga smoke","lineas":[{"sku":"SMK-44","descripcion":"Manual","cantidad":10,"cantidadRecogida":10,"unidadesPorBulto":0,"largoCm":50,"anchoCm":40,"altoCm":30,"pesoBultoKg":2}]}'
request_check 'M44 packing incompleto' POST "/api/v1/consolidaciones-carga/${CARGA_44_ID}/confirmacion" 409 m44-packing
request_check 'M44 no redondea entradas' PUT "/api/v1/consolidaciones-carga/${CARGA_44_ID}" 400 m44-decimales '{"codigo":"SMK-44","nombre":"Carga smoke","lineas":[{"sku":"SMK-44","descripcion":"Manual","cantidad":10,"cantidadRecogida":10,"unidadesPorBulto":4,"largoCm":1.001,"anchoCm":40,"altoCm":30,"pesoBultoKg":2}]}'
request_check 'M44 reemplazo de líneas completo' PUT "/api/v1/consolidaciones-carga/${CARGA_44_ID}" 200 created '{"codigo":"SMK-44","nombre":"Carga smoke","lineas":[{"sku":"SMK-44","descripcion":"Manual","cantidad":10,"cantidadRecogida":10,"unidadesPorBulto":4,"largoCm":50,"anchoCm":40,"altoCm":30,"pesoBultoKg":2}]}'
request_check 'M44 confirmar sin cuerpo' POST "/api/v1/consolidaciones-carga/${CARGA_44_ID}/confirmacion" 200 created
request_check 'M44 fecha UTC y packing conservados' GET "/api/v1/consolidaciones-carga/${CARGA_44_ID}" 200 m44-confirmada
request_check 'M44 no confirma dos veces' POST "/api/v1/consolidaciones-carga/${CARGA_44_ID}/confirmacion" 409 m44-confirmacion
request_check 'M44 no edita terminal' PUT "/api/v1/consolidaciones-carga/${CARGA_44_ID}" 409 m44-edicion '{}'
request_check 'M44 no elimina terminal' DELETE "/api/v1/consolidaciones-carga/${CARGA_44_ID}" 409 m44-baja
request_check 'M44 exige motivo' POST "/api/v1/consolidaciones-carga/${CARGA_44_ID}/cancelacion" 400 m44-motivo '{"motivo":" "}'
request_check 'M44 cancela conservando historial' POST "/api/v1/consolidaciones-carga/${CARGA_44_ID}/cancelacion" 200 created '{"motivo":"  Cambio de plan  "}'
request_check 'M44 historial UTC y líneas' GET "/api/v1/consolidaciones-carga/${CARGA_44_ID}" 200 m44-cancelada
request_check 'M44 cancelación repetida' POST "/api/v1/consolidaciones-carga/${CARGA_44_ID}/cancelacion" 409 m44-repetida '{"motivo":"Repetida"}'
request_check 'M44 filtrar canceladas' GET '/api/v1/consolidaciones-carga?estado=cancelada' 200 m44-filtro
request_check 'M44 borrador para baja' POST '/api/v1/consolidaciones-carga' 200 m44-baja-created '{"codigo":"SMK-BAJA-44","nombre":"Carga smoke","lineas":[{"sku":"SMK-BAJA-44","descripcion":"Manual","cantidad":10,"cantidadRecogida":10,"unidadesPorBulto":4,"largoCm":50,"anchoCm":40,"altoCm":30,"pesoBultoKg":2}]}'
request_check 'M44 baja con cascada' DELETE "/api/v1/consolidaciones-carga/${BAJA_44_ID}" 204 ''
request_check 'M44 confirmar baja' GET "/api/v1/consolidaciones-carga/${BAJA_44_ID}" 404 m44-missing
docker compose exec -T db psql -U titanglobal -d titanglobal -v ON_ERROR_STOP=1 <<'SQL'
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "LineasConsolidacionCarga" WHERE "Sku" = 'SMK-BAJA-44') THEN RAISE EXCEPTION 'M44: baja sin cascada'; END IF;
    BEGIN
        INSERT INTO "ConsolidacionesCarga" ("Codigo","Nombre") VALUES (' smk-44 ','SQL');
        RAISE EXCEPTION 'M44: falta unicidad normalizada';
    EXCEPTION WHEN unique_violation THEN NULL;
    END;
    BEGIN
        INSERT INTO "LineasConsolidacionCarga" ("ConsolidacionCargaId","Posicion","Sku","Descripcion","Cantidad") VALUES (2147483647,1,'FK','FK',1);
        RAISE EXCEPTION 'M44: falta FK';
    EXCEPTION WHEN foreign_key_violation THEN NULL;
    END;
    BEGIN
        INSERT INTO "LineasConsolidacionCarga" ("ConsolidacionCargaId","Posicion","Sku","Descripcion","Cantidad","CantidadRecogida") SELECT "Id",1,'CHECK','CHECK',1,2 FROM "ConsolidacionesCarga" WHERE "Codigo" = 'SMK-44';
        RAISE EXCEPTION 'M44: falta límite de recogida';
    EXCEPTION WHEN check_violation THEN NULL;
    END;
END $$;
SQL
printf 'SMOKE OK\n'
