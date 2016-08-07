using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace Tradee
{
	/// <summary>Represents a connection between a trade data source and the 'Tradee' application.</summary>
	public abstract class TradeePipe :IDisposable
	{
		public const string PipeName = "TradeePipe";
		private const int TimeoutCooldown = 1000;
		private Dispatcher m_main_thread;
		private MemoryStream m_obuf;
		private MemoryStream m_ibuf;
				
		/// <summary>Base class for pipe connections between Tradee and a trade data source</summary>
		protected TradeePipe(Action<object> dispatch_msg)
		{
			DispatchMsg = dispatch_msg;
			m_main_thread = Dispatcher.CurrentDispatcher;
			m_obuf = new MemoryStream();
			m_ibuf = new MemoryStream();
		}
		public virtual void Dispose()
		{
			Disconnect();
		}

		/// <summary>Run 'action' in the main thread context</summary>
		protected void RunOnMainThread(Action action)
		{
			m_main_thread.BeginInvoke(action);
		}

		/// <summary>Close the connection</summary>
		public void Disconnect()
		{
			AssertMainThread();
			Pipe = null;
		}

		/// <summary>True if the connection is up</summary>
		public bool IsConnected
		{
			get
			{
				AssertMainThread();
				return Pipe != null;
			}
		}

		/// <summary>Establish the pipe connection</summary>
		protected PipeStream Pipe
		{
			get
			{
				AssertMainThread();

				// Try to reconnect the pipe on demand
				if (m_pipe == null || !m_pipe.IsConnected)
				{
					try
					{
						if (Environment.TickCount - m_last_connect_attempt > TimeoutCooldown)
						{
							m_last_connect_attempt = Environment.TickCount;
							Pipe = ConnectPipe();
						}
						else
						{
							Pipe = null;
						}
					}
					catch (TimeoutException)
					{
						// Timeout means Tradee isn't there...
						Debug.WriteLine("TradeeProxy connection timeout");
					}
					catch (Exception ex)
					{
						// There's something wrong with the pipe, try make a new one
						Debug.WriteLine(string.Format("TradeeProxy connect Exception {0}", ex.Message));
					}
				}
				return m_pipe;
			}
			set
			{
				AssertMainThread();

				if (m_pipe == value) return;
				if (m_pipe != null)
				{
					m_pipe.Dispose();
				}
				m_pipe = value;
				if (m_pipe != null)
				{
					Debug.WriteLine("TradeePipe connected");

					// When a new pipe is assigned, start an async read call of zero bytes, to peek for data
					var buf = new byte[1024];
					var ctx = new AsyncPipeData{ pipe = m_pipe, buf = buf, len = buf.Length };
					PeekPipeData(ctx);
				}
				OnConnectionChanged();
			}
		}
		private PipeStream m_pipe;
		private int m_last_connect_attempt;
		protected struct AsyncPipeData
		{
			public PipeStream pipe;
			public byte[] buf;
			public int len;
		}
		protected virtual PipeStream ConnectPipe()
		{
			return null;
		}

		/// <summary>Send a message over the pipe</summary>
		public bool Post<T>(T msg) where T:ITradeeMsg
		{
			AssertMainThread();
			try
			{
				// Post the message if the pipe is connected
				if (Pipe == null)
					return false;

				// Serialise the message
				m_obuf.SetLength(0);
				using (var bw = new BinaryWriter(m_obuf, Encoding.UTF8, true))
				{
					// Add the message type
					bw.Write((int)msg.ToMsgType());

					// Add the message
					msg.Serialise(bw);

					// Calculate a checksum
					bw.Write(CheckSum(m_obuf, 0, (int)m_obuf.Position));

					// Send the message
					var buf = m_obuf.GetBuffer();
					var len = (int)m_obuf.Length;
					Pipe.Write(buf, 0, len);
					Pipe.WaitForPipeDrain();
					RunOnMainThread(RaisePosted);
					return true;
				}
			}
			catch (Exception ex)
			{
				// If something is wrong, disconnect the pipe
				Debug.WriteLine(string.Format("Post Failed: {0}", ex.Message));
				Disconnect();
				return false;
			}
		}

		/// <summary>Start an async read to peek for incoming data</summary>
		private void PeekPipeData(AsyncPipeData ctx)
		{
			try
			{
				if (!ctx.pipe.IsConnected) return;
				ctx.pipe.BeginRead(ctx.buf, 0, 0, HandleDataRead, ctx);
			}
			catch {}
		}

		/// <summary>Calculate a checksum</summary>
		private int CheckSum(MemoryStream ms, int offset, int count)
		{
			unchecked
			{
				var crc = 0x1EDC6F41;
				var buf = ms.GetBuffer();
				for (int i = offset; i != offset + count; ++i)
					crc = (crc * 397) ^ buf[i];

				return crc;
			}
		}

		/// <summary>Handle the async read operation completing</summary>
		protected void HandleDataRead(IAsyncResult ar)
		{
			// This is called in a background thread
			// Check that the pipe the read was started on is still valid
			var ctx = (AsyncPipeData)ar.AsyncState;
			ctx.pipe.EndRead(ar);
			if (!ar.IsCompleted)
				return;

			try
			{
				var pipe = ctx.pipe;
				var buf = ctx.buf;

				// Append the available data to m_ibuf
				m_ibuf.Position = m_ibuf.Length;
				for (var len = buf.Length; len == buf.Length;)
				{
					len = pipe.Read(buf, 0, buf.Length);
					m_ibuf.Write(buf, 0, len);
				}

				// Deserialise from the start of m_ibuf. If there's not enough data
				// then we'll just wait for more data to arrive
				m_ibuf.Position = 0;
				using (var br = new BinaryReader(m_ibuf, Encoding.UTF8, true))
				{
					// Scope to break if there isn't enough data
					for (;;)
					{
						// Read the message type
						if (m_ibuf.Length < 4) break;
						var msg_type = (EMsgType)br.ReadInt32();
						if (!Enum.IsDefined(typeof(EMsgType), msg_type))
							throw new InvalidDataException(string.Format("Unknown message type {0}", (int)msg_type));

						// Attempt to deserialise the message
						var msg = (ITradeeMsg)Activator.CreateInstance(msg_type.ToTradeeMsgType());
						msg.Deserialise(br);

						// Read and check the checksum value
						var crc = CheckSum(m_ibuf, 0, (int)m_ibuf.Position);
						if (crc != br.ReadInt32())
							throw new InvalidDataException(string.Format("Checksum failure for message type {0}", msg_type));

						// Handle the received message
						RunOnMainThread(() => DispatchMsg(msg));

						// Consume bytes from the front of 'm_ibuf'
						var remaining = (int)(m_ibuf.Length - m_ibuf.Position);
						if (remaining == 0)
						{
							m_ibuf.SetLength(0);
						}
						else
						{
							var leftover = m_ibuf.GetBuffer();
							var offset   = (int)m_ibuf.Position;

							m_ibuf.Position = 0;
							m_ibuf.Write(leftover, offset, remaining);
							m_ibuf.SetLength(remaining);
						}
						break;
					}
				}
			}
			catch (EndOfStreamException)
			{
				// End of stream .. wait for more data
			}
			catch (Exception ex)
			{
				// Invalid data... abort the connection
				Debug.WriteLine(ex.Message);
				m_ibuf.SetLength(0);
				RunOnMainThread(Disconnect);
				return;
			}

			// Start a new read
			PeekPipeData(ctx);
		}

		/// <summary>Raised when the pipe is connection/disconnected</summary>
		public event EventHandler ConnectionChanged;
		protected void OnConnectionChanged()
		{
			if (ConnectionChanged == null) return;
			ConnectionChanged(this, EventArgs.Empty);
		}

		/// <summary>Raised when data is posted</summary>
		public event EventHandler Posted;
		private void RaisePosted()
		{
			if (Posted == null) return;
			Posted(this, EventArgs.Empty);
		}

		/// <summary>The callback function to send received messages to</summary>
		protected Action<object> DispatchMsg
		{
			get;
			private set;
		}

		/// <summary>Protect against cross thread use</summary>
		protected void AssertMainThread()
		{
			if (m_main_thread.Thread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId) return;
			throw new Exception("Cross thread use of TradeePipe");
		}
	}

	/// <summary>Represents a connection from a trade data source to the 'Tradee' application</summary>
	public class TradeeProxy :TradeePipe
	{
		/// <summary>
		/// Create a connection to the 'Tradee' application.
		/// 'DispatchMsg' is a function provided by the trade data source to handle
		/// requests sent out from 'Tradee'.</summary>
		public TradeeProxy(Action<object> DispatchMsg)
			:base(DispatchMsg)
		{}

		/// <summary>Connect to the server</summary>
		protected override PipeStream ConnectPipe()
		{
			var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
			pipe.Connect(10);
			return pipe;
		}
	}

	/// <summary>Represents a connection from the 'Tradee' application to a trade data source</summary>
	public class TradeeClient :TradeePipe
	{
		public TradeeClient(Action<object> dispatch_msg)
			:base(dispatch_msg)
		{
			ThreadPool.QueueUserWorkItem(ListenForConnections);
		}

		/// <summary>Wait for clients to connect</summary>
		private void ListenForConnections(object x)
		{
			try
			{
				// Create a new pipe and wait (asynchronously) for a connection
				var pipe = new NamedPipeServerStream(PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
				var ar = pipe.BeginWaitForConnection(null, pipe);

				// Wait for connection
				ar.AsyncWaitHandle.WaitOne();
				pipe.EndWaitForConnection(ar);
				RunOnMainThread(() => Pipe = pipe);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(string.Format("ListenForConnections failed: {0}", ex.Message));
			}

			// Wait for the next connection
			ThreadPool.QueueUserWorkItem(ListenForConnections);
		}
	}
}
