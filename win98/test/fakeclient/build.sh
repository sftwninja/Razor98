#!/usr/bin/env bash
# Builds the fake client.exe against the i586 runtime.
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
rt="$here/../../obj/rt-i586"
llvm_mingw="${LLVM_MINGW:-$(ls -d "$HOME"/.local/share/llvm-mingw-*-msvcrt-* | sort | tail -1)}"
[ -f "$rt/lib/libmingwex.a" ] || "$here/../../native/build-runtime.sh"
cc="$llvm_mingw/bin/i686-w64-mingw32-clang"
# -resource-dir at link time only, it swaps clang's headers too
"$cc" -march=i586 -O2 -c -o "$here/fakeclient.o" "$here/fakeclient.c"
"$cc" -mwindows -s -B"$rt/lib" -L"$rt/lib" -resource-dir "$rt/clang" \
    -Wl,--major-os-version=4 -Wl,--minor-os-version=0 \
    -Wl,--major-subsystem-version=4 -Wl,--minor-subsystem-version=0 \
    -o "$here/client.exe" "$here/fakeclient.o"
python3 "$here/../../tools/check-imports.py" "$here/client.exe"
python3 "$here/../../tools/check-isa.py" "$llvm_mingw/bin/llvm-objdump" "$here/client.exe"
