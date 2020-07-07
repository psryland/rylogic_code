using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Rylogic.Utility;

namespace Rylogic.Net
{
	public sealed class WebSocket :IDisposable
	{
		private readonly ClientWebSocket m_socket;
		public WebSocket(CancellationToken shutdown)
		{
			Shutdown = shutdown;

			m_socket = new ClientWebSocket();
			m_socket.Options.AddSubProtocol("Tls");
			m_socket.Options.AddSubProtocol("Tls11");
			m_socket.Options.AddSubProtocol("Tls12");
			m_socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
		}
		public void Dispose()
		{
			Heartbeat = TimeSpan.Zero;
			Listening = false;
			Util.Dispose(m_socket);
		}

		/// <summary>Shutdown signal</summary>
		private CancellationToken Shutdown { get; }

		/// <summary></summary>
		public bool Healthy => m_socket.State == WebSocketState.Open || m_socket.State == WebSocketState.Connecting;

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
		public async Task Close(WebSocketCloseStatus status = WebSocketCloseStatus.NormalClosure, string? reason = null)
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

		/// <summary>Heartbeat timer. Set to zero to disable. If enabled, calls 'OnHeartbeat' periodically</summary>
		public TimeSpan Heartbeat
		{
			get => m_heartbeat;
			set
			{
				if (m_heartbeat == value) return;
				if (m_heartbeat_timer != null)
				{
					m_heartbeat_timer.Stop();
				}
				m_heartbeat = value;
				m_heartbeat_timer = m_heartbeat != TimeSpan.Zero ? new DispatcherTimer(m_heartbeat, DispatcherPriority.Background, HandleTimer, Dispatcher.CurrentDispatcher) : null;
				if (m_heartbeat_timer != null)
				{
					m_heartbeat_timer.Start();
				}

				// Handler
				void HandleTimer(object? sender, EventArgs e)
				{
					OnHeartbeat?.Invoke(this, EventArgs.Empty);
				}
			}
		}
		private DispatcherTimer? m_heartbeat_timer;
		private TimeSpan m_heartbeat;

		/// <summary>Start/Stop listening for data</summary>
		public bool Listening
		{
			get => m_listen_thread != null;
			set
			{
				if (Listening == value) return;
				if (Listening)
				{
					m_listen_thread_exit?.Cancel();
					m_listen_thread?.Join();
				}
				m_listen_thread = value ? new Thread(new ParameterizedThreadStart(ListenEntryPoint)) : null;
				if (Listening)
				{
					m_listen_thread_exit = CancellationTokenSource.CreateLinkedTokenSource(Shutdown);
					m_listen_thread?.Start(m_listen_thread_exit.Token);
				}

				// Thread entry point
				void ListenEntryPoint(object? args)
				{
					var exit = (CancellationToken)args!;
					try
					{
						var buffer = new byte[1024];
						var data = new MemoryStream(4096);
						for (; m_socket.State == WebSocketState.Open && !exit.IsCancellationRequested;)
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
		private CancellationTokenSource? m_listen_thread_exit;
		private Thread? m_listen_thread;

		/// <summary>Receives data on System.Net.WebSockets.ClientWebSocket as an asynchronous operation.</summary>
		/// <param name="buffer">The buffer to receive the response</param>
		/// <param name="cancel">Abort token</param>
		public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancel) => m_socket.ReceiveAsync(buffer, cancel);

		/// <summary>Sends data on System.Net.WebSockets.ClientWebSocket as an asynchronous operation.</summary>
		/// <param name="buffer">The buffer containing the message to be sent.</param>
		/// <param name="message_type">One of the enumeration values that specifies whether the buffer is clear text or in a binary format.</param>
		/// <param name="end_of_message">true to indicate this is the final asynchronous send; otherwise, false.</param>
		/// <param name="cancel">A cancellation token used to propagate notification that this operation should be canceled.</param>
		public Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType message_type, bool end_of_message, CancellationToken cancel) => m_socket.SendAsync(buffer, message_type, end_of_message, cancel);

		#if NETCOREAPP3_1
		/// <summary>Receives data on System.Net.WebSockets.ClientWebSocket to a byte memory range as an asynchronous operation.</summary>
		/// <param name="buffer"></param>
		/// <param name="cancel"></param>
		public ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancel) => m_socket.ReceiveAsync(buffer, cancel);

		/// <summary>Sends data on System.Net.WebSockets.ClientWebSocket from a read-only byte memory range as an asynchronous operation.</summary>
		/// <param name="buffer">The region of memory containing the message to be sent.</param>
		/// <param name="message_type">One of the enumeration values that specifies whether the buffer is clear text or in a binary format.</param>
		/// <param name="end_of_message">true to indicate this is the final asynchronous send; otherwise, false.</param>
		/// <param name="cancel">A cancellation token used to propagate notification that this operation should be canceled.</param>
		public ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType message_type, bool end_of_message, CancellationToken cancel) => m_socket.SendAsync(buffer, message_type, end_of_message, cancel);
		#endif

		/// <summary>Receives a string on System.Net.WebSockets.ClientWebSocket as an asynchronous operation.</summary>
		/// <param name="buffer">The buffer to receive the response</param>
		/// <param name="cancel">Abort token</param>
		public async Task<string> ReceiveStringAsync(CancellationToken cancel)
		{
			var buffer = new ArraySegment<byte>(new byte[8192]);

			using var ms = new MemoryStream();
			for (; ; )
			{
				var result = await ReceiveAsync(buffer, CancellationToken.None);
				ms.Write(buffer.Array!, buffer.Offset, result.Count);
				if (result.EndOfMessage) break;
			}

			ms.Seek(0, SeekOrigin.Begin);
			using var reader = new StreamReader(ms, Encoding.UTF8);
			return reader.ReadToEnd();
		}

		/// <summary>Send a string on the System.Net.WebSockets.ClientWebSocket as an asynchronous operation.</summary>
		/// <param name="msg">The string to send</param>
		public Task SendAsync(string msg, CancellationToken cancel)
		{
			var bytes = Encoding.UTF8.GetBytes(msg);
			return SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancel);
		}

		/// <summary>Raised when the web socket connection is closed</summary>
		public event EventHandler<OpenEventArgs>? OnOpen;

		/// <summary>Raised when the web socket connection is closed</summary>
		public event EventHandler<CloseEventArgs>? OnClose;

		/// <summary>Raised when the web socket connection is closed</summary>
		public event EventHandler<ErrorEventArgs>? OnError;

		/// <summary>Raised when the web socket connection is closed</summary>
		public event EventHandler<MessageEventArgs>? OnMessage;

		/// <summary>Raised when the heartbeat timer ticks</summary>
		public event EventHandler? OnHeartbeat;
		
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
