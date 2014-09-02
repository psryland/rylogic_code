#pragma once
#ifndef PR_NETWORK_TCPIP_H
#define PR_NETWORK_TCPIP_H

#include <string>
#include <sstream>
#include <vector>
#include <algorithm>
#include <mutex>
#include <thread>
#include <condition_variable>
#include <atomic>
#include <cassert>
#include <exception>

// Thanks microsoft... :-/
#if defined(_INC_WINDOWS) && !defined(_WINSOCK2API_)
#error "winsock2.h must be included before windows.h"
#endif

#include <ws2tcpip.h>
#include <winsock2.h>
#pragma comment(lib, "ws2_32.lib")

namespace pr
{
	namespace network
	{
		typedef unsigned short uint16;

		// Network exception
		struct exception : std::exception
		{
			int m_code;

			exception()                         :std::exception("")  ,m_code()     {}
			exception(char const* msg)          :std::exception(msg) ,m_code()     {}
			exception(int code)                 :std::exception("")  ,m_code(code) {}
			exception(char const* msg, int code):std::exception(msg) ,m_code(code) {}
			char const* what() const override { return std::exception::what(); }
			int         code() const          { return m_code; }
		};

		// This is a wrapper of the winsock dll. An instance of this object
		// should have the scope of all network activity
		struct Winsock :WSADATA
		{
			Winsock() :WSADATA()
			{
				auto version = MAKEWORD(2, 2);
				if (WSAStartup(version, this) != 0)
					throw exception("WSAStartup failed");
				if (wVersion != version)
					throw exception("WSAStartup - incorrect version");
			}
			~Winsock()
			{
				WSACleanup();
			}
		};

