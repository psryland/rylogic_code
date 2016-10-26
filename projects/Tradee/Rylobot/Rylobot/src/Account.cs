using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;
using pr.extn;
using pr.maths;
using pr.util;

namespace Rylobot
{
	/// <summary>Manages the account and current risk</summary>
	public class Broker :INotifyPropertyChanged ,IDisposable
	{
		public Broker(Rylobot bot, IAccount acct)
		{
			Bot = bot;
			Update(acct);
		}
		public void Dispose()
		{
			Bot = null;
		}

		/// <summary>The App logic</summary>
		public Rylobot Bot
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>Update the state of the account</summary>
		public void Update()
		{
			Update(Bot.Account);
			Update(Bot.Positions);
			Update(Bot.PendingOrders);
		}

		/// <summary>Update the state of the account from received account info</summary>
		private void Update(IAccount acct)
		{
			var acct_change = AccountId != acct.Number;
			if (acct_change)
				AccountChanging.Raise(this);

			AccountId = acct.Number;
			IsLive    = acct.IsLive;
			Currency  = acct.Currency;
			Balance   = acct.Balance;
			Equity    = acct.Equity;
			Leverage  = acct.Leverage;

			if (acct_change)
				AccountChanged.Raise(this);
		}

		/// <summary>Update the risk level from currently active positions</summary>
		private void Update(Positions positions)
		{
			// Add up all the potential losses from current positions
			var risk = 0.0;
			foreach (var position in positions)
			{
				// The stop loss will be negative if it is on the 'win' side
				// of the entry price, regardless of trade type.
				var sym = Bot.MarketData.GetSymbol(position.SymbolCode);
				var sl = position.StopLossRel() != 0 ? position.StopLossRel() : position.EntryPrice; // assume SL at $0 if none set
				risk += (position.StopLossRel() / sym.PipSize) * sym.PipValue * position.Volume;
			}

			// Update the current risk from active positions
			CurrentRisk = risk;
		}

		/// <summary>Update the risk level from pending orders positions</summary>
		private void Update(PendingOrders orders)
		{
			// Add up all potential losses from pending orders
			var risk = 0.0;
			foreach (var order in orders)
			{
				// The stop loss will be negative if it is on the 'win' side
				// of the entry price, regardless of trade type.
				var sym = Bot.MarketData.GetSymbol(order.SymbolCode);
				var sl = order.StopLossRel() != 0 ? order.StopLossRel() : order.TargetPrice; // assume SL at $0 if none set
				risk += (order.StopLossRel() / sym.PipSize) * sym.PipValue * order.Volume;
			}

			PendingRisk = risk;
		}

		/// <summary>Creates a market order in the given direction taking into account risk</summary>
		public Position CreateOrder(Symbol sym, TradeType tt, string label)
		{
			try
			{
				// Want a stop loss just above or below a recent high or low (depending on 'tt')
				// The stop loss should only risk up to the max risk percentage of the current balance
				// If the stop loss is nowhere near the desired risk, the volume can be adjusted
				using (var instr = new Instrument(Bot, sym.Code))
				{
					instr.Dump();

					// The caller wants an order placed now in the direction of 'tt'
					// Choose appropriate stop loss and take profit levels, and volume,
					var volume = sym.VolumeMin;
					var sl = ChooseSL(instr, tt, out volume);
					var tp = ChooseTP(instr, tt, sl, Bot.Settings.Acct.MinRewardToRisk);
					this.DumpOrder(instr, tt, sl, tp);

					// Place the order
					var r = Bot.ExecuteMarketOrder(tt, sym, volume, label, sl, tp);
					if (!r.IsSuccessful)
						throw new Exception("Execute market order failed: {0}".Fmt(r.Error));

					// Update the account balance
					Update();

					// Return the created position
					return r.Position;
				}
			}
			catch (Exception ex)
			{
				Bot.Print(ex.Message);
				return null;
			}
		}

		/// <summary>Choose an appropriate stop loss (in pips) for the given instrument</summary>
		public double ChooseSL(Instrument instr, TradeType tt, out long volume)
		{
			// Find the percentage of balance available to risk
			var max_risk_pc = Bot.Settings.Acct.MaxRiskPC;
			var available_risk_pc = max_risk_pc - TotalRiskPC;
			if (available_risk_pc <= 0)
				throw new Exception("Insufficient available risk. Current Risk: {0}%, Maximum Risk: {1}%".Fmt(TotalRiskPC, max_risk_pc));

			// Find the account currency value of the available risk
			var balance_to_risk = Balance * available_risk_pc * 0.01;

			// Look for a peak in the recent history of the quote currency
			var sign = tt.Sign();
			var current_price = instr.CurrentPrice(sign);

			// Scan backwards looking for a peak in the stop loss direction.
			var sl = 0.0;
			foreach (var candle in instr.CandleRange(-Bot.Settings.Acct.LookBackCount, 0))
			{
				var limit = candle.WickLimit(-sign);
				var diff = current_price - limit;
				if (Maths.Sign(diff) != sign) continue;
				if (Math.Abs(diff) < sl) continue;
				sl = Math.Abs(diff);
			}

			// For short trades, add the spread to the SL
			sl += (sign > 0 ? 0 : instr.Symbol.Spread);

			// Add on a bit as a safety buffer
			sl *= 1.05f;

			// Adjust the volume so that the risk is within the acceptable range
			// If the risk is too high reduce the volume first, down to the VolumeMin
			// then reduce 'peak'. If the risk is low, increase volume to fit within 'risk'

			// Find the account value risked at the current stop loss
			var sl_acct = instr.Symbol.QuotePriceToAcct(sl);
			var optimal_volume = balance_to_risk / sl_acct;

			// If the risk is too high, reduce the stop loss
			if (optimal_volume < instr.Symbol.VolumeMin)
			{
				volume = instr.Symbol.VolumeMin;
				sl = instr.Symbol.AcctToQuotePrice(balance_to_risk / volume);
			}
			// Otherwise, round down to the nearest volume multiple
			else
			{
				volume = instr.Symbol.NormalizeVolume(optimal_volume, RoundingMode.Down);
			}

			// Convert the risk to pips
			return instr.Symbol.QuotePriceToPips(sl);
		}

