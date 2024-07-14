//*******************************************************************
// Scope for CoInitialize and CoUninitialize
//  Copyright (c) Rylogic Ltd 2008
//*******************************************************************
#pragma once
#include <objbase.h>
#include <comdef.h>

namespace pr
{
	// RAII class for initialising COM
	struct InitCom
	{
		enum EFlags
		{
			None = 0,
			NoThrow = 1 << 0,
		};

		HRESULT m_res;

		explicit InitCom(DWORD dwCoInit = COINIT_MULTITHREADED, EFlags flags = EFlags::None)
			:m_res(::CoInitializeEx(0, dwCoInit))
		{
			if ((int(flags) & int(EFlags::NoThrow)) == 0 && FAILED(m_res))
				throw std::runtime_error(_com_error(m_res).ErrorMessage());
		}
		~InitCom()
		{
			::CoUninitialize();
		}
	};

	// Tests whether CoInitialize has been called
	inline bool CoInitializeCalled()
	{
		// If 'CoInitialize' has already been called, this returns S_FALSE
		// All successful calls need to be matched with 'CoUninitialize'
		auto r = ::CoInitialize(nullptr);
		::CoUninitialize();
		return r == S_FALSE;
	}
}
