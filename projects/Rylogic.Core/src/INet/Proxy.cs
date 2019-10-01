using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace Rylogic.INet
{
	// Usage:
	//  var proxy = Proxy.Create(EType.Http, "localhost", 6588);
	//  var tcp = proxy.Connect("www.rylogic.co.nz", 80);
	
	/// <summary>Proxy base class</summary>
	public abstract class Proxy
	{
		/// <summary>Factory method for creating a proxy instance by enum</summary>
		public static Proxy Create(EType type, string proxy_host, int proxy_port, string? proxy_username = null, string? proxy_password = null)
		{
			switch (type)
			{
			default: throw new ArgumentOutOfRangeException("type");
			case EType.None:    throw new NotImplementedException("Null proxy not implemented");
			case EType.Http:    return new HttpProxy   (proxy_host, proxy_port, proxy_username, proxy_password);
			case EType.Socks4:  return new Socks4Proxy (proxy_host, proxy_port, proxy_username, proxy_password);
			case EType.Socks4A: return new Socks4AProxy(proxy_host, proxy_port, proxy_username, proxy_password);
			case EType.Socks5:  return new Socks5Proxy (proxy_host, proxy_port, proxy_username, proxy_password);
			}
		}

		/// <summary>Return the default port for the given proxy type</summary>
		public static ushort PortDefault(EType type)
		{
			switch (type)
			{
			default: throw new ArgumentOutOfRangeException("type");
			case EType.None:    return 0;
			case EType.Http:    return HttpProxy.DefaultPort;
			case EType.Socks4:  return Socks4Proxy.DefaultPort;
			case EType.Socks4A: return Socks4Proxy.DefaultPort;
			case EType.Socks5:  return Socks5Proxy.DefaultPort;
			}
		}

		/// <summary>Construct</summary>
		protected Proxy(string host, int port, string? username, string? password)
		{
			if (String.IsNullOrEmpty(host))
				throw new ProxyException("ProxyHost property must contain a value.");
			if (port <= 0 || port > 65535)
				throw new ProxyException("ProxyPort value must be greater than zero and less than 65535");

			ProxyConnectTimeout = 15000; // 15 seconds
			ProxyHostname = host;
			ProxyPort = port;
			ProxyUsername = username ?? string.Empty;
			ProxyPassword = password ?? string.Empty;
		}

		/// <summary>The type of proxy connection</summary>
		public abstract EType ProxyType { get; }

		/// <summary>The hostname of the proxy server to connect to</summary>
		public string ProxyHostname { get; private set; }
		
		/// <summary>The port number used to connect to the proxy server</summary>
		public int ProxyPort { get; private set; }
		
		/// <summary>The user name used to connect to the proxy server</summary>
		public string ProxyUsername { get; private set; }
		
		/// <summary>The password needed to connect to the proxy server</summary>
		public string ProxyPassword { get; private set; }

		/// <summary>The time (in ms) to wait for a connection to the proxy server to be established</summary>
		public int ProxyConnectTimeout { get; set; }

		/// <summary>Begin connecting asynchronously through the proxy server</summary>
		public IAsyncResult BeginConnect(string destination_host, int destination_port)
		{
			// Execute the async operation
			var async = new ProxyAsyncData();
			ThreadPool.QueueUserWorkItem(x =>
				{
					try
					{
						// Connect to the proxy server
						async.TcpClient.Connect(ProxyHostname, ProxyPort);
						
						// Connect through to the remote host
						DoConnect(async.TcpClient, destination_host, destination_port);
					}
					catch (Exception ex) { async.Error = ex; }
					finally { ((ManualResetEvent)async.AsyncWaitHandle).Set(); }
				}, async);
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
		public TcpClient EndConnect(IAsyncResult ar)
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

		/// <summary>Implement the connection through the proxy server</summary>
		protected abstract void DoConnect(TcpClient tcp, string host, int port);

		/// <summary>Sends 'request' over the TcpClient stream and reads a byte[] response</summary>
		protected byte[] SendCmdAndGetReply(TcpClient tcp, byte[] request)
		{
			var stream = tcp.GetStream();
			stream.Write(request, 0, request.Length);
			
			// Wait for the proxy server to respond
			for (int tick = 0; tick <= ProxyConnectTimeout && !stream.DataAvailable; Thread.Sleep(100), tick += 100) {}
			if (!stream.DataAvailable)
			{
				var ep = (IPEndPoint)tcp.Client.RemoteEndPoint;
				string host = ep.Address.ToString();
				string port = ep.Port.ToString(CultureInfo.InvariantCulture);
				throw new ProxyException(string.Format("A timeout while connecting to proxy server {0}:{1}.", host, port));
			}

			// Read the response
			using var ms = new MemoryStream(tcp.ReceiveBufferSize);
			byte[] buf = new byte[tcp.ReceiveBufferSize];
			while (stream.DataAvailable)
			{
				int bytes = stream.Read(buf, 0, buf.Length);
				ms.Write(buf, 0, bytes);
			}
			var reply = ms.ToArray();
			string dbg; try { dbg = Encoding.ASCII.GetString(reply); } catch { }
			return reply;
		}
		
		/// <summary>Returns a 2-byte array of the port number in big endian</summary>
		protected static byte[] PortBytesBigEndian(int port)
		{
			byte[] array = new byte[2];
			array[0] = Convert.ToByte(port / 256);
			array[1] = Convert.ToByte(port % 256);
			return array;
		}

		/// <summary>Async data for the BeginConnect/EndConnect pattern</summary>
		protected class ProxyAsyncData :IAsyncResult
		{
			/// <summary>The result of the async operation</summary>
			internal TcpClient TcpClient { get; } = new TcpClient();

			/// <summary>Gets a <see cref="T:System.Threading.WaitHandle"/> that is used to wait for an asynchronous operation to complete.</summary>
			public WaitHandle AsyncWaitHandle => m_wait ?? (m_wait = new ManualResetEvent(false));
			private ManualResetEvent? m_wait;

			/// <summary>Gets a value that indicates whether the asynchronous operation has completed.</summary>
			public bool IsCompleted => AsyncWaitHandle.WaitOne(0);

			/// <summary>Gets a user-defined object that qualifies or contains information about an asynchronous operation.</summary>
			public object? AsyncState => null;

			/// <summary>Gets a value that indicates whether the asynchronous operation completed synchronously.</summary>
			public bool CompletedSynchronously => false;

			/// <summary>Signals cancel to the async operation</summary>
			internal bool CancelPending
			{
				get => m_cancel != 0;
				set { if (value) _ = Interlocked.CompareExchange(ref m_cancel, 1, 0); }
			}
			private int m_cancel;

			/// <summary>Any error that occurred or null</summary>
			internal Exception? Error { get; set; }
		}

		/// <summary>The type of proxy.</summary>
		public enum EType
		{
			None,
			Http,
			Socks4,
			Socks4A,
			Socks5,
		}
	}

	/// <summary>
	/// HTTP connection proxy class.  This class implements the HTTP standard proxy protocol.
	/// You can use this class to set up a connection to an HTTP proxy server.</summary>
	public class HttpProxy :Proxy
	{
		/// <summary>Constructor.</summary>
		/// <param name="proxy_host">Host name or IP address of the proxy server.</param>
		/// <param name="proxy_port">Port number for the proxy server.</param>
		/// <param name="proxy_username">Http basic authentication user name</param>
		/// <param name="proxy_password">Http basic authentication password</param>
		public HttpProxy(string proxy_host, int proxy_port = DefaultPort, string? proxy_username = null, string? proxy_password = null)
			: base(proxy_host, proxy_port, proxy_username, proxy_password)
		{}

		/// <summary>The typical default port used for this type of proxy</summary>
		public const int DefaultPort = 8080;

		/// <summary>The type of proxy connection</summary>
		public override EType ProxyType => EType.Http;

		/// <summary>
		/// Creates a remote TCP connection through a proxy server to the destination host on the destination port.
		/// 'host:port' is the host on the other side of the proxy server
		/// This method creates a connection to the proxy server and instructs the proxy server to make a pass
		/// through connection to the specified destination host on the specified port
		/// </summary>
		protected override void DoConnect(TcpClient tcp, string host, int port)
		{
			// Build the connection message
			var request = BuildConnectionRequest(host, port);
				
			// Send the connection command to the proxy and read the response
			string response = Encoding.UTF8.GetString(SendCmdAndGetReply(tcp, request));
				
			// Interpret the response
			ParseConnectionResponse(response, host, port);
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
						"The connection to {0}:{1} failed with the following response code: {3}.\r\n" +
						"Server response: {2}"
						,host, port, line, response_code));
				}
			}
		}

		/// <summary></summary>
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
	}

	/// <summary>Socks4 Proxy.  This class implements the Socks4 standard proxy protocol.</summary>
	/// <remarks>This class implements the Socks4 proxy protocol standard for TCP communciations.</remarks>
	public class Socks4Proxy :Proxy
	{
		/// <summary>The typical default port used for this type of proxy</summary>
		public const int DefaultPort = Socks4.DefaultPort;
		
		/// <summary>Gets String representing the name of the proxy.</summary>
		public override EType ProxyType { get { return EType.Socks4; } }

		/// <summary>Create a Socks4 proxy client object.</summary>
		/// <param name="proxy_host">Host name or IP address of the proxy server.</param>
		/// <param name="proxy_port">Port used to connect to proxy server.</param>
		/// <param name="proxy_username">Proxy user identification information.</param>
		/// <param name="proxy_password">Proxy password - Not used, just here for consistency of interface</param>
		public Socks4Proxy(string proxy_host, int proxy_port = DefaultPort, string? proxy_username = null, string? proxy_password = null)
			:base(proxy_host, proxy_port, proxy_username, proxy_password)
		{}
		
		/// <summary>
		/// Creates a TCP connection to the destination host through the proxy server host. 
		/// 'host:port' is the host on the other side of the proxy server
		/// This method creates a connection to the proxy server and instructs the proxy server
		/// to make a pass through connection to the specified destination host on the specified port.</summary>
		protected override void DoConnect(TcpClient tcp, string host, int port)
		{
			// Construct the connection message
			var request = BuildConnectionRequest(host, port);
				
			// Send the connection command to the proxy and read the response
			var response = SendCmdAndGetReply(tcp, request);
				
			// Interpret the response
			ParseConnectionResponse(response, host, port);
		}

		/// <summary>Construct the message to send for a connection request</summary>
		protected virtual byte[] BuildConnectionRequest(string host, int port)
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
			//
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
	public class Socks4AProxy :Socks4Proxy
	{
		/// <summary>Gets String representing the name of the proxy.</summary>
		public override EType ProxyType { get { return EType.Socks4A;} }
		
		public Socks4AProxy(string proxy_host, int proxy_port = DefaultPort, string? proxy_username = null, string? proxy_password = null)
			:base(proxy_host, proxy_port, proxy_username, proxy_password)
		{}
		
		/// <summary>Construct the message to send for a connection request</summary>
		protected  override byte[] BuildConnectionRequest(string host, int port)
		{
			// Connect request format (bytes):
			//  [0]: Version
			//  [1]: Command code
			//  [2..3]: Port (big endian)
			//  [4..7]: IP address
			//  [8..N]: User ID
			//  [N+1]:  Null terminator byte
			//
			// Socks4A is used on hosts that cannot resolve all domain names.
			// For version 4A, if the client cannot resolve the destination host's domain
			// name to find its IP address, it should set the first three bytes of the IP
			// to 0 and the last byte to a non-zero value (i.e. 0.0.0.x, where x is nonzero).
			// The Internet Assigned Numbers Authority: such an address is inadmissible
			// as a destination IP address and thus should never occur if the client
			// can resolve the domain name. Following the null byte terminating the user id,
			// the client sends the destination domain name and termiantes it with another
			// null byte. This is used for both CONNECT and BIND requests.
			//
			// A server using Socks4A must check the IP in the request packet. If it matches
			// 0.0.0.x, the server reads the domain name that follows in the packet. The server
			// should resolve the domain name and make connection to the destination host if it can.
			byte[] dest_ip = {0,0,0,1};  // build the invalid ip address as specified in the 4a protocol
			byte[] dest_port = PortBytesBigEndian(port);
			byte[] host_bytes = Encoding.ASCII.GetBytes(host);
			byte[] user_id = Encoding.ASCII.GetBytes(ProxyUsername ?? "");
			
			// Setup the request byte arry
			byte[] request = new byte[10 + user_id.Length + host_bytes.Length];
			request[0] = Socks4.VersionNumer;
			request[1] = Socks4.CmdConnect;
			dest_port.CopyTo(request, 2);
			dest_ip  .CopyTo(request, 4);
			user_id  .CopyTo(request, 8);  // copy the userid to the request byte array
			request[8 + user_id.Length] = 0;  // null (byte with all zeros) terminator for userId
			host_bytes.CopyTo(request, 9 + user_id.Length);  // copy the host name to the request byte array
			request[9 + user_id.Length + host_bytes.Length] = 0;  // null (byte with all zeros) terminator for userId
			return request;
		}
	}

	/// <summary>Socks5 connection proxy class.  This class implements the Socks5 standard proxy protocol.</summary>
	/// <remarks>This implementation supports TCP proxy connections with a Socks v5 server.</remarks>
	public class Socks5Proxy : Proxy
	{
		/// <summary>The typical default port used for this type of proxy</summary>
		public const int DefaultPort = Socks5.DefaultPort;
		
		/// <summary>The type of proxy connection</summary>
		public override EType ProxyType { get { return EType.Socks5; } }
		
		public Socks5Proxy(string proxy_host, int proxy_port = DefaultPort, string? proxy_username = null, string? proxy_password = null)
			:base(proxy_host, proxy_port, proxy_username, proxy_password)
		{}

		/// <summary>Implement the connection through the proxy server</summary>
		protected override void DoConnect(TcpClient tcp, string host, int port)
		{
			// Decide what authentication method to use
			Socks5.AuthenticationType auth_method = !string.IsNullOrEmpty(ProxyUsername) && !string.IsNullOrEmpty(ProxyPassword)
				? Socks5.AuthenticationType.UsernamePassword
				: Socks5.AuthenticationType.None;
				
			// Determine the authentication methods supported by the server and whether we have enough info
			NegotiateAuthenticationMethod(tcp, auth_method);
				
			// Construct the connection message
			var request = BuildConnectionRequest(host, port);
				
			// Send the connection command to the proxy and read the response
			var response = SendCmdAndGetReply(tcp, request);
				
			// Interpret the response
			ParseConnectionResponse(response, host, port);
		}
		
		/// <summary>Determine the authentication methods that the server accepts and validate that 'auth_method' is one of them</summary>
		private void NegotiateAuthenticationMethod(TcpClient tcp, Socks5.AuthenticationType auth_method)
		{
			// Authentication methods request format (bytes):
			//  [0]: Version
			//  [1]: Number of methods queried
			//  [1..255]: Methods
			byte[] auth_method_request = new byte[4];
			auth_method_request[0] = Socks5.VersionNumber;
			auth_method_request[1] = 2;
			auth_method_request[2] = Socks5.AuthMethodNoAuthenticationRequired;
			auth_method_request[3] = Socks5.AuthMethodUsernamePassword;
			var auth_method_response = SendCmdAndGetReply(tcp, auth_method_request);
			
			// Response format:
			//  [0]: Version
			//  [1]: Method to use
			// 
			// If the selected METHOD is 'FF', none of the methods listed by the
			// client are acceptable, and the client must close the connection.
			// The values currently defined for 'method' are:
			//  00 - NO AUTHENTICATION REQUIRED
			//  01 - GSSAPI
			//  02 - USERNAME/PASSWORD
			//  03 - to '7F' IANA ASSIGNED
			//  80 - to 'FE' RESERVED FOR PRIVATE METHODS
			//  FF - NO ACCEPTABLE METHODS
				
			// The first byte contains the socks version number (e.g. 5)
			// The second byte contains the auth method acceptable to the proxy server
			byte auth_reply = auth_method_response[1];
			if (auth_reply == Socks5.AuthMethodNoAuthenticationRequired)
			{
				// All good to go
				return;
			}
			if (auth_reply == Socks5.AuthMethodUsernamePassword)
			{
				if (auth_method != Socks5.AuthenticationType.UsernamePassword)
				{
					tcp.Close();
					throw new ProxyException("The proxy destination requires a username and password for authentication.");
				}
				
				// Username/Password method:
				//  [0]: Version
				//  [1]: Username length in bytes
				//  [2..N]: Username
				//  [N+1]: password length
				//  [N+2..M]: password
				int ofs = 0;
				byte[] cred = new byte[1 + 1 + ProxyUsername.Length + 1 + ProxyPassword.Length];
				cred[ofs++] = Socks5.VersionNumber;
				cred[ofs++] = (byte)ProxyUsername.Length;
				Array.Copy(Encoding.ASCII.GetBytes(ProxyUsername), 0, cred, ofs, ProxyUsername.Length); ofs += ProxyUsername.Length;
				cred[ofs++] = (byte)ProxyPassword.Length;
				Array.Copy(Encoding.ASCII.GetBytes(ProxyPassword), 0, cred, ofs, ProxyPassword.Length);
				
				// Username/Password response:
				//  [0]: Version
				//  [1]: Status   // Status == 0 means success, failure means the connection should be closed
				var cred_response = SendCmdAndGetReply(tcp, cred);
				if (cred_response[1] != Socks5.ReplySucceeded)
				{
					tcp.Close();
					throw new ProxyException("Authentication with the proxy server failed.\r\nInvalid username and/or password");
				}
				return;
			}
			if (auth_reply == Socks5.AuthMethodReplyNoAcceptableMethods)
			{
				tcp.Close();
				throw new ProxyException("The proxy does not accept any of the supported authentication methods.");
			}
			throw new ProxyException("The proxy response was not recognised");
		}

		/// <summary>Construct the message to send for a connection request</summary>
		private byte[] BuildConnectionRequest(string host, int port)
		{
			// Connection request format:
			//  [0]: Version
			//  [1]: Connect command.      (Connect == 1, Bind == 2, UDP Associate = 3)
			//  [2]: Reserved, must be 0
			//  [3]: Address type          (IPv4 = 1, DomainName ==3, IPv6 == 4)
			//  [4..N]: Address
			//  [N+1..N+3]: Port           (big endian)
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
			// Response format:
			//  [0]: Version
			//  [1]: Reply code
			//  [2]: Reserved, must be 0
			//  [3]: Address type
			//  [4..N]: Bound address
			//  [N+1..N+3]: Bound port
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
