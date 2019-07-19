using System;
using System.ComponentModel;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Utility;

namespace CoinFlip.Settings
{
	[TypeConverter(typeof(TyConv))]
	public class BotData :SettingsXml<BotData>
	{
		public BotData()
		{
			Id = Guid.Empty;
			TypeName = string.Empty;
			FundId = string.Empty;
			Name = string.Empty;
			Active = false;
			BackTesting = false;
		}
		public BotData(XElement node)
			:base(node)
		{ }

		/// <summary>A unique ID assigned to each bot instance</summary>
		public Guid Id
		{
			get { return get<Guid>(nameof(Id)); }
			set { set(nameof(Id), value); }
		}

		/// <summary>The full type name of the Bot instance type</summary>
		public string TypeName
		{
			get { return get<string>(nameof(TypeName)); }
			set { set(nameof(TypeName), value); }
		}

		/// <summary>The fund that this bot uses</summary>
		public string FundId
		{
			get { return get<string>(nameof(FundId)); }
			set { set(nameof(FundId), value); }
		}

		/// <summary>User assigned name for the bot</summary>
		public string Name
		{
			get { return get<string>(nameof(Name)); }
			set { set(nameof(Name), value); }
		}

		/// <summary>True if the bot should be activated when created</summary>
		public bool Active
		{
			get { return get<bool>(nameof(Active)); }
			set { set(nameof(Active), value); }
		}

		/// <summary>True if this bot is used with live data</summary>
		public bool BackTesting
		{
			get { return get<bool>(nameof(BackTesting)); }
			set { set(nameof(BackTesting), value); }
		}

		private class TyConv : GenericTypeConverter<BotData> { }
	}
}
