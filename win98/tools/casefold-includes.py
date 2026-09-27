#!/usr/bin/env python3
"""Symlink case-mismatched #include names (StdAfx.h vs stdafx.h) into OUTDIR.

Usage: casefold-includes.py SRCDIR OUTDIR
"""

import os
import re
import sys

src_dir, out_dir = sys.argv[1], sys.argv[2]
os.makedirs(out_dir, exist_ok=True)
files = {f.lower(): f for f in os.listdir(src_dir)}
include = re.compile(r'^\s*#\s*include\s+"([^"]+)"')

for name in os.listdir(src_dir):
    if not name.lower().endswith((".c", ".cpp", ".h")):
        continue
    for line in open(os.path.join(src_dir, name), encoding="latin-1"):
        m = include.match(line)
        if not m:
            continue
        wanted = m.group(1)
        if os.path.exists(os.path.join(src_dir, wanted)) or "/" in wanted:
            continue
        actual = files.get(wanted.lower())
        if actual is None:
            continue
        link = os.path.join(out_dir, wanted)
        if not os.path.lexists(link):
            os.symlink(os.path.abspath(os.path.join(src_dir, actual)), link)
