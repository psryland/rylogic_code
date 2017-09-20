//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************
#pragma once

#include "pr/audio/audio/forward.h"
#include "pr/audio/audio/audio_manager.h"

namespace pr
{
	namespace audio
	{
		struct Context
		{
			using InitSet = std::unordered_set<AudioContext>;

			InitSet m_inits;         // A unique id assigned to each Initialise call
			pr::AudioManager m_audio;

			Context()
				:m_inits()
				,m_audio(audio::Settings())
			{}

			// Error event. Can be called in a worker thread context
			pr::MultiCast<ReportErrorCB> OnError;

			// Report an error to the global error handler
			void ReportError(wchar_t const* msg)
			{
				OnError.Raise(msg);
			}
			void ReportError(wchar_t const* msg, std::exception const& ex)
			{
				pr::string<wchar_t> str = pr::Fmt(L"%S\n%S", msg, ex.what());
				if (str.last() != '\n') str.push_back('\n');
				ReportError(str.c_str());
			}
		};
	}
}