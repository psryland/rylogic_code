using System;
using System.ComponentModel;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.util;

namespace Tradee
{
	public class WickFollow :IndicatorBase
	{
		// Notes:
		// - Identifies sequences of candles where lows or highs are continuously
		//   higher or lower.
		// - Tracks lows and highs independently

		public WickFollow()
			:base(Guid.NewGuid(), "Wick Follow", new WickFollowSettings())
		{
			HighSeqLength =  0;
			HighRising    = false;
			LowSeqLength  = 0;
			LowRising     = false;
		}
		public WickFollow(XElement node)
			:base(node)
		{}

		/// <summary>The number of candles before the latest where the high has been consistently increasing or decreasing.</summary>
		public int HighSeqLength { get; private set; }

		/// <summary>True if the candle highs are rising, false if falling</summary>
		public bool HighRising { get; private set; }

		/// <summary>The number of candles before the latest where the low has been consistently increasing or decreasing.</summary>
		public int LowSeqLength { get; private set; }

		/// <summary>True if the candle lows are rising, false if falling</summary>
		public bool LowRising { get; private set; }

		/// <summary>Raised when either 'RisingLength' or 'FallingLength' changed</summary>
		public event EventHandler FollowLengthChanged;
		private void OnFollowLengthChanged()
		{
			FollowLengthChanged.Raise(this);
		}

		/// <summary>Find the length of the sequence of continuously rising/falling</summary>
		private void UpdateFollowLength()
		{
			var hi_falling = 0;
			var hi_rising = 0;
			var lo_falling = 0;
			var lo_rising = 0;

			// Find the rising/falling sequence lengths
			for (int i = Instrument.LastIdx - 2; i >= Instrument.FirstIdx && Instrument[i+0].High <= Instrument[i+1].High; --i, ++hi_rising ) {}
			for (int i = Instrument.LastIdx - 2; i >= Instrument.FirstIdx && Instrument[i+0].High >= Instrument[i+1].High; --i, ++hi_falling) {}
			for (int i = Instrument.LastIdx - 2; i >= Instrument.FirstIdx && Instrument[i+0].Low  <= Instrument[i+1].Low;  --i, ++lo_rising ) {}
			for (int i = Instrument.LastIdx - 2; i >= Instrument.FirstIdx && Instrument[i+0].Low  >= Instrument[i+1].Low;  --i, ++lo_falling) {}

			// These values can only be equal if the data is flat all the way back to 'FirstIdx'
			HighSeqLength = Math.Max(hi_rising, hi_falling);
			LowSeqLength  = Math.Max(lo_rising, lo_falling);
			HighRising = hi_rising > hi_falling;
			LowRising  = lo_rising > lo_falling;

			// Notify
			OnFollowLengthChanged();
		}

		/// <summary>Handle new instrument data</summary>
		protected override void HandleInstrumentDataChanged(object sender, DataEventArgs e)
		{
			UpdateFollowLength();
			base.HandleInstrumentDataChanged(sender, e);
		}
	}

	#region Settings

	[TypeConverter(typeof(TyConv))]
	public class WickFollowSettings :SettingsXml<WickFollowSettings>
	{
		public WickFollowSettings()
		{ }

		private class TyConv :GenericTypeConverter<WickFollowSettings> {}
	}

	#endregion
}
