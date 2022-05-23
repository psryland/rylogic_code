//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/wrappers.h"

namespace pr::rdr12
{
	// Create an instance of this object to enumerate the adapters and their outputs on the current system.
	// Note: modes are not enumerated because they depend on DXGI_FORMAT. Users should create a SystemConfig,
	// then call 'GetDisplayModes' for the format needed.
	struct SystemConfig
	{
		/// <summary>An output of a graphics adapter (i.e. a monitor)</summary>
		struct Output
		{
			D3DPtr<IDXGIOutput> ptr;
			DXGI_OUTPUT_DESC desc;
				
			Output()
				:ptr()
				,desc()
			{}
			Output(D3DPtr<IDXGIOutput>& output)
				:ptr(std::move(output))
				,desc()
			{
				Throw(ptr->GetDesc(&desc));
			}

			/// <summary>Return the number of modes for a given surface format</summary>
			UINT ModeCount(DXGI_FORMAT format) const
			{
				UINT mode_count = 0;
				Throw(ptr->GetDisplayModeList(format, 0, &mode_count, nullptr));
				return mode_count;
			}

			/// <summary>Populate a list of display modes for the given format</summary>
			std::vector<DisplayMode> DisplayModes(DXGI_FORMAT format) const
			{
				auto mode_count = ModeCount(format);
				
				std::vector<DisplayMode> modes(mode_count);
				if (!modes.empty())
					Throw(ptr->GetDisplayModeList(format, 0, &mode_count, modes.data()));

				return std::move(modes);
			}

			/// <summary>Return the best match for the given mode</summary>
			DisplayMode FindClosestMatchingMode(DisplayMode const& ideal) const
			{
				DisplayMode closest;
				Throw(ptr->FindClosestMatchingMode(&ideal, &closest, nullptr));
				return closest;
			}
		};
		
		/// <summary>A graphics adapter on the system</summary>
		struct Adapter
		{
			D3DPtr<IDXGIAdapter1> ptr;
			std::vector<Output> outputs;
			DXGI_ADAPTER_DESC1 desc;

			// Constructs a representation of a graphics adapter including its supported modes
			Adapter()
				: ptr()
				, outputs()
				, desc()
			{}
			Adapter(D3DPtr<IDXGIAdapter1>& adapter)
				: ptr(std::move(adapter))
			{
				// Read the description
				Throw(ptr->GetDesc1(&desc));

				// Enumerate the outputs
				D3DPtr<IDXGIOutput> output;
				for (UINT i = 0; ptr->EnumOutputs(i, &output.m_ptr) != DXGI_ERROR_NOT_FOUND; ++i)
					outputs.emplace_back(output);
			}
		};

		// Adapters on the system
		std::vector<Adapter> adapters;

		explicit SystemConfig(bool with_debug_layer)
			:adapters()
		{
			// Create a DXGIFactory
			D3DPtr<IDXGIFactory4> factory;
			pr::Throw(CreateDXGIFactory2(with_debug_layer ? DXGI_CREATE_FACTORY_DEBUG : 0, __uuidof(IDXGIFactory4), (void**)&factory.m_ptr));

			// Enumerate each adapter on the system (this includes the software  warp adapter)
			D3DPtr<IDXGIAdapter1> adapter;
			for (UINT i = 0; factory->EnumAdapters1(i, (IDXGIAdapter1**)&adapter.m_ptr) != DXGI_ERROR_NOT_FOUND; ++i)
				adapters.emplace_back(adapter);
		}
	};
}

