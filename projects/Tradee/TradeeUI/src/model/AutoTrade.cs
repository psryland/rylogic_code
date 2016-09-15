using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pr.container;
using pr.extn;
using pr.maths;
using pr.util;

namespace Tradee
{
	/// <summary>Find and make trades</summary>
	public class AutoTrade :IDisposable
	{
		public AutoTrade(Instrument instr)
		{
			PatternRecogniser = new CandlePatterns();
			Instrument = instr;
		}
		public virtual void Dispose()
		{
			Instrument = null;
			PatternRecogniser = null;
		}

		/// <summary>The App logic</summary>
		public MainModel Model
		{
			[DebuggerStepThrough] get { return Instrument.Model; }
		}

		/// <summary>Orders created and managed by this class</summary>
		private BindingSource<IOrder> Orders { get; set; }

		/// <summary></summary>
		private Instrument Instrument
		{
			get { return m_instr; }
			set
			{
				if (m_instr == value) return;
				if (m_instr != null)
				{
					PatternRecogniser.Instrument = null;
				}
				m_instr = value;
				if (m_instr != null)
				{
					PatternRecogniser.Instrument = m_instr;
				}
			}
		}
		private Instrument m_instr;

		/// <summary>Wick follower</summary>
		private CandlePatterns PatternRecogniser
		{
			get { return m_candle_patterns; }
			set
			{
				if (m_candle_patterns == value) return;
				if (m_candle_patterns != null)
				{
					m_candle_patterns.TrendLengthChanged -= HandleFollowLengthChanged;
					Util.Dispose(ref m_candle_patterns);
				}
				m_candle_patterns = value;
				if (m_candle_patterns != null)
				{
					m_candle_patterns.TrendLengthChanged += HandleFollowLengthChanged;
				}
			}
		}
		private CandlePatterns m_candle_patterns;

		/// <summary>Handle the length of the wick follow changing</summary>
		private void HandleFollowLengthChanged(object sender, EventArgs args)
		{
			#if false
			// Test..
			if (PatternRecogniser.HighSeqLength >= 3)
			{
				// Trade type
				var tt = PatternRecogniser.HighRising ? ETradeType.Long : ETradeType.Short;

				// Enter at the current market price
				var ep = PatternRecogniser.HighRising ? Instrument.PriceData.AskPrice : Instrument.PriceData.BidPrice;

				// Set the stop loss to just past the lowest (rising) or highest (falling) of the last 3 candles
				var sl = PatternRecogniser.HighRising
					? Instrument.CandleRange(Instrument.LastIdx -  3, Instrument.LastIdx).Select(x => x.Low ).Concat(ep).Min()
					: Instrument.CandleRange(Instrument.LastIdx -  3, Instrument.LastIdx).Select(x => x.High).Concat(ep).Max();

				var order = Model.Positions.Orders.Add2(new Order(0, Instrument, tt, Trade.EState.Visualising));
				order.EntryPrice = ep;
				order.StopLossAbs = sl;
				order.Volume = Model.CalculateVolume(Instrument, order.StopLossRel);
				order.State = Trade.EState.ActivePosition;
				order.Commit();
			}
			#endif
		}
	}
}
