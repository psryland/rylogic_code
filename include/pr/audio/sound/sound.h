//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************
#pragma once
#include "pr/audio/forward.h"

namespace pr::audio
{
	struct Sound :RefCount<Sound>
	{
		audio::VoicePtr<IXAudio2SourceVoice> m_src;
		AudioManager* m_audio_manager;

		Sound();

		// Access the audio manager
		AudioManager& mgr() const;

		// Ref-counting clean up function
		static void RefCountZero(pr::RefCount<Sound>* doomed);
	};
}