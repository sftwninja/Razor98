#!/usr/bin/env python3
"""resx -> .resources for .NET 2.0, since ResGen doesn't do net20 on Core MSBuild.

Writes BinaryFormatter data by hand for the few types the forms use.

Usage: resx2resources.py IN.resx=OUT.resources [...]
"""

import base64
import io
import os
import re
import struct
import sys
import xml.etree.ElementTree as ET

MSCORLIB = "mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
SYSTEM_DRAWING = "System.Drawing, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"

READER_TYPE = "System.Resources.ResourceReader, " + MSCORLIB
RESOURCE_SET_TYPE = "System.Resources.RuntimeResourceSet"

# System.Resources.ResourceTypeCode
TC_STRING = 0x01
TC_BOOLEAN = 0x02
TC_INT32 = 0x08
TC_BYTEARRAY = 0x20
TC_USER = 0x40

# designer junk that sometimes shows up in <data>, skip it
DESIGN_TIME_TYPES = {"System.CodeDom.MemberAttributes", "System.Globalization.CultureInfo"}

# 4.0 -> 2.0 in pre-serialized blobs. Same length, so NRBF prefixes stay valid.
FRAMEWORK_V4_TO_V2 = [
    (b"Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
     b"Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
    (b"Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
     b"Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"),
]


def write_7bit(buf, n):
    while n >= 0x80:
        buf.write(bytes([(n & 0x7F) | 0x80]))
        n >>= 7
    buf.write(bytes([n]))


def write_str(buf, s, encoding="utf-8"):
    raw = s.encode(encoding)
    write_7bit(buf, len(raw))
    buf.write(raw)


def i32(n):
    return struct.pack("<i", n)


def resource_hash(name):
    # System.Resources.FastResourceComparer.HashFunction
    h = 5381
    for ch in name:
        h = (((h << 5) + h) ^ ord(ch)) & 0xFFFFFFFF
    return struct.unpack("<i", struct.pack("<I", h))[0]


class Nrbf:
    """Minimal MS-NRBF writer for Bitmap, Icon, Size. Layout matches what
    2.0's BinaryFormatter writes (2.0 won't read arrays inline)."""

    ROOT_ID = 1
    LIB_ID = 2

    def __init__(self):
        self.buf = io.BytesIO()
        self.next_id = 3
        self.next_value_id = -4
        self.deferred = []
        # SerializationHeaderRecord: root id 1, header id -1, v1.0
        self.buf.write(b"\x00" + i32(self.ROOT_ID) + i32(-1) + i32(1) + i32(0))
        self.buf.write(b"\x0c" + i32(self.LIB_ID))
        write_str(self.buf, SYSTEM_DRAWING)

    def _class_header(self, obj_id, name, members):
        # ClassWithMembersAndTypes. members: [(name, binary_type, extra_bytes)]
        b = self.buf
        b.write(b"\x05" + i32(obj_id))
        write_str(b, name)
        b.write(i32(len(members)))
        for m, _, _ in members:
            write_str(b, m)
        for _, bt, _ in members:
            b.write(bytes([bt]))
        for _, _, extra in members:
            b.write(extra)
        b.write(i32(self.LIB_ID))

    def _byte_array_ref(self, data):
        # MemberReference now, ArraySinglePrimitive(Byte) after the root.
        obj_id = self.next_id
        self.next_id += 1
        self.buf.write(b"\x09" + i32(obj_id))
        self.deferred.append(b"\x0f" + i32(obj_id) + i32(len(data)) + b"\x02" + data)

    def _size(self, obj_id, w, h):
        self._class_header(obj_id, "System.Drawing.Size",
                           [("width", 0, b"\x08"), ("height", 0, b"\x08")])
        self.buf.write(i32(w) + i32(h))

    def finish(self):
        for record in self.deferred:
            self.buf.write(record)
        self.buf.write(b"\x0b")  # MessageEnd
        return self.buf.getvalue()

    @classmethod
    def bitmap(cls, data):
        n = cls()
        n._class_header(cls.ROOT_ID, "System.Drawing.Bitmap", [("Data", 7, b"\x02")])
        n._byte_array_ref(data)
        return n.finish()

    @classmethod
    def icon(cls, data):
        n = cls()
        size_class = io.BytesIO()
        write_str(size_class, "System.Drawing.Size")
        size_info = size_class.getvalue() + i32(cls.LIB_ID)
        n._class_header(cls.ROOT_ID, "System.Drawing.Icon",
                        [("IconData", 7, b"\x02"), ("IconSize", 4, size_info)])
        n._byte_array_ref(data)
        # Size.Empty, same as Icon.GetObjectData for a stream-loaded icon
        n._size(n.next_value_id, 0, 0)
        return n.finish()

    @classmethod
    def size(cls, w, h):
        n = cls()
        n._size(cls.ROOT_ID, w, h)
        return n.finish()


def blob_root_type(blob):
    s = io.BytesIO(blob)

    def rd_str():
        n = shift = 0
        while True:
            c = s.read(1)[0]
            n |= (c & 0x7F) << shift
            shift += 7
            if c < 0x80:
                return s.read(n).decode("utf-8")

    libs = {}
    s.read(17)  # header
    while True:
        rec = s.read(1)[0]
        if rec == 0x0C:
            lib_id = struct.unpack("<i", s.read(4))[0]
            libs[lib_id] = rd_str()
        elif rec in (0x05, 0x03):
            s.read(4)
            name = rd_str()
            count = struct.unpack("<i", s.read(4))[0]
            for _ in range(count):
                rd_str()
            if rec == 0x03:
                raise ValueError("ClassWithMembers root not supported")
            types = s.read(count)
            for t in types:
                if t in (0, 7):
                    s.read(1)
                elif t == 3:
                    rd_str()
                elif t == 4:
                    rd_str()
                    s.read(4)
            lib_id = struct.unpack("<i", s.read(4))[0]
            return "%s, %s" % (name, libs[lib_id])
        else:
            raise ValueError("unexpected NRBF record 0x%02x before root class" % rec)


def convert_entry(el):
    """(type_name, type_code, payload) or None to skip."""
    name = el.get("name")
    type_attr = el.get("type") or ""
    type_name = type_attr.split(",")[0].strip()
    mimetype = el.get("mimetype") or ""
    value_el = el.find("value")
    text = value_el.text if value_el is not None and value_el.text is not None else (el.text or "")

    if type_name in DESIGN_TIME_TYPES:
        return None

    if mimetype == "application/x-microsoft.net.object.bytearray.base64":
        data = base64.b64decode("".join(text.split()))
        if type_name == "System.Drawing.Bitmap":
            return ("System.Drawing.Bitmap, " + SYSTEM_DRAWING, None, Nrbf.bitmap(data))
        if type_name == "System.Drawing.Icon":
            return ("System.Drawing.Icon, " + SYSTEM_DRAWING, None, Nrbf.icon(data))
        if type_name in ("System.Byte[]", ""):
            buf = io.BytesIO()
            buf.write(i32(len(data)) + data)
            return (None, TC_BYTEARRAY, buf.getvalue())
        raise ValueError("%s: unsupported bytearray type %s" % (name, type_name))

    if mimetype == "application/x-microsoft.net.object.binary.base64":
        blob = base64.b64decode("".join(text.split()))
        for old, new in FRAMEWORK_V4_TO_V2:
            blob = blob.replace(old, new)
        return (blob_root_type(blob), None, blob)

    if mimetype:
        raise ValueError("%s: unsupported mimetype %s" % (name, mimetype))

    if type_name in ("", "System.String"):
        buf = io.BytesIO()
        write_str(buf, text)
        return (None, TC_STRING, buf.getvalue())
    if type_name == "System.Boolean":
        return (None, TC_BOOLEAN, b"\x01" if text.strip().lower() == "true" else b"\x00")
    if type_name == "System.Int32":
        return (None, TC_INT32, i32(int(text.strip())))
    if type_name == "System.Drawing.Size":
        w, h = (int(x) for x in text.split(","))
        return ("System.Drawing.Size, " + SYSTEM_DRAWING, None, Nrbf.size(w, h))
    raise ValueError("%s: unsupported type %s" % (name, type_attr))


def compile_resx(src, dst):
    root = ET.parse(src).getroot()
    entries = []
    for el in root.findall("data"):
        converted = convert_entry(el)
        if converted is not None:
            entries.append((el.get("name"),) + converted)

    user_types = []
    data = io.BytesIO()
    offsets = {}
    for name, type_name, code, payload in entries:
        offsets[name] = data.tell()
        if type_name is not None:
            if type_name not in user_types:
                user_types.append(type_name)
            code = TC_USER + user_types.index(type_name)
        write_7bit(data, code)
        data.write(payload)

    out = io.BytesIO()
    out.write(struct.pack("<I", 0xBEEFCACE) + i32(1))
    reader_hdr = io.BytesIO()
    write_str(reader_hdr, READER_TYPE)
    write_str(reader_hdr, RESOURCE_SET_TYPE)
    out.write(i32(len(reader_hdr.getvalue())) + reader_hdr.getvalue())

    out.write(i32(2) + i32(len(entries)) + i32(len(user_types)))
    for t in user_types:
        write_str(out, t)
    pad = b"PAD"
    i = 0
    while out.tell() % 8:
        out.write(pad[i % 3:i % 3 + 1])
        i += 1

    names = io.BytesIO()
    table = []
    for name, _, _, _ in entries:
        table.append((resource_hash(name), names.tell()))
        write_str(names, name, "utf-16-le")
        names.write(i32(offsets[name]))
    table.sort(key=lambda t: t[0])
    for h, _ in table:
        out.write(i32(h))
    for _, pos in table:
        out.write(i32(pos))

    data_section = out.tell() + 4 + len(names.getvalue())
    out.write(i32(data_section))
    out.write(names.getvalue())
    out.write(data.getvalue())

    os.makedirs(os.path.dirname(os.path.abspath(dst)), exist_ok=True)
    with open(dst, "wb") as f:
        f.write(out.getvalue())


def main(argv):
    status = 0
    for arg in argv:
        src, _, dst = arg.partition("=")
        try:
            compile_resx(src, dst)
        except Exception as e:  # report every bad file before failing the build
            print("%s: error RESX98: %s" % (src, e), file=sys.stderr)
            status = 1
    return status


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