		// Throw a socket error
		inline void ThrowSocketError(DWORD code)
		{
			switch (code) {
			default:                         throw exception("Unknown socket error", code);
			case WSA_INVALID_PARAMETER:      throw exception("One or more parameters are invalid.\r\nAn application used a Windows Sockets function which directly maps to a Windows function. The Windows function is indicating a problem with one or more parameters. Note that this error is returned by the operating system, so the error number may change in future releases of Windows.", code);
			case WSA_OPERATION_ABORTED:      throw exception("Overlapped operation aborted.\r\nAn overlapped operation was canceled due to the closure of the socket, or the execution of the SIO_FLUSH command in WSAIoctl. Note that this error is returned by the operating system, so the error number may change in future releases of Windows.", code);
			case WSA_IO_INCOMPLETE:          throw exception("Overlapped I/O event object not in signaled state.\r\nThe application has tried to determine the status of an overlapped operation which is not yet completed. Applications that use WSAGetOverlappedResult (with the fWait flag set to FALSE) in a polling mode to determine when an overlapped operation has completed, get this error code until the operation is complete. Note that this error is returned by the operating system, so the error number may change in future releases of Windows.", code);
			case WSA_IO_PENDING:             throw exception("Overlapped operations will complete later.\r\nThe application has initiated an overlapped operation that cannot be completed immediately. A completion indication will be given later when the operation has been completed. Note that this error is returned by the operating system, so the error number may change in future releases of Windows.",code);
			case WSAEINTR:                   throw exception("Interrupted function call.\r\nA blocking operation was interrupted by a call to WSACancelBlockingCall.",code);
			case WSAEBADF:                   throw exception("File handle is not valid.\r\nThe file handle supplied is not valid.",code);
			case WSAEACCES:                  throw exception("Permission denied.\r\nAn attempt was made to access a socket in a way forbidden by its access permissions. An example is using a broadcast address for sendto without broadcast permission being set using setsockopt(SO_BROADCAST).\r\nAnother possible reason for the WSAEACCES error is that when the bind function is called (on Windows NT 4.0 with SP4 and later), another application, service, or kernel mode driver is bound to the same address with exclusive access. Such exclusive access is a new feature of Windows NT 4.0 with SP4 and later, and is implemented by using the SO_EXCLUSIVEADDRUSE option.",code);
			case WSAEFAULT:                  throw exception("Bad address.\r\nThe system detected an invalid pointer address in attempting to use a pointer argument of a call. This error occurs if an application passes an invalid pointer value, or if the length of the buffer is too small. For instance, if the length of an argument, which is a sockaddr structure, is smaller than the sizeof(sockaddr).",code);
			case WSAEINVAL:                  throw exception("Invalid argument.\r\nSome invalid argument was supplied (for example, specifying an invalid level to the setsockopt function). In some instances, it also refers to the current state of the socket—for instance, calling accept on a socket that is not listening.",code);
			case WSAEMFILE:                  throw exception("Too many open files.\r\nToo many open sockets. Each implementation may have a maximum number of socket handles available, either globally, per process, or per thread.",code);
			case WSAEWOULDBLOCK:             throw exception("Resource temporarily unavailable.\r\nThis error is returned from operations on nonblocking sockets that cannot be completed immediately, for example recv when no data is queued to be read from the socket. It is a nonfatal error, and the operation should be retried later. It is normal for WSAEWOULDBLOCK to be reported as the result from calling connect on a nonblocking SOCK_STREAM socket, since some time must elapse for the connection to be established.",code);
			case WSAEINPROGRESS:             throw exception("Operation now in progress.\r\nA blocking operation is currently executing. Windows Sockets only allows a single blocking operation—per- task or thread—to be outstanding, and if any other function call is made (whether or not it references that or any other socket) the function fails with the WSAEINPROGRESS error.",code);
			case WSAEALREADY:                throw exception("Operation already in progress.\r\nAn operation was attempted on a nonblocking socket with an operation already in progress—that is, calling connect a second time on a nonblocking socket that is already connecting, or canceling an asynchronous request (WSAAsyncGetXbyY) that has already been canceled or completed.",code);
			case WSAENOTSOCK:                throw exception("Socket operation on nonsocket.\r\nAn operation was attempted on something that is not a socket. Either the socket handle parameter did not reference a valid socket, or for select, a member of an fd_set was not valid.",code);
			case WSAEDESTADDRREQ:            throw exception("Destination address required.\r\nA required address was omitted from an operation on a socket. For example, this error is returned if sendto is called with the remote address of ADDR_ANY.",code);
			case WSAEMSGSIZE:                throw exception("Message too long.\r\nA message sent on a datagram socket was larger than the internal message buffer or some other network limit, or the buffer used to receive a datagram was smaller than the datagram itself.",code);
			case WSAEPROTOTYPE:              throw exception("Protocol wrong type for socket.\r\nA protocol was specified in the socket function call that does not support the semantics of the socket type requested. For example, the ARPA Internet UDP protocol cannot be specified with a socket type of SOCK_STREAM.",code);
			case WSAENOPROTOOPT:             throw exception("Bad protocol option.\r\nAn unknown, invalid or unsupported option or level was specified in a getsockopt or setsockopt call.",code);
			case WSAEPROTONOSUPPORT:         throw exception("Protocol not supported.\r\nThe requested protocol has not been configured into the system, or no implementation for it exists. For example, a socket call requests a SOCK_DGRAM socket, but specifies a stream protocol.",code);
			case WSAESOCKTNOSUPPORT:         throw exception("Socket type not supported.\r\nThe support for the specified socket type does not exist in this address family. For example, the optional type SOCK_RAW might be selected in a socket call, and the implementation does not support SOCK_RAW sockets at all.",code);
			case WSAEOPNOTSUPP:              throw exception("Operation not supported.\r\nThe attempted operation is not supported for the type of object referenced. Usually this occurs when a socket descriptor to a socket that cannot support this operation is trying to accept a connection on a datagram socket.",code);
			case WSAEPFNOSUPPORT:            throw exception("Protocol family not supported.\r\nThe protocol family has not been configured into the system or no implementation for it exists. This message has a slightly different meaning from WSAEAFNOSUPPORT. However, it is interchangeable in most cases, and all Windows Sockets functions that return one of these messages also specify WSAEAFNOSUPPORT.",code);
			case WSAEAFNOSUPPORT:            throw exception("Address family not supported by protocol family.\r\nAn address incompatible with the requested protocol was used. All sockets are created with an associated address family (that is, AF_INET for Internet Protocols) and a generic protocol type (that is, SOCK_STREAM). This error is returned if an incorrect protocol is explicitly requested in the socket call, or if an address of the wrong family is used for a socket, for example, in sendto.",code);
			case WSAEADDRINUSE:              throw exception("Address already in use.\r\nTypically, only one usage of each socket address (protocol/IP address/port) is permitted. This error occurs if an application attempts to bind a socket to an IP address/port that has already been used for an existing socket, or a socket that was not closed properly, or one that is still in the process of closing. For server applications that need to bind multiple sockets to the same port number, consider using setsockopt (SO_REUSEADDR). Client applications usually need not call bind at all—connect chooses an unused port automatically. When bind is called with a wildcard address (involving ADDR_ANY), a WSAEADDRINUSE error could be delayed until the specific address is committed. This could happen with a call to another function later, including connect, listen, WSAConnect, or WSAJoinLeaf.",code);
			case WSAEADDRNOTAVAIL:           throw exception("Cannot assign requested address.\r\nThe requested address is not valid in its context. This normally results from an attempt to bind to an address that is not valid for the local computer. This can also result from connect, sendto, WSAConnect, WSAJoinLeaf, or WSASendTo when the remote address or port is not valid for a remote computer (for example, address or port 0).",code);
			case WSAENETDOWN:                throw exception("Network is down.\r\nA socket operation encountered a dead network. This could indicate a serious failure of the network system (that is, the protocol stack that the Windows Sockets DLL runs over), the network interface, or the local network itself.",code);
			case WSAENETUNREACH:             throw exception("Network is unreachable.\r\nA socket operation was attempted to an unreachable network. This usually means the local software knows no route to reach the remote host.",code);
			case WSAENETRESET:               throw exception("Network dropped connection on reset.\r\nThe connection has been broken due to keep-alive activity detecting a failure while the operation was in progress. It can also be returned by setsockopt if an attempt is made to set SO_KEEPALIVE on a connection that has already failed.",code);
			case WSAECONNABORTED:            throw exception("Software caused connection abort.\r\nAn established connection was aborted by the software in your host computer, possibly due to a data transmission time-out or protocol error.",code);
			case WSAECONNRESET:              throw exception("Connection reset by peer.\r\nAn existing connection was forcibly closed by the remote host. This normally results if the peer application on the remote host is suddenly stopped, the host is rebooted, the host or remote network interface is disabled, or the remote host uses a hard close (see setsockopt for more information on the SO_LINGER option on the remote socket). This error may also result if a connection was broken due to keep-alive activity detecting a failure while one or more operations are in progress. Operations that were in progress fail with WSAENETRESET. Subsequent operations fail with WSAECONNRESET.",code);
			case WSAENOBUFS:                 throw exception("No buffer space available.\r\nAn operation on a socket could not be performed because the system lacked sufficient buffer space or because a queue was full.",code);
			case WSAEISCONN:                 throw exception("Socket is already connected.\r\nA connect request was made on an already-connected socket. Some implementations also return this error if sendto is called on a connected SOCK_DGRAM socket (for SOCK_STREAM sockets, the to parameter in sendto is ignored) although other implementations treat this as a legal occurrence.",code);
			case WSAENOTCONN:                throw exception("Socket is not connected.\r\nA request to send or receive data was disallowed because the socket is not connected and (when sending on a datagram socket using sendto) no address was supplied. Any other type of operation might also return this error—for example, setsockopt setting SO_KEEPALIVE if the connection has been reset.",code);
			case WSAESHUTDOWN:               throw exception("Cannot send after socket shutdown.\r\nA request to send or receive data was disallowed because the socket had already been shut down in that direction with a previous shutdown call. By calling shutdown a partial close of a socket is requested, which is a signal that sending or receiving, or both have been discontinued.",code);
			case WSAETOOMANYREFS:            throw exception("Too many references.\r\nToo many references to some kernel object.",code);
			case WSAETIMEDOUT:               throw exception("Connection timed out.\r\nA connection attempt failed because the connected party did not properly respond after a period of time, or the established connection failed because the connected host has failed to respond.",code);
			case WSAECONNREFUSED:            throw exception("Connection refused.\r\nNo connection could be made because the target computer actively refused it. This usually results from trying to connect to a service that is inactive on the foreign host—that is, one with no server application running.",code);
			case WSAELOOP:                   throw exception("Cannot translate name.\r\nCannot translate a name.",code);
			case WSAENAMETOOLONG:            throw exception("Name too long.\r\nA name component or a name was too long.",code);
			case WSAEHOSTDOWN:               throw exception("Host is down.\r\nA socket operation failed because the destination host is down. A socket operation encountered a dead host. Networking activity on the local host has not been initiated. These conditions are more likely to be indicated by the error WSAETIMEDOUT.",code);
			case WSAEHOSTUNREACH:            throw exception("No route to host.\r\nA socket operation was attempted to an unreachable host. See WSAENETUNREACH.",code);
			case WSAENOTEMPTY:               throw exception("Directory not empty.\r\nCannot remove a directory that is not empty.",code);
			case WSAEPROCLIM:                throw exception("Too many processes.\r\nA Windows Sockets implementation may have a limit on the number of applications that can use it simultaneously. WSAStartup may fail with this error if the limit has been reached.",code);
			case WSAEUSERS:                  throw exception("User quota exceeded.\r\nRan out of user quota.",code);
			case WSAEDQUOT:                  throw exception("Disk quota exceeded.\r\nRan out of disk quota.",code);
			case WSAESTALE:                  throw exception("Stale file handle reference.\r\nThe file handle reference is no longer available.",code);
			case WSAEREMOTE:                 throw exception("Item is remote.\r\nThe item is not available locally.",code);
			case WSASYSNOTREADY:             throw exception("Network subsystem is unavailable.\r\nThis error is returned by WSAStartup if the Windows Sockets implementation cannot function at this time because the underlying system it uses to provide network services is currently unavailable. Users should check:\r\nThat the appropriate Windows Sockets DLL file is in the current path.\r\nThat they are not trying to use more than one Windows Sockets implementation simultaneously. If there is more than one Winsock DLL on your system, be sure the first one in the path is appropriate for the network subsystem currently loaded.\r\nThe Windows Sockets implementation documentation to be sure all necessary components are currently installed and configured correctly.",code);
			case WSAVERNOTSUPPORTED:         throw exception("Winsock.dll version out of range.\r\nThe current Windows Sockets implementation does not support the Windows Sockets specification version requested by the application. Check that no old Windows Sockets DLL files are being accessed.",code);
			case WSANOTINITIALISED:          throw exception("Successful WSAStartup not yet performed.\r\nEither the application has not called WSAStartup or WSAStartup failed. The application may be accessing a socket that the current active task does not own (that is, trying to share a socket between tasks), or WSACleanup has been called too many times.",code);
			case WSAEDISCON:                 throw exception("Graceful shutdown in progress.\r\nReturned by WSARecv and WSARecvFrom to indicate that the remote party has initiated a graceful shutdown sequence.",code);
			case WSAENOMORE:                 throw exception("No more results.\r\nNo more results can be returned by the WSALookupServiceNext function.",code);
			case WSAECANCELLED:              throw exception("Call has been canceled.\r\nA call to the WSALookupServiceEnd function was made while this call was still processing. The call has been canceled.",code);
			case WSAEINVALIDPROCTABLE:       throw exception("Procedure call table is invalid.\r\nThe service provider procedure call table is invalid. A service provider returned a bogus procedure table to Ws2_32.dll. This is usually caused by one or more of the function pointers being NULL.",code);
			case WSAEINVALIDPROVIDER:        throw exception("Service provider is invalid.\r\nThe requested service provider is invalid. This error is returned by the WSCGetProviderInfo and WSCGetProviderInfo32 functions if the protocol entry specified could not be found. This error is also returned if the service provider returned a version number other than 2.0.",code);
			case WSAEPROVIDERFAILEDINIT:     throw exception("Service provider failed to initialize.\r\nThe requested service provider could not be loaded or initialized. This error is returned if either a service provider's DLL could not be loaded (LoadLibrary failed) or the provider's WSPStartup or NSPStartup function failed.",code);
			case WSASYSCALLFAILURE:          throw exception("System call failure.\r\nA system call that should never fail has failed. This is a generic error code, returned under various conditions.\r\nReturned when a system call that should never fail does fail. For example, if a call to WaitForMultipleEvents fails or one of the registry functions fails trying to manipulate the protocol/namespace catalogs.\r\nReturned when a provider does not return SUCCESS and does not provide an extended error code. Can indicate a service provider implementation error.",code);
			case WSASERVICE_NOT_FOUND:       throw exception("Service not found.\r\nNo such service is known. The service cannot be found in the specified name space.",code);
			case WSATYPE_NOT_FOUND:          throw exception("Class type not found.\r\nThe specified class was not found.",code);
			case WSA_E_NO_MORE:              throw exception("No more results.\r\nNo more results can be returned by the WSALookupServiceNext function.",code);
			case WSA_E_CANCELLED:            throw exception("Call was canceled.\r\nA call to the WSALookupServiceEnd function was made while this call was still processing. The call has been canceled.",code);
			case WSAEREFUSED:                throw exception("Database query was refused.\r\nA database query failed because it was actively refused.",code);
			case WSAHOST_NOT_FOUND:          throw exception("Host not found.\r\nNo such host is known. The name is not an official host name or alias, or it cannot be found in the database(s) being queried. This error may also be returned for protocol and service queries, and means that the specified name could not be found in the relevant database.",code);
			case WSATRY_AGAIN:               throw exception("Nonauthoritative host not found.\r\nThis is usually a temporary error during host name resolution and means that the local server did not receive a response from an authoritative server. A retry at some time later may be successful.",code);
			case WSANO_RECOVERY:             throw exception("This is a nonrecoverable error.\r\nThis indicates that some sort of nonrecoverable error occurred during a database lookup. This may be because the database files (for example, BSD-compatible HOSTS, SERVICES, or PROTOCOLS files) could not be found, or a DNS request was returned by the server with a severe error.",code);
			case WSANO_DATA:                 throw exception("Valid name, no data record of requested type.\r\nThe requested name is valid and was found in the database, but it does not have the correct associated data being resolved for. The usual example for this is a host name-to-address translation attempt (using gethostbyname or WSAAsyncGetHostByName) which uses the DNS (Domain Name Server). An MX record is returned but no A record—indicating the host itself exists, but is not directly reachable.",code);
			case WSA_QOS_RECEIVERS:          throw exception("QoS receivers.\r\nAt least one QoS reserve has arrived.",code);
			case WSA_QOS_SENDERS:            throw exception("QoS senders.\r\nAt least one QoS send path has arrived.",code);
			case WSA_QOS_NO_SENDERS:         throw exception("No QoS senders.\r\nThere are no QoS senders.",code);
			case WSA_QOS_NO_RECEIVERS:       throw exception("QoS no receivers.\r\nThere are no QoS receivers.",code);
			case WSA_QOS_REQUEST_CONFIRMED:  throw exception("QoS request confirmed.\r\nThe QoS reserve request has been confirmed.",code);
			case WSA_QOS_ADMISSION_FAILURE:  throw exception("QoS admission error.\r\nA QoS error occurred due to lack of resources.",code);
			case WSA_QOS_POLICY_FAILURE:     throw exception("QoS policy failure.\r\nThe QoS request was rejected because the policy system couldn't allocate the requested resource within the existing policy.",code);
			case WSA_QOS_BAD_STYLE:          throw exception("QoS bad style.\r\nAn unknown or conflicting QoS style was encountered.",code);
			case WSA_QOS_BAD_OBJECT:         throw exception("QoS bad object.\r\nA problem was encountered with some part of the filterspec or the provider-specific buffer in general.",code);
			case WSA_QOS_TRAFFIC_CTRL_ERROR: throw exception("QoS traffic control error.\r\nAn error with the underlying traffic control (TC) API as the generic QoS request was converted for local enforcement by the TC API. This could be due to an out of memory error or to an internal QoS provider error.",code);
			case WSA_QOS_GENERIC_ERROR:      throw exception("QoS generic error.\r\nA general QoS error.",code);
			case WSA_QOS_ESERVICETYPE:       throw exception("QoS service type error.\r\nAn invalid or unrecognized service type was found in the QoS flowspec.",code);
			case WSA_QOS_EFLOWSPEC:          throw exception("QoS flowspec error.\r\nAn invalid or inconsistent flowspec was found in the QOS structure.",code);
			case WSA_QOS_EPROVSPECBUF:       throw exception("Invalid QoS provider buffer.\r\nAn invalid QoS provider-specific buffer.",code);
			case WSA_QOS_EFILTERSTYLE:       throw exception("Invalid QoS filter style.\r\nAn invalid QoS filter style was used.",code);
			case WSA_QOS_EFILTERTYPE:        throw exception("Invalid QoS filter type.\r\nAn invalid QoS filter type was used.",code);
			case WSA_QOS_EFILTERCOUNT:       throw exception("Incorrect QoS filter count.\r\nAn incorrect number of QoS FILTERSPECs were specified in the FLOWDESCRIPTOR.",code);
			case WSA_QOS_EOBJLENGTH:         throw exception("Invalid QoS object length.\r\nAn object with an invalid ObjectLength field was specified in the QoS provider-specific buffer.",code);
			case WSA_QOS_EFLOWCOUNT:         throw exception("Incorrect QoS flow count.\r\nAn incorrect number of flow descriptors was specified in the QoS structure.",code);
			case WSA_QOS_EUNKOWNPSOBJ:       throw exception("Unrecognized QoS object.\r\nAn unrecognized object was found in the QoS provider-specific buffer.",code);
			case WSA_QOS_EPOLICYOBJ:         throw exception("Invalid QoS policy object.\r\nAn invalid policy object was found in the QoS provider-specific buffer.",code);
			case WSA_QOS_EFLOWDESC:          throw exception("Invalid QoS flow descriptor.\r\nAn invalid QoS flow descriptor was found in the flow descriptor list.",code);
			case WSA_QOS_EPSFLOWSPEC:        throw exception("Invalid QoS provider-specific flowspec.\r\nAn invalid or inconsistent flowspec was found in the QoS provider-specific buffer.",code);
			case WSA_QOS_EPSFILTERSPEC:      throw exception("Invalid QoS provider-specific filterspec.\r\nAn invalid FILTERSPEC was found in the QoS provider-specific buffer.",code);
			case WSA_QOS_ESDMODEOBJ:         throw exception("Invalid QoS shape discard mode object.\r\nAn invalid shape discard mode object was found in the QoS provider-specific buffer.",code);
			case WSA_QOS_ESHAPERATEOBJ:      throw exception("Invalid QoS shaping rate object.\r\nAn invalid shaping rate object was found in the QoS provider-specific buffer.",code);
			case WSA_QOS_RESERVED_PETYPE:    throw exception("Reserved policy QoS element type.\r\nA reserved policy element was found in the QoS provider-specific buffer.",code);
			}
		}

