//*****************************************************************************************
// Sol
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
#pragma once
#ifndef PR_SOL_ASSET_MANAGER_H
#define PR_SOL_ASSET_MANAGER_H

#include "sol/main/forward.h"

namespace sol
{
	// Asset manager
	struct AssMgr
	{
		// Place holder method for eventually loading assets from a big data file.
		static wstring DataPath(wstring const& relpath)
		{
			return pr::filesys::CombinePath<wstring>(L"Q:\\local\\media", relpath);
		}
	};
}

#endif
