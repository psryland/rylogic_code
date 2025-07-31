//*****************************************
// Sockets
//	Copyright (c) Rylogic 2019
//*****************************************
#pragma once
#include <cstddef>
#include <cstring>
#include <memory>
#include <vector>
#include <chrono>
#include <utility>
#include <iostream>
#include <functional>
#include <format>
#include <WinSock2.h>
#include <WS2tcpip.h>
#include <ws2ipdef.h> 

namespace pr::network
{
	// stream buffer for network stream
	struct socket_streambuf : std::basic_streambuf<std::byte>
	{
		using traits_type = typename socket_streambuf::traits_type;
		using char_type   = typename socket_streambuf::char_type;
		using int_type    = typename socket_streambuf::int_type;
		using pos_type    = typename socket_streambuf::pos_type;
		using off_type    = typename socket_streambuf::off_type;
		using setstate_fn = std::function<void(std::ios_base::iostate)>;

	private:

		SOCKET m_socket;
		setstate_fn m_setstate;
		std::vector<std::byte> m_ibuf;
		std::vector<std::byte> m_obuf;
		std::chrono::milliseconds m_recv_timeout{0};
		std::chrono::milliseconds m_send_timeout{0};
		bool m_non_blocking = false;
		bool m_owns_socket = false;

	public:

		socket_streambuf(SOCKET s, setstate_fn setstate, size_t ibuf_size = 4096, size_t obuf_size = 4096)
			: std::basic_streambuf<std::byte>()
			, m_socket(s)
			, m_setstate(setstate)
			, m_ibuf(ibuf_size)
			, m_obuf(obuf_size)
		{
			setg(m_ibuf.data(), m_ibuf.data(), m_ibuf.data());
			setp(m_obuf.data(), m_obuf.data() + m_obuf.size());
		}
		socket_streambuf(char const* host, int port, IPPROTO proto, setstate_fn setstate, size_t ibuf_size = 4096, size_t obuf_size = 4096)
			: socket_streambuf(create_socket(host, port, proto), setstate, ibuf_size, obuf_size)
		{
			m_owns_socket = true;
		}
		socket_streambuf(socket_streambuf&& other) noexcept
			: std::basic_streambuf<std::byte>(std::move(other))
			, m_socket(std::exchange(other.m_socket, INVALID_SOCKET))
			, m_setstate(std::move(other.m_setstate))
			, m_ibuf(std::move(other.m_ibuf))
			, m_obuf(std::move(other.m_obuf))
			, m_recv_timeout(std::move(other.m_recv_timeout))
			, m_send_timeout(std::move(other.m_send_timeout))
			, m_non_blocking(std::move(other.m_non_blocking))
			, m_owns_socket(std::exchange(other.m_owns_socket, false))
		{}
		socket_streambuf(socket_streambuf const&) = delete;
		socket_streambuf& operator=(socket_streambuf&& other) noexcept
		{
			if (this == &other) return *this;
			std::basic_streambuf<std::byte>::operator=(std::move(other));
			m_socket = std::exchange(other.m_socket, INVALID_SOCKET);
			m_setstate = std::move(other.m_setstate);
			m_ibuf = std::move(other.m_ibuf);
			m_obuf = std::move(other.m_obuf);
			m_recv_timeout = std::move(other.m_recv_timeout);
			m_send_timeout = std::move(other.m_send_timeout);
			m_non_blocking = std::move(other.m_non_blocking);
			m_owns_socket = std::exchange(other.m_owns_socket, false);
			return *this;
		}
		socket_streambuf& operator=(socket_streambuf const&) = delete;
		virtual ~socket_streambuf()
		{
			if (m_owns_socket)
				close();
		}

		// Close the socket
		void close()
		{
			if (m_socket != INVALID_SOCKET)
			{
				::closesocket(m_socket);
				m_socket = INVALID_SOCKET;
			}
		}

		// Access the socket
		SOCKET socket() const
		{
			return m_socket;
		}

		// Read/Write timeouts
		void set_recv_timeout(std::chrono::milliseconds timeout)
		{
			m_recv_timeout = timeout;
		}
		void set_send_timeout(std::chrono::milliseconds timeout)
		{
			m_send_timeout = timeout;
		}

		// Non-blocking mode
		void set_non_blocking(bool non_blocking = true)
		{
			m_non_blocking = non_blocking;
			u_long mode = non_blocking ? 1 : 0;
			::ioctlsocket(m_socket, FIONBIO, &mode);
		}

	protected:

