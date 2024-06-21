#pragma once
#include "forward.h"

namespace allkeys
{
	class Synth
	{
		fluid_settings_t *m_settings;
		fluid_synth_t *m_synth;
		fluid_audio_driver_t *m_driver;
		int m_sfont_id;

	public:
		Synth();
		Synth(Synth&&) = default;
		Synth(Synth const&) = delete;
		Synth& operator=(Synth&&) = default;
		Synth& operator=(Synth const&) = delete;
		~Synth();

		// Load a soundfont
		void LoadSoundFont(char const *sf_path);

		// Get/Set the master gain
		[[nodiscard]] float MasterGain() const;
		void MasterGain(float gain);

		// Play a note
		void NoteOn(int channel, int key, int velocity);
		void NoteOff(int channel, int key);
	};
}