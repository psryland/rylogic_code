using System.ComponentModel;
using pr.common;
using pr.util;

namespace Tradee
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

		/// <summary>The time frames that new transmitters start with</summary>
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
			:this(string.Empty)
		{ }
		public TransmitterSettings(string sym)
		{
			SymbolCode = sym;
			TimeFrames = new [] {ETimeFrame.Min10, ETimeFrame.Hour1, ETimeFrame.Hour12};
		}

		/// <summary>The trading pair that these settings are for</summary>
		public string SymbolCode
		{
			get { return get(x => x.SymbolCode); }
			set { set(x => x.SymbolCode, value); }
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
