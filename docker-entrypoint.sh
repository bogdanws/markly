#!/usr/bin/env bash
set -euo pipefail

echo "Waiting for postgres..."
until (echo > /dev/tcp/postgres/5432) >/dev/null 2>&1; do
  sleep 1
done

echo "Running EF migrations..."
dotnet ef database update --project /src/markly.csproj --startup-project /src/markly.csproj

echo "Starting app..."
cd /app/publish
exec dotnet markly.dll
