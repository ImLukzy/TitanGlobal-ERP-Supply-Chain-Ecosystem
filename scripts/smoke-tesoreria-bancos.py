from concurrent.futures import ThreadPoolExecutor
from contextlib import contextmanager
from datetime import datetime, timezone
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
HOY = datetime.now(timezone.utc).date().isoformat()
SALDO_INSUFICIENTE = "La cuenta bancaria no tiene saldo suficiente."


def ejecutar_sql(sentencia, comprobar=True):
    return subprocess.run(
        PSQL, input=sentencia + "\n", capture_output=True, text=True,
        encoding="utf-8", check=comprobar, timeout=10,
    )


def consultar_sql(sentencia):
    return ejecutar_sql(sentencia).stdout.strip()


def rechazar_sql(sentencia, codigo, restriccion):
    resultado = ejecutar_sql(f"BEGIN;\n{sentencia}\nROLLBACK;", comprobar=False)
    assert resultado.returncode != 0 and f"{codigo}:" in resultado.stderr and restriccion in resultado.stderr, (
        restriccion, resultado.stderr,
    )


class TesoreriaSmoke:
    def __init__(self, base_url, token):
        self.ruta = base_url + "/api/v1/tesoreria-bancos"
        self.cabeceras = {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}

    def solicitar(self, metodo, sufijo, datos=None):
        cuerpo = json.dumps(datos).encode("utf-8") if datos is not None else None
        peticion = Request(self.ruta + sufijo, data=cuerpo, headers=self.cabeceras, method=metodo)
        try:
            with build_opener(ProxyHandler({})).open(peticion, timeout=30) as respuesta:
                return respuesta.status, json.load(respuesta, parse_float=Decimal)
        except HTTPError as error:
            with error:
                return error.code, json.load(error)

    def exigir(self, estado, metodo, sufijo, datos=None):
        resultado, cuerpo = self.solicitar(metodo, sufijo, datos)
        assert resultado == estado, (estado, resultado, sufijo, cuerpo)
        return cuerpo

    def crear_cuenta(self, numero):
        cuenta = self.exigir(201, "POST", "/cuentas", datos_cuenta(numero))
        return int(cuenta["id"])

    def saldo(self, cuenta):
        return self.exigir(200, "GET", f"/cuentas/{cuenta}")["saldo"]

    def movimiento(self, cuenta, tipo, importe):
        return self.exigir(201, "POST", ruta_movimientos(cuenta), datos_movimiento(tipo, importe))

    def historial(self, cuenta):
        return self.exigir(200, "GET", ruta_movimientos(cuenta))["items"]


def datos_cuenta(numero, moneda="PEN"):
    return {"banco": "BANCO SMOKE 55", "numero": numero, "nombre": "Cuenta de prueba", "tipo": "Corriente", "moneda": moneda}


def datos_movimiento(tipo, importe):
    return {"tipo": tipo, "importe": importe, "fechaOperacion": HOY}


def datos_transferencia(origen, destino, importe, referencia):
    return {
        "cuentaOrigenId": origen, "cuentaDestinoId": destino, "importe": importe,
        "fechaOperacion": HOY, "referencia": referencia,
    }


def ruta_movimientos(cuenta):
    return f"/cuentas/{cuenta}/movimientos"


@contextmanager
def bloqueo_cuentas(cuentas):
    ids = ", ".join(str(int(cuenta)) for cuenta in sorted(cuentas))
    with subprocess.Popen(PSQL, stdin=subprocess.PIPE, stdout=subprocess.PIPE, text=True, encoding="utf-8") as proceso:
        fin = "ROLLBACK"
        try:
            proceso.stdin.write(f"""
BEGIN;
SET LOCAL statement_timeout = '12s';
DO $$ BEGIN
    PERFORM "Id" FROM "CuentasBancarias" WHERE "Id" IN ({ids}) ORDER BY "Id" FOR UPDATE;
END $$;
SELECT 'listo';
""")
            proceso.stdin.flush()
            assert proceso.stdout.readline().strip() == "listo", "M55 no obtuvo los bloqueos de prueba"
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
        assert proceso.returncode == 0, "M55 falló al liberar los bloqueos de prueba"


