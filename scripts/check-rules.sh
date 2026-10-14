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
class_use_case = re.compile(r"\bclass\s+\w*UseCase\b")
controller_dependency = re.compile(r"\b(?:IUnitOfWork|DbContext|IGenericRepository|GenericRepository)\b")
member_expression = re.compile(
    r"^\s*(?:(?:public|private|protected|internal|static|async|virtual|override|sealed|new|partial|extern|unsafe|readonly)\s+)*"
    r"(?:[\w.<>?,\[\]]+\s+)?\w+\s*(?:\([^;{}]*\))?\s*=>"
)
accessor_expression = re.compile(r"^\s*(?:get|set|init)\s*=>")

for path in sorted(root.rglob("*.cs")):
    text = path.read_text(encoding="utf-8-sig")
    checks = [
        ("a: var usado como tipo", var_declaration, True),
        ("c: interfaz UseCase", interface_use_case, True),
        ("d: UseCase en Services", class_use_case, "Services" in path.parts),
        ("e: controlador con dependencia de persistencia", controller_dependency, "Controllers" in path.parts),
    ]

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
