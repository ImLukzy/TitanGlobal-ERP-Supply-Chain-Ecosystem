#!/usr/bin/env bash
set -euo pipefail

python3 - "$@" <<'PY'
from pathlib import Path
import re
import sys

root = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("src")
failed = False

var_declaration = re.compile(r"\bvar\s+(?:[A-Za-z_]\w*|\([^)]*\))\s*(?:=|\bin\b)")
interface_use_case = re.compile(r"\binterface\s+I\w*UseCase\b")
class_use_case = re.compile(r"\b(?:class|record)\s+\w*UseCase\b")
controller_dependency = re.compile(r"\b(?:IUnitOfWork|DbContext|IGenericRepository|GenericRepository)\b")
member_expression = re.compile(
    r"^\s*(?:(?:public|private|protected|internal|static|async|virtual|override|sealed|new|partial|extern|unsafe|readonly)\s+)*"
    r"(?:[\w.<>?,\[\]]+\s+)?\w+\s*(?:\([^;{}]*\))?\s*=>"
)
accessor_expression = re.compile(r"^\s*(?:get|set|init)\s*=>")
layer_dependencies = {
    "TitanGlobal.Domain": re.compile(r"\b(?:Microsoft\.EntityFrameworkCore|Npgsql|Microsoft\.AspNetCore|TitanGlobal\.(?:Application|Infrastructure|Api))\b"),
    "TitanGlobal.Application": re.compile(r"\b(?:Npgsql|Microsoft\.AspNetCore|TitanGlobal\.(?:Infrastructure|Api))\b"),
    "TitanGlobal.Infrastructure": re.compile(r"\b(?:Microsoft\.AspNetCore|TitanGlobal\.Api)\b"),
}

for path in sorted(root.rglob("*.cs")):
    text = path.read_text(encoding="utf-8-sig")
    checks = [
        ("a: var usado como tipo", var_declaration, True),
        ("c: interfaz UseCase", interface_use_case, True),
        ("d: UseCase en Services", class_use_case, "Services" in path.parts),
        ("e: controlador con dependencia de persistencia", controller_dependency, "Controllers" in path.parts),
        ("g: UseCase fuera de Application", class_use_case, any(layer in path.parts for layer in ("TitanGlobal.Api", "TitanGlobal.Infrastructure", "TitanGlobal.Domain"))),
    ]

    code = re.sub(r"//.*", "", text)
    for layer, pattern in layer_dependencies.items():
        match = pattern.search(code)
        if layer in path.parts and match:
            line = code.count("\n", 0, match.start()) + 1
            print(f"{path}:{line}: f: dependencia prohibida en {layer}")
            failed = True

    for label, pattern, enabled in checks:
        match = pattern.search(text)
        if enabled and match:
            line = text.count("\n", 0, match.start()) + 1
            print(f"{path}:{line}: {label}")
            failed = True

    for line_number, line in enumerate(text.splitlines(), 1):
        if member_expression.search(line) or accessor_expression.search(line):
            print(f"{path}:{line_number}: b: miembro con cuerpo de expresión")
            failed = True

if failed:
    sys.exit(1)
PY
