#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION_ROOT="$(cd "$ROOT/.." && pwd)"
PUBLISH_DIR="${1:-publish/host}"
PROJECT="$SOLUTION_ROOT/ZakYip.Singulation.Host/ZakYip.Singulation.Host.csproj"
OUTPUT="$SOLUTION_ROOT/$PUBLISH_DIR"

echo "[install] Publishing ZakYip.Singulation.Host to $OUTPUT"
mkdir -p "$OUTPUT"
dotnet publish "$PROJECT" -c Release -o "$OUTPUT"
