using System;
using System.Windows;
using System.Windows.Controls;
using Rylogic.Extn;
using Rylogic.Gui.WPF;

namespace CoinFlip.UI.GfxObjects
{
	public class RemainingTime :IDisposable
	{
		public RemainingTime(ChartControl chart, Instrument instrument)
		{
			Instrument = instrument;
			Label = new TextBlock
			{
				FontSize = 8,
				Foreground = chart.Options.BackgroundColour.ToMediaBrush(),
				Background = chart.Options.BackgroundColour.InvertBW(0xFF555555, 0xFFCCCCCC).ToMediaBrush(),
				Margin = new Thickness(3,0,3,0),
			};
		}
		public void Dispose()
		{
			Label.Detach();
		}

		/// <summary></summary>
		private Instrument Instrument { get; }

		/// <summary>The text label containing the remaining time</summary>
		public TextBlock Label { get; }

		/// <summary></summary>
		public void BuildScene(ChartControl chart)
		{
			if (Instrument != null && Instrument.Count != 0 && (Instrument.Count - 1.0).Within(chart.XAxis.Range))
			{
				Label.Text = (Instrument.Latest.CloseTime(Instrument.TimeFrame) - Model.UtcNow).ToPrettyString();
				Label.Measure(new Size(1000, 1000));

				var pt = chart.ChartToClient(new Point(Instrument.Count - 1, chart.YAxis.Min));
				Canvas.SetLeft(Label, pt.X - Label.DesiredSize.Width/2);
				Canvas.SetTop(Label, pt.Y - Label.DesiredSize.Height);
				chart.Overlay.Adopt(Label);
			}
			else
			{
				Label.Detach();
			}
		}
	}
}
