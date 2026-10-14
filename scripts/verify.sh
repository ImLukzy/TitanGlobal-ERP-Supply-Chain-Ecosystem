#!/usr/bin/env bash
set -euo pipefail
dotnet restore
dotnet build -warnaserror --no-restore
dotnet format --verify-no-changes --no-restore
./scripts/check-rules.sh
for rule in a b c d e; do
    ./scripts/check-rules.sh scripts/fixtures/rules/good-"$rule"
    if ./scripts/check-rules.sh scripts/fixtures/rules/bad-"$rule"; then
        echo "bad fixture for rule $rule passed unexpectedly"
        exit 1
    fi
done
dotnet test --no-build
npm --prefix frontend ci --no-audit --no-fund
npm --prefix frontend run verify
