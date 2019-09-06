using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Rylogic.Utility;

namespace ExchApi.Common
{
	public class WebSocket :IDisposable
	{
		private readonly ClientWebSocket m_socket;
		public WebSocket(CancellationToken shutdown)
		{
			Shutdown = shutdown;
			Dispatcher = Dispatcher.CurrentDispatcher;

			m_socket = new ClientWebSocket();
			m_socket.Options.AddSubProtocol("Tls");
			m_socket.Options.AddSubProtocol("Tls11");
			m_socket.Options.AddSubProtocol("Tls12");
			m_socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
		}
		public void Dispose()
		{
			Listening = false;
			Util.Dispose(m_socket);
		}

		/// <summary>Main thread dispatcher</summary>
		private Dispatcher Dispatcher { get; }

		/// <summary>Shutdown signal</summary>
		private CancellationToken Shutdown { get; }

		/// <summary></summary>
		public bool Healthy
		{
			get =>
				m_socket.State == WebSocketState.Open ||
				m_socket.State == WebSocketState.Connecting;
		}

		/// <summary>Connect the web socket</summary>
		public async Task Connect(string endpoint)
		{
			try
			{
				// Connect to 'endpoint'
				await m_socket.ConnectAsync(new Uri(endpoint), Shutdown);
				if (m_socket.State == WebSocketState.Open)
				{
					OnOpen?.Invoke(this, new OpenEventArgs(endpoint));
				}
				else
				{
					OnError?.Invoke(this, new ErrorEventArgs($"WebSocket: Unable to connection. State = {m_socket.State}", new Exception("Connection Refused")));
				}

				// Start listening for messages
				Listening = true;
			}
			catch (Exception ex)
			{
				OnError?.Invoke(this, new ErrorEventArgs("WebSocket: Connect failed", ex));
			}
		}

		/// <summary>Gracefully close the connection</summary>
		public async Task Close(WebSocketCloseStatus status = WebSocketCloseStatus.NormalClosure, string reason = null)
		{
			try
			{
				await m_socket.CloseAsync(status, status.ToString(), Shutdown);
				if (m_socket.State == WebSocketState.Closed)
				{
					OnClose?.Invoke(this, new CloseEventArgs(status, reason ?? string.Empty));
				}
				else
				{
					OnError?.Invoke(this, new ErrorEventArgs($"WebSocket: Graceful close failed. State = {m_socket.State}", new Exception("Graceful close failed")));
				}
			}
			catch (Exception ex)
			{
				OnError?.Invoke(this, new ErrorEventArgs("WebSocket: Close connection failed", ex));
			}
		}

		/// <summary>Start/Stop listening for data</summary>
		public bool Listening
		{
			get => m_listen_thread != null;
			set
			{
				if (Listening == value) return;
				if (Listening)
				{
					m_listen_thread_exit.Cancel();
					m_listen_thread.Join();
				}
				m_listen_thread = value ? new Thread(new ParameterizedThreadStart(ListenEntryPoint)) : null;
				if (Listening)
				{
					m_listen_thread_exit = CancellationTokenSource.CreateLinkedTokenSource(Shutdown);
					m_listen_thread.Start(new { m_listen_thread_exit.Token });
				}

				// Thread entry point
				void ListenEntryPoint(dynamic args)
				{
					var exit = (CancellationToken)args.Token;
					try
					{
						var buffer = new byte[1024];
						var data = new MemoryStream(4096);
						for (; m_socket.State == WebSocketState.Open && !exit.IsCancellationRequested; )
						{
							// Receive data to build up the complete message
							var result = new WebSocketReceiveResult(0, WebSocketMessageType.Binary, false);
							for (; !result.EndOfMessage;)
							{
								// Wait for a message from the server
								result = m_socket.ReceiveAsync(new ArraySegment<byte>(buffer), exit).Result;
								if (result.MessageType == WebSocketMessageType.Close)
								{
									Close(WebSocketCloseStatus.NormalClosure).Wait();
									break;
								}

								// Append the received data
								data.Write(buffer, 0, result.Count);
							}

							// Message data was received, so output a message
							if (data.Length != 0)
							{
								var msg = new MessageEventArgs(result.MessageType, data.ToArray());
								OnMessage?.Invoke(this, msg);
								data.SetLength(0);
							}
						}
					}
					catch (Exception ex)
					{
						if (ex.InnerException is OperationCanceledException) return;
						OnError?.Invoke(this, new ErrorEventArgs($"WebSocket: Error receiving data", ex));
					}
				}
			}
		}
		private CancellationTokenSource m_listen_thread_exit;
		private Thread m_listen_thread;

		/// <summary>Raised when the web socket connection is closed</summary>
		public event EventHandler<OpenEventArgs> OnOpen;

		/// <summary>Raised when the web socket connection is closed</summary>
		public event EventHandler<CloseEventArgs> OnClose;

		/// <summary>Raised when the web socket connection is closed</summary>
		public event EventHandler<ErrorEventArgs> OnError;

		/// <summary>Raised when the web socket connection is closed</summary>
		public event EventHandler<MessageEventArgs> OnMessage;

		#region EventArgs
		public class OpenEventArgs :EventArgs
		{
			public OpenEventArgs(string endpoint)
			{
				EndPoint = endpoint;
			}

			/// <summary>The connection end point</summary>
			public string EndPoint { get; }
		}
		public class CloseEventArgs :EventArgs
		{
			public CloseEventArgs(WebSocketCloseStatus status, string reason)
			{
				Status = status;
				Reason = reason;
			}

			/// <summary>The close status</summary>
			public WebSocketCloseStatus Status { get; }
			
			/// <summary>The close reason description</summary>
			public string Reason { get; }
		}
		public class ErrorEventArgs :EventArgs
		{
			public ErrorEventArgs(string message, Exception ex)
			{
				Message = message;
				Exception = ex;
			}

			/// <summary>Gets the error message</summary>
			public string Message { get; }

			/// <summary>The exception that caused the error.</summary>
			public Exception Exception { get; }
		}
		public class MessageEventArgs :EventArgs
		{
			public MessageEventArgs(WebSocketMessageType message_type, byte[] raw_data)
			{
				MessageType = message_type;
				Bytes = raw_data;
				Text = MessageType == WebSocketMessageType.Text ? Encoding.UTF8.GetString(raw_data, 0, raw_data.Length) : string.Empty;
			}

			/// <summary>Message type</summary>
			public WebSocketMessageType MessageType { get; }

			/// <summary>The message data as an array of bytes</summary>
			public byte[] Bytes { get; }

			/// <summary>The message data as a string</summary>
			public string Text { get; }
		}
		#endregion
	}
}
