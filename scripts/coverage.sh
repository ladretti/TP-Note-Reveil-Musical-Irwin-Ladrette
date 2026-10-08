#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

rm -rf artifacts/coverage
dotnet test --coverlet --coverlet-output-format cobertura \
  --coverlet-include "[ReveilMusical.*]*" \
  --coverlet-exclude "[ReveilMusical.*.Tests]*" \
  --coverlet-exclude-by-file "**/Program.cs" \
  --coverlet-exclude-by-attribute GeneratedCodeAttribute CompilerGeneratedAttribute ExcludeFromCodeCoverageAttribute \
  --results-directory artifacts/coverage
dotnet tool restore
dotnet reportgenerator -reports:"artifacts/coverage/**/coverage.cobertura.*.xml" -targetdir:artifacts/coverage/report -reporttypes:"Html;TextSummary"
cat artifacts/coverage/report/Summary.txt
