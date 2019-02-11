//**********************************************************
// Handle
//  Copyright (c) Rylogic Ltd 2011
//**********************************************************
// Scoped wrapper around a windows handle
#pragma once

#include <cassert>
#include <windows.h>

namespace pr
{
	struct Handle
	{
		HANDLE m_handle;

		~Handle()
		{
			close();
		}
		Handle()
			:m_handle(INVALID_HANDLE_VALUE)
		{}
		Handle(HANDLE handle)
			:m_handle(handle)
		{}
		Handle(Handle const& rhs)
			:m_handle(rhs.m_handle)
		{
			assert(rhs.m_handle == INVALID_HANDLE_VALUE && "pr::Handle should only be copied when the handle isn't valid");
		}
		Handle(Handle&& rhs)
			:m_handle(rhs.m_handle)
		{
			rhs.m_handle = INVALID_HANDLE_VALUE;
		}
		Handle& operator = (Handle const& rhs)
		{
			if (this == &rhs) return *this;
			assert(rhs.m_handle == INVALID_HANDLE_VALUE && "pr::Handle should only be copied when the handle isn't valid");
			close();
			return *this;
		}
		Handle& operator = (Handle&& rhs)
		{
			if (this == &rhs) return *this;
			std::swap(m_handle, rhs.m_handle);
			return *this;
		}

		void close()
		{
			if (m_handle == INVALID_HANDLE_VALUE) return;
			CloseHandle(m_handle);
			m_handle = INVALID_HANDLE_VALUE;
		}
		HANDLE release()
		{
			HANDLE h = m_handle;
			m_handle = INVALID_HANDLE_VALUE;
			return h;
		}

		operator HANDLE() const
		{
			return m_handle;
		}
	};
}