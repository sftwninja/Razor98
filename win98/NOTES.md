# Win98 build notes

Builds on Linux. Upstream project files aren't touched; everything lives
in here, and the few source edits are either portable or behind `WIN98` /
`NET20`.

    win98/build.sh      # -> win98/dist/Razor, Razor-update, Razor-win98.zip

Needs: .NET SDK 8+, the llvm-mingw *msvcrt* toolchain, python3, make,
curl, zip.

## Managed

- net20 + Roslyn (C# 7.3). Missing BCL bits (LINQ, Func/Action,
  HashSet, Span, etc.) are in `Compat/`.
- MSBuild can't do ResGen for net20, so `tools/resx2resources.py` writes
  the .resources files, including the BinaryFormatter blobs for images.
- Designer code casts PictureBox/SplitContainer to ISupportInitialize,
  which only works on .NET 4. `tools/net20-designer.py` patches that at
  build time.
- FastColoredTextBox stays strong-named; Razor's resources reference it.
- ngen on 2.0 fails with E_INVALIDARG on Roslyn output because Roslyn
  shares string suffixes in #Strings. `tools/UnshareStrings` rewrites
  the assemblies without that.
- The net20 reference assemblies include SP1/SP2 APIs that 98 doesn't
  have, so compiling isn't proof it runs. Test on 2.0 RTM.

## Native

- llvm-mingw msvcrt. UCRT doesn't exist on 98.
- `native/build-runtime.sh` rebuilds the CRT and compiler-rt for i586.
  The stock ones have SSE2/CMOV and die on a Pentium II.
- Crypt and Platform build with `-mrtd` (upstream uses /Gz). CRT
  functions are forced back to cdecl in `msvc_compat.h`, and builtins are
  off, or clang calls them stdcall.
- `.drectve` is stripped so only the .def names get exported. With the
  extra mangled names, GetProcAddress on 98 couldn't find anything.
- zlib 1.3.1 built with ZLIB_WINAPI. The one Razor ships doesn't load
  on 98.

## Injection on 9x

VirtualAllocEx doesn't work on 9x, and upstream's fixed-address fallback
lands inside the 5.x client. Loader.cpp writes the stub into a shared
file mapping instead (views live above 0x80000000 and are visible in
every process on 9x) and jumps there from the client's entry point. The
NT path is unchanged.

## Performance

- The timer heap and "is the client alive" check were rewritten for 98;
  both were doing slow clock/handle calls every tick.
- tiledata.mul is read with pointers instead of PtrToStructure, and
  preloaded in the background after launch. It was a ~3 s freeze
  entering the world on a PII.

## Testing

- `test/wine-run.sh` runs Razor under Wine with .NET 2.0. Good for
  quick checks, but Wine acts like NT, not 9x, so test on real 98 too.
- `test/fakeclient` is a stand-in client.exe for checking injection.
  Razor stops at NO_MEMCOPY with it, which is expected.
- `test/proctest` pokes at Process APIs and the DLL exports on 98.

## Known issues

- If the client dies during startup, the init error box can end up
  behind the splash screen (upstream does this too).
