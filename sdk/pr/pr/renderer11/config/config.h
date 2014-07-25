//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_CONFIG_CONFIG_H
#define PR_RDR_CONFIG_CONFIG_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/wrappers.h"

namespace pr
{
	namespace rdr
	{
		// Create an instance of this object to enumerate the adapters
		// and their outputs on the current system. Note: modes are not
		// enumerated because they depend on DXGI_FORMAT. Users should
		// create a SystemConfig, then call 'GetDisplayModes' for the
		// format needed.
		struct SystemConfig
		{
			typedef pr::vector<DisplayMode> ModeCont;
			
			// An output of a graphics adapter
			struct Output
			{
				D3DPtr<IDXGIOutput> m_output;
				DXGI_OUTPUT_DESC    m_desc;
				
				Output(){}
				Output(D3DPtr<IDXGIOutput>& output);
				UINT ModeCount(DXGI_FORMAT format) const;
				void GetDisplayModes(DXGI_FORMAT format, ModeCont& modes) const;
				DisplayMode FindClosestMatchingMode(DisplayMode const& ideal) const;
			};
			typedef pr::vector<Output> OutputCont;
			
			// A graphics adapter on the system
			struct Adapter
			{
				D3DPtr<IDXGIAdapter> m_adapter;
				DXGI_ADAPTER_DESC    m_desc;
				OutputCont           m_outputs;
				
				Adapter(D3DPtr<IDXGIAdapter>& adapter);
			};
			typedef pr::vector<Adapter> AdapterCont;
			
			AdapterCont m_adapters;
			
			SystemConfig();
		};
	}
}

#endif
