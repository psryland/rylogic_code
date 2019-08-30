using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rylogic.Gui.WPF;

namespace CoinFlip.UI.GfxObjects
{
	public class UpdatingText :IDisposable
	{
		public UpdatingText(ChartControl chart)
		{
			Text = new TextBlock
			{
				Text = "...updating...",
				Background = Brushes.Transparent,
				Foreground = chart.Options.BackgroundColour.InvertBW().ToMediaBrush(),
				IsHitTestVisible = false,
			};
		}
		public void Dispose()
		{
		}

		/// <summary>The text element</summary>
		private TextBlock Text { get; }

		/// <summary>Display the updating text</summary>
		public void BuildScene(bool updating, ChartControl chart)
		{
			if (updating)
			{
				var pt = chart.ChartToClient(new Point(chart.XAxis.Max, chart.YAxis.Max));
				Canvas.SetLeft(Text, pt.X - Text.RenderSize.Width);
				Canvas.SetTop(Text, pt.Y);
				chart.Overlay.Adopt(Text);
			}
			else
			{
				Text.Detach();
			}
		}
	}
}
