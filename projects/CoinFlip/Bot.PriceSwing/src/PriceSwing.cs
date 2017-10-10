using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using CoinFlip;
using pr.common;
using pr.container;
using pr.extn;
using pr.maths;
using pr.util;

namespace Bot.PriceSwing
{
	[Plugin(typeof(IBot))]
	public class PriceSwing :IBot
	{
		// Notes:
		// - Keep a history of 'unmatched' trades.
		// - When price moves, look for opportunities to complete "opposite" trades with a nett profit.
		// - Create new trades whenever there are no unmatched trades near the current price
		// - Create trades so that the number of B2Q and Q2B trades is even

		private Random m_rng;
		private bool m_suppress_not_created;

		public PriceSwing(Model model, XElement settings_xml)
			:base("Price Swing", model, new SettingsData(settings_xml))
		{
			TradeRecords = new BindingSource<TradeRecord>{ DataSource = new BindingListEx<TradeRecord>() };
			PendingTradeRecords = new List<TradeRecord>();
			PriceSwingUI = new PriceSwingUI(this, Model.UI);
			m_rng = new Random();
			RestorePair();
		}
		protected override void Dispose(bool disposing)
		{
			PriceSwingUI = null;
			TradeRecords = null;
			base.Dispose(disposing);
		}
		protected override void HandlePairsListChanging(object sender, ListChgEventArgs<TradePair> e)
		{
			RestorePair();
		}

		/// <summary>Settings for this strategy</summary>
		public new SettingsData Settings
		{
			get { return (SettingsData)base.Settings; }
		}

		/// <summary>The UI for displaying the loops</summary>
		public PriceSwingUI PriceSwingUI
		{
			get { return m_price_swing_ui; }
			private set
			{
				if (m_price_swing_ui == value) return;
				if (m_price_swing_ui != null)
				{
					Util.Dispose(ref m_price_swing_ui);
				}
				m_price_swing_ui = value;
				if (m_price_swing_ui != null)
				{
				}
			}
		}
		private PriceSwingUI m_price_swing_ui;

		/// <summary>The pair to trade</summary>
		public TradePair Pair
		{
			get { return m_pair; }
			set
			{
				if (m_pair == value) return;
				if (m_pair != null)
				{
					TradeRecords.Clear();
				}
				m_pair = value;
				if (m_pair != null)
				{
					Settings.PairWithExchange = m_pair.NameWithExchange;

					//// Create some fake records
					//SaveTradeRecords(new[]
					//{
					//	new TradeRecord(ETradeType.B2Q, 0.07m._(m_pair.RateUnits), 1m._(m_pair.Base), 2m._(m_pair.Quote)),
					//}.ToList());

					LoadTradesRecord();
				}
				RaisePropertyChanged(new PropertyChangedEventArgs(nameof(Pair)));
			}
		}
		private TradePair m_pair;

		/// <summary>The collection of unmatched historic trades</summary>
		public BindingSource<TradeRecord> TradeRecords
		{
			get { return m_trade_records; }
			private set
			{
				if (m_trade_records == value) return;
				if (m_trade_records != null)
				{
					m_trade_records.ListChanging -= HandleTradeRecordsListChanging;
				}
				m_trade_records = value;
				if (m_trade_records != null)
				{
					m_trade_records.ListChanging += HandleTradeRecordsListChanging;
				}
			}
		}
		private BindingSource<TradeRecord> m_trade_records;
		private void HandleTradeRecordsListChanging(object sender, ListChgEventArgs<TradeRecord> e)
		{
			if (e.IsDataChanged)
				SaveTradeRecords();
		}

		/// <summary>Trade records for orders that have been placed, but not confirmed as filled yet</summary>
		public List<TradeRecord> PendingTradeRecords { get; private set; }

