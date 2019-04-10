using System;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API
{
	/// <summary>Chart candle data</summary>
	public class Candle :IComparable<Candle>
	{
		public Candle()
		{ }
		public Candle(JArray data)
		{
			if (data.Count < 6) throw new Exception("Insufficient data for a candle");
			Timestamp = Misc.ToDateTimeOffset(data[0].Value<ulong>() / 1000);
			Open      = data[1].Value<double>();
			Close     = data[2].Value<double>();
			High      = data[3].Value<double>();
			Low       = data[4].Value<double>();
			Volume    = data[5].Value<double>();
		}

		public DateTimeOffset Timestamp { get; private set; }
		public double Open              { get; private set; }
		public double High              { get; private set; }
		public double Low               { get; private set; }
		public double Close             { get; private set; }
		public double Volume            { get; private set; }

		public int CompareTo(Candle other)
		{
			return Timestamp.CompareTo(other.Timestamp);
		}

		#region Equals
		public bool Equals(Candle rhs)
		{
			return
				rhs       != null &&
				Timestamp == rhs.Timestamp &&
				Open      == rhs.Open      &&
				High      == rhs.High      &&
				Low       == rhs.Low       &&
				Close     == rhs.Close     &&
				Volume    == rhs.Volume    ;
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as Candle);
		}
		public override int GetHashCode()
		{
			return new { Timestamp, Open, High, Low, Close, Volume }.GetHashCode();
		}
		#endregion

	}
}
