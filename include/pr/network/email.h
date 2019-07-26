//*****************************************
// Email
//	Copyright (c) Rylogic 2019
//*****************************************

#pragma once
#include <cstdio>
#include <cstdlib>
#include <fstream>
#include <iostream>
#include <array>
#include <windows.h>
#include <winsock2.h>
#include "pr/common/fmt.h"
#include "pr/network/winsock.h"

namespace pr::network
{
	class Email
	{
		// Notes:
		//  - This is the old way to send emails. It doesn't really work nowadays because
		//    email servers require SSL and don't allow sending from unknown accounts.
		//  - Library options:
		//      VMine C++ MIME Library (GNU GPL Licence)
		//      libcurl (MIT licence) https://curl.haxx.se/libcurl/
		//      libquickmail https://sourceforge.net/projects/libquickmail/


		Winsock m_winsock;
		std::string m_to_addr;
		std::string m_from_addr;
		std::string m_subject;
		std::string m_body;

	public:
		Email()
			:m_winsock()
			,m_to_addr()
			,m_from_addr()
			,m_subject()
			,m_body()
		{}

		// Email recipient
		Email& To(std::string_view recipient)
		{
			m_to_addr = recipient;
			return *this;
		}

		// Email sender
		Email& From(std::string_view sender)
		{
			m_from_addr = sender;
			return *this;
		}

		// Email subject
		Email& Subject(std::string_view subject)
		{
			m_subject = subject;
			return *this;
		}

		// Email body
		Email& Body(std::string_view body)
		{
			m_body = body;
			return *this;
		}

		// Post this email
		// E.g. Send("smtp.gmail.com", "587")
		void Send(char const* smtp_server_name, char const* port = nullptr) const
		{
			using namespace std::literals;

			// Lookup email server's IP address and port.
			auto addr = GetAddress(smtp_server_name, "mail");
			if (port != nullptr)
				addr.sin_port = (USHORT)atoi(port);

			// Create a TCP/IP socket, no specific protocol
			auto server = Socket(PF_INET, SOCK_STREAM, 0);
			if (server == INVALID_SOCKET)
				throw std::runtime_error("Failed to open socket to mail server");

			// Connect the Socket
			Check(connect(server, reinterpret_cast<SOCKADDR const*>(&addr), sizeof(addr)), "Failed to connect to the email server");

			// Receive initial response from SMTP server
			std::array<char, 4096> buffer;
			Check(recv(server, buffer.data(), int(buffer.size()), 0), "'recv' error");

			std::string msg;
			msg.reserve(4096);

			// Send 'HELO server.com'
			msg.clear(); msg.append("HELO ").append(smtp_server_name).append("\r\n");
			Check(send(server, msg.data(), int(msg.size()), 0), "'send' HELO error");
			Check(recv(server, buffer.data(), int(buffer.size()), 0), "'recv' HELO error");

			// Send MAIL FROM: <sender@mydomain.com>
			msg.clear(); msg.append("MAIL FROM:<").append(m_from_addr).append(">\r\n");
			Check(send(server, msg.data(), int(msg.size()), 0), "'send' MAIL FROM error");
			Check(recv(server, buffer.data(), int(buffer.size()), 0), "'recv' MAIL FROM error");

			// Send RCPT TO: <receiver@domain.com>
			msg.clear(); msg.append("RCPT TO:<").append(m_to_addr).append(">\r\n");
			Check(send(server, msg.data(), int(msg.size()), 0), "'send' RCPT TO error");
			Check(recv(server, buffer.data(), int(buffer.size()), 0), "'recv' RCPT TO error");

			// Send DATA
			msg.clear(); msg.append("DATA\r\n");
			Check(send(server, msg.data(), int(msg.size()), 0), "'send' DATA error");
			Check(recv(server, buffer.data(), int(buffer.size()), 0), "'recv' DATA error");

			// Send all lines of message body
			for (std::stringstream ss(m_body); ss.good();)
			{
				msg.resize(msg.capacity());
				auto len = ss.getline(msg.data(), int(msg.size())-2).gcount();
				msg.resize(len);
				msg.append("\r\n");

				Check(send(server, msg.data(), int(msg.size()), 0), "'send' message-line error");
			}

			// Send blank line and a period
			msg.clear(); msg.append("\r\n.\r\n");
			Check(send(server, msg.data(), int(msg.size()), 0), "'send' end-message");
			Check(recv(server, buffer.data(), int(buffer.size()), 0), "'recv' end-message");

			// Send QUIT
			msg.clear(); msg.append("QUIT\r\n");
			Check(send(server, msg.data(), int(msg.size()), 0), "'send' QUIT error");
			Check(recv(server, buffer.data(), int(buffer.size()), 0), "'recv' QUIT error");
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"

namespace pr::network
{
	PRUnitTest(EmailTests)
	{
		//Email()
		//	.To("psryland@gmail.com")
		//	.From("psryland@yahoo.co.nz")
		//	.Subject("Test Email")
		//	.Body("This is a\r\ntest email")
		//	.Send("smtp.gmail.com", "25");//:587");
	}
}

#endif
