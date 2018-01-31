using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using CoinFlip;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Graphix;
using Rylogic.Gui;
using Rylogic.Maths;
using Rylogic.Utility;

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

		public static readonly Guid Id = Guid.NewGuid();

		private Random m_rng;
		private bool m_suppress_not_created;

		public PriceSwing(Model model, XElement settings_xml)
			:base("Price Swing", model, new SettingsData(settings_xml))
		{
			TradeRecords = new BindingSource<TradeRecord>{ DataSource = new BindingListEx<TradeRecord>{ PerItem = true } };
			PendingTradeRecords = new List<TradeRecord>();
			UI = new PriceSwingUI(this);
			GfxTemplate = new View3d.Object("*Line trade FFFFFFFF { -2 0 0 +2 0 0 }", false, Id);
			m_rng = new Random(653);
		}
		protected override void Dispose(bool disposing)
		{
			Pair = null;
			TradeRecords = null;
			UI = null;
			GfxTemplate = null;
			base.Dispose(disposing);
		}
		protected override void HandlePairsUpdated(object sender, EventArgs e)
		{
			if (Pair != null) return;
			LoadPair();
		}

		/// <summary>Settings for this strategy</summary>
		public new SettingsData Settings
		{
			get { return (SettingsData)base.Settings; }
		}

		/// <summary>The UI for monitoring this bot</summary>
		public PriceSwingUI UI
		{
			get { return m_ui; }
			private set
			{
				if (m_ui == value) return;
				Util.Dispose(ref m_ui);
				m_ui = value;
			}
		}
		private PriceSwingUI m_ui;

		/// <summary>The pair to trade</summary>
		public TradePair Pair
		{
			get { return m_pair; }
			set
			{
				if (m_pair == value) return;

				if (Active)
					throw new Exception("Don't change the pair while the bot is running");

				if (m_pair != null)
				{
				}
				m_pair = value;
				if (m_pair != null)
				{
					Settings.PairWithExchange = m_pair.NameWithExchange;
				}
				RaisePropertyChanged(nameof(Pair));
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

				// Handlers
				void HandleTradeRecordsListChanging(object sender, ListChgEventArgs<TradeRecord> e)
				{
					switch (e.ChangeType)
					{
					case ListChg.ItemAdded:
						{
							e.Item.Gfx = GfxTemplate.CreateInstance();
							break;
						}
					case ListChg.ItemRemoved:
						{
							Util.Dispose(e.Item);
							break;
						}
					}
					if (e.IsDataChanged)
						SaveTradeRecords();
				}
			}
		}
		private BindingSource<TradeRecord> m_trade_records;

		/// <summary>Trade records for orders that have been placed, but not confirmed as filled yet</summary>
		public List<TradeRecord> PendingTradeRecords { get; private set; }

		/// <summary>Start the bot</summary>
		public override bool OnStart()
		{
			LoadPair();
			if (Pair == null)
			{
				Model.AddToUI(UI);
				UI.DockControl.IsActiveContent = true;
				return false;
			}

			LoadTradesRecord();
			return true;
		}

		/// <summary>Stop the bot</summary>
		public override void OnStop()
		{
			using (SuspendSaving())
				TradeRecords.Clear();
		}

		/// <summary>Main loop step</summary>
		public override void Step()
		{
			if (Pair == null)
				return;

			// Test for opportunities to match earlier trades for profit
			#region Match Trade Records
			if (TradeRecords.Count != 0)
			{
				// Compare the current price with the trade records, 
				// If we can trade such that the nett with a trade record
				// is above the profit threshold, then do it.
				// - Get the volume that was previously traded
				// - Get the current price to trade this volume
				// - Maximise by nett profit
				if (MaxPriceDiff(TradeRecords.Where(x => x.TradeType == ETradeType.Q2B), ETradeType.B2Q, out var  record0))
					MatchTrade(record0);
				if (MaxPriceDiff(TradeRecords.Where(x => x.TradeType == ETradeType.B2Q), ETradeType.Q2B, out var record1))
					MatchTrade(record1);

				// Find the most profitable trade record
				bool MaxPriceDiff(IEnumerable<TradeRecord> trade_records, ETradeType tt, out TradeRecord rec)
				{
					rec = null;

					// Get the current spot price to work out the price threshold
					var spot = Pair.SpotPrice(tt);
					if (spot == null)
						return false;

					// Determine the price difference threshold
					var threshold = (tt == ETradeType.B2Q ? spot.Value : (1m / spot.Value)) * (decimal)Settings.PriceChangeFrac;
					
					// Find the record with the maximum profitable price difference
					var dprice = threshold;
					foreach (var r in trade_records)
					{
						var dp = Pair.MakeTrade(Fund.Id, tt, r.VolumeOut).Price - r.PriceInv;
						if (dp < dprice) continue;
						dprice = dp;
						rec = r;
					}

					return rec != null;
				}
			}
			#endregion

			// See if there are any trade records near the current price. If not, create a trade.
			#region Generate New Trades
			{
				// Pick the trade direction based on the counts of B2Q and Q2B.
				var tt = NewTradeType;
				var spot = Pair.SpotPrice(tt);
				if (spot == null)
					return;

				// Get the volume to trade (reduced to allow for fees and rounding)
				var base_avail  = Pair.Base .Balances[Fund].Available * (1m - Pair.Fee) * 0.99999m;
				var quote_avail = Pair.Quote.Balances[Fund].Available * (1m - Pair.Fee) * 0.99999m;
				var vol = 
					tt == ETradeType.B2Q ? Math_.Min((decimal)Settings.VolumeFrac * base_avail , Pair.Base .AutoTradeLimit) :
					tt == ETradeType.Q2B ? Math_.Min((decimal)Settings.VolumeFrac * quote_avail, Pair.Quote.AutoTradeLimit) :
					0;

				// Get the price that we can trade 'vol' in the direction of 'tt' for
				var trade = Pair.MakeTrade(Fund.Id, tt, vol);

				// Determine the required price distance from other existing or pending trades
				var threshold = spot.Value * (decimal)Settings.PriceChangeFrac;

				// Look for existing trade records or pending trades near this price
				// Place a trade if there are no trade records within range of 'trade.PriceQ2B'
				var do_trade = NearbyTrades(trade.PriceQ2B, threshold);
				if (do_trade)
				{
					var validate = trade.Validate();
					if (validate == EValidation.Valid)
					{
						// Place the trade. Add it to the trade records once it's been filled.
						var res = MonitoredTrades.Add2(trade.CreateOrder());
						PendingTradeRecords.Add(new TradeRecord(trade.TradeType, res.OrderId, Model.UtcNow, trade.Price, trade.VolumeIn, trade.VolumeOut));
						m_suppress_not_created = false;
					}
					else if (!m_suppress_not_created)
					{
						Log.Write(ELogLevel.Warn, $"Order skipped. {trade.Description} - {validate}");
						m_suppress_not_created = true;
					}
				}
			}
			#endregion

			Stepped.Raise(this);
		}
		public event EventHandler Stepped;

		/// <summary>Place a trade that is the reverse of 'rec'</summary>
		private void MatchTrade(TradeRecord rec)
		{
			// Create the matching trade
			var trade =
				rec.TradeType == ETradeType.B2Q ? Pair.QuoteToBase(Fund.Id, rec.VolumeOut) :
				rec.TradeType == ETradeType.Q2B ? Pair.BaseToQuote(Fund.Id, rec.VolumeOut) :
				(Trade)null;

			// Place the trade
			var validate = trade.Validate();
			if (validate == EValidation.Valid)
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

		/// <summary>Return true if there is a trade record or a pending trade near 'price_q2b'</summary>
		private bool NearbyTrades(Unit<decimal> price_q2b, Unit<decimal> threshold)
		{
			// Check the pending trades
			if (PendingTradeRecords.Any(x =>  Math_.Abs(x.PriceQ2B - price_q2b) <= threshold))
				return false;
			
			// Check the trade records
			Debug.Assert(TradeRecords.IsOrdered((l,r) => l.PriceQ2B <= r.PriceQ2B), "TradeRecords should be ordered");
			var idx = TradeRecords.BinarySearch(x => x.PriceQ2B.CompareTo(price_q2b), find_insert_position:true);

			if ((idx >                  0 && Math_.Abs(TradeRecords[idx-1].PriceQ2B - price_q2b) <= threshold) ||
				(idx < TradeRecords.Count && Math_.Abs(TradeRecords[idx  ].PriceQ2B - price_q2b) <= threshold))
				return false;

			return true;
		}

		/// <summary>Return the trade direction for a new trade, given the directions of the existing trade records</summary>
		private ETradeType NewTradeType
		{
			get
			{
				// See what we have more of, Q2B or B2Q
				var sign = TradeRecords.Sum(x =>
					x.TradeType == ETradeType.Q2B ? +1 :
					x.TradeType == ETradeType.B2Q ? -1 : 0);

				// If it's even, choose at random
				if (sign == 0)
					sign = 2 * m_rng.Next(0,2) - 1;

				// Return the trade direction
				return sign > 0 ? ETradeType.B2Q : ETradeType.Q2B;
			}
		}

		/// <summary>Handle a monitored trade being filled</summary>
		protected override void OnPositionFilled(ulong order_id, OrderFill his)
		{
			// If the position that was filled is a 'MatchTradeId' in
			// an existing trade record then remove the trade record.
			TradeRecord rec;
			if ((rec = TradeRecords.FirstOrDefault(x => x.MatchTradeId == order_id)) != null)
			{
				TradeRecords.Remove(rec);

				// Log the win
				var nett0 = his.VolumeOut - rec.VolumeIn;
				var nett1 = rec.VolumeOut - his.VolumeIn;
				var value0 = his.CoinOut.ValueOf(nett0);
				var value1 = his.CoinIn.ValueOf(nett1);
				var sum = value0 + value1;

				var msg =
					(Model.AllowTrades ? "!Profit!\n" : "!Virtual Profit!\n")+
					$" On {Pair.Exchange.Name}:\n"+
					$"   Initial order: {rec.Description}\n"+
					$"   Matched by: {his.Description}\n"+
					$"\n"+
					$"  Nett: {nett0.ToString("G8",true)}  ({value0:C})\n"+
					$"  Nett: {nett1.ToString("G8",true)}  ({value1:C})\n"+
					$"  Total: {sum:C}";
				Log.Write(ELogLevel.Warn, msg);
				Model.WinLog.Write(ELogLevel.Info, msg);
				Res.Coins.Play();
			}
			// The filled position is a new trade, add a new trade record
			else if ((rec = PendingTradeRecords.FirstOrDefault(x => x.OrderId == order_id)) != null)
			{
				PendingTradeRecords.Remove(rec);
				var idx = TradeRecords.BinarySearch(x => x.PriceQ2B.CompareTo(rec.PriceQ2B), find_insert_position:true);
				TradeRecords.Insert(idx, rec);
			}
			// Otherwise, what the hell was it?
			else
			{
				Debug.WriteLine("Unknown monitored trade!");
				//throw new Exception("Unknown monitored trade");
			}
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
		}

		/// <summary>Return items to add to the context menu for this bot</summary>
		public override void CMenuItems(ContextMenuStrip cmenu)
		{
			cmenu.Items.AddSeparator();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Monitor"));
				opt.Click += (s,a) =>
				{
					Model.AddToUI(UI);
					UI.DockControl.IsActiveContent = true;
				};
			}
		}

		/// <summary>Add graphics to a chart displaying 'Pair'</summary>
		public override void OnChartRendering(Instrument instrument, Settings.ChartSettings chart_settings, ChartControl.ChartRenderingEventArgs args)
		{
			if (instrument.Pair != Pair)
				return;

			// Draw all of the unmatched trade records
			args.Window.RemoveObjects(new[] { Id }, 1, 0);
			foreach (var tr in TradeRecords)
			{
				if (tr.Gfx == null) continue;
				var x = (float)instrument.FIndexAt(new TimeFrameTime(tr.Timestamp, instrument.TimeFrame));
				tr.Gfx.Colour =
					tr.TradeType == ETradeType.Q2B ? (Colour32)0xFF00FF80 :
					tr.TradeType == ETradeType.B2Q ? (Colour32)0xFFFF0080 :
					tr.Gfx.Colour;

				tr.Gfx.O2P = m4x4.Translation(new v4(x, (float)(decimal)tr.PriceQ2B, ZOrder.Trades, 1f));
				args.AddToScene(tr.Gfx);
			}
		}

		/// <summary>Attempt to set 'Pair' to the pair recorded in the settings</summary>
		private void LoadPair()
		{
			Pair = Model.Pairs.FirstOrDefault(x => x.NameWithExchange == Settings.PairWithExchange);
		}

		/// <summary>Return the filepath of the file containing trade records for 'pair'</summary>
		private string TradeRecordFilepath
		{
			get
			{
				var filename =
					 Model.BackTesting ? $"TradeRecord-{Pair.Name.Strip('/')}-BackTesting.xml" :
					!Model.AllowTrades ? $"TradeRecord-{Pair.Name.Strip('/')}-Fake.xml" :
					$"TradeRecord-{Pair.Name.Strip('/')}.xml";

				return Misc.ResolveUserPath($"Bots\\{Name}\\{filename}");
			}
		}

		/// <summary>Load the history of unmatched trades for 'pair'</summary>
		private void LoadTradesRecord()
		{
			using (SuspendSaving())
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
					TradeRecords.AddRange(root
						.Elements("trade")
						.Select(x => x.As<TradeRecord>())
						.Where(x => x.Timestamp <= Model.UtcNow.Ticks)
						.OrderBy(x => x.PriceQ2B));
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
			if (m_suspend_save_records)
				return;

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

		/// <summary>Temporarily suspend saving of trade records</summary>
		private Scope SuspendSaving()
		{
			return Scope.Create(() => m_suspend_save_records = true, () => m_suspend_save_records = false);
		}
		private bool m_suspend_save_records;

		/// <summary>Template graphics object used to show trade records</summary>
		private View3d.Object GfxTemplate
		{
			get { return m_gfx_template; }
			set
			{
				if (m_gfx_template == value) return;
				Util.Dispose(ref m_gfx_template);
				m_gfx_template = value;
			}
		}
		private View3d.Object m_gfx_template;

		/// <summary>Data needed to save a fishing instance in the settings</summary>
		[TypeConverter(typeof(TyConv))]
		public class SettingsData :SettingsBase<SettingsData>
		{
			public SettingsData()
			{
				PairWithExchange = string.Empty;
				PriceChangeFrac  = 0;
				VolumeFrac       = 0;
			}
			public SettingsData(XElement node)
				:base(node)
			{}

			/// <summary>The name of the pair to trade and the exchange it's on</summary>
			public string PairWithExchange
			{
				get { return get<string>(nameof(PairWithExchange)); }
				set { set(nameof(PairWithExchange), value); }
			}

			/// <summary>The change in quote price required before matching a trade</summary>
			public double PriceChangeFrac
			{
				get { return get<double>(nameof(PriceChangeFrac)); }
				set { set(nameof(PriceChangeFrac), value); }
			}
			public double PriceChangePC
			{
				get { return PriceChangeFrac * 100.0; }
				set { PriceChangeFrac = value * 0.01; }
			}

			/// <summary>The proportion of available balance to use in each trade</summary>
			public double VolumeFrac
			{
				get { return get<double>(nameof(VolumeFrac)); }
				set { set(nameof(VolumeFrac), value); }
			}
			public double VolumePC
			{
				get { return VolumeFrac * 100.0; }
				set { VolumeFrac = value * 0.01; }
			}

			/// <summary>True if the settings are valid</summary>
			public override bool Valid(IBot bot)
			{
				return PairWithExchange.HasValue() && PriceChangeFrac > 0 && VolumeFrac > 0;
			}

			/// <summary>If 'Valid' is false, this is a text description of why</summary>
			public override string ErrorDescription
			{
				get
				{
					return
						!(PairWithExchange.HasValue()) ? "No trading pair selected" :
						!(PriceChangeFrac > 0) ? "Price change is invalid. Must be > 0" :
						!(VolumeFrac > 0) ? "Volume is invalid. Must be > 0" :
						string.Empty;
				}
			}

			private class TyConv :GenericTypeConverter<SettingsData> {}
		}
	}
}
