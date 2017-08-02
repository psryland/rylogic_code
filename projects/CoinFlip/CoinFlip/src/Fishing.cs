using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	public class Fishing :IDisposable, IShutdownAsync
	{
		// Notes:
		//  Go fishing on 'exch1' by setting offers above/below the best price on 'exch0'
		//  If the orders on 'exch1' are filled, offset the trade with the reverse trade on 'exch0'
		public Fishing(Model model, Settings.FishingData data)
		{
			Model = model;
			Settings = data;
			Log = new Logger(string.Empty, new LogToFile(Util.ResolveUserDocumentsPath("Rylogic", "CoinFlip", $"log_{data.Name}.txt"), append:false), Model.Log);
			LogUI = new LogView(this, Model.UI);
		}
		public virtual void Dispose()
		{
			Debug.Assert(!Active, "Main loop must be shutdown before Disposing");
			Log = null;
			LogUI = null;
			Model = null;
		}

		/// <summary>Async shutdown</summary>
		public Task ShutdownAsync()
		{
			Active = false;
			return Task_.WaitWhile(() => Active);
		}

		/// <summary>Main loop for the fisher</summary>
		private async void MainLoop(CancellationToken shutdown)
		{
			using (Scope.Create(() => ++m_active, () => --m_active))
			{
				// Locate the trading pairs to fish with
				var exch0 = Model.Exchanges.FirstOrDefault(x => x.Name == Settings.Exch0);
				var exch1 = Model.Exchanges.FirstOrDefault(x => x.Name == Settings.Exch1);
				Pair0 = exch0?.Pairs.Values.FirstOrDefault(x => x.Name == Settings.Pair);
				Pair1 = exch1?.Pairs.Values.FirstOrDefault(x => x.Name == Settings.Pair);
				if (Pair0 == null || Pair1 == null)
					return;

				// Sanity check
				if (Pair0.Exchange == Pair1.Exchange)
					throw new Exception("Pairs must be from different exchanges");
				if (Pair0.Exchange == Model.CrossExchange)
					throw new Exception("Pairs cannot be CrossExchange pairs");
				if (Pair1.Exchange == Model.CrossExchange)
					throw new Exception("Pairs cannot be CrossExchange pairs");
				if (Pair0.Base.Symbol != Pair1.Base.Symbol ||
					Pair0.Quote.Symbol != Pair1.Quote.Symbol)
					throw new Exception("Pairs must be for the same currencies but on different exchanges");

				Log.Write(ELogLevel.Info, $"Fishing started - {Pair0.Name} on {Pair1.Exchange.Name}");

				// Infinite loop till Shutdown called
				for (;;)
				{
					try
					{
						var exit = await Task.Run(() => shutdown.WaitHandle.WaitOne(500), shutdown);
						if (exit) break;
						shutdown.ThrowIfCancellationRequested();

						// Service the fishing trades
						await Update();
					}
					catch (Exception ex)
					{
						if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
						if (ex is OperationCanceledException) { break; }
						else Log.Write(ELogLevel.Error, ex, $"Fishing instance {Exch0.Name} -> {Exch1.Name} update failed.\r\n{ex.StackTrace}");
					}
				}

				try
				{
					// Cancel any outstanding bait trades
					await Task.WhenAll(
						BaitB2Q?.Cancel() ?? Misc.CompletedTask,
						BaitQ2B?.Cancel() ?? Misc.CompletedTask);
				}
				catch (Exception ex)
				{
					if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
					if (ex is OperationCanceledException) {}
					else Log.Write(ELogLevel.Error, ex, $"Fishing instance {Exch0.Name} -> {Exch1.Name} ClosePositions failed.\r\n{ex.StackTrace}");
				}

				Log.Write(ELogLevel.Info, $"Fishing stopped - {Pair0.Name} on {Pair1.Exchange.Name}");
			}
		}
		private CancellationTokenSource m_main_loop_shutdown;
		private int m_active;

		/// <summary>A log for this fishing instance</summary>
		private Logger Log
		{
			[DebuggerStepThrough] get { return m_log; }
			set
			{
				if (m_log == value) return;
				Util.Dispose(ref m_log);
				m_log = value;
			}
		}
		private Logger m_log;

		/// <summary>Enable/Disable fishing using this instance</summary>
		public bool Active
		{
			get { return m_active != 0 && m_main_loop_shutdown != null; }
			set
			{
				if (Active == value) return;
				if (value)
				{
					if (!Settings.Valid)
						return;

					m_main_loop_shutdown = new CancellationTokenSource();
					MainLoop(m_main_loop_shutdown.Token);
				}
				else if (m_main_loop_shutdown != null)
				{
					m_main_loop_shutdown.Cancel();
					m_main_loop_shutdown = null;
				}
			}
		}

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return m_model; }
			private set
			{
				if (m_model == value) return;
				m_model = value;
			}
		}
		private Model m_model;

		/// <summary>Settings object for this instance</summary>
		public Settings.FishingData Settings { get; set; }

		/// <summary>Binding helpers</summary>
		public string PairName  { get { return Settings.Pair; } }
		public string ExchName0 { get { return Settings.Exch0; } }
		public string ExchName1 { get { return Settings.Exch1; } }

		/// <summary>The reference exchange</summary>
		public Exchange Exch0
		{
			get { return Pair0.Exchange; }
		}

		/// <summary>The target exchange</summary>
		public Exchange Exch1
		{
			get { return Pair1.Exchange; }
		}

		/// <summary>The trading pair that we're using for the reference price</summary>
		public TradePair Pair0 { [DebuggerStepThrough] get; private set; }

		/// <summary>The trading pair that we have an order waiting to be taken on</summary>
		public TradePair Pair1 { [DebuggerStepThrough] get; private set; }

		/// <summary>A window showing the log for this fishing instance only</summary>
		public LogView LogUI
		{
			get { return m_log_ui; }
			private set
			{
				if (m_log_ui == value) return;
				Util.Dispose(ref m_log_ui);
				m_log_ui = value;
			}
		}
		private LogView m_log_ui;

		/// <summary>The order held on the target exchange to trade Base to Quote</summary>
		private FishingTrade BaitB2Q { get; set; }

		/// <summary>The order held on the target exchange to trade Quote to Base</summary>
		private FishingTrade BaitQ2B { get; set; }

		/// <summary>Service the fishing orders</summary>
		private async Task Update()
		{
			var tasks = new List<Task>();

			// Helper for placing the bait order
			Func<Trade, Trade, bool, Task<FishingTrade>> CreateBaitOrder = async (Trade trade0, Trade trade1, bool suppress_not_created) =>
			{
				// Check the amounts we want to trade are valid
				var validate0 = trade0.Validate();
				var validate1 = trade1.Validate();
				if (validate0 == Trade.EValidation.Valid && validate1 == Trade.EValidation.Valid)
				{
					// Create the bait order
					var order_result = await trade1.CreateOrder();
					Log.Write(ELogLevel.Info,
						$"Bait order (id={order_result.OrderId}) on {Exch1.Name} created. ({trade1.Description}) " +
						$"ref={trade0.PriceQ2B.ToString("G6")} {Pair1.RateUnits} - " +
						$"{(100m * Math.Abs(1m - trade1.PriceQ2B/trade0.PriceQ2B)):G4}%");

					// Create the fishing trade
					return new FishingTrade(this, order_result, trade0, trade1);
				}
				else
				{
					if (!suppress_not_created)
					{
						var msg = $"Bait order on {Exch1.Name} not created for {trade1.TradeType}. ";
						if (validate1 != Trade.EValidation.Valid) msg += $"\n  Bait Trade: {trade1.Description} ({validate1}). ";
						if (validate0 != Trade.EValidation.Valid) msg += $"\n   Ref Trade: {trade0.Description} ({validate0}). ";
						Log.Write(ELogLevel.Warn, msg);
					}
					return null;
				}
			};

			// (Re)Create the base to quote fishing trade
			if (BaitB2Q == null || BaitB2Q.Done)
			{
				// Clean up the previous one
				BaitB2Q = Util.Dispose(BaitB2Q);

				// Pair: B/Q
				//  on Exch1:  B -> Q @ P1  <--- this is our bait trade
				//  on Exch0:  Q -> B @ P0  <--- this is the trade we'll use to match it when the bait is taken
				//      need:  P1 > P0  => P1 = P0 * 1.05
				//
				// Need to know what P0 is. This is the price we're pretty sure we can trade at on Exch0
				//   => trade0 = Q -> B for our available Q on Exch0
				//   => P0 = trade0.Price
				//   => trade0.VolumeOut is the amount of B that we want to use as bait on Exch1
				//
				// VolumeB = minimum of trade0.VolumeOut and our available B on Exch1
				// Create a bait order for B -> Q of VolumeB @ P0 * 1.05 on Exch1
				// When bait is taken
				// Create a match order for Q -> B of the volume taken @ P0 on Exch0
				if (Settings.Direction.HasFlag(ETradeDirection.B2Q) &&
					Pair0.Q2B.Orders.Count != 0 &&
					Pair1.B2Q.Orders.Count != 0)
				{
					// Determine the current price on Exch0 for converting Quote to Base
					var volumeQ = Maths.Min(Settings.Scale * Pair0.Quote.Balance.Available / (1.000001m + Pair0.Fee), Settings.VolumeLimitQ._(Pair0.Quote));
					var trade = Pair0.QuoteToBase(volumeQ);

					// This tells us the equivalent volume of base currency we would get on Exch0.
					// Choose the volume of base currency to trade as the minimum of this and our available on Exch1
					var volumeB = Maths.Min(Settings.Scale * Pair1.Base.Balance.Available / (1.000001m + Pair1.Fee), Settings.VolumeLimitB._(Pair1.Base));
					var volume = Maths.Min(trade.VolumeOut, volumeB);

					// Find the price on Exch1 for trading Base to Quote.
					// If the price is within the price offset range, use this price, otherwise use the middle of the offset range
					var current = Pair1.BaseToQuote(volume);
					var min = trade.PriceInv * (1m + Settings.PriceOffset);
					var price = current.Price > min ? current.Price : (min * 1.001m);

					// Create trade objects to represent the trades we intend to make
					var trade0 = new Trade(ETradeType.Q2B, Pair0, volume*trade.PriceInv, volume, trade.Price);
					var trade1 = new Trade(ETradeType.B2Q, Pair1, volume, volume*price, price);

					BaitB2Q = await CreateBaitOrder(trade0, trade1, m_suppress_not_created_b2q);
					m_suppress_not_created_b2q = BaitB2Q == null;
				}
			}
			else
			{
				// Queue the update task
				tasks.Add(BaitB2Q.Update());
			}

			// (Re)Create the quote to base fishing trade
			if (BaitQ2B == null || BaitQ2B.Done)
			{
				// Clean up the previous one
				BaitQ2B = Util.Dispose(BaitQ2B);

				// Pair: B/Q
				//  on Exch1:  Q -> B @ P1  <--- this is out bait trade
				//  on Exch0:  B -> Q @ P0  <--- this is the trade we'll use to match
				//      need:  P1 > P0  => P1 = P0 * 1.05
				if (Settings.Direction.HasFlag(ETradeDirection.Q2B) &&
					Pair0.B2Q.Orders.Count != 0 &&
					Pair1.Q2B.Orders.Count != 0)
				{
					// Determine the current price on Exch0 for converting Base to Quote
					var volumeB = Maths.Min(Settings.Scale * Pair0.Base.Balance.Available / (1.000001m + Pair0.Fee), Settings.VolumeLimitB._(Pair0.Base));
					var trade = Pair0.BaseToQuote(volumeB);

					// This tells us the equivalent volume of quote currency we would get on Exch0.
					// Choose the volume of quote currency to trade as the minimum of this and our available on Exch1
					var volumeQ = Maths.Min(Settings.Scale * Pair1.Quote.Balance.Available / (1.000001m + Pair1.Fee), Settings.VolumeLimitQ._(Pair1.Quote));
					var volume = Maths.Min(trade.VolumeOut, volumeQ);

					// Find the price on Exch1 for trading Quote to Base.
					// If the price is within the price offset range, use this price, otherwise use the middle of the offset range
					var current = Pair1.QuoteToBase(volume);
					var min = trade.PriceInv * (1m + Settings.PriceOffset);
					var price = current.Price > min ? current.Price : (min * 1.001m);

					// Create trade objects to represent the trades we intend to make
					var trade0 = new Trade(ETradeType.B2Q, Pair0, volume*trade.PriceInv, volume, trade.Price);
					var trade1 = new Trade(ETradeType.Q2B, Pair1, volume, volume*price, price);

					BaitQ2B = await CreateBaitOrder(trade0, trade1, m_suppress_not_created_q2b);
					m_suppress_not_created_q2b = BaitQ2B == null;
				}
			}
			else
			{
				// Quote the update task
				tasks.Add(BaitQ2B.Update());
			}

			// Service the bait trades
			await Task.WhenAll(tasks);
		}
		bool m_suppress_not_created_b2q;
		bool m_suppress_not_created_q2b;

		/// <summary>Manages a fishing trade in a single direction</summary>
		private class FishingTrade :IDisposable
		{
			private readonly Fishing m_fisher;
			public FishingTrade(Fishing fisher, TradeResult order_result, Trade trade0, Trade trade1)
			{
				m_fisher = fisher;
				BaitId = order_result.OrderId ?? 0;
				Trade0 = trade0;
				Trade1 = trade1;
				MatchVolumeFrac = 0m;
				Result = order_result.OrderId != null ? EResult.Fishing : EResult.Complete;
			}
			public void Dispose()
			{
				Debug.Assert(Done);
			}
	
			/// <summary>App logic</summary>
			public Model Model
			{
				get { return m_fisher.Model; }
			}

			/// <summary>The logger instance to write to</summary>
			private Logger Log
			{
				[DebuggerStepThrough] get { return m_fisher.Log; }
			}

			/// <summary>The pair on Exch0</summary>
			public TradePair Pair0
			{
				get { return m_fisher.Pair0; }
			}

			/// <summary>The pair on Exch1</summary>
			public TradePair Pair1
			{
				get { return m_fisher.Pair1; }
			}

			/// <summary>The reference exchange</summary>
			public Exchange Exch0
			{
				get { return m_fisher.Exch0; }
			}

			/// <summary>The target exchange</summary>
			public Exchange Exch1
			{
				get { return m_fisher.Exch1; }
			}

			/// <summary>The offset from the reference price to the bait price</summary>
			public decimal PriceOffset
			{
				get { return m_fisher.Settings.PriceOffset; }
			}

			/// <summary>The trade to make on Exch0 when the bait trade is filled</summary>
			public Trade Trade0 { get; private set; }

			/// <summary>The trade describing the current bait trade</summary>
			public Trade Trade1 { get; private set; }

			/// <summary>The order id of the bait trade</summary>
			public ulong BaitId { get; private set; }

			/// <summary>The order if of the matched trade on 'Exch0' once the bait has been taken</summary>
			public ulong? MatchId { get; private set; }

			/// <summary>The fraction of Trade1 that has been completed. This is needed to match partially completed trades</summary>
			private decimal MatchVolumeFrac { get; set; }

			/// <summary>The result of fishing</summary>
			public EResult Result { get; private set; }

			/// <summary>True when there's no point in calling Update any more</summary>
			public bool Done
			{
				get { return Result == EResult.Complete; }
			}

			/// <summary>Cancel this fishing trade</summary>
			public async Task Cancel()
			{
				if (Result == EResult.Fishing)
					Result = EResult.Cancel;

				try { await Update(); }
				catch (Exception ex)
				{
					if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
					if (ex is OperationCanceledException) { }
					else Log.Write(ELogLevel.Error, ex, $"Failed to cancel bait order (id={BaitId})");
				}
			}

			/// <summary>Check the bait</summary>
			public async Task Update()
			{
				// State machine - gone round till the state stops changing
				for (var result = EResult.Unknown; Result != result;)
				{
					result = Result;
					switch (Result)
					{
					case EResult.Fishing:
						{
							// Check that the position still exists, if not, then it may have been filled.
							var pos = Exch1.Positions[BaitId];
							if (pos == null)
							{
								// Update the volume to be matched (only if the trade was filled)
								if (AssumeGoneMeansFilled)
								{
									// Trade has gone, assume it was filled
									MatchVolumeFrac = 1m;
								}
								else
								{
									// Ensure the trade history is up to date
									await Exch1.TradeHistoryUpdated();

									// If there is a historic trade matching the bait order then the order has been filled.
									var his = Exch1.History[BaitId];
									if (his != null)
										MatchVolumeFrac = 1m;
								}

								// The bait trade may have just been cancelled externally
								if (MatchVolumeFrac != 0m)
									Log.Write(ELogLevel.Warn, $"Bait order (id={BaitId}) on {Exch1.Name} filled. ({Trade1.Description})");

								Result = EResult.Taken;
							}

							// If the position does still exist, check that it's within
							// the range of the reference price plus the offset.
							else
							{
								// Update the volume to be matched
								MatchVolumeFrac = 1m - (pos.Remaining / pos.VolumeBase);

								// Get the current price for trading on Exch0
								Trade0 = Trade0.TradeType == ETradeType.B2Q
									? Pair0.BaseToQuote(Trade0.VolumeIn)
									: Pair0.QuoteToBase(Trade0.VolumeIn);

								// If the bait trade price is too close to the reference price, cancel it and create it again
								var ratio = Math.Abs(Trade0.PriceQ2B - pos.Price) / (decimal)Trade0.PriceQ2B;
								if (ratio <= PriceOffset)
								{
									Log.Write(ELogLevel.Info, $"Bait order (id={BaitId}) on {Exch1.Name} price is too close to the reference price: {pos.Price.ToString("G6")} vs {Trade0.PriceQ2B.ToString("G6")}");
									Result = EResult.Taken;
								}
							}

							break;
						}
					case EResult.Taken:
					case EResult.Cancel:
						{
							// Close the bait trade (if it still exists)
							var pos = Exch1.Positions[BaitId];
							if (pos != null)
							{
								await Exch1.CancelOrder(Pair1, BaitId);
								Log.Write(ELogLevel.Info, $"Bait order (id={BaitId}) on {Exch1.Name} cancelled. ({Trade1.Description})");
								BaitId = 0;
							}

							if (MatchVolumeFrac == 0m)
							{
								// No partial trade to match
								Result = EResult.Complete;
							}
							else
							{
								// Adjust the matching trade by the volume fraction
								Trade0 = MatchVolumeFrac == 1m ? Trade0 : new Trade(Trade0, MatchVolumeFrac);

								// Match any partial trade
								var validation0 = Trade0.Validate();
								if (validation0 == Trade.EValidation.Valid)
								{
									var match_result = await Trade0.CreateOrder();
									MatchId = match_result.OrderId ?? 0;
									Log.Write(ELogLevel.Info, $"Match order (id={MatchId}) on {Exch0.Name} created. Volume match={(100*MatchVolumeFrac).ToString("G5")}%. ({Trade0.Description})");

									// The matched order may be filled immediately.
									// If we're cancelling, set-and-forget the matched order.
									Result =
										Result == EResult.Cancel ? EResult.Complete :
										match_result.OrderId != null ? EResult.Matched :
										EResult.Profit;
								}
								else
								{
									Log.Write(ELogLevel.Info, $"Match order on {Exch0.Name} ignored because invalid ({validation0}). Volume match={(100*MatchVolumeFrac).ToString("G5")}%. ({Trade0.Description})");
									Result = EResult.Complete;
								}
							}

							break;
						}
					case EResult.Matched:
						{
							// The bait trade was filled and a matching trade has been created.
							// Wait for the matching trade to be filled
							var pos = Exch0.Positions[MatchId.Value];
							if (pos == null)
							{
								if (AssumeGoneMeansFilled)
								{
									// Trade has gone, assume it was filled
									Result = EResult.Profit;
								}
								else
								{
									// Ensure the trade history is up to date
									await Exch1.TradeHistoryUpdated();

									// If there is a historic trade matching the match order then the order has been filled.
									var his = Exch1.History[MatchId.Value];

									// The trade was taken if we can see it in the history
									Result = his != null ? EResult.Profit : EResult.Complete;
								}
							}
							break;
						}
					case EResult.Profit:
						{
							// Calculate the nett profit
							var a = Trade1.VolumeOut - Trade0.VolumeIn;
							var b = Trade0.VolumeOut - Trade1.VolumeIn;
							Log.Write(ELogLevel.Warn,
								$"Matched order (id={MatchId}) on {Exch0.Name} filled. ({Trade0.Description}).   !Profit!\n"+
								$"   Nett {Trade0.CoinIn}: {a.ToString("G6")}\n"+
								$"   Nett {Trade1.CoinIn}: {b.ToString("G6")}\n");

							Res.Coins.Play();
							Result = EResult.Complete;
							break;
						}
					case EResult.Complete:
						{
							// Do nothing
							break;
						}
					}
				}
			}
			bool AssumeGoneMeansFilled = true;

			/// <summary>Watched trade result</summary>
			public enum EResult
			{
				Unknown,

				/// <summary>Still waiting for the bait trade to be filled</summary>
				Fishing,

				/// <summary>Close the bait position, matching any partial trade that may have occurred</summary>
				Taken,

				/// <summary>Close the bait position, matching any partial trade that may have occurred but skip to complete without monitoring the matched position</summary>
				Cancel,

				/// <summary>The matched trade has been placed, and we're waiting for it to be filled</summary>
				Matched,

				/// <summary>The bait trade was taken, and the matched trade has been filled resulting in profit!</summary>
				Profit,

				/// <summary>Fish landed or trade cancelled.</summary>
				Complete,
			}
		}

		/// <summary>Log view UI</summary>
		public class LogView :ToolForm
		{
			private readonly Fishing m_fisher;
			private LogUI m_log_ui;

			public LogView(Fishing fisher, Control parent)
				:base(parent, EPin.Centre)
			{
				m_fisher = fisher;
				Size = new Size(600, 300);
				Icon = (parent as Form)?.Icon;
				Text = m_fisher.Settings.Name;
				FormBorderStyle = FormBorderStyle.Sizable;
				StartPosition = FormStartPosition.CenterParent;
				HideOnClose = false;

				m_log_ui = Controls.Add2(new LogUI(m_fisher.Settings.Name, m_fisher.Settings.Name)
				{
					Dock = DockStyle.Fill,
					LogFilepath = (m_fisher.Log.LogCB as LogToFile).Filepath,
					LogEntryPattern = Misc.LogEntryPattern,
				});
				m_log_ui.Highlighting.AddRange(Misc.LogHighlighting);
			}
		}
	}
}
