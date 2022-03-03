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
	public class BackTestingSettings : SettingsSet<BackTestingSettings>
	{
		public BackTestingSettings()
		{
			AccountBalances = new FundData();
			TestFunds = new FundData[1] { new FundData(Fund.Main.Id, new FundData.ExchData[0]) };
			TestBots = new BotData[0];
			TimeFrame = ETimeFrame.Day1;
			StartTime = (DateTimeOffset.UtcNow - TimeSpan.FromDays(365)).RoundDownTo(ETimeFrame.Day1);
			EndTime = StartTime + TimeSpan.FromDays(365);
			StepsPerCandle = 10;
			SpreadFrac = 0.001;
			OrderValueRange = new RangeF(0.1, 1000.0);
			OrdersPerBook = 500;
		}

		/// <summary>The balance of each currency on the simulation exchanges</summary>
		public FundData AccountBalances
		{
			get => get<FundData>(nameof(AccountBalances));
			set => set(nameof(AccountBalances), value);
		}

		/// <summary>The partitions of user funds used for back testing</summary>
		public FundData[] TestFunds
		{
			get => get<FundData[]>(nameof(TestFunds));
			set => set(nameof(TestFunds), value);
		}

		/// <summary>The bots created in the UI</summary>
		public BotData[] TestBots
		{
			get => get<BotData[]>(nameof(TestBots));
			set => set(nameof(TestBots), value);
		}

		/// <summary>The time frame to test with</summary>
		public ETimeFrame TimeFrame
		{
			get => get<ETimeFrame>(nameof(TimeFrame));
			set => set(nameof(TimeFrame), value);
		}

		/// <summary>The date and time to start back testing from</summary>
		public DateTimeOffset StartTime
		{
			get => get<DateTimeOffset>(nameof(StartTime));
			set => set(nameof(StartTime), value);
		}

		/// <summary>The date and time to end back testing at</summary>
		public DateTimeOffset EndTime
		{
			get => get<DateTimeOffset>(nameof(EndTime));
			set => set(nameof(EndTime), value);
		}

		/// <summary>Sub-candle simulated resolution</summary>
		public int StepsPerCandle
		{
			get => get<int>(nameof(StepsPerCandle));
			set => set(nameof(StepsPerCandle), value);
		}

		/// <summary>The price spread to use when back testing (as a fraction)</summary>
		public double SpreadFrac
		{
			get => get<double>(nameof(SpreadFrac));
			set => set(nameof(SpreadFrac), value);
		}
		public double SpreadPC
		{
			get => SpreadFrac * 100.0;
			set => SpreadFrac = value * 0.01;
		}

		/// <summary>The value in USD of generated orders</summary>
		public RangeF OrderValueRange
		{
			get => get<RangeF>(nameof(OrderValueRange));
			set => set(nameof(OrderValueRange), value);
		}

		/// <summary>The number of offers on each side of the order book for pairs</summary>
		public int OrdersPerBook
		{
			get => get<int>(nameof(OrdersPerBook));
			set => set(nameof(OrdersPerBook), value);
		}

		private class TyConv : GenericTypeConverter<BackTestingSettings> { }
	}
}
