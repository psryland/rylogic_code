//********************************
// HRESULT error codes
//  Copyright © Rylogic Ltd 2011
//********************************
//
#ifndef PR_HRESULT_H
#define PR_HRESULT_H

#include <stdio.h>
#include <windows.h>
#include <vector>
#include "pr/common/prtypes.h"
#include "pr/common/assert.h"
#include "pr/common/fmt.h"
#include "pr/common/exception.h"
#include "pr/str/wstring.h"

//#ifndef _D3D9_H_
//#error where's d3d9.h?
//#endif

// Support d3d errors if the header has been included before now. Dodgy I know :/
#if defined(PR_SUPPORT_D3D_HRESULTS) || defined(_D3D9_H_)
	#include <d3d9.h>
	#include <dxerr.h> // Requires: dxerr9.lib
	#define PR_SUPPORT_D3D_ERRORS
#endif

// Define some standard windows defines/macros in case windows headers
// are not already included. All from WinError.h
typedef long HRESULT;

namespace pr
{
	// Forward declare the ToString function
	template <typename Result> std::string ToString(Result result);
	
	// Convert an HRESULT into an error message string
	inline std::string HrMsg(HRESULT hr)
	{
		char msg[256];
		FormatMessageA(FORMAT_MESSAGE_FROM_SYSTEM|FORMAT_MESSAGE_IGNORE_INSERTS, NULL, hr, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), msg, sizeof(msg), NULL);
		return msg;
	}

	// Convert an HRESULT into a string
	template <> inline std::string ToString<HRESULT>(HRESULT result)
	{
		// If it's a d3d error code
		#ifdef PR_SUPPORT_D3D_ERRORS
		if (HRESULT_FACILITY(result) == _FACD3D)
		{
			std::string dx_err;
			dx_err  = "D3DError: ";
			dx_err += pr::str::ToAString<std::string>(DXGetErrorString(result));
			dx_err += "\nD3D Description: ";
			dx_err += pr::str::ToAString<std::string>(DXGetErrorDescription(result));
			return dx_err.c_str();
		}
		#endif
		
		// else ask windows
		LPVOID lpMsgBuf;
		if (!FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER|FORMAT_MESSAGE_FROM_SYSTEM|FORMAT_MESSAGE_IGNORE_INSERTS, NULL, result, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPTSTR)&lpMsgBuf, 0, NULL)) return "Unknown error code";
		std::string err_msg = pr::str::ToAString<std::string>((LPCTSTR)lpMsgBuf);
		LocalFree(lpMsgBuf);
		return err_msg;
	}
	
	// A string to put the last error message into
	inline std::string& Reason()
	{
		static std::string s_reason;
		return s_reason;
	}
	
	// Success / Failure / Verify
	template <typename Result> inline bool Failed   (Result result) { return !Succeeded(result); }
	template <typename Result> inline void Verify   (Result result) { PR_ASSERT(PR_DBG, Succeeded(result), Reason().c_str()); (void)result; }
	template <typename Result> inline void Throw    (Result result) { if (!Succeeded(result)) throw pr::Exception<Result>(result, Reason().c_str()); }
	template <typename Result> inline bool Succeeded(Result result)
	{
		if (result >= 0) return true;
		Reason() = pr::ToString<Result>(result);
		PR_INFO(PR_DBG, Reason().c_str());
		return false;
	}
	template <> inline bool Succeeded(bool result)
	{
		if (result) return true;
		Reason() = "false returned";
		PR_INFO(PR_DBG, Reason().c_str());
		return false;
	}
}

#undef PR_SUPPORT_D3D_ERRORS

#endif