def concurrencia(api, cuentas, *operaciones):
    espera = """SELECT count(*) FROM pg_stat_activity
        WHERE datname = current_database() AND wait_event_type = 'Lock'
          AND query LIKE 'SELECT "Id" FROM "CuentasBancarias"%FOR UPDATE%';"""
    with ThreadPoolExecutor(max_workers=len(operaciones)) as peticiones:
        with bloqueo_cuentas(cuentas):
            pendientes = [peticiones.submit(api.solicitar, *operacion) for operacion in operaciones]
            limite = time.monotonic() + 12
            while int(consultar_sql(espera)) < len(operaciones):
                assert time.monotonic() < limite, "M55 las dos peticiones deben esperar los bloqueos en PostgreSQL"
                time.sleep(0.1)
        return [peticion.result(timeout=15) for peticion in pendientes]


def error(estado, literal):
    return estado, {"StatusCode": estado, "Message": literal}


def verificar_par(transferencia):
    identificador = int(transferencia["id"])
    filas = consultar_sql(f"""SELECT movimiento."Tipo", movimiento."Id"
        FROM "MovimientosBancarios" movimiento
        JOIN "TransferenciasBancarias" transferencia ON transferencia."Id" = movimiento."TransferenciaBancariaId"
        WHERE transferencia."Id" = {identificador} AND movimiento."Importe" = transferencia."Importe"
          AND movimiento."FechaOperacion" = transferencia."FechaOperacion"
          AND movimiento."Referencia" IS NOT DISTINCT FROM transferencia."Referencia"
          AND movimiento."FechaReversion" IS NULL
          AND movimiento."CuentaBancariaId" = CASE movimiento."Tipo"
              WHEN 'Egreso' THEN transferencia."CuentaOrigenId" ELSE transferencia."CuentaDestinoId" END
        ORDER BY movimiento."Tipo";""").splitlines()
    assert filas == [
        f'Egreso|{transferencia["movimientoEgresoId"]}',
        f'Ingreso|{transferencia["movimientoIngresoId"]}',
    ], filas


def probar_restricciones():
    origen = int(consultar_sql("""SELECT "Id" FROM "CuentasBancarias" WHERE "Banco" = 'BANCO DEMO 55' AND "Numero" = '00055001';"""))
    destino = int(consultar_sql("""SELECT "Id" FROM "CuentasBancarias" WHERE "Banco" = 'BANCO DEMO 55' AND "Numero" = '00055002';"""))
    rechazar_sql("""INSERT INTO "CuentasBancarias" ("Banco", "Numero", "Nombre", "Tipo", "Moneda")
        VALUES ('BANCO DEMO 55', '00055001', 'Duplicado', 'Corriente', 'PEN');""",
                 "23505", "IX_CuentasBancarias_BancoNumero")
    rechazar_sql(f"""UPDATE "CuentasBancarias" SET "Moneda" = 'EUR' WHERE "Id" = {origen};""",
                 "23514", "CK_CuentasBancarias_Moneda")
    rechazar_sql(f"""UPDATE "CuentasBancarias" SET "Tipo" = 'Otro' WHERE "Id" = {origen};""",
                 "23514", "CK_CuentasBancarias_Tipo")

    for importe in ("'NaN'::numeric", "0", "-1"):
        rechazar_sql(f"""INSERT INTO "MovimientosBancarios" ("CuentaBancariaId", "Tipo", "Importe", "FechaOperacion")
            VALUES ({origen}, 'Ingreso', {importe}, CURRENT_DATE);""",
                     "23514", "CK_MovimientosBancarios_Importe")
        rechazar_sql(f"""INSERT INTO "TransferenciasBancarias" ("CuentaOrigenId", "CuentaDestinoId", "Importe", "FechaOperacion")
            VALUES ({origen}, {destino}, {importe}, CURRENT_DATE);""",
                     "23514", "CK_TransferenciasBancarias_Importe")

    rechazar_sql(f"""INSERT INTO "TransferenciasBancarias" ("CuentaOrigenId", "CuentaDestinoId", "Importe", "FechaOperacion")
        VALUES ({origen}, {origen}, 1, CURRENT_DATE);""", "23514", "CK_TransferenciasBancarias_CuentasDistintas")
    rechazar_sql(f"""INSERT INTO "MovimientosBancarios" ("CuentaBancariaId", "Tipo", "Importe", "FechaOperacion")
        VALUES ({origen}, 'Otro', 1, CURRENT_DATE);""", "23514", "CK_MovimientosBancarios_Tipo")
    rechazar_sql("""INSERT INTO "MovimientosBancarios" ("CuentaBancariaId", "Tipo", "Importe", "FechaOperacion")
        VALUES (-1, 'Ingreso', 1, CURRENT_DATE);""", "23503", "FK_MovimientosBancarios_CuentaBancaria")
    rechazar_sql("""INSERT INTO "MovimientosBancarios" ("CuentaBancariaId", "Tipo", "Importe", "FechaOperacion", "TransferenciaBancariaId")
        SELECT "CuentaOrigenId", 'Egreso', "Importe", "FechaOperacion", "Id" FROM "TransferenciasBancarias"
        WHERE "Referencia" = 'SEED55-TRANSFERENCIA';""", "23505", "IX_MovimientosBancarios_TransferenciaTipo")
    rechazar_sql("""UPDATE "MovimientosBancarios" SET "FechaReversion" = now(), "MotivoReversion" = 'Prueba SQL'
        WHERE "Referencia" = 'SEED55-TRANSFERENCIA' AND "Tipo" = 'Egreso';""",
                 "23514", "CK_MovimientosBancarios_Reversion")
    print("M55 PostgreSQL verificó unicidad, FKs, tipos, NaN, importes positivos y bloqueo de reversión parcial")


