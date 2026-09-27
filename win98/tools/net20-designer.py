#!/usr/bin/env python3
"""Rewrite designer ISupportInitialize casts to "as ... ?." for .NET 2.0.

PictureBox and SplitContainer only implement it from 4.0, so the plain
cast throws on 2.0.

Usage: net20-designer.py SRC_ROOT OUT_ROOT FILE...   (FILE relative to SRC_ROOT)
"""

import os
import re
import sys

CAST = re.compile(
    r"\(\(System\.ComponentModel\.ISupportInitialize\)\((this\.[A-Za-z_][A-Za-z0-9_]*)\)\)\.(BeginInit|EndInit)\(\);")

src_root, out_root, files = sys.argv[1], sys.argv[2], sys.argv[3:]
for rel in files:
    with open(os.path.join(src_root, rel), encoding="utf-8-sig") as f:
        text = f.read()
    text = CAST.sub(r"(\1 as System.ComponentModel.ISupportInitialize)?.\2();", text)
    dst = os.path.join(out_root, rel)
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    # don't touch unchanged files (incremental builds)
    if not os.path.exists(dst) or open(dst, encoding="utf-8").read() != text:
        with open(dst, "w", encoding="utf-8") as f:
            f.write(text)