		// Wraps a call to 'getaddrinfo' and 'freeaddrinfo'
		// returning an iterable collection of address infos
		struct AddrInfo
		{
			struct AddrIter
			{
				ADDRINFOA* m_ptr;
				AddrIter() :m_ptr() {}
				ADDRINFOA* operator ->() const        { return m_ptr; }
				ADDRINFOA& operator *() const         { return *m_ptr; }
				AddrIter& operator ++()               { m_ptr = m_ptr->ai_next; return *this; }
				bool operator != (AddrIter rhs) const { return m_ptr != rhs.m_ptr; }
			} m_first;

			// Convert an IP and service (aka port) into a socket address.
			// 'ip' can be an IPv4 or IPv6 address.
			// 'service' can be a string representation of a port number or
			//  a service name like 'http', 'https', or something listed in
			//  %WINDIR%\system32\drivers\etc\services
			AddrInfo(char const* ip, char const* service, int addr_family = AF_UNSPEC, int socket_type = SOCK_STREAM, int proto = IPPROTO_TCP)
			{
				ADDRINFO hints = {};
				hints.ai_family = addr_family;
				hints.ai_socktype = socket_type;
				hints.ai_protocol = proto;
				auto r = ::getaddrinfo(ip, service, &hints, &m_first.m_ptr);
				if (r != 0)
					ThrowSocketError(WSAGetLastError());
			}
			~AddrInfo()
			{
				::freeaddrinfo(m_first.m_ptr);
			}
			AddrIter begin() const { return m_first; }
			AddrIter end() const   { return AddrIter(); }
		};

