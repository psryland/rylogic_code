#pragma once
#include "forward.h"

namespace allkeys
{
	class Event
	{
		fluid_event_t* m_ev;

	public:

		Event();
		Event(Event&& rhs) noexcept;
		Event(Event const&) = delete;
		Event& operator=(Event&& rhs) noexcept;
		Event& operator=(Event const&) = delete;
		~Event();

		void NoteOn(midi_channel_t chan, midi_key_t key, midi_velocity_t vel) const;
		void NoteOff(midi_channel_t chan, midi_key_t key) const;

		operator fluid_event_t*() const;
	};
}