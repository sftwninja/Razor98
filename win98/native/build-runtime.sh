#!/usr/bin/env bash
# Rebuild mingw-w64 CRT + compiler-rt builtins for i586 into ../obj/rt-i586.
# llvm-mingw's prebuilt ones are -march=pentium4 (SSE2/CMOV), which crashes
# the client on a PII.
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
obj="$(cd "$here/.." && pwd)/obj"
src="$obj/rt-src"
rt="$obj/rt-i586"
llvm_mingw="${LLVM_MINGW:-$(ls -d "$HOME"/.local/share/llvm-mingw-*-msvcrt-* 2>/dev/null | sort | tail -1)}"
[ -x "$llvm_mingw/bin/clang" ] || { echo "build-runtime: llvm-mingw not found (set LLVM_MINGW)" >&2; exit 1; }

# pinned to match llvm-mingw 20260922's headers and clang
MINGW_REPO=https://github.com/mingw-w64/mingw-w64
MINGW_COMMIT=4564ee4b5063097bf747af3a3f8270a28adff820
LLVM_REPO=https://github.com/llvm/llvm-project
LLVM_TAG=llvmorg-23.1.2

target=i686-w64-mingw32
arch_flags="-march=i586 -mtune=generic"
cc="$llvm_mingw/bin/$target-clang"
jobs="$(nproc)"

mkdir -p "$src" "$rt"

if [ ! -d "$src/mingw-w64/.git" ]; then
    git init -q "$src/mingw-w64"
    git -C "$src/mingw-w64" fetch -q --depth 1 "$MINGW_REPO" "$MINGW_COMMIT"
    git -C "$src/mingw-w64" checkout -q FETCH_HEAD
fi
if [ ! -d "$src/llvm-project/compiler-rt" ]; then
    git clone -q --depth 1 --filter=blob:none --sparse -b "$LLVM_TAG" "$LLVM_REPO" "$src/llvm-project"
    git -C "$src/llvm-project" sparse-checkout set compiler-rt cmake llvm/cmake
fi

if [ ! -f "$rt/lib/libmingwex.a" ]; then
    rm -rf "$obj/rt-build/crt" && mkdir -p "$obj/rt-build/crt"
    ( cd "$obj/rt-build/crt" &&
      "$src/mingw-w64/mingw-w64-crt/configure" -q \
          --host="$target" --prefix="$rt" \
          --enable-lib32 --disable-lib64 --disable-libarm32 --disable-libarm64 \
          --with-default-msvcrt=msvcrt \
          CC="$cc" CFLAGS="$arch_flags -O2" \
          AR="$llvm_mingw/bin/llvm-ar" RANLIB="$llvm_mingw/bin/llvm-ranlib" \
          DLLTOOL="$llvm_mingw/bin/llvm-dlltool" &&
      make -s -j"$jobs" && make -s install )
    [ -d "$rt/lib32" ] && { mkdir -p "$rt/lib"; cp -a "$rt/lib32/." "$rt/lib/"; }
fi

builtins="$rt/clang/lib/windows/libclang_rt.builtins-i386.a"
if [ ! -f "$builtins" ]; then
    rm -rf "$obj/rt-build/builtins"
    cmake -S "$src/llvm-project/compiler-rt/lib/builtins" -B "$obj/rt-build/builtins" -G "Unix Makefiles" \
        -DCMAKE_BUILD_TYPE=Release \
        -DCMAKE_SYSTEM_NAME=Windows \
        -DCMAKE_C_COMPILER="$cc" -DCMAKE_CXX_COMPILER="$llvm_mingw/bin/$target-clang++" \
        -DCMAKE_ASM_COMPILER="$cc" \
        -DCMAKE_AR="$llvm_mingw/bin/llvm-ar" -DCMAKE_RANLIB="$llvm_mingw/bin/llvm-ranlib" \
        -DCMAKE_C_COMPILER_TARGET="$target" -DCMAKE_ASM_COMPILER_TARGET="$target" \
        -DCMAKE_C_FLAGS="$arch_flags" -DCMAKE_ASM_FLAGS="$arch_flags" \
        -DCMAKE_C_COMPILER_WORKS=1 -DCMAKE_CXX_COMPILER_WORKS=1 \
        -DCOMPILER_RT_DEFAULT_TARGET_ONLY=ON \
        -DCOMPILER_RT_BUILTINS_ENABLE_PIC=OFF \
        -DCOMPILER_RT_EXCLUDE_ATOMIC_BUILTIN=ON \
        -DCOMPILER_RT_OS_DIR=windows \
        -DCMAKE_INSTALL_PREFIX="$rt/clang" >/dev/null
    make -s -C "$obj/rt-build/builtins" -j"$jobs"
    mkdir -p "$(dirname "$builtins")"
    cp "$(find "$obj/rt-build/builtins" -name 'libclang_rt.builtins*.a' | head -1)" "$builtins"
    # the i386 .S versions of these use SSE2 no matter what, use the C ones
    sse_asm="floatdidf floatdisf floatdixf floatundidf floatundisf floatundixf"
    for fn in $sse_asm; do
        "$llvm_mingw/bin/llvm-ar" d "$builtins" "$fn.S.obj"
        "$cc" $arch_flags -O2 -c "$src/llvm-project/compiler-rt/lib/builtins/$fn.c" \
            -o "$obj/rt-build/builtins/$fn.c.obj"
        "$llvm_mingw/bin/llvm-ar" r "$builtins" "$obj/rt-build/builtins/$fn.c.obj"
    done
    "$llvm_mingw/bin/llvm-ranlib" "$builtins"
    mkdir -p "$rt/clang/lib/i686-w64-windows-gnu"
    cp "$builtins" "$rt/clang/lib/i686-w64-windows-gnu/libclang_rt.builtins.a"
fi

echo "build-runtime: i586 runtime in $rt"
