//*********************************
// GUID helper functions
//  Copyright (c) Rylogic Ltd 2006
//*********************************

#pragma once

#include <windows.h>
#include <guiddef.h>
#include <objbase.h>
#include "pr/common/to.h"
#include "pr/common/scope.h"
#include "pr/str/string.h"

// Required lib: rpcrt4.lib
#pragma comment(lib, "rpcrt4.lib")

namespace pr
{
	const GUID GUID_INVALID = { 0x00000000, 0x0000, 0x0000, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

	// Create a new GUID
	inline GUID GenerateGUID()
	{
		GUID guid;
		if (FAILED(CoCreateGuid(&guid))) return GUID_INVALID;
		return guid;
	}

	namespace convert
	{
		template <typename Str, typename Char = typename Str::value_type, bool Wide = is_same<Char,wchar_t>::value>
		struct GuidToString
		{
			template <typename = enable_if<!Wide>> static Str To(GUID const& guid, char = 0)
			{
				RPC_CSTR str = nullptr;
				auto s = CreateScope([&]{ ::UuidToStringA(static_cast<UUID const*>(&guid), &str); }, [&]{ ::RpcStringFreeA(&str); });
				return Str(reinterpret_cast<char const*>(str));
			}
			template <typename = enable_if<Wide>> static Str To(GUID const& guid, wchar_t = 0)
			{
				RPC_WSTR str = nullptr;
				auto s = CreateScope([&]{ ::UuidToStringW(static_cast<UUID const*>(&guid), &str); }, [&]{ ::RpcStringFreeW(&str); });
				return Str(reinterpret_cast<wchar_t const*>(str));
			}
		};
		struct ToGuid
		{
			static GUID To(char const* s)
			{
				GUID guid;
				::UuidFromStringA(RPC_CSTR(s), &guid);
				return guid;
			}
			static GUID To(wchar_t const* s)
			{
				GUID guid;
				::UuidFromStringW(RPC_WSTR(s), &guid);
				return guid;
			}
			template <typename Str, typename = enable_if_str_class<Str>> static GUID To(Str const& str)
			{
				return To(str.c_str());
			}
		};
	}
	template <typename Char>                struct Convert<std::basic_string<Char>, GUID> :convert::GuidToString<std::basic_string<Char>> {};
	template <typename Char, int L, bool F> struct Convert<pr::string<Char,L,F>,    GUID> :convert::GuidToString<pr::string<Char,L,F>> {};
	template <typename TFrom>               struct Convert<GUID, TFrom>                   :convert::ToGuid {};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_common_guid)
		{
			PR_CHECK(pr::To<std::string>(pr::GUID_INVALID), "00000000-0000-0000-0000-000000000000");
			PR_CHECK(pr::To<std::wstring>(pr::GUID_INVALID), L"00000000-0000-0000-0000-000000000000");
			PR_CHECK(pr::To<GUID>("00000000-0000-0000-0000-000000000000") == pr::GUID_INVALID, true);
			PR_CHECK(pr::To<GUID>(L"00000000-0000-0000-0000-000000000000") == pr::GUID_INVALID, true);
		}
	}
}
#endif
