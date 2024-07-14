//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	struct FeatureSupport
	{
		ID3D12Device* m_device;
		D3D_FEATURE_LEVEL                                                     MaxFeatureLevel;
		D3D12_FEATURE_DATA_D3D12_OPTIONS                                      Options;
		D3D12_FEATURE_DATA_D3D12_OPTIONS1                                     Options1;
		D3D12_FEATURE_DATA_D3D12_OPTIONS2                                     Options2;
		D3D12_FEATURE_DATA_D3D12_OPTIONS3                                     Options3;
		D3D12_FEATURE_DATA_D3D12_OPTIONS4                                     Options4;
		D3D12_FEATURE_DATA_D3D12_OPTIONS5                                     Options5;
		D3D12_FEATURE_DATA_D3D12_OPTIONS6                                     Options6;
		D3D12_FEATURE_DATA_D3D12_OPTIONS7                                     Options7;
		D3D12_FEATURE_DATA_D3D12_OPTIONS8                                     Options8;
		D3D12_FEATURE_DATA_D3D12_OPTIONS9                                     Options9;
		D3D12_FEATURE_DATA_GPU_VIRTUAL_ADDRESS_SUPPORT                        GPUVASupport;
		D3D12_FEATURE_DATA_SHADER_MODEL                                       ShaderModel;
		std::vector<D3D12_FEATURE_DATA_PROTECTED_RESOURCE_SESSION_SUPPORT>    ProtectedResourceSessionSupport;
		D3D12_FEATURE_DATA_ROOT_SIGNATURE                                     RootSignature;
		std::vector<D3D12_FEATURE_DATA_ARCHITECTURE1>                         Architecture1;
		D3D12_FEATURE_DATA_SHADER_CACHE                                       ShaderCache;
		D3D12_FEATURE_DATA_COMMAND_QUEUE_PRIORITY                             CommandQueuePriority;
		D3D12_FEATURE_DATA_EXISTING_HEAPS                                     ExistingHeaps;
		std::vector<D3D12_FEATURE_DATA_SERIALIZATION>                         Serialization; // Cat2 NodeIndex
		D3D12_FEATURE_DATA_CROSS_NODE                                         CrossNode;
		#if _WIN32_WINNT > _WIN32_WINNT_WIN10
		D3D12_FEATURE_DATA_DISPLAYABLE                                        Displayable;
		D3D12_FEATURE_DATA_D3D12_OPTIONS10                                    Options10;
		D3D12_FEATURE_DATA_D3D12_OPTIONS11                                    Options11;
		D3D12_FEATURE_DATA_D3D12_OPTIONS12                                    Options12;
		#endif

		struct FormatData :D3D12_FEATURE_DATA_FORMAT_SUPPORT
		{
			bool Check(D3D12_FORMAT_SUPPORT1 format) const;
			bool Check(D3D12_FORMAT_SUPPORT2 format) const;
			bool CheckSRV() const;
			bool CheckUAV() const;
			bool CheckRTV() const;
			bool CheckDSV() const;
		};

		FeatureSupport();
		FeatureSupport(ID3D12Device* device);
		void Read(ID3D12Device* device);
		FormatData Format(DXGI_FORMAT format) const;
	};
}