//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************
#include "pr/audio/forward.h"
#include "pr/audio/sound/sound.h"
#include "pr/audio/audio/audio_manager.h"

namespace pr::audio
{
	Sound::Sound()
	{}

	// Access the audio manager
	AudioManager& Sound::mgr() const
	{
		return *m_audio_manager;
	}

	// Ref-counting clean up function
	void Sound::RefCountZero(pr::RefCount<Sound>* doomed)
	{
		auto snd = static_cast<Sound*>(doomed);
		snd->mgr().Delete(snd);
	}
}