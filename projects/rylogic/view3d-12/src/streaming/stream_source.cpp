//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "view3d-12/src/streaming/stream_source.h"
#include "pr/view3d-12/ldraw/ldraw_object.h"
#include "pr/view3d-12/ldraw/ldraw_serialiser_binary.h"
#include "pr/view3d-12/ldraw/ldraw_parsing.h"

namespace pr::rdr12
{
	StreamSource::StreamSource()
		: m_socket()
		, m_context_id(GuidZero)
		, m_objects()
		, m_thread()
		, m_mutex()
	{}
	StreamSource::StreamSource(Renderer* rdr, SOCKET socket)
		: m_socket(socket)
		, m_context_id(GenerateGUID())
		, m_objects()
		, m_thread()
		, m_mutex()
	{
		// Start a thread to receive incoming data
		m_thread = std::jthread([this, rdr, socket]()
		{
			try
			{
				auto addr = network::GetSockName(socket);
				auto address = std::filesystem::path(std::format("{}:{}", network::GetIPAddress(addr), network::GetPort(addr)));

				ldraw::ParseResult out;
				//ldraw::ErrorCont errors;
		
				// Callback functions for 'Parse'
				StaticCB<void, ldraw::EParseError, ldraw::Location const&, std::string_view> ReportErrorCB = {
					[](void* ctx, ldraw::EParseError err, ldraw::Location const& loc, std::string_view msg) -> void
					{
						(void)ctx, err, loc, msg;
						//auto& errors = *static_cast<ErrorCont*>(ctx);
						//ParseErrorEventArgs args(msg, err, loc);
						//errors.push_back(std::move(args));
					}, nullptr };
				StaticCB<bool, Guid const&, ldraw::ParseResult const&, ldraw::Location const&, bool> ProgressCB = {
					[](void* ctx, Guid const& context_id, ldraw::ParseResult const& out, ldraw::Location const& loc, bool complete) -> bool
					{
						(void)ctx, context_id, out, loc, complete;
						//auto& ss = *static_cast<ScriptSources*>(ctx);
						//AddFileProgressEventArgs args(context_id, out, loc, complete);
						//ss.OnAddFileProgress(ss, args);
						//return !args.m_cancel;
						return true;
					}, nullptr };

				// Consume data from the socket
				pr::byte_data buffer(65536);
				for (int bytes_read = 0; !m_thread.get_stop_token().stop_requested();)
				{
					// Timeout on select means no more data is available.
					if (!network::SelectToRecv(socket, 100))
						continue;

					int read = ::recv(socket, buffer.data<char>() + bytes_read, s_cast<int>(buffer.size<char>() - bytes_read), 0);
					network::Check(read == 0 || read != SOCKET_ERROR);
					if (read == 0) // Reading zero bytes indicates the socket has been closed gracefully
						break;

					bytes_read += read;

					// All received data should start with an ldraw::SectionHeader. If not, flush the data
					if (bytes_read < sizeof(ldraw::SectionHeader))
						continue;

					// Parse the data by sections
					int consumed = 0;
					for (;;)
					{
						auto const& header = buffer.at_byte_ofs<ldraw::SectionHeader>(s_cast<size_t>(consumed));

						// If the first 4 bytes are not a keyword, then flush the buffer
						if (!ldraw::EKeyword_::IsValue(s_cast<int>(header.m_keyword)))
						{
							consumed = bytes_read;
							break;
						}

						// The next 4 bytes should be the section size in bytes (excluding the header)
						// If there's not enough data yet, keep waiting
						auto required = header.m_size + sizeof(ldraw::SectionHeader);
						if (bytes_read < required)
						{
							// Make sure the buffer is big enough
							buffer.resize(std::max(buffer.size(), required));
							break;
						}

						// We have a complete section, parse it!
						pr::mem_istream<char> strm(&header, required);
						ldraw::BinaryReader reader(strm, address, ReportErrorCB, ProgressCB);
						out = ldraw::Parse(*rdr, reader, m_context_id);
					}

					// Remove the consumed part of the buffer
					if (consumed == bytes_read)
					{
						bytes_read = 0;
					}
					else
					{
						memmove(buffer.data(), buffer.data() + consumed, bytes_read - consumed);
						bytes_read -= consumed;
					}
				}
			}
			catch (std::exception const&)
			{
				// log?
			}
			
			// Make this source as invalid
			m_socket = nullptr;
		});
	}
	StreamSource::StreamSource(StreamSource&& rhs) noexcept
		: m_objects(std::move(rhs.m_objects))
		, m_context_id(std::move(rhs.m_context_id))
		, m_socket(std::move(rhs.m_socket))
		, m_thread(std::move(rhs.m_thread))
	{
		// Clear the source from the original
		rhs.m_socket = nullptr;
		rhs.m_thread = std::jthread();
	}
	StreamSource& StreamSource::operator =(StreamSource&& rhs) noexcept
	{
		if (this != &rhs)
		{
			m_objects = std::move(rhs.m_objects);
			m_context_id = std::move(rhs.m_context_id);
			m_socket = std::move(rhs.m_socket);
			m_thread = std::move(rhs.m_thread);

			// Clear the source from the original
			rhs.m_socket = nullptr;
			rhs.m_thread = std::jthread();
		}
		return *this;
	}
	StreamSource::~StreamSource()
	{
		// Stop the thread
		m_thread.request_stop();
		if (m_thread.joinable())
			m_thread.join();
	}
}
