//**************************************************
// Profile Viewer
//  Copyright (c) Rylogic Ltd 2007
//**************************************************

#include "stdafx.h"
#include "pr/str/prstring.h"
#include "profileviewercl/profiledatabase.h"

struct ByBase
{
	TProfileDB const* m_data;
	ByBase(TProfileDB const& data) : m_data(&data) {}
};
struct ByName : ByBase
{
	ByName(TProfileDB const& data) : ByBase(data) {}
	bool operator ()(std::size_t lhs, std::size_t rhs) const	{ return str::Compare((*m_data)[lhs].m_name, (*m_data)[rhs].m_name) < 0; }
};
struct ByCallCount : ByBase
{
	ByCallCount(TProfileDB const& data) : ByBase(data) {}
	bool operator ()(std::size_t lhs, std::size_t rhs) const	{ return (*m_data)[lhs].m_call_count > (*m_data)[rhs].m_call_count; }
};
struct ByInclTime : ByBase
{
	ByInclTime(TProfileDB const& data) : ByBase(data) {}
	bool operator ()(std::size_t lhs, std::size_t rhs) const	{ return (*m_data)[lhs].m_incl_time_ms > (*m_data)[rhs].m_incl_time_ms; }
};
struct ByExclTime : ByBase
{
	ByExclTime(TProfileDB const& data) : ByBase(data) {}
	bool operator ()(std::size_t lhs, std::size_t rhs) const	{ return (*m_data)[lhs].m_excl_time_ms > (*m_data)[rhs].m_excl_time_ms; }
};

ProfileDatabase::ProfileDatabase()
:m_frame_number(0)
,m_frames(0)
,m_frame_time_ms(1.0f)
,m_sort_by(ESortBy_ExclTime)
,m_sort_needed(true)
,m_units(EUnits_pc_of_60hz_frame)
,m_output_start_y(0)
{}

// Update the database
void ProfileDatabase::Update(void const* data, std::size_t, bool)
{
	pr::threads::CSLock auto_cs(m_output_cs);

	PacketHeader const*	hdr       = reinterpret_cast<PacketHeader const*>(data);
	Packet const*		pkt_begin = reinterpret_cast<Packet const*>(hdr + 1);
	Packet const*		pkt_end	  = pkt_begin + hdr->m_num_packets;
	Caller const*		caller	  = reinterpret_cast<Caller const*>(pkt_end);

	m_frames			= hdr->m_frames;
	m_frame_number		= hdr->m_frame_number;
	m_frame_time_ms		= hdr->m_frame_time_ms;

	m_data.resize(0);
	m_data.reserve(hdr->m_num_packets);

	// Read the packets
	for( Packet const* pkt = pkt_begin; pkt != pkt_end; ++pkt )
	{
		ProfileData data;
		memcpy(data.m_name, pkt->m_name, NameSize);
		data.m_call_count				= pkt->m_data.m_count / float(hdr->m_frames);
		data.m_incl_time_ms				= float(pkt->m_data.m_time_incl * hdr->m_to_ms / hdr->m_frames);
		data.m_excl_time_ms				= float(pkt->m_data.m_time_excl * hdr->m_to_ms / hdr->m_frames);
		data.m_callers.clear();
		for( Caller const* c = caller + pkt->m_first_caller, *c_end = c + pkt->m_num_callers; c != c_end; ++c )
			data.m_callers[c->m_id] = *c;
		
		m_data.push_back(data);
	}
	m_sort_needed = true;
}

