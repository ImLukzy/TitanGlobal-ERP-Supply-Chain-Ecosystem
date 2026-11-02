#!/usr/bin/env python3
"""Lleva código de la estructura antigua (src/TitanGlobal.Api/Modules/...) a las cuatro capas.

Uso habitual, para un compañero con una rama empezada antes de la migración:

    git fetch origin
    git switch -c tu-nombre/moduloXX-nombre-capas origin/main
    python3 scripts/adaptar-rama-capas.py rama origin/tu-nombre/moduloXX-nombre

Reproduce los cambios de esa rama en las rutas nuevas sin modificarla y regenera su
migración. No hace commit ni push. Detalle: docs/equipo/adaptar-rama-a-capas.md.

El modo `arbol` migra el repositorio completo; se usó una vez (spec 50).
"""
import argparse
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

API = "src/TitanGlobal.Api"
DOMAIN = "src/TitanGlobal.Domain"
APPLICATION = "src/TitanGlobal.Application"
INFRASTRUCTURE = "src/TitanGlobal.Infrastructure"

TIPOS_SOLO_API = (
    "ErrorHandlingMiddleware",
    "ParameterValidationMiddleware",
    "RoleBasedAccessMiddleware",
    "PermitirCuerpoVacio",
    "JsonSerializationOptions",
)


def mapear_ruta(ruta):
    """Devuelve la ruta nueva de un archivo; si no se mueve, la misma."""
    reglas = [
        (rf"^{API}/Models/(TitanGlobalDbContext[^/]*\.cs)$", rf"{INFRASTRUCTURE}/Persistence/\1"),
        (rf"^{API}/Models/(.+)$", rf"{DOMAIN}/Models/\1"),
        (rf"^{API}/Repositories/Interfaces/(.+)$", rf"{DOMAIN}/Repositories/\1"),
        (rf"^{API}/Repositories/Implementations/(.+)$", rf"{INFRASTRUCTURE}/Repositories/Implementation/\1"),
        (rf"^{API}/Migrations/(.+)$", rf"{INFRASTRUCTURE}/Migrations/\1"),
        (rf"^{API}/Middlewares/ReglaNegocioException\.cs$", rf"{APPLICATION}/Exceptions/ReglaNegocioException.cs"),
        (rf"^{API}/Configuration/FechasUtc\.cs$", rf"{DOMAIN}/Common/FechasUtc.cs"),
        (rf"^{API}/Modules/([^/]+)/Controllers/(.+)$", rf"{API}/Controllers/\1/\2"),
        (rf"^{API}/Modules/([^/]+)/Dtos/(.+)$", rf"{APPLICATION}/DTOs/\1/\2"),
        (rf"^{API}/Modules/([^/]+)/UseCases/(.+)$", rf"{APPLICATION}/UseCases/\1/\2"),
        (rf"^{API}/Modules/([^/]+)/(.+)$", rf"{APPLICATION}/UseCases/\1/\2"),
    ]
    for patron, destino in reglas:
        if re.match(patron, ruta):
            return re.sub(patron, destino, ruta)
    return ruta


def asegurar_using(texto, espacio):
    linea = f"using {espacio};"
    if re.search(rf"^{re.escape(linea)}\s*$", texto, re.MULTILINE):
        return texto
    usings = list(re.finditer(r"^using [\w.]+;[ \t]*\n", texto, re.MULTILINE))
    if usings:
        fin = usings[-1].end()
        return texto[:fin] + linea + "\n" + texto[fin:]
    return linea + "\n\n" + texto


def quitar_using(texto, espacio):
    return re.sub(rf"^using {re.escape(espacio)};[ \t]*\n", "", texto, flags=re.MULTILINE)


