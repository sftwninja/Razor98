#!/usr/bin/env python3
"""Make an upstream MSVC .def usable with GNU ld (adds Name=mangled aliases).

Usage: gen-def.py NM UPSTREAM.def OUT.def OBJ...
"""

import re
import subprocess
import sys


def exports(def_path):
    names, in_exports = [], False
    for line in open(def_path, encoding="utf-8-sig"):
        line = line.split(";")[0].strip()
        if not line:
            continue
        if line.upper() == "EXPORTS":
            in_exports = True
            continue
        if in_exports:
            names.append(line.split()[0])
    return names


def defined_symbols(nm, objs):
    out = subprocess.run([nm, "--defined-only", "-g"] + objs, check=True,
                         capture_output=True, text=True).stdout
    syms = set()
    for line in out.splitlines():
        parts = line.split()
        if len(parts) == 3 and parts[1] in "TDBR":
            syms.add(parts[2])
    return syms


def resolve(name, syms):
    if "_" + name in syms:
        return name
    stdcall = [s for s in syms if re.fullmatch(r"_%s@\d+" % re.escape(name), s)]
    if stdcall:
        return stdcall[0][1:]
    mangled = "_Z%d%s" % (len(name), name)
    cxx = [s for s in syms if s[1:].startswith(mangled)]
    if len(cxx) == 1:
        return cxx[0][1:]
    if len(cxx) > 1:
        raise SystemExit("gen-def: %s is overloaded: %s" % (name, cxx))
    raise SystemExit("gen-def: no symbol for export %s" % name)


def main():
    nm, def_in, def_out, objs = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4:]
    syms = defined_symbols(nm, objs)
    lines = ["EXPORTS"]
    for name in exports(def_in):
        target = resolve(name, syms)
        lines.append("\t%s" % name if target == name else "\t%s=%s" % (name, target))
    with open(def_out, "w") as f:
        f.write("\n".join(lines) + "\n")


if __name__ == "__main__":
    main()
