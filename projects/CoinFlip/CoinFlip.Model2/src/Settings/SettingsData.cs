﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rylogic.Common;

namespace CoinFlip.Settings
{
	[Serializable]
	public class SettingsData : SettingsBase<SettingsData>
	{
		/// <summary>Singleton access</summary>
		public static SettingsData Settings
		{
			get
			{
				if (m_settings == null)
				{
					// Ensure the app data directory exists
					Directory.CreateDirectory(Misc.ResolveUserPath());
					m_settings = new SettingsData(Misc.ResolveUserPath("settings.new.xml"));
				}
				return m_settings;
			}
		}
		private static SettingsData m_settings;

		public SettingsData()
		{
			LastUser = string.Empty;
			Skin = ESkin.Default;
			MainLoopPeriodMS = 500;
			PriceDataUpdatePeriodMS = 500;
			MarketOrderPriceToleranceFrac = 0.0001;
			//ShowLivePrices = false;
			//Equity = new EquitySettings();
			Coins = new CoinData[] { new CoinData("BTC", 1m, true), new CoinData("ETH", 1m, true) };
			Funds = new FundData[1] { new FundData(Fund.Main, new FundData.ExchData[0]) };
			Chart = new ChartSettings();
			BackTesting = new BackTestingSettings();
			//Bots = new BotData[0];
			//Cryptopia = new CrypotopiaSettings();
			Binance = new BinanceSettings();
			Poloniex = new PoloniexSettings();
			Bittrex = new BittrexSettings();
			//Bitfinex = new BitfinexSettings();
			CrossExchange = new CrossExchangeSettings();
			UI = new UISettings();

			// Leave this disabled till everything has been set up (i.e. the Model enables it)
			AutoSaveOnChanges = false;
		}
		public SettingsData(string filepath)
			: base(filepath, ESettingsLoadFlags.IgnoreUnknownTypes) //hack
		{
			// Leave this disabled till everything has been set up (i.e. the Model enables it)
			AutoSaveOnChanges = false;
		}

		/// <summary>The name of the last user to log on</summary>
		public string LastUser
		{
			get { return get<string>(nameof(LastUser)); }
			set { set(nameof(LastUser), value); }
		}

		/// <summary>The skin of the application</summary>
		public ESkin Skin
		{
			get { return get<ESkin>(nameof(Skin)); }
			set { set(nameof(Skin), value); }
		}

		/// <summary>The period at which searches for profitable loops occur</summary>
		public int MainLoopPeriodMS
		{
			get { return get<int>(nameof(MainLoopPeriodMS)); }
			set { set(nameof(MainLoopPeriodMS), value); }
		}

		/// <summary>The polling period for reading candle data</summary>
		public int PriceDataUpdatePeriodMS
		{
			get { return get<int>(nameof(PriceDataUpdatePeriodMS)); }
			set { set(nameof(PriceDataUpdatePeriodMS), value); }
		}

		/// <summary>An order is considered 'MarketOrder' if the order price is within this tolerance of the current price to fill the order</summary>
		public double MarketOrderPriceToleranceFrac
		{
			get { return get<double>(nameof(MarketOrderPriceToleranceFrac)); }
			set { set(nameof(MarketOrderPriceToleranceFrac), value); }
		}
		public double MarketOrderPriceTolerancePC
		{
			get { return MarketOrderPriceToleranceFrac * 100.0; }
			set { MarketOrderPriceToleranceFrac = value * 0.01; }
		}

		/// <summary>Meta data for known coins</summary>
		public CoinData[] Coins
		{
			get { return get<CoinData[]>(nameof(Coins)); }
			set { set(nameof(Coins), value); }
		}

		/// <summary>The partitions of user funds</summary>
		public FundData[] Funds
		{
			get { return get<FundData[]>(nameof(Funds)); }
			set { set(nameof(Funds), value); }
		}

		/// <summary>Setting for the candle charts</summary>
		public ChartSettings Chart
		{
			get { return get<ChartSettings>(nameof(Chart)); }
			set { set(nameof(Chart), value); }
		}

		/// <summary>Settings related to back testing</summary>
		public BackTestingSettings BackTesting
		{
			get { return get<BackTestingSettings>(nameof(BackTesting)); }
			set { set(nameof(BackTesting), value); }
		}

		/// <summary>Binance exchange settings</summary>
		public BinanceSettings Binance
		{
			get { return get<BinanceSettings>(nameof(Binance)); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(nameof(Binance), value);
			}
		}

		/// <summary>Poloniex exchange settings</summary>
		public PoloniexSettings Poloniex
		{
			get { return get<PoloniexSettings>(nameof(Poloniex)); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(nameof(Poloniex), value);
			}
		}

		/// <summary>Bittrex exchange settings</summary>
		public BittrexSettings Bittrex
		{
			get { return get<BittrexSettings>(nameof(Bittrex)); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(nameof(Bittrex), value);
			}
		}

		/// <summary>CrossExchange exchange settings</summary>
		public CrossExchangeSettings CrossExchange
		{
			get { return get<CrossExchangeSettings>(nameof(CrossExchange)); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(nameof(CrossExchange), value);
			}
		}

		/// <summary>UI settings</summary>
		public UISettings UI
		{
			get { return get<UISettings>(nameof(UI)); }
			set
			{
				if (value == null) throw new ArgumentNullException();
				set(nameof(UI), value);
			}
		}
	}
}