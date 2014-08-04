//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/textures/texture2d.h"
#include "pr/renderer11/util/event_types.h"

namespace pr
{
	namespace rdr
	{
		// Useful reading:
		//   http://msdn.microsoft.com/en-us/library/windows/desktop/bb205075(v=vs.85).aspx

		// Default RdrSettings
		RdrSettings::RdrSettings(BOOL gdi_compat)
			:m_mem()
			,m_adapter()
			,m_driver_type(D3D_DRIVER_TYPE_HARDWARE)
			,m_device_layers(gdi_compat ? D3D11_CREATE_DEVICE_BGRA_SUPPORT : 0)
			,m_feature_levels()
		{
			// Add the debug layer in debug mode
			//PR_EXPAND(PR_DBG_RDR, m_device_layers |= D3D11_CREATE_DEVICE_DEBUG);
			//#pragma message(PR_LINK "WARNING: ************************************************** D3D11_CREATE_DEVICE_DEBUG enabled")
		}

		// Initialise the renderer state variables and creates the dx device and swap chain.
		RdrState::RdrState(RdrSettings const& settings)
			:m_settings(settings)
			,m_device()
			,m_immediate()
			,m_d2dfactory()
			,m_feature_level()
		{
			PR_INFO_IF(PR_DBG_RDR, (m_settings.m_device_layers & D3D11_CREATE_DEVICE_DEBUG       ) != 0, "D3D11_CREATE_DEVICE_DEBUG is enabled");
			PR_INFO_IF(PR_DBG_RDR, (m_settings.m_device_layers & D3D11_CREATE_DEVICE_BGRA_SUPPORT) != 0, "D3D11_CREATE_DEVICE_BGRA_SUPPORT is enabled");

			// Create the device interface
			pr::Throw(D3D11CreateDevice(
				m_settings.m_adapter.m_ptr,
				m_settings.m_driver_type,
				0,
				m_settings.m_device_layers,
				m_settings.m_feature_levels.empty() ? nullptr : &m_settings.m_feature_levels[0],
				static_cast<UINT>(m_settings.m_feature_levels.size()),
				D3D11_SDK_VERSION,
				&m_device.m_ptr,
				&m_feature_level,
				&m_immediate.m_ptr
				));
			PR_EXPAND(PR_DBG_RDR, NameResource(m_device, "dx device"));
			PR_EXPAND(PR_DBG_RDR, NameResource(m_immediate, "immed dc"));

			// Check dlls,dx features,etc required to run the renderer are available
			// Check the given settings are valid for the current adaptor
			if (m_feature_level < D3D_FEATURE_LEVEL_10_0)
				throw std::exception("Graphics hardware does not meet the required feature level.\r\nFeature level 10.0 required\r\n\r\n(e.g. Shader Model 4.0, non power-of-two texture sizes)");

			// Create the direct2d factory
			if (m_settings.m_device_layers & D3D11_CREATE_DEVICE_BGRA_SUPPORT)
				pr::Throw(D2D1CreateFactory(D2D1_FACTORY_TYPE_SINGLE_THREADED, &m_d2dfactory.m_ptr));
		}

		// Renderer state destruction
		RdrState::~RdrState()
		{
			PR_EXPAND(PR_DBG_RDR, int rcnt);
			PR_ASSERT(PR_DBG_RDR, (rcnt = m_immediate.RefCount()) == 1, "Outstanding references to the immediate device context");
			m_immediate->OMSetRenderTargets(0, 0, 0);
			m_immediate = nullptr;
			m_d2dfactory = nullptr;

			PR_ASSERT(PR_DBG_RDR, (rcnt = m_device.RefCount()) == 1, "Outstanding references to the dx device");
			m_device = nullptr;
		}
	}

	// Construct the renderer
	Renderer::Renderer(rdr::RdrSettings const& settings)
		:RdrState(settings)
		,m_mdl_mgr(m_settings.m_mem, m_device)
		,m_shdr_mgr(m_settings.m_mem, m_device)
		,m_tex_mgr(m_settings.m_mem, m_device, m_d2dfactory)
		,m_bs_mgr(m_settings.m_mem, m_device)
		,m_ds_mgr(m_settings.m_mem, m_device)
		,m_rs_mgr(m_settings.m_mem, m_device)
	{}
	Renderer::~Renderer()
	{
		// Notify of the renderer shutdown
		pr::events::Send(pr::rdr::Evt_RendererDestroy(*this));
	}
}