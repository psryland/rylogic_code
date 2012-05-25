//******************************************************************************
//
// Utilities
//
//******************************************************************************
#ifndef UTILS_H
#define UTILS_H

#include <stdio.h>
#include <stdarg.h>
#include <windows.h>
#pragma intrinsic(memcmp, memcpy, memset, strcat, strcmp, strcpy, strlen, _strset)
#include "Common\PRAssert.h"

// Fast quad word memset
inline void memsetDWORD(void* dest, DWORD value, DWORD quad_count)
{
	PR_ASSERT_STR((reinterpret_cast<DWORD&>(dest) % 4) == 0, "memsetDWORD called on non-aligned memory");
	_asm
	{
		mov edi, dest		; edi points to destination memory
		mov ecx, quad_count	; number of 32-bit words to move
		mov eax, value		; 32-bit value to set to
		rep stosd			; move the data
	}
}

// RECT helper functions
inline int	  RectWidth(RECT rect)			{ return rect.right - rect.left; }
inline int	  RectHeight(RECT rect)			{ return rect.bottom - rect.top; }

// Stuff a float into a DWORD
inline DWORD  FtoDW( float f )				{ return reinterpret_cast<DWORD&>(f); }// Helper function to stuff a float into a DWORD argument

// Copy and null terminate a string
inline void Strncpy(char* dest, const char* src, DWORD len) { PR_ASSERT(strlen(src) < len); strncpy(dest, src, len); dest[len - 1] = '\0'; }

#endif//UTILS_H