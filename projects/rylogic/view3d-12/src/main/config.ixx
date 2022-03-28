//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************

module;

#include "src/forward.h"

export module View3d:Config;
import :Wrappers;

namespace pr::rdr12
{
	// Create an instance of this object to enumerate the adapters and their outputs on the current system.
	// Note: modes are not enumerated because they depend on DXGI_FORMAT. Users should create a SystemConfig,
	// then call 'GetDisplayModes' for the format needed.
	export struct SystemConfig
	{
		/// <summary>An output of a graphics adapter (i.e. a monitor)</summary>
		struct Output
		{
			using ModeCont = std::vector<DisplayMode>;
			D3DPtr<IDXGIOutput> m_output;
			DXGI_OUTPUT_DESC    m_desc;
				
			Output()
				:m_output()
				,m_desc()
			{}
			Output(D3DPtr<IDXGIOutput>& output)
				:m_output(std::move(output))
				,m_desc()
			{
				Throw(m_output->GetDesc(&m_desc));
			}

			/// <summary>Return the number of modes for a given surface format</summary>
			UINT ModeCount(DXGI_FORMAT format) const
			{
				UINT mode_count = 0;
				Throw(m_output->GetDisplayModeList(format, 0, &mode_count, nullptr));
				return mode_count;
			}

			/// <summary>Populate a list of display modes for the given format</summary>
			ModeCont DisplayModes(DXGI_FORMAT format) const
			{
				auto mode_count = ModeCount(format);
				
				ModeCont modes(mode_count);
				if (!modes.empty())
					Throw(m_output->GetDisplayModeList(format, 0, &mode_count, modes.data()));

				return std::move(modes);
			}

			/// <summary>Return the best match for the given mode</summary>
			DisplayMode FindClosestMatchingMode(DisplayMode const& ideal) const
			{
				DisplayMode closest;
				Throw(m_output->FindClosestMatchingMode(&ideal, &closest, nullptr));
				return closest;
			}
		};
		
		/// <summary>A graphics adapter on the system</summary>
		struct Adapter
		{
			using OutputCont = std::vector<Output>;
			D3DPtr<IDXGIAdapter1> m_adapter;
			DXGI_ADAPTER_DESC m_desc;
			OutputCont m_outputs;

			// Constructs a representation of a graphics adapter including its supported modes
			Adapter(D3DPtr<IDXGIAdapter1>& adapter)
				:m_adapter(std::move(adapter))
			{
				// Read the description
				Throw(m_adapter->GetDesc(&m_desc));

				// Enumerate the outputs
				D3DPtr<IDXGIOutput> output;
				for (UINT i = 0; m_adapter->EnumOutputs(i, &output.m_ptr) != DXGI_ERROR_NOT_FOUND; ++i)
					m_outputs.emplace_back(output);
			}
		};

		// Adapters on the system
		using AdapterCont = std::vector<Adapter>;
		AdapterCont m_adapters;

		SystemConfig()
			:m_adapters()
		{
			// Create a DXGIFactory
			D3DPtr<IDXGIFactory4> factory;
			pr::Throw(CreateDXGIFactory1(__uuidof(IDXGIFactory4) ,(void**)&factory.m_ptr));

			// Enumerate each adapter on the system
			D3DPtr<IDXGIAdapter1> adapter;
			for (UINT i = 0; factory->EnumAdapters1(i, &adapter.m_ptr) != DXGI_ERROR_NOT_FOUND; ++i)
				m_adapters.emplace_back(adapter);

			// Add the software adapter (WARP)
			if (Succeeded(factory->EnumWarpAdapter(__uuidof(IDXGIAdapter), (void**)&adapter.m_ptr)))
				m_adapters.emplace_back(adapter);
		}
	};
}

