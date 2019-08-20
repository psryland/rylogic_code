using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Gfx;

namespace CoinFlip.Settings
{
	public class EquitySettings :SettingsXml<EquitySettings>
	{
		public EquitySettings()
		{
		}
		public EquitySettings(XElement node)
			: base(node)
		{ }
	}
}
