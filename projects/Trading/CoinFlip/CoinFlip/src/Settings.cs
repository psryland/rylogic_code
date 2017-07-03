using System;
using System.ComponentModel;
using pr.common;
using pr.util;

namespace CoinFlip
{
	[Serializable]
	public sealed class Settings :SettingsBase<Settings>
	{
		public Settings()
		{
			CoinsOfInterest           = new[]{"BTC", "ETH", "LTC"};
			MaximumLoopCount          = 5;
			FindProfitableLoopsPeriod = 500;
			Cryptopia                 = new CrypotopiaSettings();
			Poloniex                  = new PoloniexSettings();

			AutoSaveOnChanges = true;
		}
		public Settings(string filepath)
			:base(filepath)
		{
			AutoSaveOnChanges = true;
		}

		/// <summary>The coins to trade</summary>
		public string[] CoinsOfInterest
		{
			get { return get(x => x.CoinsOfInterest); }
			set { set(x => x.CoinsOfInterest, value); }
		}

		/// <summary>The maximum number of hops in a loop</summary>
		public int MaximumLoopCount
		{
			get { return get(x => x.MaximumLoopCount); }
			set { set(x => x.MaximumLoopCount, value); }
		}

		/// <summary>The period at which searches for profitable loops occur</summary>
		public int FindProfitableLoopsPeriod
		{
			get { return get(x => x.FindProfitableLoopsPeriod); }
			set { set(x => x.FindProfitableLoopsPeriod, value); }
		}

		/// <summary>Cryptopia exchange settings</summary>
		public CrypotopiaSettings Cryptopia
		{
			get { return get(x => x.Cryptopia); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(x => x.Cryptopia, value);
			}
		}

		/// <summary>Cryptopia exchange settings</summary>
		public PoloniexSettings Poloniex
		{
			get { return get(x => x.Poloniex); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(x => x.Poloniex, value);
			}
		}

		/// <summary>Settings associated with the Repo connection</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class CrypotopiaSettings :SettingsSet<CrypotopiaSettings>
		{
			public CrypotopiaSettings()
			{
				PollPeriod = 500;
			}

			/// <summary>Data polling rate (in ms)</summary>
			public int PollPeriod
			{
				get { return get(x => x.PollPeriod); }
				set { set(x => x.PollPeriod, value); }
			}

			private class TyConv :GenericTypeConverter<CrypotopiaSettings> {}
		}

		/// <summary>Settings associated with the Repo connection</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class PoloniexSettings :SettingsSet<PoloniexSettings>
		{
			public PoloniexSettings()
			{
				PollPeriod = 1000;
			}

			/// <summary>Data polling rate (in ms)</summary>
			public int PollPeriod
			{
				get { return get(x => x.PollPeriod); }
				set { set(x => x.PollPeriod, value); }
			}

			private class TyConv :GenericTypeConverter<PoloniexSettings> {}
		}
	}
}
