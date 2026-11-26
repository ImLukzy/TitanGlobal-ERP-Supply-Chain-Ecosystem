from concurrent.futures import ThreadPoolExecutor
from contextlib import contextmanager
from datetime import date, timedelta
from decimal import Decimal
import json
import os
import subprocess
import sys
import time
from urllib.error import HTTPError
from urllib.request import ProxyHandler, Request, build_opener


PSQL = [
    "docker", "compose", "exec", "-T", "db", "psql", "-X", "-U", "titanglobal",
    "-d", "titanglobal", "-v", "ON_ERROR_STOP=1", "-v", "VERBOSITY=verbose", "-qAt",
]


def ejecutar_sql(sql, comprobar=True):
    return subprocess.run(
        PSQL, input=sql + "\n", capture_output=True, text=True, encoding="utf-8",
        check=comprobar, timeout=10,
    )


def consultar_sql(sql):
    return ejecutar_sql(sql).stdout.strip()


def rechazar_sql(sql, codigo, restriccion):
    resultado = ejecutar_sql(f"BEGIN;\n{sql}\nROLLBACK;", comprobar=False)
    assert resultado.returncode != 0 and f"{codigo}:" in resultado.stderr and restriccion in resultado.stderr, (
        restriccion, resultado.stderr,
    )


class CuentasPorPagarSmoke:
    def __init__(self, base_url, token):
        self.ruta = base_url + "/api/v1/cuentas-por-pagar"
        self.cabeceras = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}
        self.corte = date.fromisoformat(consultar_sql("SELECT (CURRENT_TIMESTAMP AT TIME ZONE 'America/Lima')::date;"))

    def solicitar(self, metodo, ruta="", datos=None):
        cuerpo = json.dumps(datos).encode("utf-8") if datos is not None else None
        peticion = Request(self.ruta + ruta, data=cuerpo, headers=self.cabeceras, method=metodo)
        try:
            with build_opener(ProxyHandler({})).open(peticion, timeout=30) as respuesta:
                return respuesta.status, json.load(respuesta, parse_float=Decimal)
        except HTTPError as error:
            with error:
                return error.code, json.load(error)

    def exigir(self, esperado, metodo, ruta="", datos=None):
        estado, cuerpo = self.solicitar(metodo, ruta, datos)
        assert estado == esperado, (esperado, estado, ruta, cuerpo)
        return cuerpo

    def factura(self, numero, proveedor=1, moneda="PEN", importe=100, dias_vencidos=-30):
        return {
            "proveedorId": proveedor,
            "serie": "SM54",
            "numeroDocumento": numero,
            "fechaDocumento": (self.corte - timedelta(days=120)).isoformat(),
            "fechaVencimiento": (self.corte - timedelta(days=dias_vencidos)).isoformat(),
            "moneda": moneda,
            "baseImponible": importe,
            "importeImpuesto": 0,
        }

    def pago(self, importe):
        return {"importe": importe, "fechaPago": self.corte.isoformat()}

    def crear(self, numero):
        return int(self.exigir(201, "POST", datos=self.factura(numero))["id"])

    def detalle(self, cuenta):
        return self.exigir(200, "GET", f"/{cuenta}")

    def historial(self, cuenta):
        return self.exigir(200, "GET", f"/{cuenta}/pagos")["items"]


@contextmanager
def bloqueo(sql):
    with subprocess.Popen(PSQL, stdin=subprocess.PIPE, stdout=subprocess.PIPE, text=True, encoding="utf-8") as proceso:
        fin = "ROLLBACK"
        try:
            proceso.stdin.write("BEGIN;\nSET LOCAL statement_timeout = '12s';\n" + sql + "\nSELECT 'listo';\n")
            proceso.stdin.flush()
            assert proceso.stdout.readline().strip() == "listo", "M54 no obtuvo el bloqueo de prueba"
            yield
            fin = "COMMIT"
        finally:
            if proceso.poll() is None:
                proceso.stdin.write(fin + ";\n\\q\n")
                proceso.stdin.flush()
                try:
                    proceso.wait(timeout=8)
                except subprocess.TimeoutExpired:
                    proceso.kill()
                    proceso.wait()
                    raise
        assert proceso.returncode == 0, "M54 falló al liberar el bloqueo"


def bloqueo_cuenta(cuenta):
    return f"""DO $$ BEGIN
        PERFORM "Id" FROM "CuentasPorPagar" WHERE "Id" = {int(cuenta)} FOR UPDATE;
    END $$;"""


def concurrencia(api, sql_bloqueo, patron_espera, *operaciones):
    esperar = f"""SELECT count(*) FROM pg_stat_activity
        WHERE datname = current_database() AND wait_event_type = 'Lock' AND query LIKE '{patron_espera}';"""
    with ThreadPoolExecutor(max_workers=len(operaciones)) as peticiones:
        with bloqueo(sql_bloqueo):
            pendientes = [peticiones.submit(api.solicitar, *operacion) for operacion in operaciones]
            limite = time.monotonic() + 12
            while int(consultar_sql(esperar)) < len(operaciones):
                assert time.monotonic() < limite, "M54 las dos peticiones deben esperar el bloqueo en PostgreSQL"
                time.sleep(0.1)
        return [peticion.result(timeout=15) for peticion in pendientes]


