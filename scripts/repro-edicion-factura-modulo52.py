"""Regresiones PostgreSQL de los módulos 51 y 52, ejecutadas por smoke-postgres.sh."""

from concurrent.futures import ThreadPoolExecutor
from contextlib import contextmanager
import json
import os
import re
import subprocess
import sys
import time
from urllib.error import HTTPError
from urllib.parse import urlsplit
from urllib.request import Request, urlopen


PSQL = ["docker", "compose", "exec", "-T", "db", "psql", "-X",
        "-U", "titanglobal", "-d", "titanglobal", "-v", "ON_ERROR_STOP=1", "-qAt"]
BLOQUEO_FACTURA = """query LIKE '%FROM "Facturas"%FOR UPDATE%'"""
BLOQUEO_PRODUCTO = """query LIKE '%FROM "Productos"%'"""


def consultar(sql):
    return subprocess.run(PSQL, input=sql, encoding="utf-8", stdout=subprocess.PIPE,
                          check=True, timeout=8).stdout.strip()


def esperar(comprobar, mensaje):
    limite = time.monotonic() + 15
    while time.monotonic() < limite:
        if comprobar():
            return
        time.sleep(0.15)
    raise RuntimeError(mensaje)


def clientes_en_espera(filtro):
    return int(consultar("SELECT count(*) FROM pg_stat_activity WHERE "
                         "wait_event_type = 'Lock' AND " + filtro + ";"))


@contextmanager
def bloquear(sentencia):
    with subprocess.Popen(PSQL, stdin=subprocess.PIPE, stdout=subprocess.DEVNULL, encoding="utf-8") as sesion:
        try:
            sesion.stdin.write("SET application_name = 'tg52-control';\nBEGIN;\n" + sentencia + ";\n")
            sesion.stdin.flush()
            esperar(lambda: consultar(
                "SELECT EXISTS (SELECT 1 FROM pg_stat_activity WHERE "
                "application_name = 'tg52-control' AND state = 'idle in transaction');"
            ) == "t", "La sesión de control no obtuvo el bloqueo.")
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


def resolver_con_contencion(solicitar, facturas, operaciones):
    ids = ", ".join(str(int(factura)) for factura in facturas)
    with ThreadPoolExecutor(max_workers=len(operaciones)) as peticiones:
        with bloquear(f'SELECT "Id" FROM "Facturas" WHERE "Id" IN ({ids}) ORDER BY "Id" FOR UPDATE'):
            pendientes = []
            for cantidad, operacion in enumerate(operaciones, start=1):
                pendientes.append(peticiones.submit(solicitar, *operacion))
                esperar(lambda: clientes_en_espera(BLOQUEO_FACTURA) >= cantidad,
                        f"Solo parte de las {cantidad} peticiones esperaron el bloqueo sobre Facturas.")
        return [pendiente.result() for pendiente in pendientes]


def solicitud_factura(numero, precio):
    return {
        "clienteId": 1, "tipoComprobante": "Factura", "serie": "F52-REPRO", "numero": numero,
        "fechaEmision": "2026-10-01T00:00:00Z", "moneda": "PEN",
        "detalles": [{"productoId": 1, "descripcion": "Prueba de concurrencia",
                      "cantidad": 1, "precioUnitario": precio}],
    }


def solicitud_nota(factura, tipo="Credito", cantidad=0.5, precio=10):
    return {
        "facturaId": factura["id"], "tipo": tipo, "motivo": "Prueba de concurrencia",
        "detalles": [{"detalleFacturaId": factura["detalles"][0]["id"],
                      "cantidad": cantidad, "precioUnitario": precio}],
    }


def comprobar_http(respuesta, codigo_esperado, operacion):
    codigo, cuerpo = respuesta
    assert codigo == codigo_esperado, f"{operacion}: HTTP {codigo}, se esperaba {codigo_esperado}."
    return cuerpo


