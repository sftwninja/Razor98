#!/usr/bin/env bash
# Builds win98/dist/Razor. Needs dotnet 8+, llvm-mingw (msvcrt), python3,
# make, curl, zip.
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
root="$(dirname "$here")"
config="${CONFIG:-Release}"
bin="$here/bin/$config"
dist="$here/dist/Razor"
dotnet="${DOTNET:-$(command -v dotnet || echo "$HOME/.dotnet/dotnet")}"

echo "== managed (net20)"
"$dotnet" build "$here/Razor/Razor.csproj" -c "$config" -nologo -v quiet
# otherwise ngen on 2.0 fails with E_INVALIDARG, see tools/UnshareStrings
"$dotnet" build "$here/tools/UnshareStrings" -c Release -nologo -v quiet
"$dotnet" "$here/tools/UnshareStrings/bin/UnshareStrings.dll" "$bin/FastColoredTextBox.dll" "$root/FastColoredTextBox/FCTB_key.snk"
"$dotnet" "$here/tools/UnshareStrings/bin/UnshareStrings.dll" "$bin/Razor.exe"

echo "== native (msvcrt, i586)"
make -C "$here/native" CONFIG="$config" --no-print-directory -s

echo "== package"
rm -rf "$dist"
mkdir -p "$dist"
cp "$bin"/{Razor.exe,Razor.exe.config,FastColoredTextBox.dll,Crypt.dll,Loader.dll,Platform.dll,zlib.dll} "$dist/"
# etc/ minus installer stuff and DLLs that don't load on 98
for f in "$root"/etc/*; do
    case "$(basename "$f")" in
        zlib.dll|unrar.dll|Razor.nsi|dotNet.nsh) ;;
        *) cp -r "$f" "$dist/" ;;
    esac
done
cp "$here/../docs/win98/README.md" "$dist/README-WIN98.txt"
cp "$here/package/"* "$dist/"
# CRLF for Notepad
find "$dist" -maxdepth 1 -name '*.txt' -exec sed -i 's/\r*$/\r/' {} +

# Razor-update: same minus Razor.exe.config and counters.xml, which hold
# the user's servers and counters.
update="$here/dist/Razor-update"
rm -rf "$update"
cp -r "$dist" "$update"
rm -f "$update/Razor.exe.config" "$update/counters.xml"

( cd "$here/dist" && rm -f Razor-win98.zip && zip -qr Razor-win98.zip Razor )
echo "== done: $dist ($(du -sh "$dist" | cut -f1)), $here/dist/Razor-win98.zip"
