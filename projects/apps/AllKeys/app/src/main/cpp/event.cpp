#include "event.h"

namespace allkeys
{
	Event::Event()
		: m_ev(new_fluid_event())
	{
	}
	Event::Event(Event&& rhs) noexcept
		: m_ev(rhs.m_ev)
	{
		rhs.m_ev = nullptr;
	}
	Event& Event::operator=(Event&& rhs) noexcept
	{
		if (&rhs == this) return *this;
		std::swap(m_ev, rhs.m_ev);
		return *this;
	}
	Event::~Event()
	{
		if (m_ev)
			delete_fluid_event(m_ev);
	}

	void Event::NoteOn(midi_channel_t chan, midi_key_t key, midi_velocity_t vel) const
	{
		fluid_event_noteon(m_ev, chan, key, vel);
	}

	void Event::NoteOff(midi_channel_t chan, midi_key_t key) const
	{
		fluid_event_noteoff(m_ev, chan, key);
	}

	Event::operator fluid_event_t*() const
	{
		return m_ev;
	}
}