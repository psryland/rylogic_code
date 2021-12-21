//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once
#include "pr/view3d/forward.h"
#include "pr/view3d/util/wrappers.h"

namespace pr::rdr
{
	// Create an instance of this object to enumerate the adapters
	// and their outputs on the current system. Note: modes are not
	// enumerated because they depend on DXGI_FORMAT. Users should
	// create a SystemConfig, then call 'GetDisplayModes' for the
	// format needed.
	struct SystemConfig
	{
		using ModeCont = pr::vector<DisplayMode>;

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
		using OutputCont = pr::vector<Output>;

		// A graphics adapter on the system
		struct Adapter
		{
			D3DPtr<IDXGIAdapter> m_adapter;
			DXGI_ADAPTER_DESC    m_desc;
			OutputCont           m_outputs;
				
			Adapter(D3DPtr<IDXGIAdapter>& adapter);
		};
		using AdapterCont = pr::vector<Adapter>;

		AdapterCont m_adapters;
		SystemConfig();
	};
}
