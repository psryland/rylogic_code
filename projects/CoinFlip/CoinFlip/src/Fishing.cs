using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	public class Fishing :IDisposable
	{
		// Notes:
		//  Go fishing on 'exch1' by setting offers above/below the best price on 'exch0'
		//  If the orders on 'exch1' are filled, offset the trade with the reverse trade on 'exch0'

		public Fishing(Model model, TradePair pair0, TradePair pair1, decimal scale)
		{
			Model = model;
			Scale = scale;
			Pair0 = pair0;
			Pair1 = pair1;
			BaitPriceOffsetFrac = new RangeF<decimal>(0.01m, 0.05m);

			// Sanity check
			if (pair0.Exchange == pair1.Exchange)
				throw new Exception("Pairs must be from different exchanges");
			if (pair0.Exchange == Model.CrossExchange)
				throw new Exception("Pairs cannot be CrossExchange pairs");
			if (pair1.Exchange == Model.CrossExchange)
				throw new Exception("Pairs cannot be CrossExchange pairs");
			if (pair0.Base.Symbol != pair1.Base.Symbol ||
				pair0.Quote.Symbol != pair1.Quote.Symbol)
				throw new Exception("Pairs must be for the same currencies but on different exchanges");
		}
		public virtual void Dispose()
		{
			Active = false;
			ClosePositions();
			Model = null;
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
		public TradePair Pair0
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>The trading pair that we have an order waiting to be taken on</summary>
		public TradePair Pair1
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>Trade scaling</summary>
		public decimal Scale
		{
			get;
			set;
		}

		/// <summary>The percentage difference (as a fraction) between the price on 'Exch0' and the bait price</summary>
		public RangeF<decimal> BaitPriceOffsetFrac
		{
			get;
			set;
		}

		/// <summary>The order held on the target exchange to trade Base to Quote</summary>
		private FishingTrade BaitB2Q { get; set; }

		/// <summary>The order held on the target exchange to trade Quote to Base</summary>
		private FishingTrade BaitQ2B { get; set; }

		/// <summary>Enable/Disable fishing using this instance</summary>
		public bool Active
		{
			get { return m_main_loop_thread != null; }
			set
			{
				if (Active == value) return;
				if (Active)
				{
					m_main_loop_shutdown.Set();
					if (m_main_loop_thread.IsAlive)
						m_main_loop_thread.Join();

					Util.Dispose(ref m_main_loop_shutdown);
				}
				m_main_loop_thread = value ? new Thread(new ThreadStart(MainLoopThreadEntry)) : null;
				if (Active)
				{
					m_main_loop_shutdown = new ManualResetEvent(false);
					m_main_loop_thread.Start();
				}
			}
		}
		private ManualResetEvent m_main_loop_shutdown;
		private Thread m_main_loop_thread;

		/// <summary>Main loop for the fisher</summary>
		private void MainLoopThreadEntry()
		{
			Thread.CurrentThread.Name = $"Fisher {Pair0.Name}";
			Func<bool> shutdown = () => m_main_loop_shutdown.IsSignalled();

			var cv = new ConditionVariable<bool>(true);
			for (;;)
			{
				// Wait till we're allowed to run the next loop
				cv.Wait(loop => loop || shutdown());
				if (shutdown()) break;
				cv.NotifyAll(false);

				// Run a step of the main loop
				Model.RunOnGuiThread(async () =>
				{
					// Service the fishing trades
					try { await Update(); }
					catch (Exception ex)
					{
						if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
						if (ex is OperationCanceledException) { }
						else Model.Log.Write(ELogLevel.Error, ex, $"Fishing instance {Exch0.Name} -> {Exch1.Name} update failed.");
					}

					// Signal that the next loop can start
					cv.NotifyAll(true);
				});
			}
		}

		/// <summary>Close any positions held open by this instance</summary>
		public void ClosePositions()
		{
			BaitB2Q = Util.Dispose(BaitB2Q);
			BaitQ2B = Util.Dispose(BaitQ2B);
		}

		/// <summary>Service the fishing orders</summary>
		private async Task Update()
		{
			var tasks = new List<Task>();

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

				// Determine the current price on Exch0 for converting Quote to Base
				var trade = Pair0.QuoteToBase(Pair0.Quote.Balance.Available * Scale);

				// This tells us the equivalent volume of base currency we would get on Exch0.
				// Choose the volume of base currency to trade as the minimum of this and our available on Exch1
				var volume = Maths.Min(trade.VolumeOut, Pair1.Base.Balance.Available * Scale);

				// Set the bait trade price
				var price = trade.PriceInv * (1 + BaitPriceOffsetFrac.Mid);

				// Create trade objects to represent the trades we intend to make
				var trade0 = new Trade(ETradeType.Q2B, Pair0, volume*trade.PriceInv, volume, trade.Price);
				var trade1 = new Trade(ETradeType.B2Q, Pair1, volume, volume*price, price);

				// Check the amounts we want to trade are valid
				if (trade0.Validate() == Trade.EValidation.Valid &&
					trade1.Validate() == Trade.EValidation.Valid)
				{
					// Create the bait order
					var order_id = await trade1.CreateOrder();

					// Create the fishing trade
					BaitB2Q = new FishingTrade(this, order_id, trade0, trade1);
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
				
				// Determine the current price on Exch0 for converting Base to Quote
				var trade = Pair0.BaseToQuote(Pair0.Base.Balance.Available * Scale);

				// This tells us the equivalent volume of quote currency we would get on Exch0.
				// Choose the volume of quote currency to trade as the minimum of this and our available on Exch1
				var volume = Maths.Min(trade.VolumeOut, Pair1.Quote.Balance.Available * Scale);

				// Set the bait trade price
				var price = trade.PriceInv * (1 + BaitPriceOffsetFrac.Mid);

				// Create trade objects to represent the trades we intend to make
				var trade0 = new Trade(ETradeType.B2Q, Pair0, volume*trade.PriceInv, volume, trade.Price);
				var trade1 = new Trade(ETradeType.Q2B, Pair1, volume, volume*price, price);

				// Check the amounts we want to trade are valid
				if (trade0.Validate() == Trade.EValidation.Valid &&
					trade1.Validate() == Trade.EValidation.Valid)
				{
					// Create the bait order
					var order_id = await trade1.CreateOrder();

					// Create the fishing trade
					BaitQ2B = new FishingTrade(this, order_id, trade0, trade1);
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

		/// <summary>Manages a fishing trade in a single direction</summary>
		private class FishingTrade :IDisposable
		{
			private readonly Fishing m_fishing;
			public FishingTrade(Fishing fishing, ulong bait_order_id, Trade trade0, Trade trade1)
			{
				m_fishing = fishing;
				BaitId = bait_order_id;
				Trade0 = trade0;
				Trade1 = trade1; Result = EResult.Fishing;
			}
			public void Dispose()
			{
				// If the bait trade is still active, cancel it
				if (Result == EResult.Fishing)
					Cancel().Wait();
			}
	
			/// <summary>App logic</summary>
			public Model Model
			{
				get { return m_fishing.Model; }
			}

			/// <summary>The pair on Exch0</summary>
			public TradePair Pair0
			{
				get { return m_fishing.Pair0; }
			}

			/// <summary>The pair on Exch1</summary>
			public TradePair Pair1
			{
				get { return m_fishing.Pair1; }
			}

			/// <summary>The reference exchange</summary>
			public Exchange Exch0
			{
				get { return m_fishing.Exch0; }
			}

			/// <summary>The target exchange</summary>
			public Exchange Exch1
			{
				get { return m_fishing.Exch1; }
			}

			/// <summary>The offset from the reference price to the bait price</summary>
			public RangeF<decimal> PriceOffsetFrac
			{
				get { return m_fishing.BaitPriceOffsetFrac; }
			}

			/// <summary>The trade to make on Exch0 when the bait trade is filled</summary>
			public Trade Trade0 { get; private set; }

			/// <summary>The trade describing the current bait trade</summary>
			public Trade Trade1 { get; private set; }

			/// <summary>The order id of the bait trade</summary>
			public ulong BaitId { get; private set; }

			/// <summary>The order if of the matched trade on 'Exch0' once the bait has been taken</summary>
			public ulong? MatchId { get; private set; }

			/// <summary>The result of fishing</summary>
			public EResult Result { get; private set; }

			/// <summary>True when there's no point in calling Update any more</summary>
			public bool Done
			{
				get { return Result == EResult.Cancelled || Result == EResult.Profit; }
			}

			/// <summary>Cancel this fishing trade</summary>
			public async Task Cancel()
			{
				if (Result != EResult.Cancelled)
				{
					try { await Exch1.CancelOrder(Pair1, BaitId); }
					catch (Exception ex)
					{
						if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
						if (ex is OperationCanceledException) { }
						else Model.Log.Write(ELogLevel.Error, ex, $"Fishing trade {Exch0.Name} -> {Exch1.Name} ({Trade1.TradeType}) cancel failed.");
					}
					Result = EResult.Cancelled;
				}
			}

			/// <summary>Check the bait</summary>
			public async Task Update()
			{
				switch (Result)
				{
				case EResult.Fishing:
					#region
					{
						// Check that the position still exists.
						// If the position no longer exists, check our
						// trade history to confirm the trade has been made.
						var pos = Exch1.Positions[BaitId];
						if (pos == null)
						{
							// Ensure the trade history is up to date
							await Exch1.TradeHistoryUpdated();

							// If there is a historic trade matching the
							// bait order then the order has been filled.
							var his = Exch1.History[BaitId];
							Result = his != null ? EResult.Taken : EResult.Cancelled;
							
							// Place the reverse order at the reference price on 'Exch0'
							if (Result == EResult.Taken)
								MatchId = await Trade0.CreateOrder();
						}

						// If the position does still exist, check that it's within
						// the range of the reference price plus the offset.
						else
						{
							// Get the current price for trading on Exch0
							Trade0 = Trade0.TradeType == ETradeType.B2Q
								? Pair0.BaseToQuote(Trade0.VolumeIn)
								: Pair0.QuoteToBase(Trade0.VolumeIn);

							// If the bait trade price is outside the allowed range
							// cancel it, and create it again at the new price.
							var ref_price = Trade0.TradeType == ETradeType.B2Q ? Trade0.Price : Trade0.PriceInv;
							var min = ref_price * (1 + PriceOffsetFrac.Beg);
							var max = ref_price * (1 + PriceOffsetFrac.End);
							if (!pos.Price.Within(min, max))
							{
								// Cancel the old bait trade
								await Exch1.CancelOrder(Pair1, BaitId);

								// Create the new bait trade at the new price
								var price = Trade0.PriceInv * (1 + PriceOffsetFrac.Mid);
								Trade1 = new Trade(Trade1.TradeType, Trade1.Pair, Trade1.VolumeIn, Trade1.VolumeOut, price);
								BaitId = await Trade1.CreateOrder();
							}
						}
						break;
					}
					#endregion
				case EResult.Taken:
					#region
					{
						// The bait trade was filled and a matching trade has been created.
						// Wait for the matching trade to be filled
						var pos = Exch0.Positions[MatchId.Value];
						if (pos == null)
						{
							// Ensure the trade history is up to date
							await Exch1.TradeHistoryUpdated();
							var his = Exch1.History[MatchId.Value];
							Result = his != null ? EResult.Profit : EResult.Cancelled;
						}
						break;
					}
					#endregion
				case EResult.Cancelled:
				case EResult.Profit:
					#region
					{
						// Do nothing, wait for dispose
						break;
					}
					#endregion
				}
			}

			/// <summary>Watched trade result</summary>
			public enum EResult
			{
				/// <summary>Still waiting for the bait trade to be filled</summary>
				Fishing,

				/// <summary>The bait trade has been filled and the matched trade has been placed</summary>
				Taken,

				/// <summary>The bait trade was cancelled. No matched trade placed</summary>
				Cancelled,

				/// <summary>The bait trade was taken, and the matched trade has been filled resulting in profit!</summary>
				Profit,
			}
		}
	}
}
