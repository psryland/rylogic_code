//***********************************************************
// Profile Managers
//  Copyright © Rylogic Ltd 2007
//***********************************************************

#ifndef PR_PROFILE_MANAGER_H
#define PR_PROFILE_MANAGER_H

#include "pr/common/profile.h"

#ifdef PR_PROFILE_ON

#include "pr/common/byte_data.h"
#include "pr/common/pipe.h"

#pragma warning(push)
#pragma warning(disable:4355)

namespace pr
{
	namespace profile
	{
		// A header for a batch of profile packets
		struct PacketHeader
		{
			unsigned long		m_frame_number;		// Total frames since program began
			unsigned long		m_frames;			// Number of frames represented in this batch
			float				m_frame_time_ms;	// The average length of time for a frame in the batch
			double				m_to_ms;			// Used to convert uint64's to milliseconds
			std::size_t			m_num_packets;		// Number of profiles in the batch
			std::size_t			m_size;				// The total size for this batch of data
		};
		
		// A packet of profile data representing one profile section
		struct Packet
		{
			Data				m_data;				// The time data
			char				m_name[NameSize];	// The name of the profile section
			std::size_t			m_first_caller;		// The index of the first caller for this profile
			std::size_t			m_num_callers;		// The number of callers for this profile
		};
		typedef std::vector<Packet> TPackets;

		// An object for posting profile batch data over a named pipe
		struct Proxy
		{
			pr::Pipe<65535>		m_pipe;				// The pipe to send data on
			unsigned int		m_steps_per_update;	// How frequently to send the data
			ByteCont			m_buffer;			// A buffer for the batch of data we send
			TPackets			m_packets;			// A buffer of profile packets
			TCaller				m_callers;			// A buffer of caller data

			// Constructor
			Proxy(unsigned int steps_per_update)
			:m_pipe(_T("PRProfileStream"), Proxy::OnRecv, this)
			,m_steps_per_update(steps_per_update)
			{}

			// Send the collected data out on the pipe
			void Output()
			{
				if( get().m_frames < m_steps_per_update ) return;

				unsigned __int64 frame_time = get().m_frame_time;
				unsigned long	 frames		= get().m_frames;
				unsigned long	 frame_count= get().m_frame_count;
				double			 to_ms		= get().m_to_ms;

				// Collect up the profile data
				m_packets.resize(0);
				m_callers.resize(0);
				get().ReadAndReset(*this);

				// A header for the data
				PacketHeader hdr;
				hdr.m_frame_number	= frame_count;
				hdr.m_frames		= frames;
				hdr.m_frame_time_ms	= float(frame_time * to_ms / frames);
				hdr.m_to_ms			= to_ms;
				hdr.m_num_packets	= m_packets.size();
				hdr.m_size			= sizeof(PacketHeader) + m_packets.size()*sizeof(Packet) + m_callers.size()*sizeof(Caller);

				// Compile the data into one buffer
				m_buffer.reserve(hdr.m_size);
				AppendData(m_buffer, hdr);
				if( !m_packets.empty() ) AppendData(m_buffer, &m_packets[0], &m_packets[0] + m_packets.size());
				if( !m_callers.empty() ) AppendData(m_buffer, &m_callers[0], &m_callers[0] + m_callers.size());

				// Post the data
				m_pipe.Send(&m_buffer[0], m_buffer.size());
				m_buffer.resize(0);
			}

			// Function operator used in ReadAndReset();
			void operator()(Profile const& profile)
			{
				// Ignore profiles that are disabled
				if( profile.m_disabled ) return;

				Packet pkt;
				memcpy(pkt.m_name, profile.m_name, NameSize);
				pkt.m_data				= profile.m_data;
				pkt.m_first_caller		= m_callers.size();
				pkt.m_num_callers		= profile.m_caller.size();
				m_packets.push_back(pkt);

				// Add the callers
				for( TCallerMap::const_iterator i = profile.m_caller.begin(), i_end = profile.m_caller.end(); i != i_end; ++i )
					m_callers.push_back(i->second);
			}

			// Incoming commands from the pipe
			void OnRecv(void const* data, std::size_t data_size, bool)
			{
				data;
				data_size;
			}
			static void OnRecv(void const* data, std::size_t data_size, bool partial, void* user_data) { static_cast<Proxy*>(user_data)->OnRecv(data, data_size, partial); }
		};
	}//namespace profile
}//namespace pr

#pragma warning(pop)

#endif//PR_PROFILE_ON
#endif//PR_PROFILE_MANAGER_H


	//		struct Msg
	//		{
	//			enum ECommand { ECommand_ActivateProfile, ECommand_DeactivateProfile };
	//			ECommand		m_cmd;
	//			union {
	//			float			f;
	//			unsigned long	ul;
	//			char			ch[pr::profile::NameSize];
	//			}				m_data;
	//		};

	//			if( data_size < sizeof(Msg) ) return;
	//			Msg const& msg = *static_cast<Msg const*>(data);
	//			switch( msg.m_cmd )
	//			{
	//			case Msg::ECommand_ActivateProfile:		get().ProfileActive(msg.m_data.ch, true); break;
	//			case Msg::ECommand_DeactivateProfile:	get().ProfileActive(msg.m_data.ch, false); break;
	//			}
