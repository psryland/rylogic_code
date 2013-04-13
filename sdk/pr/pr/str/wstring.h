//*********************************
// WideString helper functions
//  Copyright © Rylogic Ltd 2006
//*********************************

#ifndef PR_STR_WSTRING_H
#define PR_STR_WSTRING_H

#include <windows.h>
#include <string>
#include "pr/macros/link.h"

#pragma message(PR_LINK "wstring.h is deprecated, use tostring.h instead")

namespace pr
{
	namespace str
	{
		namespace impl
		{
			// Convert a narrow string to a wide string
			template <typename AString, typename WString, int SrcCharSize> struct A2W {};
			template <typename AString, typename WString> struct A2W<AString, WString, 1>
			{
				static WString Conv(AString const& str)
				{
					// Find the number of wide characters we're going to need
					int num_chars = MultiByteToWideChar(CP_ACP, 0, static_cast<char const*>(&str[0]), static_cast<int>(str.size()), 0, 0);
					if (num_chars == 0) return WString();
				
					// Convert the narrow string
					WString result; result.resize(num_chars);
					MultiByteToWideChar(CP_ACP, 0, static_cast<char const*>(&str[0]), static_cast<int>(str.size()), &result[0], num_chars);
					return result;
				}
			};
			template <typename AString, typename WString> struct A2W<AString, WString, 2>
			{
				static WString Conv(AString const& str)
				{
					return WString(str); // 'AString' is already wide
				}
			};
			
			// Convert a wide string to a narrow string
			template <typename WString, typename AString, int SrcCharSize> struct W2A {};
			template <typename WString, typename AString> struct W2A<WString, AString, 2>
			{
				static AString Conv(WString const& str)
				{
					// Find the number of narrow characters we are going to get 
					int num_chars = WideCharToMultiByte(CP_ACP, 0, static_cast<wchar_t const*>(&str[0]), static_cast<int>(str.size()), 0, 0, 0, 0); 
					if (num_chars == 0) return AString();
				
					// Convert the wide string
					AString result; result.resize(num_chars);
					WideCharToMultiByte(CP_ACP, 0, static_cast<wchar_t const*>(&str[0]), static_cast<int>(str.size()), &result[0], num_chars, 0, 0);
					return result;
				}
			};
			template <typename WString, typename AString> struct W2A<WString, AString, 1>
			{
				static AString Conv(WString const& str)
				{
					return AString(str); // 'WString' is already narrow
				}
			};
		}
		
		// Convert a narrow string into a wide string
		template <typename WString, typename AString> inline WString ToWString(AString const& str)
		{
			return impl::A2W<AString,WString,sizeof(AString::value_type)>::Conv(str);
		}
		template <typename WString> inline WString ToWString(char const* str)
		{
			return ToWString<WString, std::string>(str);
		}
		template <typename WString> inline WString ToWString(wchar_t const* str)
		{
			return str;
		}
		
		// Convert a wide string into a narrow string
		template <typename AString, typename WString> inline AString ToAString(WString const& str)
		{
			return impl::W2A<WString, AString, sizeof(WString::value_type)>::Conv(str);
		}
		template <typename AString> inline AString ToAString(wchar_t const* str)
		{
			return ToAString<AString, std::wstring>(str);
		}
		template <typename AString> inline AString ToAString(char const* str)
		{
			return str;
		}
	}
}

#endif