		/// <summary>Choose an appropriate take profit (in pips) for the given instrument</summary>
		public double ChooseTP(Instrument instr, TradeType tt, double sl_pips, double min_reward_to_risk)
		{
			var sign = tt.Sign();
			var current_price = instr.CurrentPrice(sign);

			// Get the support and resistance levels
			var snr = new SnR(instr, -Bot.Settings.Acct.LookBackCount, 0);
			snr.Dump();

			// The take profit as determined by the minimum reward to risk ratio (signed value in quote currency)
			var tp = instr.Symbol.PipsToQuotePrice(sl_pips) * min_reward_to_risk;

			// Set the TP at the first significant SnR level above the RR ratio
			foreach (var lvl in snr.SnRLevels.Take(snr.SnRLevels.Count/2))
			{
				var dprice = lvl.Price - current_price;
				if (Maths.Sign(dprice) != sign) continue;
				if (sign * dprice < tp) continue;
				tp = sign * dprice;
				break;
			}

			return instr.Symbol.QuotePriceToPips(tp);
		}

		/// <summary>Raised when the AccountId is about to change</summary>
		public event EventHandler AccountChanging;

		/// <summary>Raised after the AccountId has changed</summary>
		public event EventHandler AccountChanged;

		/// <summary>Raised on property changed</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void SetProp<T>(ref T prop, T value, params string[] prop_names)
		{
			if (Equals(prop, value)) return;
			prop = value;

			// Raise notification for each affected property
			foreach (var name in prop_names)
				PropertyChanged.Raise(this, new PropertyChangedEventArgs(name));
		}

		/// <summary>Account Id</summary>
		public int AccountId
		{
			get { return m_account_id; }
			set { SetProp(ref m_account_id, value, R<Broker>.Name(x => x.AccountId)); }
		}
		private int m_account_id;

		/// <summary>True if this account uses real money</summary>
		public bool IsLive
		{
			get { return m_is_live; }
			set { SetProp(ref m_is_live, value, R<Broker>.Name(x => x.IsLive)); }
		}
		private bool m_is_live;

		/// <summary>Currency of the account</summary>
		public string Currency
		{
			get { return m_currency ?? string.Empty; }
			set { SetProp(ref m_currency, value, R<Broker>.Name(x => x.Currency)); }
		}
		private string m_currency;

		/// <summary>Account balance (in units of Currency)</summary>
		public double Balance
		{
			get { return m_balance; }
			set { SetProp(ref m_balance, value, R<Broker>.Name(x => x.Balance)); }
		}
		private double m_balance;

		/// <summary>Unrealised account balance</summary>
		public double Equity
		{
			get { return m_equity; }
			set { SetProp(ref m_equity, value, R<Broker>.Name(x => x.Equity)); }
		}
		private double m_equity;

		/// <summary>The account leverage</summary>
		public int Leverage
		{
			get { return m_leverage; }
			set { SetProp(ref m_leverage, value, R<Broker>.Name(x => x.Leverage)); }
		}
		private int m_leverage;

		/// <summary>The total risk due to active and pending orders</summary>
		public double TotalRisk
		{
			get { return CurrentRisk + PendingRisk; }
		}

		/// <summary>The risk due to open positions</summary>
		public double CurrentRisk
		{
			get { return m_position_risk; }
			set { SetProp(ref m_position_risk, value, R<Broker>.Name(x => x.CurrentRisk), R<Broker>.Name(x => x.TotalRisk)); }
		}
		private double m_position_risk;

		/// <summary>The risk due to pending orders</summary>
		public double PendingRisk
		{
			get { return m_pending_risk; }
			set { SetProp(ref m_pending_risk, value, R<Broker>.Name(x => x.PendingRisk), R<Broker>.Name(x => x.TotalRisk)); }
		}
		private double m_pending_risk;

		/// <summary>The total risk due to active and pending orders (as a percent of balance)</summary>
		public double TotalRiskPC
		{
			get { return 100.0 * TotalRisk / Balance; }
		}

		/// <summary>The risk due to open positions (as a percent of balance)</summary>
		public double CurrentRiskPC
		{
			get { return 100.0 * CurrentRisk / Balance; }
		}

		/// <summary>The risk due to pending orders (as a percent of balance)</summary>
		public double PendingRiskPC
		{
			get { return 100.0 * PendingRisk / Balance; }
		}

	}
}

	
			///// <summary>The free margin of the current account.</summary>
			//public double FreeMargin { get; set; }


			///// <summary>Represents the margin of the current account.</summary>
			//public double Margin { get; set; }

			///// <summary>
			///// Represents the margin level of the current account.
			///// Margin level (in %) is calculated using this formula: Equity / Margin * 100.</summary>
			//public double? MarginLevel { get; set; }

			///// <summary>Unrealised gross profit</summary>
			//public double UnrealizedGrossProfit { get; set; }

			///// <summary>Unrealised net profit</summary>
			//public double UnrealizedNetProfit { get; set; }