def reescribir_postgres(texto):
    """Sustituye DbUpdateException + PostgresException por ViolacionRestriccionException."""
    if "Postgres" not in texto and "DbUpdateException" not in texto:
        return texto
    alias = {}
    asignacion = (
        r"[ \t]*(?:Npgsql\.)?PostgresException\??\s+(\w+)\s*=\s*(\w+)\.InnerException"
        r"\s+as\s+(?:Npgsql\.)?PostgresException;[ \t]*\n"
    )
    for coincidencia in re.finditer(asignacion, texto):
        alias[coincidencia.group(1)] = coincidencia.group(2)
    texto = re.sub(asignacion, "", texto)
    patron_is = r"(\w+)\.InnerException\s+is\s+(?:Npgsql\.)?PostgresException\s+(\w+)"
    for coincidencia in re.finditer(patron_is, texto):
        alias[coincidencia.group(2)] = coincidencia.group(1)
    texto = re.sub(patron_is + r"\s*&&\s*", "", texto)
    for nombre, origen in alias.items():
        texto = re.sub(rf"\b{nombre}\?\.", f"{origen}.", texto)
        texto = re.sub(rf"\b{nombre}\.", f"{origen}.", texto)
    unicidad = r'(?:(?:Npgsql\.)?PostgresErrorCodes\.UniqueViolation|"23505")'
    foranea = r'(?:(?:Npgsql\.)?PostgresErrorCodes\.ForeignKeyViolation|"23503")'
    texto = re.sub(rf"(\w+)\??\.SqlState\s*==\s*{unicidad}", r"\1.Tipo == TipoViolacionRestriccion.Unicidad", texto)
    texto = re.sub(rf"(\w+)\??\.SqlState\s*==\s*{foranea}", r"\1.Tipo == TipoViolacionRestriccion.ReferenciaForanea", texto)
    texto = re.sub(r"\bConstraintName\b", "Restriccion", texto)
    texto = re.sub(r"\bDbUpdateException\b", "ViolacionRestriccionException", texto)
    return quitar_using(texto, "Npgsql")


def reescribir_namespaces_modulos(texto):
    texto = re.sub(r"TitanGlobal\.Api\.Modules\.(Modulo\d+_\w+)\.Controllers", r"TitanGlobal.Api.Controllers.\1", texto)
    texto = re.sub(r"TitanGlobal\.Api\.Modules\.(Modulo\d+_\w+)\.Dtos", r"TitanGlobal.Application.DTOs.\1", texto)
    texto = re.sub(r"TitanGlobal\.Api\.Modules\.(Modulo\d+_\w+)\.UseCases", r"TitanGlobal.Application.UseCases.\1", texto)
    return re.sub(r"TitanGlobal\.Api\.Modules\.(Modulo\d+_\w+)", r"TitanGlobal.Application.UseCases.\1", texto)