		// Create the socket
		static SOCKET create_socket(char const* host, int port, IPPROTO proto = IPPROTO_TCP)
		{
			// Convert port to string
			auto port_str = std::format("{}", port);

			// Get address info
			ADDRINFO hints = {
				.ai_family = AF_UNSPEC, // IPv4 or IPv6
				.ai_socktype = proto == IPPROTO_TCP ? SOCK_STREAM : SOCK_DGRAM,
				.ai_protocol = proto,
			};

			ADDRINFO* result = nullptr;
			auto ret = ::getaddrinfo(host, port_str.c_str(), &hints, &result);
			if (ret != 0)
				throw std::runtime_error(std::format("getaddrinfo failed for {}:{} - error: {} ({})", host, port, ret, ::gai_strerrorA(ret)));

			// Ensure cleanup of address info
			auto cleanup = [](ADDRINFO* result) { if (result) ::freeaddrinfo(result); };
			std::unique_ptr<ADDRINFO, decltype(cleanup)> addr_guard(result);

			SOCKET sock = INVALID_SOCKET;
			
			// Try each address until we successfully connect
			for (ADDRINFO* ptr = result; ptr != nullptr; ptr = ptr->ai_next)
			{
				// Create socket
				sock = ::socket(ptr->ai_family, ptr->ai_socktype, ptr->ai_protocol);
				if (sock == INVALID_SOCKET)
					continue;

				// Set socket options for better behavior
				if (proto == IPPROTO_TCP)
				{
					// Enable TCP keep-alive
					DWORD keepAlive = 1;
					::setsockopt(sock, SOL_SOCKET, SO_KEEPALIVE, reinterpret_cast<char*>(&keepAlive), sizeof(keepAlive));

					// Disable Nagle's algorithm for low latency
					DWORD nodelay = 1;
					::setsockopt(sock, IPPROTO_TCP, TCP_NODELAY, reinterpret_cast<char*>(&nodelay), sizeof(nodelay));
				}

				// Connect to server
				ret = ::connect(sock, ptr->ai_addr, static_cast<int>(ptr->ai_addrlen));
				if (ret == SOCKET_ERROR)
				{
					::closesocket(sock);
					sock = INVALID_SOCKET;
					continue;
				}
				
				// Connection successful
				break;
			}

			if (sock == INVALID_SOCKET)
			{
				auto error = WSAGetLastError();
				throw std::runtime_error(std::format("Failed to connect to {}:{} - WSA error: {}", host, port, error));
			}

			return sock;
		}

		// Block for data
		bool wait_for_data(std::chrono::milliseconds timeout)
		{
			if (timeout.count() == 0)
				return true;

			fd_set readfds = {};
			FD_ZERO(&readfds);
			FD_SET(m_socket, &readfds);

			timeval tv = {};
			tv.tv_sec = static_cast<long>(timeout.count() / 1000);
			tv.tv_usec = static_cast<long>((timeout.count() % 1000) * 1000);

			auto result = ::select(0, &readfds, nullptr, nullptr, &tv);
			if (result == SOCKET_ERROR)
			{
				m_setstate(std::ios::badbit);
				return false;
			}
			return result > 0;
		}
		bool wait_for_write(std::chrono::milliseconds timeout)
		{
			if (timeout.count() == 0)
				return true;

			fd_set writefds = {};
			FD_ZERO(&writefds);
			FD_SET(m_socket, &writefds);

			timeval tv = {};
			tv.tv_sec = static_cast<long>(timeout.count() / 1000);
			tv.tv_usec = static_cast<long>((timeout.count() % 1000) * 1000);

			auto result = ::select(0, nullptr, &writefds, nullptr, &tv);
			if (result == SOCKET_ERROR)
			{
				m_setstate(std::ios::badbit);
				return false;
			}
			return result > 0;
		}

