using System;
using System.ComponentModel;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.util;

namespace Rylobot
{
	/// <summary>Rylobot settings</summary>
	public sealed class Settings :SettingsBase<Settings>
	{
		public Settings()
		{
			InstrumentSettingsDir    = Util.ResolveAppDataPath("Rylogic", "Rylobot", ".\\Instruments");
			UILayout                 = null;
			MaxRiskPC                = 10.0;
			MinRewardToRisk          = 1.0;
			MaxRewardToRisk          = 7.0;
			LookBackCount            = 50;
			PredictionForecastLength = 100;
		}
		public Settings(string filepath)
			:base(filepath)
		{ }

		/// <summary>Settings version</summary>
		protected override string Version
		{
			get { return "v1.0"; }
		}

		/// <summary>The directory for account database files</summary>
		public string InstrumentSettingsDir
		{
			get { return get(x => x.InstrumentSettingsDir); }
			set { set(x => x.InstrumentSettingsDir, value); }
		}

		/// <summary>The dock panel layout</summary>
		public XElement UILayout
		{
			get { return get(x => x.UILayout); }
			set { set(x => x.UILayout, value); }
		}

		/// <summary>The percentage of the account balance to risk at any one time</summary>
		public double MaxRiskPC
		{
			get { return get(x => x.MaxRiskPC); }
			set { set(x => x.MaxRiskPC, value); }
		}

		/// <summary>The minimum reward to risk ratio for a trade (reward/risk) </summary>
		public double MinRewardToRisk
		{
			get { return get(x => x.MinRewardToRisk); }
			set { set(x => x.MinRewardToRisk, value); }
		}

		/// <summary>The maximum reward to risk ratio for a trade (reward/risk) </summary>
		public double MaxRewardToRisk
		{
			get { return get(x => x.MaxRewardToRisk); }
			set { set(x => x.MaxRewardToRisk, value); }
		}

		/// <summary>The number of candles to look back when setting a stop loss or take profit value</summary>
		public int LookBackCount
		{
			get { return get(x => x.LookBackCount); }
			set { set(x => x.LookBackCount, value); }
		}

		/// <summary>The number of candles after a prediction to monitor results</summary>
		public int PredictionForecastLength
		{
			get { return get(x => x.PredictionForecastLength); }
			set { set(x => x.PredictionForecastLength, value); }
		}
	}
}