// Display a print out of the database
void ProfileDatabase::Output()
{
	pr::threads::CSLock auto_cs(m_output_cs);

	if( m_sort_needed )
	{
		m_order.resize(m_data.size());
		std::size_t idx = 0;
		for( TOrder::iterator i = m_order.begin(), i_end = m_order.end(); i != i_end; ++i ) { *i = idx++; }
		switch( m_sort_by )
		{
		case ESortBy_ByName:		std::sort(m_order.begin(), m_order.end(), ByName(m_data));			break;
		case ESortBy_CallCount:		std::sort(m_order.begin(), m_order.end(), ByCallCount(m_data));		break;
		case ESortBy_InclTime:		std::sort(m_order.begin(), m_order.end(), ByInclTime(m_data));		break;
		case ESortBy_ExclTime:		std::sort(m_order.begin(), m_order.end(), ByExclTime(m_data));		break;
		default: break;
		}
		m_sort_needed = false;
	}

	char const* units_str = "";
	switch( m_units )
	{
	case EUnits_ms:					units_str = "ms";		break;
	case EUnits_pc:					units_str = "% frm";	break;
	case EUnits_pc_of_60hz_frame:	units_str = "% 60frm";	break;
	default:						units_str = "??";		break;
	}

	COORD pos = cons().GetCursor();
	cons().Clear(0, m_output_start_y, 0, 0);
	cons().SetCursor(0, m_output_start_y);
	cons().Write(Fmt(	" Profile Results:\n"
						" Frame rate: %3.3f Hz   Frame time: %3.3f ms\n"
						"=====================================================================\n"
						" name             | count        | incl (%s)  | excl(%s)  |\n"
						"=====================================================================\n"
						,1000.0f / m_frame_time_ms, m_frame_time_ms
						,units_str ,units_str
						).c_str());

	ProfileData accum;
	strcpy(accum.m_name,"Unaccounted");
	accum.m_call_count = 0;
	accum.m_incl_time_ms = 0.0f;
	accum.m_excl_time_ms = 0.0f;
	int count = 0;
	for( TOrder::const_iterator i = m_order.begin(), i_end = m_order.end(); i != i_end && count != 30; ++i, ++count )
	{
		ProfileData const& data = m_data[*i];
		accum.m_incl_time_ms += data.m_incl_time_ms;
		accum.m_excl_time_ms += data.m_excl_time_ms;
		OutputLine(data);
	}
	accum.m_incl_time_ms = m_frame_time_ms - accum.m_incl_time_ms;
	accum.m_excl_time_ms = m_frame_time_ms - accum.m_excl_time_ms;
	cons().Write("=====================================================================\n");
	OutputLine(accum);
	cons().Write("=====================================================================\n");
	cons().SetCursor(pos);
}

void ProfileDatabase::OutputLine(ProfileData const& data)
{
	switch( m_units )
	{
	case EUnits_ms:
		cons().Write(Fmt(" %16s | %12.2f | %12f | %12f |\n"
			,data.m_name
			,data.m_call_count
			,data.m_incl_time_ms
			,data.m_excl_time_ms
			));
		break;
	case EUnits_pc:
		cons().Write(Fmt(" %16s | %12.2f | %12f | %12f |\n"
			,data.m_name
			,data.m_call_count
			,100.0f * data.m_incl_time_ms / m_frame_time_ms
			,100.0f * data.m_excl_time_ms / m_frame_time_ms
			));
		break;
	case EUnits_pc_of_60hz_frame:
		cons().Write(Fmt(" %16s | %12.2f | %12f | %12f |\n"
			,data.m_name
			,data.m_call_count
			,100.0f * data.m_incl_time_ms / 16.666f
			,100.0f * data.m_excl_time_ms / 16.666f
			));
		break;
	default:
		break;
	}
}

//			if( data_size < sizeof(Msg) ) return;
//			Msg const& msg = *static_cast<Msg const*>(data);
//			switch( msg.m_cmd )
//			{
//			case Msg::ECommand_ActivateProfile:		get().ProfileActive(msg.m_data.ch, true); break;
//			case Msg::ECommand_DeactivateProfile:	get().ProfileActive(msg.m_data.ch, false); break;
//			}

//		bool			m_running;
//		unsigned int	m_freq;
//		unsigned short	m_lines;
//		unsigned int	m_sort_by;
//		float			m_fps;
//		

