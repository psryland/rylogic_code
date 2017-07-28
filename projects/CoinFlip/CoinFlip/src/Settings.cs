using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	[Serializable]
	public sealed class Settings :SettingsBase<Settings>
	{
		public Settings()
		{
			MaximumLoopCount  = 5;
			MainLoopPeriod    = 500;
			Coins             = new CoinData[0];
			Fishing           = new FishingData[0];
			UI                = new UISettings();
			Cryptopia         = new CrypotopiaSettings();
			Poloniex          = new PoloniexSettings();
			Bittrex           = new BittrexSettings();
			CrossExchange     = new CrossExchangeSettings();
			AutoSaveOnChanges = true;
		}
		public Settings(string filepath)
			:base(filepath)
		{
			AutoSaveOnChanges = true;
		}

		/// <summary>The maximum number of hops in a loop</summary>
		public int MaximumLoopCount
		{
			get { return get(x => x.MaximumLoopCount); }
			set { set(x => x.MaximumLoopCount, value); }
		}

		/// <summary>The period at which searches for profitable loops occur</summary>
		public int MainLoopPeriod
		{
			get { return get(x => x.MainLoopPeriod); }
			set { set(x => x.MainLoopPeriod, value); }
		}

		/// <summary>Meta data for known coins</summary>
		public CoinData[] Coins
		{
			get { return get(x => x.Coins); }
			set { set(x => x.Coins, value); }
		}

		/// <summary>The fishing instances</summary>
		public FishingData[] Fishing
		{
			get { return get(x => x.Fishing); }
			set { set(x => x.Fishing, value); }
		}

		/// <summary>UI settings</summary>
		public UISettings UI
		{
			get { return get(x => x.UI); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(x => x.UI, value);
			}
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

		/// <summary>Bittrex exchange settings</summary>
		public BittrexSettings Bittrex
		{
			get { return get(x => x.Bittrex); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(x => x.Bittrex, value);
			}
		}

		/// <summary>CrossExchange exchange settings</summary>
		public CrossExchangeSettings CrossExchange
		{
			get { return get(x => x.CrossExchange); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(x => x.CrossExchange, value);
			}
		}

		/// <summary>Settings associated with a connection to Rex via RexLink</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class UISettings :SettingsSet<UISettings>
		{
			public UISettings()
			{
				UILayout        = null;
				WindowPosition  = Rectangle.Empty;
				WindowMaximised = false;
			}

			/// <summary>The dock panel layout</summary>
			public XElement UILayout
			{
				get { return get(x => x.UILayout); }
				set { set(x => x.UILayout, value); }
			}

			/// <summary>The last position on screen</summary>
			public Rectangle WindowPosition
			{
				get { return get(x => x.WindowPosition); }
				set { set(x => x.WindowPosition, value); }
			}
			public bool WindowMaximised
			{
				get { return get(x => x.WindowMaximised); }
				set { set(x => x.WindowMaximised, value); }
			}

			private class TyConv :GenericTypeConverter<UISettings> {}
		}

		/// <summary>Settings associated with the Repo connection</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class CrypotopiaSettings :SettingsSet<CrypotopiaSettings> ,IExchangeSettings
		{
			public CrypotopiaSettings()
			{
				Active = true;
				PollPeriod = 500;
				TransactionFee = 0.002m;
			}

			/// <summary>True if the exchange is active</summary>
			public bool Active
			{
				get { return get(x => x.Active); }
				set { set(x => x.Active, value); }
			}

			/// <summary>Data polling rate (in ms)</summary>
			public int PollPeriod
			{
				get { return get(x => x.PollPeriod); }
				set { set(x => x.PollPeriod, value); }
			}

			/// <summary>The fee charged per trade</summary>
			public decimal TransactionFee
			{
				get { return get(x => x.TransactionFee); }
				set { set(x => x.TransactionFee, value); }
			}

			private class TyConv :GenericTypeConverter<CrypotopiaSettings> {}
		}

		/// <summary>Settings associated with the Repo connection</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class PoloniexSettings :SettingsSet<PoloniexSettings> ,IExchangeSettings
		{
			public PoloniexSettings()
			{
				Active = true;
				PollPeriod = 500;
				TransactionFee = 0.0025m;
			}

			/// <summary>True if the exchange is active</summary>
			public bool Active
			{
				get { return get(x => x.Active); }
				set { set(x => x.Active, value); }
			}

			/// <summary>Data polling rate (in ms)</summary>
			public int PollPeriod
			{
				get { return get(x => x.PollPeriod); }
				set { set(x => x.PollPeriod, value); }
			}

			/// <summary>The fee charged per trade</summary>
			public decimal TransactionFee
			{
				get { return get(x => x.TransactionFee); }
				set { set(x => x.TransactionFee, value); }
			}

			private class TyConv :GenericTypeConverter<PoloniexSettings> {}
		}

		/// <summary>Settings associated with the Repo connection</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class BittrexSettings :SettingsSet<BittrexSettings> ,IExchangeSettings
		{
			public BittrexSettings()
			{
				Active = true;
				PollPeriod = 500;
				TransactionFee = 0.0025m;
			}

			/// <summary>True if the exchange is active</summary>
			public bool Active
			{
				get { return get(x => x.Active); }
				set { set(x => x.Active, value); }
			}

			/// <summary>Data polling rate (in ms)</summary>
			public int PollPeriod
			{
				get { return get(x => x.PollPeriod); }
				set { set(x => x.PollPeriod, value); }
			}

			/// <summary>The fee charged per trade</summary>
			public decimal TransactionFee
			{
				get { return get(x => x.TransactionFee); }
				set { set(x => x.TransactionFee, value); }
			}

			private class TyConv :GenericTypeConverter<BittrexSettings> {}
		}

		/// <summary>Settings associated with the Repo connection</summary>
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class CrossExchangeSettings :SettingsSet<CrossExchangeSettings> ,IExchangeSettings
		{
			public CrossExchangeSettings()
			{
				Active = true;
				PollPeriod = 500;
				TransactionFee = 0m;
			}

			/// <summary>True if the exchange is active</summary>
			public bool Active
			{
				get { return get(x => x.Active); }
				set { set(x => x.Active, value); }
			}

			/// <summary>Data polling rate (in ms)</summary>
			public int PollPeriod
			{
				get { return get(x => x.PollPeriod); }
				set { set(x => x.PollPeriod, value); }
			}

			/// <summary>The fee charged per trade</summary>
			public decimal TransactionFee
			{
				get { return get(x => x.TransactionFee); }
				set { set(x => x.TransactionFee, value); }
			}

			private class TyConv :GenericTypeConverter<CrossExchangeSettings> {}
		}

		/// <summary>Data needed to save a fishing instance in the settings</summary>
		[Serializable]
		[DebuggerDisplay("{Pair} {Exch0} {Exch1}")]
		public class FishingData
		{
			public FishingData(string pair, string exch0, string exch1, decimal scale, decimal volume_limit_base, decimal volume_limit_quote, RangeF price_offset)
			{
				Pair         = pair;
				Exch0        = exch0;
				Exch1        = exch1;
				Scale        = scale;
				VolumeLimitB = volume_limit_base;
				VolumeLimitQ = volume_limit_quote;
				PriceOffset  = price_offset;
			}
			public FishingData(FishingData rhs)
			{
				Pair         = rhs.Pair;
				Exch0        = rhs.Exch0;
				Exch1        = rhs.Exch1;
				Scale        = rhs.Scale;
				VolumeLimitB = rhs.VolumeLimitB;
				VolumeLimitQ = rhs.VolumeLimitQ;
				PriceOffset  = rhs.PriceOffset;
			}
			public FishingData(XElement node)
			{
				Pair         = node.Element(nameof(Pair )).As(Pair );
				Exch0        = node.Element(nameof(Exch0)).As(Exch0);
				Exch1        = node.Element(nameof(Exch1)).As(Exch1);
				Scale        = node.Element(nameof(Scale)).As(Scale);
				VolumeLimitB = node.Element(nameof(VolumeLimitB)).As(VolumeLimitB);
				VolumeLimitQ = node.Element(nameof(VolumeLimitQ)).As(VolumeLimitQ);
				PriceOffset  = node.Element(nameof(PriceOffset)).As(PriceOffset);
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(nameof(Pair), Pair, false);
				node.Add2(nameof(Exch0), Exch0, false);
				node.Add2(nameof(Exch1), Exch1, false);
				node.Add2(nameof(Scale), Scale, false);
				node.Add2(nameof(VolumeLimitB), VolumeLimitB, false);
				node.Add2(nameof(VolumeLimitQ), VolumeLimitQ, false);
				node.Add2(nameof(PriceOffset), PriceOffset, false);
				return node;
			}

			/// <summary>The name of the pair to trade</summary>
			public string Pair { get; set; }

			/// <summary>The name of the reference exchange</summary>
			public string Exch0 { get; set; }

			/// <summary>The name of the target exchange</summary>
			public string Exch1 { get; set; }

			/// <summary>The trade scale to use</summary>
			public decimal Scale { get; set; }

			/// <summary>The volume limit on the base currency</summary>
			public decimal VolumeLimitB { get; set; }

			/// <summary>The volume limit on the quote currency</summary>
			public decimal VolumeLimitQ { get; set; }

			/// <summary>The price offset range (as a fraction of the reference price)</summary>
			public RangeF PriceOffset { get; set; }

			/// <summary>True if this object contains valid data</summary>
			public bool Valid
			{
				get
				{
					return 
						Exch0 != Exch1 &&
						Pair.HasValue() &&
						Scale.Within(0m, 1m) &&
						PriceOffset.Beg < PriceOffset.End &&
						VolumeLimitB >= 0 &&
						VolumeLimitQ >= 0;
				}
			}
		}
	}

	public interface IExchangeSettings
	{
		/// <summary>True if the exchange is active</summary>
		bool Active { get; set; }

		/// <summary>Data polling rate (in ms)</summary>
		int PollPeriod { get; set; }

		/// <summary>The fee charged per trade</summary>
		decimal TransactionFee { get; set; }
	}
}
