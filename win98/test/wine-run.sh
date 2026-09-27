#!/usr/bin/env bash
# Run dist/Razor under Wine with .NET 2.0. Set up the prefix first:
#   WINEPREFIX=$PWD/win98/test/wineprefix winetricks -q dotnet20
set -e
here="$(cd "$(dirname "$0")/.." && pwd)"
export WINEPREFIX="${WINEPREFIX:-$here/test/wineprefix}"
app="$(mktemp -d)"
cp -r "$here/dist/Razor/." "$app"
# Wine's CLR looks for Razor.config, not Razor.exe.config
cp "$app/Razor.exe.config" "$app/Razor.config"
cd "$app" && exec wine Razor.exe
