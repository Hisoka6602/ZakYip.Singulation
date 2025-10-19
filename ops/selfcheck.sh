#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION_ROOT="$(cd "$ROOT/.." && pwd)"
echo "[selfcheck] dotnet build"
dotnet build "$SOLUTION_ROOT/ZakYip.Singulation.sln"
