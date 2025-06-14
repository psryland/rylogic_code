//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/config/config.h"

namespace pr::rdr
{
	// Constructs a description of the current system including all available graphics adapters
	SystemConfig::SystemConfig()
	{
		// Create a DXGIFactory
		D3DPtr<IDXGIFactory> factory;
		pr::Check(CreateDXGIFactory(__uuidof(IDXGIFactory) ,(void**)factory.address_of()));

		// Enumerate each adapter on the system
		D3DPtr<IDXGIAdapter> adapter;
		for (UINT i = 0; factory->EnumAdapters(i, adapter.address_of()) != DXGI_ERROR_NOT_FOUND; ++i)
			m_adapters.push_back(Adapter(adapter));
	}

	// Constructs a representation of a graphics adapter including its supported modes
	SystemConfig::Adapter::Adapter(D3DPtr<IDXGIAdapter>& adapter)
	:m_adapter(adapter)
	{
		// Read the description
		pr::Check(m_adapter->GetDesc(&m_desc));

		// Enumerate the outputs
		D3DPtr<IDXGIOutput> output;
		for (UINT i = 0; m_adapter->EnumOutputs(i, output.address_of()) != DXGI_ERROR_NOT_FOUND; ++i)
			m_outputs.push_back(Output(output));
	}

	// Constructs a representation of a single output of a graphics adapter
	SystemConfig::Output::Output(D3DPtr<IDXGIOutput>& output)
	:m_output(output)
	{
		// Read the description
		pr::Check(m_output->GetDesc(&m_desc));
	}

	// Return the number of modes for a given surface format
	UINT SystemConfig::Output::ModeCount(DXGI_FORMAT format) const
	{
		// Get the number of display modes supported
		UINT mode_count = 0;
		pr::Check(m_output->GetDisplayModeList(format, 0, &mode_count, 0));
		return mode_count;
	}

	// Populate a list of display modes for the given format
	void SystemConfig::Output::GetDisplayModes(DXGI_FORMAT format, SystemConfig::ModeCont& modes) const
	{
		// Get the list
		UINT mode_count = ModeCount(format);
		modes.resize(mode_count);
		if (!modes.empty())
			pr::Check(m_output->GetDisplayModeList(format, 0, &mode_count, &modes[0]));
	}

	// Return the best match for
	DisplayMode SystemConfig::Output::FindClosestMatchingMode(DisplayMode const& ideal) const
	{
		DisplayMode close;
		pr::Check(m_output->FindClosestMatchingMode(&ideal, &close, 0));
		return close;
	}
}