def probar_egresos(api):
    origen = api.crear_cuenta("SM55-EGRESOS")
    api.movimiento(origen, "Ingreso", 100)
    egreso = ("POST", ruta_movimientos(origen), datos_movimiento("Egreso", 70))
    resultados = concurrencia(api, [origen], egreso, egreso)
    assert sorted(estado for estado, _ in resultados) == [201, 409], resultados
    assert error(409, SALDO_INSUFICIENTE) in resultados and api.saldo(origen) == 30
    assert [fila["tipo"] for fila in api.historial(origen)] == ["Egreso", "Ingreso"]
    print("M55 PostgreSQL serializó dos egresos sobre el mismo saldo")


def probar_transferencias_mismo_origen(api):
    origen = api.crear_cuenta("SM55-ORIGEN")
    destino_a = api.crear_cuenta("SM55-DESTINO-A")
    destino_b = api.crear_cuenta("SM55-DESTINO-B")
    api.movimiento(origen, "Ingreso", 100)
    resultados = concurrencia(
        api, [origen, destino_a, destino_b],
        ("POST", "/transferencias", datos_transferencia(origen, destino_a, 70, "M55-DOBLE-TRASLADO")),
        ("POST", "/transferencias", datos_transferencia(origen, destino_b, 70, "M55-DOBLE-TRASLADO")),
    )
    assert sorted(estado for estado, _ in resultados) == [201, 409], resultados
    assert error(409, SALDO_INSUFICIENTE) in resultados
    verificar_par(next(cuerpo for estado, cuerpo in resultados if estado == 201))
    assert api.saldo(origen) == 30 and sorted([api.saldo(destino_a), api.saldo(destino_b)]) == [0, 70]
    assert consultar_sql("""SELECT count(*) FROM "TransferenciasBancarias" WHERE "Referencia" = 'M55-DOBLE-TRASLADO';""") == "1"
    print("M55 PostgreSQL registró solo una de dos transferencias simultáneas desde el mismo origen")


def probar_transferencias_opuestas(api):
    cuenta_a = api.crear_cuenta("SM55-OPUESTA-A")
    cuenta_b = api.crear_cuenta("SM55-OPUESTA-B")
    api.movimiento(cuenta_a, "Ingreso", 100)
    api.movimiento(cuenta_b, "Ingreso", 100)
    resultados = concurrencia(
        api, [cuenta_a, cuenta_b],
        ("POST", "/transferencias", datos_transferencia(cuenta_a, cuenta_b, 70, "M55-OPUESTA-A")),
        ("POST", "/transferencias", datos_transferencia(cuenta_b, cuenta_a, 60, "M55-OPUESTA-B")),
    )
    assert [estado for estado, _ in resultados] == [201, 201], resultados
    for _, transferencia in resultados:
        verificar_par(transferencia)
    assert api.saldo(cuenta_a) == 90 and api.saldo(cuenta_b) == 110
    print("M55 PostgreSQL confirmó ambas transferencias opuestas con sus pares, sin deadlock")


