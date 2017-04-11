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
			UILayout                 = null;
			MaxDrawDownFrac          = 0.5;
			MaxRiskFrac              = 0.01;
			RewardToRisk             = new RangeF(0.1, 3.0);
			LookBackCount            = 50;
			SnRHistoryLength         = 200;
			PeaksHistoryLength       = 200;
			SlowEMAPeriods           = 200;
			PredictionForecastLength = 100;

			AutoSaveOnChanges = true;
		}
		public Settings(string filepath)
			:base(filepath)
		{
			AutoSaveOnChanges = true;
		}

		/// <summary>Settings version</summary>
		protected override string Version
		{
			get { return "v1.0"; }
		}

		/// <summary>Filepath for Rylobot settings</summary>
		public static new string DefaultFilepath
		{
			get { return Util.ResolveUserDocumentsPath("Rylogic","Rylobot","Settings.xml"); }
		}

		/// <summary>The dock panel layout</summary>
		public XElement UILayout
		{
			get { return get(x => x.UILayout); }
			set { set(x => x.UILayout, value); }
		}

		/// <summary>The maximum account draw down before emergency stop</summary>
		public double MaxDrawDownFrac
		{
			get { return get(x => x.MaxDrawDownFrac); }
			set { set(x => x.MaxDrawDownFrac, value); }
		}

		/// <summary>The percentage of the account balance to risk at any one time</summary>
		public double MaxRiskFrac
		{
			get { return get(x => x.MaxRiskFrac); }
			set { set(x => x.MaxRiskFrac, value); }
		}

		/// <summary>The range of reward to risk ratios for a trade (reward/risk) </summary>
		public RangeF RewardToRisk
		{
			get { return get(x => x.RewardToRisk); }
			set { set(x => x.RewardToRisk, value); }
		}

		/// <summary>The number of candles to look back when setting a stop loss or take profit value</summary>
		public int LookBackCount
		{
			get { return get(x => x.LookBackCount); }
			set { set(x => x.LookBackCount, value); }
		}

		/// <summary>The number of candles that should contribute to the SnR level detection</summary>
		public int SnRHistoryLength
		{
			get { return get(x => x.SnRHistoryLength); }
			set { set(x => x.SnRHistoryLength, value); }
		}

		/// <summary>The number of candles that should contribute to the peak price detection</summary>
		public int PeaksHistoryLength
		{
			get { return get(x => x.PeaksHistoryLength); }
			set { set(x => x.PeaksHistoryLength, value); }
		}

		/// <summary>The number of periods to use for a slow EMA</summary>
		public int SlowEMAPeriods
		{
			get { return get(x => x.SlowEMAPeriods); }
			set { set(x => x.SlowEMAPeriods, value); }
		}

		/// <summary>The number of candles after a prediction to monitor results</summary>
		public int PredictionForecastLength
		{
			get { return get(x => x.PredictionForecastLength); }
			set { set(x => x.PredictionForecastLength, value); }
		}
	}
}
