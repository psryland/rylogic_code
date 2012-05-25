//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_UTIL_STOCK_RESOURCES_H
#define PR_RDR_UTIL_STOCK_RESOURCES_H

#include "pr/renderer11/forward.h"

namespace pr
{
	namespace rdr
	{
		namespace EStockTexture
		{
			enum Type
			{
				Black,
				White,
				Checker,
				NumberOf
			};
			inline wchar_t const* ToString(Type type)
			{
				switch (type)
				{
				default:      return L"";
				case Black:   return L"black";
				case White:   return L"white";
				case Checker: return L"checker";
				}
			}
			inline Type Parse(wchar_t const* str)
			{
				int i; for (i = 0; i != NumberOf && !pr::str::EqualI(str, ToString(static_cast<Type>(i))); ++i) {}
				return static_cast<Type>(i);
			}
		}
	}
}

#endif
