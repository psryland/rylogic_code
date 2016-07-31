using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pr.common;
using pr.util;
using Tradee;

namespace cAlgo
{
	public class Settings :SettingsBase<Settings>
	{
		public Settings()
		{
			Transmitters = new TransmitterSettings[0];
			DefaultTimeFrames = new [] {ETimeFrame.Min1, ETimeFrame.Hour1, ETimeFrame.Hour12};
		}
		public Settings(string filepath)
			:base(filepath)
		{}

		/// <summary>Settings version</summary>
		protected override string Version
		{
			get { return "v1.0"; }
		}

		/// <summary>The time frame to open new charts at</summary>
		public TransmitterSettings[] Transmitters
		{
			get { return get(x => x.Transmitters); }
			set { set(x => x.Transmitters, value); }
		}

		/// <summary>The time frame to open new charts at</summary>
		public ETimeFrame[] DefaultTimeFrames
		{
			get { return get(x => x.DefaultTimeFrames); }
			set { set(x => x.DefaultTimeFrames, value); }
		}
	}

	/// <summary>Settings associated with a connection to Rex via RexLink</summary>
	[TypeConverter(typeof(TransmitterSettings.TyConv))]
	public class TransmitterSettings :SettingsSet<TransmitterSettings>
	{
		public TransmitterSettings()
			:this(ETradePairs.None)
		{ }
		public TransmitterSettings(ETradePairs pair)
		{
			Pair       = pair;
			TimeFrames = new [] {ETimeFrame.Min1, ETimeFrame.Hour1, ETimeFrame.Hour12};
		}

		/// <summary>The trading pair that these settings are for</summary>
		public ETradePairs Pair
		{
			get { return get(x => x.Pair); }
			set { set(x => x.Pair, value); }
		}

		/// <summary>The time frame to open new charts at</summary>
		public ETimeFrame[] TimeFrames
		{
			get { return get(x => x.TimeFrames); }
			set { set(x => x.TimeFrames, value); }
		}

		private class TyConv :GenericTypeConverter<TransmitterSettings> {}
	}
}
