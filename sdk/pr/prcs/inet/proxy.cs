using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace pr.inet
{
	// Usage:
	// ProxyClient proxy = ProxyClient.Create(ProxyType.Http, "localhost", 6588);
	// TcpClient tcp = proxy.Connect("www.rylogic.co.nz", 80);
	
	/// <summary>The type of proxy.</summary>
	public enum ProxyType
	{
		None,
		Http,
		Socks4,
		Socks4A,
		Socks5
	}
	
	/// <summary>Proxy client base class</summary>
	public abstract class ProxyClient
	{
		/// <summary>Factory method for creating a proxy client by type enum</summary>
		public static ProxyClient Create(ProxyType type, string proxy_host, int proxy_port, string proxy_username = null, string proxy_password = null)
		{
			switch (type)
			{
			default: throw new ArgumentOutOfRangeException("type");
			case ProxyType.Http:    return new HttpProxyClient   (proxy_host, proxy_port, proxy_username, proxy_password);
			case ProxyType.Socks4:  return new Socks4ProxyClient (proxy_host, proxy_port, proxy_username, proxy_password);
			case ProxyType.Socks4A: return new Socks4AProxyClient(proxy_host, proxy_port, proxy_username, proxy_password);
			case ProxyType.Socks5:  return new Socks5ProxyClient (proxy_host, proxy_port, proxy_username, proxy_password);
			}
		}

		/// <summary>Async data for the BeginConnect/EndConnect pattern</summary>
		protected class ProxyAsyncData :IAsyncResult
		{
			private readonly TcpClient m_tcp = new TcpClient();
			private ManualResetEvent m_wait;
			private int m_cancel;
			
			/// <summary>Gets a <see cref="T:System.Threading.WaitHandle"/> that is used to wait for an asynchronous operation to complete.</summary>
			public WaitHandle AsyncWaitHandle
			{
				get { return m_wait ?? (m_wait = new ManualResetEvent(false)); }
			}

			/// <summary>Gets a value that indicates whether the asynchronous operation has completed.</summary>
			public bool IsCompleted
			{
				get { return AsyncWaitHandle.WaitOne(0); }
			}

			/// <summary>Gets a user-defined object that qualifies or contains information about an asynchronous operation.</summary>
			public object AsyncState
			{
				get { return null; }
			}

			/// <summary>Gets a value that indicates whether the asynchronous operation completed synchronously.</summary>
			public bool CompletedSynchronously
			{
				get { return false; }
			}
			
			/// <summary>Signals cancel to the async operation</summary>
			internal bool CancelPending
			{
				get { return m_cancel != 0; }
				set { if (value) Interlocked.CompareExchange(ref m_cancel, 1, 0); }
			}

			/// <summary>The result of the async operation</summary>
			internal TcpClient TcpClient { get { return m_tcp; } }

			/// <summary>Any error that occurred or null</summary>
			internal Exception Error;
		}

		/// <summary>The type of proxy connection</summary>
		public abstract ProxyType ProxyType { get; }

		/// <summary>The time (in ms) to wait for a connection to the proxy server to be established</summary>
		public int ProxyConnectTimeout { get; set; }

		/// <summary>The hostname of the proxy server to connect to</summary>
		public string ProxyHostname { get; private set; }
		
		/// <summary>The port number used to connect to the proxy server</summary>
		public int ProxyPort { get; private set; }
		
		/// <summary>The user name used to connect to the proxy server</summary>
		public string ProxyUsername { get; private set; }
		
		/// <summary>The password needed to connect to the proxy server</summary>
		public string ProxyPassword { get; private set; }

		/// <summary>Gets the TcpClient object representing the remote tcp connection through the proxy server</summary>
		public TcpClient TcpClient { get; private set; }
		
		/// <summary>Begin connecting asynchronously through the proxy server</summary>
		public IAsyncResult BeginConnect(string destination_host, int destination_port)
		{
			// Execute the async operation
			var async = new ProxyAsyncData();
			ThreadPool.QueueUserWorkItem(x => DoConnect(destination_host, destination_port, async), async);
			return async;
		}

		/// <summary>Cancels any asychronous operation that is currently active.</summary>
		public IAsyncResult Cancel(IAsyncResult ar)
		{
			ProxyAsyncData async = (ProxyAsyncData)ar;
			async.CancelPending = true;
			return async;
		}
		
		/// <summary>Complete an async connection</summary>
		public static TcpClient EndConnect(IAsyncResult ar)
		{
			ProxyAsyncData async = (ProxyAsyncData)ar;
			try
			{
				ar.AsyncWaitHandle.WaitOne();
				if (async.Error != null) throw async.Error;//throw new ProxyException(String.Format(CultureInfo.InvariantCulture, "Connection to proxy host {0} on port {1} failed.", Utils.GetHost(m_tcp_client), Utils.GetPort(m_tcp_client)), ex);
				return async.TcpClient;
			}
			finally
			{
				async.AsyncWaitHandle.Close();
			}
		}

		/// <summary>Synchronous connection</summary>
		public void Connect(string destination_host, int destination_port)
		{
			EndConnect(BeginConnect(destination_host, destination_port));
		}

		protected ProxyClient(string host, int port, string username, string password)
		{
			if (String.IsNullOrEmpty(host))
				throw new ProxyException("ProxyHost property must contain a value.");
			if (port <= 0 || port > 65535)
				throw new ProxyException("ProxyPort value must be greater than zero and less than 65535");
			
			ProxyConnectTimeout = 15000; // 15 seconds
			ProxyHostname  = host;
			ProxyPort      = port;
			ProxyUsername  = username;
			ProxyPassword  = password;
			TcpClient = new TcpClient();
		}

		/// <summary>Implement the connection through the proxy server</summary>
		protected abstract void DoConnect(string host, int port, ProxyAsyncData async);

		/// <summary>Sends 'request' over the TcpClient stream and reads a byte[] response</summary>
		protected byte[] SendCmdAndGetReply(byte[] request)
		{
			NetworkStream stream = TcpClient.GetStream();
			stream.Write(request, 0, request.Length);
			
			// Wait for the proxy server to respond
			for (int tick = 0; !stream.DataAvailable; Thread.Sleep(100), tick += 100)
			{
				if (tick <= ProxyConnectTimeout) continue;
				var ep = (IPEndPoint) TcpClient.Client.RemoteEndPoint;
				string host = ep.Address.ToString();
				string port = ep.Port.ToString(CultureInfo.InvariantCulture);
				throw new ProxyException(string.Format("A timeout while connecting to proxy server {0}:{1}.", host, port));
			}
			
			// Read the response
			using (var ms = new MemoryStream(TcpClient.ReceiveBufferSize))
			{
				byte[] buf = new byte[TcpClient.ReceiveBufferSize];
				for (int bytes = stream.Read(buf, 0, buf.Length); stream.DataAvailable; bytes = stream.Read(buf, 0, buf.Length))
					ms.Write(buf, 0, bytes);
				
				return ms.ToArray();
			}
		}
		
		/// <summary>Return a byte array of the port number in big endian</summary>
		protected static byte[] PortBytesBigEndian(int value)
		{
			byte[] array = new byte[2];
			array[0] = Convert.ToByte(value / 256);
			array[1] = Convert.ToByte(value % 256);
			return array;
		}
	}

	/// <summary>
	/// HTTP connection proxy class.  This class implements the HTTP standard proxy protocol.
	/// You can use this class to set up a connection to an HTTP proxy server.</summary>
	public class HttpProxyClient :ProxyClient
	{
		// ReSharper disable UnusedMember.Local
		private enum HttpResponseCodes
		{
			None = 0,
			Continue = 100,
			SwitchingProtocols = 101,
			Ok = 200,
			Created = 201,
			Accepted = 202,
			NonAuthoritiveInformation = 203,
			NoContent = 204,
			ResetContent = 205,
			PartialContent = 206,
			MultipleChoices = 300,
			MovedPermanetly = 301,
			Found = 302,
			SeeOther = 303,
			NotModified = 304,
			UserProxy = 305,
			TemporaryRedirect = 307,
			BadRequest = 400,
			Unauthorized = 401,
			PaymentRequired = 402,
			Forbidden = 403,
			NotFound = 404,
			MethodNotAllowed = 405,
			NotAcceptable = 406,
			ProxyAuthenticantionRequired = 407,
			RequestTimeout = 408,
			Conflict = 409,
			Gone = 410,
			PreconditionFailed = 411,
			RequestEntityTooLarge = 413,
			RequestUriTooLong = 414,
			UnsupportedMediaType = 415,
			RequestedRangeNotSatisfied = 416,
			ExpectationFailed = 417,
			InternalServerError = 500,
			NotImplemented = 501,
			BadGateway = 502,
			ServiceUnavailable = 503,
			GatewayTimeout = 504,
			HttpVersionNotSupported = 505
		}
		// ReSharper restore UnusedMember.Local

		/// <summary>The typical default port used for this type of proxy</summary>
		public const int DefaultPort = 8080;

		/// <summary>The type of proxy connection</summary>
		public override ProxyType ProxyType { get { return ProxyType.Http; } }
		
		/// <summary>Constructor.</summary>
		/// <param name="proxy_host">Host name or IP address of the proxy server.</param>
		/// <param name="proxy_port">Port number for the proxy server.</param>
		/// <param name="proxy_username">Http basic authentication user name</param>
		/// <param name="proxy_password">Http basic authentication password</param>
		public HttpProxyClient(string proxy_host, int proxy_port = DefaultPort, string proxy_username = null, string proxy_password = null)
		:base(proxy_host, proxy_port, proxy_username, proxy_password)
		{}
		
		/// <summary>
		/// Creates a remote TCP connection through a proxy server to the destination host on the destination port.
		/// 'host:port' is the host on the other side of the proxy server
		/// This method creates a connection to the proxy server and instructs the proxy server to make a pass
		/// through connection to the specified destination host on the specified port
		/// </summary>
		protected override void DoConnect(string host, int port, ProxyAsyncData async)
		{
			try
			{
				// Connect to the proxy server
				TcpClient.Connect(ProxyHostname, ProxyPort);
				
				// Build the connection message
				var request = BuildConnectionRequest(host, port);
				
				// Send the connection command to the proxy and read the response
				string response = Encoding.UTF8.GetString(SendCmdAndGetReply(request));
				
				// Interpret the response
				ParseConnectionResponse(response, host, port);
			}
			catch (Exception ex) { async.Error = ex; }
		}
		
		/// <summary>Construct the message to send for a connection request</summary>
		private byte[] BuildConnectionRequest(string host, int port)
		{
			// Connection request format:
			//  CONNECT www.rylogic.co.nz:8080 HTTP/1.0<CR><LF>
			//  HOST www.rylogic.co.nz:8080<CR><LF>
			//  Proxy-Authorization: username:password<CR><LF> // username:password is one base64 encoded string
			//  [other HTTP header lines ending with <CR><LF>]
			//  <CR><LF> // empty terminator line
			var connect_cmd = string.Format(CultureInfo.InvariantCulture,
				"CONNECT {0}:{1} HTTP/1.0\r\n" +
				"HOST {0}:{1}\r\n"
				,host, port.ToString(CultureInfo.InvariantCulture));
			
			// If a username and password are given, use them
			if (!string.IsNullOrEmpty(ProxyUsername) && !string.IsNullOrEmpty(ProxyPassword))
			{
				// Encode the username:password
				string cred = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", ProxyUsername, ProxyPassword)));
				
				// Append to the connection request message
				connect_cmd += string.Format(CultureInfo.InvariantCulture, "Proxy-Authorization: Basic {0}\r\n" ,cred);
			}
			
			// Add terminator line
			connect_cmd += "\r\n";
			return Encoding.ASCII.GetBytes(connect_cmd);
		}
		
		/// <summary>Parse the response from the proxy server</summary>
		private void ParseConnectionResponse(string response, string host, int port)
		{
			// Response format:
			//  HTTP/1.0 200 Connection Established<CR><LF>
			//  [other HTTP header lines ending with <CR><LF>]
			//  <CR><LF> // empty terminator line
			
			// Parse the reply
			string[] lines = response.Split(new[]{"\r\n"}, StringSplitOptions.None);
			string line = lines[0];
			if (line.IndexOf("HTTP", StringComparison.OrdinalIgnoreCase) == -1)
				throw new ProxyException(string.Format("Invalid response from proxy server.\r\nServer response: {0}.", line));
			
			// Extract the response code
			int code;
			int begin  = line.IndexOf(" ", StringComparison.Ordinal) + 1;
			int end    = line.IndexOf(" ", begin, StringComparison.Ordinal);
			if (!int.TryParse(line.Substring(begin, end - begin), out code))
				throw new ProxyException(String.Format("Invalid response code from proxy server.\r\nServer response: {0}.", line));
			
			// Parse the returned code
			var response_code = (HttpResponseCodes)code;
			if (response_code != HttpResponseCodes.Ok)
			{
				// Extract the response text
				switch (response_code)
				{
				case HttpResponseCodes.BadGateway:
					//HTTP/1.1 502 Proxy Error (The specified Secure Sockets Layer (SSL) port is not allowed. ISA Server is not configured to allow SSL requests from this port. Most Web browsers use port 443 for SSL requests.)
					throw new ProxyException(string.Format(CultureInfo.InvariantCulture,
						"The connection to {0}:{1} failed with error code {2}.\r\n" +
						"If you are connecting to a Microsoft ISA destination please refer " +
						"to knowledge based article Q283284 for more information.\r\n" +
						"Server response: {3}"
						,host ,port ,response_code ,line));
				default:
					throw new ProxyException(string.Format(CultureInfo.InvariantCulture,
						"The connection to {0}:{1} failed due to an unknown response code.\r\n" +
						"Server response: {2}"
						,host, port, line));
				}
			}
		}
	}

	/// <summary>Socks4 Proxy.  This class implements the Socks4 standard proxy protocol.</summary>
	/// <remarks>This class implements the Socks4 proxy protocol standard for TCP communciations.</remarks>
	public class Socks4ProxyClient :ProxyClient
	{
		/// <summary>The typical default port used for this type of proxy</summary>
		public const int DefaultPort = Socks4.DefaultPort;
		
		/// <summary>Gets String representing the name of the proxy.</summary>
		public override ProxyType ProxyType { get { return ProxyType.Socks4; } }

		/// <summary>Create a Socks4 proxy client object.</summary>
		/// <param name="proxy_host">Host name or IP address of the proxy server.</param>
		/// <param name="proxy_port">Port used to connect to proxy server.</param>
		/// <param name="proxy_username">Proxy user identification information.</param>
		/// <param name="proxy_password">Proxy password - Not used, just here for consistency of interface</param>
		public Socks4ProxyClient(string proxy_host, int proxy_port = DefaultPort, string proxy_username = null, string proxy_password = null)
		:base(proxy_host, proxy_port, proxy_username, proxy_password)
		{}
		
		/// <summary>
		/// Creates a TCP connection to the destination host through the proxy server host. 
		/// 'host:port' is the host on the other side of the proxy server
		/// This method creates a connection to the proxy server and instructs the proxy server
		/// to make a pass through connection to the specified destination host on the specified port.</summary>
		protected override void DoConnect(string host, int port, ProxyAsyncData async)
		{
			try
			{
				// Connect to the proxy server
				TcpClient.Connect(ProxyHostname, ProxyPort);
				
				// Construct the connection message
				var request = BuildConnectionRequest(host, port);
				
				// Send the connection command to the proxy and read the response
				var response = SendCmdAndGetReply(request);
				
				// Interpret the response
				ParseConnectionResponse(response, host, port);
			}
			catch (Exception ex) { async.Error = ex; }
		}

		/// <summary>Construct the message to send for a connection request</summary>
		private byte[] BuildConnectionRequest(string host, int port)
		{
			// Connect request format (bytes):
			//  [0]: Version
			//  [1]: Command code
			//  [2..3]: Port (big endian)
			//  [4..7]: IP address
			//  [8..N]: User ID
			//  [N+1]:  Null terminator byte
			byte[] dest_ip   = Dns.GetHostAddresses(host)[0].GetAddressBytes();
			byte[] dest_port = PortBytesBigEndian(port);
			byte[] userid    = Encoding.ASCII.GetBytes(ProxyUsername ?? "");
			
			// Setup the request byte array
			byte[] request = new byte[9 + userid.Length];
			request[0] = Socks4.VersionNumer;
			request[1] = Socks4.CmdConnect;
			dest_port.CopyTo(request, 2);
			dest_ip  .CopyTo(request, 4);
			userid   .CopyTo(request, 8);
			request[8 + userid.Length] = 0x00;
			return request;
		}
		
		/// <summary>Parse the response from the proxy server</summary>
		private void ParseConnectionResponse(byte[] response, string host, int port)
		{
			// The SOCKS server checks to see whether such a request should be granted
			// based on any combination of source IP address, destination IP address,
			// destination port number, the userid, and information it may obtain by
			// consulting IDENT, cf. RFC 1413.  If the request is granted, the SOCKS
			// server makes a connection to the specified port of the destination host.
			// A reply packet is sent to the client when this connection is established,
			// or when the request is rejected or the operation fails.
			// Response format (bytes):
			//  [0]: Version
			//  [1]: Result code
			//  [2..3]: Destination port
			//  [4..7]: Destination IP
			//
			// Response codes:
			//  90: request granted
			//  91: request rejected or failed
			//  92: request rejected becuase SOCKS server cannot connect to identd on the client
			//  93: request rejected because the client program and identd report different user-ids
			//
			// The SOCKS server closes its connection immediately after notifying
			// the client of a failed or rejected request. For a successful request,
			// the SOCKS server gets ready to relay traffic on both directions. This
			// enables the client to do I/O on its connection as if it were directly
			// connected to the application server.
			byte reply_code = response[1];
			if (reply_code != Socks4.RequestGranted)
			{
				// Extract the ip v4 address (4 bytes) and port (2 bytes big endian)
				var reply_host = new IPAddress(new []{response[4], response[5], response[6], response[7]});
				var reply_port = (int)BitConverter.ToUInt16(new[]{response[3], response[2]}, 0);
				Socks4.TranslateReply(reply_code, host, port, reply_host, reply_port);
			}
		}
	}
	
	/// <summary>Socks4A Proxy.  This class implements the Socks4a standard proxy protocol which is an extension of the Socks4 protocol</summary>
	/// <remarks>In Socks version 4A if the client cannot resolve the destination host's domain name to find its IP address the server will attempt to resolve it.</remarks>
	public class Socks4AProxyClient :ProxyClient
	{
		/// <summary>The typical default port used for this type of proxy</summary>
		public const int DefaultPort = Socks4.DefaultPort;
		
		/// <summary>Gets String representing the name of the proxy.</summary>
		public override ProxyType ProxyType { get { return ProxyType.Socks4A;} }
		
		public Socks4AProxyClient(string proxy_host, int proxy_port = DefaultPort, string proxy_username = null, string proxy_password = null)
		:base(proxy_host, proxy_port, proxy_username, proxy_password)
		{}
		
		/// <summary>
		/// Creates a TCP connection to the destination host through the proxy server host. 
		/// Socks4A adds support for the Socks4a extensions which allow the proxy client
		/// to optionally command the proxy server to resolve the destination host IP address.</summary>
		protected override void DoConnect(string host, int port, ProxyAsyncData async)
		{
			try
			{
				TcpClient.Connect(ProxyHostname, ProxyPort);
				
				// Construct the connection message
				var request = BuildConnectionRequest(host, port);
				
				// Send connection command to the proxy for the specified destination host and port
				var response = SendCmdAndGetReply(request);
				
				// Interpret the response
				ParseConnectionResponse(response, host, port);
			}
			catch (Exception ex) { async.Error = ex; }
		}

		/// <summary>Construct the message to send for a connection request</summary>
		private byte[] BuildConnectionRequest(string host, int port)
		{
			// PROXY SERVER REQUEST
			//Please read SOCKS4.protocol first for an description of the version 4
			//protocol. This extension is intended to allow the use of SOCKS on hosts
			//which are not capable of resolving all domain names.
			//
			//In version 4, the client sends the following packet to the SOCKS server
			//to request a CONNECT or a BIND operation:
			//
			//        +----+----+----+----+----+----+----+----+----+----+....+----+
			//        | VN | CD | DSTPORT |      DSTIP        | USERID       |NULL|
			//        +----+----+----+----+----+----+----+----+----+----+....+----+
			// # of bytes:     1    1      2              4           variable       1
			//
			//VN is the SOCKS protocol version number and should be 4. CD is the
			//SOCKS command code and should be 1 for CONNECT or 2 for BIND. NULL
			//is a byte of all zero bits.
			//
			//For version 4A, if the client cannot resolve the destination host's
			//domain name to find its IP address, it should set the first three bytes
			//of DSTIP to NULL and the last byte to a non-zero value. (This corresponds
			//to IP address 0.0.0.x, with x nonzero. As decreed by IANA  -- The
			//Internet Assigned Numbers Authority -- such an address is inadmissible
			//as a destination IP address and thus should never occur if the client
			//can resolve the domain name.) Following the NULL byte terminating
			//USERID, the client must sends the destination domain name and termiantes
			//it with another NULL byte. This is used for both CONNECT and BIND requests.
			//
			//A server using protocol 4A must check the DSTIP in the request packet.
			//If it represent address 0.0.0.x with nonzero x, the server must read
			//in the domain name that the client sends in the packet. The server
			//should resolve the domain name and make connection to the destination
			//host if it can.
			//
			//SOCKSified sockd may pass domain names that it cannot resolve to
			//the next-hop SOCKS server.
			string user_id = ProxyUsername ?? "";
			byte[] user_id_bytes = Encoding.ASCII.GetBytes(user_id);
			byte[] dest_ip = {0,0,0,1};  // build the invalid ip address as specified in the 4a protocol
			byte[] dest_port = PortBytesBigEndian(port);
			byte[] host_bytes = Encoding.ASCII.GetBytes(host);
				
			// Setup the request byte arry
			byte[] request = new byte[10 + user_id_bytes.Length + host_bytes.Length];
			request[0] = Socks4.VersionNumer;
			request[1] = Socks4.CmdConnect;
			dest_port.CopyTo(request, 2);
			dest_ip.CopyTo(request, 4);
			user_id_bytes.CopyTo(request, 8);  // copy the userid to the request byte array
			request[8 + user_id_bytes.Length] = 0x00;  // null (byte with all zeros) terminator for userId
			host_bytes.CopyTo(request, 9 + user_id_bytes.Length);  // copy the host name to the request byte array
			request[9 + user_id_bytes.Length + host_bytes.Length] = 0x00;  // null (byte with all zeros) terminator for userId
			return request;
		}
		
		/// <summary>Parse the response from the proxy server</summary>
		private void ParseConnectionResponse(byte[] response, string host, int port)
		{
			// PROXY SERVER RESPONSE
			// The SOCKS server checks to see whether such a request should be granted
			// based on any combination of source IP address, destination IP address,
			// destination port number, the userid, and information it may obtain by
			// consulting IDENT, cf. RFC 1413.  If the request is granted, the SOCKS
			// server makes a connection to the specified port of the destination host.
			// A reply packet is sent to the client when this connection is established,
			// or when the request is rejected or the operation fails.
			//
			//        +----+----+----+----+----+----+----+----+
			//        | VN | CD | DSTPORT |      DSTIP        |
			//        +----+----+----+----+----+----+----+----+
			// # of bytes:     1    1      2              4
			//
			// VN is the version of the reply code and should be 0. CD is the result
			// code with one of the following values:
			//
			//    90: request granted
			//    91: request rejected or failed
			//    92: request rejected becuase SOCKS server cannot connect to
			//        identd on the client
			//    93: request rejected because the client program and identd
			//        report different user-ids
			//
			// The remaining fields are ignored.
			//
			// The SOCKS server closes its connection immediately after notifying
			// the client of a failed or rejected request. For a successful request,
			// the SOCKS server gets ready to relay traffic on both directions. This
			// enables the client to do I/O on its connection as if it were directly
			// connected to the application server.
			byte reply_code = response[1];
			if (reply_code != Socks4.RequestGranted)
			{
				// Extract the ip v4 address (4 bytes) and port (2 bytes big endian)
				var reply_host = new IPAddress(new []{response[4], response[5], response[6], response[7]});
				var reply_port = (int)BitConverter.ToUInt16(new[]{response[3], response[2]}, 0);
				Socks4.TranslateReply(reply_code, host, port, reply_host, reply_port);
			}
		}
	}

	/// <summary>Socks5 connection proxy class.  This class implements the Socks5 standard proxy protocol.</summary>
	/// <remarks>This implementation supports TCP proxy connections with a Socks v5 server.</remarks>
	public class Socks5ProxyClient : ProxyClient
	{
		/// <summary>The typical default port used for this type of proxy</summary>
		public const int DefaultPort = Socks5.DefaultPort;
		
		/// <summary>The type of proxy connection</summary>
		public override ProxyType ProxyType { get { return ProxyType.Socks5; } }
		
		public Socks5ProxyClient(string proxy_host, int proxy_port = DefaultPort, string proxy_username = null, string proxy_password = null)
		:base(proxy_host, proxy_port, proxy_username, proxy_password)
		{}

		/// <summary>Implement the connection through the proxy server</summary>
		protected override void DoConnect(string host, int port, ProxyAsyncData async)
		{
			try
			{
				// Connect to the proxy server
				TcpClient.Connect(ProxyHostname, ProxyPort);
				
				// Decide what authentication method to use
				Socks5.AuthenticationType auth_method = !string.IsNullOrEmpty(ProxyUsername) && !string.IsNullOrEmpty(ProxyPassword)
					? Socks5.AuthenticationType.UsernamePassword
					: Socks5.AuthenticationType.None;
				
				// Determine the authentication methods supported by the server and whether we have enough info
				NegotiateAuthenticationMethod(auth_method);
				
				// Construct the connection message
				var request = BuildConnectionRequest(host, port);
				
				// Send the connection command to the proxy and read the response
				var response = SendCmdAndGetReply(request);
				
				// Interpret the response
				ParseConnectionResponse(response, host, port);
			}
			catch (Exception ex) { async.Error = ex; }
		}
		
		/// <summary>Determine the authentication methods that the server accepts and validate that 'auth_method' is one of them</summary>
		private void NegotiateAuthenticationMethod(Socks5.AuthenticationType auth_method)
		{
			// SERVER AUTHENTICATION REQUEST
			// The client connects to the server, and sends a version identifier/method selection message:
			//      +----+----------+----------+
			//      |VER | NMETHODS | METHODS  |
			//      +----+----------+----------+
			//      | 1  |    1     | 1 to 255 |
			//      +----+----------+----------+
			byte[] auth_method_request = new byte[4];
			auth_method_request[0] = Socks5.VersionNumber;
			auth_method_request[1] = 2;
			auth_method_request[2] = Socks5.AuthMethodNoAuthenticationRequired;
			auth_method_request[3] = Socks5.AuthMethodUsernamePassword;
			var auth_method_response = SendCmdAndGetReply(auth_method_request);
				
			// SERVER AUTHENTICATION RESPONSE
			// The server selects from one of the methods given in METHODS, and sends a METHOD selection message:
			//    +----+--------+
			//    |VER | METHOD |
			//    +----+--------+
			//    | 1  |   1    |
			//    +----+--------+
			//
			// If the selected METHOD is X'FF', none of the methods listed by the
			// client are acceptable, and the client MUST close the connection.
			//
			// The values currently defined for METHOD are:
			//  * X'00' NO AUTHENTICATION REQUIRED
			//  * X'01' GSSAPI
			//  * X'02' USERNAME/PASSWORD
			//  * X'03' to X'7F' IANA ASSIGNED
			//  * X'80' to X'FE' RESERVED FOR PRIVATE METHODS
			//  * X'FF' NO ACCEPTABLE METHODS
				
			// The first byte contains the socks version number (e.g. 5)
			// The second byte contains the auth method acceptable to the proxy server
			byte auth_reply = auth_method_response[1];
			if (auth_reply == Socks5.AuthMethodReplyNoAcceptableMethods)
			{
				TcpClient.Close();
				throw new ProxyException("The proxy does not accept any of the supported authentication methods.");
			}
			if (auth_reply == Socks5.AuthMethodUsernamePassword && auth_method == Socks5.AuthenticationType.None)
			{
				TcpClient.Close();
				throw new ProxyException("The proxy destination requires a username and password for authentication.");
			}
			if (auth_reply == Socks5.AuthMethodUsernamePassword)
			{
				// USERNAME / PASSWORD SERVER REQUEST
				// Once the SOCKS V5 server has started, and the client has selected the
				// Username/Password Authentication protocol, the Username/Password
				// subnegotiation begins.  This begins with the client producing a
				// Username/Password request:
				//
				//       +----+------+----------+------+----------+
				//       |VER | ULEN |  UNAME   | PLEN |  PASSWD  |
				//       +----+------+----------+------+----------+
				//       | 1  |  1   | 1 to 255 |  1   | 1 to 255 |
				//       +----+------+----------+------+----------+
				byte[] credentials = new byte[ProxyUsername.Length + ProxyPassword.Length + 3];
				credentials[0] = Socks5.VersionNumber;
				credentials[1] = (byte)ProxyUsername.Length;
				Array.Copy(Encoding.ASCII.GetBytes(ProxyUsername), 0, credentials, 2, ProxyUsername.Length);
				credentials[ProxyUsername.Length + 2] = (byte)ProxyPassword.Length;
				Array.Copy(Encoding.ASCII.GetBytes(ProxyPassword), 0, credentials, ProxyUsername.Length + 3, ProxyPassword.Length);
				
				// USERNAME / PASSWORD SERVER RESPONSE
				// The server verifies the supplied UNAME and PASSWD, and sends the
				// following response:
				//
				//   +----+--------+
				//   |VER | STATUS |
				//   +----+--------+
				//   | 1  |   1    |
				//   +----+--------+
				//
				// A STATUS field of X'00' indicates success. If the server returns a
				// `failure' (STATUS value other than X'00') status, it MUST close the
				// connection.
				var cred_response = SendCmdAndGetReply(credentials);
				if (cred_response[1] != Socks5.ReplySucceeded)
				{
					TcpClient.Close();
					throw new ProxyException("Authentication with the proxy server failed.\r\nInvalid username and/or password");
				}
			}
		}

		/// <summary>Construct the message to send for a connection request</summary>
		private byte[] BuildConnectionRequest(string host, int port)
		{
			//  The connection request is made up of 6 bytes plus the
			//  length of the variable address byte array
			//
			//  +----+-----+-------+------+----------+----------+
			//  |VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
			//  +----+-----+-------+------+----------+----------+
			//  | 1  |  1  | X'00' |  1   | Variable |    2     |
			//  +----+-----+-------+------+----------+----------+
			//
			// * VER protocol version: X'05'
			// * CMD
			//   * CONNECT X'01'
			//   * BIND X'02'
			//   * UDP ASSOCIATE X'03'
			// * RSV RESERVED
			// * ATYP address itemType of following address
			//   * IP V4 address: X'01'
			//   * DOMAINNAME: X'03'
			//   * IP V6 address: X'04'
			// * DST.ADDR desired destination address
			// * DST.PORT desired destination port in network octet order
			byte address_type = Socks5.AddressType(host);
			byte[] dest_addr = Socks5.AddressBytes(host);
			byte[] dest_port = PortBytesBigEndian(port);
			
			byte[] request = new byte[4 + dest_addr.Length + 2];
			request[0] = Socks5.VersionNumber;
			request[1] = Socks5.CmdConnect;
			request[2] = 0;
			request[3] = address_type;
			dest_addr.CopyTo(request, 4);
			dest_port.CopyTo(request, 4 + dest_addr.Length);
			return request;
		}

		/// <summary>Parse the response from the proxy server</summary>
		private void ParseConnectionResponse(byte[] response, string host, int port)
		{
			//  PROXY SERVER RESPONSE
			//  +----+-----+-------+------+----------+----------+
			//  |VER | REP |  RSV  | ATYP | BND.ADDR | BND.PORT |
			//  +----+-----+-------+------+----------+----------+
			//  | 1  |  1  | X'00' |  1   | Variable |    2     |
			//  +----+-----+-------+------+----------+----------+
			//
			// * VER protocol version: X'05'
			// * REP Reply field:
			//   * X'00' succeeded
			//   * X'01' general SOCKS server failure
			//   * X'02' connection not allowed by ruleset
			//   * X'03' Network unreachable
			//   * X'04' Host unreachable
			//   * X'05' Connection refused
			//   * X'06' TTL expired
			//   * X'07' Command not supported
			//   * X'08' Address itemType not supported
			//   * X'09' to X'FF' unassigned
			//* RSV RESERVED
			//* ATYP address itemType of following address
			byte reply_code = response[1];
			if (reply_code != Socks5.ReplySucceeded)
			{
				// Determine the bind addr:port
				string bind_addr = "";
				int bind_port = 0;
				
				byte addr_type = response[3];
				switch (addr_type)
				{
				case Socks5.AddrtypeDomainName:
					byte[] addr_bytes = new byte[Convert.ToInt32(response[4])];
					Array.Copy(response, 5, addr_bytes, 0, addr_bytes.Length);
					bind_addr = Encoding.ASCII.GetString(addr_bytes);
					bind_port = BitConverter.ToUInt16(new[]{response[6 + addr_bytes.Length], response[5 + addr_bytes.Length]}, 0);
					break;
				
				case Socks5.AddrtypeIpv4:
					byte[] ipv4_bytes = new byte[4];
					Array.Copy(response, 4, ipv4_bytes, 0, 4);
					bind_addr = new IPAddress(ipv4_bytes).ToString();
					bind_port = BitConverter.ToUInt16(new[]{response[9], response[8]}, 0);
					break;
				
				case Socks5.AddrtypeIpv6:
					byte[] ipv6_bytes = new byte[16];
					Array.Copy(response, 4, ipv6_bytes, 0, 16);
					bind_addr = new IPAddress(ipv6_bytes).ToString();
					bind_port = BitConverter.ToInt16(new[]{response[21], response[20]}, 0);
					break;
				}
				Socks5.TranslateReply(reply_code, host, port, bind_addr, bind_port);
			}
		}
	}
	
	/// <summary>Socks4 constants</summary>
	internal static class Socks4
	{
		public const byte VersionNumer = 4;
		
		/// <summary>The typical default port used for a socks4 proxy</summary>
		public const int DefaultPort = 1080;
		
		/// <summary>Command codes</summary>
		public const byte CmdConnect = 0x01;
		public const byte CmdBind    = 0x02;
		
		/// <summary>Reply codes</summary>
		public const byte RequestGranted                       = 90;
		public const byte RequestRejectedOrFailed              = 91;
		public const byte RequestRejectedCannotConnectToIdentd = 92;
		public const byte RequestRejectedDifferentIdentd       = 93;
		
		/// <summary>Convert a reply code to an exception if it's an error</summary>
		public static void TranslateReply(byte reply_code, string host, int port, IPAddress reply_host, int reply_port)
		{
			switch (reply_code)
			{
			case RequestGranted:
				break;
			case RequestRejectedOrFailed:
				throw new ProxyException(string.Format(CultureInfo.InvariantCulture,
					"The connection to {0}:{1} was rejected or failed.\r\n" +
					"The destination reported the host as {2}:{3}."
					,host ,port
					,reply_host ,reply_port.ToString(CultureInfo.InvariantCulture)));
			case RequestRejectedCannotConnectToIdentd:
				throw new ProxyException(string.Format(CultureInfo.InvariantCulture,
					"The connection to {0}:{1} was rejected because SOCKS destination cannot connect to identd on the client.\r\n" +
					"The destination reported the host as {2}:{3}."
					,host ,port
					,reply_host ,reply_port.ToString(CultureInfo.InvariantCulture)));
			case RequestRejectedDifferentIdentd:
				throw new ProxyException(string.Format(CultureInfo.InvariantCulture,
					"The connection to {0}:{1} was rejected because identd has a different user-id.\r\n" +
					"The destination reported the host as {2}:{3}."
					,host ,port
					,reply_host ,reply_port.ToString(CultureInfo.InvariantCulture)));
			default:
				throw new ProxyException(string.Format(CultureInfo.InvariantCulture,
					"The connection to {0}:{1} was rejected with an unknown reply code: {4}.\r\n" +
					"The destination reported the host as {2}:{3}."
					,host ,port
					,reply_host ,reply_port.ToString(CultureInfo.InvariantCulture)
					,reply_code.ToString(CultureInfo.InvariantCulture)));
			}
		}
	}
	
	/// <summary>Socks5 constants</summary>
	internal static class Socks5
	{
		public enum AuthenticationType { None, UsernamePassword }
		public const byte VersionNumber = 5;
		
		/// <summary>The typical default port used for a socks5 proxy</summary>
		public const int DefaultPort = 1080;
		
		/// <summary>Command codes</summary>
		public const byte CmdConnect = 0x01;
		public const byte CmdBind = 0x02;
		public const byte CmdUdpAssociate = 0x03;
		
		/// <summary>Reply codes</summary>
		public const byte ReplySucceeded = 0x00;
		public const byte ReplyGeneralSocksServerFailure = 0x01;
		public const byte ReplyConnectionNotAllowedByRuleset = 0x02;
		public const byte ReplyNetworkUnreachable = 0x03;
		public const byte ReplyHostUnreachable = 0x04;
		public const byte ReplyConnectionRefused = 0x05;
		public const byte ReplyTtlExpired = 0x06;
		public const byte ReplyCommandNotSupported = 0x07;
		public const byte ReplyAddressTypeNotSupported = 0x08;

		/// <summary>Auth methods</summary>
		public const byte AuthMethodNoAuthenticationRequired = 0x00;
		public const byte AuthMethodGssapi = 0x01;
		public const byte AuthMethodUsernamePassword = 0x02;
		public const byte AuthMethodIanaAssignedRangeBegin = 0x03;
		public const byte AuthMethodIanaAssignedRangeEnd = 0x7f;
		public const byte AuthMethodReservedRangeBegin = 0x80;
		public const byte AuthMethodReservedRangeEnd = 0xfe;
		public const byte AuthMethodReplyNoAcceptableMethods = 0xff;
		
		/// <summary>Address types</summary>
		public const byte AddrtypeDomainName = 0x03;
		public const byte AddrtypeIpv4 = 0x01;
		public const byte AddrtypeIpv6 = 0x04;
		
		/// <summary>Convert a reply code to an exception if it's an error</summary>
		public static void TranslateReply(byte reply_code, string host, int port, string reply_host, int reply_port)
		{
			switch (reply_code)
			{
			case ReplySucceeded:
				break;
			case ReplyGeneralSocksServerFailure:
				throw new ProxyException(string.Format(CultureInfo.InvariantCulture,
					"Connecting to {0}:{1} reported a general socks server connection failure.\r\n" +
					"The destination reported the host as {2}:{3}."
					,host, port
					,reply_host, reply_port.ToString(CultureInfo.InvariantCulture)));
			case ReplyConnectionNotAllowedByRuleset:
				throw new ProxyException(string.Format(CultureInfo.InvariantCulture,
					"The connection to {0}:{1} is not allowed due to the proxy rule set.\r\n" +
					"The destination reported the host as {2}:{3}."
					,host, port
					,reply_host, reply_port.ToString(CultureInfo.InvariantCulture)));
			case ReplyNetworkUnreachable:
				throw new ProxyException(string.Format(CultureInfo.InvariantCulture,
					"The connection to {0}:{1} failed because the network in unreachable.\r\n" +
					"The destination reported the host as {2}:{3}."
					,host, port
					,reply_host, reply_port.ToString(CultureInfo.InvariantCulture)));
			case ReplyHostUnreachable:
				throw new ProxyException(string.Format(CultureInfo.InvariantCulture,
					"The connection to {0}:{1} failed because the host is unreachable.\r\n" +
					"The destination reported the host as {2}:{3}."
					,host, port
					,reply_host, reply_port.ToString(CultureInfo.InvariantCulture)));
			case ReplyConnectionRefused:
				throw new ProxyException(string.Format(CultureInfo.InvariantCulture,
					"The connection to {0}:{1} was refused by the remote network.\r\n" +
					"The destination reported the host as {2}:{3}."
					,host, port
					,reply_host, reply_port.ToString(CultureInfo.InvariantCulture)));
			case ReplyTtlExpired:
				throw new ProxyException(string.Format(CultureInfo.InvariantCulture,
					"The connection to {0}:{1} failed because the time to live (TTL) expired.\r\n" +
					"The destination reported the host as {2}:{3}."
					,host, port
					,reply_host, reply_port.ToString(CultureInfo.InvariantCulture)));
			case ReplyCommandNotSupported:
				throw new ProxyException(string.Format(CultureInfo.InvariantCulture,
					"The connection to {0}:{1} failed because the connect command is not supported by the proxy server.\r\n" +
					"The destination reported the host as {2}:{3}."
					,host, port
					,reply_host, reply_port.ToString(CultureInfo.InvariantCulture)));
			case ReplyAddressTypeNotSupported:
				throw new ProxyException(string.Format(CultureInfo.InvariantCulture,
					"The connection to {0}:{1} failed because the address type is not supported.\r\n" +
					"The destination reported the host as {2}:{3}."
					,host, port
					,reply_host, reply_port.ToString(CultureInfo.InvariantCulture)));
			default:
				throw new ProxyException(string.Format(CultureInfo.InvariantCulture,
					"The connection to {0}:{1} was rejected with an unknown reply code: {4}.\r\n" +
					"The destination reported the host as {2}:{3}."
					,host, port
					,reply_host, reply_port.ToString(CultureInfo.InvariantCulture)
					,reply_code.ToString(CultureInfo.InvariantCulture)));
			}
		}
		
		/// <summary>Return the address type for a given host string</summary>
		public static byte AddressType(string host)
		{
			IPAddress ip_addr;
			if (!IPAddress.TryParse(host, out ip_addr))
				return AddrtypeDomainName;
				
			switch (ip_addr.AddressFamily)
			{
			case AddressFamily.InterNetwork:   return AddrtypeIpv4;
			case AddressFamily.InterNetworkV6: return AddrtypeIpv6;
			default: throw new ProxyException(String.Format(CultureInfo.InvariantCulture,
				"The host addess {0} of type '{1}' is not a supported address type.\r\n" +
				"The supported types are InterNetwork and InterNetworkV6."
				,host ,Enum.GetName(typeof(AddressFamily), ip_addr.AddressFamily)));
			}
		}
		
		/// <summary>Return the address converted to an array of bytes</summary>
		public static byte[] AddressBytes(string host)
		{
			var addr_type = AddressType(host);
			switch (addr_type)
			{
			default:
				throw new ProxyException(string.Format(CultureInfo.InvariantCulture, "Failed to get address as a byte array, unknown address type: {0}", addr_type));
			
			case AddrtypeIpv4:
			case AddrtypeIpv6:
				return IPAddress.Parse(host).GetAddressBytes();
			
			// Return a byte array containing a pascal style ascii string of the domain name
			case AddrtypeDomainName:
				var host_bytes = Encoding.ASCII.GetBytes(host);
				byte[] bytes = new byte[host_bytes.Length + 1];
				bytes[0] = Convert.ToByte(host_bytes.Length);
				host_bytes.CopyTo(bytes, 1);
				return bytes;
			}
		}
	}
	
	/// <summary>Proxy related exceptions</summary>
	[Serializable] public class ProxyException :Exception
	{
		public ProxyException() {}
		public ProxyException(string message) :base(message) {}
		public ProxyException(string message, Exception inner_exception) :base(message, inner_exception) {}
		protected ProxyException(SerializationInfo info, StreamingContext context) :base(info, context) {}
	}
}
