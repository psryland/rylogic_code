using System;
using Rylogic.Common;
using Rylogic.Gfx;

namespace CoinFlip
{
	public interface IIndicator :ISettingsSet, IDisposable
	{
		/// <summary>Instance id for the indicator. Used to tell multiple instances of the same indicator apart</summary>
		Guid Id { get; }

		/// <summary>Colour of the indicator line</summary>
		Colour32 Colour { get; }

		/// <summary>Create a view of this indicator for displaying on a chart</summary>
		IIndicatorView CreateView(IChartView chart);
	}

	public interface IIndicatorView :IDisposable
	{
		/// <summary>The id of the indicator this is a view of</summary>
		Guid IndicatorId { get; }

		/// <summary>Name of the indicators</summary>
		string Name { get; }

		/// <summary>The main colour of the indicator</summary>
		Colour32 Colour { get; }

		/// <summary>True when the indicator is selected</summary>
		bool Selected { get; set; }

		/// <summary></summary>
		void BuildScene(IChartView chart);
	}
}
