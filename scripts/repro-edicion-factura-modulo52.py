"""Prueba opcional que se ejecuta desde smoke-postgres.sh --repro-edicion-factura."""

from concurrent.futures import ThreadPoolExecutor
from contextlib import contextmanager
import json
import os
import subprocess
import sys
import time
from urllib.error import HTTPError
from urllib.request import Request, urlopen


PSQL = ["docker", "compose", "exec", "-T", "db", "psql", "-X",
        "-U", "titanglobal", "-d", "titanglobal", "-v", "ON_ERROR_STOP=1", "-qAt"]


def consultar(sql):
    return subprocess.run(PSQL, input=sql, encoding="utf-8", stdout=subprocess.PIPE,
                          check=True, timeout=8).stdout.strip()


def esperar(comprobar, mensaje):
    limite = time.monotonic() + 15
    while time.monotonic() < limite:
        resultado = comprobar()
        if resultado:
            return resultado
        time.sleep(0.15)
    raise RuntimeError(mensaje)


@contextmanager
def bloquear_productos():
    with subprocess.Popen(PSQL, stdin=subprocess.PIPE, stdout=subprocess.DEVNULL, encoding="utf-8") as sesion:
        try:
            sesion.stdin.write("SET application_name = 'tg52-repro-edicion';\n"
                               'BEGIN;\nLOCK TABLE "Productos" IN ACCESS EXCLUSIVE MODE;\n')
            sesion.stdin.flush()
            esperar(lambda: consultar(
                "SELECT EXISTS (SELECT 1 FROM pg_stat_activity WHERE "
                "application_name = 'tg52-repro-edicion' AND state = 'idle in transaction');"
            ) == "t", "La sesión de control no obtuvo el bloqueo sobre Productos.")
            yield
        finally:
            if sesion.poll() is None:
                try:
                    sesion.stdin.write("ROLLBACK;\n\\q\n")
                    sesion.stdin.close()
                    sesion.wait(timeout=8)
                except (BrokenPipeError, subprocess.TimeoutExpired):
                    sesion.kill()
                    sesion.wait()


def solicitud_factura(precio):
    return {
        "clienteId": 1, "tipoComprobante": "Factura", "serie": "F52-REPRO", "numero": 1,
        "fechaEmision": "2026-10-01T00:00:00Z", "moneda": "PEN",
        "detalles": [{"productoId": 1, "descripcion": "Prueba de concurrencia",
                      "cantidad": 1, "precioUnitario": precio}],
    }


def comprobar_http(respuesta, codigo_esperado, operacion):
    codigo, cuerpo = respuesta
    if codigo != codigo_esperado:
        raise RuntimeError(f"{operacion} devolvió HTTP {codigo}. Se esperaba {codigo_esperado}.")
    return cuerpo


def reproducir(base_url):
    encabezados = {
        "Authorization": "Bearer " + os.environ["TITANGLOBAL_REPRO_JWT"],
        "Content-Type": "application/json",
    }

    def solicitar(metodo, ruta, datos=None):
        cuerpo = None if metodo == "GET" else b""
        if datos is not None:
            cuerpo = json.dumps(datos).encode("utf-8")
        peticion = Request(base_url + "/api/v1" + ruta, cuerpo, encabezados, method=metodo)
        try:
            with urlopen(peticion, timeout=45) as respuesta:
                return respuesta.status, json.load(respuesta)
        except HTTPError as error:
            with error:
                return error.code, json.load(error)

    def emitir_nota():
        nota = comprobar_http(solicitar("POST", "/notas-credito-debito", {
            "facturaId": factura_id, "tipo": "Credito", "serie": "NC52-REP", "numero": 1,
            "motivo": "Prueba de concurrencia", "importe": 5,
        }), 201, "Crear nota")
        ruta = f"/notas-credito-debito/{nota['id']}"
        comprobar_http(solicitar("POST", ruta + "/emitir"), 200, "Emitir nota")
        return ruta

    factura = comprobar_http(solicitar("POST", "/facturas", solicitud_factura(100)), 201, "Crear factura")
    factura_id = int(factura["id"])
    ruta_factura = f"/facturas/{factura_id}"
    consulta_emision = f"""
        SELECT CASE
            WHEN (SELECT "Estado" FROM "Facturas" WHERE "Id" = {factura_id}) = 'Emitida' THEN 'Emitida'
            WHEN EXISTS (SELECT 1 FROM pg_stat_activity WHERE wait_event_type = 'Lock'
                AND query LIKE '%FROM "Facturas"%FOR UPDATE%') THEN 'EnEspera'
            ELSE ''
        END;
    """

    with ThreadPoolExecutor(max_workers=2) as peticiones:
        with bloquear_productos():
            edicion = peticiones.submit(solicitar, "PUT", ruta_factura, solicitud_factura(180))
            esperar(lambda: consultar(
                "SELECT EXISTS (SELECT 1 FROM pg_stat_activity WHERE "
                """wait_event_type = 'Lock' AND query LIKE '%FROM "Productos"%');"""
            ) == "t", "La edición no quedó en espera al consultar Productos.")
            emision = peticiones.submit(solicitar, "POST", ruta_factura + "/emitir")
            momento_emision = esperar(lambda: consultar(consulta_emision),
                                      "La factura no se emitió ni esperó el bloqueo sobre Facturas.")
            if momento_emision == "Emitida":
                ruta_nota = emitir_nota()
        codigo_edicion, _ = edicion.result()
        comprobar_http(emision.result(), 200, "Emitir factura")

    if momento_emision == "EnEspera":
        ruta_nota = emitir_nota()
    factura = comprobar_http(solicitar("GET", ruta_factura), 200, "Consultar factura")
    nota = comprobar_http(solicitar("GET", ruta_nota), 200, "Consultar nota")
    print(f"Edición HTTP {codigo_edicion}, emisión de factura HTTP 200, "
          f"factura {factura['estado']} con total {factura['total']}, nota {nota['estado']}.", flush=True)

    edicion_valida = (momento_emision, codigo_edicion, factura["total"]) in {
        ("EnEspera", 200, 180), ("Emitida", 409, 100),
    }
    if not edicion_valida or factura["estado"] != "Emitida" or nota["estado"] != "Emitida":
        raise SystemExit("REGRESIÓN DEL 51: la edición concurrente quebró el estado o los importes del origen.")


if __name__ == "__main__":
    reproducir(sys.argv[1])
