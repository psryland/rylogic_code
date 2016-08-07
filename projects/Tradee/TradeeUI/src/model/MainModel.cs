using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO.Pipes;
using System.Threading;
using System.Windows.Threading;
using pr.container;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;

namespace Tradee
{
	/// <summary>Container object for the main app logic</summary>
	public class MainModel :IDisposable
	{
		public MainModel(MainUI owner, Settings settings)
		{
			Owner           = owner;
			Settings        = settings;
			Acct            = new AccountStatus(this);
			TradeDataSource = new TradeeClient(DispatchMsg);
			MarketData      = new MarketData(this);
			Positions       = new Positions(this);
			Alarms          = new Alarms();
			SnR             = new SupportResist(this);
		}
		public void Dispose()
		{
			TradeDataSource = null;
			SnR             = null;
			Alarms          = null;
			Positions       = null;
			MarketData      = null;
			Acct            = null;
		}

		/// <summary>Owner window</summary>
		public MainUI Owner { [DebuggerStepThrough] get; private set; }

		/// <summary>Application settings</summary>
		public Settings Settings { [DebuggerStepThrough] get; private set; }

		/// <summary>Trade account info</summary>
		public AccountStatus Acct
		{
			get { return m_acct; }
			private set
			{
				if (m_acct == value) return;
				m_acct = value;
			}
		}
		private AccountStatus m_acct;

		/// <summary>The store of market data</summary>
		public MarketData MarketData
		{
			[DebuggerStepThrough] get { return m_market_data; }
			private set
			{
				if (m_market_data == value) return;
				Util.Dispose(ref m_market_data);
				m_market_data = value;
			}
		}
		private MarketData m_market_data;

		/// <summary>The store of all orders (visualising/pending/active/closed)</summary>
		public Positions Positions
		{
			get { return m_positions; }
			set
			{
				if (m_positions == value) return;
				if (m_positions != null)
				{ }
				m_positions = value;
				if (m_positions != null)
				{ }
			}
		}
		private Positions m_positions;

		/// <summary>Alarm app logic</summary>
		public Alarms Alarms
		{
			[DebuggerStepThrough] get { return m_alarms; }
			private set
			{
				if (m_alarms == value) return;
				Util.Dispose(ref m_alarms);
				m_alarms = value;
			}
		}
		private Alarms m_alarms;

		/// <summary>Support and resistance detector</summary>
		public SupportResist SnR
		{
			[DebuggerStepThrough] get { return m_snr; }
			private set
			{
				if (m_snr == value) return;
				Util.Dispose(ref m_snr);
				m_snr = value;
			}
		}
		private SupportResist m_snr;

		/// <summary>The connection to the trade data source</summary>
		private TradeeClient TradeDataSource
		{
			[DebuggerStepThrough] get { return m_client; }
			set
			{
				if (m_client == value) return;
				if (m_client != null)
				{
					m_client.Disconnect();
					m_client.ConnectionChanged -= HandleTradeeConnectionChanged;
					Util.Dispose(ref m_client);
				}
				m_client = value;
				if (m_client != null)
				{
					m_client.ConnectionChanged += HandleTradeeConnectionChanged;
				}
			}
		}
		private TradeeClient m_client;

		/// <summary>True if the link to the trade data source is available</summary>
		public bool IsConnected
		{
			get { return TradeDataSource != null && TradeDataSource.IsConnected; }
		}

		/// <summary>Send a message to the trade data source</summary>
		public bool Post<T>(T msg) where T:ITradeeMsg
		{
			if (TradeDataSource == null || !TradeDataSource.IsConnected) return false;
			var res = TradeDataSource.Post(msg);
			Debug.WriteLine("{0}: {1}".Fmt(res ? "Posted" : "Post Failed", msg.GetType().Name));
			return res;
		}

		/// <summary>Raised when the trade data source connection changes</summary>
		public event EventHandler ConnectionChanged;

		/// <summary>Handle messages received from clients</summary>
		private void DispatchMsg(object msg)
		{
			// Ignore messages after dispose
			if (TradeDataSource == null)
				return;

			// Dispatch received messages
			switch (msg.GetType().Name)
			{
			default:
				{
					Debug.WriteLine("Unknown Message Type {0} received".Fmt(msg.GetType().Name));
					Owner.Status.SetStatusMessage(msg:"Unknown Message Type {0} received".Fmt(msg.GetType().Name), fr_color:Color.Red, display_time:TimeSpan.FromSeconds(5));
					break;
				}
			case nameof(InMsg.HelloTradee):
				#region
				{
					var m = (InMsg.HelloTradee)msg;
					MsgBox.Show(Owner, m.Text, "Test Msg Received");
					break;
				}
				#endregion
			case nameof(InMsg.CandleData):
				#region
				{
					var cd = (InMsg.CandleData)msg;
					if (cd.Candle  != null) MarketData[cd.Symbol].Add(cd.TimeFrame, cd.Candle);
					if (cd.Candles != null) MarketData[cd.Symbol].Add(cd.TimeFrame, cd.Candles);
					break;
				}
				#endregion
			case nameof(InMsg.AccountUpdate):
				#region
				{
					var acct = (InMsg.AccountUpdate)msg;
					Acct.Update(acct.Acct);
					break;
				}
				#endregion
			case nameof(InMsg.PositionsUpdate):
				#region
				{
					var pos = (InMsg.PositionsUpdate)msg;
					Acct.Update(pos.Positions);
					Positions.Update(pos.Positions);
					break;
				}
				#endregion
			case nameof(InMsg.PendingOrdersUpdate):
				#region
				{
					var orders = (InMsg.PendingOrdersUpdate)msg;
					Acct.Update(orders.PendingOrders);
					Positions.Update(orders.PendingOrders);
					break;
				}
				#endregion
			case nameof(InMsg.SymbolData):
				#region
				{
					var sd = (InMsg.SymbolData)msg;
					MarketData[sd.Symbol].PriceData = sd.Data;
					break;
				}
				#endregion
			}
		}

		/// <summary>Add a chart to the UI</summary>
		public void AddChart(Instrument instr)
		{
			Owner.AddChart(new ChartUI(this, instr));
		}

		/// <summary>Ask the trade data source for an instrument by symbol name</summary>
		public void RequestInstrument(string sym, ETimeFrame[] tf)
		{
			var msg = new OutMsg.RequestInstrument(sym, tf);
			TradeDataSource.Post(msg);
		}

		/// <summary>Request an update of the account status</summary>
		public void RefreshAcctStatus()
		{
			Post(new OutMsg.RequestAccountStatus(Acct?.AccountId));
		}

		/// <summary>Determine the stop loss level for a potential trade on 'instrument'</summary>
		public double CalculateStopLoss(Instrument instrument)
		{
			return 0.01;
		}

		/// <summary>Handle connection/disconnection of the trade data source</summary>
		private void HandleTradeeConnectionChanged(object sender, EventArgs e)
		{
			// Notify connection changed
			ConnectionChanged.Raise(this);

			// On connection, request an update of the account status
			if (TradeDataSource != null && TradeDataSource.IsConnected)
				RefreshAcctStatus();
		}
	}
}

