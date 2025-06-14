//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************
#include "pr/audio/forward.h"
#include "pr/audio/audio/audio_manager.h"
#include "pr/audio/sound/sound.h"
#include "pr/audio/waves/wave_file.h"
#include "pr/audio/util/util.h"

#pragma comment(lib, "xaudio2.lib")
#pragma comment(lib, "runtimeobject.lib")

#if PR_LOGGING
#pragma message("PR_LOGGING enabled")
#endif

namespace pr::audio
{
	State::State(Settings const& settings)
		:m_settings(settings)
		,m_xaudio()
		,m_master()
	{
		// Note: 'XAUDIO2_DEBUG_ENGINE' is not supported on Win8+

		// Check for compatibility
		if (m_settings.m_channels != XAUDIO2_DEFAULT_CHANNELS && !(m_settings.m_channels >= 1 && m_settings.m_channels <= XAUDIO2_MAX_AUDIO_CHANNELS))
			Check(false, FmtS("Too many audio channels: %d. Maximum is %d", m_settings.m_channels, XAUDIO2_MAX_AUDIO_CHANNELS));
		if (m_settings.m_sample_rate != XAUDIO2_DEFAULT_SAMPLERATE && !(m_settings.m_sample_rate >= XAUDIO2_MIN_SAMPLE_RATE && m_settings.m_sample_rate <= XAUDIO2_MAX_SAMPLE_RATE))
			Check(false, FmtS("Unsupported sample rate: %d. Supported range: [%d,%d]", m_settings.m_sample_rate, XAUDIO2_MIN_SAMPLE_RATE, XAUDIO2_MAX_SAMPLE_RATE));

		// Note:
		//  IXAudio2 is the only XAudio2 interface that is derived from the COM IUnknown interface.
		//  It controls the lifetime of the XAudio2 object using two methods derived from IUnknown: AddRef and Release.
		//  No other XAudio2 objects are reference-counted; their lifetimes are explicitly controlled using create and destroy calls,
		//  and are bounded by the lifetime of the XAudio2 object that owns them.

		// Create The XAudio2 interface
		Check(XAudio2Create(m_xaudio.address_of(), 0, XAUDIO2_DEFAULT_PROCESSOR));

		// To see the trace output, you need to view ETW logs for this application:
		// Go to Control Panel, Administrative Tools, Event Viewer.
		// View->Show Analytic and Debug Logs.
		// Applications and Services Logs / Microsoft / Windows / XAudio2. 
		// Right click on Microsoft Windows XAudio2 debug logging, Properties, then Enable Logging, and hit OK 
		#if PR_DBG_AUDIO
		XAUDIO2_DEBUG_CONFIGURATION debug = {0};
		debug.TraceMask = XAUDIO2_LOG_ERRORS | XAUDIO2_LOG_WARNINGS;
		debug.BreakMask = XAUDIO2_LOG_ERRORS;
		m_xaudio->SetDebugConfiguration(&debug, 0);
		#endif

		// Create the mastering voice, the staging buffer that goes to the hardware
		IXAudio2MasteringVoice* mastering_voice;
		Check(m_xaudio->CreateMasteringVoice(
			&mastering_voice,
			m_settings.m_channels,
			m_settings.m_sample_rate,
			0,
			m_settings.m_device_id));
		m_master.reset(mastering_voice);
	}
	State::~State()
	{
		m_master = nullptr;
		if (m_xaudio != nullptr)
		{
			PR_EXPAND(PR_DBG_AUDIO, int rcnt);
			PR_ASSERT(PR_DBG_AUDIO, (rcnt = m_xaudio.RefCount()) == 1, "Outstanding references to the XAudio device context");
			m_xaudio = nullptr;
		}
	}

	// Construct the Audio Manager
	AudioManager::AudioManager(audio::Settings const& settings)
		:State(settings)
		,m_mutex()
		,m_dbg_mem_snd()
	{}
	AudioManager::~AudioManager()
	{}

	// Load and play an audio file synchronously.
	// If the audio contains loops, 'loop_count' indicates how many times to loop
	void AudioManager::PlaySynchronous(std::filesystem::path const& filepath, int loop_count) const
	{
		// Read in the wave file into memory
		audio::WavData wave_data;
		std::unique_ptr<uint8_t[]> wave_file;
		audio::LoadWAVAudioFromFile(filepath, wave_file, wave_data);

		// Play the wave using a XAudio2SourceVoice
		// Create the source voice
		IXAudio2SourceVoice* src_voice_;
		Check(m_xaudio->CreateSourceVoice(&src_voice_, wave_data.wfx));
		audio::VoicePtr<IXAudio2SourceVoice> src_voice(src_voice_);

		// Submit the wave sample data using an XAUDIO2_BUFFER structure
		XAUDIO2_BUFFER buffer = {0};
		buffer.pAudioData = wave_data.audio_start;
		buffer.AudioBytes = wave_data.audio_bytes;
		buffer.Flags = XAUDIO2_END_OF_STREAM;  // Tell the source voice not to expect any data after this buffer
		if (wave_data.loop_length > 0)
		{
			buffer.LoopBegin = wave_data.loop_start;
			buffer.LoopLength = wave_data.loop_length;
			buffer.LoopCount = loop_count;
		}

		// Check for platform support
		Check(wave_data.seek == 0, "This platform does not support xWMA or XMA2");

		// Queue the buffer to be played on the voice
		Check(src_voice->SubmitSourceBuffer(&buffer));

		// Start the source voice playing
		Check(src_voice->Start(0));

		// Let the sound play
		for (bool running = true; running;)
		{
			XAUDIO2_VOICE_STATE state;
			src_voice->GetState(&state);
			running = state.BuffersQueued > 0;
			Sleep(10);
		}
	}

	// Create a sound instance
	SoundPtr AudioManager::CreateSound()
	{
		//Think 'Texture'
		return nullptr;
	}

	// Return a model to the allocator
	void AudioManager::Delete(Sound* sound)
	{
		if (!sound) return;
		SoundDeleted(*sound);
		AudioManager::Lock lock(*this);
		assert(m_dbg_mem_snd.remove(sound));
		audio::Delete<Sound>(sound);
	}
}