		namespace impl
		{
			// Return the maximum packet size supported by the network
			inline size_t GetMaxPacketSize(SOCKET socket)
			{
				size_t max_size;
				int max_size_size = sizeof(max_size);
				if (::getsockopt(socket, SOL_SOCKET, SO_MAX_MSG_SIZE, (char*)&max_size, &max_size_size) == SOCKET_ERROR)
					throw exception("Failed to get socket options", WSAGetLastError());

				return max_size;
			}

			// Convert an IP and service (aka port) into a socket address
			// 'ip' can be an IPv4 or IPv6 address
			// 'service' can be a string representation of a port number or
			// a service name like 'http', 'https', or something listed in
			// %WINDIR%\system32\drivers\etc\services
			inline sockaddr_in GetAddress(char const* ip, char const* service)
			{
				for (auto& i : AddrInfo(ip, service))
				{
					switch (i.ai_family)
					{
					default: break;
					case AF_INET:
					case AF_INET6:
						return *reinterpret_cast<sockaddr_in const*>(i.ai_addr);
					}
				}
				
				std::stringstream ss; ss << "Failed to resolve address: " << ip << "(" << service << ")" << std::endl;
				throw std::exception(ss.str().c_str());
			}

			// Convert an ip and port to a socket address
			inline sockaddr_in GetAddress(char const* ip, uint16 port)
			{
				char buf[32];
				return GetAddress(ip, _itoa(port, buf, 10));
			}

