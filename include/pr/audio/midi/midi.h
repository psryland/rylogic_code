//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************
#pragma once

#include "pr/audio/audio/forward.h"

namespace pr::audio
{
	// Create a wave bank for a midi instrument
	void CreateMidiInstrumentWaveBank(char const* bank_name, std::filesystem::path const& root_dir, std::filesystem::path const& xwb_filepath, std::filesystem::path const& xml_instrument_filepath);
}
