//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************
#pragma once

#include "pr/audio/audio/forward.h"

namespace pr
{
	namespace audio
	{
		// Create a wave bank for a midi instrument
		void CreateMidiInstrumentWaveBank(char const* bank_name, wchar_t const* root_dir, wchar_t const* xwb_filepath, wchar_t const* xml_instrument_filepath);
	}
}
