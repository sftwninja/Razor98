/* Force-included (see Makefile). Bits of MSVC the upstream code expects. */
#pragma once

/* MinGW headers don't mark these __cdecl like MSVC's do, so under -mrtd
 * they'd get called as stdcall. Declare them first so it sticks. */
#include <stddef.h>
#ifdef __cplusplus
extern "C" {
#endif
void *__cdecl memcpy(void *__restrict__, const void *__restrict__, size_t);
void *__cdecl memmove(void *, const void *, size_t);
void *__cdecl memset(void *, int, size_t);
int __cdecl memcmp(const void *, const void *, size_t);
char *__cdecl strcpy(char *__restrict__, const char *__restrict__);
char *__cdecl strncpy(char *__restrict__, const char *__restrict__, size_t);
char *__cdecl strcat(char *__restrict__, const char *__restrict__);
size_t __cdecl strlen(const char *);
#ifdef __cplusplus
}
#endif
#include <errno.h>
#include <stdio.h>
#include <string.h>

/* No *_s functions in 98's msvcrt 6.0. */
static inline int win98_fopen_s(FILE **fp, const char *name, const char *mode)
{
	if (fp == NULL)
		return EINVAL;
	*fp = fopen(name, mode);
	return *fp ? 0 : (errno ? errno : ENOENT);
}
#define fopen_s win98_fopen_s

#ifdef __cplusplus
template <size_t N>
static inline int win98_strcpy_s(char (&dest)[N], const char *src)
{
	if (src == NULL || strlen(src) >= N)
	{
		if (N > 0)
			dest[0] = 0;
		return src == NULL ? EINVAL : ERANGE;
	}
	strcpy(dest, src);
	return 0;
}
#define strcpy_s win98_strcpy_s

/* MSVC gives you min/max, MinGW doesn't. */
template <typename A, typename B>
inline auto min(A a, B b) -> decltype(a < b ? a : b) { return a < b ? a : b; }
template <typename A, typename B>
inline auto max(A a, B b) -> decltype(a > b ? a : b) { return a > b ? a : b; }
#endif