		/// <summary>Main loop step</summary>
		public async override Task Step()
		{
			// Test for filled trades
			await UpdateMonitoredTrades();

			// Compare the current price with the trade records, 
			// If we can trade such that the nett with a trade record
			// is above the profit threshold, then do it.
			// - Get the volume that was previously traded
			// - Get the current price to trade this volume
			// - Maximise by nett profit
			MatchTrade(TradeRecords
				.Where(x => x.TradeType == ETradeType.Q2B)
				.MaxByOrDefault(x => Pair.BaseToQuote(x.VolumeOut).Price - x.Price));
			MatchTrade(TradeRecords
				.Where(x => x.TradeType == ETradeType.B2Q)
				.MaxByOrDefault(x => Pair.QuoteToBase(x.VolumeOut).Price - x.Price));

			// See if there are any trade records near the current price. If not, create a trade.
			// Pick the trade direction based on the counts of B2Q and Q2B.
			var sign = TradeRecords.Sum(x =>
				x.TradeType == ETradeType.Q2B ? +1 :
				x.TradeType == ETradeType.B2Q ? -1 : 0);
			for (; sign == 0; sign = m_rng.Next(0,3) - 1) {}
			var tt = sign > 0 ? ETradeType.B2Q : ETradeType.Q2B;

			// Get the volume to trade (reduced to allow for fees and rounding)
			var vol = 
				tt == ETradeType.B2Q ? Maths.Min(Settings.VolumeFrac * Settings.FundAllocation * Pair.Base .Balance.Available * (1m - Pair.Fee) * 0.99999m, Pair.Base .AutoTradeLimit) :
				tt == ETradeType.Q2B ? Maths.Min(Settings.VolumeFrac * Settings.FundAllocation * Pair.Quote.Balance.Available * (1m - Pair.Fee) * 0.99999m, Pair.Quote.AutoTradeLimit) :
				0;

			// Get the price that we can trade 'vol' in the direction of 'tt' for
			var trade =
				tt == ETradeType.Q2B ? Pair.QuoteToBase(vol) :
				tt == ETradeType.B2Q ? Pair.BaseToQuote(vol) :
				(Trade)null;

			// Look for existing trade records near this price
			Debug.Assert(TradeRecords.IsOrdered((l,r) => l.PriceQ2B <= r.PriceQ2B), "TradeRecords should be ordered");
			var idx = TradeRecords.BinarySearch(x => x.PriceQ2B.CompareTo(trade.PriceQ2B), find_insert_position:true);

			// Place a trade if there are no trade records within range of 'trade.PriceQ2B'
			var do_trade = true;
			if (idx >                  0) do_trade &= Maths.Abs(TradeRecords[idx - 1].PriceQ2B - trade.PriceQ2B) > Settings.PriceChange;
			if (idx < TradeRecords.Count) do_trade &= Maths.Abs(TradeRecords[idx    ].PriceQ2B - trade.PriceQ2B) > Settings.PriceChange;
			if (do_trade)
			{
				var validate = trade.Validate();
				if (validate == Trade.EValidation.Valid)
				{
					// Place the trade. Add it to the trade records once it's been filled.
					var res = MonitoredTrades.Add2(trade.CreateOrder());
					PendingTradeRecords.Add(new TradeRecord(trade.TradeType, res.OrderId, trade.Price, trade.VolumeIn, trade.VolumeOut));
					m_suppress_not_created = false;
				}
				else if (!m_suppress_not_created)
				{
					Log.Write(ELogLevel.Warn, $"Order skipped. {trade.Description} - {validate}");
					m_suppress_not_created = true;
				}
			}
		}

		/// <summary>Place a trade that is the reverse of 'rec'</summary>
		private void MatchTrade(TradeRecord rec)
		{
			if (rec == null)
				return;

			// Create the matching trade
			var trade =
				rec.TradeType == ETradeType.B2Q ? Pair.QuoteToBase(rec.VolumeOut) :
				rec.TradeType == ETradeType.Q2B ? Pair.BaseToQuote(rec.VolumeOut) :
				(Trade)null;

			// Place the trade
			var validate = trade.Validate();
			if (validate == Trade.EValidation.Valid)
			{
				// Record the trade record and the matching trade so that when the matched
				// trade is filled we can remove the trade record.
				var res = MonitoredTrades.Add2(trade.CreateOrder());
				rec.MatchTradeId = res.OrderId;
			}
			else
			{
				Log.Write(ELogLevel.Warn, $"Matching trade skipped. {trade.Description} - {validate}");
			}
		}

		/// <summary>Handle a monitored trade being filled</summary>
		protected override void OnPositionFilled(ulong order_id, PositionFill his)
		{
			// If the position that was filled is a 'MatchTradeId' in
			// an existing trade record then remove the trade record.
			TradeRecord rec;
			if ((rec = TradeRecords.FirstOrDefault(x => x.MatchTradeId == order_id)) != null)
			{
				TradeRecords.Remove(rec);
			}
			// The filled position is a new trade, add a new trade record
			else if ((rec = PendingTradeRecords.FirstOrDefault(x => x.OrderId == order_id)) != null)
			{
				PendingTradeRecords.Remove(rec);
				TradeRecords.Add(rec);
			}
			// Otherwise, what the hell was it?
			else
			{
				throw new Exception("Unknown monitored trade");
			}

			base.OnPositionFilled(order_id, his);
		}

