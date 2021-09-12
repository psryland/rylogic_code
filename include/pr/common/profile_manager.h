//***********************************************************
// Profile Managers
//  Copyright (C) Rylogic Ltd 2007
//***********************************************************
#pragma once
#include "pr/common/profile.h"

#if PR_PROFILE_ENABLE

	#include "pr/container/byte_data.h"
	#include "pr/network/pipe.h"

	#pragma warning(push)
	#pragma warning(disable:4355)

	namespace pr::profile
	{
		// A header for a batch of profile packets
		struct PacketHeader
		{
			size_t m_frame_number;  // Total frames since program began
			size_t m_frames;        // Number of frames represented in this batch
			double m_frame_time_ms; // The average length of time for a frame in the batch
			double m_to_ms;         // Used to convert uint64's to milliseconds
			size_t m_num_packets;   // Number of profiles in the batch
			size_t m_size;          // The total size for this batch of data
		};
		
		// A packet of profile data representing one profile section
		struct Packet
		{
			Data   m_data;           // The time data
			char   m_name[NameSize]; // The name of the profile section
			size_t m_first_caller;   // The index of the first caller for this profile
			size_t m_num_callers;    // The number of callers for this profile
		};

		// An object for posting profile batch data over a named pipe
		struct Proxy
		{
			using Pipe    = pr::Pipe;
			using Packets = std::vector<Packet>;
			using Callers = std::vector<Caller>;

			Pipe         m_pipe;             // The pipe to send data on
			unsigned int m_steps_per_update; // How frequently to send the data
			bytes_t      m_buffer;           // A buffer for the batch of data we send
			Packets      m_packets;          // A buffer of profile packets
			Callers      m_callers;          // A buffer of caller data

			// Constructor
			Proxy(size_t steps_per_update)
				:m_pipe(L"RylogicProfileStream")
				,m_steps_per_update(steps_per_update)
			{}

			// Send the collected data out on the pipe
			void Output()
			{
				using namespace std::chrono;
				if (get().m_frames < m_steps_per_update)
					return;

				auto frame_time  = get().m_frame_time;
				auto frames      = get().m_frames;
				auto frame_count = get().m_frame_count;

				// Collect up the profile data
				m_packets.resize(0);
				m_callers.resize(0);
				get().ReadAndReset(*this);

				auto total_size = sizeof(PacketHeader) + m_packets.size()*sizeof(Packet) + m_callers.size()*sizeof(Caller);
				m_buffer.reserve(total_size);

				// A header for the data
				PacketHeader hdr;
				hdr.m_frame_number  = frame_count;
				hdr.m_frames        = frames;
				hdr.m_frame_time_ms = static_cast<double>(duration_cast<milliseconds>(frame_time).count()) / frames;
				hdr.m_num_packets   = m_packets.size();
				hdr.m_size          = total_size;

				// Compile the data into one buffer
				AppendData(m_buffer, hdr);
				if (!m_packets.empty())
					AppendData(m_buffer, &m_packets[0], &m_packets[0] + m_packets.size());
				if (!m_callers.empty())
					AppendData(m_buffer, &m_callers[0], &m_callers[0] + m_callers.size());

				// Post the data
				m_pipe.Write(m_buffer.data(), m_buffer.size());
				m_buffer.resize(0);
			}

			// Function operator used in ReadAndReset();
			void operator()(Profile const& profile)
			{
				// Ignore profiles that are disabled
				if (profile.m_disabled)
					return;

				Packet pkt;
				memcpy(pkt.m_name, profile.m_name, NameSize);
				pkt.m_data         = profile.m_data;
				pkt.m_first_caller = m_callers.size();
				pkt.m_num_callers  = profile.m_caller.size();
				m_packets.push_back(pkt);

				// Add the callers
				for (auto caller : profile.m_caller)
					m_callers.push_back(caller.second);
			}

			// Incoming commands from the pipe
			void OnRecv(void const* data, std::size_t data_size, bool)
			{
				data;
				data_size;
			}
			static void OnRecv(void const* data, std::size_t data_size, bool partial, void* user_data)
			{
				static_cast<Proxy*>(user_data)->OnRecv(data, data_size, partial);
			}
		};
	}

	#pragma warning(pop)

#endif
