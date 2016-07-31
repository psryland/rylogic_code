using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Threading;

namespace Tradee
{
	/// <summary>Represents a connection between a trade data source and the 'Tradee' application.</summary>
	public abstract class TradeePipe :IDisposable
	{
		public const string PipeName = "TradeePipe";
		private Dispatcher m_main_thread;
		private object m_lock;
		
		/// <summary>Base class for pipe connections between Tradee and a trade data source</summary>
		protected TradeePipe(Action<object> dispatch_msg)
		{
			m_main_thread = Dispatcher.CurrentDispatcher;
			m_lock = new object();
			DispatchMsg = dispatch_msg;
		}
		public virtual void Dispose()
		{
			Pipe = null;
		}

		/// <summary>True if the connection is up</summary>
		public bool IsConnected
		{
			get { return Pipe != null && Pipe.IsConnected; }
		}

		/// <summary>Raised when the pipe is connection/disconnected</summary>
		public event EventHandler ConnectionChanged;
		private void RaiseConnectionChanged()
		{
			if (ConnectionChanged == null) return;
			ConnectionChanged(this, EventArgs.Empty);
		}

		/// <summary>The connection</summary>
		protected PipeStream Pipe
		{
			get
			{
				lock (m_lock)
				{
					// Try to reconnect the pipe
					if (m_impl_pipe == null || !m_impl_pipe.IsConnected)
					{
						Pipe = null;
						ConnectPipe();
					}
					return m_impl_pipe;
				}
			}
			set
			{
				lock (m_lock)
				{
					if (m_impl_pipe == value) return;
					if (m_impl_pipe != null)
					{
						m_impl_pipe.Dispose();
					}
					m_impl_pipe = value;
					if (m_impl_pipe != null)
					{
						// When a new pipe is assigned, start an async read call of zero bytes, to peek for data
						var buf = new byte[0];
						m_impl_pipe.BeginRead(buf, 0, buf.Length, HandleDataRead, new ReadData{ pipe = m_impl_pipe, buf = buf });
					}
					RaiseConnectionChanged();
				}
			}
		}
		private struct ReadData { public PipeStream pipe; public byte[] buf; }
		private PipeStream m_impl_pipe;

		/// <summary>Handle the async read operation completing</summary>
		private void HandleDataRead(IAsyncResult ar)
		{
			try
			{
				// This is called in a background thread
				var rd = (ReadData)ar.AsyncState;
				var pipe = rd.pipe;
				var buf = rd.buf;

				// Data is waiting
				pipe.EndRead(ar);
				if (ar.IsCompleted && pipe.IsConnected)
				{
					try
					{
						// Deserialise the object
						var obj = new BinaryFormatter().Deserialize(pipe);
						m_main_thread.BeginInvoke(DispatchMsg, obj);

						// Start a new read
						pipe.BeginRead(buf, 0, buf.Length, HandleDataRead, rd);
						return;
					}
					catch (Exception ex)
					{
						Debug.WriteLine(ex.Message);
					}
				}
				Pipe = null;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}

		/// <summary>Send a message over the pipe</summary>
		public bool Post<T>(T msg)
		{
			try
			{
				// Post the message if the pipe is connected
				var pipe = Pipe;
				if (pipe == null)
					return false;

				new BinaryFormatter().Serialize(pipe, msg);
				pipe.WaitForPipeDrain();
				RaisePosted();
				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(string.Format("Post Failed: {0}", ex.Message));
			}

			// If something is wrong, disconnect the pipe
			Pipe = null;
			return false;
		}

		/// <summary>Raised when data is posted</summary>
		public event EventHandler Posted;
		private void RaisePosted()
		{
			if (m_main_thread.Thread == Thread.CurrentThread)
			{
				if (Posted == null) return;
				Posted(this, EventArgs.Empty);
			}
			else
			{
				m_main_thread.BeginInvoke((Action)RaisePosted);
			}
		}

		/// <summary>The callback function to send received messages to</summary>
		protected Action<object> DispatchMsg
		{
			get;
			private set;
		}

		/// <summary>Establish the pipe connection</summary>
		protected abstract void ConnectPipe();
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
		{
			ConnectPipe();
		}

		/// <summary>Establish the pipe connection</summary>
		protected override void ConnectPipe()
		{
			// Limit the connection attempt frequency
			if (Environment.TickCount - m_last_connect_attempt < 100) return;
			m_last_connect_attempt = Environment.TickCount;

			try
			{
				var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
				pipe.Connect(100);
				pipe.ReadMode = PipeTransmissionMode.Message;
				Debug.WriteLine("TradeeProxy connected");
				Pipe = pipe;
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
		private int m_last_connect_attempt;
	}

	/// <summary>Represents a connection from the 'Tradee' application to a trade data source</summary>
	public class TradeeClient :TradeePipe
	{
		public TradeeClient(Action<object> dispatch_msg)
			:base(dispatch_msg)
		{
			ListenForConnections();
		}

		/// <summary>Wait for clients to connect</summary>
		private void ListenForConnections()
		{
			try
			{
				// Create a new pipe and wait (asynchronously) for a connection
				var pipe = new NamedPipeServerStream(PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
				pipe.ReadMode = PipeTransmissionMode.Message;
				pipe.BeginWaitForConnection(HandleConnection, pipe);
				return;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(string.Format("ListenForConnections: {0}", ex.Message));
			}
			ListenForConnections();
		}

		/// <summary>Handle connection from a TradeeProxy</summary>
		private void HandleConnection(IAsyncResult ar)
		{
			var pipe = (NamedPipeServerStream)ar.AsyncState;

			pipe.EndWaitForConnection(ar);
			Debug.WriteLine("TradeeProxy connected");
			Pipe = pipe;

			// Start waiting for the next connection
			ListenForConnections();
		}

		/// <summary>Establish the pipe connection</summary>
		protected override void ConnectPipe()
		{}
	}
}
