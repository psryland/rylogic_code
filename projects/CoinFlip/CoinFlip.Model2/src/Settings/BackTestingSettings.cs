using System;
using System.ComponentModel;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Utility;

namespace CoinFlip.Settings
{
	/// <summary>Settings related to back testing</summary>
	[Serializable]
	[TypeConverter(typeof(TyConv))]
	public class BackTestingSettings : SettingsXml<BackTestingSettings>
	{
		public BackTestingSettings()
		{
			TimeFrame = ETimeFrame.Day1;
			MaxSteps = 30_000;
			Steps = 0;
			StepsPerCandle = 10.0;
			SpreadFrac = 0.001;
			OrderValueRange = new RangeF(0.1, 1000.0);
			OrdersPerBook = 50;
		}
		public BackTestingSettings(XElement node)
			: base(node)
		{ }

		/// <summary>The time frame to test with</summary>
		public ETimeFrame TimeFrame
		{
			get { return get<ETimeFrame>(nameof(TimeFrame)); }
			set { set(nameof(TimeFrame), value); }
		}

		/// <summary>The number of time frame steps backwards in time to start from</summary>
		public int Steps
		{
			get { return get<int>(nameof(Steps)); }
			set { set(nameof(Steps), value); }
		}

		/// <summary>The maximum number of steps backwards in time to start back testing from</summary>
		public int MaxSteps
		{
			get { return get<int>(nameof(MaxSteps)); }
			set { set(nameof(MaxSteps), value); }
		}

		/// <summary>How fast the simulation runs through the back testing candle data (in candles/second)</summary>
		public double StepsPerCandle
		{
			get { return get<double>(nameof(StepsPerCandle)); }
			set { set(nameof(StepsPerCandle), value); }
		}

		/// <summary>The price spread to use when back testing (as a fraction)</summary>
		public double SpreadFrac
		{
			get { return get<double>(nameof(SpreadFrac)); }
			set { set(nameof(SpreadFrac), value); }
		}
		public double SpreadPC
		{
			get { return SpreadFrac * 100.0; }
			set { SpreadFrac = value * 0.01; }
		}

		/// <summary>The value in USD of generated orders</summary>
		public RangeF OrderValueRange
		{
			get { return get<RangeF>(nameof(OrderValueRange)); }
			set { set(nameof(OrderValueRange), value); }
		}

		/// <summary>The number of offers on each side of the order book for pairs</summary>
		public int OrdersPerBook
		{
			get { return get<int>(nameof(OrdersPerBook)); }
			set { set(nameof(OrdersPerBook), value); }
		}

		private class TyConv : GenericTypeConverter<BackTestingSettings> { }
	}
}
