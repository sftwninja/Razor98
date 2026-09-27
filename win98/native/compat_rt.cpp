// Stand-in for libc++ (-nostdlib++). The real one needs Vista APIs and all
// we use from it is new/delete.

#include <stdlib.h>
#include <new>

// __cdecl on purpose: without it these come out stdcall under -mrtd and the
// stack goes sideways.
void *__cdecl operator new(size_t size) { return malloc(size ? size : 1); }
void *__cdecl operator new[](size_t size) { return malloc(size ? size : 1); }
void *__cdecl operator new(size_t size, const std::nothrow_t &) noexcept { return malloc(size ? size : 1); }
void *__cdecl operator new[](size_t size, const std::nothrow_t &) noexcept { return malloc(size ? size : 1); }
void __cdecl operator delete(void *p) noexcept { free(p); }
void __cdecl operator delete[](void *p) noexcept { free(p); }
void __cdecl operator delete(void *p, size_t) noexcept { free(p); }
void __cdecl operator delete[](void *p, size_t) noexcept { free(p); }

// referenced by libc++ headers, no exceptions here
namespace std {
inline namespace __1 {
[[noreturn]] void __cdecl __libcpp_verbose_abort(const char *, ...) noexcept { abort(); }
[[noreturn]] void __cdecl __throw_length_error(const char *) { abort(); }
[[noreturn]] void __cdecl __throw_bad_alloc() { abort(); }
[[noreturn]] void __cdecl __throw_out_of_range(const char *) { abort(); }
} // namespace __1
} // namespace std