			// Convert a time in milliseconds to a timeval
			inline timeval TimeVal(long timeout_ms)
			{
				timeval tv;
				tv.tv_sec  = timeout_ms/1000;
				tv.tv_usec = (timeout_ms - tv.tv_sec*1000)*1000;
				return tv;
			}

			// Receive data on 'socket'
			// Returns false if the connection to the client was closed gracefully
			// Throws if the connection was aborted, or had a problem
			// 'flags' : MSG_PEEK
			inline bool Recv(SOCKET socket, void* data, size_t size, size_t& bytes_read, int timeout_ms = ~0, int flags = 0)
			{
				bytes_read = 0;
				for (auto ptr = static_cast<char*>(data); size != 0;)
				{
					// Wait for the socket to be readable
					fd_set set = {0};
					FD_SET(socket, &set);
					auto timeout = TimeVal(timeout_ms);
					int result = ::select(0, &set, 0, 0, timeout_ms == ~0 ? nullptr : &timeout);
					if (result == 0)
						return true; // timeout, no more bytes available, connection still fine
					if (result == SOCKET_ERROR)
						ThrowSocketError(WSAGetLastError());

					// Read the data
					int read = ::recv(socket, ptr, int(size), flags);
					if (read == 0)
						return false; // read zero bytes indicates the socket has been closed gracefully
					if (read == SOCKET_ERROR)
						ThrowSocketError(WSAGetLastError());

					ptr += read;
					bytes_read += read;
					size -= read;
				}
				return true;
			}

			// Receive data on 'socket'
			// Returns false if the connection to the client was closed gracefully
			// Throws if the connection was aborted, or had a problem
			// 'flags' : MSG_PEEK
			inline bool RecvFrom(SOCKET socket, char const* host_ip, uint16 host_port, void* data, size_t size, size_t& bytes_read, int timeout_ms = ~0, int flags = 0)
			{
				// Set the source address
				sockaddr_in addr = GetAddress(host_ip, host_port);

				bytes_read = 0;
				for (auto ptr = static_cast<char*>(data); size != 0;)
				{
					fd_set set = {};
					FD_SET(socket, &set);
					auto timeout = TimeVal(timeout_ms);
					int result = ::select(0, &set, 0, 0, timeout_ms == ~0 ? nullptr : &timeout);
					if (result == 0)
						return true; // timeout, no more bytes available, connection still fine
					if (result == SOCKET_ERROR)
						ThrowSocketError(WSAGetLastError());

					// Read the data
					int addr_size = sizeof(addr);
					int read = ::recvfrom(socket, ptr, int(size), flags, (sockaddr*)&addr, &addr_size);
					if (read == 0)
						return false; // read zero bytes indicates the socket has been closed gracefully
					if (read == SOCKET_ERROR)
						ThrowSocketError(WSAGetLastError());

					ptr += read;
					bytes_read += read;
					size -= read;
				}
				return true;
			}

			// Send data on 'socket'
			// Returns true if all data was sent
			// Throws if the connection was aborted, or had a problem
			inline bool Send(SOCKET socket, void const* data, size_t size, size_t max_packet_size, int timeout_ms = ~0)
			{
				// Send all of the data
				size_t bytes_sent = 0;
				for (auto ptr = static_cast<char const*>(data); size != 0;)
				{
					// Select the socket to check that there's transmit buffer space
					fd_set set = {};
					FD_SET(socket, &set);
					auto timeout = TimeVal(timeout_ms);
					int result = ::select(0, 0, &set, 0, timeout_ms == ~0 ? nullptr : &timeout);
					if (result == 0)
						return false; // timeout, connection still fine but not all bytes sent
					if (result == SOCKET_ERROR)
						ThrowSocketError(WSAGetLastError());

					// Send data
					int sent = ::send(socket, ptr, int(size > max_packet_size ? max_packet_size : size), 0);
					if (sent == SOCKET_ERROR)
						ThrowSocketError(WSAGetLastError());

					ptr += sent;
					bytes_sent += sent;
					size -= sent;
				}
				return true;
			}

			// Send data to a particular ip using 'socket'.
			// Returns true if all data was sent, false if there was a problem with the connection
			// Throws if the connection was aborted, or had a problem
			inline bool SendTo(SOCKET socket, char const* host_ip, uint16 host_port, void const* data, size_t size, size_t max_packet_size, int timeout_ms = ~0)
			{
				// Set the destination address
				sockaddr_in addr = impl::GetAddress(host_ip, host_port);

				// Send all of the data to the host
				size_t bytes_sent = 0;
				for (auto ptr = static_cast<char const*>(data); size != 0;)
				{
					// Select the socket to check that there's transmit buffer space
					fd_set set = {};
					FD_SET(socket, &set);
					auto timeout = impl::TimeVal(timeout_ms);
					int result = ::select(0, 0, &set, 0, timeout_ms == ~0 ? nullptr : &timeout);
					if (result == 0)
						return false; // timeout, connection still fine but not all bytes sent
					if (result == SOCKET_ERROR)
						ThrowSocketError(WSAGetLastError());

					// Send data
					int sent = ::sendto(socket, ptr, int(size <= max_packet_size ? size : max_packet_size), 0, (sockaddr const*)&addr, sizeof(addr));
					if (sent == SOCKET_ERROR)
						ThrowSocketError(WSAGetLastError());

					ptr += sent;
					bytes_sent += sent;
					size -= sent;
				}
				return true;
			}
		}

		// A network socket with server behaviour
		class Server
		{
			typedef std::vector<SOCKET> SocketCont;
			typedef std::unique_lock<std::mutex> Lock;

