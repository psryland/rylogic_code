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
			Trades          = new BindingSource<Trade> { DataSource = new BindingListEx<Trade>() , PerItemClear = true };
			TradeDataSource = new TradeeClient(DispatchMsg);
			MarketData      = new MarketData(this);
			Alarms          = new AlarmModel();
			SnR             = new SupportResist(this);
		}
		public void Dispose()
		{
			TradeDataSource = null;
			SnR             = null;
			Alarms          = null;
			MarketData      = null;
		}

		/// <summary>Owner window</summary>
		public MainUI Owner { get; private set; }

		/// <summary>Application settings</summary>
		public Settings Settings { get; private set; }

		/// <summary>All trades, past, present, and future</summary>
		public BindingSource<Trade> Trades
		{
			[DebuggerStepThrough]get { return m_impl_trades; }
			private set
			{
				if (m_impl_trades == value) return;
				if (m_impl_trades != null)
				{
					m_impl_trades.ListChanging -= HandleTradesListChanging;
				}
				m_impl_trades = value;
				if (m_impl_trades != null)
				{
					m_impl_trades.ListChanging += HandleTradesListChanging;
				}
			}
		}
		private BindingSource<Trade> m_impl_trades;

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

		/// <summary>Alarm app logic</summary>
		public AlarmModel Alarms
		{
			[DebuggerStepThrough] get { return m_alarms; }
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
			[DebuggerStepThrough] get { return m_snr; }
			private set
			{
				if (m_snr == value) return;
				Util.Dispose(ref m_snr);
				m_snr = value;
			}
		}
		private SupportResist m_snr;

		/// <summary>Add a chart to the UI</summary>
		public void AddChart(Instrument instr)
		{
			Owner.AddChart(new ChartUI(this, instr, null));
		}

		/// <summary>The connection to the trade data source</summary>
		public TradeeClient TradeDataSource
		{
			[DebuggerStepThrough] get { return m_client; }
			set
			{
				if (m_client == value) return;
				Util.Dispose(ref m_client);
				m_client = value;
			}
		}
		private TradeeClient m_client;

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
					Owner.Status.SetStatusMessage(msg:"Unknown Message Type {0} received".Fmt(msg.GetType().Name), fr_color:Color.Red, display_time:TimeSpan.FromSeconds(5));
					break;
				}
			case nameof(InMsg.TestMsg):
				{
					var m = (InMsg.TestMsg)msg;
					MsgBox.Show(Owner, m.Text, "Test Msg Received");
					break;
				}
			case nameof(InMsg.CandleData):
				{
					var cd = (InMsg.CandleData)msg;
					if (cd.Candle  != null) MarketData[cd.Symbol].Add(cd.TimeFrame, cd.Candle);
					if (cd.Candles != null) MarketData[cd.Symbol].Add(cd.TimeFrame, cd.Candles);
					break;
				}
			case nameof(InMsg.AccountStatus):
				{
					break;
				}
			case nameof(InMsg.SymbolData):
				{
					var sd = (InMsg.SymbolData)msg;
					MarketData[sd.Symbol].PriceData = sd.Data;
					break;
				}
			}
		}

		/// <summary>Create a new order</summary>
		public void CreateNewTrade(Instrument instrument, ETradeType tt, ChartUI chart = null)
		{
			Trades.Add(new Trade(this, instrument, tt, chart));
		}

		/// <summary>Ask the trade data source for an instrument by symbol name</summary>
		public void RequestInstrument(string sym, ETimeFrame tf)
		{
			var msg = new OutMsg.ChangeInstrument(sym, tf);
			TradeDataSource.Post(msg);
		}

		/// <summary>Handle trades added or removed from the Trades collection</summary>
		private void HandleTradesListChanging(object sender, ListChgEventArgs<Trade> e)
		{
			var trade = e.Item;
			switch (e.ChangeType)
			{
			case ListChg.ItemAdded:
				{
					break;
				}
			case ListChg.ItemRemoved:
				{
					// Safety first...
					if (Bit.AnySet(trade.State, Trade.EState.PendingOrder|Trade.EState.ActivePosition))
						throw new Exception("Cannot delete a trade with active or pending orders");

					trade.Dispose();
					break;
				}
			}
		}
	}
}

