//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	template <typename T>
	concept HasPrivateData = requires(T v, Guid const& guid, UINT* mdata_size, UINT cdata_size, void* mdata, void const* cdata)
	{
		{ v.GetPrivateData(guid, mdata_size, mdata) } -> std::same_as<HRESULT>;
		{ v.SetPrivateData(guid, cdata_size, cdata) } -> std::same_as<HRESULT>;
	};

	/// <summary>Set the name on a DX resource (debug only)</summary>
	template <typename T>
	void NameResource(T* res, char const* name) requires HasPrivateData<T>
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

		std::string_view res_name(name);
		Throw(res->SetPrivateData(WKPDID_D3DDebugObjectName, s_cast<UINT>(res_name.size()), res_name.data()));

		#endif
	}
	void NameResource(ID3D12Object* res, char const* name)
	{
		NameResource<ID3D12Object>(res, name);
	}
	void NameResource(IDXGIObject* res, char const* name)
	{
		NameResource<IDXGIObject>(res, name);
	}

	// Helper for getting the ref count of a COM pointer.
	ULONG RefCount(IUnknown* ptr)
	{
		// Don't inline this function so that it can be used in the Immediate window during debugging
		if (ptr == nullptr) return 0;
		auto count = ptr->AddRef();
		ptr->Release();
		return count - 1;
	}

	// The number of supported quality levels for the given format and sample count
	UINT MultisampleQualityLevels(ID3D12Device* device, DXGI_FORMAT format, UINT sample_count)
	{
		D3D12_FEATURE_DATA_MULTISAMPLE_QUALITY_LEVELS opts = {format, sample_count, D3D12_MULTISAMPLE_QUALITY_LEVELS_FLAG_NONE};
		auto hr = device->CheckFeatureSupport(D3D12_FEATURE_MULTISAMPLE_QUALITY_LEVELS, &opts, sizeof(opts));
		if (hr == E_INVALIDARG)
			return 0;

		Throw(hr);
		return opts.NumQualityLevels;
	}
}