			Winsock const&          m_winsock;         // The winsock instance we're bound to
			SOCKET                  m_listen_socket;   // The socket we're listen for incoming connections on
			uint16                  m_listen_port;     // The port we're listening on
			int                     m_max_connections; // The maximum number of clients we'll accept connections from
			int                     m_protocol;        // TCP or UDP
			size_t                  m_max_packet_size; // The maximum size of a single packet that the underlying provider supports
			SocketCont              m_clients;         // The connected clients
			bool                    m_run_server;      // True while the server should run
			mutable std::mutex      m_mutex;           // Synchronise access to the clients list
			std::condition_variable m_cv_run_server;   // Sync
			std::condition_variable m_cv_clients;      // Sync
			std::thread             m_listen_thread;   // Thread that listens for incoming connections

			Server(Server const&); // no copying
			Server& operator =(Server const&);

			// Thread for listening for incoming connections
			template <typename ConnectionCB> void ListenThread(ConnectionCB connect_cb)
			{
				assert(m_listen_socket != INVALID_SOCKET && "Socket not initialised");

				// Track the number of clients
				size_t client_count = 0;
				{
					Lock lock(m_mutex);
					client_count = m_clients.size();
				}

				// Check for client connections to the server and dump old connections
				for (bool listening = false; m_run_server;)
				{
					// Set 'm_listen_socket' to listen for incoming connections mode
					if (!listening && ::listen(m_listen_socket, m_max_connections) == SOCKET_ERROR)
					{
						auto code = WSAGetLastError();
						switch (code)
						{
						case WSAEISCONN:     // The socket is already connected.
							break;

						case WSAEINPROGRESS: // A blocking Windows Sockets 1.1 call is in progress, or the service provider is still processing a callback function.
 						case WSAENETDOWN:    // The network subsystem has failed.
						case WSAEWOULDBLOCK:
							std::this_thread::sleep_for(std::chrono::milliseconds(200));
							continue; // retry

						default:
							ThrowSocketError(code);
							break;
						}
					}
					listening = true;

					try
					{
						// Wait for new connections
						if (client_count < size_t(m_max_connections))
							client_count += WaitForConnections(100, connect_cb);
						else
							std::this_thread::sleep_for(std::chrono::milliseconds(100));

						// Remove dead connections
						client_count -= RemoveDeadConnections(connect_cb);
					}
					catch (exception const& ex)
					{
						switch (ex.code()) {
						default: throw;
						case WSAENETDOWN:
						case WSAECONNRESET:
						case WSAEWOULDBLOCK:
							listening = false;
							break; // retry to listen
						}
					}
				}
			}

			// Block for up to 'timeout_ms' waiting for incoming connections
			// Returns the number of new clients added (0 or 1)
			template <typename ConnectionCB> size_t WaitForConnections(int timeout_ms, ConnectionCB connect_cb)
			{
				// Test for the listen socket being readable (meaning incoming connection request)
				fd_set set = {};
				FD_SET(m_listen_socket, &set);
				auto timeout = impl::TimeVal(timeout_ms);
				int result = ::select(0, &set, 0, 0, &timeout);
				if (result == 0)
					return 0; // no incoming connections
				if (result == SOCKET_ERROR)
					ThrowSocketError(WSAGetLastError());

				// Someone is trying to connect
				sockaddr_in client_addr;
				int client_addr_size = static_cast<int>(sizeof(client_addr));
				SOCKET client = ::accept(m_listen_socket, (sockaddr*)&client_addr, &client_addr_size);
				if (client == SOCKET_ERROR)
					ThrowSocketError(WSAGetLastError());

				// Add 'client'
				Lock lock(m_mutex);
				m_clients.push_back(client);
				m_cv_clients.notify_all();

				// Notify connect
				connect_cb(client, &client_addr);
				return 1;
			}

			// Looks for dead connections and removes them from m_clients
			// Returns the number removed
			template <typename ConnectionCB> size_t RemoveDeadConnections(ConnectionCB connect_cb)
			{
				Lock lock(m_mutex); // Lock access to 'clients'

				// Shutdown closed client sockets
				int dropped = 0;
				for (auto& client : m_clients)
				{
					// Detect shutdown sockets by those that "can be read" but return '0' bytes read
					fd_set set = {};
					FD_SET(client, &set);
					auto timeout = impl::TimeVal(0);
					int result = ::select(0, &set, 0, 0, &timeout);
					if (result == 0)
						continue; // socket cannot be read (i.e. no data, that's fine it's not closed)
					if (result == SOCKET_ERROR)
						ThrowSocketError(WSAGetLastError());

					char sink_char;
					result = ::recv(client, &sink_char, 1, MSG_PEEK);
					if (result != SOCKET_ERROR)
						continue; // read is ready and the connection is still good

					// Check the socket error for dead connection cases
					auto code = WSAGetLastError();
					switch (code) {
					case WSAEINTR:
					case WSAEINPROGRESS:
					case WSAEWOULDBLOCK:
						break;

					case WSAENOTCONN:
					case WSAENETDOWN:
					case WSAENETRESET:
					case WSAESHUTDOWN:
					case WSAECONNABORTED:
					case WSAETIMEDOUT:
					case WSAECONNRESET:
						// Notify disconnect
						connect_cb(client, nullptr);

						// Close the client socket
						::shutdown(client, SD_BOTH);
						::closesocket(client);
						client = INVALID_SOCKET;
						++dropped;
						break;

					default:
						ThrowSocketError(code);
						break;
					}
				}

				// Remove dead sockets from the container
				auto end = std::remove_if(std::begin(m_clients), std::end(m_clients), [=](SOCKET s){ return s == INVALID_SOCKET; });
				m_clients.erase(end, std::end(m_clients));
				m_cv_clients.notify_all();
				return dropped;
			}

		public:

			explicit Server(Winsock const& winsock, int protocol = IPPROTO_TCP)
				:m_winsock(winsock)
				,m_listen_socket(INVALID_SOCKET)
				,m_listen_port()
				,m_max_connections()
				,m_protocol(protocol)
				,m_max_packet_size(~0U)
				,m_run_server(false)
				,m_mutex()
				,m_cv_run_server()
				,m_cv_clients()
				,m_listen_thread()
			{}
			~Server()
			{
				StopConnections();
			}

			// The port we're listening on
			uint16_t ListenPort() const { return m_listen_port; }