ESPERA_FILA = 'SELECT "Id" FROM "CuentasPorPagar"%FOR UPDATE%'


def error(estado, literal):
    return estado, {"StatusCode": estado, "Message": literal}


def probar_restricciones():
    cuenta = int(consultar_sql("""SELECT "Id" FROM "CuentasPorPagar" WHERE "Serie" = 'F540' AND "NumeroDocumento" = '000001';"""))
    rechazar_sql(f"""INSERT INTO "CuentasPorPagar"
        ("ProveedorId", "Serie", "NumeroDocumento", "FechaDocumento", "FechaVencimiento", "Moneda", "BaseImponible", "ImporteImpuesto", "Total")
        SELECT "ProveedorId", "Serie", "NumeroDocumento", "FechaDocumento", "FechaVencimiento", "Moneda", "BaseImponible", "ImporteImpuesto", "Total"
        FROM "CuentasPorPagar" WHERE "Id" = {cuenta};""", "23505", "IX_CuentasPorPagar_ProveedorSerieNumero")
    for asignacion in (
        '"BaseImponible" = \'NaN\'::numeric', '"ImporteImpuesto" = \'NaN\'::numeric',
        '"Total" = \'NaN\'::numeric', '"BaseImponible" = -1', '"Total" = 1',
    ):
        rechazar_sql(f"""UPDATE "CuentasPorPagar" SET {asignacion} WHERE "Id" = {cuenta};""",
                     "23514", "CK_CuentasPorPagar_Importes")
    for asignacion, codigo, restriccion in (
        ('"Serie" = \'f540\'', "23514", "CK_CuentasPorPagar_Clave"),
        ('"NumeroDocumento" = \'000 001\'', "23514", "CK_CuentasPorPagar_Clave"),
        ('"Moneda" = \'EUR\'', "23514", "CK_CuentasPorPagar_Moneda"),
        ('"FechaAnulacion" = now()', "23514", "CK_CuentasPorPagar_Anulacion"),
        ('"ProveedorId" = -1', "23503", "FK_CuentasPorPagar_Proveedor"),
        ('"OrdenAbastecimientoId" = -1', "23503", "FK_CuentasPorPagar_OrdenAbastecimiento"),
    ):
        rechazar_sql(f"""UPDATE "CuentasPorPagar" SET {asignacion} WHERE "Id" = {cuenta};""", codigo, restriccion)
    for importe in ("'NaN'::numeric", "0", "-1"):
        rechazar_sql(f"""INSERT INTO "AplicacionesPago" ("CuentaPorPagarId", "Importe", "FechaPago")
            VALUES ({cuenta}, {importe}, CURRENT_DATE);""", "23514", "CK_AplicacionesPago_Importe")
    rechazar_sql("""INSERT INTO "AplicacionesPago" ("CuentaPorPagarId", "Importe", "FechaPago")
        VALUES (-1, 1, CURRENT_DATE);""", "23503", "FK_AplicacionesPago_CuentaPorPagar")
    print("M54 PostgreSQL verificó unicidad, FKs, moneda, NaN y coherencia de importes")


def probar_pagos_en_paralelo(api):
    cuenta = api.crear("DOBLE-PAGO")
    pago = ("POST", f"/{cuenta}/pagos", api.pago(70))
    resultados = concurrencia(api, bloqueo_cuenta(cuenta), ESPERA_FILA, pago, pago)
    assert sorted(estado for estado, _ in resultados) == [201, 409], resultados
    assert error(409, "El pago supera el saldo pendiente.") in resultados
    detalle = api.detalle(cuenta)
    assert detalle["saldo"] == 30 and detalle["importePagado"] == 70
    assert len(api.historial(cuenta)) == 1
    print("M54 PostgreSQL permitió solo uno de dos pagos concurrentes que juntos superaban el saldo")


def probar_reversion_en_paralelo(api):
    cuenta = api.crear("DOBLE-REVERSION")
    pago = api.exigir(201, "POST", f"/{cuenta}/pagos", api.pago(40))
    ruta = f'/{cuenta}/pagos/{pago["id"]}/revertir'
    resultados = concurrencia(
        api, bloqueo_cuenta(cuenta), ESPERA_FILA,
        ("POST", ruta, {"motivo": "Primera reversión"}), ("POST", ruta, {"motivo": "Segunda reversión"}),
    )
    assert sorted(estado for estado, _ in resultados) == [200, 409], resultados
    assert error(409, "El pago ya fue revertido.") in resultados
    revertido = next(cuerpo for estado, cuerpo in resultados if estado == 200)
    assert revertido["fechaReversion"].endswith("Z") and revertido["id"] == pago["id"]
    assert api.historial(cuenta) == [revertido] and api.detalle(cuenta)["saldo"] == 100
    print("M54 PostgreSQL serializó la reversión del mismo pago y conservó el historial")


