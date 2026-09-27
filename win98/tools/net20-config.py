#!/usr/bin/env python3
"""Copy app.config to Razor.exe.config with supportedRuntime set to v2.0.

Usage: net20-config.py app.config Razor.exe.config
"""

import re
import sys

src, dst = sys.argv[1], sys.argv[2]
text = open(src, encoding="utf-8-sig").read()
text, n = re.subn(r"<supportedRuntime\b[^>]*/>", '<supportedRuntime version="v2.0.50727"/>', text)
if n != 1:
    sys.exit("net20-config: expected one <supportedRuntime> in %s, found %d" % (src, n))
with open(dst, "w", encoding="utf-8", newline="\r\n") as f:
    f.write(text)
