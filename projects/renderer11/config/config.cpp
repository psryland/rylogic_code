//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/config/config.h"

using namespace pr::rdr;

// Constructs a description of the current system including all available graphics adapters
pr::rdr::SystemConfig::SystemConfig()
{
	// Create a DXGIFactory
	D3DPtr<IDXGIFactory> factory;
	pr::Throw(CreateDXGIFactory(__uuidof(IDXGIFactory) ,(void**)&factory.m_ptr));
	
	// Enumerate each adapter on the system
	D3DPtr<IDXGIAdapter> adapter;
	for (UINT i = 0; factory->EnumAdapters(i, &adapter.m_ptr) != DXGI_ERROR_NOT_FOUND; ++i)
		m_adapters.push_back(Adapter(adapter));
}

// Constructs a representation of a graphics adapter including its supported modes
pr::rdr::SystemConfig::Adapter::Adapter(D3DPtr<IDXGIAdapter>& adapter)
:m_adapter(adapter)
{
	// Read the description
	pr::Throw(m_adapter->GetDesc(&m_desc));
	
	// Enumerate the outputs
	D3DPtr<IDXGIOutput> output;
	for (UINT i = 0; m_adapter->EnumOutputs(i, &output.m_ptr) != DXGI_ERROR_NOT_FOUND; ++i)
		m_outputs.push_back(Output(output));
}

// Constructs a representation of a single output of a graphics adapter
pr::rdr::SystemConfig::Output::Output(D3DPtr<IDXGIOutput>& output)
:m_output(output)
{
	// Read the description
	pr::Throw(m_output->GetDesc(&m_desc));
}

// Return the number of modes for a given surface format
UINT pr::rdr::SystemConfig::Output::ModeCount(DXGI_FORMAT format) const
{
	// Get the number of display modes supported
	UINT mode_count = 0;
	pr::Throw(m_output->GetDisplayModeList(format, 0, &mode_count, 0));
	return mode_count;
}

// Populate a list of display modes for the given format
void pr::rdr::SystemConfig::Output::GetDisplayModes(DXGI_FORMAT format, pr::rdr::SystemConfig::ModeCont& modes) const
{
	// Get the list
	UINT mode_count = ModeCount(format);
	modes.resize(mode_count);
	if (!modes.empty())
		pr::Throw(m_output->GetDisplayModeList(format, 0, &mode_count, &modes[0]));
}

// Return the best match for 
pr::rdr::DisplayMode pr::rdr::SystemConfig::Output::FindClosestMatchingMode(pr::rdr::DisplayMode const& ideal) const
{
	DisplayMode close;
	pr::Throw(m_output->FindClosestMatchingMode(&ideal, &close, 0));
	return close;
}
