#include "sequencer.h"
#include "synth.h"
#include "event.h"

namespace allkeys
{
	Sequencer::Sequencer(Synth& synth, char const* unique_name)
		: m_sequencer(new_fluid_sequencer2(false))
		, m_dst_id(fluid_sequencer_register_fluidsynth(m_sequencer, synth))
		, m_src_id(fluid_sequencer_register_client(m_sequencer, unique_name, nullptr, nullptr))
	{}
	Sequencer::Sequencer(Sequencer&& rhs) noexcept
		: m_sequencer(rhs.m_sequencer)
		, m_dst_id(rhs.m_dst_id)
		, m_src_id(rhs.m_src_id)
	{
		rhs.m_sequencer = nullptr;
	}
	Sequencer& Sequencer::operator=(Sequencer&& rhs) noexcept
	{
		if (&rhs == this) return *this;
		std::swap(m_sequencer, rhs.m_sequencer);
		std::swap(m_dst_id, rhs.m_dst_id);
		std::swap(m_src_id, rhs.m_src_id);
		return *this;
	}
	Sequencer::~Sequencer()
	{
		if (m_sequencer == nullptr) return;
		delete_fluid_sequencer(m_sequencer);
	}

	// Get the current sequencer time
	milliseconds_t Sequencer::Tick() const
	{
		return fluid_sequencer_get_tick(m_sequencer);
	}

	// Queue a note on event
	void Sequencer::Queue(fluid_event_t* ev, milliseconds_t time_ms, bool absolute) const
	{
		fluid_event_set_source(ev, m_src_id);
		fluid_event_set_dest(ev, m_dst_id);
		Check(fluid_sequencer_send_at(m_sequencer, ev, time_ms, absolute), "Failed to add event to sequencer");
	}

	// Flush all queued events
	void Sequencer::Flush(fluid_seq_event_type event_type) const
	{
		fluid_sequencer_remove_events(m_sequencer, m_src_id, m_dst_id, event_type);
	}
}
