#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet restore --verbosity quiet
echo "## Vulnérabilités connues (directes et transitives)"
dotnet list package --vulnerable --include-transitive
echo "## Paquets obsolètes"
dotnet list package --outdated
echo "## Licences et fraîcheur"
dotnet run scripts/PackageReport.cs
