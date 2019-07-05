using System;
using System.ComponentModel;
using System.Xml.Linq;
using CoinFlip.Bots;
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
			Active = false;
		}
		public BotData(IBot bot)
		{
			Id = bot.Id;
			TypeName = bot.GetType().FullName;
			Active = bot.Active;
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

		/// <summary>True if the bot should be activated when created</summary>
		public bool Active
		{
			get { return get<bool>(nameof(Active)); }
			set { set(nameof(Active), value); }
		}

		private class TyConv : GenericTypeConverter<BotData> { }
	}
}
