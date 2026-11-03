#!/bin/sh
# Compile-check Deck Party assemblies against Unity's managed DLLs (no tests are run).
cd "$(dirname "$0")"
W="$(pwd -W)"
dotnet build Core/Core.csproj -nologo -v q 2>&1 | grep -E "error CS|Build succeeded" | sort -u | sed 's/^/[Core] /' | head -40
dotnet build Runtime/Runtime.csproj -nologo -v q "-p:ExtraDefines=DECKPARTY_RELAY%3BDECKPARTY_LOCALIZATION" "-p:ExtraSources=$W/Stubs/*.cs" 2>&1 | grep -E "error CS|Build succeeded" | sort -u | sed 's/^/[Runtime] /' | head -60
[ "$1" = "runtime" ] && exit 0
dotnet build Editor/Editor.csproj -nologo -v q "-p:ExtraDefines=DECKPARTY_LOCALIZATION" "-p:EditorStubs=$W/EditorStubs/*.cs" 2>&1 | grep -E "error CS|Build succeeded" | sort -u | sed 's/^/[Editor] /' | head -60
dotnet build Juice/Juice.csproj -nologo -v q 2>&1 | grep -E "error CS|Build succeeded" | sort -u | sed 's/^/[Juice] /' | head -40
dotnet build JuiceEditor/JuiceEditor.csproj -nologo -v q 2>&1 | grep -E "error CS|Build succeeded" | sort -u | sed 's/^/[JuiceEditor] /' | head -40
