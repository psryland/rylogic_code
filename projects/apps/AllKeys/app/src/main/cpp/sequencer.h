#pragma once
#include "forward.h"

namespace allkeys
{
	class Sequencer
	{
		fluid_sequencer_t *m_sequencer;
		fluid_seq_id_t m_dst_id;
		fluid_seq_id_t m_src_id;

	public:

		Sequencer(Synth& synth, char const* unique_name);
		Sequencer(Sequencer&& rhs) noexcept;
		Sequencer(Sequencer const&) = delete;
		Sequencer& operator=(Sequencer&& rhs) noexcept;
		Sequencer& operator=(Sequencer const&) = delete;
		~Sequencer();

		// Get the current sequencer time
		[[nodiscard]] milliseconds_t Tick() const;

		// Queue an event
		void Queue(fluid_event_t* ev, milliseconds_t time_ms, bool absolute) const;

		// Flush all queued events
		void Flush(fluid_seq_event_type event_type) const;
	};
}