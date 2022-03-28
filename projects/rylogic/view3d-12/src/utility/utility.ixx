//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
module;

#include "src/forward.h"

export module View3d:Utility;

namespace pr::rdr12
{
	/// <summary>Set the name on a DX resource (debug only)</summary>
	export template <typename T>
	void NameResource(T* res, char const* name)
	{
		#if PR_DBG_RDR

		char existing[256];
		UINT size(sizeof(existing) - 1);
		if (res->GetPrivateData(WKPDID_D3DDebugObjectName, &size, existing) != DXGI_ERROR_NOT_FOUND)
		{
			existing[size] = 0;
			if (!str::Equal(existing, name))
				OutputDebugStringA(FmtS("Resource is already named '%s'. New name '%s' ignored", existing, name));
			return;
		}

		std::string res_name = name;
		Throw(res->SetPrivateData(WKPDID_D3DDebugObjectName, UINT(res_name.size()), res_name.c_str()));

		#endif
	}
}

