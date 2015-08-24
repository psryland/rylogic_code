//*******************************************************************
// A collection of window related functions
//  Copyright (c) Rylogic Ltd 2008
//*******************************************************************

#pragma once

#include <windows.h>
#include "pr/common/hresult.h"

namespace pr
{
	// RAII class for initialising COM
	struct InitCom
	{
		enum EFlag { NoThrow };
		HRESULT m_res;

		InitCom()      { pr::Throw(m_res = ::CoInitialize(nullptr)); }
		InitCom(EFlag) { m_res = ::CoInitialize(nullptr); }
		~InitCom()     { ::CoUninitialize(); }
	};
	struct InitComEx
	{
		enum EFlag { NoThrow };
		HRESULT m_res;
		InitComEx(DWORD dwCoInit = COINIT_MULTITHREADED)        { pr::Throw(m_res = ::CoInitializeEx(0, dwCoInit)); }
		InitComEx(EFlag, DWORD dwCoInit = COINIT_MULTITHREADED) { m_res = ::CoInitializeEx(0, dwCoInit); }
		~InitComEx()                                            { ::CoUninitialize(); }
	};
}