def probar_reversiones(api):
    cuenta = api.crear_cuenta("SM55-REVERSION")
    ingreso = api.movimiento(cuenta, "Ingreso", 50)
    ruta = f'{ruta_movimientos(cuenta)}/{ingreso["id"]}/revertir'
    resultados = concurrencia(
        api, [cuenta],
        ("POST", ruta, {"motivo": "Primera corrección"}),
        ("POST", ruta, {"motivo": "Segunda corrección"}),
    )
    assert sorted(estado for estado, _ in resultados) == [200, 409], resultados
    assert error(409, "El movimiento bancario ya fue revertido.") in resultados
    actualizado = next(cuerpo for estado, cuerpo in resultados if estado == 200)
    assert actualizado["fechaReversion"].endswith("Z") and actualizado["id"] == ingreso["id"]
    assert api.historial(cuenta) == [actualizado] and api.saldo(cuenta) == 0
    print("M55 PostgreSQL permitió solo una reversión concurrente y preservó el movimiento histórico")


def probar_cambio_moneda(api):
    numero = "SM55-MONEDA"
    cuenta = api.crear_cuenta(numero)
    cambio, ingreso = concurrencia(
        api, [cuenta],
        ("PUT", f"/cuentas/{cuenta}", datos_cuenta(numero, "USD")),
        ("POST", ruta_movimientos(cuenta), datos_movimiento("Ingreso", 50)),
    )
    assert ingreso[0] == 201, ingreso
    detalle = api.exigir(200, "GET", f"/cuentas/{cuenta}")
    if cambio[0] == 200:
        assert detalle["moneda"] == ingreso[1]["moneda"] == "USD"
    else:
        assert cambio == error(409, "No se puede cambiar la moneda de una cuenta con movimientos."), cambio
        assert detalle["moneda"] == ingreso[1]["moneda"] == "PEN"
    assert detalle["saldo"] == 50 and api.historial(cuenta) == [ingreso[1]]
    print("M55 PostgreSQL serializó el cambio de moneda con el primer movimiento")


def probar_rollback(api):
    origen = api.crear_cuenta("SM55-ROLLBACK-A")
    destino = api.crear_cuenta("SM55-ROLLBACK-B")
    api.movimiento(origen, "Ingreso", 100)
    api.movimiento(destino, "Ingreso", 20)
    # Solo después de insertar la primera pata, provoca una FK inválida en la segunda.
    consultar_sql("""
CREATE FUNCTION m55_smoke_fallo_segundo_movimiento() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NEW."Referencia" = 'M55-ROLLBACK' AND EXISTS (
        SELECT 1 FROM "MovimientosBancarios" WHERE "TransferenciaBancariaId" = NEW."TransferenciaBancariaId"
    ) THEN
        NEW."CuentaBancariaId" := -1;
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER m55_smoke_fallo_segundo_movimiento BEFORE INSERT ON "MovimientosBancarios"
    FOR EACH ROW EXECUTE FUNCTION m55_smoke_fallo_segundo_movimiento();
""")
    datos = datos_transferencia(origen, destino, 30, "M55-ROLLBACK")
    try:
        resultado = api.solicitar("POST", "/transferencias", datos)
        assert resultado == error(409, "No se pudo guardar la operación bancaria porque una referencia ya no existe."), resultado
        assert api.saldo(origen) == 100 and api.saldo(destino) == 20
        for tabla in ("TransferenciasBancarias", "MovimientosBancarios"):
            assert consultar_sql(f"""SELECT count(*) FROM "{tabla}" WHERE "Referencia" = 'M55-ROLLBACK';""") == "0", tabla
    finally:
        consultar_sql("""
DROP TRIGGER m55_smoke_fallo_segundo_movimiento ON "MovimientosBancarios";
DROP FUNCTION m55_smoke_fallo_segundo_movimiento();
""")
    verificar_par(api.exigir(201, "POST", "/transferencias", datos))
    assert api.saldo(origen) == 70 and api.saldo(destino) == 50
    print("M55 PostgreSQL devolvió 409 por FK en la segunda pata y revirtió ambas patas y la cabecera")


def main():
    api = TesoreriaSmoke(sys.argv[1], os.environ["M55_TOKEN"])
    probar_restricciones()
    probar_egresos(api)
    probar_transferencias_mismo_origen(api)
    probar_transferencias_opuestas(api)
    probar_reversiones(api)
    probar_cambio_moneda(api)
    probar_rollback(api)


if __name__ == "__main__":
    main()
