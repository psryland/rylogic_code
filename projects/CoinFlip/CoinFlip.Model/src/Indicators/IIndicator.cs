using System;
using System.ComponentModel;
using Rylogic.Common;
using Rylogic.Gfx;

namespace CoinFlip
{
	public interface IIndicator :ISettingsSet, IDisposable
	{
		/// <summary>Instance id for the indicator. Used to tell multiple instances of the same indicator apart</summary>
		Guid Id { get; }

		/// <summary>User assigned name for the indicator</summary>
		string Name { get; }

		/// <summary>The label to use when displaying this indicator</summary>
		string Label { get; }

		/// <summary>Colour of the indicator line</summary>
		Colour32 Colour { get; set; }

		/// <summary>Show this indicator</summary>
		bool Visible { get; set; }

		/// <summary>Create a view of this indicator for displaying on a chart</summary>
		IIndicatorView CreateView(IChartView chart);
	}

	public interface IIndicatorView :IDisposable, INotifyPropertyChanged
	{
		/// <summary>The id of the indicator this is a view of</summary>
		Guid IndicatorId { get; }

		/// <summary>Description/Name of the indicator</summary>
		string Label { get; }

		/// <summary>The main colour of the indicator</summary>
		Colour32 Colour { get; }

		/// <summary>True when the indicator is selected</summary>
		bool Selected { get; set; }

		/// <summary>True when the indicator is visible</summary>
		bool Visible { get; set; }

		/// <summary></summary>
		void BuildScene(IChartView chart);

		/// <summary>Display the options UI</summary>
		void ShowOptionsUI();
	}
}