def reescribir_cs(texto, destino):
    """Reescribe un .cs según la capa en la que acaba."""
    en_application = destino.startswith(APPLICATION)
    en_persistencia = destino.startswith(f"{INFRASTRUCTURE}/Persistence/")
    en_migraciones = destino.startswith(f"{INFRASTRUCTURE}/Migrations/")
    es_contexto = "TitanGlobalDbContext" in texto

    texto = texto.replace("\r\n", "\n")
    texto = re.sub(r"TitanGlobal\.Api\.Models\.TitanGlobalDbContext", "TitanGlobal.Infrastructure.Persistence.TitanGlobalDbContext", texto)
    texto = reescribir_namespaces_modulos(texto)
    texto = texto.replace("TitanGlobal.Api.Repositories.Interfaces", "TitanGlobal.Domain.Repositories")
    texto = texto.replace("TitanGlobal.Api.Repositories.Implementations", "TitanGlobal.Infrastructure.Repositories.Implementation")
    texto = texto.replace("TitanGlobal.Api.Migrations", "TitanGlobal.Infrastructure.Migrations")
    texto = texto.replace("TitanGlobal.Api.Middlewares.ReglaNegocioException", "ReglaNegocioException")

    if en_persistencia:
        texto = texto.replace("namespace TitanGlobal.Api.Models;", "namespace TitanGlobal.Infrastructure.Persistence;")
        texto = asegurar_using(texto, "TitanGlobal.Domain.Models")
    else:
        texto = texto.replace("namespace TitanGlobal.Api.Models;", "namespace TitanGlobal.Domain.Models;")

    if re.search(r"^using TitanGlobal\.Api\.Models;", texto, re.MULTILINE):
        texto = quitar_using(texto, "TitanGlobal.Api.Models")
        if not en_migraciones:
            texto = asegurar_using(texto, "TitanGlobal.Domain.Models")
        if es_contexto:
            texto = asegurar_using(texto, "TitanGlobal.Infrastructure.Persistence")
    texto = texto.replace("TitanGlobal.Api.Models.", "TitanGlobal.Domain.Models.")

    if destino == f"{APPLICATION}/Exceptions/ReglaNegocioException.cs":
        texto = texto.replace("namespace TitanGlobal.Api.Middlewares;", "namespace TitanGlobal.Application.Exceptions;")
    if destino == f"{DOMAIN}/Common/FechasUtc.cs":
        texto = texto.replace("namespace TitanGlobal.Api.Configuration;", "namespace TitanGlobal.Domain.Common;")

    if "FechasUtc" in texto and destino != f"{DOMAIN}/Common/FechasUtc.cs":
        texto = quitar_using(texto, "TitanGlobal.Api.Configuration")
        texto = asegurar_using(texto, "TitanGlobal.Domain.Common")

    if en_application:
        texto = reescribir_postgres(texto)
        texto = re.sub(r"\bStatusCodes\.Status", "CodigosEstado.Status", texto)
        texto = quitar_using(texto, "Microsoft.AspNetCore.Http")
        texto = quitar_using(texto, "TitanGlobal.Api.Middlewares")

    usa_excepciones = re.search(r"\b(ReglaNegocioException|ViolacionRestriccionException|CodigosEstado)\b", texto)
    if usa_excepciones and not destino.startswith(f"{APPLICATION}/Exceptions/"):
        if not any(tipo in texto for tipo in TIPOS_SOLO_API):
            texto = quitar_using(texto, "TitanGlobal.Api.Middlewares")
        texto = asegurar_using(texto, "TitanGlobal.Application.Exceptions")
    return texto


def reescribir_markdown(texto):
    """Actualiza las rutas que cita una spec nueva de la rama."""
    texto = re.sub(r"src/TitanGlobal\.Api/Modules/(Modulo\d+_\w+)/Controllers", rf"{API}/Controllers/\1", texto)
    texto = re.sub(r"src/TitanGlobal\.Api/Modules/(Modulo\d+_\w+)/Dtos", rf"{APPLICATION}/DTOs/\1", texto)
    texto = re.sub(r"src/TitanGlobal\.Api/Modules/(Modulo\d+_\w+)/UseCases", rf"{APPLICATION}/UseCases/\1", texto)
    texto = re.sub(r"src/TitanGlobal\.Api/Models/TitanGlobalDbContext", f"{INFRASTRUCTURE}/Persistence/TitanGlobalDbContext", texto)
    texto = re.sub(r"src/TitanGlobal\.Api/Models/", f"{DOMAIN}/Models/", texto)
    texto = re.sub(r"src/TitanGlobal\.Api/Migrations", f"{INFRASTRUCTURE}/Migrations", texto)
    return re.sub(r"(docs/specs/|specs/|\()\d+-(modulo\d+-)", r"\1\2", texto)


def ruta_spec(ruta):
    """Las specs nuevas de módulo pierden el número global para no chocar."""
    return re.sub(r"^docs/specs/\d+-(modulo\d+-.+\.md)$", r"docs/specs/\1", ruta)


def reescribir(contenido, destino):
    if destino.endswith(".cs"):
        return reescribir_cs(contenido, destino)
    if destino.endswith(".md"):
        return reescribir_markdown(contenido)
    return contenido


def git(*argumentos, comprobar=True):
    resultado = subprocess.run(["git", *argumentos], capture_output=True)
    if comprobar and resultado.returncode != 0:
        sys.exit(f"git {' '.join(argumentos)}: {resultado.stderr.decode('utf-8', 'replace').strip()}")
    return resultado.stdout


def leer_de_git(revision, ruta):
    resultado = subprocess.run(["git", "show", f"{revision}:{ruta}"], capture_output=True)
    return resultado.stdout if resultado.returncode == 0 else None


def a_texto(contenido):
    try:
        return contenido.decode("utf-8-sig")
    except UnicodeDecodeError:
        return None


