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
	struct bstr_t
	{
		// Notes:
		//  BSTR's are allocated on a special heap that retains the length of the allocation.
		//  This means BSTR's can contain embedded '\0'.

		BSTR m_str;
		bool m_own;

		bstr_t()
			:m_str()
			,m_own(true)
		{}
		bstr_t(BSTR str, bool own)
			:m_str(str)
			,m_own(own)
		{}
		bstr_t(bstr_t const& rhs)
			:m_str(SysAllocString(rhs))
			,m_own(true)
		{}
		bstr_t(bstr_t&& rhs)
			:m_str(rhs.m_str)
			,m_own(rhs.m_own)
		{
			rhs.m_str = nullptr;
			rhs.m_own = false;
		}
		~bstr_t()
		{
			if (m_str && m_own)
				SysFreeString(m_str);
		}

		// Assignment
		bstr_t& operator = (bstr_t const& rhs)
		{
			if (&rhs == this) return *this;
			if (m_str && m_own) SysFreeString(m_str);
			m_str = SysAllocString(rhs.m_str);
			m_own = true;
			return *this;
		}
		bstr_t& operator = (bstr_t&& rhs)
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
