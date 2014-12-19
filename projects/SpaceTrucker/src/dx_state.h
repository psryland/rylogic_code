#pragma once

#include "SpaceTrucker/src/forward.h"
#include "SpaceTrucker/src/settings.h"

namespace st
{
	struct DxState
	{
		pr::rdr::SystemConfig       m_config;
		D3DPtr<ID3D11Device>        m_device;
		D3D_FEATURE_LEVEL           m_feature_level;
		D3DPtr<ID3D11DeviceContext> m_immediate;

		DxState(Settings const&)
			:m_config()
		{
			//auto& adp = m_config.m_adapters[0];
			//pr::Throw(D3D11CreateDevice(
			//	adp.m_adapter.m_ptr,
			//	D3D_DRIVER_TYPE_HARDWARE,
			//	0,
			//	0,
			//	nullptr,
			//	0,
			//	D3D11_SDK_VERSION,
			//	&m_device.m_ptr,
			//	&m_feature_level,
			//	&m_immediate.m_ptr
			//	));
		}
	};
}