using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using pr.common;
using pr.util;

namespace Rylobot
{
	/// <summary>Settings for instruments</summary>
	[TypeConverter(typeof(TyConv))]
	public class InstrumentSettings :SettingsBase<InstrumentSettings>
	{
		public InstrumentSettings()
		{
		}
		public InstrumentSettings(string filepath)
			:base(filepath)
		{ }
		public InstrumentSettings(XElement node)
			:base(node)
		{}

		private class TyConv :GenericTypeConverter<InstrumentSettings> {}
	}

}

