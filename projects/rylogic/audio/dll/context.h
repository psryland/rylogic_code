//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************
#pragma once

#include "pr/audio/forward.h"
#include "pr/audio/audio/audio_manager.h"

namespace pr::audio
{
	struct Context
	{
		using InitSet = std::unordered_set<AudioContext>;

		InitSet m_inits;         // A unique id assigned to each Initialise call
		AudioManager m_audio;

		Context()
			:m_inits()
			,m_audio(audio::Settings())
		{}

		// Error event. Can be called in a worker thread context
		MultiCast<ReportErrorCB> OnError;
		void ReportError(wchar_t const* msg)
		{
			OnError(msg);
		}
		void ReportError(wchar_t const* msg, std::exception const& ex)
		{
			auto str = Fmt(L"%S\n%S", msg, ex.what());
			if (str.back() != '\n') str.push_back(L'\n');
			ReportError(str.c_str());
		}
	};
}