		/// <summary>Handle a monitored trade being cancelled</summary>
		protected override void OnPositionCancelled(ulong order_id)
		{
			// If the cancelled trade was the 'MatchTradeId' of an existing
			// trade record clear the 'MatchTradeId' since it hasn't been matched.
			TradeRecord rec;
			if ((rec = TradeRecords.FirstOrDefault(x => x.MatchTradeId == order_id)) != null)
			{
				rec.MatchTradeId = null;
			}
			// If the cancelled position was a new trade, remove it from the pending list
			else if ((rec = PendingTradeRecords.FirstOrDefault(x => x.OrderId == order_id)) != null)
			{
				PendingTradeRecords.Remove(rec);
			}
			// Otherwise, what the hell was it?
			else
			{
				throw new Exception("Unknown monitored trade");
			}

			base.OnPositionCancelled(order_id);
		}

		/// <summary>Return items to add to the context menu for this bot</summary>
		public override void CMenuItems(ContextMenuStrip cmenu)
		{
			cmenu.Items.AddSeparator();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Properties"));
				opt.Click += (s,a) =>
				{
					PriceSwingUI.Show(Model.UI);
				};
			}
		}

		/// <summary>Attempt to set 'Pair' to the pair recorded in the settings</summary>
		private void RestorePair()
		{
			if (Pair != null) return;
			Pair = Model.Pairs.FirstOrDefault(x => x.NameWithExchange == Settings.PairWithExchange);
		}

		/// <summary>Return the filepath of the file containing trade records for 'pair'</summary>
		private string TradeRecordFilepath
		{
			get { return Misc.ResolveUserPath($"Bots\\{Name}\\TradeRecord-{Pair.Name.Strip('/')}.xml"); }
		}

		/// <summary>Load the history of unmatched trades for 'pair'</summary>
		private void LoadTradesRecord()
		{
			using (TradeRecords.SuspendEvents(reset_bindings_on_resume: true, preserve_position: false))
			{
				TradeRecords.Clear();

				// If there is no record for this pair, then we're done
				var filepath = TradeRecordFilepath;
				if (!Path_.FileExists(filepath))
					return;

				try
				{
					// Load the record from disk
					var root = XDocument.Load(filepath).Root;
					foreach (var elem in root.Elements("trade"))
						TradeRecords.Add(elem.As<TradeRecord>());

					// Order the records by price
					TradeRecords.Sort(Cmp<TradeRecord>.From((l,r) => l.Price.CompareTo(r.Price)));
				}
				catch (Exception ex)
				{
					Model.Log.Write(ELogLevel.Error, ex, $"Failed to load trade record for {Pair.Name}");
				}
			}
		}

		/// <summary>Write the trade records to disk</summary>
		private void SaveTradeRecords()
		{
			var filepath = TradeRecordFilepath;
			Path_.CreateDirs(Path_.Directory(filepath));
			
			try
			{
				var root = new XElement("root");
				foreach (var rec in TradeRecords)
					root.Add2("trade", rec, false);

				root.Save(filepath);
			}
			catch (Exception ex)
			{
				Model.Log.Write(ELogLevel.Error, ex, $"Failed to write trade records for {Pair.Name}");
			}
		}

		/// <summary>Data needed to save a fishing instance in the settings</summary>
		[TypeConverter(typeof(TyConv))]
		public class SettingsData :SettingsBase<SettingsData>
		{
			public SettingsData()
			{
				PairWithExchange = string.Empty;
				PriceChange      = 0m._();
				VolumeFrac       = 0.01m;
			}
			public SettingsData(SettingsData rhs)
				:base(rhs)
			{
				PairWithExchange = rhs.PairWithExchange;
				PriceChange      = rhs.PriceChange;
				VolumeFrac       = rhs.VolumeFrac;
			}
			public SettingsData(XElement node)
				:base(node)
			{}

			/// <summary>The name of the pair to trade and the exchange it's on</summary>
			public string PairWithExchange
			{
				get { return get(x => x.PairWithExchange); }
				set { set(x => x.PairWithExchange, value); }
			}

			/// <summary>The change in quote price required before matching a trade</summary>
			public Unit<decimal> PriceChange
			{
				get { return get(x => x.PriceChange); }
				set { set(x => x.PriceChange, value); }
			}

			/// <summary>The proportion of available balance to use in each trade</summary>
			public decimal VolumeFrac
			{
				get { return get(x => x.VolumeFrac); }
				set { set(x => x.VolumeFrac, value); }
			}

			/// <summary>True if the settings are valid</summary>
			public override bool Valid
			{
				get { return PairWithExchange.HasValue() && PriceChange > 0 && VolumeFrac > 0; }
			}

			/// <summary>If 'Valid' is false, this is a text description of why</summary>
			public override string ErrorDescription
			{
				get
				{
					return
						!(PairWithExchange.HasValue()) ? "No trading pair selected" :
						!(PriceChange > 0) ? "Price change is invalid. Must be > 0" :
						!(VolumeFrac > 0) ? "Volume fraction is invalid. Must be > 0" :
						string.Empty;
				}
			}

			private class TyConv :GenericTypeConverter<SettingsData> {}
		}
	}
}
