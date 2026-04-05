#ifndef HEXA_UTILS_COMMON_H
#define HEXA_UTILS_COMMON_H

#include <stdint.h>
/* Calling convention */

#if defined(_WIN32) || defined(_WIN64)
#define HEXA_UTILS_CALL __cdecl
#elif defined(__GNUC__) || defined(__clang__)
#define HEXA_UTILS_CALL __attribute__((__cdecl__))
#else
#define HEXA_UTILS_CALL
#endif

/* API export/import */
#if defined(_WIN32) || defined(_WIN64)
#ifdef HEXA_UTILS_BUILD_SHARED
#define HEXA_UTILS_EXPORT
#else
#define HEXA_UTILS_EXPORT
#endif
#elif defined(__GNUC__) || defined(__clang__)
#ifdef HEXA_UTILS_BUILD_SHARED
#define HEXA_UTILS_EXPORT __attribute__((visibility("default")))
#else
#define HEXA_UTILS_EXPORT
#endif
#else
#define HEXA_UTILS_EXPORT
#endif

#if defined __cplusplus
#define HEXA_UTILS_EXTERN extern "C"
#else
#include <stdarg.h>
#include <stdbool.h>
#define HEXA_UTILS_EXTERN extern
#endif

#define HEXA_UTILS_API(type) HEXA_UTILS_EXTERN HEXA_UTILS_EXPORT type HEXA_UTILS_CALL
#define HEXA_UTILS_API_INTERNAL(type) HEXA_UTILS_EXTERN type HEXA_UTILS_CALL

#endif