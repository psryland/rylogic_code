using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CoinFlip
{
	public class ChartContainer :ObservableCollection<IChartView>
	{
		public ChartContainer(Func<IChartView> create_chart_cb)
		{
			CreateChartCB = create_chart_cb;
		}

		/// <summary>Callback for creating new chart instances</summary>
		private Func<IChartView> CreateChartCB { get; }

		/// <summary>Return the first active chart, or if none are active, make the first one active</summary>
		public IChartView ActiveChart
		{
			get
			{
				// No charts? add one
				if (Count == 0)
					Add(CreateChartCB());

				// Find a chart that is visible
				var active = this.FirstOrDefault(x => x.IsActiveContentInPane);
				if (active != null)
					return active;

				// Make the first chart visible
				this[0].EnsureActiveContent();
				return this[0];
			}
		}
	}
}