		// Read/Write underflow/overflow
		int_type underflow() override
		{
			// Wait for data before attempting to receive (if timeout is set)
			if (m_recv_timeout.count() > 0 && !wait_for_data(m_recv_timeout))
				return traits_type::eof(); // Timeout occurred

			// Ensure buffer size fits in int range
			auto buf_size = std::min(m_ibuf.size(), static_cast<size_t>(INT_MAX));
			auto n = ::recv(m_socket, reinterpret_cast<char*>(m_ibuf.data()), static_cast<int>(buf_size), 0);
			if (n == SOCKET_ERROR)
			{
				// Check for WSAEWOULDBLOCK in non-blocking mode
				auto error = WSAGetLastError();
				if (error == WSAEWOULDBLOCK)
					return traits_type::eof(); // Would block, treat as EOF for now

				// Real error occurred
				m_setstate(std::ios::badbit);
				return traits_type::eof();
			}
			if (n == 0) // Connection closed gracefully
			{
				return traits_type::eof();
			}

			setg(m_ibuf.data(), m_ibuf.data(), m_ibuf.data() + n);
			return traits_type::to_int_type(*gptr());
		}
		int_type overflow(int_type c) override
		{
			if (pbase() != pptr())
			{
				auto total_bytes = pptr() - pbase();
				auto sent_bytes = 0;
				for (; sent_bytes != total_bytes; )
				{
					// Wait for socket to be ready for writing (if timeout is set)
					if (m_send_timeout.count() > 0 && !wait_for_write(m_send_timeout))
					{
						update_obuf(sent_bytes, total_bytes);
						return traits_type::eof();
					}

					// Ensure buffer size fits in int range
					auto buf_size = std::min(static_cast<size_t>(total_bytes - sent_bytes), static_cast<size_t>(INT_MAX));
					auto n = ::send(m_socket,
						reinterpret_cast<char const*>(pbase() + sent_bytes),
						static_cast<int>(buf_size),
						0);

					if (n == SOCKET_ERROR)
					{
						// Check for WSAEWOULDBLOCK in non-blocking mode
						auto error = WSAGetLastError();
						if (error == WSAEWOULDBLOCK)
						{
							// Expected if non-blocking
							if (m_non_blocking) break;

							// For blocking sockets, this shouldn't happen after wait_for_write
							m_setstate(std::ios::badbit);
							return traits_type::eof();
						}

						// Other errors are always bad
						m_setstate(std::ios::badbit);
						return traits_type::eof();
					}
					sent_bytes += n;
				}
				update_obuf(sent_bytes, total_bytes);     
			}
			else
			{
				update_obuf(0, 0);
			}
			if (c != traits_type::eof())
			{
				*pptr() = static_cast<std::byte>(c);
				pbump(1);
			}
			return traits_type::not_eof(c);
		}

		int sync() override
		{
			return overflow(traits_type::eof()) == traits_type::eof() ? -1 : 0;
		}

		// Update the output buffer
		void update_obuf(std::ptrdiff_t sent_bytes, std::ptrdiff_t total_bytes)
		{
			// Partial send - move remaining data to beginning of buffer
			if (sent_bytes < total_bytes)
			{
				std::memmove(pbase(), pbase() + sent_bytes, total_bytes - sent_bytes);
				setp(pbase() + (total_bytes - sent_bytes), epptr());
			}
			else
			{
				setp(m_obuf.data(), m_obuf.data() + m_obuf.size());
			}
		}
	};

	// Network stream
	struct socket_stream : std::basic_iostream<std::byte>
	{
		socket_streambuf m_buf;

		socket_stream()
			: std::basic_iostream<std::byte>(&m_buf)
			, m_buf(INVALID_SOCKET, setstate_fn(this), 0, 0)
		{
		}
		socket_stream(char const* host, int port, IPPROTO proto = IPPROTO_TCP, size_t ibuf_size = 4096, size_t obuf_size = 4096)
			: std::basic_iostream<std::byte>(&m_buf)
			, m_buf(host, port, proto, setstate_fn(this), ibuf_size, obuf_size)
		{
		}
		socket_stream(SOCKET s, size_t ibuf_size = 4096, size_t obuf_size = 4096)
			: std::basic_iostream<std::byte>(&m_buf)
			, m_buf(s, setstate_fn(this), ibuf_size, obuf_size)
		{
		}

		// Try to connect to a host. Use `if (stream.connect("host",port).good()) stream << data;`
		socket_stream& connect(char const* host, int port, IPPROTO proto = IPPROTO_TCP, size_t ibuf_size = 4096, size_t obuf_size = 4096)
		{
			// Already connected
			if (is_open())
				return *this;

			try
			{
				// Try to connect
				m_buf = socket_streambuf(host, port, proto, setstate_fn(this), ibuf_size, obuf_size);
				clear(); // Clear any error flags on successful connection
			}
			catch (std::exception const&)
			{
				// Set 'failbit' instead of 'badbit' to allow recovery.
				// This allows the stream to be used again and makes good() return false
				setstate(std::ios::failbit);
			}
			return *this;
		}

		// Close the socket
		void close()
		{
			m_buf.close();
		}

		// Access the socket
		SOCKET socket() const
		{
			return m_buf.socket();
		}

		// Add method to check if socket is valid
		bool is_open() const
		{
			return m_buf.socket() != INVALID_SOCKET;
		}

		// Network parameters
		void set_recv_timeout(std::chrono::milliseconds timeout)
		{
			m_buf.set_recv_timeout(timeout);
		}
		void set_send_timeout(std::chrono::milliseconds timeout)
		{
			m_buf.set_send_timeout(timeout);
		}
		void set_non_blocking(bool non_blocking = true)
		{
			m_buf.set_non_blocking(non_blocking);
		}

	private:

		static socket_streambuf::setstate_fn setstate_fn(socket_stream* self)
		{
			return [self](std::ios::iostate state) { self->setstate(state); };
		}
	};
}