			// Turn on/off the server
			// 'listen_port' is a port number of your choosing
			// 'protocol' is one of 'IPPROTO_TCP', 'IPPROTO_UDP', etc
			// ConnectionCB should have the signature:
			//   void ConnectionCB(SOCKET socket, sockaddr_in const* client_addr);
			// 'client_addr' will be non-null for connections, null for disconnections
			template <typename ConnectionCB> void AllowConnections(uint16 listen_port, ConnectionCB connect_cb, int max_connections = SOMAXCONN)
			{
				StopConnections();

				m_listen_port     = listen_port;
				m_max_connections = max_connections;

				// Create the listen socket
				switch (m_protocol)
				{
				default: throw exception("Unknown protocol");
				case IPPROTO_TCP: m_listen_socket = ::socket(AF_INET, SOCK_STREAM, IPPROTO_TCP); break;
				case IPPROTO_UDP: m_listen_socket = ::socket(AF_INET, SOCK_DGRAM,  IPPROTO_UDP); break;
				}
				if (m_listen_socket == INVALID_SOCKET)
					ThrowSocketError(WSAGetLastError());

				// Bind the local address to the socket
				sockaddr_in my_address          = {};
				my_address.sin_family           = AF_INET;
				my_address.sin_port             = htons(m_listen_port);
				my_address.sin_addr.S_un.S_addr = INADDR_ANY;
				int result = ::bind(m_listen_socket, (sockaddr const*)&my_address, sizeof(my_address));
				if (result == SOCKET_ERROR)
					ThrowSocketError(WSAGetLastError());

				// For message-oriented sockets (i.e UDP) we must not exceed the max packet size of
				// the underlying provider. Assume all clients use the same provider as m_socket
				if (m_protocol == IPPROTO_UDP)
					m_max_packet_size = impl::GetMaxPacketSize(m_listen_socket);

				// Start the thread for incoming connections
				m_run_server = true;
				m_listen_thread = std::thread([&]
				{
					try                              { ListenThread(connect_cb); }
					catch (std::exception const& ex) { assert(false && ex.what()); (void)ex; }
					catch (...)                      { assert(false && "Unhandled exception in tcp listen thread"); }
				});
			}
			void AllowConnections(uint16 listen_port, int max_connections = SOMAXCONN)
			{
				AllowConnections(listen_port, [](SOCKET, sockaddr_in const*){}, max_connections);
			}

			// Block until 'client_count' connections have been made
			bool WaitForClients(size_t client_count, int timeout_ms = ~0)
			{
				Lock lock(m_mutex);
				if (timeout_ms != ~0)
					return m_cv_clients.wait_for(lock, std::chrono::milliseconds(timeout_ms), [&]{ return m_clients.size() >= client_count; });

				m_cv_clients.wait(lock, [&] { return m_clients.size() >= client_count; });
				return true;
			}

			// Stop accepting incoming connections
			void StopConnections()
			{
				if (m_listen_socket == INVALID_SOCKET)
					return;

				{// Stop the incoming connections thread
					Lock lock(m_mutex);
					m_run_server = false;
					m_cv_run_server.notify_all();
				}

				if (m_listen_thread.joinable())
					m_listen_thread.join();

				{// Shutdown the listen socket
					::shutdown(m_listen_socket, SD_BOTH);
					::closesocket(m_listen_socket);
					m_listen_socket = INVALID_SOCKET;
				}

				{// Shutdown all client connections
					Lock lock(m_mutex);
					for (auto& client : m_clients)
					{
						::shutdown(client, SD_BOTH);
						::closesocket(client);
					}
					m_clients.resize(0);
				}
			}

			// Return the number of connected clients
			size_t ClientCount() const
			{
				Lock lock(m_mutex);
				return m_clients.size();
			}

			// Send data to a single client
			// Returns true if all data was sent, false otherwise
			bool Send(SOCKET client, void const* data, size_t size, int timeout_ms = ~0)
			{
				return impl::Send(client, data, size, m_max_packet_size, timeout_ms);
			}

			// Send data to all clients
			bool Send(void const* data, size_t size, int timeout_ms = ~0)
			{
				Lock lock(m_mutex);

				// Send the data to each client
				bool all_sent = true;
				for (auto& client : m_clients)
					all_sent &= impl::Send(client, data, size, m_max_packet_size, timeout_ms);

				return all_sent;
			}

			// Send data to a particular ip using 'socket'.
			// Returns true if all data was sent, false if there was a problem with the connection
			bool SendTo(SOCKET socket, char const* host_ip, uint16 host_port, void const* data, size_t size, int timeout_ms = ~0)
			{
				return impl::SendTo(socket, host_ip, host_port, data, size, m_max_packet_size, timeout_ms);
			}

			// Receive data from 'client'
			// Any received data is from one client only
			// Returns false if the connection to the client was closed gracefully.
			// Throws if the connection was aborted, or had a problem.
			bool Recv(SOCKET client, void* data, size_t size, size_t& bytes_read, int timeout_ms = ~0, int flags = 0)
			{
				bytes_read = 0;
				return impl::Recv(client, data, size, bytes_read, timeout_ms, flags);
			}

			// Receive data from any client
			// Returns true when data is read from a client, 'out_client' is the client that was read from
			// Returns false if no data was read from any client.
			// Throws if a connection was aborted, or had a problem
			bool Recv(void* data, size_t size, size_t& bytes_read, int timeout_ms = 0, int flags = 0, SOCKET* out_client = nullptr)
			{
				Lock lock(m_mutex);

				// Attempt to read from all clients
				for (auto& client : m_clients)
				{
					// Read data from this client, if data found, then return it
					bytes_read = 0;
					if (impl::Recv(client, data, size, bytes_read, timeout_ms, flags) && bytes_read != 0)
					{
						if (out_client) *out_client = client;
						return true;
					}
				}

				// no data read from any client
				return false;
			}

			// Receive data from client ignoring bytes_read
			bool Recv(void* data, size_t size, int timeout_ms = ~0, int flags = 0, SOCKET* out_client = nullptr)
			{
				size_t bytes_read;
				return Recv(data, size, bytes_read, timeout_ms, flags, out_client);
			}

			// Receive data from a host
			// Returns false if the connection to the client was closed gracefully
			// Throws if the connection was aborted, or had a problem
			bool RecvFrom(SOCKET socket, char const* host_ip, uint16 host_port, void* data, size_t size, size_t& bytes_read, int timeout_ms = ~0, int flags = 0)
			{
				return impl::RecvFrom(socket, host_ip, host_port, data, size, bytes_read, timeout_ms, flags);
			}
		};

