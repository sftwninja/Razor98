# Crypt/Loader/Platform/zlib DLLs for Win98, into ../bin/$(CONFIG).
# Needs llvm-mingw's msvcrt build (UCRT ones won't load on 98). Set
# LLVM_MINGW or unpack it under ~/.local/share.
#
#   make          build
#
# Upstream sources build unmodified. __try/__except becomes if/else (every
# guarded read already checks IsBadReadPtr). .drectve is stripped so only the
# .def names get exported; 98's GetProcAddress chokes on the extra mangled ones.

CONFIG ?= Release
LLVM_MINGW ?= $(lastword $(sort $(wildcard $(HOME)/.local/share/llvm-mingw-*-msvcrt-*)))
CROSS  ?= $(LLVM_MINGW)/bin/i686-w64-mingw32-
CC      = $(CROSS)clang
CXX     = $(CROSS)clang++
NM      = $(LLVM_MINGW)/bin/llvm-nm
OBJCOPY = $(LLVM_MINGW)/bin/llvm-objcopy
PYTHON ?= python3

ROOT   := $(abspath ../..)
OUT    := $(abspath ../bin/$(CONFIG))
OBJ    := $(abspath ../obj/native)
TOOLS  := $(abspath ../tools)

CPPFLAGS = -DWIN32 -DNDEBUG -D_WINDOWS -D_USRDLL -D_CRT_SECURE_NO_WARNINGS \
           -D_WIN32_WINNT=0x0410 -DWINVER=0x0410 -D_WIN32_WINDOWS=0x0410 \
           -D__USE_MINGW_ANSI_STDIO=0 \
           '-D__try=if (1)' '-D__except(x)=else' \
           -include $(CURDIR)/msvc_compat.h $(EXTRA_CPPFLAGS)
ARCH     = -march=i586 -mtune=generic
# e.g. EXTRA_CPPFLAGS=-DLOGGING for a Crypt.log build
EXTRA_CPPFLAGS ?=
CFLAGS   = $(ARCH) -O2 -fno-strict-aliasing -fms-extensions -w
CXXFLAGS = $(CFLAGS) -fno-exceptions -fno-rtti -fno-threadsafe-statics -Wno-error=c++11-narrowing
# i586 runtime from build-runtime.sh; the prebuilt one uses SSE2.
RT       = $(abspath ../obj/rt-i586)
RT_STAMP = $(RT)/clang/lib/windows/libclang_rt.builtins-i386.a
LDFLAGS  = -B$(RT)/lib -L$(RT)/lib -resource-dir $(RT)/clang \
           -shared -static -nostdlib++ -s \
           -Wl,--kill-at \
           -Wl,--major-os-version=4 -Wl,--minor-os-version=0 \
           -Wl,--major-subsystem-version=4 -Wl,--minor-subsystem-version=0

LIBS = -lws2_32 -lgdi32 -luser32 -ladvapi32 -lkernel32

# same lists as the upstream .vcxproj files
CRYPT_SRC    = Crypt.cpp LoginEncryption.cpp MemFinder.cpp OldStatusBar.cpp \
               OSIEncryption.cpp PacketInfo.cpp UOArt.cpp uo_huffman.cpp twofish.c
LOADER_SRC   = Loader.cpp
PLATFORM_SRC = Platform.cpp

objs = $(patsubst %,$(OBJ)/$(1)/%.o,$(basename $(2)))
RT_OBJ = $(OBJ)/compat_rt.o

CRYPT_OWN    := $(call objs,Crypt,$(CRYPT_SRC))
PLATFORM_OWN := $(call objs,Platform,$(PLATFORM_SRC))
CRYPT_OBJ    := $(RT_OBJ) $(CRYPT_OWN)
LOADER_OBJ   := $(RT_OBJ) $(call objs,Loader,$(LOADER_SRC))
PLATFORM_OBJ := $(RT_OBJ) $(PLATFORM_OWN)

# Upstream builds these with /Gz (stdcall default). The client calls into
# Crypt expecting callee cleanup, so cdecl here crashes it.
$(CRYPT_OWN) $(PLATFORM_OWN): CONV := -mrtd -fno-builtin

# etc/zlib.dll is an MSVC 2015+ build 98 can't load. ZLIB_WINAPI matches it.
ZLIB_VER    = 1.3.1
ZLIB_SHA256 = 9a93b2b7dfdac77ceba5a558a580e74667dd6fede4585b91eefb60f03b72df23
ZLIB_URL    = https://github.com/madler/zlib/releases/download/v$(ZLIB_VER)/zlib-$(ZLIB_VER).tar.gz
ZLIB_DIR    = $(OBJ)/zlib-$(ZLIB_VER)
ZLIB_SRC    = adler32 compress crc32 deflate gzclose gzlib gzread gzwrite infback \
              inffast inflate inftrees trees uncompr zutil
