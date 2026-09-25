#!/usr/bin/env bash
# Runs the pure-C# game logic tests (no UnityEngine) with the .NET SDK and NUnit,
# so rules can be verified even when the Unity editor is not installed.
# Usage: ./Scripts/run-pure-tests.sh [dotnet test args...]
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SCRIPTS="$ROOT/Assets/_Project/Scripts"
WORK="${PURE_TEST_DIR:-${TMPDIR:-/tmp}/mirror-legacy-pure-tests}"

# Keep this list in sync when adding pure logic or pure tests (files must not use UnityEngine).
SOURCES=(
  Data/Enums.cs
  Data/CultivationEnums.cs
  Data/CharacterData.cs
  Clan/KinshipRules.cs
  Clan/MarriageMatchmaker.cs
  Clan/CharacterNames.cs
  Characters/PowerLadder.cs
  Characters/RankCatalog.cs
)
TESTS=(
  Tests/EditMode/KinshipRulesTests.cs
  Tests/EditMode/MarriageMatchmakerTests.cs
  Tests/EditMode/PowerLadderTests.cs
  Tests/EditMode/RankCatalogTests.cs
)

command -v dotnet >/dev/null || { echo "ERROR: dotnet SDK not found" >&2; exit 1; }

for f in "${SOURCES[@]}" "${TESTS[@]}"; do
  if grep -q "UnityEngine" "$SCRIPTS/$f"; then
    echo "ERROR: $f references UnityEngine and cannot run outside Unity" >&2
    exit 1
  fi
done

mkdir -p "$WORK"
{
  echo '<Project Sdk="Microsoft.NET.Sdk">'
  echo '  <PropertyGroup><TargetFramework>net8.0</TargetFramework><Nullable>disable</Nullable><LangVersion>9.0</LangVersion><IsPackable>false</IsPackable></PropertyGroup>'
  echo '  <ItemGroup>'
  for f in "${SOURCES[@]}" "${TESTS[@]}"; do echo "    <Compile Include=\"$SCRIPTS/$f\" />"; done
  echo '  </ItemGroup>'
  echo '  <ItemGroup>'
  echo '    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />'
  echo '    <PackageReference Include="NUnit" Version="3.14.0" />'
  echo '    <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />'
  echo '  </ItemGroup>'
  echo '</Project>'
} > "$WORK/PureTests.csproj"

cd "$WORK"
dotnet test "$@"