def probar_pago_con_anulacion(api):
    cuenta = api.crear("PAGO-ANULACION")
    pago, anulacion = concurrencia(
        api, bloqueo_cuenta(cuenta), ESPERA_FILA,
        ("POST", f"/{cuenta}/pagos", api.pago(40)),
        ("POST", f"/{cuenta}/anular", {"motivo": "Factura duplicada"}),
    )
    detalle = api.detalle(cuenta)
    if pago[0] == 201:
        assert anulacion == error(409, "No se puede anular una cuenta por pagar con pagos."), anulacion
        assert not detalle["anulada"] and detalle["saldo"] == 60 and api.historial(cuenta) == [pago[1]]
    else:
        assert anulacion[0] == 200 and pago == error(409, "La cuenta por pagar está anulada."), (pago, anulacion)
        assert detalle["anulada"] and detalle["saldo"] == 0 and api.historial(cuenta) == []
    print("M54 PostgreSQL serializó pago y anulación sin dejar una cuenta anulada con pagos vigentes")


def probar_altas_duplicadas(api):
    primera = api.factura("DUPLICADA")
    segunda = dict(primera, serie=" sm 54 ", numeroDocumento=" dup li cada ")
    resultados = concurrencia(
        api, 'LOCK TABLE "CuentasPorPagar" IN SHARE MODE;', 'INSERT INTO "CuentasPorPagar"%',
        ("POST", "", primera), ("POST", "", segunda),
    )
    assert sorted(estado for estado, _ in resultados) == [201, 409], resultados
    assert error(409, "La factura del proveedor ya está registrada.") in resultados
    assert consultar_sql("""SELECT count(*) FROM "CuentasPorPagar"
        WHERE "ProveedorId" = 1 AND "Serie" = 'SM54' AND "NumeroDocumento" = 'DUPLICADA';""") == "1"
    print("M54 PostgreSQL aplicó la unicidad de facturas frente a dos altas concurrentes")


def probar_rollback(api):
    cuenta = api.crear("ROLLBACK")
    pago_original = api.exigir(201, "POST", f"/{cuenta}/pagos", api.pago(20))
    # Tras la primera inserción, provocar una violación real de FK dentro de la misma operación.
    consultar_sql(f"""
CREATE FUNCTION m54_smoke_fallo_pago() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NEW."CuentaPorPagarId" = {cuenta} AND NEW."Importe" = 30 THEN
        INSERT INTO "AplicacionesPago" ("CuentaPorPagarId", "Importe", "FechaPago")
        VALUES (-1, NEW."Importe", NEW."FechaPago");
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER m54_smoke_fallo_pago AFTER INSERT ON "AplicacionesPago"
    FOR EACH ROW EXECUTE FUNCTION m54_smoke_fallo_pago();
""")
    try:
        resultado = api.solicitar("POST", f"/{cuenta}/pagos", api.pago(30))
        assert resultado == error(409, "No se pudo guardar la operación porque una referencia ya no existe."), resultado
        assert api.detalle(cuenta)["saldo"] == 80 and api.historial(cuenta) == [pago_original]
        assert consultar_sql(f'SELECT count(*) FROM "AplicacionesPago" WHERE "CuentaPorPagarId" = {cuenta};') == "1"
    finally:
        consultar_sql("""
DROP TRIGGER m54_smoke_fallo_pago ON "AplicacionesPago";
DROP FUNCTION m54_smoke_fallo_pago();
""")
    api.exigir(201, "POST", f"/{cuenta}/pagos", api.pago(30))
    assert api.detalle(cuenta)["saldo"] == 50 and len(api.historial(cuenta)) == 2
    print("M54 PostgreSQL revirtió el pago completo ante una FK y conservó el saldo y el pago previo")


def probar_vencimientos(api):
    for dias in (0, 1, 30, 31, 60, 61, 90, 91):
        api.exigir(201, "POST", datos=api.factura(f"VENC-PEN-{dias}", proveedor=4, importe=dias or 10, dias_vencidos=dias))
    api.exigir(201, "POST", datos=api.factura("VENC-USD", proveedor=4, moneda="USD", importe=99, dias_vencidos=31))
    resumen = api.exigir(200, "GET", "/resumen?proveedorId=4")
    assert resumen["fechaCorte"] == api.corte.isoformat() and len(resumen["grupos"]) == 2
    pen, usd = resumen["grupos"]
    assert pen["moneda"] == "PEN" and usd["moneda"] == "USD" and pen["saldoAlDia"] == 10
    assert [pen[k] for k in ("vencido0A30", "vencido31A60", "vencido61A90", "vencidoMasDe90")] == [31, 91, 151, 91]
    assert pen["saldoVencido"] == 364 and usd["saldoVencido"] == usd["vencido31A60"] == 99
    print("M54 PostgreSQL verificó los límites del reporte y la separación PEN/USD")


def main():
    api = CuentasPorPagarSmoke(sys.argv[1], os.environ["M54_TOKEN"])
    probar_restricciones()
    probar_pagos_en_paralelo(api)
    probar_reversion_en_paralelo(api)
    probar_pago_con_anulacion(api)
    probar_altas_duplicadas(api)
    probar_rollback(api)
    probar_vencimientos(api)


if __name__ == "__main__":
    main()