def verificar(base_url):
    assert urlsplit(base_url).hostname in ("localhost", "127.0.0.1", "::1"), "Usar la API local del smoke."
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

    def crear_factura(numero, emitir=True):
        factura = comprobar_http(solicitar("POST", "/facturas", solicitud_factura(numero, 100)),
                                 201, "Crear factura")
        if emitir:
            return comprobar_http(solicitar("POST", f"/facturas/{factura['id']}/emitir"),
                                  200, "Emitir factura")
        return factura

    def crear_nota(factura, cantidad=0.5, precio=10):
        return comprobar_http(solicitar("POST", "/notas-credito-debito",
                                        solicitud_nota(factura, cantidad=cantidad, precio=precio)),
                              201, "Crear nota")

    factura = crear_factura(1, emitir=False)
    ruta_factura = f"/facturas/{factura['id']}"
    with ThreadPoolExecutor(max_workers=2) as peticiones:
        with bloquear('LOCK TABLE "Productos" IN ACCESS EXCLUSIVE MODE'):
            edicion = peticiones.submit(solicitar, "PUT", ruta_factura, solicitud_factura(1, 180))
            esperar(lambda: clientes_en_espera(BLOQUEO_PRODUCTO) == 1,
                    "La edición no quedó en espera durante el cálculo de la factura.")
            emision = peticiones.submit(solicitar, "POST", ruta_factura + "/emitir")
            esperar(lambda: clientes_en_espera(BLOQUEO_FACTURA) == 1,
                    "Regresión 51: emitir no esperó el bloqueo que mantiene la edición.")
        factura_editada = comprobar_http(edicion.result(), 200, "Editar antes de emitir")
        factura_emitida = comprobar_http(emision.result(), 200, "Emitir después de editar")
    assert factura_editada["total"] == factura_emitida["total"] == 180
    nota = crear_nota(factura_emitida)
    comprobada = comprobar_http(solicitar("POST", f"/notas-credito-debito/{nota['id']}/emitir"), 200, "Emitir nota")
    ultima = comprobar_http(solicitar("GET", ruta_factura), 200, "Leer factura tras la nota")
    assert ultima["estado"] == comprobada["estado"] == "Emitida" and ultima["total"] == 180

    factura = crear_factura(2, emitir=False)
    ruta_factura = f"/facturas/{factura['id']}"
    emitida, editada = resolver_con_contencion(solicitar, [factura["id"]], [
        ("POST", ruta_factura + "/emitir"),
        ("PUT", ruta_factura, solicitud_factura(2, 180)),
    ])
    assert comprobar_http(emitida, 200, "Emitir antes de editar")["total"] == 100
    assert comprobar_http(editada, 409, "Editar tras emitir")["Message"] == (
        "Solo se pueden modificar facturas en estado Borrador."
    )
    print("Concurrencia 51: edición/emisión serializadas en ambos órdenes.", flush=True)

    for numero, primero_nota in ((3, True), (4, False)):
        factura = crear_factura(numero)
        nota = crear_nota(factura)
        emitir_nota = ("POST", f"/notas-credito-debito/{nota['id']}/emitir")
        anular_factura = ("POST", f"/facturas/{factura['id']}/anular", {"motivo": "Prueba de concurrencia"})
        operaciones = [emitir_nota, anular_factura] if primero_nota else [anular_factura, emitir_nota]
        aceptada, rechazada = resolver_con_contencion(solicitar, [factura["id"]], operaciones)
        comprobar_http(aceptada, 200, "Primera operación")
        mensaje = comprobar_http(rechazada, 409, "Segunda operación")["Message"]
        assert mensaje == ("No se puede anular un comprobante con notas emitidas." if primero_nota
                           else "El estado de la factura no permite registrar la nota.")
        factura_final = comprobar_http(solicitar("GET", f"/facturas/{factura['id']}"), 200, "Consultar factura")
        nota_final = comprobar_http(solicitar("GET", f"/notas-credito-debito/{nota['id']}"), 200, "Consultar nota")
        assert factura_final["total"] == 100
        assert (factura_final["estado"], nota_final["estado"]) == (
            ("Emitida", "Emitida") if primero_nota else ("Anulada", "Borrador")
        )
    print("Concurrencia 51/52: emisión de nota y anulación de factura, 200/409 en ambos órdenes.", flush=True)

    for numero, cantidad, precio, mensaje in (
        (5, 0.6, 50, "El crédito supera la cantidad disponible de la línea de factura."),
        (6, 0.5, 120, "El crédito supera el importe disponible de la línea de factura."),
    ):
        factura = crear_factura(numero)
        notas = [crear_nota(factura, cantidad, precio) for _ in range(2)]
        aceptada, rechazada = resolver_con_contencion(solicitar, [factura["id"]],
            [("POST", f"/notas-credito-debito/{nota['id']}/emitir") for nota in notas])
        assert comprobar_http(aceptada, 200, "Emitir primer crédito")["estado"] == "Emitida"
        assert comprobar_http(rechazada, 409, "Emitir crédito que supera tope")["Message"] == mensaje
        respuesta = comprobar_http(solicitar("GET", f"/notas-credito-debito/{notas[1]['id']}"),
                                    200, "Consultar crédito rechazado")
        assert respuesta["estado"] == "Borrador" and respuesta["fechaEmision"] is None
    print("Concurrencia 52: topes por cantidad e importe, 200/409.", flush=True)

    facturas = [crear_factura(7), crear_factura(8)]
    for tipo, prefijo, secuencia in (("Credito", "NC", "SecuenciaNotasCredito"),
                                     ("Debito", "ND", "SecuenciaNotasDebito")):
        ultimo = int(consultar(f'SELECT last_value FROM "{secuencia}";'))
        respuestas = resolver_con_contencion(solicitar, [factura["id"] for factura in facturas],
            [("POST", "/notas-credito-debito", solicitud_nota(factura, tipo)) for factura in facturas])
        folios = [comprobar_http(respuesta, 201, "Crear nota concurrente")["numero"] for respuesta in respuestas]
        assert all(re.fullmatch(prefijo + r"-[0-9]{6,19}", folio) for folio in folios)
        assert sorted(int(folio[3:]) for folio in folios) == [ultimo + 1, ultimo + 2]
    assert consultar("""SELECT count(*) FROM "NotasCreditoDebito" WHERE "Numero" LIKE 'TMP-%';""") == "0"
    print("Concurrencia 52: folios únicos y secuencias NC/ND independientes.", flush=True)


if __name__ == "__main__":
    verificar(sys.argv[1])
