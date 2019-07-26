using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Gfx;

namespace CoinFlip.Settings
{
	public class EquitySettings :SettingsXml<EquitySettings>
	{
		public EquitySettings()
		{
			NettWorthColour = 0xffbbd2eb;
			XAxisLabelMode = EXAxisLabelMode.LocalTime;
		}
		public EquitySettings(XElement node)
			: base(node)
		{ }

		/// <summary>The colour to draw the Nett Value region with</summary>
		public Colour32 NettWorthColour
		{
			get { return get<Colour32>(nameof(NettWorthColour)); }
			set { set(nameof(NettWorthColour), value); }
		}

		/// <summary>The format of tick text on the chart X axis</summary>
		public EXAxisLabelMode XAxisLabelMode
		{
			get { return get<EXAxisLabelMode>(nameof(XAxisLabelMode)); }
			set { set(nameof(XAxisLabelMode), value); }
		}

	}
}