def modo_arbol():
    for ruta in git("ls-files").decode("utf-8").splitlines():
        nueva = mapear_ruta(ruta)
        if nueva != ruta:
            Path(nueva).parent.mkdir(parents=True, exist_ok=True)
            git("mv", ruta, nueva)
    for ruta in git("ls-files", "src", "tests").decode("utf-8").splitlines():
        if ruta.endswith(".cs") and Path(ruta).exists():
            archivo = Path(ruta)
            original = archivo.read_bytes().decode("utf-8-sig")
            nuevo = reescribir_cs(original, ruta)
            archivo.write_bytes(nuevo.encode("utf-8"))
    print("Árbol migrado a cuatro capas. Revisa con: dotnet build")


def fusionar(actual, base, suyo):
    """Fusión a tres bandas. Devuelve (contenido, hay_conflicto)."""
    with tempfile.TemporaryDirectory() as carpeta:
        rutas = {}
        for nombre, contenido in (("main", actual), ("base", base), ("rama", suyo)):
            rutas[nombre] = Path(carpeta) / nombre
            rutas[nombre].write_bytes(contenido)
        resultado = subprocess.run(
            ["git", "merge-file", "-p", "-L", "main", "-L", "base", "-L", "tu rama",
             str(rutas["main"]), str(rutas["base"]), str(rutas["rama"])],
            capture_output=True,
        )
        return resultado.stdout, resultado.returncode != 0


def nombre_migracion(nombres):
    return "".join(nombre[0].upper() + nombre[1:] for nombre in nombres).replace("_", "")


def ejecutar(comando):
    print(f"\n$ {' '.join(comando)}")
    return subprocess.run(comando).returncode == 0


