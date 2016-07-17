using System;
using System.Diagnostics;
using System.Drawing;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using pr.container;
using pr.extn;
using pr.util;

namespace Tradee
{
	/// <summary>Container object for the main app logic</summary>
	public class MainModel :IDisposable
	{
		public MainModel(MainUI owner)
		{
			Owner      = owner;
			MarketData = new MarketDataDB();
			Alarms     = new AlarmModel();
			SnR        = new SupportResist(MarketData);
		}
		public void Dispose()
		{
			RunServer  = false;
			SnR        = null;
			Alarms     = null;
			MarketData = null;
		}

		/// <summary>Owner window</summary>
		public MainUI Owner { get; private set; }

		/// <summary>The store of market data</summary>
		public MarketDataDB MarketData
		{
			get { return m_market_data; }
			private set
			{
				if (m_market_data == value) return;
				Util.Dispose(ref m_market_data);
				m_market_data = value;
			}
		}
		private MarketDataDB m_market_data;

		/// <summary>Alarm app logic</summary>
		public AlarmModel Alarms
		{
			get { return m_alarms; }
			private set
			{
				if (m_alarms == value) return;
				Util.Dispose(ref m_alarms);
				m_alarms = value;
			}
		}
		private AlarmModel m_alarms;

		/// <summary>Support and resistance detector</summary>
		public SupportResist SnR
		{
			get { return m_snr; }
			private set
			{
				if (m_snr == value) return;
				Util.Dispose(ref m_snr);
				m_snr = value;
			}
		}
		private SupportResist m_snr;

		/// <summary>True while the pipe server thread is running</summary>
		public bool RunServer
		{
			get { return m_srv != null; }
			set
			{
				if (RunServer == value) return;
				if (value)
				{
					// The background thread for servicing the clients
					m_srv = new Thread(new ThreadStart(() =>
					{
						m_srv.Name = "Pipe Server";
						for (;RunServer;)
						{
							try
							{
								//'use a semaphore
								// Wait for the next client connection
								var pipe = new NamedPipeServerStream("TradeePipeIn", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.None) { ReadMode = PipeTransmissionMode.Message };
								pipe.WaitForConnection();
								ThreadPool.QueueUserWorkItem(p =>
								{
									// Read and dispatch the message from the pipe
									try
									{
										var pipe_ = (NamedPipeServerStream)p;
										var obj = new BinaryFormatter().Deserialize(pipe_);
										if (obj != null)
											Owner.BeginInvoke(() => DispatchMsg(obj));
									}
									catch (Exception ex)
									{
										Debug.WriteLine("De-serialisation failed: {0}".Fmt(ex.MessageFull()));
									}
								}, pipe);
							}
							catch (Exception ex)
							{
								Debug.WriteLine("Pipe server failed: {0}".Fmt(ex.MessageFull()));
							}
						}
					}));
					m_srv.Start();
				}
				else
				{
					var srv = m_srv;
					m_srv = null; // Signal thread exit
					try { new NamedPipeClientStream(".", "TradeePipeIn").Connect(); } catch { } // Connect to wake up from WaitForConnection
					srv.Join();
				}
			}
		}
		private Thread m_srv;

		/// <summary>Handle messages received on the pipe</summary>
		private void DispatchMsg(object obj)
		{
			// Dispatch received messages
			switch (obj.GetType().Name)
			{
			default:
				{
					Owner.Status.SetStatusMessage(msg:"Unknown Message Type {0} received".Fmt(obj.GetType().Name), fr_color:Color.Red, display_time:TimeSpan.FromSeconds(5));
					break;
				}
			case nameof(HelloMsg):
				{
					Owner.Status.SetStatusMessage(msg:((HelloMsg)obj).Msg, display_time:TimeSpan.FromMilliseconds(500));
					break;
				}
			case nameof(Candles):
				{
					MarketData.Add((Candles)obj);
					break;
				}
			case nameof(Candle):
				{
					MarketData.Add((Candle)obj);
					break;
				}
			case nameof(AccountStatus):
				{
					break;
				}
			}
		}

		/// <summary>Debug dump</summary>
		public void Dump()
		{
			foreach (var sym in SnR.Values)
			{
				var csv = new CSVData();
				foreach (var lvl in sym.Levels)
					csv.Add(lvl.Price, lvl.Volume);
				csv.Save("P:\\dump\\dump.csv");
			}
		}
	}
}
