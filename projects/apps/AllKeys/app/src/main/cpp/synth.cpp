#include "synth.h"

namespace allkeys
{
	inline fluid_settings_t* CreateSettings()
	{
		auto settings = new_fluid_settings();
		//fluid_settings_setstr(settings, "audio.driver", "opensles");
		//fluid_settings_setnum(settings, "synth.audio-channels", 3);
		//fluid_settings_setint(settings, "audio.period-size", 512);
		//fluid_settings_setint(settings, "audio.periods", 4);
		return settings;
	}

	Synth::Synth()
		: m_settings(CreateSettings())
		, m_synth(new_fluid_synth(m_settings))
		, m_driver(new_fluid_audio_driver(m_settings, m_synth))
		, m_sf_id(-1)
	{}
	Synth::Synth(Synth&& rhs) noexcept
		: m_settings(rhs.m_settings)
		, m_synth(rhs.m_synth)
		, m_driver(rhs.m_driver)
		, m_sf_id(rhs.m_sf_id)
	{
		rhs.m_settings = nullptr;
		rhs.m_synth = nullptr;
		rhs.m_driver = nullptr;
		rhs.m_sf_id = -1;
	}
	Synth& Synth::operator=(Synth&& rhs) noexcept
	{
		if (&rhs == this) return *this;
		std::swap(m_driver, rhs.m_driver);
		std::swap(m_synth, rhs.m_synth);
		std::swap(m_settings, rhs.m_settings);
		std::swap(m_sf_id, rhs.m_sf_id);
		return *this;
	}
	Synth::~Synth()
	{
		if (m_driver)
			delete_fluid_audio_driver(m_driver);
		if (m_synth)
			delete_fluid_synth(m_synth);
		if (m_settings)
			delete_fluid_settings(m_settings);
	}

	// Load/Unload a soundfont
	void Synth::LoadSoundFont(char const *sf_path)
	{
		m_sf_id = Check(fluid_synth_sfload(m_synth, sf_path, 1), StrJoin("Failed to load soundfont: ", sf_path));
	}
	void Synth::ReloadSoundFont(char const* sf_path)
	{
		if (m_sf_id < 0) return;
		Check(fluid_synth_sfreload(m_synth, m_sf_id), StrJoin("Failed to unload soundfont: ", m_sf_id));
	}
	void Synth::UnloadSoundFont()
	{
		if (m_sf_id < 0) return;
		Check(fluid_synth_sfunload(m_synth, m_sf_id, 1), StrJoin("Failed to unload soundfont: ", m_sf_id));
		m_sf_id = -1;
	}

	// Get/Set the master gain
	float Synth::MasterGain() const
	{
		return fluid_synth_get_gain(m_synth);
	}
	void Synth::MasterGain(float gain)
	{
		fluid_synth_set_gain(m_synth, gain);
	}

	// Immediately stop all sounds
	void Synth::AllSoundsOff(midi_channel_t channel)
	{
		Check(fluid_synth_all_sounds_off(m_synth, channel), "Failed to stop all sounds");
	}

	// Stops notes with a 'Release' event
	void Synth::AllNotesOff(midi_channel_t channel)
	{
		fluid_synth_all_notes_off(m_synth, channel);
	}

	// Play a note
	void Synth::NoteOn(midi_channel_t channel, midi_key_t key, midi_velocity_t velocity)
	{
		fluid_synth_noteon(m_synth, channel, key, velocity);
	}

	// Stop a note
	void Synth::NoteOff(midi_channel_t channel, midi_key_t key)
	{
		fluid_synth_noteoff(m_synth, channel, key);
	}

	// Set the instrument to use for a given channel
	void Synth::ProgramChange(midi_channel_t channel, int program)
	{
		Check(fluid_synth_program_change(m_synth, channel, program), StrJoin("Failed to change program for channel ", channel, " to ", program));
	}

	Synth::operator fluid_synth_t*() const
	{
		return m_synth;
	}
}
