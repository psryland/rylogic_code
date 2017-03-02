using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using pr.extn;

namespace Rylobot
{
	/// <summary>Detects the highs and lows of the price</summary>
	public class PricePeaks
	{
		// Notes:
		//  - price is trending up if the lows are getting higher
		//  - price is trending down if the highs are getting lower
		//  - trend is unknown otherwise
		//  - notice higher highs and lower lows are not used


		/// <summary>Find the highs and lows of the price</summary>
		/// <param name="instr">The instrument to find peaks in</param>
		/// <param name="iend">The last candle, i.e. look backwards from here</param>
		/// <param name="count">The number of candles to scan</param>
		public PricePeaks(Instrument instr, NegIdx iend, int? count = null, int window_size = 5)
		{
			Instrument = instr;
			Count = Math.Min(count ?? instr.Bot.Settings.PeaksHistoryLength, instr.Count);
			End = iend + 1;
			Beg = End - Count;

			Peaks = FindPeaks(window_size).ToList();
		}

		/// <summary>The instrument on which the SnR levels have been calculated</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return m_instrument; }
			set
			{
				if (m_instrument == value) return;
				m_instrument = value;
			}
		}
		private Instrument m_instrument;

		/// <summary>The price peaks, in order of increasing age(i.e. latest is at 0)</summary>
		public List<Peak> Peaks { get; private set; }

		/// <summary>The index of the first candle considered</summary>
		public NegIdx Beg { get; private set; }

		/// <summary>The index of the end candle considered</summary>
		public NegIdx End { get; private set; }

		/// <summary>The number of candles that contributed</summary>
		public int Count { get; private set; }

		/// <summary>Returns the price peaks using the small window size</summary>
		public IEnumerable<Peak> FindPeaks(int window_size)
		{
			// Create window buffers for the high/low prices
			var price_hi = new QuoteCurrency[window_size];
			var price_lo = new QuoteCurrency[window_size];
			for (int i = 0; i != window_size; ++i)
			{
				price_hi[i] = -double.MaxValue;
				price_lo[i] = +double.MaxValue;
			}

			// Look for peaks
			int d = 0, hh = -1, ll = -1, hcount = 0, lcount = 0;
			for (NegIdx i = End, iend = Beg; i-- != iend; d = (d+1) % window_size)
			{
				var candle = Instrument[i];

				// Add the new price values
				price_hi[d] = candle.High;
				price_lo[d] = candle.Low;

				// Find the ring buffer index of the highest and lowest price
				var h = price_hi.IndexOfMaxBy(x => x);
				var l = price_lo.IndexOfMinBy(x => x);

				// If the high is the highest for the full window size, output it
				if (hh == h)
				{
					if (++hcount == window_size)
					{
						yield return new Peak(i + (d - hh + window_size) % window_size, price_hi[hh], true);
						hh = -1;
						hcount = 0;
					}
				}
				else
				{
					hh = h;
					hcount = 1;
				}

				// If the low is the lowest for the full window size, output it
				if (ll == l)
				{
					if (++lcount == window_size)
					{
						yield return new Peak(i + (d - ll + window_size) % window_size, price_lo[ll], false);
						ll = -1;
						lcount = 0;
					}
				}
				else
				{
					ll = l;
					lcount = 1;
				}
			}
		}

		/// <summary>Returns the trend strength as indicated by the peaks</summary>
		public double Trend
		{
			get
			{
				// The last high/low encountered
				var hi = (Peak)null;
				var lo = (Peak)null;

				// The number of sequential higher lows, etc
				var higher_highs = 0; var hh_done = false;
				var higher_lows  = 0; var hl_done = false; 
				var lower_highs  = 0; var lh_done = false; 
				var lower_lows   = 0; var ll_done = false;

				var tolerance = Instrument.PipSize * 10;

				// Scan through historic peaks (*remember* scanning backwards in time!)
				for (int i = 0; i != Peaks.Count && !(hh_done && hl_done && lh_done && ll_done); ++i)
				{
					var pk = Peaks[i];
					if (pk.High)
					{
						// The first high encountered
						if (hi != null)
						{
							// A higher high?
							if (hi.Price > pk.Price - tolerance)
							{
								if (!hh_done) ++higher_highs;
								lh_done |= hi.Price > pk.Price + tolerance;
							}
							// A lower high?
							if (hi.Price < pk.Price + tolerance)
							{
								if (!lh_done) ++lower_highs;
								hh_done |= hi.Price < pk.Price - tolerance;
							}
						}
						hi = pk;
					}
					else // pk.Low
					{
						// The first low encountered
						if (lo != null)
						{
							// A lower low?
							if (lo.Price < pk.Price + tolerance)
							{
								if (!ll_done) ++lower_lows;
								hl_done |= lo.Price < pk.Price - tolerance;
							}
							// A higher low?
							if (lo.Price > pk.Price - tolerance)
							{
								if (!hl_done) ++higher_lows;
								ll_done |= lo.Price > pk.Price + tolerance;
							}
						}
						lo = pk;
					}
				}

				// Combine the peak counts into a trend strength measure.
				// A sequence of 1 is no trend (0), 2 is a moderate trend (0.5), 3 is strong trend (0.75), 4+ is awesome trend (1.0)
				// Scale each count onto the range [-1,+1]
				var hh = higher_highs != 0 ? +1.0 - 1.0/higher_highs : 0.0;
				var hl = higher_lows  != 0 ? +1.0 - 1.0/higher_lows  : 0.0;
				var lh = lower_highs  != 0 ? -1.0 + 1.0/lower_highs  : 0.0;
				var ll = lower_lows   != 0 ? -1.0 + 1.0/lower_lows   : 0.0;

				// Return the weighted average
				// 'hl' and 'lh' have more significance than 'hh' and 'll'.
				const double a = 1.0, b = 1.5, c = 1.5, d = 1.0;
				return (a*hh + b*hl + c*lh + d*ll) / (a+b+c+d);
			}
		}
	}

	/// <summary>A single price peak</summary>
	[DebuggerDisplay("{Index} peak={Price} high={High}")]
	public class Peak
	{
		public Peak(NegIdx index, QuoteCurrency price, bool high)
		{
			Index = index;
			Price = price;
			High = high;
		}

		/// <summary>The candle index of the peak</summary>
		public NegIdx Index { get; private set; }

		/// <summary>The price at the peak</summary>
		public QuoteCurrency Price { get; private set; }

		/// <summary>True if the peak is a high, false if it's a low</summary>
		public bool High { get; private set; }
	}
}
