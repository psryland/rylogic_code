#pragma once
#include "forward.h"

namespace allkeys
{
	class Synth
	{
		fluid_settings_t *m_settings;
		fluid_synth_t *m_synth;
		fluid_audio_driver_t *m_driver;
		int m_sf_id;

	public:
		Synth();
		Synth(Synth&& rhs) noexcept;
		Synth(Synth const&) = delete;
		Synth& operator=(Synth&& rhs) noexcept;
		Synth& operator=(Synth const&) = delete;
		~Synth();

		// Load/Unload a soundfont
		void LoadSoundFont(char const *sf_path);
		void ReloadSoundFont(char const* sf_path);
		void UnloadSoundFont();

		// Get/Set the master gain
		[[nodiscard]] float MasterGain() const;
		void MasterGain(float gain);

		// Immediately stop all sounds
		void AllSoundsOff(midi_channel_t channel);

		// Stops notes with a 'Release' event
		void AllNotesOff(midi_channel_t channel);

		// Play a note
		void NoteOn(midi_channel_t channel, midi_key_t key, midi_velocity_t velocity);
		void NoteOff(midi_channel_t channel, midi_key_t key);

		// Set the instrument to use for a given channel
		void ProgramChange(midi_channel_t channel, int program);

		operator fluid_synth_t*() const;
	};
}