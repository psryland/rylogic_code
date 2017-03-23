using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	/// <summary>Types of patterns found using 'PricePeaks'</summary>
	public enum EPeakPattern
	{
		BreakOutHigh,
		BreakOutLow,
		HighReversal,
		LowReversal,
	}

	/// <summary>Detects the highs and lows of the price</summary>
	[DebuggerDisplay("[{Beg},{End}) strength={Strength}  hh={HigherHighs} hl={HigherLows} lh={LowerHighs} ll={LowerLows}")]
	public class PricePeaks
	{
		// Notes:
		//  - price is trending up if the lows are getting higher
		//  - price is trending down if the highs are getting lower
		//  - trend is unknown otherwise
		//  - Break outs can be detected by comparing two sets of price peaks

		/// <summary>Find the highs and lows of the price</summary>
		/// <param name="instr">The instrument to find peaks in</param>
		/// <param name="iend">The last candle, i.e. look backwards from here</param>
		public PricePeaks(Instrument instr, Idx iend, int window_size = 5)
		{
			Instrument   = instr;
			WindowSize   = window_size;
			ConfirmTrend = 0.5;
			Beg          = iend;
			End          = iend;
			Highs        = new List<Peak>();
			Lows         = new List<Peak>();

			#region Find peaks
			{
				var threshold = ConfirmTrend * Instrument.MCS;
				var corr_hi = new Correlation();
				var corr_lo = new Correlation();

				// The last high/low encountered
				var hi = (Peak)null;
				var lo = (Peak)null;

				var done_hi = false;
				var done_lo = false;

				// Iterate through the peaks
				foreach (var pk in FindPeaks(iend))
				{
					// Save the first peak as it might be a break out
					if (FirstPeak == null)
					{
						FirstPeak = pk;
						continue;
					}

					var last  = pk.High ? hi        : lo;
					var peaks = pk.High ? Highs     : Lows;
					var corr  = pk.High ? corr_hi   : corr_lo;
					var trend = pk.High ? TrendHigh : TrendLow;
					var done  = pk.High ? done_hi   : done_lo;

					// First peak encountered?
					if (last == null)
					{
						// Just save the peak
						corr.Add((double)pk.Index, (double)pk.Price);
						peaks.Add(pk);
					}
					// The trend has not been broken
					else if (!done)
					{
						// Second peak encountered
						if (trend == null)
						{
							// Form a trend line between the peaks
							if (pk.High) TrendHigh = Monic.FromPoints((double)pk.Index, (double)pk.Price, (double)last.Index, (double)last.Price);
							else         TrendLow  = Monic.FromPoints((double)pk.Index, (double)pk.Price, (double)last.Index, (double)last.Price);
							corr.Add((double)pk.Index, (double)pk.Price);
							peaks.Add(pk);
						}
						// 3+ peak encountered, confirm trend strength
						else
						{
							// Get the predicted value from the trend line
							var p = trend.F((double)pk.Index);
							if (Math.Abs(p - pk.Price) < threshold)
							{
								// If within tolerance, the trend is confirmed
								corr.Add((double)pk.Index, (double)pk.Price);
								if (pk.High) TrendHigh = corr.LinearRegression;
								else         TrendLow  = corr.LinearRegression;
								peaks.Add(pk);
							}
							else
							{
								if (pk.High) done_hi = true;
								else         done_lo = true;

								// Otherwise, if the trend does not have 3 points, it is rejected
								if (peaks.Count < 3)
								{
									if (pk.High) TrendHigh = null;
									else         TrendLow  = null;
								}
							}
						}
					}

					// Save the peak as last
					if (pk.High) hi = pk;
					else         lo = pk;

					// If the high and low trends are done, break the loop
					if (done_hi && done_lo)
						break;
				}
			}
			#endregion
		}

		/// <summary>The window size to use for peak detection</summary>
		public int WindowSize { get; set; }

		/// <summary>The cut-off for being considered 'near' the trend line (in units of MCS)</summary>
		public double ConfirmTrend { get; set; }

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

		/// <summary>The index of the first candle considered</summary>
		public Idx Beg { get; private set; }

		/// <summary>The index of the end candle considered</summary>
		public Idx End { get; private set; }

		/// <summary>The first peak encountered (possible break out)</summary>
		public Peak FirstPeak { get; private set; }

		/// <summary>The peaks that contribute to the trend</summary>
		public List<Peak> Highs { get; private set; }
		public List<Peak> Lows { get; private set; }

		/// <summary>Return all peaks in order of increasing age</summary>
		public IEnumerable<Peak> Peaks
		{
			get
			{
				int h = 0, hend = Highs.Count;
				int l = 0, lend = Lows.Count;

				for (; h != hend && l != lend;)
					yield return Highs[h].Index > Lows[l].Index ? Highs[h++] : Lows[l++];
				for (; h != hend;)
					yield return Highs[h++];
				for (; l != lend;)
					yield return Lows[l++];
			}
		}

		/// <summary>The total number of peaks contributing to the peak data</summary>
		public int PeakCount
		{
			get { return Highs.Count + Lows.Count; }
		}

		/// <summary>The upper trend line (or null)</summary>
		public Monic TrendHigh { get; private set; }

		/// <summary>The lower trend line (or null)</summary>
		public Monic TrendLow { get; private set; }

		/// <summary>How strong the upper trend line is (in the range [0,+1))</summary>
		public double TrendHighStrength
		{
			get { return TrendStrength(TrendHigh, Highs, true); }
		}

		/// <summary>How strong the lower trend line is (in the range [0,+1))</summary>
		public double TrendLowStrength
		{
			get { return TrendStrength(TrendLow, Lows, false); }
		}

		/// <summary>Determine a measure of trend strength</summary>
		private double TrendStrength(Monic trend, List<Peak> peaks, bool high)
		{
			if (trend == null)
				return 0.0;

			// Trend strength has to be a measure of how often price approached the trend line
			// and bounced off. Candles miles away from the trend line don't count, only consider
			// candles that span or are within a tolerance range of the trend line.
			var above = 0.0; var below = 0.0; var count = 0;
			var threshold = ConfirmTrend * Instrument.MCS;
			foreach (var c in Instrument.CandleRange(peaks.Back().Index, Instrument.IdxLast - WindowSize))
			{
				var p = trend.F((double)(c.Index + Instrument.IdxFirst));
				if (c.High < p - threshold) continue;
				if (c.Low  > p + threshold) continue;
				above += Math.Max(0, c.High - p);
				below += Math.Max(0, p - c.Low);
				++count;
			}

			// There must be some candles contributing
			var total = above + below;
			if (total == 0)
				return 0.0;

			// Return the proportion of above to below
			var strength = (high ? +1 : -1) * (below - above) / total;

			// Weight the strength based on the number of candles that contribute
			var weighted_count = Maths.Sigmoid(count, 6);

			return strength * weighted_count;
		}

		/// <summary>A value in the range [0,+1) indicating trend strength</summary>
		public double Strength
		{
			get { return Math.Max(TrendHighStrength, TrendLowStrength); }
		}

		/// <summary>Return the direction of the peak trend</summary>
		public int Sign
		{
			get
			{
				var slope =
					TrendHighStrength > TrendLowStrength  ? TrendHigh.A :
					TrendLowStrength  > TrendHighStrength ? TrendLow.A  :
					0.0;

				var trend = Instrument.MeasureTrendFromSlope(slope);
				return Math.Abs(trend) >= 0.5 ? Math.Sign(trend) : 0;
			}
		}

		/// <summary>The minimum distance between any two adjacent high/low peaks. Useful when 'Strength' is near zero</summary>
		public QuoteCurrency PeakGap
		{
			get
			{
				var last = (Peak)null;
				var dist = (QuoteCurrency)double.MaxValue;
				foreach (var pk in Peaks)
				{
					if (last == null)
						last = pk;
					else if (last.High != pk.High)
						dist = Math.Min(dist, Math.Abs(pk.Price - last.Price));
				}
				return dist;
			}
		}

		/// <summary>True if a break-out is detected</summary>
		public bool IsBreakOut
		{
			get { return IsBreakOutHigh || IsBreakOutLow; }
		}

		/// <summary>True if a break-out to the long side is detected</summary>
		public bool IsBreakOutHigh
		{
			get { return IsBreakOutInternal(TrendHigh, Highs, true); }
		}

		/// <summary>True if a break-out to the short side is detected</summary>
		public bool IsBreakOutLow
		{
			get { return IsBreakOutInternal(TrendLow, Lows, false); }
		}

		/// <summary>True if a break-out to the 'high' side is detected</summary>
		private bool IsBreakOutInternal(Monic trend, List<Peak> peaks, bool high)
		{
			// A break-out is when the latest candle is significantly above the upper trend line
			// or below the lower trend line and showing signs of going further. Also, the preceding candles
			// must be below the trend line.

			// No trend, no break-out
			if (trend == null)
				return false;

			// The latest candle must be in the break-out direction
			var sign = high ? +1 : -1;
			var latest = Instrument.Latest;
			if (latest.Sign != sign)
				return false;

			// The price must be beyond the trend by a significant amount
			var price_threshold = trend.F(0.0) + sign * Instrument.MCS;
			if (Math.Sign(latest.Close - price_threshold) != sign)
				return false;

			// Only the latest few candles can be beyond the trend line
			// and all must be in the direction of the break out.
			if (peaks[0].Index < -2)
			{
				// Allow the last two candles to be part of the break out
				foreach (var c in Instrument.CandleRange(peaks[0].Index, -2))
				{
					// If more than half the candle is beyond the trend line, not a breakout
					var ratio = sign * Instrument.Compare(c, trend, false);
					if (ratio > 0)
						return false;
				}
			}

			return true;
		}

		/// <summary>Returns the price peaks using a window with size 'window_size'</summary>
		public IEnumerable<Peak> FindPeaks(Idx iend)
		{
			// Create window buffers for the high/low prices
			var price_hi = new QuoteCurrency[WindowSize];
			var price_lo = new QuoteCurrency[WindowSize];
			for (int i = 0; i != WindowSize; ++i)
			{
				price_hi[i] = -double.MaxValue;
				price_lo[i] = +double.MaxValue;
			}

			// Look for peaks
			int d = 0, hh = -1, ll = -1, hcount = 0, lcount = 0;
			for (Idx i = iend; i-- != Instrument.IdxFirst; d = (d+1) % WindowSize)
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
					if (++hcount == WindowSize)
					{
						// Skip index == 0 because it's not a complete candle
						var idx = i + (d - hh + WindowSize) % WindowSize;
						if (Instrument.IdxLast - idx > 1) yield return new Peak(idx, price_hi[hh], true);
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
					if (++lcount == WindowSize)
					{
						// Skip index == 0 because it's not a complete candle
						var idx = i + (d - ll + WindowSize) % WindowSize;
						if (Instrument.IdxLast - idx > 1) yield return new Peak(idx, price_lo[ll], false);
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
	}

	/// <summary>A single price peak</summary>
	[DebuggerDisplay("{Index} peak={Price} high={High}")]
	public class Peak
	{
		public Peak(Idx index, QuoteCurrency price, bool high)
		{
			Index = index;
			Price = price;
			High = high;
		}

		/// <summary>The candle index of the peak</summary>
		public Idx Index { get; private set; }

		/// <summary>The price at the peak</summary>
		public QuoteCurrency Price { get; private set; }

		/// <summary>True if the peak is a high, false if it's a low</summary>
		public bool High { get; private set; }
	}
}

#if false

		/// <summary>A value in the range [-1,+1] indicating trend strength</summary>
		public double Strength
		{
			get
			{
				// Combine the peak counts into a trend strength measure.
				// A sequence of 1 is no trend (0), 2 is a moderate trend (0.5), 3 is strong trend (0.75), 4+ is awesome trend (1.0)
				// Scale each count onto the range [-1,+1]
				var hh = HigherHighs != 0 ? +1.0 - 1.0/HigherHighs : 0.0;
				var hl = HigherLows  != 0 ? +1.0 - 1.0/HigherLows  : 0.0;
				var lh = LowerHighs  != 0 ? +1.0 - 1.0/LowerHighs  : 0.0;
				var ll = LowerLows   != 0 ? +1.0 - 1.0/LowerLows   : 0.0;

				// Return the weighted average
				// 'hl' and 'lh' have more significance than 'hh' and 'll'.
				const double a = 1.0, b = 1.5;
				return (a*hh + b*hl - b*lh - a*ll) / (a+b);
			}
		}
		/// <summary>A polynomial curve fitted to the high or low peaks</summary>
		public IPolynomial TrendLine(bool high)
		{
			// A trend line is valid when the latest two peaks form a line that
			// the third latest peak confirms.

			// Create a line from the last two peaks
			var peaks = Peaks.Where(x => x.High == high).ToArray();
			if (peaks.Length < 3) return null;
			var line = Monic.FromPoints(
				(double)peaks[1].Index, (double)peaks[1].Price,
				(double)peaks[0].Index, (double)peaks[0].Price);

			// Look for the third peak to confirm the trend line
			var p3 = line.F((double)peaks[2].Index);
			var ConfirmThreshold = Instrument.MedianCS_50 * 0.5;
			if (Misc.Abs(peaks[2].Price - p3) > ConfirmThreshold)
				return null;

			return line;

			//// Perform a linear regression on the peaks
			//var corr = new Correlation();
			//foreach (var hi in Peaks.Where(x => x.High))
			//	corr.Add((double)hi.Index, (double)hi.Price);
			//return corr.Count >= 2 ? corr.LinearRegression : new Monic(0,0);
		}

		/// <summary>Return a measure of how much the price obeys the trend line</summary>
		public double TrendLineStrength(bool high)
		{
			var line = TrendLine(high);
			if (line == null)
				return 0.0;

			var sign = high ? +1 : -1;
			var peaks = Peaks.Where(x => x.High == high).ToArray();
			Debug.Assert(peaks.Length >= 3);

			// Compare the ratio of max price on each side of the line
			var right = 0.0;
			var wrong = 0.0;
			foreach (var c in Instrument.CandleRange(peaks[2].Index, Instrument.IdxLast))
			{
				// If the candle is 'above' the trend line then the trend line is less good
				var diff = sign * (c.WickLimit(sign) - line.F((double)(c.Index + Instrument.IdxFirst)));
				if (diff > 0) wrong = Math.Max(+diff, wrong);
				if (diff < 0) right = Math.Max(-diff, right);
			}

			if (right == 0) return 0.0;
			if (wrong == 0) return 1.0;
			var ratio = (right - wrong) / (right + wrong);

			// Scale the threshold value to 0.5.
			const double threshold = 0.5;
			return Maths.Sigmoid(ratio, threshold);
		}
#endif

// Get the last two high peaks and last two low peaks

// 


/*

// The last high/low encountered
var hi = (Peak)null;
var lo = (Peak)null;

// The number of sequential higher lows, etc
var hh_done = false; var lh_done = false;
var hl_done = false; var ll_done = false;

// Scan through historic peaks (*remember* scanning backwards in time!)
foreach (var pk in FindPeaks(iend))
{
	// All trends finished?
	if (hh_done && hl_done && lh_done && ll_done)
		break;

	// Record the last peak used
	if (End == iend)
		End = pk.Index;

	// Found a high peak
	if (pk.High)
	{
		if (hi == null)
		{
			Peaks.Add(pk);
		}
		else
		{
			// A higher high?
			if (hi.Price > pk.Price)
			{
				if (!hh_done)
				{
					++HigherHighs;
					Beg = pk.Index;
					Peaks.Add(pk);
				}
				if (hi.Price > pk.Price)
				{
					lh_done = true;
					ll_done = true;
				}
			}
			// A lower high?
			else if (hi.Price < pk.Price)
			{
				if (!lh_done)
				{
					++LowerHighs;
					Beg = pk.Index;
					Peaks.Add(pk);
				}
				if (hi.Price < pk.Price)
				{
					hh_done = true;
					hl_done = true;
				}
			}
		}
		hi = pk;
	}
	else // pk.Low
	{
		// The first low encountered
		if (lo == null)
		{
			Peaks.Add(pk);
		}
		else
		{
			// A lower low?
			if (lo.Price < pk.Price)
			{
				if (!ll_done)
				{
					++LowerLows;
					Beg = pk.Index;
					Peaks.Add(pk);
				}
				if (lo.Price < pk.Price)
				{
					hl_done = true;
					hh_done = true;
				}
			}
			// A higher low?
			else if (lo.Price > pk.Price)
			{
				if (!hl_done)
				{
					++HigherLows;
					Beg = pk.Index;
					Peaks.Add(pk);
				}
				if (lo.Price > pk.Price)
				{
					ll_done = true;
					lh_done = true;
				}
			}
		}
		lo = pk;
	}
}
*/
