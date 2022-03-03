using System;
using System.Windows;
using System.Windows.Controls;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Maths;

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
			if (Instrument is Instrument instrument && instrument.Latest is Candle latest && (Instrument.Count - 1.0).Within(chart.XAxis.Range))
			{
				Label.Text = (latest.CloseTime(Instrument.TimeFrame) - Model.UtcNow).ToPrettyString();
				Label.Measure(new Size(1000, 1000));

				var pt = chart.ChartToScene(new v4(instrument.Count - 1, (float)chart.YAxis.Min, 0, 1));
				Canvas.SetLeft(Label, pt.x - Label.DesiredSize.Width/2);
				Canvas.SetTop(Label, pt.y - Label.DesiredSize.Height);
				chart.Overlay.Adopt(Label);
			}
			else
			{
				Label.Detach();
			}
		}
	}
}