		// A network socket with client behaviour
		class Client
		{
			Winsock const& m_winsock;         // The winsock instance we're bound to
			SOCKET         m_host_socket;     // The socket we've connected to the host with
			uint16         m_host_port;       // The port we're connected to
			int            m_protocol;        // TCP or UDP
			size_t         m_max_packet_size; // The maximum size of a single packet that the underlying provider supports

			Client(Client const&); // no copying
			Client& operator =(Client const&);

		public:

			explicit Client(Winsock const& winsock, int protocol = IPPROTO_TCP)
				:m_winsock(winsock)
				,m_host_socket(INVALID_SOCKET)
				,m_host_port()
				,m_protocol(protocol)
				,m_max_packet_size(~0U)
			{}
			~Client()
			{
				Disconnect();
			}

			// For a TCP connections, use IPPROTO_TCP, the ip address and port
			// For a UDP connection with a default ip/port, use IPPROTO_UDP, ip, port
			//  Send/Recv can be used with this type of connection, the UDP packets go
			//  to the specified ip/port. I.e connect sets them as the default ip/port.
			// For a UDP conection without any default ip/port, use 'nullptr, 0, IPPROTO_UDP'
			//  Send/Recv return errors for this connection type, however, SendTo and RecvFrom work.
			// Returns true if the connection is established, false on timeout. Throws on failure
			bool Connect(char const* ip, uint16 port, int timeout_ms = ~0)
			{
				Disconnect();

				// Create the socket
				m_host_socket = ::socket(AF_INET, m_protocol == IPPROTO_TCP ? SOCK_STREAM : SOCK_DGRAM, m_protocol);
				if (m_host_socket == INVALID_SOCKET)
					ThrowSocketError(WSAGetLastError());

				// Explicit binding is "not encouraged" for client connections
				//// Bind the socket to our local address
				//sockaddr_in my_address     = {0};
				//my_address.sin_family      = AF_INET;
				//my_address.sin_port        = htons(listen_port);
				//my_address.sin_addr.s_addr = INADDR_ANY;
				//if (bind(m_host, (PSOCKADDR)&my_address, sizeof(my_address)) == SOCKET_ERROR)
					//throw exception("Bind socket failed");

				// For message-oriented sockets (i.e UDP) we must not exceed
				// the max packet size of the underlying provider.
				if (m_protocol == IPPROTO_UDP)
					m_max_packet_size = impl::GetMaxPacketSize(m_host_socket);

				// If an ip address is given, attempt to connect to it
				if (ip != nullptr)
				{
					sockaddr_in host_addr = impl::GetAddress(ip, port);
					int result = ::connect(m_host_socket, (sockaddr*)&host_addr, sizeof(host_addr));
					if (result == SOCKET_ERROR)
						ThrowSocketError(WSAGetLastError());

					// Wait for the socket to say it's writable (meaning it's connected)
					fd_set set = {};
					FD_SET(m_host_socket, &set);
					auto timeout = impl::TimeVal(timeout_ms);
					result = ::select(0, 0, &set, 0, timeout_ms == ~0 ? nullptr : &timeout);
					if (result == 0)
						return false;
					if (result == SOCKET_ERROR)
						ThrowSocketError(WSAGetLastError());
				}
				return true;
			}
			void Disconnect()
			{
				if (m_host_socket == INVALID_SOCKET)
					return;

				::shutdown(m_host_socket, SD_BOTH);
				::closesocket(m_host_socket);
				m_host_socket = INVALID_SOCKET;
			}

			// Send/Recv data to/from the host
			// Returns true if all data was sent/received
			// Returns false if the connection to the client was closed gracefully
			// Throws if the connection was aborted, or had a problem
			bool Send(void const* data, size_t size, int timeout_ms = ~0)
			{
				return impl::Send(m_host_socket, data, size, m_max_packet_size, timeout_ms);
			}
			bool Recv(void* data, size_t size, size_t& bytes_read, int timeout_ms = ~0, int flags = 0)
			{
				return impl::Recv(m_host_socket, data, size, bytes_read, timeout_ms, flags);
			}
			bool Recv(void* data, size_t size, int timeout_ms = ~0, int flags = 0)
			{
				size_t bytes_read;
				return Recv(data, size, bytes_read, timeout_ms, flags);
			}

			// Send to a particular host (connection-less sockets)
			// Returns true if all data was sent/received
			// Returns false if the connection to the client was closed gracefully
			// Throws if the connection was aborted, or had a problem
			bool SendTo(char const* host_ip, uint16 host_port, void const* data, size_t size, int timeout_ms = ~0)
			{
				return impl::SendTo(m_host_port, host_ip, host_port, data, size, m_max_packet_size, timeout_ms);
			}
			bool RecvFrom(char const* host_ip, uint16 host_port, void* data, size_t size, size_t& bytes_read, int timeout_ms = ~0, int flags = 0)
			{
				return impl::RecvFrom(m_host_socket, host_ip, host_port, data, size, bytes_read, timeout_ms, flags);
			}
		};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"

namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_network_tcpip)
		{
			pr::network::Winsock wsa;
			{
				bool connected = false;

				pr::network::Server svr(wsa);
				svr.AllowConnections(54321, [&](SOCKET, sockaddr_in const* client_addr)
					{
						connected = client_addr != nullptr;
					});

				pr::network::Client client(wsa);
				client.Connect("127.0.0.1", 54321);

				PR_CHECK(svr.WaitForClients(1), true);
				PR_CHECK(connected, true);

				char const data[] = "Test data";
				PR_CHECK(svr.Send(data, sizeof(data)), true);

				char result[sizeof(data)] = {};
				PR_CHECK(client.Recv(result, sizeof(result)), true);

				PR_CHECK(data, result);
				svr.StopConnections();
			}
			{
				pr::network::Server svr(wsa);
				svr.AllowConnections(54321, 10);

				pr::network::Client client(wsa);
				client.Connect("127.0.0.1", 54321);

				PR_CHECK(svr.WaitForClients(1), true);

				char const data[] = "Test data";
				PR_CHECK(client.Send(data, sizeof(data)), true);

				char result[sizeof(data)] = {};
				PR_CHECK(svr.Recv(result, sizeof(result)), true);

				PR_CHECK(data, result);

				client.Disconnect();
				svr.StopConnections();
			}
		}
	}
}
#endif
#endif