ZLIB_OBJ   := $(patsubst %,$(OBJ)/zlib/%.o,$(ZLIB_SRC))

DLLS = $(OUT)/Crypt.dll $(OUT)/Loader.dll $(OUT)/Platform.dll $(OUT)/zlib.dll

all: $(DLLS)

$(RT_STAMP): build-runtime.sh
	LLVM_MINGW=$(LLVM_MINGW) ./build-runtime.sh

$(OUT)/Crypt.dll: $(CRYPT_OBJ) $(ROOT)/Crypt/Crypt.def $(RT_STAMP) | $(OUT)
	$(PYTHON) $(TOOLS)/gen-def.py $(NM) $(ROOT)/Crypt/Crypt.def $(OBJ)/Crypt.def $(CRYPT_OBJ)
	$(CXX) $(LDFLAGS) -o $@ $(CRYPT_OBJ) $(OBJ)/Crypt.def $(LIBS)

$(OUT)/Loader.dll: $(LOADER_OBJ) $(ROOT)/Loader/Loader.def $(RT_STAMP) | $(OUT)
	$(PYTHON) $(TOOLS)/gen-def.py $(NM) $(ROOT)/Loader/Loader.def $(OBJ)/Loader.def $(LOADER_OBJ)
	$(CXX) $(LDFLAGS) -o $@ $(LOADER_OBJ) $(OBJ)/Loader.def $(LIBS)

$(OUT)/Platform.dll: $(PLATFORM_OBJ) $(ROOT)/Platform/Platform.def $(RT_STAMP) | $(OUT)
	$(PYTHON) $(TOOLS)/gen-def.py $(NM) $(ROOT)/Platform/Platform.def $(OBJ)/Platform.def $(PLATFORM_OBJ)
	$(CXX) $(LDFLAGS) -o $@ $(PLATFORM_OBJ) $(OBJ)/Platform.def $(LIBS)

$(OBJ)/%.o: $(ROOT)/%.cpp msvc_compat.h
	@mkdir -p $(dir $@)
	$(PYTHON) $(TOOLS)/casefold-includes.py $(dir $<) $(OBJ)/incfix/$(*D)
	$(CXX) $(CPPFLAGS) $(CONV) $(CXXFLAGS) -I$(dir $<) -I$(OBJ)/incfix/$(*D) -c -o $@ $<
	$(OBJCOPY) --remove-section=.drectve $@

$(OBJ)/%.o: $(ROOT)/%.c msvc_compat.h
	@mkdir -p $(dir $@)
	$(PYTHON) $(TOOLS)/casefold-includes.py $(dir $<) $(OBJ)/incfix/$(*D)
	$(CC) $(CPPFLAGS) $(CONV) $(CFLAGS) -I$(dir $<) -I$(OBJ)/incfix/$(*D) -c -o $@ $<
	$(OBJCOPY) --remove-section=.drectve $@

$(ZLIB_DIR)/zlib.h:
	@mkdir -p $(OBJ)
	curl -sSL -o $(OBJ)/zlib-$(ZLIB_VER).tar.gz $(ZLIB_URL)
	echo "$(ZLIB_SHA256)  $(OBJ)/zlib-$(ZLIB_VER).tar.gz" | sha256sum -c -
	tar -xzf $(OBJ)/zlib-$(ZLIB_VER).tar.gz -C $(OBJ)
	touch $@

$(OBJ)/zlib/%.o: $(ZLIB_DIR)/zlib.h
	@mkdir -p $(dir $@)
	$(CC) $(ARCH) -O2 -w -DZLIB_WINAPI -D_WIN32_WINNT=0x0410 -DWINVER=0x0410 -I$(ZLIB_DIR) -c -o $@ $(ZLIB_DIR)/$*.c

$(OUT)/zlib.dll: $(ZLIB_OBJ) $(RT_STAMP) | $(OUT)
	$(PYTHON) $(TOOLS)/gen-def.py $(NM) $(ZLIB_DIR)/win32/zlib.def $(OBJ)/zlib.def $(ZLIB_OBJ)
	$(CC) $(LDFLAGS) -o $@ $(ZLIB_OBJ) $(OBJ)/zlib.def -lkernel32

$(RT_OBJ): compat_rt.cpp
	@mkdir -p $(dir $@)
	$(CXX) $(CXXFLAGS) -c -o $@ $<

$(OUT):
	mkdir -p $@

clean:
	rm -rf $(OBJ) $(DLLS)

.PHONY: all clean
