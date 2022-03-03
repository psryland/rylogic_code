using System;

namespace CoinFlip
{
	/// <summary>Interface to an available chart</summary>
	public interface IChartView
	{
		/// <summary>The title bar name of the chart</summary>
		string ChartName { get; }

		/// <summary>The currently selected Exchange</summary>
		Exchange? Exchange { get; set; }

		/// <summary>The currently selected trade pair</summary>
		TradePair? Pair { get; set; }

		/// <summary>The currently selected time frame</summary>
		ETimeFrame TimeFrame { get; set; }

		/// <summary>The instrument displayed on the chart</summary>
		Instrument? Instrument { get; }

		/// <summary>True if this chart is at the front of it's dock pane</summary>
		bool IsActiveContentInPane { get; }

		/// <summary>Bring this chart to the front of any dock pane it's in</summary>
		void EnsureActiveContent();

		/// <summary>Scroll the chart to make 'time' visible</summary>
		void ScrollToTime(DateTimeOffset time);
	}
}
