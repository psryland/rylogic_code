//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "view3d-12/src/ldraw/sources/source_stream.h"
#include "pr/view3d-12/ldraw/ldraw_object.h"
#include "pr/view3d-12/ldraw/ldraw_serialiser_binary.h"
#include "pr/view3d-12/ldraw/ldraw_parsing.h"

namespace pr::rdr12::ldraw
{
	SourceStream::SourceStream(Guid const* context_id, Renderer* rdr, Socket&& socket, sockaddr_in addr)
		: SourceBase(context_id)
		, m_socket(std::move(socket))
		, m_thread()
		, m_mutex()
	{
		auto address = std::format("{}:{}", network::GetIPAddress(addr), network::GetPort(addr));

		// Start a thread to receive incoming data
		m_thread = std::jthread([this, rdr, address]()
		{
			threads::SetCurrentThreadName(address);
			try
			{
				// Consume data from the socket
				byte_data<4> buffer(65536);
				for (int bytes_read = 0; !m_thread.get_stop_token().stop_requested();)
				{
					// Timeout on select means no more data is available.
					if (!network::SelectToRecv(m_socket, 100))
						continue;

					// Read into '&buffer[bytes_read]'.
					// Reading zero bytes indicates the socket has been closed gracefully.
					int read = ::recv(m_socket, buffer.data<char>() + bytes_read, s_cast<int>(buffer.size<char>() - bytes_read), 0);
					network::Check(read == 0 || read != SOCKET_ERROR);
					if (read == 0)
						break;

					bytes_read += read;

					// Parse the data by batches of sections. Find the range of whole sections to consume.
					int consume = 0;
					for (; bytes_read - consume >= sizeof(ldraw::SectionHeader);)
					{
						auto const& header = buffer.at_byte_ofs<ldraw::SectionHeader>(s_cast<size_t>(consume));

						// If the first 4 bytes are not a keyword, then flush the buffer
						if (!ldraw::EKeyword_::IsValue(s_cast<int>(header.m_keyword)))
						{
							bytes_read = 0;
							consume = 0;
							break;
						}

						// The next 4 bytes should be the section size in bytes (excluding the header)
						auto required = s_cast<int>(header.m_size + sizeof(ldraw::SectionHeader));
						if (consume + required > bytes_read)
						{
							// We can only use up to 'consume' bytes
							break;
						}

						// The next section is complete, include it for consumption
						consume += required;
					}

					// If there are sections to consume, do that
					if (consume != 0)
					{
						mem_istream<char> strm(buffer.data(), consume);
						BinaryReader reader(strm, address, { OnReportError, this }, { OnProgress, this });
						auto out = ldraw::Parse(*rdr, reader, m_context_id);

						rdr->RunOnMainThread([this, out = std::move(out)]() mutable noexcept
						{
							m_output += std::move(out);
							ProcessCommands();
						});

						// Move any remaining data to the front
						memmove(buffer.data(), buffer.data() + consume, bytes_read - consume);
						bytes_read -= consume;
					}

					// Otherwise, if 0 bytes can be consumed, check the buffer is big enough and the partial data is not invalid
					else if (bytes_read >= sizeof(ldraw::SectionHeader))
					{
						auto const& header = buffer.at_byte_ofs<ldraw::SectionHeader>(s_cast<size_t>(0));
						auto required = header.m_size + sizeof(ldraw::SectionHeader);

						// If there's not enough data yet, keep waiting. Make sure the buffer is big enough
						buffer.resize(std::max(buffer.size(), required));
					}
				}
			}
			catch (std::exception const& ex)
			{
				// log?
				OutputDebugStringA(ex.what());
			}
			
			// Make this source as invalid
			m_socket = nullptr;
		});
	}
	SourceStream::SourceStream(SourceStream&& rhs) noexcept
		: SourceBase(rhs)
		, m_socket()
		, m_thread()
	{
		std::swap(m_socket, rhs.m_socket);
		std::swap(m_thread, rhs.m_thread);
	}
	SourceStream& SourceStream::operator =(SourceStream&& rhs) noexcept
	{
		if (this == &rhs) return *this;
		SourceBase::operator=(rhs);
		std::swap(m_socket, rhs.m_socket);
		std::swap(m_thread, rhs.m_thread);
		return *this;
	}
	SourceStream::~SourceStream()
	{
		// Stop the thread
		m_thread.request_stop();
		if (m_thread.joinable())
			m_thread.join();
	}

	// Process any commands received
	void SourceStream::ProcessCommands()
	{
		for (auto& cmd : m_output.m_commands)
		{
			(void)cmd;
		}
		m_output.m_commands.resize(0);
	}
}