def modo_rama(rama, sin_dotnet):
    if not Path(f"{DOMAIN}/Models").is_dir():
        sys.exit("Ejecuta esto en una rama nueva creada desde origin/main (ya en cuatro capas).")
    if git("status", "--porcelain").strip():
        sys.exit("Tienes cambios sin confirmar. Confírmalos o guárdalos antes de empezar.")

    base = git("merge-base", rama, "HEAD").decode("utf-8").strip()
    print(f"Rama: {rama}\nBase común: {base[:7]}\n")

    conflictos = []
    migraciones = []
    avisos = []
    lineas = git("diff", "--name-status", "--no-renames", base, rama).decode("utf-8").splitlines()
    for linea in lineas:
        estado, ruta = linea.split("\t", 1)
        if ruta.startswith(f"{API}/Migrations/"):
            nombre = re.match(r"^\d+_(.+)\.cs$", Path(ruta).name)
            if estado == "A" and nombre and not ruta.endswith(".Designer.cs"):
                migraciones.append(nombre.group(1))
            continue
        destino = ruta_spec(mapear_ruta(ruta)) if estado == "A" else mapear_ruta(ruta)
        archivo = Path(destino)

        if estado == "D":
            if archivo.exists():
                archivo.unlink()
                avisos.append(f"borrado (como en tu rama): {destino}")
            continue

        suyo = leer_de_git(rama, ruta)
        if ruta.endswith((".csproj", ".sln")):
            avisos.append(f"sin aplicar, revísalo a mano: {ruta}")
            continue
        texto_suyo = a_texto(suyo)

        if estado == "A":
            contenido = suyo if texto_suyo is None else reescribir(texto_suyo, destino).encode("utf-8")
            if archivo.exists() and archivo.read_bytes() != contenido:
                conflictos.append(destino)
                contenido, _ = fusionar(archivo.read_bytes(), b"", contenido)
            archivo.parent.mkdir(parents=True, exist_ok=True)
            archivo.write_bytes(contenido)
            print(f"  nuevo      {destino}")
            continue

        base_contenido = leer_de_git(base, ruta) or b""
        texto_base = a_texto(base_contenido)
        if texto_suyo is None or texto_base is None:
            avisos.append(f"binario, copiado de tu rama: {destino}")
            archivo.write_bytes(suyo)
            continue
        if not archivo.exists():
            avisos.append(f"ya no existe en main, copiado de tu rama: {destino}")
            archivo.parent.mkdir(parents=True, exist_ok=True)
            archivo.write_bytes(reescribir(texto_suyo, destino).encode("utf-8"))
            continue
        contenido, hay_conflicto = fusionar(
            archivo.read_bytes(),
            reescribir(texto_base, destino).encode("utf-8"),
            reescribir(texto_suyo, destino).encode("utf-8"),
        )
        archivo.write_bytes(contenido)
        print(f"  {'CONFLICTO ' if hay_conflicto else 'fusionado '} {destino}")
        if hay_conflicto:
            conflictos.append(destino)

    restantes = subprocess.run(
        ["git", "grep", "--untracked", "-lE", r"\bPostgresException\b|\bNpgsql\b|\bDbUpdateException\b", "--", f"{APPLICATION}/*"],
        capture_output=True,
    ).stdout.decode("utf-8").split()

    for aviso in avisos:
        print(f"Aviso: {aviso}")
    if restantes:
        print("\nHay que revisar a mano el acceso a PostgreSQL en:")
        for ruta in restantes:
            print(f"  {ruta}")

    if conflictos:
        print("\nConflictos por resolver (busca <<<<<<< en cada archivo):")
        for ruta in conflictos:
            print(f"  {ruta}")

    nombre = nombre_migracion(migraciones)
    reglas_correctas = True
    if not conflictos:
        reglas = subprocess.run(["bash", "scripts/check-rules.sh"], capture_output=True, text=True)
        reglas_correctas = reglas.returncode == 0
        if not reglas_correctas:
            print("\nEl código de la rama incumple las reglas de capas:")
            print(reglas.stdout)
            print("Una entidad de Domain no puede usar EF Core: pasa sus [Index] y [Precision] a OnModelCreating.")
            print("Consulta docs/equipo/adaptar-rama-a-capas.md.")

    compilado = False
    migracion_generada = False
    if not sin_dotnet and not conflictos and reglas_correctas:
        proyectos = ["-p", INFRASTRUCTURE, "-s", API]
        compilado = ejecutar(["dotnet", "build", "--nologo", "-v", "q"])
        if compilado and nombre:
            migracion_generada = ejecutar(["dotnet", "tool", "restore"]) and ejecutar(["dotnet", "ef", "migrations", "add", nombre, *proyectos])
        if compilado and (migracion_generada or not nombre):
            ejecutar(["dotnet", "format"])
        else:
            print("\nLa compilación o la migración fallaron: corrige el error y sigue los pasos de abajo.")

    print("\nSiguientes pasos:")
    pasos = []
    if conflictos:
        pasos.append("Resuelve los conflictos de arriba.")
    if not reglas_correctas:
        pasos.append("Corrige las reglas de capas de arriba.")
    if nombre and not migracion_generada:
        pasos.append(f"dotnet ef migrations add {nombre} -p {INFRASTRUCTURE} -s {API}")
    pasos.append("dotnet format && ./scripts/verify.sh")
    if nombre:
        pasos.append("./scripts/smoke-postgres.sh")
    pasos.append("git add -A && git commit y abre el pull request contra main.")
    for numero, paso in enumerate(pasos, 1):
        print(f"  {numero}. {paso}")
    print(f"\nTu rama original ({rama}) no se ha modificado.")
    if sin_dotnet:
        return 1 if conflictos or not reglas_correctas else 0
    return 0 if compilado and (migracion_generada or not nombre) else 1


def main():
    if not shutil.which("git"):
        sys.exit("Falta git en el PATH.")
    analizador = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    modos = analizador.add_subparsers(dest="modo", required=True)
    modos.add_parser("arbol", help="migra el repositorio completo (ya aplicado en main)")
    modo = modos.add_parser("rama", help="reproduce una rama antigua sobre main")
    modo.add_argument("rama", help="por ejemplo origin/tu-nombre/modulo41-vehiculos")
    modo.add_argument("--sin-dotnet", action="store_true", help="no compila ni regenera la migración")
    argumentos = analizador.parse_args()
    if argumentos.modo == "arbol":
        modo_arbol()
        return 0
    return modo_rama(argumentos.rama, argumentos.sin_dotnet)


if __name__ == "__main__":
    sys.exit(main())
