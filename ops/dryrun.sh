#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION_ROOT="$(cd "$ROOT/.." && pwd)"
PROJECT="$SOLUTION_ROOT/ZakYip.Singulation.ConsoleDemo/ZakYip.Singulation.ConsoleDemo.csproj"
echo "[dryrun] Running regression scenario"
dotnet run --project "$PROJECT" -- --regression
