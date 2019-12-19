//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************
#pragma once
#include "pr/audio/forward.h"

namespace pr::audio
{
	// Ownership pointer for 'IXAudio2Voice' instances
	struct DestroyVoice { void operator()(IXAudio2Voice* x) { x->DestroyVoice(); } };
	template <typename TVoice> using VoicePtr = std::unique_ptr<TVoice, DestroyVoice>;

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