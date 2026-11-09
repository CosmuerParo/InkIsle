#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

# Prefer a workspace-local .NET 8 install when present (used in this environment).
if [[ -x "../.dotnet/dotnet" ]]; then
    export DOTNET_ROOT="$(cd ../.dotnet && pwd)"
    export PATH="$DOTNET_ROOT:$PATH"
    export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$(cd .. && pwd)/.dotnet-home}"
    mkdir -p "$DOTNET_CLI_HOME"
fi

APP_URL="${ASPNETCORE_URLS:-http://0.0.0.0:5088}"
exec dotnet run --urls "$APP_URL" "$@"
