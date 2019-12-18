//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************
#pragma once

// 'xaudio.2.h' is not in forward.h because not every project
// uses audio.lib. Some just need headers from the audio module.
#include <xaudio2.h>
#include "pr/audio/forward.h"

#ifndef _WIN32_WINNT 
#define _WIN32_WINNT _WIN32_WINNT_WIN8
#elif _WIN32_WINNT < _WIN32_WINNT_WIN8 
#error "_WIN32_WINNT >= _WIN32_WINNT_WIN8 required"
#endif

namespace pr::audio
{
	// unique_ptr deleted for voices
	struct VoiceDeleter
	{
		void operator()(IXAudio2Voice* voice)
		{
			voice->DestroyVoice();
		}
	};

	// Ownership pointer for 'IXAudio2Voice' instances
	template <typename TVoice>
	using VoicePtr = std::unique_ptr<TVoice, VoiceDeleter>;

	// Settings for constructing the audio manager
	struct Settings
	{
		// Set the device id using SystemConfig (or leave as null for the default device)
		wchar_t const* m_device_id;
		uint32_t m_channels;
		uint32_t m_sample_rate;

		Settings()
			:m_device_id(nullptr)
			,m_channels(XAUDIO2_DEFAULT_CHANNELS)
			,m_sample_rate(XAUDIO2_DEFAULT_SAMPLERATE)
		{}
	};

	// Audio manager state variables
	struct State
	{
		Settings m_settings;
		pr::RefPtr<IXAudio2> m_xaudio;
		pr::audio::VoicePtr<IXAudio2MasteringVoice> m_master;

		State(Settings const& settings);
		~State();
	};
}
namespace pr
{
	// The "Renderer" of Audio
	class AudioManager :audio::State
	{
	public:

		explicit AudioManager(audio::Settings const& settings = audio::Settings());
		~AudioManager();

		// Load and play an audio file synchronously.
		// If the audio contains loops, 'loop_count' indicates how many times to loop
		void PlaySynchronous(std::filesystem::path const& filepath, int loop_count = 0) const;
	};
}