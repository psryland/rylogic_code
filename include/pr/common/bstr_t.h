//*************************************************************************************************
// BSTR
// Copyright (c) Rylogic Ltd 2017
//*************************************************************************************************
#pragma once

#include <string>
#include <algorithm>
#include <wtypes.h>

namespace pr
{
	// An RAII wrapper for BSTR
	struct BSTR_t
	{
		// Notes:
		//  There is a macro named 'bstr_t' in one of the com headers... :(
		//  BSTR's are allocated on a special heap that retains the length of the allocation.
		//  This means BSTR's can contain embedded '\0'.
		//  BSTR's are wchar_t strings

		BSTR m_str;
		bool m_own;

		BSTR_t()
			:m_str()
			,m_own(true)
		{}
		BSTR_t(BSTR str, bool own)
			:m_str(str)
			,m_own(own)
		{}
		BSTR_t(BSTR_t const& rhs)
			:m_str(SysAllocString(rhs))
			,m_own(true)
		{}
		BSTR_t(BSTR_t&& rhs) noexcept
			:m_str(rhs.m_str)
			,m_own(rhs.m_own)
		{
			rhs.m_str = nullptr;
			rhs.m_own = false;
		}
		~BSTR_t()
		{
			if (m_str && m_own)
				SysFreeString(m_str);
		}

		// Assignment
		BSTR_t& operator = (BSTR_t const& rhs)
		{
			if (&rhs == this) return *this;
			if (m_str && m_own) SysFreeString(m_str);
			m_str = SysAllocString(rhs.m_str);
			m_own = true;
			return *this;
		}
		BSTR_t& operator = (BSTR_t&& rhs) noexcept
		{
			if (&rhs == this) return *this;
			std::swap(m_str, rhs.m_str);
			std::swap(m_own, rhs.m_own);
			return *this;
		}

		// Access as raw BSTR
		operator BSTR const&() const
		{
			return m_str;
		}
		operator BSTR&()
		{
			return m_str;
		}

		// Convert to wstring
		std::wstring wstr() const
		{
			return m_str ? std::wstring(m_str, SysStringLen(m_str)) : L"";
		}

		// std::string interface
		size_t size() const
		{
			return m_str ? SysStringLen(m_str) : 0U;
		}
	};
}
