using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using pr.attrib;
using pr.common;
using pr.extn;
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
			var logname = Util.IsDebug ? $"log_{data.Name}_debug.txt" : $"log_{data.Name}.txt";
			var logpath = Util.ResolveUserDocumentsPath("Rylogic", "CoinFlip", $"Logs\\{logname}");
			Directory.CreateDirectory(Path_.Directory(logpath));

			Model = model;
			Settings = data;
			Log = new Logger(string.Empty, new LogToFile(logpath, append:false), Model.Log);
			Log.TimeZero = Log.TimeZero - Log.TimeZero .TimeOfDay;
			DetailsUI = new FishingDetailsUI(this);
		}
		public virtual void Dispose()
		{
			Debug.Assert(!Active, "Main loop must be shutdown before Disposing");
			DetailsUI = null;
			Log = null;
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

				Log.Write(ELogLevel.Info, $"Fishing started - {Pair0.Name} on {Exch1.Name} vs {Exch0.Name}");

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

						// Notify updated
						Updated.Raise(this);
					}
					catch (Exception ex)
					{
						if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
						if (ex is OperationCanceledException) { break; }
						else Log.Write(ELogLevel.Error, ex, $"Fishing instance {Settings.Name} update failed.\r\n{ex.StackTrace}");
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
					else Log.Write(ELogLevel.Error, ex, $"Fishing instance {Settings.Name} ClosePositions failed.\r\n{ex.StackTrace}");
				}

				Updated.Raise(this);
				Log.Write(ELogLevel.Info, $"Fishing stopped - {Pair0.Name} on {Exch1.Name} vs {Exch0.Name}");
			}
		}
		private CancellationTokenSource m_main_loop_shutdown;
		private int m_active;

		/// <summary>A log for this fishing instance</summary>
		public Logger Log
		{
			[DebuggerStepThrough] get { return m_log; }
			private set
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

					m_suppress_not_created_b2q = false;
					m_suppress_not_created_q2b = false;
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
		public string Name      { get { return Settings.Name; } }
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
		public FishingDetailsUI DetailsUI
		{
			get { return m_details_ui; }
			private set
			{
				if (m_details_ui == value) return;
				Util.Dispose(ref m_details_ui);
				m_details_ui = value;
			}
		}
		private FishingDetailsUI m_details_ui;

		/// <summary>The order held on the target exchange to trade Base to Quote</summary>
		public FishingTrade BaitB2Q
		{
			get { return m_baitb2q; }
			private set
			{
				if (m_baitb2q == value) return;
				Util.Dispose(ref m_baitb2q);
				m_baitb2q = value;
			}
		}
		private FishingTrade m_baitb2q;

		/// <summary>The order held on the target exchange to trade Quote to Base</summary>
		public FishingTrade BaitQ2B
		{
			get { return m_baitq2b; }
			private set
			{
				if (m_baitq2b == value) return;
				Util.Dispose(ref m_baitq2b);
				m_baitq2b = value;
			}
		}
		private FishingTrade m_baitq2b;

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
					// Create the fishing trade
					var fisher = new FishingTrade(this, trade0, trade1);
					await fisher.Start();
					return fisher;
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

			// Pair: B/Q
			//  on Exch1:  B -> Q @ P1  <--- this is the bait trade (the one we have in the order book on the exchange, waiting to be taken)
			//  on Exch0:  Q -> B @ P0  <--- this is the match trade (used to match the bait trade when it is taken)
			//      need:  P1 > P0      <--- for this to be profitable, P1 needs to be higher than P0 (note: conversion price, not Q2B price)
			//       let:  P1 = P0 * (1 + price_offset)
			//
			// Need to know what P0 is on Exch0. This is the match trade price we're pretty sure we can trade at on Exch0
			//   => VolumeQ = available balance of Q on Exch0. (within limits)
			//   => trade0 = Q -> B for VolumeQ @ the best price on Exch0.
			//   => P0 = 'trade0.Price'
			//   => 'trade0.VolumeOut' is the amount of B that we want to use as bait on Exch1.
			//   => VolumeB = minimum of 'trade0.VolumeOut' and our available balance of B on Exch1 (within limits).
			//
			// Bait order = B -> Q of VolumeB @ P0 * price_offset on Exch1
			// 'price_offset' must be greater than 'Exch0.Fee + Exch1.Fee' since we're doing a transaction on each exchange.
			//
			// When the bait order is filled:
			// Match order = Q -> B of VolumeQ @ P0 on Exch0

			// (Re)Create the base to quote fishing trade
			if (BaitB2Q == null || BaitB2Q.Done)
			{
				// Clean up the previous one
				BaitB2Q = null;

				if (Settings.Direction.HasFlag(ETradeDirection.B2Q) &&
					Pair0.Q2B.Orders.Count != 0 &&
					Pair1.B2Q.Orders.Count != 0)
				{
					// Find the available balances on each exchange (reduced to allow for fees and rounding)
					var volumeQ = Maths.Min(Pair0.Quote.Balance.Available * (1m - Pair0.Fee) * 0.99999m, Pair0.Quote.AutoTradeLimit);
					var volumeB = Maths.Min(Pair1.Base .Balance.Available * (1m - Pair1.Fee) * 0.99999m, Pair1.Base .AutoTradeLimit);

					// Determine the current price on Exch0 for converting Quote to Base
					var trade = Pair0.QuoteToBase(volumeQ);

					// This tells us the equivalent volume of base currency we would get on Exch0.
					// Choose the volume of base currency to trade as the minimum of this and our available on Exch1
					var volume = Maths.Min(trade.VolumeOut, volumeB);

					// Find the spot price on Exch1 for trading Base to Quote.
					var spot = Pair1.QuoteToBase(volumeQ);
					var min = trade.PriceInv * (1m + Settings.PriceOffset);
					var def = trade.PriceInv * (1m + Settings.PriceOffset * 1.5m);
					var price = spot.PriceInv > min ? spot.PriceInv : def;

					// Create trade objects to represent the trades we intend to make
					var trade0 = new Trade(ETradeType.Q2B, Pair0, volume/trade.Price, volume, trade.Price); // Match
					var trade1 = new Trade(ETradeType.B2Q, Pair1, volume, volume*price, price); // Bait

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
				BaitQ2B = null;

				if (Settings.Direction.HasFlag(ETradeDirection.Q2B) &&
					Pair0.B2Q.Orders.Count != 0 &&
					Pair1.Q2B.Orders.Count != 0)
				{
					// Find the available balances each exchange (reduced to allow for fees and rounding)
					var volumeB = Maths.Min(Pair0.Base.Balance.Available  * (1m - Pair0.Fee) * 0.99999m, Pair0.Base .AutoTradeLimit);
					var volumeQ = Maths.Min(Pair1.Quote.Balance.Available * (1m - Pair1.Fee) * 0.99999m, Pair1.Quote.AutoTradeLimit);

					// Determine the current price on Exch0 for converting Base to Quote
					var trade = Pair0.BaseToQuote(volumeB);

					// This tells us the equivalent volume of quote currency we would get on Exch0.
					// Choose the volume of quote currency to trade as the minimum of this and our available on Exch1
					var volume = Maths.Min(trade.VolumeOut, volumeQ);

					// Find the spot price on Exch1 for trading Quote to Base.
					// If the price is within the price offset range, use this price, otherwise use the middle of the offset range
					var spot = Pair1.BaseToQuote(volumeB);
					var min = trade.PriceInv * (1m + Settings.PriceOffset);
					var def = trade.PriceInv * (1m + Settings.PriceOffset * 1.5m);
					var price = spot.PriceInv > min ? spot.PriceInv : def;

					// Create trade objects to represent the trades we intend to make
					var trade0 = new Trade(ETradeType.B2Q, Pair0, volume/trade.Price, volume, trade.Price);
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

		/// <summary>Raised after each update</summary>
		public event EventHandler Updated;

		/// <summary>Manages a fishing trade in a single direction</summary>
		public class FishingTrade :IDisposable
		{
			private readonly Fishing m_fisher;
			private Guid m_balance_hold;

			public FishingTrade(Fishing fisher, Trade trade0, Trade trade1)
			{
				m_fisher = fisher;
				Trade0 = trade0;
				Trade1 = trade1;
				MatchVolumeFrac = 0m;
				BaitId = 0;
				Result = EResult.Fishing;

				// Check that the bait and match orders should result in profit
				var a = Trade0.VolumeNett - Trade1.VolumeIn;
				var b = Trade1.VolumeNett - Trade0.VolumeIn;
				var profit0 = a + b * Trade0.Price;
				var profit1 = b + a * Trade1.Price;
				if (profit0 < 0 || profit1 < 0)
					Log.Write(ELogLevel.Warn, "Bait/Match trades do not result in profit");
			}
			public void Dispose()
			{
				Trade0.CoinIn.Balance.Release(m_balance_hold);
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

			/// <summary>Open the bait trade and start fishing</summary>
			public async Task Start()
			{
				// Reserve funds for 'trade0' until the bait order is taken. (enough to cover the transaction fee as well)
				m_balance_hold = Trade0.CoinIn.Balance.Hold(Trade0.VolumeIn * (1m + Pair0.Fee), b => !Done);

				// Create the bait order
				var order_result = await Trade1.CreateOrder();

				// Record the bait trade id
				BaitId = order_result.OrderId ?? 0;

				// Log the fishing order
				Log.Write(ELogLevel.Info, Str.Build(
					$"Bait order (id={order_result.OrderId}) on {Exch1.Name} created for {Trade1.TradeType}.\n",
					$"  Match: {Trade0.Description} \n",
					$"   Bait: {Trade1.Description} \n",
					$"  Ratio: {(100m * Math.Abs(1m - Trade1.PriceQ2B/Trade0.PriceQ2B)):G4}%"));
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
				// State machine - go around till the state stops changing
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
								if (Exch1.TradeHistoryUseful)
								{
									// Ensure the trade history is up to date
									await Exch1.TradeHistoryUpdated();

									// If there is a historic trade matching the bait order then the order has been filled.
									var his = Exch1.History[BaitId];
									if (his != null)
										MatchVolumeFrac = 1m;

									if (his != null)
										Log.Write(ELogLevel.Debug, $"Bait order found in history. Confirmed order filled. ({his.Description})");
									else
										Log.Write(ELogLevel.Debug, $"Match order (id={BaitId}) not found in history. Order cancelled externally");
								}
								else
								{
									// Trade has gone, assume it was filled
									MatchVolumeFrac = 1m;
								}

								// Log if some or all of the bait trade was filled
								if (MatchVolumeFrac != 0m)
									Log.Write(ELogLevel.Warn, $"Bait order (id={BaitId}) on {Exch1.Name} filled to {(100*MatchVolumeFrac).ToString("G4")}%. ({Trade1.Description})");

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

								// Determine the price to reference price ratio.
								var ratio = Maths.Div(Math.Abs(Trade0.PriceQ2B - pos.Price), (decimal)Trade0.PriceQ2B, 0m);
								var validation0 = Trade0.Validate(m_balance_hold);

								// If the bait trade price is too close to the reference price, cancel it and create it again
								if (ratio <= PriceOffset)
								{
									Log.Write(ELogLevel.Info, $"Bait order (id={BaitId}) on {Exch1.Name} price is too close to the reference price: {pos.Price.ToString("G6")} vs {Trade0.PriceQ2B.ToString("G6")} ({100*ratio:G6}%)");
									Result = EResult.Taken;
								}
								// If the bait trade price is too far away from the reference price,
								// and a long way down the order book, cancel it.
								else if (ratio > 2 * PriceOffset)
								{
									// A B2Q trade means we need someone else to trade Q2B. Q2B.Orders are the currently offered Q2B trades.
									// We need to look at what is being offered for Q2B to see where the bait price fits in.
									var idx = Trade1.OrderBookIndex;
									if (idx >= 10)
									{
										Log.Write(ELogLevel.Info, $"Bait order (id={BaitId}) on {Exch1.Name} price is too far from the reference price: {pos.Price.ToString("G6")} vs {Trade0.PriceQ2B.ToString("G6")}, order book index: {idx} ({100*ratio:G6}%)");
										Result = EResult.Taken;
									}
								}
								// If, for some reason, the matching trade is no longer valid (e.g. lack of funds)
								// cancel the bait trade, because we won't be able to match it.
								else if (validation0 != Trade.EValidation.Valid)
								{
									Log.Write(ELogLevel.Info, $"Bait order (id={BaitId}) on {Exch1.Name} can not be matched on {Exch0.Name}: {validation0} ({Trade0.Description})");
									Result = EResult.Cancel;
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
							}
							BaitId = 0;

							if (MatchVolumeFrac == 0m)
							{
								// No partial trade to match
								Result = EResult.Complete;
							}
							else
							{
								// Adjust the matching trade by the volume fraction
								Trade0 = MatchVolumeFrac == 1m ? Trade0 : new Trade(Trade0, MatchVolumeFrac);

								// Release the hold on the balance we had reserved for 'Trade0'
								Trade0.CoinIn.Balance.Release(m_balance_hold);

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
								if (Exch0.TradeHistoryUseful)
								{
									// Ensure the trade history is up to date
									await Exch0.TradeHistoryUpdated();

									// If there is a historic trade matching the match order then the order has been filled.
									var his = Exch0.History[MatchId.Value];

									// The trade was taken if we can see it in the history
									Result = his != null ? EResult.Profit : EResult.Complete;

									if (his != null)
										Log.Write(ELogLevel.Debug, $"Match order (id={MatchId}) on {Exch0.Name} found in history. Order filled confirmed. ({his.Description})");
									else
										Log.Write(ELogLevel.Debug, $"Match order (id={MatchId}) on {Exch0.Name} not found in history. Order cancelled externally.");
								}
								else
								{
									// Trade has gone, assume it was filled
									Result = EResult.Profit;
									Log.Write(ELogLevel.Info, $"Matched order (id={MatchId}) on {Exch0.Name} filled. ({Trade0.Description}).");
								}
							}
							break;
						}
					case EResult.Profit:
						{
							// Calculate the nett profit
							var out0 = (decimal)Trade0.VolumeNett;
							var out1 = (decimal)Trade1.VolumeNett;

							var nett0 = out0 - Trade1.VolumeIn;
							var nett1 = out1 - Trade0.VolumeIn;

							var value0 = Trade0.CoinOut.Value(nett0);
							var value1 = Trade1.CoinOut.Value(nett1);
							var sum = value0 + value1;

							var effective_price0 = (decimal)Trade0.PriceNettQ2B;
							var effective_price1 = (decimal)Trade1.PriceNettQ2B;
							var ratio = Math.Abs(effective_price0 - effective_price1) / effective_price0;

							Log.Write(ELogLevel.Warn, Str.Build(
								$"!Profit!\n",
								$"  Bait order on {Exch1.Name}: {Trade1.Description} (After Fees: {out1:G6} @ {effective_price1:G6})\n",
								$" Match order on {Exch0.Name}: {Trade0.Description} (After Fees: {out0:G6} @ {effective_price0:G6})\n",
								$"\n",
								$"  Nett {Trade0.CoinOut}: {nett0:G8}  ({value0:C})\n",
								$"  Nett {Trade1.CoinOut}: {nett1:G8}  ({value1:C})\n",
								$"  Total: {sum:C}  Ratio: {100*ratio:G6}%"));

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

			/// <summary>Watched trade result</summary>
			public enum EResult
			{
				[Assoc("Color", 0xFFabababU)] Unknown,

				/// <summary>Still waiting for the bait trade to be filled</summary>
				[Assoc("Color", 0xFF90ee90U)] Fishing,

				/// <summary>Close the bait position, matching any partial trade that may have occurred</summary>
				[Assoc("Color", 0xFF41c73aU)] Taken,

				/// <summary>Close the bait position, matching any partial trade that may have occurred but skip to complete without monitoring the matched position</summary>
				[Assoc("Color", 0xFFff7e39U)] Cancel,

				/// <summary>The matched trade has been placed, and we're waiting for it to be filled</summary>
				[Assoc("Color", 0xFF00d5ffU)] Matched,

				/// <summary>The bait trade was taken, and the matched trade has been filled resulting in profit!</summary>
				[Assoc("Color", 0xFFe7a5ffU)] Profit,

				/// <summary>Fish landed or trade cancelled.</summary>
				[Assoc("Color", 0xFFFFFFFFU)] Complete,
			}
		}
	}
}
