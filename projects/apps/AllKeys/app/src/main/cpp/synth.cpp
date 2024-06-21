#include "forward.h"
#include "synth.h"

namespace allkeys
{
	Synth::Synth()
		: m_settings(new_fluid_settings())
		, m_synth(new_fluid_synth(m_settings))
		, m_driver(new_fluid_audio_driver(m_settings, m_synth))
		, m_sfont_id(-1)
	{}

	Synth::~Synth()
	{
		delete_fluid_audio_driver(m_driver);
		delete_fluid_synth(m_synth);
		delete_fluid_settings(m_settings);
	}

	// Load a soundfont
	void Synth::LoadSoundFont(char const *sf_path)
	{
		auto id = fluid_synth_sfload(m_synth, sf_path, 1);
		if (id == -1) throw std::runtime_error("Failed to load soundfont");
		m_sfont_id = id;
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

	// Play a note
	void Synth::NoteOn(int channel, int key, int velocity)
	{
		fluid_synth_noteon(m_synth, channel, key, velocity);
	}

	void Synth::NoteOff(int channel, int key)
	{
		fluid_synth_noteoff(m_synth, channel, key);
	}
}
