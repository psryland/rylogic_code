using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;
using Rylogic.Extn;
using Rylogic.Utility;

namespace ExchApi.Common
{
	public class ExchangeApi<THasher> : IExchangeApi, IDisposable where THasher: HMAC
	{
		// Notes:
		//  Common code for all exchange APIs
		protected ExchangeApi(string key, string secret, CancellationToken shutdown, double request_rate, string base_address, string wss_address)
		{
			// Initialise the main thread id, assuming this object is constructed in the main thread
			Debug.Assert(Misc.AssertMainThread());

			Key = key;
			Secret = secret;
			Shutdown = shutdown;
			RequestThrottle = new RequestThrottle(request_rate);
			UrlRestAddress = base_address;
			UrlSocketAddress = wss_address;
			Client = new HttpClient();
			Dispatcher = Dispatcher.CurrentDispatcher;
			Hasher = Secret != null ? Util<THasher>.New(Encoding.ASCII.GetBytes(Secret)) : Util<THasher>.New();
			Subscriptions = new SubscriptionContainer(this);
		}
		public virtual void Dispose()
		{
			WebSocket = null;
			Client = null;
		}

		/// <summary>API key</summary>
		protected string Key { get; }

		/// <summary>API secret</summary>
		protected string Secret { get; }

		/// <summary></summary>
		public string UrlRestAddress { get; }

		/// <summary></summary>
		public string UrlSocketAddress { get; }

		/// <summary>Blocking method for throttling requests</summary>
		public RequestThrottle RequestThrottle { get; }

		/// <summary>Shutdown token</summary>
		public CancellationToken Shutdown { get; }

		/// <summary>For marshalling to the main thread</summary>
		protected Dispatcher Dispatcher { get; }

		/// <summary>Hasher</summary>
		protected THasher Hasher { get; }

		#region WebSocket API

		/// <summary>The web socket client</summary>
		public ClientWebSocket WebSocket
		{
			[DebuggerStepThrough]
			get { return m_web_socket; }
			protected set
			{
				if (m_web_socket == value) return;

				// Should not be opening a web socket connection while 'Shutdown' is signalled
				if (Shutdown.IsCancellationRequested && value != null)
					throw new Exception("Attempt to open a web socket connection during shutdown");

				if (m_web_socket != null)
				{
					m_web_socket_cancel.Cancel();
					Subscriptions.StopAll().Wait();
				}
				m_web_socket = value;
				if (m_web_socket != null)
				{
					m_web_socket_cancel = CancellationTokenSource.CreateLinkedTokenSource(Shutdown);
					ServiceWebSocket(m_web_socket, m_web_socket_cancel.Token);
				}

				// Handlers
				async void ServiceWebSocket(ClientWebSocket web_socket, CancellationToken shutdown)
				{
					// Notes:
					// - 'web_socket' is passed in so that changing 'WebSocket' doesn't need to wait for this async method to exit.
					// - The WebSocket protocol says that after an error the connection is always dropped.
					//   Create a new WebSocket whenever an error occurs.
					var buf = new byte[4096];
					for (; ; )
					{
						try
						{
							// Open the connection
							if (web_socket.State == WebSocketState.None)
							{
								var uri = new Uri(UrlSocketAddress);
								await web_socket.ConnectAsync(uri, shutdown);
								if (web_socket.State != WebSocketState.Open)
									throw new Exception("Connection failed");

								// Subscribe to channels
								await Subscriptions.StartAll();
							}

							// Await messages
							var data = new ArraySegment<byte>(buf, 0, buf.Length);
							var r = await web_socket.ReceiveAsync(data, shutdown);
							var count = r.Count;

							// Dynamically grow 'buf' to handle large messages
							for (; !r.EndOfMessage;)
							{
								const int MaxMessageSize = 10_000_000;
								Array.Resize(ref buf, Math.Min(buf.Length * 2, MaxMessageSize));
								data = new ArraySegment<byte>(buf, count, buf.Length - count);
								r = await web_socket.ReceiveAsync(data, shutdown);
								count += r.Count;
							}

							// Handle the message
							switch (r.MessageType)
							{
							// Server closed the connection
							case WebSocketMessageType.Close:
								WebSocketClosedStatus = r.CloseStatus;
								WebSocketClosedReason = r.CloseStatusDescription;
								break;

							// Server sent a binary message
							case WebSocketMessageType.Binary:
								await web_socket.CloseAsync(WebSocketCloseStatus.ProtocolError, "Binary message received", shutdown);
								break;

							// Server sent a text message
							case WebSocketMessageType.Text:
								OnMessageReceived(JToken.Parse(Encoding.UTF8.GetString(buf, 0, count)));
								break;
							}
						}
						catch (Exception ex)
						{
							var cancel = ex is TaskCanceledException || ex is OperationCanceledException;
							if (!cancel)
							{
								RaiseError($"WebSocket error", ex);
								shutdown.WaitHandle.WaitOne(1000); // Delay before reconnecting
							}
							break;
						}
					}

					// Close the connection
					web_socket.Dispose();

					// Reconnect unless stop has been called
					await Dispatcher.BeginInvoke(new Action(() =>
					{
						if (WebSocket == null) return;
						WebSocket = new ClientWebSocket();
					}));
				}
			}
		}
		private ClientWebSocket m_web_socket;
		private CancellationTokenSource m_web_socket_cancel;

		/// <summary>Subscriptions to web socket data streams. One per channel Id</summary>
		public SubscriptionContainer Subscriptions { get; }

		/// <summary>Process a message received from the server</summary>
		protected virtual void OnMessageReceived(JToken jtok)
		{ }

		/// <summary></summary>
		protected virtual void RaiseError(string message, Exception ex = null)
		{
			throw new Exception(message, ex);
		}

		/// <summary>The status provided by the server when it last closed the connection</summary>
		public WebSocketCloseStatus? WebSocketClosedStatus { get; private set; }

		/// <summary>The message sent when the connection was last closed</summary>
		public string WebSocketClosedReason { get; private set; }

		#endregion

		#region REST API

		/// <summary>The Http client for REST requests</summary>
		protected HttpClient Client
		{
			get { return m_client; }
			private set
			{
				if (m_client == value) return;
				if (m_client != null)
				{
					Util.Dispose(ref m_client);
				}
				m_client = value;
				if (m_client != null)
				{
					m_client.BaseAddress = new Uri(UrlRestAddress);
					m_client.Timeout = TimeSpan.FromSeconds(10);
				}
			}
		}
		private HttpClient m_client;

		#endregion
	}
}
